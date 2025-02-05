import { useLocalStorage } from '@mantine/hooks'
import { useState } from 'react'
import useSWR from 'swr'

export enum WsrxState {
  Disconnected = 0,
  Pending = 1,
  Connected = 2,
}

export interface WsrxConfig {
  addr: string
}

export const useWsrx = () => {
  const [localConfig, setLocalConfig] = useLocalStorage<WsrxConfig>({
    key: 'wsrx-config',
    defaultValue: { addr: 'http://127.0.0.1:3307' },
  })
  const {
    data: state,
    error: _,
    mutate: refreshState,
  } = useSWR<WsrxState>(`${localConfig.addr}/connect`, async () => {
    const res = await fetch(`${localConfig.addr}/connect`)
    if (!res.ok) return WsrxState.Disconnected
    switch (res.status) {
      case 201:
        return WsrxState.Pending
      default:
        return WsrxState.Connected
    }
  })

  const connect = async () => {
    await fetch(`${localConfig.addr}/connect`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(location.origin),
    })
    refreshState()
  }
  return {
    localConfig,
    setLocalConfig,
    state,
    refreshState,
    connect,
  }
}
