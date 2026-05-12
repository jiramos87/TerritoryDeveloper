---
description: Use when the user wants a state-of-art survey on an external topic (TOPIC) combined with a frozen-in-time audit of the repo's existing implementation of the same subject area, followed by critique and N improvement methodologies sourced from the research. Output = one Markdown doc with 4 ordered sections (Findings · Audit · Critique · Exploration). Does NOT create master plan, BACKLOG row, exploration seed, or commit. Triggers: "/topic-research-survey {TOPIC}", "research + audit + critique + improve {TOPIC}", "state-of-art survey for {SUBSYSTEM}".
argument-hint: "{TOPIC} [--queries q1,q2,...] [--as-of YYYY-MM] [--audit-scope "{repo subsystem}"] [--out docs/research/{slug}.md] [--n-improvements N] (e.g. "unity ui-as-code" --queries "unity procedural-ui,agentic unity ui" --as-of 2026-05 --audit-scope "ui-as-code system" --n-improvements 10)"
---

# /topic-research-survey — Produce a single research doc that pairs heavy external web research on a topic with an independent in-repo audit of the same subject area, then layers a strengths/weaknesses critique and a list of N methodology-named improvement proposals. Section isolation is the core invariant: findings, audit, critique, and improvements are written in order and never bleed across phases.

Drive `$ARGUMENTS` via the [`topic-research-survey`](../agents/topic-research-survey.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code blocks, commits, security/auth, verbatim web quotes (research findings retain original wording when quoted), structured MCP payloads, external URLs and citation lines. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /topic-research-survey
- research and audit and critique
- state-of-art survey
- external research plus repo audit
- propose improvements from web research
## Dispatch

Single Agent invocation with `subagent_type: "topic-research-survey"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/topic-research-survey/SKILL.md`](../../ia/skills/topic-research-survey/SKILL.md) §Hard boundaries.
