/**
 * Smoke test: spawn the IA MCP server the same way Cursor does and call tools via the SDK client.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
// scripts/ → mcp-ia-server/ → tools/ → repository root
const repoRoot = path.resolve(__dirname, "../../..");

function parseJsonFromToolResult(result: {
  content?: Array<{ type: string; text?: string }>;
}): unknown {
  const block = result.content?.find((c) => c.type === "text");
  if (!block?.text) throw new Error("No text content in tool result");
  return JSON.parse(block.text);
}

async function main(): Promise<void> {
  const transport = new StdioClientTransport({
    command: "npx",
    args: ["-y", "tsx", "tools/mcp-ia-server/src/index.ts"],
    cwd: repoRoot,
    env: {
      ...process.env,
      REPO_ROOT: repoRoot,
    },
    stderr: "inherit",
  });

  const client = new Client(
    { name: "territory-ia-verify", version: "0.0.1" },
    { capabilities: {} },
  );

  await client.connect(transport);

  const server = client.getServerVersion();
  console.log("Connected. Server:", server?.name, server?.version);

  const { tools } = await client.listTools();
  const names = tools.map((t) => t.name).sort();
  console.log("Tools:", names.join(", "));
  if (!names.includes("list_specs") || !names.includes("spec_outline")) {
    throw new Error("Expected list_specs and spec_outline");
  }

  const all = parseJsonFromToolResult(
    await client.callTool({ name: "list_specs", arguments: {} }),
  ) as Array<{ key: string; category: string }>;
  console.log("list_specs count:", all.length);
  if (all.length !== 19) {
    throw new Error(`Expected 19 IA documents, got ${all.length}`);
  }

  const rules = parseJsonFromToolResult(
    await client.callTool({
      name: "list_specs",
      arguments: { category: "rule" },
    }),
  ) as Array<{ category: string }>;
  if (rules.length !== 9 || rules.some((r) => r.category !== "rule")) {
    throw new Error("list_specs category=rule filter failed");
  }

  const outline = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_outline",
      arguments: { spec: "isometric-geography-system" },
    }),
  ) as { outline: unknown[]; error?: string };
  if (outline.error || !Array.isArray(outline.outline)) {
    throw new Error("spec_outline isometric-geography-system failed");
  }
  console.log("spec_outline isometric-geography-system top-level headings:", outline.outline.length);

  const inv = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_outline",
      arguments: { spec: "invariants" },
    }),
  ) as { frontmatter: Record<string, unknown> | null; outline: unknown[] };
  if (inv.frontmatter === null) {
    throw new Error("Expected invariants.mdc to have frontmatter");
  }

  const bad = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_outline",
      arguments: { spec: "nonexistent-xyz" },
    }),
  ) as { error?: string; available_keys?: string[] };
  if (bad.error !== "unknown_spec" || !Array.isArray(bad.available_keys)) {
    throw new Error("Expected unknown_spec error with available_keys");
  }

  await transport.close();
  console.log("OK — MCP server and tools verified (stdio, same launch as .cursor/mcp.json).");
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
