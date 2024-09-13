import { ModalProps } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import React, { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import ChallengeModal from '@Components/ChallengeModal'
import { ChallengeCategoryItemProps } from '@Utils/Shared'
import { ChallengeDetailModel } from '@Api'

interface ChallengePreviewModalProps extends ModalProps {
  challenge: ChallengeDetailModel
  cateData: ChallengeCategoryItemProps
}

interface FakeContext {
  closeTime: string | null
  instanceEntry: string | null
  url: string
}

const ChallengePreviewModal: FC<ChallengePreviewModalProps> = (props) => {
  const { challenge, cateData, ...modalProps } = props

  const [context, setContext] = useState<FakeContext>({
    closeTime: null,
    instanceEntry: null,
    url: '/assets/attachment.zip',
  })

  const { t } = useTranslation()
  const [flag, setFlag] = useInputState('')

  const onCreate = () => {
    setContext({
      ...context,
      closeTime: dayjs().add(10, 'm').add(10, 's').toJSON(),
      instanceEntry: 'localhost:2333',
    })
  }

  const onDestroy = () => {
    setContext({
      ...context,
      closeTime: null,
      instanceEntry: null,
    })
  }

  const onExtend = () => {
    setContext({
      ...context,
      closeTime: dayjs(context.closeTime).add(10, 'm').toJSON(),
      instanceEntry: context.instanceEntry,
    })
  }

  const onSubmit = () => {
    showNotification({
      color: 'teal',
      title: t('admin.notification.games.challenges.preview.flag_submitted'),
      message: flag,
      icon: <Icon path={mdiCheck} size={1} />,
    })
    setFlag('')
  }

  const onDownload = () => {
    showNotification({
      color: 'teal',
      message: t('admin.notification.games.challenges.preview.attachment_downloaded'),
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  return (
    <ChallengeModal
      {...modalProps}
      challenge={{ ...challenge, context: context }}
      cateData={cateData}
      flag={flag}
      setFlag={setFlag}
      onCreate={onCreate}
      onDestroy={onDestroy}
      onSubmitFlag={onSubmit}
      onExtend={onExtend}
      onDownload={onDownload}
    />
  )
}

export default ChallengePreviewModal
