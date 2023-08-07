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
import { GameColorMap, GameStatus } from '@Components/GameCard'
import { getGameStatus } from '@Utils/useGame'
import { BasicGameInfoModel } from '@Api'

export interface RecentGameProps {
  game: BasicGameInfoModel
}

const POSTER_HEIGHT = '15vh'

const RecentGame: FC<RecentGameProps> = ({ game, ...others }) => {
  const theme = useMantineTheme()

  const { title, poster } = game
  const { startTime, endTime, status } = getGameStatus(game)

  const color = GameColorMap.get(status)

  const duration =
    status === GameStatus.OnGoing ? endTime.diff(dayjs(), 'h') : endTime.diff(startTime, 'h')

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
      <Card.Section pos="relative">
        {poster ? (
          <Image src={poster} height={POSTER_HEIGHT} alt="poster" />
        ) : (
          <Center mih={POSTER_HEIGHT}>
            <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
          </Center>
        )}
      </Card.Section>

      <Card.Section
        inheritPadding
        pos="relative"
        mt={`calc(16px - ${POSTER_HEIGHT})`}
        style={{
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
        h={34}
        pos="relative"
        mt={`calc(${POSTER_HEIGHT} - 2rem - 34px)`}
        bg="rgba(0,0,0,.5)"
        display="flex"
        p="0 16px"
        style={{
          alignItems: 'center',
        }}
      >
        <Title lineClamp={1} order={4} align="left" color={theme.colors.gray[0]}>
          &gt; {title}
        </Title>
      </Card.Section>

      <Stack spacing={0} mt={16}>
        <Group noWrap spacing={0} position="apart">
          <Text size="sm" fw={700}>
            {status === GameStatus.Coming ? '开始于' : '结束于'}
          </Text>
          <Badge size="xs" color={color} variant="light">
            {status === GameStatus.Coming
              ? dayjs(startTime).format('YY/MM/DD HH:mm')
              : dayjs(endTime).format('YY/MM/DD HH:mm')}
          </Badge>
        </Group>
        <Group noWrap spacing={0} position="apart">
          <Text size="sm" fw={700}>
            {status === GameStatus.OnGoing ? '剩余时间' : '持续时间'}
          </Text>
          <Badge size="xs" color={color} variant="light">
            {`${duration} 小时`}
          </Badge>
        </Group>
      </Stack>
    </Card>
  )
}

export default RecentGame
