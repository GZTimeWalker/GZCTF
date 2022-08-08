import { FC } from 'react'
import { useParams } from 'react-router-dom'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import api, { Role } from '@Api/Api'
import WithGameTab from '@Components/WithGameTab'

const Monitor: FC = () => {
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

        </WithGameTab>
      </WithRole>
    </WithNavBar>
  )
}

export default Monitor
