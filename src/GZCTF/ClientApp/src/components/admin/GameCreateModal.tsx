import { Button, Group, Modal, ModalProps, Stack, TextInput } from '@mantine/core'
import { DatePickerInput, TimeInput } from '@mantine/dates'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { showErrorNotification } from '@Utils/ApiHelper'
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

  const { t } = useTranslation()

  const onCreate = () => {
    if (!title || end < start) {
      showNotification({
        color: 'red',
        title: t('common.error.param_invalid'),
        message: t('admin.notification.games.no_title_time'),
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
          message: t('admin.notification.games.created'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        onAddGame(data.data)
        navigate(`/admin/games/${data.data.id}/info`)
      })
      .catch((err) => {
        showErrorNotification(err, t)
        setDisabled(false)
      })
  }

  return (
    <Modal size="30%" title={t('admin.button.games.new')} {...modalProps}>
      <Stack>
        <TextInput
          label={t('admin.content.games.info.title.label')}
          type="text"
          required
          w="100%"
          value={title}
          onChange={setTitle}
        />

        <Group grow justify="space-between">
          <DatePickerInput
            label={t('admin.content.games.info.start_date')}
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
            label={t('admin.content.games.info.start_time')}
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

        <Group grow justify="space-between">
          <DatePickerInput
            label={t('admin.content.games.info.end_date')}
            minDate={start.toDate()}
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
            label={t('admin.content.games.info.end_time')}
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
        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            {t('admin.button.games.new')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default GameCreateModal
