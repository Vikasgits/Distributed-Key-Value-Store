const BASE_PORT = 5000 // must match BasePort in MasterService

export default function ToolBar({ busy, nextSlaveId, onAdd, onRefresh }) {
  return (
    <div className="toolbar">
      <button type="button" disabled={busy} onClick={onAdd}>
        Add slave {nextSlaveId}
      </button>
      <button type="button" className="secondary" disabled={busy} onClick={onRefresh}>
        Refresh
      </button>
      <span className="muted">
        Start the slave first: <code>--urls http://localhost:{BASE_PORT + nextSlaveId} --SlaveId {nextSlaveId}</code>
      </span>
    </div>
  )
}
