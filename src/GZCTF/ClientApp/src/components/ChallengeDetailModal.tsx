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
import { showErrorNotification } from '@Utils/ApiHelper'
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

const ChallengeDetailModal: FC<ChallengeDetailModalProps> = (props) => {
  const { gameId, gameEnded, challengeId, tagData, title, score, solved, ...modalProps } = props
  const { classes: tooltipClasses } = useTooltipStyles()

  const { data: challenge, mutate } = api.game.useGameGetChallenge(
    gameId,
    challengeId,
    OnceSWRConfig
  )

  const { t } = useTranslation()

  const placeholders = t('challenge.content.flag_placeholders', {
    returnObjects: true,
  }) as string[]

  const wrong_flag_hints = t('challenge.content.wrong_flag_hints', {
    returnObjects: true,
  }) as string[]

  const [placeholder, setPlaceholder] = useState('')

  useEffect(() => {
    setPlaceholder(placeholders[Math.floor(Math.random() * placeholders.length)])
  }, [challengeId])

  const isDynamic =
    challenge?.type === ChallengeType.StaticContainer ||
    challenge?.type === ChallengeType.DynamicContainer
  const { classes, theme } = useTypographyStyles()

  const [disabled, setDisabled] = useState(false)
  const [onSubmitting, setOnSubmitting] = useState(false)
  const [submitId, setSubmitId] = useState(0)
  const [flag, setFlag] = useInputState('')

  const onCreateContainer = () => {
    if (!challengeId || disabled) return
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
          title: t('challenge.notification.instance.created.title'),
          message: t('challenge.notification.instance.created.message'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => setDisabled(false))
  }

  const onDestroyContainer = () => {
    if (!challengeId || disabled) return
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
          title: t('challenge.notification.instance.destroyed.title'),
          message: t('challenge.notification.instance.destroyed.message'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => setDisabled(false))
  }

  const onExtendContainer = () => {
    if (!challengeId || disabled) return
    setDisabled(true)
    api.game
      .gameExtendContainerLifetime(gameId, challengeId)
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
        message: t('challenge.notification.flag.empty'),
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
          title: t('challenge.notification.flag.submitted.title'),
          message: t('challenge.notification.flag.submitted.message'),
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
        title: t('challenge.notification.flag.accepted.title'),
        message: gameEnded
          ? t('challenge.notification.flag.accepted.ended')
          : t('challenge.notification.flag.accepted.message'),
        icon: <Icon path={mdiCheck} size={1} />,
        autoClose: 8000,
        loading: false,
      })
      if (isDynamic && challenge.context?.instanceEntry) onDestroyContainer()
      mutate()
      props.onClose()
    } else if (data === AnswerResult.WrongAnswer) {
      updateNotification({
        id: 'flag-submitted',
        color: 'red',
        title: t('challenge.notification.flag.wrong'),
        message: wrong_flag_hints[Math.floor(Math.random() * wrong_flag_hints.length)],
        icon: <Icon path={mdiClose} size={1} />,
        autoClose: 8000,
        loading: false,
      })
    } else {
      updateNotification({
        id: 'flag-submitted',
        color: 'yellow',
        title: t('challenge.notification.flag.unknown.title'),
        message: t('challenge.notification.flag.unknown.message', {
          id,
        }),
        icon: <Icon path={mdiLoading} size={1} />,
        autoClose: false,
        withCloseButton: true,
      })
    }
  }

  return (
    <Modal
      size="45%"
      withCloseButton={false}
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
        <Group wrap="nowrap" w="100%" justify="space-between" gap="sm">
          <Group wrap="nowrap" gap="sm">
            {tagData && (
              <Icon path={tagData.icon} size={1} color={theme.colors[tagData?.color][5]} />
            )}
            <Title w="calc(100% - 1.5rem)" order={4} lineClamp={1}>
              {challenge?.title ?? title}
            </Title>
          </Group>
          <Text miw="5em" fw="bold" ff="monospace">
            {challenge?.score ?? score} pts
          </Text>
        </Group>
      }
    >
      <Stack gap="sm">
        <Divider />
        <Stack gap="sm" justify="space-between" pos="relative" mih="20vh">
          <LoadingOverlay visible={!challenge} />
          <Group grow wrap="nowrap" justify="right" align="flex-start" gap={2}>
            <Box className={classes.root} mih="4rem">
              {challenge?.context?.url && (
                <Tooltip
                  label={t('challenge.button.download.attachment')}
                  position="left"
                  classNames={tooltipClasses}
                >
                  <ActionIcon
                    component="a"
                    href={challenge.context?.url ?? '#'}
                    target="_blank"
                    rel="noreferrer"
                    variant="filled"
                    size="lg"
                    color={theme.primaryColor}
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
            <Stack gap={2}>
              {challenge.hints.map((hint) => (
                <Group gap="xs" align="flex-start" wrap="nowrap">
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
              onExtend={onExtendContainer}
              onDestroy={onDestroyContainer}
              disabled={disabled}
            />
          )}
        </Stack>
        <Divider />
        {solved ? (
          <Text ta="center" fw="bold">
            {t('challenge.content.already_solved')}
          </Text>
        ) : (
          <form onSubmit={onSubmit}>
            <Group justify="space-between" gap="sm" align="flex-end">
              <TextInput
                placeholder={placeholder}
                value={flag}
                disabled={disabled}
                onChange={setFlag}
                style={{ flexGrow: 1 }}
                styles={{
                  input: {
                    fontFamily: theme.fontFamilyMonospace,
                  },
                }}
              />
              <Button miw="6rem" type="submit" disabled={onSubmitting}>
                {t('challenge.button.submit_flag')}
              </Button>
            </Group>
          </form>
        )}
      </Stack>
    </Modal>
  )
}

export default ChallengeDetailModal
