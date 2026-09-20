import useSWR from 'swr'
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

const localeQuery = (locale: string) => encodeURIComponent(locale || 'en')

export const learningKeys = {
  paths: (locale: string) => `/api/learning-paths?locale=${localeQuery(locale)}`,
  preview: (slug: string, locale: string) => `/api/learning-paths/${encodeURIComponent(slug)}/preview?locale=${localeQuery(locale)}`,
  enrollments: (locale: string) => `/api/learning-paths/enrollments?locale=${localeQuery(locale)}`,
  record: (locale: string) => `/api/my-learning?locale=${localeQuery(locale)}`,
}

export const useLearningPaths = (locale: string) =>
  useSWR<LearningPathSummary[]>(learningKeys.paths(locale), fetcher)

export const useLearningPathPreview = (slug: string | undefined, locale: string) =>
  useSWR<LearningPathPreview>(slug ? learningKeys.preview(slug, locale) : null, fetcher)

