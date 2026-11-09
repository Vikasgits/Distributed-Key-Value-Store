import { useState } from 'react'
import { getKey } from '../api/kvapi'

export default function GetKeyForm({ busy }) {
  const [key, setKeyText] = useState('')
  const [looking, setLooking] = useState(false)
  const [result, setResult] = useState(null) 

  async function handleSubmit(event) {
    event.preventDefault()
    const wanted = key.trim()
    setLooking(true)
    try {
      setResult({ key: wanted, value: await getKey(wanted) })
    } catch (error) {
      setResult({ error: error.message })
    } finally {
      setLooking(false)
    }
  }

  return (
    <form className="panel" onSubmit={handleSubmit}>
      <h2>Get a key</h2>
      <div className="row">
        <input
          placeholder="key"
          value={key}
          onChange={(event) => setKeyText(event.target.value)}
          disabled={busy || looking}
          required
        />
        <button type="submit" disabled={busy || looking}>
          Get
        </button>
      </div>

      {result?.error && <p className="unreachable">{result.error}</p>}
      {result && !result.error && (
        <p>
          {result.value === null ? (
            <span className="muted">&ldquo;{result.key}&rdquo; not found (never set, or evicted).</span>
          ) : (
            <>
              <strong>{result.key}</strong> = {result.value}
            </>
          )}
        </p>
      )}
    </form>
  )
}
