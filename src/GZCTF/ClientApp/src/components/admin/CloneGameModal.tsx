import { Button, Group, Modal, ModalProps, Stack, Switch, Text, TextInput } from '@mantine/core'
import { DateTimePicker } from '@mantine/dates'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiContentDuplicate } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { showErrorMsg } from '@Utils/Shared'
import { GameInfoModel } from '@Api'

interface CloneGameModalProps extends ModalProps {
  game: GameInfoModel | null
}

export const CloneGameModal: FC<CloneGameModalProps> = ({ game, ...props }) => {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [title, setTitle] = useState('')
  const [start, setStart] = useState<Date | null>(null)
  const [end, setEnd] = useState<Date | null>(null)
  const [includeChallenges, setIncludeChallenges] = useState(true)
  const [loading, setLoading] = useState(false)

  const canSubmit = title.trim().length >= 3 && start && end && end > start

  const onClose = () => {
    setTitle('')
    setStart(null)
    setEnd(null)
    setIncludeChallenges(true)
    props.onClose()
  }

  const onClone = async () => {
    if (!game?.id || !canSubmit) return
    setLoading(true)
    try {
      const resp = await fetch(`/api/edit/games/${game.id}/clone`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: title.trim(),
          startTimeUtc: start!.toISOString(),
          endTimeUtc: end!.toISOString(),
          includeChallenges,
        }),
      })
      if (!resp.ok) {
        const err = await resp.json().catch(() => ({ title: 'Clone failed' }))
        throw new Error(err.title ?? 'Clone failed')
      }
      const newId: number = await resp.json()
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.cloned', `Game cloned successfully`),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      onClose()
      navigate(`/admin/games/${newId}/info`)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Modal
      {...props}
      onClose={onClose}
      title={
        <Group gap="xs">
          <Icon path={mdiContentDuplicate} size={0.9} />
          <Text fw="bold">{t('admin.button.games.clone', 'Clone Game')}: {game?.title}</Text>
        </Group>
      }
    >
      <Stack gap="sm">
        <Text size="sm" c="dimmed">
          {t('admin.content.games.clone_hint', 'Creates a new hidden game with the same settings. Attachments are not copied.')}
        </Text>
        <TextInput
          label={t('common.label.title')}
          placeholder={game ? `Copy of ${game.title}` : ''}
          value={title}
          onChange={(e) => setTitle(e.currentTarget.value)}
          error={title.length > 0 && title.trim().length < 3 ? t('admin.error.games.title_too_short', 'At least 3 characters') : undefined}
        />
        <DateTimePicker
          label={t('admin.content.games.info.start_time')}
          value={start}
          onChange={(e) => setStart(e ? new Date(e) : null)}
          clearable
        />
        <DateTimePicker
          label={t('admin.content.games.info.end_time')}
          value={end}
          minDate={start ?? undefined}
          onChange={(e) => setEnd(e ? new Date(e) : null)}
          clearable
        />
        <Switch
          label={t('admin.label.games.clone_challenges', 'Clone challenges & flags')}
          checked={includeChallenges}
          onChange={(e) => setIncludeChallenges(e.currentTarget.checked)}
        />
        <Button
          fullWidth
          leftSection={<Icon path={mdiContentDuplicate} size={0.9} />}
          loading={loading}
          disabled={!canSubmit}
          onClick={onClone}
        >
          {t('admin.button.games.clone', 'Clone Game')}
        </Button>
      </Stack>
    </Modal>
  )
}
