import {
  ActionIcon,
  Anchor,
  Badge,
  Button,
  Center,
  Code,
  Container,
  Group,
  Loader,
  Modal,
  Paper,
  ScrollArea,
  Select,
  Stack,
  Table,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import {
  mdiCheck,
  mdiContentCopy,
  mdiDeleteOutline,
  mdiHammerWrench,
  mdiImageBrokenVariant,
  mdiRefresh,
  mdiTextBoxOutline,
  mdiTrashCanOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import { FC, useMemo, useState } from 'react'

dayjs.extend(relativeTime)
import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { AdminPage } from '@Components/admin/AdminPage'
import { showErrorMsg } from '@Utils/Shared'
import api, { ChallengeBuildAuditModel, ChallengeBuildStatus } from '@Api'

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
  const modals = useModals()
  const clipboard = useClipboard({ timeout: 1500 })
  const [statusFilter, setStatusFilter] = useState<ChallengeBuildStatus | ''>('')
  const [busy, setBusy] = useState(false)
  const [logRow, setLogRow] = useState<ChallengeBuildAuditModel | null>(null)

  // Refresh in-progress every 2s; history every 5s (cheap enough and
  // catches new audit rows produced by background scans).
  const { data: inProgress } = api.admin.useAdminListBuildsInProgress(
    { refreshInterval: 2000 },
  )
  const { data: history, mutate: mutateHistory } = api.admin.useAdminListBuilds(
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

  const failedCount = useMemo(
    () => history?.filter((b) => b.status === 'Failed').length ?? 0,
    [history],
  )

  const onDelete = (row: ChallengeBuildAuditModel) => {
    modals.openConfirmModal({
      title: t('admin.button.builds.delete'),
      children: (
        <Text size="sm">
          {t('admin.content.builds.confirm_delete', { challenge: row.challengeTitle })}
        </Text>
      ),
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        setBusy(true)
        try {
          await api.admin.adminDeleteBuildAudit(row.id)
          mutateHistory()
        } catch (e) { showErrorMsg(e, t) }
        finally { setBusy(false) }
      },
    })
  }

  const onReenqueue = async (row: ChallengeBuildAuditModel) => {
    setBusy(true)
    try {
      await api.admin.adminReenqueueBuild(row.id)
      showNotification({
        color: 'teal',
        message: t('admin.notification.builds.enqueued'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutateHistory()
    } catch (e) { showErrorMsg(e, t) }
    finally { setBusy(false) }
  }

  const onPruneFailed = () => {
    if (failedCount === 0) return
    modals.openConfirmModal({
      title: t('admin.button.builds.prune_failed'),
      children: (
        <Text size="sm">
          {t('admin.content.builds.confirm_prune_failed', { count: failedCount })}
        </Text>
      ),
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        setBusy(true)
        try {
          const resp = await api.admin.adminPruneFailedBuildAudits()
          showNotification({
            color: 'teal',
            message: t('admin.notification.builds.pruned', { count: resp.data.removed }),
            icon: <Icon path={mdiCheck} size={1} />,
          })
          mutateHistory()
        } catch (e) { showErrorMsg(e, t) }
        finally { setBusy(false) }
      },
    })
  }

  const onPruneImages = () => {
    modals.openConfirmModal({
      title: t('admin.button.builds.prune_images'),
      children: (
        <Stack gap={4}>
          <Text size="sm">{t('admin.content.builds.confirm_prune_images.line1')}</Text>
          <Text size="xs" c="dimmed">{t('admin.content.builds.confirm_prune_images.line2')}</Text>
        </Stack>
      ),
      confirmProps: { color: 'orange' },
      onConfirm: async () => {
        setBusy(true)
        try {
          const resp = await api.admin.adminPruneOrphanBuildImages()
          showNotification({
            color: 'teal',
            message: t('admin.notification.builds.images_pruned', { count: resp.data.removed }),
            icon: <Icon path={mdiCheck} size={1} />,
          })
        } catch (e) { showErrorMsg(e, t) }
        finally { setBusy(false) }
      },
    })
  }

  return (
    <AdminPage isLoading={!history}>
      <Container size="xl" mt="md">
        <Stack gap="lg">
          <Group justify="space-between" align="flex-end" wrap="wrap">
            <Stack gap={0}>
              <Group gap="xs">
                <Icon path={mdiHammerWrench} size={1} />
                <Title order={2}>{t('admin.content.builds.title')}</Title>
              </Group>
              <Text c="dimmed">{t('admin.content.builds.subtitle')}</Text>
            </Stack>
            <Group gap="xs">
              <Button
                size="xs"
                variant="default"
                color="red"
                leftSection={<Icon path={mdiTrashCanOutline} size={0.9} />}
                onClick={onPruneFailed}
                disabled={busy || failedCount === 0}
              >
                {t('admin.button.builds.prune_failed')} ({failedCount})
              </Button>
              <Button
                size="xs"
                variant="default"
                color="orange"
                leftSection={<Icon path={mdiImageBrokenVariant} size={0.9} />}
                onClick={onPruneImages}
                disabled={busy}
              >
                {t('admin.button.builds.prune_images')}
              </Button>
              <Select
                size="xs"
                w={200}
                data={statusOptions}
                value={statusFilter}
                onChange={(v) => setStatusFilter((v ?? '') as ChallengeBuildStatus | '')}
                placeholder={t('admin.content.builds.filter.placeholder')}
                clearable
              />
            </Group>
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
                      <Table.Th />
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
                        <Table.Td>
                          <Group gap={4} wrap="nowrap" justify="flex-end">
                            <Tooltip label={t('admin.button.builds.view_log')}>
                              <ActionIcon
                                variant="subtle"
                                disabled={!b.logTail}
                                onClick={() => setLogRow(b)}
                              >
                                <Icon path={mdiTextBoxOutline} size={0.9} />
                              </ActionIcon>
                            </Tooltip>
                            {(b.status === 'Failed' || b.status === 'MissingDockerfile') && (
                              <Tooltip label={t('admin.button.builds.reenqueue')}>
                                <ActionIcon
                                  variant="subtle"
                                  color="blue"
                                  disabled={busy}
                                  onClick={() => onReenqueue(b)}
                                >
                                  <Icon path={mdiRefresh} size={0.9} />
                                </ActionIcon>
                              </Tooltip>
                            )}
                            <Tooltip label={t('admin.button.builds.delete')}>
                              <ActionIcon
                                variant="subtle"
                                color="red"
                                disabled={busy}
                                onClick={() => onDelete(b)}
                              >
                                <Icon path={mdiDeleteOutline} size={0.9} />
                              </ActionIcon>
                            </Tooltip>
                          </Group>
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

      <Modal
        size="xl"
        opened={logRow !== null}
        onClose={() => setLogRow(null)}
        title={
          <Group gap="xs">
            <Icon path={mdiTextBoxOutline} size={1} />
            <Text fw={700}>{t('admin.content.builds.log_modal_title')}</Text>
            {logRow && (
              <Text size="sm" c="dimmed">
                {logRow.challengeTitle} — {dayjs(logRow.enqueuedAtUtc).format('YYYY-MM-DD HH:mm')}
              </Text>
            )}
          </Group>
        }
      >
        {logRow && (
          <Stack gap="xs">
            <Group gap="xs">
              <Badge
                color={STATUS_COLOR[logRow.status]}
                variant={STATUS_VARIANT(logRow.status)}
              >
                {logRow.status}
              </Badge>
              <Badge variant="light" color="gray">{logRow.trigger}</Badge>
              <Badge variant="light" color="gray">
                {t('admin.content.builds.attempt', { n: logRow.attempt })}
              </Badge>
              <Badge variant="light" color="gray" ff="monospace">
                {formatDuration(logRow.durationMs)}
              </Badge>
              <Button
                size="xs"
                variant="default"
                ml="auto"
                leftSection={<Icon path={clipboard.copied ? mdiCheck : mdiContentCopy} size={0.8} />}
                onClick={() => clipboard.copy(logRow.logTail ?? '')}
                disabled={!logRow.logTail}
              >
                {clipboard.copied
                  ? t('admin.button.builds.copied')
                  : t('admin.button.builds.copy')}
              </Button>
            </Group>
            {logRow.errorMessage && (
              <Code c="red" block style={{ whiteSpace: 'pre-wrap', fontSize: 12 }}>
                {logRow.errorMessage}
              </Code>
            )}
            <Code
              block
              style={{
                whiteSpace: 'pre-wrap',
                maxHeight: '60vh',
                overflowY: 'auto',
                fontSize: 11,
              }}
            >
              {logRow.logTail || t('admin.content.builds.no_log')}
            </Code>
          </Stack>
        )}
      </Modal>
    </AdminPage>
  )
}

export default Builds
