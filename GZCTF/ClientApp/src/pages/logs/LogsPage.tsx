import { FC, useEffect, useRef, useState } from 'react';
import { ADMIN_API, PuzzleLog } from '../../redux/admin.api';
import { LoadingMask } from '../../common/components/LoadingMask';
import {
  Button,
  Flex,
  HStack,
  IconButton,
  Input,
  Popover,
  PopoverArrow,
  PopoverBody,
  PopoverCloseButton,
  PopoverContent,
  PopoverTrigger,
  Table,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
  useToast,
  keyframes
} from '@chakra-ui/react';
import * as signalR from '@microsoft/signalr';
import { BackIcon } from '../../common/components/BackIcon';
import { ForwardIcon } from '../../common/components/ForwardIcon';
import { AimIcon } from '../../common/components/AimIcon';

function formatDate(dateString: string) {
  const date = new Date(dateString);
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

const ITEM_COUNT_PER_PAGE = 30;

export const LogsPage: FC = () => {
  const [page, setPage] = useState(1);
  const { data, isLoading, error } = ADMIN_API.useGetLogsQuery({
    count: ITEM_COUNT_PER_PAGE,
    skip: (page - 1) * ITEM_COUNT_PER_PAGE
  });
  const [, update] = useState(new Date());
  const newLogs = useRef<PuzzleLog[]>([]);
  const pageInputRef = useRef<HTMLInputElement>(null);
  const toast = useToast();
  const fadeAnimation = `${keyframes`
  0% {opacity:0;}
  100% {opacity:1;}
  `} 0.5s linear`;

  useEffect(() => {
    let connection = new signalR.HubConnectionBuilder()
      .withUrl('/hub/log')
      .withHubProtocol(new signalR.JsonHubProtocol())
      .withAutomaticReconnect()
      .build();

    connection.serverTimeoutInMilliseconds = 60 * 1000 * 60 * 24;

    connection.on('ReceivedLog', (message: PuzzleLog) => {
      console.log(message);
      newLogs.current = [message, ...newLogs.current];
      update(new Date(message.time));
    });

    connection.start().catch((error) => {
      toast({
        title: 'signalR 连接失败',
        description: error.message,
        status: 'error',
        duration: 5000
      });
    });

    return () => {
      connection.stop().catch(() => {});
    };
  }, [toast]);

  if (isLoading) {
    return <LoadingMask />;
  }

  if (error) {
    return <LoadingMask error={error} />;
  }

  return (
    <Flex flexDirection="column" maxHeight="100vh" p="56px">
      <Flex flex="none" p="24px" flexDirection="row-reverse" alignItems="center">
        <HStack spacing="12px">
          <IconButton
            variant="outline"
            aria-label="Previous page"
            icon={<BackIcon />}
            disabled={page <= 1}
            onClick={() => setPage(page - 1)}
          />
          <IconButton
            variant="outline"
            aria-label="Previous page"
            icon={<ForwardIcon />}
            disabled={data!.length < ITEM_COUNT_PER_PAGE}
            onClick={() => setPage(page + 1)}
          />
          <Popover>
            <PopoverTrigger>
              <IconButton variant="outline" aria-label="Jump to page" icon={<AimIcon />} />
            </PopoverTrigger>
            <PopoverContent maxWidth="12rem">
              <PopoverArrow />
              <PopoverCloseButton />
              <PopoverBody p="12px">
                <Text fontSize="sm" mb="4px">
                  跳转到指定页
                </Text>
                <HStack
                  as="form"
                  onSubmit={(event) => {
                    event.preventDefault();
                    setPage(pageInputRef!.current!.valueAsNumber);
                  }}
                >
                  <Input type="number" ref={pageInputRef} size="sm" />
                  <Button size="sm" type="submit">
                    确认
                  </Button>
                </HStack>
              </PopoverBody>
            </PopoverContent>
          </Popover>
        </HStack>
        <Text mr="24px">第 {page} 页</Text>
      </Flex>
      <Table flex="1" size="sm" maxWidth="100%">
        <Thead>
          <Tr>
            <Th>时间</Th>
            <Th>用户</Th>
            <Th>IP</Th>
            <Th>信息</Th>
            <Th>状态</Th>
          </Tr>
        </Thead>
        <Tbody fontSize="xs">
          {[...(page === 1 ? newLogs.current : []), ...data!].map((item, index) =>
            index === 0 && page === 1 && newLogs.current.length > 0 ? (
              <Tr key={page + item.time + index} animation={fadeAnimation}>
                <Td fontFamily="mono">{formatDate(item.time)}</Td>
                <Td fontFamily="mono">{item.name}</Td>
                <Td fontFamily="mono">{item.ip}</Td>
                <Td>{item.msg}</Td>
                <Td fontFamily="mono">{item.status}</Td>
              </Tr>
            ) : (
              <Tr key={page + item.time + index}>
                <Td fontFamily="mono">{formatDate(item.time)}</Td>
                <Td fontFamily="mono">{item.name}</Td>
                <Td fontFamily="mono">{item.ip}</Td>
                <Td>{item.msg}</Td>
                <Td fontFamily="mono">{item.status}</Td>
              </Tr>
            )
          )}
        </Tbody>
      </Table>
    </Flex>
  );
};
