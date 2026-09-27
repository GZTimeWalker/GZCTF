import { expect, test, type Page } from '@playwright/test'

const treeId = '10000000-0000-0000-0000-000000000001'
const categoryId = '20000000-0000-0000-0000-000000000001'

const mockAdmin = async (page: Page) => {
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '30000000-0000-0000-0000-000000000001', userName: 'review-admin', role: 'Admin' } })
  )
  await page.route('**/api/info/config', (route) => route.fulfill({ json: { title: 'GZ' } }))
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
}

const conflict = {
  code: 'skill_tree_revision_conflict',
  message: 'Changed by another administrator.',
}

test('saving an out-of-date skill tree draft does not resubmit local edits with a new version', async ({ page }) => {
  await mockAdmin(page)
  let writes = 0
  await page.route(`**/api/admin/skill-trees/${treeId}/draft`, (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: { revisionId: treeId, name: 'Original', summary: '', iconKey: 'flag', rowVersion: 1, categories: [] } })
    }
    writes += 1
    return route.fulfill({ status: 409, json: conflict })
  })
  await page.route('**/api/admin/skill-categories', (route) => route.fulfill({ json: [] }))

  await page.goto(`/admin/skill-trees/${treeId}`)
  await page.getByRole('textbox', { name: 'Name' }).fill('My local edit')
  await page.getByRole('button', { name: 'Save draft' }).click()

  await expect(page.getByRole('alert', { name: /changed|conflict/i })).toBeVisible()
  expect(writes).toBe(1)
})

test('publishing an out-of-date skill tree does not retry with a new version', async ({ page }) => {
  await mockAdmin(page)
  let publishes = 0
  await page.route(`**/api/admin/skill-trees/${treeId}/draft`, (route) =>
    route.fulfill({ json: { revisionId: treeId, name: 'Original', summary: '', iconKey: 'flag', rowVersion: 1, categories: [] } })
  )
  await page.route(`**/api/admin/skill-trees/${treeId}/publish`, (route) => {
    publishes += 1
    return route.fulfill({ status: 409, json: conflict })
  })
  await page.route('**/api/admin/skill-categories', (route) => route.fulfill({ json: [] }))

  await page.goto(`/admin/skill-trees/${treeId}`)
  await page.getByRole('button', { name: 'Publish', exact: true }).click()

  await expect(page.getByRole('alert', { name: /changed|conflict/i })).toBeVisible()
  expect(publishes).toBe(1)
})

test('deleting an out-of-date category requires a fresh impact review', async ({ page }) => {
  await mockAdmin(page)
  let deletes = 0
  await page.route(`**/api/admin/skill-categories/${categoryId}`, (route) => {
    if (route.request().method() === 'DELETE') {
      deletes += 1
      return route.fulfill({ status: 409, json: conflict })
    }
    return route.fulfill({ json: { categoryId, name: 'Category', summary: '', iconKey: 'flag', rowVersion: 1, contents: [], trees: [] } })
  })
  await page.route(`**/api/admin/skill-categories/${categoryId}/delete-impact`, (route) =>
    route.fulfill({ json: { rowVersion: 1, requiresTypedConfirmation: false, draftTreeCount: 0, publishedTreeCount: 0, challengeCount: 0, lessonCount: 0 } })
  )
  await page.route('**/api/admin/skill-trees', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/admin/skill-categories', (route) => route.fulfill({ json: [] }))

  await page.goto(`/admin/skill-categories/${categoryId}`)
  await page.getByRole('button', { name: 'Delete category' }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Delete', exact: true }).click()

  await expect(page.getByRole('alert', { name: /changed|conflict/i })).toBeVisible()
  expect(deletes).toBe(1)
})

test('changing cohorts clears the members selected for the previous cohort', async ({ page }) => {
  await mockAdmin(page)
  await page.route('**/api/admin/cohorts', (route) =>
    route.fulfill({ json: [
      { id: treeId, name: 'Class A', isActive: true, memberCount: 0 },
      { id: categoryId, name: 'Class B', isActive: true, memberCount: 0 },
    ] })
  )
  await page.route(/\/api\/admin\/cohorts\/[\w-]+\/members$/, (route) => route.fulfill({ json: [] }))
  await page.route('**/api/admin/users/search**', (route) =>
    route.fulfill({ json: { data: [{ id: '30000000-0000-0000-0000-000000000002', userName: 'Learner' }], count: 1 } })
  )

  await page.goto('/admin/skill-trees?tab=members')
  await page.getByRole('textbox', { name: 'Search users' }).fill('Learner')
  await page.getByRole('button', { name: 'Search', exact: true }).click()
  await page.getByRole('combobox', { name: 'Members to assign' }).click()
  await page.getByRole('option', { name: 'Learner' }).click()
  await page.keyboard.press('Escape')
  await expect(page.getByRole('button', { name: 'Assign selected members' })).toBeEnabled()

  await page.getByText('Class B').click()
  await expect(page.getByRole('button', { name: 'Assign selected members' })).toBeDisabled()
})

test('a cohort request failure is not presented as an empty cohort list', async ({ page }) => {
  await mockAdmin(page)
  await page.route('**/api/admin/cohorts', (route) => route.fulfill({ status: 500, json: { message: 'Unavailable' } }))

  await page.goto('/admin/skill-trees?tab=members')
  await expect(page.getByText('Learning content could not be loaded.')).toBeVisible()
  await expect(page.getByText('No cohorts yet')).toHaveCount(0)
})

test('a background category refresh cannot replace the version paired with local edits', async ({ page }) => {
  test.setTimeout(30_000)
  await mockAdmin(page)
  let currentVersion = 1
  let reads = 0
  let submittedVersion: number | undefined
  await page.route(`**/api/admin/skill-categories/${categoryId}`, (route) => {
    if (route.request().method() === 'GET') {
      reads += 1
      return route.fulfill({ json: { categoryId, name: currentVersion === 1 ? 'Original' : 'Remote edit', summary: '', iconKey: 'flag', rowVersion: currentVersion, contents: [], trees: [] } })
    }
    submittedVersion = route.request().postDataJSON().rowVersion
    return route.fulfill({ status: 409, json: conflict })
  })
  await page.route('**/api/admin/skill-trees', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/admin/skill-categories', (route) => route.fulfill({ json: [] }))

  await page.goto(`/admin/skill-categories/${categoryId}`)
  await page.getByRole('textbox', { name: 'Name' }).fill('My local edit')
  const readsBeforeRefresh = reads
  currentVersion = 2
  await expect.poll(() => reads, { timeout: 15_000 }).toBeGreaterThan(readsBeforeRefresh)
  await expect(page.getByRole('textbox', { name: 'Name' })).toHaveValue('My local edit')
  await page.getByRole('button', { name: 'Save metadata' }).first().click()
  await expect(page.getByRole('alert', { name: /changed|conflict/i })).toBeVisible()
  expect(submittedVersion).toBe(1)
})
