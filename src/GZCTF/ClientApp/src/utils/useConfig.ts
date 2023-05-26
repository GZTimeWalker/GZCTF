import dayjs from 'dayjs'
import LZString from 'lz-string'
import { Cache } from 'swr'
import { useEffect, useRef } from 'react'
import { useLocalStorage } from '@mantine/hooks'
import api, { GlobalConfig } from '@Api'

const sha = import.meta.env.VITE_APP_GIT_SHA ?? 'unknown'
const tag = import.meta.env.VITE_APP_GIT_NAME ?? 'unknown'
const timestamp = import.meta.env.VITE_APP_BUILD_TIMESTAMP ?? ''
const builtdate = import.meta.env.DEV ? dayjs() : dayjs(timestamp)
const repo = 'https://github.com/GZTimeWalker/GZCTF'

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
    } as GlobalConfig,
  })

  useEffect(() => {
    if (config) {
      setGlobalConfig(config)
    }
  }, [config])

  return { config: config ?? globalConfig, error, mutate }
}

const showBanner = () => {
  const valid =
    timestamp.length === 20 && builtdate.isValid() && sha.length === 40 && tag.length > 0
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
      `\n${bold}License   : ${brand}GNU Affero General Public License v3.0${rst}` +
      `\n${bold}Commit    : ${commit}${rst}` +
      `\n${bold}Pushed at : ${brand}${builtdate.format('YYYY-MM-DDTHH:mm:ssZ')}${rst}` +
      `\n${bold}Issues    : ${repo}/issues` +
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

export const localStorageProvider = () => {
  const cacheKey = 'gzctf-cache'
  const map = new Map(JSON.parse(LZString.decompress(localStorage.getItem(cacheKey) || '') || '[]'))

  window.addEventListener('beforeunload', () => {
    const appCache = LZString.compress(JSON.stringify(Array.from(map.entries())))
    localStorage.setItem(cacheKey, appCache)
  })

  return map as Cache
}
