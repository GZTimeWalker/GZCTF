import { test, expect } from '@playwright/test'

test.describe('learning route discovery', () => {
  test('route discovery is reachable anonymously without aggregate progress', async ({ page }) => {
    await page.goto('/skill-trees')
    await expect(page).toHaveURL(/\/skill-trees(?:\/)?$/)
    await expect(page.getByText(/progress|进度/i)).toHaveCount(0)
  })
})
