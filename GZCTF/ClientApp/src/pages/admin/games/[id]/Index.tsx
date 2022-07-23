import { FC, useState } from 'react'
import { useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Title } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import Icon from '@mdi/react'
import api, { GameInfoModel } from '../../../../Api'
import AdminPage from '../../../../components/admin/AdminPage'

const GameEdit: FC = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const [game, setGame] = useState<GameInfoModel>()

  useEffect(() => {
    const numId = parseInt(id ?? '-1')

    if (numId < 0) {
      showNotification({
        color: 'red',
        message: `比赛 Id 错误：${id}`,
        icon: <Icon path={mdiClose} size={1} />,
      })
      navigate('/admin/games')
      return
    }

    api.edit
      .editGetGame(numId)
      .then((data) => {
        setGame(data.data)
      })
      .catch((err) => {
        if (err.status === 404) {
          showNotification({
            color: 'red',
            message: `比赛未找到：${id}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
          navigate('/admin/games')
        }

        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
  }, [id])

  return (
    <AdminPage>
      <Title order={1}># {game?.title}</Title>
    </AdminPage>
  )
}

export default GameEdit
