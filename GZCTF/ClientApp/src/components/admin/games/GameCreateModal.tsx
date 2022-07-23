import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Group, Modal, ModalProps, Stack, TextInput } from '@mantine/core'
import { DatePicker, TimeInput } from '@mantine/dates'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../../Api'

const GameCreateModal: FC<ModalProps> = (props) => {
  const [disabled, setDisabled] = useState(false)
  const navigate = useNavigate()
  const [title, setTitle] = useInputState('')
  const [start, setStart] = useInputState(new Date())
  const [end, setEnd] = useInputState(dayjs(new Date()).add(2, 'hours').toDate())

  const onCreate = () => {
    if (!title || end < start) {
      showNotification({
        color: 'red',
        title: '输入不合法',
        message: '请输入标题和时间信息',
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)
    api.edit
      .editAddGame({
        title,
        start: start.toJSON(),
        end: end.toJSON(),
      })
      .then((data) => {
        showNotification({
          color: 'teal',
          message: '比赛创建成功',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        navigate(`/admin/games/${data.data.id}`)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
        setDisabled(false)
      })
  }

  return (
    <Modal {...props}>
      <Stack>
        <TextInput
          label="比赛标题"
          type="text"
          required
          placeholder="Title"
          style={{ width: '100%' }}
          value={title}
          onChange={setTitle}
        />

        <Group grow position="apart">
          <DatePicker
            label="开始日期"
            placeholder="Start Date"
            value={start}
            clearable={false}
            onChange={(e) => setStart(e)}
            required
          />
          <TimeInput
            label="开始时间"
            placeholder="Start Time"
            value={start}
            onChange={(e) => setStart(e)}
            withSeconds
            required
          />
        </Group>

        <Group grow position="apart">
          <DatePicker
            label="结束日期"
            minDate={start}
            placeholder="End time"
            value={end}
            clearable={false}
            onChange={(e) => setEnd(e)}
            error={end < start}
            required
          />
          <TimeInput
            label="结束时间"
            placeholder="End time"
            value={end}
            onChange={(e) => setEnd(e)}
            error={end < start}
            withSeconds
            required
          />
        </Group>
        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            创建比赛
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default GameCreateModal
