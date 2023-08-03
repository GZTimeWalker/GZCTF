import dayjs from 'dayjs'
import React, { FC, useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Group,
  Text,
  useMantineTheme,
  ActionIcon,
  ScrollArea,
  Stack,
  Card,
  Switch,
  Input,
} from '@mantine/core'
import { useLocalStorage } from '@mantine/hooks'
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
import * as signalR from '@microsoft/signalr'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import { useTableStyles } from '@Utils/ThemeOverride'
import { useGame } from '@Utils/useGame'
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

  const [hideContainerEvents, setHideContainerEvents] = useLocalStorage({
    key: 'hide-container-events',
    defaultValue: false,
    getInitialValueInEffect: false,
  })

  const [activePage, setPage] = useState(1)

  const [, update] = useState(new Date())
  const newEvents = useRef<GameEvent[]>([])
  const [events, setEvents] = useState<GameEvent[]>()

  const { game } = useGame(numId)

  const iconMap = EventTypeIconMap(1.15)
  const { classes } = useTableStyles()

  useEffect(() => {
    api.game
      .gameEvents(numId, {
        hideContainer: hideContainerEvents,
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
        })
      })
    if (activePage === 1) {
      newEvents.current = []
    }
  }, [activePage, hideContainerEvents])

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

      connection
        .start()
        .then(() => {
          showNotification({
            color: 'teal',
            message: '实时事件连接成功',
            icon: <Icon path={mdiCheck} size={1} />,
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

  const filteredEvents = newEvents.current.filter(
    (e) =>
      !hideContainerEvents ||
      (e.type !== EventType.ContainerStart && e.type !== EventType.ContainerDestroy)
  )

  return (
    <WithGameMonitorTab>
      <Group position="apart" w="100%">
        <Switch
          label={SwitchLabel('隐藏容器事件', '隐藏容器启动/销毁事件')}
          checked={hideContainerEvents}
          onChange={(e) => setHideContainerEvents(e.currentTarget.checked)}
        />
        <Group position="right">
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
      <ScrollArea offsetScrollbars h="calc(100vh - 160px)">
        <Stack spacing="xs" pr={10} w="100%">
          {[...(activePage === 1 ? filteredEvents : []), ...(events ?? [])]?.map((event, i) => (
            <Card
              shadow="sm"
              radius="sm"
              p="xs"
              key={`${event.time}@${i}`}
              className={
                i === 0 && activePage === 1 && filteredEvents.length > 0 ? classes.fade : undefined
              }
            >
              <Group noWrap align="flex-start" position="right" spacing="sm" w="100%">
                {iconMap.get(event.type)}
                <Stack spacing={2} w="100%">
                  <Input
                    variant="unstyled"
                    value={event.content}
                    readOnly
                    sx={(theme) => ({
                      wrapper: {
                        width: '100%',
                      },

                      input: {
                        userSelect: 'none',
                        fontWeight: 500,
                        fontSize: theme.fontSizes.md,
                        lineHeight: '1em',
                        height: '1em',
                      },
                    })}
                  />
                  <Group noWrap position="apart">
                    <Text size="sm" fw={500} c="dimmed">
                      {event.team}, {event.user}
                    </Text>
                    <Text size="xs" fw={500} c="dimmed">
                      {dayjs(event.time).format('MM/DD HH:mm:ss')}
                    </Text>
                  </Group>
                </Stack>
              </Group>
            </Card>
          ))}
        </Stack>
      </ScrollArea>
    </WithGameMonitorTab>
  )
}

export default Events
