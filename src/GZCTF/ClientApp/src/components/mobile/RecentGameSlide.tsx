import { Badge, Group, Paper, Stack, Title } from '@mantine/core'
import dayjs from 'dayjs'
import { FC, MouseEvent as ReactMouseEvent, TouchEvent as ReactTouchEvent, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { GameColorMap, GameStatus } from '@Components/GameCard'
import { RecentGameProps } from '@Components/RecentGame'
import { useForeground } from '@Hooks/useForeground'
import { getGameStatus } from '@Hooks/useGame'
import classes from '@Styles/RecentGameSlide.module.css'
import { storeGameTransitionState } from '@Utils/gameTransition'

export const RecentGameSlide: FC<RecentGameProps> = ({ game, ...others }) => {
  const { title, poster } = game
  const { startTime, endTime, status } = getGameStatus(game)

  const { t } = useTranslation()
  const color = GameColorMap.get(status)

  const duration = status === GameStatus.OnGoing ? endTime.diff(dayjs(), 'h') : endTime.diff(startTime, 'h')

  const titleColor = useForeground(poster)
  const cardRef = useRef<HTMLAnchorElement>(null)

  const captureTransition = () => {
    if (!cardRef.current) {
      return
    }
    const rect = cardRef.current.getBoundingClientRect()
    storeGameTransitionState({
      id: game.id,
      top: rect.top,
      left: rect.left,
      width: rect.width,
      height: rect.height,
      poster,
    })
  }

  const handleClickCapture = (event: ReactMouseEvent<HTMLAnchorElement>) => {
    if (event.defaultPrevented) {
      return
    }
    if (event.button !== 0) {
      return
    }
    if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
      return
    }
    captureTransition()
  }

  const handleTouchStart = (_event: ReactTouchEvent<HTMLAnchorElement>) => {
    captureTransition()
  }

  return (
    <Paper
      {...others}
      component={Link}
      to={`/games/${game.id}`}
      ref={cardRef}
      onClickCapture={handleClickCapture}
      onTouchStart={handleTouchStart}
      shadow="md"
      p="md"
      __vars={{
        '--slide-image': `url(${poster})`,
        '--slide-title-color': titleColor,
      }}
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
        <Title pb={10} order={3} className={classes.title}>
          {title}
        </Title>
      </Stack>
    </Paper>
  )
}
