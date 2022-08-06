import api from '@Api/Api'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import { FC } from 'react'
import { useParams } from 'react-router-dom'

const Scoreboard: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data } = api.game.useGameScoreboard(numId, {
    refreshInterval: 0,
  })

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })
  console.log(data)

  return (
    <WithNavBar width="90%">
      <WithGameTab isLoading={!game} game={game}></WithGameTab>
    </WithNavBar>
  )
}

export default Scoreboard
