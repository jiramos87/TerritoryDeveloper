# TECH-27 — BACKLOG glossary alignment and glossary↔spec link checker

> **Issue:** [TECH-27](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **10**.

## 1. Summary

Audit **open** `BACKLOG.md` issues so **Depends on**, **Spec**, **Files**, and **Notes** use vocabulary from [`.cursor/specs/glossary.md`](../../.cursor/specs/glossary.md) and linked **reference specs** where practical. Optionally add an automated **link checker**: for each glossary table row, the **Spec** column path must exist in the repo; optional **anchor** validation if headings are stable enough.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Measurable pass: e.g. 90%+ of open issues cite at least one glossary-canonical term in **Notes** where applicable (agent/human judgment for edge cases).
2. Optional script: `npm run check:glossary-specs` (name agent-owned) exits 0 only if all Spec cell paths resolve.
3. Update **TECH-27** backlog item when passes complete.

### 2.2 Non-Goals (Out of Scope)

1. Rewriting **completed** issues except broken links.
2. Enforcing glossary terms inside **Unity** C# identifiers.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | I want **`backlog_issue`** text to match spec vocabulary. | Open issues use **road stroke**, **wet run**, etc. consistently. |
| 2 | Maintainer | I want glossary Spec links to never 404. | Script validates paths. |

## 4. Current State

### 4.1 Domain behavior

N/A — documentation hygiene.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Files | `BACKLOG.md`, `glossary.md` |
| Prior work | **TECH-22** (completed terminology pass on reference specs) |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Manual audit pass with checklist in **Implementation Plan**.
- Parser: read glossary markdown table Spec column; `fs.existsSync` for paths relative to repo root.
- Anchor check: optional regex against target `.md` for `#anchor` — fragile; document limitations.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Optional anchor check | **TECH-22** headings may shift | Paths-only first |

## 7. Implementation Plan

### Phase 1 — Manual backlog pass

- [ ] Triage High → Low priority sections; edit **Notes** for canonical terms.

### Phase 2 — Link checker script

- [ ] Implement path existence checker for glossary Spec column.
- [ ] Optional: wire into `npm run verify` at repo root or `tools/`.

## 8. Acceptance Criteria

- [ ] **TECH-27** audit documented (short summary in **Decision Log** or **Lessons**).
- [ ] If script added: runs in CI or documented manual step; fails on missing path.
- [ ] **glossary.md** Spec column has zero broken file paths after fix pass.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only.
