### Stage 5 — MEDIUM / LOW band (IP6–IP9) / Validator extensions (IP8)

**Status:** In Progress — 2026-04-24 (5 tasks filed)

**Objectives:** Extend `tools/validate-backlog-yaml.mjs` with cross-record checks: `related` ids must exist; `depends_on_raw` non-empty when `depends_on: []` non-empty; warn on drift when `depends_on_raw` mentions ids not in `depends_on: []`. All new checks land fixtures under `tools/scripts/test-fixtures/`.

**Exit:**

- `validate-backlog-yaml.mjs` implements the three new checks via the shared lint core (`backlog-record-schema.ts` from Stage 1.2) where applicable — cross-record checks (which need the whole set) stay in the script.
- Fixture set under `tools/scripts/test-fixtures/` — for each check, one passing fixture + one failing fixture + expected error text.
- `npm run validate:backlog-yaml` + `npm run validate:all` green on passing fixtures, red on failing fixtures (via a fixture-runner test harness).
- Phase 1 — `related` id existence check + fixtures.
- Phase 2 — `depends_on_raw` non-empty + drift warning + fixtures.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Cross-check `related` ids exist | **TECH-858** | Draft | In `tools/validate-backlog-yaml.mjs`, after loading both dirs, iterate records + assert every id in `related: []` exists in the combined set (open + archive). Emit error with source record + missing id. |
| T5.2 | Fixtures for `related` existence check | **TECH-860** | Draft | Add to `tools/scripts/test-fixtures/` — `related-exists-pass/` (two records, one refers to the other), `related-exists-fail/` (record refers to nonexistent id). Extend fixture harness to assert pass/fail outcomes + expected error text. |
| T5.3 | Enforce `depends_on_raw` non-empty | **TECH-861** | Draft | In `validate-backlog-yaml.mjs`, reject records where `depends_on: []` is non-empty AND `depends_on_raw` is empty / missing. Error names the record id + field. |
| T5.4 | Warn on `depends_on_raw` drift | **TECH-862** | Draft | Warning (not error) when `depends_on_raw` mentions an id not present in `depends_on: []`. Tokenize raw by `,` + strip soft markers before compare. Emit warning w/ record id + drift token. |
| T5.5 | Fixtures for `depends_on_raw` checks | **TECH-863** | Draft | Add fixtures — `depends-raw-pass/`, `depends-raw-empty-fail/`, `depends-raw-drift-warn/`. Fixture harness asserts error / warning outcomes + expected text. |
