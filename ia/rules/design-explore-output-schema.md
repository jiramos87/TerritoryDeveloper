---
purpose: "Schema contract for the 8 enriched fields produced by design-explore Phase 3.5 grill + consumed by ship-plan Phase B digest authoring. Lifts spatial / structured information from MD body into typed slots so HTML render carries the same signal — not cosmetic chrome."
audience: agent
loaded_by: on-demand
slices_via: none
description: "Canonical schema for the 8 enriched fields (visual_mockup_svg, before_after_code, edge_cases, glossary_anchors, failure_modes, decision_dependencies, shared_seams, touched_paths_with_preview) — shape, mandatory band, source authoring phase, downstream ingestion point, MD persistence shape, HTML widget binding, validator coverage. Required reading before design-explore Phase 3.5 grill + ship-plan Phase B digest authoring."
alwaysApply: false
---

# Design-explore output schema — 8 enriched fields

Caveman-tech register. Agent surface. Cross-link from [`agent-router.md`](agent-router.md) routing table.

## Why this schema exists

The four-skill lifecycle = `design-explore → ship-plan → ship-cycle → ship-final`. Pre-uplift, only the lean YAML frontmatter (`stages[].tasks[]` with `prefix` / `depends_on` / `digest_outline` / `touched_paths` / `kind`) crossed the design-explore → ship-plan boundary as structured data. MD body was human-reviewed prose; ship-plan composed digest bodies from `digest_outline` strings + glossary lookups + invariant summaries.

**Causal chain**:

1. design-explore Phase 3.5 grill authors 8 typed fields per task/stage.
2. Fields persist as MD subsections under each stage block (`#### Edge Cases`, `#### Failure Modes`, etc.) AND as parallel `enriched:` blocks inside the YAML frontmatter task entries.
3. design-explore Phase 9 renders HTML via Node script; widgets surface each field through a Thariq-pattern UI element (carousel / card grid / dep graph / etc.).
4. ship-plan Phase A.0 extracts MD from `.html` when present; falls back to legacy `.md`.
5. ship-plan Phase B prompt reads the 8 MD subsections per task/stage and INJECTS them verbatim into the digest `body_md` as 8 additional `### §...` subsections (after the legacy 3: §Goal + §Red-Stage Proof + §Work Items).
6. ship-cycle Pass A preflight emits `digest_bodies` from `task_spec_body` MCP slice (full body_md) → Sonnet inference receives the enriched subsections transparently. spec-implementer + verify-loop pick up the richer grounding without skill change.

This rule pins steps 1–5. ship-cycle benefits by data path alone (verified in design-explore-html-effectiveness-uplift plan §3.5 D2.0 pre-flight).

## Field reference (8 fields)

### 1. `visual_mockup_svg`

| Aspect | Value |
|---|---|
| Per-X | task |
| Mandatory band | always — Stage 1 tracer-slice tasks; per-task-type — Stages 2+ tasks whose `kind=code` AND whose `touched_paths` include user-visible UI surface; optional — `doc-only` / `mcp-only` / `tooling` tasks |
| Shape (TS-ish) | `string` — inline SVG markup, root `<svg>` element, viewport `0 0 400 240` recommended, palette = ds-* CSS variables, ≤200 LOC |
| Source authoring | design-explore Phase 3.5 grill poll: "What does this task SHOW the user when shipped?" — author hand-draws the expected end-state in SVG. |
| Downstream ingestion | ship-plan Phase B injects under §Visual Mockup subsection in body_md. spec-implementer treats as visual acceptance criterion alongside §Goal. |
| HTML surface | Widget I — mockup carousel inside each task card (per `enriched.visual_mockup_svg`). Lazy-rendered inside collapsible. |
| Example | `<svg viewBox="0 0 400 240"><rect fill="var(--bg-elev)" .../></svg>` |

### 2. `before_after_code`

| Aspect | Value |
|---|---|
| Per-X | task |
| Mandatory band | per-task-type — `kind=code` tasks whose largest touched-path LOC ≥50; optional — others |
| Shape (TS-ish) | `{ before: string, after: string, path: string }` — paired code snippets ≤30 LOC each, plus the path they reference |
| Source authoring | design-explore Phase 3.5 grill poll loads `csharp_class_summary` for `.cs` paths in `touched_paths`; agent extracts current shape (≤30 LOC excerpt of target method/class) + drafts target shape. |
| Downstream ingestion | ship-plan Phase B injects under §Before / After subsection in body_md. spec-implementer uses as anchor candidate when locating edit site against HEAD. |
| HTML surface | Widget G — annotated diff strip inside each task card. Two-pane code (current / target) with margin notes. |
| Example | ```{ before: "void TryDraw() { /* legacy */ }", after: "void TryDraw() { /* w/ treasury floor */ }", path: "Assets/Scripts/Economy/BudgetService.cs" }``` |

### 3. `edge_cases[]`

| Aspect | Value |
|---|---|
| Per-X | stage |
| Mandatory band | always |
| Shape (TS-ish) | `Array<{ input: string, state: string, expected: string }>` — input/state/expected triples; ≥3 entries per stage |
| Source authoring | design-explore Phase 3.5 grill poll: "What edge cases must the red-stage proof cover before this stage closes?" — agent enumerates input × state combinations. |
| Downstream ingestion | ship-plan Phase B injects under §Edge Cases subsection in body_md (stage-level → repeats across all tasks of the stage). verify-loop Pass B reads as additional verification scope. |
| HTML surface | Widget J — edge-case card grid inside each stage body. Filterable by state. |
| Example | ```[{ input: "save during midnight tick", state: "treasury_floor_hit", expected: "save blocks; player warned; no corrupted ledger" }, ...]``` |

### 4. `glossary_anchors[]`

| Aspect | Value |
|---|---|
| Per-X | task |
| Mandatory band | always — ≥1 entry per task |
| Shape (TS-ish) | `string[]` — kebab-case glossary slugs from `ia/specs/glossary.md`; resolved via `glossary_lookup` MCP at authoring time |
| Source authoring | design-explore Phase 3.5 Tool recipe extension: batch `glossary_lookup` per task using keywords from `digest_outline` + interface names + touched_paths. Use returned anchors verbatim. |
| Downstream ingestion | ship-plan Phase B injects under §Glossary Anchors subsection in body_md. Skips ambiguity resolution at digest authoring time — anchors already canonical. |
| HTML surface | No dedicated widget — surfaces via `data-glossary="{slug}"` attribute on any glossary link rendered in body. Lifts glossary semantics into DOM for downstream tooling. |
| Example | `["hud-bar", "panel-detail", "ui-toolkit-strangler"]` |

### 5. `failure_modes[]`

| Aspect | Value |
|---|---|
| Per-X | task |
| Mandatory band | always — ≥1 entry per task |
| Shape (TS-ish) | `string[]` — "Fails if X" statements; concrete regression surface |
| Source authoring | design-explore Phase 3.5 grill poll: "Name the concrete ways this task can ship broken." — agent enumerates failure-mode bullets. |
| Downstream ingestion | ship-plan Phase B injects under §Failure Modes subsection in body_md. verify-loop reads as red-flag scan list during Pass B. |
| HTML surface | Widget K — failure-mode list inside each task card. Red-bordered cards with "fails if" prefix. |
| Example | `["Fails if visual baseline tooling cannot resolve Assets/UI/Prefabs/Generated/", "Fails if pixel-diff tolerance config absent at first run"]` |

### 6. `decision_dependencies`

| Aspect | Value |
|---|---|
| Per-X | task |
| Mandatory band | per-task-type — tasks whose touched_paths overlap `ia/specs/architecture/**` OR whose digest_outline references a DEC-A* slug; optional — others |
| Shape (TS-ish) | `Array<{ slug: string, role: string }>` — slug = `arch_decisions.slug` (e.g. `DEC-A28`); role = `inherits` \| `extends` \| `constrains` |
| Source authoring | design-explore Phase 3.5 Tool recipe extension: call `arch_decision_list` once per design-explore run filtered by `plan_slug`. Surface matching decisions per task during grill poll. |
| Downstream ingestion | ship-plan Phase B injects under §Decision Dependencies subsection in body_md. spec-implementer treats as load-bearing context for design-level decisions inherited by this task. |
| HTML surface | Widget E — clickable dep graph; nodes = tasks; edges = depends_on + DEC-A* annotations. Click node scrolls to task card. |
| Example | `[{ slug: "DEC-A28", role: "inherits" }, { slug: "DEC-A15", role: "constrains" }]` |

### 7. `shared_seams[]`

| Aspect | Value |
|---|---|
| Per-X | stage |
| Mandatory band | per-stage-type — stages that consume or produce interface contracts bridging earlier/later stages; optional — leaf-stages whose touched_paths are self-contained |
| Shape (TS-ish) | `Array<{ name: string, producer_stage: string, consumer_stages: string[], contract: string }>` — name = interface name (e.g. `IPanelEmitter`); contract = ≤200 char description of the bridge |
| Source authoring | design-explore Phase 3.5 grill poll: "What interface contracts cross from this stage forward or backward into other stages?" |
| Downstream ingestion | ship-plan Phase B injects under §Shared Seams subsection in body_md (stage-level → repeats across all tasks of the stage). |
| HTML surface | No dedicated widget — surfaces inside task card body under `<details>` "Shared seams" subsection. Cross-stage refs render as anchor links between task cards. |
| Example | ```[{ name: "IPanelEmitter", producer_stage: "ui-bake-handler-atomization Stage 1", consumer_stages: ["1.0", "2.0"], contract: "Bake a single panel from DB row → emit prefab + meta in canonical location" }]``` |

### 8. `touched_paths_with_preview`

| Aspect | Value |
|---|---|
| Per-X | task |
| Mandatory band | always — one entry per item in `touched_paths` |
| Shape (TS-ish) | `Array<{ path: string, loc: number \| null, kind: string, summary: string }>` — `loc` null when target is created (new file); `kind` ∈ `new` \| `extend` \| `delete` \| `refactor`; `summary` = ≤2-line excerpt from `csharp_class_summary` MCP |
| Source authoring | design-explore Phase 3.5 Tool recipe extension: call `csharp_class_summary` for each `.cs` path in `touched_paths`. Use returned LOC + class summary. New paths → `loc: null, kind: "new"`. |
| Downstream ingestion | ship-plan Phase B injects under §Touched Paths Preview subsection in body_md. spec-implementer reads as anchor candidate map before locating edit sites. |
| HTML surface | Widget F — filterable task table. All tasks single view. Chip filters by prefix / stage / file-root. Sortable columns. Per-task expansion shows path + LOC + 2-line summary. |
| Example | ```[{ path: "Assets/Scripts/Editor/Bridge/UxmlBakeHandler.cs", loc: null, kind: "new", summary: "Sidecar emitter implementing IPanelEmitter — DB row → UXML+USS pair" }]``` |

## MD persistence shape

The 8 fields persist in two surfaces inside the exploration MD doc:

### A. YAML frontmatter `enriched:` sub-block per task

```yaml
tasks:
  - id: "1.0.1"
    prefix: TECH
    depends_on: []
    digest_outline: "..."
    touched_paths: ["Assets/Scripts/X.cs"]
    kind: code
    enriched:
      visual_mockup_svg: |
        <svg viewBox="0 0 400 240">...</svg>
      before_after_code:
        path: "Assets/Scripts/X.cs"
        before: "..."
        after: "..."
      glossary_anchors: ["slug-a", "slug-b"]
      failure_modes:
        - "Fails if X"
        - "Fails if Y"
      decision_dependencies:
        - { slug: "DEC-A28", role: "inherits" }
      touched_paths_with_preview:
        - { path: "Assets/Scripts/X.cs", loc: 350, kind: "extend", summary: "..." }
```

Stage-level fields (`edge_cases[]`, `shared_seams[]`) persist under the stage entry:

```yaml
stages:
  - id: "1.0"
    title: "..."
    exit: "..."
    enriched:
      edge_cases:
        - { input: "...", state: "...", expected: "..." }
      shared_seams:
        - { name: "IPanelEmitter", producer_stage: "...", consumer_stages: [...], contract: "..." }
    tasks: [ ... ]
```

### B. Parallel MD subsections in the doc body

Under each stage block in the body MD, emit one `#### Task {ID} — Enriched` heading per task whose `enriched:` block is non-empty. Inside, emit the 8 subsections in fixed order (skip when field absent):

```markdown
#### Task 1.0.1 — Enriched

##### Visual Mockup
<inline svg block>

##### Before / After Code
<paired code fences>

##### Glossary Anchors
- `slug-a`
- `slug-b`

##### Failure Modes
- Fails if X
- Fails if Y

##### Decision Dependencies
- DEC-A28 (inherits)

##### Touched Paths Preview
- `Assets/Scripts/X.cs` — 350 LOC, extend — Sidecar emitter ...
```

Stage-level fields (`edge_cases[]`, `shared_seams[]`) emit under a `#### Stage {ID} — Enriched` heading at the top of the stage block:

```markdown
#### Stage 1.0 — Enriched

##### Edge Cases
- Input: ... · State: ... · Expected: ...

##### Shared Seams
- `IPanelEmitter` — produced by ui-bake-handler-atomization Stage 1; consumed by Stage 1.0, 2.0 — contract: bake one panel from DB row → emit prefab + meta
```

Reason for the dual surface: YAML carries machine-typed values for the HTML renderer + validators. MD body carries human-readable prose that ship-plan Phase B injects verbatim into body_md without re-parsing the YAML.

## HTML surface (widget catalog)

| Field | Widget | Behavior |
|---|---|---|
| `visual_mockup_svg` | I — mockup carousel | Per-task collapsible card body. Inline SVG. Lazy-rendered. |
| `before_after_code` | G — annotated diff strip | Per-task collapsible. Two-pane code (current / target) with margin notes. |
| `edge_cases[]` | J — edge-case card grid | Per-stage. Cards per triple. Filterable by state. |
| `glossary_anchors[]` | (no widget — DOM `data-glossary` attribute) | Anchor link decoration; lifts glossary semantics into DOM. |
| `failure_modes[]` | K — failure-mode list | Per-task. Red-bordered cards with "fails if" prefix. |
| `decision_dependencies` | E — clickable dep graph | SVG. Nodes = tasks. Edges = depends_on + DEC-A* annotations. Click node scrolls to task card. |
| `shared_seams[]` | (no widget — `<details>` subsection inside task card) | Cross-stage references render as anchor links between task cards. |
| `touched_paths_with_preview` | F — filterable task table | All tasks single view. Chip filters. Sortable columns. |

Widget catalog rendered by `tools/scripts/render-design-explore-html.mjs` reading the frontmatter `stages[].enriched.*` + `stages[].tasks[].enriched.*` keys. Extracted MD round-trips via `tools/scripts/extract-exploration-md.mjs`.

## Validation

`tools/scripts/validate-design-explore-yaml.mjs` extended to assert:

1. **Task-level mandatory band.** Each task entry under `tasks[]` carries `enriched.glossary_anchors` (≥1 entry) AND `enriched.failure_modes` (≥1 entry) AND `enriched.touched_paths_with_preview` (entries match length of `touched_paths`).
2. **Stage 1 strict band.** Every task of the Stage with `id` starting `1.` carries all 8 fields. Used for the prototype-first tracer slice — Stage 1 is the load-bearing prototype demonstration; partial enrichment defeats the purpose.
3. **Stage-level mandatory band.** Each stage entry under `stages[]` carries `enriched.edge_cases` (≥3 entries).
4. **Skip-clause.** Fields marked `optional` band may be absent. Validator emits `INFO:` line per skipped field; no error.
5. **YAML / MD parity.** When MD body carries a `#### Task {ID} — Enriched` heading, the corresponding YAML `enriched:` sub-block must exist. Vice versa. Drift → exit 1.

Validator exit codes:

- `0` — clean.
- `1` — schema violation (missing mandatory band field, parity drift).

When `enriched.*` blocks absent across all tasks (legacy doc shape) → validator emits `WARNING:` lines but exits 0. Legacy explorations stay shippable.

## Cross-references

- [`ia/skills/design-explore/SKILL.md`](../skills/design-explore/SKILL.md) — Phase 3.5 grill body + Tool recipe extension.
- [`ia/skills/ship-plan/SKILL.md`](../skills/ship-plan/SKILL.md) — Phase A.0 source resolution + Phase B prompt update.
- [`ia/rules/plan-digest-contract.md`](plan-digest-contract.md) — Legacy 3-section digest body; enriched subsections layer ON TOP (do NOT replace).
- [`ia/rules/agent-router.md`](agent-router.md) — Routing row "Design exploration output schema" → this rule.
- [`tools/scripts/render-design-explore-html.mjs`](../../tools/scripts/render-design-explore-html.mjs) — Renderer; consumes `enriched.*` keys.
- [`tools/scripts/extract-exploration-md.mjs`](../../tools/scripts/extract-exploration-md.mjs) — Inverse extractor; reads `<script id="rawMarkdown">` block.
- [`tools/scripts/validate-design-explore-yaml.mjs`](../../tools/scripts/validate-design-explore-yaml.mjs) — Extended validator.
- [Thariq HTML-effectiveness playbook](https://thariqs.github.io/html-effectiveness/) — Origin of the 11-widget surface; 8 fields map to 11 widgets (3 fields share zero dedicated widget surface; surface via DOM attributes / `<details>` nesting).
