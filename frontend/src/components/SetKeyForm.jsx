import { useState } from 'react'

export default function SetKeyForm({ busy, onSet }) {
  const [key, setKeyText] = useState('')
  const [value, setValue] = useState('')

  async function handleSubmit(event) {
    event.preventDefault()
    const stored = await onSet(key.trim(), value)
    if (stored) {
      setKeyText('')
      setValue('')
    }
  }

  return (
    <form className="panel" onSubmit={handleSubmit}>
      <h2>Set a key</h2>
      <div className="row">
        <input
          placeholder="key"
          value={key}
          onChange={(event) => setKeyText(event.target.value)}
          disabled={busy}
          required
        />
        <input
          placeholder="value"
          value={value}
          onChange={(event) => setValue(event.target.value)}
          disabled={busy}
          required
        />
        <button type="submit" disabled={busy}>
          Set
        </button>
      </div>
    </form>
  )
}
