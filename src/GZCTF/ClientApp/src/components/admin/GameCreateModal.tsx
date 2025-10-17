import { Button, Group, Modal, ModalProps, Stack, TextInput } from '@mantine/core'
import { DateTimePicker } from '@mantine/dates'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { showErrorMsg } from '@Utils/Shared'
import api, { GameInfoModel } from '@Api'

interface GameCreateModalProps extends ModalProps {
  onAddGame: (game: GameInfoModel) => void
}

export const GameCreateModal: FC<GameCreateModalProps> = (props) => {
  const { onAddGame, ...modalProps } = props
  const [disabled, setDisabled] = useState(false)
  const navigate = useNavigate()
  const [title, setTitle] = useInputState('')
  const [start, setStart] = useInputState(dayjs())
  const [end, setEnd] = useInputState(dayjs().add(2, 'h'))

  const { t } = useTranslation()

  const onCreate = async () => {
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

    try {
      const res = await api.edit.editAddGame({
        title,
        start: start.valueOf(),
        end: end.valueOf(),
      })
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.created'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      onAddGame(res.data)
      navigate(`/admin/games/${res.data.id}/info`)
    } catch (e) {
      showErrorMsg(e, t)
      setDisabled(false)
    }
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
        <DateTimePicker
          label={t('admin.content.games.info.start_time')}
          size="sm"
          value={start.toDate()}
          valueFormat="L LT"
          clearable={false}
          onChange={(e) => {
            const newDate = dayjs(e)
            setStart(newDate)
            if (newDate && end < newDate) {
              setEnd(newDate.add(2, 'h'))
            }
          }}
          required
        />
        <DateTimePicker
          label={t('admin.content.games.info.end_time')}
          size="sm"
          minDate={start.toDate()}
          valueFormat="L LT"
          value={end.toDate()}
          clearable={false}
          onChange={(e) => {
            setEnd(dayjs(e))
          }}
          error={end < start}
          required
        />
        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            {t('admin.button.games.new')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
