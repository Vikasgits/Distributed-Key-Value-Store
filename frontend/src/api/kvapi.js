// The only module that knows about URLs and fetch. Components call these functions.
const BASE_URL = 'http://localhost:5000'

async function request(path, params = {}) {
  const query = new URLSearchParams(params).toString() // encodes keys and values safely
  const url = `${BASE_URL}${path}${query ? `?${query}` : ''}`

  let response
  try {
    response = await fetch(url)
  } catch {
    throw new Error(`Cannot reach the master at ${BASE_URL}. Is it running?`)
  }

  const body = await response.json().catch(() => ({}))

  if (!response.ok) {
    // The master puts a readable message in { error: ... }
    const error = new Error(body.error ?? `Request failed with status ${response.status}`)
    error.status = response.status
    throw error
  }

  return body
}

// -> array of slave ids
export async function getSlaves() {
  const body = await request('/api/slaves')
  return body.slaves
}

// -> object { key: value } held by one slave
export async function getSlaveData(slaveId) {
  const body = await request('/api/slave/data', { slaveId })
  return body.data
}

// -> { key, value, slaveId }
export function setKey(key, value) {
  return request('/setkey', { key, value })
}

// -> the value, or null on a miss (never set, or evicted by the LRU)
export async function getKey(key) {
  try {
    const body = await request('/getkey', { key })
    return body.value
  } catch (error) {
    if (error.status === 404) return null
    throw error
  }
}

// -> array of slave ids after the change. The slave process must already be running.
export async function addSlave() {
  const body = await request('/api/addSlave')
  return body.slaves
}

// -> array of slave ids after the change
export async function removeSlave(slaveId) {
  const body = await request('/api/popSlave', { slaveId })
  return body.slaves
}
