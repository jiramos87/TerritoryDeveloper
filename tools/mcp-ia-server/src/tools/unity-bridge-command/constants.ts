/**
 * Shared constants + timeout schema for unity_bridge_command / unity_compile.
 */

import { z } from "zod";

/** Max chars for last_output_preview in bridge_timeout details. */
export const BRIDGE_OUTPUT_PREVIEW_MAX = 512;

/** Upper bound for `timeout_ms` on `unity_bridge_command` / `unity_compile`. Agents use 40s initial + escalation protocol (see docs/agent-led-verification-policy.md). */
export const UNITY_BRIDGE_TIMEOUT_MS_MAX = 120_000;

/** Exported for `unity_compile` and unit tests. */
export const unityBridgeTimeoutMsSchema = z
  .number()
  .int()
  .min(1000)
  .max(UNITY_BRIDGE_TIMEOUT_MS_MAX)
  .default(30_000)
  .describe(
    "Max time to wait for Unity to dequeue, run the command, and complete the job row (requires Postgres + Unity on REPO_ROOT). Capped at 120s; default 30s. Agents: use 40s initial, then escalation protocol (npm run unity:ensure-editor + retry 60s). Deferred ScreenCapture completes within ~15s on the Unity side when healthy.",
  );
