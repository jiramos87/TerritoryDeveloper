/**
 * MCP text content envelope for JSON tool payloads (stdio-safe).
 */

export function jsonToolResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}
