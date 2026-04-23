import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { defineConfig } from 'vitest/config';

const root = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  resolve: {
    alias: {
      '@': root,
    },
  },
  test: {
    // Run only __tests__ unit tests + catalog API integration specs (not playwright e2e).
    include: [
      'lib/__tests__/**/*.test.ts',
      'components/**/__tests__/**/*.test.tsx',
      'tests/api/**/*.spec.ts',
    ],
    // Populate DATABASE_URL from repo root .env / postgres-dev.json for DB-backed tests.
    setupFiles: ['tests/api/_vitest-setup.ts'],
    environment: 'node',
    // Force serial file execution: catalog API specs mutate the same Postgres DB
    // (TRUNCATE + seed per test); parallel workers clobber each other. Vitest 4
    // removed `poolOptions` nesting — singleThread/singleFork are top-level.
    fileParallelism: false,
    singleThread: true,
    singleFork: true,
  },
});
