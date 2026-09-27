import { expect, test, type Page } from '@playwright/test'

type MockChallenge = { id: string; type: string; publicationState: string; title: string; summary: string }
type MockMember = { id: string; userName: string }
type MockCohort = { id: string; name: string; isActive: boolean; memberCount: number }

const mockAdmin = async (page: Page) => {
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '30000000-0000-0000-0000-000000000001', userName: 'workspace-admin', role: 'Admin' } })
  )
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('zh-CN')))
}

test.describe('admin workspace', () => {
  test('all four tabs remain accessible on a narrow screen', async ({ page }) => {
    await mockAdmin(page)
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/admin/skill-trees')

    const tabs = page.getByRole('tab')
    await expect(tabs).toHaveCount(4)
    const positions = await tabs.evaluateAll((nodes) => nodes.map((node) => node.getBoundingClientRect().top))
    expect(new Set(positions.map(Math.round)).size).toBe(1)
    await page.getByRole('tab', { name: '成员管理' }).click()
    await expect(page).toHaveURL(/\?tab=members$/)
    await expect(page.getByRole('button', { name: '创建年级' }).first()).toBeVisible()
  })

  test('four tabs switch in place and legacy addresses land on their tabs', async ({ page }) => {
    test.setTimeout(120_000)
    await mockAdmin(page)

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

    await page.goto('/')
    await page.evaluate(() => localStorage.setItem('language', JSON.stringify('zh-CN')))

    // 1. The workspace shows the four tabs in order.
    await page.goto('/admin/skill-trees')
    const labels = ['技能树', '全局类别', '题目管理', '成员管理']
    for (const label of labels) {
      await expect(page.getByRole('tab', { name: label })).toBeVisible()
    }
    await expect(page.getByRole('tab')).toHaveText(labels)

    // 2. Selecting the challenges tab updates the URL and survives a reload.
    await page.getByRole('tab', { name: '题目管理' }).click()
    await expect(page).toHaveURL(/\/admin\/skill-trees\?tab=challenges$/)
    await page.reload()
    await expect(page.getByRole('tab', { name: '题目管理' })).toHaveAttribute('aria-selected', 'true')

    // 3. The members tab behaves the same way.
    await page.getByRole('tab', { name: '成员管理' }).click()
    await expect(page).toHaveURL(/\/admin\/skill-trees\?tab=members$/)
    await expect(page.getByRole('tab', { name: '成员管理' })).toHaveAttribute('aria-selected', 'true')

    // 4. The retired challenge library address opens the challenges tab.
    await page.goto('/admin/library/challenges')
    await expect(page).toHaveURL(/\/admin\/skill-trees\?tab=challenges$/)
    await expect(page.getByRole('tab', { name: '题目管理' })).toHaveAttribute('aria-selected', 'true')

    // 5. The retired cohort address opens the members tab.
    await page.goto('/admin/cohorts')
    await expect(page).toHaveURL(/\/admin\/skill-trees\?tab=members$/)
    await expect(page.getByRole('tab', { name: '成员管理' })).toHaveAttribute('aria-selected', 'true')

    // 6. An unknown tab value falls back to the skill trees panel.
    await page.goto('/admin/skill-trees?tab=not-a-tab')
    await expect(page.getByRole('tab', { name: '技能树' })).toHaveAttribute('aria-selected', 'true')

    expect(failures, failures.join('\n')).toEqual([])
  })

  test('challenge library lists, creates and returns from the detail page', async ({ page }) => {
    test.setTimeout(120_000)
    await mockAdmin(page)

    await page.goto('/')
    await page.evaluate(() => localStorage.setItem('language', JSON.stringify('zh-CN')))

    const challengeId = '00000000-0000-0000-0000-000000000001'
    const title = '工作台模拟题目'
    let challenges: MockChallenge[] = []

    await page.route(/\/api\/admin\/challenges(\?|$)/, async (route) => {
      const request = route.request()
      if (request.method() === 'GET') return route.fulfill({ json: challenges })
      if (request.method() === 'POST') {
        const body = request.postDataJSON()
        challenges = [
          {
            id: challengeId,
            type: 'StaticAttachment',
            publicationState: 'Draft',
            title: body.localizations[0].title,
            summary: '',
          },
        ]
        return route.fulfill({ json: { challenge: { id: challengeId } } })
      }
      return route.fallback()
    })

    await page.route(/\/api\/admin\/challenges\/[\w-]+\/edit/, async (route) =>
      route.fulfill({
        json: {
          challenge: { id: challengeId, type: 'StaticAttachment', publicationState: 'Draft' },
          runtimeConfigurationJson: null,
          localizations: [{ locale: 'en', title, summary: '', body: 'Body' }],
          flags: [],
          hints: [],
          writeups: [],
          publication: { rowVersion: 1, publicationState: 'Draft', categoryIds: [] },
        },
      })
    )

    // 1. Direct access shows the challenges tab with its empty state.
    await page.goto('/admin/skill-trees?tab=challenges')
    await expect(page.getByRole('tab', { name: '题目管理' })).toHaveAttribute('aria-selected', 'true')
    await expect(page.getByText('暂无题目')).toBeVisible()

    // 2. The header action opens the create modal and the list refreshes.
    await page.getByRole('button', { name: '创建题目' }).first().click()
    const dialog = page.getByRole('dialog')
    await dialog.getByRole('textbox', { name: '标题', exact: true }).fill(title)
    await dialog.getByRole('combobox', { name: 'CTF 分类' }).click()
    await page.getByRole('option', { name: /Misc/ }).click()
    await dialog.getByRole('combobox', { name: '运行方式' }).click()
    await page.getByRole('option', { name: /静态附件/ }).click()
    await dialog.getByRole('button', { name: '创建', exact: true }).click()
    await expect(page.getByText('题目已创建')).toBeVisible()
    await expect(page).toHaveURL(new RegExp(`/admin/library/challenges/${challengeId}/?$`))
    await expect(page.getByRole('heading', { name: title })).toBeVisible()

    // 3. The card opens the original detail page.
    await page.getByRole('link', { name: '题目管理' }).click()
    await expect(page.getByText(title).first()).toBeVisible()
    await page.getByText(title).first().click()
    await expect(page).toHaveURL(new RegExp(`/admin/library/challenges/${challengeId}/?$`))
    await expect(page.getByRole('heading', { name: title })).toBeVisible()

    // 4. The back entry lands on the challenges tab with the list still rendered.
    await page.getByRole('link', { name: '题目管理' }).click()
    await expect(page).toHaveURL(/\/admin\/skill-trees\?tab=challenges$/)
    await expect(page.getByRole('tab', { name: '题目管理' })).toHaveAttribute('aria-selected', 'true')
    await expect(page.getByText(title).first()).toBeVisible()

  })

  test('cohort members can be searched, assigned, removed and created', async ({ page }) => {
    test.setTimeout(120_000)
    await mockAdmin(page)

    await page.goto('/')
    await page.evaluate(() => localStorage.setItem('language', JSON.stringify('zh-CN')))

    const cohortId = '10000000-0000-0000-0000-00000000000a'
    const newCohortId = '10000000-0000-0000-0000-00000000000b'
    const alice: MockMember = { id: '20000000-0000-0000-0000-00000000000a', userName: 'workspace-alice' }
    const bob: MockMember = { id: '20000000-0000-0000-0000-00000000000b', userName: 'workspace-bob' }
    let cohorts: MockCohort[] = [{ id: cohortId, name: '24级', isActive: true, memberCount: 1 }]
    const membersByCohort: Record<string, MockMember[]> = { [cohortId]: [alice] }

    await page.route(/\/api\/admin\/cohorts(\?|$)/, async (route) => {
      const request = route.request()
      if (request.method() === 'GET') {
        return route.fulfill({
          json: cohorts.map((cohort) => ({ ...cohort, memberCount: (membersByCohort[cohort.id] ?? []).length })),
        })
      }
      if (request.method() === 'POST') {
        const body = request.postDataJSON()
        const created: MockCohort = { id: newCohortId, name: body.name, isActive: true, memberCount: 0 }
        cohorts = [...cohorts, created]
        return route.fulfill({ json: created })
      }
      return route.fallback()
    })

    await page.route(/\/api\/admin\/cohorts\/[\w-]+\/members\/?(\?.*)?$/, async (route) => {
      const request = route.request()
      const cohort = request.url().match(/\/api\/admin\/cohorts\/([\w-]+)\/members/)?.[1] ?? ''
      if (request.method() === 'GET') return route.fulfill({ json: membersByCohort[cohort] ?? [] })
      if (request.method() === 'POST') {
        const body = request.postDataJSON()
        const current = membersByCohort[cohort] ?? []
        membersByCohort[cohort] = [
          ...current,
          ...[alice, bob].filter((user) => body.userIds?.includes(user.id) && !current.some((m) => m.id === user.id)),
        ]
        return route.fulfill({ json: {} })
      }
      return route.fallback()
    })

    await page.route(/\/api\/admin\/cohorts\/[\w-]+\/members\/[\w-]+$/, async (route) => {
      const request = route.request()
      if (request.method() === 'DELETE') {
        const cohort = request.url().match(/\/api\/admin\/cohorts\/([\w-]+)\/members/)?.[1] ?? ''
        const userId = request.url().split('/').pop() ?? ''
        membersByCohort[cohort] = (membersByCohort[cohort] ?? []).filter((member) => member.id !== userId)
        return route.fulfill({ json: {} })
      }
      return route.fallback()
    })

    await page.route(/\/api\/admin\/users\/search(\?|$)/, async (route) =>
      route.request().method() === 'POST'
        ? route.fulfill({ json: { data: [bob], count: 1 } })
        : route.fallback()
    )

    // 1. Direct access shows the cohort list, its member count and the members table.
    await page.goto('/admin/skill-trees?tab=members')
    await expect(page.getByRole('tab', { name: '成员管理' })).toHaveAttribute('aria-selected', 'true')
    await expect(page.getByText('24级').first()).toBeVisible()
    await expect(page.getByText('成员 · 1')).toBeVisible()
    await expect(page.getByText('workspace-alice')).toBeVisible()

    // 2. Search candidate users and assign them to the current cohort.
    await page.getByRole('textbox', { name: '搜索成员' }).fill('bob')
    await page.getByRole('button', { name: '搜索', exact: true }).click()
    await page.getByRole('combobox', { name: '选择要分配的成员' }).click()
    await page.getByRole('option', { name: 'workspace-bob' }).click()
    await page.keyboard.press('Escape')
    await page.getByRole('button', { name: '批量分配到当前年级' }).click()
    await expect(page.getByText('成员已分配')).toBeVisible()
    const bobRow = page.getByRole('row', { name: /workspace-bob/ })
    await expect(bobRow).toBeVisible()
    await expect(page.getByText('成员 · 2')).toBeVisible()

    // 3. Remove the member again and the list and count refresh.
    await bobRow.getByRole('button', { name: '移出年级' }).click()
    await expect(page.getByText('成员已移出')).toBeVisible()
    await expect(page.getByRole('row', { name: /workspace-bob/ })).toHaveCount(0)
    await expect(page.getByText('成员 · 1')).toBeVisible()

    // 4. Create a cohort from the header action; it becomes the selected cohort.
    await page.getByRole('button', { name: '创建年级' }).first().click()
    const dialog = page.getByRole('dialog')
    await dialog.getByRole('textbox', { name: '年级名称' }).fill('25级')
    await dialog.getByRole('button', { name: '创建', exact: true }).click()
    await expect(page.getByText('年级已创建')).toBeVisible()
    await expect(page.getByText('25级').first()).toBeVisible()
    await expect(page.getByText('当前年级暂无成员')).toBeVisible()

  })

  test('members tab shows the empty state when no cohorts exist', async ({ page }) => {
    test.setTimeout(60_000)
    await mockAdmin(page)

    await page.goto('/')
    await page.evaluate(() => localStorage.setItem('language', JSON.stringify('zh-CN')))

    await page.route(/\/api\/admin\/cohorts(\?|$)/, async (route) => {
      if (route.request().method() === 'GET') return route.fulfill({ json: [] })
      return route.fulfill({ json: { id: '10000000-0000-0000-0000-00000000000a', name: '24级' } })
    })

    await page.goto('/admin/skill-trees?tab=members')
    await expect(page.getByText('暂无年级')).toBeVisible()

  })
})
