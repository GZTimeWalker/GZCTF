import {
  ActionIcon,
  Anchor,
  Badge,
  Button,
  Center,
  Code,
  Container,
  Group,
  NumberInput,
  Paper,
  Stack,
  Switch,
  Text,
  TextInput,
  Title,
  Tooltip,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDeleteOutline, mdiPause, mdiPlay, mdiPlus, mdiRefresh, mdiSourceBranch } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import { FC, useState } from 'react'

dayjs.extend(relativeTime)
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { AdminPage } from '@Components/admin/AdminPage'
import { showErrorMsg } from '@Utils/Shared'
import api, { RepoBindingInfoModel, RepoBindingScanResultModel } from '@Api'

const RepoBindings: FC = () => {
  const { t } = useTranslation()
  const modals = useModals()
  const { data: bindings, mutate } = api.admin.useAdminListRepoBindings()

  const [repoUrl, setRepoUrl] = useState('')
  const [refValue, setRefValue] = useState('')
  const [githubToken, setGithubToken] = useState('')
  const [intervalSeconds, setIntervalSeconds] = useState<number | string>(600)
  const [runImmediately, setRunImmediately] = useState(true)
  const [busy, setBusy] = useState(false)
  const [lastResult, setLastResult] = useState<RepoBindingScanResultModel | null>(null)

  const flash = (r: RepoBindingScanResultModel) => {
    setLastResult(r)
    showNotification({
      color: r.failures === 0 ? 'teal' : 'orange',
      title: t('admin.notification.repo_binding.scanned'),
      message: t('admin.notification.repo_binding.summary', {
        games: r.gamesCreated + r.gamesUpdated,
        challenges: r.challengesImported + r.challengesUpdated,
        failures: r.failures,
      }),
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  const onAdd = async () => {
    if (!repoUrl) return
    setBusy(true)
    setLastResult(null)
    try {
      const resp = await api.admin.adminCreateRepoBinding({
        repoUrl,
        ref: refValue || null,
        githubToken: githubToken || null,
        intervalSeconds: Number(intervalSeconds) || 600,
        runImmediately,
      })
      flash(resp.data)
      setRepoUrl('')
      setRefValue('')
      setGithubToken('')
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  const onScan = async (b: RepoBindingInfoModel) => {
    setBusy(true)
    try {
      const resp = await api.admin.adminScanRepoBinding(b.id)
      flash(resp.data)
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  const onTogglePause = async (b: RepoBindingInfoModel) => {
    setBusy(true)
    try {
      await api.admin.adminUpdateRepoBinding(b.id, {
        status: b.status === 'Active' ? 'Paused' : 'Active',
      })
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  const onDelete = (b: RepoBindingInfoModel) => {
    modals.openConfirmModal({
      title: t('admin.content.repo_binding.delete_title', { repo: b.repoUrl }),
      children: <Text size="sm">{t('admin.content.repo_binding.delete_warning')}</Text>,
      labels: { confirm: t('admin.button.repo_binding.delete'), cancel: t('common.button.cancel') },
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        setBusy(true)
        try {
          await api.admin.adminDeleteRepoBinding(b.id)
          mutate()
        } catch (e) {
          showErrorMsg(e, t)
        } finally {
          setBusy(false)
        }
      },
    })
  }

  return (
    <AdminPage isLoading={!bindings}>
      <Container size="xl" mt="md">
        <Stack gap="lg">
          <Stack gap={0}>
            <Title order={2}>{t('admin.content.repo_binding.title')}</Title>
            <Text c="dimmed">{t('admin.content.repo_binding.subtitle')}</Text>
          </Stack>

          <Paper p="md" withBorder>
            <Stack gap="sm">
              <Title order={5}>{t('admin.content.repo_binding.add')}</Title>
              <TextInput
                label={t('admin.content.repo_binding.repo_url')}
                placeholder="https://github.com/TCP1P/findit-ctf-2026"
                value={repoUrl}
                onChange={(e) => setRepoUrl(e.currentTarget.value)}
              />
              <Group grow>
                <TextInput
                  label={t('admin.content.repo_binding.ref')}
                  placeholder="main"
                  value={refValue}
                  onChange={(e) => setRefValue(e.currentTarget.value)}
                />
                <TextInput
                  label={t('admin.content.repo_binding.token')}
                  description={t('admin.content.repo_binding.token_help')}
                  type="password"
                  placeholder="github_pat_…"
                  value={githubToken}
                  onChange={(e) => setGithubToken(e.currentTarget.value)}
                />
              </Group>
              <Group grow>
                <NumberInput
                  label={t('admin.content.repo_binding.interval')}
                  description={t('admin.content.repo_binding.interval_help')}
                  min={60}
                  max={86400}
                  step={60}
                  value={intervalSeconds}
                  onChange={setIntervalSeconds}
                />
                <Switch
                  label={t('admin.content.repo_binding.run_immediately')}
                  checked={runImmediately}
                  onChange={(e) => setRunImmediately(e.currentTarget.checked)}
                />
              </Group>
              <Group justify="flex-end">
                <Button
                  leftSection={<Icon path={mdiPlus} size={1} />}
                  loading={busy}
                  disabled={!repoUrl}
                  onClick={onAdd}
                >
                  {t('admin.button.repo_binding.add')}
                </Button>
              </Group>
            </Stack>
          </Paper>

          {lastResult && (
            <Paper p="sm" withBorder>
              <Stack gap="xs">
                <Group gap="md">
                  <Badge color="teal" variant="light">games +{lastResult.gamesCreated}</Badge>
                  <Badge color="blue" variant="light">games ~{lastResult.gamesUpdated}</Badge>
                  <Badge color="teal" variant="light">challenges +{lastResult.challengesImported}</Badge>
                  <Badge color="blue" variant="light">challenges ~{lastResult.challengesUpdated}</Badge>
                  <Badge color={lastResult.failures > 0 ? 'red' : 'gray'} variant="light">
                    failures {lastResult.failures}
                  </Badge>
                </Group>
                {lastResult.messages.length > 0 && (
                  <Stack gap={2}>
                    {lastResult.messages.slice(0, 12).map((m, i) => (
                      <Code key={i} block style={{ whiteSpace: 'pre-wrap', fontSize: 11 }}>
                        {m}
                      </Code>
                    ))}
                    {lastResult.messages.length > 12 && (
                      <Text size="xs" c="dimmed">
                        …and {lastResult.messages.length - 12} more
                      </Text>
                    )}
                  </Stack>
                )}
              </Stack>
            </Paper>
          )}

          {!bindings || bindings.length === 0 ? (
            <Center h="30vh">
              <Stack gap={0} align="center">
                <Title order={4}>{t('admin.content.repo_binding.empty_title')}</Title>
                <Text c="dimmed">{t('admin.content.repo_binding.empty')}</Text>
              </Stack>
            </Center>
          ) : (
            <Stack gap="md">
              {bindings.map((b) => (
                <Paper key={b.id} p="md" withBorder>
                  <Stack gap="sm">
                    {/* Header: repo URL + PAT chip on the left; status + interval + actions on the right */}
                    <Group justify="space-between" wrap="nowrap" align="flex-start">
                      <Group gap="xs" wrap="nowrap" miw={0} style={{ flex: 1 }}>
                        <Icon path={mdiSourceBranch} size={1} />
                        <Tooltip label={b.repoUrl}>
                          <Text size="sm" ff="monospace" truncate fw="bold">
                            {b.repoUrl.replace('https://github.com/', '')}
                          </Text>
                        </Tooltip>
                        {b.hasGitHubToken && (
                          <Tooltip label={t('admin.content.repo_binding.has_token')}>
                            <Badge size="xs" color="gray" variant="light">PAT</Badge>
                          </Tooltip>
                        )}
                      </Group>
                      <Group gap="xs" wrap="nowrap">
                        <Badge color={b.status === 'Active' ? 'teal' : 'gray'} variant="filled">
                          {b.status}
                        </Badge>
                        <Badge color="gray" variant="light">
                          {b.intervalSeconds}s
                        </Badge>
                        <Tooltip label={t('admin.button.repo_binding.scan')}>
                          <ActionIcon variant="subtle" disabled={busy} onClick={() => onScan(b)}>
                            <Icon path={mdiRefresh} size={1} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label={t(
                          b.status === 'Active'
                            ? 'admin.button.repo_binding.pause'
                            : 'admin.button.repo_binding.resume',
                        )}>
                          <ActionIcon variant="subtle" disabled={busy} onClick={() => onTogglePause(b)}>
                            <Icon path={b.status === 'Active' ? mdiPause : mdiPlay} size={1} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label={t('admin.button.repo_binding.delete')}>
                          <ActionIcon variant="subtle" color="red" disabled={busy} onClick={() => onDelete(b)}>
                            <Icon path={mdiDeleteOutline} size={1} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Group>

                    {/* Subheader: ref + event count */}
                    <Text size="xs" c="dimmed">
                      {t('admin.content.repo_binding.card.ref_label')}: {b.ref ?? 'default'}
                      {' · '}
                      {t('admin.content.repo_binding.card.events_count', { count: b.games.length })}
                    </Text>

                    {/* Child games */}
                    {b.games.length === 0 ? (
                      <Text size="xs" c="dimmed">
                        {t('admin.content.repo_binding.no_games')}
                      </Text>
                    ) : (
                      <Stack gap={4}>
                        {b.games.map((g) => (
                          <Group key={g.id} gap="xs" wrap="nowrap">
                            <Anchor
                              component={Link}
                              to={`/admin/games/${g.id}`}
                              size="sm"
                            >
                              {g.title}
                            </Anchor>
                            {g.eventManifestPath && (
                              <Badge size="xs" variant="outline" color="gray">
                                <Text size="xs" ff="monospace">{g.eventManifestPath}</Text>
                              </Badge>
                            )}
                          </Group>
                        ))}
                      </Stack>
                    )}

                    {/* Footer: timing + commit */}
                    <Group gap="md" wrap="wrap">
                      <Text size="xs" c="dimmed">
                        {b.lastScanUtc
                          ? `${t('admin.content.repo_binding.card.last_scan')} ${dayjs(b.lastScanUtc).fromNow()}`
                          : t('admin.content.repo_binding.card.never_scanned')}
                      </Text>
                      <Text size="xs" c="dimmed">
                        {b.status === 'Paused'
                          ? t('admin.content.repo_binding.paused_short')
                          : b.nextScanUtc
                            ? `${t('admin.content.repo_binding.card.next_scan')} ${dayjs(b.nextScanUtc).fromNow()}`
                            : t('admin.content.repo_binding.due_now')}
                      </Text>
                      {b.lastCommitSha && (
                        <Text size="xs" c="dimmed">
                          {t('admin.content.repo_binding.card.commit')}: <Code>{b.lastCommitSha.substring(0, 7)}</Code>
                        </Text>
                      )}
                    </Group>

                    {b.lastScanMessage && (
                      <Text size="xs" c="dimmed" lineClamp={2} ff="monospace">
                        {b.lastScanMessage}
                      </Text>
                    )}
                  </Stack>
                </Paper>
              ))}
            </Stack>
          )}
        </Stack>
      </Container>
    </AdminPage>
  )
}

export default RepoBindings
