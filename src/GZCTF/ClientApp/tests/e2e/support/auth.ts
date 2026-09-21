import { expect, type Browser, type BrowserContext, type Page } from '@playwright/test'

export type TestRole = 'anonymous' | 'learner' | 'administrator'

type Credentials = {
  userName: string
  password: string
}

const credentials: Record<Exclude<TestRole, 'anonymous'>, Credentials | undefined> = {
  learner: process.env.E2E_LEARNER_USER && process.env.E2E_LEARNER_PASSWORD
    ? { userName: process.env.E2E_LEARNER_USER, password: process.env.E2E_LEARNER_PASSWORD }
    : undefined,
  administrator: process.env.E2E_ADMIN_USER && process.env.E2E_ADMIN_PASSWORD
    ? { userName: process.env.E2E_ADMIN_USER, password: process.env.E2E_ADMIN_PASSWORD }
    : undefined,
}

export async function createRoleContext(browser: Browser, role: TestRole): Promise<BrowserContext> {
  const context = await browser.newContext()
  if (role === 'anonymous') return context

  const account = credentials[role]
  if (!account) {
    await context.close()
    throw new Error(`Missing E2E credentials for ${role}`)
  }

  const page = await context.newPage()
  await login(page, account)
  await page.close()
  return context
}

export async function login(page: Page, account: Credentials): Promise<void> {
  await page.goto('/account/login')
  await page.locator('input[type="text"], input[name="userName"], input[type="email"]').first().fill(account.userName)
  await page.locator('input[type="password"]').first().fill(account.password)
  await page.getByRole('button', { name: /log ?in|sign ?in|登录/i }).click()
  await expect(page).not.toHaveURL(/\/account\/login/)
}
