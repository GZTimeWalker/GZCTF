import {
  Alert,
  Badge,
  Button,
  Card,
  FileButton,
  Group,
  Modal,
  NumberInput,
  Pagination,
  Select,
  SimpleGrid,
  Stack,
  Switch,
  Table,
  Text,
  Textarea,
  TextInput,
  Title,
} from '@mantine/core'
import { modals } from '@mantine/modals'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { ChoiceConfig, ChoiceQuestion, ChoiceResult, choiceError, choiceRequest } from '@Utils/ChoiceApi'
import {
  downloadChoiceFile,
  exampleQuestions,
  optionLabel,
  parseAnswerLetters,
  parseChoiceImport,
  validateQuestion,
} from '@Utils/ChoiceImport'

const emptyConfig: ChoiceConfig = {
  enabled: false,
  singleCount: 0,
  multipleCount: 0,
  singleScore: 1,
  multipleScore: 2,
  version: 0,
  locked: false,
  questions: [],
}

export default function Choice() {
  const id = Number(useParams().id)
  return <ChoiceEditor key={id} id={id} />
}

function ChoiceEditor({ id }: { id: number }) {
  const { t } = useTranslation()
  const [config, setConfig] = useState<ChoiceConfig>(emptyConfig)
  const [results, setResults] = useState<ChoiceResult[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [dirty, setDirty] = useState(false)
  const [page, setPage] = useState(1)
  const [editIndex, setEditIndex] = useState<number | null>(null)
  const [question, setQuestion] = useState<ChoiceQuestion>(exampleQuestions[0])
  const [options, setOptions] = useState('')
  const [answers, setAnswers] = useState('')
  const [editError, setEditError] = useState('')
  const [importMode, setImportMode] = useState<string | null>('append')

  const load = async () => {
    setLoading(true)
    setError('')
    try {
      const [data, scores] = await Promise.all([
        choiceRequest<ChoiceConfig>(id, 'config'),
        choiceRequest<ChoiceResult[]>(id, 'results'),
      ])
      setConfig(data)
      setResults(scores)
      setDirty(false)
    } catch (e) {
      setError(choiceError(e))
    } finally {
      setLoading(false)
    }
  }
  useEffect(() => {
    void load()
  }, [id])
  useEffect(() => {
    const warn = (e: BeforeUnloadEvent) => {
      if (dirty) e.preventDefault()
    }
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [dirty])

  const change = (data: Partial<ChoiceConfig>) => {
    setConfig((c) => ({ ...c, ...data }))
    setDirty(true)
    setNotice('')
  }
  const locked = loading || busy || config.locked
  const counts = {
    Single: config.questions.filter((q) => q.type === 'Single').length,
    Multiple: config.questions.filter((q) => q.type === 'Multiple').length,
  }
  const save = async () => {
    setBusy(true)
    setError('')
    setNotice('')
    try {
      setConfig(await choiceRequest<ChoiceConfig>(id, 'config', 'PUT', config))
      setDirty(false)
      setNotice(t('choice.saved_config'))
    } catch (e) {
      setError(choiceError(e))
    } finally {
      setBusy(false)
    }
  }
  const importFile = async (file: File | null) => {
    if (!file) return
    setError('')
    try {
      if (file.size > 20 * 1024 * 1024) throw new Error(t('choice.file_too_large'))
      const imported = parseChoiceImport(await file.text(), file.name)
      const questions = importMode === 'replace' ? imported : [...config.questions, ...imported]
      if (questions.length > 5000) throw new Error(t('choice.bank_limit'))
      change({ questions })
      setPage(1)
      setNotice(t('choice.imported', { count: imported.length }))
    } catch (e) {
      setError(choiceError(e))
    }
  }
  const openEdit = (index: number) => {
    const q =
      index < 0
        ? { type: 'Single' as const, content: '', options: ['', ''], correctAnswers: [] }
        : config.questions[index]
    setQuestion(q)
    setOptions(q.options.join('\n'))
    setAnswers(q.correctAnswers.map(optionLabel).join(','))
    setEditError('')
    setEditIndex(index)
  }
  const applyQuestion = () => {
    try {
      const q = validateQuestion(
        { ...question, options: options.split('\n').map((o) => o.trim()), correctAnswers: parseAnswerLetters(answers) },
        (editIndex ?? 0) + 1
      )
      const questions = [...config.questions]
      if (editIndex === -1) questions.push(q)
      else questions[editIndex!] = q
      change({ questions })
      setEditIndex(null)
    } catch (e) {
      setEditError(choiceError(e))
    }
  }

  return (
    <WithGameEditTab
      isLoading={loading}
      head={
        <>
          <Title order={3}>{t('choice.title')}</Title>
          <Group>
            <Button
              variant="default"
              onClick={() =>
                dirty
                  ? modals.openConfirmModal({
                      title: t('choice.reload_confirm'),
                      children: <Text>{t('choice.unsaved')}</Text>,
                      onConfirm: () => void load(),
                    })
                  : void load()
              }
            >
              {t('choice.reload')}
            </Button>
            <Button loading={busy} disabled={locked || !dirty} onClick={save}>
              {t('choice.save_config')}
            </Button>
          </Group>
        </>
      }
    >
      {error && (
        <Alert color="red" role="alert">
          {error}
        </Alert>
      )}
      {notice && <Alert color="teal">{notice}</Alert>}
      <Alert color={config.locked ? 'yellow' : 'blue'}>
        {t(config.locked ? 'choice.config_locked' : 'choice.config_help')}
      </Alert>
      <Card withBorder>
        <Stack>
          <Switch
            label={t('choice.enable')}
            checked={config.enabled}
            disabled={locked}
            onChange={(e) => change({ enabled: e.currentTarget.checked })}
          />
          <SimpleGrid cols={4}>
            {(['singleCount', 'multipleCount', 'singleScore', 'multipleScore'] as const).map((field) => (
              <NumberInput
                key={field}
                label={t(`choice.${field}`)}
                value={config[field]}
                disabled={locked}
                allowDecimal={false}
                allowNegative={false}
                min={field.endsWith('Count') ? 0 : 1}
                max={field.endsWith('Count') ? 1000 : 10000}
                onChange={(v) => change({ [field]: Number(v) })}
              />
            ))}
          </SimpleGrid>
          <Text>
            {t('choice.total', {
              count: config.singleCount + config.multipleCount,
              score: config.singleCount * config.singleScore + config.multipleCount * config.multipleScore,
            })}
          </Text>
          {(config.singleCount > counts.Single || config.multipleCount > counts.Multiple) && (
            <Text c="red">{t('choice.insufficient')}</Text>
          )}
        </Stack>
      </Card>
      <Card withBorder>
        <Stack>
          <Group justify="space-between">
            <Title order={4}>{t('choice.bank')}</Title>
            <Text>{t('choice.bank_counts', { single: counts.Single, multiple: counts.Multiple })}</Text>
          </Group>
          <Text size="sm" c="dimmed">
            {t('choice.import_help')}
          </Text>
          <Group>
            <Button disabled={locked || config.questions.length >= 5000} onClick={() => openEdit(-1)}>
              {t('choice.add')}
            </Button>
            <Select
              aria-label={t('choice.import_mode')}
              data={[
                { value: 'append', label: t('choice.append') },
                { value: 'replace', label: t('choice.replace') },
              ]}
              value={importMode}
              onChange={setImportMode}
              disabled={locked}
              w={140}
            />
            <FileButton onChange={importFile} accept=".json,.csv">
              {(props) => (
                <Button {...props} variant="light" disabled={locked}>
                  {t('choice.import')}
                </Button>
              )}
            </FileButton>
            <Button
              variant="subtle"
              onClick={() => downloadChoiceFile('choice-template.json', JSON.stringify(exampleQuestions, null, 2))}
            >
              {t('choice.json_template')}
            </Button>
            <Button
              variant="subtle"
              onClick={() =>
                downloadChoiceFile(
                  'choice-template.csv',
                  '\uFEFFtype,content,options,answers\r\nSingle,HTTP 默认端口是？,80|443|22|53,A\r\nMultiple,下列哪些属于非对称加密算法？,RSA|AES|ECC|DES,"A,C"\r\n',
                  'text/csv;charset=utf-8'
                )
              }
            >
              {t('choice.csv_template')}
            </Button>
            <Button
              variant="subtle"
              disabled={!config.questions.length}
              onClick={() => downloadChoiceFile('choice-bank.json', JSON.stringify(config.questions, null, 2))}
            >
              {t('choice.export')}
            </Button>
          </Group>
          {config.questions.slice((page - 1) * 20, page * 20).map((q, offset) => {
            const index = (page - 1) * 20 + offset
            return (
              <Card key={index} withBorder>
                <Group justify="space-between" wrap="nowrap">
                  <Stack gap={4} style={{ minWidth: 0 }}>
                    <Group>
                      <Badge>{t(`choice.${q.type}`)}</Badge>
                      <Text size="sm">#{index + 1}</Text>
                    </Group>
                    <Text lineClamp={2}>{q.content}</Text>
                    <Text size="sm" c="dimmed">
                      {t('choice.correct')}: {q.correctAnswers.map(optionLabel).join(', ')}
                    </Text>
                  </Stack>
                  <Group wrap="nowrap">
                    <Button variant="subtle" disabled={locked} onClick={() => openEdit(index)}>
                      {t('choice.edit')}
                    </Button>
                    <Button
                      variant="subtle"
                      color="red"
                      disabled={locked}
                      onClick={() => {
                        change({ questions: config.questions.filter((_, i) => i !== index) })
                        setPage(1)
                      }}
                    >
                      {t('choice.remove')}
                    </Button>
                  </Group>
                </Group>
              </Card>
            )
          })}
          {!config.questions.length && <Text c="dimmed">{t('choice.empty_bank')}</Text>}
          <Pagination total={Math.max(1, Math.ceil(config.questions.length / 20))} value={page} onChange={setPage} />
        </Stack>
      </Card>
      <Card withBorder>
        <Stack>
          <Title order={4}>{t('choice.results')}</Title>
          <Table.ScrollContainer minWidth={650}>
            <Table>
              <Table.Thead>
                <Table.Tr>
                  {['team', 'progress', 'status', 'score', 'updated'].map((key) => (
                    <Table.Th key={key}>{t(`choice.${key}`)}</Table.Th>
                  ))}
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {results.map((r) => (
                  <Table.Tr key={r.teamId}>
                    <Table.Td>{r.teamName}</Table.Td>
                    <Table.Td>
                      {r.answeredCount}/{r.questionCount}
                    </Table.Td>
                    <Table.Td>{t(r.submittedAt ? 'choice.submitted' : 'choice.draft')}</Table.Td>
                    <Table.Td>{r.score ?? '—'}</Table.Td>
                    <Table.Td>{new Date(r.updatedAt).toLocaleString()}</Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Table.ScrollContainer>
          {!results.length && <Text c="dimmed">{t('choice.no_results')}</Text>}
        </Stack>
      </Card>
      <Modal opened={editIndex !== null} onClose={() => setEditIndex(null)} title={t('choice.edit_question')} size="lg">
        <Stack>
          {editError && <Alert color="red">{editError}</Alert>}
          <Select
            label={t('choice.type')}
            data={[
              { value: 'Single', label: t('choice.Single') },
              { value: 'Multiple', label: t('choice.Multiple') },
            ]}
            value={question.type}
            onChange={(v) => setQuestion((q) => ({ ...q, type: v as ChoiceQuestion['type'] }))}
          />
          <Textarea
            label={t('choice.content')}
            value={question.content}
            onChange={(e) => {
              const content = e.currentTarget.value
              setQuestion((q) => ({ ...q, content }))
            }}
            minRows={3}
            autosize
            maxLength={10000}
          />
          <Textarea
            label={t('choice.options')}
            description={t('choice.options_help')}
            value={options}
            onChange={(e) => setOptions(e.currentTarget.value)}
            minRows={4}
            autosize
          />
          <TextInput
            label={t('choice.correct')}
            description={t('choice.answers_help')}
            value={answers}
            onChange={(e) => setAnswers(e.currentTarget.value)}
          />
          <Button onClick={applyQuestion}>{t('choice.apply')}</Button>
        </Stack>
      </Modal>
    </WithGameEditTab>
  )
}
