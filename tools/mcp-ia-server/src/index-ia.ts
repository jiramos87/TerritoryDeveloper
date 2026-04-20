/**
 * Territory IA MCP server — lean IA-core shape (TECH-524 / B1 server split).
 *
 * Registers IA-authoring tools only: specs, rules, glossary, backlog, router,
 * invariants, journal, reserve, materialize, plan-apply surfaces. Excludes
 * Unity-bridge + compute tools (those live in `index-bridge.ts`).
 *
 * Loaded standalone when MCP_SPLIT_SERVERS=1 (IA-authoring sessions).
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { buildRegistry, resolveRepoRoot } from "./config.js";
import { loadRepoDotenvIfNotCi } from "./ia-db/repo-dotenv.js";
import { registerIaCoreTools } from "./server-registrations.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

const server = new McpServer({
  name: "territory-ia",
  version: "0.5.0",
  description:
    "Information Architecture server (IA-core split) — exposes specs, rules, glossary, backlog, router, invariants, journal, reserve, materialize, plan-apply surfaces. Bridge + compute tools live in territory-ia-bridge.",
});

const registry = buildRegistry();
registerIaCoreTools(server, registry);

const transport = new StdioServerTransport();
await server.connect(transport);
