import {
  ActionIcon,
  Card,
  Group,
  Input,
  ScrollArea,
  Stack,
  Switch,
  Text,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { useLocalStorage } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiCheck,
  mdiClose,
  mdiExclamationThick,
  mdiFlag,
  mdiLightningBolt,
  mdiReplay,
  mdiToggleSwitchOffOutline,
  mdiToggleSwitchOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import * as signalR from '@microsoft/signalr'
import dayjs from 'dayjs'
import { TFunction } from 'i18next'
import React, { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameMonitor } from '@Components/WithGameMonitor'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import { handleAxiosError } from '@Utils/ApiHelper'
import { useLanguage } from '@Utils/I18n'
import { useDisplayInputStyles } from '@Utils/ThemeOverride'
import { useGame } from '@Hooks/useGame'
import api, { AnswerResult, EventType, GameEvent } from '@Api'
import tableClasses from '@Styles/Table.module.css'

const ITEM_COUNT_PER_PAGE = 30

const EventTypeIconMap = (size: number) => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()
  const colorIdx = colorScheme === 'dark' ? 5 : 7

  return new Map([
    [EventType.FlagSubmit, { path: mdiFlag, size, color: theme.colors.cyan[colorIdx] }],
    [EventType.ContainerStart, { path: mdiToggleSwitchOutline, size, color: theme.colors.green[colorIdx] }],
    [EventType.ContainerDestroy, { path: mdiToggleSwitchOffOutline, size, color: theme.colors.red[colorIdx] }],
    [EventType.CheatDetected, { path: mdiExclamationThick, size, color: theme.colors.orange[colorIdx] }],
    [EventType.Normal, { path: mdiLightningBolt, size, color: theme.colors.light[colorIdx] }],
  ])
}

const formatAnswer = (t: TFunction, res: AnswerResult) => {
  switch (res) {
    case AnswerResult.Accepted:
      return t('game.event.answer.accepted')
    case AnswerResult.WrongAnswer:
      return t('game.event.answer.wrong')
    case AnswerResult.CheatDetected:
      return t('game.event.answer.cheat')
    case AnswerResult.FlagSubmitted:
      return t('game.event.answer.submitted')
    case AnswerResult.NotFound:
      return t('game.event.answer.not_found')
    default:
      return ''
  }
}

const formatEvent = (t: TFunction, event: GameEvent) => {
  switch (event.type) {
    case EventType.Normal:
      return event.values.at(-1) || ''
    case EventType.FlagSubmit:
      return t('game.event.flag_submit', {
        status: formatAnswer(t, event.values.at(0) as AnswerResult),
        flag: event.values.at(1),
        chal: event.values.at(2),
        id: event.values.at(3),
      })
    case EventType.CheatDetected:
      return t('game.event.cheat_detected', {
        chal: event.values.at(0),
        team: event.values.at(1),
        steam: event.values.at(2),
      })
    case EventType.ContainerStart:
      return t('game.event.container.start', {
        id: event.values.at(0),
        chal: event.values.at(1),
      })
    case EventType.ContainerDestroy:
      return t('game.event.container.destroy', {
        id: event.values.at(0),
        chal: event.values.at(1),
      })
    default:
      return event.values.at(-1) || ''
  }
}

const Events: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [hideContainerEvents, setHideContainerEvents] = useLocalStorage({
    key: 'hide-container-events',
    defaultValue: false,
    getInitialValueInEffect: false,
  })

  const { locale } = useLanguage()

  const [activePage, setPage] = useState(1)

  const [, update] = useState(new Date())
  const newEvents = useRef<GameEvent[]>([])
  const [events, setEvents] = useState<GameEvent[]>()

  const { game } = useGame(numId)

  const iconMap = EventTypeIconMap(1.15)
  const { classes: inputClasses } = useDisplayInputStyles({ fw: 500 })
  const { t } = useTranslation()
  const viewport = useRef<HTMLDivElement>(null)

  useEffect(() => {
    viewport.current?.scrollTo({ top: 0, behavior: 'smooth' })
  }, [activePage, viewport])

  useEffect(() => {
    const fetchEvents = async () => {
      try {
        const res = await api.game.gameEvents(numId, {
          hideContainer: hideContainerEvents,
          count: ITEM_COUNT_PER_PAGE,
          skip: (activePage - 1) * ITEM_COUNT_PER_PAGE,
        })
        setEvents(res.data)
      } catch (err) {
        showNotification({
          color: 'red',
          title: t('game.notification.fetch_failed.event'),
          message: await handleAxiosError(err),
          icon: <Icon path={mdiClose} size={1} />,
        })
      }
    }

    fetchEvents()

    if (activePage === 1) {
      newEvents.current = []
    }
  }, [activePage, hideContainerEvents, numId, t])

  useEffect(() => {
    if (game?.end && new Date() < new Date(game.end)) {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hub/monitor?game=${numId}`)
        .withHubProtocol(new signalR.JsonHubProtocol())
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.None)
        .build()

      connection.serverTimeoutInMilliseconds = 60 * 1000 * 60 * 2

      connection.on('ReceivedGameEvent', (message: GameEvent) => {
        console.log(message)
        newEvents.current = [message, ...newEvents.current]
        update(new Date(message.time!))
      })

      const startConnection = async () => {
        try {
          await connection.start()
          showNotification({
            color: 'teal',
            message: t('game.notification.connected.event'),
            icon: <Icon path={mdiCheck} size={1} />,
          })
        } catch (err) {
          console.error(err)
        }
      }

      startConnection()

      return () => {
        connection.stop().catch((err) => {
          console.error(err)
        })
      }
    }
  }, [game, numId, t])

  const filteredEvents = newEvents.current.filter(
    (e) => !hideContainerEvents || (e.type !== EventType.ContainerStart && e.type !== EventType.ContainerDestroy)
  )

  return (
    <WithGameMonitor isLoading={!events}>
      <Group justify="space-between" w="100%">
        <Switch
          label={SwitchLabel(
            t('game.content.hide_container_events.label'),
            t('game.content.hide_container_events.description')
          )}
          checked={hideContainerEvents}
          onChange={(e) => setHideContainerEvents(e.currentTarget.checked)}
        />
        <Group justify="right">
          <ActionIcon size="lg" disabled={activePage <= 1} onClick={() => setPage(1)}>
            <Icon path={mdiReplay} size={1} />
          </ActionIcon>
          <ActionIcon size="lg" disabled={activePage <= 1} onClick={() => setPage(activePage - 1)}>
            <Icon path={mdiArrowLeftBold} size={1} />
          </ActionIcon>
          <ActionIcon
            size="lg"
            disabled={events && events.length < ITEM_COUNT_PER_PAGE}
            onClick={() => setPage(activePage + 1)}
          >
            <Icon path={mdiArrowRightBold} size={1} />
          </ActionIcon>
        </Group>
      </Group>
      <ScrollArea viewportRef={viewport} offsetScrollbars h="calc(100vh - 160px)">
        <Stack gap="xs" pr={10} w="100%">
          {[...(activePage === 1 ? filteredEvents : []), ...(events ?? [])]?.map((event, i) => (
            <Card
              shadow="sm"
              radius="sm"
              p="xs"
              key={`${event.time}@${i}`}
              className={i === 0 && activePage === 1 && filteredEvents.length > 0 ? tableClasses.fade : undefined}
            >
              <Group wrap="nowrap" align="flex-start" justify="right" gap="sm" w="100%">
                <Icon {...iconMap.get(event.type)!} />
                <Stack gap={2} w="100%">
                  <Input
                    variant="unstyled"
                    value={formatEvent(t, event)}
                    readOnly
                    size="md"
                    classNames={inputClasses}
                  />
                  <Group wrap="nowrap" justify="space-between">
                    <Text size="sm" fw={500} c="dimmed">
                      {event.team}, {event.user}
                    </Text>
                    <Text size="xs" fw={500} c="dimmed">
                      {dayjs(event.time).locale(locale).format('SL LTS')}
                    </Text>
                  </Group>
                </Stack>
              </Group>
            </Card>
          ))}
        </Stack>
      </ScrollArea>
    </WithGameMonitor>
  )
}

export default Events
