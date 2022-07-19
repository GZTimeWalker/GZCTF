import * as signalR from '@microsoft/signalr';
import { FC, useEffect, useState, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { Stack, Group, Table, createStyles, ActionIcon, keyframes, Badge } from '@mantine/core';
import { showNotification } from '@mantine/notifications';
import { mdiCheck, mdiClose, mdiArrowRightBold, mdiArrowLeftBold } from '@mdi/js';
import Icon from '@mdi/react';
import api, { LogMessageModel } from '../../Api';

const ITEM_COUNT_PER_PAGE = 30;

const useStyles = createStyles((theme) => ({
  mono: {
    fontFamily: theme.fontFamilyMonospace,
  },
  fade: {
    animation: `${keyframes`0% {opacity:0;} 100% {opacity:1;}`} 0.5s linear`,
  },
}));

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
  const params = useParams();
  const level = params.level ?? 'All';

  const [activePage, setPage] = useState(1);
  const { classes, cx } = useStyles();

  const [, update] = useState(new Date());
  const newLogs = useRef<LogMessageModel[]>([]);
  const [logs, setLogs] = useState<LogMessageModel[]>([]);

  useEffect(() => {
    api.admin
      .adminLogs(level, {
        count: ITEM_COUNT_PER_PAGE,
        skip: (activePage - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((data) => {
        setLogs(data.data);
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '获取日志失败',
          message: err.message,
          icon: <Icon path={mdiClose} size={1} />,
          disallowClose: true,
        });
      });
    if (activePage === 1) {
      newLogs.current = [];
    }
  }, [activePage, level]);

  useEffect(() => {
    let connection = new signalR.HubConnectionBuilder()
      .withUrl('/hub/admin')
      .withHubProtocol(new signalR.JsonHubProtocol())
      .withAutomaticReconnect()
      .build();

    connection.serverTimeoutInMilliseconds = 60 * 1000 * 60 * 24;

    connection.on('ReceivedLog', (message: LogMessageModel) => {
      console.log(message);
      newLogs.current = [message, ...newLogs.current];
      update(new Date(message.time!));
    });

    connection
      .start()
      .then(() => {
        showNotification({
          color: 'teal',
          message: '实时日志连接成功',
          icon: <Icon path={mdiCheck} size={1} />,
        });
      })
      .catch((error) => {
        console.error(error);
      });

    return () => {
      connection.stop().catch(() => {});
    };
  }, []);

  const rows = [...(activePage === 1 ? newLogs.current : []), ...logs!].map((item, i) => (
    <tr
      key={`${item.time}@${i}`}
      className={
        i === 0 && activePage === 1 && newLogs.current.length > 0 ? cx(classes.fade) : undefined
      }
    >
      <td className={cx(classes.mono)}>{formatDate(item.time)}</td>
      <td className={cx(classes.mono)}>{item.name}</td>
      <td>{item.msg}</td>
      <td className={cx(classes.mono)}>{item.status}</td>
      <td className={cx(classes.mono)}>{item.ip}</td>
    </tr>
  ));

  return (
    <Stack>
      <Group position="apart">
        <Badge size="xl" radius="sm" variant="outline">
          第 {activePage} 页
        </Badge>
        <Group position="right">
          <ActionIcon
            size="lg"
            variant="hover"
            disabled={activePage <= 1}
            onClick={() => setPage(activePage - 1)}
          >
            <Icon path={mdiArrowLeftBold} size={1} />
          </ActionIcon>
          <ActionIcon
            size="lg"
            variant="hover"
            disabled={logs ? logs.length < ITEM_COUNT_PER_PAGE : false}
            onClick={() => setPage(activePage + 1)}
          >
            <Icon path={mdiArrowRightBold} size={1} />
          </ActionIcon>
        </Group>
      </Group>
      <Table>
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
    </Stack>
  );
};

export default LogViewer;
