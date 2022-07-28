import dayjs from 'dayjs'
import { FC } from 'react'
import { Link } from 'react-router-dom'
import {
  Badge,
  Card,
  Center,
  Group,
  Image,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { BasicGameInfoModel } from '../Api'
import { getGameStatus, GameColorMap, GameStatus } from './GameCard'

interface RecentGameProps {
  game: BasicGameInfoModel
}

const POSTER_HEIGHT = 105

const RecentGame: FC<RecentGameProps> = ({ game, ...others }) => {
  const theme = useMantineTheme()

  const { title, poster, start, end } = game

  const startTime = new Date(start!)
  const endTime = new Date(end!)

  const status = getGameStatus(startTime, endTime)
  const color = GameColorMap.get(status)

  const duration =
    status === GameStatus.Coming
      ? dayjs(endTime.getTime()).diff(startTime.getTime(), 'h')
      : dayjs(endTime.getTime()).diff(new Date(), 'h')

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
      <Card.Section style={{ position: 'relative' }}>
        {poster ? (
          <Image src={poster} height={POSTER_HEIGHT} alt="poster" />
        ) : (
          <Center style={{ height: POSTER_HEIGHT }}>
            <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
          </Center>
        )}
      </Card.Section>

      <Card.Section
        inheritPadding
        style={{
          position: 'relative',
          marginTop: 16 - POSTER_HEIGHT,
          alignContent: 'flex-end',
        }}
      >
        <Group noWrap spacing="xs" position="right">
          <Badge size="xs" color={color} variant="filled">
            {status}
          </Badge>
        </Group>
      </Card.Section>

      <Card.Section
        style={{
          position: 'relative',
          marginTop: POSTER_HEIGHT - 32 - 34,
          background: 'rgba(0,0,0,.4)',
          display: 'flex',
          height: 34,
          padding: '0 16px',
          alignItems: 'center',
        }}
      >
        <Title order={4} align="left">
          &gt; {title}
        </Title>
      </Card.Section>

      <Stack spacing={0} style={{ marginTop: 16 }}>
        <Group noWrap spacing={0} position="apart">
          <Text size="sm" weight={700}>
            {status === GameStatus.Coming ? '开始于' : '结束于'}
          </Text>
          <Badge size="xs" color={color} variant="light">
            {status === GameStatus.Coming
              ? dayjs(startTime).format('YY/MM/DD HH:mm')
              : dayjs(endTime).format('YY/MM/DD HH:mm')}
          </Badge>
        </Group>
        <Group noWrap spacing={0} position="apart">
          <Text size="sm" weight={700}>
            {status === GameStatus.Coming ? '持续时间' : '剩余时间'}
          </Text>
          <Badge size="xs" color={color} variant="light">
            {duration}小时
          </Badge>
        </Group>
      </Stack>
    </Card>
  )
}

export default RecentGame
