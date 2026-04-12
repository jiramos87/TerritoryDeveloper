# File-backed information architecture with an MCP server (pattern guide)

**Audience:** Teams who want **agents** (or other MCP clients) to query **Markdown** specs, rules, glossaries, and related docs **without** loading entire files into context — using a small **Node.js / TypeScript** MCP server.

**Territory Developer reference implementation:** [`tools/mcp-ia-server/`](../tools/mcp-ia-server/), [`docs/mcp-ia-server.md`](mcp-ia-server.md). This document abstracts the **mechanisms** so you can reuse the pattern in other products or codebases.

---

## 1. Core idea

1. **Humans maintain truth in Git** as `.md` / `.md` (or similar) files.
2. A **registry** lists every document the system knows about (key, path, category, short description).
3. A **parser layer** turns files into structures agents can target: heading trees, line ranges, optional frontmatter, tables.
4. **MCP tools** expose **slices** (sections, rows, routing hints) as JSON — not raw file dumps by default.
5. **Stdio transport** keeps deployment simple for IDE-integrated MCP hosts (no HTTP setup for local use).

This is **information architecture as code**: same files power humans, linters, and agents.

---

## 2. When this pattern fits

| Good fit | Poor fit |
|----------|----------|
| Docs live in-repo; size is roughly **10²–10⁴** lines per corpus | Sub-second search across **10⁶** tokens without index |
| Sections have **headings** (or predictable blocks) | Unstructured blobs (PDF-only, scans) |
| You want **deterministic**, testable behavior | You need semantic search quality only embeddings provide |
| Single writer / small team; Git is source of truth | Strict ACL per paragraph (needs DB + auth) |

You can still **later** sync the same corpus into PostgreSQL or a vector store (**evolution**, not day-one requirement).

---

## 3. Architectural components

### 3.1 Corpus taxonomy (conceptual)

Split files by **role** so tools can stay focused:

| Role | Typical content | Tooling |
|------|-----------------|---------|
| **Long-form spec** | Domain behavior, numbered sections | `spec_outline`, `spec_section` |
| **Short rule / policy** | Guardrails, coding standards, YAML frontmatter | `list_rules`, `rule_content` |
| **Glossary** | Tables: term, definition, pointer to spec (typically one natural language for matching) | `glossary_lookup`, `glossary_discover` — agents pass queries in that language (here: **English**; translate from chat if needed) |
| **Router** | Task domain → where to read | `router_for_task` |
| **Invariants / principles** | Numbered + bulleted constraints | `invariants_summary` |
| **Work tracking** (optional) | Issues, backlog | Dedicated parser + tool (e.g. `backlog_issue`) — usually **excluded** from “spec list” |

Categories are **yours**; the pattern is “one registry entry per logical document.”

### 3.2 Registry (`buildRegistry`)

- Scan fixed directories at server startup (or on demand).
- Emit stable **`key`** strings (filename stem or convention).
- Store **absolute** paths internally; expose **repo-relative** paths to agents when helpful.
- Attach **`description`** from frontmatter or first paragraph.

### 3.3 Parser spine

Almost everything builds on:

1. **Line-accurate source map** — `lineStart` / `lineEnd` for each heading block match **physical file** lines (including frontmatter), so slices are debuggable and stable.
2. **Heading tree** — depth, title, derived **section id** (numeric prefix or slug).
3. **Frontmatter** — for `.md` or docs that use YAML; body scan must align line numbers with the full file.
4. **Table parser** — reusable for glossary, routing tables, any `| Col1 | Col2 |` data.

### 3.4 Tool families (minimal viable set)

| Family | Responsibility | Example tools |
|--------|----------------|---------------|
| **Catalog** | What exists? | `list_specs`, `list_rules` |
| **Navigate** | Table of contents | `spec_outline` |
| **Retrieve** | One slice | `spec_section`, `rule_content`, `glossary_lookup`, `glossary_discover` |
| **Route** | Where to start reading | `router_for_task` |
| **Constraints** | Non-negotiables | `invariants_summary` |
| **External slice** | Outside main registry | Issue tracker file, API schema dir, etc. |

Add tools only when they **reduce average tokens** or **prevent repeated mistakes**.

### 3.5 MCP / SDK layer

- **`@modelcontextprotocol/sdk`**: `McpServer` + `StdioServerTransport`.
- **Zod** `inputSchema` shapes → JSON Schema for the client; validate every `callTool`.
- Return **`JSON.stringify`** inside a `text` content block if that is what your integration expects (common and simple).

---

## 4. Resilience for real LLM clients

1. **Structured errors** — On miss, return `available_keys`, `available_sections`, or `suggestions` so the model can self-correct in one turn.
2. **Aliases** — Map `geo` → `isometric-geography-system` (or your equivalents) in one place.
3. **Fuzzy fallback** — Lightweight edit distance / token collapse on **short strings** (terms, headings); cap results and thresholds; test with real corpus typos.
4. **Parameter aliases** — Models often send wrong JSON keys; optional normalization layer **before** business logic (see Territory `spec_section` lesson).
5. **Truncation** — `max_chars` + `truncated` + `totalChars` on large sections.

---

## 5. Observability and verification

- **Stderr-only diagnostics** — Never `console.log` on stdout (breaks stdio JSON-RPC).
- **Per-tool timing** on stderr helps spot slow parses.
- **Parse cache** (`Map` keyed by absolute path) is enough for small corpora.
- **`npm run verify`**-style script: spawn the same command the IDE uses, `listTools`, assert required names, call representative tools, assert registry size or golden outputs.

---

## 6. Testing strategy

| Layer | Goal |
|-------|------|
| **Parser unit tests** | Fixtures with known headings, tables, edge cases (empty file, frontmatter-only). |
| **Pure ranking / fuzzy** | Deterministic scores, no I/O. |
| **Registry** | Counts or keys when the repo fixture is present (or inject fake registry). |
| **Avoid brittle live-repo asserts** | If tests read real `BACKLOG.md`, prefer flexible checks (regex, partial strings). |

---

## 7. Operations checklist

- [ ] **Working directory** — MCP host may not use repo root; set `REPO_ROOT` (or equivalent) explicitly in `mcp.json` `env`.
- [ ] **Launch command** — `npx -y tsx …` or `node dist/index.js` after `tsc`; document the one you verified.
- [ ] **Version** — Bump server `version` when tools or contracts change.
- [ ] **Docs triad** — `README` (developer), `docs/…` (operator + agents), Cursor rules / `AGENTS.md` (behavior bias).

---

## 8. Evolution paths

1. **New tools** — Glossary keyword discovery, dependency graph, `search_docs` with BM25 — keep contracts backward-compatible when possible.
2. **Database** — Ingest sections into SQL; keep tool **names** stable, swap implementation to query DB (see Territory [`BACKLOG.md`](../BACKLOG.md) for DB-backed IA direction).
3. **Embeddings** — Hybrid: registry + slices for precision, retrieval API for recall.
4. **CI** — Run `npm test` + verify script on pull requests touching specs or server code.

---

## 9. Summary

The pattern is **not** “MCP instead of docs” — it is **MCP as a typed, low-token API** over the same Markdown source of truth. Invest once in **line-accurate parsing** and a **registry**; add tools incrementally; harden with **tests, verify script, aliases, and structured errors**. Territory Developer’s **territory-ia** package (`tools/mcp-ia-server/`) documents one full implementation path; this guide captures the reusable skeleton for any domain.
