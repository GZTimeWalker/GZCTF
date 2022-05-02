import { AppProps } from 'next/app';
import Head from 'next/head';
import { MantineProvider, Global, ColorScheme, ColorSchemeProvider } from '@mantine/core';
import { useLocalStorage } from '@mantine/hooks';
import { NotificationsProvider } from '@mantine/notifications';
import { SWRConfig } from 'swr';
import './_app.css';

export default function App(props: AppProps) {
  const { Component, pageProps } = props;

  const [colorScheme, setColorScheme] = useLocalStorage<ColorScheme>({
    key: 'color-scheme',
    defaultValue: 'dark',
    getInitialValueInEffect: true,
  });

  const toggleColorScheme = (value?: ColorScheme) =>
    setColorScheme(value || (colorScheme === 'dark' ? 'light' : 'dark'));

  return (
    <>
      <Head>
        <title>GZ::CTF</title>
        <meta name="viewport" content="minimum-scale=1, initial-scale=1, width=device-width" />
        <link rel="shortcut icon" href="/favicon.png" />
      </Head>
      <ColorSchemeProvider colorScheme={colorScheme} toggleColorScheme={toggleColorScheme}>
        <MantineProvider
          withGlobalStyles
          withCSSVariables
          theme={{
            colorScheme: colorScheme,
            colors: {
              gray: [
                '#EBEBEB',
                '#CFCFCF',
                '#B3B3B3',
                '#969696',
                '#7A7A7A',
                '#5E5E5E',
                '#414141',
                '#252525',
                '#202020',
              ],
              brand: [
                '#A7FFEB',
                '#64FFDA',
                '#25EEBA',
                '#1DE9B6',
                '#0AD7AF',
                '#03CAAB',
                '#00BFA5',
                '#009985',
                '#007F6E',
              ],
              alert: ['#FF8A80', '#FF5252', '', '#FF1744', '', '', '#D50000', '', ''],
            },
            primaryColor: 'brand',
            fontFamily: "Play, 'Noto Sans SC', sans-serif",
            fontFamilyMonospace: "'JetBrains Mono', monospace",
            fontSizes: {
              xs: 13,
              sm: 16,
              md: 20,
              lg: 25,
              xl: 30,
            },
            headings: {
              fontFamily: "Play,'Noto Sans SC', sans-serif",
            },
          }}
        >
          <NotificationsProvider zIndex={5000}>
            <Global
              styles={(theme) => ({
                body: {
                  ...theme.fn.fontStyles(),
                  backgroundColor:
                    theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.white,
                  color: theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[7],
                  lineHeight: theme.lineHeight,
                  padding: 0,
                  margin: 0,
                },
              })}
            />
            <SWRConfig
              value={{
                refreshInterval: 10000,
                fetcher: (arg: string) => fetch(arg).then(res => res.json()),
              }}
            >
              <Component {...pageProps} />
            </SWRConfig>
          </NotificationsProvider>
        </MantineProvider>
      </ColorSchemeProvider>
    </>
  );
}
