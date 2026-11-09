import { useEffect, useState } from 'react'
import { addSlave, getSlaveData, getSlaves, removeSlave, setKey } from './api/kvapi'
import GetKeyForm from './components/GetKeyForm'
import SetKeyForm from './components/SetKeyForm'
import SlaveGrid from './components/SlaveGrid'
import ToolBar from './components/ToolBar'
import './App.css'

const SLAVE_CAPACITY = 10 


async function loadSlaves() {
  const ids = await getSlaves()
  return Promise.all(
    ids.map(async (id) => {
      try {
        return { id, data: await getSlaveData(id), error: null }
      } catch (error) {
        return { id, data: null, error: error.message }
      }
    }),
  )
}

export default function App() {
  const [slaves, setSlaves] = useState([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)

  useEffect(() => {
    let cancelled = false
    loadSlaves()
      .then((result) => {
        if (!cancelled) setSlaves(result)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message)
      })
    return () => {
      cancelled = true
    }
  }, [])

  
  async function run(action, successMessage) {
    setBusy(true)
    setError(null)
    setNotice(null)
    try {
      const result = await action()
      setSlaves(await loadSlaves())
      setNotice(successMessage(result))
      return true
    } catch (err) {
      setError(err.message)
      return false
    } finally {
      setBusy(false)
    }
  }

  const handleSet = (key, value) =>
    run(() => setKey(key, value), (r) => `Stored "${key}" on slave ${r.slaveId}.`)

  const handleAdd = () =>
    run(addSlave, (ids) => `Slave added. Cluster is now: ${ids.join(', ')}.`)

  const handleRemove = (id) =>
    run(() => removeSlave(id), (ids) => `Slave ${id} removed. Cluster is now: ${ids.join(', ')}.`)

  const handleRefresh = () => run(async () => {}, () => 'Refreshed.')

  const nextSlaveId = slaves.length ? Math.max(...slaves.map((s) => s.id)) + 1 : 1

  return (
    <main className="page">
      <h1>Distributed Key-Value Store</h1>

      {error && <div className="banner error" role="alert">{error}</div>}
      {notice && <div className="banner ok" role="status">{notice}</div>}

      <ToolBar busy={busy} nextSlaveId={nextSlaveId} onAdd={handleAdd} onRefresh={handleRefresh} />

      <div className="forms">
        <SetKeyForm busy={busy} onSet={handleSet} />
        <GetKeyForm busy={busy} />
      </div>

      <SlaveGrid slaves={slaves} capacity={SLAVE_CAPACITY} busy={busy} onRemove={handleRemove} />
    </main>
  )
}
