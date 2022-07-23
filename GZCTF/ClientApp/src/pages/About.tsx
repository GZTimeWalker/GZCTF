import { FC, useState } from 'react'
import { Text, Stack, Popover, Badge, Group, Code } from '@mantine/core'
import LogoHeader from '../components/LogoHeader'
import WithNavBar from '../components/WithNavbar'

const About: FC = () => {
  const sha = import.meta.env.VITE_APP_GIT_SHA ?? '000000'
  const tag = import.meta.env.VITE_APP_GIT_NAME ?? 'develop'
  const timestamp = import.meta.env.VITE_APP_BUILD_TIMESTAMP ?? '2022-07-23_12:00:00'
  const [detailOpened, setDetailOpened] = useState(false)

  return (
    <WithNavBar>
      <Stack justify="space-between" style={{ height: 'calc(100vh - 32px)' }}>
        <LogoHeader />
        <Stack style={{ height: 'calc(100vh - 48px)' }}>
          <Text>About</Text>
        </Stack>
        <Group position="right">
          <Popover
            opened={detailOpened}
            onClose={() => setDetailOpened(false)}
            position="top"
            placement="center"
            withArrow
            trapFocus={false}
            closeOnEscape={false}
            styles={{
              body: { pointerEvents: 'none', border: 'none' },
              arrow: { border: 'none' },
            }}
            target={
              <Badge
                onMouseEnter={() => setDetailOpened(true)}
                onMouseLeave={() => setDetailOpened(false)}
                onClick={() => window.open('https://github.com/GZTimeWalker/GZCTF')}
                style={{
                  cursor: 'pointer',
                }}
                size="md"
              >
                © 2022 GZTime {`#${sha.substring(0, 6)}`}
              </Badge>
            }
          >
            <Group spacing="xs">
              <Badge variant="gradient" gradient={{ from: 'teal', to: 'blue', deg: 60 }} size="md">
                {`#${sha.substring(0, 6)}:${tag}`}
              </Badge>
              <Code style={{ background: 'transparent' }}>
                {`built at ${timestamp.replaceAll('_', ' ')}`}
              </Code>
            </Group>
          </Popover>
        </Group>
      </Stack>
    </WithNavBar>
  )
}

export default About
