import { Alert, Button, Card, Code, Group, Stack, Text, Textarea, Title } from '@mantine/core'
import { Link } from 'react-router'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Markdown } from '@Components/MarkdownRenderer'
import {
  useChallenge,
  useChallengeInstance,
  useLearningMutations,
} from '@Hooks/useChallengeLibraryAdmin'
import { useUser } from '@Hooks/useUser'
import { useLanguage } from '@Utils/I18n'
import { useSWRConfig } from 'swr'
import { ChallengeInstanceStatus } from '@Api'

export type ChallengeWorkspaceProps = {
  challengeId: string
  backHref?: string
  previousHref?: string
  nextHref?: string
}

export const ChallengeWorkspace = ({ challengeId, backHref, previousHref, nextHref }: ChallengeWorkspaceProps) => {
  const { locale } = useLanguage()
  const { user } = useUser()
  const { t } = useTranslation('learning')
  const { data: challenge, error } = useChallenge(challengeId, locale, !!user)
  const { data: instance, mutate: mutateInstance } = useChallengeInstance(challengeId, !!user)
  const { mutate } = useSWRConfig()
  const { startInstance, extendInstance, stopInstance, submitChallenge, nextHint, revealWriteup } = useLearningMutations()
  const [flag, setFlag] = useState('')
  const [hints, setHints] = useState<string[]>([])
  const [writeup, setWriteup] = useState<string>()
  const [message, setMessage] = useState<string>()
  const [solveMode, setSolveMode] = useState<string>()

  if (!user) return <Text c="dimmed">{t('signInToStudy')}</Text>
  if (!challenge && !error) return <Text>{t('loading')}</Text>
  if (error) return <Text c="red">{t('loadFailed')}</Text>

  const run = async (action: () => Promise<unknown>) => {
    try {
      setMessage(undefined)
      await action()
    } catch {
      setMessage(t('actionFailed'))
    }
  }

  const submit = async () => {
    if (!flag.trim()) return
    try {
      const result = await submitChallenge(challengeId, flag)
      setMessage(result.accepted ? t('accepted') : t('rejected'))
      if (result.accepted) {
        setSolveMode(result.solveMode === undefined || result.solveMode === null ? undefined : String(result.solveMode))
        await mutate((key) => typeof key === 'string' && key.startsWith('/api/my-learning'))
      }
      setFlag('')
    } catch {
      setMessage(t('actionFailed'))
    }
  }

  const revealHint = async () => {
    await run(async () => {
      const hint = await nextHint(challengeId, locale)
      if (hint) setHints((current) => [...current, hint.content])
    })
  }

  const revealOfficialWriteup = async () => {
    await run(async () => {
      const result = await revealWriteup(challengeId, locale)
      if (result) setWriteup(result.content)
    })
  }

  return (
    <Stack gap="lg">
      {(backHref || previousHref || nextHref) && (
        <Group gap="xs">
          {backHref && (
            <Button component={Link} to={backHref} variant="subtle">
              ← Back
            </Button>
          )}
          {previousHref && (
            <Button component={Link} to={previousHref} variant="subtle">
              ← Previous
            </Button>
          )}
          {nextHref && (
            <Button component={Link} to={nextHref} variant="subtle">
              Next →
            </Button>
          )}
        </Group>
      )}
      <Card withBorder padding="xl">
        <Stack>
          <Group justify="space-between">
            <Title order={2}>{challenge!.title}</Title>
            <Code>{challenge!.type}</Code>
          </Group>
          <Text c="dimmed">{challenge!.summary}</Text>
          <Markdown source={challenge!.body} />
        </Stack>
      </Card>

      {(challenge!.hasAttachment || challenge!.hasContainer) && (
        <Card withBorder>
          <Stack>
            <Title order={3}>{t('instance')}</Title>
            {instance?.status === ChallengeInstanceStatus.Running ? (
              <Group>
                <Text>{instance.publicIp ?? t('instanceRunning')}:{instance.publicPort ?? ''}</Text>
                <Button variant="light" onClick={() => run(async () => { await extendInstance(challengeId); await mutateInstance() })}>{t('extend')}</Button>
                <Button color="red" variant="light" onClick={() => run(async () => { await stopInstance(challengeId); await mutateInstance(undefined, false) })}>{t('stop')}</Button>
              </Group>
            ) : challenge!.hasContainer ? (
              <Button onClick={() => run(async () => { await startInstance(challengeId); await mutateInstance() })}>{t('start')}</Button>
            ) : (
              <Button component="a" href={`/api/challenges/${challengeId}/attachment`}>{t('downloadAttachment')}</Button>
            )}
          </Stack>
        </Card>
      )}

      <Card withBorder>
        <Stack>
          <Title order={3}>{t('submit')}</Title>
          <Textarea value={flag} onChange={(event) => setFlag(event.currentTarget.value)} placeholder={t('flagPlaceholder')} />
          <Button onClick={submit}>{t('submit')}</Button>
          {message && <Alert>{message}</Alert>}
          {solveMode && <Text c="teal">{t('solveMode')}: {solveMode}</Text>}
        </Stack>
      </Card>

      {(challenge!.hintLocaleCount > 0 || challenge!.hasWriteup) && (
        <Card withBorder>
          <Stack>
            <Title order={3}>{t('help')}</Title>
            <Group>
              {challenge!.hintLocaleCount > 0 && <Button variant="light" onClick={revealHint}>{t('nextHint')}</Button>}
              {challenge!.hasWriteup && <Button variant="light" onClick={revealOfficialWriteup}>{t('showWriteup')}</Button>}
            </Group>
            {hints.map((hint, index) => <Alert key={`${index}-${hint}`}>{hint}</Alert>)}
            {writeup && <Markdown source={writeup} />}
          </Stack>
        </Card>
      )}
    </Stack>
  )
}
