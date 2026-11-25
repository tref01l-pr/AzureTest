import { expect, test } from '@playwright/test'

function minimalPdf(): Buffer {
  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>',
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

test('register → upload → public link downloads anonymously', async ({ page, browser }) => {
  const email = `e2e-${Date.now()}@example.com`

  await page.goto('/register')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password', { exact: true }).fill('Secret123!')
  await page.getByLabel('Confirm password').fill('Secret123!')
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page.getByRole('heading', { name: 'My files' })).toBeVisible()

  await page.getByTestId('upload-input').setInputFiles({
    name: 'e2e.pdf',
    mimeType: 'application/pdf',
    buffer: minimalPdf(),
  })
  const row = page.getByRole('row', { name: /e2e\.pdf/ })
  await expect(row.getByText('Ready')).toBeVisible({ timeout: 60_000 })
  await expect(row.getByRole('cell').nth(2)).toHaveText('1')

  await row.getByRole('button', { name: 'Share e2e.pdf' }).click()
  await page.getByRole('button', { name: 'Create public link' }).click()
  const shareUrl = await page.getByLabel('Share link').first().inputValue()
  expect(shareUrl).toMatch(/\/s\/[\w-]+$/)

  const anonymous = await browser.newContext()
  const visitor = await anonymous.newPage()
  await visitor.goto(shareUrl)
  await expect(visitor.getByRole('heading', { name: 'e2e.pdf' })).toBeVisible()
  const downloadUrl = visitor.waitForResponse((r) => r.url().includes('/download-url') && r.ok())
  await visitor.getByRole('button', { name: 'Download' }).click()
  expect((await (await downloadUrl).json()).url).toMatch(/^https?:\/\//)
  await anonymous.close()

  await page.getByRole('button', { name: 'Close dialog' }).click()
  page.on('dialog', (dialog) => dialog.accept())
  await row.getByRole('button', { name: 'Delete e2e.pdf' }).click()
  await expect(row).toHaveCount(0)
})
