---
purpose: "Explore real-time(ish) inter-agent communication for parallel main-session Claude Code agents. Today: only async via spec/MEMORY edits; no push, no shared channel. Goal: typed message broker on existing DB + MCP surface."
audience: agent
loaded_by: on-demand
created_at: 2026-05-11
related_docs:
  - ia/specs/architecture/interchange.md
  - docs/explorations/async-cron-jobs.md
  - ia/skills/section-claim/SKILL.md
related_mcp_slices:
  - claim_heartbeat
  - claims_sweep
  - cron_journal_append_enqueue
  - section_claim
---

# Agent-to-agent IPC — exploration

## §Discussion abstract (2026-05-11)

Parallel main-session agents (two `claude` CLIs, two Cursor tabs, etc.) cannot signal each other in real time. Current "comms" surfaces all async + manual:

- Spec / MEMORY / master-plan edits → other agent only sees on next file read.
- `ia_section_claims` / `ia_stage_claims` → coordination via row presence, but no payload, no inbox.
- `cron_journal_append_enqueue` → audit trail, write-only, no recipient.

**No native Claude Code IPC primitive.** Agents only "wake" on tool result or user turn — there is no push channel agent-side regardless of broker. So real-time caps at `poll_interval × turn_latency`. "Real-time enough" = ≤30 s poll on a hot loop, ≤5 min on idle.

**Premise.** Add `ia_agent_messages` table + `mcp__territory-ia__agent_post` / `agent_inbox_drain` MCP pair. Sender enqueues; recipient polls inbox at skill phase boundaries (already natural cadence in `ship-cycle`, `section-claim` heartbeat ticks). Cron sweeps stale unread rows past TTL.

**Recommendation (tentative).** Mirror `cron_*_enqueue` shape — typed payload, kind discriminator, recipient = `agent_session_id` (issued at session start) OR broadcast topic (`section:{slug}`, `stage:{id}`). Drain returns ordered batch + flips `read_at`. Heartbeat tick (existing `claim_heartbeat` cadence) doubles as inbox poll — no new polling infra.

**Cross-link.** `async-cron-jobs` already established per-kind table precedent + tick-script shape. IPC = same template, recipient column instead of cron schedule.

## §Open questions

- **Q1 — addressing model.** Direct (`session_id` → `session_id`) OR topic-bus (`section:{slug}`, `stage:{id}`, broadcast) OR both? Direct = tight coupling but cheap; topic = decoupled but recipient discovery harder.
- **Q2 — session identity.** No native Claude Code session id — invent one (uuid in `.claude/active-session.json`)? Bind to git worktree path? Bind to `ia_section_claims.row_id`? Affects whether IPC survives session restart.
- **Q3 — poll cadence + carrier.** Piggyback on existing `claim_heartbeat` MCP (every Pass A iteration in `ship-stage`) OR new dedicated `agent_inbox_drain` call at every skill phase boundary? First = zero new overhead; second = lower latency on non-claim skills.
- **Q4 — payload schema.** Free-form JSON + sender-defined `kind`, OR enum kinds (`question`, `handoff`, `blocker`, `verify_request`) with typed schema per kind? Enum = drift-resistant; free-form = ergonomic.
- **Q5 — delivery semantics.** At-least-once with idempotency key, OR exactly-once via `read_at` + transaction? Cron sweep TTL — minutes (chat-fast) or hours (overnight handoff)?
- **Q6 — push approximation.** Pure poll, OR file-watcher tickle (`ia/state/inbox/{session_id}.flag` touched by sender → agent's next tool call sees mtime change → drains inbox)? Watcher = sub-second wake on ANY tool result, but only works if agent issues tool calls at all.
- **Q7 — human-in-the-loop bridge.** Should main-session agent surface inbound messages as `AskUserQuestion` polls when ambiguous, OR auto-act on typed `handoff` kinds without user friction?
- **Q8 — observability.** New MCP `agent_inbox_list` slice for debugging dead-lettered messages? `validate:agent-messages` drift gate?

## §Constraints

- MCP-first directive — IPC surface must be `mcp__territory-ia__*` tools, not raw SQL or file scribbles.
- DB = Postgres (local instance, see `ia/specs/architecture/interchange.md`); MVCC + serializable txns available; no native pub/sub used (LISTEN/NOTIFY out of scope) — poll-only.
- No agent-side push channel exists; "real-time" = bounded by poll cadence.
- Hook denylist + `flock` precedent (`invariants.md`) — new lockfile `.agent-messages.lock` if cross-session writes contend.
- Sessions ephemeral — message TTL must survive at least one Claude Code restart; recipient may not return.
- Caveman register everywhere except: payload `kind="question"` body (forwarded to human → product register).

## §Hand-off

Run `/design-explore docs/explorations/agent-to-agent-ipc.md` to expand, resolve Q1–Q8, pick addressing + poll model. Likely outcome = single TECH-* issue (schema mig + MCP slice pair + tick script) OR small master plan if topic-bus + watcher tickle both land. Orthogonal to `large-file-atomization-hub-thinning-sweep` — pure MCP-server + new mig.

Next: `/design-explore docs/explorations/agent-to-agent-ipc.md`

---

## Design Expansion

> Phase 0 + 0.5 interview run in main session. Phases 1–9 expanded by `design-explore` subagent (2026-05-11). Locked iQ answers (scope=both, session id=UUID PK + `own_id` vendor-native, payload=enum kinds + typed body, push=pure poll, delivery=exactly-once + TTL) absorbed verbatim. Remaining open axes Q1 / Q7 / Q8 resolved with principled defaults — flagged **NEEDS-RATIFICATION** in Review Notes; main session must confirm before `/master-plan-new`.

### Chosen Approach

| Axis | Decision | Rationale |
|---|---|---|
| Q1 addressing | **Both — union column (`recipient_session_id` XOR `topic`)** | Direct = cheap 1:1 for question/handoff. Topic = required for cross-vendor restart-survival (e.g. `stage:{id}` lets replacement Cursor session pick up dropped handoff). One nullable column each + `CHECK ((recipient_session_id IS NULL) <> (topic IS NULL))` keeps schema tight. |
| Q7 human bridge | **Kind-driven gate** | `handoff` / `verify_request` / `ack` auto-act on drain. `question` / `blocker` surface via `AskUserQuestion`. Matches locked enum kinds (iQ payload schema) — gate baked into kind, not per-message flag. |
| Q8 observability | **Both — `agent_inbox_list` slice + `validate:agent-messages` drift gate** | Slice: filter by session/topic/kind/status for ad-hoc debug. Gate: CI red on `unread + expired > N` (default 5) for any session in last 24h. Cheap once core post/drain land. |

#### Phase 1 — addressing-model matrix

| Axis | Direct only | Topic-bus only | **Both (union)** | Defer topic |
|---|---|---|---|---|
| Constraint fit (multi-vendor + restart) | Partial — recipient UUID lost on restart unless `own_id` lookup chains | Strong — topic outlives session | **Strong — direct for hot loop, topic for restart-survival** | Partial — same as direct only |
| Effort | Low (1 col, 1 idx) | Low (1 col, 1 idx) | **Low+ (2 cols + XOR check)** | Lowest (1 col) |
| Output control | Tight 1:1 | Loose; sender doesn't know who consumes | **Tight when needed; loose when broadcast** | Tight 1:1 |
| Maintainability | Simple | Simple, but discovery overhead (who subscribes?) | **Subscription registry adds shape; XOR keeps row contract clear** | Simple |
| Dependencies / risk | Sender must resolve recipient UUID before post; restart breaks chain | Subscriber discovery problem; lost-update risk if multi-drainer | **Subscription registry (`ia_agent_topic_subs`); slight mig surface bump** | Same as direct; topic retrofit later = column-rename pain |

#### Phase 2 — selection

**Both (union)** picked. Phase 2 user gate **deferred to main session** (subagent cannot poll). If main session rejects → fall back to **Defer topic** (single column `recipient_session_id`, add `topic` nullable now but no drain support — TECH-* upgrade later).

### Architecture Decision

> Phase 2.5 MCP writes (`arch_decision_write` → `cron_arch_changelog_append_enqueue` → `arch_drift_scan`) **deferred to main session**. Subagent cannot run AskUserQuestion polls (slug → rationale → alternatives → affected `arch_surfaces[]`). Main session: after ratifying Phase 2 selection, run the 4 polls inline and emit the MCP write chain. Proposed slug: `dec-agent-to-agent-ipc-broker`. Affected `arch_surfaces[]` (provisional): `db/migrations/`, `tools/mcp-ia-server/src/tools/agent-post.ts`, `tools/mcp-ia-server/src/tools/agent-inbox-drain.ts`, `tools/mcp-ia-server/src/tools/agent-inbox-list.ts`, `tools/cron-server/handlers/agent-messages-sweep-cron-handler.ts`, `ia/specs/architecture/interchange.md`.

### Architecture

```mermaid
flowchart LR
  subgraph Sender["Sender agent (skill phase)"]
    S1[skill emits message]
  end
  subgraph MCP["MCP slice"]
    P[agent_post]
    D[agent_inbox_drain]
    L[agent_inbox_list]
  end
  subgraph DB["Postgres"]
    T[(ia_agent_messages)]
    Subs[(ia_agent_sessions + ia_agent_topic_subs)]
  end
  subgraph Cron["cron-server"]
    Sweep[agent-messages-sweep handler]
  end
  subgraph Receiver["Receiver agent"]
    H[claim_heartbeat tick]
    PB[skill phase boundary]
    AUQ[AskUserQuestion bridge]
    Auto[auto-act handler]
  end

  S1 -->|kind + body + recipient_session_id OR topic + ttl_minutes| P
  P -->|INSERT row| T
  H -->|poll own_uuid + subscribed topics| D
  PB -->|poll own_uuid + subscribed topics| D
  D -->|SELECT FOR UPDATE + flip read_at in txn| T
  D -->|kind in {question, blocker}| AUQ
  D -->|kind in {handoff, verify_request, ack}| Auto
  Sweep -->|expired_at < now AND read_at IS NULL| T
  L -.->|debug filter| T
  Subs -.->|topic subscription lookup| D
```

Entry / exit:
- **Entry (sender):** any skill phase calls `agent_post`. P95 < 100 ms (single INSERT).
- **Exit (receiver):** `agent_inbox_drain` flips `read_at` in same txn that returns rows → exactly-once. Kind-driven routing decides AskUserQuestion vs auto-act.
- **Sweep (cron):** per-row `expires_at = enqueued_at + ttl_minutes` precomputed. Expired-unread rows logged + purged (or marked `expired`) by `agent-messages-sweep` handler at `* * * * *` cadence. Reuses existing cron-server lib (`claim.ts` / `flip.ts`).

### Subsystem Impact

| Subsystem | Dep | Invariant risk | Breaking? | Mitigation |
|---|---|---|---|---|
| `db/migrations/` (new `0096_ia_agent_messages.sql`, `0097_ia_agent_sessions.sql`, `0098_ia_agent_topic_subs.sql`) | hard | **Inv 13** (monotonic id source) — reuse `reserve-id.sh` for migration number gating not required (mig nums separate); generated UUIDs via `gen_random_uuid()`, **never** hand-issue | additive | one mig per table; rollback files included |
| `tools/mcp-ia-server/src/tools/agent-post.ts` (new) | hard | none | additive | wrapTool + envelope shape; Zod schema per kind |
| `tools/mcp-ia-server/src/tools/agent-inbox-drain.ts` (new) | hard | none | additive | txn boundary (`SELECT … FOR UPDATE … RETURNING` + `UPDATE … SET read_at=now()`) |
| `tools/mcp-ia-server/src/tools/agent-inbox-list.ts` (new) | soft | none | additive | read-only debug slice |
| `tools/mcp-ia-server/src/server-registrations.ts` | hard | none | additive | three new `register*` calls |
| `tools/cron-server/handlers/agent-messages-sweep-cron-handler.ts` (new) | hard | none | additive | mirror `stale-sweep-cron-handler` shape |
| `tools/cron-server/index.ts` (cadence registration) | hard | none | additive | `* * * * *` line |
| `ia/skills/section-claim/SKILL.md` (heartbeat-tick poll) | soft | none | additive | add "drain inbox" step inside heartbeat loop |
| `ia/skills/ship-cycle/SKILL.md` (phase-boundary poll) | soft | none | additive | drain at Pass A start + Pass B start |
| `ia/specs/architecture/interchange.md` | soft | none | additive | new §Agent-to-agent IPC subsection |
| `ia/specs/glossary.md` | soft | **Inv 12** (terminology canonicality) — add rows for `agent message`, `inbox drain`, `topic subscription`, `own_id` | additive | one row per term + cite spec section |

Invariants flagged: **12** (glossary additions), **13** (UUID generation flow — confirmed safe: server-side `gen_random_uuid()`, no `id-counter.json` touch).

### Implementation Points

Phased checklist. Stage sizing follows prototype-first methodology.

**Stage 1.0 — tracer (red→green slice)**
- [ ] (a) Mig `0096_ia_agent_messages.sql` — columns: `message_id uuid PK`, `kind text` (enum check), `sender_session_id uuid NOT NULL`, `recipient_session_id uuid NULL`, `topic text NULL`, `body jsonb NOT NULL`, `ttl_minutes int NOT NULL`, `enqueued_at timestamptz NOT NULL DEFAULT now()`, `expires_at timestamptz GENERATED ALWAYS AS (enqueued_at + (ttl_minutes || ' minutes')::interval) STORED`, `read_at timestamptz NULL`, `status text NOT NULL DEFAULT 'unread'` (`unread|read|expired`). CHECK `(recipient_session_id IS NULL) <> (topic IS NULL)`. Indexes: `(recipient_session_id, read_at, enqueued_at)`, `(topic, read_at, enqueued_at)`, `(expires_at) WHERE read_at IS NULL`.
- [ ] (a) Mig `0097_ia_agent_sessions.sql` — `session_id uuid PK DEFAULT gen_random_uuid()`, `own_id text NOT NULL` (vendor-native), `vendor text NOT NULL` (`claude_code|cursor|codex|other`), `worktree_path text`, `created_at`, `last_seen_at`. UNIQUE `(vendor, own_id)`.
- [ ] (a) Mig `0098_ia_agent_topic_subs.sql` — `subscription_id uuid PK`, `session_id uuid REFERENCES ia_agent_sessions`, `topic text NOT NULL`, `created_at`. UNIQUE `(session_id, topic)`.
- [ ] (b) MCP slice `agent_post` — Zod schema: `kind ∈ {question, handoff, blocker, verify_request, ack}`, per-kind body schema (discriminated union), `recipient_session_id` XOR `topic` enforced, `ttl_minutes` default 30 (kind=question) / 480 (handoff) / 60 (others). Returns `{message_id, expires_at}`.
- [ ] (b) MCP slice `agent_inbox_drain` — input: `session_id` (own UUID); auto-resolves subscribed topics from `ia_agent_topic_subs`. Output: ordered batch (FIFO) + flips `read_at = now()` in same txn (`SELECT … FOR UPDATE SKIP LOCKED … RETURNING` then `UPDATE`). Returns `[{message_id, kind, sender_session_id, topic, body, enqueued_at}]`.
- [ ] (c) Skill integration — `claim_heartbeat` flow drains inbox once per tick (existing cadence in `ship-stage` Pass A). Kind-router: `handoff`/`verify_request`/`ack` → handler stub; `question`/`blocker` → emit `AskUserQuestion`.
- [ ] (d) Cron sweep — `cron-server/handlers/agent-messages-sweep-cron-handler.ts`: `UPDATE ia_agent_messages SET status='expired' WHERE expires_at < now() AND read_at IS NULL`. Cadence `* * * * *`.

**Stage 2.0 — observability + adoption**
- [ ] (e) MCP slice `agent_inbox_list` (read-only debug; filters by `session_id`, `topic`, `kind`, `status`, time window).
- [ ] (e) Validator `validate:agent-messages.mjs` — fail when `expired+unread > 5` per session in last 24h.
- [ ] (f) Skill-level adoption — first call sites: `ship-cycle` Pass A boundary (drain), `section-claim` heartbeat (drain), `design-explore` Phase 2 (post `question` when human gate needed), `ship-final` (post `ack` on master-plan close).
- [ ] (f) Session-registration helper — `tools/scripts/register-agent-session.mjs` writes `(vendor, own_id, worktree_path)` row on `.claude/active-session.json` change.

**Deferred / out of scope**
- File-watcher tickle (locked iQ: pure poll only).
- Free-form `kind` (locked iQ: enum + per-kind schema).
- At-least-once + idempotency key (locked iQ: exactly-once via `read_at` flip).
- Cross-machine push channel (no wake primitive exists; poll cap is the design ceiling).

### Examples

**Typed `question` kind** — agent A asks agent B to confirm a slug.
- Input: `agent_post({kind:'question', sender_session_id:'…uuid-A…', recipient_session_id:'…uuid-B…', body:{prompt:'Confirm slug `dec-agent-to-agent-ipc-broker`?', options:['yes','no']}, ttl_minutes:30})`
- Output: `{message_id:'…uuid-msg…', expires_at:'2026-05-11T17:00:00Z'}`
- Receiver drain → kind='question' → `AskUserQuestion` surface to human at next phase boundary.

**Typed `handoff` kind** — main session hands open stage to a fresh subagent.
- Input: `agent_post({kind:'handoff', sender_session_id:'…A…', topic:'stage:large-file-atomization-stage-3.3', body:{action:'continue_pass_b', stage_id:'3.3', plan_slug:'large-file-atomization-hub-thinning-sweep'}, ttl_minutes:480})`
- Output: row enqueued. Any session subscribed to `stage:large-file-atomization-stage-3.3` (via `ia_agent_topic_subs`) drains it.
- Receiver drain → kind='handoff' → auto-act (kind-driven gate): triggers `ship-cycle` continuation. No human prompt.

**TTL expiry** — handoff never claimed (recipient laptop sleeps overnight, ttl=480 min).
- Cron sweep at `* * * * *` finds row where `expires_at < now() AND read_at IS NULL`.
- Flip `status='expired'`. Validator `validate:agent-messages` flags it next CI run.

**Multi-vendor `own_id` lookup** — Claude Code session posts to a Cursor recipient.
- Sender knows Cursor's `own_id` (e.g. cursor's window id from `.cursor/active-session.json`).
- Sender resolves recipient UUID: `SELECT session_id FROM ia_agent_sessions WHERE vendor='cursor' AND own_id='…cursor-win-id…'`.
- `agent_post` writes row with `recipient_session_id` = resolved UUID. Cursor session's heartbeat drains it on next poll.

### Review Notes

Self-review (Plan subagent not spawnable inline). Carry into main session.

**BLOCKING (must resolve before `/master-plan-new`):**
1. **Original doc says SQLite (`§Constraints` line: "DB = SQLite (`ia/state/ia.db`)") — actual repo DB is Postgres (`pg` pool, `db/migrations/` numbered 0001–0095+, `gen_random_uuid()` used).** All architecture above written for Postgres. Main session must amend `§Constraints` SQLite line before plan authoring, or this expansion is mis-scoped.
2. **Phase 2 user gate skipped (subagent cannot poll).** Main session must run `AskUserQuestion` confirming **Both (union)** addressing before plan authoring. If overridden → re-derive Stage 1.0 mig column set.
3. **Phase 2.5 MCP arch-decision writes not executed** (subagent cannot run AskUserQuestion). Main session: run 4 polls + `arch_decision_write` + `cron_arch_changelog_append_enqueue` + `arch_drift_scan` chain before `/master-plan-new`.

**NON-BLOCKING (carry forward):**
- Q7 (kind-driven gate) and Q8 (slice + gate both) picked without main-session ratification — confirm in next turn.
- `expires_at` as `GENERATED ALWAYS AS … STORED` requires PG ≥ 12 (confirmed available — existing migrations use `gen_random_uuid()` from `pgcrypto`).
- `ia_agent_topic_subs` adds a registration step at session start. Helper script (Stage 2.0 task) closes the loop but a missed registration = silent topic miss.
- Multi-drainer race on topic rows: `SELECT … FOR UPDATE SKIP LOCKED` already mitigates — confirmed by `cron_audit_log` precedent.
- Caveman register applies to MCP descriptors + spec prose. `kind='question'` body is the documented product-register exception (per `§Constraints` line).

**SUGGESTIONS:**
- Add `correlation_id` column to `ia_agent_messages` for request/response pairing (sender posts `question`, receiver posts `ack` carrying same correlation_id). Cheap (one nullable uuid col) + unlocks future RPC-style patterns.
- Consider `priority int NOT NULL DEFAULT 0` column; drain `ORDER BY priority DESC, enqueued_at ASC`. Optional Stage 2.0+.

### Expansion metadata

- **Date (ISO):** 2026-05-11
- **Model:** claude-opus-4-7
- **Approach selected:** Q1 = Both (union) — **ratified main session 2026-05-12**
- **Blocking items resolved:** 3 of 3 — (1) Postgres correction applied to §Constraints; (2) Phase 2 ratified Both-union; (3) Phase 2.5 arch chain executed below

### Architecture Decision (DEC-A27)

- **Slug:** `DEC-A27` `agent-to-agent-ipc-broker` — `status=active`, written 2026-05-12
- **Rationale:** Typed broker for parallel multi-vendor agents (Claude/Cursor/Codex). `ia_agent_messages` + `ia_agent_sessions` tables, MCP `agent_post`/`agent_inbox_drain` slices, poll-at-heartbeat, exactly-once via `read_at` + `ttl_minutes`. Unblocks hot-loop + async handoff.
- **Alternatives rejected:** Postgres LISTEN/NOTIFY (no agent push channel); file-watcher tickle (.flag infra overhead); free-form JSON kinds (drift risk); direct-only or topic-only addressing.
- **Affected `arch_surfaces`:** `data-flows/persistence` (FK), `interchange/agent-ia` (body).
- **Changelog:** queued `cron_arch_changelog_append_enqueue` job `8a04dbfd-a7c6-42de-add6-114a77dad156` (kind=`design_explore_decision`).

#### Drift report (arch_drift_scan, 2026-05-12)

8 stages flagged. Most predate DEC-A27 (carry-over from DEC-A26 / `data-flows/initialization` edits on 2026-05-06). DEC-A27 changelog drain will add fresh hits on every stage linking `data-flows/persistence`. Per DEC-A14: never auto-rewrite plans — flag for `/arch-drift-scan` follow-up per plan.

| Plan slug | Stage | Drifted surface | Trigger |
|---|---|---|---|
| `grid-asset-visual-registry` | 3.3 | `data-flows/persistence` | DEC-A26 changelog (will re-hit on DEC-A27 drain) |
| `lifecycle-refactor` | 9.0 | `data-flows/persistence` | DEC-A26 changelog |
| `multi-scale` | 4.0 | `data-flows/initialization` | spec_edit_commit ×3 |
| `multi-scale` | 14.0 | `data-flows/persistence` | DEC-A26 changelog |
| `music-player` | 5.0 | `data-flows/persistence` | DEC-A26 changelog |
| `utilities` | 2.0 | `data-flows/initialization` | spec_edit_commit ×3 |
| `utilities` | 12.0 | `data-flows/persistence` | DEC-A26 changelog |
| `web-platform` | 38.0 | `data-flows/persistence` | DEC-A26 changelog |

**Action:** Pre-existing drift — not blocking DEC-A27 close. Recommend `/arch-drift-scan {slug}` per plan when those stages next move through lifecycle.

