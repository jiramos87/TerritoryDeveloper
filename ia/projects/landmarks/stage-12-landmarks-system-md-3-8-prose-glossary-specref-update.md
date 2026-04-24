### Stage 12 — Super-utility bridge + UI surface + spec closeout / landmarks-system.md §3–§8 prose + glossary specRef update

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Fill spec sections 3–8 w/ full prose (state machine, commission pipeline, placement + reconciliation, landmarks↔utilities bridge, BUG-20 interaction, save schema). Update glossary rows added in Stage 1.3 to point at specific §. Cross-link sibling utilities doc. Same end-of-plan spec-closeout pattern as utilities Stage 4.2.

**Exit:**

- `ia/specs/landmarks-system.md` §3 Progression state machine — unlock gate discriminator, idempotency rule, tick ordering, example flow for both tracks.
- §4 Commission pipeline — `TryCommission` contract, bond open, monthly tick, pause/resume, tier-defining bypass, `CommissionResult` matrix.
- §5 Placement + reconciliation — `LandmarkPlacementService.Place` + `RestoreCellTag`, atomic-save pairing, sidecar-wins rule, dangling-tag clear, diagnostic channel.
- §6 Landmarks↔Utilities bridge — `Register` / `Unregister` call contract, nullable `utilityContributorRef` semantics, sibling Bucket 4-a ownership. Marked authoritative; sibling utilities doc consumes.
- §7 BUG-20 interaction — orthogonal note; landmark placement is tile-sprite only, invariant #1 compliant, does not fix or reopen BUG-20 (which concerns visual-restore of zone buildings).
- §8 Save schema — sidecar JSON schema, v3 envelope cell-tag extract, Bucket 3 coordination note.
- Glossary `specReference` fields updated to precise §N anchors (e.g. **Commission ledger** → `landmarks-system §4`).
- Sibling `docs/landmarks-exploration.md` closing link → `ia/specs/landmarks-system.md` noted as canonical landing doc.
- Sibling `ia/projects/utilities-master-plan.md` Stage 4.2 sibling-contract section cross-linked (coordinate at stage-file time — may require edit to utilities doc).
- `npm run validate:all` green — spec index regen + glossary graph index regen.
- Phase 1 — §3 + §4 prose authoring.
- Phase 2 — §5 + §6 + §7 prose authoring.
- Phase 3 — §8 save schema + glossary specRef update.
- Phase 4 — Cross-links + MCP regen.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | §3 Progression state machine prose | _pending_ | _pending_ | Edit `ia/specs/landmarks-system.md` §3 — document `LandmarkPopGate` discriminator, `Tick()` evaluation order, idempotency guard, tick-ordering rule (after ScaleTierController). Include scale-transition + intra-tier example flow. |
| T12.2 | §4 Commission pipeline prose | _pending_ | _pending_ | §4 — `TryCommission` contract (unlock check, dedupe, bond open), `OnGameMonth` tick, `LandmarkBuildCompleted` event, pause/resume semantics, tier-defining bypass rule, `CommissionResult` enum matrix. |
| T12.3 | §5 Placement + reconciliation prose | _pending_ | _pending_ | §5 — `LandmarkPlacementService.Place` + `RestoreCellTag`, invariant #1 compliance note, atomic-save pairing (main + sidecar temp+rename), sidecar-wins reconciliation rule, dangling-tag clear, diagnostic log format. |
| T12.4 | §6 Landmarks↔Utilities bridge prose | _pending_ | _pending_ | §6 — `UtilityContributorRegistry.Register(ref, multiplier)` call contract, load-path re-register via `RestoreCellTag`, nullable `utilityContributorRef` semantics. Mark section authoritative; note sibling Bucket 4-a consumes. |
| T12.5 | §7 BUG-20 interaction prose | _pending_ | _pending_ | §7 — short section documenting that landmark placement is orthogonal to BUG-20 (visual-restore of zone buildings). Landmark placement = tile-sprite, invariant #1 safe, cell-tag rebuilt from sidecar on load. Does not fix or reopen BUG-20. |
| T12.6 | §8 Save schema prose | _pending_ | _pending_ | §8 — sidecar JSON schema table (fields + types), v3 envelope `regionCells[].landmarkId` + `cityCells[].landmarkId` extract, Bucket 3 ownership note (no mid-tier bump from this plan). |
| T12.7 | Glossary specReference updates | _pending_ | _pending_ | Edit `ia/specs/glossary.md` — update 8 rows added in Stage 1.3 to precise `specReference` (e.g. **Commission ledger** → `landmarks-system §4`, **Landmark sidecar** → `landmarks-system §5`). |
| T12.8 | Sibling doc cross-links + exploration closing note | _pending_ | _pending_ | Edit `docs/landmarks-exploration.md` — add closing note linking `ia/specs/landmarks-system.md` as canonical landing doc. Edit `ia/projects/utilities-master-plan.md` Stage 4.2 section — cross-link the landmarks-system §6 bridge contract. |
| T12.9 | MCP index regen + validate:all | _pending_ | _pending_ | Run `npm run validate:all` — regenerates glossary + spec indexes. Commit regen artifacts. Green signal required for stage close. |

---
