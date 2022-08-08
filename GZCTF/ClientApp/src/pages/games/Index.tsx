import { FC, useState } from 'react'
import { SimpleGrid, Stack } from '@mantine/core'
import { mdiFlag, mdiPackageVariantClosed, mdiProgressClock } from '@mdi/js'
import { Icon } from '@mdi/react'
import GameCard, { GameColorMap, GameStatus } from '@Components/GameCard'
import IconTabs from '@Components/IconTabs'
import WithNavBar from '@Components/WithNavbar'
import api, { BasicGameInfoModel } from '@Api'

const Games: FC = () => {
  const { data: allGames } = api.game.useGameGamesAll({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [activeTab, setActiveTab] = useState(0)
  const [gameType, setGameType] = useState('ongoing')

  const onChange = (active: number, tabKey: string) => {
    setActiveTab(active)
    setGameType(tabKey)
  }

  const now = new Date()
  const games = new Map<string, BasicGameInfoModel[] | undefined>([
    [GameStatus.Coming, allGames?.filter((game) => new Date(game.start!) > now)],
    [
      GameStatus.OnGoing,
      allGames?.filter((game) => new Date(game.start!) <= now && new Date(game.end!) >= now),
    ],
    [GameStatus.Ended, allGames?.filter((game) => new Date(game.end!) < now)],
  ])

  return (
    <WithNavBar>
      <Stack>
        <IconTabs
          withIcon
          active={activeTab}
          onTabChange={onChange}
          tabs={[
            {
              tabKey: GameStatus.OnGoing,
              label: '进行中',
              icon: <Icon path={mdiFlag} size={1} />,
              color: GameColorMap.get(GameStatus.OnGoing)!,
            },
            {
              tabKey: GameStatus.Coming,
              label: '未开始',
              icon: <Icon path={mdiProgressClock} size={1} />,
              color: GameColorMap.get(GameStatus.Coming)!,
            },
            {
              tabKey: GameStatus.Ended,
              label: '已结束',
              icon: <Icon path={mdiPackageVariantClosed} size={1} />,
              color: GameColorMap.get(GameStatus.Ended)!,
            },
          ]}
        />
        <SimpleGrid
          cols={3}
          spacing="lg"
          breakpoints={[
            { maxWidth: 1200, cols: 2, spacing: 'md' },
            { maxWidth: 800, cols: 1, spacing: 'sm' },
          ]}
        >
          {games.get(gameType)?.map((g) => (
            <GameCard key={g.id} game={g} />
          ))}
        </SimpleGrid>
      </Stack>
    </WithNavBar>
  )
}

export default Games
