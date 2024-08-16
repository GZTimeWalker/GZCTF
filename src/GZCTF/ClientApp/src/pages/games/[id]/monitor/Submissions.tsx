import {
  ActionIcon,
  Badge,
  Group,
  Input,
  Paper,
  ScrollArea,
  SegmentedControl,
  Table,
  Text,
  Tooltip,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import {
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiCheck,
  mdiClose,
  mdiCrosshairsQuestion,
  mdiDotsHorizontal,
  mdiDownload,
  mdiExclamationThick,
  mdiFlag,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import * as signalR from '@microsoft/signalr'
import dayjs from 'dayjs'
import { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import { downloadBlob } from '@Utils/ApiHelper'
import { useDisplayInputStyles } from '@Utils/ThemeOverride'
import { useGame } from '@Utils/useGame'
import api, { AnswerResult, Submission } from '@Api'
import tableClasses from '@Styles/Table.module.css'
import tooltipClasses from '@Styles/Tooltip.module.css'

const ITEM_COUNT_PER_PAGE = 50

const AnswerResultMap = new Map([
  [AnswerResult.Accepted, 'AC'],
  [AnswerResult.WrongAnswer, 'WA'],
  [AnswerResult.CheatDetected, 'CD'],
  [AnswerResult.NotFound, 'NF'],
])

const AnswerResultIconMap = (size: number) => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  const colorIdx = colorScheme === 'dark' ? 4 : 7

  return new Map([
    [AnswerResult.Accepted, { path: mdiCheck, size, color: theme.colors.green[colorIdx] }],
    [AnswerResult.WrongAnswer, { path: mdiClose, size, color: theme.colors.red[colorIdx] }],
    [
      AnswerResult.NotFound,

      { path: mdiCrosshairsQuestion, size, color: theme.colors.gray[colorIdx] },
    ],
    [
      AnswerResult.CheatDetected,

      { path: mdiExclamationThick, size, color: theme.colors.orange[colorIdx] },
    ],
    [
      AnswerResult.FlagSubmitted,
      { path: mdiDotsHorizontal, size, color: theme.colors.gray[colorIdx] },
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
  const [disabled, setDisabled] = useState(false)

  const { game } = useGame(numId)

  const iconMap = AnswerResultIconMap(0.8)
  const { classes: inputClasses } = useDisplayInputStyles({ ff: 'monospace' })
  const theme = useMantineTheme()

  const { t } = useTranslation()
  const viewport = useRef<HTMLDivElement>(null)

  useEffect(() => {
    viewport.current?.scrollTo({ top: 0, behavior: 'smooth' })
  }, [activePage, viewport])

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
          title: t('game.notification.fetch_failed.submission'),
          message: err.response.data.title,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
    if (activePage === 1) {
      newSubmissions.current = []
    }
  }, [activePage, type, numId, t])

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
            message: t('game.notification.connected.submission'),
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
  })

  const filteredSubs = newSubmissions.current.filter(
    (item) => type === 'All' || item.status === type
  )

  const rows = [...(activePage === 1 ? filteredSubs : []), ...(submissions ?? [])].map(
    (item, i) => (
      <Table.Tr
        key={`${item.time}@${i}`}
        className={
          i === 0 && activePage === 1 && filteredSubs.length > 0 ? tableClasses.fade : undefined
        }
      >
        <Table.Td>
          <Icon {...iconMap.get(item.status ?? AnswerResult.FlagSubmitted)!} />
        </Table.Td>
        <Table.Td ff="monospace">
          <Badge size="sm" color="indigo">
            {dayjs(item.time).format('MM/DD HH:mm:ss')}
          </Badge>
        </Table.Td>
        <Table.Td>
          <Text size="sm" fw="bold">
            {item.team ?? 'Team'}
          </Text>
        </Table.Td>
        <Table.Td>
          <Text ff="monospace" size="sm" fw="bold">
            {item.user ?? 'User'}
          </Text>
        </Table.Td>
        <Table.Td>{item.challenge ?? 'Challenge'}</Table.Td>
        <Table.Td
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
            classNames={inputClasses}
          />
        </Table.Td>
      </Table.Tr>
    )
  )

  const onDownloadSubmissionSheet = () =>
    downloadBlob(
      api.game.gameSubmissionSheet(numId, { format: 'blob' }),
      `Submission_${numId}_${Date.now()}.xlsx`,
      setDisabled,
      t
    )

  return (
    <WithGameMonitorTab isLoading={!submissions}>
      <Group justify="space-between" w="100%">
        <SegmentedControl
          color={theme.primaryColor}
          value={type}
          styles={{
            root: {
              background: 'transparent',
            },
          }}
          onChange={(value) => {
            setType(value as AnswerResult | 'All')
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
        <Group justify="right">
          <Tooltip
            label={t('game.button.download.submissionsheet')}
            position="left"
            classNames={tooltipClasses}
          >
            <ActionIcon disabled={disabled} size="lg" onClick={onDownloadSubmissionSheet}>
              <Icon path={mdiDownload} size={1} />
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
        <ScrollArea viewportRef={viewport} offsetScrollbars h="calc(100vh - 200px)">
          <Table className={tableClasses.table}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th style={{ width: '0.6rem' }}>
                  <Group align="center">
                    <Icon path={mdiFlag} size={0.8} />
                  </Group>
                </Table.Th>
                <Table.Th style={{ width: '9rem' }}>{t('common.label.time')}</Table.Th>
                <Table.Th style={{ minWidth: '4.5rem' }}>{t('common.label.team')}</Table.Th>
                <Table.Th style={{ minWidth: '4.5rem' }}>{t('common.label.user')}</Table.Th>
                <Table.Th style={{ minWidth: '3rem' }}>{t('common.label.challenge')}</Table.Th>
                <Table.Th ff="monospace">{t('common.label.flag')}</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>{rows}</Table.Tbody>
          </Table>
        </ScrollArea>
      </Paper>
    </WithGameMonitorTab>
  )
}

export default Submissions
