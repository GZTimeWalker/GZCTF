import { expect, type Browser, type Page } from '@playwright/test'
import { createRoleContext, login } from './auth'

/**
 * Shared seeding helpers for the ST27 browser suites. Publishing content requires the
 * target category to already belong to a published tree, so the order is:
 * create tree -> publish empty -> create category -> attach to tree -> publish tree ->
 * publish content.
 */

export type TreeSummary = {
  skillTreeId: string
  name: string
  iconKey: string
  rowVersion: number
}

export type SeededTree = {
  treeId: string
  categoryId: string
  lessonId: string
  challengeId: string
}

export async function loginAsAdmin(page: Page): Promise<void> {
  await login(page, {
    userName: process.env.E2E_ADMIN_USER!,
    password: process.env.E2E_ADMIN_PASSWORD!,
  })
}

export async function createAdminContext(browser: Browser): Promise<{ page: Page; close: () => Promise<void> }> {
  const context = await browser.newContext()
  const page = await context.newPage()
  await loginAsAdmin(page)
  return { page, close: () => context.close() }
}

async function listTrees(page: Page): Promise<TreeSummary[]> {
  const response = await page.request.get('/api/admin/skill-trees')
  expect(response.ok()).toBeTruthy()
  return await response.json() as TreeSummary[]
}

/** The draft revision owns the row version both the draft save and publish endpoints check. */
async function draftRowVersion(page: Page, treeId: string): Promise<{ name: string; iconKey: string; rowVersion: number }> {
  const response = await page.request.get(`/api/admin/skill-trees/${treeId}/draft`)
  expect(response.ok()).toBeTruthy()
  const draft = await response.json()
  return {
    name: draft.name as string,
    iconKey: draft.iconKey as string,
    rowVersion: draft.rowVersion as number,
  }
}

export async function createTree(page: Page, name: string, iconKey = 'web'): Promise<string> {
  const response = await page.request.post('/api/admin/skill-trees', {
    data: { name, summary: '', iconKey },
  })
  expect(response.ok()).toBeTruthy()
  return (await response.json()).skillTreeId as string
}

export async function createCategory(page: Page, name: string, iconKey = 'flag'): Promise<string> {
  const response = await page.request.post('/api/admin/skill-categories', {
    data: { name, summary: '', iconKey },
  })
  expect(response.ok()).toBeTruthy()
  return (await response.json()).categoryId as string
}

export async function attachCategory(page: Page, treeId: string, categoryIds: string[]): Promise<void> {
  const draft = await draftRowVersion(page, treeId)
  const response = await page.request.put(`/api/admin/skill-trees/${treeId}/draft`, {
    data: {
      name: draft.name,
      summary: '',
      iconKey: draft.iconKey,
      rowVersion: draft.rowVersion,
      categories: categoryIds.map((categoryId, index) => ({ categoryId, sortOrder: index })),
    },
  })
  expect(response.ok()).toBeTruthy()
}

export async function publishTree(page: Page, treeId: string): Promise<void> {
  const draft = await draftRowVersion(page, treeId)
  const response = await page.request.post(`/api/admin/skill-trees/${treeId}/publish`, {
    data: { rowVersion: draft.rowVersion },
  })
  expect(response.ok()).toBeTruthy()
}

export async function createLesson(page: Page, title: string): Promise<string> {
  const response = await page.request.post('/api/admin/lessons', {
    data: {
      locale: 'en',
      localizations: [{ locale: 'en', title, body: '# Lesson\n\nMarkdown body.' }],
    },
  })
  expect(response.ok()).toBeTruthy()
  return (await response.json()).id as string
}

export async function createChallenge(
  page: Page,
  input: { title: string; flag: string },
): Promise<string> {
  const response = await page.request.post('/api/admin/challenges', {
    data: {
      type: 'StaticAttachment',
      localizations: [
        { locale: 'en', title: input.title, summary: 'Summary', body: 'Body' },
      ],
      flags: [{ kind: 'Static', value: input.flag }],
      hints: [{ locale: 'en', sortOrder: 0, content: 'Try the obvious.' }],
      writeups: [{ locale: 'en', content: 'Official write-up body.' }],
    },
  })
  expect(response.ok()).toBeTruthy()
  return (await response.json()).challenge.id as string
}

export async function publishContent(
  page: Page,
  kind: 'lesson' | 'challenge',
  contentId: string,
  categoryIds: string[],
): Promise<void> {
  // Challenges expose their publication state through the edit endpoint; lessons do not.
  const detailPath = kind === 'challenge'
    ? `/api/admin/challenges/${contentId}/edit?locale=en`
    : `/api/admin/lessons/${contentId}?locale=en`
  const detail = await page.request.get(detailPath)
  expect(detail.ok()).toBeTruthy()
  const rowVersion = (await detail.json()).publication.rowVersion as number
  const response = await page.request.post(`/api/admin/${kind}s/${contentId}/publish`, {
    data: { rowVersion, categoryIds, inlineCategories: [] },
  })
  expect(response.ok()).toBeTruthy()
}

/**
 * Creates two published trees that share one category holding a lesson and a challenge.
 */
export async function seedSharedTrees(
  browser: Browser,
  input: {
    suffix: string
    categoryName: string
    firstTreeName: string
    secondTreeName: string
    lessonTitle: string
    challengeTitle: string
    flag: string
    emptyTreeName?: string
  },
): Promise<SeededTree & { secondTreeId: string; emptyTreeId?: string }> {
  const { page, close } = await createAdminContext(browser)
  try {
    const firstTreeId = await createTree(page, input.firstTreeName)
    const secondTreeId = await createTree(page, input.secondTreeName)
    const emptyTreeId = input.emptyTreeName
      ? await createTree(page, input.emptyTreeName)
      : undefined
    if (emptyTreeId) await publishTree(page, emptyTreeId)
    await publishTree(page, firstTreeId)
    await publishTree(page, secondTreeId)

    const categoryId = await createCategory(page, input.categoryName)
    await attachCategory(page, firstTreeId, [categoryId])
    await attachCategory(page, secondTreeId, [categoryId])
    await publishTree(page, firstTreeId)
    await publishTree(page, secondTreeId)

    const lessonId = await createLesson(page, input.lessonTitle)
    const challengeId = await createChallenge(page, { title: input.challengeTitle, flag: input.flag })
    await publishContent(page, 'lesson', lessonId, [categoryId])
    await publishContent(page, 'challenge', challengeId, [categoryId])

    return { treeId: firstTreeId, secondTreeId, emptyTreeId, categoryId, lessonId, challengeId }
  } finally {
    await close()
  }
}

export { createRoleContext }

export const PROGRESS_TEXT = /progress|完成率|路线进度|进度/i
const RAW_KEY = /^(skillTrees|learning|common)\.[a-zA-Z0-9_.]+$/

/** A request the journey deliberately makes that is allowed to answer with a non-2xx status. */
export type ExpectedProbe = { url: RegExp; status: number }

export function collectFailures(page: Page, expected: ExpectedProbe[] = []): string[] {
  const failures: string[] = []
  const ignorable = (text: string) => /wsrx|ERR_CONNECTION_REFUSED/i.test(text)
  const isExpected = (url: string, text: string) => {
    const status = Number(text.match(/status of (\d{3})/)?.[1])
    return expected.some((probe) => probe.url.test(url) && probe.status === status)
  }
  page.on('pageerror', (error) => {
    if (!ignorable(error.message)) failures.push(`pageerror: ${error.message}`)
  })
  page.on('console', (message) => {
    if (message.type() !== 'error' || ignorable(message.text())) return
    const url = message.location().url
    if (url && isExpected(url, message.text())) return
    failures.push(url ? `console: ${message.text()} (${url})` : `console: ${message.text()}`)
  })
  page.on('response', (response) => {
    if (response.status() >= 500) failures.push(`http ${response.status()}: ${response.url()}`)
  })
  return failures
}

export async function switchLanguage(page: Page, language: string): Promise<void> {
  await page.evaluate((value) => {
    window.localStorage.setItem('language', JSON.stringify(value))
  }, language)
  await page.reload()
  await page.waitForLoadState('networkidle')
}

export async function assertNoRawKeys(page: Page): Promise<void> {
  const texts = await page.evaluate(() =>
    Array.from(document.querySelectorAll('main *'))
      .map((element) => element.textContent?.trim() ?? '')
      .filter((text) => text.length > 0)
  )
  expect(texts.filter((text) => RAW_KEY.test(text))).toEqual([])
}
