import {
  BackgroundImage,
  Badge,
  Card,
  Center,
  Group,
  MantineColor,
  Stack,
  Title,
  Text,
  useMantineTheme,
} from '@mantine/core'
import { mdiChevronTripleRight, mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, MouseEvent as ReactMouseEvent, TouchEvent as ReactTouchEvent, useMemo, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useLanguage } from '@Utils/I18n'
import { getGameStatus, toLimitTag } from '@Hooks/useGame'
import { BasicGameInfoModel } from '@Api'
import misc from '@Styles/Misc.module.css'
import { storeGameTransitionState } from '@Utils/gameTransition'

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

export const GameCard: FC<GameCardProps> = ({ game, ...others }) => {
  const theme = useMantineTheme()
  const { t } = useTranslation()
  const { locale } = useLanguage()
  const cardRef = useRef<HTMLAnchorElement>(null)

  const { summary, title, poster, limit } = game
  const { startTime, endTime, status } = getGameStatus(game)

  const duration = endTime.diff(startTime, 'hours')

  const color = GameColorMap.get(status)

  const durationLabel = useMemo(
    () =>
      t('game.content.duration', {
        hours: duration,
      }),
    [duration, t]
  )

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
    <Card
      {...others}
      shadow="sm"
      component={Link}
      to={`/games/${game.id}`}
      ref={cardRef}
      onClickCapture={handleClickCapture}
      onTouchStart={handleTouchStart}
      classNames={{ root: misc.hoverCard }}
    >
      <Card.Section>
        <BackgroundImage src={poster ?? ''} h="12rem" w="100%" pos="relative">
          {!poster && (
            <Center h="100%">
              <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
            </Center>
          )}
          <Center pos="absolute" top={8} right={8}>
            <Badge color={color} size="sm" variant="filled">
              {status}
            </Badge>
          </Center>
        </BackgroundImage>
      </Card.Section>
      <Stack gap="sm" pt="sm">
        <Group gap={0} justify="space-between" align="flex-start">
          <Stack gap={2} flex={1}>
            <Group wrap="nowrap" gap="xs">
              <Badge size="xs" color={color}>
                {toLimitTag(t, limit)}
              </Badge>
              <Badge size="xs" color={color}>
                {durationLabel}
              </Badge>
            </Group>
            <Title order={3} ta="left" lineClamp={2}>
              {title}
            </Title>
            <Group mt={4} wrap="nowrap" gap={3}>
              <Badge size="xs" color={color}>
                {startTime.locale(locale).format('L LTS')}
              </Badge>
              <Icon path={mdiChevronTripleRight} size={1} />
              <Badge size="xs" color={color}>
                {endTime.locale(locale).format('L LTS')}
              </Badge>
            </Group>
          </Stack>
        </Group>
        <Text fw={500} size="sm" lineClamp={3}>
          {summary}
        </Text>
      </Stack>
    </Card>
  )
}
