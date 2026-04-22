import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // Run only __tests__ unit tests (not playwright e2e)
    include: ['lib/__tests__/**/*.test.ts', 'components/**/__tests__/**/*.test.tsx'],
    environment: 'node',
  },
});
