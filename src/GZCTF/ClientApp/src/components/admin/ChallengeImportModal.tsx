import {
  Alert,
  Button,
  FileButton,
  Group,
  Modal,
  ModalProps,
  Stack,
  Tabs,
  Text,
  TextInput,
  Code,
  Title,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiAlert, mdiCheck, mdiUpload } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { showErrorMsg } from '@Utils/Shared'
import api, { ChallengeImportResult } from '@Api'

interface ChallengeImportModalProps extends ModalProps {
  gameId: number
  /** When true call the admin auto-approve endpoints; otherwise the user-submit endpoint. */
  asAdmin: boolean
  onImported?: (result: ChallengeImportResult) => void
}

export const ChallengeImportModal: FC<ChallengeImportModalProps> = (props) => {
  const { gameId, asAdmin, onImported, onClose, ...modalProps } = props
  const { t } = useTranslation()

  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<ChallengeImportResult | null>(null)
  const [file, setFile] = useState<File | null>(null)
  const [repoUrl, setRepoUrl] = useState('')
  const [refValue, setRefValue] = useState('')
  const [subpath, setSubpath] = useState('')

  const reset = () => {
    setFile(null)
    setRepoUrl('')
    setRefValue('')
    setSubpath('')
    setResult(null)
  }

  const close = () => {
    reset()
    onClose?.()
  }

  const runImport = async (kind: 'tarball' | 'github') => {
    setBusy(true)
    setResult(null)
    try {
      let resp
      if (kind === 'tarball') {
        if (!file) throw new Error(t('admin.content.import.no_file'))
        resp = asAdmin
          ? await api.edit.editImportChallenge(gameId, file)
          : await api.edit.editSubmitChallenge(gameId, file)
      } else {
        if (!repoUrl) throw new Error(t('admin.content.import.no_url'))
        resp = await api.edit.editImportChallengeFromGitHub(gameId, {
          repoUrl,
          ref: refValue || null,
          subpath: subpath || null,
        })
      }
      setResult(resp.data)
      onImported?.(resp.data)
      showNotification({
        color: 'teal',
        message: t('admin.notification.import.complete'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal size="lg" onClose={close} {...modalProps}>
      <Tabs defaultValue="github">
        <Tabs.List>
          <Tabs.Tab value="github">{t('admin.content.import.github_tab')}</Tabs.Tab>
          <Tabs.Tab value="tarball">{t('admin.content.import.tarball_tab')}</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="github" pt="md">
          <Stack gap="sm">
            <TextInput
              label={t('admin.content.import.repo_url')}
              placeholder="https://github.com/TCP1P/TCP1P-CTF-2024-Challenges-Public"
              value={repoUrl}
              onChange={(e) => setRepoUrl(e.currentTarget.value)}
            />
            <Group grow>
              <TextInput
                label={t('admin.content.import.ref')}
                placeholder="main"
                value={refValue}
                onChange={(e) => setRefValue(e.currentTarget.value)}
              />
              <TextInput
                label={t('admin.content.import.subpath')}
                placeholder="quals"
                value={subpath}
                onChange={(e) => setSubpath(e.currentTarget.value)}
              />
            </Group>
            <Button
              leftSection={<Icon path={mdiUpload} size={1} />}
              loading={busy}
              disabled={!repoUrl}
              onClick={() => runImport('github')}
            >
              {asAdmin ? t('admin.button.import.run') : t('admin.button.import.submit')}
            </Button>
          </Stack>
        </Tabs.Panel>

        <Tabs.Panel value="tarball" pt="md">
          <Stack gap="sm">
            <Text size="sm" c="dimmed">
              {t('admin.content.import.tarball_help')}
            </Text>
            <Group>
              <FileButton onChange={setFile} accept=".tar,.tar.gz,.tgz,application/gzip,application/x-tar">
                {(p) => <Button {...p} variant="default">{t('admin.button.import.pick_file')}</Button>}
              </FileButton>
              {file && <Text size="sm" ff="monospace">{file.name}</Text>}
            </Group>
            <Button
              leftSection={<Icon path={mdiUpload} size={1} />}
              loading={busy}
              disabled={!file}
              onClick={() => runImport('tarball')}
            >
              {asAdmin ? t('admin.button.import.run') : t('admin.button.import.submit')}
            </Button>
          </Stack>
        </Tabs.Panel>
      </Tabs>

      {result && (
        <Stack gap="xs" mt="md">
          <Title order={5}>{t('admin.content.import.result')}</Title>
          <Group gap="md">
            <Text size="sm" c="teal">+{result.imported} {t('admin.content.import.imported')}</Text>
            <Text size="sm" c="blue">~{result.updated} {t('admin.content.import.updated')}</Text>
            <Text size="sm" c="gray">{result.skipped} {t('admin.content.import.skipped')}</Text>
            <Text size="sm" c="red">{result.failed} {t('admin.content.import.failed')}</Text>
          </Group>
          {result.messages.length > 0 && (
            <Alert color="yellow" icon={<Icon path={mdiAlert} size={1} />} variant="light">
              <Stack gap={2}>
                {result.messages.map((m, i) => (
                  <Code key={i} block style={{ whiteSpace: 'pre-wrap', fontSize: 11 }}>
                    {m}
                  </Code>
                ))}
              </Stack>
            </Alert>
          )}
        </Stack>
      )}
    </Modal>
  )
}
