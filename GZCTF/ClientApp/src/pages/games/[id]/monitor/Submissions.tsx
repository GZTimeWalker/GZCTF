import * as signalR from '@microsoft/signalr'
import dayjs from 'dayjs'
import { FC, useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Group,
  SegmentedControl,
  ActionIcon,
  Paper,
  Table,
  ScrollArea,
  useMantineTheme,
  Input,
  Tooltip,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import {
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiCheck,
  mdiClose,
  mdiCrosshairsQuestion,
  mdiDotsHorizontal,
  mdiDownloadOutline,
  mdiExclamationThick,
  mdiFlag,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import { useTableStyles } from '@Utils/ThemeOverride'
import api, { AnswerResult, Submission } from '@Api'

const ITEM_COUNT_PER_PAGE = 50

const AnswerResultMap = new Map([
  [AnswerResult.Accepted, 'AC'],
  [AnswerResult.WrongAnswer, 'WA'],
  [AnswerResult.CheatDetected, 'CD'],
  [AnswerResult.NotFound, 'NF'],
])

const AnswerResultIconMap = (size: number) => {
  const theme = useMantineTheme()
  const colorIdx = theme.colorScheme === 'dark' ? 4 : 7

  return new Map([
    [
      AnswerResult.Accepted,
      <Icon path={mdiCheck} size={size} color={theme.colors.green[colorIdx]} />,
    ],
    [
      AnswerResult.WrongAnswer,
      <Icon path={mdiClose} size={size} color={theme.colors.red[colorIdx]} />,
    ],
    [
      AnswerResult.NotFound,
      <Icon path={mdiCrosshairsQuestion} size={size} color={theme.colors.gray[colorIdx]} />,
    ],
    [
      AnswerResult.CheatDetected,
      <Icon path={mdiExclamationThick} size={size} color={theme.colors.orange[colorIdx]} />,
    ],
    [
      AnswerResult.FlagSubmitted,
      <Icon path={mdiDotsHorizontal} size={size} color={theme.colors.gray[colorIdx]} />,
    ],
  ])
}

const Submissions: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [activePage, setPage] = useState(1)

  const [, update] = useState(new Date())
  const newSubmissions = useRef<Submission[]>([])
  const [submissions, setSubmissions] = useState<Submission[]>()
  const [type, setType] = useState<AnswerResult | 'All'>('All')

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  const iconMap = AnswerResultIconMap(0.8)
  const { classes, cx } = useTableStyles()

  useEffect(() => {
    api.game
      .gameSubmissions(numId, {
        type: type === 'All' ? undefined : type,
        count: ITEM_COUNT_PER_PAGE,
        skip: (activePage - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((data) => {
        setSubmissions(data.data)
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
      newSubmissions.current = []
    }
  }, [activePage, type])

  useEffect(() => {
    if (game?.end && new Date() < new Date(game.end)) {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hub/monitor?game=${numId}`)
        .withHubProtocol(new signalR.JsonHubProtocol())
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.None)
        .build()

      connection.serverTimeoutInMilliseconds = 60 * 1000 * 60 * 2

      connection.on('ReceivedSubmissions', (message: Submission) => {
        console.log(message)
        newSubmissions.current = [message, ...newSubmissions.current]
        update(new Date(message.time!))
      })

      connection
        .start()
        .then(() => {
          showNotification({
            color: 'teal',
            message: '实时提交连接成功',
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

  const rows = [...(activePage === 1 ? newSubmissions.current : []), ...(submissions ?? [])].map(
    (item, i) => (
      <tr
        key={`${item.time}@${i}`}
        className={
          i === 0 && activePage === 1 && newSubmissions.current.length > 0
            ? cx(classes.fade)
            : undefined
        }
      >
        <td>{iconMap.get(item.status ?? AnswerResult.FlagSubmitted)}</td>
        <td className={cx(classes.mono)}>{dayjs(item.time).format('MM/DD HH:mm:ss')}</td>
        <td>{item.team ?? 'Team'}</td>
        <td>{item.user ?? 'User'}</td>
        <td>{item.challenge ?? 'Challenge'}</td>
        <td
          style={{
            width: '36vw',
            maxWidth: '100%',
            padding: 0,
          }}
        >
          <Input
            variant="unstyled"
            value={item.answer}
            readOnly
            size="sm"
            sx={(theme) => ({
              input: {
                fontFamily: theme.fontFamilyMonospace,
              },
              wrapper: {
                width: '100%',
              },
            })}
          />
        </td>
      </tr>
    )
  )

  return (
    <WithGameMonitorTab>
      <Group position="apart" style={{ width: '100%' }}>
        <SegmentedControl
          color="brand"
          value={type}
          styles={{
            root: {
              background: 'transparent',
            },
          }}
          onChange={(value: AnswerResult | 'All') => {
            setType(value)
            setPage(1)
          }}
          data={[
            {
              label: 'All',
              value: 'All',
            },
            ...Object.entries(AnswerResult)
              .map((role) => ({
                value: role[1],
                label: AnswerResultMap.get(role[1]),
              }))
              .filter((role) => role.value !== AnswerResult.FlagSubmitted),
          ]}
        />
        <Group position="right">
          <Tooltip label="下载全部提交" position='left'>
            <ActionIcon
              size="lg"
              onClick={() => window.open(`/api/game/${numId}/submissionsheet`, '_blank')}
            >
              <Icon path={mdiDownloadOutline} size={1} />
            </ActionIcon>
          </Tooltip>
          <ActionIcon size="lg" disabled={activePage <= 1} onClick={() => setPage(activePage - 1)}>
            <Icon path={mdiArrowLeftBold} size={1} />
          </ActionIcon>
          <ActionIcon
            size="lg"
            disabled={submissions && submissions.length < ITEM_COUNT_PER_PAGE}
            onClick={() => setPage(activePage + 1)}
          >
            <Icon path={mdiArrowRightBold} size={1} />
          </ActionIcon>
        </Group>
      </Group>
      <Paper shadow="md" p="md">
        <ScrollArea offsetScrollbars style={{ height: 'calc(100vh - 200px)' }}>
          <Table className={classes.table}>
            <thead>
              <tr>
                <th>
                  <Group align="center">
                    <Icon path={mdiFlag} size={0.8} />
                  </Group>
                </th>
                <th>时间</th>
                <th>队伍</th>
                <th>用户</th>
                <th>题目</th>
                <th className={cx(classes.mono)}>flag</th>
              </tr>
            </thead>
            <tbody>{rows}</tbody>
          </Table>
        </ScrollArea>
      </Paper>
    </WithGameMonitorTab>
  )
}

export default Submissions
