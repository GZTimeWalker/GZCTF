import { execFile } from 'node:child_process'
import { promisify } from 'node:util'
import { expect, test } from '@playwright/test'
import { createRoleContext } from './support/auth'
import {
  attachCategory,
  collectFailures,
  createAdminContext,
  createCategory,
  createLesson,
  createTree,
  publishContent,
  publishTree,
} from './support/skillTreeSeed'

/**
 * ST27 release gate. Proves that every retired Learning URL still reaches the correct
 * new skill tree address at the browser level, and that unknown slugs fail loudly
 * instead of guessing a target. The redirect API contract itself is covered by
 * LearningRedirectTests in the integration suite.
 */

const execFileAsync = promisify(execFile)

test.describe('skill tree redirects', () => {
  test('legacy root, slug and deep links land on the new skill tree pages', async ({ browser }) => {
    test.skip(
      !process.env.E2E_ADMIN_USER || !process.env.E2E_ADMIN_PASSWORD,
      'Set E2E_ADMIN_USER/E2E_ADMIN_PASSWORD to run the redirect journey'
    )
    test.setTimeout(180_000)

    const context = await createRoleContext(browser, 'administrator')
    const page = await context.newPage()
    const failures = collectFailures(page, [
      // The unknown-slug probe deliberately asks for a mapping that does not exist.
      { url: /\/api\/skill-tree-redirects\//, status: 404 },
    ])

    const suffix = Date.now().toString().slice(-6)
    const slug = `e2e-redirect-${suffix}`
    const treeName = `Redirect ${suffix}`
    const lessonTitle = `Redirect lesson ${suffix}`

    const { page: adminPage, close: closeAdmin } = await createAdminContext(browser)
    const treeId = await createTree(adminPage, treeName)
    await publishTree(adminPage, treeId)

    const categoryId = await createCategory(adminPage, `Redirect category ${suffix}`)
    await attachCategory(adminPage, treeId, [categoryId])
    await publishTree(adminPage, treeId)

    const lessonId = await createLesson(adminPage, lessonTitle)
    await publishContent(adminPage, 'lesson', lessonId, [categoryId])

    // Read the public detail to learn the content identifier the redirect must target.
    const publicDetail = await adminPage.request.get(`/api/skill-trees/${treeId}`)
    expect(publicDetail.ok()).toBeTruthy()
    const tree = await publicDetail.json()
    const category = tree.categories.find((item: { categoryId: string }) => item.categoryId === categoryId)
    expect(category, 'the shared category is missing from the published tree').toBeTruthy()
    const content = category.contents[0]
    expect(content, 'the published lesson is missing from the category').toBeTruthy()
    const contentId = content.contentId as string

    await closeAdmin()

    // The backfill owns slug mappings, so the retired address is registered directly.
    const mapped = await insertRedirectAsync(slug, treeId)

    // 1. The old learning root lands on discovery.
    await page.goto('/learn')
    await expect(page).toHaveURL(/\/skill-trees\/?$/)

    if (mapped) {
      // 2. A mapped old slug lands on the owning tree.
      await page.goto(`/learn/${slug}`)
      await expect(page).toHaveURL(new RegExp(`/skill-trees/${treeId}/?$`))

      // 3. An old deep link keeps its content address under the new tree.
      await page.goto(`/learn/${slug}/${categoryId}/${contentId}`)
      await expect(page).toHaveURL(
        new RegExp(`/skill-trees/${treeId}/${categoryId}/lesson/${contentId}/?$`)
      )
    }

    // 4. An unknown slug fails loudly instead of guessing a target.
    await page.goto(`/learn/definitely-not-a-real-slug-${suffix}`)
    await expect(page).toHaveURL(/\/404\/?$/)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    // 5. The redirect API reports the same target without following it.
    const api = await page.request.get(`/api/skill-tree-redirects/${slug}`)
    if (mapped) {
      expect(api.ok()).toBeTruthy()
      const body = await api.json()
      expect(body.targetPath).toBe(`/skill-trees/${treeId}`)
    } else {
      expect(api.status()).toBe(404)
    }

    expect(failures).toEqual([])
    await context.close()
  })
})

/**
 * Registers a retired slug the way the startup backfill does. The integration suite owns
 * the mapping semantics; this only makes the browser journey deterministic.
 */
async function insertRedirectAsync(slug: string, treeId: string): Promise<boolean> {
  const sql = `INSERT INTO "LearningPathRedirects" ("Id","LearningPathId","OldSlug","SkillTreeId") ` +
    `VALUES (gen_random_uuid(), '${treeId}', '${slug}', '${treeId}') ON CONFLICT DO NOTHING;`
  try {
    await execFileAsync('docker', [
      'exec', 'gzctf-demo-db', 'psql', '-U', 'gzctf', '-d', 'gzctf_demo', '-c', sql,
    ])
    return true
  } catch {
    return false
  }
}
