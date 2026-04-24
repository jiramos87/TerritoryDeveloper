### Stage 15 — Late-hardening + archive backfill (deferred) / Flip validator default to blocking

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Flip `validate-parent-plan-locator.mjs` default mode from advisory → strict; keep `--advisory` as opt-out. Update `validate:all` chain so CI fails on drift. Document the flip in `docs/agent-led-verification-policy.md`. Gate on zero-drift-for-≥1-week-in-production (tracked in this plan's acceptance).

**Exit:**

- Validator CLI default exit code = 1 on any error (was 0 advisory-default in Step 3).
- `--advisory` flag retained + documented; flips exit code back to 0.
- `package.json` `validate:all` script chains the validator in strict mode.
- `docs/agent-led-verification-policy.md` entry documents the flip + opt-out.
- Fixture test covers: strict-default-fail-on-drift, advisory-opt-out-still-green.
- Phase 1 — CLI default flip + `validate:all` wire-in.
- Phase 2 — Docs + fixture updates.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T15.1 | Flip validator CLI default | _pending_ | _pending_ | Edit `tools/validate-parent-plan-locator.mjs` — default mode = strict (exit 1 on any error); `--advisory` flag retained (exit 0 + drift count). MCP tool `parent_plan_validate` default input flipped to `strict: true` as well; `strict: false` retained as opt-out. |
| T15.2 | Chain strict validator into `validate:all` | _pending_ | _pending_ | Edit root `package.json` — `validate:all` script includes `npm run validate:parent-plan-locator` (strict by default after T6.1.1). Document chain entry in `ARCHITECTURE.md` Local verification table if the script is listed there. |
| T15.3 | Document blocking-flip in verification policy | _pending_ | _pending_ | Edit `docs/agent-led-verification-policy.md` — add entry documenting the advisory → strict flip: gate criteria (≥1 week zero drift in production), `--advisory` opt-out contract, fallback on CI red (run `backfill-parent-plan-locator.sh` + re-run). |
| T15.4 | Fixture tests for strict default | _pending_ | _pending_ | Update `tools/mcp-ia-server/tests/tools/parent-plan-validate.test.ts` fixtures — assert strict-default-fail on drift fixtures (previously advisory-green); assert `--advisory` opt-out still exits 0. Cover MCP tool `strict: false` input path too. |
