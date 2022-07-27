import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Stack, Text, Title } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { GameDetailModel } from '../../../Api'
import LogoHeader from '../../../components/LogoHeader'
import WithNavBar from '../../../components/WithNavbar'

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
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })

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
