import {
  ActionIcon,
  Badge,
  Box,
  Button,
  Center,
  Code,
  Group,
  NumberInput,
  Paper,
  Stack,
  Switch,
  Table,
  Text,
  TextInput,
  Title,
  Tooltip,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDeleteOutline, mdiKeyOutline, mdiPause, mdiPlay, mdiPlus, mdiRefresh } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import { FC, useState } from 'react'

dayjs.extend(relativeTime)
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import api, { RepoWatchInfoModel } from '@Api'

const Watches: FC = () => {
  const { id } = useParams()
  const gameId = parseInt(id ?? '-1')
  const { t } = useTranslation()

  const { data: watches, mutate } = api.edit.useEditListRepoWatches(gameId, undefined, gameId > 0)

  const [repoUrl, setRepoUrl] = useState('')
  const [refValue, setRefValue] = useState('')
  const [subpath, setSubpath] = useState('')
  const [interval, setInterval] = useState<number>(600)
  const [runImmediately, setRunImmediately] = useState(true)
  const [githubToken, setGithubToken] = useState('')
  const [busy, setBusy] = useState(false)

  const onCreate = async () => {
    if (!repoUrl) return
    setBusy(true)
    try {
      await api.edit.editCreateRepoWatch(gameId, {
        repoUrl,
        ref: refValue || null,
        subpath: subpath || null,
        intervalSeconds: interval,
        runImmediately,
        githubToken: githubToken || null,
      })
      showNotification({ color: 'teal', message: t('admin.notification.watch.created'), icon: <Icon path={mdiCheck} size={1} /> })
      setRepoUrl('')
      setRefValue('')
      setSubpath('')
      setGithubToken('')
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  const onUpdateToken = async (w: RepoWatchInfoModel) => {
    const value = window.prompt(t('admin.content.watches.token_prompt'))
    if (value === null) return
    setBusy(true)
    try {
      await api.edit.editUpdateRepoWatch(gameId, w.id, { githubToken: value })
      showNotification({ color: 'teal', message: t('admin.notification.watch.token_updated'), icon: <Icon path={mdiCheck} size={1} /> })
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  const onTogglePause = async (w: RepoWatchInfoModel) => {
    setBusy(true)
    try {
      await api.edit.editUpdateRepoWatch(gameId, w.id, { status: w.status === 'Active' ? 'Paused' : 'Active' })
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  const onRunNow = async (w: RepoWatchInfoModel) => {
    setBusy(true)
    try {
      await api.edit.editRunRepoWatchNow(gameId, w.id)
      showNotification({ color: 'teal', message: t('admin.notification.watch.run_queued'), icon: <Icon path={mdiCheck} size={1} /> })
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  const onDelete = async (w: RepoWatchInfoModel) => {
    setBusy(true)
    try {
      await api.edit.editDeleteRepoWatch(gameId, w.id)
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  return (
    <WithGameEditTab isLoading={!watches}>
      <Stack gap="md" w="100%">
        <Title order={3}>{t('admin.content.watches.title')}</Title>

        <Paper p="md" withBorder>
          <Stack gap="sm">
            <Title order={5}>{t('admin.content.watches.add')}</Title>
            <TextInput
              label={t('admin.content.watches.repo_url')}
              placeholder="https://github.com/TCP1P/TCP1P-CTF-2024-Challenges-Public"
              value={repoUrl}
              onChange={(e) => setRepoUrl(e.currentTarget.value)}
            />
            <Group grow>
              <TextInput
                label={t('admin.content.watches.ref')}
                placeholder="main"
                value={refValue}
                onChange={(e) => setRefValue(e.currentTarget.value)}
              />
              <TextInput
                label={t('admin.content.watches.subpath')}
                placeholder="quals"
                value={subpath}
                onChange={(e) => setSubpath(e.currentTarget.value)}
              />
              <NumberInput
                label={t('admin.content.watches.interval')}
                min={60}
                max={86400}
                step={60}
                value={interval}
                onChange={(v) => setInterval(typeof v === 'number' ? v : 600)}
              />
            </Group>
            <TextInput
              label={t('admin.content.watches.token')}
              description={t('admin.content.watches.token_help')}
              placeholder="github_pat_…"
              type="password"
              value={githubToken}
              onChange={(e) => setGithubToken(e.currentTarget.value)}
            />
            <Group justify="space-between">
              <Switch
                label={t('admin.content.watches.run_immediately')}
                checked={runImmediately}
                onChange={(e) => setRunImmediately(e.currentTarget.checked)}
              />
              <Button leftSection={<Icon path={mdiPlus} size={1} />} loading={busy} disabled={!repoUrl} onClick={onCreate}>
                {t('admin.button.watches.add')}
              </Button>
            </Group>
          </Stack>
        </Paper>

        {!watches || watches.length === 0 ? (
          <Center h="20vh">
            <Text c="dimmed">{t('admin.content.watches.empty')}</Text>
          </Center>
        ) : (
          <Table withTableBorder striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('admin.content.watches.column.repo')}</Table.Th>
                <Table.Th>{t('admin.content.watches.column.ref')}</Table.Th>
                <Table.Th>{t('admin.content.watches.column.subpath')}</Table.Th>
                <Table.Th>{t('admin.content.watches.column.interval')}</Table.Th>
                <Table.Th>{t('admin.content.watches.column.last_run')}</Table.Th>
                <Table.Th>{t('admin.content.watches.column.commit')}</Table.Th>
                <Table.Th>{t('admin.content.watches.column.status')}</Table.Th>
                <Table.Th>{t('admin.content.watches.column.actions')}</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {watches.map((w) => (
                <Table.Tr key={w.id}>
                  <Table.Td><Text size="sm" ff="monospace">{w.repoUrl.replace('https://github.com/', '')}</Text></Table.Td>
                  <Table.Td>{w.ref ?? <Text c="dimmed">default</Text>}</Table.Td>
                  <Table.Td>{w.subpath ?? '—'}</Table.Td>
                  <Table.Td>{Math.round(w.intervalSeconds / 60)}m</Table.Td>
                  <Table.Td>
                    {w.lastRunUtc ? (
                      <Stack gap={0}>
                        <Text size="xs">{dayjs(w.lastRunUtc).fromNow()}</Text>
                        {w.lastSync && (
                          <Text size="xs" c="dimmed">
                            +{w.lastSync.imported} / ~{w.lastSync.updated} / {w.lastSync.skipped}s / {w.lastSync.failed}f
                          </Text>
                        )}
                      </Stack>
                    ) : '—'}
                  </Table.Td>
                  <Table.Td>{w.lastCommitSha ? <Code>{w.lastCommitSha.substring(0, 7)}</Code> : '—'}</Table.Td>
                  <Table.Td>
                    <Group gap={4} wrap="nowrap">
                      <Badge color={w.status === 'Active' ? 'teal' : 'gray'}>{w.status}</Badge>
                      {w.hasGitHubToken && (
                        <Tooltip label={t('admin.content.watches.has_token')}>
                          <Box component="span" style={{ display: 'inline-flex' }}>
                            <Icon path={mdiKeyOutline} size={0.8} />
                          </Box>
                        </Tooltip>
                      )}
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Group gap={2} wrap="nowrap">
                      <Tooltip label={t('admin.button.watches.run')}>
                        <ActionIcon variant="subtle" disabled={busy} onClick={() => onRunNow(w)}>
                          <Icon path={mdiRefresh} size={1} />
                        </ActionIcon>
                      </Tooltip>
                      <Tooltip label={t('admin.button.watches.token')}>
                        <ActionIcon variant="subtle" disabled={busy} onClick={() => onUpdateToken(w)}>
                          <Icon path={mdiKeyOutline} size={1} />
                        </ActionIcon>
                      </Tooltip>
                      <Tooltip label={w.status === 'Active' ? t('admin.button.watches.pause') : t('admin.button.watches.resume')}>
                        <ActionIcon variant="subtle" disabled={busy} onClick={() => onTogglePause(w)}>
                          <Icon path={w.status === 'Active' ? mdiPause : mdiPlay} size={1} />
                        </ActionIcon>
                      </Tooltip>
                      <Tooltip label={t('admin.button.watches.delete')}>
                        <ActionIcon variant="subtle" color="red" disabled={busy} onClick={() => onDelete(w)}>
                          <Icon path={mdiDeleteOutline} size={1} />
                        </ActionIcon>
                      </Tooltip>
                    </Group>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        )}
      </Stack>
    </WithGameEditTab>
  )
}

export default Watches
