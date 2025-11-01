import { useDebouncedCallback, useLocalStorage } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { Wsrx, WsrxError, WsrxErrorKind, WsrxFeature, WsrxOptions, WsrxState } from '@xdsec/wsrx'
import { t } from 'i18next'
import { createContext, useCallback, use, useEffect, useMemo, useState } from 'react'
import { showErrorMsg } from '@Utils/Shared'
import { useConfig } from '@Hooks/useConfig'

interface CustomWsrxOptions {
  name: string
  api: string
  allowLan: boolean
}

export const DefaultWsrxOptions: CustomWsrxOptions = {
  api: 'http://127.0.0.1:3307',
  name: 'GZ::CTF',
  allowLan: false,
}

export const HandleWsrxError = (err: unknown, t: (key: string) => string) => {
  if (err instanceof WsrxError) {
    switch (err.kind) {
      case WsrxErrorKind.VersionMismatch:
        showNotification({
          id: 'wsrx-version-mismatch',
          color: 'orange',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('wsrx.error.version_mismatch.title'),
          message: t('wsrx.error.version_mismatch.message'),
        })
        break
      case WsrxErrorKind.DaemonUnavailable:
        showNotification({
          id: 'wsrx-daemon-offline',
          color: 'red',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('wsrx.error.daemon_unavailable.title'),
          message: t('wsrx.error.daemon_unavailable.message'),
        })
        break
      case WsrxErrorKind.DaemonError:
        showNotification({
          id: 'wsrx-daemon-error',
          color: 'red',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('wsrx.error.daemon_error.title'),
          message: t('wsrx.error.daemon_error.message'),
        })
        break
      default:
        showNotification({
          id: 'wsrx-unknown-error',
          color: 'red',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('common.error.unknown'),
          message: t('wsrx.error.unknown'),
        })
    }
  } else {
    showErrorMsg(err, t)
  }
}

interface WsrxContextType {
  wsrx: Wsrx
  wsrxState: WsrxState
  wsrxOptions: CustomWsrxOptions
  doWsrxConnect: () => void
  setWsrxOptions: (options: CustomWsrxOptions | ((prev: CustomWsrxOptions) => CustomWsrxOptions)) => void
}

const WsrxContext = createContext<WsrxContextType | null>(null)

const getWsrxConfig = (options: CustomWsrxOptions) => {
  const config: WsrxOptions = {
    name: options.name ?? DefaultWsrxOptions.name,
    api: options.api ?? DefaultWsrxOptions.api,
    features: [WsrxFeature.Basic, WsrxFeature.Pingfall],
    settings: {
      pingfall: {
        status: [400, 404],
        drop_unknown: false,
      },
    },
  }

  return config
}

export const WsrxProvider: React.FC<React.PropsWithChildren> = ({ children }) => {
  const [wsrxState, setWsrxState] = useState<WsrxState>(WsrxState.Invalid)
  const platformConfig = useConfig()

  const [wsrxOptions, setWsrxOptions] = useLocalStorage<CustomWsrxOptions>({
    key: 'wsrx-options',
    defaultValue: DefaultWsrxOptions,
    getInitialValueInEffect: false,
  })

  const wsrx = useMemo(() => new Wsrx(getWsrxConfig(wsrxOptions)), [])

  const doWsrxConnect = useDebouncedCallback(async () => {
    try {
      wsrx.connect()
    } catch (err) {
      if (err instanceof WsrxError && err.kind !== WsrxErrorKind.DaemonUnavailable) HandleWsrxError(err, t)
    }
  }, 100)

  useEffect(() => {
    if (wsrxOptions) {
      wsrx.setOptions(getWsrxConfig(wsrxOptions))
      doWsrxConnect()
    }
  }, [wsrx, wsrxOptions, doWsrxConnect])

  useEffect(() => {
    if (platformConfig?.config.title) {
      const newName = platformConfig.config.title + '::CTF'
      setWsrxOptions((prevOptions) => {
        if (prevOptions.name === newName) return prevOptions
        return {
          ...prevOptions,
          name: newName,
        }
      })
    }
  }, [platformConfig?.config.title, setWsrxOptions])

  const updateState = useCallback((newState: WsrxState) => {
    setWsrxState((prev) => {
      if (newState === WsrxState.Invalid && prev !== WsrxState.Invalid) {
        showNotification({
          id: 'wsrx-daemon-offline',
          color: 'red',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('wsrx.error.daemon_offline.title'),
          message: t('wsrx.error.daemon_offline.message'),
        })
      }
      return newState
    })
  }, [])

  useEffect(() => {
    const id = wsrx.onStateChange(updateState)
    return () => wsrx.offStateChange(id)
  }, [wsrx, updateState])

  const contextValue = useMemo(
    () => ({
      wsrx,
      wsrxState,
      wsrxOptions,
      doWsrxConnect,
      setWsrxOptions,
    }),
    [wsrx, wsrxState, wsrxOptions, doWsrxConnect, setWsrxOptions]
  )

  return <WsrxContext.Provider value={contextValue}>{children}</WsrxContext.Provider>
}

export const useWsrx = () => {
  const context = use(WsrxContext)
  if (!context) {
    throw new Error('useWsrx must be used within a WsrxProvider')
  }
  return context
}
