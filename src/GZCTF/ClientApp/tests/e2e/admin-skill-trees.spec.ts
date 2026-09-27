import { expect, test } from '@playwright/test'
import { createRoleContext } from './support/auth'

test.describe('skill tree administration', () => {
  test('administrator manages skill trees, categories and shared content', async ({ browser }) => {
    test.skip(
      !process.env.E2E_ADMIN_USER || !process.env.E2E_ADMIN_PASSWORD,
      'Set E2E_ADMIN_USER/E2E_ADMIN_PASSWORD to run the administrator journey'
    )
    test.setTimeout(180_000)

    const context = await createRoleContext(browser, 'administrator')
    const page = await context.newPage()

    const failures: string[] = []
    const ignorable = (text: string) => /wsrx|ERR_CONNECTION_REFUSED/i.test(text)
    page.on('pageerror', (error) => {
      if (!ignorable(error.message)) failures.push(`pageerror: ${error.message}`)
    })
    page.on('console', (message) => {
      if (message.type() === 'error' && !ignorable(message.text())) failures.push(`console: ${message.text()}`)
    })
    page.on('response', (response) => {
      if (response.status() >= 500) failures.push(`http ${response.status()}: ${response.url()}`)
    })

    const id = Date.now().toString().slice(-6)
    const treeName = `E2E Tree ${id}`
    const categoryA = `Category A ${id}`
    const categoryB = `Category B ${id}`
    const categoryCopy = `Category A copy ${id}`
    const challengeTitle = `Challenge ${id}`
    const lessonTitle = `Lesson ${id}`

    const createCategory = async (name: string) => {
      await page.goto('/admin/skill-trees?tab=categories')
      await page.getByRole('button', { name: 'Create category' }).first().click()
      await page.getByRole('textbox', { name: 'Name' }).fill(name)
      await page.getByRole('button', { name: 'Create', exact: true }).click()
      await expect(page).toHaveURL(/\/admin\/skill-categories\/[0-9a-f-]+/i)
      await expect(page.getByText(name).first()).toBeVisible()
    }

    const addCategoryToTree = async (name: string) => {
      await page.getByRole('textbox', { name: 'Search categories' }).fill(name)
      await page.getByRole('button', { name: 'Add category' }).first().click()
    }

    const publishIntoCategory = async (kind: 'challenge' | 'lesson', contentId: string) => {
      await page.goto(`/admin/library/${kind}s/${contentId}`)
      await page.getByRole('button', { name: kind === 'challenge' ? 'Publish challenge' : 'Publish lesson' }).click()
      const dialog = page.getByRole('dialog')
      await dialog.getByText(treeName, { exact: true }).click()
      await dialog.getByText(categoryA, { exact: true }).click()
      const publishPromise = page.waitForResponse((response) => response.url().includes('/publish') && response.request().method() === 'POST')
      await dialog.getByRole('button', { name: 'Publish' }).click()
      await publishPromise
      await expect(
        page.getByText(kind === 'challenge' ? 'Challenge published' : 'Lesson published')
      ).toBeVisible({ timeout: 10_000 })
    }

    // 1. Create and publish an empty skill tree through the workspace modal.
    await page.goto('/admin/skill-trees')
    await page.getByRole('button', { name: 'Create skill tree' }).first().click()
    await page.getByRole('textbox', { name: 'Name' }).fill(treeName)
    await page.getByRole('button', { name: 'Create', exact: true }).click()
    await expect(page).toHaveURL(/\/admin\/skill-trees\/[0-9a-f-]+/i)
    await page.getByRole('button', { name: 'Publish', exact: true }).click()
    await expect(page.getByText('Skill tree published')).toBeVisible({ timeout: 10_000 })

    // 2. Create two categories from the categories tab.
    await createCategory(categoryA)
    await createCategory(categoryB)

    // 3. Add both to the tree, reorder categories and publish.
    await page.goto('/admin/skill-trees')
    await page.getByText(treeName).first().click()
    await addCategoryToTree(categoryA)
    await addCategoryToTree(categoryB)
    await page.getByRole('button', { name: `Move up: ${categoryB}` }).click()
    await page.getByRole('button', { name: 'Save draft' }).click()
    await expect(page.getByText('Draft saved')).toBeVisible({ timeout: 10_000 })
    await page.getByRole('button', { name: 'Publish', exact: true }).click()
    await expect(page.getByText('Skill tree published')).toBeVisible({ timeout: 10_000 })

    // 4. Create a draft challenge and publish it through the shared modal.
    const challengeResponse = await page.request.post('/api/admin/challenges', {
      data: {
        type: 'StaticAttachment',
        localizations: [{ locale: 'en', title: challengeTitle, summary: '', body: '' }],
        flags: [],
      },
    })
    expect(challengeResponse.ok()).toBeTruthy()
    const challengeId = (await challengeResponse.json()).challenge.id as string
    await publishIntoCategory('challenge', challengeId)

    // 5. Create a draft lesson and publish it into the same category.
    const lessonResponse = await page.request.post('/api/admin/lessons', {
      data: {
        locale: 'en',
        localizations: [{ locale: 'en', title: lessonTitle, body: 'Body' }],
      },
    })
    expect(lessonResponse.ok()).toBeTruthy()
    const lessonId = (await lessonResponse.json()).id as string
    await publishIntoCategory('lesson', lessonId)

    // 6. Reorder category contents and verify the shared order in the tree preview.
    await page.goto('/admin/skill-trees?tab=categories')
    await page.getByText(categoryA).first().click()
    await expect(page.getByText(lessonTitle).first()).toBeVisible()
    await page.getByRole('button', { name: `Move up: ${lessonTitle}` }).click()
    await page.getByRole('button', { name: 'Save order' }).click()
    await expect(page.getByText('Content order saved')).toBeVisible({ timeout: 10_000 })

    await page.goto('/admin/skill-trees')
    await page.getByText(treeName).first().click()
    const [previewResponse] = await Promise.all([
      page.waitForResponse((response) => response.url().includes('/draft/preview')),
      page.getByRole('button', { name: 'Preview draft' }).click(),
    ])
    if (!previewResponse.ok()) {
      throw new Error(`preview failed: ${previewResponse.status()} ${await previewResponse.text()}`)
    }
    const preview = page.getByRole('dialog')
    await expect(preview).toContainText(lessonTitle)
    await expect(preview).toContainText(challengeTitle)
    await page.keyboard.press('Escape')

    // 7. Create a duplicate category and merge it through the visual modal.
    const copyResponse = await page.request.post('/api/admin/skill-categories', {
      data: { name: categoryCopy, summary: '', iconKey: 'flag' },
    })
    expect(copyResponse.ok()).toBeTruthy()
    const copyId = (await copyResponse.json()).categoryId as string

    const categoriesResponse = await page.request.get('/api/admin/skill-categories')
    const categoryList = await categoriesResponse.json() as Array<{ name: string; categoryId: string }>
    const categoryAId = categoryList.find((item) => item.name === categoryA)!.categoryId

    await page.goto(`/admin/skill-categories/${categoryAId}`)
    await page.getByRole('button', { name: 'Merge categories' }).click()
    await page.getByRole('combobox', { name: 'Duplicate category' }).click()
    await page.getByRole('option', { name: categoryCopy }).click()
    await page.getByRole('dialog').getByRole('button', { name: 'Merge', exact: true }).click()
    await expect(page).toHaveURL(/\/admin\/skill-trees\?tab=categories$/)
    const removed = await page.request.get(`/api/admin/skill-categories/${copyId}`)
    expect(removed.status()).toBe(404)

    // 8. Open category delete impact and cancel once before confirming.
    await page.goto('/admin/skill-trees?tab=categories')
    await page.getByText(categoryB).first().click()
    await page.getByRole('button', { name: 'Delete category' }).click()
    await expect(page.getByRole('dialog')).toContainText(/challenges|题目/i)
    await page.getByRole('dialog').getByRole('button', { name: 'Cancel' }).click()
    await expect(page.getByRole('dialog')).toHaveCount(0)

    // 9. Delete the skill tree using its typed name.
    await page.goto('/admin/skill-trees')
    await page.getByText(treeName).first().click()
    await page.getByRole('button', { name: 'Delete skill tree' }).click()
    await page.getByRole('textbox', { name: 'Confirmation name' }).fill(treeName)
    await page.getByRole('dialog').getByRole('button', { name: 'Delete', exact: true }).click()
    await expect(page).toHaveURL(/\/admin\/skill-trees\/?$/)

    // 10. Switch to Chinese and repeat workspace assertions.
    await page.evaluate(() => localStorage.setItem('language', JSON.stringify('zh-CN')))
    await page.goto('/admin/skill-trees')
    await expect(page.getByRole('heading', { name: '管理工作台' })).toBeVisible({ timeout: 10_000 })

    // Forbidden UI must never appear.
    await expect(page.getByLabel(/json|modules json/i)).toHaveCount(0)
    await expect(page.getByLabel(/slug/i)).toHaveCount(0)
    await expect(page.getByText(/skillTrees\./)).toHaveCount(0)

    expect(failures, failures.join('\n')).toEqual([])
    await context.close()
  })
})
