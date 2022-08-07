import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Group, Stack } from '@mantine/core'
import ChallengePanel from '@Components/ChallengePanel'
import TeamRank from '@Components/TeamRank'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import api, { Role } from '@Api/Api'
import ChallengeNoticePanel from '@Components/ChallengeNoticePanel'

const Challenges: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <WithGameTab isLoading={!game} game={game} status={game?.status}>
          <Group spacing="sm" position="apart" align="flex-start" grow noWrap>
            <ChallengePanel />
            <Stack style={{ maxWidth: '20rem' }}>
              <TeamRank />
              <ChallengeNoticePanel />
            </Stack>
          </Group>
        </WithGameTab>
      </WithRole>
    </WithNavBar>
  )
}

export default Challenges
