import { FC } from 'react'
import { Group, Stack } from '@mantine/core'
import GameCard from '@Components/GameCard'
import LogoHeader from '@Components/LogoHeader'
import WithNavBar from '@Components/WithNavbar'
import { usePageTitle } from '@Utils/PageTitle'
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
      <Group
        position="apart"
        align="flex-end"
        sx={(theme) => ({
          width: '100%',
          top: 16,
          paddingBottom: 8,
          position: 'sticky',
          zIndex: 50,
          backgroundColor:
            theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2],

          '::before': {
            content: '""',
            position: 'absolute',
            top: -16,
            right: 0,
            left: 0,
            backgroundColor:
              theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2],
            paddingTop: 16,
          },

          [theme.fn.smallerThan('xs')]: {
            display: 'none',
          },
        })}
      >
        <LogoHeader />
      </Group>
      <Stack>
        {games.map((g) => (
          <GameCard key={g.id} game={g} />
        ))}
      </Stack>
    </WithNavBar>
  )
}

export default Games
