### Stage 2 — Catalog + data model + glossary/spec seed / Catalog YAML + validator rule

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Author the 6-row catalog file + extend `validate:all` with landmark-catalog lint. Ensures authoring errors (duplicate id, dangling `utilityContributorRef`) fail CI before runtime loads.

**Exit:**

- `Assets/StreamingAssets/landmark-catalog.yaml` — 6 rows: 2 tier-defining (city→region: `regional_plocks`, region→country: `country_capital`) + 4 intra-tier (`big_power_plant` super-utility w/ `contributorScalingFactor: 10`, `state_university` non-utility, `grand_hospital` non-utility, `major_airport` non-utility). All commission-cost fields comment-flagged `// cost-catalog bucket 11 placeholder`.
- `tools/scripts/validate-landmark-catalog.ts` (OR equivalent Node script) — parses YAML, asserts id uniqueness, asserts `utilityContributorRef` non-null rows resolve against a placeholder allowlist (sibling utilities catalog not yet shipped — use a hard-coded allowlist + TODO-link to utilities Stage 2.1 archetype asset names), asserts `popGate.kind ∈ { scale_transition, intra_tier }`, asserts `tierCount` maps to a valid `LandmarkTier`.
- Validator wired into `package.json` `validate:all` chain + CI script.
- EditMode smoke — load YAML via upcoming Store (stubbed) + assert 6 rows parsed (reference check moved to Stage 1.3 once Store lands).
- Phase 1 — Author 6-row YAML.
- Phase 2 — Validator script + CI wiring.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Author 6 catalog rows | _pending_ | _pending_ | Create `Assets/StreamingAssets/landmark-catalog.yaml` with 6 rows per Exploration Examples block. Tier-defining rows: `commissionCost: 0`, `buildMonths: 0`, `utilityContributorRef: null`. Intra-tier super-utility (`big_power_plant`): `utilityContributorRef: contributors/coal_plant`, `contributorScalingFactor: 10`. Comment every `commissionCost` line w/ cost-catalog bucket 11 marker. |
| T2.2 | StreamingAssets dir conventions | _pending_ | _pending_ | Add `Assets/StreamingAssets/.meta` + README stub under `Assets/StreamingAssets/README.md` (new dir — create if missing) documenting loading convention (Unity `Application.streamingAssetsPath` + YAML parser). Update `.gitignore` if needed. |
| T2.3 | Landmark-catalog validator script | _pending_ | _pending_ | Add `tools/scripts/validate-landmark-catalog.ts` — parse YAML, assert id uniqueness, assert `utilityContributorRef` resolves against placeholder allowlist (TODO-link sibling utilities archetype assets), assert `popGate.kind` enum, assert cost/buildMonths ≥ 0. Exit code nonzero on violations. |
| T2.4 | Wire validator into validate:all | _pending_ | _pending_ | Edit `package.json` — add `validate:landmark-catalog` script, chain into `validate:all`. Update CI matrix (if separate from validate:all root). Document in `docs/agent-led-verification-policy.md` validator list. |
