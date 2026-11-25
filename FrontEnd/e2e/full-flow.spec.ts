import { randomBytes } from 'node:crypto'
import { expect, test, type Browser, type Page } from '@playwright/test'

const PASSWORD = 'E2e_Passw0rd1'

function pdf(pages: number): Buffer {
  const kids = Array.from({ length: pages }, (_, i) => `${i + 3} 0 R`).join(' ')
  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    `<< /Type /Pages /Kids [${kids}] /Count ${pages} >>`,
    ...Array.from({ length: pages }, () => '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>'),
  ]
  let body = '%PDF-1.4\n'
  const offsets: number[] = []
  objects.forEach((obj, i) => {
    offsets.push(body.length)
    body += `${i + 1} 0 obj\n${obj}\nendobj\n`
  })
  const xref = body.length
  body += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`
  body += offsets.map((o) => `${String(o).padStart(10, '0')} 00000 n \n`).join('')
  body += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`
  return Buffer.from(body, 'latin1')
}

const corruptedPdf = () =>
  Buffer.concat([Buffer.from('%PDF-1.7\n'), randomBytes(30_000), Buffer.from('\n%%EOF\n')])

async function register(browser: Browser, email: string): Promise<Page> {
  const page = await (await browser.newContext()).newPage()
  await page.goto('/register')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password', { exact: true }).fill(PASSWORD)
  await page.getByLabel('Confirm password').fill(PASSWORD)
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page.getByRole('heading', { name: 'My files' })).toBeVisible()
  return page
}

async function upload(page: Page, name: string, buffer: Buffer) {
  await page.getByTestId('upload-input').setInputFiles({ name, mimeType: 'application/pdf', buffer })
}

test('full flow: upload, processing, public + restricted sharing, revoke, delete', async ({ browser }) => {
  test.setTimeout(180_000)
  const run = Date.now().toString(36)
  const ownerEmail = `e2e-${run}-owner@test.local`
  const friendEmail = `e2e-${run}-friend@test.local`
  const strangerEmail = `e2e-${run}-stranger@test.local`

  const friendPage = await register(browser, friendEmail)
  const strangerPage = await register(browser, strangerEmail)
  const owner = await register(browser, ownerEmail)

  await test.step('client-side validation rejects non-PDF and >20 MB', async () => {
    await owner.getByTestId('upload-input').setInputFiles({
      name: 'notes.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from('hello'),
    })
    await expect(owner.getByRole('alert')).toContainText('Only PDF files are allowed')
    await upload(owner, 'huge.pdf', Buffer.alloc(20 * 1024 * 1024 + 1, 0x20))
    await expect(owner.getByRole('alert')).toContainText('larger than 20 MB')
  })

  const name = `report-${run}.pdf`
  const row = owner.getByRole('row', { name: new RegExp(name.replace('.', '\\.')) })

  await test.step('valid 3-page PDF: Processing → Ready, page count and thumbnail', async () => {
    await upload(owner, name, pdf(3))
    await expect(row.getByText('Ready')).toBeVisible({ timeout: 60_000 })
    await expect(row.getByRole('cell').nth(2)).toHaveText('3')
    await expect
      .poll(() => row.locator('img').evaluate((img: HTMLImageElement) => img.naturalWidth), { timeout: 15_000 })
      .toBeGreaterThan(0)
  })

  await test.step('corrupted PDF is accepted, then Rejected by the Function', async () => {
    const bad = `broken-${run}.pdf`
    await upload(owner, bad, corruptedPdf())
    const badRow = owner.getByRole('row', { name: new RegExp(bad.replace('.', '\\.')) })
    await expect(badRow.getByText('Rejected')).toBeVisible({ timeout: 60_000 })
  })

  await test.step('owner downloads the real file', async () => {
    const download = owner.waitForEvent('download')
    await row.getByRole('button', { name: new RegExp(`Download ${name}`) }).click()
    expect((await download).suggestedFilename()).toBe(name)
  })

  let publicUrl = ''
  await test.step('public link: anonymous visitor sees info and downloads', async () => {
    await row.getByRole('button', { name: `Share ${name}` }).click()
    await owner.getByRole('button', { name: 'Create public link' }).click()
    publicUrl = await owner.getByLabel('Share link').first().inputValue()
    expect(publicUrl).toMatch(/\/s\/[\w-]+$/)

    const visitor = await (await browser.newContext()).newPage()
    await visitor.goto(publicUrl)
    await expect(visitor.getByRole('heading', { name })).toBeVisible()
    await expect(visitor.getByText('3 pages')).toBeVisible()
    const download = visitor.waitForEvent('download')
    await visitor.getByRole('button', { name: 'Download' }).click()
    expect((await download).suggestedFilename()).toBe(name)
  })

  let restrictedUrl = ''
  await test.step('restricted link for the friend (user search)', async () => {
    await owner.getByRole('tab', { name: 'Restricted' }).click()
    await owner.getByRole('combobox', { name: 'Add people by email' }).fill(`e2e-${run}-fr`)
    await owner.getByRole('option', { name: friendEmail }).click()
    await expect(owner.getByRole('list', { name: 'Selected people' })).toContainText(friendEmail)
    await owner.getByRole('button', { name: 'Create restricted link' }).click()
    await expect(owner.getByText('Link created')).toBeVisible()
    restrictedUrl = await owner.getByLabel('Share link').first().inputValue()
    await expect(owner.getByText(`Shared with: ${friendEmail}`)).toBeVisible()
  })

  await test.step('restricted: anonymous is sent to login, then the friend gets the file', async () => {
    const page = await (await browser.newContext()).newPage()
    await page.goto(restrictedUrl)
    await expect(page).toHaveURL(/\/login\?returnUrl=/)
    await page.getByLabel('Email').fill(friendEmail)
    await page.getByLabel('Password').fill(PASSWORD)
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page.getByRole('heading', { name })).toBeVisible()
    const download = page.waitForEvent('download')
    await page.getByRole('button', { name: 'Download' }).click()
    expect((await download).suggestedFilename()).toBe(name)
  })

  await test.step('restricted: a signed-in user who is not on the list gets "No access"', async () => {
    await strangerPage.goto(restrictedUrl)
    await expect(strangerPage.getByRole('heading', { name: 'No access' })).toBeVisible()
  })

  await test.step('friend: "Shared with me" lists the file and downloads it; stranger sees nothing', async () => {
    await friendPage.goto('/shared')
    const sharedRow = friendPage.getByRole('row', { name: new RegExp(name.replace('.', '\\.')) })
    await expect(sharedRow).toContainText(ownerEmail)
    const download = friendPage.waitForEvent('download')
    await sharedRow.getByRole('button').click()
    expect((await download).suggestedFilename()).toBe(name)

    await strangerPage.goto('/shared')
    await expect(strangerPage.getByText('Nothing shared with you yet')).toBeVisible()
  })

  await test.step('friend has no owner actions: own list is empty', async () => {
    await friendPage.goto('/files')
    await expect(friendPage.getByRole('row', { name: new RegExp(name.replace('.', '\\.')) })).toHaveCount(0)
  })

  await test.step('revoking the public link makes it unavailable', async () => {
    const publicItem = owner
      .getByRole('listitem')
      .filter({ has: owner.locator(`input[value="${publicUrl}"]`) })
    await publicItem.getByRole('button', { name: 'Revoke' }).click()
    await expect(owner.getByText('Revoked').first()).toBeVisible()

    const visitor = await (await browser.newContext()).newPage()
    await visitor.goto(publicUrl)
    await expect(visitor.getByRole('heading', { name: 'Link not available' })).toBeVisible()
  })

  await test.step('session survives a full page reload (refresh cookie)', async () => {
    await friendPage.reload()
    await expect(friendPage.getByRole('heading', { name: 'My files' })).toBeVisible()
  })

  await test.step('deleting the file kills its restricted link; cleanup', async () => {
    await owner.getByRole('button', { name: 'Close dialog' }).click()
    owner.on('dialog', (dialog) => dialog.accept())
    for (const fileRow of await owner.getByRole('row').filter({ hasText: run }).all()) {
      await fileRow.getByRole('button', { name: /^Delete / }).click()
    }
    await expect(owner.getByRole('row').filter({ hasText: run })).toHaveCount(0)

    await friendPage.goto(restrictedUrl)
    await expect(friendPage.getByRole('heading', { name: 'Link not available' })).toBeVisible()
  })

  await test.step('logout returns to sign-in and protected pages redirect', async () => {
    await owner.getByRole('button', { name: 'Log out' }).click()
    await expect(owner).toHaveURL(/\/login/)
    await owner.goto('/files')
    await expect(owner).toHaveURL(/\/login\?returnUrl=%2Ffiles/)
  })
})
