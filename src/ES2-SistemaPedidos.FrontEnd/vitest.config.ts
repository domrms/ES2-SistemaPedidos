import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'jsdom',
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      reportsDirectory: 'coverage',
      include: ['script.ts'],
      thresholds: {
        statements: 95,
        branches: 80,
        functions: 95,
        lines: 95
      }
    }
  }
});
