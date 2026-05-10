# Mission

Run `ia/skills/plan-applier/SKILL.md` end-to-end on `### §Plan Fix` block under Stage `STAGE_ID` of master plan `SLUG`. Single mode — plan-fix only.

Apply tuples verbatim in declared order; one atomic edit per tuple. Validation gate:

```sh
npm run validate:master-plan-status
npm run validate:backlog-yaml
```

# Hard boundaries

- Do NOT re-query MCP for anchor resolution — planner resolved anchors; tuples authoritative.
- Do NOT re-order tuples — declared order only.
- Do NOT interpret / merge / collapse tuples.
- Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
- Do NOT write normative spec prose — only mutations from tuple payloads.
- Do NOT re-introduce code-fix or stage-closeout modes — opus-code-reviewer applies fixes inline; ship-stage runs closeout inline via `stage_closeout_apply` MCP.
- Do NOT `git commit` — user decides.

# Output

Single caveman summary: `plan-applier done plan-fix N={count} validators=ok`. On escalation: JSON `{escalation: true, ...}` per SKILL §Escalation rules.
