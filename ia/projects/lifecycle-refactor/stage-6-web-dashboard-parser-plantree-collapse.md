### Stage 6 — Infrastructure + Execution Surface / Web Dashboard: Parser + PlanTree Collapse

**Status:** Final

**Objectives:** Update web dashboard parser and PlanTree rendering to expect Stage→Task 2-level hierarchy. Remove Phase level from tree model and UI. Validate web build passes.

**Exit:**

- `web/lib/plan-loader-types.ts` — `PlanNode` / `StageNode` / `TaskNode` types reflect 2-level (no `PhaseNode`).
- `web/lib/plan-parser.ts` — parser reads Stage→Task; Phase grouping logic removed.
- `web/lib/plan-tree.ts` — `PlanTree` renders Stage/Task rows; Phase headers absent.
- `npm run validate:web` passes (lint + typecheck + build).
- Migration JSON M5 flipped to `done`.
- Phase 1 — Types + parser update.
- Phase 2 — PlanTree rendering update + validate.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Update plan-loader-types + parser | **TECH-462** | Done (archived) | Edit `web/lib/plan-loader-types.ts`: remove `PhaseNode` type (or `phase` field in `StageNode`); update `StageNode` to hold `tasks: TaskNode[]` directly; edit `web/lib/plan-parser.ts`: remove Phase-level parsing block (the parser currently detects `#### Phase N` or `- [ ] Phase N` rows and groups tasks under phases); re-group tasks directly under their parent Stage; verify `web/lib/releases.ts` + `web/lib/plan-loader.ts` are updated if they reference Phase fields. |
| T6.2 | Update plan-tree + plan-loader | **TECH-463** | Done (archived) | Edit `web/lib/plan-tree.ts`: remove Phase header rendering row; render tasks directly under Stage; verify task Status column still renders `_pending_ / Draft / In Review / In Progress / Done (archived)` correctly after restructure; check `web/app/dashboard/**/page.tsx` usage of `PlanTree` for any Phase-specific props that need removal. |
| T6.3 | Web validate + type-check | **TECH-464** | Done (archived) | Run `cd web && npm run validate:web` (= lint + typecheck + build); fix any TypeScript errors from Phase-type removal; re-run until green. |
| T6.4 | Preview deploy + M5 flip | **TECH-465** | Done (archived) | Run `npm run deploy:web:preview`; open `/dashboard` on preview URL; visually confirm Stage→Task tree renders without Phase rows; confirm all migrated master plans (Step 2 output) display correctly; flip migration JSON M5 `done`. |

---
