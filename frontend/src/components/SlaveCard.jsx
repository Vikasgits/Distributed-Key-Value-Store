export default function SlaveCard({ slave, capacity, busy, onRemove }) {
  const entries = slave.data
    ? Object.entries(slave.data).sort(([a], [b]) => a.localeCompare(b))
    : []
  const fill = Math.min(100, (entries.length / capacity) * 100)

  function handleRemove() {
    const message = `Remove slave ${slave.id}? Its keys will be moved to the remaining slaves.`
    if (window.confirm(message)) onRemove(slave.id)
  }

  return (
    <article className="card">
      <header className="card-header">
        <h3>Slave {slave.id}</h3>
        <button type="button" className="danger" disabled={busy} onClick={handleRemove}>
          Remove
        </button>
      </header>

      {slave.error ? (
        <p className="unreachable">Unreachable: {slave.error}</p>
      ) : (
        <>
          <div className="meter" aria-label={`${entries.length} of ${capacity} slots used`}>
            <div className="meter-fill" style={{ width: `${fill}%` }} />
          </div>
          <p className="muted">
            {entries.length} / {capacity} keys
          </p>

          {entries.length === 0 ? (
            <p className="muted">Empty</p>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Key</th>
                  <th>Value</th>
                </tr>
              </thead>
              <tbody>
                {entries.map(([key, value]) => (
                  <tr key={key}>
                    <td>{key}</td>
                    <td>{value}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </article>
  )
}
