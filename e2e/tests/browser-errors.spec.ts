import { createServer, type Server } from 'node:http';
import { readFileSync } from 'node:fs';
import { expect, test } from '@playwright/test';
import { BROWSER_KEY, INGEST_PORT } from '../playwright.config';

interface Recorded {
  method: string;
  path: string;
  query: Record<string, string>;
  authorization?: string;
  body: string;
}

let server: Server;
let recorded: Recorded[] = [];

// A tiny ingest: answers the browser's CORS preflight like production and records every request.
test.beforeAll(async () => {
  server = createServer((req, res) => {
    const url = new URL(req.url!, 'http://localhost');
    const origin = req.headers.origin;
    const cors = origin ? { 'Access-Control-Allow-Origin': origin, Vary: 'Origin' } : {};
    if (req.method === 'OPTIONS') {
      res.writeHead(204, {
        ...cors,
        'Access-Control-Allow-Headers': 'authorization, content-type',
        'Access-Control-Allow-Methods': 'POST'
      }).end();
      return;
    }
    const chunks: Buffer[] = [];
    req.on('data', (chunk) => chunks.push(chunk));
    req.on('end', () => {
      recorded.push({
        method: req.method!,
        path: url.pathname,
        query: Object.fromEntries(url.searchParams),
        authorization: req.headers.authorization,
        body: Buffer.concat(chunks).toString('utf8')
      });
      res.writeHead(200, { ...cors, 'Content-Type': 'application/json' }).end('{}');
    });
  });
  await new Promise<void>((resolve) => server.listen(INGEST_PORT, '127.0.0.1', resolve));
});

test.afterAll(async () => {
  await new Promise((resolve) => server.close(resolve));
});

test.beforeEach(async ({ page }) => {
  recorded = [];
  // The script is served from ourfault's app origin. OURFAULT_BROWSER_SCRIPT points at a local build to use instead.
  if (process.env.OURFAULT_BROWSER_SCRIPT) {
    const script = readFileSync(process.env.OURFAULT_BROWSER_SCRIPT, 'utf8');
    await page.route('https://app.ourfault.dev/browser/v1.js', (route) =>
      route.fulfill({ contentType: 'text/javascript', body: script })
    );
  }
});

const records = () =>
  recorded
    .filter((r) => r.path === '/v1/logs' && r.method === 'POST' && r.body.startsWith('{'))
    .map((r) => {
      const record = JSON.parse(r.body).resourceLogs[0].scopeLogs[0].logRecords[0];
      const attrs: Record<string, any> = {};
      for (const a of record.attributes) attrs[a.key] = a.value;
      return { request: r, record, attrs, name: record.eventName as string | undefined };
    });

const waitForError = async (type: string) => {
  await expect.poll(() => records().some((r) => r.attrs['exception.type']?.stringValue === type), { timeout: 10_000 }).toBe(true);
  return records().find((r) => r.attrs['exception.type']?.stringValue === type)!;
};

test('the newsletter form throws a TypeError that reaches ingest after the page view, with the visitor\'s clicks', async ({ page }) => {
  await page.goto('/book');
  await expect.poll(() => records().some((r) => r.name === 'page_view')).toBe(true);

  await page.getByRole('button', { name: 'Seasonal offers' }).click();
  await page.getByLabel('Email address for the newsletter').fill('ingrid@example.com');
  await page.getByRole('button', { name: 'Subscribe to the newsletter' }).click();

  const error = await waitForError('TypeError');
  expect(error.request.authorization).toBe(`Bearer ${BROWSER_KEY}`);
  expect(error.attrs['url.path']).toEqual({ stringValue: '/book' });
  expect(error.attrs['exception.stacktrace'].stringValue).toMatch(/\/js\/newsletter\.[0-9a-f]{8}\.js/);
  const crumbs = JSON.parse(error.attrs['browser.breadcrumbs'].stringValue);
  expect(crumbs).toContainEqual(expect.objectContaining({ kind: 'click', detail: 'button "Seasonal offers"' }));
  expect(crumbs).toContainEqual(expect.objectContaining({ kind: 'click', detail: 'button "Subscribe to the newsletter"' }));
  // The typed address is an input value and never leaves the page.
  expect(JSON.stringify(recorded)).not.toContain('ingrid@example.com');

  const all = records();
  const firstView = all.findIndex((r) => r.name === 'page_view');
  expect(firstView).toBeGreaterThanOrEqual(0);
  expect(firstView).toBeLessThan(all.findIndex((r) => r.attrs['exception.type']?.stringValue === 'TypeError'));
  expect(all[firstView].request.authorization).toBe(`Bearer ${BROWSER_KEY}`);
  expect(all[firstView].attrs['url.path']).toEqual({ stringValue: '/book' });
});

test('"Check availability" on the landing page with ?variant=js2 throws a TypeError from its own module', async ({ page }) => {
  await page.goto('/?variant=js2');
  await expect.poll(() => records().some((r) => r.name === 'page_view')).toBe(true);

  await page.getByRole('button', { name: 'Check availability' }).click();

  const error = await waitForError('TypeError');
  expect(error.attrs['exception.stacktrace'].stringValue).toMatch(/\/js\/availability\.[0-9a-f]{8}\.js/);
  const crumbs = JSON.parse(error.attrs['browser.breadcrumbs'].stringValue);
  expect(crumbs).toContainEqual(expect.objectContaining({ kind: 'click', detail: 'button "Check availability"' }));
});

test('the landing page has no availability button without the variant', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('button', { name: 'Check availability' })).toHaveCount(0);
});
