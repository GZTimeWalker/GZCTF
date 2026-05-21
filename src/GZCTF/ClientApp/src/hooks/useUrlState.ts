import { useSearchParams } from 'react-router'

/**
 * Generic read/write hook for a single URL search-param slot.
 *
 * Caller supplies a `parse` that turns the raw string (or null when absent)
 * into the typed value, and a `serialize` that turns the typed value back
 * into a string — or `null` to mean "remove this key from the URL". The
 * latter is how default/empty values stay out of the URL so shared links
 * remain tidy.
 *
 * Writes use `{ replace: true }` so per-keystroke updates don't pollute
 * the browser back-button history.
 *
 * Example:
 *   const [mode, setMode] = useUrlState<'ascii' | 'hex'>(
 *     'mode',
 *     (raw) => (raw === 'hex' ? 'hex' : 'ascii'),
 *     (v) => (v === 'hex' ? 'hex' : null),
 *   )
 */
export function useUrlState<T>(
  key: string,
  parse: (raw: string | null) => T,
  serialize: (value: T) => string | null
): [T, (next: T) => void] {
  const [params, setParams] = useSearchParams()
  const value = parse(params.get(key))

  const setValue = (next: T) => {
    setParams(
      (prev) => {
        const out = new URLSearchParams(prev)
        const ser = serialize(next)
        if (ser === null) out.delete(key)
        else out.set(key, ser)
        return out
      },
      { replace: true }
    )
  }

  return [value, setValue]
}
