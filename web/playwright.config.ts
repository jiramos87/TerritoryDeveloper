import { defineConfig, devices } from '@playwright/test';

// TECH-8609 / Stage 19.1 — `webServer` block self-boots `next dev` so the
// critical-path smoke does not require an externally-running server. Existing
// dashboard-filters/routes/plans-sections/meta specs continue to work since
// `reuseExistingServer: true` lets a manually-started dev session take
// precedence in dev workflows.
export default defineConfig({
  testDir: './tests',
  globalSetup: './tests/_fixtures/playwright-global-setup.ts',
  // Restrict Playwright discovery to top-level `tests/*.spec.ts` Playwright
  // files; exclude `tests/api/**/*.spec.ts` (Vitest integration suite —
  // TECH-8608) and `tests/db/**`, `tests/perf/**` (Vitest DB tests).
  testIgnore: ['**/api/**', '**/db/**', '**/perf/**', '**/_fixtures/**'],
  outputDir: './playwright-report',
  workers: process.env.CI ? 1 : undefined,
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:4000',
  },
  webServer: {
    command: 'npm run dev',
    port: 4000,
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
    env: {
      NEXT_PUBLIC_AUTH_DEV_FALLBACK: '1',
    },
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
