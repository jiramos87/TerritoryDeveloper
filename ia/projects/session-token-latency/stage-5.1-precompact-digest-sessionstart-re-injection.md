### Stage 5.1 — PreCompact digest + SessionStart re-injection


**Status:** In Progress — Stage 5.1 (TECH-520, TECH-521, TECH-522, TECH-523)

**Pre-conditions:** Stage 3.1 T3.1.2 + T3.1.3 Done. Stage 4.1 T4.1.1 optional (soft).

**Objectives:** Author `context-pack.sh` on PreCompact event writing `.claude/context-pack.md` per schema; wire into `.claude/settings.json`; extend `session-start-prewarm.sh` to re-inject pack content after deterministic block (volatile suffix zone — preserves Stage 3.1 D2 cacheable prefix); enforce 300-line cap + 24 h freshness gate. Land manual re-orientation integration test evidence + docs update.

**Exit:**

- `tools/scripts/claude-hooks/context-pack.sh` exists, executable, shebang `#!/usr/bin/env bash`, `set -uo pipefail` (no `-e` — graceful partial failure). Reads `ia/state/runtime-state.json` + active plan Stage block + telemetry jsonl; emits schema per extensions doc §2.
- `.claude/settings.json` hooks array contains PreCompact entry running `context-pack.sh`.
- Size cap 300 lines enforced via awk block-boundary truncation; Relevant surfaces never truncated; truncation marker `_[...truncated N oldest decisions]_` emitted when cap triggers.
- `session-start-prewarm.sh` cats pack content after deterministic block + `---` separator, gated on `-f .claude/context-pack.md` AND pack `ts` header <24 h old.
- Deterministic prefix byte-stable across runs (verified by diff of two runs with different pack content).
- `.claude/context-pack.md` added to `.gitignore`.
- Manual re-orientation test passes per `docs/agent-led-verification-policy.md` §Session continuity protocol; evidence linked (screenshot + tool-call log) in task Verification block.
- `docs/agent-led-verification-policy.md` §Session continuity extended with ≥3-line "Context pack re-injection" paragraph covering schema, freshness gate, truncation policy.
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T5.1.1 | PreCompact digest script — schema + runtime-state | **TECH-520** | Draft | Author `tools/scripts/claude-hooks/context-pack.sh`: on PreCompact event reads `ia/state/runtime-state.json` for `queued_test_scenario_id`, `last_verify_exit_code`, `last_bridge_preflight_exit_code`; reads `.claude/active-session.json` or `.cursor/active-session.json` for `active_task_id`, `active_stage`; parses active master plan Stage block via same narrow regex `/ship-stage` Phase 0 uses (Stage name + Exit criteria first 5 bullets + Relevant surfaces first 20 lines); emits `.claude/context-pack.md` per extensions doc §2 schema (Active focus + Relevant surfaces + Loaded context sources sections). Add PreCompact hook entry to `.claude/settings.json` hooks array. Add `.claude/context-pack.md` to `.gitignore`. All `jq` calls guarded with `\|\| echo "unknown"`; exit 0 on partial failure; `# Context pack — SCHEMA MISMATCH` marker on malformed inputs. No `claude -p` subprocess. |
| T5.1.2 | Digest script — telemetry + tool-usage + size cap | **TECH-521** | Draft | Extend `context-pack.sh`: append `Last tool outputs (pointers only)` section from `.claude/telemetry/{session-id}.jsonl` (last 10 rows via `tail -10 \| jq -c '{name, exit, ts}'`); if `.claude/tool-usage.jsonl` exists (Stage 4.1 T4.1.1), append `Recent memoized calls` section with top 10 `{tool_name, args_hash_short, result_hash_short, ts}`. Enforce 300-line cap via awk truncation at Recent decisions / Open questions block boundaries (blank-line delimited, not mid-line): drop oldest Recent decisions block first, then oldest Open questions. Emit `_[...truncated N oldest decisions]_` marker when truncation fires. Relevant surfaces never truncated. Soft-guard missing files with `[ -f ... ]` checks. |
| T5.1.3 | SessionStart re-injection + deterministic preamble compat | **TECH-522** | Draft | Extend `tools/scripts/claude-hooks/session-start-prewarm.sh` (Stage 3.1 T3.1.1): after deterministic preamble block + `---` separator, if `-f .claude/context-pack.md` AND pack `ts` header <24 h old, then `cat .claude/context-pack.md`. Stale pack (>24 h) → stderr warning `stale context pack ({age_hours} h old); regenerate via /pack-context`, no stdout emission. Missing pack → silent, no stdout or stderr. Platform-agnostic ts parsing (macOS BSD `date -jf` + GNU `date -d` fallback). Placement in volatile suffix preserves Stage 3.1 D2 deterministic prefix cacheability — verify via diff of two runs. Document re-injection contract in `docs/agent-led-verification-policy.md` §Session continuity (extend sub-section first added by Stage 3.1 T3.1.4). |
| T5.1.4 | Re-orientation integration test + validate:all | **TECH-523** | Draft | Manual integration test per protocol in extensions doc §5 T3.3.4 §Examples: start session on filed task → 2 Read + 2 Edit on 4 distinct source files → `/compact` → inspect `.claude/context-pack.md` (Active focus populated; Relevant surfaces lists all 4 files; ≥1 Recent decision; Last tool outputs lists last 4 actions); resume session (new terminal) → verify SessionStart preamble includes pack content; ask agent "what are you working on?" → confirm model cites active task + stage + ≥2 relevant surfaces with **zero** Read calls on source files before first answer. Screenshot + tool-call log evidence linked in task Verification block. `docs/agent-led-verification-policy.md` §Session continuity updated with full re-injection contract (≥3-line paragraph). `npm run validate:all` green. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-520"
  task_key: "T5.1.1"
  title: "PreCompact digest script — schema + runtime-state"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Author tools/scripts/claude-hooks/context-pack.sh. On PreCompact event reads
    ia/state/runtime-state.json (active_task_id, active_stage, queued_test_scenario_id,
    last_verify_exit_code, last_bridge_preflight_exit_code); parses active master plan
    Stage block via narrow regex; emits .claude/context-pack.md per extensions doc §2
    schema (Active focus + Relevant surfaces + Loaded context sources). Wire PreCompact
    hook entry into .claude/settings.json. Gitignore pack. No claude -p subprocess.
    Graceful partial failure: jq calls guarded with `|| echo "unknown"`, exit 0,
    SCHEMA MISMATCH marker on malformed runtime-state.json.
  depends_on: []
  related:
    - "T5.1.2"
    - "T5.1.3"
    - "T5.1.4"
  stub_body:
    summary: |
      PreCompact hook script emits .claude/context-pack.md digesting runtime-state + active
      Stage block. Core deliverable of Stage 5.1 Phase 1. Shell-only, <200 ms, deterministic.
      Sibling to Stage 3.1 compact-summary.sh (Stop/PostCompact counterpart).
    goals: |
      - tools/scripts/claude-hooks/context-pack.sh executable, shebang bash, set -uo pipefail.
      - Reads ia/state/runtime-state.json + active Stage block via same narrow regex /ship-stage Phase 0 uses.
      - Emits §2-schema sections: Active focus, Relevant surfaces, Loaded context sources.
      - PreCompact hook entry wired in .claude/settings.json.
      - Pack gitignored (session-ephemeral).
      - Graceful partial failure: exit 0 on malformed inputs; SCHEMA MISMATCH marker present.
    systems_map: |
      New: tools/scripts/claude-hooks/context-pack.sh.
      Touches: .claude/settings.json (hooks array), .gitignore.
      Reads: ia/state/runtime-state.json (Stage 3.1 T3.1.2), active master plan Stage block,
      ia/projects/session-token-latency-master-plan.md.
      Writes: .claude/context-pack.md (gitignored).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Digest script + hook wire.
      1. Draft context-pack.sh skeleton (shebang, set -uo pipefail, schema header emit).
      2. Parse runtime-state.json via jq; extract 5 keys; fallback to "unknown" per key.
      3. Regex-extract active Stage block from master plan (Stage header + Exit criteria first 5 + Relevant surfaces first 20 lines).
      4. Emit §2 schema sections to .claude/context-pack.md.
      5. Add PreCompact hook entry to .claude/settings.json.
      6. Add .claude/context-pack.md to .gitignore.
      7. Smoke-test: synthetic runtime-state.json + malformed variant both exit 0.
      8. npm run validate:all.
```

```yaml
- reserved_id: "TECH-521"
  task_key: "T5.1.2"
  title: "Digest script — telemetry + tool-usage + size cap"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Extend context-pack.sh with telemetry tail (last 10 rows of
    .claude/telemetry/{session-id}.jsonl → Last tool outputs section) and optional
    tool-usage block (Stage 4.1 T4.1.1 soft dep → Recent memoized calls, top 10).
    Enforce 300-line size cap via awk truncation at Recent decisions / Open questions
    block boundaries (blank-line delimited); drop oldest Recent decisions first, then
    oldest Open questions; Relevant surfaces never truncated. Emit truncation marker
    `_[...truncated N oldest decisions]_`. Soft-guard missing files via `[ -f ... ]`.
  depends_on: []
  related:
    - "T5.1.1"
    - "T5.1.3"
    - "T5.1.4"
  stub_body:
    summary: |
      Second half of Phase 1 digest authoring. Adds telemetry + memoization sections and
      enforces 300-line cap with block-boundary truncation. Keeps Relevant surfaces
      untouched (hard invariant — cited surfaces must always survive).
    goals: |
      - Last tool outputs section populated from .claude/telemetry/{session-id}.jsonl tail -10.
      - Recent memoized calls section populated from .claude/tool-usage.jsonl if present; omitted silently if absent.
      - 300-line cap enforced via awk at blank-line block boundaries (no mid-line cuts).
      - Drop order: oldest Recent decisions → oldest Open questions.
      - Relevant surfaces block never truncated.
      - Truncation marker emitted when cap fires.
    systems_map: |
      Touches: tools/scripts/claude-hooks/context-pack.sh (extends T5.1.1 deliverable).
      Reads: .claude/telemetry/{session-id}.jsonl (Stage 1 output), .claude/tool-usage.jsonl (Stage 4.1 soft dep).
      Writes: .claude/context-pack.md.
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Telemetry + cap.
      1. Append Last tool outputs section via tail -10 + jq -c pipe.
      2. Guard tool-usage.jsonl with [ -f ... ]; if present, jq top-10 {tool_name, args_hash_short, result_hash_short, ts}.
      3. Author awk block-boundary truncation pass: count lines; if >300, drop oldest Recent decisions block, recount, repeat with Open questions.
      4. Emit `_[...truncated N oldest decisions]_` marker on drop.
      5. Smoke-test: oversized synthetic pack → truncation fires; Relevant surfaces intact.
      6. npm run validate:all.
```

```yaml
- reserved_id: "TECH-522"
  task_key: "T5.1.3"
  title: "SessionStart re-injection + deterministic preamble compat"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Extend tools/scripts/claude-hooks/session-start-prewarm.sh (Stage 3.1 T3.1.1 output):
    after deterministic preamble block + `---` separator, if
    -f .claude/context-pack.md AND pack ts header <24 h old, cat pack content.
    Stale pack (>24 h) → stderr warning, no stdout. Missing pack → silent.
    Platform-agnostic ts parsing (macOS BSD `date -jf` + GNU `date -d` fallback).
    Placement in volatile suffix preserves Stage 3.1 D2 deterministic prefix
    cacheability — verify via diff of two runs. Extend
    docs/agent-led-verification-policy.md §Session continuity sub-section first added
    by Stage 3.1 T3.1.4.
  depends_on: []
  related:
    - "T5.1.1"
    - "T5.1.2"
    - "T5.1.4"
  stub_body:
    summary: |
      Phase 2 re-injection half. Extends session-start-prewarm.sh to cat pack content in
      volatile suffix zone, preserving Stage 3.1 D2 cacheable deterministic prefix.
      Implements 24 h freshness gate + graceful absence.
    goals: |
      - session-start-prewarm.sh cats .claude/context-pack.md after deterministic block + `---` separator.
      - Existence gate (-f) + 24 h freshness gate on pack ts header.
      - Stale pack → stderr warning, no stdout.
      - Missing pack → silent (no stderr, no stdout).
      - BSD + GNU date parsing both supported.
      - Deterministic prefix byte-stable across runs (diff-verified).
      - §Session continuity doc sub-section extended with re-injection contract.
    systems_map: |
      Touches: tools/scripts/claude-hooks/session-start-prewarm.sh (Stage 3.1 T3.1.1 output).
      Touches: docs/agent-led-verification-policy.md §Session continuity.
      Reads: .claude/context-pack.md.
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Re-injection.
      1. Append conditional block to session-start-prewarm.sh: `-f` + ts-age check.
      2. Portable age-compute: try `date -jf` (BSD) first, fallback `date -d` (GNU).
      3. 24 h gate → cat; stale → stderr warning line, no stdout.
      4. Verify deterministic prefix byte-stable: run twice with different pack content; diff preamble up to `---` separator (must be identical).
      5. Extend docs/agent-led-verification-policy.md §Session continuity with re-injection contract.
      6. npm run validate:all.
```

```yaml
- reserved_id: "TECH-523"
  task_key: "T5.1.4"
  title: "Re-orientation integration test + validate:all"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Manual integration test per extensions doc §5 T3.3.4 §Examples protocol.
    Start session on filed task → 2 Read + 2 Edit on 4 distinct source files →
    /compact → inspect .claude/context-pack.md (Active focus populated; 4 Relevant
    surfaces listed; ≥1 Recent decision; Last tool outputs has 4 actions) → resume
    session (new terminal) → verify SessionStart preamble includes pack content →
    ask "what are you working on?" → confirm model cites active task + stage +
    ≥2 relevant surfaces with zero Read calls before first answer. Screenshot +
    tool-call log linked in Verification block. Extend
    docs/agent-led-verification-policy.md §Session continuity with full re-injection
    contract (≥3-line paragraph). npm run validate:all green.
  depends_on: []
  related:
    - "T5.1.1"
    - "T5.1.2"
    - "T5.1.3"
  stub_body:
    summary: |
      Final gating task for Stage 5.1. Validates end-to-end re-orientation UX via manual
      test protocol. Evidence-linked Verification. Docs extended. validate:all green
      unblocks Stage 5.1 closeout.
    goals: |
      - Manual integration test executed per extensions §5 protocol; evidence captured.
      - Pack file content verified: Active focus, Relevant surfaces (all 4 files),
        ≥1 Recent decision, Last tool outputs (4 actions).
      - Resumed session's SessionStart preamble includes pack content.
      - Agent cites active task + stage + ≥2 relevant surfaces with zero pre-answer Reads.
      - docs/agent-led-verification-policy.md §Session continuity has ≥3-line re-injection paragraph.
      - npm run validate:all green.
    systems_map: |
      Touches: docs/agent-led-verification-policy.md §Session continuity.
      Reads: .claude/context-pack.md, session preamble stdout.
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Integration test + docs.
      1. Run test protocol: 2 Read + 2 Edit → /compact → inspect pack → resume → query agent.
      2. Capture screenshot + tool-call log; attach to Verification block.
      3. Extend §Session continuity sub-section with ≥3-line re-injection contract paragraph.
      4. npm run validate:all green.
      5. Stage 5.1 closeout unblocked.
```

#### §Plan Fix — PASS (no drift)

<!-- plan-review verdict — 2026-04-20 — Stage 5.1 (TECH-520/521/522/523) — 12/12 checks pass; downstream continue. -->

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
