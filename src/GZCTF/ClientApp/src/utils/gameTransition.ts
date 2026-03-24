const STORAGE_KEY = 'gzctf-game-transition'

export interface GameTransitionState {
  id: number
  top: number
  left: number
  width: number
  height: number
  poster?: string | null
  timestamp: number
}

interface StorePayload {
  id: number
  top: number
  left: number
  width: number
  height: number
  poster?: string | null
}

const isBrowser = () => typeof window !== 'undefined' && typeof sessionStorage !== 'undefined'

export const storeGameTransitionState = (payload: StorePayload) => {
  if (!isBrowser()) {
    return
  }

  const state: GameTransitionState = {
    ...payload,
    timestamp: Date.now(),
  }

  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state))
  } catch (error) {
    console.warn('[GameTransition] Failed to store transition state', error)
  }
}

export const consumeGameTransitionState = (id: number, maxAge = 2000): GameTransitionState | null => {
  if (!isBrowser()) {
    return null
  }

  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return null
    }

    const parsed = JSON.parse(raw) as GameTransitionState
    sessionStorage.removeItem(STORAGE_KEY)

    if (parsed.id !== id) {
      return null
    }

    if (Date.now() - parsed.timestamp > maxAge) {
      return null
    }

    return parsed
  } catch (error) {
    console.warn('[GameTransition] Failed to consume transition state', error)
    return null
  }
}