import useSWR from 'swr'
import { useSWRConfig } from 'swr'
import { fetcher } from '@Api'

export type LearningPathSummary = {
  pathId: string
  slug: string
  title: string
  summary: string
  moduleCount: number
  itemCount: number
  expectedMinutes: number
}

export type LearningItemPreview = {
  id: string
  sortOrder: number
  kind: 'lesson' | 'challenge' | string
  contentId: string
  title: string
  summary: string
}

export type LearningModulePreview = {
  id: string
  sortOrder: number
  expectedMinutes: number
  title: string
  summary: string
  items: LearningItemPreview[]
}

export type LearningPathPreview = {
  pathId: string
  revisionId: string
  slug: string
  title: string
  summary: string
  modules: LearningModulePreview[]
}

export type LearningEnrollment = {
  enrollmentId: string
  pathId: string
  slug: string
  title: string
  isCurrent: boolean
  enrolledAtUtc: string
}

export type LessonContent = {
  lessonId: string
  locale: string
  title: string
  body: string
}

const localeQuery = (locale: string) => encodeURIComponent(locale || 'en')

export const learningKeys = {
  paths: (locale: string) => `/api/learning-paths?locale=${localeQuery(locale)}`,
  preview: (slug: string, locale: string) => `/api/learning-paths/${encodeURIComponent(slug)}/preview?locale=${localeQuery(locale)}`,
  enrollments: (locale: string) => `/api/learning-paths/enrollments?locale=${localeQuery(locale)}`,
  record: (locale: string) => `/api/my-learning?locale=${localeQuery(locale)}`,
  lesson: (id: string, locale: string) => `/api/learning-lessons/${id}?locale=${localeQuery(locale)}`,
}

const request = async <T>(path: string, init?: RequestInit): Promise<T> => {
  const response = await fetch(path, { credentials: 'include', ...init })
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  return response.status === 204 ? (undefined as T) : (await response.json() as T)
}

export const useLearningPaths = (locale: string) =>
  useSWR<LearningPathSummary[]>(learningKeys.paths(locale), fetcher)

export const useLearningPathPreview = (slug: string | undefined, locale: string) =>
  useSWR<LearningPathPreview>(slug ? learningKeys.preview(slug, locale) : null, fetcher)

export const useLearningEnrollments = (locale: string, enabled: boolean) =>
  useSWR<LearningEnrollment[]>(enabled ? learningKeys.enrollments(locale) : null, fetcher)

export const useLesson = (id: string | undefined, locale: string, enabled: boolean) =>
  useSWR<LessonContent>(id && enabled ? learningKeys.lesson(id, locale) : null, fetcher)

export const useLearningMutations = () => {
  const { mutate } = useSWRConfig()

  const enroll = async (pathId: string, locale: string) => {
    const result = await request<LearningEnrollment>(
      `/api/learning-paths/${pathId}/enroll?locale=${localeQuery(locale)}`,
      { method: 'POST' }
    )
    await mutate((key) => typeof key === 'string' && key.startsWith('/api/learning-paths/enrollments'))
    return result
  }

  const selectCurrent = async (pathId: string, locale: string) => {
    const result = await request<LearningEnrollment>(
      `/api/learning-paths/${pathId}/select?locale=${localeQuery(locale)}`,
      { method: 'POST' }
    )
    await mutate((key) => typeof key === 'string' && key.startsWith('/api/learning-paths/enrollments'))
    return result
  }

  const completeLesson = async (lessonId: string) => {
    await request<void>(`/api/learning-lessons/${lessonId}/complete`, { method: 'POST' })
    await mutate((key) => typeof key === 'string' && (key.startsWith('/api/my-learning') || key.startsWith('/api/learning-paths/')))
  }

  return { enroll, selectCurrent, completeLesson }
}
