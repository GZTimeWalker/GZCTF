import * as signalR from '@microsoft/signalr'
import { FC, useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Card, List, ScrollArea, Stack } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { GameNotice, NoticeType } from '@Api'
import { NoticTypeIconMap } from '../utils/ChallengeItem'
import Empty from './Empty'

const ChallengeNoticePanel: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [, update] = useState(new Date())
  const newNotices = useRef<GameNotice[]>([])
  const [notices, setNotices] = useState<GameNotice[]>()
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
          disallowClose: true,
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
        if (message.type === NoticeType.NewChallenge) api.game.mutateGameChallenges(numId)
        update(new Date(message.time!))
      })

      connection
        .start()
        .then(() => {
          console.log('> 实时比赛通知已连接')
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
  }, [])

  const allNotices = [...newNotices.current, ...(notices ?? [])]

  const noticesToShow = [
    ...allNotices.filter((notice) => notice.type === NoticeType.Normal),
    ...allNotices.filter((notice) => notice.type !== NoticeType.Normal),
  ]

  return (
    <Card shadow="sm">
      {noticesToShow.length ? (
        <ScrollArea
          offsetScrollbars
          scrollbarSize={4}
          style={{ height: 'calc(100vh - 20rem)' }}
          sx={{
            scrollbar: {
              '&:hover': {
                backgroundColor: 'transparent',
              },
            },
          }}
        >
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
            {noticesToShow.map((notice) => (
              <List.Item key={notice.id} icon={iconMap.get(notice.type)}>
                {notice.content}
              </List.Item>
            ))}
          </List>
        </ScrollArea>
      ) : (
        <Stack justify="center" style={{ height: 'calc(100vh - 20rem)' }}>
          <Empty description="暂无通知" />
        </Stack>
      )}
    </Card>
  )
}

export default ChallengeNoticePanel
