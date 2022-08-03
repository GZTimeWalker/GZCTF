import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Stack, Text } from '@mantine/core'
import LogoHeader from '../../../components/LogoHeader'
import WithNavBar from '../../../components/WithNavbar'
import api from '../../../Api'

const Scoreboard: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data } = api.game.useGameScoreboard(numId, {
    refreshInterval: 0,
  })

  console.log(data)

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <Text>Scoreboard of No.{id}</Text>
        <Text>See console for Scoreboard</Text>
      </Stack>
    </WithNavBar>
  )
}

export default Scoreboard
