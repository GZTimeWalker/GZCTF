import { useLocalStorage } from '@mantine/hooks'
import dayjs from 'dayjs'
import LZString from 'lz-string'
import { useEffect, useRef } from 'react'
import { Cache, SWRConfiguration } from 'swr'
import api, { GlobalConfig } from '@Api'

export const OnceSWRConfig: SWRConfiguration = {
  refreshInterval: 0,
  revalidateOnFocus: false,
}

const RepoMeta = {
  sha: import.meta.env.VITE_APP_GIT_SHA ?? 'unknown',
  tag: import.meta.env.VITE_APP_GIT_NAME ?? 'unknown',
  timestamp: import.meta.env.VITE_APP_BUILD_TIMESTAMP ?? '',
  buildtime: import.meta.env.DEV ? dayjs() : dayjs(import.meta.env.VITE_APP_BUILD_TIMESTAMP),
  repo: 'https://github.com/GZTimeWalker/GZCTF',
}

export const useConfig = () => {
  const {
    data: config,
    error,
    mutate,
  } = api.info.useInfoGetGlobalConfig({
    refreshInterval: 0,
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
      beianInfo: null,
    } as GlobalConfig,
  })

  useEffect(() => {
    if (config) {
      setGlobalConfig(config)
    }
  }, [config])

  return { config: config ?? globalConfig, error, mutate }
}

export const ValidatedRepoMeta = () => {
  const { sha, tag, timestamp, buildtime } = RepoMeta
  const valid =
    timestamp.length === 20 && buildtime.isValid() && sha.length === 40 && tag.length > 0
  return { valid, ...RepoMeta }
}

const showBanner = () => {
  const { sha, tag, buildtime, repo, valid } = ValidatedRepoMeta()
  const rst = '\x1b[0m'
  const bold = '\x1b[1m'
  const brand = '\x1b[38;2;4;202;171m'
  const alert = '\x1b[38;2;254;48;48m'
  const padding = ' '.repeat(45)

  const showtag = valid ? `${brand}${tag}` : `${alert}Unknown`
  const commit = valid ? `${brand}${sha}` : `${alert}Unofficial build version`

  const title = `
 ██████╗ ███████╗ ${brand}        ${rst}  ██████╗████████╗███████╗
██╔════╝ ╚══███╔╝ ${brand} ██╗██╗ ${rst} ██╔════╝╚══██╔══╝██╔════╝
██║  ███╗  ███╔╝  ${brand} ╚═╝╚═╝ ${rst} ██║        ██║   █████╗
██║   ██║ ███╔╝   ${brand} ██╗██╗ ${rst} ██║        ██║   ██╔══╝
╚██████╔╝███████╗ ${brand} ╚═╝╚═╝ ${rst} ╚██████╗   ██║   ██║
 ╚═════╝ ╚══════╝ ${brand}        ${rst}  ╚═════╝   ╚═╝   ╚═╝

${padding}${bold}@ ${showtag}${rst}
`

  console.log(
    `${title}` +
      `\n${bold}Copyright (C) 2022-now, GZTimeWalker, All rights reserved.${rst}\n` +
      `\n${bold}License  : ${brand}GNU Affero General Public License v3.0${rst}` +
      `\n${bold}Commit   : ${commit}${rst}` +
      `\n${bold}Built at : ${brand}${buildtime.format('YYYY-MM-DDTHH:mm:ssZ')}${rst}` +
      `\n${bold}Issues   : ${repo}/issues` +
      '\n'
  )
}

export const useBanner = () => {
  const mounted = useRef(false)
  useEffect(() => {
    if (!mounted.current) {
      showBanner()
      mounted.current = true
    }
  }, [])
}

export const useLocalStorageCache = () => {
  const cacheKey = 'gzctf-cache'

  const mapRef = useRef(
    new Map(JSON.parse(LZString.decompress(localStorage.getItem(cacheKey) || '') || '[]'))
  )

  const saveCache = () => {
    const appCache = LZString.compress(JSON.stringify(Array.from(mapRef.current.entries())))
    localStorage.setItem(cacheKey, appCache)
  }

  const localCacheProvider = () => {
    window.addEventListener('beforeunload', saveCache)
    return mapRef.current as Cache
  }

  const clearLocalCache = () => {
    window.removeEventListener('beforeunload', saveCache)
    localStorage.removeItem('gzctf-cache')
    window.location.reload()
  }

  return { localCacheProvider, clearLocalCache }
}
