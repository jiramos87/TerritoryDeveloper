/**
 * Zod schema + type for unity_bridge_get.
 */

import { z } from "zod";

const getInputShape = {
  command_id: z.string().uuid().describe("Bridge job id returned by unity_bridge_command or dequeue."),
  wait_ms: z
    .number()
    .int()
    .min(0)
    .max(10_000)
    .default(0)
    .describe(
      "Optional blocking wait: poll every ~150ms until status is completed or failed, or wait_ms elapses (0 = single read).",
    ),
};

/** Exported for tests and IA tooling that mirror MCP `unity_bridge_get` inputSchema. */
export const unityBridgeGetInputSchema = z.object(getInputShape);

export type UnityBridgeGetInput = z.infer<typeof unityBridgeGetInputSchema>;
