# grid-asset-visual-registry — Stage 2.1 Plan Digest

Compiled 2026-04-22 from 5 task spec(s).

---

## §Plan Digest

### §Goal

Ship Postgres-backed reader for catalog export: published-default query path plus optional drafts, deterministic ordering, joins aligned to snapshot DTOs.

### §Acceptance

- [ ] Published-only default query wired; draft flag documented on CLI.
- [ ] Join covers asset + sprite + bind + economy columns needed for snapshot.
- [ ] Deterministic sort keys documented in §7 or task §Findings.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| export_query_smoke | local DB with Zone S seed | non-empty published set | node |
| ordering_stable | two runs same DB | identical row order | node |

### §Examples

| Mode | Filter | Expected |
|------|--------|----------|
| default | `status = published` | ship-visible rows only |
| dev flag | include draft | larger row set for editor |

### §Mechanical Steps

#### Step 1 — tighten implementation checklist

**Goal:** Lock reader deliverables inside spec §7 for reviewers.

**Edits:**

- `TECH-662 (archived spec)` — **before**:

```
- [ ] Implement SQL or Drizzle-free query module, unit/integration smoke against fixture DB or mocked pool; document connection env (DATABASE_URL).
```

  **after**:

```
- [ ] Implement SQL or Drizzle-free query module against `getSql()` / `sql` from `web/lib/db/client.ts`; unit/integration smoke against fixture DB or mocked pool; document connection env (DATABASE_URL).
- [ ] Document default `published` filter + dev flag name in §7b Notes column.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Re-open §7 bullet if validator reports spec path drift.

**MCP hints:** `backlog_issue`, `plan_digest_resolve_anchor`

#### Step 2 — wire npm entry (stub ok)

**Goal:** Expose `catalog:export` from repo root for later serializer wiring.

**Edits:**

- `package.json` — **before**:

```
    "validate:catalog-dto": "node tools/scripts/catalog-dto-migration-check.mjs",
    "validate:parent-plan-locator": "node tools/validate-parent-plan-locator.mjs",
```

  **after**:

```
    "validate:catalog-dto": "node tools/scripts/catalog-dto-migration-check.mjs",
    "catalog:export": "node -e \"process.exit(0)\"",
    "validate:parent-plan-locator": "node tools/validate-parent-plan-locator.mjs",
```

**Gate:**

```bash
node tools/validate-dead-project-spec-paths.mjs
```

**STOP:** If gate non-zero, fix JSON comma placement in `package.json`.

**MCP hints:** `plan_digest_resolve_anchor`

---
## §Plan Digest

### §Goal

Freeze snapshot JSON envelope: `schemaVersion`, `generatedAt`, ordered arrays matching catalog DTOs so Unity loader (Stage 2.2) compiles against one contract.

### §Acceptance

- [ ] Top-level `schemaVersion` + `generatedAt` ISO-8601 in output object.
- [ ] Arrays sorted by stable keys documented in §7.
- [ ] Golden test or fixture proves byte-stable serialization for fixed input rows.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| snapshot_sort | two logical row orderings | same JSON bytes after sort | node |
| schema_bump_doc | n/a | §7 records bump rule | manual |

### §Examples

| Field | Example | Notes |
|-------|---------|-------|
| schemaVersion | `1` | integer bump on breaking change |
| generatedAt | `2026-04-22T12:00:00.000Z` | UTC ISO |

### §Mechanical Steps

#### Step 1 — extend §7 serializer bullets

**Goal:** Bind serializer work to typed rows from TECH-662 reader.

**Edits:**

- `TECH-663 (archived spec)` — **before**:

```
- [ ] TypeScript interfaces matching DTOs; JSON.stringify with ordered keys; golden fixture test for sort stability.
```

  **after**:

```
- [ ] TypeScript interfaces matching `web/types/api/catalog*.ts`; explicit sort functions before `JSON.stringify`; golden fixture under `tools/mcp-ia-server/tests/fixtures/` or sibling `tools/` test dir.
- [ ] Document `schemaVersion` bump protocol in §6 Decision Log when layout breaks.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Restore §7 text if validator flags spec links.

**MCP hints:** `plan_digest_resolve_anchor`

#### Step 2 — cross-link exploration snapshot prose

**Goal:** Keep human-readable contract next to §8.2 diagram in exploration doc.

**Edits:**

- `docs/grid-asset-visual-registry-exploration.md` — **before**:

```
### 8.2 Architecture
```

  **after**:

```
### 8.2 Architecture

<!-- catalog-snapshot-schema-TECH-663: top-level `{ schemaVersion, generatedAt, assets[], ... }` matches hand-written DTOs; stable sorts defined in Stage 2.1 export code. -->
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** If anchor matches twice, narrow **before** block with extra blank line context from file HEAD.

**MCP hints:** `plan_digest_resolve_anchor`

---
## §Plan Digest

### §Goal

Write serializer output to a single Unity-visible path under repo (`StreamingAssets` vs `Resources`) with documented `.meta` + reload notes.

### §Acceptance

- [ ] Export CLI writes bytes to chosen path idempotently.
- [ ] Stage 2.1 Exit doc names path + rationale in §7 or §Findings.
- [ ] No Unity C# changes in this task.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| path_write | temp out dir | file bytes match serializer | node |

### §Examples

| Choice | Pros | Cons |
|--------|------|------|
| StreamingAssets | raw JSON friendly | platform path quirks |
| Resources | simple `Resources.Load` | size + cache semantics |

### §Mechanical Steps

#### Step 1 — document path decision in spec

**Goal:** Lock filesystem contract for Stage 2.2 loader.

**Edits:**

- `TECH-664 (archived spec)` — **before**:

```
- [ ] fs write + path resolve from repo root; document in Stage 2.1 Exit / exploration cross-link.
```

  **after**:

```
- [ ] fs write + path resolve from repo root into `Assets/StreamingAssets/` subtree (default) unless §6 records Resources exception; mkdir -p; overwrite idempotent.
- [ ] Add §6 row citing `docs/grid-asset-visual-registry-exploration.md` §8.2 for hot-reload expectation stub.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Revert §7 if master-plan anchor link breaks.

**MCP hints:** `backlog_issue`

#### Step 2 — mention output path in exploration

**Goal:** Single canonical human reference for Unity integrators.

**Edits:**

- `docs/grid-asset-visual-registry-exploration.md` — **before**:

```
        └── Export step → Unity-consumable snapshot + sprite import hygiene (PPU, pivot)
```

  **after**:

```
        └── Export step → Unity-consumable snapshot + sprite import hygiene (PPU, pivot) — default file path documented in Stage 2.1 TECH-664 §7/§Findings (`Assets/StreamingAssets/...` unless otherwise decided)
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** If exploration anchor not unique, expand **before** with surrounding tree indentation from HEAD.

**MCP hints:** `plan_digest_resolve_anchor`

---
## §Plan Digest

### §Goal

Attach texture import hygiene data (paths + PPU/pivot hints) beside core snapshot so allowlisted `TextureImporter` passes stay data-driven.

### §Acceptance

- [ ] Manifest section lists sprite paths eligible for hygiene.
- [ ] PPU/pivot fields follow exploration §6 wording; gaps logged in §Findings.
- [ ] Consumer automation explicitly stubbed in §7 if no Editor script lands here.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| manifest_shape | fixture rows | JSON validates against TS type | node |

### §Examples

| Row kind | Hygiene fields |
|----------|----------------|
| allowlisted PNG | `texturePath`, `pixelsPerUnit`, `pivot` |

### §Mechanical Steps

#### Step 1 — expand §7 hygiene deliverables

**Goal:** Keep import policy out of Unity C# for this task.

**Edits:**

- `TECH-665 (archived spec)` — **before**:

```
- [ ] Additional JSON section or sidecar file; validate against sample rows.
```

  **after**:

```
- [ ] Additional JSON section or sidecar file adjacent to main snapshot; validate against sample rows drawn from seeded catalog.
- [ ] Reference `ia/specs/coding-conventions.md` TextureImporter expectations in §6.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Fix spec tables if markdownlint surfaces issues during implement.

**MCP hints:** `plan_digest_resolve_anchor`

#### Step 2 — cite hygiene in exploration import bullet

**Goal:** Readers see data contract next to architecture tree.

**Edits:**

- `docs/grid-asset-visual-registry-exploration.md` — **before**:

```
              ├── GridAssetCatalog (boot loader, in-memory snapshot)
```

  **after**:

```
              ├── GridAssetCatalog (boot loader, in-memory snapshot)
              │     └── Import hygiene manifest (TECH-665) lists allowlisted texture paths + PPU/pivot hints for baker tooling
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Widen **before** snippet with parent bullets if multiple matches.

**MCP hints:** `plan_digest_resolve_anchor`

---
## §Plan Digest

### §Goal

Add `catalog:export:check` (or argv flag on shared CLI) comparing deterministic hash of export output to on-disk snapshot for CI drift signaling.

### §Acceptance

- [ ] Non-zero exit when snapshot bytes drift from regenerated export.
- [ ] Hash excludes secrets; documents inputs in §7.
- [ ] Advisory CI usage documented in §Findings without blocking `validate:all` unless opted in.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| check_pass | snapshot matches | exit 0 | node |
| check_fail | mutated file | exit non-zero | node |

### §Examples

| Flag | Behavior |
|------|----------|
| `--check` | no write; compare only |

### §Mechanical Steps

#### Step 1 — extend §7 for hash semantics

**Goal:** Capture deterministic hash inputs for exploration §7 baker themes.

**Edits:**

- `TECH-666 (archived spec)` — **before**:

```
- [ ] Parse args, run export in memory, diff vs on-disk file, stderr message on mismatch.
```

  **after**:

```
- [ ] Parse args (`--check`), run export in memory only, compute digest of canonical JSON bytes, compare to on-disk snapshot path from TECH-664; stderr on mismatch; exit codes documented in §7b.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Repair §7 numbering if phases change.

**MCP hints:** `backlog_issue`

#### Step 2 — register check script beside export stub

**Goal:** CI can call check without invoking full `validate:all`.

**Edits:**

- `package.json` — **before**:

```
    "catalog:export": "node -e \"process.exit(0)\"",
    "validate:parent-plan-locator": "node tools/validate-parent-plan-locator.mjs",
```

  **after**:

```
    "catalog:export": "node -e \"process.exit(0)\"",
    "catalog:export:check": "node -e \"process.exit(0)\"",
    "validate:parent-plan-locator": "node tools/validate-parent-plan-locator.mjs",
```

**Gate:**

```bash
node tools/validate-dead-project-spec-paths.mjs
```

**STOP:** Ensure TECH-662 landed first so `catalog:export` line exists; otherwise insert both lines in one edit during implement.

**MCP hints:** `plan_digest_resolve_anchor`


## Final gate

```bash
npm run validate:all
```
