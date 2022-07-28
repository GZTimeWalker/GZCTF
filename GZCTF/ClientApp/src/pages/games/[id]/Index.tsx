import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Stack, Text, Title } from '@mantine/core'
import api, { GameDetailModel } from '../../../Api'
import LogoHeader from '../../../components/LogoHeader'
import WithNavBar from '../../../components/WithNavbar'
import { showErrorNotification } from '../../../utils/ApiErrorHandler'

const GameDetail: FC = () => {
  const { id } = useParams()

  const [game, setGame] = useState<GameDetailModel>()
  const navigate = useNavigate()

  useEffect(() => {
    api.game
      .gameGames(parseInt(id!))
      .then((data) => {
        setGame(data.data)
        console.log(data.data)
      })
      .catch((err) => {
        showErrorNotification(err)

        if (err.status === 404) {
          navigate('/games')
        }
      })
  }, [id])

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
