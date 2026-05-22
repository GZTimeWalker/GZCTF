import {
  Anchor,
  Badge,
  Button,
  Center,
  Code,
  Divider,
  Group,
  Loader,
  Modal,
  ModalProps,
  Paper,
  ScrollArea,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { mdiDownload, mdiFileDocumentOutline, mdiFolderZipOutline, mdiHammerWrench } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { HunamizeSize, showErrorMsg } from '@Utils/Shared'
import api, { ChallengeAuditModel } from '@Api'

interface ChallengeAuditModalProps extends Omit<ModalProps, 'children'> {
  gameId: number
  challengeId: number | null
  challengeTitle?: string
  submitter?: string | null
}

export const ChallengeAuditModal: FC<ChallengeAuditModalProps> = (props) => {
  const { gameId, challengeId, challengeTitle, submitter, opened, onClose, ...rest } = props
  const { t } = useTranslation()
  const [audit, setAudit] = useState<ChallengeAuditModel | null>(null)
  const [loading, setLoading] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    if (!opened || challengeId == null) {
      setAudit(null)
      return
    }
    let cancelled = false
    let timer: number | null = null

    // Polls AuditMeta on a 2s cadence whenever the build is in flight
    // so the modal reflects Queued → Building → Success/Failed without
    // the operator having to reload. 2s matches the live-strip cadence
    // on /admin/builds.
    const tick = async (isInitial: boolean) => {
      if (isInitial) setLoading(true)
      try {
        const res = await api.edit.editGetChallengeAuditMeta(gameId, challengeId)
        if (cancelled) return
        setAudit(res.data)
        const inFlight = res.data.buildStatus === 'Queued' || res.data.buildStatus === 'Building'
        if (inFlight && !cancelled) {
          timer = window.setTimeout(() => void tick(false), 2000)
        }
      } catch (e) {
        if (!cancelled) {
          if (isInitial) setAudit(null)
          showErrorMsg(e, t)
        }
      } finally {
        if (isInitial && !cancelled) setLoading(false)
      }
    }

    void tick(true)
    return () => {
      cancelled = true
      if (timer != null) window.clearTimeout(timer)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opened, gameId, challengeId, reloadKey, t])

  const downloadArchive = () => {
    if (challengeId == null) return
    window.open(`/api/edit/games/${gameId}/challenges/${challengeId}/auditarchive`, '_blank')
  }

  const [rebuilding, setRebuilding] = useState(false)
  const onRebuild = async () => {
    if (challengeId == null) return
    setRebuilding(true)
    try {
      const resp = await api.edit.editRebuildChallengeImage(gameId, challengeId)
      // The endpoint now returns 202 with buildStatus=Queued. Patch the
      // local state to Queued immediately so the operator sees the
      // transition; the next AuditMeta tick (kicked by reloadKey
      // bumping) will drive Building → Success/Failed.
      setAudit((prev) => prev ? {
        ...prev,
        buildStatus: resp.data.buildStatus,
        lastBuildLog: resp.data.lastBuildLog,
      } : prev)
      setReloadKey((k) => k + 1)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setRebuilding(false)
    }
  }

  return (
    <Modal
      size="xl"
      opened={opened}
      onClose={onClose}
      title={
        <Group gap="sm">
          <Icon path={mdiFolderZipOutline} size={1} />
          <Stack gap={0}>
            <Title order={4}>{t('admin.content.audit.title')}</Title>
            {challengeTitle && (
              <Text size="xs" c="dimmed">
                {challengeTitle}
                {submitter ? ` — ${submitter}` : ''}
              </Text>
            )}
          </Stack>
        </Group>
      }
      {...rest}
    >
      {loading ? (
        <Center py="xl">
          <Loader />
        </Center>
      ) : !audit ? (
        <Center py="xl">
          <Text c="dimmed">{t('admin.content.audit.unavailable')}</Text>
        </Center>
      ) : (
        <Stack gap="md">
          {audit.archiveAvailable ? (
            <Group justify="space-between">
              <Text size="sm" c="dimmed">
                {t('admin.content.audit.archive_available')}
              </Text>
              <Group gap="xs">
                {audit.buildStatus && audit.buildStatus !== 'None' && (
                  <Button
                    size="xs"
                    variant="default"
                    leftSection={<Icon path={mdiHammerWrench} size={0.9} />}
                    loading={rebuilding}
                    onClick={onRebuild}
                  >
                    {t('admin.button.audit.rebuild')}
                  </Button>
                )}
                <Button
                  size="xs"
                  leftSection={<Icon path={mdiDownload} size={0.9} />}
                  onClick={downloadArchive}
                >
                  {t('admin.button.audit.download')}
                </Button>
              </Group>
            </Group>
          ) : (
            <Text size="sm" c="dimmed">
              {t('admin.content.audit.no_archive')}
            </Text>
          )}

          {audit.buildStatus && audit.buildStatus !== 'None' && (
            <Paper p="sm" withBorder>
              <Stack gap={4}>
                <Group gap="xs">
                  <Title order={6}>{t('admin.content.audit.build_log')}</Title>
                  <Badge
                    size="xs"
                    color={
                      audit.buildStatus === 'Success' ? 'teal'
                      : audit.buildStatus === 'Failed' ? 'red'
                      : audit.buildStatus === 'NotApplicable' ? 'gray'
                      : audit.buildStatus === 'MissingDockerfile' ? 'orange'
                      : audit.buildStatus === 'Queued' ? 'blue'
                      : 'yellow'
                    }
                    variant={audit.buildStatus === 'Failed' ? 'filled' : 'light'}
                  >
                    {audit.buildStatus}
                  </Badge>
                </Group>
                {audit.lastBuildLog ? (
                  <Code
                    block
                    style={{
                      whiteSpace: 'pre-wrap',
                      maxHeight: '30vh',
                      overflowY: 'auto',
                      fontSize: 11,
                    }}
                  >
                    {audit.lastBuildLog}
                  </Code>
                ) : (
                  <Text size="xs" c="dimmed">{t('admin.content.audit.no_build_log')}</Text>
                )}
              </Stack>
            </Paper>
          )}

          <Divider />

          <Stack gap={6}>
            <Title order={5}>{t('admin.content.audit.yaml')}</Title>
            {audit.yamlText ? (
              <Code
                block
                style={{
                  whiteSpace: 'pre-wrap',
                  maxHeight: '40vh',
                  overflowY: 'auto',
                  fontSize: 12,
                }}
              >
                {audit.yamlText}
              </Code>
            ) : (
              <Text size="sm" c="dimmed">
                {t('admin.content.audit.no_yaml')}
              </Text>
            )}
          </Stack>

          <Divider />

          <Stack gap={6}>
            <Title order={5}>
              {t('admin.content.audit.files')}{' '}
              <Text span size="sm" c="dimmed">
                ({audit.files.length})
              </Text>
            </Title>
            <ScrollArea h={Math.min(audit.files.length * 26 + 12, 240)} type="auto">
              <Stack gap={2}>
                {audit.files.map((f) => (
                  <Group key={f.path} gap="xs" wrap="nowrap" justify="space-between">
                    <Group gap={4} wrap="nowrap" miw={0}>
                      <Icon path={mdiFileDocumentOutline} size={0.7} />
                      <Anchor
                        component="span"
                        size="sm"
                        ff="monospace"
                        truncate
                        c={Object.keys(audit.previews).includes(f.path) ? 'blue' : undefined}
                      >
                        {f.path}
                      </Anchor>
                    </Group>
                    <Badge size="xs" variant="light" color="gray">
                      {HunamizeSize(f.size)}
                    </Badge>
                  </Group>
                ))}
              </Stack>
            </ScrollArea>
          </Stack>

          {Object.keys(audit.previews).length > 0 && (
            <>
              <Divider />
              <Stack gap="sm">
                <Title order={5}>{t('admin.content.audit.previews')}</Title>
                {Object.entries(audit.previews).map(([path, contents]) => (
                  <Paper key={path} p="sm" withBorder>
                    <Stack gap={4}>
                      <Text size="xs" ff="monospace" fw="bold">
                        {path}
                      </Text>
                      <Code
                        block
                        style={{
                          whiteSpace: 'pre-wrap',
                          maxHeight: '30vh',
                          overflowY: 'auto',
                          fontSize: 12,
                        }}
                      >
                        {contents}
                      </Code>
                    </Stack>
                  </Paper>
                ))}
              </Stack>
            </>
          )}
        </Stack>
      )}
    </Modal>
  )
}
