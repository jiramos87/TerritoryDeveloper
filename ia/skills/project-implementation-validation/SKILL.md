---
name: project-implementation-validation
description: >
  Use after substantive implementation when you need repo Node checks aligned with CI: dead project spec
  paths, MCP package tests, JSON fixtures, IA index drift. Root: npm run validate:all (includes compute-lib build + steps 1–4).
  Full local chain (Unity/Postgres when applicable): npm run verify:local (alias: verify:post-implementation).
  Triggers: "post-implementation validation", "run npm checks after backlog work", "validate fixtures", "IA tools parity",
  "MCP tests", "generate:ia-indexes --check", "validate:all", "verify:local".
---

# Project implementation validation (post-implementation checks)

This skill **does not** call MCP tools itself. It is a **checklist** of **existing** **`npm`** commands — **not** a second copy of `tools/validate-dead-project-spec-paths.mjs` or the **MCP** scripts.

**Related:** **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (phase exit → **Pre-commit Checklist** — **this** skill adds **automated** **Node** steps). **[`project-spec-close`](../project-spec-close/SKILL.md)** (optional: run **after** **IA persistence** when the change touched **MCP**, **schemas**, or **spec/glossary** bodies that feed indexes — **before** mandatory `npm run validate:dead-project-specs` in closeout). **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)** (optional **Unity** logs/screenshots via MCP — **not** part of this manifest). **[`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md)** + [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) — **Verification** block (**validate:all**, compile, **Agent test mode batch**, **IDE agent bridge** with **`timeout_ms` 40000** initial + escalation protocol) for agent completion messages. **CI parity:** [`.github/workflows/ia-tools.yml`](../../../.github/workflows/ia-tools.yml). **Dead project-spec paths:** `npm run validate:dead-project-specs`. **Conventions:** [`.cursor/skills/README.md`](../README.md).

## Failure policy

Run steps **in order**. If any step fails, **stop**, report the error output, and do **not** continue to later steps unless the human explicitly overrides.

## When to skip (N/A)

| Situation | Guidance |
|-----------|----------|
| Diff is **only** runtime **C#** / **Unity** assets | You may mark **all** manifest rows **N/A** and follow **[`AGENTS.md`](../../../AGENTS.md)** **Pre-commit Checklist** (Unity build, domain checks) instead. |
| Diff touches **`tools/mcp-ia-server`**, **`docs/schemas`**, **`.cursor/specs`** bodies, **`.cursor/specs/glossary.md`**, or committed **`tools/mcp-ia-server/data/*-index.json`** | Run the full **IA tools**-aligned subset below (at least steps 1–4). |
| Diff only fixes **BACKLOG** prose without **`Spec:`** or **MCP** paths | Step 1 may suffice; use judgment — **CI** still runs the full **Node** job on relevant paths. |

## Validation manifest (v1)

Run from **repository root** unless **Cwd** says otherwise. Script names match root [`package.json`](../../../package.json) and [`tools/mcp-ia-server/package.json`](../../../tools/mcp-ia-server/package.json) at ship time.

| Step | Command | Cwd | Notes |
|------|---------|-----|--------|
| 0 | `npm run compute-lib:build` | repo root | **`territory-compute-lib`** **`tsc`** — same ordering as **CI** before **`test:ia`**; included inside **`validate:all`** |
| 1 | `npm run validate:dead-project-specs` | repo root | Open **BACKLOG** **`Spec:`** must point at existing `.cursor/projects/*.md` |
| 2 | `npm run test:ia` | repo root | Delegates to `npm --prefix tools/mcp-ia-server test` — same tests **CI** runs after `npm ci` under **`tools/mcp-ia-server`** |
| 3 | `npm run validate:fixtures` | repo root | Delegates via `--prefix` to **`tools/mcp-ia-server`** |
| 4 | `npm run generate:ia-indexes -- --check` | repo root | Ensures committed **`spec-index.json`** / **`glossary-index.json`** match **markdown** sources |
| 5 | `npm run verify` | `tools/mcp-ia-server` | **Advisory** — **not** in **IA tools** **Node** job today; run when touching **MCP** registration, parsers, or tool handlers |

**Single command (steps 0–4):** From repo root, `npm run validate:all` runs **dead project spec** paths, **`compute-lib:build`**, then steps 2–4 above. It does **not** run `npm ci`; install **`tools/compute-lib`** / **`tools/mcp-ia-server`** dependencies first if **`compute-lib:build`** or **`test:ia`** fails (see root **`package.json`** `description`).

**Full local closed loop (canonical — macOS default orchestration, no env flag):** From repo root, **`npm run verify:local`** runs **`validate:all`** then [`tools/scripts/post-implementation-verify.sh`](../../../tools/scripts/post-implementation-verify.sh) with **`--skip-node-checks`**: if **`Temp/UnityLockfile`** exists, AppleScript **Save** + **Quit** and wait up to **30s** → **`unity:compile-check`** → **`db:migrate`** → **`db:bridge-preflight`** → if the lock exists again, repeat save/quit + **30s** → **`open`** Unity on **`REPO_ROOT`**, wait up to **60s** for the lock → **`db:bridge-playmode-smoke`** (optional: **`npm run verify:local -- "x,y"`** for **`seed_cell`**). **`npm run verify:post-implementation`** is an **alias** for **`verify:local`**. **Non-macOS:** same through **`db:bridge-preflight`**, then prints manual bridge instructions. Requires **Postgres**, **`.env`** / **`config/postgres-dev.json`**, **Accessibility** for **System Events** if save/quit automation is used, and **AgentBridgeCommandRunner** after Editor starts. **`unity:compile-check`** sources repo-root **`.env`** / **`.env.local`** inside the bash script — **AI agents** must **not** skip **`npm run unity:compile-check`** because **`$UNITY_EDITOR_PATH`** is empty in the shell. **Canonical** reference: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) **Local verification**.

**Equivalent in package folder:** For step 2, `cd tools/mcp-ia-server && npm test` after `npm ci` matches **CI** exactly.

**Project specs:** New **`.cursor/projects/{ISSUE_ID}.md`** stubs should include **`## 7b. Test Contracts`** ([`.cursor/templates/project-spec-template.md`](../../templates/project-spec-template.md)) so **Acceptance** maps to these **Node** checks where applicable.

## Verification block (agent messages — alongside this manifest)

When reporting **Verification** after substantive implementation, follow [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md): include **`npm run validate:all`** (exit code), **`npm run unity:compile-check`** if **`Assets/`** **C#** changed, **Path A** **`npm run unity:testmode-batch`** summary (prefer **`--quit-editor-first`** when an Editor might hold **`REPO_ROOT`**), and **Path B** **`unity_bridge_command`** outcome with **`timeout_ms`:** **`40000`** initial (escalation protocol on timeout; or **N/A** + reason). This is **separate** from the **Validation manifest** table above (Node-only **CI** parity).

## Optional: IDE agent bridge evidence (dev machine — **N/A** in CI)

When **§8 Acceptance** or **§7b** calls for **Play Mode** **Console** excerpts or **Game view** screenshots (e.g. HUD visible, no **`error`** severities), an agent **with** **territory-ia** and a configured dev machine **may** call **`unity_bridge_command`** (**`get_console_logs`**, **`capture_screenshot`**, **`include_ui`** for **Overlay** UI). See **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)** for prerequisites, parameters, and limits.

- **Do not** add these calls as mandatory rows in the **Validation manifest** above — **GitHub Actions** does not run **Unity** or **Postgres** bridge dequeue for game projects.
- Treat bridge output as **human / agent evidence** attached to the issue or chat, not a substitute for **`npm run validate:all`** or **`npm run verify:local`**.

## Future / N/A (placeholders)

Add rows here when **BACKLOG** issues ship new **`npm`** commands (record the addition in a **Decision Log** on the shipping issue’s project spec, or in this **SKILL.md** body with maintainer review):

- **Unity** **Edit Mode** / **batchmode** — open **BACKLOG** harness rows / **UTF** (no single **v1** one-liner).
- **`tools/compute-lib`** tests — when the package and **`npm test`** exist.
- Mechanical static scans — when merged as **`npm run`** targets ([`BACKLOG.md`](../../../BACKLOG.md)).

## Optional territory-ia preface

When the session maps to a **BACKLOG** id, call **`backlog_issue`** first for **Files** / **Acceptance**. Call **`invariants_summary`** only if validation is paired with **guardrail** or runtime **C#** doc edits (unusual for pure test runs).

## Manual fallback (no local Node)

Rely on **[`.github/workflows/ia-tools.yml`](../../../.github/workflows/ia-tools.yml)** on push/PR, or run the same commands in CI logs order when a local **Node** install is unavailable.

## Seed prompt (parameterize)

Replace `{CHANGED_AREAS}` with a short note on what shipped (e.g. **MCP** parser, **glossary**, **schema** fixture).

```markdown
Run **project-implementation-validation** after implementing {CHANGED_AREAS}.
Use `.cursor/skills/project-implementation-validation/SKILL.md`: apply **When to skip**, then run **`npm run validate:all`** and/or **`npm run verify:local`** (canonical local post-implementation when **Postgres** + **Unity** bridge apply); stop on first failure.
Do not reimplement the dead-spec scanner — use `npm run validate:dead-project-specs` only.
```
