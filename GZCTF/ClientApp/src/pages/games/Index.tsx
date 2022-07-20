import { FC } from 'react'
import { SimpleGrid, Stack, Tabs, useMantineTheme } from '@mantine/core'
import LogoHeader from '../../components/LogoHeader'
import WithNavBar from '../../components/WithNavbar'
import GameCard from "../../components/GameCard"
import Icon from "@mdi/react"
import { mdiFlag, mdiInbox, mdiPackageVariantClosed, mdiProgressClock } from "@mdi/js"

const Games: FC = () => {
  const theme = useMantineTheme();
  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <Tabs tabPadding="md">
          <Tabs.Tab label="进行中" icon={<Icon path={mdiFlag} size={1} />}>
            <SimpleGrid
              cols={3}
              spacing="lg"
              breakpoints={[
                { maxWidth: 1200, cols: 2, spacing: 'md' },
                { maxWidth: 800, cols: 1, spacing: 'sm' },
              ]}
            >
              <GameCard state={0} />
              <GameCard state={1} />
              <GameCard state={2} />
            </SimpleGrid>
          </Tabs.Tab>
          <Tabs.Tab label="未开始" icon={<Icon path={mdiProgressClock} size={1} />} color="yellow">
          </Tabs.Tab>
          <Tabs.Tab label="已结束" icon={<Icon path={mdiPackageVariantClosed} size={1} />}
            color="red">
          </Tabs.Tab>
        </Tabs>
      </Stack>
    </WithNavBar>
  )
}

export default Games
