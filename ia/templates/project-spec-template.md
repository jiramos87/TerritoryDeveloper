---
purpose: "{ISSUE_ID} — {Title}."
audience: both
loaded_by: ondemand
slices_via: none
---
# {ISSUE_ID} — {Title}

> **Issue:** [{ISSUE_ID}](../../BACKLOG.md)
> **Status:** Draft | In Review | In Progress | Final
> **Created:** YYYY-MM-DD
> **Last updated:** YYYY-MM-DD

<!--
  Filename: `ia/projects/{ISSUE_ID}-{description}.md` (e.g. `BUG-37-zone-cleanup.md`,
  `FEAT-44-water-junction.md`). Legacy bare `{ISSUE_ID}.md` accepted for back-compat.
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Glossary: ../specs/glossary.md (spec wins on conflict).
  Separate product behavior (§1–5.1, §8, Open Questions) from impl notes (§5.2+, §7, optional "Implementation investigation").
  Authoring style: caveman prose (drop articles/filler/hedging; fragments OK). Tables, code, seed prompts stay normal.
-->

## 1. Summary

<!-- 2–3 sentences. What + why. Domain vocabulary only. -->

## 2. Goals and Non-Goals

### 2.1 Goals

<!-- Specific, measurable outcomes. -->

1. …

### 2.2 Non-Goals (Out of Scope)

<!-- Explicit exclusions. Prevents scope creep. -->

1. …

## 3. User / Developer Stories

<!-- Format: "As a [role], I want [capability] so that [benefit]." -->

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | … | … |
| 2 | Developer | … | … |

## 4. Current State

### 4.1 Domain behavior

<!-- Observed vs expected. Glossary terms. No code. -->

### 4.2 Systems map

<!-- Backlog Files, subsystems, spec sections. Optional file/class table for implementers. -->

### 4.3 Implementation investigation notes (optional)

<!-- Tech hypotheses for implementing agent — not product requirements. -->

## 5. Proposed Design

### 5.1 Target behavior (product)

<!-- Player-visible rules + definitions; glossary-aligned. -->

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

<!-- Classes, data flow, algorithms. Agent proposes unless user locked design. -->

### 5.3 Method / algorithm notes (optional)

<!-- Signatures, pseudo-code — only if product owner must approve. -->

## 6. Decision Log

<!-- Non-obvious choices. Update as project evolves. -->

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| YYYY-MM-DD | … | … | … |

## 7. Implementation Plan

<!-- Ordered phases; concrete deliverables; independently testable. -->

### Phase 1 — {Name}

- [ ] …

### Phase 2 — {Name}

- [ ] …

<!--
  ## 7b. Test Contracts — ../projects/PROJECT-SPEC-STRUCTURE.md (list item "7b. Test Contracts").
  Tooling / verification table. Glossary terms for *what* is checked. Not a substitute for ## Open Questions.
  **FEAT-** / **BUG-** specs w/ runtime **C#**, **Play Mode**, **Load pipeline** claims: 1+ row per **§8** bullet (or **TBD** + owner).
  Doc-only **TECH-**/**ART-**/**AUDIO-**: single **N/A** row OK when **§8** has no testable claims.
-->

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Example: change touches MCP, schemas, glossary, or **reference spec** bodies feeding **IA indexes** | Node | `npm run validate:all` (repo root) | Chains **validate:dead-project-specs**, **test:ia**, **validate:fixtures**, **generate:ia-indexes --check** |
| Example: agent **Verification** block (substantive **C#** / **Load pipeline** / **test mode** work) | Agent report | **`validate:all`** + **`unity:compile-check`** (if **Assets/** **C#**) + **`npm run unity:testmode-batch`** + **`unity_bridge_command`** (**`timeout_ms`:** **40000** initial; escalation protocol on timeout) | [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md); **`ia/skills/agent-test-mode-verify/SKILL.md`** |
| Example: Play / HUD acceptance — console clean + screenshot w/ **Overlay** UI | MCP / dev machine | **territory-ia** **`unity_bridge_command`**: **`get_console_logs`** (`severity_filter`); **`capture_screenshot`** (`include_ui: true`) | **N/A** in CI; **Postgres** **0008** + **Unity** on **REPO_ROOT**; see **`ia/skills/ide-bridge-evidence/SKILL.md`** |
| … | … | … | … |

## 8. Acceptance Criteria

<!-- Conditions for project complete. Map back to §2.1 Goals + §3 Stories. -->

- [ ] …

## 9. Issues Found During Development

<!-- Unanticipated problems. Root cause + resolution (or link to new backlog items). -->

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

<!-- Carry-forward insights. On completion: migrate to AGENTS.md, coding-conventions, canonical specs. -->

- …

## Open Questions (resolve before / during implementation)

<!--
  REQUIRED for collaborative specs.
  Rules: canonical glossary terms only.
  Ask GAME LOGIC + definitions — NOT code, APIs, class names.
  Implementing agent resolves tech approach unless it changes intended behavior (then Decision Log or ask user).
  TOOLING-ONLY (CI, MCP, scripts, docs with no gameplay change): write "None — tooling only; see §8 Acceptance criteria" (or dev policy questions like CI blocking vs advisory). Do NOT invent fake game rules to fill.
-->

1. …
