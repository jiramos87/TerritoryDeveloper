# TECH-17a — MCP Server Bootstrap + Markdown Parser + First Two Tools

> **Issue:** [TECH-17](../../BACKLOG.md)
> **Status:** Final (historical — implementation shipped; see **§11** and [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) for current truth)
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02 (§9–11 retrospective)
> **Sequence:** Part 1 of 3 (TECH-17a → TECH-17b → TECH-17c)

## 1. Summary

Stand up the MCP server project scaffold under `tools/mcp-ia-server/`, implement the core Markdown/MDC parser module, and ship the first two tools (`list_specs`, `spec_outline`) so Cursor can discover and connect to the server. This phase proves the end-to-end pipeline: Cursor launches the server via stdio, an agent calls a tool, the server reads a `.md`/`.mdc` file from disk, parses it, and returns structured data.

### 1.1 Implementer guidance (AI agents)

When **implementing** this project (not when using the finished MCP at runtime):

1. **Domain vocabulary:** If an example, smoke test, or spec excerpt mentions a game concept (e.g. wet run, stroke, shore band, deck span), treat **`.cursor/specs/glossary.md`** as the authoritative definition. **Do not** substitute meanings from general knowledge or other games.
2. **Deeper behavior:** Use the glossary’s **Spec** column to open the cited sections in the linked Markdown specs when you need full rules, not a paraphrase.
3. **Repo routing:** `AGENTS.md` and `.cursor/rules/agent-router.mdc` already send “domain terms” tasks to the glossary — align validation and manual checks with that hierarchy.

This reduces implementation drift and mistaken assumptions about terminology while you wire parsers and tools to real files.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Working TypeScript MCP server launchable from Cursor via stdio transport.
2. Reusable Markdown parser module for both `.md` and `.mdc`: frontmatter extraction, heading-tree construction, and per-heading **line ranges** (`lineStart` / `lineEnd` in the **full file**, including frontmatter lines). **Slice-by-section content extraction** (`extractSection` / `spec_section`) is implemented in **TECH-17b** on top of this tree.
3. Two registered tools: `list_specs` and `spec_outline`.
4. `.cursor/mcp.json` configuration so Cursor auto-discovers the server.
5. Minimal README for maintainers explaining how to run, develop, and debug.

### 2.2 Non-Goals (Out of Scope)

1. Tools beyond `list_specs` and `spec_outline` — those are TECH-17b.
2. **`spec_section` and `max_chars` truncation** — TECH-17b (semantic tools).
3. **Fuzzy matching, spec-key aliases, in-memory parse cache, and richer error UX** — TECH-17c.
4. PostgreSQL, database schema, or any persistence — those are TECH-19.
5. ~~Tests beyond manual smoke-testing — unit tests are TECH-17c.~~ **Superseded:** TECH-17c added `node:test` + `c8` + `npm run verify`.
6. Publishing as an npm package.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | I want to list all available spec files so I know what documentation exists without scanning the filesystem | `list_specs` returns names, paths, and descriptions for all `.cursor/specs/*.md` files plus `AGENTS.md`, `ARCHITECTURE.md`, and `.cursor/rules/*.mdc` |
| 2 | AI agent | I want to see the outline (heading tree) of a spec so I can decide which section to request | `spec_outline` returns a structured list of headings with depth, title, and line range for any known file |
| 3 | Developer | I want to start the MCP server locally with one command for development | `npm run dev` starts the server, `npm run build` produces JS output |
| 4 | Developer | I want Cursor to discover the server automatically when I open the project | `.cursor/mcp.json` is configured and Cursor shows the server in Tools & MCP |

## 4. Current State

### 4.1 Relevant Files and Systems

| File / Path | Role in this context |
|-------------|----------------------|
| `.cursor/specs/*.md` (8 files, ~1480 lines total) | Spec documents the MCP will serve |
| `.cursor/rules/*.mdc` (11 files after TECH-17 close; count may change) | Rule documents with YAML frontmatter |
| `AGENTS.md` (106 lines) | Agent workflow guide |
| `ARCHITECTURE.md` (148 lines) | System layers and dependency map |
| **IA registry size (at TECH-17 close)** | **21** `list_specs` entries: 8 `.cursor/specs/*.md` + 11 `.cursor/rules/*.mdc` + `AGENTS.md` + `ARCHITECTURE.md` (grows when new rules/specs are added; `verify-mcp.ts` asserts the live count) |
| `.cursor/specs/glossary.md` (183 lines) | Domain glossary (table format) |
| `.cursor/mcp.json` | Does not exist yet — will be created |
| `tools/` | Does not exist yet — will be created |

### 4.2 Document Structure Patterns

**`.md` files** use standard Markdown headings (`##`, `###`, `####`). Some use `§` numbering in heading text (e.g. `## 13. Roads: manual draw, interstate, bridges, shared validation`). The glossary uses `## Category` headings with Markdown tables beneath.

**`.mdc` files** have YAML frontmatter between `---` delimiters with fields: `description`, `alwaysApply`, `globs` (optional). Body is standard Markdown.

## 5. Proposed Design

### 5.1 Repository Layout

```
tools/
  mcp-ia-server/
    package.json
    tsconfig.json
    README.md
    src/
      index.ts              # Entry point: McpServer + StdioServerTransport
      config.ts             # Paths, file registry, constants
      parser/
        markdown-parser.ts  # Core parser: frontmatter, heading tree, line ranges (content slicing APIs in TECH-17b)
        types.ts            # Shared types: HeadingNode, ParsedDocument, etc.
      tools/
        list-specs.ts       # list_specs tool registration
        spec-outline.ts     # spec_outline tool registration
```

### 5.2 Dependencies

| Package | Purpose | Version constraint |
|---------|---------|-------------------|
| `@modelcontextprotocol/sdk` | MCP server + client (`McpServer`, `StdioServerTransport`) | ^1.29+ (verify at upgrade time) |
| `zod` | Schema validation for tool inputs (MCP SDK peer dep) | v4+ |
| `gray-matter` | YAML frontmatter extraction from `.mdc` files | latest |
| `typescript` | Build | ^5.x |
| `tsx` | Dev runner (no build step needed for dev) | latest |

No runtime dependencies beyond MCP SDK, Zod, and gray-matter. Node.js built-in `fs` and `path` for file access.

**SDK package name (verify at scaffold time):** The official npm scope and entrypoints have changed between MCP SDK generations. Before locking `package.json`, confirm on [npm](https://www.npmjs.com/) and the current [TypeScript SDK docs](https://modelcontextprotocol.io/) which package exports `McpServer` / `StdioServerTransport` (or the equivalent for the chosen major version). Adjust imports and `registerTool` signatures to match that version.

### 5.3 Cursor Configuration

**`.cursor/mcp.json`** (repo root):

```json
{
  "mcpServers": {
    "territory-ia": {
      "command": "npx",
      "args": ["tsx", "tools/mcp-ia-server/src/index.ts"],
      "env": {
        "REPO_ROOT": "."
      }
    }
  }
}
```

The `REPO_ROOT` env var lets the server resolve paths relative to the workspace. If not set, the server defaults to `process.cwd()`.

**Working directory:** Cursor typically launches MCP with **workspace root** as CWD, so paths like `tools/mcp-ia-server/src/index.ts` resolve correctly. If a host uses a different CWD, either set `REPO_ROOT` to the absolute repo path or change `args` to run `npm run dev` with `"cwd": "tools/mcp-ia-server"` (if the host supports it) and a script that starts `src/index.ts`. Document the **verified** command in `README.md` after first successful connect.

**`tsx` availability:** Prefer `npx -y tsx` if `tsx` is not installed globally, or depend on `tsx` in `devDependencies` and invoke it via `npm exec tsx` / a `package.json` script from the tool directory.

### 5.4 Core Parser Module (`markdown-parser.ts`)

#### Types (`types.ts`)

```typescript
interface HeadingNode {
  depth: number;         // 1-6 (## = 2, ### = 3, etc.)
  title: string;         // Raw heading text, e.g. "13.4 Bridges and water approach"
  sectionId: string;     // Normalized: "13.4" if present, else slugified title
  lineStart: number;     // 1-based line where heading appears
  lineEnd: number;       // 1-based last line before next same-or-higher heading (or EOF)
  children: HeadingNode[];
}

interface ParsedDocument {
  filePath: string;
  fileName: string;
  frontmatter: Record<string, unknown> | null;  // null for .md without frontmatter
  headings: HeadingNode[];                       // Top-level heading tree
  lineCount: number;
}

interface SpecRegistryEntry {
  key: string;           // Short key: "glossary", "roads-system", "invariants", etc.
  fileName: string;      // "glossary.md", "invariants.mdc"
  filePath: string;      // Absolute path on disk
  description: string;   // From frontmatter or first line of file
  category: "spec" | "rule" | "root-doc";
}
```

#### Parsing Algorithm

1. **Read file** as UTF-8 string, split into lines.
2. **Frontmatter extraction** (`.mdc` files or any file starting with `---`): use `gray-matter` to separate frontmatter and body. Store `frontmatter` object.
3. **Heading scan**: iterate lines, match `^(#{1,6})\s+(.+)$`. For each match, record `depth`, raw `title`, `lineStart`.
4. **Section ID extraction**: from heading title, extract leading `§` or numeric prefix (regex: `/^(?:§?\s*)?(\d+(?:\.\d+)*)/`). If no numeric prefix, generate a slug from the title (`toLowerCase`, replace non-alphanumeric with `-`, collapse).
5. **Line ranges**: each heading's `lineEnd` is the line before the next heading of same or shallower depth, or EOF. Compute in a single reverse pass.
6. **Tree construction**: build parent-child hierarchy from depth. A heading at depth N is a child of the nearest preceding heading at depth N-1.

**Line numbering (critical for TECH-17b):** `lineStart`, `lineEnd`, and `lineCount` are always **1-based indices into the physical file on disk** (first line of the file = 1), **including** YAML frontmatter lines on `.mdc`. Heading detection runs on the full file after separating frontmatter for metadata only — implement by scanning the **original** line array, or by offsetting body-only line numbers to absolute file lines. `extractLines` in TECH-17b must read the same file and use these absolute line numbers.

#### File Registry (`config.ts`)

The server maintains a static registry of known IA files, built at startup by scanning:
- `.cursor/specs/*.md`
- `.cursor/rules/*.mdc`
- `AGENTS.md`
- `ARCHITECTURE.md`

Each entry gets a `key` derived from filename without extension (e.g. `isometric-geography-system`, `agent-router`, `invariants`). The `description` comes from:
- `.mdc`: `description` field in frontmatter.
- `.md`: first non-empty, non-heading line; or the first `>` blockquote line.

The registry is built once at server start and cached in memory. Files are re-read on each tool call (no file-watching complexity in this phase).

### 5.5 MCP tool responses

Tool handlers return **JSON-serializable objects**. The MCP SDK may require wrapping them as text (e.g. `JSON.stringify` into a `text` content block) or using structured content, depending on the SDK version. Implement each `registerTool` callback per the chosen SDK’s documented return type so Cursor/agents receive parseable JSON.

### 5.6 Tool Contracts

#### `list_specs`

| Property | Value |
|----------|-------|
| **Name** | `list_specs` |
| **Description** | List all Information Architecture documents (specs, rules, root docs) available for querying. |
| **Input schema** | `z.object({ category: z.enum(["spec", "rule", "root-doc", "all"]).optional().describe("Filter by category. Defaults to 'all'.") })` |
| **Output** | JSON array of `{ key, fileName, relativePath, description, category, lineCount }` — `relativePath` is repo-relative (e.g. `.cursor/specs/glossary.md`, `AGENTS.md`) |
| **Errors** | None expected — always returns the registry (possibly empty). |

**Example call:**
```
list_specs({ category: "spec" })
```

**Example response:**
```json
[
  { "key": "isometric-geography-system", "fileName": "isometric-geography-system.md", "relativePath": ".cursor/specs/isometric-geography-system.md", "description": "Canonical spec for isometric geography (single source of truth).", "category": "spec", "lineCount": 772 },
  { "key": "glossary", "fileName": "glossary.md", "relativePath": ".cursor/specs/glossary.md", "description": "Quick-reference for domain concepts.", "category": "spec", "lineCount": 183 }
]
```

#### `spec_outline`

| Property | Value |
|----------|-------|
| **Name** | `spec_outline` |
| **Description** | Get the heading outline (table of contents) for a spec, rule, or root document. |
| **Input schema** | `z.object({ spec: z.string().describe("Key or filename of the document (e.g. 'glossary', 'roads-system', 'invariants', 'AGENTS.md').") })` |
| **Output** | `{ key, fileName, description, frontmatter, outline: HeadingNode[] }` where `HeadingNode` includes `depth`, `title`, `sectionId`, `lineStart`, `lineEnd`, `children` (recursive). |
| **Errors** | If `spec` key not found: return `{ error: "unknown_spec", message: "No document found for key '...'. Use list_specs to see available documents.", available_keys: [...] }` |

**Example call:**
```
spec_outline({ spec: "roads-system" })
```

**Example response (abbreviated):**
```json
{
  "key": "roads-system",
  "fileName": "roads-system.md",
  "description": "Deep reference for road placement, pathfinding, bridge validation, and prefab resolution.",
  "frontmatter": null,
  "outline": [
    {
      "depth": 2,
      "title": "Shared validation surface (geography spec §13.1)",
      "sectionId": "shared-validation-surface",
      "lineStart": 6,
      "lineEnd": 23,
      "children": [
        {
          "depth": 3,
          "title": "Two ways to build the plan",
          "sectionId": "two-ways-to-build-the-plan",
          "lineStart": 10,
          "lineEnd": 16,
          "children": []
        }
      ]
    }
  ]
}
```

### 5.7 Entry Point (`index.ts`)

```typescript
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { registerListSpecs } from "./tools/list-specs.js";
import { registerSpecOutline } from "./tools/spec-outline.js";
import { buildRegistry } from "./config.js";

const server = new McpServer({
  name: "territory-ia",
  version: "0.1.0",
  description: "Information Architecture server for Territory Developer — exposes specs, rules, glossary, and architecture docs via MCP tools.",
});

const registry = buildRegistry();

registerListSpecs(server, registry);
registerSpecOutline(server, registry);

const transport = new StdioServerTransport();
await server.connect(transport);
```

Each tool module exports a `register*` function that receives the `McpServer` instance and the registry, calls `server.registerTool(...)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | TypeScript + MCP SDK (not Python) | Team has more TS experience; SDK API is mature; no new runtime dependency for a Unity project | Python MCP SDK |
| 2026-04-02 | `tools/mcp-ia-server/` location | Keeps tooling separate from Unity `Assets/`; clean `package.json` isolation | Repo root (pollutes root), `.cursor/mcp-server/` (hidden in dotfile) |
| 2026-04-02 | stdio transport (not HTTP) | Cursor's default local transport; zero networking config; lower latency | HTTP + SSE (overkill for single-user local) |
| 2026-04-02 | `gray-matter` for frontmatter | Battle-tested, handles edge cases (multi-line, nested YAML) | Manual regex (fragile) |
| 2026-04-02 | Static file registry, re-read on each call | Simple, correct for a small file set (~15 files); avoids file-watcher complexity | Chokidar watcher (premature for phase 1) |
| 2026-04-02 | `tsx` for dev, `tsc` for build | `tsx` gives instant startup for dev; `tsc` output for production/CI | `ts-node` (slower startup), `esbuild` (complexity) |
| 2026-04-02 | `list_specs` includes `relativePath` | Matches user story (“paths”) and helps agents locate files without guessing repo layout | Omit path (story vs contract drift) |
| 2026-04-02 | Verify MCP npm package at scaffold | Package names and APIs shift between SDK majors; spec cannot hard-code one import path forever | Shipped on `@modelcontextprotocol/sdk` (see §5.2) |
| 2026-04-02 | `REPO_ROOT` + workspace CWD | Hosts that start MCP with a non-repo CWD break relative paths | Documented in README; verify script sets `REPO_ROOT` explicitly |

## 7. Implementation Plan

*All phases completed as part of TECH-17 rollout.*

### Phase 1 — Project scaffold

- [x] Create `tools/mcp-ia-server/` directory.
- [x] Initialize `package.json` with `name: "@territory/mcp-ia-server"`, `type: "module"`, scripts: `dev`, `build`, `start`.
- [x] Create `tsconfig.json` targeting ES2022, `moduleResolution: "NodeNext"`, `outDir: "dist"`.
- [x] Install dependencies: `@modelcontextprotocol/sdk`, `zod`, `gray-matter`.
- [x] Install dev dependencies: `typescript`, `tsx`, `@types/node`.
- [x] Create `src/index.ts` with `McpServer` + `StdioServerTransport` boilerplate.
- [x] Verify server starts (see `npm run dev` / Cursor MCP).

### Phase 2 — Parser module

- [x] `src/parser/types.ts`, `markdown-parser.ts` (incl. line ranges aligned with **physical file** lines, frontmatter-aware body scan).
- [x] `src/config.ts`: `buildRegistry`, `resolveRepoRoot`, registry helpers.

### Phase 3 — Tools + Cursor config

- [x] `list_specs`, `spec_outline`, wiring in `index.ts`.
- [x] `.cursor/mcp.json` (typically `npx -y tsx tools/mcp-ia-server/src/index.ts` from repo root).
- [x] `README.md`, `.gitignore` entries for `node_modules/` / `dist/`.

### Phase 4 — Smoke test

- [x] Cursor lists **territory-ia**; `list_specs` / `spec_outline` validated (registry size later rose to **21** with additional `.mdc` rules).

## 8. Acceptance Criteria

- [x] Server starts locally; Cursor discovers MCP.
- [x] `list_specs` returns full IA registry with `relativePath` and categories.
- [x] `spec_outline` matches on-disk heading structure for large specs and `.mdc` frontmatter.
- [x] Unknown keys return structured errors with `available_keys`.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | SDK import path / package name drift | Upstream renamed scope to `@modelcontextprotocol/sdk` | Use subpath imports (`server/mcp.js`, `server/stdio.js`) |
| 2 | Line numbers must include frontmatter | Body-only line indices break `extractSection` | Scan headings against full file lines with frontmatter offset (see TECH-17b) |
| 3 | Stdio vs stdout | Logging to stdout corrupts MCP JSON | Log timing/diagnostics to **stderr** only |
| 4 | Registry count tests | New rules/specs change `list_specs` length | `verify-mcp.ts` encodes expected count; bump when IA grows |

## 10. Lessons Learned

- **Filesystem IA scales well** for ~2k–10k lines: no DB required for v1; latency stays low with parse cache (TECH-17c).
- **Heading tree + absolute line ranges** are the spine for every “slice” tool; invest once in the parser, reuse everywhere.
- **Zod raw shapes** for `registerTool` integrate cleanly with the SDK’s JSON Schema generation; prefer stable field names and add **aliases** later if models mis-key arguments (see TECH-17c / `spec_section`).
- **REPO_ROOT** is cheap insurance when the IDE’s MCP CWD differs from the git root.

## 11. Post-ship outcomes (consolidated)

- **Delivered:** Scaffold, parser foundation, first two tools, Cursor config — all superseded in behavior by the living server; this document remains a **design archaeology** reference.
- **Follow-on work:** TECH-17b added semantic tools; TECH-17c added fuzzy matching, rule tools, tests, `npm run verify`, and follow-ups (`backlog_issue`, argument aliases). Canonical operator docs: [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md).
- **Generic pattern:** See [`docs/mcp-markdown-ia-pattern.md`](../../docs/mcp-markdown-ia-pattern.md) for a domain-agnostic description of the same architecture.
