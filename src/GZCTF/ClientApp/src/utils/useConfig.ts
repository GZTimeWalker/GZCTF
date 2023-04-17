import api from '@Api'

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

  return { config, error, mutate }
}
