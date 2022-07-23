import { FC } from 'react'
import { Text, Stack } from '@mantine/core'
import LogoHeader from '../components/LogoHeader'
import WithNavBar from '../components/WithNavbar'

const About: FC = () => {
  return (
    <WithNavBar>
      <Stack justify="space-between" style={{ height: 'calc(100vh - 32px)' }}>
        <LogoHeader />
        <Stack style={{ height: 'calc(100vh - 48px)' }}>
          <Text>About</Text>
        </Stack>
        <Text size="xs" align="center">
          © 2022 GZTime
          {import.meta.env.PROD &&
            `#${import.meta.env.VITE_APP_GIT_SHA.substring(0, 6)} - ${
              import.meta.env.VITE_APP_TIMESTAMP
            }`}
        </Text>
      </Stack>
    </WithNavBar>
  )
}

export default About
