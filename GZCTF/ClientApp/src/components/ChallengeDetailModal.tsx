import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import { FC, useEffect, useState } from 'react'
import React from 'react'
import {
  ActionIcon,
  Box,
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
  Tooltip,
} from '@mantine/core'
import { useClipboard, useDisclosure, useInputState } from '@mantine/hooks'
import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiDownload, mdiLightbulbOnOutline, mdiLoading } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import { useTypographyStyles } from '@Utils/useTypographyStyles'
import api, { AnswerResult, ChallengeType } from '@Api'
import { ChallengeTagItemProps } from '../utils/ChallengeItem'
import MarkdownRender from './MarkdownRender'

interface ChallengeDetailModalProps extends ModalProps {
  gameId: number
  tagData: ChallengeTagItemProps
  title: string
  score: number
  challengeId: number
  solved?: boolean
}

dayjs.extend(duration)

const Countdown: FC<{ time: string }> = ({ time }) => {
  const end = dayjs(time)
  const [now, setNow] = useState(dayjs())
  const countdown = dayjs.duration(end.diff(now))

  useEffect(() => {
    if (dayjs() > end) return
    const interval = setInterval(() => setNow(dayjs()), 1000)
    return () => clearInterval(interval)
  }, [])

  return (
    <Card style={{ width: '5rem', textAlign: 'center', padding: '0px 4px' }}>
      <Text size="sm" style={{ fontWeight: 700 }}>
        {countdown.asSeconds() > 0 ? countdown.format('HH:mm:ss') : '00:00:00'}
      </Text>
    </Card>
  )
}

const FlagPlaceholders: string[] = [
  '横看成岭侧成峰，flag 高低各不同',
  'flag 当关，万夫莫开',
  '寻寻觅觅，冷冷清清，flag 惨惨戚戚',
  '问君能有几多愁？恰似一江 flag 向东流',
  '人生得意须尽欢，莫使 flag 空对月',
  '汉皇重色思 flag，御宇多年求不得',
  'flag 几时有？把酒问青天',
  '羽扇纶巾，谈笑间，flag 灰飞烟灭',
  '浊酒一杯家万里，flag 未勒归无计',
  '孤帆远影碧空尽，唯见 flag 天际流',
  '安得 flag 千万间，大庇天下 ctfer 俱欢颜！',
  '两个黄鹂鸣翠柳，一行 flag 上青天',
  'flag 一场大梦，人生几度秋凉？',
  '剪不断，理还乱，是 flag',
  '蓦然回首，flag 却在，灯火阑珊处',
  '稻花香里说丰年，听取 flag 一片',
  '采菊东篱下，悠然见 flag',
  '不畏 flag 遮望眼，自缘身在最高层',
  '便纵有千种 flag，更与何人说？',
  '人生自古谁无死？留取 flag 照汗青',
  '借问 flag 何处有？牧童遥指杏花村',
]

const ChallengeDetailModal: FC<ChallengeDetailModalProps> = (props) => {
  const { gameId, challengeId, tagData, title, score, solved, ...modalProps } = props
  const [downloadOpened, { close: downloadClose, open: downloadOpen }] = useDisclosure(false)

  const { data: challenge, mutate } = api.game.useGameGetChallenge(gameId, challengeId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  const [placeholder, setPlaceholder] = useState('')

  useEffect(() => {
    setPlaceholder(FlagPlaceholders[Math.floor(Math.random() * FlagPlaceholders.length)])
  }, [challengeId])

  const instanceCloseTime = dayjs(challenge?.context?.closeTime ?? 0)
  const instanceLeft = instanceCloseTime.diff(dayjs(), 'minute')

  const isDynamic =
    challenge?.type === ChallengeType.StaticContainer ||
    challenge?.type === ChallengeType.DynamicContainer
  const { classes, theme } = useTypographyStyles()
  const { classes: tooltipClasses } = useTooltipStyles()
  const clipBoard = useClipboard()

  const [disabled, setDisabled] = useState(false)
  const [onSubmitting, setOnSubmitting] = useState(false)
  const [submitId, setSubmitId] = useState(0)
  const [flag, setFlag] = useInputState('')

  const onCreateContainer = () => {
    if (challengeId) {
      setDisabled(true)
      api.game
        .gameCreateContainer(gameId, challengeId)
        .then((res) => {
          mutate({
            ...challenge,
            context: {
              ...challenge?.context,
              closeTime: res.data.expectStopAt,
              instanceEntry: res.data.entry,
            },
          })
        })
        .catch(showErrorNotification)
        .finally(() => setDisabled(false))
    }
  }

  const onDestroyContainer = () => {
    if (challengeId) {
      setDisabled(true)
      api.game
        .gameDeleteContainer(gameId, challengeId)
        .then(() => {
          mutate({
            ...challenge,
            context: {
              ...challenge?.context,
              closeTime: null,
              instanceEntry: null,
            },
          })
        })
        .catch(showErrorNotification)
        .finally(() => setDisabled(false))
    }
  }

  const onProlongContainer = () => {
    if (challengeId) {
      setDisabled(true)
      api.game
        .gameProlongContainer(gameId, challengeId)
        .then((res) => {
          mutate({
            ...challenge,
            context: {
              ...challenge?.context,
              closeTime: res.data.expectStopAt,
            },
          })
        })
        .catch(showErrorNotification)
        .finally(() => setDisabled(false))
    }
  }

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault()

    if (!challengeId || !flag) {
      showNotification({
        color: 'red',
        message: 'Flag 为空不可提交',
        icon: <Icon path={mdiClose} size={1} />,
        disallowClose: true,
      })
      return
    }

    setOnSubmitting(true)
    api.game
      .gameSubmit(gameId, challengeId, {
        flag,
      })
      .then((res) => {
        setSubmitId(res.data)

        showNotification({
          id: 'flag-submitted',
          color: 'orange',
          message: '请等待 flag 检查……',
          loading: true,
          autoClose: false,
          disallowClose: true,
        })
      })
      .catch(showErrorNotification)
  }

  useEffect(() => {
    // submitId initialization will trigger useEffect
    if (!submitId) return

    const polling = setInterval(() => {
      api.game
        .gameStatus(gameId, challengeId, submitId)
        .then((res) => {
          if (res.data !== AnswerResult.FlagSubmitted) {
            setOnSubmitting(false)
            setFlag('')
            checkDataFlag(res.data)
            clearInterval(polling)
            setDisabled(false)
            api.game.mutateGameChallengesWithTeamInfo(gameId)
          }
        })
        .catch((err) => {
          setOnSubmitting(false)
          setFlag('')
          showErrorNotification(err)
          clearInterval(polling)
          setDisabled(false)
        })
    }, 500)
    return () => clearInterval(polling)
  }, [submitId])

  const checkDataFlag = (data: string) => {
    if (data === AnswerResult.Accepted) {
      updateNotification({
        id: 'flag-submitted',
        color: 'teal',
        message: 'Flag 正确',
        icon: <Icon path={mdiCheck} size={1} />,
        disallowClose: true,
      })
      if (isDynamic) onDestroyContainer()
      mutate()
      props.onClose()
    } else if (data === AnswerResult.WrongAnswer) {
      updateNotification({
        id: 'flag-submitted',
        color: 'red',
        message: 'Flag 错误',
        icon: <Icon path={mdiClose} size={1} />,
        disallowClose: true,
      })
    } else {
      updateNotification({
        id: 'flag-submitted',
        color: 'yellow',
        message: 'Flag 状态未知',
        icon: <Icon path={mdiLoading} size={1} />,
        disallowClose: true,
      })
    }
  }

  return (
    <Modal
      {...modalProps}
      onClose={() => {
        setFlag('')
        modalProps.onClose()
      }}
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
          <Group>
            {tagData && (
              <Icon path={tagData.icon} size={1} color={theme.colors[tagData?.color][5]} />
            )}
            <Title order={4}>{challenge?.title ?? title}</Title>
          </Group>
          <Text weight={700} sx={(theme) => ({ fontFamily: theme.fontFamilyMonospace })}>
            {challenge?.score ?? score} pts
          </Text>
        </Group>
      }
    >
      <Stack spacing="sm" style={{ marginTop: theme.spacing.sm }}>
        <Divider />
        <Stack spacing="sm" justify="space-between" style={{ position: 'relative', minHeight: '20vh' }}>
          <LoadingOverlay visible={!challenge} />
          <Group grow noWrap position="right" align="flex-start" spacing={2}>
            <Box className={classes.root} style={{ minHeight: '4rem' }}>
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
                      variant='filled'
                      size="lg"
                      color="brand"
                      onMouseEnter={downloadOpen}
                      onMouseLeave={downloadClose}
                      onClick={() => window.open(challenge.context?.url ?? '#')}
                      style={{
                        float: 'right'
                      }}
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
              <MarkdownRender source={challenge?.content ?? ''} />
            </Box>
          </Group>
          {challenge?.hints && challenge.hints.length > 0 && (
            <Stack spacing={2}>
              {challenge.hints.map((hint) => (
                <Group spacing="xs" align="flex-start" noWrap>
                  <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]} />
                  <Text key={hint} size="sm" style={{ maxWidth: 'calc(100% - 2rem)' }}>
                    {hint}
                  </Text>
                </Group>
              ))}
            </Stack>
          )}
          {isDynamic && !challenge?.context?.instanceEntry && (
            <Group position="center" spacing={2}>
              <Button onClick={onCreateContainer} disabled={disabled} loading={disabled}>
                开启实例
              </Button>
            </Group>
          )}
          {isDynamic && challenge?.context?.instanceEntry && (
            <Stack align="center">
              <Group>
                <Text size="sm" weight={600}>
                  实例访问入口：
                  <Tooltip label="点击复制" withArrow classNames={tooltipClasses}>
                    <Code
                      style={{
                        backgroundColor: 'transparent',
                        fontSize: theme.fontSizes.sm,
                        cursor: 'pointer',
                      }}
                      onClick={() => {
                        clipBoard.copy(challenge.context?.instanceEntry ?? '')
                        showNotification({
                          color: 'teal',
                          message: '实例入口已复制到剪贴板',
                          icon: <Icon path={mdiCheck} size={1} />,
                          disallowClose: true,
                        })
                      }}
                    >
                      {challenge?.context?.instanceEntry}
                    </Code>
                  </Tooltip>
                </Text>
                <Countdown time={challenge?.context?.closeTime ?? '0'} />
              </Group>
              <Group position="center">
                <Button color="orange" onClick={onProlongContainer} disabled={instanceLeft > 10}>
                  延长时间
                </Button>
                <Button color="red" onClick={onDestroyContainer} disabled={disabled}>
                  销毁实例
                </Button>
              </Group>
            </Stack>
          )}
        </Stack>
        <Divider />
        {solved ? (
          <Text align="center" weight={700}>
            该题目已被解出
          </Text>
        ) : (
          <form onSubmit={onSubmit}>
            <TextInput
              placeholder={placeholder}
              value={flag}
              onChange={setFlag}
              styles={{
                rightSection: {
                  width: 'auto',
                },
                input: {
                  fontFamily: `${theme.fontFamilyMonospace}, ${theme.fontFamily}`,
                },
              }}
              rightSection={
                <Button type="submit" onClick={onSubmit} disabled={onSubmitting}>
                  提交 flag
                </Button>
              }
            />
          </form>
        )}
      </Stack>
    </Modal>
  )
}

export default ChallengeDetailModal
