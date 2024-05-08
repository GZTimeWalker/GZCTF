import { Center, Loader, MantineProvider } from '@mantine/core'
import { DatesProvider } from '@mantine/dates'
import { emotionTransform, MantineEmotionProvider } from '@mantine/emotion'
import { ModalsProvider } from '@mantine/modals'
import { Notifications } from '@mantine/notifications'
import { FC, Suspense } from 'react'
import { useTranslation } from 'react-i18next'
import { useRoutes } from 'react-router-dom'
import { SWRConfig } from 'swr'
import routes from '~react-pages'
import { useLanguage } from '@Utils/I18n'
import { CustomTheme } from '@Utils/ThemeOverride'
import { useBanner, localCacheProvider } from '@Utils/useConfig'
import { fetcher } from '@Api'
import '@mantine/carousel/styles.css'
import '@mantine/core/styles.css'
import '@mantine/dates/styles.css'
import '@mantine/dropzone/styles.css'
import '@mantine/notifications/styles.css'
import './App.css'

export const App: FC = () => {
  useBanner()

  const { t } = useTranslation()
  const { locale } = useLanguage()

  return (
    <MantineProvider
      defaultColorScheme="dark"
      theme={CustomTheme}
      stylesTransform={emotionTransform}
    >
      <MantineEmotionProvider>
        <Notifications zIndex={5000} />

        <DatesProvider settings={{ locale }}>
          <ModalsProvider
            labels={{ confirm: t('common.modal.confirm'), cancel: t('common.modal.cancel') }}
          >
            <SWRConfig
              value={{
                refreshInterval: 10000,
                provider: localCacheProvider,
                fetcher,
              }}
            >
              <Suspense
                fallback={
                  <Center h="100vh" w="100vw">
                    <Loader />
                  </Center>
                }
              >
                {useRoutes(routes)}
              </Suspense>
            </SWRConfig>
          </ModalsProvider>
        </DatesProvider>
      </MantineEmotionProvider>
    </MantineProvider>
  )
}
