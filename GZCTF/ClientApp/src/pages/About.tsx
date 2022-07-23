import { FC } from 'react'
import { Text, Stack } from '@mantine/core'
import LogoHeader from '../components/LogoHeader'
import WithNavBar from '../components/WithNavbar'

const About: FC = () => {
  const sha = import.meta.env.VITE_APP_GIT_SHA
  const tag = import.meta.env.VITE_APP_GIT_NAME
  const timestamp = import.meta.env.VITE_APP_BUILD_TIMESTAMP

  return (
    <WithNavBar>
      <Stack justify="space-between" style={{ height: 'calc(100vh - 32px)' }}>
        <LogoHeader />
        <Stack style={{ height: 'calc(100vh - 48px)' }}>
          <Text>About</Text>
        </Stack>
        <Text size="xs" align="center">
          © 2022 GZTime
          {import.meta.env.PROD && sha && ` #${sha.substring(0, 6)}:${tag} built at ${timestamp}`}
        </Text>
      </Stack>
    </WithNavBar>
  )
}

export default About
