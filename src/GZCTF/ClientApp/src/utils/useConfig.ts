import { useEffect } from 'react'
import { useLocalStorage } from '@mantine/hooks'
import api, { GlobalConfig } from '@Api'

export const useConfig = () => {
  const {
    data: config,
    error,
    mutate,
  } = api.info.useInfoGetGlobalConfig({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
    revalidateOnReconnect: false,
    refreshWhenHidden: false,
    shouldRetryOnError: false,
    refreshWhenOffline: false,
  })

  const [globalConfig, setGlobalConfig] = useLocalStorage({
    key: 'global-config',
    defaultValue: {
      title: 'GZ',
      slogan: 'Hack for fun not for profit',
    } as GlobalConfig,
  })

  useEffect(() => {
    if (config) {
      setGlobalConfig(config)
    }
  }, [config])

  return { config: config ?? globalConfig, error, mutate }
}
