import dayjs, { Dayjs } from 'dayjs'
import { FC } from 'react'
import { Link } from 'react-router-dom'
import {
  BackgroundImage,
  Badge,
  Card,
  Center,
  Group,
  MantineColor,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { mdiChevronTripleRight, mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { BasicGameInfoModel } from '@Api'

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

export const getGameStatus = (start: Dayjs, end: Dayjs) => {
  const now = dayjs()
  return end < now ? GameStatus.Ended : start > now ? GameStatus.Coming : GameStatus.OnGoing
}

const GameCard: FC<GameCardProps> = ({ game, ...others }) => {
  const theme = useMantineTheme()

  const { summary, title, poster, start, end, limit } = game
  const startTime = dayjs(start)
  const endTime = dayjs(end)

  const duration = endTime.diff(startTime, 'hours')

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
        width: '100%',
        '&:hover': {
          filter: theme.colorScheme === 'dark' ? 'brightness(1.2)' : 'brightness(.97)',
        },
      })}
    >
      <Card.Section>
        <Group noWrap align="flex-start">
          <BackgroundImage src={poster ?? ''} h="10rem" maw="20rem" miw="20rem">
            <Center h="100%">
              {!poster && <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />}
            </Center>
          </BackgroundImage>
          <Stack spacing="sm" p="md" w="100%">
            <Group spacing={0} position="apart" align="flex-start">
              <Stack spacing={2}>
                <Group noWrap spacing="xs">
                  <Badge size="xs" color={color}>
                    {limit === 0 ? '多' : limit === 1 ? '个' : limit}人赛
                  </Badge>
                  <Badge size="xs" color={color}>
                    {`${duration} 小时`}
                  </Badge>
                </Group>
                <Title order={2} align="left">
                  {title}
                </Title>
              </Stack>
              <Group mt={4} noWrap spacing={3}>
                <Badge size="xs" color={color}>
                  {startTime.format('YYYY/MM/DD HH:mm:ss')}
                </Badge>
                <Icon path={mdiChevronTripleRight} size={1} />
                <Badge size="xs" color={color}>
                  {endTime.format('YYYY/MM/DD HH:mm:ss')}
                </Badge>
              </Group>
            </Group>
            <Text fw={500} size="sm" lineClamp={3}>
              {summary}
            </Text>
          </Stack>
        </Group>
      </Card.Section>
    </Card>
  )
}

export default GameCard
