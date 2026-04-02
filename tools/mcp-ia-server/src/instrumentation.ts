/**
 * Per-tool duration logging on stderr (safe for MCP stdio transport).
 */

/**
 * Run an async tool handler and log elapsed milliseconds to stderr.
 */
export async function runWithToolTiming<T>(
  toolName: string,
  fn: () => Promise<T>,
): Promise<T> {
  const t0 = performance.now();
  try {
    return await fn();
  } finally {
    const ms = performance.now() - t0;
    console.error(`[territory-ia] ${toolName} ${ms.toFixed(1)}ms`);
  }
}
