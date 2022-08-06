import api from '@Api/Api'
import ChallengePanel from '@Components/ChallengePanel'
import TeamRank from '@Components/TeamRank'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Group, Stack } from '@mantine/core'

const Challenges: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  return (
    <WithNavBar width="90%">
      <WithGameTab isLoading={!game} game={game}>
        <Group position="apart" align="flex-start" grow noWrap>
          <ChallengePanel />

          <Stack style={{ maxWidth: '20rem' }}>
            <TeamRank />
          </Stack>
        </Group>
      </WithGameTab>
    </WithNavBar>
  )
}

export default Challenges
