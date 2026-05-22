import {
  Accordion,
  Alert,
  Badge,
  Button,
  Code,
  Group,
  Paper,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { showNotification } from '@mantine/notifications'
import {
  mdiAlertCircleOutline,
  mdiCheck,
  mdiCheckCircleOutline,
  mdiCloseCircleOutline,
  mdiFileTreeOutline,
  mdiFolderZipOutline,
  mdiUpload,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameTab } from '@Components/WithGameTab'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useGame } from '@Hooks/useGame'
import { HunamizeSize, showErrorMsg } from '@Utils/Shared'
import api, { ChallengeImportResult, Role } from '@Api'

const MAX_SIZE = 64 * 1024 * 1024

const Submit: FC = () => {
  const { id } = useParams()
  const gameId = parseInt(id ?? '-1')
  const { t } = useTranslation()
  const theme = useMantineTheme()

  const { game } = useGame(gameId)
  const disabled = game?.allowUserSubmissions === false

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
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <WithGameTab>
          <Stack gap="lg" maw="48rem" mx="auto" w="100%">
            <Stack gap={0}>
              <Title order={2}>{t('game.submit.title')}</Title>
              <Text c="dimmed">{t('game.submit.subtitle')}</Text>
            </Stack>

            {disabled ? (
              <Alert color="orange" variant="light" icon={<Icon path={mdiAlertCircleOutline} size={1} />}>
                {t('game.submit.disabled_notice')}
              </Alert>
            ) : (
              <Alert color="blue" variant="light" icon={<Icon path={mdiAlertCircleOutline} size={1} />}>
                {t('game.submit.review_notice')}
              </Alert>
            )}

            {!disabled && (
              <Accordion variant="separated" defaultValue={null} radius="md">
                <Accordion.Item value="layout">
                  <Accordion.Control icon={<Icon path={mdiFileTreeOutline} size={1} />}>
                    <Text size="sm" fw={500}>{t('game.submit.example.title')}</Text>
                  </Accordion.Control>
                  <Accordion.Panel>
                    <Stack gap="md">
                      <Text size="sm">{t('game.submit.example.intro')}</Text>

                      <Stack gap={4}>
                        <Text size="sm" fw={600}>{t('game.submit.example.attachment_title')}</Text>
                        <Text size="xs" c="dimmed">
                          {t('game.submit.example.attachment_desc')}
                        </Text>
                        <Code block style={{ fontSize: 12 }}>
                          {`my-challenge.zip
├── challenge.yml      ← required, see below
└── dist/              ← (optional) files handed to the player
    └── handout.zip`}
                        </Code>
                        <Code block style={{ fontSize: 12, whiteSpace: 'pre-wrap' }}>
                          {`name: "Cool Web Challenge"
author: "you"
type: "StaticAttachment"
category: "Web"
description: |
  Markdown is supported.
  Solve me!
flags:
  - "flag{example}"
provide: "./dist/handout.zip"`}
                        </Code>
                      </Stack>

                      <Stack gap={4}>
                        <Text size="sm" fw={600}>{t('game.submit.example.container_title')}</Text>
                        <Text size="xs" c="dimmed">
                          {t('game.submit.example.container_desc')}
                        </Text>
                        <Code block style={{ fontSize: 12 }}>
                          {`my-challenge.zip
├── challenge.yml
├── dist/              ← player handouts (optional)
│   └── handout.zip
└── src/               ← server-side; platform builds this
    ├── Dockerfile
    ├── app.py
    └── flag.txt`}
                        </Code>
                        <Code block style={{ fontSize: 12, whiteSpace: 'pre-wrap' }}>
                          {`name: "Pwn Me"
author: "you"
type: "StaticContainer"
category: "Pwn"
description: |
  \`nc {{ .host }} 1337\`
flags:
  - "flag{example}"
provide: "./dist/handout.zip"
container:
  containerImage: "{{.slug}}:latest"   # auto-built from ./src/Dockerfile
  memoryLimit: 256
  cpuCount: 1
  exposePort: 1337`}
                        </Code>
                      </Stack>

                      <Stack gap={2}>
                        <Text size="sm" fw={600}>{t('game.submit.example.tips_title')}</Text>
                        <Text size="xs" c="dimmed">• {t('game.submit.example.tips.value_ignored')}</Text>
                        <Text size="xs" c="dimmed">• {t('game.submit.example.tips.visible_ignored')}</Text>
                        <Text size="xs" c="dimmed">• {t('game.submit.example.tips.review_queue')}</Text>
                        <Text size="xs" c="dimmed">• {t('game.submit.example.tips.template_image')}</Text>
                        <Text size="xs" c="dimmed">• {t('game.submit.example.tips.size_cap')}</Text>
                      </Stack>
                    </Stack>
                  </Accordion.Panel>
                </Accordion.Item>
              </Accordion>
            )}

            <Paper p="lg" withBorder style={disabled ? { opacity: 0.55, pointerEvents: 'none' } : undefined}>
              <Stack gap="md">
                <Dropzone
                  multiple={false}
                  maxSize={MAX_SIZE}
                  accept={[
                    'application/gzip',
                    'application/x-gzip',
                    'application/x-tar',
                    'application/zip',
                    'application/x-zip-compressed',
                    '.tar',
                    '.tar.gz',
                    '.tgz',
                    '.zip',
                  ]}
                  onDrop={(files) => setFile(files[0] ?? null)}
                  onReject={(rejections) => {
                    const msg = rejections[0]?.errors[0]?.message ?? t('game.submit.dropzone.rejected')
                    showErrorMsg(new Error(msg), t)
                  }}
                  style={{
                    borderStyle: file ? 'solid' : 'dashed',
                    borderColor: file ? theme.colors.teal[6] : undefined,
                  }}
                >
                  <Group justify="center" gap="xl" mih={140} style={{ pointerEvents: 'none' }}>
                    <Dropzone.Accept>
                      <Icon path={mdiCheckCircleOutline} size={2.5} color={theme.colors.teal[6]} />
                    </Dropzone.Accept>
                    <Dropzone.Reject>
                      <Icon path={mdiCloseCircleOutline} size={2.5} color={theme.colors.red[6]} />
                    </Dropzone.Reject>
                    <Dropzone.Idle>
                      <Icon path={mdiFolderZipOutline} size={2.5} />
                    </Dropzone.Idle>
                    <Stack gap={4} align="flex-start">
                      <Text size="lg" fw={700}>
                        {t('game.submit.dropzone.title')}
                      </Text>
                      <Text size="sm" c="dimmed">
                        {t('game.submit.dropzone.hint')}
                      </Text>
                    </Stack>
                  </Group>
                </Dropzone>

                {file && (
                  <Group justify="space-between" wrap="nowrap">
                    <Group gap="sm" wrap="nowrap" miw={0}>
                      <Icon path={mdiFolderZipOutline} size={1} />
                      <Text ff="monospace" truncate>
                        {file.name}
                      </Text>
                      <Badge size="sm" variant="light">
                        {HunamizeSize(file.size)}
                      </Badge>
                    </Group>
                    <Button
                      size="xs"
                      variant="subtle"
                      color="gray"
                      onClick={() => setFile(null)}
                      disabled={busy}
                    >
                      {t('game.submit.button.clear')}
                    </Button>
                  </Group>
                )}

                <Button
                  size="md"
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
                    <Badge color="teal" variant="light">+{result.imported}</Badge>
                    <Badge color="blue" variant="light">~{result.updated}</Badge>
                    <Badge color="gray" variant="light">{result.skipped}</Badge>
                    <Badge color="red" variant="light">{result.failed}</Badge>
                  </Group>
                  {result.messages.length > 0 && (
                    <Stack gap={2}>
                      {result.messages.map((m, i) => (
                        <Code key={i} block style={{ whiteSpace: 'pre-wrap', fontSize: 11 }}>
                          {m}
                        </Code>
                      ))}
                    </Stack>
                  )}
                </Stack>
              </Paper>
            )}
          </Stack>
        </WithGameTab>
      </WithRole>
    </WithNavBar>
  )
}

export default Submit
