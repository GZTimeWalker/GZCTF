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
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiArrowLeftBold, mdiArrowRightBold, mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import * as signalR from '@microsoft/signalr'
import cx from 'clsx'
import dayjs from 'dayjs'
import React, { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import AdminPage from '@Components/admin/AdminPage'
import { TaskStatusColorMap } from '@Utils/Shared'
import { useDisplayInputStyles } from '@Utils/ThemeOverride'
import api, { LogMessageModel, TaskStatus } from '@Api'
import tableClasses from '@Styles/Table.module.css'

const ITEM_COUNT_PER_PAGE = 50

enum LogLevel {
  Info = 'Information',
  Warn = 'Warning',
  Error = 'Error',
  All = 'All',
}

const Logs: FC = () => {
  const [level, setLevel] = useState(LogLevel.Info)
  const [activePage, setPage] = useState(1)
  const theme = useMantineTheme()

  const [, update] = useState(new Date())
  const newLogs = useRef<LogMessageModel[]>([])
  const [logs, setLogs] = useState<LogMessageModel[]>()

  const { t } = useTranslation()
  const viewport = useRef<HTMLDivElement>(null)
  const { classes: inputClasses } = useDisplayInputStyles({ fw: 500, ff: 'monospace' })

  useEffect(() => {
    viewport.current?.scrollTo({ top: 0, behavior: 'smooth' })
  }, [activePage, level, viewport])

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
          title: t('admin.notification.logs.fetch_failed'),
          message: err.response.data.title,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
    if (activePage === 1) {
      newLogs.current = []
    }
  }, [activePage, level])

  useEffect(() => {
    setPage(1)
  }, [level])

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
          message: t('admin.notification.logs.connected'),
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

  const rows = [...(activePage === 1 ? newLogs.current : []), ...(logs ?? [])]
    .filter((item) => level === 'All' || item.level === level)
    .map((item, i) => (
      <Table.Tr
        key={`${item.time}@${i}`}
        className={
          i === 0 &&
          activePage === 1 &&
          newLogs.current.length > 0 &&
          newLogs.current[0].level === level
            ? tableClasses.fade
            : undefined
        }
      >
        <Table.Td>
          <Badge size="sm" color="indigo">
            {dayjs(item.time).format('MM/DD HH:mm:ss')}
          </Badge>
        </Table.Td>
        <Table.Td>
          <Input
            variant="unstyled"
            value={item.ip || ''}
            readOnly
            size="sm"
            classNames={inputClasses}
          />
        </Table.Td>
        <Table.Td>
          <Text ff="monospace" size="sm" fw="bold" lineClamp={1}>
            {item.name}
          </Text>
        </Table.Td>
        <Table.Td>
          <Input variant="unstyled" value={item.msg || ''} readOnly size="sm" />
        </Table.Td>
        <Table.Td ff="monospace">
          {item.status && (
            <Badge size="sm" color={TaskStatusColorMap.get(item.status as TaskStatus) ?? 'gray'}>
              {item.status}
            </Badge>
          )}
        </Table.Td>
      </Table.Tr>
    ))

  return (
    <AdminPage
      isLoading={!logs}
      head={
        <>
          <SegmentedControl
            color={theme.primaryColor}
            value={level}
            bg="transparent"
            onChange={(value) => setLevel(value as LogLevel)}
            data={Object.entries(LogLevel).map((role) => ({
              value: role[1],
              label: role[0],
            }))}
          />
          <Group justify="right">
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
        <ScrollArea
          viewportRef={viewport}
          offsetScrollbars
          scrollbarSize={4}
          h="calc(100vh - 190px)"
        >
          <Table className={cx(tableClasses.table, tableClasses.nopadding)}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th style={{ width: '7rem' }}>{t('common.label.time')}</Table.Th>
                <Table.Th style={{ width: '12%' }}>{t('common.label.ip')}</Table.Th>
                <Table.Th style={{ width: '6rem' }}>{t('common.label.user')}</Table.Th>
                <Table.Th>{t('admin.label.logs.message')}</Table.Th>
                <Table.Th style={{ width: '5rem' }}>{t('admin.label.logs.status')}</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>{rows}</Table.Tbody>
          </Table>
        </ScrollArea>
      </Paper>
    </AdminPage>
  )
}

export default Logs
