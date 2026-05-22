import {
  Anchor,
  Badge,
  Center,
  Code,
  Container,
  Group,
  Loader,
  Paper,
  ScrollArea,
  Select,
  Stack,
  Table,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { mdiHammerWrench } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import { FC, useMemo, useState } from 'react'

dayjs.extend(relativeTime)
import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { AdminPage } from '@Components/admin/AdminPage'
import api, { ChallengeBuildStatus } from '@Api'

const STATUS_COLOR: Record<ChallengeBuildStatus, string> = {
  None: 'gray',
  Success: 'teal',
  Failed: 'red',
  Building: 'yellow',
  NotApplicable: 'gray',
  Queued: 'blue',
  MissingDockerfile: 'orange',
}

const STATUS_VARIANT = (s: ChallengeBuildStatus): 'filled' | 'light' =>
  s === 'Failed' ? 'filled' : 'light'

const formatDuration = (ms: number) => {
  if (!ms) return '—'
  if (ms < 1000) return `${ms}ms`
  if (ms < 60_000) return `${(ms / 1000).toFixed(1)}s`
  return `${Math.floor(ms / 60_000)}m ${Math.floor((ms % 60_000) / 1000)}s`
}

const Builds: FC = () => {
  const { t } = useTranslation()
  const [statusFilter, setStatusFilter] = useState<ChallengeBuildStatus | ''>('')

  // Refresh in-progress every 2s; history every 5s (cheap enough and
  // catches new audit rows produced by background scans).
  const { data: inProgress } = api.admin.useAdminListBuildsInProgress(
    { refreshInterval: 2000 },
  )
  const { data: history } = api.admin.useAdminListBuilds(
    { count: 100, status: statusFilter || undefined },
    { refreshInterval: 5000 },
  )

  const statusOptions = useMemo(
    () => [
      { value: '', label: t('admin.content.builds.filter.all') },
      { value: 'Queued', label: 'Queued' },
      { value: 'Building', label: 'Building' },
      { value: 'Success', label: 'Success' },
      { value: 'Failed', label: 'Failed' },
      { value: 'MissingDockerfile', label: 'MissingDockerfile' },
      { value: 'NotApplicable', label: 'NotApplicable' },
    ],
    [t],
  )

  return (
    <AdminPage isLoading={!history}>
      <Container size="xl" mt="md">
        <Stack gap="lg">
          <Group justify="space-between" align="flex-end">
            <Stack gap={0}>
              <Group gap="xs">
                <Icon path={mdiHammerWrench} size={1} />
                <Title order={2}>{t('admin.content.builds.title')}</Title>
              </Group>
              <Text c="dimmed">{t('admin.content.builds.subtitle')}</Text>
            </Stack>
            <Select
              size="xs"
              w={220}
              data={statusOptions}
              value={statusFilter}
              onChange={(v) => setStatusFilter((v ?? '') as ChallengeBuildStatus | '')}
              placeholder={t('admin.content.builds.filter.placeholder')}
              clearable
            />
          </Group>

          <Stack gap={6}>
            <Title order={5}>{t('admin.content.builds.in_progress_title')}</Title>
            {!inProgress ? (
              <Center py="sm">
                <Loader size="xs" />
              </Center>
            ) : inProgress.length === 0 ? (
              <Text size="sm" c="dimmed">{t('admin.content.builds.no_in_progress')}</Text>
            ) : (
              <Paper p="xs" withBorder>
                <Stack gap={4}>
                  {inProgress.map((b) => (
                    <Group key={b.auditId} gap="sm" justify="space-between" wrap="nowrap">
                      <Group gap="xs" wrap="nowrap" miw={0}>
                        <Loader size="xs" />
                        <Anchor
                          component={Link}
                          to={`/admin/games/${b.gameId}/challenges`}
                          size="sm"
                          fw="bold"
                          truncate
                        >
                          {b.slug}
                        </Anchor>
                        <Badge size="xs" color="gray" variant="light">
                          {t('admin.content.builds.attempt', { n: b.attempt })}
                        </Badge>
                        <Badge size="xs" color="blue" variant="light">
                          {b.trigger}
                        </Badge>
                      </Group>
                      <Text size="xs" c="dimmed" ff="monospace">
                        {dayjs(b.startedAtUtc).fromNow()}
                      </Text>
                    </Group>
                  ))}
                </Stack>
              </Paper>
            )}
          </Stack>

          {!history || history.length === 0 ? (
            <Center h="30vh">
              <Stack gap={0} align="center">
                <Title order={4}>{t('admin.content.builds.empty_title')}</Title>
                <Text c="dimmed">{t('admin.content.builds.empty')}</Text>
              </Stack>
            </Center>
          ) : (
            <Paper p="xs" withBorder>
              <ScrollArea>
                <Table withTableBorder striped highlightOnHover>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>{t('admin.content.builds.column.when')}</Table.Th>
                      <Table.Th>{t('admin.content.builds.column.challenge')}</Table.Th>
                      <Table.Th>{t('admin.content.builds.column.trigger')}</Table.Th>
                      <Table.Th>{t('admin.content.builds.column.attempt')}</Table.Th>
                      <Table.Th>{t('admin.content.builds.column.status')}</Table.Th>
                      <Table.Th>{t('admin.content.builds.column.duration')}</Table.Th>
                      <Table.Th>{t('admin.content.builds.column.detail')}</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {history.map((b) => (
                      <Table.Tr key={b.id}>
                        <Table.Td>
                          <Stack gap={0}>
                            <Text size="sm">{dayjs(b.enqueuedAtUtc).fromNow()}</Text>
                            <Text size="xs" c="dimmed" ff="monospace">
                              {dayjs(b.enqueuedAtUtc).format('YYYY-MM-DD HH:mm')}
                            </Text>
                          </Stack>
                        </Table.Td>
                        <Table.Td>
                          <Anchor
                            component={Link}
                            to={`/admin/games/${b.gameId}/challenges`}
                            size="sm"
                            fw="bold"
                          >
                            {b.challengeTitle || `#${b.challengeId}`}
                          </Anchor>
                        </Table.Td>
                        <Table.Td>
                          <Badge size="xs" color="gray" variant="light">{b.trigger}</Badge>
                        </Table.Td>
                        <Table.Td>
                          <Text size="sm" ff="monospace">{b.attempt}</Text>
                        </Table.Td>
                        <Table.Td>
                          <Badge
                            size="sm"
                            color={STATUS_COLOR[b.status]}
                            variant={STATUS_VARIANT(b.status)}
                          >
                            {b.status}
                          </Badge>
                        </Table.Td>
                        <Table.Td>
                          <Text size="sm" ff="monospace">{formatDuration(b.durationMs)}</Text>
                        </Table.Td>
                        <Table.Td maw={420}>
                          {b.errorMessage ? (
                            <Tooltip label={b.errorMessage} multiline w={400}>
                              <Code c="red">{b.errorMessage.slice(0, 80)}{b.errorMessage.length > 80 ? '…' : ''}</Code>
                            </Tooltip>
                          ) : b.digest ? (
                            <Code>{b.digest.slice(0, 19)}</Code>
                          ) : (
                            <Text size="xs" c="dimmed">—</Text>
                          )}
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

export default Builds
