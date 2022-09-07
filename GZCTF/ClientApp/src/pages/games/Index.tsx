import { FC } from 'react'
import { Stack } from '@mantine/core'
import GameCard from '@Components/GameCard'
import StickyHeader from '@Components/StickyHeader'
import WithNavBar from '@Components/WithNavbar'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

const Games: FC = () => {
  const { data: allGames } = api.game.useGameGamesAll({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  allGames?.sort((a, b) => new Date(a.end!).getTime() - new Date(b.end!).getTime())

  const now = new Date()
  const games = [
    ...(allGames?.filter((g) => now < new Date(g.end ?? '')) ?? []),
    ...(allGames?.filter((g) => now >= new Date(g.end ?? '')).reverse() ?? []),
  ]

  usePageTitle('赛事')

  return (
    <WithNavBar>
      <StickyHeader />
      <Stack>
        {games.map((g) => (
          <GameCard key={g.id} game={g} />
        ))}
      </Stack>
    </WithNavBar>
  )
}

export default Games
