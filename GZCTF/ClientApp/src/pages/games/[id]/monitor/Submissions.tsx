import api, { AnswerResult, Submission } from '@Api'
import * as signalR from '@microsoft/signalr'
import { FC, useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Group, SegmentedControl, ActionIcon } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiArrowLeftBold, mdiArrowRightBold, mdiCheck, mdiClose } from '@mdi/js'
import Icon from '@mdi/react'
import WithGameMonitorTab from '@Components/WithGameMonitor'

const ITEM_COUNT_PER_PAGE = 50

const AnswerResultMap = new Map([
  [AnswerResult.Accepted, 'AC'],
  [AnswerResult.WrongAnswer, 'WA'],
  [AnswerResult.CheatDetected, 'CD'],
  [AnswerResult.NotFound, 'NF'],
])

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
        .withUrl(`/hub/user?game=${numId}`)
        .withHubProtocol(new signalR.JsonHubProtocol())
        .withAutomaticReconnect()
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
  }, [numId])

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
          onChange={(value: AnswerResult | 'All') => setType(value)}
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
    </WithGameMonitorTab>
  )
}

export default Submissions
