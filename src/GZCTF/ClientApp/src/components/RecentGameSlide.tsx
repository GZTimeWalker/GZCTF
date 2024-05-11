import { Badge, Group, Paper, Stack, Title } from '@mantine/core'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { GameColorMap, GameStatus } from '@Components/GameCard'
import { RecentGameProps } from '@Components/RecentGame'
import { getGameStatus } from '@Utils/useGame'
import classes from '@Styles/RecentGameSlide.module.css'

const RecentGameSlide: FC<RecentGameProps> = ({ game, ...others }) => {
  const { title, poster } = game
  const { startTime, endTime, status } = getGameStatus(game)

  const { t } = useTranslation()
  const color = GameColorMap.get(status)

  const duration =
    status === GameStatus.OnGoing ? endTime.diff(dayjs(), 'h') : endTime.diff(startTime, 'h')

  return (
    <Paper
      {...others}
      component={Link}
      to={`/games/${game.id}`}
      shadow="md"
      p="md"
      radius="md"
      style={{ backgroundImage: `url(${poster})` }}
      className={classes.card}
    >
      <Stack h="100%" gap={2} justify="space-between">
        <Group gap={4}>
          <Badge size="sm" variant="filled">
            {game.limit === 0
              ? t('game.tag.multiplayer')
              : game.limit === 1
                ? t('game.tag.individual')
                : t('game.tag.limited', { count: game.limit })}
          </Badge>
          <Badge size="sm" variant="filled" color={color}>
            {status === GameStatus.OnGoing
              ? t('game.content.remaining_duration', { hours: duration })
              : t('game.content.total_duration', { hours: duration })}
          </Badge>
        </Group>
        <Title pb={10} order={3} lineClamp={1} className={classes.title}>
          {title}
        </Title>
      </Stack>
    </Paper>
  )
}

export default RecentGameSlide
