### Stage 6 — Dogfood Cycle (Phase E) / First Retrospective + Meta-Dogfood

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Execute `design-explore` retrospective; iterate prompt/schema if signal weak; run meta-dogfood; record all outcomes in §Changelog entries; mark orchestrator Final.

**Exit:**

- `ia/skills/design-explore/train-proposal-{DATE}.md` present; non-empty; ≥1 friction point.
- `skill-train/SKILL.md §Changelog` carries `source: dogfood-result` entry for both target runs.
- Any schema/prompt iterations recorded in `skill-train/SKILL.md §Changelog` with `source: iteration`.
- `npm run validate:all` exits 0. Orchestrator Status: Final.
- Phase 1 — Dogfood readiness check + first retrospective run.
- Phase 2 — Iterate if weak + meta-dogfood + orchestrator final.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Dogfood readiness check | **TECH-604** | Draft | Verify ≥1 real run of `design-explore` (or any wired skill) has occurred since Phase-N-tail wiring (Step 2) landed, generating a `source: self-report` §Changelog entry. If none: trigger a short `design-explore` invocation on an existing stub doc to accumulate signal. Document readiness note in `skill-train/SKILL.md §Changelog`. |
| T6.2 | Run /skill-train design-explore | **TECH-605** | Draft | Execute `/skill-train design-explore`. Capture: proposal file path, friction-count, severity. Review proposal — judge signal quality against known §Changelog entries or observed run behavior. Record outcome (`strong`/`weak`/`partial`) in `design-explore/SKILL.md §Changelog` as `source: dogfood-result`. |
| T6.3 | Iterate schema + prompt if weak | **TECH-606** | Draft | If T6.2 outcome = `weak` or `partial`: identify gap (aggregation threshold off? Phase 2 summarization too vague? diff too coarse?). Edit `skill-train/SKILL.md` Phase 2 or 3; re-run `/skill-train design-explore`; verify stronger signal. Append `source: iteration` §Changelog entry noting change. If outcome = `strong`: skip edits; append `source: dogfood-result` entry confirming first-run success. |
| T6.4 | Meta-dogfood + orchestrator final | **TECH-607** | Draft | Run `/skill-train skill-train`. Capture proposal if generated; record in `skill-train/SKILL.md §Changelog source: dogfood-result`. Apply any self-proposed patches that survive user review. Run `npm run validate:all`; exit 0. Flip this orchestrator to `Status: Final`. |

### §Plan Fix — PASS (no drift)

> plan-review re-entry exit 0 — Stage 6 Task specs aligned. Prior 2 tuples already reflected in file state (no-ops). No new drift found. Downstream pipeline continue.

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Verify ≥1 real design-explore run since Phase-N-tail wiring landed; trigger short invocation if none; document readiness in skill-train/SKILL.md §Changelog"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Read design-explore/SKILL.md §Changelog for any source: self-report entry added after
    Stage 3 wiring (TECH-430). If absent, trigger short design-explore invocation on any
    existing stub doc to accumulate signal. Append dogfood-readiness note to
    skill-train/SKILL.md §Changelog (source: dogfood-result or source: readiness-note).
  depends_on: []
  related: []
  stub_body:
    summary: |
      Readiness gate before first skill-train retrospective run. Confirms design-explore
      has a real §Changelog self-report entry since Phase-N-tail wiring landed (Stage 3).
      Triggers signal-accumulation run if §Changelog is empty.
    goals: |
      - Read ia/skills/design-explore/SKILL.md §Changelog; check for source: self-report entry.
      - If none present: invoke design-explore on an existing stub doc to generate signal.
      - Confirm §Changelog entry created by that run (friction_types[] populated or empty clean run).
      - Append readiness note to skill-train/SKILL.md §Changelog documenting outcome.
    systems_map: |
      Primary files: ia/skills/design-explore/SKILL.md (§Changelog read), ia/skills/skill-train/SKILL.md (§Changelog write).
      Related: .claude/agents/skill-train.md, .claude/commands/skill-train.md.
    impl_plan_sketch: |
      Phase 1 — Readiness check:
        1. Read design-explore/SKILL.md §Changelog tail; scan for source: self-report entries post-wiring.
        2. If found: proceed to T6.2 directly; append readiness note (found signal).
        3. If not found: select an existing exploration doc under docs/; invoke /design-explore {path}
           to produce at least one §Changelog entry (friction or clean).
        4. Append readiness note to skill-train/SKILL.md §Changelog.

- reserved_id: ""
  title: "Execute /skill-train design-explore; capture proposal path + friction-count + severity; judge signal quality; record outcome in design-explore/SKILL.md §Changelog as source: dogfood-result"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Invoke /skill-train design-explore subagent (Opus). Read generated train-proposal-{DATE}.md;
    evaluate signal quality against known §Changelog entries or observed run behavior.
    Classify outcome as strong / weak / partial. Append source: dogfood-result entry to
    design-explore/SKILL.md §Changelog noting classification.
  depends_on: []
  related: []
  stub_body:
    summary: |
      First real skill-train retrospective run targeting design-explore skill.
      Produces ia/skills/design-explore/train-proposal-{DATE}.md; evaluates proposal signal quality.
      Records dogfood outcome in design-explore/SKILL.md §Changelog.
    goals: |
      - Run /skill-train design-explore (Opus subagent dispatch).
      - Capture: proposal file path, friction-count aggregated, severity field value.
      - Review proposal diff hunks against known friction in §Changelog entries.
      - Judge signal: strong (clear actionable diff) / weak (vague or trivial) / partial (mixed).
      - Append source: dogfood-result §Changelog entry to design-explore/SKILL.md with outcome classification.
    systems_map: |
      Primary files: ia/skills/design-explore/SKILL.md (§Changelog append), ia/skills/design-explore/train-proposal-{DATE}.md (created by skill-train).
      Agent: .claude/agents/skill-train.md (Opus). Command: .claude/commands/skill-train.md.
      Skill body: ia/skills/skill-train/SKILL.md Phase 0–5.
    impl_plan_sketch: |
      Phase 1 — Retrospective run:
        1. Invoke claude-personal "/skill-train design-explore".
        2. Wait for proposal file write; note path + friction-count.
        3. Read proposal; evaluate quality (actionable hunks vs vague summary).
        4. Classify outcome; append §Changelog entry to design-explore/SKILL.md.

- reserved_id: ""
  title: "If T6.2 outcome weak or partial: identify gap, edit skill-train/SKILL.md Phase 2 or 3, re-run /skill-train design-explore, verify stronger signal, append source: iteration §Changelog entry; if strong: append source: dogfood-result confirming first-run success"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Conditional on T6.2 classification. Weak/partial → diagnose gap (threshold? aggregation?
    diff granularity?); edit skill-train/SKILL.md Phase 2 or 3 body; re-run; judge again.
    Strong → skip edits; append confirmation entry only. All changes appended to
    skill-train/SKILL.md §Changelog as source: iteration or source: dogfood-result.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Iterative refinement pass on skill-train Phase 2/3 if first retrospective signal is weak.
      Edits skill-train/SKILL.md aggregation or diff logic; re-runs /skill-train design-explore.
      Appends source: iteration §Changelog entries for each edit cycle.
    goals: |
      - Evaluate T6.2 classification (received from T6.2 §Changelog entry or review notes).
      - If strong: append source: dogfood-result to skill-train/SKILL.md §Changelog; proceed to T6.4.
      - If weak/partial: diagnose gap category (threshold, Phase 2 grouping, Phase 3 diff granularity).
      - Edit skill-train/SKILL.md (Phase 2 or Phase 3 body) to address gap.
      - Re-run /skill-train design-explore; re-evaluate; repeat until strong or max 2 iterations.
      - Append source: iteration §Changelog entry per cycle noting change + outcome.
    systems_map: |
      Primary files: ia/skills/skill-train/SKILL.md (Phase 2/3 edits, §Changelog append),
        ia/skills/design-explore/SKILL.md (§Changelog read for signal context),
        ia/skills/design-explore/train-proposal-{DATE}.md (re-generated per iteration).
      Agent: .claude/agents/skill-train.md (Opus). Command: .claude/commands/skill-train.md.
    impl_plan_sketch: |
      Phase 1 — Signal evaluation + conditional iteration:
        1. Read T6.2 outcome from design-explore/SKILL.md §Changelog (last source: dogfood-result entry).
        2. If strong: append confirmation to skill-train/SKILL.md §Changelog; skip to Phase 2.
        3. If weak/partial: read skill-train/SKILL.md Phase 2 (aggregation) + Phase 3 (diff synthesis).
        4. Edit identified gap; re-run /skill-train design-explore; re-evaluate.
        5. Append source: iteration entry after each cycle.
      Phase 2 — Close iteration loop:
        6. Confirm signal upgraded to strong or document final state if max iterations reached.

- reserved_id: ""
  title: "Run /skill-train skill-train (meta-dogfood); record proposal in skill-train/SKILL.md §Changelog source: dogfood-result; apply user-approved patches; run npm run validate:all; flip orchestrator to Status: Final"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Meta-dogfood: skill-train retrospects itself. Invoke /skill-train skill-train; capture
    proposal if generated; user reviews + approves any patches; apply approved patches to
    skill-train/SKILL.md. Run npm run validate:all; confirm exit 0. Update orchestrator
    skill-training-master-plan.md Status line from Draft → Final.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Meta-dogfood run: skill-train retrospects its own SKILL.md via /skill-train skill-train.
      User reviews generated proposal; approved patches applied. Orchestrator flipped Final after validate:all passes.
    goals: |
      - Invoke /skill-train skill-train; capture proposal file path + friction-count.
      - User reviews proposal; approve or reject each hunk.
      - Apply approved patches to skill-train/SKILL.md.
      - Append source: dogfood-result entry to skill-train/SKILL.md §Changelog.
      - Run npm run validate:all; confirm exit 0.
      - Edit skill-training-master-plan.md Status line → Status: Final; flip all Stage 6 task rows Done (archived).
    systems_map: |
      Primary files: ia/skills/skill-train/SKILL.md (retrospect target + patch destination + §Changelog),
        ia/projects/skill-training-master-plan.md (Status flip).
      Agent: .claude/agents/skill-train.md (Opus). Command: .claude/commands/skill-train.md.
      Validator: npm run validate:all (package.json).
    impl_plan_sketch: |
      Phase 1 — Meta-dogfood:
        1. Invoke /skill-train skill-train.
        2. Capture proposal; present to user for hunk-by-hunk review.
        3. Apply approved hunks to skill-train/SKILL.md; discard rejected hunks.
        4. Append source: dogfood-result §Changelog entry.
      Phase 2 — Close + validate:
        5. Run npm run validate:all; confirm exit 0; fix any failures inline.
        6. Edit orchestrator Status: Final; flip T6.1–T6.4 rows to Done (archived).
```

---
