import React, { FC } from 'react'
import { AppShell, Box, Center } from '@mantine/core'
import AppNavbar from './AppNavbar'

const WithNavBar: FC<React.PropsWithChildren> = ({ children }) => {
  return (
    <AppShell
      padding="md"
      fixed
      navbar={<AppNavbar />}
    >
      <Center style={{ width: '100%' }}>
        <Box style={{ width: '80%' }}>{children}</Box>
      </Center>
    </AppShell>
  )
}

export default WithNavBar
