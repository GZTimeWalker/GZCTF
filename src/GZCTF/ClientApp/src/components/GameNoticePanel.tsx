import {
  Card,
  Center,
  List,
  ScrollArea,
  SegmentedControl,
  Stack,
  Text,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import * as signalR from '@microsoft/signalr'
import dayjs from 'dayjs'
import { TFunction } from 'i18next'
import { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import Empty from '@Components/Empty'
import { InlineMarkdown } from '@Components/MarkdownRenderer'
import { NoticTypeIconMap } from '@Utils/Shared'
import api, { GameNotice, NoticeType } from '@Api'
import typoClasses from '@Styles/Typography.module.css'

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

const formatNotice = (t: TFunction, notice: GameNotice) => {
  switch (notice.type) {
    case NoticeType.Normal:
      return notice.values.at(-1) || ''
    case NoticeType.NewChallenge:
      return t('game.notice.new_challenge', {
        title: notice.values.at(0),
      })
    case NoticeType.NewHint:
      return t('game.notice.new_hint', {
        title: notice.values.at(0),
      })
    case NoticeType.FirstBlood:
      return t('game.notice.blood', {
        team: notice.values.at(0),
        chal: notice.values.at(1),
        blood: t('challenge.bonus.first_blood'),
      })
    case NoticeType.SecondBlood:
      return t('game.notice.blood', {
        team: notice.values.at(0),
        chal: notice.values.at(1),
        blood: t('challenge.bonus.second_blood'),
      })
    case NoticeType.ThirdBlood:
      return t('game.notice.blood', {
        team: notice.values.at(0),
        chal: notice.values.at(1),
        blood: t('challenge.bonus.third_blood'),
      })
    default:
      return notice.values.at(-1) || ''
  }
}

const PANEL_HEIGHT = 'calc(100vh - 25rem)'

const GameNoticePanel: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [, update] = useState(new Date())
  const newNotices = useRef<GameNotice[]>([])
  const [notices, setNotices] = useState<GameNotice[]>()
  const [filter, setFilter] = useState<NoticeFilter>(NoticeFilter.All)
  const iconMap = NoticTypeIconMap(0.8)

  const { t } = useTranslation()
  const theme = useMantineTheme()

  useEffect(() => {
    api.game
      .gameNotices(numId)
      .then((data) => {
        setNotices(data.data)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: t('game.notification.fetch_failed.notice'),
          message: err.response.data.title,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
  }, [numId, t])

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
            message: formatNotice(t, message),
            autoClose: 5000,
          })
        }

        if (message.type === NoticeType.Normal) {
          showNotification({
            color: theme.primaryColor,
            message: formatNotice(t, message),
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
  })

  const allNotices = [...newNotices.current, ...(notices ?? [])]
  const filteredNotices = ApplyFilter(allNotices, filter)

  filteredNotices.sort((a, b) =>
    a.type !== b.type && (a.type === NoticeType.Normal || b.type == NoticeType.Normal)
      ? +(a.type !== NoticeType.Normal) || -1
      : dayjs(b.time).diff(a.time)
  )

  return (
    <Card shadow="sm" w="100%">
      <Stack gap="xs">
        <SegmentedControl
          value={filter}
          color={theme.primaryColor}
          fullWidth
          bg="transparent"
          fw={500}
          onChange={(value) => setFilter(value as NoticeFilter)}
          data={[
            { value: NoticeFilter.All, label: t('game.label.notice_type.all') },
            { value: NoticeFilter.Game, label: t('game.label.notice_type.game') },
            { value: NoticeFilter.Events, label: t('game.label.notice_type.events') },
            { value: NoticeFilter.Challenge, label: t('game.label.notice_type.challenge') },
          ]}
        />
        {filteredNotices.length ? (
          <ScrollArea offsetScrollbars scrollbarSize={0} h={PANEL_HEIGHT}>
            <List
              size="sm"
              spacing={3}
              styles={{
                itemWrapper: {
                  alignItems: 'normal',
                },
              }}
            >
              {filteredNotices.map((notice) => (
                <List.Item key={notice.id} icon={<Icon {...iconMap.get(notice.type)!} />}>
                  <Stack gap={1}>
                    <Text fz="xs" fw="bold" c="dimmed">
                      {dayjs(notice.time).format('YY/MM/DD HH:mm:ss')}
                    </Text>
                    {notice.type === NoticeType.Normal ? (
                      <InlineMarkdown
                        fz="sm"
                        fw={500}
                        c="dimmed"
                        source={formatNotice(t, notice)}
                      />
                    ) : (
                      <Text fz="sm" fw={500} c="dimmed" className={typoClasses.inline}>
                        {formatNotice(t, notice)}
                      </Text>
                    )}
                  </Stack>
                </List.Item>
              ))}
            </List>
          </ScrollArea>
        ) : (
          <Center h={PANEL_HEIGHT}>
            <Empty description={t('game.content.no_notice')} />
          </Center>
        )}
      </Stack>
    </Card>
  )
}

export default GameNoticePanel
