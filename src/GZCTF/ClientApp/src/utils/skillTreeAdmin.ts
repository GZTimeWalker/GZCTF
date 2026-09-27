export const skillTreeIcons = {
  flag: '🚩',
  web: '🕸️',
  crypto: '🔐',
  pwn: '💣',
  brain: '🧠',
  ai: '🤖',
} as const

export type SkillTreeIconKey = keyof typeof skillTreeIcons

export const skillTreeIconKeys = Object.keys(skillTreeIcons) as SkillTreeIconKey[]

export const normalizeOrder = <T>(items: T[]): Array<T & { sortOrder: number }> =>
  items.map((item, sortOrder) => ({ ...item, sortOrder }))

export const moveItem = <T>(items: T[], from: number, to: number): Array<T & { sortOrder: number }> => {
  const copy = [...items]
  const [moved] = copy.splice(from, 1)
  copy.splice(to, 0, moved)
  return normalizeOrder(copy)
}

export const skillTreeAdminKeys = {
  trees: '/api/admin/skill-trees',
  treeDraft: (id: string) => `/api/admin/skill-trees/${id}/draft`,
  treePreview: (id: string) => `/api/admin/skill-trees/${id}/draft/preview`,
  treeImpact: (id: string) => `/api/admin/skill-trees/${id}/delete-impact`,
  categories: '/api/admin/skill-categories',
  category: (id: string) => `/api/admin/skill-categories/${id}`,
  categoryImpact: (id: string) => `/api/admin/skill-categories/${id}/delete-impact`,
} as const

export const skillTreeErrorMessages: Record<string, string> = {
  skill_tree_revision_conflict: 'skillTrees:errors.revisionConflict',
  skill_category_merge_conflict: 'skillTrees:errors.mergeConflict',
  skill_tree_confirmation_mismatch: 'skillTrees:errors.confirmationMismatch',
  skill_category_invalid: 'skillTrees:errors.categoryInvalid',
  skill_tree_invalid_icon: 'skillTrees:errors.invalidIcon',
  content_category_required: 'skillTrees:errors.categoryRequired',
  content_category_has_no_active_tree: 'skillTrees:errors.categoryHasNoTree',
  content_invalid_publication: 'skillTrees:errors.invalidPublication',
}

export const getApiErrorCode = (error: unknown): string | undefined => {
  const response = (error as { response?: { data?: { code?: string } } })?.response
  return response?.data?.code
}

export const resolveSkillTreeErrorMessageKey = (error: unknown): string | undefined => {
  const code = getApiErrorCode(error)
  return code ? skillTreeErrorMessages[code] : undefined
}
