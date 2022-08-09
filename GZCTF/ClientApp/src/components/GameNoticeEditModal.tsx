import { FC, useEffect, useState } from 'react'
import { Button, Group, Modal, ModalProps, Stack, Text, Textarea, TextInput } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import api, { GameNotice } from '../Api'
import { useParams } from 'react-router-dom'

interface GameNoticeEditModalProps extends ModalProps {
  gameNotice?: GameNotice | null
  mutateGameNotice: (gameNotice: GameNotice) => void
}

const GameNoticeEditModal: FC<GameNoticeEditModalProps> = (props) => {
  const { id } = useParams();
  const numId = parseInt(id ?? '-1') // gameId
  const { gameNotice, mutateGameNotice, ...modalProps } = props

  // const [content, setContent] = useInputState(gameNotice?.content )
  const [content, setContent] = useState<string | undefined>(gameNotice?.content)
  const [disabled, setDisabled] = useState(false)
  // to be added
  // 通知选项

  useEffect(() => {
    setContent(gameNotice?.content)
  }, [gameNotice])

  const onConfirm = () => {
    if (!content) {
      showNotification({
        color: 'red',
        title: '输入不能为空',
        message: '请输入内容',
        icon: <Icon path={mdiClose} size={1} />
      })
      return
    }
    if (content === gameNotice?.content) {
      showNotification({
        color: 'orange',
        message: '似乎没有变化哦',
        icon: <Icon path={mdiClose} size={1} />,
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
            disallowClose: true,
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
            disallowClose: true,
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
          onChange={(e) => setContent(e.currentTarget.value) }
        />
        <Group grow style={{margin:'auto', width:'100%'} }>
          <Button
            fullWidth
            disabled={disabled}
            onClick={onConfirm }
            >
            {"确认" }
          </Button>
        </Group>
      </Stack>
    </Modal>
    )
}

export default GameNoticeEditModal
