import SlaveCard from './SlaveCard'

export default function SlaveGrid({ slaves, capacity, busy, onRemove }) {
  if (slaves.length === 0) {
    return <p className="muted">No slaves registered.</p>
  }

  return (
    <section className="grid">
      {slaves.map((slave) => (
        <SlaveCard
          key={slave.id}
          slave={slave}
          capacity={capacity}
          busy={busy}
          onRemove={onRemove}
        />
      ))}
    </section>
  )
}
