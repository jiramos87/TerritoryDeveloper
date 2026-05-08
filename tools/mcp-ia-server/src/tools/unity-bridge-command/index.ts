/**
 * Barrel: re-exports all public surfaces from unity-bridge-command module folder.
 * Import from "./unity-bridge-command.js" at the tools/ level unchanged.
 */

export { BRIDGE_OUTPUT_PREVIEW_MAX, UNITY_BRIDGE_TIMEOUT_MS_MAX, unityBridgeTimeoutMsSchema } from "./constants.js";
export { unityBridgeCommandInputSchema } from "./input-schema.js";
export type { UnityBridgeCommandInput } from "./input-schema.js";
export type {
  UnityBridgeLogLine,
  PrefabInspectField,
  PrefabInspectComponent,
  PrefabInspectRect,
  PrefabInspectNode,
  UiTreeScreenRect,
  UiTreeNode,
  UiTreeCanvas,
  ConformanceRow,
  ConformanceResult,
  UnityBridgeResponsePayload,
} from "./response-types.js";
export { unityBridgeGetInputSchema } from "./get-schema.js";
export type { UnityBridgeGetInput } from "./get-schema.js";
export { jsonResult, sleepMs, selectBridgeRow, buildRequestEnvelope } from "./envelope.js";
export type { BridgeRow } from "./envelope.js";

export {
  EXPORT_SUGAR_DEFAULT_TIMEOUT_MS,
  resolveExportSugarTimeoutMs,
  enqueueUnityBridgeJob,
  pollUnityBridgeJobUntilTerminal,
  runUnityBridgeCommand,
  runUnityBridgeGet,
} from "./run.js";
export type { UnityBridgeCommandRunOptions } from "./run.js";
export { unityCompileInputSchema, registerUnityBridgeCommand } from "./register.js";
