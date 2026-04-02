/**
 * Territory IA MCP server — stdio transport, specs/rules/root docs tools.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { buildRegistry } from "./config.js";
import { registerListSpecs } from "./tools/list-specs.js";
import { registerSpecOutline } from "./tools/spec-outline.js";

const server = new McpServer({
  name: "territory-ia",
  version: "0.1.0",
  description:
    "Information Architecture server for Territory Developer — exposes specs, rules, glossary, and architecture docs via MCP tools.",
});

const registry = buildRegistry();

registerListSpecs(server, registry);
registerSpecOutline(server, registry);

const transport = new StdioServerTransport();
await server.connect(transport);
