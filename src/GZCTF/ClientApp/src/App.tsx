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
import { useLanguage } from '@Utils/I18n'
import { useCustomTheme } from '@Utils/ThemeOverride'
import { useBanner, localCacheProvider } from '@Hooks/useConfig'
import { fetcher } from '@Api'
import '@mantine/carousel/styles.css'
import '@mantine/core/styles.css'
import '@mantine/dates/styles.css'
import '@mantine/dropzone/styles.css'
import '@mantine/notifications/styles.css'
import './styles/App.css'

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
                  refreshInterval: 10000,
                  provider: localCacheProvider,
                  fetcher,
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
