namespace Core;


public class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity=0;
    private readonly Dictionary<TKey, TValue>_hashTable;
    private readonly List<TKey> _order = new();

    public LRUCache(int capacity=10){
        if(capacity <=0){
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive. LRUCache.cs");
        }
        _hashTable = new Dictionary<TKey, TValue>();
        _capacity = capacity;
    }

    public int Count
    {
        get { return _hashTable.Count; }
    }

    public int Capacity
    {
        get { return _capacity; }
    }

    public void Set(TKey key, TValue value){
          if (_hashTable.ContainsKey(key)){    //If key is already present, just update it freshness
                _hashTable[key] = value;
                _order.Remove(key);
                _order.Add(key);
                return;
            }
            if (_hashTable.Count >=_capacity){   // Remove when the capacity is full
                TKey leastRecentlyUsed = _order[0];
                _order.RemoveAt(0);
                _hashTable.Remove(leastRecentlyUsed);
            }
            _hashTable.Add(key, value);
            _order.Add(key);
    }

    public bool TryGet(TKey key, out TValue? value){
        if (!_hashTable.TryGetValue(key, out value)) //when there is a miss
        {
            value= default;
            return false;
        }
        _order.Remove(key);
        _order.Add(key);
        return true;
    }

    public bool TryRemove(TKey key, out TValue? value) // When a slave is added or removed, the Master must take a key out of the old owner and put it on the new one. Removing returns the value so it can be re-set elsewhere.
    {
        if(!_hashTable.TryGetValue(key, out value))
        {
            value= default;
            return false;
        }
        _order.Remove(key);
        _hashTable.Remove(key);
        return true;
    }

    public IReadOnlyList<KeyValuePair<TKey, TValue>> Snapshot()
    {
        var snapshot = new List<KeyValuePair<TKey, TValue>>();

        foreach (var key in _order)
        {
            if (_hashTable.TryGetValue(key, out var value))
            {
                snapshot.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        return snapshot;
    }


}

// TryGetValue has signature public bool TryGetValue(TKey key, out TValue value);
//It provides two outputs:
// 1. A boolean indicating whether the key exists.
// 2. The corresponding value through the out parameter.