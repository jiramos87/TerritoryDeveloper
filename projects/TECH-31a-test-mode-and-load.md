# TECH-31a — Test mode, load path, and contracts

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31a**.  
**Backlog:** [TECH-31](../BACKLOG.md).  
**Prerequisite stages:** none.

## Summary

Introduce a gated **test mode** entry path, load a **committed** **`GameSaveData`-compatible** scenario by **scenario id** or filesystem path through the normal **save/load** pipeline, target **32×32** **test map** policy, and document the **UTF** / **batchmode** / **IDE agent bridge** driver matrix (including which paths require **Postgres**—none for this stage).

## Goals

- **Test mode** flag and boot flow (scene, CLI, or bridge hook) with minimal **TEST-MODE** on-screen indicator.
- **Security:** non-dev / release builds cannot enable **test mode** (Editor, **development build**, and/or **scripting define**).
- **32×32** policy documented: dedicated scene vs init flag; **GridManager** / camera assumptions called out.
- Single load path via **`GameSaveManager`** (or one documented wrapper)—no duplicate deserializer.
- Folder layout: `tools/fixtures/scenarios/` (or agreed **`Assets/`** path); **scenario id** convention (**kebab-case**, ASCII, unique).
- Document **`GameSaveData`** version compatibility for checked-in artifacts; regenerate on schema bump.
- README: glossary-aligned terms, local + **CI** instructions, driver matrix.

## Non-goals (this stage)

- Descriptor **builder** (31b). **City metrics** / **TECH-82** (31d). **MCP** tool (31e).

## Open Questions (resolve before / during implementation)

**N/A** for this stage (tooling and **Load pipeline** integration only). Product-level **scenario** preconditions and terrain rules stay in [`ia/projects/TECH-31.md`](../ia/projects/TECH-31.md).

## Risks (mitigations)

| Risk | Mitigation |
|------|------------|
| **Load path fork** | Only **`GameSaveManager`** (or documented wrapper). |
| **Grid size** | Smoke load on **32×32**; watch hard-coded dimensions. |
| **Test mode in player builds** | Compile-time / build-type gates. |

## Implementation checklist

- [x] **Test mode** flag + boot + **TEST-MODE** UI; security gate.
- [x] **32×32** map policy + documented constraints.
- [x] Load scenario by id/path via **`GameSaveManager`**.
- [x] Artifact folder layout + naming; **scenario id** rules.
- [x] README + driver matrix (**UTF** / **batchmode** / bridge) + **Postgres** callouts for later stages.
- [x] **Implementing agent verification gate** (below): **`npm run validate:all`** then **`npm run unity:compile-check`**; both exit **0** before asking a human to review or confirm.

## Implementing agent verification gate (mandatory)

After substantive implementation work on this stage, the **implementing agent** must complete automated checks **before** requesting human intervention for review or sign-off. This keeps handoff to “green checks + any documented exceptions” instead of mid-flight questions.

1. **Repo root:** run **`npm run validate:all`** (Node / **IA** tooling aligned with **CI** — dead project-spec paths, **territory-compute-lib** build, **`test:ia`**, **`validate:fixtures`**, **`generate:ia-indexes --check`**, etc.). **Required** whenever this stage touches tooling, fixtures, schemas consumed by **IA** indexes, **`tools/mcp-ia-server`**, or related docs that feed validation. Follow [`ia/skills/project-implementation-validation/SKILL.md`](../ia/skills/project-implementation-validation/SKILL.md).
2. **Unity batch compile:** run **`npm run unity:compile-check`** from the repository root **whenever** Unity **C#**, scenes, or serialized assets change (**AGENTS.md** pre-commit checklist).
3. **Order:** **`validate:all`** first, then **`unity:compile-check`**. If **`validate:all`** fails, fix or document the failure before Unity compile; do not skip **`validate:all`** because Unity passed.
4. **Optional dev machine superset:** **`npm run verify:local`** (includes **`validate:all`** plus **Postgres** / **Editor** steps per **`ARCHITECTURE.md`**) does **not** replace the explicit **`validate:all`** requirement here — agents must still run **`validate:all`** as a named gate so **CI**-parity checks are never skipped by mistake.
5. **Play Mode** / **IDE agent bridge** evidence (e.g. **`get_console_logs`**, **`capture_screenshot`**) is **not** mandatory for **31a** acceptance unless you extend **Test contracts** for a specific bridge-backed check; use [`ia/skills/ide-bridge-evidence/SKILL.md`](../ia/skills/ide-bridge-evidence/SKILL.md) when you add such a row.

Document in the stage **README** that implementers follow this gate so runs are reproducible locally and in **CI**.

## Test contracts (stage)

| Goal | Check type | Command or check | Notes |
|------|------------|------------------|--------|
| **IA** / Node parity after implementation | **CI**-aligned (agent **required**) | `npm run validate:all` | Repo root; see **Implementing agent verification gate** |
| Reference save loads in **test mode** | Manual, **UTF**, or scripted | Document args: **scenario id** / path | **Load pipeline** order is authoritative (**persistence-system**); no duplicate deserializer |
| Unity compiles after changes | Batch compile (agent **required** when Unity touched) | `npm run unity:compile-check` | Repo root; **`AGENTS.md`** |

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-09 | Implementing agents must run **`npm run validate:all`** then **`npm run unity:compile-check`** before human review | **CI**-parity **IA** checks plus Unity compile; **`verify:local`** does not replace the explicit **`validate:all`** gate |
| 2026-04-09 | **Test mode** CLI: **`-testScenarioId`**, **`-testScenarioPath`**; gate **Editor** / **DEVELOPMENT_BUILD** / **`TERRITORY_ALLOW_TEST_MODE`** | Release builds cannot enable **test mode**; **`GameStartInfo`** + **`GameBootstrap`** keep a single **`LoadGame`** path |
| 2026-04-09 | **Editor** queue file **`tools/fixtures/scenarios/.queued-test-scenario-id`** (gitignored) | Agents + **`unity_bridge_command`** **`enter_play_mode`** without restarting Unity with CLI args |
| 2026-04-09 | **Follow-on:** **31a2** (**`npm run unity:testmode-batch`**, **executeMethod**, shell quit) then **31a3** (**agent-test-mode-verify** skill) | Keeps **31a** focused on runtime + contracts; **31b** prerequisites documented in program tracker |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |
