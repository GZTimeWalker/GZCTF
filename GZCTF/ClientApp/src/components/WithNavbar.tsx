import React, { FC } from 'react'
import {
  AppShell,
  Box,
  Center,
  LoadingOverlay,
  MantineNumberSize,
  useMantineTheme,
} from '@mantine/core'
import AppHeader from './AppHeader'
import AppNavbar from './AppNavbar'
import WithWiderScreen from './WithWiderScreen'

interface WithNavBarProps extends React.PropsWithChildren {
  width?: string
  minWidth?: number
  padding?: MantineNumberSize
  isLoading?: boolean
}

const WithNavBar: FC<WithNavBarProps> = ({ children, width, padding, isLoading, minWidth }) => {
  const theme = useMantineTheme()

  return (
    <WithWiderScreen minWidth={minWidth}>
      <AppShell padding={padding ?? 'md'} fixed navbar={<AppNavbar />} header={<AppHeader />}>
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
      </AppShell>
    </WithWiderScreen>
  )
}

export default WithNavBar
