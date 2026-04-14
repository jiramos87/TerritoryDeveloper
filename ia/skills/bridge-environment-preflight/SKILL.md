---
purpose: Verify Postgres connectivity and agent_bridge_job table before using unity_bridge_command or unity_compile.
audience: agent
loaded_by: skill:bridge-environment-preflight
slices_via: none
name: bridge-environment-preflight
description: >
  Verify Postgres connectivity and agent_bridge_job table before using unity_bridge_command or unity_compile.
  Run npm run db:bridge-preflight; interpret exit codes; bounded repair (one attempt per failure class).
  Triggers: "bridge preflight", "postgres ready for bridge", "dev environment check", "agent_bridge_job check",
  "before unity_bridge_command".
---

# Bridge environment preflight — Postgres + IDE agent bridge readiness

Run before any `unity_bridge_command` / `unity_compile` / `unity_bridge_get`, or at start of a close-dev-loop session. Checks Postgres reachable + migration 0008 (`agent_bridge_job`) applied — IDE agent bridge queue prereqs.

**Related:** [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (Step 0). **Post exit 0:** `unity_bridge_command` with `timeout_ms: 40000` initial; on timeout follow escalation protocol ([`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md)): `npm run unity:ensure-editor` → retry 60 s. **Normative:** [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md), [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md), unity-development-context §10.

## Prerequisites

| Requirement | Notes |
|---|---|
| Node.js 18+ | Script uses `pg` + `tsx` |
| `DATABASE_URL` or `config/postgres-dev.json` | Same resolution as territory-ia MCP (`resolveIaDatabaseUrl`) |
| Postgres server running | Local or remote; default dev port 5434 per `config/postgres-dev.json` |

Not required: Unity Editor (bridge commands check separately).

## Tool recipe — execution order

```
1. `npm run db:bridge-preflight`.
2. Exit 0 → proceed.
3. Exit 1 (no URL) → report "Set DATABASE_URL or add database_url to config/postgres-dev.json"; no retry.
4. Exit 2 (server down) → `npm run db:setup-local` once → step 1 (max one attempt).
5. Exit 3 (table missing) → `npm run db:migrate` once → step 1 (max one attempt).
6. Exit 4 (SQL error) → report code + stderr tail; no retry.
7. Still failing after one repair → report + escalate; no loop.
```

## Exit codes (stable contract)

| Code | Meaning | Repair class |
|---|---|---|
| `0` | OK — Postgres reachable, `agent_bridge_job` + `agent_bridge_lease` present | — |
| `1` | No URL — neither `DATABASE_URL` env nor `config/postgres-dev.json` `database_url` resolved | Config |
| `2` | Connection refused / timeout — URL resolved, Postgres unreachable | Server down |
| `3` | Table missing — connected but `agent_bridge_job` (migration 0008) or `agent_bridge_lease` (migration 0010) absent | Migrations |
| `4` | Unexpected SQL error — connected, query failed otherwise | Manual |

## URL resolution note

Script imports `resolveIaDatabaseUrl` — same two-layer resolution as territory-ia MCP: `DATABASE_URL` env wins, else `config/postgres-dev.json` when not in CI. Unity Editor may use EditorPrefs or repo-root `.env.local` — NOT read by this script. Preflight passes but bridge times out → likely URL mismatch between MCP and Unity. See [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md).

## Seed prompt

```markdown
Run bridge-environment-preflight (`ia/skills/bridge-environment-preflight/SKILL.md`):
`npm run db:bridge-preflight` — interpret exit codes, apply bounded repair if needed, then proceed to {NEXT_STEP: unity_bridge_command | close-dev-loop step 1}.
```
