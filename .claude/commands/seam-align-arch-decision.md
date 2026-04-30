---
description: Evaluates a proposed change to an architecture decision record and determines change_kind (amend|supersede|deprecate|noop). Returns aligned_record + rationale matching tools/seams/align-arch-decision/output.schema.json. Invoked by seams_run MCP tool when dispatch_mode=subagent. Returns raw JSON only — no markdown fences.
argument-hint: ""
---

# /seam-align-arch-decision — Plan-covered subagent for the align-arch-decision seam. Receives arch decision record + proposed change, returns AlignArchDecisionOutput JSON (amend|supersede|deprecate|noop).

Drive `$ARGUMENTS` via the [`seam-align-arch-decision`](../agents/seam-align-arch-decision.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, output JSON block (raw JSON output — no fences, no prose wrapper). Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- seams_run dispatch_mode=subagent name=align-arch-decision
## Dispatch

Single Agent invocation with `subagent_type: "seam-align-arch-decision"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/seam-align-arch-decision/SKILL.md`](../../ia/skills/seam-align-arch-decision/SKILL.md) §Hard boundaries.
