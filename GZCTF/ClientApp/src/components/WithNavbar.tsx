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
      <AppShell padding={padding ?? 'md'} fixed navbar={<AppNavbar />} header={<AppHeader />}>
        <Watermark
          text={user?.userId ?? ''}
          textColor={theme.colorScheme == 'dark' ? theme.white : theme.black}
          rotate={-13}
          textSize={15}
          opacity={theme.colorScheme == 'dark' ? 0.004 : 0.015}
          fontFamily="JetBrains Mono"
        >
          <Center style={{ width: '100%' }}>
            <LoadingOverlay
              visible={isLoading ?? false}
              overlayOpacity={1}
              overlayColor={
                theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2]
              }
            />
            <Box style={{ width: width ?? '80%' }}>{children}</Box>
          </Center>
        </Watermark>
      </AppShell>
    </WithWiderScreen>
  )
}

export default WithNavBar
