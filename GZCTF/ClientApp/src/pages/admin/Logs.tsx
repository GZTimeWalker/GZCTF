import * as signalR from '@microsoft/signalr'
import dayjs from 'dayjs'
import React, { FC, useEffect, useRef, useState } from 'react'
import { Group, SegmentedControl, ActionIcon, Table, Paper, ScrollArea } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose, mdiCheck, mdiArrowLeftBold, mdiArrowRightBold } from '@mdi/js'
import { Icon } from '@mdi/react'
import AdminPage from '@Components/admin/AdminPage'
import { useTableStyles } from '@Utils/ThemeOverride'
import api, { LogMessageModel } from '@Api'

const ITEM_COUNT_PER_PAGE = 30

enum LogLevel {
  Info = 'Information',
  Warn = 'Warning',
  Error = 'Error',
  All = 'All',
}

const Logs: FC = () => {
  const [level, setLevel] = useState(LogLevel.Info)
  const [activePage, setPage] = useState(1)
  const { classes, cx } = useTableStyles()

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
          disallowClose: true,
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
  }, [])

  const rows = [...(activePage === 1 ? newLogs.current : []), ...(logs ?? [])].map((item, i) => (
    <tr
      key={`${item.time}@${i}`}
      className={
        i === 0 && activePage === 1 && newLogs.current.length > 0 ? cx(classes.fade) : undefined
      }
    >
      <td className={cx(classes.mono)}>{dayjs(item.time).format('MM/DD HH:mm:ss')}</td>
      <td className={cx(classes.mono)}>{item.name}</td>
      <td>{item.msg}</td>
      <td className={cx(classes.mono)}>{item.status}</td>
      <td className={cx(classes.mono)}>{item.ip}</td>
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
      <Paper shadow="md" p="md">
        <ScrollArea offsetScrollbars scrollbarSize={4} style={{ height: 'calc(100vh - 190px)' }}>
          <Table className={classes.table}>
            <thead>
              <tr>
                <th>时间</th>
                <th>用户名</th>
                <th>信息</th>
                <th>状态</th>
                <th>IP</th>
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
