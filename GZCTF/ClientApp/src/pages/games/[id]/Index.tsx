import { FC, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Stack, Text, Title } from '@mantine/core'
import api from '../../../Api'
import LogoHeader from '../../../components/LogoHeader'
import WithNavBar from '../../../components/WithNavbar'
import { showErrorNotification } from '../../../utils/ApiErrorHandler'

const GameDetail: FC = () => {
  const { id } = useParams()
  const navigate = useNavigate()

  const { data: game, error } = api.game.useGameGames(parseInt(id!))

  useEffect(() => {
    if (error) {
      showErrorNotification(error)
      navigate('/games')
    }
  }, [error])

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <Title>{game?.title}</Title>
        <Text>Game No.{id}</Text>
      </Stack>
    </WithNavBar>
  )
}

export default GameDetail
