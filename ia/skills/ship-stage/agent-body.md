# Mission

Pass A per-task loop dispatches `tools/recipes/ship-stage-pass-a.yaml`. Setup (Phases 0–4) and Pass B (Phases 6–10) run inline per `ia/skills/ship-stage/SKILL.md`.

# Setup — Phases 0–4

Follow `ia/skills/ship-stage/SKILL.md` §Phases 0–4: parse → `stage_bundle` (idle exit when done) → `BASELINE_DIRTY` snapshot → `domain-context-load` once → §Plan Digest gate → resume gate.

# Pass A — Recipe (Phase 5)

Recipe: `tools/recipes/ship-stage-pass-a.yaml`. CLI: `npm run recipe:run -- ship-stage-pass-a --inputs <inputs.json>`. Inputs: `{slug, stage_id}`. Carcass when `section_id` set: `stage_claim` pre-loop; `claim_heartbeat` per task + post-loop.

# Pass B — Recipe (Phase 6–10)

Recipe: `tools/recipes/ship-stage-pass-b.yaml`. CLI: `npm run recipe:run -- ship-stage-pass-b --inputs <inputs.json>`. Inputs: `{slug, stage_id, section_id?}`. Carcass when `section_id` set: `arch_drift_scan` pre-closeout; `stage_claim_release` post-flip.

# Hard boundaries

- IF recipe engine unavailable → fall back to `ia/skills/ship-stage/SKILL.md` inline flow.
- Pass A NEVER commits — single stage-end commit (Phase 8) covers everything.
- `PASSED` invalid before Phase 7 closeout + Phase 8 commit + `stage_verification_flip`.
- No code-review in chain — `/code-review {ISSUE_ID}` available out-of-band.
- DB sole source of truth — closeout DB-only, no filesystem mv.
