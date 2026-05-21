import {
  Alert,
  Button,
  Code,
  Container,
  FileButton,
  Group,
  Paper,
  Stack,
  Tabs,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiAlert, mdiCheck, mdiUpload } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { showErrorMsg } from '@Utils/Shared'
import api, { ChallengeImportResult, Role } from '@Api'

const Submit: FC = () => {
  const { id } = useParams()
  const gameId = parseInt(id ?? '-1')
  const { t } = useTranslation()

  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<ChallengeImportResult | null>(null)
  const [file, setFile] = useState<File | null>(null)
  const [repoUrl, setRepoUrl] = useState('')
  const [refValue, setRefValue] = useState('')
  const [subpath, setSubpath] = useState('')

  const submit = async (kind: 'tarball' | 'github') => {
    setBusy(true)
    setResult(null)
    try {
      let resp
      if (kind === 'tarball') {
        if (!file) throw new Error(t('game.submit.no_file'))
        resp = await api.edit.editSubmitChallenge(gameId, file)
      } else {
        if (!repoUrl) throw new Error(t('game.submit.no_url'))
        resp = await api.edit.editImportChallengeFromGitHub(gameId, {
          repoUrl,
          ref: refValue || null,
          subpath: subpath || null,
        })
      }
      setResult(resp.data)
      showNotification({
        color: 'teal',
        title: t('game.submit.notification.submitted'),
        message: t('game.submit.notification.under_review'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  return (
    <WithNavBar width="80%">
      <WithRole requiredRole={Role.User}>
        <Container size="md" mt="lg">
          <Stack gap="lg">
            <Stack gap={0}>
              <Title order={2}>{t('game.submit.title')}</Title>
              <Text c="dimmed">{t('game.submit.subtitle')}</Text>
            </Stack>

            <Alert color="blue" variant="light" icon={<Icon path={mdiAlert} size={1} />}>
              {t('game.submit.review_notice')}
            </Alert>

            <Paper p="md" withBorder>
              <Tabs defaultValue="github">
                <Tabs.List>
                  <Tabs.Tab value="github">{t('game.submit.github_tab')}</Tabs.Tab>
                  <Tabs.Tab value="tarball">{t('game.submit.tarball_tab')}</Tabs.Tab>
                </Tabs.List>

                <Tabs.Panel value="github" pt="md">
                  <Stack gap="sm">
                    <TextInput
                      label={t('game.submit.repo_url')}
                      placeholder="https://github.com/your-org/your-ctf"
                      value={repoUrl}
                      onChange={(e) => setRepoUrl(e.currentTarget.value)}
                    />
                    <Group grow>
                      <TextInput
                        label={t('game.submit.ref')}
                        placeholder="main"
                        value={refValue}
                        onChange={(e) => setRefValue(e.currentTarget.value)}
                      />
                      <TextInput
                        label={t('game.submit.subpath')}
                        placeholder="quals"
                        value={subpath}
                        onChange={(e) => setSubpath(e.currentTarget.value)}
                      />
                    </Group>
                    <Button
                      leftSection={<Icon path={mdiUpload} size={1} />}
                      loading={busy}
                      disabled={!repoUrl}
                      onClick={() => submit('github')}
                    >
                      {t('game.submit.button.submit')}
                    </Button>
                  </Stack>
                </Tabs.Panel>

                <Tabs.Panel value="tarball" pt="md">
                  <Stack gap="sm">
                    <Text size="sm" c="dimmed">{t('game.submit.tarball_help')}</Text>
                    <Group>
                      <FileButton onChange={setFile} accept=".tar,.tar.gz,.tgz,application/gzip,application/x-tar">
                        {(p) => <Button {...p} variant="default">{t('game.submit.button.pick_file')}</Button>}
                      </FileButton>
                      {file && <Text size="sm" ff="monospace">{file.name}</Text>}
                    </Group>
                    <Button
                      leftSection={<Icon path={mdiUpload} size={1} />}
                      loading={busy}
                      disabled={!file}
                      onClick={() => submit('tarball')}
                    >
                      {t('game.submit.button.submit')}
                    </Button>
                  </Stack>
                </Tabs.Panel>
              </Tabs>
            </Paper>

            {result && (
              <Paper p="md" withBorder>
                <Stack gap="xs">
                  <Title order={5}>{t('game.submit.result')}</Title>
                  <Group gap="md">
                    <Text size="sm" c="teal">+{result.imported}</Text>
                    <Text size="sm" c="blue">~{result.updated}</Text>
                    <Text size="sm" c="gray">{result.skipped}</Text>
                    <Text size="sm" c="red">{result.failed}</Text>
                  </Group>
                  {result.messages.length > 0 && (
                    <Stack gap={2}>
                      {result.messages.map((m, i) => (
                        <Code key={i} block style={{ whiteSpace: 'pre-wrap', fontSize: 11 }}>{m}</Code>
                      ))}
                    </Stack>
                  )}
                </Stack>
              </Paper>
            )}
          </Stack>
        </Container>
      </WithRole>
    </WithNavBar>
  )
}

export default Submit
