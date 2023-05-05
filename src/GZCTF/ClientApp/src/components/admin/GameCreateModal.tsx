import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Group, Modal, ModalProps, Stack, TextInput } from '@mantine/core'
import { DatePickerInput, TimeInput } from '@mantine/dates'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import api, { GameInfoModel } from '@Api'

interface GameCreateModalProps extends ModalProps {
  onAddGame: (game: GameInfoModel) => void
}

const GameCreateModal: FC<GameCreateModalProps> = (props) => {
  const { onAddGame, ...modalProps } = props
  const [disabled, setDisabled] = useState(false)
  const navigate = useNavigate()
  const [title, setTitle] = useInputState('')
  const [start, setStart] = useInputState(dayjs())
  const [end, setEnd] = useInputState(dayjs().add(2, 'h'))

  const onCreate = () => {
    if (!title || end < start) {
      showNotification({
        color: 'red',
        title: '输入不合法',
        message: '请输入标题和时间信息',
        icon: <Icon path={mdiClose} size={1} />,
        withCloseButton: false,
      })
      return
    }

    setDisabled(true)
    api.edit
      .editAddGame({
        title,
        start: start.toJSON(),
        end: end.toJSON(),
        wpddl: end.add(3, 'h').toJSON(),
      })
      .then((data) => {
        showNotification({
          color: 'teal',
          message: '比赛创建成功',
          icon: <Icon path={mdiCheck} size={1} />,
          withCloseButton: false,
        })
        onAddGame(data.data)
        navigate(`/admin/games/${data.data.id}/info`)
      })
      .catch((err) => {
        showErrorNotification(err)
        setDisabled(false)
      })
  }

  return (
    <Modal size="30%" title="新建比赛" {...modalProps}>
      <Stack>
        <TextInput
          label="比赛标题"
          type="text"
          required
          placeholder="Title"
          w="100%"
          value={title}
          onChange={setTitle}
        />

        <Group grow position="apart">
          <DatePickerInput
            label="开始日期"
            placeholder="Start Date"
            value={start.toDate()}
            clearable={false}
            onChange={(e) => {
              const newDate = dayjs(e)
                .hour(start.hour())
                .minute(start.minute())
                .second(start.second())
              setStart(newDate)
              if (newDate && end < newDate) {
                setEnd(newDate.add(2, 'h'))
              }
            }}
            required
          />
          <TimeInput
            label="开始时间"
            placeholder="Start Time"
            value={start.format('HH:mm:ss')}
            onChange={(e) => {
              const newTime = e.target.value.split(':')
              const newDate = dayjs(start)
                .hour(Number(newTime[0]))
                .minute(Number(newTime[1]))
                .second(Number(newTime[2]))
              setStart(newDate)
              if (newDate && end < newDate) {
                setEnd(newDate.add(2, 'h'))
              }
            }}
            withSeconds
            required
          />
        </Group>

        <Group grow position="apart">
          <DatePickerInput
            label="结束日期"
            minDate={start.toDate()}
            placeholder="End time"
            value={end.toDate()}
            clearable={false}
            onChange={(e) => {
              const newDate = dayjs(e).hour(end.hour()).minute(end.minute()).second(end.second())
              setEnd(newDate)
            }}
            error={end < start}
            required
          />
          <TimeInput
            label="结束时间"
            placeholder="End time"
            value={end.format('HH:mm:ss')}
            onChange={(e) => {
              const newTime = e.target.value.split(':')
              const newDate = dayjs(end)
                .hour(Number(newTime[0]))
                .minute(Number(newTime[1]))
                .second(Number(newTime[2]))
              setEnd(newDate)
            }}
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
