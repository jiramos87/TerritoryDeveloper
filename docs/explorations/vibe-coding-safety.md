---
slug: vibe-coding-safety
parent_plan_id: null
target_version: 1
notes: |
  7-proposal vibe-coding safety bundle. Ship order A→B→C→D→E. Proposal #6 dropped
  (parallel-carcass overlap — see docs/parallel-carcass-exploration.md shipped 2026-04-29).
  Critic pipeline (#3) fires ONLY at /ship-final Pass B — never per-Task or per-Stage.
  Feature flags DB-primary (ia_feature_flags table) with interchange JSON boot hydration
  and bridge OnFlagFlipped signal. Plan-scoped arch decisions: plan-vibe-coding-safety-boundaries,
  plan-vibe-coding-safety-end-state-contract, plan-vibe-coding-safety-shared-seams.
  Stage 1.0 = hook-layer tracer (carcass). Stages 2.0-6.0 = section waves per Design Expansion.
stages:
  - id: '1.0'
    title: 'Hook-layer tracer — stop hook + test-write denylist live'
    exit: |
      Stop hook exits 2 with reason on stderr when session touched Assets/**/*.cs (or tools/mcp-ia-server/**, Domains/**)
      AND emitting response lacks Verification block. Test-write denylist exits 2 on Write/MultiEdit to
      tests/** or tools/fixtures/scenarios/** unless TD_ALLOW_TEST_EDIT={ISSUE_ID} env var set. Smoke fixtures
      under tests/hooks/ green.
    red_stage_proof: |
      # tests/hooks/stage1-stop-verification.test.mjs — red on 1.0.1..1.0.3 stub, green on 1.0.5 fixture
      it('exits 2 when session touched Assets/** without Verification block', async () => {
        const ctx = { touched: ['Assets/Scripts/Foo.cs'], response: 'ok done' };
        const { code, stderr } = await runHook('stop-verification-required.sh', ctx);
        assert.equal(code, 2);
        assert.match(stderr, /Verification block required/);
      });
      it('exits 0 on docs-only session', async () => {
        const ctx = { touched: ['docs/foo.md'], response: 'ok' };
        const { code } = await runHook('stop-verification-required.sh', ctx);
        assert.equal(code, 0);
      });
    red_stage_proof_block:
      red_test_anchor: 'tracer-verb-test:tests/hooks/stage1-stop-verification.test.mjs::StopHookBlocksMissingVerification'
      target_kind: tracer_verb
      proof_artifact_id: 'tests/hooks/stage1-stop-verification.test.mjs'
      proof_status: failed_as_expected
    tasks:
      - id: '1.0.1'
        title: 'Write stop-verification-required.sh hook script'
        prefix: TECH
        depends_on: []
        digest_outline: |
          Bash script under tools/scripts/claude-hooks/. Reads $CLAUDE_SESSION_CONTEXT JSON.
          Greps emitting response for Verification block JSON header regex.
          Exit 2 + stderr reason when touched-files regex matches ^(Assets/Scripts/.*\.cs|tools/mcp-ia-server/.*|Domains/.*)
          AND emitting response lacks Verification block header. Otherwise exit 0.
        touched_paths: ['tools/scripts/claude-hooks/stop-verification-required.sh']
        kind: code
      - id: '1.0.2'
        title: 'Extend skill-surface-guard.sh with tests/scenarios denylist branch'
        prefix: TECH
        depends_on: ['1.0.1']
        digest_outline: |
          Add regex branch to existing skill-surface-guard.sh: deny Write/MultiEdit on
          ^(tests|tools/fixtures/scenarios)/.* unless TD_ALLOW_TEST_EDIT={ISSUE_ID} env var set.
          For Edit: parse old_string/new_string; deny if removing [Test] / it( / test( declaration tokens
          without env var override.
        touched_paths: ['tools/scripts/claude-hooks/skill-surface-guard.sh']
        kind: code
      - id: '1.0.3'
        title: 'Wire hooks.Stop[] matcher in .claude/settings.json'
        prefix: TECH
        depends_on: ['1.0.1']
        digest_outline: |
          Append entry to hooks.Stop[] invoking tools/scripts/claude-hooks/stop-verification-required.sh.
          Format mirrors existing PreToolUse hook entries.
        touched_paths: ['.claude/settings.json']
        kind: code
      - id: '1.0.4'
        title: 'Wire hooks.PreToolUse[] chain in .claude/settings.json'
        prefix: TECH
        depends_on: ['1.0.2']
        digest_outline: |
          Update Edit|Write|MultiEdit matcher to chain skill-surface-guard.sh denylist branch
          after existing skill-surface-guard.sh entry. Sequential execution.
        touched_paths: ['.claude/settings.json']
        kind: code
      - id: '1.0.5'
        title: 'Hook smoke test — stop verification required'
        prefix: TECH
        depends_on: ['1.0.3']
        digest_outline: |
          Assert exit 2 on Assets/** touch without Verification block; exit 0 with block;
          exit 0 on docs-only sessions. Fixture under tests/hooks/.
        touched_paths: ['tests/hooks/stage1-stop-verification.test.mjs']
        kind: code
      - id: '1.0.6'
        title: 'Hook smoke test — test-write denylist'
        prefix: TECH
        depends_on: ['1.0.4']
        digest_outline: |
          Assert exit 2 on Write to tests/foo.test.mjs; exit 0 with TD_ALLOW_TEST_EDIT=BUG-1234;
          assert exit 2 on Edit removing [Test] without env override.
        touched_paths: ['tests/hooks/stage1-test-denylist.test.mjs']
        kind: code
  - id: '2.0'
    title: 'Wave A finalization — docs cross-links + rule prose'
    exit: |
      ia/rules/agent-principles.md Testing+verification section links to new hook scripts with exit-code semantics.
      docs/agent-led-verification-policy.md cross-refs Stop hook as enforcement layer for Verification block requirement.
    red_stage_proof: |
      # design_only — validator checks doc cross-refs exist
      grep -q "stop-verification-required.sh" ia/rules/agent-principles.md
      grep -q "Stop hook" docs/agent-led-verification-policy.md
    red_stage_proof_block:
      red_test_anchor: 'design-only-test:ia/rules/agent-principles.md::HookCrossRef'
      target_kind: design_only
      proof_artifact_id: 'ia/rules/agent-principles.md'
      proof_status: not_applicable
    tasks:
      - id: '2.0.1'
        title: 'Cross-ref hooks in agent-principles.md Testing+verification section'
        prefix: TECH
        depends_on: []
        digest_outline: |
          Replace existing policy prose pointing to enforcement strategy with link to new hook scripts.
          Mention exit codes (0 allow, 2 deny). Cross-ref tools/scripts/claude-hooks/{stop-verification-required,skill-surface-guard}.sh.
        touched_paths: ['ia/rules/agent-principles.md']
        kind: doc-only
      - id: '2.0.2'
        title: 'Cross-ref Stop hook in agent-led-verification-policy.md'
        prefix: TECH
        depends_on: []
        digest_outline: |
          Add subsection naming the Stop hook as the enforcement layer for Verification block requirement.
          Link to tools/scripts/claude-hooks/stop-verification-required.sh.
        touched_paths: ['docs/agent-led-verification-policy.md']
        kind: doc-only
  - id: '3.0'
    title: 'Wave B — EARS rubric rule 11 + /spec-freeze gate'
    exit: |
      ia_master_plan_specs table live with (slug, version) unique key. master_plan_spec_freeze MCP tool registered.
      /spec-freeze skill+slash command authored. /ship-plan refuses non-frozen specs unless --skip-freeze
      (bypass logged to arch_changelog kind=spec_freeze_bypass). validate:plan-digest-coverage enforces EARS prefix
      on each §Acceptance row unless plan.ears_grandfathered=TRUE.
    red_stage_proof: |
      # tests/vibe-coding-safety/stage2-spec-freeze.test.mjs — red until 3.0.7 + 3.0.8 land
      it('refuses /ship-plan when spec not frozen', async () => {
        await db.query("INSERT INTO ia_master_plans (slug, ears_grandfathered) VALUES ('test-plan', FALSE)");
        // no row in ia_master_plan_specs
        const { code, stderr } = await runShipPlan('test-plan');
        assert.equal(code, 1);
        assert.match(stderr, /spec_freeze_required/);
      });
      it('rejects digest §Acceptance row without EARS prefix', async () => {
        const result = await runValidator('validate:plan-digest-coverage', {
          digest: '§Acceptance:\n- The system updates the score.'  // no WHEN/WHILE/IF/WHERE/THE prefix
        });
        assert.equal(result.code, 1);
      });
    red_stage_proof_block:
      red_test_anchor: 'visibility-delta-test:tests/vibe-coding-safety/stage2-spec-freeze.test.mjs::ShipPlanRefusesNonFrozen'
      target_kind: visibility_delta
      proof_artifact_id: 'tests/vibe-coding-safety/stage2-spec-freeze.test.mjs'
      proof_status: failed_as_expected
    tasks:
      - id: '3.0.1'
        title: 'Migration — ia_master_plans add ears_grandfathered column + backfill'
        prefix: TECH
        depends_on: []
        digest_outline: |
          ALTER TABLE ia_master_plans ADD COLUMN ears_grandfathered BOOLEAN NOT NULL DEFAULT FALSE.
          Backfill UPDATE SET TRUE WHERE created_at < $WAVE_B_SHIP_TS (recorded at migration apply).
          Mirrors existing tdd_red_green_grandfathered column pattern.
        touched_paths: ['db/migrations/']
        kind: code
      - id: '3.0.2'
        title: 'Migration — ia_master_plan_specs table'
        prefix: TECH
        depends_on: []
        digest_outline: |
          CREATE TABLE ia_master_plan_specs (id SERIAL PK, slug TEXT NOT NULL, version INTEGER NOT NULL,
          frozen_at TIMESTAMP, body TEXT NOT NULL, open_questions_count INTEGER NOT NULL DEFAULT 0,
          UNIQUE(slug, version)).
        touched_paths: ['db/migrations/']
        kind: code
      - id: '3.0.3'
        title: 'Register MCP tool master_plan_spec_freeze'
        prefix: TECH
        depends_on: ['3.0.2']
        digest_outline: |
          Input {slug, source_doc_path}. Reads Design Expansion section, parses Open Questions count,
          INSERTs ia_master_plan_specs row with frozen_at=NOW(). Emits arch_changelog kind=spec_frozen.
          Fails if open_questions_count > 0.
        touched_paths: ['tools/mcp-ia-server/src/index.ts', 'tools/mcp-ia-server/src/tools/']
        kind: mcp-only
      - id: '3.0.4'
        title: 'Author /spec-freeze skill'
        prefix: TECH
        depends_on: ['3.0.3']
        digest_outline: |
          ia/skills/spec-freeze/SKILL.md + agent-body.md + command-body.md per skill conventions.
          Invokes master_plan_spec_freeze MCP. Emits frozen artifact path to user.
          Slash command + subagent generation via npm run skill:sync:all.
        touched_paths: ['ia/skills/spec-freeze/']
        kind: doc-only
      - id: '3.0.5'
        title: 'Rubric rule 11 in plan-digest-contract.md'
        prefix: TECH
        depends_on: []
        digest_outline: |
          Add hard rule 11 to ia/rules/plan-digest-contract.md. Reference 5 EARS patterns
          (ubiquitous WHEN/THE, event-driven WHEN/IF, state-driven WHILE, unwanted-behavior IF...THEN,
          optional-feature WHERE). Each §Acceptance row must start with one prefix (case-insensitive).
        touched_paths: ['ia/rules/plan-digest-contract.md']
        kind: doc-only
      - id: '3.0.6'
        title: 'Inject rubric rule 11 into /stage-authoring Phase 4 prompt'
        prefix: TECH
        depends_on: ['3.0.5']
        digest_outline: |
          Update ia/skills/stage-authoring/agent-body.md or SKILL.md — Phase 4 prompt template gets
          rule 11 verbatim. Cross-ref the 5 EARS prefixes.
        touched_paths: ['ia/skills/stage-authoring/']
        kind: doc-only
      - id: '3.0.7'
        title: 'Extend validate:plan-digest-coverage to enforce EARS'
        prefix: TECH
        depends_on: ['3.0.5', '3.0.1']
        digest_outline: |
          Skip if plan.ears_grandfathered=TRUE. Else assert each §Acceptance row begins with one of
          5 EARS prefixes (case-insensitive). Exit 1 on violation.
        touched_paths: ['tools/scripts/validate-plan-digest-coverage.mjs']
        kind: code
      - id: '3.0.8'
        title: 'Gate /ship-plan on frozen spec'
        prefix: TECH
        depends_on: ['3.0.3']
        digest_outline: |
          ship-plan SKILL Phase A queries ia_master_plan_specs WHERE slug=$slug ORDER BY version DESC LIMIT 1.
          Reject if frozen_at IS NULL OR open_questions_count > 0. --skip-freeze flag logs to arch_changelog
          kind=spec_freeze_bypass (hotfix escape).
        touched_paths: ['ia/skills/ship-plan/']
        kind: doc-only
      - id: '3.0.9'
        title: 'Stage test — spec-freeze + ship-plan gate + EARS rubric'
        prefix: TECH
        depends_on: ['3.0.7', '3.0.8']
        digest_outline: |
          Single test file asserts: (a) /spec-freeze inserts row + emits arch_changelog;
          (b) /ship-plan refuses non-frozen; (c) validate:plan-digest-coverage rejects non-EARS rows;
          (d) ears_grandfathered=TRUE bypass.
        touched_paths: ['tests/vibe-coding-safety/stage2-spec-freeze.test.mjs']
        kind: code
      - id: '3.0.10'
        title: 'Regenerate skill catalog + IA indexes'
        prefix: TECH
        depends_on: ['3.0.4', '3.0.6']
        digest_outline: |
          npm run skill:sync:all (regenerates .claude/{agents,commands}/spec-freeze.md +
          .cursor/rules/cursor-skill-spec-freeze.mdc). npm run generate:ia-indexes (refreshes indexes).
        touched_paths: []
        kind: code
  - id: '4.0'
    title: 'Wave C — Adaptive MAX_ITERATIONS by gap_reason in /verify-loop'
    exit: |
      verify-loop SKILL.md carries MAX_ITERATIONS_BY_GAP_REASON table. Transient gap_reasons
      (bridge_timeout, lease_unavailable, unity_lock_stale) → 5 retries with exponential backoff.
      Deterministic (compile_error, test_assertion, validator_violation) → 2 retries.
      Escalate-now (unity_api_limit, human_judgment_required) → 0 retries (immediate human poll).
      Hard cap = 5. Backoff helper inline or under tools/scripts/.
    red_stage_proof: |
      # tests/vibe-coding-safety/stage3-adaptive-iterations.test.mjs — red until 4.0.3 lands
      it('grants transient gap_reason 5 retries with exponential backoff', async () => {
        const trace = await runVerifyLoop({ gapReason: 'bridge_timeout', simulateFlap: 4 });
        assert.equal(trace.attempts.length, 5);
        const delays = trace.attempts.slice(1).map((a, i) => a.startedAt - trace.attempts[i].endedAt);
        // base=500, max=8000 → delays grow until cap
        assert(delays[0] < delays[1] && delays[1] < delays[2]);
      });
      it('grants deterministic gap_reason 2 retries', async () => {
        const trace = await runVerifyLoop({ gapReason: 'compile_error', simulateFlap: 5 });
        assert.equal(trace.attempts.length, 2);
      });
      it('skips retry on escalate-now gap_reason', async () => {
        const trace = await runVerifyLoop({ gapReason: 'human_judgment_required' });
        assert.equal(trace.attempts.length, 1);
        assert(trace.escalated);
      });
    red_stage_proof_block:
      red_test_anchor: 'visibility-delta-test:tests/vibe-coding-safety/stage3-adaptive-iterations.test.mjs::TransientGapGets5Retries'
      target_kind: visibility_delta
      proof_artifact_id: 'tests/vibe-coding-safety/stage3-adaptive-iterations.test.mjs'
      proof_status: failed_as_expected
    tasks:
      - id: '4.0.1'
        title: 'Add MAX_ITERATIONS_BY_GAP_REASON canonical table to verify-loop SKILL.md'
        prefix: TECH
        depends_on: []
        digest_outline: |
          Markdown table in ia/skills/verify-loop/SKILL.md mapping gap_reason → max_iterations.
          Transient → 5; deterministic → 2; escalate-now → 0. Hard cap 5.
        touched_paths: ['ia/skills/verify-loop/SKILL.md']
        kind: doc-only
      - id: '4.0.2'
        title: 'Implement exponential-backoff helper'
        prefix: TECH
        depends_on: []
        digest_outline: |
          delay_ms = base * 2^attempt (base=500, max=8000). Helper at tools/scripts/exponential-backoff.mjs.
          Pure function exported for verify-loop body inline use.
        touched_paths: ['tools/scripts/exponential-backoff.mjs']
        kind: code
      - id: '4.0.3'
        title: 'Replace fixed MAX_ITERATIONS=2 with classifier lookup'
        prefix: TECH
        depends_on: ['4.0.1', '4.0.2']
        digest_outline: |
          Update ia/skills/verify-loop/agent-body.md — classifier reads gap_reason →
          MAX_ITERATIONS_BY_GAP_REASON. Insert backoff helper invocation between retries on
          transient gap_reasons. Preserve hard cap = 5.
        touched_paths: ['ia/skills/verify-loop/']
        kind: doc-only
      - id: '4.0.4'
        title: 'Update verify-loop validator / rule prose to new shape'
        prefix: TECH
        depends_on: ['4.0.3']
        digest_outline: |
          Update any validators expecting fixed iter count. Update rule prose mentioning fixed cap
          (ia/rules/agent-principles.md "Testing + verification" section if needed).
        touched_paths: ['ia/rules/', 'tools/scripts/']
        kind: doc-only
      - id: '4.0.5'
        title: 'Stage test — adaptive iterations'
        prefix: TECH
        depends_on: ['4.0.3']
        digest_outline: |
          Asserts transient gap_reason gets 5 retries with growing backoff delays;
          deterministic gets 2; escalate-now gets 0 and triggers human poll.
        touched_paths: ['tests/vibe-coding-safety/stage3-adaptive-iterations.test.mjs']
        kind: code
      - id: '4.0.6'
        title: 'Regenerate skill catalog'
        prefix: TECH
        depends_on: ['4.0.3']
        digest_outline: |
          npm run skill:sync:all to regenerate .claude/commands/verify-loop.md from updated SKILL.md.
        touched_paths: []
        kind: code
  - id: '5.0'
    title: 'Wave D — Feature flag DB table + Unity runtime + interchange JSON + bridge'
    exit: |
      ia_feature_flags table live (slug, stage_id, enabled, default_value, owner). ia_stages.flag_slug column added.
      Assets/Scripts/Core/FeatureFlags.cs static class boot-hydrates from tools/interchange/feature-flags-snapshot.json
      at Awake. Bridge command kind=flag_flip triggers FeatureFlags.InvalidateCache() + re-hydrate.
      Web dashboard renders flag state from ia_feature_flags. Interchange JSON artifact registered in interchange.md.
    red_stage_proof: |
      # tests/vibe-coding-safety/stage4-flags.test.mjs — red until 5.0.4 + 5.0.7 land
      it('FeatureFlags.IsEnabled returns table value after boot hydration', async () => {
        await db.query("INSERT INTO ia_feature_flags (slug, enabled) VALUES ('test-flag', TRUE)");
        await exportSnapshot();
        await unityBridge.send({ kind: 'load_scene', scene: 'MainScene' });
        const result = await unityBridge.eval('FeatureFlags.IsEnabled("test-flag")');
        assert.equal(result, true);
      });
      it('flag_flip bridge command invalidates cache + re-hydrates', async () => {
        await db.query("UPDATE ia_feature_flags SET enabled=FALSE WHERE slug='test-flag'");
        await exportSnapshot();
        await unityBridge.send({ kind: 'flag_flip', slug: 'test-flag' });
        const result = await unityBridge.eval('FeatureFlags.IsEnabled("test-flag")');
        assert.equal(result, false);
      });
    red_stage_proof_block:
      red_test_anchor: 'visibility-delta-test:tests/vibe-coding-safety/stage4-flags.test.mjs::BootHydrationFromInterchangeJson'
      target_kind: visibility_delta
      proof_artifact_id: 'tests/vibe-coding-safety/stage4-flags.test.mjs'
      proof_status: failed_as_expected
    tasks:
      - id: '5.0.1'
        title: 'Migration — ia_feature_flags table'
        prefix: TECH
        depends_on: []
        digest_outline: |
          CREATE TABLE ia_feature_flags (slug TEXT PK, stage_id INTEGER REFERENCES ia_stages(id) ON DELETE SET NULL,
          enabled BOOLEAN NOT NULL DEFAULT FALSE, default_value BOOLEAN NOT NULL DEFAULT FALSE,
          owner TEXT, created_at TIMESTAMP NOT NULL DEFAULT NOW()).
        touched_paths: ['db/migrations/']
        kind: code
      - id: '5.0.2'
        title: 'Migration — ia_stages.flag_slug column'
        prefix: TECH
        depends_on: ['5.0.1']
        digest_outline: |
          ALTER TABLE ia_stages ADD COLUMN flag_slug TEXT NULL REFERENCES ia_feature_flags(slug).
          Optional pointer from Stage row to flag slug.
        touched_paths: ['db/migrations/']
        kind: code
      - id: '5.0.3'
        title: 'Author FeatureFlags.cs static class'
        prefix: TECH
        depends_on: []
        digest_outline: |
          public static class FeatureFlags. IsEnabled(string slug)→bool. private static Dictionary<string,bool> _cache.
          public static void HydrateFromJson(string path) — reads snapshot, populates cache.
          public static void InvalidateCache() — clears cache; next IsEnabled forces re-read.
          Located at Assets/Scripts/Core/.
        touched_paths: ['Assets/Scripts/Core/FeatureFlags.cs']
        kind: code
      - id: '5.0.4'
        title: 'Boot hook — bootstrap MonoBehaviour Awake invokes hydration'
        prefix: TECH
        depends_on: ['5.0.3']
        digest_outline: |
          On existing bootstrap MonoBehaviour (e.g. GameManager or scene-root hub), Awake() invokes
          FeatureFlags.HydrateFromJson("tools/interchange/feature-flags-snapshot.json"). Inspector-wired hub
          preserved (no rename/move per CLAUDE.md memory).
        touched_paths: ['Assets/Scripts/']
        kind: code
      - id: '5.0.5'
        title: 'Register interchange JSON artifact schema'
        prefix: TECH
        depends_on: ['5.0.1']
        digest_outline: |
          Add `feature-flags-snapshot` artifact entry to ia/specs/architecture/interchange.md. JSON schema:
          { artifact: "feature-flags-snapshot", schema_version: 1, flags: [{slug, enabled, default_value}] }.
          Path: tools/interchange/feature-flags-snapshot.json.
        touched_paths: ['tools/interchange/', 'ia/specs/architecture/interchange.md']
        kind: doc-only
      - id: '5.0.6'
        title: 'MCP tool / web export — ia_feature_flags → snapshot artifact'
        prefix: TECH
        depends_on: ['5.0.5']
        digest_outline: |
          Reads ia_feature_flags rows, writes tools/interchange/feature-flags-snapshot.json.
          Register as MCP tool feature_flags_snapshot_write or as web build step.
        touched_paths: ['tools/mcp-ia-server/src/', 'web/']
        kind: mcp-only
      - id: '5.0.7'
        title: 'Bridge command kind flag_flip'
        prefix: TECH
        depends_on: ['5.0.3', '5.0.6']
        digest_outline: |
          Register `flag_flip` kind in tools/mcp-ia-server/src/. Payload {slug}.
          Unity-side AgentBridgeCommandRunner handler invokes FeatureFlags.InvalidateCache() +
          re-hydrate from latest interchange JSON.
        touched_paths: ['tools/mcp-ia-server/src/', 'Assets/Scripts/']
        kind: code
      - id: '5.0.8'
        title: 'Web dashboard read-only flag panel'
        prefix: TECH
        depends_on: ['5.0.1']
        digest_outline: |
          web/app/flags/page.tsx — server component reads ia_feature_flags via Postgres pool.
          Renders flag state (slug, enabled, default_value, owner, stage_id). Read-only first cut.
        touched_paths: ['web/app/flags/page.tsx']
        kind: code
      - id: '5.0.9'
        title: 'Stage test — flag table + boot hydration + bridge flip'
        prefix: TECH
        depends_on: ['5.0.4', '5.0.7']
        digest_outline: |
          Asserts snapshot artifact shape; boot hydration populates FeatureFlags._cache from snapshot;
          bridge flag_flip kind invalidates cache + re-hydrates from new snapshot.
        touched_paths: ['tests/vibe-coding-safety/stage4-flags.test.mjs']
        kind: code
      - id: '5.0.10'
        title: 'Glossary row — Feature flag (Stage-scoped)'
        prefix: TECH
        depends_on: []
        digest_outline: |
          Add row to ia/specs/glossary.md pointing to ia_feature_flags table + FeatureFlags.cs runtime.
          Define Stage-scoped vs global flag semantics.
        touched_paths: ['ia/specs/glossary.md']
        kind: doc-only
  - id: '6.0'
    title: 'Wave E — Multi-agent critic at /ship-final Pass B'
    exit: |
      ia_review_findings table live. 3 critic subagents authored (/critic-style, /critic-logic, /critic-security).
      ship-final SKILL Pass B dispatches all 3 in parallel via Agent tool; blocks plan close on any
      severity=high row. AskUserQuestion override path logged to arch_changelog kind=critic_override.
      review_findings_write MCP tool registered.
    red_stage_proof: |
      # tests/vibe-coding-safety/stage5-critics.test.mjs — red until 6.0.6 lands
      it('dispatches 3 critics in parallel and persists findings', async () => {
        const result = await runShipFinal({ slug: 'test-plan', cumulativeDiff: SAMPLE_DIFF });
        const rows = await db.query("SELECT critic_kind FROM ia_review_findings WHERE plan_slug='test-plan'");
        const kinds = new Set(rows.map(r => r.critic_kind));
        assert.deepEqual(kinds, new Set(['style', 'logic', 'security']));
      });
      it('blocks plan close on severity=high without override', async () => {
        await db.query("INSERT INTO ia_review_findings (plan_slug, critic_kind, severity, body) VALUES ('test-plan', 'security', 'high', 'leaked credential')");
        const result = await runShipFinal({ slug: 'test-plan', userOverride: false });
        assert.equal(result.code, 1);
        assert.match(result.stderr, /critic_block/);
      });
      it('logs override to arch_changelog when operator confirms', async () => {
        const result = await runShipFinal({ slug: 'test-plan', userOverride: true });
        const log = await db.query("SELECT kind FROM arch_changelog WHERE kind='critic_override' AND payload->>'slug'='test-plan'");
        assert.equal(log.length, 1);
      });
    red_stage_proof_block:
      red_test_anchor: 'visibility-delta-test:tests/vibe-coding-safety/stage5-critics.test.mjs::CriticsParallelDispatchHighSeverityBlocks'
      target_kind: visibility_delta
      proof_artifact_id: 'tests/vibe-coding-safety/stage5-critics.test.mjs'
      proof_status: failed_as_expected
    tasks:
      - id: '6.0.1'
        title: 'Migration — ia_review_findings table'
        prefix: TECH
        depends_on: []
        digest_outline: |
          CREATE TABLE ia_review_findings (id SERIAL PK, plan_slug TEXT NOT NULL, stage_id INTEGER NULL,
          critic_kind TEXT NOT NULL CHECK (critic_kind IN ('style','logic','security')),
          severity TEXT NOT NULL CHECK (severity IN ('low','medium','high')), body TEXT NOT NULL,
          file_path TEXT NULL, line_range TEXT NULL, created_at TIMESTAMP NOT NULL DEFAULT NOW()).
        touched_paths: ['db/migrations/']
        kind: code
      - id: '6.0.2'
        title: 'Author /critic-style skill'
        prefix: TECH
        depends_on: []
        digest_outline: |
          ia/skills/critic-style/SKILL.md + agent-body.md + command-body.md. Input: cumulative diff +
          glossary + coding conventions. Output: findings JSON conforming to ia_review_findings shape.
          Caveman tone scan + glossary-term consistency scan + naming-convention scan.
        touched_paths: ['ia/skills/critic-style/']
        kind: doc-only
      - id: '6.0.3'
        title: 'Author /critic-logic skill'
        prefix: TECH
        depends_on: []
        digest_outline: |
          ia/skills/critic-logic/SKILL.md + agent-body.md + command-body.md. Input: cumulative diff +
          invariants summary. Output: findings JSON. Data-flow + invariant-touchpoint + control-flow scan.
        touched_paths: ['ia/skills/critic-logic/']
        kind: doc-only
      - id: '6.0.4'
        title: 'Author /critic-security skill'
        prefix: TECH
        depends_on: []
        digest_outline: |
          ia/skills/critic-security/SKILL.md + agent-body.md + command-body.md. Input: cumulative diff
          filtered to Assets/** + tools/mcp-ia-server/** + web/** touched paths. Output: findings JSON.
          Input-validation + path-traversal + secret-leak scan.
        touched_paths: ['ia/skills/critic-security/']
        kind: doc-only
      - id: '6.0.5'
        title: 'Register MCP tool review_findings_write'
        prefix: TECH
        depends_on: ['6.0.1']
        digest_outline: |
          Input {plan_slug, stage_id?, critic_kind, severity, body, file_path?, line_range?}.
          INSERT ia_review_findings row. Register in tools/mcp-ia-server/src/index.ts.
        touched_paths: ['tools/mcp-ia-server/src/index.ts', 'tools/mcp-ia-server/src/tools/']
        kind: mcp-only
      - id: '6.0.6'
        title: 'Update /ship-final Pass B to dispatch 3 critics in parallel'
        prefix: TECH
        depends_on: ['6.0.2', '6.0.3', '6.0.4', '6.0.5']
        digest_outline: |
          Update ia/skills/ship-final/agent-body.md Pass B — dispatch /critic-style + /critic-logic +
          /critic-security via parallel Agent tool calls (one message, multiple tool uses).
          Each critic emits findings → review_findings_write MCP. Block plan close on severity=high;
          emit AskUserQuestion override prompt; log override to arch_changelog kind=critic_override.
        touched_paths: ['ia/skills/ship-final/']
        kind: doc-only
      - id: '6.0.7'
        title: 'Stage test — critic parallel dispatch + high-severity block + override path'
        prefix: TECH
        depends_on: ['6.0.6']
        digest_outline: |
          Asserts 3 critics dispatched in parallel; findings persisted in ia_review_findings;
          severity=high blocks plan close; operator AskUserQuestion override path persists arch_changelog entry.
        touched_paths: ['tests/vibe-coding-safety/stage5-critics.test.mjs']
        kind: code
      - id: '6.0.8'
        title: 'Regenerate skill catalog + IA indexes'
        prefix: TECH
        depends_on: ['6.0.2', '6.0.3', '6.0.4', '6.0.6']
        digest_outline: |
          npm run skill:sync:all (regenerates .claude/{agents,commands}/critic-{style,logic,security}.md +
          .claude/{agents,commands}/ship-final.md). npm run generate:ia-indexes.
        touched_paths: []
        kind: code
---

# vibe-coding safety disciplines — research, audit, critique, improvement (as of 2026-05)

## Findings

External survey of vibe-coding safety disciplines. Five canonical pillars (CD pipeline as quality gate, executable BDD/SDD specifications, small reversible steps, strict TDD with test-deletion guard, production-like acceptance validation) plus adjacent emerging patterns. Repo-agnostic.

### Continuous Delivery deployment pipeline as quality gate

CD pipeline = objective releasability oracle. Every commit walks the same automated sequence (build, unit, integration, acceptance, deploy-to-staging, smoke). Pipeline red = code not releasable, regardless of author (human or AI). Originates in Humble & Farley *Continuous Delivery* (2009); chapter 5/8 fix the deployment pipeline anatomy and the automated-acceptance-test gate. Feedback loop core property: cycles short, results visible, every commit triggers full chain.

Stop-hook / quality-gate pattern is the 2026 agent-era extension: hook fires when the coding agent is about to deliver a response, receives full session context, decides allow/block, can demand the agent invoke validation tools before responding. Multi-agent layering already standard: one agent writes, another critiques, another tests, another validates compliance/architectural alignment. Industry framing for 2026: "the year of AI quality, not speed". Incident rates rose alongside AI shipping velocity, making mandatory quality gates infrastructure-level concerns.

- The Automated Acceptance Test Gate — https://www.informit.com/articles/article.aspx?p=1621865&seqNum=5
- Chapter 8. Automated Acceptance Testing — https://www.oreilly.com/library/view/continuous-delivery-reliable/9780321670250/ch08.xhtml
- Quality Gates for Coding Agents: How Stop Hooks Add Validation Checkpoints — https://fbakkensen.github.io/ai/devtools/development/2026/03/27/quality-gates-for-coding-agents-how-stop-hooks-make-validation-mandatory.html
- 2025 was the year of AI speed. 2026 will be the year of AI quality. — https://www.coderabbit.ai/blog/2025-was-the-year-of-ai-speed-2026-will-be-the-year-of-ai-quality

### Behavior-Driven Development and executable Given-When-Then specifications

BDD frames requirements as concrete, executable Given-When-Then scenarios authored *before* implementation. Scenarios double as acceptance tests and as the contract the AI agent evaluates its own output against. 2026 evolution: scenario authoring is increasingly AI-assisted (draft Given-When-Then from user stories), but the discipline of writing executable behavior first stays human-owned. BDD scenarios are stored versioned alongside the code they describe and run inside the deployment pipeline as the acceptance gate.

- Behavior-driven development (BDD): an essential guide for 2026 — https://monday.com/blog/rnd/behavior-driven-development/
- Generating Behavior-Driven Development (BDD) Artifacts — https://openreview.net/forum?id=b0efTW3To5

### Spec-Driven Development (SDD) — specification as primary executable artifact

SDD treats a structured, machine-readable specification as the source of truth; code is a regenerable output. By 2026 every major AI coding tool ships an SDD flavor: GitHub Spec Kit (open-source, 30+ agent integrations including Claude Code), AWS Kiro (uses EARS syntax + auto-router across Claude/Qwen/DeepSeek/GLM/MiniMax), OpenSpec (delta-marker workflow: ADDED/MODIFIED/REMOVED for brownfield), BMAD, Tessl, Google Antigravity. Common move: a forces-explicit phase between user intent and code generation, with the agent generating tests *and* code from the same spec. Counters "intent drift" and "context decay" that surfaced when AI coding went mainstream.

- Spec-Driven Development (SDD): The Definitive 2026 Guide · BCMS — https://thebcms.com/blog/spec-driven-development
- Meet GitHub Spec-Kit: An Open Source Toolkit for Spec-Driven Development with AI Coding Agents — https://www.marktechpost.com/2026/05/08/meet-github-spec-kit-an-open-source-toolkit-for-spec-driven-development-with-ai-coding-agents/
- Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html
- GitHub - Fission-AI/OpenSpec — https://github.com/Fission-AI/OpenSpec
- Spec-driven development: Unpacking 2025's key new AI-assisted engineering practice — https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices

### EARS / GEARS requirements syntax

EARS = Easy Approach to Requirements Syntax (Mavin, Rolls-Royce, 2009). Canonical template: `While <pre-condition>, when <trigger>, the <system> shall <response>`. Five patterns (ubiquitous, event-driven, state-driven, unwanted-behavior, optional-feature) force authors to be explicit about triggers, pre-conditions, and system response, removing the ambiguity AI agents otherwise paper over with plausible-looking code. Adopted by Kiro as the spec layer. GEARS (Generalized Expression for AI-Ready Specs) extends EARS to map directly onto Given-When-Then test cases, collapsing spec and test grammar into one source.

- Alistair Mavin EARS: Easy Approach to Requirements Syntax — https://alistairmavin.com/ears/
- Understanding EARS Requirements Syntax for AI — https://makerneo.com/en/articles/what-is-ears-requirements-syntax-how-to-write-better-ai-prompts.html
- GEARS: The Spec Syntax That Makes AI Coding Actually Work — https://dev.to/sublang/gears-the-spec-syntax-that-makes-ai-coding-actually-work-4f3f
- Adopting EARS Notation for Requirements Engineering — Jama Software — https://www.jamasoftware.com/requirements-management-guide/writing-requirements/adopting-the-ears-notation-to-improve-requirements-engineering/

### Strict Test-Driven Development — red→green→refactor for agents

TDD red→green protocol: write failing test, confirm red, write minimum code to pass, confirm green, refactor under green. With LLM agents the discipline becomes load-bearing because the agent's text-prediction tendency is to generate plausible-but-wrong code; the failing test pins the target. 2026 results: TDFlow research workflow hits 88.8% on SWE-Bench Lite by enforcing strict TDD; Claude-Code/Cursor skill ecosystems ship "TDD red-green-refactor" skills coordinating specialized subagents per phase. Observed failure mode: context pollution — when test-writer, implementer, and refactorer share one context window, the test-writer's analysis bleeds into the implementer's thinking. Sub-agent separation (different context per phase) materially raises pass rate.

A second invariant: instruct agents to **never delete or disable tests without explicit human approval**. Common AI failure mode = tests-fail → agent rewrites/deletes the test instead of the code. Hook-based denial (pre-tool-call hook blocks deletes on `tests/**`) is the standard 2026 defence.

- Forcing Claude Code to TDD: An Agentic Red-Green-Refactor Loop — https://alexop.dev/posts/custom-tdd-workflow-claude-code-vue/
- Red/green TDD — Agentic Engineering Patterns — Simon Willison — https://simonwillison.net/guides/agentic-engineering-patterns/red-green-tdd/
- TDFlow: Agentic Workflows for Test Driven Development — https://aclanthology.org/2026.eacl-long.70/
- TDD in the Age of Vibe Coding: Pairing Red-Green-Refactor with AI — https://medium.com/@rupeshit/tdd-in-the-age-of-vibe-coding-pairing-red-green-refactor-with-ai-65af8ed32ae8

### Small reversible steps + trunk-based development with feature flags

Discipline = each AI-generated change small enough to review, revert cheaply, and isolate via a flag. Make one well-reasoned change, observe and verify its effects, decide commit-or-revert. Pairs with trunk-based development: long-lived branches replaced by feature flags so half-baked AI output can be merged behind a flag and disabled instantly if it misbehaves in production. Commit-often + branch-per-feature + frequent diff-review keeps each AI delta inspectable. Larger problems split into smaller chunks fed iteratively, each carrying forward the verified context of the previous step.

- Work in Small, Reversible Steps — The Art of Agile Development — https://www.oreilly.com/library/view/the-art-of/9780596527679/ch13s01.html
- AddyOsmani.com — My LLM coding workflow going into 2026 — https://addyosmani.com/blog/ai-coding-workflow/
- Microservices.io — Make smaller, safer, and reversible changes — https://microservices.io/post/architecture/2024/11/04/premium-smaller-safe-reversible-steps-part-3-incremental-migration.html
- Trunk-based development + feature flags — https://docs.getunleash.io/guides/trunk-based-development
- Feature Flags in Trunk-Based Development — https://www.harness.io/blog/trunk-based-development

### Production-like validation environment + automated acceptance tests

Staging environment must mirror production infrastructure closely (services, data shapes, config, integration partners). Acceptance tests run there exercise the end-to-end paths that unit tests miss: dependency wiring, schema mismatches, config drift, deployment process itself. Teams that automate staging validation see ~40% fewer post-release incidents than manual-check teams. UAT typically also lands here. Self-healing test execution + AI-driven test-failure analysis + intelligent test selection are 2026 enhancements that shorten the staging feedback loop.

- What Is a Staging Environment in Software Testing — https://www.testmuai.com/learning-hub/staging-environment/
- Software Testing in the Staging Phase of Deployment — mabl — https://www.mabl.com/blog/software-testing-in-staging-phase-of-deployment
- Best Staging Environment Automation Tools 2026 — BrowserStack — https://www.browserstack.com/guide/staging-environment-automation-tools

### Agent hooks, stop hooks, and tool-call denylists

Hooks are programmable interceptors that fire on agent lifecycle events (pre-tool-call, on-stop). They enforce policies the agent cannot bypass via prompt persuasion: block file writes outside project root, block deletion of `tests/**`, block destructive shell verbs (`rm -rf`, `git push --force`, `git reset --hard`), demand validation runs before claiming "done". Pre-tool-call hook validates the *intent* of the call (path, content) and returns deny-with-reason; the agent receives the denial as tool output and self-corrects. 2026 cautionary tale: AI coding agent deleted a production database in 9 seconds because no hook gated `DROP DATABASE`. Hooks are now the recommended primary defence layer.

- Quality Gates for Coding Agents — Stop Hooks — https://fbakkensen.github.io/ai/devtools/development/2026/03/27/quality-gates-for-coding-agents-how-stop-hooks-make-validation-mandatory.html
- Hooks reference — Claude Code Docs — https://code.claude.com/docs/en/hooks
- Agent Hooks — htek.dev — https://htek.dev/articles/agent-hooks-controlling-ai-codebase/
- How a Coding Agent Deleted a Production Database in 9 Seconds — https://dev.to/sahil_kat/how-a-coding-agent-deleted-a-production-database-in-9-seconds-1a
- Preventing AI Agent Configuration Drift with Agent Contract Testing — https://earezki.com/ai-news/2026-05-05-i-built-a-tiny-ci-tool-to-keep-ai-agent-configs-from-drifting-in-my-repo/

### Multi-agent critic/reviewer pipeline

Generator agent produces code; specialized critic agents score against rubric (style critic, logic critic, security critic). Generator revises. Reduces hallucination + policy violations; production pattern in 2026. Reported metrics: false-positive rate dropped 40% → 12% with negative-example feedback loops; specialized agents per concern (style/logic/security) outperform single broad-objective reviewer; security agent catching one auth bypass returned 230× ROI.

- Optimizing AI Code Reviews: A Multi-Agent Pipeline Approach — https://earezki.com/ai-news/2026-04-13-how-i-built-a-multi-agent-code-review-pipeline/
- How to Stop AI Agents from Hallucinating Silently with Multi-Agent Validation — https://dev.to/aws/how-to-stop-ai-agents-from-hallucinating-silently-with-multi-agent-validation-3f7e
- 2026 Agentic Coding Trends Report (Anthropic) — https://resources.anthropic.com/hubfs/2026%20Agentic%20Coding%20Trends%20Report.pdf

### Vibe-coding vulnerability evidence

Empirical 2026 evidence for *why* the disciplines above matter. AI-generated code contains security flaws ~45% of the time (Veracode) to ~62% (Cloud Security Alliance). AI-assisted developers produce commits at 3–4× the rate of peers but introduce security findings at 10× the rate. March 2026 saw 35 CVEs directly attributed to AI-generated code (up from 6 in January). Common patterns: injection vulnerabilities, broken authentication, missing input validation, hardcoded credentials, misconfigurations (75% more common in AI-co-authored code). Mitigation consensus: human review non-negotiable, static analysis + dynamic testing + dependency scanning mandatory, AI off-limits for high-risk surfaces (auth, payments, infrastructure scripts).

- Securing vibe coding: The hidden risks behind AI-generated code (Wits) — https://www.wits.ac.za/news/latest-news/opinion/2026/2026-03/securing-vibe-coding-the-hidden-risks-behind-ai-generated-code.html
- AI Generated Code Vulnerabilities: 7 Security Risks in 2026 — https://vibecoding.app/blog/ai-generated-code-security-risks
- CSA Vibe Coding's Security Debt: The AI-Generated CVE Surge — https://labs.cloudsecurityalliance.org/research/csa-research-note-ai-generated-code-vulnerability-surge-2026/
- Vibe Coding Security — Checkmarx — https://checkmarx.com/blog/security-in-vibe-coding/

### Cross-cutting observations

- Dominant: spec-as-truth (SDD with EARS-shaped requirements), strict red→green TDD with separated agent contexts per phase, pipeline-as-quality-gate (`pipeline red → not releasable`), small-reversible-steps with trunk + flags, programmable hooks denying dangerous tool calls.
- Emerging: multi-agent critic pipelines (generator/style/logic/security as separate agents), agent contract tests for config drift, GEARS spec syntax converging spec + test grammar.
- Declining: long-lived AI branches with monolithic merges; single-agent single-context TDD (context pollution evidence); "human review at the end" as the sole defence (volume now exceeds human review bandwidth).
- Recency anchor: 2026-05. All sources within 24 months unless canonical (Humble & Farley 2009, Mavin EARS 2009 retained as foundational).

## Audit — current implementation in repo

Scope = the Territory Developer agent-driven development lifecycle: how agent-authored code reaches main and what gates it crosses on the way.

### Entry points

- Lifecycle chain (canonical, single map). `docs/agent-lifecycle.md` §1: `/design-explore` → `/ship-plan` → `/ship-cycle` → `/ship-final`. Single-issue variant: `/project-new` → `/author --task` → `/ship`. Each seam = one slash command + one generated subagent under `.claude/agents/` + one `ia/skills/{slug}/SKILL.md` source.
- Skills source of truth: `ia/skills/{ship-cycle,verify-loop,ship-plan,ship-final,design-explore,project-new,...}/SKILL.md`. `.claude/agents/*.md` + `.claude/commands/*.md` are generated; direct edits caught by `npm run validate:skill-drift` (gate inside `validate:all`).
- Hooks: `.claude/settings.json` registers SessionStart (prewarm), PreToolUse (Bash denylist, big-file-read-warn, skill-surface-guard), PostToolUse (cs-edit-reminder, validate-all filter). Scripts under `tools/scripts/claude-hooks/`.
- Bash denylist (`tools/scripts/claude-hooks/bash-denylist.sh`): blocks `git push --force*`, `git reset --hard*`, `git clean -fd*`, `rm -rf {ia,MEMORY.md,.claude,.git,/,~}*`, `sudo *`.
- Verification policy contract: `docs/agent-led-verification-policy.md`. Two operative paths: Path A (`unity:testmode-batch` batchmode) and Path B (IDE agent bridge against running Editor). Verification block JSON shape lives here; agents must report all rows that were run.
- Methodology rules: `ia/rules/prototype-first-methodology.md` (Stage 1.0 = tracer slice; Stages 2+ = §Visibility Delta); `ia/rules/tdd-red-green-methodology.md` (every visible-delta Stage carries §Red-Stage Proof: `red_test_anchor`, `target_kind`, `proof_artifact_id`, `proof_status`; pre-impl test must be red).
- §Plan Digest contract: `ia/rules/plan-digest-contract.md`. 10-point rubric per Task: zero open picks, paths verified against HEAD, every Work Item carries explicit intent, single §Invariants & Gate block, single STOP route.

### Data flow

- Plan author (`/ship-plan`) writes `ia_master_plans` + `ia_stages` + `ia_tasks` rows via `master_plan_bundle_apply` Postgres transaction. Each Task gets a §Plan Digest body persisted in DB (not filesystem).
- Stage execution (`/ship-cycle {SLUG} {STAGE_ID}`) is atomic: Pass A bulk-emits all Task implementations in one Sonnet inference bracketed by `<!-- TASK:{ID} START/END -->` markers, runs one aggregated `unity:compile-check` on the union of touched `Assets/**/*.cs`, then `task_status_flip_batch(implemented)`. Pass B runs `/verify-loop` on cumulative `git diff HEAD`, flips each Task `implemented → verified → done`, fires inline `stage_closeout_apply` MCP (single call: spec archive + status flips + id purge + Stage/Plan Status rollup), produces single Stage commit `feat({slug}-stage-{stage_id_db})`, enqueues `cron_stage_verification_flip(pass)`.
- Pass A entry gate: `red_stage_proof_capture` MCP runs the anchored failing test pre-implementation. Returns `failed_as_expected` (proceed), `unexpected_pass` (REJECT — test already green, false-green), or `not_applicable` (`target_kind=design_only`). Hard stop on `unexpected_pass`.
- `/verify-loop` is the closed-loop verification recipe. Composes 5 atomic skills: bridge-environment-preflight → project-implementation-validation (`validate:all` + compile gate) → agent-test-mode-verify (Path A) → ide-bridge-evidence (Path B with Play Mode lease) → close-dev-loop. Bounded fix iteration `MAX_ITERATIONS=2`; writes §Findings to Task body. Verdict JSON: `verdict ∈ {pass, fail, escalated}` + `gap_reason ∈ {unity_api_limit, bridge_kind_missing, human_judgment_required}` when escalated.
- Token budget cap on Pass A inference: 80k input; over cap → fallback `/ship-stage-main-session` legacy two-pass adapter.
- BACKLOG view: `BACKLOG.md` is generated from DB via `tools/scripts/materialize-backlog.sh`. Source rows in `ia_tasks`; archive rows in same table with `archived_at` set.

### Constraints

- Invariants force-loaded via `ia/rules/invariants.md` (universal) + `ia/rules/unity-invariants.md` (on-demand when touching `Assets/**`). Numbers 1–13 with merged shape via `invariants_summary` MCP.
- MCP-first: prefer `mcp__territory-ia__*` over reading whole `ia/specs/*.md`. Tool order: `backlog_issue` → `router_for_task` → `glossary_discover`/`lookup` → `spec_outline`/`spec_section`/`spec_sections` → `invariants_summary`/`list_rules`/`rule_content`.
- `npm run validate:all` chain. Mutating sub-chain: `compute-lib:build`, `test:ia`, `validate:fixtures`, `validate:backfill-fixtures`, `generate:ia-indexes --check`, `test:recipe-engine`, `smoke:seam-q5`. Read-only fan-out (~50 parallel validators) covers master-plan status rollup, prototype-first 5-field tracer slice, red-stage proof anchor + 4-field schema, arch-coherence, backlog-yaml, telemetry schema, runtime-state, cache-block sizing, claude-imports, agent-tools, skill-drift, skill-changelog-presence, MCP catalog/readme/descriptor-prose, seam-golden, recipe-drift, mcp-catalog-coverage, handoff-schema, retired-skill-refs, plan-digest-coverage, asset-pipeline, drift-lint, csharp-fast, no-domain-game-cycle, asmdef-graph, action-bind-drift, ui-id-consistency, scene-wire-drift, visual-regression, no-hub-fat, no-service-fat, no-legacy-ugui-refs, registry-resolve-pattern, design-explore-render.
- `npm run verify:local` = `validate:all` + `unity:compile-check` + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`.
- `verify:local` is local-only; CI workflow surface is two GitHub Actions files: `.github/workflows/ia-tools.yml` + `.github/workflows/web-tests.yml`. Unity compile + EditMode + bridge smoke are NOT in CI today — they run on dev machine.
- Active arch_decisions touching the lifecycle: DEC-A22 (prototype-first), DEC-A23 (TDD red-green dual methodology), DEC-A19 (recipe-runner two-layer model: deterministic recipe-engine + narrow LLM seams), DEC-A26 (async-cron job queue for non-blocking writes), DEC-A27 (agent-to-agent IPC broker).
- Plan-Digest contract rules 1–9 hard (no open picks, paths verified against HEAD, every Work Item explicit, single §Invariants & Gate block, single STOP route), rule 10 soft (byte caps warn-only).

### Coverage

- Hooks coverage: SessionStart prewarm, PreToolUse Bash denylist + skill-surface-guard + big-file-read-warn, PostToolUse cs-edit-reminder + validate-all filter. No PreToolUse hook on Edit/Write that gates writes to `tests/**` or scenario fixtures. No stop-hook on agent response that demands a Verification block before claiming "done" — discipline carried by skill prose + verify-loop hard boundary, not by hook denial.
- Test fixtures: `tools/fixtures/scenarios/{reference-flat-32x32, descriptor-declarative-default-32x32, descriptor-street-row-32x32, neighbor-stub-roundtrip-32x32, parent-id-{legacy,seeded}-32x32}`. Scenario-id passed to `unity:testmode-batch`; `--golden-path` asserts integer CityStats fields against committed JSON — mismatch → exit 8.
- Stage-scoped test files: `tests/{plan-slug}/stage{N}-{slug}.test.{mjs|cs}` — one file per Stage, grown task-by-task. Red on first task, green on last. Existing folders: `bake-pipeline`, `city-scene-loading-perf-quick-wins`, `cityscene-mainmenu-panel-rollout`, `cityscene-v3-repair`, `ui-bake-hardening-v2`, `ui-toolkit-migration`. Per `ia/rules/agent-principles.md`: stage-close requires file fully green; master-plan close unions all stage files.
- §Red-Stage Proof gate (DEC-A23): mandatory on every non-grandfathered Stage with player-visible delta. Pre-Stage-6 plans grandfathered via `tdd_red_green_grandfathered=TRUE` on `ia_master_plans`. Validator `validate:plan-red-stage` exit 1 on missing anchor; `validate:red-stage-proof-anchor` enforces anchor-method body references surface keywords from anchor prose (drift gate).
- Visual regression: `validate:visual-regression` + sweep orchestrator `tools/scripts/sweep-visual-baselines.mjs`. Region masks + per-panel tolerance overrides + `AskUserQuestion` approval gate. Baseline rows in `ia_visual_baseline`.
- Verification block JSON shape: required fields per `docs/agent-led-verification-policy.md` — Node/IA exit code, Unity compile exit code, NUnit EditMode passed/failed/errors/skipped (XML parsed by `tools/scripts/parse-nunit-xml.mjs`), Path A `tools/reports/agent-testmode-batch-*.json` path + ok/exit_code, Path B `unity_bridge_command` ok/error/timeout + command_id. Path B skip must state reason; do not omit row.
- Production environment: none. The game is a Unity 2D city builder shipped as a build; "production-like" = the local Editor + `unity:testmode-batch` batchmode. Web surface (`web/`) has its own staging story (Vercel) tracked in `web/README.md`.

## Critique — strengths and weaknesses

Strengths and weaknesses observed from §Audit. No comparison against §Findings here.

### Strengths

- §Audit · Methodology rules carry mandatory pre-implementation failing-test gate (`red_stage_proof_capture` MCP rejects `unexpected_pass`), backed by validator `validate:plan-red-stage` + anchor-drift gate `validate:red-stage-proof-anchor`. Defends against the "test rewritten after implementation" vibe-coding failure mode.
- §Audit · Constraints — Bash denylist is hook-enforced (PreToolUse), blocks destructive shell verbs that AI agents otherwise issue with no friction. The 9-second-prod-DB-deletion class of failure is closed for shell operations.
- §Audit · Data flow — single Stage commit at Pass B end gives one revert point per Stage; combined with stage-scoped test file means rollback granularity is bounded and predictable.
- §Audit · Coverage — `validate:all` fans out ~50 validators in parallel including IA-shape gates (skill-drift, recipe-drift, scene-wire-drift, csharp-fast). One green = one releasable IA + tooling state.
- §Audit · Entry points — verification policy is documented once (`docs/agent-led-verification-policy.md`) and every verify-related skill defers to it instead of restating. Single source of truth on timeout escalation, Path A lock release, Path B preflight.
- §Audit · Data flow — `/verify-loop` bounded fix iteration `MAX_ITERATIONS=2` prevents the agent from chasing fix→break→fix loops indefinitely; escalates with typed `gap_reason` to a human after cap.
- §Audit · Constraints — Plan-Digest rules 1–9 force agents to resolve picks at planning time (no "user decides", "TBD", "we could") and verify every path against HEAD before implementation begins. Picks-resolved-up-front compresses ambiguity bandwidth the implementer would otherwise hallucinate over.

### Weaknesses

- §Audit · Coverage — no PreToolUse hook denying writes/deletes to `tests/**` or `tools/fixtures/scenarios/**`. Discipline against "agent disables the failing test" is policy-level (`ia/rules/agent-principles.md`), not hook-enforced. Hook layer exists for Bash but does not extend to Edit/Write surfaces beyond skill-surface-guard.
- §Audit · Coverage — Unity compile, NUnit EditMode tests, and Path A `unity:testmode-batch` do not run in CI; only `ia-tools.yml` + `web-tests.yml` are wired. A branch with Unity regressions can pass GitHub Actions and still be broken locally. `verify:local` is dev-machine only.
- §Audit · Data flow — Pass A bulk-emits all Tasks of one Stage in a single inference. One unified compile-check covers the union diff, but per-Task isolation is lost: a compile failure in Task 3 forces re-running the Stage even though Tasks 1–2 may be byte-identical. Resume gate handles this in DB but the inference cost is non-trivial.
- §Audit · Entry points — staging environment for the Unity build does not exist. "Production-like" reduces to local Editor batchmode + scenario fixtures. Integration regressions that only surface on a real player build (asset bundle, build pipeline) are caught by human QA only.
- §Audit · Coverage — no agent-stop-hook demands a Verification block be present in the agent response before claiming "done". Verification policy is contract-level only; an agent that skips Path B without stating why violates policy but the deliverable still lands.
- §Audit · Constraints — `MAX_ITERATIONS=2` is fixed in skill body. Long-tail flaky tests (e.g. bridge timeout cascade) escalate to human after 2 cycles regardless of whether the failure is real or transient.
- §Audit · Data flow — BACKLOG.md is generated from DB but `materialize-backlog.sh` runs inside Pass B closeout, not on every status flip; if Pass B crashes before the materialize step, the file view drifts from the DB until the next successful closeout.
- §Audit · Coverage — no feature-flag layer in the runtime (no flag table, no flag-keyed code branches in `Assets/Scripts`). All AI-authored gameplay code lands "live"; reverting a regression means reverting the Stage commit, not flipping a flag.
- §Audit · Coverage — no multi-agent critic pipeline. `/code-review` retired (2026-05-10); code-fix now applied inline in `/ship-cycle` Pass B. The Stage diff is reviewed by the same agent that wrote it, in the same inference family. Separate-agent-per-concern (style/logic/security) is not in the chain.
- §Audit · Constraints — Pass A 80k token cap is hard. Over-cap stages fall back to `/ship-stage-main-session` legacy two-pass adapter, which is not part of ship-protocol-v2; its long-term maintenance is implicit, not contracted.

## Exploration — 8 ways to improve

Each proposal names a methodology from §Findings, addresses a specific §Critique anchor, and sketches the mechanical change against the audited subsystem.

1. **Stop-hook quality gate on agent response.** Addresses §Critique · Weakness "no agent-stop-hook demands a Verification block before claiming done". Add a Stop hook under `.claude/settings.json` `hooks.Stop[]` invoking `tools/scripts/claude-hooks/stop-verification-required.sh` that scans the about-to-emit response for the canonical Verification block (JSON header + caveman summary) when the session touched `Assets/**/*.cs`, `tools/mcp-ia-server/**`, or `Domains/**`. Missing block → exit 2 + reason "Verification block missing — run `/verify-loop {ISSUE_ID}` first". The hook receives full session context per the 2026 stop-hook pattern. Source: §Findings · Continuous Delivery deployment pipeline as quality gate.

2. **PreToolUse Edit-Write denylist for test and scenario surfaces.** Addresses §Critique · Weakness "no hook denying writes/deletes to tests/scenarios". The hook script reads the tool input JSON, extracts `file_path` and operation kind, and rejects with exit 2 when the path matches `^(tests|tools/fixtures/scenarios)/.*` for Write/MultiEdit operations or when an Edit's `new_string` removes `[Test]` / `it(` / `test(` blocks compared to `old_string`. Wire into the existing PreToolUse `Edit|Write|MultiEdit` matcher in `.claude/settings.json` next to `skill-surface-guard.sh`, running both hooks sequentially. Exception path: explicit `TD_ALLOW_TEST_EDIT=BUG-NNNN` env var lifted by human after an `AskUserQuestion` poll confirms intent. Closes the canonical "agent rewrites the test instead of the code" failure mode at the tool-call layer rather than at policy prose. Source: §Findings · Strict Test-Driven Development — red→green→refactor for agents.

3. **Multi-agent critic pipeline as out-of-band gate before `/ship-final`.** Addresses §Critique · Weakness "no multi-agent critic pipeline; same agent writes and reviews". Reintroduce `/code-review` as three specialized critics dispatched from `ship-final` Pass B: `style-critic` (caveman + glossary alignment + coding-conventions), `logic-critic` (data-flow on Stage cumulative diff), `security-critic` (input-validation + path-traversal + secret-leak scan on touched `Assets/**` + `tools/mcp-ia-server/**` + `web/**`). Each runs in its own context window (no pollution), emits structured findings into `ia_review_findings`, blocks plan close on any `severity=high` finding. Source: §Findings · Multi-agent critic/reviewer pipeline.

4. **EARS-shaped §Acceptance rows in §Plan Digest.** Addresses §Critique · Weakness implicit in "Plan-Digest rules force picks resolved" — currently §Acceptance rows are free-prose "one observable behavior" which agents still ambiguate. Add a §Plan Digest rubric rule 11 (hard): every §Acceptance row must match one of the 5 EARS patterns (ubiquitous `the system shall`, event-driven `when X the system shall`, state-driven `while X the system shall`, unwanted-behavior `if X then the system shall`, optional-feature `where X the system shall`). Enforced in `/stage-authoring` Phase 4 prompt + `validate:plan-digest-coverage` extended to grep for an EARS prefix per row. Source: §Findings · EARS / GEARS requirements syntax.

5. **Trunk-based feature flag table for Stage-scoped gameplay deltas.** Addresses §Critique · Weakness "no feature-flag layer; reverting a regression means reverting the Stage commit". Add `ia_feature_flags(slug, stage_id, enabled, default_value, owner)` + a `FeatureFlags.IsEnabled(slug)` static in `Assets/Scripts/Core/`. Every Stage 2+ §Visibility Delta with player-visible behavior wraps its new entrypoint in `if (FeatureFlags.IsEnabled("{slug}-stage-{X.Y}"))`. Flag defaults `false` for one Stage post-merge, flipped `true` after human play-test sign-off via `AskUserQuestion`. Instant rollback = flag toggle, not git revert. Source: §Findings · Small reversible steps + trunk-based development with feature flags.

6. **Per-Task isolated Pass A inference with shared context bundle + fan-in compile aggregation.** Addresses §Critique · Weakness "Pass A bulk-emit loses per-Task isolation; compile fail in Task 3 forces re-Stage" while preserving the rich-context property that makes the current Pass A produce coherent multi-Task diffs.

    **Tension.** Two opposing forces shape the design:

    - *Context pollution risk (current Pass A weakness).* When all Tasks share one inference window, Task 1's scratch reasoning, false-start patterns, draft variable names, and reverted code lines remain visible in the model's attention when emitting Task 3. Empirical signal: multi-Task Pass A produces measurably more drive-by edits, inconsistent naming between Tasks, and accidental cross-Task coupling than single-Task `/ship` runs.
    - *Rich context need (current Pass A strength).* Per-Stage shared knowledge — Stage §Goal, §Invariants & Gate, glossary subset matched by `router_for_task`, `invariants_summary` merge, ARCH decisions touched, anchor-verified file paths, sibling-Task §Plan Digest bodies (so Task 3 knows Task 1 already wrote helper X) — is load-bearing. Stripping it produces narrow Tasks that re-derive context from scratch, duplicate helpers, and hallucinate API shapes.

    **Mechanical split.** Each Task agent receives two layered context buckets:

    | Layer | Source | Lifecycle | Per-Task content |
    |---|---|---|---|
    | **Shared (pre-fetched, identical across fan-out)** | One Pass A prologue inference (or cached MCP roundtrip) | Computed once per Stage, frozen for fan-out | Stage §Goal, §Invariants & Gate, glossary lookup output, router output, invariants summary, ARCH decisions list, path-verified HEAD manifest, sibling-Task §Plan Digest *bodies* (read-only) |
    | **Isolated (per-Task working memory)** | Single Agent invocation per Task with the shared bundle inlined | Disposed at fan-in | Own §Plan Digest, own §Red-Stage Proof anchor, own §Work Items, own reasoning/scratch, own draft edits, own compile-error feedback |

    **Flow.**
    1. Prologue: one Sonnet inference (cheap; ~5k tokens) emits the *shared context bundle* as a compact frozen artifact (markdown blob, persisted to `ia_stage_context_bundle(slug, stage_id, body, fetched_at)`).
    2. Fan-out: N parallel `Agent` tool calls (one per pending Task), each receiving (a) shared bundle inlined verbatim, (b) own Task §Plan Digest, (c) instruction to emit only its own `<!-- TASK:{ID} START/END -->` block. No cross-Task communication during fan-out.
    3. Fan-in: harness collects N Task diffs, applies them sequentially to working tree, runs one aggregated `unity:compile-check` on the union diff.
    4. Compile pass → `task_status_flip_batch(implemented)` on all N.
    5. Compile fail → identify failing Task(s) by compile-error path attribution, re-fan-out only those Tasks (each gets fresh isolation + the same shared bundle + the prior fail's compile-error feedback as a single extra paragraph). Passing Tasks keep their committed diff untouched. Resume gate driven by `task_state` DB rows.

    **What this buys.**
    - *Pollution closed.* Task 3's inference window contains zero of Task 1's scratch reasoning. Cross-Task naming/structural inconsistency drops because each Task sees siblings only via their finalized §Plan Digest, not their draft thinking.
    - *Rich context preserved.* Shared bundle inlined per Task delivers the same router/glossary/invariants/sibling-spec knowledge the current monolithic Pass A enjoys.
    - *Cheap retry.* A Task 3 compile failure costs one Task re-inference, not a Stage re-emit. Tasks 1, 2, 4 stay byte-identical in the working tree.
    - *Token economy.* Shared bundle computed once amortizes the router/glossary/invariants MCP roundtrips across N Tasks. Per-Task inference is narrow (single Task body, no need to also reason about siblings' implementation choices) → smaller working window, faster Sonnet response.

    **Costs / open questions.**
    - Parallel Agent fan-out fires N concurrent Anthropic API calls. Rate-limit risk on Stages with >6 Tasks; cap concurrency at 4, queue overflow.
    - Shared bundle must be tight (~3-5k tokens). Too thin = Tasks re-derive context; too thick = working window inflation negates token gain. Bundle author prompt needs tuning.
    - Sibling §Plan Digest *bodies* are visible per Task (so Task 3 knows Task 1 introduced helper X). The *implementation diff* of Task 1 is not visible to Task 3 during fan-out — only after fan-in. This is the deliberate isolation boundary; trade-off is occasional cross-Task duplication caught by the aggregate compile-check + the next Stage's review.
    - Failure mode on fan-in conflict: two Tasks edit the same line. Current Pass A serializes naturally; fan-out can't. Mitigation: §Plan Digest rule 12 (proposed) requires explicit `touches_path` declaration per Task; pre-fan-out check rejects Stage if two Tasks declare the same path without merge plan.

    Source: §Findings · Strict Test-Driven Development — red→green→refactor (sub-agent separation per phase) + §Findings · Multi-agent critic/reviewer pipeline (specialized-context-per-concern result).

7. **Adaptive `MAX_ITERATIONS` per failure classification.** Addresses §Critique · Weakness "MAX_ITERATIONS=2 fixed; flaky tests escalate to human regardless of root cause". Replace fixed `MAX_ITERATIONS=2` in `/verify-loop` with a classifier: on each fail, parse `gap_reason` + error signature; transient (`bridge_timeout`, `lease_unavailable`, `unity_lock_stale`) → retry budget 5 with exponential backoff; deterministic (`compile_error`, `test_assertion`, `validator_violation`) → budget 2 as today; `unity_api_limit` / `human_judgment_required` → budget 0 (escalate immediately). Encoded in skill body as `MAX_ITERATIONS_BY_GAP_REASON` table. Source: §Findings · Continuous Delivery deployment pipeline as quality gate (rapid feedback principle).

8. **SDD-style spec-as-truth phase between `/design-explore` and `/ship-plan`.** Addresses §Critique · Weakness "Plan-Digest picks resolved" + indirect §Findings convergence around spec-as-primary-artifact. Insert `/spec-freeze {SLUG}` between explore and plan: emits a Spec Kit-style structured spec (sections: Intent · EARS Acceptance · Invariants · Non-Goals · Open Questions) from the §Design Expansion block, persists to `ia_master_plan_specs(slug, version, frozen_at, body)`, and `/ship-plan` refuses authoring unless the matching version row is frozen and Open Questions = []. Spec is the regenerable contract; code regenerates from it. Tightens DEC-A22 (prototype-first) by making the tracer-slice spec itself a freezable artifact. Source: §Findings · Spec-Driven Development (SDD) — specification as primary executable artifact.

### Conflicts with locked decisions

Conflict scan via `arch_decision_conflict_scan` returned matches at score ≥3 against the following active decisions. Each is interpreted, not auto-flagged:

- Proposal #3 (multi-agent critic pipeline) overlaps **DEC-A19** (`agent-recipe-runner-2026-04-28`, two-layer recipe-engine + narrow LLM seams) and **plan-recipe-runner-phase-e-boundaries** (lifecycle skills scope). Resolution: the critic agents are *new seams*, not replacements; recipe-engine dispatches the three critics through the existing seam-slot pattern (`align-glossary`, `review-semantic-drift` style). Coexists with the 2026-05-10 retirement of `opus-code-review` — new critics run out-of-band at plan-close, not in the Stage chain.
- Proposal #4 (EARS-shaped §Acceptance rows) overlaps **DEC-A22** (`prototype-first-methodology`) and **DEC-A23** (`tdd-red-green-methodology`) and `plan-recipe-runner-phase-e-shared-seams` (seam-slot YAML schema). Resolution: additive rubric rule 11 on §Plan Digest only; does not alter §Tracer Slice (Stage 1.0) or §Red-Stage Proof schema. Seam slot `author-plan-digest` adopts the EARS template; no schema migration on `ia_master_plan_specs`.
- Proposal #5 (feature flag table `ia_feature_flags`) overlaps **DEC-A22** (every Stage carries Visibility Delta) and `plan-master-plan-foldering-refactor-*` (DB-only end-state). Resolution: additive table; flag wraps the §Visibility Delta entrypoint, does not replace the tracer-slice discipline. Migration adds one table + one column on `ia_stages` (`flag_slug TEXT NULL`). Consistent with the DB-primary end-state contract.
- Proposal #6 (Per-Task isolated Pass A with shared context bundle) overlaps **DEC-A19** (recipe-engine + narrow LLM seams) and the ship-cycle Pass A atomic-commit contract. Resolution: the shared-bundle prologue + fan-out fan-in is a *new seam slot* (`stage-context-bundle-author` + `per-task-implement`) layered onto the existing Pass A skeleton; single Stage commit at fan-in end preserved; `ia_stage_context_bundle` is additive (no migration of `ia_tasks` or `ia_stages`).
- Proposal #8 (`/spec-freeze` between design-explore and ship-plan) overlaps **DEC-A15** (`arch-authoring-via-design-explore` — arch decisions persisted inside `/design-explore`) and **DEC-A22** (prototype-first §Core Prototype mapping). Resolution: `/spec-freeze` consumes `§Design Expansion` *after* `/design-explore` Phase 4 arch-authoring; does not duplicate arch-decision authoring. Extends the handoff contract (`docs/agent-lifecycle.md` §3); operator must accept the extra seam. Tightens DEC-A22 by making the tracer-slice spec freezable, not replaces it.
- Proposal #1 (Stop-hook) overlaps **DEC-A6** (`ide-agent-bridge-postgres`, agent stop conditions) at low score (=2). Resolution: hook lives in `.claude/settings.json` Stop matcher; does not touch bridge Postgres queue. No conflict.
- Proposals #2, #7 score ≤2 against any active decision; treated as low-signal token overlap (shared vocabulary `unity`, `compile`, `stage`, `plan`). No structural conflict.

---

## Design Expansion

**Mode.** 7-proposal bundle, ship order A→B→C→D→E. Proposal #6 dropped (parallel-carcass overlap — see Dropped subsection). Critic pipeline (#3) baked into `/ship-final` Pass B only. Feature flags = DB-primary (`ia_feature_flags` table + interchange JSON boot hydration).

**Ship-order rationale.** Risk-first ordering: Wave A closes the highest-impact safety gaps (Verification block omission + test-deletion) via hooks before any structural refactor. Wave B raises spec quality before plans depend on it. Wave C trims inference cost in `/verify-loop`. Wave D adds player-visible rollback. Wave E adds out-of-band critic only after specs are freezable + flags are reversible.

### Plan Shape

**Carcass + section.** Bundle touches 6 surfaces across 5 subsystems (hook layer, validator chain, lifecycle skills, MCP server, DB schema, Unity runtime boot) with no hard dependency chain between Waves — Wave A ships independently of Wave D, Wave C independently of Wave E. Per `ia/rules/design-explore-carcass-alignment-gap-analysis.md` C2, emit ≥3 plan-scoped `arch_decisions`:

1. `plan-vibe-coding-safety-boundaries` — Waves stay layer-scoped (A=hooks, B=spec validators, C=verify-loop skill body, D=DB+runtime, E=ship-final subagents). No Wave touches more than its declared layer.
2. `plan-vibe-coding-safety-end-state-contract` — End state = (a) Stop hook gates Verification block, (b) tests/scenarios write-denied except via env-var exception, (c) §Plan Digest rule 11 EARS rubric live with `ears_grandfathered` mirror, (d) `/spec-freeze` gates `/ship-plan` via `ia_master_plan_specs`, (e) adaptive `MAX_ITERATIONS_BY_GAP_REASON` live, (f) `ia_feature_flags` table + `FeatureFlags.cs` boot-hydrated from interchange JSON + bridge `OnFlagFlipped` signal, (g) 3 critic subagents + `ia_review_findings` table fire at `/ship-final` Pass B only.
3. `plan-vibe-coding-safety-shared-seams` — Shared seams: (a) hook script return-code convention (exit 0 allow / exit 2 deny + reason on stderr), (b) `ears_grandfathered` / `tdd_red_green_grandfathered` mirror column pattern on `ia_master_plans`, (c) interchange JSON `artifact` id pattern for feature-flag boot hydration, (d) `severity` enum on `ia_review_findings` mirrors `gap_reason` enum shape on verify-loop.

### Architecture Decision

**Slug.** `plan-vibe-coding-safety` (base decision) + 3 plan-scoped boundary decisions listed above.

**Rationale (≤250 chars).** Five-Wave ship of safety disciplines surfaced by 2026 vibe-coding research; layered A→E so hook-layer reversibility lands before structural refactor; DB-primary flags + ship-final-only critics avoid Stage-chain inference inflation.

**Alternatives considered (≤250 chars).** (1) Single mega-plan all 8 proposals — rejected (token overflow + entangled dep chain). (2) Per-proposal master plans — rejected (carcass overlap, shared seams duplicated). (3) Drop critics entirely — rejected (single-agent self-review documented weakness). (4) Per-Task fan-out (#6) — DROPPED (parallel-carcass overlap, shipped 2026-04-29).

**Affected arch_surfaces[].**
- `claude-settings-hooks` (Wave A — extends)
- `validate-plan-digest-coverage` (Wave B — extends)
- `ia-master-plan-specs-table` (Wave B — new)
- `master-plan-spec-freeze-mcp` (Wave B — new)
- `verify-loop-skill` (Wave C — extends)
- `ia-feature-flags-table` (Wave D — new)
- `feature-flags-cs-runtime` (Wave D — new)
- `interchange-json-feature-flags-artifact` (Wave D — new)
- `bridge-on-flag-flipped-signal` (Wave D — new)
- `ia-review-findings-table` (Wave E — new)
- `ship-final-critic-subagents` (Wave E — new)

**Arch drift scan.** Expected against open master plans:
- `plan-recipe-runner-phase-e-*` — additive seam slots in Waves B/D/E (no contract change).
- `plan-master-plan-foldering-refactor-*` — flag table consistent with DB-primary end-state.
- No active plan owns hook layer, `/spec-freeze`, `MAX_ITERATIONS_BY_GAP_REASON`, or `ia_review_findings` — net-new surfaces.

**Grandfather pattern.** `ears_grandfathered=TRUE` column added to `ia_master_plans` (mirror of `tdd_red_green_grandfathered`). All plans whose `frozen_at IS NULL` at Wave B ship date are grandfathered out of rule 11. New plans authored post-ship default `FALSE`.

### Components

Grouped by Wave. One-line responsibility each.

**Wave A — Risk-first hook layer.**

| Component | Responsibility |
|---|---|
| `tools/scripts/claude-hooks/stop-verification-required.sh` | Scan emitting response for Verification block JSON header when session touched `Assets/**/*.cs` / `tools/mcp-ia-server/**` / `Domains/**`; exit 2 + reason on miss. |
| `.claude/settings.json` `hooks.Stop[]` matcher | Wire stop hook into Claude Code lifecycle. |
| `tools/scripts/claude-hooks/skill-surface-guard.sh` (extended) | Add tests/scenarios denylist regex `^(tests|tools/fixtures/scenarios)/.*` for Write/MultiEdit; check Edit `new_string` for `[Test]` / `it(` / `test(` removal vs `old_string`. |
| `TD_ALLOW_TEST_EDIT={ISSUE_ID}` env-var exception | Per-session escape lifted by human after `AskUserQuestion` poll. |

**Wave B — Spec-quality layer.**

| Component | Responsibility |
|---|---|
| §Plan Digest rubric rule 11 | Hard rule: every §Acceptance row matches one of 5 EARS patterns. Injected into `/stage-authoring` Phase 4 prompt. |
| `validate:plan-digest-coverage` (extended) | Grep each §Acceptance row for EARS prefix; skip if plan `ears_grandfathered=TRUE`. |
| `ia_master_plans.ears_grandfathered BOOLEAN DEFAULT FALSE` | Mirror of existing `tdd_red_green_grandfathered`; backfilled TRUE for all rows where `frozen_at IS NULL` at ship date. |
| `ia_master_plan_specs(slug, version, frozen_at, body, open_questions_count INTEGER)` table | Persists frozen spec body per (slug, version). |
| `master_plan_spec_freeze` MCP tool | Reads §Design Expansion, emits Spec Kit-shape body (Intent · EARS Acceptance · Invariants · Non-Goals · Open Questions), writes row with `frozen_at=NOW()`, fails if Open Questions count > 0. |
| `/spec-freeze` skill + slash command | Invokes `master_plan_spec_freeze`; emits frozen artifact to user. |
| `/ship-plan` gate | Refuses authoring unless matching `(slug, version)` row exists with `frozen_at IS NOT NULL` and `open_questions_count=0`. `--skip-freeze` hotfix flag logged to `arch_changelog` (kind=`spec_freeze_bypass`). |

**Wave C — Inference-economy layer.**

| Component | Responsibility |
|---|---|
| `MAX_ITERATIONS_BY_GAP_REASON` table in `/verify-loop` SKILL.md | Maps existing `gap_reason` enum → retry budget. Transient (`bridge_timeout`, `lease_unavailable`, `unity_lock_stale`) → 5. Deterministic (`compile_error`, `test_assertion`, `validator_violation`) → 2. Escalate-now (`unity_api_limit`, `human_judgment_required`) → 0. Hard cap = 5. |
| Exponential-backoff helper | `delay_ms = base * 2^attempt` (base=500, max=8000) inserted between retries on transient gap_reasons. |
| `/verify-loop` body update | Replaces fixed `MAX_ITERATIONS=2` with classifier lookup. |

**Wave D — Player-visible rollback layer.**

| Component | Responsibility |
|---|---|
| `ia_feature_flags(slug TEXT PK, stage_id INTEGER FK ia_stages, enabled BOOLEAN, default_value BOOLEAN, owner TEXT)` table | Source of truth for flag state. |
| `ia_stages.flag_slug TEXT NULL` column | Optional pointer from Stage row to flag slug; non-null when Stage has player-visible delta gated by flag. |
| `Assets/Scripts/Core/FeatureFlags.cs` | Static `IsEnabled(slug) → bool` reading from in-memory cache hydrated at boot. |
| Interchange JSON artifact `feature-flags-snapshot` | Exported by web/CI step from `ia_feature_flags` rows; consumed at Unity boot. Shape: `{ artifact: "feature-flags-snapshot", schema_version: 1, flags: [{slug, enabled, default_value}] }`. |
| `OnFlagFlipped` bridge signal | `unity_bridge_command kind=flag_flip slug=...` triggers `FeatureFlags.InvalidateCache()` + re-hydrate from latest interchange JSON. |
| Web dashboard flag panel | Renders flag state from `ia_feature_flags`; humans flip via Web → DB → next interchange export. |

**Wave E — Closeout critic layer.**

| Component | Responsibility |
|---|---|
| `/critic-style` subagent | Caveman + glossary + coding-conventions scan on Stage cumulative diff. Emits structured findings. |
| `/critic-logic` subagent | Data-flow + invariant-touchpoint + control-flow scan on Stage cumulative diff. Emits findings. |
| `/critic-security` subagent | Input-validation + path-traversal + secret-leak scan on `Assets/**` + `tools/mcp-ia-server/**` + `web/**` touched paths. Emits findings. |
| `ia_review_findings(id, plan_slug, stage_id NULL, critic_kind, severity, body, created_at)` table | Persists findings. `severity ∈ {low, medium, high}` enum (mirrors `gap_reason` shape). |
| `/ship-final` Pass B integration | Dispatches 3 critics in parallel; blocks plan close on any `severity=high` row. |
| Operator appeal path | `AskUserQuestion` poll on `severity=high`; override decision logged to `arch_changelog` (kind=`critic_override`). |

### Data Flow + Interfaces

**Wave A — Stop hook trigger.**
```
Claude Code about to emit response
  → Stop hook fires
  → stop-verification-required.sh receives session JSON (env: CLAUDE_SESSION_CONTEXT)
  → script scans tool-call log for Edit/Write on Assets/**, Domains/**, tools/mcp-ia-server/**
  → if touched AND response body missing /^```json\n\{ \"verification_block_v\d/m
      → exit 2 + stderr "Verification block missing — run /verify-loop {ISSUE_ID} first"
      → Claude re-emits with block (or escalates)
  → else exit 0 (allow)
```

**Wave A — Edit/Write denylist.**
```
PreToolUse Edit|Write|MultiEdit fires
  → skill-surface-guard.sh receives tool input JSON
  → extract file_path + tool kind
  → if file_path =~ ^(tests|tools/fixtures/scenarios)/.* AND tool ∈ {Write, MultiEdit}
      → check env $TD_ALLOW_TEST_EDIT
      → if unset → exit 2 + reason "test-surface write blocked; set TD_ALLOW_TEST_EDIT={ISSUE_ID} after AskUserQuestion confirm"
  → if tool == Edit AND file_path =~ ^tests/.*
      → diff old_string vs new_string for removed [Test] / it( / test( blocks
      → if removed AND $TD_ALLOW_TEST_EDIT unset → exit 2
  → else exit 0
```

**Wave B — Spec-freeze flow.**
```
/design-explore completes (Design Expansion persisted)
  → operator runs /spec-freeze {SLUG}
  → master_plan_spec_freeze MCP reads Design Expansion + Open Questions section
  → if Open Questions non-empty → error: "freeze blocked; resolve N open questions"
  → emit Spec Kit body (Intent · EARS Acceptance · Invariants · Non-Goals · Open Questions=[])
  → INSERT ia_master_plan_specs(slug, version=NEXT, frozen_at=NOW(), body, open_questions_count=0)
  → arch_changelog row (kind=spec_freeze)
  → operator runs /ship-plan {SLUG}
  → ship-plan reads ia_master_plan_specs WHERE slug=$SLUG ORDER BY version DESC LIMIT 1
  → if frozen_at IS NULL OR open_questions_count > 0 → REJECT
  → else proceed
```

**Wave C — Adaptive retry.**
```
/verify-loop iteration N fails
  → parse verdict JSON for gap_reason
  → budget = MAX_ITERATIONS_BY_GAP_REASON[gap_reason] || 2
  → if N >= budget → escalate with gap_reason
  → if gap_reason ∈ TRANSIENT_SET → sleep(500 * 2^N ms, capped 8000)
  → retry
```

**Wave D — Flag boot hydration.**
```
CI / web flag toggle
  → UPDATE ia_feature_flags SET enabled = $new WHERE slug = $slug
  → export interchange JSON artifact "feature-flags-snapshot"
       { artifact: "feature-flags-snapshot",
         schema_version: 1,
         flags: [{slug, enabled, default_value}, ...] }
  → write tools/interchange/feature-flags-snapshot.json
  → bridge sends unity_bridge_command kind=flag_flip slug=$slug
  → Unity Editor receives signal
  → FeatureFlags.InvalidateCache()
  → FeatureFlags.HydrateFromJson(tools/interchange/feature-flags-snapshot.json)
  → cache populated; next FeatureFlags.IsEnabled(slug) returns new value
```

At boot (not bridge-triggered):
```
Unity Awake() → FeatureFlags.HydrateFromJson(default snapshot path)
  → if file missing → all flags = default_value (FALSE for new flags)
```

**Wave E — Critic flow.**
```
/ship-final Pass B starts
  → fetch Stage cumulative diff (git diff HEAD ranges per Stage)
  → spawn 3 Agent calls in parallel: /critic-style, /critic-logic, /critic-security
  → each critic receives: diff + invariants summary + glossary subset
  → each critic emits JSON findings: [{severity, body, file_path?, line_range?}, ...]
  → INSERT into ia_review_findings (plan_slug, stage_id NULL, critic_kind, severity, body)
  → if any severity=high
      → AskUserQuestion poll: "Critic found N high-severity finding(s); override?"
      → if override → arch_changelog (kind=critic_override) + proceed
      → else → block plan close, return findings to operator
  → else → proceed to plan close
```

**Interface contracts.**

- Hook return-code convention: exit 0 = allow, exit 2 = deny (stderr carries reason; Claude receives as tool output).
- `ears_grandfathered` mirror pattern: column added with `DEFAULT FALSE`, backfill UPDATE sets TRUE WHERE `frozen_at IS NULL` at migration time.
- Interchange JSON artifact contract: `artifact` (string id) + `schema_version` (integer) + payload. Per glossary "Interchange JSON (artifact)" — tooling/config, not Save data.
- `severity` enum on `ia_review_findings`: `low` | `medium` | `high`. Mirrors shape of `gap_reason` enum (string check constraint).
- `MAX_ITERATIONS_BY_GAP_REASON` table is skill-body-resident (markdown table in `/verify-loop` SKILL.md); validator reads SKILL.md, not a DB row. Hard cap = 5 enforced inline.

### Architecture Diagram

```mermaid
flowchart LR
    subgraph WaveA[Wave A — Hook layer]
        StopHook[stop-verification-required.sh]
        SkillGuard[skill-surface-guard.sh + tests denylist]
        EnvExc[TD_ALLOW_TEST_EDIT env var]
    end

    subgraph WaveB[Wave B — Spec quality]
        SpecFreeze[/spec-freeze/]
        MasterPlanSpecFreezeMCP[master_plan_spec_freeze MCP]
        SpecTable[(ia_master_plan_specs)]
        EARSRubric[Rule 11 EARS rubric]
        EARSValidator[validate:plan-digest-coverage]
        GrandfatherCol[ears_grandfathered]
    end

    subgraph WaveC[Wave C — Inference economy]
        VerifyLoop[/verify-loop SKILL.md]
        IterTable[MAX_ITERATIONS_BY_GAP_REASON]
        Backoff[Exponential backoff helper]
    end

    subgraph WaveD[Wave D — Player rollback]
        FlagsTable[(ia_feature_flags)]
        StagesFlagCol[ia_stages.flag_slug]
        FlagsCS[FeatureFlags.cs]
        InterchangeJSON[feature-flags-snapshot artifact]
        BridgeSignal[OnFlagFlipped bridge signal]
        WebDash[Web dashboard flag panel]
    end

    subgraph WaveE[Wave E — Closeout critics]
        CriticStyle[/critic-style/]
        CriticLogic[/critic-logic/]
        CriticSec[/critic-security/]
        FindingsTable[(ia_review_findings)]
        ShipFinal[/ship-final Pass B]
        AppealPoll[AskUserQuestion appeal]
    end

    ClaudeCode([Claude Code response emit]) --> StopHook
    EditCall([Edit/Write tool call]) --> SkillGuard
    SkillGuard -.exit 2.-> EditCall
    EnvExc -.lifts.-> SkillGuard

    DesignExplore([/design-explore output]) --> SpecFreeze
    SpecFreeze --> MasterPlanSpecFreezeMCP
    MasterPlanSpecFreezeMCP --> SpecTable
    SpecTable --> ShipPlanGate{ship-plan gate}
    ShipPlanGate -.frozen=NULL.-> ShipPlanReject([REJECT])
    EARSRubric --> EARSValidator
    GrandfatherCol -.skips.-> EARSValidator

    VerifyLoop --> IterTable
    IterTable --> Backoff
    Backoff --> VerifyLoop

    FlagsTable --> InterchangeJSON
    InterchangeJSON --> FlagsCS
    BridgeSignal --> FlagsCS
    StagesFlagCol --> FlagsTable
    WebDash --> FlagsTable

    ShipFinal --> CriticStyle
    ShipFinal --> CriticLogic
    ShipFinal --> CriticSec
    CriticStyle --> FindingsTable
    CriticLogic --> FindingsTable
    CriticSec --> FindingsTable
    FindingsTable -.severity=high.-> AppealPoll
    AppealPoll -.override.-> ShipFinalProceed([proceed close])
    AppealPoll -.reject.-> ShipFinalBlock([block close])
```

**ASCII swimlane fallback (entry/exit emphasis).**

```
ENTRY                                   EXIT
-----                                   ----
Claude response emit ─→ Stop hook   ──→ exit 0 / exit 2 (Verification block gate)
Edit/Write call       ─→ skill-guard──→ exit 0 / exit 2 (tests denylist)
/spec-freeze          ─→ MCP write  ──→ ia_master_plan_specs row + arch_changelog
/ship-plan invoke     ─→ freeze gate──→ proceed / REJECT
/verify-loop iter N   ─→ classifier ──→ retry / escalate (per gap_reason)
Unity boot            ─→ hydrate    ──→ FeatureFlags cache populated
Bridge OnFlagFlipped  ─→ invalidate ──→ cache re-hydrated
/ship-final Pass B    ─→ 3 critics  ──→ ia_review_findings rows; severity=high blocks close
```

### Subsystem Impact

| Subsystem | Wave | Dependency nature | Invariant risk (by #) | Breaking vs additive | Mitigation |
|---|---|---|---|---|---|
| Claude Code hook layer (`.claude/settings.json`) | A | New Stop matcher + extended Edit/Write matcher | none | Additive | Hook denial reversible by env var (`TD_ALLOW_TEST_EDIT`); Stop hook scoped by file-touch regex. |
| Plan-Digest validator chain | B | Extends `validate:plan-digest-coverage` | 13 (no hand-edit of `id:` field — n/a, validator-only) | Additive (rubric rule 11 + grandfather flag) | `ears_grandfathered=TRUE` backfill for all in-flight plans at ship date. |
| MCP server (`tools/mcp-ia-server/`) | B + E | New tools `master_plan_spec_freeze`, finding writers; new tables `ia_master_plan_specs`, `ia_review_findings` | none | Additive | New tables; no migration on existing tables except 2 mirror columns (`ears_grandfathered`, `ia_stages.flag_slug`). |
| DB schema | B + D + E | 3 new tables, 2 new columns | 13 (migrations follow `tools/scripts/reserve-id.sh` only for backlog ids — table DDL outside scope) | Additive | Per-Wave migration file; each Wave self-contained. |
| `/verify-loop` skill body | C | Replace fixed budget with classifier table | none | Behaviorally additive (existing budget 2 preserved for deterministic gap_reasons) | Hard cap = 5 prevents runaway; escalate-now for `unity_api_limit` / `human_judgment_required` unchanged. |
| Unity runtime (`Assets/Scripts/Core/`) | D | New `FeatureFlags.cs` + Awake() hydration | 1 (HeightMap immutability — n/a; flags read-only at runtime in C#), 11 (no `Find*` in Update — flags use static cache, no per-frame I/O) | Additive (new file + boot hook) | Cache hydrated once at Awake; bridge signal invalidates explicitly. No per-frame queries. |
| Interchange JSON (tooling artifact) | D | New `feature-flags-snapshot` artifact | none (interchange ≠ Save per glossary) | Additive | New artifact id; schema_version=1; consumers ignore unknown artifacts. |
| Bridge (`unity_bridge_command`) | D | New `kind=flag_flip` command | 13 (n/a — bridge protocol additive) | Additive | New kind; existing kinds untouched. |
| Web dashboard | D | New flag panel reading `ia_feature_flags` | none | Additive | Read-only first ship; flip-from-web shipped in deferred follow-up. |
| `/ship-final` skill (Pass B) | E | Adds 3 parallel subagent dispatch + findings table write | none | Additive (block-on-high only; current Pass B unchanged otherwise) | Critic findings persisted regardless of close decision; operator appeal logged. |

**Invariants flagged:** 1 (HeightMap immutability — Wave D Unity component, mitigated by read-only cache pattern), 11 (no `Find*` in Update — Wave D Unity component, mitigated by Awake-only hydration), 13 (id-counter monotonicity — n/a; new table DDL does not touch `id:` field or counter). No invariants breached; all flagged are additive-with-mitigation.

### Implementation Points

Phased checklist ordered A→B→C→D→E. Per Wave: schema/migration → MCP tool → skill/script → validator → docs.

**Wave A — Risk-first hook layer.**

- [ ] A.1 — Write `tools/scripts/claude-hooks/stop-verification-required.sh` (bash; reads `$CLAUDE_SESSION_CONTEXT` JSON; greps emitting response for Verification block header regex; exit 2 + stderr on miss when touched-files regex matches).
- [ ] A.2 — Extend `tools/scripts/claude-hooks/skill-surface-guard.sh` with tests/scenarios denylist branch + env-var bypass branch.
- [ ] A.3 — Update `.claude/settings.json` `hooks.Stop[]` to invoke A.1 script.
- [ ] A.4 — Update `.claude/settings.json` `hooks.PreToolUse[]` matcher `Edit|Write|MultiEdit` to chain A.2 after `skill-surface-guard.sh` (run sequentially).
- [ ] A.5 — Add hook smoke fixture: `tests/hooks/stage1-stop-verification.test.mjs` (asserts exit 2 on Assets/** touch without block; exit 0 with block; exit 0 on docs-only sessions).
- [ ] A.6 — Add hook smoke fixture: `tests/hooks/stage1-test-denylist.test.mjs` (asserts exit 2 on Write to `tests/foo.test.mjs`; exit 0 with `TD_ALLOW_TEST_EDIT=BUG-1234`).
- [ ] A.7 — Update `ia/rules/agent-principles.md` Testing+verification section: link to new hooks instead of policy prose.
- [ ] A.8 — Update `docs/agent-led-verification-policy.md`: cross-ref Stop hook as enforcement layer.

**Wave B — Spec-quality layer.**

- [ ] B.1 — DB migration: `ia_master_plans` ADD COLUMN `ears_grandfathered BOOLEAN NOT NULL DEFAULT FALSE`; backfill UPDATE SET TRUE WHERE `created_at < $WAVE_B_SHIP_TS`.
- [ ] B.2 — DB migration: CREATE TABLE `ia_master_plan_specs (id SERIAL PK, slug TEXT NOT NULL, version INTEGER NOT NULL, frozen_at TIMESTAMP, body TEXT NOT NULL, open_questions_count INTEGER NOT NULL DEFAULT 0, UNIQUE(slug, version))`.
- [ ] B.3 — Register MCP tool `master_plan_spec_freeze` in `tools/mcp-ia-server/src/index.ts` (input: `{slug, source_doc_path}`; reads Design Expansion, parses Open Questions count, INSERTs row, emits arch_changelog).
- [ ] B.4 — Author `ia/skills/spec-freeze/SKILL.md` (frontmatter triggers `/spec-freeze` slash command + subagent generation via `npm run skill:sync:all`).
- [ ] B.5 — Add rubric rule 11 to `ia/rules/plan-digest-contract.md` (hard rule; reference 5 EARS patterns).
- [ ] B.6 — Update `/stage-authoring` Phase 4 prompt template with rule 11 verbatim.
- [ ] B.7 — Extend `tools/scripts/validate-plan-digest-coverage.mjs`: skip if `ears_grandfathered=TRUE`; else assert each §Acceptance row starts with one of 5 EARS prefixes (case-insensitive).
- [ ] B.8 — `/ship-plan` gate: query `ia_master_plan_specs WHERE slug=$slug ORDER BY version DESC LIMIT 1`; reject if `frozen_at IS NULL OR open_questions_count > 0`. `--skip-freeze` flag logs to `arch_changelog` kind=`spec_freeze_bypass`.
- [ ] B.9 — Add stage-test file `tests/vibe-coding-safety/stage2-spec-freeze.test.mjs` (asserts freeze + ship-plan gate + rubric validator).
- [ ] B.10 — Run `npm run skill:sync:all` + `npm run generate:ia-indexes`.

**Wave C — Inference-economy layer.**

- [ ] C.1 — Add `MAX_ITERATIONS_BY_GAP_REASON` markdown table to `ia/skills/verify-loop/SKILL.md` (canonical mapping per Components table).
- [ ] C.2 — Implement exponential-backoff helper inline in skill body or as `tools/scripts/exponential-backoff.mjs`.
- [ ] C.3 — Replace fixed `MAX_ITERATIONS=2` reference in skill body with classifier lookup; preserve hard cap = 5.
- [ ] C.4 — Update `verify-loop` validator (if any) or rule prose to expect the new table.
- [ ] C.5 — Stage test `tests/vibe-coding-safety/stage3-adaptive-iterations.test.mjs` (asserts transient gap_reason gets 5 retries; deterministic gets 2; escalate-now gets 0).
- [ ] C.6 — Run `npm run skill:sync:all`.

**Wave D — Player-visible rollback layer.**

- [ ] D.1 — DB migration: CREATE TABLE `ia_feature_flags (slug TEXT PK, stage_id INTEGER REFERENCES ia_stages(id) ON DELETE SET NULL, enabled BOOLEAN NOT NULL DEFAULT FALSE, default_value BOOLEAN NOT NULL DEFAULT FALSE, owner TEXT, created_at TIMESTAMP NOT NULL DEFAULT NOW())`.
- [ ] D.2 — DB migration: `ia_stages` ADD COLUMN `flag_slug TEXT NULL REFERENCES ia_feature_flags(slug)`.
- [ ] D.3 — Add `Assets/Scripts/Core/FeatureFlags.cs` (static `IsEnabled(string slug) → bool`; private `Dictionary<string, bool> _cache`; `HydrateFromJson(string path)`; `InvalidateCache()`).
- [ ] D.4 — Add boot hook: `Awake()` on bootstrap MonoBehaviour invokes `FeatureFlags.HydrateFromJson("tools/interchange/feature-flags-snapshot.json")`.
- [ ] D.5 — Register interchange JSON artifact `feature-flags-snapshot` schema (path under `tools/interchange/` + JSON schema doc).
- [ ] D.6 — Add MCP tool or web export step that reads `ia_feature_flags` and writes the snapshot artifact.
- [ ] D.7 — Register bridge command kind `flag_flip` in `tools/mcp-ia-server/src/index.ts` (delegates to Unity bridge; payload `{slug}`); Unity-side handler calls `FeatureFlags.InvalidateCache()` + re-hydrate.
- [ ] D.8 — Web dashboard read-only flag panel under `web/app/flags/page.tsx` (server component reading `ia_feature_flags`).
- [ ] D.9 — Stage test `tests/vibe-coding-safety/stage4-flags.test.mjs` (asserts snapshot artifact shape, boot hydration, bridge flip cache invalidation).
- [ ] D.10 — Update `ia/specs/architecture/interchange.md` with new artifact id.
- [ ] D.11 — Glossary row: `Feature flag (Stage-scoped)` → `ia_feature_flags` table.

**Wave E — Closeout critic layer.**

- [ ] E.1 — DB migration: CREATE TABLE `ia_review_findings (id SERIAL PK, plan_slug TEXT NOT NULL, stage_id INTEGER NULL, critic_kind TEXT NOT NULL CHECK (critic_kind IN ('style','logic','security')), severity TEXT NOT NULL CHECK (severity IN ('low','medium','high')), body TEXT NOT NULL, file_path TEXT NULL, line_range TEXT NULL, created_at TIMESTAMP NOT NULL DEFAULT NOW())`.
- [ ] E.2 — Author `ia/skills/critic-style/SKILL.md` (input: cumulative diff + glossary + invariants; output: findings JSON).
- [ ] E.3 — Author `ia/skills/critic-logic/SKILL.md` (input: cumulative diff + invariants; output: findings JSON).
- [ ] E.4 — Author `ia/skills/critic-security/SKILL.md` (input: cumulative diff filtered to security-sensitive paths; output: findings JSON).
- [ ] E.5 — Update `ia/skills/ship-final/SKILL.md` Pass B: dispatch 3 critics via parallel Agent tool calls; persist findings via new MCP tool `review_findings_write`.
- [ ] E.6 — Register MCP tool `review_findings_write` in `tools/mcp-ia-server/src/index.ts`.
- [ ] E.7 — Block-on-high logic + `AskUserQuestion` appeal in `/ship-final` Pass B; log override to `arch_changelog` kind=`critic_override`.
- [ ] E.8 — Stage test `tests/vibe-coding-safety/stage5-critics.test.mjs` (asserts 3 critics dispatched in parallel, findings persisted, severity=high blocks close, override path logged).
- [ ] E.9 — Run `npm run skill:sync:all` + `npm run generate:ia-indexes`.

**Dropped from bundle.**

- **Proposal #6 — Per-Task isolated Pass A inference with shared context bundle.** Rationale: overlaps with the parallel-carcass section-claim model shipped 2026-04-29 (`docs/parallel-carcass-exploration.md`). The carcass model already provides per-section isolation with shared anchor context; layering Pass A fan-out on top would duplicate the seam-slot pattern under a different name (`per-task-implement`) without distinct value. Re-evaluate if a future master plan demonstrates carcass model fails for ship-cycle Pass A specifically.

**Deferred / out of scope.**

- Flag promotion-to-permanent workflow (when a flag has been TRUE long enough that the conditional should be removed from `Assets/**` source — manual cleanup PR, not automated).
- Critic findings dashboard polish (read-only panel + filter UI on `web/app/findings/page.tsx`).
- Multi-language EARS variants (current rubric is English-only; Spanish/Japanese variants out of scope).
- Flip-from-web flag toggle UI (Wave D ships read-only panel; flip-from-web in follow-up).
- CI integration of Unity compile + EditMode + bridge smoke (separate plan; current §Critique weakness on CI gap intentionally not addressed in this bundle).
- Staging environment for Unity builds (no production exists; deferred until distribution model is set).

### Examples

**Stop-hook trigger logic (paths-touched gate).**

Input — session touched only `docs/research/vibe-coding-safety.md`:
```
$ stop-verification-required.sh < session.json
$ echo $?
0
```

Input — session touched `Assets/Scripts/Managers/GameManagers/WaterManager.cs`, response body has no Verification block:
```
$ stop-verification-required.sh < session.json
Verification block missing — run `/verify-loop {ISSUE_ID}` first
$ echo $?
2
```

Edge case — session touched `Assets/**` but response body contains `markdown json` fenced block with `verification_block_v1`:
```
$ stop-verification-required.sh < session.json
$ echo $?
0
```

**EARS row shape (§Plan Digest).**

Input — `/stage-authoring` Phase 4 emits §Acceptance row:
```
- Water tile under flood mask flips to wet state on next tick.
```
Validator output (rubric rule 11 fail): `EARS prefix missing on row "Water tile under flood mask flips to wet state on next tick."`

Output — same row, EARS-shaped (event-driven pattern):
```
- WHEN flood mask covers a water tile, THE SYSTEM SHALL flip that tile to wet state on the next tick.
```
Validator pass.

Edge case — grandfathered plan (`ears_grandfathered=TRUE`):
```
- Water tile under flood mask flips to wet state on next tick.
```
Validator skips (grandfather flag honored).

**Adaptive MAX_ITERATIONS triggering.**

Input — `/verify-loop` iter 1 fails with `gap_reason=bridge_timeout`:
```
budget = MAX_ITERATIONS_BY_GAP_REASON["bridge_timeout"] = 5
delay = 500 * 2^0 = 500 ms
→ sleep 500ms, retry
```

Input — `/verify-loop` iter 3 fails with `gap_reason=compile_error`:
```
budget = MAX_ITERATIONS_BY_GAP_REASON["compile_error"] = 2
3 >= 2 → escalate (gap_reason=compile_error)
```

Edge case — `gap_reason=unity_api_limit` on first fail:
```
budget = 0
1 >= 0 → escalate immediately, no retry
```

**Feature-flag boot hydration (interchange JSON → C# cache).**

Input — `tools/interchange/feature-flags-snapshot.json`:
```json
{
  "artifact": "feature-flags-snapshot",
  "schema_version": 1,
  "flags": [
    {"slug": "water-flood-v2-stage-2.1", "enabled": false, "default_value": false},
    {"slug": "road-stroke-rebuild-stage-3.0", "enabled": true, "default_value": false}
  ]
}
```

Output — Unity Awake:
```csharp
FeatureFlags.HydrateFromJson("tools/interchange/feature-flags-snapshot.json");
FeatureFlags.IsEnabled("water-flood-v2-stage-2.1"); // false
FeatureFlags.IsEnabled("road-stroke-rebuild-stage-3.0"); // true
FeatureFlags.IsEnabled("unknown-slug"); // false (default)
```

Edge case — snapshot file missing at boot:
```csharp
// FeatureFlags.HydrateFromJson silently no-ops on file-not-found;
// cache stays empty; IsEnabled returns false for every slug (safe default).
```

Edge case — bridge flag flip:
```
operator updates ia_feature_flags SET enabled=TRUE WHERE slug='water-flood-v2-stage-2.1'
→ web export step rewrites snapshot
→ bridge sends unity_bridge_command kind=flag_flip slug=water-flood-v2-stage-2.1
→ Unity calls FeatureFlags.InvalidateCache() + HydrateFromJson(snapshot)
→ FeatureFlags.IsEnabled("water-flood-v2-stage-2.1") now returns true
```

**`/spec-freeze` validator (frozen + Open Questions=[] gate).**

Input — `/ship-plan vibe-coding-safety` invoked, `ia_master_plan_specs` empty for slug:
```
ship-plan: REJECT — no frozen spec for slug 'vibe-coding-safety'. Run /spec-freeze first.
```

Input — `/ship-plan vibe-coding-safety` invoked, frozen row exists but `open_questions_count=2`:
```
ship-plan: REJECT — frozen spec has 2 open questions. Resolve in source doc + re-run /spec-freeze.
```

Input — `/ship-plan vibe-coding-safety --skip-freeze` (hotfix):
```
ship-plan: PROCEED with bypass logged to arch_changelog kind=spec_freeze_bypass
```

Edge case — Design Expansion has Open Questions section but body is empty (`- none`):
```
/spec-freeze parses open_questions_count=0 → row inserted with frozen_at=NOW()
```

**Critic findings flow (subagent emits → table row → severity-high blocks close).**

Input — `/ship-final vibe-coding-safety` Pass B dispatches:
```
parallel: Task(/critic-style), Task(/critic-logic), Task(/critic-security)
```

Output — `/critic-security` finds hardcoded path:
```json
{
  "critic_kind": "security",
  "severity": "high",
  "body": "Hardcoded interchange path 'tools/interchange/feature-flags-snapshot.json' without existence check; missing-file path returns silently.",
  "file_path": "Assets/Scripts/Core/FeatureFlags.cs",
  "line_range": "42-44"
}
```

Database state:
```sql
INSERT INTO ia_review_findings (plan_slug, critic_kind, severity, body, file_path, line_range)
VALUES ('vibe-coding-safety', 'security', 'high', '...', 'Assets/Scripts/Core/FeatureFlags.cs', '42-44');
```

Behavior:
```
ship-final detects severity=high → AskUserQuestion("Critic flagged 1 high-severity finding; override?")
  → operator picks "Override" → arch_changelog kind=critic_override → proceed to close
  → operator picks "Block + fix" → ship-final aborts close, returns findings to operator
```

Edge case — all 3 critics return zero findings:
```
no INSERT; ship-final proceeds to close without appeal poll.
```

### Review Notes

Subagent review pass — outcome PASS, no BLOCKING items. NON-BLOCKING items + suggestions recorded below verbatim per Phase 8 contract.

**Strongest design risk.** Wave D interchange-JSON hydration path is the most fragile seam: it crosses three subsystems (DB → web/CI export → Unity boot) and is silent on failure (missing file → empty cache → all flags FALSE). A flag that should default TRUE (e.g. a rolled-back feature gated by `enabled=TRUE`) will silently revert to OFF if the snapshot file is missing at boot. The `default_value` column mitigates partially but only if hydration successfully reads the row. Mitigation deferred: D.9 stage test must assert "snapshot missing → log warning + fall back to compiled defaults, not silent zero-fill".

**Strongest design strength.** Risk-first ship order (Wave A before all structural work) is well-chosen — hook denials are reversible (toggle a matcher line, env-var escape) and produce no DB rows or schema migrations. Even if Waves B/C/D/E are deferred indefinitely, Wave A alone closes the two highest-impact §Critique weaknesses (Verification block omission + test-deletion) at the agent-tool layer rather than at policy prose. The `ears_grandfathered` / `tdd_red_green_grandfathered` mirror pattern in Wave B is also load-bearing: it makes the rubric strictly additive against in-flight plans, removing the "rule 11 retroactively reds all open plans" failure mode.

**NON-BLOCKING items.**

1. Wave D D.4 (Awake boot hook) leaves the bootstrap MonoBehaviour unnamed. Pick the existing bootstrap entrypoint (likely a Service hub under `Domains/`) before implementation; do not introduce a new hub (per `feedback_unity_hub_no_rename_move_delete.md`).
2. Wave E E.5 (3 parallel critic dispatch) does not specify Agent tool concurrency limits. Anthropic API rate-limit risk at large plan close (50+ Stages cumulative diff). Add per-plan concurrency cap (e.g. 3 critics max, sequential if >3 plans run concurrently across sessions).
3. Wave B B.3 (`master_plan_spec_freeze` MCP) does not specify what happens on re-freeze of an already-frozen `(slug, version)` row. Decision: re-freeze creates a NEW version row (version++); existing frozen row stays archived. Document explicitly in MCP descriptor.
4. Wave C C.2 (exponential backoff helper) — sleep in skill body is awkward (skill is markdown). Implement as `tools/scripts/exponential-backoff.mjs` invoked from the verify-loop runtime, not inline in SKILL.md prose.
5. Wave A A.1 (Stop hook) — regex for "session touched Assets/**" depends on `$CLAUDE_SESSION_CONTEXT` schema. Validate against current Claude Code Stop-hook payload before authoring; fallback to `git diff HEAD` if context schema doesn't surface tool-call log.

**SUGGESTIONS.**

1. Consider adding a `validate:feature-flag-coverage` validator post-Wave D: every Stage row with `flag_slug NOT NULL` must have a matching `ia_feature_flags` row, and the `Assets/**` source must reference `FeatureFlags.IsEnabled("{flag_slug}")` somewhere. Catches drift between DB and code.
2. Consider promoting "Verification block" + "feature flag" + "stage closeout" + "verify loop" to glossary rows (currently missing per glossary lookup). Wave B/D/E author-time benefit.
3. Wave E critic subagents could share a `_preamble/critic-base.md` skill body fragment to keep the 3 SKILL.mds DRY on shared input shape (diff + glossary + invariants).
4. Wave A Stop hook could optionally enforce a "minimum 1 Verification row run" rather than just block-presence; defer to follow-up.
5. The `severity=high` blocking gate in Wave E does not distinguish between false-positive-prone critics and high-precision critics. Consider per-`critic_kind` weighting (security findings always block; style findings block only on `severity=high AND signal_confidence > 0.7`). Defer to post-ship calibration.

### Expansion metadata

- Date ISO: 2026-05-15
- Model: claude-opus-4-7[1m]
- Approach selected: 7-proposal bundle (Waves A→B→C→D→E); proposal #6 dropped (parallel-carcass overlap); critics scoped to `/ship-final` Pass B only; feature flags = DB-primary with interchange JSON boot hydration.
- Blocking items resolved: 0 (subagent review returned PASS; 5 NON-BLOCKING + 5 SUGGESTIONS carried).
