### Stage 13 — Save/load + landmarks hook + glossary/spec closeout / Canonical spec + glossary closeout + landmarks contract freeze

**Status:** In Progress (BUG-20 filed)

**Objectives:** Author `ia/specs/utility-system.md` per invariant #12 (utilities = permanent domain). Link glossary rows from Step 1 to this spec. Freeze landmarks registry contract documentation. Note BUG-20 orthogonal status.

**Exit:**

- `ia/specs/utility-system.md` new spec — covers pool state machine, EMA thresholds, rollup math, deficit cascade, contributor lifecycle, natural-wealth adjacency, `RegisterWithMultiplier` landmarks hook. Frontmatter per `ia/templates/spec-template.md`.
- Glossary rows (added Step 1.1 / 1.2) updated to reference `ia/specs/utility-system.md` as `specReference`.
- Sibling `docs/landmarks-exploration.md` cross-linked: landmarks-contract section in the new spec is marked authoritative; landmarks doc consumes.
- BUG-20 note in `ia/specs/utility-system.md` §BUG-20 interaction — orthogonal, not resolved by this plan.
- MCP spec index regenerated (`npm run validate:all` includes this).
- Phase 1 — Spec authoring.
- Phase 2 — Glossary link updates + MCP index regen.
- Phase 3 — Landmarks-doc cross-link.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | Author utility-system.md | _pending_ | _pending_ | Create `ia/specs/utility-system.md` using `ia/templates/spec-template.md` — sections: §State machine, §Rollup + cascade, §Contributor lifecycle, §Natural wealth adjacency, §Landmarks hook contract, §BUG-20 interaction, §Save schema. |
| T13.2 | Spec prose + invariants | _pending_ | _pending_ | Fill spec sections — cite invariants #3, #4, #5, #6 at relevant touchpoints; copy state-machine pseudocode from exploration Implementation Points §1; link architecture diagram from exploration §Architecture. |
| T13.3 | Glossary specReference updates | _pending_ | _pending_ | Edit `ia/specs/glossary.md` rows added in Step 1 (Utility pool, Utility contributor, Utility consumer, Pool status, Freeze flag, EMA warning, Deficit cascade) — set `specReference` to `utility-system §{section}`. |
| T13.4 | MCP index regen | _pending_ | _pending_ | Run `npm run validate:all` → regenerates `tools/mcp-ia-server/data/glossary-index.json`, `glossary-graph-index.json`, `spec-index.json` including new spec + updated glossary rows. Commit regen artifacts w/ spec. |
| T13.5 | Landmarks doc cross-link + BUG-20 note | _pending_ | _pending_ | Edit `docs/landmarks-exploration.md` — reference `ia/specs/utility-system.md §Landmarks hook contract` as authoritative. Add BUG-20 orthogonal note to spec's §BUG-20 interaction. |

---
