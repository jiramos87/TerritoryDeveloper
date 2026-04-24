### Stage 1.3 — Cursor skill + narrative alignment

**Status:** Final

**Notes:** archived 2026-04-20 — 6 Tasks Done (TECH-587..592)

**Backlog state (Stage 1.3):** 6 filed → archived

**Objectives:** Ship **`.claude/skills/debug-sorting-order`** (Cursor-only). Patch **`ia/skills/ide-bridge-evidence`** only if Stage 1.1–1.2 changed evidence DTOs. Align **Close Dev Loop** / staging supersession text with exploration §7.1 / §10-B.

**Exit criteria:**

- **`.claude/skills/debug-sorting-order/SKILL.md`** committed with phases: bridge calls → **`spec_section`** **`geo`** §7 → compare → fix loop.
- **`ide-bridge-evidence`** updated OR explicit "no delta" note in Stage exit if DTOs unchanged.
- Docs note how **`debug_context_bundle`** relates to sugar tools (no contradiction with **`close-dev-loop`**).

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `ia/specs/isometric-geography-system.md` §7 — sorting formula authority for debug-sorting skill
- `.claude/skills/debug-sorting-order/SKILL.md` **(new)**
- `ia/skills/ide-bridge-evidence/SKILL.md` (exists) — update only if response DTOs change
- `docs/mcp-ia-server.md` — MCP tool catalog

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.3.1 | debug-sorting-order SKILL body | **TECH-587** | Done (archived) | Author **`.claude/skills/debug-sorting-order/SKILL.md`**: triggers, prerequisites (**`DATABASE_URL`**, Unity on **`REPO_ROOT`**), recipe calling **`unity_export_sorting_debug`** + **`unity_export_cell_chunk`**, **`spec_section`** **`geo`** §7, comparison checklist (**BUG-28**-style). |
| T1.3.2 | Symlink + skill index | **TECH-588** | Done (archived) | If required by repo convention, symlink **`ia/skills/...`** → **`.claude/skills/...`**; add row to **`ia/skills/README.md`** only if this repo lists Cursor-packaged skills (minimal). |
| T1.3.3 | ide-bridge-evidence diff | **TECH-589** | Done (archived) | Read **`ia/skills/ide-bridge-evidence/SKILL.md`**; update tool names / bundle fields if Stage 1.1–1.2 changed responses; otherwise add single-line "no bridge DTO change" exit note in task report. |
| T1.3.4 | Glossary / router spot-check | **TECH-590** | Done (archived) | Verify **`glossary_lookup`** "IDE agent bridge" + **`router_for_task`** domains still accurate; no new glossary row unless new public term introduced (terminology rule). |
| T1.3.5 | Close Dev Loop doc alignment | **TECH-591** | Done (archived) | Update **`docs/agent-led-verification-policy.md`** or **`docs/mcp-ia-server.md`** short subsection: **`close-dev-loop`** + **`debug_context_bundle`** vs sugar tools — supersession of registry staging (per analysis). |
| T1.3.6 | Optional backlog spec pointer | **TECH-592** | Done (archived) | If **`ia/backlog/TECH-552.yaml`** (or successor) tracks bridge program, add **`spec:`** → this orchestrator path + **`npm run materialize-backlog.sh`** — only if issue record exists; do not invent issue id in orchestrator body. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "debug-sorting-order SKILL body"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Author .claude/skills/debug-sorting-order/SKILL.md. Triggers, DATABASE_URL + Unity on REPO_ROOT,
    unity_export_sorting_debug + unity_export_cell_chunk, spec_section geo §7, BUG-28-style comparison checklist.
    Touches .claude/skills/ only.
  depends_on: []
  related:
    - "TECH-588"
    - "TECH-589"
    - "TECH-590"
    - "TECH-591"
    - "TECH-592"
  stub_body:
    summary: |
      New Cursor skill documents end-to-end sorting-order debug: bridge exports, isometric geography §7
      authority via spec_section, and agent comparison loop.
    goals: |
      1. SKILL.md lists triggers and prerequisites (DATABASE_URL, Unity Editor, REPO_ROOT).
      2. Recipe covers unity_export_sorting_debug and unity_export_cell_chunk plus spec_section geo §7.
      3. Checklist matches close-dev-loop style (BUG-28 reference pattern) for before/after comparison.
      4. No ia/skills clone; Cursor path under .claude/skills per orchestrator header.
    systems_map: |
      - .claude/skills/debug-sorting-order/SKILL.md — new
      - docs/unity-ide-agent-bridge-analysis.md — Design Expansion cross-link optional
      - ia/specs/isometric-geography-system.md §7 — sorting formula authority
      - tools/mcp-ia-server — unity_export_* tool names
    impl_plan_sketch: |
      - Author SKILL.md sections (purpose, triggers, prerequisites, phased recipe)
      - Wire glossary terms: IDE agent bridge, unity_bridge_command, spec_section
      - Add symlink row in ia/skills README or note defer to TECH-588

- reserved_id: ""
  title: "Symlink + skill index"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Symlink ia/skills/debug-sorting-order to .claude/skills if repo convention requires; minimal
    ia/skills/README.md row when index lists Cursor-packaged skills.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-589"
    - "TECH-590"
    - "TECH-591"
    - "TECH-592"
  stub_body:
    summary: |
      Align repository skill wiring so debug-sorting-order is discoverable from both ia/skills and
      .claude/skills per existing symlink pattern.
    goals: |
      1. Symlink exists if and only if sibling skills use same pattern.
      2. README row added only when table already lists packaged skills; otherwise document skip in task report.
      3. No duplicate SKILL bodies — single source path documented.
    systems_map: |
      - .claude/skills/ — Cursor symlink targets
      - ia/skills/README.md — optional index row
      - ia/skills/debug-sorting-order/ — symlink target if created
    impl_plan_sketch: |
      - Compare existing .claude/skills → ia/skills symlinks
      - Add symlink or record explicit no-op with reason
      - Patch README minimally if required by convention

- reserved_id: ""
  title: "ide-bridge-evidence diff"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Diff ia/skills/ide-bridge-evidence/SKILL.md against Stage 1.1–1.2 bridge DTO changes; update tool names or
    bundle fields if needed; else one-line no-change note in findings.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-588"
    - "TECH-590"
    - "TECH-591"
    - "TECH-592"
  stub_body:
    summary: |
      Ensure ide-bridge-evidence skill text matches shipped bridge responses and MCP tool names after
      Stage 1.1–1.2 parameter work.
    goals: |
      1. Read ide-bridge-evidence SKILL end-to-end.
      2. If export kinds or response DTOs changed, update skill prose and examples.
      3. If no delta, capture explicit no DTO change note for audit trail.
    systems_map: |
      - ia/skills/ide-bridge-evidence/SKILL.md
      - docs/mcp-ia-server.md — tool catalog
      - ia/specs/unity-development-context.md §10
    impl_plan_sketch: |
      - Compare SKILL tool names vs MCP registerTool + §10 table
      - Edit SKILL or add no-change sentence to §Findings / report
      - npm run validate:all if MCP descriptors touched

- reserved_id: ""
  title: "Glossary / router spot-check"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Verify glossary_lookup IDE agent bridge + router_for_task domains; add glossary row only if new public term; terminology-consistency rule.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-588"
    - "TECH-589"
    - "TECH-591"
    - "TECH-592"
  stub_body:
    summary: |
      Spot-check MCP routing and glossary anchors for bridge vocabulary after Stage 1 work; no gratuitous new terms.
    goals: |
      1. glossary_lookup and router_for_task return coherent entries for bridge workflow.
      2. Document pass/fail in spec; new glossary row only if truly new domain term.
      3. No issue ids in durable specs per terminology rule.
    systems_map: |
      - ia/specs/glossary.md
      - tools/mcp-ia-server — router + glossary tools
      - docs/mcp-ia-server.md
    impl_plan_sketch: |
      - Run glossary_lookup + router_for_task probes (record outputs in §Verification later)
      - File gap as backlog only if tool broken; else narrative confirmation in spec

- reserved_id: ""
  title: "Close Dev Loop doc alignment"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Short subsection in docs/agent-led-verification-policy.md or docs/mcp-ia-server.md: close-dev-loop vs
    debug_context_bundle vs unity_export_* sugar; registry staging supersession narrative.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-588"
    - "TECH-589"
    - "TECH-590"
    - "TECH-592"
  stub_body:
    summary: |
      Align durable docs so agents understand when close-dev-loop, debug_context_bundle, and export sugar
      tools apply — consistent with unity-ide-agent-bridge analysis §7.1 / §10-B.
    goals: |
      1. One subsection links close-dev-loop skill to bridge evidence paths without contradiction.
      2. debug_context_bundle vs sugar tools relationship explicit.
      3. npm run validate:all green after doc edits.
    systems_map: |
      - docs/agent-led-verification-policy.md
      - docs/mcp-ia-server.md
      - docs/unity-ide-agent-bridge-analysis.md — §7.1 narrative
    impl_plan_sketch: |
      - Choose policy vs MCP doc anchor for subsection
      - Add cross-links to ide-bridge-evidence + close-dev-loop skills
      - validate:all

- reserved_id: ""
  title: "Optional backlog spec pointer"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    If TECH-552 (or successor) yaml tracks bridge program, set spec to this orchestrator; run materialize-backlog.sh.
    Skip if issue absent or out of scope.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-588"
    - "TECH-589"
    - "TECH-590"
    - "TECH-591"
  stub_body:
    summary: |
      Optional alignment between bridge umbrella backlog record and this master plan path when TECH-552 or
      successor exists.
    goals: |
      1. Confirm whether TECH-552.yaml (or listed successor) is active bridge tracker.
      2. If yes, set spec field to ia/projects/unity-agent-bridge-master-plan.md and materialize backlog.
      3. If no, document skip — do not invent ids in orchestrator body.
    systems_map: |
      - ia/backlog/TECH-552.yaml — conditional
      - BACKLOG.md — generated view
      - ia/projects/unity-agent-bridge-master-plan.md — orchestrator path
    impl_plan_sketch: |
      - backlog_issue TECH-552 (or successor) status check
      - Patch yaml spec field if appropriate
      - bash tools/scripts/materialize-backlog.sh when yaml changes
```

#### §Plan Fix

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

_retroactive-skip — Stage 1.3 closed pre-canonical-§Stage-Audit (2026-04-24 structure refactor). No audit paragraphs persisted at close time._

#### §Stage Closeout Plan

> stage-closeout-plan — 6 Tasks (applied inline 2026-04-20). `plan-applier` Mode stage-closeout executed: archive backlog yaml **TECH-587**…**TECH-592** → `ia/backlog-archive/`; delete per-Task project specs for those ids; flip task rows → `Done (archived)`; Stage 1.3 **Status** → `Final`. No glossary/rule/doc shared migrations; no durable-doc id purge.

```yaml
closed_issue_ids: ["TECH-587","TECH-588","TECH-589","TECH-590","TECH-591","TECH-592"]
completed_iso: "2026-04-20"
validators: ["materialize-backlog.sh", "npm run validate:all"]
```

---
