import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Stack } from '@mantine/core'
import ScoreboardTable from '@Components/ScoreboardTable'
import TimeLine from '@Components/TimeLine'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import api from '@Api/Api'

const Scoreboard: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

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
