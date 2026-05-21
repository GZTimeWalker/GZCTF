import {
  ActionIcon,
  Badge,
  Center,
  Code,
  Container,
  Group,
  Paper,
  ScrollArea,
  Stack,
  Table,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDeleteOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import { FC, useState } from 'react'

dayjs.extend(relativeTime)
import { useTranslation } from 'react-i18next'
import { AdminPage } from '@Components/admin/AdminPage'
import { showErrorMsg } from '@Utils/Shared'
import api, { AntiCheatBlockModel } from '@Api'

const AntiCheat: FC = () => {
  const { t } = useTranslation()
  const { data: blocks, mutate } = api.admin.useAdminListAntiCheatBlocks({ count: 200 })
  const [busy, setBusy] = useState(false)

  const onClear = async (b: AntiCheatBlockModel) => {
    setBusy(true)
    try {
      await api.admin.adminClearAntiCheatBlock(b.id)
      showNotification({
        color: 'teal',
        message: t('admin.notification.anti_cheat.cleared'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  return (
    <AdminPage isLoading={!blocks}>
      <Container size="xl" mt="md">
        <Stack gap="lg">
          <Stack gap={0}>
            <Title order={2}>{t('admin.content.anti_cheat.title')}</Title>
            <Text c="dimmed">{t('admin.content.anti_cheat.subtitle')}</Text>
          </Stack>

          {!blocks || blocks.length === 0 ? (
            <Center h="30vh">
              <Stack gap={0} align="center">
                <Title order={4}>{t('admin.content.anti_cheat.empty_title')}</Title>
                <Text c="dimmed">{t('admin.content.anti_cheat.empty')}</Text>
              </Stack>
            </Center>
          ) : (
            <Paper p="xs" withBorder>
              <ScrollArea>
                <Table withTableBorder striped highlightOnHover>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>{t('admin.content.anti_cheat.column.when')}</Table.Th>
                      <Table.Th>{t('admin.content.anti_cheat.column.user')}</Table.Th>
                      <Table.Th>{t('admin.content.anti_cheat.column.kind')}</Table.Th>
                      <Table.Th>{t('admin.content.anti_cheat.column.conflict_with')}</Table.Th>
                      <Table.Th>{t('admin.content.anti_cheat.column.value')}</Table.Th>
                      <Table.Th />
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {blocks.map((b) => (
                      <Table.Tr key={b.id}>
                        <Table.Td>
                          <Stack gap={0}>
                            <Text size="sm">{dayjs(b.occurredAtUtc).fromNow()}</Text>
                            <Text size="xs" c="dimmed" ff="monospace">
                              {dayjs(b.occurredAtUtc).format('YYYY-MM-DD HH:mm')}
                            </Text>
                          </Stack>
                        </Table.Td>
                        <Table.Td>
                          <Text size="sm" fw="bold">{b.userName ?? '—'}</Text>
                        </Table.Td>
                        <Table.Td>
                          <Badge
                            size="sm"
                            color={b.kind === 'Ip' ? 'blue' : 'orange'}
                            variant="light"
                          >
                            {b.kind}
                          </Badge>
                        </Table.Td>
                        <Table.Td>
                          <Text size="sm">{b.conflictUserName ?? '—'}</Text>
                        </Table.Td>
                        <Table.Td>
                          {b.conflictingValue ? (
                            <Code>{b.kind === 'Fingerprint'
                              ? b.conflictingValue.substring(0, 16) + '…'
                              : b.conflictingValue}</Code>
                          ) : '—'}
                        </Table.Td>
                        <Table.Td align="right">
                          <Tooltip label={t('admin.button.anti_cheat.clear')}>
                            <ActionIcon
                              variant="subtle"
                              color="red"
                              disabled={busy}
                              onClick={() => onClear(b)}
                            >
                              <Icon path={mdiDeleteOutline} size={1} />
                            </ActionIcon>
                          </Tooltip>
                        </Table.Td>
                      </Table.Tr>
                    ))}
                  </Table.Tbody>
                </Table>
              </ScrollArea>
            </Paper>
          )}
        </Stack>
      </Container>
    </AdminPage>
  )
}

export default AntiCheat
