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
    // Run only __tests__ unit tests (not playwright e2e)
    include: ['lib/__tests__/**/*.test.ts', 'components/**/__tests__/**/*.test.tsx'],
    environment: 'node',
  },
});
