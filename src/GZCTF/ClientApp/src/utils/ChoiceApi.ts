import useSWR from 'swr'
import api, { ContentType, ParticipationStatus } from '@Api'

export type ChoiceType = 'Single' | 'Multiple'
export interface ChoiceQuestion {
  type: ChoiceType
  content: string
  options: string[]
  correctAnswers: number[]
}
export interface ChoiceConfig {
  enabled: boolean
  singleCount: number
  multipleCount: number
  singleScore: number
  multipleScore: number
  version: number
  locked: boolean
  questions: ChoiceQuestion[]
}
export type ChoiceInfo = Pick<
  ChoiceConfig,
  'enabled' | 'singleCount' | 'multipleCount' | 'singleScore' | 'multipleScore'
>
export interface ChoicePaperQuestion extends Omit<ChoiceQuestion, 'correctAnswers'> {
  id: number
  score: number
}
export interface ChoiceAttempt {
  questions: ChoicePaperQuestion[]
  answers: Record<number, number[]>
  version: number
  updatedAt: number
  submittedAt: number | null
  score: number | null
}
export interface ChoiceResult {
  teamId: number
  teamName: string
  updatedAt: number
  submittedAt: number | null
  answeredCount: number
  questionCount: number
  score: number | null
}

export const choiceRequest = async <T>(id: number, path: string, method = 'GET', body?: unknown): Promise<T> => {
  const response = await api.request<T>({
    path: `/api/game/${id}/choice/${path}`,
    method,
    body,
    type: ContentType.Json,
    format: 'json',
  })
  return response.status === 204 ? (null as T) : response.data
}

export const useChoiceInfo = (id: number, status?: ParticipationStatus) =>
  useSWR<ChoiceInfo>(
    status === ParticipationStatus.Accepted ? `/api/game/${id}/choice/info` : null,
    () => choiceRequest<ChoiceInfo>(id, 'info'),
    { keepPreviousData: false }
  )

export const choiceError = (error: unknown): string => {
  const e = error as {
    title?: string
    message?: string
    response?: { data?: { title?: string; errors?: Record<string, string[]> } }
  }
  const data = e?.response?.data
  return data?.errors
    ? Object.values(data.errors).flat().join('\n')
    : (data?.title ?? e?.title ?? e?.message ?? String(error))
}
