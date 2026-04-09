---
name: bridge-environment-preflight
description: >
  Verify Postgres connectivity and agent_bridge_job table before using unity_bridge_command or unity_compile.
  Run npm run db:bridge-preflight; interpret exit codes; bounded repair (one attempt per failure class).
  Triggers: "bridge preflight", "postgres ready for bridge", "dev environment check", "agent_bridge_job check",
  "before unity_bridge_command".
---

# Bridge environment preflight — Postgres + IDE agent bridge readiness

Run **before** any **`unity_bridge_command`** / **`unity_compile`** / **`unity_bridge_get`** call, or at the start of a **close-dev-loop** session. The preflight checks that **Postgres** is reachable and migration **0008** (**`agent_bridge_job`**) is applied — the two prerequisites for the **IDE agent bridge** queue.

**Related:** **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)** (Play Mode evidence). **[`close-dev-loop`](../close-dev-loop/SKILL.md)** (Step 0 references this Skill). **Agent-led verification:** after exit **0**, use **`unity_bridge_command`** with **`timeout_ms`:** **`40000`** (initial; on timeout follow **escalation protocol** in [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md): `npm run unity:ensure-editor` → retry 60 s). **Normative IA:** [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) (**Bridge environment preflight**), [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md), **unity-development-context** §10.

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| **Node.js** 18+ | Script uses `pg` and `tsx` |
| **`DATABASE_URL`** or **`config/postgres-dev.json`** | Same resolution as **territory-ia** MCP (`resolveIaDatabaseUrl`) |
| **Postgres** server running | Local or remote; default dev port **5434** per `config/postgres-dev.json` |

**Not required for preflight:** Unity Editor (checked separately by bridge commands).

## Tool recipe — execution order

```
1. Run `npm run db:bridge-preflight`.
2. Exit 0 → proceed to unity_bridge_command / close-dev-loop step 1.
3. Exit 1 (no URL) → report: "Set DATABASE_URL or add database_url to config/postgres-dev.json"; do not retry.
4. Exit 2 (server down) → run `npm run db:setup-local` once → go to 1 (max one attempt).
5. Exit 3 (table missing) → run `npm run db:migrate` once → go to 1 (max one attempt).
6. Exit 4 (SQL error) → report code + stderr tail; do not retry.
7. If still failing after one repair → report code + stderr tail; do not loop.
```

## Exit codes (stable contract)

| Code | Meaning | Repair class |
|------|---------|--------------|
| `0` | **OK** — Postgres reachable, **`agent_bridge_job`** table present | — |
| `1` | **No URL** — neither **`DATABASE_URL`** env nor **`config/postgres-dev.json`** `database_url` resolved | Config |
| `2` | **Connection refused / timeout** — URL resolved but Postgres unreachable | Server down |
| `3` | **Table missing** — connected but **`agent_bridge_job`** missing (migration **0008** not applied) | Migrations |
| `4` | **Unexpected SQL error** — connected, query failed for another reason | Manual |

## URL resolution note

The preflight script imports **`resolveIaDatabaseUrl`** — the same two-layer resolution as **territory-ia** MCP: `DATABASE_URL` env wins, else `config/postgres-dev.json` when not in CI. **Unity Editor** may use **EditorPrefs** or repo-root **`.env.local`** — those are **not** read by this script. If preflight passes but bridge commands time out, the most likely cause is a **URL mismatch** between MCP and Unity — see [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) (**Bridge environment preflight**).

## Seed prompt (parameterize)

```markdown
Run bridge-environment-preflight (`.cursor/skills/bridge-environment-preflight/SKILL.md`):
`npm run db:bridge-preflight` — interpret exit codes, apply bounded repair if needed, then proceed to {NEXT_STEP: unity_bridge_command | close-dev-loop step 1}.
```
