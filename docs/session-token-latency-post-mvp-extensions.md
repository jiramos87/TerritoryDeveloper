# Session-token-latency — Post-MVP extensions

> **Companion to:** `ia/projects/session-token-latency-master-plan.md` (Steps 1–4 orchestrator).
> **Exploration source:** `docs/session-token-latency-audit-exploration.md`.
>
> Scope: extensions to the compact-survival hook plane (Stage 3.1 D4) that go beyond last-N-tools re-orientation into **full synthesized-context digest packaging**. Depends on Stage 3.1 landing (`.claude/runtime-state.json` skeleton + Stop/PostCompact hook infrastructure).

---

## 1. Background — what default `/compact` actually does

Default Claude Code `/compact` summarizes the **conversation transcript only**. It does **not** re-read the source files that appeared during the session. Post-compact re-injected context:

- Model-generated conversation summary.
- `CLAUDE.md` + memory files (`MEMORY.md` + referenced per-topic files).
- Slash-command / skill bodies — only when re-invoked post-compact.
- `Referenced file` entries in the compact panel = **paths recorded as pointers**; the file content is gone.

Stage 3.1 D4 already plans `.claude/last-compact-summary.md` with `{active_task_id, active_stage, last_3_tools, ts}` — minimal re-orientation **signal**, not a **pack**. After compaction, an agent still loses:

- Which relevant surfaces were under active read/edit.
- The last N non-trivial decisions.
- Open questions accumulated mid-session.
- Pointers to tool outputs worth recalling (bridge logs, verify exit codes, queued test scenario IDs).

Result: agent re-reads source files + re-derives decisions post-compact. Stage 3.1 solves "which task am I on"; this extension solves "what was I doing on it".

## 2. Proposed extension — Synthesized context pack (D5)

Add a **PreCompact digest script** that, before model summarization, writes a structured markdown file at `.claude/context-pack.md` (session-ephemeral, gitignored) and a **SessionStart re-injection** that cats it into the preamble stdout after the deterministic block. Compact-survival becomes "pack resumes where you were" instead of "pack remembers your task ID".

Output schema for `.claude/context-pack.md` (~200–300 line cap):

```
# Context pack — {ts}

## Active focus
- task: {active_task_id}   stage: {active_stage}
- exit criteria: {stage_exit_lines_truncated_first_5}

## Relevant surfaces
- {path} — {1-line role} (last-op: read|edit @{line_range})
- ...

## Recent decisions (last 5)
- {ts}: {decision} — {rationale}
- ...

## Open questions
- {question} — {blocking|advisory}
- ...

## Last tool outputs (pointers only)
- verify: exit={code} ({ts})   scenario: {queued_test_scenario_id}
- bridge-preflight: exit={code} ({ts})
- Read: exit=0 ({ts})
- ...

## Loaded context sources
- CLAUDE.md
- MEMORY.md
- ia/projects/{active_plan}.md §{active_stage}
- ...
```

Values populated from:
- `.claude/runtime-state.json` (Stage 3.1 T3.1.2) — active task + exit codes.
- `.claude/telemetry/{session-id}.jsonl` (if present) — last 10 tool-call rows.
- `.claude/tool-usage.jsonl` (Stage 4.1 T4.1.1, optional) — if present, extra memoization pointer section.
- Active plan's Stage block parsed via the same narrow regex `/ship-stage` Phase 0 uses.

## 3. Locked decisions

- **Semantic placement as Stage 3.3** of existing Step 3 (extends compact-survival into full digest). `/master-plan-extend` is append-only, so apply-time the skill may land this as **Step 5** instead — human reviewer may manually move the block to Stage 3.3 post-apply if preferred, but the tasks themselves are self-contained either way.
- **Gitignored** (`.claude/context-pack.md`) — session-ephemeral, matches Stage 3.1 `.claude/last-compact-summary.md` pattern. Cross-session memory stays in `MEMORY.md` + master plans.
- **No `claude -p` subprocess** in the PreCompact hook — keeps execution <200 ms and recursion-safe. Digest built from runtime-state.json + jsonl files + Stage block regex via shell + `jq` + `awk` only. Model-backed richer synthesis deferred to an optional `/pack-context` slash command (explicitly out of scope for this Stage).
- **Re-injection via SessionStart stdout**, not PostCompact. PostCompact fires inside the same session where state is still live; SessionStart is the real re-orientation moment (new terminal, resumed process). Cached-preamble compatibility (Stage 3.1 T3.1.1 D2) preserved by emitting pack content only after the deterministic block + `---` separator, in the volatile suffix zone.
- **Size cap 300 lines** — overflow truncates oldest `Recent decisions` entries first, then oldest `Open questions`. `Relevant surfaces` never truncated (it is the primary re-hydration hint; losing it defeats the mechanism).
- **Freshness gate 24 h** — stale pack (>24 h old) triggers stderr warning + skips re-injection rather than misleading the agent.

## 4. Proposed Stage 3.3 — Synthesized context pack (D5)

### Stage header (mirror of Stage 3.1 shape)

**Status:** Draft (tasks _pending_ — not yet filed)

**Pre-conditions:** Stage 3.1 T3.1.2 (`.claude/runtime-state.json` skeleton) + T3.1.3 (compact-summary.sh Stop/PostCompact hook) must be **Done**. Stage 3.3 reuses the runtime-state schema + hook-entry plumbing pattern.

**Objectives:** Extend compact-survival from last-3-tools signal (Stage 3.1) into a full synthesized context pack written on PreCompact event and re-injected on SessionStart. Agents resuming after `/compact` read `.claude/context-pack.md` and recover active focus + surfaces + recent decisions + open questions without re-reading source files. Hook stays shell-only — no model subprocess — to keep compact path fast and deterministic.

**Exit:**

- `.claude/settings.json` hooks array gains a PreCompact entry invoking `tools/scripts/claude-hooks/context-pack.sh`.
- `tools/scripts/claude-hooks/context-pack.sh` (new) reads `.claude/runtime-state.json` + Stage block from active master plan + last 10 entries of `.claude/telemetry/{session-id}.jsonl` + (if present) last 10 entries of `.claude/tool-usage.jsonl`; writes `.claude/context-pack.md` per §2 schema.
- Size cap 300 lines enforced in script; overflow truncates oldest `Recent decisions` then oldest `Open questions` at block boundaries (blank-line delimited).
- Freshness gate in `session-start-prewarm.sh`: pack >24 h old → stderr warning, no stdout emission.
- `session-start-prewarm.sh` (Stage 3.1 T3.1.1) extended to cat `.claude/context-pack.md` after the deterministic preamble block + `---` separator, gated by file existence + freshness.
- `.claude/context-pack.md` added to `.gitignore` (session-ephemeral).
- Re-orientation integration test passes: session → edit → `/compact` → resume → model cites active task + stage + last decisions from pack with zero Read calls on source files before first answer.
- `docs/agent-led-verification-policy.md` §Session continuity extended with pack re-injection contract (schema, freshness gate, truncation policy).
- `npm run validate:all` green.

**Art:** None.

**Relevant surfaces (load when stage opens):**
- `tools/scripts/claude-hooks/session-start-prewarm.sh` (will exist post-Stage 3.1 T3.1.1) — re-injection extension target.
- `tools/scripts/claude-hooks/compact-summary.sh` (will exist post-Stage 3.1 T3.1.3) — sibling hook; `context-pack.sh` is the richer counterpart on PreCompact.
- `.claude/runtime-state.json` (will exist post-Stage 3.1 T3.1.2) — primary digest input.
- `.claude/settings.json` — PreCompact hook entry addition.
- `.claude/telemetry/{session-id}.jsonl` — secondary digest input.
- `.claude/tool-usage.jsonl` (will exist post-Stage 4.1 T4.1.1, optional) — tertiary input.
- `ia/projects/session-token-latency-master-plan.md` — Stage block regex parser target.
- `docs/agent-led-verification-policy.md` §Session continuity — re-injection contract doc target.

**Phases:**

- [ ] Phase 1 — Digest script authoring (schema + runtime-state + telemetry + size cap).
- [ ] Phase 2 — Re-injection + integration test + docs.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.3.1 | PreCompact digest script — schema + runtime-state population | 1 | _pending_ | _pending_ | Author `tools/scripts/claude-hooks/context-pack.sh`: on PreCompact event reads `.claude/runtime-state.json` → extracts `active_task_id`, `active_stage`, `queued_test_scenario_id`, `last_verify_exit_code`, `last_bridge_preflight_exit_code`; parses active master plan Stage block via same narrow regex `/ship-stage` Phase 0 uses (Stage name + Exit criteria first 5 bullets + Relevant surfaces first 20 lines); emits `.claude/context-pack.md` per §2 schema Active focus + Relevant surfaces + Loaded context sources sections. Add PreCompact hook entry to `.claude/settings.json` hooks array. Add `.claude/context-pack.md` to `.gitignore`. No `claude -p` subprocess. Exits 0 on any partial-source-file failure (emits `unknown` placeholders). |
| T3.3.2 | Digest script — telemetry + tool-usage + size cap | 1 | _pending_ | _pending_ | Extend `context-pack.sh`: append `Last tool outputs (pointers only)` section from `.claude/telemetry/{session-id}.jsonl` (last 10 rows via `tail -10 \| jq -c '{name, exit, ts}'`); if `.claude/tool-usage.jsonl` exists (Stage 4.1 T4.1.1), append `Recent memoized calls` section with top 10 `{tool_name, args_hash_short, result_hash_short, ts}`. Enforce 300-line cap via `awk` truncation at Recent decisions / Open questions block boundaries (blank-line delimited, not mid-line): drop oldest Recent decisions block first, then oldest Open questions. Emit `_[...truncated N oldest decisions]_` marker when truncation occurs. Guard all jq selects with `\|\| echo "unknown"`. |
| T3.3.3 | SessionStart re-injection + deterministic preamble compat | 2 | _pending_ | _pending_ | Extend `tools/scripts/claude-hooks/session-start-prewarm.sh` (Stage 3.1 T3.1.1): after deterministic preamble block + `---` separator, if `-f .claude/context-pack.md` AND pack `ts` header <24 h old, then `cat .claude/context-pack.md`. Stale pack (>24 h) → stderr warning `stale context pack ({age_hours} h old); regenerate via /pack-context`, no stdout emission. Missing pack → silent, no stdout or stderr. Placement in volatile suffix preserves Stage 3.1 D2 deterministic prefix cacheability. Document re-injection contract in `docs/agent-led-verification-policy.md` §Session continuity (extend sub-section first added by Stage 3.1 T3.1.4). |
| T3.3.4 | Re-orientation integration test + validate:all | 2 | _pending_ | _pending_ | Manual integration test per protocol documented in T3.3.4 §Test Blueprint: start session on a filed task → perform 2 Read + 2 Edit on distinct source files → invoke `/compact` → inspect `.claude/context-pack.md` (Active focus populated; Relevant surfaces lists all 4 files; ≥1 Recent decision; Last tool outputs lists last 4 actions); resume session (new terminal) → verify SessionStart preamble includes pack content; ask agent "what are you working on?" → confirm model cites active task + stage + ≥2 relevant surfaces with **zero** Read calls on source files before answering. Screenshot + tool-call log evidence linked in task Verification block. `docs/agent-led-verification-policy.md` §Session continuity updated with full re-injection contract (≥3-line paragraph). `npm run validate:all` green. |

## 5. Pre-authored task specs (§Plan Author bulk output)

> Mimics `/author` Opus Stage-scoped bulk pass output (see `.claude/commands/author.md`). Populates §Audit Notes / §Examples / §Test Blueprint / §Acceptance per filed task spec. At `/stage-file` apply time, each block below gets pasted into the corresponding `ia/projects/TECH-{ID}-{slug}.md` spec between §10 Lessons Learned and §Open Questions.

---

### T3.3.1 — PreCompact digest script — schema + runtime-state population

#### §Audit Notes

- **Risk:** PreCompact hook fires before model summarization; if script crashes, compaction may abort depending on hook blocking mode. Mitigation: all `jq` calls wrapped with `|| echo "unknown"`; script `exit 0` on any partial failure; fail-loud only for catastrophic schema mismatch (first line becomes `# Context pack — SCHEMA MISMATCH` so resume-session agent sees failure immediately, not a misleading stale pack).
- **Risk:** Stage block regex from `/ship-stage` Phase 0 assumes stable 6-col task table. If plan schema drifts, regex silently produces malformed pack. Mitigation: regex match fails → emit `SCHEMA MISMATCH` marker rather than best-effort garbage.
- **Invariant touch:** Extends `.claude/settings.json` hooks array (Stage 3.1 T3.1.3 adds Stop/PostCompact entry; T3.3.1 adds PreCompact entry). Distinct events — no hook conflict.
- **Ambiguity — "active master plan" detection:** Read `active_task_id` from runtime-state.json; parse T-key prefix (`T3.3.1` → Stage 3.3 → scan `ia/projects/*.md` for matching `#### Stage 3.3` header). Single-plan assumption acceptable for now — multi-plan sessions emit `# Context pack — MULTI-PLAN` placeholder (deferred resolution).
- **No model invocation** — hook execution stays <200 ms. Model-backed synthesis deferred to optional `/pack-context` slash command (out of scope).

#### §Examples

Example runtime-state.json input:

```json
{
  "active_task_id": "T3.3.1",
  "active_stage": "Stage 3.3",
  "last_verify_exit_code": 0,
  "last_bridge_preflight_exit_code": 0,
  "queued_test_scenario_id": null
}
```

Example emitted `.claude/context-pack.md` (Phase 1 scope — telemetry + size cap land in T3.3.2):

```markdown
# Context pack — 2026-04-20T14:32:15Z

## Active focus
- task: T3.3.1   stage: Stage 3.3
- exit criteria (first 5):
  - tools/scripts/claude-hooks/context-pack.sh authored + executable
  - .claude/settings.json hooks array gains PreCompact entry
  - Schema per §2 emitted (Active focus + Relevant surfaces + Loaded context sources)
  - Exits 0 on missing runtime-state.json (unknown placeholders)
  - .claude/context-pack.md gitignored

## Relevant surfaces
- tools/scripts/claude-hooks/context-pack.sh (new) — primary deliverable
- .claude/settings.json — hooks array extension
- .claude/runtime-state.json — digest input
- ia/projects/session-token-latency-master-plan.md — Stage block regex target
- .gitignore — ephemeral marker

## Loaded context sources
- CLAUDE.md
- MEMORY.md
- ia/projects/session-token-latency-master-plan.md §Stage 3.3
```

Edge case — fresh session, no active_task_id:

```markdown
# Context pack — 2026-04-20T14:32:15Z

## Active focus
- task: unknown   stage: unknown
- exit criteria: unknown (no active task in runtime-state.json)

## Relevant surfaces
_none recorded this session_
```

Edge case — malformed runtime-state.json (non-JSON line, truncated file):

```markdown
# Context pack — SCHEMA MISMATCH
runtime-state.json parse failed at 2026-04-20T14:32:15Z. Resume session should re-read active plan manually.
```

#### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| writes_active_focus_from_runtime_state | runtime-state.json w/ active_task_id=T3.3.1, active_stage=Stage 3.3 | context-pack.md line `- task: T3.3.1   stage: Stage 3.3` present | bash + grep |
| emits_unknown_on_missing_runtime_state | no runtime-state.json | pack contains `task: unknown   stage: unknown`; exit 0 | bash |
| emits_schema_mismatch_on_malformed_json | runtime-state.json w/ truncated JSON | pack first line `# Context pack — SCHEMA MISMATCH`; exit 0 | bash |
| extracts_stage_relevant_surfaces | active_task_id=T3.3.1 + plan file present | Relevant surfaces section contains `tools/scripts/claude-hooks/context-pack.sh` | bash + grep |
| emits_loaded_context_sources | runtime-state + CLAUDE.md + MEMORY.md exist | `Loaded context sources` section lists all three | grep |
| settings_json_has_precompact_entry | .claude/settings.json after Apply | `jq '.hooks.PreCompact[0].hooks[0].command' settings.json` ends with `context-pack.sh` | jq |
| gitignore_contains_pack_entry | .gitignore after Apply | `grep -qE '^\.claude/context-pack\.md$' .gitignore` | bash |
| hook_execution_under_200ms | live PreCompact invocation | wall time <200 ms (timed via `time`) | bash |

#### §Acceptance

- [ ] `tools/scripts/claude-hooks/context-pack.sh` exists, executable (`chmod +x`), shebang `#!/usr/bin/env bash`, `set -uo pipefail` (no `-e` — script must not bail on partial failure).
- [ ] Script reads `.claude/runtime-state.json` via `jq`, emits `Active focus` block per §2 schema (task + stage + first 5 exit-criteria bullets).
- [ ] Script parses active master plan Stage block; emits `Relevant surfaces` section (first 20 lines under the Stage header's Relevant surfaces sub-block).
- [ ] Script emits `Loaded context sources` section listing `CLAUDE.md`, `MEMORY.md`, and active plan §Stage path.
- [ ] Script exits 0 on missing runtime-state.json (unknown placeholders), malformed runtime-state.json (SCHEMA MISMATCH marker), or missing plan file.
- [ ] `.claude/settings.json` hooks array contains PreCompact entry.
- [ ] `.claude/context-pack.md` added to `.gitignore`.
- [ ] Hook execution <200 ms on local dev machine.
- [ ] `npm run validate:all` green.

---

### T3.3.2 — Digest script — telemetry + tool-usage + size cap

#### §Audit Notes

- **Risk:** `.claude/telemetry/{session-id}.jsonl` may not exist on fresh session or if Stage 1 telemetry sweep incomplete. Mitigation: `[ -f ... ]` guard; section omitted with `_none recorded_` placeholder.
- **Risk:** 300-line cap via naive `head -300` produces mid-section cuts that leave dangling markdown. Mitigation: awk truncation at blank-line block boundaries only; drop whole Recent decisions / Open questions entries, never partial.
- **Invariant touch:** Telemetry jsonl schema (expected `{name, exit, ts}` per row from Stage 1 sweep) — if Stage 1 not yet Done, jq selects on non-existent keys produce `null`; guarded with `// "unknown"`.
- **Dependency coupling:** `.claude/tool-usage.jsonl` from Stage 4.1 T4.1.1 — soft (not blocking). Present → extra `Recent memoized calls` section; absent → section omitted silently.
- **Truncation order rationale:** Recent decisions are time-ordered; oldest drop first is the least-surprising policy. Open questions are often still-relevant; keep them as long as possible. Relevant surfaces never truncated — they are the primary re-hydration signal for agents that cannot recover source-file content otherwise.

#### §Examples

Example telemetry tail (15 rows, we keep last 10):

```jsonl
{"name":"Read","exit":0,"ts":"2026-04-20T14:30:11Z"}
{"name":"Edit","exit":0,"ts":"2026-04-20T14:30:42Z"}
{"name":"Bash","exit":1,"ts":"2026-04-20T14:31:05Z"}
{"name":"Read","exit":0,"ts":"2026-04-20T14:31:20Z"}
...
```

Example emitted `Last tool outputs` section:

```markdown
## Last tool outputs (pointers only)
- Read: exit=0 (2026-04-20T14:30:11Z)
- Edit: exit=0 (2026-04-20T14:30:42Z)
- Bash: exit=1 (2026-04-20T14:31:05Z)
- ... (last 10)
```

Size cap example — pack grows to 400 lines → oldest Recent decisions block removed first:

```markdown
## Recent decisions (last 5)
_[...truncated 3 oldest decisions]_
- 2026-04-20T14:28:00Z: Keep hook shell-only — rationale: <200 ms budget
- 2026-04-20T14:29:00Z: Gate re-injection on 24 h freshness — rationale: stale-pack risk
```

Edge case — tool-usage.jsonl absent → `Recent memoized calls` section simply not emitted; the section below (`Loaded context sources`) follows directly.

#### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| appends_telemetry_last_10_rows | telemetry.jsonl w/ 15 rows | Last tool outputs section contains exactly 10 rows (the last 10) | bash + grep -c |
| omits_telemetry_when_absent | no telemetry.jsonl | `_none recorded_` under Last tool outputs, OR section absent entirely | grep |
| appends_tool_usage_when_present | tool-usage.jsonl w/ 5 rows | `Recent memoized calls` section w/ 5 rows | bash + grep -c |
| omits_tool_usage_when_absent | no tool-usage.jsonl | no `Recent memoized calls` section | grep -L |
| enforces_300_line_cap | inflated input → pack grows to ~400 lines pre-cap | wc -l output ≤ 300 | bash |
| truncation_marker_emitted | cap triggers | pack contains `_[...truncated N oldest` | grep |
| truncates_decisions_first | 400-line pack w/ 10 decisions + 5 questions | all 5 questions retained; decisions count <10 | grep -c |
| truncation_at_block_boundaries | cap input | no dangling partial markdown (each remaining decision is a complete bullet w/ ts + rationale) | bash + awk |

#### §Acceptance

- [ ] `context-pack.sh` appends `Last tool outputs (pointers only)` section from telemetry jsonl (last 10 rows).
- [ ] Script appends `Recent memoized calls` section when tool-usage.jsonl exists; silently omitted otherwise.
- [ ] Size cap 300 lines enforced at Recent decisions / Open questions block boundaries (blank-line delimited).
- [ ] Truncation marker `_[...truncated N oldest decisions]_` emitted when cap triggers.
- [ ] Relevant surfaces section never truncated.
- [ ] Section absence handled gracefully — no `jq: error` leaks to pack content.
- [ ] All new §Test Blueprint rows pass.
- [ ] `npm run validate:all` green.

---

### T3.3.3 — SessionStart re-injection + deterministic preamble compat

#### §Audit Notes

- **Risk:** Stage 3.1 T3.1.1 establishes a deterministic cacheable stdout preamble. Unconditionally concatenating `.claude/context-pack.md` would break prefix stability (pack content is per-session volatile). Mitigation: emit only after deterministic block + `---` separator, in the known volatile suffix zone; cached prefix remains byte-stable across sessions.
- **Risk:** Stale pack (previous session's pack, not regenerated) re-injects misleading content. Mitigation: freshness gate reads pack's `ts` header; age >24 h → stderr warning + skip. 24 h threshold chosen to cover overnight resumption but catch abandoned sessions.
- **Risk:** `date -d` flag non-portable between macOS (BSD) and Linux (GNU) — Stage 3.1 T3.1.1 already runs on dev machine's shell. Mitigation: document tested platforms (macOS 14+ BSD date, Ubuntu 22.04+ GNU date); use shell-agnostic timestamp parsing (e.g. `date -jf` fallback for BSD).
- **Invariant touch:** Extends Stage 3.1 T3.1.1 stdout block contract. Ordering preserved: deterministic prefix first, `---` separator, volatile suffix second.
- **UX:** Agent sees pack at SessionStart — not PostCompact. PostCompact runs inside the same session where state is still live in memory; SessionStart is the real re-orientation moment (new terminal, resumed process, post-restart).

#### §Examples

Example `session-start-prewarm.sh` extension (pseudo-diff — full ts-parsing is platform-specific):

```bash
# === Existing deterministic block (Stage 3.1 T3.1.1) ===
cat <<EOF
[territory-developer] MCP: territory-ia v$(node -e "...") | Ruleset: invariants+lifecycle+caveman | Freeze: $(jq -r '.status' ia/state/lifecycle-refactor-migration.json)
EOF

# === NEW — Stage 3.3 T3.3.3 extension ===
pack=".claude/context-pack.md"
if [ -f "$pack" ]; then
  pack_ts=$(head -1 "$pack" | awk -F'— ' '{print $2}')
  pack_epoch=$(date -j -f "%Y-%m-%dT%H:%M:%SZ" "$pack_ts" +%s 2>/dev/null || \
               date -d "$pack_ts" +%s 2>/dev/null || echo 0)
  now_epoch=$(date +%s)
  age_hours=$(( (now_epoch - pack_epoch) / 3600 ))
  if [ "$pack_epoch" -gt 0 ] && [ "$age_hours" -lt 24 ]; then
    echo "---"
    cat "$pack"
  else
    echo "stale context pack (${age_hours} h old); regenerate via /pack-context" >&2
  fi
fi
```

Edge case — no pack at all: nothing emitted after deterministic block; stderr silent. Cached prefix byte-stable.

Edge case — pack present but ts unparseable: `pack_epoch=0` → `age_hours` is huge → stderr warning; no stdout. Graceful.

#### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| emits_fresh_pack | pack written <1 h ago | stdout contains `---` + full pack content | bash |
| skips_stale_pack | pack w/ ts 25 h old | stdout ends at deterministic block; stderr contains `stale context pack` + `25 h` | bash + stderr capture |
| skips_missing_pack | no .claude/context-pack.md | stdout ends at deterministic block; stderr empty | bash |
| skips_unparseable_ts | pack first line `# Context pack — garbage-ts` | stdout ends at deterministic block; stderr warning | bash |
| cached_prefix_byte_stable | two runs w/ different pack content | `diff <(run1 \| head -1) <(run2 \| head -1)` empty | bash |
| platform_date_parsing | macOS BSD + Ubuntu GNU | both parse correctly without error | manual cross-platform |
| docs_session_continuity_extended | docs/agent-led-verification-policy.md after Apply | §Session continuity contains "Context pack re-injection" (≥3 lines) | grep -A 10 |

#### §Acceptance

- [ ] `session-start-prewarm.sh` cats `.claude/context-pack.md` after deterministic block + `---` separator, only when pack fresh (<24 h).
- [ ] Stale pack → stderr warning `stale context pack ({hours} h old); regenerate via /pack-context`, no stdout emission.
- [ ] Missing pack → silent, no stdout, no stderr.
- [ ] Unparseable ts → treated as stale, stderr warning, no stdout.
- [ ] Deterministic prefix byte-stable across runs (Stage 3.1 D2 cacheability preserved — verified by diff).
- [ ] Platform date parsing works on macOS BSD + Ubuntu GNU.
- [ ] `docs/agent-led-verification-policy.md` §Session continuity extended with ≥3-line "Context pack re-injection" paragraph covering schema, freshness gate, truncation policy.
- [ ] `npm run validate:all` green.

---

### T3.3.4 — Re-orientation integration test + validate:all

#### §Audit Notes

- **Risk:** Integration test is manual — `/compact` cannot be triggered from CI. Mitigation: document exact repro steps; screenshot + tool-call log evidence linked in task Verification block per `docs/agent-led-verification-policy.md`.
- **Risk:** Agent may cite pack content but still re-read source files anyway, wasting the optimization. Mitigation: test protocol explicitly requires "zero Read tool calls on source files between resume and first model turn".
- **Risk:** Test is dev-machine-only (requires live Claude Code session + `/compact`). Acceptable — this is a lifecycle UX test, not a runtime correctness test.
- **Invariant touch:** Documentation update to `docs/agent-led-verification-policy.md` §Session continuity — extends sub-section first created by Stage 3.1 T3.1.4. No conflict; complementary content.

#### §Examples

Integration test protocol (manual, documented in `docs/agent-led-verification-policy.md` §Session continuity after this task):

1. **Setup:** `claude-personal /ship TECH-XXX` — start a real filed task. Ensure `.claude/runtime-state.json` populated with `active_task_id`.
2. **Work:** agent performs 2 Read + 2 Edit on 4 distinct source files.
3. **Compact:** user invokes `/compact`.
4. **Pack inspection** — `cat .claude/context-pack.md`:
   - [ ] `Active focus` block populated with `active_task_id` + stage.
   - [ ] `Relevant surfaces` lists all 4 files touched.
   - [ ] ≥1 `Recent decisions` entry (from conversation summary or prior session carry-over).
   - [ ] `Last tool outputs` lists last 4 actions (2 Read + 2 Edit).
   - [ ] `Loaded context sources` references CLAUDE.md + MEMORY.md + active plan §Stage.
5. **Resume:** user opens new terminal → session resumes.
6. **Preamble check:** SessionStart stdout contains deterministic block + `---` + pack content (verified via stderr/stdout capture or log scrape).
7. **Agent probe:** user asks "what are you working on?"
8. **Expected:** agent answers citing `active_task_id` + stage + ≥2 relevant surfaces **before** making any Read tool call on source files. Verify via tool-call log inspection — 0 Read calls on source files in the first turn after resume.

Evidence artifacts attached to task Verification block:
- Screenshot of `.claude/context-pack.md` content post-compact.
- Tool-call log showing resume-turn with 0 Read calls before first model response.
- `npm run validate:all` terminal output.

#### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| manual_reorientation_pass | live session → 2 Read + 2 Edit → /compact → resume → probe question | agent cites active_task_id + stage + ≥2 relevant surfaces; 0 Read calls on source files before first answer | manual + tool-call log + screenshot |
| pack_content_complete_post_compact | /compact on populated session | pack has all 6 §2 schema sections populated | manual inspection |
| session_continuity_docs_extended | docs/agent-led-verification-policy.md | §Session continuity contains new "Context pack re-injection" paragraph ≥3 lines covering schema + freshness + truncation | grep -A 15 |
| stage_31_reorientation_no_regression | existing Stage 3.1 test: .claude/last-compact-summary.md still written on Stop/PostCompact | file exists, contains active_task_id + last_3_tools | bash |
| validate_all_green | repo state after all 4 tasks Done | `npm run validate:all` exit 0 | npm |

#### §Acceptance

- [ ] Manual integration test passed; screenshot + tool-call log evidence linked in task Verification block.
- [ ] `docs/agent-led-verification-policy.md` §Session continuity documents full re-injection contract (pack schema, freshness gate, truncation policy, size cap).
- [ ] Stage 3.1 re-orientation test still passes (no regression on `.claude/last-compact-summary.md`).
- [ ] `npm run validate:all` green.

---

## 6. Open questions

- **`/pack-context` slash command scope** — optional model-backed richer synthesis. In-scope for Stage 3.3 or deferred to a separate Stage 3.4? Current draft defers — hook stays shell-only + fast + deterministic; richer synthesis is a separate product surface. Deferring leaves a clean hook-plane boundary.
- **Last-compaction-summary capture** — should `.claude/context-pack.md` also capture the last model-generated summary (the one `/compact` produced)? Reaching into internal Claude Code state to grab that output may not be supported by the PreCompact hook event payload. Needs verification of Claude Code's PreCompact hook contract (what env/stdin/args the hook receives). If unsupported, accept the limitation; if supported, add a §Previous compaction summary section.
- **Tool-usage.jsonl dependency strictness** — currently soft (section omitted if Stage 4.1 T4.1.1 not yet Done). Should it be hard-gated? Current choice keeps Stage 3.3 shippable independent of Stage 4.1.
- **Multi-plan session detection** — if user works across multiple master plans in one session, which one is "active"? Current draft emits `MULTI-PLAN` placeholder. Defer richer detection (e.g. track recent file edits per-plan) unless real usage shows it's needed.

## 7. Handoff

Next step (from this draft, once reviewed + approved):

```
claude-personal "/master-plan-extend ia/projects/session-token-latency-master-plan.md docs/session-token-latency-post-mvp-extensions.md"
```

Skill will:
- **Phase 0** — Read + validate both files. Extensions-doc shape accepted (title matches `{slug}-post-mvp-extensions.md`, companion to named master plan).
- **Phase 1** — Start-number resolution. Last existing Step = 4, so default = Step 5. Duplication gate vs existing step names: none collide.
- **Phase 2** — MCP context + surface-path pre-check. Tooling-only plan → `invariants_summary` skipped per skill rule. Glob of listed Relevant surfaces flags `(new)` paths correctly (context-pack.sh, settings.json PreCompact entry).
- **Phase 3** — User confirm on step ordering + name ("Synthesized context pack" — proposed as single-stage Step).
- **Phase 4–5** — Step + Stage decomposition. Already done in §4 above — skill consumes directly.
- **Phase 6** — Cardinality gate: 4 tasks fits 2–6 bound per phase. PASS.
- **Phase 7** — Persist: append new Step 5 block to plan file immediately before the `---` preceding `## Orchestration guardrails`. Header sync: append `docs/session-token-latency-post-mvp-extensions.md` to `**Exploration source:**` Read-first list.
- **Phase 7b** — `npm run progress` regen (deterministic; non-blocking on failure).
- **Phase 8** — Handoff to:

```
claude-personal "/stage-file ia/projects/session-token-latency-master-plan.md Stage 5.1"
```

`/stage-file` at N=4:
- Creates `ia/projects/TECH-{auto-id}-context-pack-*.md` per row (4 specs).
- Calls `gh issue create` for each → back-fills Issue column with `#{num}` references.
- Auto-invokes `/author ia/projects/session-token-latency-master-plan.md Stage 5.1` as the multi-task post-apply step. `/author` pastes §5 pre-authored §Plan Author content from THIS doc into each spec between §10 Lessons Learned and §Open Questions.
- Next: `/plan-review ia/projects/session-token-latency-master-plan.md Stage 5.1` to verify canonical-term fold + §Plan Author integrity.
- Then `/ship-stage ia/projects/session-token-latency-master-plan.md Stage 5.1` to execute the chain (kickoff → implement → verify-loop → closeout) per task.

**If user wants the block to semantically live at Stage 3.3 instead of Step 5:** after Phase 7 apply, manually move the persisted block from Step 5 position into Step 3 as sub-section `#### Stage 3.3 — Synthesized context pack (D5)`, renumber internal T-keys from `T5.1.N` → `T3.3.N`, and re-run `npm run progress`. This is a one-time manual adjustment outside the skill's guardrails.

## 8. Scope guardrail

Locked decisions for Stage 3.3 (do not reopen during v1):

- PreCompact hook is **shell-only** — no `claude -p` subprocess, no model invocation.
- Pack remains **session-ephemeral** (gitignored). Cross-session memory stays in `MEMORY.md` + master plans.
- **No changes to existing Stage 3.1 tasks** — this Stage strictly extends the hook plane; does not amend `compact-summary.sh` or re-scope the Stop/PostCompact contract.
- **Model-backed synthesis** (future `/pack-context` slash command) explicitly out of scope; candidate for a separate post-MVP extensions row if real usage shows the shell-only digest is insufficient.
- **Freshness gate fixed at 24 h** — no per-user configurability in v1; revisit only if real sessions routinely run >24 h.

Candidate extensions surfaced during Stage 3.3 implementation (bug fixes, refactors exposed by real use) land here as new rows under matching sections — do not silently promote into Stage 3.3 scope mid-flight.
