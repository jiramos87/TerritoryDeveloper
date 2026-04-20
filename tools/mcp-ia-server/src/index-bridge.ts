/**
 * Territory IA MCP server — Unity-bridge + compute shape (TECH-524 / B1 server split).
 *
 * Registers Unity-bridge + compute tools only. Paired with `index-ia.ts`
 * IA-core server behind MCP_SPLIT_SERVERS flag. Opt-in for verify/implement
 * seams that actually touch Unity Editor or compute helpers.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { resolveRepoRoot } from "./config.js";
import { loadRepoDotenvIfNotCi } from "./ia-db/repo-dotenv.js";
import { registerBridgeTools } from "./server-registrations.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

const server = new McpServer({
  name: "territory-ia-bridge",
  version: "0.5.0",
  description:
    "Territory Unity-bridge + compute server — Unity Editor bridge commands, compute helpers (geography, grid, pathfinding, desirability, growth ring), findobjectoftype scan, city metrics history, Unity callers/subscribers analysis. Paired with territory-ia IA-core server.",
});

registerBridgeTools(server);

const transport = new StdioServerTransport();
await server.connect(transport);
