import { Button, Group, Modal, ModalProps, Stack, Text, Textarea } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { showErrorNotification } from '@Utils/ApiHelper'
import api, { GameNotice } from '@Api'

interface GameNoticeEditModalProps extends ModalProps {
  gameNotice?: GameNotice | null
  mutateGameNotice: (gameNotice: GameNotice) => void
}

const GameNoticeEditModal: FC<GameNoticeEditModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { gameNotice, mutateGameNotice, ...modalProps } = props

  const [content, setContent] = useState<string>(gameNotice?.values.at(-1) || '')
  const [disabled, setDisabled] = useState(false)
  const { t } = useTranslation()

  useEffect(() => {
    setContent(gameNotice?.values.at(-1) || '')
  }, [gameNotice])

  const onConfirm = () => {
    if (!content) {
      showNotification({
        color: 'red',
        message: t('common.error.empty'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }
    if (content === gameNotice?.values.at(-1)) {
      showNotification({
        color: 'orange',
        message: t('common.error.no_change'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)

    if (gameNotice && !disabled) {
      api.edit
        .editUpdateGameNotice(numId, gameNotice.id, {
          content: content.trim(),
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: t('admin.notification.games.notices.updated'),
            icon: <Icon path={mdiCheck} size={1} />,
          })
          mutateGameNotice(data.data)
          modalProps.onClose()
        })
        .catch((e) => showErrorNotification(e, t))
        .finally(() => {
          setDisabled(false)
        })
    } else {
      api.edit
        .editAddGameNotice(numId, {
          content: content.trim(),
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: t('admin.notification.games.notices.created'),
            icon: <Icon path={mdiCheck} size={1} />,
          })
          mutateGameNotice(data.data)
          setDisabled(false)
          modalProps.onClose()
        })
        .catch((e) => showErrorNotification(e, t))
        .finally(() => {
          setDisabled(false)
          setContent('')
        })
    }
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <Text>{t('admin.content.markdown_inline_support')}</Text>
        <Textarea
          value={content}
          w="100%"
          autosize
          minRows={5}
          maxRows={16}
          onChange={(e) => setContent(e.currentTarget.value)}
        />
        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onConfirm}>
            {t('common.modal.confirm')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default GameNoticeEditModal
