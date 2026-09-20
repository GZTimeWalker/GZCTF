import useSWR from 'swr'
import { fetcher } from '@Api'

export type AdminChallenge = { id: string; type: string; publicationState: string; title: string; summary: string }
export type AdminLesson = { id: string; locale: string; title: string; body: string }
export type AdminPath = { pathId: string; slug: string; title: string; isPublished: boolean; hasDraft: boolean }

export const adminLearningKeys = {
  challenges: '/api/admin/challenges?locale=en',
  lessons: '/api/admin/lessons?locale=en',
  paths: '/api/admin/learning-paths',
}

export const useAdminChallenges = () => useSWR<AdminChallenge[]>(adminLearningKeys.challenges, fetcher)
export const useAdminLessons = () => useSWR<AdminLesson[]>(adminLearningKeys.lessons, fetcher)
export const useAdminPaths = () => useSWR<AdminPath[]>(adminLearningKeys.paths, fetcher)

export async function adminRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { credentials: 'include', ...init })
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  return response.status === 204 ? (undefined as T) : await response.json() as T
}

