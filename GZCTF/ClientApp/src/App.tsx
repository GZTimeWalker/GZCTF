import { SWRConfig } from 'swr'
import routes from '~react-pages'
import { FC, Suspense } from 'react'
import { useRoutes } from 'react-router-dom'
import {
  MantineProvider,
  Global,
  ColorScheme,
  ColorSchemeProvider,
  Center,
  Loader,
} from '@mantine/core'
import { useLocalStorage } from '@mantine/hooks'
import { ModalsProvider } from '@mantine/modals'
import { NotificationsProvider } from '@mantine/notifications'
import { ThemeOverride } from '@Utils/ThemeOverride'
import { fetcher } from '@Api'

export const App: FC = () => {
  const [colorScheme, setColorScheme] = useLocalStorage<ColorScheme>({
    key: 'color-scheme',
    defaultValue: 'dark',
    getInitialValueInEffect: true,
  })

  const toggleColorScheme = (value?: ColorScheme) =>
    setColorScheme(value || (colorScheme === 'dark' ? 'light' : 'dark'))

  return (
    <ColorSchemeProvider colorScheme={colorScheme} toggleColorScheme={toggleColorScheme}>
      <MantineProvider
        withGlobalStyles
        withCSSVariables
        theme={{ ...ThemeOverride, colorScheme: colorScheme }}
      >
        <NotificationsProvider zIndex={5000}>
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
              },
            })}
          />
          <ModalsProvider labels={{ confirm: '确认', cancel: '取消' }}>
            <SWRConfig
              value={{
                refreshInterval: 10000,
                fetcher,
              }}
            >
              <Suspense
                fallback={
                  <Center style={{ height: '100vh' }}>
                    <Loader />
                  </Center>
                }
              >
                {useRoutes(routes)}
              </Suspense>
            </SWRConfig>
          </ModalsProvider>
        </NotificationsProvider>
      </MantineProvider>
    </ColorSchemeProvider>
  )
}
