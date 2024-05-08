import { Badge, Card, Center, Group, Image, Stack, Text, Title } from '@mantine/core'
import { mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { GameColorMap, GameStatus } from '@Components/GameCard'
import { useHoverCardStyles } from '@Utils/ThemeOverride'
import { getGameStatus } from '@Utils/useGame'
import { BasicGameInfoModel } from '@Api'

export interface RecentGameProps {
  game: BasicGameInfoModel
}

const POSTER_HEIGHT = '9rem'

const RecentGame: FC<RecentGameProps> = ({ game, ...others }) => {
  const { t } = useTranslation()

  const { title, poster } = game
  const { startTime, endTime, status } = getGameStatus(game)
  const { classes: cardClasses, theme } = useHoverCardStyles()

  const color = GameColorMap.get(status)

  const duration =
    status === GameStatus.OnGoing ? endTime.diff(dayjs(), 'h') : endTime.diff(startTime, 'h')

  return (
    <Card
      {...others}
      shadow="sm"
      component={Link}
      to={`/games/${game.id}`}
      classNames={cardClasses}
    >
      <Card.Section pos="relative">
        {poster ? (
          <Image src={poster} h={POSTER_HEIGHT} alt="poster" />
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
        <Group wrap="nowrap" gap="xs" justify="right">
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
        <Title lineClamp={1} order={4} ta="left" c={theme.colors.gray[0]}>
          &gt; {title}
        </Title>
      </Card.Section>

      <Stack gap={0} mt={16}>
        <Group wrap="nowrap" gap={0} justify="space-between">
          <Text size="sm" fw="bold">
            {status === GameStatus.Coming ? t('game.content.start_at') : t('game.content.end_at')}
          </Text>
          <Badge size="xs" color={color} variant="light">
            {status === GameStatus.Coming
              ? dayjs(startTime).format('YY/MM/DD HH:mm')
              : dayjs(endTime).format('YY/MM/DD HH:mm')}
          </Badge>
        </Group>
        <Group wrap="nowrap" gap={0} justify="space-between">
          <Text size="sm" fw="bold">
            {status === GameStatus.OnGoing
              ? t('game.content.remaining_time')
              : t('game.content.total_time')}
          </Text>
          <Badge size="xs" color={color} variant="light">
            {t('game.content.duration', { hours: duration })}
          </Badge>
        </Group>
      </Stack>
    </Card>
  )
}

export default RecentGame
