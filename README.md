# Distributed-Key-Value-Store
Built a distributed in-memory key-value store in C#/.NET and React: consistent hashing with virtual nodes for sharding, per-node LRU eviction, and live node add/remove with key migration.

# Distributed Key-Value Store

A distributed in-memory key-value store built with C#/.NET and React. A **Master** service shards keys across several **Slave** cache nodes using **consistent hashing**. Each Slave evicts with an **LRU** policy. Nodes can be added or removed at runtime, and the affected keys are migrated automatically. A React dashboard shows each node's contents.

## Setup

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (current LTS) and npm

### Run order
Open one terminal per process, from the repo root. **Start the slaves before the Master.**

1. **Slaves.** Node `N` always listens on port `5000 + N`.
   ```bash
   dotnet run --project src/Slave -- --urls http://localhost:5001 --SlaveId 1
   dotnet run --project src/Slave -- --urls http://localhost:5002 --SlaveId 2
   dotnet run --project src/Slave -- --urls http://localhost:5003 --SlaveId 3
   ```
2. **Master** (port 5000). It registers slaves 1 to 3 at startup.
   ```bash
   dotnet run --project src/Master
   ```
3. **Frontend** (port 3000).
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
4. Open http://localhost:3000.

The Master's CORS policy allows only `http://localhost:3000`, so the frontend must run on that port.

### Quick check without the UI
```bash
curl "http://localhost:5000/setkey?key=name&value=alice"
curl "http://localhost:5000/getkey?key=name"
```

### Scaling at runtime
- **Add a slave:** first start its process (for the next id, N, run the same command with port `5000 + N`), then click **Add slave**. The Master does not launch processes.
- **Remove a slave:** click **Remove** on its card. Its keys move to the remaining slaves. The process keeps running and is not cleared, so stop it yourself.

## How it works

```
 React UI ──HTTP──▶ MASTER (:5000) ──HTTP──▶ SLAVE 1 (:5001) ── LRU cache
  (:3000)           ring + registry  ├─────▶ SLAVE 2 (:5002) ── LRU cache
                                     └─────▶ SLAVE 3 (:5003) ── LRU cache
```

These are separate processes that talk only over HTTP. Each Slave owns its own memory. The Master owns the routing logic but stores no data.

## Modules

### `src/Core` (shared library, no HTTP)
- **`LRUCache<TKey,TValue>`**: a fixed-capacity cache (10 entries per Slave). On insert into a full cache it evicts the least recently used key. It offers `Set`, `TryGet`, `TryRemove`, `Snapshot`, `Count` and `Capacity`. Lookups mark a key as recently used.
- **`ConsistentHashRing`**: places each node on a ring at several virtual points and answers *which node owns this key?* by walking clockwise from the key's hash. Adding or removing a node moves only about `1/N` of the keys, unlike `hash % N`, which reshuffles almost everything.
- **`HashHelper`**: a stable MD5-based hash. .NET's `GetHashCode()` is randomized per process, so it can't be used for routing.

### `src/Slave` (one cache node)
A small web API wrapping one `LRUCache`. It knows nothing about other nodes or the ring.

| Endpoint | Purpose |
|---|---|
| `GET /setKey?key=&value=` | store or update a value |
| `GET /getKey?key=` | read a value (404 on a miss) |
| `GET /pop?key=` | remove and return a key, used during migration |
| `GET /getData` | dump all entries for the dashboard and migration |

### `src/Master` (router and coordinator)
- **`SlaveRegistry`**: maps a node id to its URL and lists the active nodes. The ring says *who*, the registry says *where*.
- **`SlaveClient`**: the HTTP client used to call Slaves. A 404 means a miss. Network failures surface as errors.
- **`MasterService`**: the logic that combines the three: `SetAsync` and `GetAsync` route through the ring, and `AddSlaveAsync` and `RemoveSlaveAsync` migrate keys. Scaling operations run one at a time.
- **`Program.cs`**: the public API, CORS, and mapping of errors to HTTP statuses.

| Endpoint | Purpose |
|---|---|
| `GET /setkey?key=&value=` | store a key (returns which slave holds it) |
| `GET /getkey?key=` | read a key (404 on a miss) |
| `GET /api/slaves` | list active slave ids |
| `GET /api/slave/data?slaveId=` | one slave's contents |
| `GET /api/addSlave` | add the next slave and migrate keys to it |
| `GET /api/popSlave?slaveId=` | remove a slave and migrate its keys away |

### `frontend` (React + Vite)
A dashboard with one card per slave (its keys and fill level), forms to set and get a key, and Add/Remove controls. All HTTP calls live in `src/api/kvapi.js`. `App.jsx` owns the state, and every action ends with a refresh of the whole cluster view.

## Request flows

**Write** (`set name=alice`):
1. The client calls the Master.
2. The ring picks the owner, for example node 2.
3. The registry gives node 2's URL.
4. The Master calls that Slave, which stores the value in its LRU cache.
5. The response says which slave stored it.

**Read:** the same routing, so the same key always reaches the same node. A miss means the key was never set, or the LRU evicted it.

**Add a slave:**
1. The Master checks the new process is reachable.
2. It adds the node to the ring and registry.
3. For each existing slave, it finds the keys the new node now owns.
4. For each of those keys, it pops it from the old owner and sets it on the new one.

**Remove a slave:**
1. The Master reads the leaving slave's data.
2. It removes the node from the ring.
3. It re-sets each key on its new owner.
4. It unregisters the slave.

## Limitations
- **Data is in memory only.** A restarted slave loses its keys.
- **No replication or fault tolerance.** If a slave dies, its keys are gone, and requests routed to it return 502.
- **Eviction can lose data during scaling.** Each slave holds 10 keys, so removing a node from a nearly full cluster evicts keys, and the Master still reports success.
- **No rollback.** If migration fails halfway, some keys are moved and others are not.
- **Brief misses during scaling.** Keys in flight can return "not found".
- **The Master is a single point of failure.** Its ring and registry are in memory.
- **The Master does not start or stop slave processes.**
- Writes use `GET` for compatibility with the original design.


```
