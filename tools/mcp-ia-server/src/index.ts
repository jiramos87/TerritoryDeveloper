/**
 * Territory IA MCP server — stdio transport.
 *
 * Default entry (backward compat): registers BOTH IA-core + bridge tools on a
 * single `territory-ia` server. When `MCP_SPLIT_SERVERS=1`, loads the IA-core
 * standalone path (bridge tools hidden); paired `territory-ia-bridge` server
 * must be declared separately in `.mcp.json` for opt-in consumption.
 *
 * TECH-524 / B1 server split — shared registration helpers live in
 * `./server-registrations.ts`.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { buildRegistry, resolveRepoRoot } from "./config.js";
import { loadRepoDotenvIfNotCi } from "./ia-db/repo-dotenv.js";
import { registerBridgeTools, registerIaCoreTools } from "./server-registrations.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

const splitMode = process.env.MCP_SPLIT_SERVERS === "1";

const server = new McpServer({
  name: "territory-ia",
  version: "0.5.0",
  description: splitMode
    ? "Information Architecture server (IA-core split) — specs, rules, glossary, backlog, router, invariants, journal, reserve, materialize, plan-apply surfaces. Bridge + compute tools hidden; territory-ia-bridge server handles those."
    : "Information Architecture server for Territory Developer — exposes specs, rules, glossary, backlog issues, architecture docs, optional Postgres project-spec journal, city metrics history queries, computational helpers, and Unity Editor bridge commands (Postgres agent_bridge_job) via MCP tools.",
});

const registry = buildRegistry();

registerIaCoreTools(server, registry);
if (!splitMode) {
  registerBridgeTools(server);
}

const transport = new StdioServerTransport();
await server.connect(transport);
