import React, { FC } from 'react'
import { AppShell, Box, Center } from '@mantine/core'
import AppNavbar from './AppNavbar'

interface WithNavBarProps extends React.PropsWithChildren {
  width?: string
}

const WithNavBar: FC<WithNavBarProps> = ({ children, width }) => {
  return (
    <AppShell padding="md" fixed navbar={<AppNavbar />}>
      <Center style={{ width: '100%' }}>
        <Box style={{ width: width ?? '80%' }}>{children}</Box>
      </Center>
    </AppShell>
  )
}

export default WithNavBar
