import dayjs from 'dayjs'
import { FC } from 'react'
import {
  Text,
  Stack,
  Badge,
  Group,
  HoverCard,
  Title,
  createStyles,
  Anchor,
  keyframes,
  Center,
} from '@mantine/core'
import WithNavBar from '@Components/WithNavbar'
import MainIcon from '@Components/icon/MainIcon'
import { useConfig } from '@Utils/useConfig'
import { usePageTitle } from '@Utils/usePageTitle'

const sha = import.meta.env.VITE_APP_GIT_SHA ?? '000000'
const tag = import.meta.env.VITE_APP_GIT_NAME ?? 'v0.0.0'
const timestamp = import.meta.env.VITE_APP_BUILD_TIMESTAMP ?? '2022-07-23T12:00:00Z'
const builtdate = dayjs(import.meta.env.DEV ? new Date() : new Date(timestamp))

const useStyles = createStyles((theme) => ({
  title: {
    marginLeft: '-20px',
    marginBottom: '-5px',
    color: theme.colorScheme === 'dark' ? theme.colors.white[0] : theme.colors.gray[6],
  },
  brand: {
    color: theme.colors[theme.primaryColor][4],
  },
  bio: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 'bold',
    fontSize: '1.5rem',
    color: theme.colorScheme === 'dark' ? theme.colors.gray[2] : theme.colors.dark[4],
  },
  blink: {
    animation: `${keyframes`0%, 100% {opacity:0;} 50% {opacity:1;}`} 1s infinite steps(1,start)`,
  },
  watermark: {
    position: 'absolute',
    fontSize: '12rem',
    fontWeight: 'bold',
    opacity: 0.05,
    transform: 'scale(1.5)',
    userSelect: 'none',
  },
}))

const About: FC = () => {
  const { classes } = useStyles()
  const { config } = useConfig()

  usePageTitle('关于')

  return (
    <WithNavBar>
      <Stack justify="space-between" style={{ height: 'calc(100vh - 32px)' }}>
        <Center style={{ height: 'calc(100vh - 32px)' }}>
          <Title order={2} className={classes.watermark}>
            GZ::CTF
          </Title>
          <Text className={classes.bio}>
            &gt; {config?.slogan ?? 'Hack for fun not for profit'}
            <Text span className={classes.blink}>
              _
            </Text>
          </Text>
        </Center>
        <Group position="right">
          <HoverCard shadow="md" position="top-end" withArrow openDelay={200} closeDelay={400}>
            <HoverCard.Target>
              <Badge
                onClick={() => window.open('https://github.com/GZTimeWalker/GZCTF')}
                style={{
                  cursor: 'pointer',
                }}
                size="lg"
                variant="outline"
              >
                © 2022 GZTime {`#${sha.substring(0, 6)}`}
              </Badge>
            </HoverCard.Target>
            <HoverCard.Dropdown>
              <Stack>
                <Group>
                  <MainIcon style={{ maxWidth: 60, height: 'auto' }} />
                  <Stack spacing="xs">
                    <Title className={classes.title}>
                      GZ<span className={classes.brand}>::</span>CTF
                    </Title>
                    <Group sx={{ marginLeft: '-18px', marginTop: '-5px' }}>
                      <Anchor
                        href="https://github.com/GZTimeWalker"
                        color="dimmed"
                        size="sm"
                        weight={500}
                        sx={{ lineHeight: 1 }}
                      >
                        @GZTimeWalker
                      </Anchor>
                      <Badge
                        variant="gradient"
                        gradient={{ from: 'teal', to: 'blue', deg: 60 }}
                        size="xs"
                      >
                        {`${tag}#${sha.substring(0, 6)}`}
                      </Badge>
                    </Group>
                  </Stack>
                </Group>
                <Group spacing="xs">
                  <Text
                    size="xs"
                    weight={500}
                    color="dimmed"
                    sx={(theme) => ({ fontFamily: theme.fontFamilyMonospace })}
                  >
                    built at {builtdate.format('YYYY-MM-DDTHH:mm:ssZ')}
                  </Text>
                </Group>
              </Stack>
            </HoverCard.Dropdown>
          </HoverCard>
        </Group>
      </Stack>
    </WithNavBar>
  )
}

export default About
