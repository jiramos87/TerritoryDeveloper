/design-explore docs/explorations/ui-toolkit-authoring-mcp-slices.md

## Context this session must carry forward

You are receiving a design seed for **20 proposed MCP slice tools** under a new
`ui_toolkit_*` prefix that give agents a mechanical surface for inspecting,
authoring, modifying, wiring, and verifying Unity UI Toolkit panels (UXML + USS
+ TSS + Host MonoBehaviour + ModalCoordinator + scene UIDocument).

Your job: run the design-explore skill (per `ia/skills/design-explore/SKILL.md`)
across this seed and emit a formal ship-plan with stages + tasks ready for the
`ship-plan` skill to bulk-author into the DB. **Do NOT implement any tool**.
This is design-only.

## What the prior session achieved (the spirit + intent you must transmit)

1. Shipped a UI Toolkit parity recovery (43 iterations across Effort 1..10)
   reaching an accepted **current UI baseline** — HUD strip, modals, MainMenu
   sub-views, toast, hover-info, MAP panel, BUDGET, STATS, subtype picker.
   Pixel goldens locked under `tools/visual-baseline/golden/`. Recovery plan
   `docs/ui-toolkit-parity-recovery-plan.html` stays open as historical
   authoring record only — it is NOT canonical context.

2. Pre-flight audit (in seed §2) found the DEC-A28 emitter sidecar
   (`UxmlEmissionService` + `TssEmissionService` + `UxmlBakeHandler`) is a
   22-line shell stub + not wired into `unity:bake-ui`. DB `panel_child` rows
   describe the legacy uGUI prefab structure (DEC-A24), not the current UI
   Toolkit shape. Hosts hard-code disk UXML element names. Two surfaces
   (`HoverInfoHost`, `MapPanelHost.BuildRuntimePanel`) have no UXML at all —
   they construct VisualElement trees programmatically in C#.

3. 3-path decision matrix was resolved: Path A (lock + document) executed
   this session — 9 TECH tickets filed (TECH-34678..TECH-34686), §15.4.3
   closeout appended, canonical specs updated (glossary, layers, data-flows,
   ui-design-system). Path B (extend emitter parity + reverse-capture DB
   rows) is the **active program** — companion seed at
   `docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md`.
   Path C (two-tree DB) is rejected per DEC-A28 Q2 history.

4. This MCP-slice seed (the one you are exploring) is a **companion to Path B
   parity work** — it gives Path B the agent-side authoring primitives it
   needs. The two seeds are sequenced: Path B authors the emitter + DB
   schema; this seed authors the MCP tools agents drive Path B with. Both
   ship-plans should reference each other and not block on each other.

## Spirit you must preserve in the master plan you author

- **Same MCP surface, swappable backend.** Tools work in BOTH the current
  disk-canonical world (`Assets/UI/Generated/*.uxml/uss`, `Assets/UI/Themes/*.tss`)
  AND the future DB-canonical world (`panel_child` + `token_detail` + emitter
  sidecar producing the same files). Path B emitter parity flips the backend
  silently; the slice contract stays stable. Design the stage sequence so the
  initial tool delivery works against disk; the backend swap is a later
  stage triggered when the emitter parity ships.

- **Pixel goldens are the contract.** Every mutation tool must be backed by
  `ui_toolkit_panel_pixel_diff` evidence against the locked goldens. Diff
  regression = rollback signal. Bake this into the verify stage.

- **DB-primary pivot.** No SQL, no yaml hand-edit. Mutation tools own the
  row write + bake invocation. Per the `feedback_db_primary_pivot` memory.

- **Host C# stays outside the bake pipeline** per DEC-A28 invariant I4.
  Tools that touch Host source (`ui_toolkit_host_q_bind`, `ui_toolkit_host_lint`)
  generate code-stubs or scan for drift; they never auto-rewrite without an
  explicit flag.

- **Strangler per slug, not big-bang.** Tools accept a `slug` filter; the
  cutover unit is the panel. Legacy `UiBakeHandler` retires per-slug.

- **Recovery plan is historical.** Canonical context lives in
  `ia/specs/glossary.md §User interface — UI Toolkit (current UI baseline)` +
  `ia/specs/ui-design-system.md §Codebase inventory (UI Toolkit overlay — current
  UI baseline)` + `ia/specs/architecture/layers.md` (UI Toolkit overlay layer) +
  `ia/specs/architecture/data-flows.md §UI / UX design system`. Your plan
  should reference these, not the recovery plan HTML.

- **Bridge coordination** per `unity_bridge_lease` + Editor-on-REPO_ROOT
  preconditions for tools that need Play Mode (preview, pixel diff).

## What to deliver (per design-explore phase sequence)

1. Compare approaches for the **tool delivery cadence** (per seed §5 Q1):
   big-bang all 20 vs per-stage rollout (Inspect first → Author → Wire → Verify),
   vs vertical-slice per panel slug. Recommend one with rationale.
2. Select + expand the recommended approach into stages with explicit
   per-tool tasks. Each task should specify red-stage proof shape (preferred:
   MCP smoke test under `tools/mcp-ia-server/src/tools/__tests__/`).
3. Architecture pass: address the backend abstraction layer question (seed §5
   Q2) — design the `IUIToolkitPanelBackend` boundary so the disk-canonical →
   DB-canonical flip is a swap, not a rewrite.
4. Subsystem impact pass: enumerate what new validators land in
   `validate:all`, what test harness extensions are needed, whether
   `tools/blueprints/panel-schema.yaml` extends to UI Toolkit kinds, what
   shape `ui_toolkit_drift_scan` joining `validate:all` takes (seed §5 Q6).
5. Implementation-points pass: per tool, name the C# / TypeScript files +
   the existing `tools/mcp-ia-server/src/tools/ui-*.ts` template they extend.
6. Examples pass: show a worked authoring loop (e.g. "agent adds a new
   `hud-budget-extra` button using the new slices") end-to-end.
7. Subagent review pass per design-explore protocol.
8. Persist the design expansion in `docs/explorations/ui-toolkit-authoring-mcp-slices.md`
   in-place (append a Design Expansion section after §6) + emit the lean
   YAML frontmatter handoff for `/ship-plan` (per `ia/skills/design-explore/SKILL.md`
   Phase 4).

## Constraints (do NOT)

- Do NOT implement any of the 20 MCP tools. Design + sub-task spec only.
- Do NOT modify the seed §1–§6 prose. Append a §7 Design Expansion below.
- Do NOT alter the existing legacy-side `ui_panel_*` / `ui_token_*` /
  `ui_component_*` slices (those target DEC-A24 prefab pipeline + stay alive
  per strangler invariant).
- Do NOT rewrite Hosts, DB rows, or `UxmlEmissionService` — that's Path B
  emitter parity work (companion seed
  `docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md`).
- Do NOT decide tool authorship allow-list (`is_caller_authorized` per
  existing slice convention) until the design expansion settles the cutover
  agent set — list it as an open question instead.

## Stop condition

design-explore Phase 4 (persist) completes successfully + the lean YAML
handoff is emitted at the top of `docs/explorations/ui-toolkit-authoring-mcp-slices.md`
ready for `/ship-plan ui-toolkit-authoring-mcp-slices` in a follow-up session.
