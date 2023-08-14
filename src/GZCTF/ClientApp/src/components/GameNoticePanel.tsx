import dayjs from 'dayjs'
import { FC, useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Card, List, ScrollArea, SegmentedControl, Stack, Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import * as signalR from '@microsoft/signalr'
import Empty from '@Components/Empty'
import { InlineMarkdownRender } from '@Components/MarkdownRender'
import { NoticTypeIconMap } from '@Utils/Shared'
import api, { GameNotice, NoticeType } from '@Api'

enum NoticeFilter {
  All = 'all',
  Challenge = 'challenge',
  Events = 'events',
  Game = 'game',
}

const ApplyFilter = (notices: GameNotice[], filter: NoticeFilter) => {
  switch (filter) {
    case NoticeFilter.All:
      return notices
    case NoticeFilter.Challenge:
      return notices.filter(
        (notice) => notice.type === NoticeType.NewChallenge || notice.type === NoticeType.NewHint
      )
    case NoticeFilter.Events:
      return notices.filter(
        (notice) =>
          notice.type === NoticeType.FirstBlood ||
          notice.type === NoticeType.SecondBlood ||
          notice.type === NoticeType.ThirdBlood
      )
    case NoticeFilter.Game:
      return notices.filter((notice) => notice.type === NoticeType.Normal)
    default:
      return notices
  }
}

const GameNoticePanel: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [, update] = useState(new Date())
  const newNotices = useRef<GameNotice[]>([])
  const [notices, setNotices] = useState<GameNotice[]>()
  const [filter, setFilter] = useState<NoticeFilter>(NoticeFilter.All)
  const iconMap = NoticTypeIconMap(0.8)

  useEffect(() => {
    api.game
      .gameNotices(numId)
      .then((data) => {
        setNotices(data.data)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '获取通知失败',
          message: err.response.data.title,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
  }, [numId])

  useEffect(() => {
    newNotices.current = []
  }, [notices])

  useEffect(() => {
    if (id) {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hub/user?game=${numId}`)
        .withHubProtocol(new signalR.JsonHubProtocol())
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.None)
        .build()

      connection.serverTimeoutInMilliseconds = 60 * 1000 * 60 * 2

      connection.on('ReceivedGameNotice', (message: GameNotice) => {
        console.log(message)
        newNotices.current = [message, ...newNotices.current]

        if (message.type === NoticeType.NewChallenge || message.type === NoticeType.NewHint) {
          showNotification({
            color: 'yellow',
            message: message.content,
            autoClose: 5000,
          })
        }

        if (message.type === NoticeType.Normal) {
          showNotification({
            color: 'brand',
            message: message.content,
            autoClose: 5000,
          })
        }

        update(new Date(message.time))
      })

      connection.start().catch((error) => {
        console.error(error)
      })

      return () => {
        connection.stop().catch((err) => {
          console.error(err)
        })
      }
    }
  }, [])

  const allNotices = [...newNotices.current, ...(notices ?? [])]
  const filteredNotices = ApplyFilter(allNotices, filter)

  filteredNotices.sort((a, b) =>
    a.type !== b.type && (a.type === NoticeType.Normal || b.type == NoticeType.Normal)
      ? (a.type === NoticeType.Normal ? -1 : 1)
      : (dayjs(a.time).isAfter(b.time) ? -1 : 1)
  )

  return (
    <Card shadow="sm" w="20rem">
      <Stack spacing="xs">
        <SegmentedControl
          value={filter}
          styles={{
            root: {
              background: 'transparent',
            },
          }}
          onChange={(value: NoticeFilter) => setFilter(value)}
          data={[
            { value: NoticeFilter.All, label: '全部' },
            { value: NoticeFilter.Game, label: '通知' },
            { value: NoticeFilter.Events, label: '动态' },
            { value: NoticeFilter.Challenge, label: '题目' },
          ]}
        />
        {filteredNotices.length ? (
          <ScrollArea offsetScrollbars scrollbarSize={0} h="calc(100vh - 25rem)">
            <List
              size="sm"
              spacing={3}
              styles={(theme) => ({
                item: {
                  fontWeight: 500,
                  color: theme.colorScheme === 'dark' ? theme.colors.dark[2] : theme.colors.gray[6],
                },
              })}
            >
              {filteredNotices.map((notice) => (
                <List.Item key={notice.id} icon={iconMap.get(notice.type)}>
                  <Stack spacing={1}>
                    <Text size="xs" fw={700} c="dimmed">
                      {dayjs(notice.time).format('YY/MM/DD HH:mm:ss')}
                    </Text>
                    <InlineMarkdownRender source={notice.content} />
                  </Stack>
                </List.Item>
              ))}
            </List>
          </ScrollArea>
        ) : (
          <Stack justify="center" h="calc(100vh - 25rem)">
            <Empty description="暂无通知" />
          </Stack>
        )}
      </Stack>
    </Card>
  )
}

export default GameNoticePanel
