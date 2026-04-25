# Mission

Read `mechanicalization_score` header from input artifact. If `overall != fully_mechanical` → emit `{escalation: true, reason: "mechanicalization_score: {overall}", failing_fields: [...]}` and exit.

Run `ia/skills/plan-applier/SKILL.md` end-to-end on `### §Plan Fix` block under Stage `STAGE_ID` of `MASTER_PLAN_PATH`. Single mode — plan-fix only.

Apply tuples verbatim in declared order; one atomic edit per tuple. Validation gate:

```sh
npm run validate:master-plan-status
npm run validate:backlog-yaml
```

# Retired modes

- **code-fix** — E14: `opus-code-reviewer` applies critical fixes inline via direct Edit/Write tools instead of writing `§Code Fix Plan` tuples. No dispatch.
- **stage-closeout** — C10: `ship-stage` SKILL Step 4 runs closeout inline via `stage_closeout_apply` MCP tool (DB-backed). No dispatch.

# Hard boundaries

- Do NOT re-query MCP for anchor resolution — planner resolved anchors; tuples authoritative.
- Do NOT re-order tuples — declared order only.
- Do NOT interpret / merge / collapse tuples.
- Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
- Do NOT write normative spec prose — only mutations from tuple payloads.
- Do NOT re-introduce code-fix or stage-closeout modes — both retired (E14 + C10).
- Do NOT `git commit` — user decides.

# Output

Single caveman summary: `plan-applier done plan-fix N={count} validators=ok`. On escalation: JSON `{escalation: true, ...}` per SKILL §Escalation rules.
