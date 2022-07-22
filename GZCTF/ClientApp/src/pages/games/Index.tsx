import { FC, useState } from 'react'
import { SimpleGrid, Stack } from '@mantine/core'
import { mdiFlag, mdiPackageVariantClosed, mdiProgressClock } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { BasicGameInfoModel } from '../../Api'
import GameCard from '../../components/GameCard'
import IconTabs from '../../components/IconTabs'
import WithNavBar from '../../components/WithNavbar'

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
    ['coming', allGames?.filter((game) => new Date(game.start!) > now)],
    [
      'ongoing',
      allGames?.filter((game) => new Date(game.start!) <= now && new Date(game.end!) >= now),
    ],
    ['ended', allGames?.filter((game) => new Date(game.end!) < now)],
  ])

  return (
    <WithNavBar>
      <Stack>
        <IconTabs
          active={activeTab}
          onTabChange={onChange}
          tabs={[
            {
              tabKey: 'ongoing',
              label: '进行中',
              icon: <Icon path={mdiFlag} size={1} />,
              color: 'brand',
            },
            {
              tabKey: 'coming',
              label: '未开始',
              icon: <Icon path={mdiProgressClock} size={1} />,
              color: 'yellow',
            },
            {
              tabKey: 'ended',
              label: '已结束',
              icon: <Icon path={mdiPackageVariantClosed} size={1} />,
              color: 'red',
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
            <GameCard {...g} />
          ))}
        </SimpleGrid>
      </Stack>
    </WithNavBar>
  )
}

export default Games
