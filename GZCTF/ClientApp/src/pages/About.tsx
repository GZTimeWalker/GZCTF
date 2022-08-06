import LogoHeader from '@Components/LogoHeader'
import WithNavBar from '@Components/WithNavbar'
import MainIcon from '@Components/icon/MainIcon'
import dayjs from 'dayjs'
import { FC } from 'react'
import { Text, Stack, Badge, Group, HoverCard, Title, createStyles, Anchor } from '@mantine/core'

const sha = import.meta.env.VITE_APP_GIT_SHA ?? '000000'
const tag = import.meta.env.VITE_APP_GIT_NAME ?? 'develop'
const timestamp = import.meta.env.VITE_APP_BUILD_TIMESTAMP ?? '2022-07-23T12:00:00Z'
const builtdate = dayjs(import.meta.env.DEV ? new Date() : new Date(timestamp))

const useStyles = createStyles((theme) => ({
  brand: {
    color: theme.colors[theme.primaryColor][4],
  },
  title: {
    marginLeft: '-20px',
    marginBottom: '-5px',
    color: theme.colorScheme === 'dark' ? theme.colors.white[0] : theme.colors.gray[6],
  },
}))

const About: FC = () => {
  const { classes } = useStyles()

  return (
    <WithNavBar>
      <Stack justify="space-between" style={{ height: 'calc(100vh - 32px)' }}>
        <LogoHeader />
        <Stack style={{ height: 'calc(100vh - 48px)' }}>
          <Text>About</Text>
        </Stack>
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
                        {`#${sha.substring(0, 6)}:${tag}`}
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
