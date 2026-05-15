/**
 * Exponential backoff helper for verify-loop transient gap_reason retries.
 *
 * delay_ms(attempt) = min(base * 2^attempt, max_ms)
 * base = 500 ms, max_ms = 8000 ms
 *
 * Usage:
 *   import { delayMs } from "./exponential-backoff.mjs";
 *   await new Promise(r => setTimeout(r, delayMs(attempt)));
 */

const BASE_MS = 500;
const MAX_MS = 8000;

/**
 * Returns backoff delay in milliseconds for a given retry attempt (0-indexed).
 * @param {number} attempt - Zero-indexed retry attempt number.
 * @param {number} [base] - Base delay in ms (default 500).
 * @param {number} [maxMs] - Max delay cap in ms (default 8000).
 * @returns {number} Delay in milliseconds.
 */
export function delayMs(attempt, base = BASE_MS, maxMs = MAX_MS) {
  return Math.min(base * Math.pow(2, attempt), maxMs);
}

export default delayMs;
