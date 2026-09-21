import { useSWRConfig } from 'swr'
import api from '@Api'

export const useAdminSkillTrees = () => api.adminSkillTrees.useAdminSkillTreesList()

export const useAdminSkillCategories = () => api.adminSkillCategories.useAdminSkillCategoriesList()

export const useAdminSkillTreeDraft = (id?: string) =>
  api.adminSkillTrees.useAdminSkillTreesGetDraft(id ?? '', undefined, Boolean(id))

export const useAdminSkillCategory = (id?: string) =>
  api.adminSkillCategories.useAdminSkillCategoriesGet(id ?? '', undefined, Boolean(id))

const skillTreeKeyMatcher = (key: unknown) =>
  typeof key === 'string' &&
  (key.startsWith('/api/admin/skill-trees') ||
    key.startsWith('/api/admin/skill-categories') ||
    key.startsWith('/api/skill-trees') ||
    key.startsWith('/api/my-learning'))

export const useSkillTreeAdminMutations = () => {
  const { mutate } = useSWRConfig()

  const invalidate = async () => {
    await mutate(skillTreeKeyMatcher)
  }

  return {
    invalidate,
    createTree: async (data: Parameters<typeof api.adminSkillTrees.adminSkillTreesCreate>[0]) => {
      const result = await api.adminSkillTrees.adminSkillTreesCreate(data)
      await invalidate()
      return result
    },
    saveTreeDraft: async (
      id: string,
      data: Parameters<typeof api.adminSkillTrees.adminSkillTreesUpdateDraft>[1]
    ) => {
      const result = await api.adminSkillTrees.adminSkillTreesUpdateDraft(id, data)
      await invalidate()
      return result
    },
    publishTree: async (
      id: string,
      data: Parameters<typeof api.adminSkillTrees.adminSkillTreesPublish>[1]
    ) => {
      const result = await api.adminSkillTrees.adminSkillTreesPublish(id, data)
      await invalidate()
      return result
    },
    deleteTree: async (
      id: string,
      data: Parameters<typeof api.adminSkillTrees.adminSkillTreesDelete>[1]
    ) => {
      const result = await api.adminSkillTrees.adminSkillTreesDelete(id, data)
      await invalidate()
      return result
    },
    createCategory: async (
      data: Parameters<typeof api.adminSkillCategories.adminSkillCategoriesCreate>[0]
    ) => {
      const result = await api.adminSkillCategories.adminSkillCategoriesCreate(data)
      await invalidate()
      return result
    },
    saveCategory: async (
      id: string,
      data: Parameters<typeof api.adminSkillCategories.adminSkillCategoriesUpdate>[1]
    ) => {
      const result = await api.adminSkillCategories.adminSkillCategoriesUpdate(id, data)
      await invalidate()
      return result
    },
    sortCategoryContents: async (
      id: string,
      data: Parameters<typeof api.adminSkillCategories.adminSkillCategoriesUpdateContents>[1]
    ) => {
      const result = await api.adminSkillCategories.adminSkillCategoriesUpdateContents(id, data)
      await invalidate()
      return result
    },
    saveTreeMemberships: async (
      id: string,
      data: Parameters<typeof api.adminSkillCategories.adminSkillCategoriesUpdateTreeMemberships>[1]
    ) => {
      const result = await api.adminSkillCategories.adminSkillCategoriesUpdateTreeMemberships(id, data)
      await invalidate()
      return result
    },
    mergeCategories: async (
      data: Parameters<typeof api.adminSkillCategories.adminSkillCategoriesMerge>[0]
    ) => {
      const result = await api.adminSkillCategories.adminSkillCategoriesMerge(data)
      await invalidate()
      return result
    },
    deleteCategory: async (
      id: string,
      data: Parameters<typeof api.adminSkillCategories.adminSkillCategoriesDelete>[1]
    ) => {
      const result = await api.adminSkillCategories.adminSkillCategoriesDelete(id, data)
      await invalidate()
      return result
    },
    publishChallenge: async (
      id: string,
      data: Parameters<typeof api.adminChallenges.adminChallengesPublish>[1]
    ) => {
      const result = await api.adminChallenges.adminChallengesPublish(id, data)
      await invalidate()
      return result
    },
    publishLesson: async (
      id: string,
      data: Parameters<typeof api.adminLessons.adminLessonsPublish>[1]
    ) => {
      const result = await api.adminLessons.adminLessonsPublish(id, data)
      await invalidate()
      return result
    },
  }
}
