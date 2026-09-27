import { useEffect, useRef } from 'react'

/**
 * Runs `sync` whenever one of `deps` changes, for local state that mirrors a prop or
 * freshly fetched data.
 *
 * Unlike `useEffect(sync, deps)` it does **not** run on mount, so the `useState`
 * initializers have to produce the same values `sync` would.
 */
export const useSyncOnChange = (deps: unknown[], sync: () => void) => {
  const previous = useRef(deps)

  useEffect(() => {
    const prev = previous.current

    if (prev.length === deps.length && prev.every((dep, index) => Object.is(dep, deps[index]))) return

    previous.current = deps
    sync()
  })
}
