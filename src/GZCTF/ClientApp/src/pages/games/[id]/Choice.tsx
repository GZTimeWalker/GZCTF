import {
  Alert,
  Badge,
  Button,
  Card,
  Checkbox,
  Group,
  Loader,
  Progress,
  Radio,
  SimpleGrid,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { modals } from '@mantine/modals'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameTab } from '@Components/WithGameTab'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { ChoiceAttempt, ChoiceInfo, choiceError, choiceRequest } from '@Utils/ChoiceApi'
import { optionLabel } from '@Utils/ChoiceImport'
import { Role } from '@Api'

export default function Choice() {
  const id = Number(useParams().id)
  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <WithGameTab>
          <ChoicePaper key={id} id={id} />
        </WithGameTab>
      </WithRole>
    </WithNavBar>
  )
}

function ChoicePaper({ id }: { id: number }) {
  const { t } = useTranslation()
  const [info, setInfo] = useState<ChoiceInfo | null>(null)
  const [attempt, setAttempt] = useState<ChoiceAttempt | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const inFlight = useRef(false)
  const [error, setError] = useState('')
  const [index, setIndex] = useState(0)
  const [pending, setPending] = useState<{ questionId: number; options: number[] } | null>(null)
  const [confirming, setConfirming] = useState(false)

  const load = async () => {
    if (inFlight.current) return
    inFlight.current = true
    setLoading(true)
    setError('')
    try {
      const [config, saved] = await Promise.all([
        choiceRequest<ChoiceInfo>(id, 'info'),
        choiceRequest<ChoiceAttempt | null>(id, 'attempt'),
      ])
      setInfo(config)
      setAttempt(saved)
      setPending(null)
      if (saved) {
        const first = saved.questions.findIndex((q) => !saved.answers[q.id]?.length)
        setIndex(first < 0 ? 0 : first)
      }
    } catch (e) {
      setError(choiceError(e))
    } finally {
      inFlight.current = false
      setLoading(false)
    }
  }
  useEffect(() => {
    void load()
  }, [id])
  useEffect(() => {
    const warn = (e: BeforeUnloadEvent) => {
      if (pending || busy) e.preventDefault()
    }
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [pending, busy])

  const start = async () => {
    if (inFlight.current) return
    inFlight.current = true
    setBusy(true)
    setError('')
    try {
      setAttempt(await choiceRequest<ChoiceAttempt>(id, 'attempt', 'POST'))
    } catch (e) {
      setError(choiceError(e))
    } finally {
      inFlight.current = false
      setBusy(false)
    }
  }
  const save = async (questionId: number, options: number[]) => {
    if (!attempt || attempt.submittedAt || inFlight.current) return
    inFlight.current = true
    setBusy(true)
    setError('')
    setPending({ questionId, options })
    try {
      const saved = await choiceRequest<ChoiceAttempt>(id, `answers/${questionId}`, 'PUT', {
        version: attempt.version,
        selectedOptions: options,
      })
      setAttempt(saved)
      setPending(null)
    } catch (e) {
      setError(choiceError(e))
    } finally {
      inFlight.current = false
      setBusy(false)
    }
  }
  const submit = async () => {
    if (!attempt || pending || inFlight.current) return
    inFlight.current = true
    setBusy(true)
    setError('')
    try {
      setAttempt(await choiceRequest<ChoiceAttempt>(id, 'submit', 'POST', { version: attempt.version }))
    } catch (e) {
      setError(choiceError(e))
    } finally {
      inFlight.current = false
      setBusy(false)
    }
  }
  const confirmSubmit = () => {
    setConfirming(true)
    modals.openConfirmModal({
      title: t('choice.submit_confirm'),
      children: <Text>{t('choice.submit_warning')}</Text>,
      onConfirm: () => {
        setConfirming(false)
        void submit()
      },
      onCancel: () => setConfirming(false),
      onClose: () => setConfirming(false),
      confirmProps: { color: 'orange' },
    })
  }

  if (loading)
    return (
      <Group justify="center">
        <Loader />
        <Text>{t('choice.restoring')}</Text>
      </Group>
    )
  const question = attempt?.questions[index]
  const answered = attempt ? Object.values(attempt.answers).filter((a) => a.length).length : 0
  const complete = !!attempt && answered === attempt.questions.length
  const submitted = !!attempt?.submittedAt
  const selected = question
    ? ((pending?.questionId === question.id ? pending.options : attempt?.answers[question.id]) ?? [])
    : []
  const total = attempt?.questions.reduce((sum, q) => sum + q.score, 0) ?? 0
  const disabled = busy || submitted || confirming
  const reload = () =>
    pending
      ? modals.openConfirmModal({
          title: t('choice.reload_confirm'),
          children: <Text>{t('choice.discard_pending')}</Text>,
          onConfirm: () => void load(),
        })
      : void load()

  return (
    <Stack maw={1100} w="100%" mx="auto" pb="xl">
      {error && (
        <Alert color="red" role="alert">
          <Stack gap="xs">
            <Text>{error}</Text>
            <Group>
              {pending && (
                <Button size="xs" onClick={() => save(pending.questionId, pending.options)} loading={busy}>
                  {t('choice.retry_save')}
                </Button>
              )}
              <Button size="xs" variant="light" disabled={busy} onClick={reload}>
                {t('choice.reload')}
              </Button>
            </Group>
          </Stack>
        </Alert>
      )}
      {!attempt ? (
        <Card withBorder>
          <Stack>
            <Title order={3}>{t('choice.title')}</Title>
            <Text>{t('choice.player_help')}</Text>
            {info && (
              <Text>
                {t('choice.paper_info', {
                  single: info.singleCount,
                  multiple: info.multipleCount,
                  singleScore: info.singleScore,
                  multipleScore: info.multipleScore,
                })}
              </Text>
            )}
            <Button onClick={start} loading={busy} disabled={!info?.enabled}>
              {t('choice.start')}
            </Button>
          </Stack>
        </Card>
      ) : (
        <>
          <Card withBorder>
            <Stack>
              <Group justify="space-between">
                <Title order={3}>{t('choice.title')}</Title>
                <Badge color={submitted ? 'teal' : pending ? 'orange' : 'blue'}>
                  {t(
                    submitted
                      ? 'choice.submitted'
                      : busy
                        ? 'choice.saving'
                        : pending
                          ? 'choice.unsaved_answer'
                          : 'choice.saved'
                  )}
                </Badge>
              </Group>
              <Text>{t('choice.answered', { answered, count: attempt.questions.length, score: total })}</Text>
              <Progress value={(answered / attempt.questions.length) * 100} />
              <Text size="sm" c="dimmed" role="status" aria-live="polite">
                {t(busy ? 'choice.saving' : pending ? 'choice.unsaved_answer' : 'choice.saved_at', {
                  time: new Date(attempt.updatedAt).toLocaleString(),
                })}
              </Text>
              {submitted && (
                <Alert color="teal">
                  {t('choice.final_result', {
                    score: attempt.score,
                    total,
                    time: new Date(attempt.submittedAt!).toLocaleString(),
                  })}
                </Alert>
              )}
              <SimpleGrid cols={{ base: 5, sm: 10, lg: 15 }} spacing="xs">
                {attempt.questions.map((q, i) => (
                  <Button
                    key={q.id}
                    size="compact-sm"
                    variant={index === i ? 'filled' : 'light'}
                    color={attempt.answers[q.id]?.length ? 'teal' : 'gray'}
                    disabled={busy || !!pending || confirming}
                    onClick={() => setIndex(i)}
                    aria-label={t('choice.question_number', { number: i + 1 })}
                  >
                    {i + 1}
                  </Button>
                ))}
              </SimpleGrid>
            </Stack>
          </Card>
          {question && (
            <Card withBorder>
              <Stack gap="lg">
                <Group>
                  <Badge>{t(`choice.${question.type}`)}</Badge>
                  <Text fw={600}>
                    #{index + 1} · {question.score} {t('choice.points')}
                  </Text>
                </Group>
                <Text style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{question.content}</Text>
                {question.type === 'Single' ? (
                  <Radio.Group
                    aria-label={t('choice.select_answer')}
                    value={selected.length ? String(selected[0]) : ''}
                    onChange={(value) => void save(question.id, [Number(value)])}
                  >
                    <Stack>
                      {question.options.map((option, i) => (
                        <Radio key={i} value={String(i)} disabled={disabled} label={`${optionLabel(i)}. ${option}`} />
                      ))}
                    </Stack>
                  </Radio.Group>
                ) : (
                  <Checkbox.Group
                    aria-label={t('choice.select_answer')}
                    value={selected.map(String)}
                    onChange={(values) => void save(question.id, values.map(Number))}
                  >
                    <Stack>
                      {question.options.map((option, i) => (
                        <Checkbox
                          key={i}
                          value={String(i)}
                          disabled={disabled}
                          label={`${optionLabel(i)}. ${option}`}
                        />
                      ))}
                    </Stack>
                  </Checkbox.Group>
                )}
                <Group justify="space-between">
                  <Button
                    variant="default"
                    disabled={index === 0 || busy || !!pending || confirming}
                    onClick={() => setIndex((i) => i - 1)}
                  >
                    {t('choice.previous')}
                  </Button>
                  <Button
                    disabled={index === attempt.questions.length - 1 || busy || !!pending || confirming}
                    onClick={() => setIndex((i) => i + 1)}
                  >
                    {t('choice.next')}
                  </Button>
                </Group>
              </Stack>
            </Card>
          )}
          {!submitted && (
            <Group justify="space-between">
              <Text size="sm" c="dimmed">
                {t('choice.submit_help')}
              </Text>
              <Button
                color="orange"
                disabled={!complete || busy || !!pending || confirming}
                loading={busy}
                onClick={confirmSubmit}
              >
                {t('choice.submit')}
              </Button>
            </Group>
          )}
        </>
      )}
    </Stack>
  )
}
