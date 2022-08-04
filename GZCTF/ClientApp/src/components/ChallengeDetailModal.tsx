import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import { marked } from 'marked'
import { FC, useEffect, useState } from 'react'
import {
  ActionIcon,
  Button,
  Card,
  Code,
  Divider,
  Group,
  LoadingOverlay,
  Modal,
  ModalProps,
  Popover,
  Stack,
  Text,
  TextInput,
  Title,
  TypographyStylesProvider,
} from '@mantine/core'
import { useClipboard, useDisclosure, useInterval } from '@mantine/hooks'
import { mdiDownload, mdiLightbulbOnOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeType } from '../Api'
import { useTypographyStyles } from '../utils/ThemeOverride'
import { ChallengeTagItemProps } from './ChallengeItem'

interface ChallengeDetailModalProps extends ModalProps {
  gameId: number
  tagData: ChallengeTagItemProps
  title: string
  score: number
  challengeId: number
}

dayjs.extend(duration)

const Countdown: FC<{ time: string }> = ({ time }) => {
  const end = dayjs(time)
  const [now, setNow] = useState(dayjs())
  const interval = useInterval(() => setNow(dayjs()), 1000)

  const countdown = dayjs.duration(end.diff(now))

  useEffect(() => {
    if (dayjs() < end) {
      interval.start()
      return interval.stop
    }
  }, [])

  return (
    <Card style={{ width: '5rem', textAlign: 'center', padding: '0px 4px' }}>
      <Text size="sm" style={{ fontWeight: 700 }}>
        {countdown.asSeconds() > 0
          ? countdown.format('HH:mm:ss')
          : '00:00:00'}
      </Text>
    </Card>
  )
}

const ChallengeDetailModal: FC<ChallengeDetailModalProps> = (props) => {
  const { gameId, challengeId, tagData, title, score, ...modalProps } = props
  const [downloadOpened, { close: downloadClose, open: downloadOpen }] = useDisclosure(false)

  const { data: challenge } = api.game.useGameGetChallenge(gameId, challengeId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  const instanceCloseTime = dayjs(challenge?.context?.closeTime ?? 0)
  const instanceLeft = instanceCloseTime.diff(dayjs(), 'minute')

  const isDynamic =
    challenge?.type === ChallengeType.StaticContainer ||
    challenge?.type === ChallengeType.DynamicContainer
  const { classes, theme } = useTypographyStyles()
  const clipBoard = useClipboard()

  const onCreateContainer = () => {
    if (challengeId) {
      api.game.gameCreateContainer(gameId, challengeId).then((res) => {
        console.log(res.data)
      })
    }
  }

  const onDestoryContainer = () => {
    if (challengeId) {
      api.game.gameDeleteContainer(gameId, challengeId).then((res) => {
        console.log(res.data)
      })
    }
  }

  return (
    <Modal
      {...modalProps}
      styles={{
        ...modalProps.styles,
        header: {
          margin: 0,
        },
        title: {
          width: '100%',
          margin: 0,
        },
      }}
      title={
        <Group style={{ width: '100%' }} position="apart">
          <Title order={4}>{challenge?.title ?? title}</Title>
          <Text weight={700} sx={(theme) => ({ fontFamily: theme.fontFamilyMonospace })}>
            {challenge?.score ?? score} pts
          </Text>
        </Group>
      }
    >
      <Stack spacing="sm">
        <Divider
          size="sm"
          variant="dashed"
          color={tagData?.color}
          labelPosition="center"
          label={tagData && <Icon path={tagData.icon} size={1} />}
        />
        <Stack style={{ position: 'relative', minHeight: '5rem' }}>
          <LoadingOverlay visible={!challenge} />
          <Group grow noWrap position="right" align="flex-start" spacing={2}>
            <TypographyStylesProvider className={classes.root} style={{ minHeight: '4rem' }}>
              {challenge?.context?.url && (
                <Popover
                  opened={downloadOpened}
                  position="left"
                  width="5rem"
                  styles={{
                    dropdown: {
                      padding: '5px',
                    },
                  }}
                >
                  <Popover.Target>
                    <ActionIcon
                      onMouseEnter={downloadOpen}
                      onMouseLeave={downloadClose}
                      onClick={() => window.open(challenge.context?.url ?? '#')}
                      sx={(theme) => ({
                        ...theme.fn.hover({
                          color: theme.colors[tagData.color][5],
                        }),
                        float: 'right',
                      })}
                    >
                      <Icon path={mdiDownload} size={1} />
                    </ActionIcon>
                  </Popover.Target>
                  <Popover.Dropdown>
                    <Text size="sm" align="center">
                      下载附件
                    </Text>
                  </Popover.Dropdown>
                </Popover>
              )}
              <div dangerouslySetInnerHTML={{ __html: marked(challenge?.content ?? '') }} />
            </TypographyStylesProvider>
          </Group>
          {isDynamic && !challenge?.context?.instanceEntry && (
            <Group position="center" spacing={2}>
              <Button onClick={onCreateContainer}>启动实例</Button>
            </Group>
          )}
          {isDynamic && challenge?.context?.instanceEntry && (
            <Stack align="center">
              <Group>
                <Text size="sm" weight={600}>
                  实例访问入口：
                  <Code
                    style={{
                      backgroundColor: 'transparent',
                      fontSize: theme.fontSizes.sm,
                    }}
                    onClick={() => clipBoard.copy(challenge.context?.instanceEntry ?? '')}
                  >
                    {challenge?.context?.instanceEntry}
                  </Code>
                </Text>
                <Countdown time={challenge?.context?.closeTime ?? '0'} />
              </Group>
              <Group position="right">
                <Button color="orange" disabled={instanceLeft > 10}>
                  延长时间
                </Button>
                <Button color="red" onClick={onDestoryContainer}>销毁实例</Button>
              </Group>
            </Stack>
          )}
          {challenge?.hints && (
            <Stack spacing={2}>
              {challenge.hints.split(';').map((hint) => (
                <Group spacing="xs" align="flex-start" noWrap>
                  <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]} />
                  <Text key={hint} size="sm" style={{ maxWidth: 'calc(100% - 2rem)' }}>
                    {hint}
                  </Text>
                </Group>
              ))}
            </Stack>
          )}
        </Stack>
        <Divider size="sm" variant="dashed" color={tagData.color} />
        <TextInput
          placeholder="flag{...}"
          styles={{
            rightSection: {
              width: 'auto',
            },
            input: {
              fontFamily: theme.fontFamilyMonospace,
            },
          }}
          rightSection={<Button>提交 flag</Button>}
        />
      </Stack>
    </Modal>
  )
}

export default ChallengeDetailModal
