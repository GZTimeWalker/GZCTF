import { useSWRConfig, type SWRConfiguration } from 'swr'
import Api from '@Api'

export const skillTreeKeys = {
  list: '/api/skill-trees',
  detail: (id: string) => `/api/skill-trees/${id}`,
  enrollments: '/api/skill-tree-enrollments',
  record: '/api/my-learning',
} as const

export const useSkillTrees = (options?: SWRConfiguration, doFetch: boolean = true) =>
  Api.skillTrees.useSkillTreesList(options, doFetch)

export const useSkillTree = (id?: string, options?: SWRConfiguration) =>
  Api.skillTrees.useSkillTreesDetail(id ?? '', options, Boolean(id))

export const useSkillTreeMutations = () => {
  const { mutate } = useSWRConfig()

  const invalidateList = () => mutate(skillTreeKeys.list)
  const invalidateDetail = (id: string) => mutate(skillTreeKeys.detail(id))
  const invalidateEnrollments = () => mutate(skillTreeKeys.enrollments)
  const invalidateRecord = () => mutate(skillTreeKeys.record)

  const join = async (id: string) => {
    await Api.skillTreeEnrollments.skillTreeEnrollmentsEnroll(id)
    invalidateList()
    invalidateDetail(id)
    invalidateEnrollments()
    invalidateRecord()
  }

  const leave = async (id: string) => {
    await Api.skillTreeEnrollments.skillTreeEnrollmentsLeave(id)
    invalidateList()
    invalidateDetail(id)
    invalidateEnrollments()
    invalidateRecord()
  }

  const setCurrent = async (id: string) => {
    await Api.skillTreeEnrollments.skillTreeEnrollmentsSelectCurrent(id)
    invalidateEnrollments()
    invalidateRecord()
  }

  return { join, leave, setCurrent, invalidateList, invalidateDetail }
}
