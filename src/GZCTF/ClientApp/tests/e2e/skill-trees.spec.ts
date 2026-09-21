import { expect, test, type Page } from '@playwright/test'
import { createRoleContext } from './support/auth'
import {
  assertNoRawKeys,
  collectFailures,
  PROGRESS_TEXT,
  seedSharedTrees,
  switchLanguage,
} from './support/skillTreeSeed'

/**
 * ST27 release gate. Walks the anonymous discovery, enrollment, lesson, challenge and
 * personal record journeys end to end and fails on any console error, page error or
 * response with a status of 500 or above.
 */

test.describe('skill tree learner journey', () => {
  test('visitor and learner journeys stay clean and progress stays private', async ({ browser }) => {
    test.skip(
      !process.env.E2E_ADMIN_USER || !process.env.E2E_ADMIN_PASSWORD ||
        !process.env.E2E_LEARNER_USER || !process.env.E2E_LEARNER_PASSWORD,
      'Set E2E_ADMIN_USER/E2E_ADMIN_PASSWORD and E2E_LEARNER_USER/E2E_LEARNER_PASSWORD to run the journey'
    )
    test.setTimeout(240_000)

    const suffix = Date.now().toString().slice(-6)
    const sharedCategory = `Shared ${suffix}`
    const firstTree = `Journey A ${suffix}`
    const secondTree = `Journey B ${suffix}`
    const lessonTitle = `Journey lesson ${suffix}`
    const challengeTitle = `Journey challenge ${suffix}`
    const flag = `flag{journey-${suffix}}`

    const seed = await seedSharedTrees(browser, {
      suffix,
      categoryName: sharedCategory,
      firstTreeName: firstTree,
      secondTreeName: secondTree,
      lessonTitle,
      challengeTitle,
      flag,
      // An empty published tree backs the anonymous empty-state assertion.
      emptyTreeName: `Empty ${suffix}`,
    })
    const emptyTreeId = seed.emptyTreeId!

    // 1. Anonymous visitor opens discovery and an empty published tree.
    const anonymous = await browser.newContext()
    const visitorPage = await anonymous.newPage()
    const visitorFailures = collectFailures(visitorPage, [
      // An anonymous visitor legitimately has no profile.
      { url: /\/api\/account\/profile$/, status: 401 },
    ])

    await visitorPage.goto('/skill-trees')
    await expect(visitorPage).toHaveURL(/\/skill-trees\/?$/)
    await expect(visitorPage.getByText(firstTree).first()).toBeVisible()
    await expect(visitorPage.getByText(secondTree).first()).toBeVisible()
    await expect(visitorPage.getByText(PROGRESS_TEXT)).toHaveCount(0)

    await visitorPage.goto(`/skill-trees/${emptyTreeId}`)
    await expect(visitorPage.getByText(PROGRESS_TEXT)).toHaveCount(0)
    await anonymous.close()

    // 2. Sign in, join two trees and set one current.
    const context = await createRoleContext(browser, 'learner')
    const page = await context.newPage()
    const learnerFailures = collectFailures(page, [
      // The challenge workspace probes for an instance that has not been started yet.
      { url: /\/instances$/, status: 404 },
    ])

    const joinAndSelect = async (treeId: string) => {
      await page.goto(`/skill-trees/${treeId}`)
      // Wait for the enrollment control to settle before deciding which state it is in.
      await expect(page.getByRole('button', { name: /^Join$|^Set current$/ }).first()).toBeVisible()
      const join = page.getByRole('button', { name: 'Join' })
      if (await join.count()) await join.click()
      await page.getByRole('button', { name: 'Set current' }).click()
      await expect(page.getByText('Current').first()).toBeVisible()
    }

    await joinAndSelect(seed.treeId)
    await joinAndSelect(seed.secondTreeId)

    // 3. Open a Markdown lesson and mark complete.
    await page.goto(`/skill-trees/${seed.treeId}/${seed.categoryId}/lesson/${seed.lessonId}`)
    await expect(page.getByRole('heading', { name: lessonTitle })).toBeVisible()
    await page.getByRole('button', { name: 'Mark complete' }).click()
    await expect(page.getByRole('button', { name: 'Mark complete' })).toBeEnabled()

    // 4. Reveal a hint and the official write-up, then submit the Flag.
    await page.goto(`/skill-trees/${seed.treeId}/${seed.categoryId}/challenge/${seed.challengeId}`)
    await expect(page.getByRole('heading', { name: challengeTitle })).toBeVisible()
    await page.getByRole('button', { name: 'Reveal next hint' }).click()
    await page.getByRole('button', { name: 'Show official write-up' }).click()
    await page.getByRole('textbox').first().fill(flag)
    await page.getByRole('button', { name: 'Submit flag' }).click()
    await expect(page.getByText('Flag accepted.')).toBeVisible({ timeout: 15_000 })

    // 5. Personal records reflect both shared references.
    await page.goto('/account/learning')
    await expect(page.getByText(firstTree).first()).toBeVisible()
    await expect(page.getByText(secondTree).first()).toBeVisible()
    await expect(page.getByText(challengeTitle).first()).toBeVisible()
    await expect(page.getByText(lessonTitle).first()).toBeVisible()

    // 6. Switch languages and assert no raw translation key leaks.
    await switchLanguage(page, 'zh-CN')
    await expect(page.getByText(firstTree).first()).toBeVisible()
    await assertNoRawKeys(page)
    await switchLanguage(page, 'en-US')
    await assertNoRawKeys(page)

    expect(learnerFailures).toEqual([])
    expect(visitorFailures).toEqual([])

    await context.close()
  })
})
