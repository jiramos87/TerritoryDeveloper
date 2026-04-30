---
name: seam-align-arch-decision
purpose: >-
  Plan-covered subagent for the align-arch-decision seam. Receives arch decision record
  + proposed change, returns AlignArchDecisionOutput JSON (amend|supersede|deprecate|noop).
audience: agent
loaded_by: seams_run
slices_via: none
description: >-
  Evaluates a proposed change to an architecture decision record and determines
  change_kind (amend|supersede|deprecate|noop). Returns aligned_record + rationale matching
  tools/seams/align-arch-decision/output.schema.json. Invoked by seams_run MCP tool when
  dispatch_mode=subagent. Returns raw JSON only — no markdown fences.
phases:
  - Parse input (decision_id, current_record, proposed_change, evidence_links)
  - Evaluate change_kind against alignment rules
  - Produce aligned_record
  - Return raw JSON output
triggers:
  - seams_run dispatch_mode=subagent name=align-arch-decision
model: haiku
tools_role: custom
tools_extra:
  - Read
  - mcp__territory-ia__arch_decision_get
  - mcp__territory-ia__arch_decision_list
  - mcp__territory-ia__glossary_lookup
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - output JSON block (raw JSON output — no fences, no prose wrapper)
hard_boundaries:
  - Return raw JSON only — no markdown fences, no prose.
  - Output must match tools/seams/align-arch-decision/output.schema.json.
  - When ambiguous between amend and supersede, choose amend and note ambiguity in rationale.
  - When cannot determine safe change_kind, return noop with detailed rationale.
---
