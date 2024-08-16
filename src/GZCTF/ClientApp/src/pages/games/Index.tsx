import { Stack } from '@mantine/core'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import GameCard from '@Components/GameCard'
import WithNavBar from '@Components/WithNavbar'
import { OnceSWRConfig } from '@Utils/useConfig'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

const Games: FC = () => {
  const { t } = useTranslation()

  const { data: allGames } = api.game.useGameGamesAll(OnceSWRConfig)

  allGames?.sort((a, b) => new Date(a.end!).getTime() - new Date(b.end!).getTime())

  const now = new Date()
  const games = [
    ...(allGames?.filter((g) => now < new Date(g.end ?? '')) ?? []),
    ...(allGames?.filter((g) => now >= new Date(g.end ?? '')).reverse() ?? []),
  ]

  usePageTitle(t('game.title.index'))

  return (
    <WithNavBar withHeader stickyHeader>
      <Stack>
        {games.map((g) => (
          <GameCard key={g.id} game={g} />
        ))}
      </Stack>
    </WithNavBar>
  )
}

export default Games
