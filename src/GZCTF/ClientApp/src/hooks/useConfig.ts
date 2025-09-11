// SPDX-License-Identifier: LicenseRef-GZCTF-Restricted
// Copyright (C) 2022-2025 GZTimeWalker
// Restricted Component - NOT under AGPLv3.
// See licenses/LicenseRef-GZCTF-Restricted.txt
import { useLocalStorage } from '@mantine/hooks'
import dayjs from 'dayjs'
import { useEffect, useRef } from 'react'
import { SWRConfiguration } from 'swr'
import api, { ClientConfig, ContainerPortMappingType } from '@Api'

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

  const [clientConfig, setClientConfig] = useLocalStorage<ClientConfig>({
    key: 'client-config',
    defaultValue: {
      title: 'GZ',
      slogan: 'Hack for fun not for profit',
      portMapping: ContainerPortMappingType.Default,
      footerInfo: null,
      customTheme: null,
      defaultLifetime: 120,
      extensionDuration: 120,
      renewalWindow: 10,
    },
  })

  useEffect(() => {
    if (config) {
      setClientConfig(config)
    }
  }, [config])

  return { config: config ?? clientConfig, error, mutate }
}

export const useCaptchaConfig = () => {
  const { data, error, mutate } = api.info.useInfoGetClientCaptchaInfo({
    refreshInterval: 0,
    revalidateOnFocus: false,
    revalidateOnReconnect: false,
    refreshWhenHidden: false,
    shouldRetryOnError: false,
    refreshWhenOffline: false,
  })

  return { info: data, error, mutate }
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

  // GZCTF Banner Block
  // Core licensed under AGPLv3; certain components under LicenseRef-GZCTF-Restricted.
  // See NOTICE and LICENSE_ADDENDUM.txt for attribution & trademark guidance.
  const current = new Date().getFullYear()

  const banner = `
  ██████╗ ███████╗           ██████╗████████╗███████╗
 ██╔════╝ ╚══███╔╝ %c ██╗██╗ %c ██╔════╝╚══██╔══╝██╔════╝
 ██║  ███╗  ███╔╝  %c ╚═╝╚═╝ %c ██║        ██║   █████╗
 ██║   ██║ ███╔╝   %c ██╗██╗ %c ██║        ██║   ██╔══╝
 ╚██████╔╝███████╗ %c ╚═╝╚═╝ %c ╚██████╗   ██║   ██║
  ╚═════╝ ╚══════╝           ╚═════╝   ╚═╝   ╚═╝
  ${padding}%c@ %c${valid ? tag : 'Unknown'}

%cCopyright (C) 2022-${current}, GZTimeWalker, All rights reserved.

%cLicense  : %cGNU Affero General Public License v3.0 (Core)
%cLicense  : %cLicenseRef-GZCTF-Restricted (Restricted components)
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
    ...textClr.concat(textClr, valid ? textClr : badClr, textClr, textClr)
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
