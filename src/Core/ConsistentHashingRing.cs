namespace Core;

public class ConsistentHashRing
{
    private readonly int _virtualNodes;
    private readonly Dictionary<uint, int> _owners = new(); // stores virtual ring positions and their physical owners.
    private readonly HashSet<int> _nodes = new(); //Store Physical Node Id's
    private uint[] _positions = Array.Empty<uint>();  //sorted copy of all the virtual-node positions currently on the ring.

    public ConsistentHashRing(int virtualNodes = 10)
    {
        if (virtualNodes <= 0)
            throw new ArgumentOutOfRangeException(nameof(virtualNodes), "Must be positive.");
        _virtualNodes = virtualNodes;
    }

    public IReadOnlyCollection<int> Nodes => _nodes.OrderBy(n => n).ToArray();

    public bool Contains(int nodeId) => _nodes.Contains(nodeId);

    public bool AddNode(int nodeId)      //Adding a physical node
    {
        if (!_nodes.Add(nodeId))
            return false;                                    // already on the ring

        //Add virtual nodes of this newly created Physical node
        for (int i = 0; i < _virtualNodes; i++)
        {
            string label = $"{nodeId}#{i}";
            uint pos = HashHelper.Hash(label);

            int salt = 0;
            while (_owners.ContainsKey(pos))            // rare collision: probe deterministically
                pos = HashHelper.Hash($"{label}#{++salt}");

            _owners[pos] = nodeId;
        }

        RebuildPositions();
        return true;
    }

    public bool RemoveNode(int nodeId)     // Removing a Physical node 
    {
        if (!_nodes.Remove(nodeId))
            return false;

        var mine = _owners.Where(kv => kv.Value == nodeId).Select(kv => kv.Key).ToList();
        foreach (uint pos in mine)
            _owners.Remove(pos);

        RebuildPositions();
        return true;
    }

    // Used By Master to write a key . It helps master find the first node in Clockwise direction
    public int GetNode(string key)
    {
        if (_positions.Length == 0)
            throw new InvalidOperationException("The ring has no nodes.");

        uint hash = HashHelper.Hash(key);

        int index = Array.BinarySearch(_positions, hash);
        if (index < 0)
            index = ~index;                                  // not found: complement = next larger element
        if (index == _positions.Length)
            index = 0;                                       // ran past the end: wrap around

        return _owners[_positions[index]];
    }

    private void RebuildPositions()
    {
        _positions = _owners.Keys.ToArray();
        Array.Sort(_positions);
    }
}