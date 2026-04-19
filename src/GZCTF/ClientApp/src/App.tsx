import { Center, Loader, MantineProvider } from '@mantine/core'
import { DatesProvider } from '@mantine/dates'
import { emotionTransform, MantineEmotionProvider } from '@mantine/emotion'
import { ModalsProvider } from '@mantine/modals'
import { Notifications } from '@mantine/notifications'
import { FC, Suspense } from 'react'
import { ErrorBoundary } from 'react-error-boundary'
import { useTranslation } from 'react-i18next'
import { useRoutes } from 'react-router'
import { SWRConfig } from 'swr'
import routes from '~react-pages'
import { ErrorFallback } from '@Components/ErrorFallback'
import { WsrxProvider } from '@Components/WsrxProvider'
import { localCacheProvider } from '@Utils/Cache'
import { useLanguage } from '@Utils/I18n'
import { useCustomTheme } from '@Utils/ThemeOverride'
import { useBanner } from '@Hooks/useConfig'
import { fetcher as rawFetcher } from '@Api'
import '@mantine/core/styles.css'
import '@mantine/dates/styles.css'
import '@mantine/dropzone/styles.css'
import '@mantine/notifications/styles.css'
import './styles/App.css'

/**
 * Wraps the generated swagger fetcher so any 401 globally redirects
 * to /account/login?from=<current> and pops a "session expired" toast.
 * This replaces the old silent-empty-state behaviour after cookie
 * expiry.  Flagged on window so the interceptor fires at most once
 * per navigation.
 */
let authRedirectInFlight = false
const authAwareFetcher = async (args: Parameters<typeof rawFetcher>[0]) => {
  try {
    return await rawFetcher(args)
  } catch (e: unknown) {
    const status = (e as { status?: number } | undefined)?.status
    const path = typeof args === 'string' ? args : args[0]
    const isAuthEndpoint = path.includes('/account/') || path.includes('/info')
    if (
      status === 401 &&
      !authRedirectInFlight &&
      !isAuthEndpoint &&
      typeof window !== 'undefined' &&
      !window.location.pathname.startsWith('/account/')
    ) {
      authRedirectInFlight = true
      try {
        const { showNotification } = await import('@mantine/notifications')
        showNotification({
          id: 'session-expired',
          color: 'red',
          title: 'Session expired',
          message: 'Please log in again.',
        })
      } catch {
        // notifications unavailable — skip toast, still redirect
      }
      const from = window.location.pathname + window.location.search
      window.location.href = `/account/login?from=${encodeURIComponent(from)}`
    }
    throw e
  }
}

export const App: FC = () => {
  useBanner()

  const { t } = useTranslation()
  const { locale } = useLanguage()
  const { theme } = useCustomTheme()

  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} stylesTransform={emotionTransform}>
      <MantineEmotionProvider>
        <ErrorBoundary FallbackComponent={ErrorFallback}>
          <Notifications zIndex={5000} />
          <DatesProvider settings={{ locale }}>
            <ModalsProvider labels={{ confirm: t('common.modal.confirm'), cancel: t('common.modal.cancel') }}>
              <SWRConfig
                value={{
                  // Default refresh at 60s keeps most screens cheap; hot
                  // paths (live scoreboard, notices, instance polling)
                  // opt in to shorter intervals explicitly at call sites.
                  refreshInterval: 60_000,
                  keepPreviousData: true,
                  provider: localCacheProvider,
                  fetcher: authAwareFetcher,
                }}
              >
                <WsrxProvider>
                  <Suspense
                    fallback={
                      <Center h="100vh" w="100vw">
                        <Loader />
                      </Center>
                    }
                  >
                    {useRoutes(routes)}
                  </Suspense>
                </WsrxProvider>
              </SWRConfig>
            </ModalsProvider>
          </DatesProvider>
        </ErrorBoundary>
      </MantineEmotionProvider>
    </MantineProvider>
  )
}
