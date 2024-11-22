import { Button, Modal, ModalProps, Select, Stack, TextInput } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { useGame } from '@Hooks/useGame'
import { useTeams } from '@Hooks/useUser'
import { GameJoinModel } from '@Api'

interface GameJoinModalProps extends ModalProps {
  onSubmitJoin: (info: GameJoinModel) => Promise<void>
}

export const GameJoinModal: FC<GameJoinModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { onSubmitJoin, ...modalProps } = props
  const { teams } = useTeams()
  const { game } = useGame(numId)

  const [inviteCode, setInviteCode] = useState('')
  const [division, setDivision] = useState('')
  const [team, setTeam] = useState('')
  const [disabled, setDisabled] = useState(false)

  const { t } = useTranslation()

  useEffect(() => {
    if (teams && teams.length === 1) {
      setTeam(teams[0].id!.toString())
    }
  }, [teams])

  const onJoinGame = () => {
    setDisabled(true)

    if (!team) {
      showNotification({
        color: 'orange',
        message: t('game.notification.no_team'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      setDisabled(false)
      return
    }

    if (game?.inviteCodeRequired && !inviteCode) {
      showNotification({
        color: 'orange',
        message: t('game.notification.no_invite_code'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      setDisabled(false)
      return
    }

    if (game?.divisions && game.divisions.length > 0 && !division) {
      showNotification({
        color: 'orange',
        message: t('game.notification.no_division'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      setDisabled(false)
      return
    }

    onSubmitJoin({
      teamId: parseInt(team),
      inviteCode: game?.inviteCodeRequired ? inviteCode : undefined,
      division: game?.divisions && game.divisions.length > 0 ? division : undefined,
    }).finally(() => {
      setInviteCode('')
      setDivision('')
      setDisabled(false)
      props.onClose()
    })
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <Select
          required
          label={t('game.content.join.team.label')}
          description={t('game.content.join.team.description')}
          data={teams?.map((t) => ({ label: t.name!, value: t.id!.toString() })) ?? []}
          disabled={disabled}
          value={team}
          onChange={(e) => setTeam(e ?? '')}
        />
        {game?.inviteCodeRequired && (
          <TextInput
            required
            label={t('game.content.join.invite_code.label')}
            description={t('game.content.join.invite_code.description')}
            value={inviteCode}
            onChange={(e) => setInviteCode(e.target.value)}
            disabled={disabled}
          />
        )}
        {game?.divisions && game.divisions.length > 0 && (
          <Select
            required
            label={t('game.content.join.division.label')}
            description={t('game.content.join.division.description')}
            data={game.divisions}
            disabled={disabled}
            value={division}
            onChange={(e) => setDivision(e ?? '')}
          />
        )}
        <Button disabled={disabled} onClick={onJoinGame}>
          {t('game.button.join')}
        </Button>
      </Stack>
    </Modal>
  )
}
