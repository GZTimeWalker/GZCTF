import dayjs from 'dayjs'
import React, { FC, useEffect, useRef, useState } from 'react'
import {
  Group,
  SegmentedControl,
  ActionIcon,
  Table,
  Paper,
  ScrollArea,
  Input,
  createStyles,
  Text,
  Badge,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose, mdiCheck, mdiArrowLeftBold, mdiArrowRightBold } from '@mdi/js'
import { Icon } from '@mdi/react'
import * as signalR from '@microsoft/signalr'
import AdminPage from '@Components/admin/AdminPage'
import { TaskStatusColorMap } from '@Utils/Shared'
import { useTableStyles } from '@Utils/ThemeOverride'
import api, { LogMessageModel, TaskStatus } from '@Api'

const ITEM_COUNT_PER_PAGE = 50

enum LogLevel {
  Info = 'Information',
  Warn = 'Warning',
  Error = 'Error',
  All = 'All',
}

const NoPaddingTable = createStyles(() => ({
  table: {
    padding: 0,
    borderCollapse: 'collapse',
    borderSpacing: 0,
    width: '100%',

    '& tbody tr td': {
      whiteSpace: 'nowrap',
      padding: '0 1rem 0 0',
    },
  },
}))

const Logs: FC = () => {
  const [level, setLevel] = useState(LogLevel.Info)
  const [activePage, setPage] = useState(1)
  const { classes, cx, theme } = useTableStyles()
  const { classes: noPaddingClasses } = NoPaddingTable()

  const [, update] = useState(new Date())
  const newLogs = useRef<LogMessageModel[]>([])
  const [logs, setLogs] = useState<LogMessageModel[]>()

  useEffect(() => {
    api.admin
      .adminLogs({
        level,
        count: ITEM_COUNT_PER_PAGE,
        skip: (activePage - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((data) => {
        setLogs(data.data)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '获取日志失败',
          message: err.response.data.title,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
    if (activePage === 1) {
      newLogs.current = []
    }
  }, [activePage, level])

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hub/admin')
      .withHubProtocol(new signalR.JsonHubProtocol())
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.None)
      .build()

    connection.serverTimeoutInMilliseconds = 60 * 1000 * 60 * 24

    connection.on('ReceivedLog', (message: LogMessageModel) => {
      console.log(message)
      newLogs.current = [message, ...newLogs.current]
      update(new Date(message.time!))
    })

    connection
      .start()
      .then(() => {
        showNotification({
          color: 'teal',
          message: '实时日志连接成功',
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
  }, [])

  const rows = [...(activePage === 1 ? newLogs.current : []), ...(logs ?? [])].map((item, i) => (
    <tr
      key={`${item.time}@${i}`}
      className={
        i === 0 && activePage === 1 && newLogs.current.length > 0 ? cx(classes.fade) : undefined
      }
    >
      <td className={cx(classes.mono)}>
        <Badge size="sm" color="indigo">
          {dayjs(item.time).format('MM/DD HH:mm:ss')}
        </Badge>
      </td>
      <td className={cx(classes.mono)}>
        <Text ff={theme.fontFamilyMonospace} size="sm" fw={300}>
          {item.ip || 'localhost'}
        </Text>
      </td>
      <td className={cx(classes.mono)}>
        <Text ff={theme.fontFamilyMonospace} size="sm" fw="bold" lineClamp={1}>
          {item.name}
        </Text>
      </td>
      <td>
        <Input
          variant="unstyled"
          value={item.msg || ''}
          readOnly
          size="sm"
          sx={() => ({
            input: {
              userSelect: 'none',
              lineHeight: 1,
            },
          })}
        />
      </td>
      <td className={cx(classes.mono)}>
        <Badge size="sm" color={TaskStatusColorMap.get(item.status as TaskStatus) ?? 'gray'}>
          {item.status}
        </Badge>
      </td>
    </tr>
  ))

  return (
    <AdminPage
      isLoading={!logs}
      head={
        <>
          <SegmentedControl
            color="brand"
            value={level}
            styles={{
              root: {
                background: 'transparent',
              },
            }}
            onChange={(value: LogLevel) => setLevel(value)}
            data={Object.entries(LogLevel).map((role) => ({
              value: role[1],
              label: role[0],
            }))}
          />
          <Group position="right">
            <ActionIcon
              size="lg"
              disabled={activePage <= 1}
              onClick={() => setPage(activePage - 1)}
            >
              <Icon path={mdiArrowLeftBold} size={1} />
            </ActionIcon>
            <Text fw="bold" size="sm">
              {activePage}
            </Text>
            <ActionIcon
              size="lg"
              disabled={logs && logs.length < ITEM_COUNT_PER_PAGE}
              onClick={() => setPage(activePage + 1)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="md" w="100%">
        <ScrollArea offsetScrollbars scrollbarSize={4} h="calc(100vh - 190px)">
          <Table className={cx(classes.table, noPaddingClasses.table)}>
            <thead>
              <tr>
                <th style={{ width: '8rem' }}>时间</th>
                <th style={{ width: '10rem' }}>IP</th>
                <th style={{ width: '6rem' }}>用户名</th>
                <th>信息</th>
                <th style={{ width: '3rem' }}>状态</th>
              </tr>
            </thead>
            <tbody>{rows}</tbody>
          </Table>
        </ScrollArea>
      </Paper>
    </AdminPage>
  )
}

export default Logs
