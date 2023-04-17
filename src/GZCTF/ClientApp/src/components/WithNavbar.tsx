import React, { FC } from 'react'
import {
  AppShell,
  Box,
  Center,
  LoadingOverlay,
  MantineNumberSize,
  useMantineTheme,
} from '@mantine/core'
import { useUser } from '@Utils/useUser'
import AppHeader from './AppHeader'
import AppNavbar from './AppNavbar'
import Watermark from './Watermark'
import WithWiderScreen from './WithWiderScreen'

interface WithNavBarProps extends React.PropsWithChildren {
  width?: string
  minWidth?: number
  padding?: MantineNumberSize
  isLoading?: boolean
}

const WithNavBar: FC<WithNavBarProps> = ({ children, width, padding, isLoading, minWidth }) => {
  const theme = useMantineTheme()
  const { user } = useUser()

  return (
    <WithWiderScreen minWidth={minWidth}>
      <Watermark
        text={user?.userId ?? ''}
        textColor={theme.colorScheme === 'dark' ? theme.colors.gray[3] : theme.colors.gray[7]}
        rotate={-9}
        textSize={15}
        gutter={20}
        opacity={theme.colorScheme === 'dark' ? 0.006 : 0.012}
        fontFamily="JetBrains Mono"
      >
        <AppShell
          padding={padding ?? 'md'}
          fixed
          navbar={<AppNavbar />}
          header={<AppHeader />}
          styles={{ body: { overflow: 'hidden' } }}
        >
          <Center style={{ width: '100%' }}>
            <LoadingOverlay
              visible={isLoading ?? false}
              overlayOpacity={1}
              overlayColor={
                theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2]
              }
            />
            <Box
              sx={(theme) => ({
                width: width ?? '80%',

                [theme.fn.smallerThan('xs')]: {
                  width: '97%',
                },
              })}
            >
              {children}
            </Box>
          </Center>
        </AppShell>
      </Watermark>
    </WithWiderScreen>
  )
}

export default WithNavBar
