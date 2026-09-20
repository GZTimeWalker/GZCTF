import { test, expect } from '@playwright/test'

test.describe('learning route discovery', () => {
  test('route discovery is reachable anonymously without aggregate progress', async ({ page }) => {
    await page.goto('/learn')
    await expect(page).toHaveURL(/\/learn(?:\/)?$/)
    await expect(page.getByText(/progress|进度/i)).not.toBeVisible()
  })
})
