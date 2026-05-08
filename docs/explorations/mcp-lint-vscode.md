---
slug: mcp-lint-vscode
target_version: 1
parent_plan_id: null
notes: "Repo-agnostic VS Code/Cursor extension that turns any MCP server into a live diagnostics provider. Tools that emit a standard {findings[]} contract surface as squiggles + Problems panel + quick-fixes. Pluggable via per-repo .mcp-lint.json (server command, tool allowlist, glob scope, debounce, severity remap). Mixes deterministic checks (AST/regex/grammar) with optional LLM-backed tools under a uniform shape. Drop-in alongside ESLint/Roslyn — no replacement, additive layer."
stages: []
tasks:
  - prefix: TECH
    depends_on: []
    digest_outline: "Placeholder — stages not yet authored."
    touched_paths: []
    kind: code
---

# MCP-Lint for VS Code / Cursor — exploration

Caveman-tech default per `ia/rules/agent-output-caveman.md`.

---

## 1. Problem

Editor diagnostics today bind 1:1 to a language server or framework linter (ESLint, Roslyn, Pyright). Project-specific rules — invariant violations, glossary drift, intent/code mismatch, plan-digest lint, anchor drift — live in MCP tools or Node validators. Agents see them; developers in the editor do not. Result: humans ship code that the agent later flags, round-trip cost on every iteration.

Goal: any MCP tool that produces structured findings shows up as live squiggles in VS Code / Cursor without per-rule extension code. Repo drops a config file, the extension auto-discovers tools, diagnostics light up.

Cursor inherits the VS Code extension API, so the same package serves both editors.

---

## 2. Scope

- In: VS Code extension (TS, packaged via `vsce`); MCP stdio client; diagnostic mapper; quick-fix bridge; per-repo config; status bar; manual command palette entries.
- Out: language server protocol implementation; remote MCP transport (HTTP/SSE) — stdio only for v1; multi-root workspace edge cases.
- Non-goals: replacing ESLint/Roslyn; running LLMs in-extension; bundling MCP server binaries.

---

## 3. Tool contract

Every MCP tool that participates returns:

```json
{
  "findings": [
    {
      "file": "relative/path/from/repo/root.ts",
      "range": { "startLine": 12, "startCol": 4, "endLine": 12, "endCol": 24 },
      "severity": "error" | "warning" | "info" | "hint",
      "code": "rule-slug",
      "message": "Human-readable explanation.",
      "fixes": [
        {
          "title": "Apply fix description",
          "edits": [
            { "file": "...", "range": { ... }, "newText": "..." }
          ]
        }
      ]
    }
  ]
}
```

`fixes` optional. `range` 0-based. Tools without ranges emit file-level findings (line 0, col 0).

Tools opt in via naming convention (`lint:*`, `check:*`, `validate:*`) **or** via MCP tool tag `mcp-lint:participant` (preferred — explicit beats convention).

---

## 4. Repo-level config

`.mcp-lint.json` at repo root:

```json
{
  "servers": [
    {
      "id": "primary",
      "command": "node",
      "args": ["tools/mcp-server/dist/index.js"],
      "env": { "DATABASE_URL": "${env:DATABASE_URL}" }
    }
  ],
  "tools": {
    "include": ["lint:*", "validate:invariants"],
    "exclude": ["lint:expensive-llm-check"]
  },
  "scope": {
    "globs": ["src/**", "ia/**", "Assets/Scripts/**"],
    "excludeGlobs": ["**/*.generated.*", "**/dist/**"]
  },
  "triggers": {
    "onSave": ["lint:*"],
    "onChange": ["lint:fast"],
    "manual": ["lint:expensive-llm-check"]
  },
  "debounceMs": 400,
  "severityRemap": { "warning": "info" },
  "cost": { "cheap": "onChange", "medium": "onSave", "llm": "manual" }
}
```

Pluggable: drop file, restart extension, lint runs. Zero extension changes.

---

## 5. Detection layers (cost-tiered)

| Tier | Cost | Examples | Trigger |
|---|---|---|---|
| Deterministic | cheap | AST scan, regex, grammar, file presence | onChange (debounced) |
| Semantic lookup | medium | glossary cross-ref, invariant index, schema diff | onSave |
| LLM-backed | expensive | intent/code drift, prose clarity, suggested rewrites | manual / onSave optional |

Tools self-declare `cost` field in their MCP descriptor. Extension routes triggers per tier from `.mcp-lint.json`.

---

## 6. Quick-fixes + code actions

Findings with `fixes[]` register as `vscode.CodeAction` of kind `QuickFix`. User picks fix from lightbulb → extension applies `WorkspaceEdit` from the tuple. No extra round-trip.

For LLM-generated fixes that need confirmation, tool returns `fixes[].requiresConfirmation: true` → extension shows preview diff before apply.

---

## 7. Status + diagnostics surfaces

- `vscode.languages.createDiagnosticCollection("mcp-lint")` per server id.
- Problems panel groups by code prefix (e.g. `lint:invariants`, `lint:intent`).
- Status bar item: `MCP-Lint: 3 servers · 47 findings · idle | running`.
- Output channel `MCP-Lint` for tool stdout/stderr (debug aid).
- Command palette: `MCP-Lint: Run All`, `MCP-Lint: Reload Config`, `MCP-Lint: Show Last Run Trace`.

---

## 8. Comparison axes (for design-explore Phase 1)

| Axis | Option A — Convention-based discovery | Option B — Explicit tag opt-in | Option C — Hybrid (tag preferred, fallback to glob) |
|---|---|---|---|
| Setup cost per tool | zero | one tag annotation | zero or one |
| False-positive risk | high (any matching name pulled in) | low | low |
| Migration story | implicit | breaks tools without tag | smoothest |

| Axis | Option A — One server per repo | Option B — Multi-server array |
|---|---|---|
| Complexity | low | medium |
| Use case | single MCP project | poly-repo workspace, layered linters |

| Axis | Option A — Diagnostics only | Option B — Diagnostics + quick-fixes | Option C — Diagnostics + fixes + AI rewrite preview |
|---|---|---|---|
| Effort | 1 wk | 2 wk | 4 wk |
| Value | shows problems | shows + solves mechanical | shows + solves + suggests creative |

---

## 9. Open questions

- **Range resolution.** When tool emits file-level finding (no range), do we anchor to line 1 or to file header? Policy needed.
- **Multi-repo workspaces.** One config per workspace folder, or single root? Lean: per-folder, merged.
- **Performance ceiling.** Above N findings (~500?) Problems panel chokes. Truncate + "show more" UX?
- **MCP transport.** Stdio v1 firm. HTTP/SSE later for hosted MCP. Defer.
- **Auth + secrets.** Env interpolation in `.mcp-lint.json` — VS Code secret storage integration v2?
- **Reuse for territory-developer.** Tool examples worth wrapping first: `invariant_preflight`, `intent_lint`, `plan_digest_lint`, `validate:drift-lint`, `csharp_class_summary`.

---

## 10. MVP scope estimate

- Extension scaffold + manifest + activation events: 1 day
- MCP stdio client (reuse `@modelcontextprotocol/sdk`): 1 day
- Tool discovery + config loader + watcher: 1 day
- Diagnostic mapper + range translator: 2 days
- Debounce + trigger router (onSave/onChange/manual): 1 day
- Status bar + output channel + commands: 1 day

Total MVP: ~1 week to working diagnostics.
Quick-fixes + multi-server + LLM tier polish: +2 weeks.
Production-quality (settling, telemetry, tests, marketplace publish): +2-3 weeks.

---

## 11. Non-goals

- Replacing language servers — extension stacks alongside, never overrides.
- Bundling MCP servers — extension calls existing server, doesn't ship its own.
- Editing files outside reported ranges — fixes constrained to `edits[]` tuples.
- Running tools at boot for unopened files — lazy, per active editor.
