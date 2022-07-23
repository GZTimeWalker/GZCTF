import { FC, useState } from 'react'
import { useEffect } from 'react'
import { ActionIcon, Button, Group, SimpleGrid } from '@mantine/core'
import { mdiArrowLeftBold, mdiArrowRightBold, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { GameInfoModel } from '../../../Api'
import GameCard from '../../../components/GameCard'
import AdminPage from '../../../components/admin/AdminPage'
import GameCreateModal from '../../../components/admin/GameCreateModal'
import { useNavigate } from 'react-router-dom'

const ITEM_COUNT_PER_PAGE = 30

const Games: FC = () => {
  const [page, setPage] = useState(1)
  const [createOpened, setCreateOpened] = useState(false)
  const [games, setGames] = useState<GameInfoModel[]>([])
  const navigate = useNavigate()

  useEffect(() => {
    api.edit
      .editGetGames({
        count: ITEM_COUNT_PER_PAGE,
        skip: (page - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((res) => {
        res.data.sort((a, b) => new Date(b.end) < new Date(a.end) ? -1 : 1)
        setGames(res.data)
      })
  }, [page])

  return (
    <AdminPage
      headProps={{ position: 'apart' }}
      head={
        <>
          <Button leftIcon={<Icon path={mdiPlus} size={1} />} onClick={() => setCreateOpened(true)}>
            新建比赛
          </Button>
          <Group position="right">
            <ActionIcon
              size="lg"
              variant="hover"
              disabled={page <= 1}
              onClick={() => setPage(page - 1)}
            >
              <Icon path={mdiArrowLeftBold} size={1} />
            </ActionIcon>
            <ActionIcon
              size="lg"
              variant="hover"
              disabled={games && games.length < ITEM_COUNT_PER_PAGE}
              onClick={() => setPage(page + 1)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <SimpleGrid
        cols={3}
        spacing="lg"
        breakpoints={[
          { maxWidth: 1200, cols: 2, spacing: 'md' },
          { maxWidth: 800, cols: 1, spacing: 'sm' },
        ]}
      >
        {games.map((g) => (
          <GameCard game={g} onClick={() => {
            navigate(`/admin/games/${g.id}`)
          }}/>
        ))}
      </SimpleGrid>
      <GameCreateModal
        title="新建比赛"
        centered
        size="30%"
        opened={createOpened}
        onClose={() => setCreateOpened(false)}
      />
    </AdminPage>
  )
}

export default Games
