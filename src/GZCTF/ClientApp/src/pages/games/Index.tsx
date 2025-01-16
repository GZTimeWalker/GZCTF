import { Center, Pagination, Stack } from '@mantine/core'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { GameCard } from '@Components/GameCard'
import { WithNavBar } from '@Components/WithNavbar'
import { OnceSWRConfig } from '@Hooks/useConfig'
import { usePageTitle } from '@Hooks/usePageTitle'
import api from '@Api'


const Games: FC = () => {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)

  const { data: allGamesResponse, mutate } = api.game.useGameGamesAll({
    page: page
  }, OnceSWRConfig)

  useEffect(() => {
    mutate()
  }, [page, mutate])

  const allGames = allGamesResponse?.data
  allGames?.sort((a, b) => new Date(a.end!).getTime() - new Date(b.end!).getTime())

  const now = new Date()
  const games = [
    ...(allGames?.filter((g) => now < new Date(g.end ?? '')) ?? []),
    ...(allGames?.filter((g) => now >= new Date(g.end ?? '')).reverse() ?? []),
  ]

  usePageTitle(t('game.title.index'))

  return (
    <WithNavBar minWidth={0} withHeader stickyHeader>
      <Stack>
        <Stack mih='calc(100vh - 140px)'>
          {games.map((g) => (
            <GameCard key={g.id} game={g} />
          ))}
        </Stack>
        <footer style={{ marginBottom: '1rem' }}>
          <Center>
            <Pagination total={allGamesResponse?.totalPage ?? 0} value={page} onChange={setPage} />
          </Center>
        </footer>
      </Stack>
    </WithNavBar>
  )
}

export default Games
