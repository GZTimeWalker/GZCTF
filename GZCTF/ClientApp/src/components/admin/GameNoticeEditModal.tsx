import { FC, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button, Group, Modal, ModalProps, Stack, Text, Textarea } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import api, { GameNotice } from '@Api'

interface GameNoticeEditModalProps extends ModalProps {
  gameNotice?: GameNotice | null
  mutateGameNotice: (gameNotice: GameNotice) => void
}

const GameNoticeEditModal: FC<GameNoticeEditModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { gameNotice, mutateGameNotice, ...modalProps } = props

  const [content, setContent] = useState<string | undefined>(gameNotice?.content)
  const [disabled, setDisabled] = useState(false)

  useEffect(() => {
    setContent(gameNotice?.content)
  }, [gameNotice])

  const onConfirm = () => {
    if (!content) {
      showNotification({
        color: 'red',
        title: '输入不能为空',
        message: '请输入内容',
        icon: <Icon path={mdiClose} size={1} />,
        withCloseButton: false,
      })
      return
    }
    if (content === gameNotice?.content) {
      showNotification({
        color: 'orange',
        message: '似乎没有变化哦',
        icon: <Icon path={mdiClose} size={1} />,
        withCloseButton: false,
      })
      return
    }

    if (gameNotice && !disabled) {
      setDisabled(true)
      api.edit
        .editUpdateGameNotice(numId, gameNotice.id, {
          content: content,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '通知修改成功',
            icon: <Icon path={mdiCheck} size={1} />,
            withCloseButton: false,
          })
          mutateGameNotice(data.data)
          modalProps.onClose()
        })
        .catch(showErrorNotification)
        .finally(() => {
          setDisabled(false)
        })
    } else {
      // add notice
      api.edit
        .editAddGameNotice(numId, {
          content: content,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '通知已添加',
            icon: <Icon path={mdiCheck} size={1} />,
            withCloseButton: false,
          })
          mutateGameNotice(data.data)
          setDisabled(false)
          modalProps.onClose()
        })
        .catch(showErrorNotification)
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <Textarea
          label={
            <Group spacing="sm">
              <Text size="sm">通知详情 </Text>
            </Group>
          }
          value={content}
          style={{ width: '100%' }}
          autosize
          minRows={5}
          maxRows={16}
          onChange={(e) => setContent(e.currentTarget.value)}
        />
        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth disabled={disabled} onClick={onConfirm}>
            {'确认'}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default GameNoticeEditModal
