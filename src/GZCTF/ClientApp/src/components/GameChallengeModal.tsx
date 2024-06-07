import { ModalProps } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiLoading } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import ChallengeModal from '@Components/ChallengeModal'
import { showErrorNotification } from '@Utils/ApiHelper'
import { ChallengeTagItemProps } from '@Utils/Shared'
import { OnceSWRConfig } from '@Utils/useConfig'
import api, { AnswerResult, ChallengeType } from '@Api'

interface GameChallengeModalProps extends ModalProps {
  gameId: number
  gameEnded: boolean
  tagData: ChallengeTagItemProps
  title: string
  score: number
  challengeId: number
  solved?: boolean
}

const GameChallengeModal: FC<GameChallengeModalProps> = (props) => {
  const { gameId, gameEnded, challengeId, tagData, solved, title, score, ...modalProps } = props

  const { data: challenge, mutate } = api.game.useGameGetChallenge(
    gameId,
    challengeId,
    OnceSWRConfig
  )

  const { t } = useTranslation()

  const wrong_flag_hints = t('challenge.content.wrong_flag_hints', {
    returnObjects: true,
  }) as string[]

  const isDynamic =
    challenge?.type === ChallengeType.StaticContainer ||
    challenge?.type === ChallengeType.DynamicContainer

  const [disabled, setDisabled] = useState(false)
  const [submitId, setSubmitId] = useState(0)
  const [flag, setFlag] = useInputState('')

  const onCreate = () => {
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

  const onDestroy = () => {
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

  const onExtend = () => {
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

  const onSubmit = () => {
    if (!challengeId || !flag) {
      showNotification({
        color: 'red',
        message: t('challenge.notification.flag.empty'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)
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
            setDisabled(false)
            setFlag('')
            checkDataFlag(submitId, res.data)
            clearInterval(polling)
          }
        })
        .catch((err) => {
          setDisabled(false)
          setFlag('')
          showErrorNotification(err, t)
          clearInterval(polling)
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
      if (isDynamic && challenge.context?.instanceEntry) onDestroy()
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
    <ChallengeModal
      {...modalProps}
      challenge={challenge ?? { title, score }}
      tagData={tagData}
      solved={solved}
      flag={flag}
      setFlag={setFlag}
      onCreate={onCreate}
      onDestroy={onDestroy}
      onSubmitFlag={onSubmit}
      disabled={disabled}
      onExtend={onExtend}
    />
  )
}

export default GameChallengeModal
