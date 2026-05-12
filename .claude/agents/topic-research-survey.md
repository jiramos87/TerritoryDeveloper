---
name: topic-research-survey
description: Use when the user wants a state-of-art survey on an external topic (TOPIC) combined with a frozen-in-time audit of the repo's existing implementation of the same subject area, followed by critique and N improvement methodologies sourced from the research. Output = one Markdown doc with 4 ordered sections (Findings · Audit · Critique · Exploration). Does NOT create master plan, BACKLOG row, exploration seed, or commit. Triggers: "/topic-research-survey {TOPIC}", "research + audit + critique + improve {TOPIC}", "state-of-art survey for {SUBSYSTEM}".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, WebSearch, WebFetch, mcp__territory-ia__list_specs, mcp__territory-ia__arch_decision_list, mcp__territory-ia__csharp_class_summary, mcp__territory-ia__research_doc_scaffold, mcp__territory-ia__web_findings_dedupe, mcp__territory-ia__audit_scope_resolve, mcp__territory-ia__arch_decision_conflict_scan, mcp__territory-ia__improvement_proposal_lint, mcp__territory-ia__research_doc_to_exploration_seed
model: inherit
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code blocks, commits, security/auth, verbatim web quotes (research findings retain original wording when quoted), structured MCP payloads, external URLs and citation lines. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
# Mission

Run [`ia/skills/topic-research-survey/SKILL.md`](../../ia/skills/topic-research-survey/SKILL.md) end-to-end for `$ARGUMENTS`. Produce a single research doc that pairs heavy external web research on a topic with an independent in-repo audit of the same subject area, then layers a strengths/weaknesses critique and a list of N methodology-named improvement proposals. Section isolation is the core invariant: findings, audit, critique, and improvements are written in order and never bleed across phases.

# Recipe

Follow `ia/skills/topic-research-survey/SKILL.md` end-to-end. Phase sequence:

1. Inputs + recency anchor + scope lock
2. External research (broad multi-query web sweep)
3. Findings section (pure, no comparison)
4. Repo audit (independent, no findings reference)
5. Critique (strengths + weaknesses, audit-only basis)
6. Exploration (N methodology-named improvements sourced from §Findings)
7. Persist + handoff

# Hard boundaries

- §Findings phase writes only what external sources say. Do NOT reference repo state, our system, or 'we'.
- §Audit phase writes only what is in the repo. Do NOT reference findings, web sources, or external methodology names.
- §Critique phase reasons over §Audit alone. Strengths/weaknesses are observable from the repo, not aspirational versus findings.
- §Exploration phase is the only place where §Findings cross-pollinates §Audit. Each improvement names a methodology from §Findings + cites the source line.
- If TOPIC is ambiguous or AUDIT_SCOPE cannot be resolved to a repo subsystem → STOP, ask user. Do NOT guess.
- If WebSearch / WebFetch quota or access fails on >50% of queries → STOP, report degraded coverage, ask user to retry or narrow.
- Do NOT create master plan, BACKLOG row, exploration-doc seed, or arch_decision. User triggers next step after review.
- Do NOT commit. Doc lives unstaged until user decides.
- Do NOT collapse §Findings and §Audit. Section isolation is the core invariant.

See [`ia/skills/topic-research-survey/SKILL.md`](../../ia/skills/topic-research-survey/SKILL.md) §Hard boundaries for full constraints.
