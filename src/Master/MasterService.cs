using Core;

namespace Master;

public class MasterService
{
    private readonly ConsistentHashRing _ring;
    private readonly SlaveRegistry _registry; // Handling of slaves 
    private readonly SlaveClient _client;

    private const int InitialSlaves = 3;
    private const int BasePort = 5000; 

    private readonly ReaderWriterLockSlim _ringLock = new(); //Allow multiple readers or a single writer but not both
    private readonly SemaphoreSlim _scaleLock = new(1, 1);
    public MasterService(ConsistentHashRing ring, SlaveRegistry registry, SlaveClient client)
    {
        _ring = ring;
        _registry = registry;
        _client = client;

        for (int id = 1; id <= InitialSlaves; id++)
        {
            _registry.Register(id, $"http://localhost:{BasePort + id}");
            _ring.AddNode(id);
        }
    }

    public async Task<int?> SetAsync(string key, string value)
    {
        int id;
        string url;

        _ringLock.EnterReadLock();                           
        try
        {
            id = _ring.GetNode(key);                        //1. Get the node that is going to store the key           // raw key; the ring hashes it once
            url = _registry.GetUrl(id);                     //2. Get the url of the node where it is hosted 
        }
        finally { _ringLock.ExitReadLock(); }

        bool stored = await _client.SetAsync(url, key, value);  
        return stored ? id : null;
    }

    public async Task<string?> GetAsync(string key)
    {
        string url;

        _ringLock.EnterReadLock();
        try
        {
            int id = _ring.GetNode(key);
            url = _registry.GetUrl(id);
        }
        finally { _ringLock.ExitReadLock(); }

        return await _client.GetAsync(url, key);
    }

    public IReadOnlyList<int> GetSlaves() => _registry.Ids;

    public async Task<Dictionary<string, string>> GetSlaveDataAsync(int slaveId)
    {
        string url = _registry.GetUrl(slaveId);
        return await _client.GetDataAsync(url);
    }

    public async Task<IReadOnlyList<int>> AddSlaveAsync()
    {
        await _scaleLock.WaitAsync();
        try
        {
            IReadOnlyList<int> oldIds = _registry.Ids;
            int newId = oldIds.Count == 0 ? 1 : oldIds.Max() + 1;      // Calculate the port where new slave can be hosted 
            string newUrl = $"http://localhost:{BasePort + newId}";

            // Fail before changing anything if the new process isn't up (throws if unreachable).
            await _client.GetDataAsync(newUrl);

            _ringLock.EnterWriteLock();
            try
            {
                _registry.Register(newId, newUrl);
                _ring.AddNode(newId);
            }
            finally { _ringLock.ExitWriteLock(); }

            // Move every key the new node now owns off its old owner.
            foreach (int oldId in oldIds)
            {
                string oldUrl = _registry.GetUrl(oldId);
                Dictionary<string, string> data = await _client.GetDataAsync(oldUrl);

                foreach (string key in data.Keys)
                {
                    if (_ring.GetNode(key) != newId)
                        continue;                              // this key stays where it is

                    string? value = await _client.PopAsync(oldUrl, key);
                    if (value is null)
                        continue;                              // evicted meanwhile: normal for a cache

                    if (!await _client.SetAsync(newUrl, key, value))
                        throw new InvalidOperationException($"Slave {newId} rejected key '{key}' during migration.");
                }
            }

            return _registry.Ids;
        }
        finally { _scaleLock.Release(); }
    }

    public async Task<IReadOnlyList<int>> RemoveSlaveAsync(int slaveId)
    {
        await _scaleLock.WaitAsync();
        try
        {
            IReadOnlyList<int> ids = _registry.Ids;
            if (!ids.Contains(slaveId))
                throw new KeyNotFoundException($"Slave {slaveId} is not registered.");
            if (ids.Count == 1)
                throw new InvalidOperationException("Cannot remove the last slave.");

            // 1. Read what the leaving node holds BEFORE touching the ring or registry.
            string leavingUrl = _registry.GetUrl(slaveId);
            Dictionary<string, string> data = await _client.GetDataAsync(leavingUrl);

            // 2. Take it off the ring, so GetNode now returns each key's new owner.
            _ringLock.EnterWriteLock();
            try { _ring.RemoveNode(slaveId); }
            finally { _ringLock.ExitWriteLock(); }

            // 3. Re-home every key.
            foreach (var (key, value) in data)
            {
                string newUrl = _registry.GetUrl(_ring.GetNode(key));
                if (!await _client.SetAsync(newUrl, key, value))
                    throw new InvalidOperationException($"A slave rejected key '{key}' during migration.");
            }

            // 4. Forget the address LAST.
            _registry.UnRegister(slaveId);
            return _registry.Ids;
        }
        finally { _scaleLock.Release(); }
    }
}