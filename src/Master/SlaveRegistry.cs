namespace Master;

//After we get the node where the key is sored SlaveRegistry helps us find the url of the node where 
// value corresponding to a  key is hosted
//key ──▶ ring.GetNode(key) ──▶ 2 ──▶ registry.GetUrl(2) ──▶ http://localhost:5002 ──▶ HTTP call to Slave 2
public class SlaveRegistry
{
    private readonly Dictionary<int, string > _address = new();
    private readonly object _lock = new();
    public bool Register(int slaveId, string url)
    {
        if (string.IsNullOrEmpty(url)) throw new ArgumentException("URL is required.", nameof(url));

        string normalized = url.Trim().TrimEnd('/'); 

         lock (_lock)
        {
            if (_address.ContainsKey(slaveId))
                return false;                                 // never overwrite silently

            _address[slaveId] = normalized;
            return true;
        }
    }

    public bool UnRegister(int slaveId)
    {
         lock (_lock)
        {
            return _address.Remove(slaveId);  
        }
        
    }

    public string GetUrl(int slaveId)
    {
        lock (_lock)
        {
            if (_address.TryGetValue(slaveId, out var url))
                return url;
        }
        throw new KeyNotFoundException($"Slave {slaveId} is not registered.");
    }

    public IReadOnlyList<int> Ids
    {
        get
        {
            lock (_lock)
            {
                var ids = _address.Keys.ToList();                // copy INSIDE the lock
                ids.Sort();
                return ids;
            }
        }
    }
}