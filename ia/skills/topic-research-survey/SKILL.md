---
name: topic-research-survey
purpose: >-
  Produce a single research doc that pairs heavy external web research on a topic with an
  independent in-repo audit of the same subject area, then layers a strengths/weaknesses
  critique and a list of N methodology-named improvement proposals. Section isolation is
  the core invariant: findings, audit, critique, and improvements are written in order and
  never bleed across phases.
audience: agent
loaded_by: "skill:topic-research-survey"
slices_via: glossary_discover, glossary_lookup, router_for_task, spec_section, list_specs, arch_decision_list
description: >-
  Use when the user wants a state-of-art survey on an external topic (TOPIC) combined with
  a frozen-in-time audit of the repo's existing implementation of the same subject area,
  followed by critique and N improvement methodologies sourced from the research. Output =
  one Markdown doc with 4 ordered sections (Findings · Audit · Critique · Exploration).
  Does NOT create master plan, BACKLOG row, exploration seed, or commit. Triggers:
  "/topic-research-survey {TOPIC}", "research + audit + critique + improve {TOPIC}",
  "state-of-art survey for {SUBSYSTEM}".
phases:
  - Inputs + recency anchor + scope lock
  - External research (broad multi-query web sweep)
  - Findings section (pure, no comparison)
  - Repo audit (independent, no findings reference)
  - Critique (strengths + weaknesses, audit-only basis)
  - Exploration (N methodology-named improvements sourced from §Findings)
  - Persist + handoff
triggers:
  - /topic-research-survey
  - research and audit and critique
  - state-of-art survey
  - external research plus repo audit
  - propose improvements from web research
argument_hint: >-
  {TOPIC} [--queries q1,q2,...] [--as-of YYYY-MM] [--audit-scope "{repo subsystem}"]
  [--out docs/research/{slug}.md] [--n-improvements N]
  (e.g. "unity ui-as-code" --queries "unity procedural-ui,agentic unity ui"
   --as-of 2026-05 --audit-scope "ui-as-code system" --n-improvements 10)
model: inherit
reasoning_effort: high
input_token_budget: 160000
tools_role: planner
tools_extra:
  - WebSearch
  - WebFetch
  - mcp__territory-ia__glossary_discover
  - mcp__territory-ia__glossary_lookup
  - mcp__territory-ia__router_for_task
  - mcp__territory-ia__spec_section
  - mcp__territory-ia__list_specs
  - mcp__territory-ia__arch_decision_list
  - mcp__territory-ia__csharp_class_summary
  - mcp__territory-ia__research_doc_scaffold
  - mcp__territory-ia__web_findings_dedupe
  - mcp__territory-ia__audit_scope_resolve
  - mcp__territory-ia__arch_decision_conflict_scan
  - mcp__territory-ia__improvement_proposal_lint
  - mcp__territory-ia__research_doc_to_exploration_seed
caveman_exceptions:
  - code blocks
  - commits
  - security/auth
  - verbatim web quotes (research findings retain original wording when quoted)
  - structured MCP payloads
  - external URLs and citation lines
hard_boundaries:
  - "§Findings phase writes only what external sources say. Do NOT reference repo state, our system, or 'we'."
  - "§Audit phase writes only what is in the repo. Do NOT reference findings, web sources, or external methodology names."
  - "§Critique phase reasons over §Audit alone. Strengths/weaknesses are observable from the repo, not aspirational versus findings."
  - "§Exploration phase is the only place where §Findings cross-pollinates §Audit. Each improvement names a methodology from §Findings + cites the source line."
  - "If TOPIC is ambiguous or AUDIT_SCOPE cannot be resolved to a repo subsystem → STOP, ask user. Do NOT guess."
  - "If WebSearch / WebFetch quota or access fails on >50% of queries → STOP, report degraded coverage, ask user to retry or narrow."
  - "Do NOT create master plan, BACKLOG row, exploration-doc seed, or arch_decision. User triggers next step after review."
  - "Do NOT commit. Doc lives unstaged until user decides."
  - "Do NOT collapse §Findings and §Audit. Section isolation is the core invariant."
caller_agent: topic-research-survey
---

# Topic research survey — research, audit, critique, improve

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Web quotes in §Findings keep original wording.

**Position in lifecycle:** fires _before_ `design-explore`. Output doc can be promoted to an exploration seed by hand or future tool.

`topic-research-survey` → (user review) → `design-explore` → `ship-plan` → `ship-cycle` → `ship-final`.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `TOPIC` | 1st arg | Subject phrase. Quoted form preferred (e.g. `"unity ui-as-code"`). Required. |
| `QUERIES` | `--queries` | Comma-separated extra search queries. Skill auto-derives 5 variants from `TOPIC` if omitted. |
| `AS_OF` | `--as-of` | Recency anchor `YYYY-MM`. Default = current month. Used in WebSearch phrasing + filter. |
| `AUDIT_SCOPE` | `--audit-scope` | Plain-language description of the repo subsystem to audit. Must resolve via `router_for_task` or `glossary_discover`. Required. |
| `OUTPUT_PATH` | `--out` | Default `docs/research/{topic-slug}.md`. Slug = `TOPIC` lower-kebab. |
| `N_IMPROVEMENTS` | `--n-improvements` | Default `10`. Count of methodology-named proposals in §Exploration. |

---

## Phase sequence (gated — each phase depends on the previous)

### Phase 0 — Inputs + recency anchor + scope lock

1. Validate `TOPIC` non-empty. Reject if single bare word with no domain qualifier.
2. Resolve `AUDIT_SCOPE` → call `glossary_discover` + `router_for_task` to confirm subsystem exists in repo. If unresolved → STOP, ask user.
3. Derive `topic_slug` (lower-kebab `TOPIC`) and final `OUTPUT_PATH`.
4. If `QUERIES` empty: synthesize 5 query variants — `{TOPIC}`, `{TOPIC} {as-of-year}`, `{TOPIC} state of the art`, `agentic {TOPIC}`, `{TOPIC} from code`. Append literal variants the user listed.
5. Hold `{TOPIC, queries[], as_of, audit_scope, output_path, n_improvements}` for downstream phases.

### Phase 1 — External research (broad multi-query web sweep)

1. For each query in `queries[]`: run `WebSearch` with `query` + recency hint `{as_of}`. Collect hit list `{title, url, snippet}`.
2. For top 3–5 hits per query (dedupe by URL): run `WebFetch` to capture full content. Prefer canonical sources (vendor docs, GitHub READMEs, peer-reviewed posts) over aggregator pages.
3. Tag each hit with `{source_type: vendor|community|paper|blog|repo}`, `{publish_year}` if visible. Drop hits older than 24 months from `as_of` unless they are canonical specs (engine docs, RFCs).
4. Stash raw findings in memory only — do NOT write to doc yet.

> **Guardrail:** §Findings must be **pure**. No comparison to repo state at this phase. No "in our system…", no "we currently…", no `Assets/` paths.

### Phase 2 — §Findings section (pure)

1. Open `{output_path}` (create with `Write` if absent).
2. Write doc header — `# {TOPIC} — research, audit, critique, improvement (as of {as_of})`.
3. Write `## Findings` section. Structure:
   - One subsection per coherent methodology / library / pattern surfaced (e.g. `### UI Toolkit (UXML + USS)`, `### Procedural Mesh-based UI`, `### AI-driven layout`).
   - Each subsection = 3–8 lines: what it is, who maintains it, maturity signal, primary source link.
   - Citation lines under each subsection — bullet list of `{title} — {url}` for ≥1 source.
4. Append a short `### Cross-cutting observations` subsection covering: dominant patterns, emerging shifts, declining patterns, recency anchor (`{as_of}`).

Voice: descriptive third-person. Repo-agnostic. No "we", no `territory-developer`, no class names.

### Phase 3 — §Audit section (independent)

1. Append `## Audit — current implementation in repo` heading.
2. Resolve scope: `router_for_task({domain: audit_scope})` → returns paths. Optionally call `csharp_class_summary` on key classes flagged by router.
3. Optionally call `spec_section` on any spec the router cites.
4. Optionally call `arch_decision_list({status: 'active'})` to surface decisions that constrain the subsystem.
5. Write structured audit prose:
   - `### Entry points` — files / classes / specs.
   - `### Data flow` — how the subsystem produces or consumes state.
   - `### Constraints` — locked arch_decisions or invariants touching the subsystem.
   - `### Coverage` — what tests / validators exist.
6. Voice: caveman-tech. Repo-only. No mention of methodologies from §Findings. No "compared to…", no "better than the industry…".

> **Guardrail:** §Audit must be readable as a standalone repo description. A reader who skipped §Findings should still understand the current system.

### Phase 4 — §Critique section (audit-only basis)

1. Append `## Critique — strengths and weaknesses` heading.
2. Write `### Strengths` — 3–7 bullets. Each = observable property of the audited system. No "compared to industry".
3. Write `### Weaknesses` — 3–7 bullets. Each = observable friction, gap, or risk. Tie to a file/class/spec from §Audit when possible.
4. Strengths/weaknesses derived from §Audit observation, not from §Findings. (§Findings only enters in Phase 5.)

Voice: caveman-tech. Cite §Audit anchors (e.g. `§Audit · Entry points · UiBakeHandler.cs`).

### Phase 5 — §Exploration section (improvement proposals)

1. Append `## Exploration — {n_improvements} ways to improve` heading.
2. Cross-link §Findings ↔ §Critique. Each improvement must:
   - Name a methodology / pattern / library **drawn from §Findings**.
   - State the target weakness from §Critique it addresses (cite anchor).
   - Describe how it would apply to the repo subsystem (1–3 lines, mechanical).
   - Cite the §Findings subsection it sources from.
3. Numbered list `1.` through `{n_improvements}.`. One paragraph each.
4. Optionally append a short `### Conflicts with locked decisions` subsection if any proposal collides with an active `arch_decision`.

Voice: caveman-tech. Each proposal = `[methodology] [target weakness] [mechanical sketch] [source link]`.

### Phase 6 — Persist + handoff

1. Final write of `{output_path}` via single `Write` (or `Edit` if pre-existing).
2. Emit caveman summary to caller:

```
topic-research-survey: doc written at {output_path}.
Topic: {TOPIC}. As-of: {as_of}. Audit scope: {audit_scope}.
Sections: Findings ({k_subsections}), Audit, Critique, Exploration ({n_improvements}).
Sources: {n_urls} URLs cited. Recency-filtered to ≥ {as_of - 24mo}.
Next: review doc → /design-explore {output_path} OR promote to docs/explorations/ by hand.
```

3. Return `{output_path, n_findings_subsections, n_sources, n_improvements, conflicts_with_decisions[]}`.

---

## Tool recipe (territory-ia + web)

| Step | Tool | Purpose |
|------|------|---------|
| 0.2 | `audit_scope_resolve` | Single-call fusion of glossary_discover + router_for_task + active arch_decisions over `AUDIT_SCOPE`. Replaces 3–4 sequential queries. |
| 0.5 | `research_doc_scaffold` | Write canonical 4-section skeleton + N proposal placeholders at `OUTPUT_PATH`. Guarantees section order. |
| 1.x | `WebSearch` | One call per query in `queries[]`. |
| 1.x | `WebFetch` | Top hits per query. Canonical sources only. |
| 1.end | `web_findings_dedupe` | Collapse URL duplicates + recency-filter the full hit batch before §Findings authoring. |
| 3.2 | `router_for_task` | Map audit scope → file/spec list (already prefetched in 0.2 via `audit_scope_resolve`). |
| 3.2 | `csharp_class_summary` | Per key C# class flagged by router. |
| 3.3 | `spec_section` | Per spec cited by router (slice, not full read). |
| 3.4 | `arch_decision_list` | Surface locked decisions touching subsystem (already prefetched in 0.2). |
| 5.end | `arch_decision_conflict_scan` | Score §Exploration proposals vs active arch_decisions → populates §Exploration · Conflicts. |
| 5.end | `improvement_proposal_lint` | Validate every numbered §Exploration entry carries `{methodology_name, target_weakness, mechanical_sketch, source_link}`. Block Phase 6 on `ok=false`. |
| 6.opt | `research_doc_to_exploration_seed` | Opt-in handoff — promote doc to `docs/explorations/{slug}.md` + prepend lean YAML frontmatter for `design-explore --resume`. |

Read-only across all phases. `research_doc_scaffold` + `research_doc_to_exploration_seed` write to filesystem only (no IA / DB mutation). All other tools read-only.

---

## Output doc shape (canonical template)

```
# {TOPIC} — research, audit, critique, improvement (as of {as_of})

## Findings
### {Methodology / Library / Pattern A}
{3–8 lines, pure description}
- {title} — {url}

### {Methodology B}
...

### Cross-cutting observations
- Dominant: ...
- Emerging: ...
- Declining: ...

## Audit — current implementation in repo
### Entry points
### Data flow
### Constraints
### Coverage

## Critique — strengths and weaknesses
### Strengths
- ...
### Weaknesses
- ...

## Exploration — {N} ways to improve
1. **{Methodology name}.** {target weakness anchor}. {mechanical sketch}. Source: §Findings · {subsection}.
2. ...
...

### Conflicts with locked decisions (optional)
- Improvement #X conflicts with `arch_decision:{id}` — {reason}.
```

---

## Hard boundaries

- Section isolation is the invariant. §Findings ⊥ §Audit ⊥ §Critique. Only §Exploration crosses §Findings × §Critique.
- Do NOT promote the doc to `docs/explorations/`, do NOT add `## Design Expansion`, do NOT call `ship-plan` — user decides.
- Do NOT mutate IA, BACKLOG, arch_decisions, or master_plans.
- Do NOT commit.
- Do NOT fabricate citations. Every URL in §Findings must come from a `WebSearch` / `WebFetch` result.
- Do NOT exceed `input_token_budget`. If web sweep returns > budget, summarize per-hit at fetch time.

---

## MCP tool extensions

Six tools shipped with the skill (wired into Tool recipe above):

| Tool | Phase | Role |
|------|-------|------|
| `research_doc_scaffold` | 0 | Canonical 4-section skeleton writer. |
| `web_findings_dedupe` | 1 | URL dedupe + recency filter for `WebSearch` / `WebFetch` hits. |
| `audit_scope_resolve` | 0, 3 | Single-call fusion glossary + router + active arch_decisions. |
| `arch_decision_conflict_scan` | 5 | Score proposals vs active arch_decisions. |
| `improvement_proposal_lint` | 5 | Validate each §Exploration entry carries four signals. |
| `research_doc_to_exploration_seed` | 6 | Promote research doc → exploration doc with lean YAML frontmatter. |

Future proposals (not implemented — need new DB tables or cron infra):

| Proposed tool | Phase | What it would do | Why |
|---------------|-------|------------------|-----|
| `topic_findings_persist` | 2 | Append a structured row per methodology subsection to `ia_topic_findings` (proposed table) keyed by `{topic_slug, methodology, source_url}`. | Builds a reusable corpus across surveys; future surveys query prior findings before fresh web sweep. |
| `cron_topic_recency_refresh_enqueue` | post-Phase 6 | Schedule a re-run of the web sweep N months later when `as_of` ages out. | Keeps survey docs from going stale silently. |

---

## Cross-references

- [`ia/skills/design-explore/SKILL.md`](../design-explore/SKILL.md) — downstream consumer; receives the doc as input.
- [`ia/rules/agent-output-caveman.md`](../../rules/agent-output-caveman.md) — caveman default + exceptions for citations.
- [`ia/rules/agent-principles.md`](../../rules/agent-principles.md) — token economy, MCP-first.
- [`ia/skills/README.md`](../README.md) — skill conventions + index.
