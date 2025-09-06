import LZString from 'lz-string'
import { gzip, ungzip } from 'pako'
import type { Cache } from 'swr'

// -----------------------------------------
// SWR Persistent Cache (Improved)
// -----------------------------------------

const LEGACY_CACHE_KEY = 'gzctf-cache'
const IDB_DB_NAME = 'gzctf-cache'
const IDB_STORE = 'swr'
const IDB_KEY = 'cache-map'

type BinaryLike = Uint8Array | ArrayBuffer

class PersistentCache implements Cache<any> {
  private map = new Map<any, any>()

  get size() {
    return this.map.size
  }

  // Basic Map interface required by SWR
  get(key: any) {
    return this.map.get(key)
  }
  has(key: any) {
    return this.map.has(key)
  }
  set(key: any, value: any) {
    this.map.set(key, value)
    schedulePersist()
    return this
  }
  delete(key: any) {
    const r = this.map.delete(key)
    schedulePersist()
    return r as any
  }
  clear() {
    this.map.clear()
    schedulePersist()
  }
  // Iteration
  keys() {
    return this.map.keys()
  }
  values() {
    return this.map.values()
  }
  entries() {
    return this.map.entries()
  }
  [Symbol.iterator]() {
    return this.map[Symbol.iterator]()
  }
  forEach(cb: (value: any, key: any, map: Map<any, any>) => void, thisArg?: any) {
    return this.map.forEach(cb as any, thisArg)
  }

  // Bulk hydrate (from_iter style)
  bulkAdd(entries: [any, any][]) {
    if (!entries.length) return
    let added = 0
    for (const [k, v] of entries) {
      if (!this.map.has(k)) {
        this.map.set(k, v)
        added++
      }
    }
    if (added) {
      dirty = true
      schedulePersist()
    }
    return added
  }

  snapshotEntries() {
    return Array.from(this.map.entries())
  }
}

const inMemoryCache = new PersistentCache()

let idbSupported = typeof indexedDB !== 'undefined'
let dbPromise: Promise<IDBDatabase> | null = null
let hydrationStarted = false
let hydrated = false
let dirty = false
let persistTimer: number | null = null

const textEncoder = new TextEncoder()
const textDecoder = new TextDecoder()

const openDB = (): Promise<IDBDatabase> => {
  if (!idbSupported) return Promise.reject(new Error('IndexedDB not supported'))
  if (dbPromise) return dbPromise
  dbPromise = new Promise((resolve, reject) => {
    const req = indexedDB.open(IDB_DB_NAME, 1)
    req.onupgradeneeded = () => {
      const db = req.result
      if (!db.objectStoreNames.contains(IDB_STORE)) {
        db.createObjectStore(IDB_STORE)
      }
    }
    req.onsuccess = () => resolve(req.result)
    req.onerror = () => reject(req.error)
  })
  return dbPromise
}

const encodeMap = (cache: PersistentCache): Uint8Array => {
  const json = JSON.stringify(cache.snapshotEntries())
  return gzip(textEncoder.encode(json))
}

const decodeMap = (bin: BinaryLike): [any, any][] => {
  const u8 = bin instanceof Uint8Array ? bin : new Uint8Array(bin)
  const json = textDecoder.decode(ungzip(u8))
  return JSON.parse(json)
}

const fallbackHydrateLocalStorage = () => {
  try {
    const raw = localStorage.getItem(LEGACY_CACHE_KEY)
    if (!raw) return
    const decompressed = LZString.decompress(raw)
    if (!decompressed) return
    const entries: [any, any][] = JSON.parse(decompressed || '[]')
    inMemoryCache.bulkAdd(entries)
    localStorage.removeItem(LEGACY_CACHE_KEY)
    console.info('[cache] migrated legacy localStorage cache')
  } catch (e) {
    console.warn('[cache] legacy migration failed', e)
  }
}

const fallbackPersistLocalStorage = () => {
  try {
    const serialized = JSON.stringify(inMemoryCache.snapshotEntries())
    const compressed = LZString.compress(serialized)
    localStorage.setItem(LEGACY_CACHE_KEY, compressed)
  } catch (e) {
    console.warn('[cache] fallback localStorage persist failed', e)
  }
}

const persistToIDB = async () => {
  if (!idbSupported || !dirty) return
  dirty = false
  try {
    const db = await openDB()
    const tx = db.transaction(IDB_STORE, 'readwrite')
    const store = tx.objectStore(IDB_STORE)
    const data = encodeMap(inMemoryCache)
    store.put(data, IDB_KEY)
    tx.onabort = () => console.warn('[cache] persist aborted', tx.error)
  } catch (e) {
    console.warn('[cache] persist failed, falling back to localStorage', e)
    fallbackPersistLocalStorage()
  }
}

const schedulePersist = () => {
  dirty = true
  if (persistTimer != null) return
  persistTimer = window.setTimeout(() => {
    persistTimer = null
    void persistToIDB()
  }, 3000)
}

const hydrateFromIDB = async () => {
  if (!idbSupported || hydrated) return
  hydrationStarted = true
  try {
    const db = await openDB()
    const tx = db.transaction(IDB_STORE, 'readonly')
    const store = tx.objectStore(IDB_STORE)
    const req = store.get(IDB_KEY)
    req.onsuccess = () => {
      try {
        const data = req.result as BinaryLike | undefined
        if (data) {
          const decoded = decodeMap(data)
          const added = inMemoryCache.bulkAdd(decoded)
          if (added) console.info('[cache] hydrated from IndexedDB, new entries:', added)
        }
        hydrated = true
      } catch (e) {
        console.warn('[cache] decode failed, attempting legacy migration', e)
        fallbackHydrateLocalStorage()
      }
    }
    req.onerror = () => {
      console.warn('[cache] IndexedDB read failed, using legacy localStorage', req.error)
      fallbackHydrateLocalStorage()
    }
  } catch (e) {
    console.warn('[cache] openDB failed, falling back to localStorage', e)
    idbSupported = false
    fallbackHydrateLocalStorage()
  }
}

const flushAndFallback = () => {
  if (idbSupported) void persistToIDB()
  else fallbackPersistLocalStorage()
}

const setupPersistenceSideEffects = () => {
  if (typeof window === 'undefined') return
  if (!hydrationStarted) void hydrateFromIDB()
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) flushAndFallback()
  })
  window.addEventListener(
    'beforeunload',
    () => {
      flushAndFallback()
    },
    { capture: true }
  )
}

export const localCacheProvider = (): Cache<any> => {
  setupPersistenceSideEffects()
  if (!hydrationStarted && !hydrated) fallbackHydrateLocalStorage()
  return inMemoryCache
}

export const clearLocalCache = () => {
  ;(async () => {
    try {
      if (idbSupported) {
        const db = await openDB()
        const tx = db.transaction(IDB_STORE, 'readwrite')
        tx.objectStore(IDB_STORE).delete(IDB_KEY)
      }
    } catch (e) {
      console.warn('[cache] clear idb failed', e)
    }
    try {
      localStorage.removeItem(LEGACY_CACHE_KEY)
    } catch {}
    inMemoryCache.clear()
    window.location.reload()
  })()
}
