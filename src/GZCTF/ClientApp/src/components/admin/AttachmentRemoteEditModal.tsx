import { FC, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button, Modal, Text, Stack, Textarea, useMantineTheme, ModalProps } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useEditChallenge } from '@Utils/useEdit'
import api, { FileType, FlagCreateModel } from '@Api'

const AttachmentRemoteEditModal: FC<ModalProps> = (props) => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const [disabled, setDisabled] = useState(false)

  const { mutate } = useEditChallenge(numId, numCId)

  const [text, setText] = useState('')
  const [flags, setFlags] = useState<FlagCreateModel[]>([])

  const theme = useMantineTheme()

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
    if (flags.length > 0) {
      api.edit
        .editAddFlags(numId, numCId, flags)
        .then(() => {
          showNotification({
            color: 'teal',
            message: '附件已更新',
            icon: <Icon path={mdiCheck} size={1} />,
          })
          setText('')
          mutate()
          props.onClose()
        })
        .catch((err) => showErrorNotification(err))
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  return (
    <Modal {...props}>
      <Stack>
        <Text>
          批量设置远程附件及对应 flag，每行一组。
          <br />
          <Text fw="bold" span>
            请将 flag 字符串与 url 以空格隔开。
          </Text>
          <br />
          <Text fw="bold" c="orange" span>
            建议上传预期参赛队伍数量的两倍的动态附件。
          </Text>
          <br />
        </Text>
        <Textarea
          styles={{
            input: {
              fontFamily: theme.fontFamilyMonospace,
            },
          }}
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
          批量添加
        </Button>
      </Stack>
    </Modal>
  )
}

export default AttachmentRemoteEditModal
