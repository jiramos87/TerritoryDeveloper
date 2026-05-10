<!-- bake_handler_version: 3 -->
<!-- Blueprint: ui-from-db.md — canonical task blueprint for ui_from_db task_kind. -->
<!-- Version stamp = UiBakeHandler schema_version (panels.json). Bump when handler changes. -->
<!-- Section ids are deterministic — ship-plan Phase 4 detects these exact headings. -->

# Blueprint: UI-from-DB task

Canonical blueprint for tasks that bake UI prefabs from a Postgres catalog (DB-driven UI).
Use `task_kind: ui_from_db` in backlog yaml to trigger ship-plan blueprint loader.

---

## Schema-Probe

**Purpose.** Verify the catalog DB row (panel / archetype / widget) exists and matches expected shape before any bake attempt.

### §Goal

{{intent_one_liner — describe what DB row(s) to probe and what shape fields are required}}

### §Red-Stage Proof anchor placeholder

```
red_test_anchor: unit-test:tests/{{plan-slug}}/{{stage-file}}.test.mjs::{{testMethodName}}
target_kind: tracer_verb
proof_artifact_id: tests/{{plan-slug}}/{{stage-file}}.test.mjs
proof_status: failed_as_expected
```

### §Work Items

- [ ] Query catalog table for `entry_id = {{catalog_entry_id}}`; assert row exists + `panel_kind` field matches expected value.
- [ ] Confirm `params_json` schema matches current `UiBakeHandler` schema_version (bake_handler_version: 3).
- [ ] Fail test if row absent or schema mismatch.

---

## Bake-Apply

**Purpose.** Invoke `catalog_preview` bridge command (or direct bake call) and assert prefab materializes without errors.

### §Goal

{{intent_one_liner — describe which catalog entry bakes and what prefab path should be produced}}

### §Red-Stage Proof anchor placeholder

```
red_test_anchor: unit-test:tests/{{plan-slug}}/{{stage-file}}.test.mjs::{{testMethodName}}
target_kind: tracer_verb
proof_artifact_id: tests/{{plan-slug}}/{{stage-file}}.test.mjs
proof_status: failed_as_expected
```

### §Work Items

- [ ] Call `unity_bridge_command(kind="catalog_preview", catalog_entry_id={{entry_id}})`.
- [ ] Assert `response.screenshot_path` non-null and file exists on disk.
- [ ] Assert no `compilation_failed` flag in bridge response.
- [ ] Confirm output prefab path under `Assets/UI/Prefabs/Generated/{{expected_stem}}.prefab`.

---

## Render-Check

**Purpose.** Capture a screenshot and assert visual correctness: panel visible, no missing sprites, no Z-fighting.

### §Goal

{{intent_one_liner — describe what visual state the prefab should present after bake}}

### §Red-Stage Proof anchor placeholder

```
red_test_anchor: unit-test:tests/{{plan-slug}}/{{stage-file}}.test.mjs::{{testMethodName}}
target_kind: visibility_delta
proof_artifact_id: tests/{{plan-slug}}/{{stage-file}}.test.mjs
proof_status: failed_as_expected
```

### §Work Items

- [ ] Call `unity_bridge_command(kind="capture_screenshot", include_ui=true)`.
- [ ] Assert screenshot file size > 0 bytes.
- [ ] Inspect `sorting_order_debug` on seed cell — no duplicate sorting_order within same layer.
- [ ] Compare against baseline snapshot (if exists); flag pixel diff > threshold.

---

## Console-Sweep

**Purpose.** Assert Unity console is error-free after bake + render. Catch missing script references, null-refs, missing prefab links.

### §Goal

{{intent_one_liner — describe the clean-console gate expected after full bake cycle}}

### §Red-Stage Proof anchor placeholder

```
red_test_anchor: unit-test:tests/{{plan-slug}}/{{stage-file}}.test.mjs::{{testMethodName}}
target_kind: tracer_verb
proof_artifact_id: tests/{{plan-slug}}/{{stage-file}}.test.mjs
proof_status: failed_as_expected
```

### §Work Items

- [ ] Call `unity_bridge_command(kind="get_console_logs", severity_filter="error")`.
- [ ] Assert `response.log_lines` is empty (zero error lines).
- [ ] Assert `prefab_manifest` shows no missing script references for baked prefab.
- [ ] Document any expected/suppressed errors in task notes.

---

## Tracer

**Purpose.** End-to-end integration smoke: full play-mode entry → panel visible → action fires → no console errors. Stage file flips green here.

### §Goal

{{intent_one_liner — describe the tracer action that closes the loop (click, open, navigate)}}

### §Red-Stage Proof anchor placeholder

```
red_test_anchor: unit-test:tests/{{plan-slug}}/{{stage-file}}.test.mjs::{{testMethodName}}
target_kind: tracer_verb
proof_artifact_id: tests/{{plan-slug}}/{{stage-file}}.test.mjs
proof_status: failed_as_expected
```

### §Work Items

- [ ] Enter play mode via `unity_bridge_command(kind="enter_play_mode")`.
- [ ] Assert `GridManager.isInitialized` via `response.ready`.
- [ ] Trigger panel open action via `UiActionRegistry` dispatch.
- [ ] Assert panel visible (`ui_tree_walk` finds panel root GO active).
- [ ] Sweep console for errors post-action.
- [ ] Exit play mode via `unity_bridge_command(kind="exit_play_mode")`.
- [ ] Stage test file asserts all 5 section behaviors green.
