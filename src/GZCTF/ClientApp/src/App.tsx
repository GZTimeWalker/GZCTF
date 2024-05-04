import { Center, Loader, MantineProvider } from '@mantine/core'
import '@mantine/core/styles.css'
import { emotionTransform, MantineEmotionProvider } from '@mantine/emotion'
import { ModalsProvider } from '@mantine/modals'
import { Notifications } from '@mantine/notifications'
import { FC, Suspense } from 'react'
import { useTranslation } from 'react-i18next'
import { useRoutes } from 'react-router-dom'
import { SWRConfig } from 'swr'
import routes from '~react-pages'
import { CustomTheme } from '@Utils/ThemeOverride'
import { useBanner, localCacheProvider } from '@Utils/useConfig'
import { fetcher } from '@Api'
import './App.css'

export const App: FC = () => {
  useBanner()

  const { t } = useTranslation()

  return (
    <MantineProvider
      stylesTransform={emotionTransform}
      defaultColorScheme="dark"
      theme={CustomTheme}
    >
      <MantineEmotionProvider>
        {/* TODO: wait for fix in next patch*/}
        <MantineProvider theme={CustomTheme}>
          <Notifications zIndex={5000} />

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
        </MantineProvider>
      </MantineEmotionProvider>
    </MantineProvider>
  )
}
