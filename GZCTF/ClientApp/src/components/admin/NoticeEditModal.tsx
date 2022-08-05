import { FC, useEffect, useState } from 'react'
import { Button, Group, Modal, ModalProps, Stack, Text, Textarea, TextInput } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { Notice } from '../../Api'
import { showErrorNotification } from '../../utils/ApiErrorHandler'

interface NoticeEditModalProps extends ModalProps {
  notice?: Notice | null
  mutateNotice: (notice: Notice) => void
}

const NoticeEditModal: FC<NoticeEditModalProps> = (props) => {
  const { notice, mutateNotice, ...modalProps } = props

  const [title, setTitle] = useInputState(notice?.title)
  const [content, setContent] = useInputState(notice?.content)
  const [disabled, setDisabled] = useState(false)

  useEffect(() => {
    setTitle(notice?.title)
    setContent(notice?.content)
  }, [notice])

  const onUpdate = () => {
    if (!title || !content) {
      showNotification({
        color: 'red',
        title: '输入不能为空',
        message: '请输入标题和内容',
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }
    if (title === notice?.title && content === notice?.content) {
      showNotification({
        color: 'orange',
        message: '似乎没有变化哦',
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    if (notice && !disabled) {
      setDisabled(true)
      api.edit
        .editUpdateNotice(notice.id!, {
          title: title,
          content: content,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '通知修改成功',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          mutateNotice(data.data)
          modalProps.onClose()
        })
        .catch(showErrorNotification)
        .finally(() => {
          setDisabled(false)
        })
    } else {
      api.edit
        .editAddNotice({
          title: title,
          content: content,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '通知已添加',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          mutateNotice(data.data)
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
        <TextInput
          label="通知标题"
          type="text"
          placeholder="Title"
          style={{ width: '100%' }}
          value={title}
          onChange={setTitle}
        />
        <Textarea
          label={
            <Group spacing="sm">
              <Text size="sm">通知详情</Text>
              <Text size="xs" color="dimmed">
                支持 markdown 语法
              </Text>
            </Group>
          }
          value={content}
          style={{ width: '100%' }}
          autosize
          minRows={5}
          maxRows={16}
          onChange={setContent}
        />
        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button
            fullWidth
            variant="outline"
            color="orange"
            disabled={disabled}
            onClick={() => {
              setTitle(notice?.title)
              setContent(notice?.content)
            }}
          >
            {notice ? '还原通知' : '清空通知'}
          </Button>
          <Button fullWidth disabled={disabled} onClick={onUpdate}>
            {notice ? '更改通知' : '新建通知'}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default NoticeEditModal
