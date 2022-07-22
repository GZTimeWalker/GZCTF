import { FC } from 'react'
import { SimpleGrid, Stack, Tabs, useMantineTheme } from '@mantine/core'
import { mdiFlag, mdiPackageVariantClosed, mdiProgressClock } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../Api'
import GameCard from '../../components/GameCard'
import LogoHeader from '../../components/LogoHeader'
import WithNavBar from '../../components/WithNavbar'

const Games: FC = () => {
  const { data: games } = api.game.useGameGamesAll({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const now = new Date()

  const coming = games?.filter((game) => new Date(game.start!) > now)
  const ongoing = games?.filter(
    (game) => new Date(game.start!) <= now && new Date(game.end!) >= now
  )
  const ended = games?.filter((game) => new Date(game.end!) < now)

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <Tabs tabPadding="xl" grow styles={(theme) => ({
          root: { marginTop: -64 },
          tabsListWrapper: { marginLeft: 210 }
        })}>
          <Tabs.Tab label="进行中" icon={<Icon path={mdiFlag} size={1} />}>
            <SimpleGrid
              cols={3}
              spacing="lg"
              breakpoints={[
                { maxWidth: 1200, cols: 2, spacing: 'md' },
                { maxWidth: 800, cols: 1, spacing: 'sm' },
              ]}
            >
              {ongoing && ongoing.map((g) => <GameCard {...g} />)}
            </SimpleGrid>
          </Tabs.Tab>
          <Tabs.Tab
            label="未开始"
            icon={<Icon path={mdiProgressClock} size={1} />}
            color="yellow"
          ></Tabs.Tab>
          <Tabs.Tab
            label="已结束"
            icon={<Icon path={mdiPackageVariantClosed} size={1} />}
            color="red"
          ></Tabs.Tab>
        </Tabs>
      </Stack>
    </WithNavBar>
  )
}

export default Games
