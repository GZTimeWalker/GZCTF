import {
  ActionIcon,
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
  Tooltip,
} from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountMultiplePlus,
  mdiAlertCircleOutline,
  mdiCheck,
  mdiCheckCircleOutline,
  mdiClose,
  mdiDeleteOutline,
  mdiDownload,
  mdiEmailOutline,
  mdiFileDelimited,
  mdiInformationOutline,
  mdiPlus,
  mdiUpload,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useMemo, useState } from 'react'


// ─── Backend response types ───────────────────────────────────────────────────

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
const PAGE_SIZE = 50

interface EditableRow {
  id: string
  realName: string
  email: string
  userNameOverride: string  // blank = auto-generate from realName
  teamName: string
  stdNumber: string
  phone: string
  deleted: boolean
}

interface ColMap {
  realName: string
  email: string
  teamName: string
  stdNumber: string
  phone: string
}

interface Options {
  emailConfirmed: boolean
  teamMode: 'fromrow' | 'single' | 'none'
  singleTeamName: string
}

// ─── Utilities ───────────────────────────────────────────────────────────────

function splitCsvLine(line: string): string[] {
  const fields: string[] = []
  let field = ''
  let inQ = false
  for (let i = 0; i < line.length; i++) {
    const c = line[i]
    if (c === '"') {
      if (inQ && line[i + 1] === '"') { field += '"'; i++ }
      else inQ = !inQ
    } else if (c === ',' && !inQ) {
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
  if (!lines.length) return { headers: [], rowCount: 0 }
  return { headers: splitCsvLine(lines[0]), rowCount: lines.length - 1 }
}

function csvToEditableRows(text: string, headers: string[], map: ColMap): EditableRow[] {
  const lines = text.trim().split(/\r?\n/).filter((l) => l.trim()).slice(1)
  return lines.map((line, i) => {
    const fields = splitCsvLine(line)
    const get = (col: string) => col !== NONE ? ((fields[headers.indexOf(col)] ?? '')).trim() : ''
    return {
      id: `${i}-${Date.now()}`,
      realName: get(map.realName),
      email: get(map.email),
      userNameOverride: '',
      teamName: get(map.teamName),
      stdNumber: get(map.stdNumber),
      phone: get(map.phone),
      deleted: false,
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

/** Client-side username preview — same logic as backend CsvGenerateUsername */
function previewUsername(realName: string, override: string): string {
  if (override.trim()) return override.trim().slice(0, 15)
  const clean = realName.toLowerCase().replace(/\s+/g, '.').replace(/[^a-z0-9.]/g, '').slice(0, 15)
  return clean || 'user'
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
  const [map, setMap] = useState<ColMap>({ realName: NONE, email: NONE, teamName: NONE, stdNumber: NONE, phone: NONE })
  const [editableRows, setEditableRows] = useState<EditableRow[]>([])
  const [filterText, setFilterText] = useState('')
  const [page, setPage] = useState(1)
  const [opts, setOpts] = useState<Options>({ emailConfirmed: true, teamMode: 'fromrow', singleTeamName: '' })
  const [loading, setLoading] = useState(false)
  const [importResult, setImportResult] = useState<CsvImportResult | null>(null)
  const [importError, setImportError] = useState<string | null>(null)
  const [sendingEmail, setSendingEmail] = useState(false)
  const [emailSendResult, setEmailSendResult] = useState<{ sent: number; failed: number } | null>(null)

  // Step 0 → parse headers only
  const process = useCallback((text: string) => {
    const { headers: hdrs, rowCount } = parseCSVInfo(text)
    if (hdrs.length < 2 || rowCount < 1) {
      showNotification({ message: 'CSV must have at least a header row and one data row', color: 'red' })
      return
    }
    setHeaders(hdrs)
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

  // Step 1 preview (first 5 rows, no editing yet)
  const previewRows5 = useMemo(() => {
    return csvToEditableRows(rawText, headers, map).slice(0, 5)
  }, [rawText, headers, map])

  const canNext1 = map.email !== NONE && map.realName !== NONE

  // Step 1 → Step 2: parse all rows into editable state
  const toEditStep = () => {
    const rows = csvToEditableRows(rawText, headers, map)
    setEditableRows(rows)
    setFilterText('')
    setPage(1)
    setStep(2)
  }

  // Step 2 helpers
  const updateRow = (id: string, field: keyof EditableRow, value: string) =>
    setEditableRows((rows) => rows.map((r) => (r.id === id ? { ...r, [field]: value } : r)))

  const deleteRow = (id: string) =>
    setEditableRows((rows) => rows.map((r) => (r.id === id ? { ...r, deleted: true } : r)))

  const restoreRow = (id: string) =>
    setEditableRows((rows) => rows.map((r) => (r.id === id ? { ...r, deleted: false } : r)))

  const addRow = () => {
    const newRow: EditableRow = {
      id: `new-${Date.now()}`,
      realName: '', email: '', userNameOverride: '', teamName: '',
      stdNumber: '', phone: '', deleted: false,
    }
    setEditableRows((rows) => [...rows, newRow])
    // Jump to last page to show the new row
    const filtered = editableRows.filter((r) => !r.deleted)
    setPage(Math.ceil((filtered.length + 1) / PAGE_SIZE))
  }

  const activeRows = useMemo(
    () => editableRows.filter((r) => !r.deleted),
    [editableRows]
  )

  const filteredRows = useMemo(() => {
    const q = filterText.toLowerCase()
    return editableRows.filter(
      (r) =>
        !r.deleted &&
        (q === '' ||
          r.realName.toLowerCase().includes(q) ||
          r.email.toLowerCase().includes(q) ||
          r.teamName.toLowerCase().includes(q))
    )
  }, [editableRows, filterText])

  const totalPages = Math.max(1, Math.ceil(filteredRows.length / PAGE_SIZE))
  const pageRows = filteredRows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  const deletedCount = editableRows.filter((r) => r.deleted).length

  // Validation summary for step 2
  const invalidEmails = activeRows.filter((r) => !r.email || !r.email.includes('@'))
  const duplicateEmails = useMemo(() => {
    const seen = new Set<string>()
    const dupes = new Set<string>()
    activeRows.forEach((r) => {
      const e = r.email.toLowerCase()
      if (e && seen.has(e)) dupes.add(e)
      seen.add(e)
    })
    return dupes
  }, [activeRows])

  const hasErrors = invalidEmails.length > 0 || duplicateEmails.size > 0

  // Step 3 → run import
  const runImport = async () => {
    setLoading(true)
    setImportError(null)
    setStep(4)

    const rows = editableRows.filter((r) => !r.deleted)

    try {
      const resp = await fetch('/api/admin/users/import', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          rows: rows.map((r) => ({
            email: r.email,
            realName: r.realName,
            userNameOverride: r.userNameOverride || undefined,
            teamName: r.teamName || undefined,
            stdNumber: r.stdNumber || undefined,
            phone: r.phone || undefined,
          })),
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
    setStep(0); setRawText(''); setHeaders([]); setMap({ realName: NONE, email: NONE, teamName: NONE, stdNumber: NONE, phone: NONE })
    setEditableRows([]); setFilterText(''); setPage(1)
    setOpts({ emailConfirmed: true, teamMode: 'fromrow', singleTeamName: '' })
    setLoading(false); setImportResult(null); setImportError(null)
    setSendingEmail(false); setEmailSendResult(null)
  }

  const goFixFailedRows = () => {
    if (!importResult) return
    const failedRows: EditableRow[] = importResult.users
      .filter((u) => u.status === 'skipped')
      .map((u, i) => ({
        id: `retry-${i}-${Date.now()}`,
        realName: u.realName,
        email: u.email,
        userNameOverride: '',
        teamName: u.teamName ?? '',
        stdNumber: '',
        phone: '',
        deleted: false,
      }))
    setEditableRows(failedRows)
    setFilterText('')
    setPage(1)
    setEmailSendResult(null)
    setStep(2)
  }

  const sendCredentialsEmail = async () => {
    if (!importResult) return
    setSendingEmail(true)
    setEmailSendResult(null)
    const items = importResult.users
      .filter((u) => u.status !== 'skipped')
      .map((u) => ({ email: u.email, userName: u.userName }))

    try {
      const resp = await fetch('/api/admin/users/credentials/send', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ items }),
      })
      if (!resp.ok) {
        const err = await resp.json().catch(() => ({ title: 'Failed to send emails' }))
        throw new Error(err.title ?? 'Failed to send emails')
      }
      const result: { sent: number; failed: number } = await resp.json()
      setEmailSendResult(result)
      showNotification({
        message: result.failed === 0
          ? `Credentials sent to ${result.sent} user(s)`
          : `Sent: ${result.sent}, failed: ${result.failed}`,
        color: result.failed === 0 ? 'teal' : 'orange',
        icon: <Icon path={result.failed === 0 ? mdiCheck : mdiAlertCircleOutline} size={1} />,
      })
    } catch (e: any) {
      showNotification({ message: e?.message ?? 'Failed to send emails', color: 'red' })
    } finally {
      setSendingEmail(false)
    }
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
      size="90%"
      styles={{ body: { paddingTop: 0 } }}
      onClose={() => { reset(); props.onClose() }}
    >
      <Stack gap="lg" pt="md">
        <Stepper active={step} size="sm" allowNextStepsSelect={false}>
          <Stepper.Step label="Upload" description="Select file" />
          <Stepper.Step label="Map Columns" description="Field mapping" />
          <Stepper.Step label="Edit & Review" description="Fix mistakes" />
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

        {/* ── Step 1: Map Columns + 5-row Preview ── */}
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
                    <Badge variant="light">{parseCSVInfo(rawText).rowCount} rows total</Badge>
                  </Group>
                  <ScrollArea>
                    <Table striped highlightOnHover withTableBorder withColumnBorders fz="xs">
                      <Table.Thead>
                        <Table.Tr>
                          <Table.Th>Real Name</Table.Th>
                          <Table.Th>Email</Table.Th>
                          <Table.Th>Team</Table.Th>
                        </Table.Tr>
                      </Table.Thead>
                      <Table.Tbody>
                        {previewRows5.map((r, i) => (
                          <Table.Tr key={i}>
                            <Table.Td>{r.realName || <Text c="dimmed" fz="xs">—</Text>}</Table.Td>
                            <Table.Td ff="monospace">{r.email || <Text c="dimmed" fz="xs">—</Text>}</Table.Td>
                            <Table.Td>{r.teamName || <Text c="dimmed" fz="xs">—</Text>}</Table.Td>
                          </Table.Tr>
                        ))}
                      </Table.Tbody>
                    </Table>
                  </ScrollArea>
                  <Text size="xs" c="dimmed">Proceed to Edit & Review to change individual entries.</Text>
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
              <Button disabled={!canNext1} onClick={toEditStep}>Edit & Review →</Button>
            </Group>
          </Stack>
        )}

        {/* ── Step 2: Edit & Review (full editable table) ── */}
        {step === 2 && (
          <Stack gap="sm">
            <Group justify="space-between" wrap="nowrap">
              <Group gap="sm">
                <TextInput
                  size="xs"
                  placeholder="Filter by name / email / team…"
                  value={filterText}
                  onChange={(e) => { setFilterText(e.currentTarget.value); setPage(1) }}
                  w={260}
                />
                <Text size="sm" c="dimmed">
                  <strong>{activeRows.length}</strong> active
                  {deletedCount > 0 && <>, <Text span c="orange">{deletedCount} deleted</Text></>}
                </Text>
                {hasErrors && (
                  <Badge color="red" variant="light">
                    {invalidEmails.length + duplicateEmails.size} error(s)
                  </Badge>
                )}
              </Group>
              <Button
                size="xs"
                variant="light"
                leftSection={<Icon path={mdiPlus} size={0.8} />}
                onClick={addRow}
              >
                Add Row
              </Button>
            </Group>

            <Alert icon={<Icon path={mdiInformationOutline} size={1} />} color="blue" p="xs">
              <Text size="xs">
                Edit any cell inline. <strong>Username Override</strong> — leave blank to auto-generate from Real Name.
                Red X deletes the row (undo by clicking Restore). Deleted rows are not imported.
              </Text>
            </Alert>

            <Paper withBorder>
              <ScrollArea h="calc(100vh - 460px)" mih={200}>
                <Table striped highlightOnHover withColumnBorders fz="xs" style={{ minWidth: 780 }}>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th style={{ width: 160 }}>Real Name</Table.Th>
                      <Table.Th style={{ width: 200 }}>Email *</Table.Th>
                      <Table.Th style={{ width: 140 }}>
                        <Tooltip label="Leave blank to auto-generate from Real Name" position="top" withArrow>
                          <Text size="xs" style={{ cursor: 'help', textDecoration: 'underline dotted' }}>Username Override</Text>
                        </Tooltip>
                      </Table.Th>
                      <Table.Th style={{ width: 130 }}>Team</Table.Th>
                      <Table.Th style={{ width: 100 }}>Student ID</Table.Th>
                      <Table.Th style={{ width: 30 }} />
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {pageRows.map((row) => {
                      const emailInvalid = row.email !== '' && !row.email.includes('@')
                      const emailDupe = duplicateEmails.has(row.email.toLowerCase())
                      return (
                        <Table.Tr
                          key={row.id}
                          style={
                            emailInvalid || emailDupe
                              ? { backgroundColor: 'var(--mantine-color-red-0)' }
                              : undefined
                          }
                        >
                          <Table.Td>
                            <TextInput
                              size="xs"
                              variant="unstyled"
                              value={row.realName}
                              onChange={(e) => updateRow(row.id, 'realName', e.currentTarget.value)}
                              placeholder="Real Name"
                            />
                          </Table.Td>
                          <Table.Td>
                            <Tooltip
                              label={emailDupe ? 'Duplicate email' : emailInvalid ? 'Invalid email' : undefined}
                              disabled={!emailInvalid && !emailDupe}
                              color="red"
                            >
                              <TextInput
                                size="xs"
                                variant="unstyled"
                                value={row.email}
                                onChange={(e) => updateRow(row.id, 'email', e.currentTarget.value)}
                                placeholder="user@example.com"
                                ff="monospace"
                                error={emailInvalid || emailDupe}
                              />
                            </Tooltip>
                          </Table.Td>
                          <Table.Td>
                            <Tooltip
                              label={`Auto-preview: ${previewUsername(row.realName, row.userNameOverride)}`}
                              position="top"
                            >
                              <TextInput
                                size="xs"
                                variant="unstyled"
                                value={row.userNameOverride}
                                onChange={(e) => updateRow(row.id, 'userNameOverride', e.currentTarget.value)}
                                placeholder={`auto: ${previewUsername(row.realName, '')}`}
                                ff="monospace"
                                maxLength={15}
                              />
                            </Tooltip>
                          </Table.Td>
                          <Table.Td>
                            <TextInput
                              size="xs"
                              variant="unstyled"
                              value={row.teamName}
                              onChange={(e) => updateRow(row.id, 'teamName', e.currentTarget.value)}
                              placeholder="Team"
                            />
                          </Table.Td>
                          <Table.Td>
                            <TextInput
                              size="xs"
                              variant="unstyled"
                              value={row.stdNumber}
                              onChange={(e) => updateRow(row.id, 'stdNumber', e.currentTarget.value)}
                              placeholder="ID"
                              ff="monospace"
                            />
                          </Table.Td>
                          <Table.Td>
                            <Tooltip label="Delete row (won't be imported)" position="left">
                              <ActionIcon
                                color="red"
                                size="sm"
                                variant="subtle"
                                onClick={() => deleteRow(row.id)}
                              >
                                <Icon path={mdiDeleteOutline} size={0.8} />
                              </ActionIcon>
                            </Tooltip>
                          </Table.Td>
                        </Table.Tr>
                      )
                    })}

                    {/* Show deleted rows at the bottom (greyed out with Restore) */}
                    {filterText === '' &&
                      editableRows
                        .filter((r) => r.deleted)
                        .map((row) => (
                          <Table.Tr key={row.id} style={{ opacity: 0.35 }}>
                            <Table.Td colSpan={5}>
                              <Text size="xs" ff="monospace">
                                {row.realName} — {row.email} — {row.teamName}
                              </Text>
                            </Table.Td>
                            <Table.Td>
                              <Tooltip label="Restore row" position="left">
                                <ActionIcon
                                  color="teal"
                                  size="sm"
                                  variant="subtle"
                                  onClick={() => restoreRow(row.id)}
                                >
                                  <Icon path={mdiCheck} size={0.8} />
                                </ActionIcon>
                              </Tooltip>
                            </Table.Td>
                          </Table.Tr>
                        ))}

                    {filteredRows.length === 0 && (
                      <Table.Tr>
                        <Table.Td colSpan={6}>
                          <Text ta="center" c="dimmed" size="sm" py="md">
                            {filterText ? 'No rows match the filter.' : 'No rows.'}
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    )}
                  </Table.Tbody>
                </Table>
              </ScrollArea>
            </Paper>

            {/* Pagination */}
            {totalPages > 1 && (
              <Group justify="center" gap="xs">
                <ActionIcon size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                  ‹
                </ActionIcon>
                <Text size="sm">Page {page} / {totalPages}</Text>
                <ActionIcon size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                  ›
                </ActionIcon>
              </Group>
            )}

            {hasErrors && (
              <Alert icon={<Icon path={mdiAlertCircleOutline} size={1} />} color="orange">
                <Text size="sm">
                  {invalidEmails.length > 0 && <>{invalidEmails.length} row(s) have invalid emails. </>}
                  {duplicateEmails.size > 0 && <>{duplicateEmails.size} duplicate email(s). </>}
                  Fix or delete these rows before proceeding — the server will skip them.
                </Text>
              </Alert>
            )}

            <Group justify="space-between">
              <Button variant="outline" onClick={() => setStep(1)}>← Back</Button>
              <Button disabled={activeRows.length === 0} onClick={() => setStep(3)}>
                Options → ({activeRows.length} rows)
              </Button>
            </Group>
          </Stack>
        )}

        {/* ── Step 3: Options ── */}
        {step === 3 && (
          <Stack gap="md">
            <Alert icon={<Icon path={mdiInformationOutline} size={1} />} color="blue">
              <Stack gap={4}>
                <Text size="sm">
                  <strong>{activeRows.length}</strong> rows reviewed and ready. The server generates unique usernames
                  (respecting any overrides) and secure passwords in a single atomic transaction.
                </Text>
                <Text size="xs" c="dimmed">
                  Download the credentials CSV after import — passwords are not stored beyond this session.
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
                    <Radio value="fromrow" label="Use team name from each row (from the Team column in your CSV)" />
                    <Radio value="single" label="Override — assign all users to a single team:" />
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
              <Button variant="outline" onClick={() => setStep(2)}>← Back to Edit</Button>
              <Button
                color="green"
                leftSection={<Icon path={mdiUpload} size={0.9} />}
                disabled={opts.teamMode === 'single' && !opts.singleTeamName.trim()}
                onClick={runImport}
              >
                Import {activeRows.length} Users
              </Button>
            </Group>
          </Stack>
        )}

        {/* ── Step 4: Result ── */}
        {step === 4 && (
          <Stack gap="md">
            {loading && (
              <Stack align="center" gap="md" py="xl">
                <Loader size="lg" />
                <Text c="dimmed" size="sm">
                  Importing {activeRows.length} users — the server is generating credentials and creating accounts…
                </Text>
              </Stack>
            )}

            {!loading && importError && (
              <Stack gap="sm">
                <Alert icon={<Icon path={mdiAlertCircleOutline} size={1} />} color="red" title="Import failed">
                  <Text size="sm">{importError}</Text>
                </Alert>
                <Group>
                  <Button variant="outline" onClick={() => setStep(3)}>← Go Back</Button>
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
                      <ScrollArea h={80}>
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

                {emailSendResult && (
                  <Alert
                    icon={<Icon path={emailSendResult.failed === 0 ? mdiCheckCircleOutline : mdiAlertCircleOutline} size={1} />}
                    color={emailSendResult.failed === 0 ? 'teal' : 'orange'}
                  >
                    <Text size="sm">
                      Credentials emailed: <strong>{emailSendResult.sent}</strong> sent
                      {emailSendResult.failed > 0 && <>, <strong>{emailSendResult.failed}</strong> failed (check SMTP config)</>}.
                    </Text>
                  </Alert>
                )}

                <Group justify="space-between">
                  <Group gap="sm">
                    <Button variant="outline" onClick={reset}>Import Another File</Button>
                    {importResult.skipped > 0 && (
                      <Button variant="light" color="orange" onClick={goFixFailedRows}>
                        ← Fix Failed Rows ({importResult.skipped})
                      </Button>
                    )}
                  </Group>
                  <Group gap="sm">
                    {importResult.created + importResult.updated > 0 && (
                      <>
                        <Button
                          variant="outline"
                          leftSection={sendingEmail ? <Loader size="xs" /> : <Icon path={mdiEmailOutline} size={0.9} />}
                          loading={sendingEmail}
                          disabled={sendingEmail || !!emailSendResult}
                          onClick={sendCredentialsEmail}
                        >
                          {emailSendResult ? `Sent ${emailSendResult.sent}` : 'Send Credentials Email'}
                        </Button>
                        <Button
                          leftSection={<Icon path={mdiDownload} size={0.9} />}
                          onClick={() => triggerDownload(buildCredentialsCsv(importResult.users), 'imported_credentials.csv')}
                        >
                          Download Credentials ({importResult.created + importResult.updated})
                        </Button>
                      </>
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
