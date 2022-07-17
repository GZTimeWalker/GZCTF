import * as signalR from '@microsoft/signalr';
import { HubConnectionState, LogLevel } from '@microsoft/signalr';
import { FC, useEffect, useState, useRef } from 'react';
import { useRouter } from 'next/router';
import { Stack, Title, Pagination, Group, Table } from '@mantine/core';
import { showNotification, updateNotification } from '@mantine/notifications';
import { mdiCheck, mdiClose } from '@mdi/js';
import Icon from '@mdi/react';
import api, { LogMessageModel } from '../../Api';

const ITEM_COUNT_PER_PAGE = 30;

function formatDate(dateString?: string) {
  const date = new Date(dateString!);
  return (
    `${date.getMonth() + 1}`.padStart(2, '0') +
    '/' +
    `${date.getDate()}`.padStart(2, '0') +
    ' ' +
    `${date.getHours()}`.padStart(2, '0') +
    ':' +
    `${date.getMinutes()}`.padStart(2, '0') +
    ':' +
    `${date.getSeconds()}`.padStart(2, '0')
  );
}

const LogViewer: FC = () => {
  const router = useRouter();
  const level = router.query.level ?? 'All';

  const [activePage, setPage] = useState(1);

  const newLogs = useRef<LogMessageModel[]>([]);
  const [, update] = useState(new Date());

  const { data: logs, error } = api.admin.useAdminLogs(
    level as string,
    {
      count: ITEM_COUNT_PER_PAGE,
      skip: (activePage - 1) * ITEM_COUNT_PER_PAGE,
    },
    {
      refreshInterval: 0,
      revalidateIfStale: false,
      revalidateOnFocus: false,
    }
  );

  useEffect(() => {
    let connection = new signalR.HubConnectionBuilder()
      .withUrl('/hub/admin')
      .withHubProtocol(new signalR.JsonHubProtocol())
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Debug)
      .build();

    connection.on('ReceivedLog', (message: LogMessageModel) => {
      console.log(message);
      newLogs.current = [message, ...newLogs.current];
      update(new Date(message.time!));
    });

    connection.start().catch((error) => {
      console.log(error);
      showNotification({
        color: 'red',
        title: 'signalR 连接失败',
        message: error.message.split(':')[0],
        icon: <Icon path={mdiClose} size={1} />,
      });
    });

    return () => {
      connection.stop().catch(() => {});
    };
  });

  const rows = logs?.map((item, i) => (
    <tr key={`${item.time}@${i}`}>
      <td>{formatDate(item.time)}</td>
      <td>{item.name}</td>
      <td>{item.ip}</td>
      <td>{item.msg}</td>
      <td>{item.status}</td>
    </tr>
  ));

  return (
    <Stack>
      <Group></Group>
      <Table>
        <thead>
          <tr>
            <th>时间</th>
            <th>用户名</th>
            <th>IP</th>
            <th>信息</th>
            <th>状态</th>
          </tr>
        </thead>
        <tbody>{rows}</tbody>
      </Table>
    </Stack>
  );
};

export default LogViewer;
