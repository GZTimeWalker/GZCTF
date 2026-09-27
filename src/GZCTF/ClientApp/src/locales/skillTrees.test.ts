import { describe, expect, test } from 'vitest'
import en from './en-US/skillTrees.json'
import zh from './zh-CN/skillTrees.json'

const flatten = (value: object, prefix = ''): string[] =>
  Object.entries(value).flatMap(([key, child]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return child && typeof child === 'object' ? flatten(child as object, path) : [path]
  })

describe('skill tree locales', () => {
  test('skill tree locale keys stay in parity', () => {
    expect(flatten(zh).sort()).toEqual(flatten(en).sort())
  })
})
