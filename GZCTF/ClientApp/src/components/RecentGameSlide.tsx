import dayjs from 'dayjs'
import { FC } from 'react'
import { Link } from 'react-router-dom'
import { Badge, Group, Stack, Title, createStyles, Paper } from '@mantine/core'
import { getGameStatus, GameColorMap, GameStatus } from './GameCard'
import { RecentGameProps } from './RecentGame'

const useStyles = createStyles((theme) => ({
  card: {
    height: 200,
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    backgroundSize: 'cover',
    backgroundPosition: 'center',
  },

  title: {
    fontWeight: 700,
    color: theme.white,
    lineHeight: 1.4,
    fontSize: 24,
    marginTop: theme.spacing.xs,
    textShadow: '0 0 10px rgba(0, 0, 0, 0.5)',
  },
}))

const RecentGameSlide: FC<RecentGameProps> = ({ game, ...others }) => {
  const { classes } = useStyles()

  const { title, poster, start, end } = game

  const startTime = dayjs(start!)
  const endTime = dayjs(end!)

  const status = getGameStatus(startTime, endTime)
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
      sx={{ backgroundImage: `url(${poster})` }}
      className={classes.card}
    >
      <Stack style={{ height: '100%' }} spacing={2} justify="space-between">
        <Group spacing={4}>
          <Badge size="sm" variant="filled">
            {game?.limit === 0 ? '多' : game?.limit === 1 ? '个' : game?.limit}人赛
          </Badge>
          <Badge size="sm" variant="filled" color={color}>
            {`${status === GameStatus.OnGoing ? '剩余' : '共'} ${duration} 小时`}
          </Badge>
        </Group>
        <Title pb={10} order={3} className={classes.title}>
          {title}
        </Title>
      </Stack>
    </Paper>
  )
}

export default RecentGameSlide
