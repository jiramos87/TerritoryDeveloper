### Stage 6.5 — Curation-trained quality gate


**Status:** Final — closed 2026-04-23 (7 tasks **TECH-723**..**TECH-729** archived via `1ac0da0`). **Locks consumed:** L11 (curation/promoted.jsonl + rejected.jsonl feed the signature aggregator; composer gates renders against the evolving envelope).

**Objectives:** Close the feedback loop from artist curation back into the generator. `curate.py` gains `log-promote` + `log-reject --reason` subcommands that append JSONL rows (verb names disambiguate from existing `promote` = PNG→Unity ship + `reject` = glob-delete — TECH-179). The signature extractor becomes a three-source aggregator: `envelope = catalog ∪ promoted − rejected-zones` (rejection reasons carve out floor zones in `vary.`*). The composer adds a render-time gate: sample `vary:` → render → score against the evolving envelope → re-sample up to N times; after N, write best-scoring variant and mark a `.needs_review` metadata sidecar. Ship tests + DAS §5 addendum.

**Exit:**

- `tools/sprite-gen/src/curate.py` — `log-promote` appends JSONL row to `curation/promoted.jsonl` (rendered variant + sampled `vary:` values + measured bbox/palette stats); `log-reject --reason <tag>` appends to `curation/rejected.jsonl`.
- `tools/sprite-gen/src/signature.py` — aggregator `envelope = catalog ∪ promoted − rejected-zones`; rejection reasons map to `vary.`* floor zones (e.g. `roof-too-shallow` → floor on `vary.roof.h_px`).
- `tools/sprite-gen/src/compose.py` — render-time score-and-retry loop: sample `vary:` → render → score → if below floor, re-sample (configurable N, default 5).
- `tools/sprite-gen/src/compose.py` — after N retries without meeting floor, write best-scoring output + `.needs_review` sidecar in metadata.
- `tools/sprite-gen/tests/test_curation_loop.py` — (a) envelope tightens toward promoted samples after N promotes (before/after fixture); (b) `vary:` range shrinks in direction of rejection reasons (before/after fixture); (c) `.needs_review` flag set when floor not met in N tries.
- `docs/sprite-gen-art-design-system.md` §5 addendum — curation loop + scoring floor + `.needs_review` semantics.
- `pytest tools/sprite-gen/tests/` exits 0.

**Tasks:**


| Task   | Name                                              | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                               |
| ------ | ------------------------------------------------- | ------------ | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.5.1 | `curate.py log-promote` → `promoted.jsonl`        | **TECH-723** | Done   | `tools/sprite-gen/src/curate.py` — add `log-promote <variant>` subcommand that appends JSONL row to `curation/promoted.jsonl`. Row carries: rendered variant path, sampled `vary:` values, measured bbox/palette stats from the rendered image. Idempotent append; no mutation. Verb disambiguates from existing `promote` (TECH-179 PNG→Unity ship). Consumes L11.                                             |
| T6.5.2 | `curate.py log-reject --reason` → `rejected.jsonl`| **TECH-724** | Done   | `tools/sprite-gen/src/curate.py` — add `log-reject <variant> --reason <tag>` subcommand. `<tag>` is a controlled vocabulary (initial set: `roof-too-shallow`, `roof-too-tall`, `facade-too-saturated`, `ground-too-uniform`). Row format mirrors `promoted.jsonl` plus `reason: <tag>`. Verb disambiguates from existing `reject` (TECH-179 glob-delete). Consumes L11.                                           |
| T6.5.3 | Signature three-source aggregator                 | **TECH-725** | Done   | `tools/sprite-gen/src/signature.py` — `compute_envelope(catalog, promoted, rejected)` returns `vary.`* bounds where `envelope = catalog ∪ promoted − rejected-zones`. Each rejection `reason` maps to a zone carve-out (e.g. `roof-too-shallow` floors `vary.roof.h_px.min`). Deterministic. Consumes L11.                                           |
| T6.5.4 | Composer render-time score-and-retry gate         | **TECH-726** | Done   | `tools/sprite-gen/src/compose.py` — wrap variant render in score-and-retry loop: sample `vary:` from envelope → render → score variant against envelope floor → if below, re-sample (new `palette_seed + i + retry`). Configurable N (default 5). Scoring heuristic: normalized distance from envelope centroid + hard-fail penalty on carved zones. |
| T6.5.5 | `.needs_review` sidecar on floor-miss             | **TECH-727** | Done   | `tools/sprite-gen/src/compose.py` — after N retries without meeting floor, emit best-scoring variant and write `<sprite>.needs_review.json` sidecar containing: final score, envelope snapshot, attempted seeds, failing zones. CI / curator consumes sidecars to surface low-confidence renders.                                                    |
| T6.5.6 | Tests: `test_curation_loop.py`                    | **TECH-728** | Done   | `tools/sprite-gen/tests/test_curation_loop.py` — three cases: (a) envelope tightens toward promoted samples after N promotes (before/after fixture); (b) `vary:` range shrinks in direction of rejection reasons (before/after); (c) `.needs_review` flag set when floor not met in N tries. Deterministic seeds throughout.                         |
| T6.5.7 | DAS §5 addendum — curation loop + floor + sidecar | **TECH-729** | Done   | `docs/sprite-gen-art-design-system.md` §5 — new/extended section covering promotion/rejection JSONL schema, envelope aggregator rule, rejection-reason → `vary.`* zone map, composer score-and-retry contract, and `.needs_review` sidecar semantics.                                                                                                |


#### §Stage File Plan



```yaml
- reserved_id: TECH-723
  title: curate.py log-promote → promoted.jsonl
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/curate.py` — new `log-promote <variant>` subcommand appending a JSONL row to `curation/promoted.jsonl`. Row carries rendered variant path, sampled `vary:` values, measured bbox/palette stats. Verb disambiguates from existing `promote` (TECH-179 PNG→Unity ship + catalog push).
  depends_on:
    - TECH-704
    - TECH-705
    - TECH-706
    - TECH-707
    - TECH-708
  related:
    - TECH-724
    - TECH-725
  stub_body:
    summary: |
      `log-promote` subcommand captures curator approvals into a JSONL log so the signature aggregator can tighten the envelope toward real artist-validated variants.
    goals: |
      1. `log-promote <variant>` appends one JSON row to `curation/promoted.jsonl`.
      2. Row carries variant path + sampled `vary:` values + measured bbox/palette stats.
      3. Idempotent append; no mutation of prior rows.
    systems_map: |
      `tools/sprite-gen/src/curate.py`; consumers: `src/signature.py::compute_envelope` (TECH-725).
    impl_plan_sketch: |
      Phase 1 — CLI subcommand scaffold; Phase 2 — Measurement helpers (bbox + palette stats); Phase 3 — JSONL writer + idempotency test.
- reserved_id: TECH-724
  title: curate.py log-reject --reason → rejected.jsonl
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/curate.py` — new `log-reject <variant> --reason <tag>` subcommand appending to `curation/rejected.jsonl`. Controlled reason vocabulary: `roof-too-shallow`, `roof-too-tall`, `facade-too-saturated`, `ground-too-uniform`. Verb disambiguates from existing `reject` (TECH-179 glob-delete).
  depends_on:
    - TECH-723
  related:
    - TECH-725
  stub_body:
    summary: |
      `log-reject` captures artist vetoes with a controlled reason tag, so the signature aggregator can carve out `vary.*` zones that produce undesirable variants.
    goals: |
      1. `log-reject <variant> --reason <tag>` appends JSONL row.
      2. Row shape mirrors `promoted.jsonl` plus `reason: <tag>`.
      3. Invalid `<tag>` → CLI error (controlled vocab enforced).
    systems_map: |
      `tools/sprite-gen/src/curate.py`; consumers: `src/signature.py::compute_envelope` (TECH-725).
    impl_plan_sketch: |
      Phase 1 — Controlled vocab constant; Phase 2 — Row writer reuses TECH-723 helpers; Phase 3 — Unit test for invalid reason.
- reserved_id: TECH-725
  title: Signature three-source aggregator
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/signature.py` — `compute_envelope(catalog, promoted, rejected)` returns `vary.*` bounds where `envelope = catalog ∪ promoted − rejected-zones`. Rejection reasons map to zone carve-outs.
  depends_on:
    - TECH-723
    - TECH-724
  related:
    - TECH-726
    - TECH-729
  stub_body:
    summary: |
      Aggregator consumes catalog signatures + promoted samples and subtracts rejected-zones, producing the live envelope the composer gate consults.
    goals: |
      1. Union of catalog + promoted tightens bounds toward validated variants.
      2. Rejection reasons carve out `vary.*` floor zones via a reason→axis map.
      3. Deterministic: same inputs → same envelope.
    systems_map: |
      `tools/sprite-gen/src/signature.py`; consumers: composer score-and-retry gate (TECH-726).
    impl_plan_sketch: |
      Phase 1 — Reason→axis carve-out table; Phase 2 — Envelope math (union + subtraction); Phase 3 — Unit tests.
- reserved_id: TECH-726
  title: Composer render-time score-and-retry gate
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — wrap variant render in score-and-retry loop: sample → render → score against envelope → re-sample up to N times (default 5). Scoring = normalized distance from envelope centroid + hard-fail penalty on carved zones.
  depends_on:
    - TECH-725
  related:
    - TECH-727
  stub_body:
    summary: |
      Composer gate rejects variants that land in carved zones or drift too far from the envelope, re-sampling until a variant passes or N retries exhausted.
    goals: |
      1. Retry count configurable; default 5.
      2. Deterministic: same seeds → same retry trajectory.
      3. Zero retries case = byte-identical to pre-gate render (feature flag off).
    systems_map: |
      `tools/sprite-gen/src/compose.py`; consumes `src/signature.py::compute_envelope` (TECH-725).
    impl_plan_sketch: |
      Phase 1 — Score function; Phase 2 — Retry loop with seed advancement; Phase 3 — Feature-flag for back-compat.
- reserved_id: TECH-727
  title: .needs_review sidecar on floor-miss
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — on N-retries exhaustion, write `<sprite>.needs_review.json` sidecar with final score, envelope snapshot, attempted seeds, failing zones. Curator consumes sidecars to surface low-confidence renders.
  depends_on:
    - TECH-726
  related: []
  stub_body:
    summary: |
      Sidecar metadata file surfaces low-confidence renders for curator review without blocking the pipeline.
    goals: |
      1. File name `<sprite>.needs_review.json` adjacent to rendered sprite.
      2. Contents: final score, envelope snapshot, attempted seeds, failing zones.
      3. Absent when variant meets floor within retries.
    systems_map: |
      `tools/sprite-gen/src/compose.py`; consumer: curator tooling / CI gate (future).
    impl_plan_sketch: |
      Phase 1 — Sidecar schema dataclass; Phase 2 — Writer on floor-miss branch; Phase 3 — Absence test on floor-met branch.
- reserved_id: TECH-728
  title: Tests — test_curation_loop.py
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_curation_loop.py` — (a) envelope tightens toward promoted samples after N promotes (before/after fixture); (b) `vary:` range shrinks in direction of rejection reasons (before/after); (c) `.needs_review` flag set when floor not met in N tries.
  depends_on:
    - TECH-726
    - TECH-727
  related: []
  stub_body:
    summary: |
      One test file exercising the full curation → aggregator → gate → sidecar loop with deterministic before/after fixtures.
    goals: |
      1. Before/after envelope comparison after N promotes.
      2. Before/after `vary.*` range after N rejects with a named reason.
      3. `.needs_review` sidecar presence/absence assertion.
    systems_map: |
      `tools/sprite-gen/tests/test_curation_loop.py`; consumers: `curate.py`, `signature.py`, `compose.py`.
    impl_plan_sketch: |
      Phase 1 — Before/after envelope test; Phase 2 — Rejection-zone test; Phase 3 — Needs_review test.
- reserved_id: TECH-729
  title: DAS §5 addendum — curation loop + floor + sidecar
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §5 — promotion/rejection JSONL schema, envelope aggregator rule, rejection-reason → `vary.*` zone map, composer score-and-retry contract, `.needs_review` sidecar semantics.
  depends_on:
    - TECH-723
    - TECH-724
    - TECH-725
    - TECH-726
    - TECH-727
  related: []
  stub_body:
    summary: |
      Docs close the loop — artists learn the curation contract + reason vocabulary + what `.needs_review` means from the design system doc, not code comments.
    goals: |
      1. §5 documents JSONL schema for promoted / rejected rows.
      2. §5 publishes rejection-reason → `vary.*` zone carve-out map.
      3. §5 documents `.needs_review` sidecar semantics.
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §5.
    impl_plan_sketch: |
      Phase 1 — JSONL schema table; Phase 2 — Reason→axis map table; Phase 3 — Sidecar semantics subsection.
```

**Dependency gate:** Stage 6.2 merged (TECH-704..708). L12 stage order lock. Signature aggregator (TECH-725) specifically extends TECH-704's extractor with new inputs.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.5 tasks **TECH-723**..**TECH-729** aligned with §3 Stage 6.5 block of `/tmp/sprite-gen-improvement-session.md`; lock L11 threaded through all 7 tasks. Aggregate doc: `docs/implementation/sprite-gen-stage-6.5-plan.md`. Downstream: file Stage 6.6.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
