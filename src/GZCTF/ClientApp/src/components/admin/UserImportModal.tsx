import {
  Alert,
  Badge,
  Button,
  FileButton,
  Group,
  Loader,
  Modal,
  ModalProps,
  Paper,
  Radio,
  ScrollArea,
  Select,
  SimpleGrid,
  Stack,
  Stepper,
  Switch,
  Table,
  Text,
  Textarea,
  TextInput,
  ThemeIcon,
} from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountMultiplePlus,
  mdiAlertCircleOutline,
  mdiCheck,
  mdiCheckCircleOutline,
  mdiClose,
  mdiDownload,
  mdiFileDelimited,
  mdiInformationOutline,
  mdiUpload,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useMemo, useState } from 'react'

// ─── Backend response types (mirror CsvImportResultModel) ────────────────────

interface CsvImportUserResult {
  email: string
  realName: string
  userName: string
  password: string
  teamName?: string
  status: 'created' | 'updated' | 'skipped'
  error?: string
}

interface CsvImportResult {
  total: number
  created: number
  updated: number
  skipped: number
  users: CsvImportUserResult[]
}

// ─── Internal types ───────────────────────────────────────────────────────────

const NONE = '(none)'

interface ColMap {
  realName: string
  email: string
  teamName: string
  stdNumber: string
  phone: string
}

interface Options {
  emailConfirmed: boolean
  teamMode: 'csv' | 'single' | 'none'
  singleTeamName: string
}

// ─── Utilities ───────────────────────────────────────────────────────────────

function splitCsvLine(line: string): string[] {
  const fields: string[] = []
  let field = ''
  let inQuote = false
  for (let i = 0; i < line.length; i++) {
    const c = line[i]
    if (c === '"') {
      if (inQuote && line[i + 1] === '"') { field += '"'; i++ }
      else inQuote = !inQuote
    } else if (c === ',' && !inQuote) {
      fields.push(field.trim()); field = ''
    } else {
      field += c
    }
  }
  fields.push(field.trim())
  return fields
}

function parseCSVInfo(text: string): { headers: string[]; rowCount: number } {
  const lines = text.trim().split(/\r?\n/).filter((l) => l.trim())
  if (lines.length < 1) return { headers: [], rowCount: 0 }
  return { headers: splitCsvLine(lines[0]), rowCount: lines.length - 1 }
}

function parsePreviewRows(text: string, headers: string[], map: ColMap, limit = 5) {
  const lines = text.trim().split(/\r?\n/).filter((l) => l.trim()).slice(1, 1 + limit)
  return lines.map((line) => {
    const fields = splitCsvLine(line)
    const get = (col: string) => {
      const i = headers.indexOf(col)
      return i >= 0 && i < fields.length ? fields[i] : ''
    }
    return {
      realName: map.realName !== NONE ? get(map.realName) : '',
      email: map.email !== NONE ? get(map.email) : '',
      teamName: map.teamName !== NONE ? get(map.teamName) : '',
    }
  })
}

function autoMap(headers: string[]): ColMap {
  const lc = headers.map((h) => h.toLowerCase())
  const find = (...kw: string[]) => {
    const i = lc.findIndex((h) => kw.some((k) => h.includes(k)))
    return i >= 0 ? headers[i] : NONE
  }
  return {
    realName: find('name', 'real', 'full'),
    email: find('email', 'mail'),
    teamName: find('team', 'group', 'org'),
    stdNumber: find('student', 'std', 'nim', 'nrp', 'matric'),
    phone: find('phone', 'mobile', 'tel', 'contact'),
  }
}

function triggerDownload(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob)
  const a = Object.assign(document.createElement('a'), { href: url, download: name })
  a.click()
  URL.revokeObjectURL(url)
}

function buildCredentialsCsv(users: CsvImportUserResult[]): Blob {
  const hdr = ['Username', 'Password', 'Email', 'Real Name', 'Team', 'Status']
  const q = (v: string) => `"${(v ?? '').replace(/"/g, '""')}"`
  const lines = [
    hdr.join(','),
    ...users
      .filter((u) => u.status !== 'skipped')
      .map((u) => [u.userName, u.password, u.email, u.realName, u.teamName ?? '', u.status].map(q).join(',')),
  ]
  return new Blob([lines.join('\n')], { type: 'text/csv' })
}

const TEMPLATE_CSV =
  'Real Name,Email,Team Name,Student ID,Phone\n' +
  'John Doe,john@example.com,TeamAlpha,2024001,+628123456789\n' +
  'Jane Smith,jane@example.com,TeamBeta,2024002,\n'

// ─── Component ───────────────────────────────────────────────────────────────

interface UserImportModalProps extends ModalProps {
  onImportComplete?: () => void
}

export const UserImportModal: FC<UserImportModalProps> = ({ onImportComplete, ...props }) => {
  const [step, setStep] = useState(0)
  const [rawText, setRawText] = useState('')
  const [headers, setHeaders] = useState<string[]>([])
  const [rowCount, setRowCount] = useState(0)
  const [map, setMap] = useState<ColMap>({ realName: NONE, email: NONE, teamName: NONE, stdNumber: NONE, phone: NONE })
  const [opts, setOpts] = useState<Options>({ emailConfirmed: true, teamMode: 'csv', singleTeamName: '' })
  const [loading, setLoading] = useState(false)
  const [importResult, setImportResult] = useState<CsvImportResult | null>(null)
  const [importError, setImportError] = useState<string | null>(null)

  const process = useCallback((text: string) => {
    const { headers: hdrs, rowCount: rc } = parseCSVInfo(text)
    if (hdrs.length < 2 || rc < 1) {
      showNotification({ message: 'CSV must have at least a header row and one data row', color: 'red' })
      return
    }
    setHeaders(hdrs)
    setRowCount(rc)
    setMap(autoMap(hdrs))
    setRawText(text)
    setStep(1)
  }, [])

  const onFile = (f: File | null) => {
    if (!f) return
    const r = new FileReader()
    r.onload = (e) => process(e.target?.result as string)
    r.readAsText(f)
  }

  const headerOptions = useMemo(
    () => [{ value: NONE, label: '— not mapped —' }, ...headers.map((h) => ({ value: h, label: h }))],
    [headers]
  )

  const previewRows = useMemo(() => parsePreviewRows(rawText, headers, map), [rawText, headers, map])

  const canNext1 = map.email !== NONE && map.realName !== NONE

  const runImport = async () => {
    setLoading(true)
    setImportError(null)
    setStep(3)

    try {
      const resp = await fetch('/api/admin/users/import', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          csvText: rawText,
          realNameColumn: map.realName,
          emailColumn: map.email,
          teamNameColumn: map.teamName !== NONE ? map.teamName : undefined,
          stdNumberColumn: map.stdNumber !== NONE ? map.stdNumber : undefined,
          phoneColumn: map.phone !== NONE ? map.phone : undefined,
          teamMode: opts.teamMode,
          singleTeamName: opts.teamMode === 'single' ? opts.singleTeamName : undefined,
          emailConfirmed: opts.emailConfirmed,
        }),
      })

      if (!resp.ok) {
        const err = await resp.json().catch(() => ({ title: 'Import failed' }))
        throw new Error(err.title ?? err.message ?? 'Import failed')
      }

      const result: CsvImportResult = await resp.json()
      setImportResult(result)

      if (result.created > 0 || result.updated > 0) {
        onImportComplete?.()
        showNotification({
          message: `${result.created} users created, ${result.updated} updated`,
          color: 'teal',
          icon: <Icon path={mdiCheck} size={1} />,
        })
      }
    } catch (e: any) {
      setImportError(e?.message ?? 'Import failed')
      showNotification({ message: e?.message ?? 'Import failed', color: 'red' })
    } finally {
      setLoading(false)
    }
  }

  const reset = () => {
    setStep(0); setRawText(''); setHeaders([]); setRowCount(0)
    setMap({ realName: NONE, email: NONE, teamName: NONE, stdNumber: NONE, phone: NONE })
    setOpts({ emailConfirmed: true, teamMode: 'csv', singleTeamName: '' })
    setLoading(false); setImportResult(null); setImportError(null)
  }

  return (
    <Modal
      {...props}
      title={
        <Group gap="xs">
          <ThemeIcon variant="light" color="blue">
            <Icon path={mdiAccountMultiplePlus} size={0.9} />
          </ThemeIcon>
          <Text fw="bold">Import Users from CSV</Text>
        </Group>
      }
      size="80%"
      styles={{ body: { paddingTop: 0 } }}
      onClose={() => { reset(); props.onClose() }}
    >
      <Stack gap="lg" pt="md">
        <Stepper active={step} size="sm" allowNextStepsSelect={false}>
          <Stepper.Step label="Upload" description="Select file" />
          <Stepper.Step label="Map Columns" description="Configure fields" />
          <Stepper.Step label="Options" description="Import settings" />
          <Stepper.Step label="Result" description="Download credentials" />
        </Stepper>

        {/* ── Step 0: Upload ── */}
        {step === 0 && (
          <Stack gap="md">
            <Dropzone
              onDrop={(files) => onFile(files[0])}
              accept={{ 'text/csv': ['.csv'], 'text/plain': ['.txt', '.csv'] }}
              maxSize={10 * 1024 * 1024}
            >
              <Group justify="center" gap="xl" mih={120} style={{ pointerEvents: 'none' }}>
                <Dropzone.Accept><Icon path={mdiCheck} size={2.5} color="teal" /></Dropzone.Accept>
                <Dropzone.Reject><Icon path={mdiClose} size={2.5} color="red" /></Dropzone.Reject>
                <Dropzone.Idle><Icon path={mdiFileDelimited} size={2.5} /></Dropzone.Idle>
                <Stack gap={4} align="center">
                  <Text size="lg" fw={700}>Drag a CSV file here</Text>
                  <Text size="sm" c="dimmed">Supports .csv and .txt — max 10 MB</Text>
                </Stack>
              </Group>
            </Dropzone>

            <Text c="dimmed" ta="center" size="sm">— or paste CSV text below —</Text>

            <Textarea
              placeholder={'Real Name,Email,Team Name\nJohn Doe,john@example.com,TeamAlpha\n...'}
              rows={6}
              value={rawText}
              onChange={(e) => setRawText(e.currentTarget.value)}
              ff="monospace"
              fz="sm"
            />

            <Group justify="space-between">
              <Button
                variant="subtle"
                size="sm"
                leftSection={<Icon path={mdiDownload} size={0.8} />}
                onClick={() => triggerDownload(new Blob([TEMPLATE_CSV], { type: 'text/csv' }), 'import_template.csv')}
              >
                Download Template
              </Button>
              <Group gap="sm">
                <FileButton onChange={onFile} accept=".csv,.txt,text/csv,text/plain">
                  {(fp) => (
                    <Button variant="outline" {...fp} leftSection={<Icon path={mdiUpload} size={0.8} />}>
                      Browse File
                    </Button>
                  )}
                </FileButton>
                <Button disabled={!rawText.trim()} onClick={() => process(rawText)}>
                  Parse & Continue →
                </Button>
              </Group>
            </Group>
          </Stack>
        )}

        {/* ── Step 1: Map Columns + Preview ── */}
        {step === 1 && (
          <Stack gap="md">
            <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
              <Paper p="md" withBorder>
                <Stack gap="sm">
                  <Text fw={600} size="sm">CSV Column → Field Mapping</Text>
                  <Select label="Real Name *" data={headerOptions} value={map.realName} onChange={(v) => setMap((m) => ({ ...m, realName: v ?? NONE }))} />
                  <Select label="Email *" data={headerOptions} value={map.email} onChange={(v) => setMap((m) => ({ ...m, email: v ?? NONE }))} />
                  <Select label="Team Name" data={headerOptions} value={map.teamName} onChange={(v) => setMap((m) => ({ ...m, teamName: v ?? NONE }))} />
                  <Select label="Student ID" data={headerOptions} value={map.stdNumber} onChange={(v) => setMap((m) => ({ ...m, stdNumber: v ?? NONE }))} />
                  <Select label="Phone" data={headerOptions} value={map.phone} onChange={(v) => setMap((m) => ({ ...m, phone: v ?? NONE }))} />
                </Stack>
              </Paper>

              <Paper p="md" withBorder>
                <Stack gap="sm">
                  <Group justify="space-between">
                    <Text fw={600} size="sm">Preview (first 5 rows)</Text>
                    <Badge variant="light">{rowCount} rows total</Badge>
                  </Group>
                  <ScrollArea>
                    <Table striped highlightOnHover withTableBorder withColumnBorders fz="xs">
                      <Table.Thead>
                        <Table.Tr>
                          <Table.Th>#</Table.Th>
                          <Table.Th>Real Name</Table.Th>
                          <Table.Th>Email</Table.Th>
                          <Table.Th>Team</Table.Th>
                        </Table.Tr>
                      </Table.Thead>
                      <Table.Tbody>
                        {previewRows.map((r, i) => (
                          <Table.Tr key={i}>
                            <Table.Td c="dimmed">{i + 1}</Table.Td>
                            <Table.Td>{r.realName || <Text c="dimmed" fz="xs">—</Text>}</Table.Td>
                            <Table.Td ff="monospace">{r.email || <Text c="dimmed" fz="xs">—</Text>}</Table.Td>
                            <Table.Td>{r.teamName || <Text c="dimmed" fz="xs">—</Text>}</Table.Td>
                          </Table.Tr>
                        ))}
                      </Table.Tbody>
                    </Table>
                  </ScrollArea>
                  <Text size="xs" c="dimmed">Usernames and passwords are generated server-side during import.</Text>
                </Stack>
              </Paper>
            </SimpleGrid>

            {!canNext1 && (
              <Alert icon={<Icon path={mdiAlertCircleOutline} size={1} />} color="orange">
                <Text size="sm">Map at least <strong>Real Name</strong> and <strong>Email</strong> columns to continue.</Text>
              </Alert>
            )}

            <Group justify="space-between">
              <Button variant="outline" onClick={() => setStep(0)}>← Back</Button>
              <Button disabled={!canNext1} onClick={() => setStep(2)}>Options →</Button>
            </Group>
          </Stack>
        )}

        {/* ── Step 2: Options ── */}
        {step === 2 && (
          <Stack gap="md">
            <Alert icon={<Icon path={mdiInformationOutline} size={1} />} color="blue">
              <Stack gap={4}>
                <Text size="sm">
                  <strong>{rowCount}</strong> rows found. The server generates unique usernames and secure passwords for
                  each account in a single atomic transaction — no rate limiting concerns.
                </Text>
                <Text size="xs" c="dimmed">
                  Invalid or duplicate emails are skipped. Download the credentials CSV after import — passwords are
                  not stored.
                </Text>
              </Stack>
            </Alert>

            <Paper p="md" withBorder>
              <Stack gap="sm">
                <Text fw={600} size="sm">Account Settings</Text>
                <Switch
                  label="Auto-confirm email — users can log in immediately without email verification"
                  checked={opts.emailConfirmed}
                  onChange={(e) => setOpts((o) => ({ ...o, emailConfirmed: e.currentTarget.checked }))}
                />
              </Stack>
            </Paper>

            <Paper p="md" withBorder>
              <Stack gap="sm">
                <Text fw={600} size="sm">Team Assignment</Text>
                <Radio.Group value={opts.teamMode} onChange={(v) => setOpts((o) => ({ ...o, teamMode: v as Options['teamMode'] }))}>
                  <Stack gap="sm">
                    <Radio
                      value="csv"
                      label={`Use team name from CSV${map.teamName === NONE ? ' (no team column mapped — skipped)' : ` — column "${map.teamName}"`}`}
                    />
                    <Radio value="single" label="Assign all users to a single team:" />
                    {opts.teamMode === 'single' && (
                      <TextInput
                        ml="xl"
                        placeholder="Team name"
                        value={opts.singleTeamName}
                        onChange={(e) => setOpts((o) => ({ ...o, singleTeamName: e.currentTarget.value }))}
                        maw={300}
                      />
                    )}
                    <Radio value="none" label="No team assignment" />
                  </Stack>
                </Radio.Group>
              </Stack>
            </Paper>

            <Group justify="space-between">
              <Button variant="outline" onClick={() => setStep(1)}>← Back</Button>
              <Button
                color="green"
                leftSection={<Icon path={mdiUpload} size={0.9} />}
                disabled={opts.teamMode === 'single' && !opts.singleTeamName.trim()}
                onClick={runImport}
              >
                Import {rowCount} Users
              </Button>
            </Group>
          </Stack>
        )}

        {/* ── Step 3: Result ── */}
        {step === 3 && (
          <Stack gap="md">
            {loading && (
              <Stack align="center" gap="md" py="xl">
                <Loader size="lg" />
                <Text c="dimmed" size="sm">
                  Importing {rowCount} users — the server is generating credentials and creating accounts…
                </Text>
              </Stack>
            )}

            {!loading && importError && (
              <Stack gap="sm">
                <Alert icon={<Icon path={mdiAlertCircleOutline} size={1} />} color="red" title="Import failed">
                  <Text size="sm">{importError}</Text>
                </Alert>
                <Group>
                  <Button variant="outline" onClick={() => setStep(2)}>← Go Back</Button>
                </Group>
              </Stack>
            )}

            {!loading && importResult && (
              <Stack gap="md">
                <SimpleGrid cols={3} spacing="sm">
                  <Paper p="md" withBorder ta="center" style={{ borderColor: 'var(--mantine-color-teal-4)' }}>
                    <Text size="xl" fw={700} c="teal">{importResult.created}</Text>
                    <Text size="xs" c="dimmed">Created</Text>
                  </Paper>
                  <Paper p="md" withBorder ta="center" style={{ borderColor: 'var(--mantine-color-blue-4)' }}>
                    <Text size="xl" fw={700} c="blue">{importResult.updated}</Text>
                    <Text size="xs" c="dimmed">Updated</Text>
                  </Paper>
                  <Paper p="md" withBorder ta="center" style={{ borderColor: 'var(--mantine-color-orange-4)' }}>
                    <Text size="xl" fw={700} c="orange">{importResult.skipped}</Text>
                    <Text size="xs" c="dimmed">Skipped</Text>
                  </Paper>
                </SimpleGrid>

                {importResult.skipped > 0 && (
                  <Paper withBorder p="sm">
                    <Stack gap={4}>
                      <Text size="sm" fw={600} c="orange">Skipped rows:</Text>
                      <ScrollArea h={100}>
                        <Stack gap={2} p={4}>
                          {importResult.users
                            .filter((u) => u.status === 'skipped')
                            .map((u, i) => (
                              <Group key={i} gap="xs" wrap="nowrap">
                                <Icon path={mdiAlertCircleOutline} size={0.6} color="var(--mantine-color-orange-6)" />
                                <Text size="xs" ff="monospace">
                                  {u.email || '(empty)'}: {u.error}
                                </Text>
                              </Group>
                            ))}
                        </Stack>
                      </ScrollArea>
                    </Stack>
                  </Paper>
                )}

                {importResult.created + importResult.updated > 0 && (
                  <Alert icon={<Icon path={mdiCheckCircleOutline} size={1} />} color="teal">
                    <Text size="sm">
                      Import complete. Download the credentials CSV now — passwords are not stored and cannot be
                      retrieved later.
                    </Text>
                  </Alert>
                )}

                <Group justify="space-between">
                  <Button variant="outline" onClick={reset}>Import Another File</Button>
                  <Group gap="sm">
                    {importResult.created + importResult.updated > 0 && (
                      <Button
                        leftSection={<Icon path={mdiDownload} size={0.9} />}
                        onClick={() =>
                          triggerDownload(buildCredentialsCsv(importResult.users), 'imported_credentials.csv')
                        }
                      >
                        Download Credentials ({importResult.created + importResult.updated})
                      </Button>
                    )}
                    <Button variant="filled" onClick={() => { reset(); props.onClose() }}>Done</Button>
                  </Group>
                </Group>
              </Stack>
            )}
          </Stack>
        )}
      </Stack>
    </Modal>
  )
}
