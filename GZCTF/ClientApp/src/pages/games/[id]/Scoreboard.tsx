import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Stack } from '@mantine/core'
import TimeLine from '@Components/TimeLine'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import api from '@Api/Api'
import ScoreboardTable from '@Components/ScoreboardTable'

const Scoreboard: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: scoreboard } = api.game.useGameScoreboard(numId, {
    refreshInterval: 0,
  })

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })
  console.log(scoreboard)

  return (
    <WithNavBar width="90%">
      <WithGameTab isLoading={!game} game={game} status={game?.status}>
        <Stack>
          <TimeLine />
          <ScoreboardTable />
        </Stack>
      </WithGameTab>
    </WithNavBar>
  )
}

export default Scoreboard
