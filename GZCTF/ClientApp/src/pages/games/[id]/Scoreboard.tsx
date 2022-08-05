import { FC } from 'react'
import { useParams } from 'react-router-dom'
import api from '../../../Api'
import WithGameTab from '../../../components/WithGameTab'
import WithNavBar from '../../../components/WithNavbar'

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
