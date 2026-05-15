---
slug: large-file-atomization-cutover-refactor
parent_plan_id: null
target_version: 1
notes: |
  Cutover follow-up to large-file-atomization-refactor v1 (parent: stages 1..15 ported logic
  to Domains/{X}/Services/*.cs but left Manager scripts carrying duplicated body). This plan
  trims the Manager body + delegates to Domain port via pass-through (Approach 1b). Hard
  constraints: DO NOT MOVE Manager file; DO NOT RENAME class/namespace; DO NOT MODIFY any
  [SerializeField] field; preserve every public method signature + Unity lifecycle hook +
  coroutine attachment.
  Locked decisions: Q1=1b (pass-through), Q2=2a (Stage 6 single full-inline of 20+ mutation
  kinds), Q3=3b (compile + scene-load + 1 PlayMode smoke per stage), Q4=4c (tracer + 5 = 6
  stages), Q5=5b (dead-publics flagged in §Open Questions, not auto-deleted).
  Prerequisites: BUG-63 (.meta capture in stage commit) must land before Stage 1.0.
  Hub move (Managers/** -> Domains/{X}/) deferred to a separate later plan.
stages:
  - id: "1.0"
    title: "Tracer — InterstateManager.cs cutover"
    exit: "InterstateManager.cs trimmed 1149 -> ~150 LOC; class name + namespace + path + every [SerializeField] field unchanged; InterstateService POCO absorbs full body (90 -> ~1000 LOC); each public method body = single-line delegate `return _service.Method(args);`; csharp compile green; MainScene loads; AUTO ProcessTick PlayMode smoke = GREEN; dead-publics flagged in §Open Questions."
    red_stage_proof: |
      tracer-test:Assets/Scripts/Managers/GameManagers/InterstateManager.cs::CutoverPassThroughDelegationTest
        BEFORE: every public method body carries inline algorithm body (~30 methods, ~1100 LOC body total).
        AFTER: each public method body = single-line `return _interstateService.Method(args);`; class/namespace/path UNCHANGED; serialized_fields_diff(file) == set(); unity_lifecycle_diff(file) == only `_interstateService = new InterstateService(this);` injection; coroutine call sites unchanged.
        SMOKE: csharp_compile_passes(); scene_loads("MainScene"); playmode_smoke("AUTO ProcessTick interstate auto-build") == "GREEN".
        CALLERS: all live caller surfaces (signatures preserved) compile + behave identically pre/post.
    tasks:
      - id: "1.0.1"
        title: "InterstateManager pass-through cutover via InterstateService"
        prefix: TECH
        depends_on: []
        kind: code
        digest_outline: "Trim duplicated algorithm body on InterstateManager.cs (1149 LOC -> ~150 LOC); grow InterstateService.cs port (90 LOC -> ~1000 LOC) absorbing the full body; inject `_interstateService` field via Awake; replace each public method body with single-line delegate; preserve every [SerializeField], Unity lifecycle hook, coroutine attachment, partial-class shape (n/a here), namespace, class name, file path. Smoke gate: compile + MainScene load + AUTO ProcessTick PlayMode round-trip. Dead-public sweep (zero-caller publics) -> §Open Questions per Q5=5b."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/InterstateManager.cs
          - Assets/Scripts/Domains/Roads/Services/InterstateService.cs
  - id: "2"
    title: "UIManager.Theme.cs partial cutover via ThemeService"
    exit: "UIManager.Theme.cs trimmed 1004 -> ~150 LOC; partial-class declaration (`public partial class UIManager`) preserved; every [SerializeField] field on UIManager UNCHANGED; ThemeService POCO grows 112 -> ~900 LOC absorbing full theme body; each public method body = single-line delegate; csharp compile green; MainScene loads; UI theme-swap PlayMode smoke = GREEN (color/font assertion identical pre/post)."
    red_stage_proof: |
      tracer-test:Assets/Scripts/Managers/GameManagers/UIManager.Theme.cs::CutoverPartialClassDelegationTest
        BEFORE: UIManager.Theme.cs partial carries inline theme algorithm body (color/font/style swap logic).
        AFTER: each public method body = single-line `return _themeService.Method(args);`; `public partial class UIManager` declaration UNCHANGED; serialized_fields_diff(file) == set(); partial-class composition with sibling UIManager partials (UIManager.cs etc.) unbroken; service field declared once on .Theme.cs partial.
        SMOKE: csharp_compile_passes(); scene_loads("MainScene"); playmode_smoke("UI theme swap applies same colors/fonts pre/post") == "GREEN".
    tasks:
      - id: "2.1"
        title: "UIManager.Theme partial cutover via ThemeService"
        prefix: TECH
        depends_on: ["TECH-1.0.1"]
        kind: code
        digest_outline: "Trim duplicated theme algorithm body on UIManager.Theme.cs partial (1004 LOC -> ~150 LOC); grow ThemeService.cs port (112 LOC -> ~900 LOC) absorbing full theme body; inject `_themeService` field on .Theme.cs partial (visible to other UIManager partials via shared `this`); replace each public method body with single-line delegate; preserve `public partial class UIManager` declaration + every [SerializeField] + Unity lifecycle hook on UIManager. Smoke gate: compile + MainScene load + UI theme swap PlayMode color/font assertion. Dead-public sweep -> §Open Questions per Q5=5b."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/UIManager.Theme.cs
          - Assets/Scripts/Domains/UI/Services/ThemeService.cs
  - id: "3"
    title: "RoadPrefabResolver.cs pass-through cutover"
    exit: "RoadPrefabResolver.cs trimmed 1122 -> ~180 LOC; class name + namespace + path + every [SerializeField] field UNCHANGED; PrefabResolverService POCO absorbs remaining body (938 -> ~1100 LOC); each public method body = single-line delegate; csharp compile green; MainScene loads; AUTO road-place PlayMode smoke = GREEN (returns identical prefab GUIDs pre/post)."
    red_stage_proof: |
      tracer-test:Assets/Scripts/Managers/GameManagers/RoadPrefabResolver.cs::CutoverPassThroughDelegationTest
        BEFORE: RoadPrefabResolver carries inline prefab-resolution body (cache invalidation, GUID lookup, fallback chains).
        AFTER: each public method body = single-line `return _prefabResolverService.Method(args);`; class/namespace/path UNCHANGED; serialized_fields_diff(file) == set(); cache invalidation (Inv #2) preserved inside the port.
        SMOKE: csharp_compile_passes(); scene_loads("MainScene"); playmode_smoke("AUTO road-place returns same prefab GUIDs pre/post") == "GREEN".
    tasks:
      - id: "3.1"
        title: "RoadPrefabResolver pass-through cutover via PrefabResolverService"
        prefix: TECH
        depends_on: ["TECH-2.1"]
        kind: code
        digest_outline: "Trim duplicated prefab-resolution body on RoadPrefabResolver.cs (1122 LOC -> ~180 LOC); grow PrefabResolverService.cs port (938 LOC -> ~1100 LOC) absorbing remaining body; inject `_prefabResolverService` field via Awake; replace each public method body with single-line delegate; preserve every [SerializeField], Unity lifecycle, coroutine attachment, namespace, class name, file path. Cache-invalidation logic (Inv #2 — InvalidateRoadCache) MUST stay inside the port. Smoke gate: compile + MainScene load + AUTO road-place PlayMode GUID-equivalence assertion. Dead-public sweep -> §Open Questions per Q5=5b."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/RoadPrefabResolver.cs
          - Assets/Scripts/Domains/Roads/Services/PrefabResolverService.cs
  - id: "4"
    title: "GeographyManager.cs pass-through cutover"
    exit: "GeographyManager.cs trimmed 1143 -> ~200 LOC; class name + namespace + path + every [SerializeField] field UNCHANGED; GeographyWaterDesirabilityService POCO grows 49 -> ~1000 LOC absorbing full geography body; each public method body = single-line delegate; HeightMap/Cell sync (Inv #1) + shore band (Inv #7) + river bed monotonic (Inv #8) + cliff (Inv #9) preserved; csharp compile green; MainScene loads; zone-place PlayMode smoke = GREEN (desirability reads identical pre/post)."
    red_stage_proof: |
      tracer-test:Assets/Scripts/Managers/GameManagers/GeographyManager.cs::CutoverPassThroughDelegationTest
        BEFORE: GeographyManager carries inline desirability algorithm body (water proximity, shore band, river bed, cliff awareness).
        AFTER: each public method body = single-line `return _geographyService.Method(args);`; class/namespace/path UNCHANGED; serialized_fields_diff(file) == set(); HeightMap/Cell sync logic (Inv #1) preserved inside the port; shore-band + river-bed + cliff invariants preserved.
        SMOKE: csharp_compile_passes(); scene_loads("MainScene"); playmode_smoke("zone-place reads identical desirability pre/post") == "GREEN".
    tasks:
      - id: "4.1"
        title: "GeographyManager pass-through cutover via GeographyWaterDesirabilityService"
        prefix: TECH
        depends_on: ["TECH-3.1"]
        kind: code
        digest_outline: "Trim duplicated desirability/geography body on GeographyManager.cs (1143 LOC -> ~200 LOC); grow GeographyWaterDesirabilityService.cs port (49 LOC -> ~1000 LOC) absorbing full body; inject `_geographyService` field via Awake; replace each public method body with single-line delegate; preserve every [SerializeField] (desirability tunables stay inspector-visible), Unity lifecycle, namespace, class name, file path. HeightMap/Cell sync (Inv #1), shore band (Inv #7), river-bed monotonic (Inv #8), cliff (Inv #9) MUST stay inside the port. Smoke gate: compile + MainScene load + zone-place PlayMode desirability-equivalence assertion. Dead-public sweep -> §Open Questions per Q5=5b."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GeographyManager.cs
          - Assets/Scripts/Domains/Geography/Services/GeographyWaterDesirabilityService.cs
  - id: "5"
    title: "TerraformingService.cs (Manager) pass-through cutover"
    exit: "Manager TerraformingService.cs (Managers/GameManagers) trimmed 1095 -> ~180 LOC; class name + namespace + path + every [SerializeField] field UNCHANGED; Domain TerraformingService.cs port (Domains/Terrain/Services) grows 730 -> ~900 LOC absorbing remaining body; each public method body = single-line delegate; HeightMap/Cell sync (Inv #1) + shore band (Inv #7) preserved; csharp compile green; MainScene loads; AUTO terraform PlayMode smoke = GREEN (height/cliff mutations identical pre/post)."
    red_stage_proof: |
      tracer-test:Assets/Scripts/Managers/GameManagers/TerraformingService.cs::CutoverPassThroughDelegationTest
        BEFORE: Manager TerraformingService carries inline terraform algorithm body (height/cliff mutation, smoothing, edits).
        AFTER: each public method body = single-line `return _terraformingService.Method(args);`; class/namespace/path UNCHANGED on the Manager file; serialized_fields_diff(Manager file) == set(); Editor coroutine hook on Manager preserved; Domain port (Domains/Terrain/Services/TerraformingService.cs) absorbs remaining body.
        DISAMBIGUATION: target = Managers/GameManagers/TerraformingService.cs (1095 LOC). Domain port = Domains/Terrain/Services/TerraformingService.cs (730 LOC). Same filename, different folders.
        SMOKE: csharp_compile_passes(); scene_loads("MainScene"); playmode_smoke("AUTO terraform op mutates height identically pre/post") == "GREEN".
    tasks:
      - id: "5.1"
        title: "Manager TerraformingService.cs pass-through cutover (delegates to Domain port)"
        prefix: TECH
        depends_on: ["TECH-4.1"]
        kind: code
        digest_outline: "Trim duplicated terraform algorithm body on Managers/GameManagers/TerraformingService.cs (1095 LOC -> ~180 LOC); grow Domains/Terrain/Services/TerraformingService.cs port (730 LOC -> ~900 LOC) absorbing remaining body; inject `_terraformingService` field via Awake on Manager; replace each public method body with single-line delegate; preserve every [SerializeField], Unity lifecycle, Editor coroutine hook, namespace, class name, file path on the Manager. HeightMap/Cell sync (Inv #1) + shore-band (Inv #7) MUST stay inside the port. File-name ambiguity: Manager (Managers/GameManagers/) vs Domain port (Domains/Terrain/Services/) — both named TerraformingService.cs. Smoke gate: compile + MainScene load + AUTO terraform PlayMode height-mutation-equivalence assertion. Dead-public sweep -> §Open Questions per Q5=5b."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/TerraformingService.cs
          - Assets/Scripts/Domains/Terrain/Services/TerraformingService.cs
  - id: "6"
    title: "AgentBridgeCommandRunner.Mutations.cs full-inline cutover (20+ mutation kinds, single stage)"
    exit: "AgentBridgeCommandRunner.Mutations.cs trimmed 1386 -> ~250 LOC; partial-class declaration (`public partial class AgentBridgeCommandRunner`) preserved + composition with sibling partials (AgentBridgeCommandRunner.cs, .Conformance.cs) unbroken; Editor-only ref boundary preserved (file stays under Assets/Scripts/Editor/); MutationDispatchService POCO grows 31 -> ~1100 LOC absorbing every mutation kind (asset_database_refresh, road_place, zone_place, terraform_*, etc., 20+ kinds total); each public method body = single-line delegate; csharp compile green; MainScene loads; bridge round-trip PlayMode smoke = GREEN per mutation kind (every enumerated kind tested, not sampled)."
    red_stage_proof: |
      tracer-test:Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs::CutoverFullInlineDispatchDelegationTest
        BEFORE: AgentBridgeCommandRunner.Mutations.cs partial carries inline body for 20+ mutation kinds (asset_database_refresh, road_place, zone_place, terraform_*, ...).
        AFTER: every public mutation entry-point body = single-line `return _dispatchService.{Kind}(args);`; `public partial class AgentBridgeCommandRunner` declaration UNCHANGED; partial-class composition with .cs + .Conformance.cs siblings unbroken; Editor-only ref boundary preserved (file stays in Assets/Scripts/Editor/); MutationDispatchService.cs POCO exposes one method per mutation kind, mirroring the partial.
        ENUMERATION: enumerate_mutation_kinds(target) >= 20; every enumerated kind ported; per-kind atomicity inside the stage (rollback one kind = rollback just that kind's port).
        SMOKE: csharp_compile_passes(); scene_loads("MainScene"); for kind in mutation_kinds: bridge_smoke_round_trip(kind) == "GREEN".
    tasks:
      - id: "6.1"
        title: "AgentBridgeCommandRunner.Mutations full inline (single stage, all 20+ mutation kinds)"
        prefix: TECH
        depends_on: ["TECH-5.1"]
        kind: code
        digest_outline: "Single-stage full inline of every mutation kind from AgentBridgeCommandRunner.Mutations.cs (1386 LOC -> ~250 LOC) into MutationDispatchService.cs (31 LOC stub -> ~1100 LOC). Enumerate every mutation kind (asset_database_refresh, road_place, zone_place, terraform_*, etc.; >= 20 kinds); port each kind to a one-method-per-kind dispatch service; replace each Manager partial body with single-line delegate; preserve `public partial class AgentBridgeCommandRunner` declaration + composition with .cs + .Conformance.cs siblings + Editor-only ref boundary (file stays in Assets/Scripts/Editor/) + every [SerializeField] (n/a — Editor partial) + Unity lifecycle hook. Per-kind atomicity: smoke fails on one kind -> rollback just that kind's port. Smoke gate: compile + MainScene load + bridge round-trip PlayMode per mutation kind (every enumerated kind, not sampled). Dead-public sweep -> §Open Questions per Q5=5b. Stage 6 acknowledged as the biggest diff + biggest test surface in the plan (Q2=2a)."
        touched_paths:
          - Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs
          - Assets/Scripts/Domains/Bridge/Services/MutationDispatchService.cs
---

# Large-file atomization cutover refactor — exploration

Caveman-tech default per `ia/rules/agent-output-caveman.md`.

> **Status:** locked design — all 5 open decisions answered; ready for `/master-plan-new --from-exploration`.
> **Date:** 2026-05-08
> **Author:** Javier (+ agent)
> **Trigger:** parent plan `large-file-atomization-refactor` v1 ported Domain Service code (Stages 1..15) but did NOT cut Manager scripts over to delegate. Result: Managers still carry full duplicated logic body; Domain Services exist but are dead code (or partially live for new call sites). This plan = the **cutover** — trim duplicated bodies on Manager scripts, delegate to Domain ports, keep Manager files in place + class names intact + `[SerializeField]` fields untouched.

---

## 1. Problem statement

- Parent plan ported logic to `Assets/Scripts/Domains/{X}/Services/*.cs` but left Manager scripts at `Assets/Scripts/Managers/**` carrying the SAME logic body.
- Result: two copies of every algorithm — drift risk, maintenance cost, no token economy gain on Read.
- Hub move + Unity scene reconnection deferred (separate plan) — moving Manager files breaks every prefab reference + scene wiring; out of scope here.
- Cutover scope = body-only trim + delegate; surface-preserving, scene-safe.

## 2. Driving intent

- **Trim** duplicated logic bodies on every Manager that has a Domain port.
- **Delegate** Manager methods to Domain Service — pass-through shape.
- **Preserve** every public method signature, every `[SerializeField]` field, every Unity event/coroutine attachment point on Manager.
- **Validate** scene load + 1 PlayMode smoke per stage to catch wiring break early.
- **Defer** hub move (Manager → `Domains/{X}/`) + scene rebind to a follow-up plan.

## 3. Approaches surveyed

(Locked-doc note: this section retained for audit trail; user already selected approach in prior brief — see §4.)

| # | Name | Shape | Pro | Con |
|---|---|---|---|---|
| 1a | Full move + rename | Manager `git mv`'d to `Domains/{X}/`, class renamed, scene rebind in same stage | Single hop to final architecture | Massive scene-wiring break risk; Unity .meta GUID migration; out of cutover scope |
| **1b** | **Pass-through + scene-side glue** | **Manager file stays in place, class stays, `[SerializeField]` stays, methods delegate to Domain Service** | **Surface-preserving; scene-safe; trim is mechanical** | **Two surfaces still exist — Manager + Service. Hub move deferred.** |
| 1c | Inline-only | Trim duplicated body, no delegation; logic moved entirely into Service, Manager methods empty | Smallest Manager file | Breaks scene wiring if any Editor inspector or coroutine reads Manager state mid-call |

## 4. Recommendation

**Approach 1b — pass-through + scene-side glue.** Locked.

## 5. Open questions resolved

| Q | Question | Locked answer |
|---|---|---|
| Q1 | Manager shape post-cutover | **1b** — pass-through; `[SerializeField]` reads + Unity events + coroutines stay inside Manager |
| Q2 | Bridge mutations stage shape | **2a** — one stage, full inline of all 20+ mutation kinds into `MutationDispatchService` |
| Q3 | Per-stage smoke gate | **3b** — compile + scene-load + 1 PlayMode smoke (AUTO ProcessTick OR zone-place per Manager domain) |
| Q4 | Stage cardinality | **4c** — tracer (1.0) + 5 = 6 stages total |
| Q5 | Dead-public handling | **5b** — strict for live callers; dead publics (zero callers) flagged in §Open Questions per stage for human decision |

---

## Design Expansion

### Chosen Approach

**Approach 1b — pass-through + scene-side glue cutover.**

Per-stage shape (post-cutover):

```
Assets/Scripts/Managers/GameManagers/{ManagerName}.cs        (~150-250 LOC after trim)
├── [SerializeField] fields                                  (UNTOUCHED — scene wiring preserved)
├── Awake/Start/OnEnable/OnDisable                           (Unity lifecycle — UNTOUCHED)
├── coroutines + StartCoroutine call sites                   (UNTOUCHED — Manager owns ticks)
├── public method signatures                                 (UNTOUCHED — caller surface preserved)
└── method body                                              → trimmed to: return _service.Method(args);

Assets/Scripts/Domains/{X}/Services/{Y}Service.cs            (grows to absorb full algorithm body)
├── POCO class                                               (no MonoBehaviour, no [SerializeField])
└── pure methods                                             (testable in isolation)
```

Selection rationale (vs 1a / 1c):
- 1a (full move + rename) — defers correctly to a separate plan; scene rebind risk too high to bundle.
- 1c (inline-only, no delegation) — breaks Manager methods that mid-call read Manager state ([SerializeField] / cached transform / coroutine handle).
- 1b — preserves every Unity-side seam; trim is mechanical (body → delegate); follow-up plan handles hub move when ready.

### Architecture

```mermaid
flowchart LR
  subgraph SceneSide[Scene-side glue — UNTOUCHED]
    direction TB
    Sc[Scene wiring + prefab refs]
    SF["[SerializeField] fields"]
    UL[Unity lifecycle]
    Co[Coroutines]
  end

  subgraph Manager[Assets/Scripts/Managers/**/{Manager}.cs]
    direction TB
    MgrCls[class Manager : MonoBehaviour]
    MgrSig[public method signatures preserved]
    MgrBody["method body: return _service.Method(args);"]
  end

  subgraph Domain[Assets/Scripts/Domains/{X}/Services/{Y}Service.cs]
    direction TB
    SvcCls[POCO class Service]
    SvcLogic[full algorithm body]
  end

  Sc --> MgrCls
  SF --> MgrCls
  UL --> MgrCls
  Co --> MgrCls
  MgrCls --> MgrSig
  MgrSig --> MgrBody
  MgrBody --> SvcCls
  SvcCls --> SvcLogic
```

#### Red-Stage Proof — Stage 1.0 (InterstateManager tracer)

```python
# tracer cutover — InterstateManager.cs (1149 LOC → ~150 LOC)
def stage_1_0_cutover():
    target = "Assets/Scripts/Managers/GameManagers/InterstateManager.cs"
    domain_port = "Assets/Scripts/Domains/Roads/Services/InterstateService.cs"
    # 1. Domain port grows from 90 LOC stub to ~1000 LOC absorbing full algorithm body
    grow_domain_port(domain_port, absorb_body_from=target)
    # 2. Manager file stays at original path; class name + namespace UNTOUCHED
    assert path_unchanged(target)
    assert class_name_unchanged("InterstateManager")
    assert namespace_unchanged()
    # 3. [SerializeField] fields UNTOUCHED — preserve scene wiring
    assert serialized_fields_diff(target) == set()
    # 4. Manager constructs Domain service via composition
    inject_service_field(target, "_interstateService", "new InterstateService(this)")
    # 5. Each public method body becomes: return _interstateService.Method(args);
    for method in public_methods(target):
        if method in delegated_methods:
            assert method_body_is_passthrough(target, method)
        else:
            flag_dead_public_in_open_questions(method)  # Q5 = 5b
    # 6. Unity lifecycle (Awake/Start/OnEnable) + coroutines UNTOUCHED
    assert unity_lifecycle_diff(target) == set()
    # 7. Smoke gate — Q3 = 3b
    assert csharp_compile_passes()
    assert scene_loads("MainScene")
    assert playmode_smoke("AUTO ProcessTick interstate auto-build") == "GREEN"
    # 8. Visibility delta
    assert interstate_auto_build_still_ticks()
```

#### Red-Stage Proof — Stage 2 (UIManager.Theme partial)

```python
def stage_2_cutover():
    target = "Assets/Scripts/Managers/GameManagers/UIManager.Theme.cs"  # 1004 LOC, partial
    domain_port = "Assets/Scripts/Domains/UI/Services/ThemeService.cs"  # 112 LOC port
    # Domain port grows to absorb remaining theme body
    grow_domain_port(domain_port, absorb_body_from=target)
    assert path_unchanged(target)
    assert partial_class_signature_unchanged("UIManager")  # partial keyword preserved
    inject_service_field(target, "_themeService")
    for method in public_methods(target):
        replace_body_with_delegate(target, method, "_themeService")
    assert csharp_compile_passes()
    assert scene_loads("MainScene")
    assert playmode_smoke("UI theme swap applies same colors/fonts") == "GREEN"
```

#### Red-Stage Proof — Stage 3 (RoadPrefabResolver)

```python
def stage_3_cutover():
    target = "Assets/Scripts/Managers/GameManagers/RoadPrefabResolver.cs"  # 1122 LOC
    domain_port = "Assets/Scripts/Domains/Roads/Services/PrefabResolverService.cs"  # 938 LOC
    # Domain port already substantial (938 LOC); trim Manager body, delegate
    assert path_unchanged(target)
    assert serialized_fields_diff(target) == set()
    inject_service_field(target, "_prefabResolverService")
    for method in public_methods(target):
        replace_body_with_delegate(target, method, "_prefabResolverService")
    assert csharp_compile_passes()
    assert scene_loads("MainScene")
    assert playmode_smoke("AUTO road-place returns same prefab GUIDs") == "GREEN"
```

#### Red-Stage Proof — Stage 4 (GeographyManager)

```python
def stage_4_cutover():
    target = "Assets/Scripts/Managers/GameManagers/GeographyManager.cs"  # 1143 LOC
    domain_port = "Assets/Scripts/Domains/Geography/Services/GeographyWaterDesirabilityService.cs"  # 49 LOC stub
    # Domain port grows to absorb full geography body
    grow_domain_port(domain_port, absorb_body_from=target)
    assert path_unchanged(target)
    assert serialized_fields_diff(target) == set()
    inject_service_field(target, "_geographyService")
    for method in public_methods(target):
        replace_body_with_delegate(target, method, "_geographyService")
    assert csharp_compile_passes()
    assert scene_loads("MainScene")
    assert playmode_smoke("zone-place reads identical desirability") == "GREEN"
```

#### Red-Stage Proof — Stage 5 (TerraformingService Manager)

```python
def stage_5_cutover():
    # NOTE: target is the MANAGER file, not the Domain port
    target = "Assets/Scripts/Managers/GameManagers/TerraformingService.cs"  # 1095 LOC
    domain_port = "Assets/Scripts/Domains/Terrain/Services/TerraformingService.cs"  # 730 LOC port
    # Domain port already absorbs most logic; trim Manager body
    assert path_unchanged(target)
    assert serialized_fields_diff(target) == set()
    inject_service_field(target, "_terraformingService")
    for method in public_methods(target):
        replace_body_with_delegate(target, method, "_terraformingService")
    assert csharp_compile_passes()
    assert scene_loads("MainScene")
    assert playmode_smoke("AUTO terraform op mutates height identically") == "GREEN"
```

#### Red-Stage Proof — Stage 6 (Bridge Mutations full inline, single stage)

```python
def stage_6_cutover():
    # Q2 = 2a — single stage, full inline of all 20+ mutation kinds
    target = "Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs"  # 1386 LOC
    domain_port = "Assets/Scripts/Domains/Bridge/Services/MutationDispatchService.cs"  # 31 LOC stub
    # Domain port grows from 31 LOC stub → ~1100 LOC (full mutation body)
    mutation_kinds = enumerate_mutation_kinds(target)  # 20+ kinds
    assert len(mutation_kinds) >= 20
    for kind in mutation_kinds:
        port_kind_to_dispatch_service(kind, source=target, dest=domain_port)
    # Editor partial class stays at original path; preserves Editor-only ref boundary
    assert path_unchanged(target)
    inject_service_field(target, "_dispatchService")
    for method in public_methods(target):
        replace_body_with_delegate(target, method, "_dispatchService")
    assert csharp_compile_passes()
    assert scene_loads("MainScene")
    # Big test surface — round-trip every mutation kind
    for kind in mutation_kinds:
        assert bridge_smoke_round_trip(kind) == "GREEN"
```

### Subsystem Impact

| # | Stage | Manager file | LOC before | LOC after | Domain port | Port LOC before | Port LOC after | Invariant risk | Mitigation |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 1.0 (tracer) | `InterstateManager.cs` | 1149 | ~150 | `Domains/Roads/Services/InterstateService.cs` | 90 | ~1000 | Inv #2 (InvalidateRoadCache), #10 (PathTerraformPlan) | Domain service preserves cache invalidation + plan order; Manager preserves coroutine attachment |
| 2 | 2 | `UIManager.Theme.cs` | 1004 | ~150 | `Domains/UI/Services/ThemeService.cs` | 112 | ~900 | Guardrail #5 (partial-class declaration) | Partial keyword preserved on UIManager; ThemeService POCO absorbs body |
| 3 | 3 | `RoadPrefabResolver.cs` | 1122 | ~180 | `Domains/Roads/Services/PrefabResolverService.cs` | 938 | ~1100 | Inv #2 | Cache invalidation preserved in port; Manager pass-through |
| 4 | 4 | `GeographyManager.cs` | 1143 | ~200 | `Domains/Geography/Services/GeographyWaterDesirabilityService.cs` | 49 | ~1000 | Inv #1 (HeightMap/Cell sync), #7 (shore band), #8 (river bed monotonic), #9 (cliff) | Service preserves sync logic; Manager keeps `[SerializeField]` for desirability tunables |
| 5 | 5 | `TerraformingService.cs` (Manager) | 1095 | ~180 | `Domains/Terrain/Services/TerraformingService.cs` | 730 | ~900 | Inv #1, #7 | Port already covers core; Manager pass-through preserves Editor coroutine hook |
| 6 | 6 | `AgentBridgeCommandRunner.Mutations.cs` | 1386 | ~250 | `Domains/Bridge/Services/MutationDispatchService.cs` | 31 | ~1100 | Inv guardrail #14 (Mutations partial pattern) | Single-stage full inline; preserves Editor-only ref boundary; partial class shape kept on Manager file |

**Cross-cutting observations:**
- Every Manager keeps original path + class name + namespace + `[SerializeField]` fields → scene wiring NEVER breaks across this plan.
- `[SerializeField]` immutability is the load-bearing invariant of this plan; per-stage smoke gate (Q3 = 3b) catches drift if introduced accidentally.
- Hub move (`Assets/Scripts/Managers/**` → `Assets/Scripts/Domains/{X}/`) deferred to follow-up plan; that plan owns the scene-rebind + prefab GUID migration.
- Per-stage §Open Questions captures dead-public list (Q5 = 5b) — human decides whether to delete or retain pre-hub-move.

### Implementation Points

**Per-stage task pattern (mechanical):**

1. **Body absorption** — port full algorithm body from Manager into Domain Service file (file size grows).
2. **Service injection** — add `private readonly {Y}Service _service = new {Y}Service(this);` field on Manager (keeps Unity composition shape).
3. **Method delegation** — replace each public method body with single-line `return _service.Method(args);` (or void equivalent).
4. **`[SerializeField]` audit** — diff `[SerializeField]` field set pre/post. Must be empty.
5. **Unity lifecycle audit** — Awake/Start/OnEnable/OnDisable diff. Must be empty (or only the service-injection delta).
6. **Coroutine audit** — `StartCoroutine` call sites unchanged.
7. **Dead-public sweep** — for each public method, count call sites. Zero call sites → flag in stage §Open Questions per Q5 = 5b.
8. **Smoke gate** — compile + scene load + 1 PlayMode smoke per Q3 = 3b.
9. **BUG-63 dependency** — Pass B `asset_database_refresh` MUST land before Stage 1.0 to capture .meta growth on Domain Service file LOC expansion.

**Per-stage smoke matrix (Q3 = 3b):**

| Stage | Compile | Scene load | PlayMode smoke kind |
|---|---|---|---|
| 1.0 | required | MainScene | AUTO ProcessTick (interstate auto-build tick) |
| 2 | required | MainScene | UI theme swap (color/font assertion) |
| 3 | required | MainScene | AUTO road-place (prefab resolution) |
| 4 | required | MainScene | zone-place (geography desirability read) |
| 5 | required | MainScene | AUTO terraform (height/cliff mutation) |
| 6 | required | MainScene | bridge round-trip per mutation kind (20+ kinds in single smoke run) |

**Stage 6 risk acknowledgment (Q2 = 2a):**

Single-stage full inline of 20+ mutation kinds = biggest diff + biggest test surface in this plan. Mitigations:
- Preserve dispatch shape — `MutationDispatchService` exposes one method per kind, mirrors current partial.
- Preserve Editor-only ref boundary — Manager file stays in `Assets/Scripts/Editor/`, partial class shape kept.
- Smoke gate runs round-trip on every kind enumerated, not just sampled.
- If smoke fails on one kind, rollback just that kind's port (per-kind atomicity inside the stage).

### Examples

**Tracer (Stage 1.0) — `InterstateManager.cs`:**

Before (sketch, 1149 LOC):
```csharp
namespace TerritoryDeveloper.Managers.GameManagers
{
    public class InterstateManager : MonoBehaviour
    {
        [SerializeField] private float _autoBuildTickRate = 0.5f;
        [SerializeField] private GameObject _interstatePrefab;
        // ... ~30 more [SerializeField] fields ...

        private void Awake() { /* lifecycle */ }
        private IEnumerator AutoBuildTickCoroutine() { /* coroutine */ }

        public void StartAutoBuild()
        {
            // ~80 LOC of algorithm body
            ...
        }

        public bool TryPlaceInterstate(Vector2Int pos)
        {
            // ~120 LOC of algorithm body
            ...
        }
        // ... ~30 more public methods, total ~1100 LOC of body ...
    }
}
```

After cutover (~150 LOC):
```csharp
namespace TerritoryDeveloper.Managers.GameManagers
{
    public class InterstateManager : MonoBehaviour
    {
        [SerializeField] private float _autoBuildTickRate = 0.5f;          // UNTOUCHED
        [SerializeField] private GameObject _interstatePrefab;              // UNTOUCHED
        // ... all original [SerializeField] fields UNTOUCHED ...

        private InterstateService _service;

        private void Awake()                                                // lifecycle UNTOUCHED
        {
            _service = new InterstateService(this);
        }

        private IEnumerator AutoBuildTickCoroutine()                        // coroutine UNTOUCHED
        {
            return _service.AutoBuildTickCoroutine(_autoBuildTickRate);
        }

        public void StartAutoBuild() => _service.StartAutoBuild();
        public bool TryPlaceInterstate(Vector2Int pos) => _service.TryPlaceInterstate(pos);
        // ... ~30 more public methods, each = single-line delegate ...
    }
}
```

`InterstateService.cs` (Domain port) grows from 90 LOC stub → ~1000 LOC absorbing full algorithm body. POCO, testable in isolation, no `[SerializeField]`, no MonoBehaviour.

**Edge case — Stage 2 partial class preservation:**

`UIManager.Theme.cs` (1004 LOC) is partial of `UIManager`. Same rule — `public partial class UIManager` declaration MUST stay. Service injection field declared on `.Theme.cs` partial.

**Edge case — Stage 6 partial class preservation:**

`AgentBridgeCommandRunner.cs` (1754 LOC) + `.Mutations.cs` (1386 LOC) + `.Conformance.cs` (1219 LOC) = partial-class family. This plan only cuts over `.Mutations.cs`. Partial class declaration on `.Mutations.cs` MUST stay (`public partial class AgentBridgeCommandRunner`) — removing it would break the partial-class composition with the other two files. Service injection field declared on `.Mutations.cs` partial; visible to other partials via shared `this`.

### Review Notes

**BLOCKING resolved (3):**
- Stage 5 file-name ambiguity disambiguated — Manager file at `Assets/Scripts/Managers/GameManagers/TerraformingService.cs` (1095 LOC) vs Domain port at `Assets/Scripts/Domains/Terrain/Services/TerraformingService.cs` (730 LOC). Per-stage YAML `target_manager` + `domain_port` paths explicit.
- BUG-63 prerequisite captured — `.meta` capture in stage commit must land before Stage 1.0 to avoid orphan .meta accumulation as Domain Service files grow.
- Stage 6 partial-class declaration preservation explicit — removing the `partial` keyword on `.Mutations.cs` would break `AgentBridgeCommandRunner` composition with `.Conformance.cs` + main file.

**NON-BLOCKING carried (4):**
- Hub move follow-up plan — once cutover complete, separate plan handles `Assets/Scripts/Managers/**` → `Assets/Scripts/Domains/{X}/` move + scene rebind + prefab GUID migration. NOT this plan.
- Dead-public retention — Q5 = 5b means each stage's §Open Questions accumulates a list of zero-caller publics. Aggregate decision (delete vs retain) made post-cutover, before hub-move plan. Carry list across stages.
- Domain service growth tracking — service files grow substantially (e.g. InterstateService 90 → ~1000 LOC; MutationDispatchService 31 → ~1100 LOC). Post-cutover, each Domain service may need its own atomization sub-stage in a future plan. Note in §Open Questions per stage.
- `validate:red-stage-proof-anchor` compatibility — this plan uses logic-body trim + delegate, not test-method extension. Anchor target = pass-through method body shape (`return _service.Method(args);`). Validator may need carve-out for "single-line delegate" pattern.

### Expansion metadata

- **Date:** 2026-05-08
- **Model:** claude-opus-4-7
- **Approach selected:** 1b — pass-through + scene-side glue
- **Blocking items resolved:** 3
- **Non-blocking carried:** 4
- **Mode:** standard (Approaches list present; locked-decision brief provided by user — interview phase 0.5 skipped)
- **Phases completed:** 0–9 (0.5 skipped per locked brief; 2.5 inline DEC note — no new arch surface lock; this plan operates within DEC `large-file-atomization-folder-shape-v1`)

---

## Next

`/master-plan-new --from-exploration docs/explorations/large-file-atomization-cutover-refactor.md` — author master plan + seed Stages 1.0..6 + open BACKLOG TECH umbrella + per-stage tasks.
