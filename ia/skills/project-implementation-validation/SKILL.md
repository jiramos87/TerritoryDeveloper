---
purpose: Use after substantive implementation when you need repo Node checks aligned with CI: dead project spec paths, MCP package tests, JSON fixtures, IA index drift.
audience: agent
loaded_by: skill:project-implementation-validation
slices_via: none
name: project-implementation-validation
description: >
  Use after substantive implementation when you need repo Node checks aligned with CI: dead project spec
  paths, MCP package tests, JSON fixtures, IA index drift. Root: npm run validate:all (includes compute-lib build + steps 1–4).
  Full local chain (Unity/Postgres when applicable): npm run verify:local (alias: verify:post-implementation).
  Triggers: "post-implementation validation", "run npm checks after backlog work", "validate fixtures", "IA tools parity",
  "MCP tests", "generate:ia-indexes --check", "validate:all", "verify:local".
model: inherit
---

# Project implementation validation (post-implementation checks)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). **Additional exception:** structured JSON Verification header must parse as JSON.

Checklist of existing `npm` commands — not a second copy of validation scripts or MCP tools.

**Related:** [`project-spec-implement`](../project-spec-implement/SKILL.md) (phase exit Pre-commit) · [`stage-closeout-apply`](../stage-closeout-apply/SKILL.md) (post-IA persistence — retired `project-spec-close` folded into Stage-scoped pair per M6 collapse) · [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (optional bridge, not part of this manifest) · [`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md) + [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) (Verification block). **CI parity:** [`.github/workflows/ia-tools.yml`](../../../.github/workflows/ia-tools.yml).

## Failure policy

Run in order. Any step fails → stop, report error, do not continue unless human overrides.

## When to skip

| Situation | Guidance |
|---|---|
| Only runtime C# / Unity assets | All rows N/A; follow AGENTS.md Pre-commit Checklist instead. |
| Touches `tools/mcp-ia-server`, `docs/schemas`, `ia/specs` bodies, glossary, or committed index JSON | Run full IA tools subset (steps 1–4 minimum). |
| Only BACKLOG prose, no `Spec:` / MCP paths | Step 1 may suffice. |

## Validation manifest (v1)

Run from repo root. Script names match [`package.json`](../../../package.json) + [`tools/mcp-ia-server/package.json`](../../../tools/mcp-ia-server/package.json).

| Step | Command | Cwd | Notes |
|---|---|---|---|
| 0 | `npm run compute-lib:build` | root | `territory-compute-lib` tsc; included in `validate:all` |
| 1 | `npm run validate:dead-project-specs` | root | BACKLOG `Spec:` → existing `ia/projects/*.md` |
| 2 | `npm run test:ia` | root | Delegates to `tools/mcp-ia-server` tests (CI parity) |
| 3 | `npm run validate:fixtures` | root | Delegates via `--prefix` |
| 4 | `npm run generate:ia-indexes -- --check` | root | Committed indexes match markdown sources |
| 5 | `npm run verify` | `tools/mcp-ia-server` | Advisory — not in CI; run when touching MCP handlers |

**Single command (steps 0–4):** `npm run validate:all`. Does not run `npm ci`; install deps first if build/test fails.

**Full local chain:** `npm run verify:local` (alias `verify:post-implementation`) — `validate:all` → [`post-implementation-verify.sh`](../../../tools/scripts/post-implementation-verify.sh) with `--skip-node-checks`: Lockfile check → save/quit Editor → `unity:compile-check` → `db:migrate` → `db:bridge-preflight` → reopen Editor → `db:bridge-playmode-smoke` (optional `seed_cell`). Requires Postgres, `.env`/`config/postgres-dev.json`, macOS Accessibility for save/quit automation. `unity:compile-check` sources `.env`/`.env.local` — do not skip because `$UNITY_EDITOR_PATH` empty in shell. Reference: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) Local verification.

## Verification block

Format, Path A/B sequencing, bridge timeouts → [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md). Manifest above is Node-only CI parity subset.

## Optional: IDE bridge evidence (dev machine, N/A in CI)

When §8/§7b calls for Play Mode Console or screenshots → `unity_bridge_command` per [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md). Not mandatory manifest rows — bridge output is evidence, not substitute for `validate:all`/`verify:local`.

## Future placeholders

Add rows when BACKLOG ships new `npm` commands: Unity Edit Mode/batchmode, `tools/compute-lib` tests, static scans.

## Optional territory-ia preface

Session maps to BACKLOG id → `backlog_issue` for Files/Acceptance. `invariants_summary` only when paired with guardrail/runtime C# edits.

## Manual fallback

No local Node → rely on [`.github/workflows/ia-tools.yml`](../../../.github/workflows/ia-tools.yml) on push/PR.

## Seed prompt

```markdown
Run **project-implementation-validation** after implementing {CHANGED_AREAS}.
Apply **When to skip**, then `npm run validate:all` and/or `npm run verify:local`; stop on first failure.
```
