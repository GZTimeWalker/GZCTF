import { FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button, Modal, ModalProps, Select, Stack, TextInput } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { useTeams } from '@Utils/useUser'
import api, { GameJoinModel } from '@Api'

interface GameJoinModalProps extends ModalProps {
  onSubmitJoin: (info: GameJoinModel) => Promise<void>
}

const GameJoinModal: FC<GameJoinModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { onSubmitJoin, ...modalProps } = props
  const { teams } = useTeams()

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  const [inviteCode, setInviteCode] = useState('')
  const [organization, setOrganization] = useState('')
  const [team, setTeam] = useState('')
  const [disabled, setDisabled] = useState(false)

  return (
    <Modal {...modalProps}>
      <Stack>
        <Select
          required
          label="选择你的参赛队伍"
          description="请选择一个队伍参与比赛，选定后不可更改"
          data={teams?.map((t) => ({ label: t.name!, value: t.id!.toString() })) ?? []}
          disabled={disabled}
          value={team}
          onChange={(e) => setTeam(e ?? '')}
        />
        {game?.inviteCodeRequired && (
          <TextInput
            required
            label="请输入邀请码"
            description="本场比赛开启了邀请参赛，报名参赛需提供邀请码"
            value={inviteCode}
            onChange={(e) => setInviteCode(e.target.value)}
            disabled={disabled}
          />
        )}
        {game?.organizations && game.organizations.length > 0 && (
          <Select
            required
            label="选择你的参赛组织"
            description="本场比赛具有多个参赛组织，请选择你的参赛组织"
            data={game.organizations}
            disabled={disabled}
            value={organization}
            onChange={(e) => setOrganization(e ?? '')}
          />
        )}
        <Button
          disabled={disabled}
          onClick={() => {
            setDisabled(true)

            if (game?.inviteCodeRequired && !inviteCode) {
              showNotification({
                color: 'orange',
                message: '邀请码不能为空',
                icon: <Icon path={mdiClose} size={1} />,
                disallowClose: true,
              })
              return
            }

            if (game?.organizations && game.organizations.length > 0 && !organization) {
              showNotification({
                color: 'orange',
                message: '请选择参赛组织',
                icon: <Icon path={mdiClose} size={1} />,
                disallowClose: true,
              })
              return
            }

            onSubmitJoin({
              teamId: parseInt(team),
              inviteCode: game?.inviteCodeRequired ? inviteCode : undefined,
              organization:
                game?.organizations && game.organizations.length > 0 ? organization : undefined,
            }).finally(() => {
              setInviteCode('')
              setOrganization('')
              setDisabled(false)
              props.onClose()
            })
          }}
        >
          报名参赛
        </Button>
      </Stack>
    </Modal>
  )
}

export default GameJoinModal
