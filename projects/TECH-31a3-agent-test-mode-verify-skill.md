# TECH-31a3 — **Agent test-mode verify** skill (orchestration + human **QA** handoff)

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31a3**.  
**Backlog:** [TECH-31](../BACKLOG.md).  
**Prerequisite stages:** **31a** (runtime **test mode** + fixtures); **31a2** (**batchmode** shell + **`executeMethod`** + quit helper).

## Summary

Finalize **`.cursor/skills/agent-test-mode-verify/SKILL.md`** as the **normative** agent workflow: **gate** (run vs skip), **Path A** (**31a2** **batch** tools), **Path B** (**IDE agent bridge** **hybrid** with **`.queued-test-scenario-id`**), bounded **iterate-until-green** with **`validate:all`** / **compile** gates, and a **structured handoff** requesting **human** **normal-game** **QA** only at the end. Integrate with **`project-spec-implement`** (already cross-linked) and **`close-dev-loop`** (**compose**, do not fork). **Does not** replace **human** **issue** verification per **`AGENTS.md`**.

## Goals

1. **Complete** **`SKILL.md`**: triggers, prerequisites, **exit-code** / failure-class table (mapping **31a2** codes + bridge failures), full **tool recipe** (**territory-ia** + **Node**/**shell** order), **seed prompt**.
2. **Gating heuristics** from **project spec** **§7b** / **§8**, touched **Files**, domains (**Load pipeline**, **GridManager**, **HUD**, **simulation**).
3. **Scenario path** **v1**: reference **`reference-flat-32x32`** + **`tools/fixtures/scenarios/agent-generated/{run-id}/save.json`** conventions; **v2**: hook **31b** builder output when shipped.
4. **`AGENTS.md`** + **`ARCHITECTURE.md`** short subsections pointing to the skill.
5. **macOS** **E2E** documented once (**Path A** and **Path B**).

## Non-goals

- Implementing **31a2** scripts (**prerequisite**).
- **Scenario builder** logic (**31b**).
- **Human** **closeout** / **archive** without confirmation.

## Current state

- **Stub** skill exists; **`project-spec-implement`** already references it.
- **31a2** delivers **unity-testmode-batch** + **executeMethod** + **quit**; this stage **documents consumption** and **polishes** orchestration prose.

## Proposed design (implementation-owned)

| Deliverable | Notes |
|-------------|--------|
| **`agent-test-mode-verify/SKILL.md`** | Remove “pending” language; link **31a2** + **31a3** |
| **`AGENTS.md`**, **`ARCHITECTURE.md`** | One subsection each |
| **`.gitignore`** | **`tools/fixtures/scenarios/agent-generated/`** if not already |

## Implementation plan

### Phase 1 — Skill completion

- [ ] Replace stub **status**; full **tool recipe**; **Path A** / **Path B**; **`{MAX_ITERATIONS}`** default **2** (align **`close-dev-loop`**).
- [ ] **Exit-code** table: **31a2** + bridge **`timeout`** / **`db_unconfigured`**.

### Phase 2 — Docs + **E2E**

- [ ] **`AGENTS.md`**, **`ARCHITECTURE.md`** bullets.
- [ ] **E2E** steps in **skill** body (expected **stdout**, report path).
- [ ] **`npm run validate:all`** after doc edits.

### Phase 3 — **Scenario** contract until **31b**

- [ ] Document **agent-generated** layout; pointer to **31b** descriptor pipeline.

## 7b. Test contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|--------|
| **Skill** complete | Manual | Read **`SKILL.md`** | No “pending” pointer only |
| **Docs** | Node | `npm run validate:all` | After **`AGENTS.md`** / **`ARCHITECTURE.md`** |
| **E2E** | Manual / dev machine | **Path A** + **Path B** once each | Owner sign-off |

## Acceptance criteria

- [ ] **`SKILL.md`** is **normative** (implements this spec + **31a2** tools).
- [ ] **`AGENTS.md`** + **`ARCHITECTURE.md`** updated.
- [ ] **E2E** recorded; **`npm run validate:all`** green.

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-09 | Consolidated former standalone **backlog** row into **TECH-31** as **31a3** | Single **program** trace; **31b** prereqs explicit |
| 2026-04-09 | **Compose** **`close-dev-loop`** | Avoid duplicate **Moore** **diff** prose |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## Open Questions (resolve before / during implementation)

**N/A** (tooling / workflow). **Game** **scenario** **Open Questions** live in **`.cursor/projects/TECH-31.md`**.
