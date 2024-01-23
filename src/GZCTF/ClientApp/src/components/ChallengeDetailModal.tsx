import {
  ActionIcon,
  Box,
  Button,
  Divider,
  Group,
  LoadingOverlay,
  Modal,
  ModalProps,
  Stack,
  Text,
  TextInput,
  Title,
  Tooltip,
} from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiDownload, mdiLightbulbOnOutline, mdiLoading } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import MarkdownRender, { InlineMarkdownRender } from '@Components/MarkdownRender'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ChallengeTagItemProps } from '@Utils/Shared'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import { OnceSWRConfig } from '@Utils/useConfig'
import { useTypographyStyles } from '@Utils/useTypographyStyles'
import api, { AnswerResult, ChallengeType } from '@Api'
import InstanceEntry from './InstanceEntry'

interface ChallengeDetailModalProps extends ModalProps {
  gameId: number
  gameEnded: boolean
  tagData: ChallengeTagItemProps
  title: string
  score: number
  challengeId: number
  solved?: boolean
}

export const FlagPlaceholders: string[] = [
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

export const WrongFlagHints: string[] = [
  '饮水思源，重新审题吧。',
  '诗云：路漫漫其修远兮，再接再厉吧。',
  '沉着冷静，可能会有意想不到的收获。',
  '失败乃成功之母，回去再琢磨琢磨。',
  '非也非也，不是这个 flag。',
  '望眼欲穿，flag 却不在这里。',
  '不要浮躁，仔细再思考思考。',
  '翻遍天涯，也不是这个答案。',
  '走马观花，可找不到 flag。',
  '反复推敲，答案应该就在你手边。',
  '深谋远虑，flag 不是那么简单。',
  '山高水远，flag 藏得真是深啊！',
  '时运不济，碾过你的难道是寂寞？',
  '兴奋过头，还需要学会更加冷静的思考。',
  '碰壁了，难道是你已经到了巅峰？',
  '岁月静好，flag 却已然远去。',
  '浅水已涸，flag 不可复得。',
  '白雪纷纷何所似，似此 flag 被错过。',
  '旧事追思，往事如烟。flag 已然消逝。',
  '桃花潭水深千尺，不及 flag 不见了踪迹。',
  '万籁俱寂，唯有 flag 的错误提示在耳边响起。',
  '陌上花开，可缓缓归矣。flag 未得而返。',
  '风萧萧兮易水寒，无奈 flag 仍未到彼岸。',
]

const ChallengeDetailModal: FC<ChallengeDetailModalProps> = (props) => {
  const { gameId, gameEnded, challengeId, tagData, title, score, solved, ...modalProps } = props
  const { classes: tooltipClasses } = useTooltipStyles()

  const { data: challenge, mutate } = api.game.useGameGetChallenge(
    gameId,
    challengeId,
    OnceSWRConfig
  )

  const [placeholder, setPlaceholder] = useState('')

  useEffect(() => {
    setPlaceholder(FlagPlaceholders[Math.floor(Math.random() * FlagPlaceholders.length)])
  }, [challengeId])

  const isDynamic =
    challenge?.type === ChallengeType.StaticContainer ||
    challenge?.type === ChallengeType.DynamicContainer
  const { classes, theme } = useTypographyStyles()

  const [disabled, setDisabled] = useState(false)
  const [onSubmitting, setOnSubmitting] = useState(false)
  const [submitId, setSubmitId] = useState(0)
  const [flag, setFlag] = useInputState('')

  const { t } = useTranslation()

  const onCreateContainer = () => {
    if (!challengeId) return
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
        showNotification({
          color: 'teal',
          title: '实例已创建',
          message: '请注意实例到期时间',
          icon: <Icon path={mdiCheck} size={1} />,
        })
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => setDisabled(false))
  }

  const onDestroyContainer = () => {
    if (!challengeId) return
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
        showNotification({
          color: 'teal',
          title: '实例已销毁',
          message: '你可以重新创建实例',
          icon: <Icon path={mdiCheck} size={1} />,
        })
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => setDisabled(false))
  }

  const onProlongContainer = () => {
    if (!challengeId) return
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
      .catch((e) => showErrorNotification(e, t))
      .finally(() => setDisabled(false))
  }

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault()

    if (!challengeId || !flag) {
      showNotification({
        color: 'red',
        message: '不能提交空 flag',
        icon: <Icon path={mdiClose} size={1} />,
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
        notifications.clean()
        showNotification({
          id: 'flag-submitted',
          color: 'orange',
          title: 'flag 已提交',
          message: '请等待 flag 检查……',
          loading: true,
          autoClose: false,
        })
      })
      .catch((e) => showErrorNotification(e, t))
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
            checkDataFlag(submitId, res.data)
            clearInterval(polling)
            setDisabled(false)
          }
        })
        .catch((err) => {
          setOnSubmitting(false)
          setFlag('')
          showErrorNotification(err, t)
          clearInterval(polling)
          setDisabled(false)
        })
    }, 500)
    return () => clearInterval(polling)
  }, [submitId])

  const checkDataFlag = (id: number, data: string) => {
    if (data === AnswerResult.Accepted) {
      updateNotification({
        id: 'flag-submitted',
        color: 'teal',
        title: 'flag 正确',
        message: gameEnded ? '比赛已结束，本次提交不会被计分' : '排行榜将稍后更新……',
        icon: <Icon path={mdiCheck} size={1} />,
        autoClose: 8000,
      })
      if (isDynamic && challenge.context?.instanceEntry) onDestroyContainer()
      mutate()
      props.onClose()
    } else if (data === AnswerResult.WrongAnswer) {
      updateNotification({
        id: 'flag-submitted',
        color: 'red',
        title: 'flag 错误',
        message: WrongFlagHints[Math.floor(Math.random() * WrongFlagHints.length)],
        icon: <Icon path={mdiClose} size={1} />,
        autoClose: 8000,
      })
    } else {
      updateNotification({
        id: 'flag-submitted',
        color: 'yellow',
        title: 'flag 状态未知',
        message: `请联系管理员确认提交：${id}`,
        icon: <Icon path={mdiLoading} size={1} />,
        autoClose: false,
        withCloseButton: true,
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
        <Group w="100%" position="apart">
          <Group>
            {tagData && (
              <Icon path={tagData.icon} size={1} color={theme.colors[tagData?.color][5]} />
            )}
            <Title order={4}>{challenge?.title ?? title}</Title>
          </Group>
          <Text fw={700} ff={theme.fontFamilyMonospace}>
            {challenge?.score ?? score} pts
          </Text>
        </Group>
      }
    >
      <Stack spacing="sm">
        <Divider />
        <Stack spacing="sm" justify="space-between" pos="relative" mih="20vh">
          <LoadingOverlay visible={!challenge} />
          <Group grow noWrap position="right" align="flex-start" spacing={2}>
            <Box className={classes.root} mih="4rem">
              {challenge?.context?.url && (
                <Tooltip label="下载附件" position="left" classNames={tooltipClasses}>
                  <ActionIcon
                    component="a"
                    href={challenge.context?.url ?? '#'}
                    target="_blank"
                    rel="noreferrer"
                    variant="filled"
                    size="lg"
                    color="brand"
                    top={0}
                    right={0}
                    pos="absolute"
                  >
                    <Icon path={mdiDownload} size={1} />
                  </ActionIcon>
                </Tooltip>
              )}
              <MarkdownRender
                source={challenge?.content ?? ''}
                sx={{
                  '& div > p:first-child:before': {
                    content: '""',
                    float: 'right',
                    width: 45,
                    height: 45,
                  },
                }}
              />
            </Box>
          </Group>
          {challenge?.hints && challenge.hints.length > 0 && (
            <Stack spacing={2}>
              {challenge.hints.map((hint) => (
                <Group spacing="xs" align="flex-start" noWrap>
                  <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]} />
                  <InlineMarkdownRender
                    key={hint}
                    size="sm"
                    maw="calc(100% - 2rem)"
                    source={hint}
                  />
                </Group>
              ))}
            </Stack>
          )}
          {isDynamic && challenge.context && (
            <InstanceEntry
              context={challenge.context}
              onCreate={onCreateContainer}
              onProlong={onProlongContainer}
              onDestroy={onDestroyContainer}
              disabled={disabled}
            />
          )}
        </Stack>
        <Divider />
        {solved ? (
          <Text align="center" fw={700}>
            该题目已被解出
          </Text>
        ) : (
          <form onSubmit={onSubmit}>
            <TextInput
              placeholder={placeholder}
              value={flag}
              disabled={disabled}
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
              rightSectionWidth="6rem"
            />
          </form>
        )}
      </Stack>
    </Modal>
  )
}

export default ChallengeDetailModal
