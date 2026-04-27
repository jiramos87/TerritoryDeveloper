---
purpose: "§Plan Digest section template fragment for task specs (stage-authoring pass). Relaxed shape — intent over verbatim code."
audience: agent
loaded_by: ondemand
slices_via: none
---

<!--
  §Plan Digest section template — RELAXED shape.

  Authoring rule: digester resolves picks, paths, names, and design intent — does NOT lay down
  verbatim code. Implementer locates exact byte positions against current HEAD and decides
  operation type, helper extraction, micro-edit sequencing, and test input shape.

  Per-section soft byte caps (warn-only — emit `n_section_overrun` counter; do NOT abort):
    §Goal               ≤ 400 B
    §Acceptance         ≤ 1500 B
    §Pending Decisions  ≤ 1500 B
    §Implementer Lat.   ≤  800 B
    §Work Items         ≤ 2000 B
    §Test Blueprint     ≤ 1000 B
    §Invariants & Gate  ≤  800 B
    total target        ≈ 8 KB

  Backwards compat: legacy digests with `§Mechanical Steps` (verbose Edit tuples + before/after
  blocks) remain valid and continue to ship via `/ship-stage` Pass A unchanged. New authoring
  uses the shape below.
-->

## §Plan Digest

### §Goal

<!-- 1–2 sentences — task outcome in product / domain terms. Glossary-aligned. Soft cap ≤400 B. -->

### §Acceptance

<!-- Sharp behavior contract. Each row = one observable behavior code-review + verify-loop will gate on. Soft cap ≤1500 B. -->

- [ ] {observable behavior 1}
- [ ] {observable behavior 2}

### §Pending Decisions

<!-- Picks RESOLVED by the digester. Each row = a design pivot the implementer would otherwise
     have to negotiate. Capture the choice + rationale; do NOT include code. Soft cap ≤1500 B. -->

- {decision name}: {choice chosen} — rationale: {why}
- {path or symbol name}: {resolved value}

### §Implementer Latitude

<!-- Picks DEFERRED to the implementer. Each row = an explicit freedom + the constraint that
     bounds it (invariant id or §Acceptance row). Empty list = digest is fully prescriptive. Soft cap ≤800 B. -->

- {area}: implementer chooses freely (constraint: {invariant id or §Acceptance row})
- {area}: implementer chooses freely (constraint: …)

### §Work Items

<!-- Flat list of file targets + 1-line intent. NO verbatim before/after code blocks. NO
     numbered steps. Implementer sequences and locates anchors against current HEAD. Soft cap ≤2000 B. -->

**Edits:** (intent only — implementer locates anchors against current HEAD)

- `{repo-relative-path}`: {what changes + why, 1 line}
- `{repo-relative-path}`: {what changes + why, 1 line}
- (Scene Wiring) `Assets/Scenes/{scene}.unity`: wire `{ComponentName}` per `ia/rules/unity-scene-wiring.md` — only when triggers fire (new MonoBehaviour / `[SerializeField]` / scene prefab / scene `UnityEvent`).

### §Test Blueprint

<!-- Test INTENTS only — implementer designs inputs, expected values, and picks harness from
     {node, unity-batch, bridge, manual}. Soft cap ≤1000 B. -->

- {test_name}: assert {behavior in glossary terms}
- {test_name}: assert {…}

### §Invariants & Gate

<!-- ONE block per digest (not per step). Implementer runs the gate after applying all work
     items. STOP route is the single escalation contract. Soft cap ≤800 B. -->

invariant_touchpoints:
  - id: {invariant_id}
    expected: pass | unchanged | none

validator_gate: {npm run validate:all | npm run unity:compile-check | …}

escalation_enum: STOP-on-anchor-mismatch | STOP-on-acceptance-unmet | STOP-on-invariant-regression | STOP-on-validator-fail

**Gate:**
```bash
{single command line — typically npm run unity:compile-check && npm run validate:all}
```

**STOP:** anchor_hint mismatch (HEAD diverged from §Pending Decisions assumption) OR §Acceptance row unmet OR invariant regression OR validator_gate non-zero → escalate to caller; do NOT silently adapt.
