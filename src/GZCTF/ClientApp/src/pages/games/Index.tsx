import { Badge, Group, Stack, Text } from '@mantine/core'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { GameCard, GameColorMap } from '@Components/GameCard'
import { WithNavBar } from '@Components/WithNavbar'
import { GanttTimeLine } from '@Components/charts/GanttTimeline'
import { OnceSWRConfig } from '@Hooks/useConfig'
import { getGameStatus, toLimitTag, useRecentGames } from '@Hooks/useGame'
import { usePageTitle } from '@Hooks/usePageTitle'
import api from '@Api'
import ganttClasses from '@Styles/GanttTimeline.module.css'

const Games: FC = () => {
  const { t } = useTranslation()

  const { recentGames } = useRecentGames()

  const { data: games } = api.game.useGameGames({ count: 50, skip: 0 }, OnceSWRConfig)

  usePageTitle(t('game.title.index'))

  const navigate = useNavigate()

  const recents =
    recentGames?.map((game) => {
      const { startTime, endTime, status } = getGameStatus(game)
      const color = GameColorMap.get(status)
      return {
        id: game.id,
        textTitle: game.title ?? '',
        title: (
          <Group gap="sm" className={ganttClasses.gameBox} onClick={() => navigate(`/games/${game.id}`)}>
            <Text size="sm" className={ganttClasses.title}>
              {game.title}
            </Text>
            <Badge size="sm" color={color}>
              {toLimitTag(t, game.limit)}
            </Badge>
          </Group>
        ),
        start: startTime,
        end: endTime,
      }
    }) ?? []

  return (
    <WithNavBar withHeader stickyHeader>
      <Stack>
        <GanttTimeLine items={recents} />
        {games && games.data.map((g) => <GameCard key={g.id} game={g} />)}
      </Stack>
    </WithNavBar>
  )
}

export default Games
