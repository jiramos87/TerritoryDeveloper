/**
 * Re-export barrel: unity-bridge-command.ts → unity-bridge-command/ module folder.
 * All import sites (server-registrations.ts, scripts/, tests/) remain unchanged.
 */

export {
  BRIDGE_OUTPUT_PREVIEW_MAX,
  UNITY_BRIDGE_TIMEOUT_MS_MAX,
  unityBridgeTimeoutMsSchema,
  unityBridgeCommandInputSchema,
  unityBridgeGetInputSchema,
  jsonResult,
  sleepMs,
  selectBridgeRow,
  buildRequestEnvelope,
  EXPORT_SUGAR_DEFAULT_TIMEOUT_MS,
  resolveExportSugarTimeoutMs,
  enqueueUnityBridgeJob,
  pollUnityBridgeJobUntilTerminal,
  runUnityBridgeCommand,
  runUnityBridgeGet,
  unityCompileInputSchema,
  registerUnityBridgeCommand,
} from "./unity-bridge-command/index.js";

export type {
  UnityBridgeCommandInput,
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
  UnityBridgeGetInput,
  BridgeRow,
  UnityBridgeCommandRunOptions,
} from "./unity-bridge-command/index.js";
