import {
  Center,
  ColorScheme,
  ColorSchemeProvider,
  Global,
  Loader,
  MantineProvider,
} from '@mantine/core'
import { useLocalStorage } from '@mantine/hooks'
import { ModalsProvider } from '@mantine/modals'
import { Notifications } from '@mantine/notifications'
import { FC, Suspense } from 'react'
import { useRoutes } from 'react-router-dom'
import { SWRConfig } from 'swr'
import routes from '~react-pages'
import { useTranslation } from '@Utils/I18n'
import { ThemeOverride } from '@Utils/ThemeOverride'
import { useBanner, useLocalStorageCache } from '@Utils/useConfig'
import { fetcher } from '@Api'

export const App: FC = () => {
  const [colorScheme, setColorScheme] = useLocalStorage<ColorScheme>({
    key: 'color-scheme',
    defaultValue: 'dark',
    getInitialValueInEffect: true,
  })

  const toggleColorScheme = (value?: ColorScheme) =>
    setColorScheme(value || (colorScheme === 'dark' ? 'light' : 'dark'))

  useBanner()

  const { t } = useTranslation()
  const { localCacheProvider } = useLocalStorageCache()

  return (
    <ColorSchemeProvider colorScheme={colorScheme} toggleColorScheme={toggleColorScheme}>
      <MantineProvider withGlobalStyles withCSSVariables theme={{ ...ThemeOverride, colorScheme }}>
        <Notifications zIndex={5000} />
        {StyledGlobal}
        <ModalsProvider labels={{ confirm: t('Modal_Confirm'), cancel: t('Modal_Cancel') }}>
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
    </ColorSchemeProvider>
  )
}

const StyledGlobal = (
  <Global
    styles={(theme) => ({
      body: {
        ...theme.fn.fontStyles(),
        backgroundColor:
          theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2],
        color: theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[7],
        lineHeight: theme.lineHeight,
        padding: 0,
        margin: 0,

        '@media print': {
          backgroundColor: theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.white,
        },
      },

      '::-webkit-scrollbar': {
        height: 6,
        width: 6,
      },

      '::-webkit-scrollbar-thumb': {
        background: 'var(--mantine-color-dark-3)',
        borderRadius: 3,
      },

      '::-webkit-scrollbar-track': {
        backgroundColor: 'transparent',
      },

      '::-webkit-scrollbar-corner': {
        backgroundColor: 'transparent',
      },
    })}
  />
)
