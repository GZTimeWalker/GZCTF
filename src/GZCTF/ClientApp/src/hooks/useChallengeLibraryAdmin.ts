import useSWR, { useSWRConfig } from 'swr'
import api, { ChallengeInstanceStatus, ChallengeSolveMode, fetcher } from '@Api'

const localeQuery = (locale: string) => encodeURIComponent(locale || 'en')

export const useLesson = (id: string | undefined, locale: string, enabled: boolean) =>
  useSWR<{ lessonId: string; locale: string; title: string; body: string }>(
    id && enabled ? `/api/learning-lessons/${id}?locale=${localeQuery(locale)}` : null,
    fetcher,
  )

export const useChallenge = (id: string | undefined, locale: string, enabled: boolean) =>
  useSWR<{
    id: string
    title: string
    summary: string
    body: string
    type: string
    hintLocaleCount: number
    hasWriteup: boolean
    hasAttachment: boolean
    hasContainer: boolean
  }>(id && enabled ? `/api/challenges/${id}?locale=${localeQuery(locale)}` : null, fetcher)

export const useChallengeInstance = (id: string | undefined, enabled: boolean) =>
  useSWR<{
    id: string
    status: ChallengeInstanceStatus
    startedAtUtc?: string
    expiresAtUtc?: string
    publicIp?: string
    publicPort?: string
    attachmentFileName?: string
    attachmentSha256?: string
  }>(id && enabled ? `/api/challenges/${id}/instances` : null, fetcher)

export const useLearningMutations = () => {
  const { mutate } = useSWRConfig()

  const completeLesson = async (lessonId: string) => {
    await api.lessons.lessonsComplete(lessonId)
    await mutate((key: string) => typeof key === 'string' && key.startsWith('/api/my-learning'))
  }

  const startInstance = async (challengeId: string) => {
    const result = (await api.challengeInstances.challengeInstancesStart(challengeId)).data as {
      id: string
      status: ChallengeInstanceStatus
      startedAtUtc?: string
      expiresAtUtc?: string
      publicIp?: string
      publicPort?: string
      attachmentFileName?: string
      attachmentSha256?: string
    }
    await mutate(`/api/challenges/${challengeId}/instances`)
    return result
  }

  const extendInstance = async (challengeId: string) => {
    const result = (await api.challengeInstances.challengeInstancesExtend(challengeId)).data as {
      id: string
      status: ChallengeInstanceStatus
      startedAtUtc?: string
      expiresAtUtc?: string
      publicIp?: string
      publicPort?: string
      attachmentFileName?: string
      attachmentSha256?: string
    }
    await mutate(`/api/challenges/${challengeId}/instances`, result, false)
    return result
  }

  const stopInstance = async (challengeId: string) => {
    await api.challengeInstances.challengeInstancesStop(challengeId)
    await mutate(`/api/challenges/${challengeId}/instances`, undefined, false)
  }

  const submitChallenge = async (challengeId: string, flag: string) =>
    (await api.challengeSubmissions.challengeSubmissionsSubmit(challengeId, { flag })).data as {
      accepted: boolean
      firstSolve: boolean
      solveMode?: ChallengeSolveMode | null
      rejectionCode?: string
      submissionId: string
    }

  const nextHint = async (challengeId: string, locale: string) =>
    (await api.challenges.challengesNextHint(challengeId, { locale })).data as { hintId: string; sortOrder: number; locale: string; content: string } | null

  const revealWriteup = async (challengeId: string, locale: string) =>
    (await api.challenges.challengesWriteup(challengeId, { locale })).data as { locale: string; content: string; firstViewedAtUtc: number } | null

  return { completeLesson, startInstance, extendInstance, stopInstance, submitChallenge, nextHint, revealWriteup }
}

export const useAdminChallenges = () =>
  useSWR<{ id: string; type: string; publicationState: string; title: string; summary: string }[]>(
    '/api/admin/challenges?locale=en',
    fetcher,
  )

export const useAdminLessons = () =>
  useSWR<{ id: string; locale: string; title: string; body: string }[]>('/api/admin/lessons?locale=en', fetcher)

export const useImportBatches = () =>
  useSWR<
    { id: string; sourceType: string; state: string; challengeCount: number; pathCount: number; warningCount: number; errorCount: number; parityReportJson?: string; startedAtUtc: string; completedAtUtc?: string }[]
  >('/api/admin/imports', fetcher)

export async function adminRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { credentials: 'include', ...init })
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  return response.status === 204 ? (undefined as T) : await response.json() as T
}
