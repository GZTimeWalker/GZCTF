import { BasicGameInfoModel } from '@Api/Api'
import { FC } from 'react'
import { Link } from 'react-router-dom'
import {
  Badge,
  Card,
  Center,
  Group,
  Image,
  MantineColor,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { mdiChevronTripleRight, mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'

export enum GameStatus {
  Coming = 'coming',
  OnGoing = 'ongoing',
  Ended = 'ended',
}

export const GameColorMap = new Map<GameStatus, MantineColor>([
  [GameStatus.Coming, 'yellow'],
  [GameStatus.OnGoing, 'green'],
  [GameStatus.Ended, 'blue'],
])

interface GameCardProps {
  game: BasicGameInfoModel
}

export const getGameStatus = (start: Date, end: Date) => {
  const now = new Date()
  return end < now ? GameStatus.Ended : start > now ? GameStatus.Coming : GameStatus.OnGoing
}

const GameCard: FC<GameCardProps> = ({ game, ...others }) => {
  const theme = useMantineTheme()

  const { summary, title, poster, start, end } = game
  const startTime = new Date(start!)
  const endTime = new Date(end!)

  const status = getGameStatus(startTime, endTime)
  const color = GameColorMap.get(status)

  return (
    <Card
      {...others}
      shadow="sm"
      component={Link}
      to={`/games/${game.id}`}
      sx={(theme) => ({
        transition: 'filter .2s',
        '&:hover': {
          filter: theme.colorScheme === 'dark' ? 'brightness(1.2)' : 'brightness(.97)',
        },
      })}
    >
      <Card.Section>
        {poster ? (
          <Image src={poster} height={160} alt="poster" />
        ) : (
          <Center style={{ height: 160 }}>
            <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
          </Center>
        )}
      </Card.Section>

      <Stack style={{ flexGrow: 1, marginTop: theme.spacing.sm }}>
        <Title order={2} align="left">
          {title}
        </Title>
        <Group spacing="xs">
          <Badge size="xs" color={color} variant="light">
            {startTime.toLocaleString()}
          </Badge>
          <Icon path={mdiChevronTripleRight} size={1} />
          <Badge size="xs" color={color} variant="light">
            {endTime.toLocaleString()}
          </Badge>
        </Group>

        <Text size="md" lineClamp={3} style={{ height: '4.9rem' }}>
          {summary}
        </Text>
      </Stack>
    </Card>
  )
}

export default GameCard
