/*
 * This file is protected and may not be modified without permission.
 * See LICENSE_ADDENDUM.txt for details.
 */
import { useLocalStorage } from '@mantine/hooks'
import dayjs from 'dayjs'
import LZString from 'lz-string'
import { useEffect, useRef } from 'react'
import { Cache, SWRConfiguration } from 'swr'
import api, { ClientConfig } from '@Api'

export const OnceSWRConfig: SWRConfiguration = {
  refreshInterval: 0,
  revalidateOnFocus: false,
}

const RepoMeta = {
  sha: import.meta.env.VITE_APP_GIT_SHA ?? 'unknown',
  rawTag: import.meta.env.VITE_APP_GIT_NAME ?? 'unknown',
  timestamp: import.meta.env.VITE_APP_BUILD_TIMESTAMP ?? '',
  buildTime: import.meta.env.DEV ? dayjs() : dayjs(import.meta.env.VITE_APP_BUILD_TIMESTAMP),
  repo: 'https://github.com/GZTimeWalker/GZCTF',
}

export const useConfig = () => {
  const {
    data: config,
    error,
    mutate,
  } = api.info.useInfoGetClientConfig({
    refreshInterval: 0,
    revalidateOnFocus: false,
    revalidateOnReconnect: false,
    refreshWhenHidden: false,
    shouldRetryOnError: false,
    refreshWhenOffline: false,
  })

  const [clientConfig, setClientConfig] = useLocalStorage({
    key: 'client-config',
    defaultValue: {
      title: 'GZ',
      slogan: 'Hack for fun not for profit',
      footerInfo: null,
      customTheme: null,
      defaultLifetime: 120,
      extensionDuration: 120,
      renewalWindow: 10,
    } as ClientConfig,
  })

  useEffect(() => {
    if (config) {
      setClientConfig(config)
    }
  }, [config])

  return { config: config ?? clientConfig, error, mutate }
}

export const ValidatedRepoMeta = () => {
  const { sha, rawTag, timestamp, buildTime: buildtime } = RepoMeta

  const tag = rawTag.replace(/-.*$/, '')

  const valid =
    timestamp.length === 20 &&
    buildtime.isValid() &&
    sha.length === 40 &&
    (/^v\d+\.\d+\.\d+$/i.test(tag) || tag === 'develop')

  return { valid, tag, ...RepoMeta }
}

const showBanner = () => {
  const { sha, rawTag: tag, buildTime, repo, valid } = ValidatedRepoMeta()
  const padding = ' '.repeat(45)

  const bannerClr = ['color: #4ccaaa', 'color: unset']
  const textClr = ['font-weight: bold', 'font-weight: bold; color: #4ccaaa']
  const badClr = ['font-weight: bold', 'font-weight: bold; color: #fe3030']

  // The GZCTF identifier is protected by the License.
  // DO NOT REMOVE OR MODIFY THE FOLLOWING LINE.
  // Please see LICENSE_ADDENDUM.txt for details.

  const banner = `
  ██████╗ ███████╗           ██████╗████████╗███████╗
 ██╔════╝ ╚══███╔╝ %c ██╗██╗ %c ██╔════╝╚══██╔══╝██╔════╝
 ██║  ███╗  ███╔╝  %c ╚═╝╚═╝ %c ██║        ██║   █████╗
 ██║   ██║ ███╔╝   %c ██╗██╗ %c ██║        ██║   ██╔══╝
 ╚██████╔╝███████╗ %c ╚═╝╚═╝ %c ╚██████╗   ██║   ██║
  ╚═════╝ ╚══════╝           ╚═════╝   ╚═╝   ╚═╝
  ${padding}%c@ %c${valid ? tag : 'Unknown'}

%cCopyright (C) 2022-now, GZTimeWalker, All rights reserved.

%cLicense  : %cGNU Affero General Public License v3.0
%cCommit   : %c${valid ? sha : 'Unofficial build version'}
%cBuilt at : %c${buildTime.format('YYYY-MM-DDTHH:mm:ssZ')}
%cIssues   : %c${repo}/issues
 `

  // rewrite the show banner function with %c and css
  console.log(
    banner,
    ...bannerClr.concat(bannerClr, bannerClr, bannerClr),
    ...(valid ? textClr : badClr),
    'font-weight: bold',
    ...textClr.concat(valid ? textClr : badClr, textClr, textClr)
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

const cacheKey = 'gzctf-cache'
const cacheMap = new Map(
  JSON.parse(LZString.decompress(localStorage.getItem(cacheKey) || '') || '[]')
)

const saveCache = () => {
  const appCache = LZString.compress(JSON.stringify(Array.from(cacheMap.entries())))
  localStorage.setItem(cacheKey, appCache)
}

export const localCacheProvider = () => {
  window.addEventListener('beforeunload', saveCache, true)
  return cacheMap as Cache
}

export const clearLocalCache = () => {
  window.removeEventListener('beforeunload', saveCache, true)
  localStorage.removeItem(cacheKey)
  cacheMap.clear()
  window.location.reload()
}
