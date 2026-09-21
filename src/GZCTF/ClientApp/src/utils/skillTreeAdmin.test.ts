import { describe, expect, it } from 'vitest'
import { moveItem, normalizeOrder, skillTreeIcons, resolveSkillTreeErrorMessageKey } from './skillTreeAdmin'

describe('skill tree admin utilities', () => {
  it('exposes only backend supported icon keys', () => {
    expect(Object.keys(skillTreeIcons)).toEqual(['flag', 'web', 'crypto', 'pwn', 'brain', 'ai'])
  })

  it('normalizes order after movement or removal', () => {
    expect(normalizeOrder([{ id: 'b' }, { id: 'a' }])).toEqual([
      { id: 'b', sortOrder: 0 },
      { id: 'a', sortOrder: 1 },
    ])
  })

  it('moves a category and emits continuous order', () => {
    expect(moveItem([{ id: 'a' }, { id: 'b' }, { id: 'c' }], 2, 0)).toEqual([
      { id: 'c', sortOrder: 0 },
      { id: 'a', sortOrder: 1 },
      { id: 'b', sortOrder: 2 },
    ])
  })

  it('maps stable backend error codes to translation keys', () => {
    expect(resolveSkillTreeErrorMessageKey({
      response: { data: { code: 'skill_tree_revision_conflict' } },
    })).toBe('skillTrees:errors.revisionConflict')
    expect(resolveSkillTreeErrorMessageKey(new Error('unknown'))).toBeUndefined()
  })
})
