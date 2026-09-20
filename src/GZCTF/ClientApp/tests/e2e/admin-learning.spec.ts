import { test, expect } from '@playwright/test'

test.describe('learning administration', () => {
  test('administration fixtures are configured explicitly', async () => {
    test.skip(!process.env.E2E_ADMIN_USER || !process.env.E2E_ADMIN_PASSWORD, 'Set E2E_ADMIN_USER/PASSWORD')
    expect(process.env.E2E_ADMIN_USER).toBeTruthy()
  })
})
