import { Badge, Card, Center, Group, Image, Stack, Text, Title, useMantineTheme } from '@mantine/core'
import { mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { GameColorMap, GameStatus } from '@Components/GameCard'
import { useLanguage } from '@Utils/I18n'
import { useForeground } from '@Hooks/useForeground'
import { getGameStatus } from '@Hooks/useGame'
import { BasicGameInfoModel } from '@Api'
import classes from '@Styles/HoverCard.module.css'
import misc from '@Styles/Misc.module.css'

export interface RecentGameProps {
  game: BasicGameInfoModel
}

const POSTER_HEIGHT = '9rem'

export const RecentGame: FC<RecentGameProps> = ({ game, ...others }) => {
  const { t } = useTranslation()
  const { locale } = useLanguage()

  const { title, poster } = game
  const { startTime, endTime, status } = getGameStatus(game)
  const theme = useMantineTheme()

  const color = GameColorMap.get(status)

  const duration = status === GameStatus.OnGoing ? endTime.diff(dayjs(), 'h') : endTime.diff(startTime, 'h')

  const titleColor = useForeground(poster)

  return (
    <Card {...others} shadow="sm" component={Link} to={`/games/${game.id}`} classNames={classes}>
      <Card.Section pos="relative">
        {poster ? (
          <Image src={poster} h={POSTER_HEIGHT} alt="poster" />
        ) : (
          <Center mih={POSTER_HEIGHT}>
            <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
          </Center>
        )}
      </Card.Section>

      <Card.Section inheritPadding pos="relative" mt={`calc(16px - ${POSTER_HEIGHT})`} className={misc.alignEnd}>
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
        display="flex"
        p="0 16px"
        className={misc.alignCenter}
      >
        <Title lineClamp={1} order={4} ta="left" c={titleColor}>
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
              ? dayjs(startTime).locale(locale).format('L LT')
              : dayjs(endTime).locale(locale).format('L LT')}
          </Badge>
        </Group>
        <Group wrap="nowrap" gap={0} justify="space-between">
          <Text size="sm" fw="bold">
            {status === GameStatus.OnGoing ? t('game.content.remaining_time') : t('game.content.total_time')}
          </Text>
          <Badge size="xs" color={color} variant="light">
            {t('game.content.duration', { hours: duration })}
          </Badge>
        </Group>
      </Stack>
    </Card>
  )
}
