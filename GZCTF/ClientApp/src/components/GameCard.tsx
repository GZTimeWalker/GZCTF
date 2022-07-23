import { FC } from 'react'
import {
  Badge,
  Box,
  Card,
  Group,
  Image,
  MantineColor,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { BasicGameInfoModel } from '../Api'

export enum GameStatus {
  Coming = 'coming',
  OnGoing = 'ongoing',
  Ended = 'ended',
}

export const GameColorMap = new Map<GameStatus, MantineColor>([
  [GameStatus.Coming, 'yellow'],
  [GameStatus.OnGoing, 'brand'],
  [GameStatus.Ended, 'red'],
])

interface GameCardProps {
  game: BasicGameInfoModel
  onClick?: () => void
}

const getGameStatus = (start: Date, end: Date) => {
  const now = new Date()
  return end < now ? GameStatus.Ended : start > now ? GameStatus.Coming : GameStatus.OnGoing
}

const GameCard: FC<GameCardProps> = ({ game, ...others }) => {
  const theme = useMantineTheme()

  const { summary, title, poster, start, end } = game
  const startTime = new Date(start!)
  const endTime = new Date(end!)

  const color = GameColorMap.get(getGameStatus(startTime, endTime))

  return (
    <Card
      {...others}
      shadow="sm"
      sx={(theme) => ({
        cursor: 'pointer',
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
          <Box
            style={{ height: 160, display: 'flex', alignItems: 'center', justifyContent: 'center' }}
          >
            <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
          </Box>
        )}
      </Card.Section>
      <Stack style={{ flexGrow: 1 }}>
        <Group align="end" position="apart">
          <Title order={2} align="left">
            {title}
          </Title>
          <Text size="md">
            <Badge color={color} variant="light">
              {startTime.toLocaleString()}
            </Badge>
            ~
            <Badge color={color} variant="light">
              {endTime.toLocaleString()}
            </Badge>
          </Text>
        </Group>
        <Text size="md" lineClamp={1}>
          {summary}
        </Text>
      </Stack>
    </Card>
  )
}

export default GameCard
