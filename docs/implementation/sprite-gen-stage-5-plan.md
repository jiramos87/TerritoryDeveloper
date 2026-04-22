# sprite-gen — Stage 5 Plan Digest

Compiled 2026-04-22 from 6 task spec(s).

---


## §Plan Digest

### §Goal

Ship a small **HTTP client** the promote pipeline and tests reuse, aligned with **tg-catalog-api** contract from grid registry work (**TECH-640**..**645** archived).

### §Acceptance

- [ ] `RegistryClient` methods + exception types implemented.
- [ ] `requests` declared in `tools/sprite-gen/requirements.txt`.
- [ ] `npm run validate:all` passes.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| n/a here |  |  | covered in **TECH-678** |

### §Examples

| Case | Outcome | Notes |
|------|---------|-------|
| 201 on POST | parsed JSON dict | return body |
| 409 | raise `ConflictError` | carry server body if present |

### §Mechanical Steps

#### Step 1 — Add `requests` dependency

**Goal:** Declare HTTP library for client module.

**Edits:**

- `tools/sprite-gen/requirements.txt` — **before**:
  ```
  pyyaml>=6,<7
  ```
  **after**:
  ```
  pyyaml>=6,<7
  requests>=2,<3
  ```

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:all
```

**STOP:** Re-open **TECH-674** project spec **§7** (see `BACKLOG.md` / issue row) if the dependency line reverts during merge.

**MCP hints:** `backlog_issue` (TECH-674), `glossary_lookup` (catalog asset) if terms drift.

#### Step 2 — Implement `RegistryClient` in new `src` module

**Goal:** New module under existing package root with three methods and exception tree.

**Edits:** Author new source file next to `tools/sprite-gen/src/cli.py` (same directory layout); implement JSON POST/PATCH/GET using base URL from constructor. Do not copy live secrets into the repo. Keep docstrings in English per **coding** norms.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:all
```

**STOP:** If package name under `src/` is not the one expected by `python -m sprite_gen`, read `tools/sprite-gen/src/__init__.py` and table the actual import path before growing the client.

**MCP hints:** `backlog_issue` (TECH-674), `invariants_summary` (universal only — no Unity C#).

---
## §Plan Digest

### §Goal

One importable `resolve_catalog_url()` with documented precedence, plus a typed `CatalogConfigError` for missing config when a caller still intends to push.

### §Acceptance

- [ ] Env beats `config.toml` `[catalog] url` when both set (test proves).
- [ ] When neither set and caller requires URL, `CatalogConfigError` raised.
- [ ] `npm run validate:all` green.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_resolve_env_wins | env set, config has different url | env value | pytest |
| test_resolve_config | no env, config url | config value | pytest |
| test_resolve_both_missing_push | no env, empty catalog url | `CatalogConfigError` | pytest |

### §Examples

| Input | Outcome | Notes |
|-------|---------|-------|
| `TG_CATALOG_API_URL=https://a` | `https://a` | strip trailing slash policy in **Decision Log** |

### §Mechanical Steps

#### Step 1 — Implement resolver beside `RegistryClient` scaffolding

**Goal:** Add `resolve_catalog_url()` and `CatalogConfigError` in `registry_client` module; read env + TOML.

**Edits:**

- `tools/sprite-gen/src/registry_client.py` — **before**:
  ```
  """RegistryClient — HTTP access to tg-catalog-api (TECH-674..)."""

  # Scaffold only; implement in TECH-674.

  class RegistryClientError(Exception):
      """Base for registry client failures."""


  __all__ = ["RegistryClientError"]
  ```
  **after**:
  ```
  import os
  from pathlib import Path

  # ... (implementer inserts TOML load for tools/sprite-gen/config.toml via tool root)

  class RegistryClientError(Exception):
      """Base for registry client failures."""


  class CatalogConfigError(RegistryClientError):
      """Missing catalog base URL when required."""


  def resolve_catalog_url() -> str:
      """Read TG_CATALOG_API_URL, then [catalog] url, else raise."""
      raise NotImplementedError


  __all__ = [
      "RegistryClientError",
      "CatalogConfigError",
      "resolve_catalog_url",
  ]
  ```

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:all
```

**STOP:** If TOML parser dependency missing, add `tomli` (py<3.11) or stdlib `tomllib` (py3.11+) in **Decision Log** and one line in `tools/sprite-gen/requirements.txt` only after the choice is made.

**MCP hints:** `backlog_issue` (TECH-675), `glossary_lookup` (catalog) optional.

---
## §Plan Digest

### §Goal

Connect promote output to the live **catalog** HTTP API: create or reconcile rows without duplicate inserts when the slug already exists with same image path.

### §Acceptance

- [ ] `create_asset` called on happy path; `get_asset_by_slug` + compare on 409; `patch_asset` on drift; single retry.
- [ ] Mocks prove behavior; no real HTTP in default pytest run.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_post_happy | mock 201 | row dict | pytest |
| test_409_identical | mock 409 + get returns same | no PATCH | pytest |
| test_409_drift | mock 409 + different row | PATCH once | pytest |

### §Examples

| HTTP | Action | Notes |
|------|--------|-------|
| 201 | return body |  |
| 409 | read slug, branch | compare `world_sprite_path` + `generator_archetype_id` per master plan |

### §Mechanical Steps

#### Step 1 — Payload builder + `RegistryClient` create path

**Goal:** Add typed payload builder and wire `create_asset` in client module; keep curation call site behind a function importable from future `curate` module.

**Edits:**

- `tools/sprite-gen/src/registry_client.py` — extend after **TECH-674** base: add `RegistryClient.create_asset` body + `_build_catalog_payload` (or module-level) per master plan field list.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:all
```

**STOP:** If **TECH-674** is not yet merged, complete **TECH-674** first so class shape is stable; no duplicate `RegistryClient` definitions.

**MCP hints:** `backlog_issue` (TECH-676, TECH-674).

#### Step 2 — 409 compare + `patch_asset`

**Goal:** On `ConflictError` from create, run `get_asset_by_slug`, compare, branch.

**Edits:** Same module: implement private `_sync_after_conflict` (name flexible) and unit tests in `tools/sprite-gen/tests/test_registry_client.py` using `responses` or `unittest.mock` as chosen in **§7b**.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && pytest tools/sprite-gen/tests/test_registry_client.py -q
```

**STOP:** If pytest is not in CI, still run locally; **TECH-678** will broaden coverage.

**MCP hints:** `backlog_issue` (TECH-678) for test expansion handoff.

---
## §Plan Digest

### §Goal

Expose an explicit user switch to skip the catalog **HTTP** step while still copying PNG and `.meta` (offline pipeline).

### §Acceptance

- [ ] `--no-push` on `promote` when the subcommand is active; `push=False` to `curate.promote` when set.
- [ ] `README` lists the flag and env var.
- [ ] `npm run validate:all` green.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_promote_no_push | `--no-push` + mock curate | no `RegistryClient` call | pytest once CLI exists |

### §Examples

| argv tail | `push` to curate | Notes |
|------------|------------------|------|
| `promote x --as y` | `True` | default |
| `promote x --as y --no-push` | `False` |  |

### §Mechanical Steps

#### Step 1 — Document flag in `README` (docs-first safe)

**Goal:** Add bullet or table row before parser wiring if subcommand is still reserved.

**Edits:**

- `tools/sprite-gen/README.md` — **before**:
  ```
  ## Documentation

  - Orchestrator master plan: `ia/projects/sprite-gen-master-plan.md`
  - Exploration / design rationale: `docs/isometric-sprite-generator-exploration.md`
  ```
  **after**:
  ```
  ## Documentation

  - When catalog push is enabled, set `TG_CATALOG_API_URL` or `[catalog] url` in `tools/sprite-gen/config.toml`. Use `promote --no-push` to skip HTTP (Stage 5).
  - Orchestrator master plan: `ia/projects/sprite-gen-master-plan.md`
  - Exploration / design rationale: `docs/isometric-sprite-generator-exploration.md`
  ```

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:all
```

**STOP:** Re-read this section if `README` line wraps — keep single-line paths only where markdown allows.

**MCP hints:** `backlog_issue` (TECH-177).

#### Step 2 — Add `--no-push` to `promote` argparse path

**Goal:** When `cli.py` promote handler is active, add flag + pass `push=not args.no_push` into curation.

**Edits:** `tools/sprite-gen/src/cli.py` — replace reserved stub with full parser and handler only after **TECH-180**; until then, add the flag to the `add_parser("promote", ...)` chain using `set_defaults` or subparser that accepts `--no-push` in advance, following argparse patterns already used in `render` in the same file.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && python -m compileall -q tools/sprite-gen/src
```

**STOP:** If `python -m compileall` is not in PATH, use `python3` and record in local shell profile — not a repo change.

**MCP hints:** `backlog_issue` (TECH-180) if ordering conflict; resolve merge with **TECH-180** owner.

---
## §Plan Digest

### §Goal

Lock HTTP edge-case behavior in tests so **TECH-676** and **TECH-679** can rely on stable error mapping while iterating on curation.

### §Acceptance

- [ ] Each HTTP outcome has an explicit test name.
- [ ] `push=False` path performs zero HTTP in that scenario (mock counts zero).
- [ ] `npm run validate:all` green.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_create_200 | mock 201/200 | dict | pytest+responses |
| test_409_skip | 409+GET same | no PATCH | pytest |
| test_409_patch | 409+GET differ | one PATCH | pytest |
| test_422 | 422 body | `ValidationError` | pytest |
| test_conn | refused | `ConnectionError` | pytest |
| test_no_http_when_push_off | high-level helper | 0 calls | pytest |

### §Examples

| status | assert | Module API |
|--------|--------|------------|
| 422 | `err.errors` from body |  |

### §Mechanical Steps

#### Step 1 — Replace placeholder with real `responses` wiring

**Goal:** Real tests against mocked base URL, no network.

**Edits:**

- `tools/sprite-gen/tests/test_registry_client.py` — **before**:
  ```
  """Tests for registry_client (TECH-678)."""


  def test_placeholder() -> None:
      """Flesh out in TECH-678."""
      assert True
  ```
  **after**:
  ```
  """Tests for registry_client (TECH-678)."""
  # implementer: import responses, build RegistryClient against mock base URL, assert each branch
  def test_synthetic_import() -> None:
      import importlib
      m = importlib.import_module("registry_client", package="src")
      assert m is not None
  ```
  (Tighten with real `RegistryClient` once **TECH-674** is implemented.)

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && pytest tools/sprite-gen/tests/test_registry_client.py -q
```

**STOP:** If `registry_client` import path differs, adjust `sys.path` in conftest (new file) only when needed — keep conftest in `tools/sprite-gen/tests/`.

**MCP hints:** `backlog_issue` (TECH-678, TECH-674).

---
## §Plan Digest

### §Goal

Prove full **sprite-gen** curation + **catalog** pipeline under pytest with no external services, and document operator exit codes in English in `docs/sprite-gen-usage.md`.

### §Acceptance

- [ ] `test_promote_push` exercises render → promote → assert POST once.
- [ ] Second scenario `--no-push` has zero calls.
- [ ] Usage doc: exit 5, `TG_CATALOG_API_URL`, `config.toml` reference.
- [ ] `npm run validate:all` green.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_smoke_post | mocked HTTP | 1 POST | pytest |
| test_smoke_no_push | --no-push | 0 calls | pytest |

### §Examples

| step | art |
|------|-----|
| render | `out/*.png` |
| promote | POST JSON |

### §Mechanical Steps

#### Step 1 — Expand usage doc (exit 5 + env)

**Goal:** English operator-facing text for `docs/sprite-gen-usage.md` with exit 5 and env/config preface (full sentences allowed in **web/content**-style user docs — this path is `docs/`, so normal technical English is fine for clarity).

**Edits:**

- `docs/sprite-gen-usage.md` — **before**:
  ```
  # Sprite-gen usage (Territory)

  Working notes for the **sprite-gen** CLI. **TECH-679** will document registry exit code **5** (HTTP or transport failure) once the promote + catalog path lands.

  - Render: `python -m sprite_gen render <archetype>`
  - Curation: see `ia/projects/sprite-gen-master-plan.md` **Stage 5**
  ```
  **after**:
  ```
  # Sprite-gen usage (Territory)

  - Render: `python -m sprite_gen render <archetype>` (or `render --all`).
  - Curation: see `ia/projects/sprite-gen-master-plan.md` Stage 5.
  - Registry push: set `TG_CATALOG_API_URL` or `tools/sprite-gen/config.toml` under `[catalog]` (see TECH-675). On transport or non-recoverable HTTP, CLI exits 5.
  - Exit codes: 0 success; 1 data/spec; 2 argparse; 4 Aseprite missing; 5 registry transport or unrecoverable HTTP; others reserved per CLI header.

  (Keep bullets tight; add subsections as needed during implement.)
  ```

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:all
```

**STOP:** If **validate:all** flags spellcheck on this doc, fix wording without changing code semantics.

**MCP hints:** `backlog_issue` (TECH-679), `glossary_lookup` (catalog) optional.

#### Step 2 — Implement E2E smoke in test file

**Goal:** Full integration test; replace placeholder in `test_promote_push.py` with real steps once **TECH-676** and **TECH-180** are green enough to import.

**Edits:** `tools/sprite-gen/tests/test_promote_push.py` — same pattern as **TECH-678** before/after with synthetic import first, then E2E when dependencies land.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && pytest tools/sprite-gen/tests/test_promote_push.py -q
```

**STOP:** If render step is too slow, seed minimal fixture PNG in `tools/sprite-gen/tests/fixtures/` and point promote at that path — new fixture file must be added in same PR with binary size under reasonable cap.

**MCP hints:** `backlog_issue` (TECH-679, TECH-180, TECH-676).


## Final gate

```bash
npm run validate:all
```
