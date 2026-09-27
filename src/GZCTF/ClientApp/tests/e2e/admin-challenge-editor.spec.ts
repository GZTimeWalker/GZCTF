import { expect, test } from '@playwright/test'

test('administrator creates an independent Web container challenge from the workspace', async ({ page }) => {
  const challengeId = '10000000-0000-0000-0000-000000000001'
  let created: Record<string, unknown> | undefined

  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  await page.route(/\/api\/admin\/challenges(\?|$)/, (route) => {
    if (route.request().method() === 'GET') return route.fulfill({ json: [] })
    created = route.request().postDataJSON()
    return route.fulfill({ json: { challenge: { id: challengeId, title: 'Web shell' } } })
  })

  await page.goto('/admin/skill-trees?tab=challenges')
  await page.getByRole('button', { name: 'Create challenge' }).first().click()
  const dialog = page.getByRole('dialog')
  await dialog.getByRole('textbox', { name: 'English title' }).fill('Web shell')
  await dialog.getByRole('combobox', { name: 'CTF category' }).click()
  await page.getByRole('option', { name: /Web/ }).click()
  await dialog.getByRole('combobox', { name: 'Runtime type' }).click()
  await page.getByRole('option', { name: /Dynamic Container/ }).click()
  await dialog.getByRole('button', { name: 'Create', exact: true }).click()

  await expect(page).toHaveURL(new RegExp(`/admin/library/challenges/${challengeId}$`))
  expect(created).toMatchObject({ type: 'DynamicContainer', ctfCategory: 'Web' })
})

test('administrator edits container settings and Flag template without writing JSON', async ({ page }) => {
  const challengeId = '10000000-0000-0000-0000-000000000002'
  let saved: Record<string, unknown> | undefined
  const edit = {
    challenge: {
      id: challengeId, title: 'Container lesson', type: 'DynamicContainer', ctfCategory: 'Web',
      difficulty: 'Normal', isEnabled: true, publicationState: 'Draft',
    },
    localizations: [{ locale: 'en', title: 'Container lesson', summary: '', body: 'Original body' }],
    runtimeConfigurationJson: JSON.stringify({
      ContainerImage: 'registry.test/old:1', ExposedPort: 8080, Cpu: 1,
      MemoryMb: 128, StorageMb: 256, NetworkMode: 'Open', FlagTemplate: 'flag{old-{userId}}',
    }),
    flags: [{ kind: 'Template', template: 'flag{old-{userId}}' }],
    hints: [], writeups: [],
    publication: { rowVersion: 1, publicationState: 'Draft', categoryIds: [] },
  }
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  await page.route(`**/api/admin/challenges/${challengeId}/edit**`, (route) => route.fulfill({ json: edit }))
  await page.route(`**/api/admin/challenges/${challengeId}`, (route) => {
    saved = route.request().postDataJSON()
    return route.fulfill({ json: edit })
  })

  await page.goto(`/admin/library/challenges/${challengeId}`)
  await page.getByRole('textbox', { name: 'Challenge description' }).fill('Updated body')
  await page.getByRole('textbox', { name: 'Container image' }).fill('registry.test/new:2')
  await page.getByRole('textbox', { name: 'Flag template' }).fill('flag{new-{userId}}')
  await page.getByRole('textbox', { name: 'Submission limit' }).fill('3')
  await page.getByRole('button', { name: 'Save draft' }).click()

  expect(saved).toMatchObject({
    ctfCategory: 'Web',
    type: 'DynamicContainer',
    submissionLimit: 3,
    localizations: [{ locale: 'en', title: 'Container lesson', body: 'Updated body' }],
    flags: [{ kind: 'Template', template: 'flag{new-{userId}}' }],
  })
  expect(JSON.parse(String(saved?.runtimeConfigurationJson))).toMatchObject({
    ContainerImage: 'registry.test/new:2', ExposedPort: 8080,
    FlagTemplate: 'flag{new-{userId}}',
  })
})

test('administrator can take a published challenge off the skill tree without deleting it', async ({ page }) => {
  const challengeId = '10000000-0000-0000-0000-000000000003'
  let enabled = true
  let update: Record<string, unknown> | undefined
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  await page.route(/\/api\/admin\/challenges(\?|$)/, (route) =>
    route.fulfill({ json: [{
      id: challengeId, title: 'Visible challenge', type: 'StaticAttachment',
      ctfCategory: 'Web', difficulty: 'Easy', publicationState: 'Published', isEnabled: enabled,
    }] })
  )
  await page.route(`**/api/admin/challenges/${challengeId}`, (route) => {
    update = route.request().postDataJSON()
    enabled = Boolean(update?.isEnabled)
    return route.fulfill({ json: { challenge: { id: challengeId, isEnabled: enabled } } })
  })

  await page.goto('/admin/skill-trees?tab=challenges')
  await page.getByRole('switch', { name: 'Enabled for learners' }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Disable challenge' }).click()
  await expect(page.getByRole('switch', { name: 'Enabled for learners' })).not.toBeChecked()
  expect(update).toMatchObject({ isEnabled: false })
})

test('static challenge uploads a handout and publishes into a selected tree category', async ({ page }) => {
  const challengeId = '10000000-0000-0000-0000-000000000004'
  const treeId = '30000000-0000-0000-0000-000000000001'
  const categoryId = '40000000-0000-0000-0000-000000000001'
  const hash = 'a'.repeat(64)
  let saved: Record<string, unknown> | undefined
  let published: Record<string, unknown> | undefined
  const edit = {
    challenge: { id: challengeId, title: 'Handout', type: 'StaticAttachment', ctfCategory: 'Misc',
      difficulty: 'Normal', expectedMinutes: 30, isEnabled: true, publicationState: 'Draft' },
    localizations: [{ locale: 'en', title: 'Handout', summary: '', body: 'Find the Flag' }],
    runtimeConfigurationJson: null, flags: [], hints: [], writeups: [],
    publication: { rowVersion: 1, publicationState: 'Draft', categoryIds: [] },
  }
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  await page.route('**/api/admin/skill-trees', (route) =>
    route.fulfill({ json: [{ skillTreeId: treeId, name: 'Intro Tree', isPublished: true }] })
  )
  await page.route('**/api/admin/skill-categories', (route) =>
    route.fulfill({ json: [{ categoryId, name: 'Web Basics', trees: [{ skillTreeId: treeId }] }] })
  )
  await page.route('**/api/assets', (route) => route.fulfill({ json: [{ hash, name: 'handout.txt' }] }))
  await page.route(`**/api/admin/challenges/${challengeId}/edit**`, (route) => route.fulfill({ json: edit }))
  await page.route(`**/api/admin/challenges/${challengeId}`, (route) => {
    saved = route.request().postDataJSON()
    return route.fulfill({ json: { ...edit, publication: { ...edit.publication, rowVersion: 2 } } })
  })
  await page.route(`**/api/admin/challenges/${challengeId}/publish`, (route) => {
    published = route.request().postDataJSON()
    return route.fulfill({ status: 204, body: '' })
  })

  await page.goto(`/admin/library/challenges/${challengeId}`)
  await page.getByRole('button', { name: 'Add Flag' }).click()
  await page.getByRole('textbox', { name: 'Flag 1' }).fill('flag{handout}')
  const [chooser] = await Promise.all([
    page.waitForEvent('filechooser'),
    page.getByRole('button', { name: 'Upload attachment' }).click(),
  ])
  await chooser.setFiles({ name: 'handout.txt', mimeType: 'text/plain', buffer: Buffer.from('attachment') })
  await expect(page.getByText('handout.txt')).toBeVisible()
  await page.getByRole('button', { name: 'Publish challenge' }).click()
  const modal = page.getByRole('dialog')
  await modal.getByText('Intro Tree').click()
  await modal.getByText('Web Basics').click()
  await modal.getByRole('button', { name: 'Publish', exact: true }).click()

  expect(saved?.flags).toEqual([{ kind: 'Static', value: 'flag{handout}' }])
  expect(JSON.parse(String(saved?.runtimeConfigurationJson))).toMatchObject({
    Attachments: [{ FileName: 'handout.txt', StorageKey: `uploads/aa/aa/${hash}`, Sha256: hash }],
  })
  expect(published).toMatchObject({ rowVersion: 2, categoryIds: [categoryId] })
})

test('challenge library filters independently by CTF category', async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  await page.route(/\/api\/admin\/challenges(\?|$)/, (route) => route.fulfill({ json: [
    { id: '10000000-0000-0000-0000-000000000005', title: 'Web challenge', type: 'StaticAttachment',
      ctfCategory: 'Web', publicationState: 'Draft', isEnabled: true },
    { id: '10000000-0000-0000-0000-000000000006', title: 'Misc challenge', type: 'StaticAttachment',
      ctfCategory: 'Misc', publicationState: 'Draft', isEnabled: true },
  ] }))

  await page.goto('/admin/skill-trees?tab=challenges')
  await page.getByRole('combobox', { name: 'Filter by CTF category' }).click()
  await page.getByRole('option', { name: /Web/ }).click()
  await expect(page.getByText('Web challenge')).toBeVisible()
  await expect(page.getByText('Misc challenge')).toHaveCount(0)
})

test('stale challenge edits stop for review instead of overwriting another administrator', async ({ page }) => {
  const challengeId = '10000000-0000-0000-0000-000000000007'
  let writes = 0
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  await page.route(`**/api/admin/challenges/${challengeId}/edit**`, (route) => route.fulfill({ json: {
    challenge: { id: challengeId, title: 'Original', type: 'StaticAttachment', ctfCategory: 'Web',
      difficulty: 'Normal', isEnabled: true, publicationState: 'Draft' },
    localizations: [{ locale: 'en', title: 'Original', summary: '', body: '' }],
    flags: [], hints: [], writeups: [], publication: { rowVersion: 1, categoryIds: [] },
  } }))
  await page.route(`**/api/admin/challenges/${challengeId}`, (route) => {
    writes += 1
    return route.fulfill({ status: 409, json: { code: 'learning.challenge_changed' } })
  })

  await page.goto(`/admin/library/challenges/${challengeId}`)
  await page.getByRole('textbox', { name: 'English title' }).fill('My edit')
  await page.getByRole('button', { name: 'Save draft' }).click()
  await expect(page.getByRole('alert').filter({ hasText: 'Reload and review' })).toBeVisible()
  expect(writes).toBe(1)
})

test('retiring a challenge requires confirmation and returns to the library', async ({ page }) => {
  const challengeId = '10000000-0000-0000-0000-000000000008'
  let retired = false
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  await page.route(`**/api/admin/challenges/${challengeId}/edit**`, (route) => route.fulfill({ json: {
    challenge: { id: challengeId, title: 'Retire me', type: 'StaticAttachment', ctfCategory: 'Misc',
      difficulty: 'Normal', isEnabled: true, publicationState: 'Published' },
    localizations: [{ locale: 'en', title: 'Retire me', summary: '', body: '' }],
    flags: [], hints: [], writeups: [], publication: { rowVersion: 1, categoryIds: [] },
  } }))
  await page.route(`**/api/admin/challenges/${challengeId}`, (route) => {
    if (route.request().method() === 'DELETE') retired = true
    return route.fulfill({ status: 204, body: '' })
  })

  await page.goto(`/admin/library/challenges/${challengeId}`)
  await page.getByRole('button', { name: 'Retire challenge' }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Retire', exact: true }).click()
  await expect(page).toHaveURL(/\/admin\/skill-trees\?tab=challenges$/)
  expect(retired).toBe(true)
})

test('dynamic attachments keep a separate Flag for each uploaded file', async ({ page }) => {
  const challengeId = '10000000-0000-0000-0000-000000000009'
  let uploads = 0
  let saved: Record<string, unknown> | undefined
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  await page.route(`**/api/admin/challenges/${challengeId}/edit**`, (route) => route.fulfill({ json: {
    challenge: { id: challengeId, title: 'Attachment pool', type: 'DynamicAttachment',
      ctfCategory: 'Misc', difficulty: 'Normal', isEnabled: true, publicationState: 'Draft' },
    localizations: [{ locale: 'en', title: 'Attachment pool', summary: '', body: '' }],
    flags: [], hints: [], writeups: [], publication: { rowVersion: 1, categoryIds: [] },
  } }))
  await page.route('**/api/assets', (route) => {
    uploads += 1
    return route.fulfill({ json: [{ hash: String(uploads).repeat(64), name: `file-${uploads}.txt` }] })
  })
  await page.route(`**/api/admin/challenges/${challengeId}`, (route) => {
    saved = route.request().postDataJSON()
    return route.fulfill({ json: { publication: { rowVersion: 2 } } })
  })

  await page.goto(`/admin/library/challenges/${challengeId}`)
  for (const number of [1, 2]) {
    const [chooser] = await Promise.all([
      page.waitForEvent('filechooser'),
      page.getByRole('button', { name: 'Upload attachment' }).click(),
    ])
    await chooser.setFiles({ name: `file-${number}.txt`, mimeType: 'text/plain', buffer: Buffer.from(`${number}`) })
  }
  await page.getByRole('textbox', { name: 'Flag for this attachment' }).nth(0).fill('flag{one}')
  await page.getByRole('textbox', { name: 'Flag for this attachment' }).nth(1).fill('flag{two}')
  await page.getByRole('button', { name: 'Save draft' }).click()

  const flags = saved?.flags as Array<{ kind: string; metadataJson: string }>
  expect(flags).toHaveLength(1)
  expect(flags[0].kind).toBe('DynamicAttachment')
  expect(JSON.parse(flags[0].metadataJson)).toMatchObject([
    { FileName: 'file-1.txt', Flag: 'flag{one}' },
    { FileName: 'file-2.txt', Flag: 'flag{two}' },
  ])
})

test('administrator can start and stop a test container from an unpublished draft', async ({ page }) => {
  const challengeId = '10000000-0000-0000-0000-000000000010'
  let starts = 0
  let stops = 0
  await page.addInitScript(() => localStorage.setItem('language', JSON.stringify('en-US')))
  await page.route('**/api/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/account/profile', (route) =>
    route.fulfill({ json: { userId: '20000000-0000-0000-0000-000000000001', userName: 'admin', role: 'Admin' } })
  )
  const edit = {
    challenge: { id: challengeId, title: 'Test container', type: 'StaticContainer',
      ctfCategory: 'Web', difficulty: 'Normal', isEnabled: true, publicationState: 'Draft' },
    localizations: [{ locale: 'en', title: 'Test container', summary: '', body: '' }],
    runtimeConfigurationJson: JSON.stringify({ ContainerImage: 'registry.test/box:1', ExposedPort: 8080,
      Cpu: 1, MemoryMb: 128, StorageMb: 256, NetworkMode: 'Open' }),
    flags: [{ kind: 'Static', value: 'flag{test}' }], hints: [], writeups: [],
    publication: { rowVersion: 1, categoryIds: [] },
  }
  await page.route(`**/api/admin/challenges/${challengeId}/edit**`, (route) => route.fulfill({ json: edit }))
  await page.route(`**/api/admin/challenges/${challengeId}`, (route) =>
    route.fulfill({ json: { ...edit, publication: { rowVersion: 2, categoryIds: [] } } })
  )
  await page.route(`**/api/challenges/${challengeId}/instances`, (route) => {
    if (route.request().method() === 'POST') {
      starts += 1
      return route.fulfill({ json: {
        id: '50000000-0000-0000-0000-000000000001', status: 1,
        publicIp: '203.0.113.5', publicPort: 18080,
      } })
    }
    if (route.request().method() === 'DELETE') {
      stops += 1
      return route.fulfill({ status: 204, body: '' })
    }
    return route.fulfill({ status: 404, json: {} })
  })

  await page.goto(`/admin/library/challenges/${challengeId}`)
  await page.getByRole('button', { name: 'Start test container' }).click()
  await expect(page.getByRole('button', { name: 'Stop test container' })).toBeVisible()
  await expect(page.getByText('203.0.113.5:18080')).toBeVisible()
  await page.getByRole('button', { name: 'Stop test container' }).click()
  await expect(page.getByRole('button', { name: 'Start test container' })).toBeVisible()
  expect({ starts, stops }).toEqual({ starts: 1, stops: 1 })
})
