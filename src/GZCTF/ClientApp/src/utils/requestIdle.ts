export type CancelIdle = () => void

type WindowIdleApi = {
  requestIdleCallback?: (cb: () => void, opts?: { timeout: number }) => number
  cancelIdleCallback?: (id: number) => void
}

export const requestIdle = (
  cb: () => void,
  opts?: { timeout?: number; fallbackDelayMs?: number }
): CancelIdle => {
  const w = window as unknown as WindowIdleApi

  if (w.requestIdleCallback) {
    const id = w.requestIdleCallback(cb, { timeout: opts?.timeout ?? 150 })
    return () => w.cancelIdleCallback?.(id)
  }

  const id = window.setTimeout(cb, opts?.fallbackDelayMs ?? 0)
  return () => window.clearTimeout(id)
}

