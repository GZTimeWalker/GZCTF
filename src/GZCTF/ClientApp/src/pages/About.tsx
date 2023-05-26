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

const sha = import.meta.env.VITE_APP_GIT_SHA ?? 'unknown'
const tag = import.meta.env.VITE_APP_GIT_NAME ?? 'unknown'
const timestamp = import.meta.env.VITE_APP_BUILD_TIMESTAMP ?? ''
const builtdate = import.meta.env.DEV ? dayjs() : dayjs(timestamp)
const repo = 'https://github.com/GZTimeWalker/GZCTF'

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

  const valid =
    timestamp.length === 20 && builtdate.isValid() && sha.length === 40 && tag.length > 0

  return (
    <WithNavBar>
      <Stack justify="space-between" h="calc(100vh - 32px)">
        <Center h="calc(100vh - 32px)">
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
                onClick={() => window.open(repo)}
                style={{
                  cursor: 'pointer',
                }}
                size="lg"
                variant="outline"
              >
                © 2022-Now GZTime {valid ? `#${sha.substring(0, 6)}` : ''}
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
                    <Group ml="-18px" mt="-5px">
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
                        gradient={
                          valid
                            ? { from: 'teal', to: 'blue', deg: 60 }
                            : { from: 'red', to: 'orange', deg: -60 }
                        }
                        size="xs"
                      >
                        {valid ? `${tag}#${sha.substring(0, 6)}` : 'UNOFFICIAL'}
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
                    {valid
                      ? `Pushed at ${builtdate.format('YYYY-MM-DDTHH:mm:ssZ')}`
                      : 'This release is not officially built'}
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
