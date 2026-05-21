import {
  Alert,
  Button,
  Code,
  Container,
  FileButton,
  Group,
  Paper,
  Stack,
  Text,
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

  const submit = async () => {
    if (!file) {
      showErrorMsg(new Error(t('game.submit.no_file')), t)
      return
    }
    setBusy(true)
    setResult(null)
    try {
      const resp = await api.edit.editSubmitChallenge(gameId, file)
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
              <Stack gap="sm">
                <Text size="sm" c="dimmed">{t('game.submit.archive_help')}</Text>
                <Group>
                  <FileButton onChange={setFile} accept=".tar,.tar.gz,.tgz,.zip,application/gzip,application/x-tar,application/zip">
                    {(p) => <Button {...p} variant="default">{t('game.submit.button.pick_file')}</Button>}
                  </FileButton>
                  {file && <Text size="sm" ff="monospace">{file.name}</Text>}
                </Group>
                <Button
                  leftSection={<Icon path={mdiUpload} size={1} />}
                  loading={busy}
                  disabled={!file}
                  onClick={submit}
                >
                  {t('game.submit.button.submit')}
                </Button>
              </Stack>
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
