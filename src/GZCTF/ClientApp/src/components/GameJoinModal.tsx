import { Button, Modal, ModalProps, Select, Stack, TextInput } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { OnceSWRConfig } from '@Hooks/useConfig'
import { useGame } from '@Hooks/useGame'
import { useTeams } from '@Hooks/useUser'
import api, { GameJoinModel } from '@Api'

interface GameJoinModalProps extends ModalProps {
  onSubmitJoin: (info: GameJoinModel) => Promise<void>
}

export const GameJoinModal: FC<GameJoinModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { onSubmitJoin, ...modalProps } = props

  const { teams } = useTeams()
  const { game } = useGame(numId)

  const { data: checkInfo } = api.game.useGameGetGameJoinCheckInfo(numId, OnceSWRConfig, props.opened && numId > 0)

  const [inviteCode, setInviteCode] = useState('')
  const [divisionId, setDivisionId] = useState('')
  const [team, setTeam] = useState<string | null>(null)
  const [disabled, setDisabled] = useState(false)

  const { t } = useTranslation()

  useEffect(() => {
    if (!team && teams && teams.length >= 1) {
      setTeam(teams[0].id!.toString())
    }
  }, [team, teams])

  useEffect(() => {
    if (divisionId) return

    if (typeof game?.division === 'number') {
      setDivisionId(game.division.toString())
    } else if (game?.divisions && game.divisions.length >= 1 && !!game.divisions[0].id) {
      setDivisionId(game.divisions[0].id.toString())
    }
  }, [divisionId, game])

  const divisionOptions = useMemo(
    () =>
      (game?.divisions ?? [])
        .filter((d) => d.id && (!checkInfo?.joinableDivisions || checkInfo.joinableDivisions.includes(d.id!)))
        .map((d) => ({
          value: d.id!.toString(),
          label: d.name ?? `Division #${d.id}`,
        })),
    [game?.divisions, checkInfo]
  )

  const gameCheckInfo = useMemo(() => {
    const map = new Map<string, { div: number | null; joinable?: boolean }>()
    checkInfo?.joinedTeams?.forEach((jt) => {
      map.set(jt.id.toString(), { div: jt.division, joinable: checkInfo.joinableDivisions?.includes(jt.division) })
    })
    return map
  }, [checkInfo])

  const selectedDivision = useMemo(
    () => (game?.divisions ?? []).find((d) => d.id?.toString() === divisionId) ?? null,
    [divisionId, game?.divisions]
  )

  const teamsData = useMemo(() => {
    return teams?.map((t) => ({ label: t.name!, value: t.id!.toString() })) ?? []
  }, [teams])

  const joinedTeam = team ? gameCheckInfo.get(team) : null
  const joinedDivision = joinedTeam?.div ? game?.divisions?.find((d) => d.id === joinedTeam?.div) : null
  const hasDivision = divisionOptions.length > 0 || joinedTeam?.div
  const canSelectDivision = !joinedTeam

  const shouldRequireInviteCode = hasDivision
    ? Boolean(selectedDivision?.inviteCodeRequired)
    : Boolean(game?.inviteCodeRequired)

  useEffect(() => {
    if (!shouldRequireInviteCode) {
      setInviteCode('')
    }
  }, [shouldRequireInviteCode])

  const onJoinGame = async () => {
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

    if (shouldRequireInviteCode && !inviteCode) {
      showNotification({
        color: 'orange',
        message: t('game.notification.no_invite_code'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      setDisabled(false)
      return
    }

    if (canSelectDivision && hasDivision && !divisionId) {
      showNotification({
        color: 'orange',
        message: t('game.notification.no_division'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      setDisabled(false)
      return
    }

    try {
      await onSubmitJoin({
        teamId: parseInt(team, 10),
        inviteCode: shouldRequireInviteCode ? inviteCode : undefined,
        divisionId:
          canSelectDivision && hasDivision
            ? parseInt(divisionId, 10)
            : !canSelectDivision && joinedDivision
              ? joinedDivision.id
              : undefined,
      })
    } finally {
      setInviteCode('')
      setDivisionId('')
      setDisabled(false)
      props.onClose()
    }
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <Select
          required
          label={t('game.content.join.team.label')}
          description={t('game.content.join.team.description')}
          data={teamsData}
          disabled={disabled}
          value={team}
          onChange={(e) => e && setTeam(e)}
        />
        {canSelectDivision && hasDivision && (
          <Select
            required
            label={t('game.content.join.division.label')}
            description={t('game.content.join.division.description')}
            readOnly={!canSelectDivision}
            data={divisionOptions}
            disabled={disabled}
            value={divisionId}
            onChange={(e) => setDivisionId(e ?? '')}
          />
        )}
        {!canSelectDivision && joinedDivision && (
          <Select
            required
            label={t('game.content.join.division.label')}
            description={t('game.content.join.division.description')}
            readOnly={true}
            disabled={true}
            data={[
              {
                label: joinedDivision.name ?? `Division #${joinedDivision.id}`,
                value: joinedDivision.id!.toString(),
              },
            ]}
            value={joinedDivision.id!.toString()}
          />
        )}
        {shouldRequireInviteCode && (
          <TextInput
            required
            label={t('game.content.join.invite_code.label')}
            description={t('game.content.join.invite_code.description')}
            value={inviteCode}
            onChange={(e) => setInviteCode(e.target.value)}
            disabled={disabled}
          />
        )}
        <Button disabled={disabled} onClick={onJoinGame}>
          {t('game.button.join')}
        </Button>
      </Stack>
    </Modal>
  )
}
