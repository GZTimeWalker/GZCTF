import { Button, Group, Modal, ModalProps, Stack, Switch, Text, Textarea } from '@mantine/core'
import { DateTimePicker } from '@mantine/dates'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { showErrorMsg } from '@Utils/Shared'
import api, { GameNotice } from '@Api'

interface GameNoticeEditModalProps extends ModalProps {
  gameNotice?: GameNotice | null
  mutateGameNotice: (gameNotice: GameNotice) => void
}

export const GameNoticeEditModal: FC<GameNoticeEditModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { gameNotice, mutateGameNotice, ...modalProps } = props

  const [content, setContent] = useState<string>(gameNotice?.values.at(-1) || '')
  const [scheduled, setScheduled] = useState(false)
  const [publishAt, setPublishAt] = useState<Date | null>(null)
  const [disabled, setDisabled] = useState(false)
  const { t } = useTranslation()

  useEffect(() => {
    setContent(gameNotice?.values.at(-1) || '')
    // Pre-populate schedule if existing notice has a future publish time
    if (gameNotice?.time) {
      const t = new Date(gameNotice.time)
      if (t > new Date()) {
        setScheduled(true)
        setPublishAt(t)
      } else {
        setScheduled(false)
        setPublishAt(null)
      }
    }
  }, [gameNotice])

  const onConfirm = async () => {
    if (!content) {
      showNotification({
        color: 'red',
        message: t('common.error.empty'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }
    if (content === gameNotice?.values.at(-1) && !scheduled) {
      showNotification({
        color: 'orange',
        message: t('common.error.no_change'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)

    try {
      const body = {
        content: content.trim(),
        publishAt: scheduled && publishAt ? publishAt.toISOString() : undefined,
      }
      const res = gameNotice
        ? await api.edit.editUpdateGameNotice(numId, gameNotice.id, body)
        : await api.edit.editAddGameNotice(numId, body)
      showNotification({
        color: 'teal',
        message: scheduled && publishAt
          ? t('admin.notification.games.notices.scheduled')
          : t(`admin.notification.games.notices.${gameNotice ? 'updated' : 'created'}`),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutateGameNotice(res.data)
      modalProps.onClose()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
      setContent('')
      setScheduled(false)
      setPublishAt(null)
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
        <Switch
          label={t('admin.label.games.notices.schedule')}
          checked={scheduled}
          onChange={(e) => {
            setScheduled(e.currentTarget.checked)
            if (!e.currentTarget.checked) setPublishAt(null)
          }}
        />
        {scheduled && (
          <DateTimePicker
            label={t('admin.label.games.notices.publish_at')}
            placeholder={t('admin.placeholder.games.notices.publish_at')}
            value={publishAt}
            onChange={setPublishAt}
            minDate={new Date()}
            clearable
          />
        )}
        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled || (scheduled && !publishAt)} onClick={onConfirm}>
            {t('common.modal.confirm')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
