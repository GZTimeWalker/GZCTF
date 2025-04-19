import { useLocalStorage } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { Wsrx, WsrxError, WsrxErrorKind, WsrxFeature, WsrxOptions, WsrxState } from '@xdsec/wsrx'
import { t } from 'i18next'
import { useEffect } from 'react'
import { showErrorNotification } from '@Utils/ApiHelper'

export const DefaultWsrxOptions: WsrxOptions = {
  api: 'http://127.0.0.1:3307',
  name: 'GZ::CTF',
  features: [WsrxFeature.Basic, WsrxFeature.Pingfall],
}

export const HandleWsrxError = (err: unknown, t: (key: string) => string) => {
  if (err instanceof WsrxError) {
    switch (err.kind) {
      case WsrxErrorKind.VersionMismatch:
        showNotification({
          color: 'orange',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('wsrx.error.version_mismatch.title'),
          message: t('wsrx.error.version_mismatch.message'),
        })
        break
      case WsrxErrorKind.DaemonUnavailable:
        showNotification({
          color: 'red',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('wsrx.error.daemon_unavailable.title'),
          message: t('wsrx.error.daemon_unavailable.message'),
        })
        break
      case WsrxErrorKind.DaemonError:
        showNotification({
          color: 'red',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('wsrx.error.daemon_error.title'),
          message: t('wsrx.error.daemon_error.message'),
        })
        break
      default:
        showNotification({
          color: 'red',
          icon: <Icon path={mdiClose} size={1} />,
          title: t('common.error.unknown'),
          message: t('wsrx.error.unknown'),
        })
    }
  } else {
    showErrorNotification(err, t)
  }
}

const wsrx = new Wsrx(DefaultWsrxOptions)
let cachedState: WsrxState | null = null

wsrx.onStateChange((state) => {
  if (state === WsrxState.Invalid && cachedState !== WsrxState.Invalid) {
    showNotification({
      color: 'red',
      icon: <Icon path={mdiClose} size={1} />,
      title: t('wsrx.error.daemon_offline.title'),
      message: t('wsrx.error.daemon_offline.message'),
    })
  }
  cachedState = state
})

export const useWsrx = () => {
  const [wsrxOptions, setWsrxOptions] = useLocalStorage<WsrxOptions>({
    key: 'wsrx-options',
    defaultValue: DefaultWsrxOptions,
  })

  useEffect(() => {
    wsrx.setOptions(wsrxOptions)
  }, [wsrxOptions])

  return {
    wsrxOptions,
    setWsrxOptions,
    wsrx,
  }
}
