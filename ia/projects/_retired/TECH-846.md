---
purpose: "TECH-846 — Big-file Read PreToolUse warn hook (Tier A4 compaction mitigation)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-846 — Big-file Read PreToolUse warn hook (Tier A4 compaction mitigation)

> **Issue:** [TECH-846](../../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-05-08
> **Last updated:** 2026-05-08

## 1. Summary

Add PreToolUse Read interceptor detecting large C# source reads (>800 lines under Assets/**) emitting advisory recommending csharp_class_summary MCP tool first. Non-blocking (exit 0); bypass via BIG_FILE_READ_OK=1 env. Reduces token spend during atomization sweep without blocking legitimate full reads.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Detect Read tool invocations on Assets/**/*.cs files exceeding 800-line threshold.
2. Emit caveman-style advisory pointing at csharp_class_summary MCP tool.
3. Keep hook non-blocking (exit 0) — pure nudge, never gate.
4. Provide BIG_FILE_READ_OK=1 escape hatch env-var for legitimate full reads.
5. Reuse JSON-parsing pattern from bash-denylist.sh (jq primary, sed fallback).

### 2.2 Non-Goals (Out of Scope)

1. Blocking reads — hook always exits 0.
2. Alternative file-size thresholds — 800 lines fixed per Tier A4 spec.
3. Monitoring / analytics — pure advisory, no logging.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | During atomization stages, hook alerts on reads >800 lines | Advisory fires; csharp_class_summary named explicitly |
| 2 | Developer | Can bypass advisory when full read needed | BIG_FILE_READ_OK=1 silences hook |
| 3 | Developer | Hook does not interfere with legitimate large-file reads | Exit 0 always; Read proceeds |

## 4. Current State

### 4.1 Domain behavior

Claude Code hooks run on Tool invocations. PreToolUse hooks receive JSON stdin; they can emit advisory (stdout), escalate (exit 2), or allow (exit 0). Hook patterns exist: bash-denylist.sh (destructive cmd guard), cs-edit-reminder.sh (MCP reminder). Atomization sweep currently reads mega-files (4200+ lines) without guidance toward csharp_class_summary first, incurring high token cost.

### 4.2 Systems map

| Subsystem | File | Role |
|-----------|------|------|
| Hook script | tools/scripts/claude-hooks/big-file-read-warn.sh | New interceptor; reads stdin JSON, checks Assets/**/*.cs glob, line-count, emit advisory |
| Hook wiring | .claude/settings.json | Register PreToolUse matcher for "Read" tool |
| Pattern reference | tools/scripts/claude-hooks/bash-denylist.sh | JSON parse + jq pattern (primary) + sed fallback |
| Reference spec | docs/audit/compaction-loop-mitigation.md | Tier A4 seed + context |

## 5. Proposed Design

### 5.1 Target behavior (product)

1. When agent reads Assets/**/*.cs > 800 lines → hook emits advisory (stderr): "Consider using csharp_class_summary MCP tool first to reduce token spend. Bypass: BIG_FILE_READ_OK=1"
2. Read proceeds (exit 0).
3. When BIG_FILE_READ_OK=1 set → hook silent; Read proceeds.
4. Non-C# reads → hook silent.

### 5.2 Architecture / implementation (agent-owned)

**Script: tools/scripts/claude-hooks/big-file-read-warn.sh**
- Reads stdin JSON (`tool_call` object from PreToolUse hook).
- Extracts `tool_input.file_path` (jq primary: `(.tool_input.file_path // "")`, sed fallback via grep).
- Pattern-match: `Assets/**/*.cs` glob (e.g., `Assets/Scripts/Managers/TerrainManager.cs`).
- Line-count via `wc -l < "${file_path}"`.
- If line-count > 800 AND BIG_FILE_READ_OK unset → emit advisory to stderr.
- Always exit 0.

**Hook wiring in .claude/settings.json**
- Add PreToolUse hook entry: `{"matcher": "Read", "command": "bash tools/scripts/claude-hooks/big-file-read-warn.sh"}`

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-05-08 | 800-line threshold | Tier A4 spec seed; balances alerting vs false positives | 500 / 1000 — revisit if tuning needed |
| 2026-05-08 | Exit 0 always (non-blocking) | Mirrors bash-denylist.sh advisory pattern; never gate legitimate work | Exit 1 on threshold — rejects; too aggressive |
| 2026-05-08 | BIG_FILE_READ_OK escape hatch | Matches bash-denylist.sh env pattern; manual override simple | Config file — harder for rapid iteration |

## 7. Implementation Plan

_pending — plan-author writes phases at N=1._

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Hook detects >800 line C# read + emits advisory | Smoke / manual | Read Assets/Scripts/Managers/GameManagers/TerrainManager.cs (4273 lines) — advisory fires stderr | Exit 0; Read proceeds |
| Hook silent on small C# file | Smoke / manual | Read Assets/Scripts/SomeSmallFile.cs (< 800 lines) — no advisory | Silent; Read proceeds |
| BIG_FILE_READ_OK=1 silences hook | Smoke / manual | BIG_FILE_READ_OK=1 Read Assets/Scripts/Managers/GameManagers/TerrainManager.cs — no advisory | Silent; Read proceeds |
| Hook silent on non-C# Asset reads | Smoke / manual | Read Assets/Textures/SomeImage.png — no advisory | Silent; Read proceeds |
| Hook exits 0 in all cases | Integration | Part of above smoke tests | Never gates Read |
| JSON jq parsing handles missing file_path | Unit / fixture | Malformed stdin JSON — exits 0 gracefully | Sed fallback layer tested |

## 8. Acceptance Criteria

- [ ] tools/scripts/claude-hooks/big-file-read-warn.sh created + executable
- [ ] .claude/settings.json PreToolUse hook wiring added (matcher: Read)
- [ ] Smoke tests pass: >800 line file fires advisory; <800 silent; BIG_FILE_READ_OK=1 silent
- [ ] Non-C# Asset reads do not trigger hook
- [ ] Hook always exits 0 (non-blocking)
- [ ] Advisory prose in caveman style, names csharp_class_summary MCP tool

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| —  | — | — | — |

## 10. Lessons Learned

- —

## §Plan Digest

### §Goal

Add PreToolUse Read hook nudging agents toward `csharp_class_summary` MCP tool when reading large Unity C# files (`Assets/**/*.cs` >800 lines). Non-blocking advisory; reduces token spend during atomization sweep without gating legitimate full reads.

### §Acceptance

- [ ] `tools/scripts/claude-hooks/big-file-read-warn.sh` exists, executable bit set, parses stdin JSON via jq (sed fallback)
- [ ] Hook detects `Assets/**/*.cs` reads, line-counts via `wc -l`, threshold = 800 lines (fixed)
- [ ] Advisory emitted on stderr when threshold crossed AND `BIG_FILE_READ_OK` env unset; names `csharp_class_summary` MCP tool explicitly
- [ ] `BIG_FILE_READ_OK=1` env silences advisory
- [ ] Non-C# / non-Asset / small-file Reads → silent
- [ ] Hook exits 0 in every branch (never blocks Read)
- [ ] `.claude/settings.json` `hooks.PreToolUse` carries new entry with matcher `"Read"` invoking absolute path to script, grouped after existing PreToolUse entries
- [ ] Smoke tests pass: big-file emits advisory, small-file silent, env-bypass silent, non-Assets silent, malformed stdin exits 0

### §Test Blueprint

| Scenario | Stdin | Env | Expected stdout/stderr | Expected exit |
|---|---|---|---|---|
| Big C# Asset read (>800 lines) | `{"tool_input":{"file_path":"<repo>/Assets/Scripts/Managers/GameManagers/TerrainManager.cs"}}` | unset | stderr advisory naming `csharp_class_summary` + `BIG_FILE_READ_OK=1` | 0 |
| Same with bypass | same stdin | `BIG_FILE_READ_OK=1` | empty | 0 |
| Small C# Asset read | `{"tool_input":{"file_path":"<repo>/Assets/Scripts/Tiny.cs"}}` (synthetic <800-line file) | unset | empty | 0 |
| Non-Asset C# read | `{"tool_input":{"file_path":"<repo>/tools/x.cs"}}` | unset | empty | 0 |
| Non-C# Asset read | `{"tool_input":{"file_path":"<repo>/Assets/Textures/foo.png"}}` | unset | empty | 0 |
| Missing file_path key | `{"tool_input":{}}` | unset | empty | 0 |
| File does not exist | `{"tool_input":{"file_path":"<repo>/Assets/Scripts/DoesNotExist.cs"}}` | unset | empty | 0 |

### §Examples

**Advisory prose (stderr):**

```
[territory-developer · big-file-read-warn]
Big C# file read: <file_path> (<line_count> lines, threshold 800).
Consider `csharp_class_summary` MCP tool first — returns class/method/field outline at fraction of token cost.
Bypass: set BIG_FILE_READ_OK=1 to silence (legitimate full read).
```

**Settings.json wiring (new PreToolUse entry, placed after existing Edit|Write|MultiEdit block):**

```jsonc
{
  "matcher": "Read",
  "hooks": [
    {
      "type": "command",
      "command": "/Users/javier/bacayo-studio/territory-developer/tools/scripts/claude-hooks/big-file-read-warn.sh"
    }
  ]
}
```

### §Mechanical Steps

#### Step 1 — Author `tools/scripts/claude-hooks/big-file-read-warn.sh`

**Goal:** New PreToolUse Read hook script. Reads stdin JSON, extracts `tool_input.file_path`, glob-matches `Assets/**/*.cs`, line-counts, emits advisory if >800 + env unset, exits 0 always.

**Edits — before:** file does not exist.

**Edits — after:** new file with shebang `#!/usr/bin/env bash`, `set +e`, jq+sed parse pattern from `bash-denylist.sh:41-45`, glob match `*"/Assets/"*.cs|"Assets/"*.cs` from `cs-edit-reminder.sh:25`, `wc -l < "$file_path"`, threshold `800`, advisory heredoc on stderr, `exit 0`.

**Gate:** `bash -n /Users/javier/bacayo-studio/territory-developer/tools/scripts/claude-hooks/big-file-read-warn.sh` exits 0 (syntax check). Then `chmod +x` the script. Then smoke matrix:

```bash
# big-file → advisory + exit 0
echo '{"tool_input":{"file_path":"/Users/javier/bacayo-studio/territory-developer/Assets/Scripts/Managers/GameManagers/TerrainManager.cs"}}' \
  | /Users/javier/bacayo-studio/territory-developer/tools/scripts/claude-hooks/big-file-read-warn.sh
# expect: stderr advisory, exit 0

# bypass → silent + exit 0
BIG_FILE_READ_OK=1 echo '{"tool_input":{"file_path":"/Users/javier/bacayo-studio/territory-developer/Assets/Scripts/Managers/GameManagers/TerrainManager.cs"}}' \
  | env BIG_FILE_READ_OK=1 /Users/javier/bacayo-studio/territory-developer/tools/scripts/claude-hooks/big-file-read-warn.sh
# expect: empty, exit 0

# non-Assets → silent
echo '{"tool_input":{"file_path":"/Users/javier/bacayo-studio/territory-developer/tools/scripts/claude-hooks/bash-denylist.sh"}}' \
  | /Users/javier/bacayo-studio/territory-developer/tools/scripts/claude-hooks/big-file-read-warn.sh
# expect: empty, exit 0

# missing key → silent
echo '{"tool_input":{}}' \
  | /Users/javier/bacayo-studio/territory-developer/tools/scripts/claude-hooks/big-file-read-warn.sh
# expect: empty, exit 0
```

**STOP:** if any smoke case exits non-zero or wrong stream output → fix script, re-run.

**MCP hints:** none — pure shell.

#### Step 2 — Wire hook into `.claude/settings.json`

**Goal:** Register PreToolUse entry matcher `"Read"` invoking absolute-path command.

**Edits — before:** `hooks.PreToolUse` array contains `{"matcher": "Bash", ...}` and `{"matcher": "Edit|Write|MultiEdit", ...}` entries.

**Edits — after:** append new entry after `Edit|Write|MultiEdit` block:

```jsonc
{
  "matcher": "Read",
  "hooks": [
    {
      "type": "command",
      "command": "/Users/javier/bacayo-studio/territory-developer/tools/scripts/claude-hooks/big-file-read-warn.sh"
    }
  ]
}
```

**Gate:** `python3 -c "import json; json.load(open('/Users/javier/bacayo-studio/territory-developer/.claude/settings.json'))"` exits 0 (valid JSON). Verify via grep that new matcher present:

```bash
grep -c '"matcher": "Read"' /Users/javier/bacayo-studio/territory-developer/.claude/settings.json
# expect: 1
```

**STOP:** if JSON parse fails or grep count ≠ 1 → fix file, re-run.

**MCP hints:** none — pure JSON edit.

#### Step 3 — Final smoke aggregate

**Goal:** Re-run all 6 stdin cases from §Test Blueprint end-to-end; confirm script + wiring agree.

**Edits — before/after:** none (read-only verification).

**Gate:** all 6 cases produce expected stdout/stderr/exit per §Test Blueprint table.

**STOP:** any deviation → triage, fix, re-run.

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Confirm 800-line threshold vs alternative (e.g., 500 / 1000) — seed doc says 800.
2. Should advisory cite specific csharp_class_summary invocation example, or just tool name?
3. Test fixture parity with bash-denylist.test.sh — required at file time or follow-up TECH?

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
