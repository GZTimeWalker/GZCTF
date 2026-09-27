import { expect, test } from '@playwright/test'

test.describe('skill tree discovery', () => {
  test('shows public counts and never renders aggregate progress', async ({ page }) => {
    await page.route('**/api/skill-trees', (route) =>
      route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify([
          {
            skillTreeId: '01990000-0000-7000-8000-000000000001',
            name: 'Web 工程师',
            summary: '从 HTTP 到漏洞利用',
            iconKey: 'web',
            categoryCount: 3,
            challengeCount: 12,
            lessonCount: 6,
          },
        ]),
      }),
    )

    await page.goto('/skill-trees')
    await expect(page.getByText('Web 工程师')).toBeVisible()
    await expect(page.getByText(/3\s+categories|3\s+个类别/)).toBeVisible()
    await expect(page.getByText(/12\s+challenges|12\s+道题目/)).toBeVisible()
    await expect(page.getByText(/6\s+lessons|6\s+篇课节/)).toBeVisible()
    await expect(page.getByText(/%|路线进度|完成模块|完成课时/i)).toHaveCount(0)
  })
})
