/**
 * Shared 1-second ticker.
 *
 * Every component that previously ran its own `setInterval(() => setNow(dayjs()), 1000)`
 * (WithGameTab, ChallengeModal, InstanceEntry, etc.) can subscribe here.  One
 * module-level interval fans out to all subscribers, so a page with 3+
 * countdown widgets no longer runs 3 independent intervals + 3 independent
 * setState calls every second.
 *
 * Also pauses automatically when the tab is hidden — no background CPU.
 */
import dayjs, { Dayjs } from 'dayjs'
import { useEffect, useState } from 'react'

type Listener = (now: Dayjs) => void

const listeners = new Set<Listener>()
let interval: ReturnType<typeof setInterval> | null = null
let lastValue: Dayjs = dayjs()

const tick = (): void => {
  lastValue = dayjs()
  listeners.forEach((fn) => fn(lastValue))
}

const start = (): void => {
  if (interval !== null) return
  // Align first tick to the next whole second so multiple mounts agree on
  // the clock to the millisecond.
  const toNextSecond = 1000 - (Date.now() % 1000)
  setTimeout(() => {
    tick()
    interval = setInterval(tick, 1000)
  }, toNextSecond)
}

const stop = (): void => {
  if (interval === null) return
  clearInterval(interval)
  interval = null
}

if (typeof document !== 'undefined') {
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) stop()
    else if (listeners.size > 0) start()
  })
}

/**
 * Returns the current time as a dayjs object, updated once per second.
 * The value is shared across all consumers — no duplicate intervals.
 */
export const useTicker = (): Dayjs => {
  const [now, setNow] = useState<Dayjs>(lastValue)

  useEffect(() => {
    listeners.add(setNow)
    if (listeners.size === 1) start()
    return () => {
      listeners.delete(setNow)
      if (listeners.size === 0) stop()
    }
  }, [])

  return now
}
