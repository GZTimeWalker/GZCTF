import { Button, FileButton, Modal, ModalProps, Progress, Stack, Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiFileUploadOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WriteupExampleList } from '@Components/WriteupExampleList'
import { showErrorMsg } from '@Utils/Shared'
import { OnceSWRConfig } from '@Hooks/useConfig'
import api, { WriteupExampleModel } from '@Api'

interface WriteupExamplesModalProps extends ModalProps {
  gameId: number
}

export const WriteupExamplesModal: FC<WriteupExamplesModalProps> = ({ gameId, ...props }) => {
  const { data, mutate } = api.admin.useAdminWriteups(gameId, OnceSWRConfig, props.opened)
  const [busy, setBusy] = useState(false)
  const [progress, setProgress] = useState(0)
  const resetFile = useRef<() => void>(null)
  const { t } = useTranslation()

  useEffect(() => {
    if (props.opened) void mutate()
  }, [gameId, props.opened, mutate])

  const onUpload = async (file: File | null) => {
    if (!file || busy) return
    if (file.size === 0 || file.size > 20 * 1024 * 1024 || !/\.(pdf|docx?|odt)$/i.test(file.name)) {
      showNotification({ color: 'red', message: t('admin.content.games.writeups.examples.invalid_file') })
      resetFile.current?.()
      return
    }

    setBusy(true)
    setProgress(0)
    try {
      await api.admin.adminUploadWriteupExample(
        gameId,
        { file },
        {
          onUploadProgress: (event) => setProgress((event.loaded / (event.total ?? 1)) * 100),
        }
      )
      await mutate()
      showNotification({
        color: 'teal',
        message: t('admin.content.games.writeups.examples.uploaded'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (error) {
      showErrorMsg(error, t)
    } finally {
      setBusy(false)
      setProgress(0)
      resetFile.current?.()
    }
  }

  const onDelete = async (example: WriteupExampleModel) => {
    if (busy) return
    setBusy(true)
    try {
      await api.admin.adminDeleteWriteupExample(gameId, example.id)
      await mutate()
      showNotification({
        color: 'teal',
        message: t('admin.content.games.writeups.examples.deleted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (error) {
      showErrorMsg(error, t)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal {...props} title={t('game.content.writeup.examples')} size="lg">
      <Stack gap="sm">
        <Text size="sm" c="dimmed">
          {t('admin.content.games.writeups.examples.description')}
        </Text>
        <FileButton onChange={onUpload} accept=".pdf,.doc,.docx,.odt" resetRef={resetFile}>
          {(buttonProps) => (
            <Button {...buttonProps} disabled={busy} leftSection={<Icon path={mdiFileUploadOutline} size={1} />}>
              {progress > 0 ? t('common.button.uploading') : t('admin.content.games.writeups.examples.upload')}
            </Button>
          )}
        </FileButton>
        {busy && progress > 0 && <Progress value={progress} />}
        {data?.examples?.length ? (
          <WriteupExampleList examples={data.examples} disabled={busy} onDelete={onDelete} />
        ) : (
          <Text size="sm" c="dimmed">
            {t('admin.content.games.writeups.examples.empty')}
          </Text>
        )}
      </Stack>
    </Modal>
  )
}
