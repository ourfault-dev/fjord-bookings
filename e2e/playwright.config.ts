import { defineConfig, devices } from '@playwright/test';

export const SITE_PORT = 5180;
export const INGEST_PORT = 5191;
export const BROWSER_KEY = 'pk_0123456789abcdef0123456789abcdef';

// The site runs locally with a browser key and an ingest endpoint that the spec's fake answers.
export default defineConfig({
  testDir: 'tests',
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['github']] : 'list',
  use: { baseURL: `http://127.0.0.1:${SITE_PORT}`, trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: `dotnet run --project ../src/FjordBookings --no-launch-profile --urls http://127.0.0.1:${SITE_PORT}`,
    url: `http://127.0.0.1:${SITE_PORT}/healthz`,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      OURFAULT_BROWSER_KEY: BROWSER_KEY,
      OURFAULT_BROWSER_ENDPOINT: `http://127.0.0.1:${INGEST_PORT}`
    }
  }
});
