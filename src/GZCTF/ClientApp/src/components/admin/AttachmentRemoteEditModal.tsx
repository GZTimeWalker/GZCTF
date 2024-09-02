import { Button, Modal, ModalProps, Stack, Text, Textarea } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useEditChallenge } from '@Utils/useEdit'
import api, { FileType, FlagCreateModel } from '@Api'

const AttachmentRemoteEditModal: FC<ModalProps> = (props) => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const [disabled, setDisabled] = useState(false)

  const { mutate } = useEditChallenge(numId, numCId)

  const [text, setText] = useState('')
  const [flags, setFlags] = useState<FlagCreateModel[]>([])

  const { t } = useTranslation()

  useEffect(() => {
    const list: FlagCreateModel[] = []
    text.split('\n').forEach((line) => {
      let part = line.split(' ')
      part = part.length === 1 ? line.split('\t') : part

      if (part.length !== 2) return

      list.push({
        flag: part[0],
        attachmentType: FileType.Remote,
        remoteUrl: part[1],
      })
    })
    setFlags(list)
  }, [text])

  const onUpload = () => {
    if (flags.length <= 0) return
    api.edit
      .editAddFlags(numId, numCId, flags)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.attachment.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        setText('')
        mutate()
        props.onClose()
      })
      .catch((err) => showErrorNotification(err, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <Modal {...props}>
      <Stack>
        <Text>
          {t('admin.content.games.challenges.attachment.instruction.remote.content')}
          <br />
          <Text fw="bold" span>
            {t('admin.content.games.challenges.attachment.instruction.remote.format')}
          </Text>
          <br />
          <Text fw="bold" c="orange" span>
            {t('admin.content.games.challenges.attachment.instruction.amount_double')}
          </Text>
          <br />
        </Text>
        <Textarea
          ff="monospace"
          placeholder={
            'flag{hello_world} http://example.com/1.zip\nflag{he11o_world} http://example.com/2.zip'
          }
          minRows={8}
          maxRows={12}
          value={text}
          onChange={(e) => setText(e.target.value)}
          required
        />
        <Button fullWidth disabled={disabled} onClick={onUpload}>
          {t('admin.button.games.challenges.attachment.batch_add')}
        </Button>
      </Stack>
    </Modal>
  )
}

export default AttachmentRemoteEditModal
