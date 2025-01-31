import {
  Badge,
  Group,
  Pagination,
  Stack,
  Text,
  UnstyledButton,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { GameCard, GameColorMap } from '@Components/GameCard'
import { WithNavBar } from '@Components/WithNavbar'
import { GanttTimeLine } from '@Components/charts/GanttTimeline'
import { getGameStatus, toLimitTag, useRecentGames } from '@Hooks/useGame'
import { usePageTitle } from '@Hooks/usePageTitle'
import api from '@Api'
import ganttClasses from '@Styles/GanttTimeline.module.css'

const ITEM_PER_PAGE = 10

const Games: FC = () => {
  const { t } = useTranslation()

  const { recentGames } = useRecentGames()
  const [activePage, setPage] = useState(1)

  const { data: games } = api.game.useGameGames(
    { count: ITEM_PER_PAGE, skip: (activePage - 1) * ITEM_PER_PAGE },
    {
      refreshInterval: 5 * 60 * 1000,
    }
  )

  usePageTitle(t('game.title.index'))

  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  const recents =
    recentGames?.map((game) => {
      const { startTime, endTime, status } = getGameStatus(game)
      const color = GameColorMap.get(status) ?? 'gray'
      const colorHex = theme.colors[color][colorScheme === 'dark' ? 9 : 4]

      return {
        id: game.id,
        textTitle: game.title ?? '',
        color: colorHex,
        title: (
          <UnstyledButton w="100%" component={Link} to={`/games/${game.id}`}>
            <Group gap="sm" className={ganttClasses.gameBox}>
              <Text size="sm" className={ganttClasses.title}>
                {game.title}
              </Text>
              <Badge size="sm" color={color}>
                {toLimitTag(t, game.limit)}
              </Badge>
            </Group>
          </UnstyledButton>
        ),
        start: startTime,
        end: endTime,
      }
    }) ?? []

  const pageCount = Math.ceil((games?.total ?? 0) / ITEM_PER_PAGE)

  return (
    <WithNavBar withFooter withHeader stickyHeader>
      <GanttTimeLine items={recents} />
      <Stack pt="md" mih="calc(100vh - 78px)" justify="space-between">
        <Stack>{games && games.data.map((g) => <GameCard key={g.id} game={g} />)}</Stack>
        <Pagination.Root total={pageCount} siblings={3} value={activePage} onChange={setPage} mb="xl">
          <Group gap={5} justify="flex-end">
            <Pagination.First />
            <Pagination.Previous />
            <Pagination.Items />
            <Pagination.Next />
            <Pagination.Last />
          </Group>
        </Pagination.Root>
      </Stack>
    </WithNavBar>
  )
}

export default Games
