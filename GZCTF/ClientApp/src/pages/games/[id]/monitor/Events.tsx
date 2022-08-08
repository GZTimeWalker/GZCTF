import * as signalR from '@microsoft/signalr'
import dayjs from 'dayjs'
import React, { FC, useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Group,
  Text,
  useMantineTheme,
  ActionIcon,
  ScrollArea,
  Badge,
  Stack,
  Card,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import {
  mdiFlag,
  mdiLightningBolt,
  mdiToggleSwitchOutline,
  mdiToggleSwitchOffOutline,
  mdiExclamationThick,
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiCheck,
  mdiClose,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import api, { EventType, GameEvent } from '@Api'

const ITEM_COUNT_PER_PAGE = 30

const EventTypeIconMap = (size: number) => {
  const theme = useMantineTheme()
  const colorIdx = theme.colorScheme === 'dark' ? 5 : 7

  return new Map([
    [EventType.FlagSubmit, <Icon path={mdiFlag} size={size} color={theme.colors.cyan[colorIdx]} />],
    [
      EventType.ContainerStart,
      <Icon path={mdiToggleSwitchOutline} size={size} color={theme.colors.green[colorIdx]} />,
    ],
    [
      EventType.ContainerDestroy,
      <Icon path={mdiToggleSwitchOffOutline} size={size} color={theme.colors.red[colorIdx]} />,
    ],
    [
      EventType.CheatDetected,
      <Icon path={mdiExclamationThick} size={size} color={theme.colors.orange[colorIdx]} />,
    ],
    [
      EventType.Normal,
      <Icon path={mdiLightningBolt} size={size} color={theme.colors.white[colorIdx]} />,
    ],
  ])
}

const Events: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [activePage, setPage] = useState(1)

  const [, update] = useState(new Date())
  const newEvents = useRef<GameEvent[]>([])
  const [events, setEvents] = useState<GameEvent[]>()

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })
  const iconMap = EventTypeIconMap(1)

  useEffect(() => {
    api.game
      .gameEvents(numId, {
        count: ITEM_COUNT_PER_PAGE,
        skip: (activePage - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((data) => {
        setEvents(data.data)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '获取提交失败',
          message: err.response.data.title,
          icon: <Icon path={mdiClose} size={1} />,
          disallowClose: true,
        })
      })
    if (activePage === 1) {
      newEvents.current = []
    }
  }, [activePage])

  useEffect(() => {
    if (game?.end && new Date() < new Date(game.end)) {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hub/monitor?game=${numId}`)
        .withHubProtocol(new signalR.JsonHubProtocol())
        .withAutomaticReconnect()
        .build()

      connection.serverTimeoutInMilliseconds = 60 * 1000 * 60 * 2

      connection.on('ReceivedGameEvent', (message: GameEvent) => {
        console.log(message)
        newEvents.current = [message, ...newEvents.current]
        update(new Date(message.time!))
      })

      connection
        .start()
        .then(() => {
          showNotification({
            color: 'teal',
            message: '实时事件连接成功',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
        })
        .catch((error) => {
          console.error(error)
        })

      return () => {
        connection.stop().catch((err) => {
          console.error(err)
        })
      }
    }
  }, [game])

  return (
    <WithGameMonitorTab>
      <ScrollArea offsetScrollbars style={{ height: 'calc(100vh - 160px)' }}>
        <Stack spacing="xs">
          {[...(activePage === 1 ? newEvents.current : []), ...(events ?? [])]?.map((event) => (
            <Card shadow="sm" radius="sm" p="xs">
              <Stack>
                <Group position="apart">
                  <Group position="right">
                    {iconMap.get(event.type)}
                    <Text>{event.content}</Text>
                  </Group>
                  <Badge size="xs" variant="dot">
                    {dayjs(event.time).format('MM/DD HH:mm:ss')}
                  </Badge>
                </Group>
              </Stack>
              <Text size="sm" weight={500} color="dimmed">
                {event.team}, {event.user}
              </Text>
            </Card>
          ))}
        </Stack>
      </ScrollArea>
      <Group position="right" style={{ width: '100%' }}>
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
    </WithGameMonitorTab>
  )
}

export default Events
