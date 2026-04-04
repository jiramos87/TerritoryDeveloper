/**
 * Territory IA MCP server — stdio transport, specs/rules/root docs tools.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { buildRegistry } from "./config.js";
import { registerListSpecs } from "./tools/list-specs.js";
import { registerSpecOutline } from "./tools/spec-outline.js";
import { registerSpecSection } from "./tools/spec-section.js";
import { registerGlossaryLookup } from "./tools/glossary-lookup.js";
import { registerGlossaryDiscover } from "./tools/glossary-discover.js";
import { registerRouterForTask } from "./tools/router-for-task.js";
import { registerInvariantsSummary } from "./tools/invariants-summary.js";
import { registerListRules } from "./tools/list-rules.js";
import { registerRuleContent } from "./tools/rule-content.js";
import { registerBacklogIssue } from "./tools/backlog-issue.js";
import { registerSpecSections } from "./tools/spec-sections.js";
import { registerProjectSpecCloseoutDigest } from "./tools/project-spec-closeout-digest.js";

const server = new McpServer({
  name: "territory-ia",
  version: "0.4.3",
  description:
    "Information Architecture server for Territory Developer — exposes specs, rules, glossary, backlog issues, and architecture docs via MCP tools.",
});

const registry = buildRegistry();

registerListSpecs(server, registry);
registerSpecOutline(server, registry);
registerSpecSection(server, registry);
registerGlossaryLookup(server, registry);
registerGlossaryDiscover(server, registry);
registerRouterForTask(server, registry);
registerInvariantsSummary(server, registry);
registerListRules(server, registry);
registerRuleContent(server, registry);
registerBacklogIssue(server);
registerSpecSections(server, registry);
registerProjectSpecCloseoutDigest(server);

const transport = new StdioServerTransport();
await server.connect(transport);
