---
purpose: "Analysis — practical mechanisms to capture and analyze the token footprint of long skill chains (ship-stage, release-rollout) for offline Opus review. Companion to session-token-latency-audit-exploration.md."
audience: both
loaded_by: ondemand
slices_via: none
---

# Chain execution token analysis — capture + diagnosis guide

> **Status:** Living doc — add sections as captures accumulate.
> **Created:** 2026-04-20
> **Related:** [`docs/session-token-latency-audit-exploration.md`](session-token-latency-audit-exploration.md) — canonical optimization roadmap (Themes A–F). This doc covers the *capture* layer; that doc covers the *fix* layer. Do not re-propose optimizations here — file against the exploration.
> **Planned harness:** Stage 0 + B7-extended in `session-token-latency-master-plan.md` (post-M8) will build the structured telemetry harness. This doc is the interim how-to until that lands.

---

## 1. Can we capture it?

Short answer: yes — three mechanisms, ranked by effort vs signal fidelity.

| Mechanism | Effort | What you get | Token data |
|---|---|---|---|
| **A — stdout tee** | 30 s | Full text transcript of all agent prose + tool summaries visible to user | No direct token counts; can estimate from output length |
| **B — PostToolUse hook** | 1–2 h | Per-tool-call JSONL: tool name, input JSON, output length, wall-clock ms | Estimate input+output tokens from char counts; accurate enough for hotspot ranking |
| **C — `--output-stats` flag** | 0 (if supported) | Claude Code CLI may expose total tokens + cache breakdown per session | Aggregate only — no per-tool breakdown |

Claude Code doesn't currently expose per-turn token counts inside an agent session (API-level `usage` object is not surfaced to the model or hooks). Mechanisms A and B are the practical path.

---

## 2. Mechanism A — stdout capture (immediate, zero setup)

```bash
claude-personal "/ship-stage ia/projects/{slug}-master-plan.md Stage 1.1" \
  | tee .claude/traces/ship-stage-$(date +%Y%m%d-%H%M%S).txt
```

Creates a timestamped plaintext trace. Contents:
- All agent status lines emitted between tool calls
- Tool call summaries (Edit/Read/Bash shown inline in the terminal stream)
- Final SHIP_STAGE digest JSON

**Limitations.** Does NOT capture:
- Tool input payloads (only summaries)
- MCP response sizes
- Which `CHAIN_CONTEXT` fields were actually read vs ignored by inner agents

**Best for:** rough cost mapping — grep for tool names, count occurrences, estimate per-tool frequency.

**Quick hotspot count from a trace:**

```bash
grep -oP '(mcp__|Bash|Read|Edit|Glob|Grep)\w*' .claude/traces/ship-stage-*.txt | sort | uniq -c | sort -rn | head -20
```

---

## 3. Mechanism B — PostToolUse hook (structured, ~1–2 h to build)

**Audit-mode only — off by default.** Hook is always registered in `.claude/settings.json` but exits immediately unless `CLAUDE_TELEMETRY=1` is set in the calling shell. Zero disk I/O in normal operation.

**To start an audit run:**
```bash
CLAUDE_TELEMETRY=1 claude-personal "/ship-stage ia/projects/{slug}-master-plan.md Stage X.Y"
```

**To stop:** omit the env var. No settings change needed.

Add to `.claude/settings.json` `hooks.PostToolUse` array:

```json
{
  "matcher": "*",
  "hooks": [{
    "type": "command",
    "command": "bash tools/scripts/claude-hooks/telemetry-capture.sh"
  }]
}
```

`tools/scripts/claude-hooks/telemetry-capture.sh`:

```bash
#!/usr/bin/env bash
# Audit-mode telemetry — no-op unless CLAUDE_TELEMETRY=1
# Writes one JSONL line per tool call to .claude/telemetry/{session}.jsonl

[ "${CLAUDE_TELEMETRY}" = "1" ] || exit 0   # ← guard: silent exit in normal mode

SESSION_ID="${CLAUDE_SESSION_ID:-$(cat .claude/current-session-id 2>/dev/null || echo "unknown")}"
OUT_DIR=".claude/telemetry"
mkdir -p "$OUT_DIR"
OUT_FILE="$OUT_DIR/${SESSION_ID}.jsonl"

INPUT="$(cat)"

if command -v jq >/dev/null 2>&1; then
  TOOL_NAME="$(printf '%s' "$INPUT" | jq -r '.tool_name // "unknown"')"
  INPUT_LEN="$(printf '%s' "$INPUT" | jq -r '(.tool_input | tostring | length) // 0')"
  OUTPUT_LEN="$(printf '%s' "$INPUT" | jq -r '(.tool_response | tostring | length) // 0')"
else
  TOOL_NAME="unknown"; INPUT_LEN=0; OUTPUT_LEN=0
fi

INPUT_TOK=$(( INPUT_LEN / 4 ))
OUTPUT_TOK=$(( OUTPUT_LEN / 4 ))
TIMESTAMP="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

printf '{"ts":"%s","session":"%s","tool":"%s","input_tok_est":%d,"output_tok_est":%d}\n' \
  "$TIMESTAMP" "$SESSION_ID" "$TOOL_NAME" "$INPUT_TOK" "$OUTPUT_TOK" \
  >> "$OUT_FILE"
```

**Aggregate after a session:**

```bash
# per-tool total estimated tokens (input + output)
jq -s '
  group_by(.tool) |
  map({tool: .[0].tool, calls: length, total_tok: (map(.input_tok_est + .output_tok_est) | add)}) |
  sort_by(-.total_tok)
' .claude/telemetry/${SESSION_ID}.jsonl
```

**Note:** `CLAUDE_SESSION_ID` env var exposure depends on Claude Code harness version. Fallback: use a UUID written to `.claude/current-session-id` by a `SessionStart` hook and read that here.

---

## 4. Static map — known token hotspots in `/ship-stage`

Even without runtime capture, reading the skill bodies reveals the major loads. This table is a static estimate based on the `/ship-stage` + `domain-context-load` skill files (rev 2026-04-19).

| Step | Source | Token cost estimate | Fires how often |
|---|---|---|---|
| CLAUDE.md cascade (4 files + 5 @-rules) | System-level, always-loaded | **~7–8k** | Once per agent start — multiplied by subagent count |
| `domain-context-load` MCP recipe (5 calls) | Step 1 | **~4–6k** | Once per Stage (good!) |
| `CHAIN_CONTEXT` serialization in dispatch prompts | Step 2.1, 3.1, 3.2 | **~2–4k per subagent dispatch** | N tasks × subagent count |
| Master plan re-read (full file) | Step 0, 2.6, 5 | **~1–3k per read** × (1 + N + 1) reads | N+2 times per chain |
| Spec file reads per task | Step 2.1 (implementer) | **~1–2k per spec** | N times |
| Skill body loading per subagent | Each subagent context | **~2–5k per subagent** | Multiplied by subagent count |
| Verify-loop (full Path A+B) | Step 3.1 | **~5–15k** context + evidence | Once per Stage |
| Code-review Opus dispatch + diff | Step 3.2 | **~3–8k** diff context | Once per Stage |
| Closeout planner + applier pair | Step 3.5 | **~2–4k** per agent | Twice (Opus + Sonnet) |

**Worst case for N=4 tasks, 6-subagent chain (author, implement×4, verify-loop, code-review, audit, closeout×2):**

- CLAUDE.md × 10 agent starts ≈ 70–80k
- CHAIN_CONTEXT serialization × 8 dispatches ≈ 16–32k
- Skill body loading × 10 ≈ 20–50k
- Master plan reads × 6 ≈ 6–18k
- Spec reads × 4 ≈ 4–8k
- MCP recipe (once) ≈ 4–6k
- Verify + code-review + closeout context ≈ 15–35k

**Rough total: ~135–230k tokens** for a 4-task Stage chain.

F9 positive signal (ship-stage Stage 8, 2026-04-19): ~103.1k tokens, 4 tasks — within this range, suggesting the lower end holds when master plan + specs are small.

---

## 5. The cross-subagent caching problem

The `domain-context-load` subskill builds a `cache_block` with `cache_control: {"type":"ephemeral","ttl":"1h"}`. This is the right design — BUT:

**Ephemeral cache requires matching prompt prefix.** When ship-stage dispatches a new subagent (spec-implementer, verify-loop, etc.), the subagent runs in a fresh API context. Its prompt prefix is:
```
[system: CLAUDE.md cascade] [system: skill body] [user: dispatch mission + CHAIN_CONTEXT]
```
This prefix does NOT match the parent ship-stage agent's prefix (which has the conversation history prepended). Result: **cache miss on every subagent dispatch**. The Tier 2 bundle never warms in practice — it warms for reuse within the SAME context, not across subagent dispatches.

Fix direction: the `CHAIN_CONTEXT` block passed in the dispatch prompt itself should be structured as a deterministic cacheable prefix segment (fixed content, no volatile fields like timestamps or task counts). This is the substance of lifecycle-refactor Stage 10 T10.2 (`stable-block.md`) + T10.3 (Tier 2 per-Stage bundle policy). Details in the exploration doc §§ A1/F1.

---

## 6. What an Opus analysis of a trace would reveal

Given a Mechanism B JSONL trace from a 4-task Stage chain, an Opus analysis prompt would look like:

```
You are analyzing a token-usage trace from a `/ship-stage` execution.
File: .claude/telemetry/{session-id}.jsonl

Tasks:
1. Group tool calls by lifecycle phase (context-load, implement, verify, code-review, audit, closeout).
2. Rank tool calls by estimated token cost (input + output).
3. Flag any tool called ≥3x with similar input (repeated context fetches).
4. Flag tools where output_tok_est >> input_tok_est (large response tools).
5. Estimate what fraction of total tokens is "ambient overhead" (CLAUDE.md + skill bodies)
   vs "task-specific" (spec content, MCP domain context).
6. Propose the 3 highest-leverage cuts.
```

Expected findings from current architecture:
- MCP `spec_sections` + `invariants_summary` appear once (good — domain-context-load cached them)
- `Read` on master plan appears N+2 times (each re-read is ~1–3k)
- Bash (`unity:compile-check`) appears N times — each is short (CLI output) but triggers harness overhead
- `mcp__territory-ia__backlog_issue` appears per task in implementer + closeout — possible repeat

---

## 7. Immediate actionable steps

1. **Capture a trace now (Mechanism A — 30s)**

   ```bash
   claude-personal "/ship-stage ia/projects/{slug}-master-plan.md Stage X.Y" \
     | tee .claude/traces/ship-stage-$(date +%Y%m%d-%H%M%S).txt
   ```

   Tool frequency analysis post-run:
   ```bash
   grep -oP '(mcp__\w+|Read|Edit|Bash|Glob|Grep|Write)' .claude/traces/ship-stage-*.txt \
     | sort | uniq -c | sort -rn
   ```

2. **Install hook (Mechanism B — 1–2 h)**  
   File `telemetry-capture.sh` per §3 above. Add PostToolUse entry to `.claude/settings.json`. Hook is a no-op in every normal session; activate only via `CLAUDE_TELEMETRY=1` prefix.

3. **Structured Opus analysis (1 h per trace)**  
   Feed JSONL + prompt from §6 to `claude-personal`. Ask for 3 highest-leverage cuts.

4. **Route optimization proposals to the existing exploration**  
   Any concrete proposals → file under `docs/session-token-latency-audit-exploration.md` §Audit findings or as a new `## Design Expansion` block per the FREEZE / post-M8 sequencing already documented there.

---

## 8. What NOT to do

- **Don't manually estimate per-turn token counts** — Claude Code doesn't expose the API `usage` object inside agent runs. Char-count estimation is close enough for ranking; exact counts require API proxy.
- **Don't add logging to skill SKILL.md bodies** — that adds ambient load. Hook-based logging keeps signal outside the context window.
- **Don't re-propose optimizations here** — they live in `docs/session-token-latency-audit-exploration.md`. This doc is the *how to measure* layer only.

---

## 9. Open questions

1. **Does Claude Code set `CLAUDE_SESSION_ID` in hook subprocess environment?** If not, use a `SessionStart` hook to write a UUID to `.claude/current-session-id` and read that in the PostToolUse hook.
2. **Does `--output-stats` or `--show-cost` flag in claude CLI expose per-session token totals for non-interactive (`-p`) invocations?** Check `claude --help`. If yes, wrap `/ship-stage` in a cost-capturing shell function.
3. **Mechanism B hook overhead** — hook fires on EVERY tool call (including all N×Bash during compile-check). Budget ~2–5 ms per shell fork. For a 100-tool session: 200–500 ms added. Acceptable.
4. **Cross-subagent caching confirmation** — the F9 run (103.1k tokens / 4 tasks) suggests ~25k/task. If Tier 2 cache were actually hitting, theory predicts ~60k total (15k warm + 48k task-specific). Gap ~43k = probable ambient overhead from subagent starts. Capture would confirm.

---

## 10. Relationship to existing work

| Existing item | Where | Status | This doc's angle |
|---|---|---|---|
| F2 — PostToolUse tool-usage.jsonl | `session-token-latency-audit-exploration.md` Theme F | `_pending_` (post-M8) | §3 here is a simpler interim version; full F2 = content-addressable MCP memoisation, higher ambition |
| B7-extended baseline harness | Same doc §Design Expansion Stage 1 | `_pending_` (post-M8) | §3 telemetry hook here is a lightweight precursor |
| Stage 0 baseline collect | Same doc §Implementation Points | `_pending_` (post-M8) | §7 step 1 here is the manual-tee precursor |
| Tier 2 per-Stage cache (`domain-context-load`) | `lifecycle-refactor-master-plan.md` Stage 10 T10.3 | Draft (Q9-gated) | §5 here explains WHY cache currently misses cross-subagent |
