/**
 * Layer 1 lint runner — per-entity rule execution (TECH-2568 / Stage 12.1).
 *
 * Orchestrates per-rule audit fns across the 8 catalog kinds
 * (`sprite | asset | button | panel | pool | token | archetype | audio`).
 * Reads enabled rules via `loadEnabledLintRules(kind)` (Stage 9.1) and
 * dispatches each `rule_id` to its audit fn. Audio rules delegate to
 * `auditAudioLoudness` (Stage 9.1); non-audio kinds ship stub audit fns
 * returning `[]` per DEC-A30 §Layer 1 ("hard gates ship first, soft lints
 * accrue"). Unknown `rule_id` values surface as `info` rows so DB seeds can
 * outpace code without breaking publish.
 *
 * @see ia/projects/asset-pipeline/stage-12.1 — TECH-2568 §Plan Digest
 * @see web/lib/lint/audio-loudness.ts — audio audit fn delegation target
 * @see db/migrations/0039_publish_lint_rule_non_audio_seed.sql — non-audio seed
 */

import type { Sql } from "postgres";

import { auditAudioLoudness } from "@/lib/lint/audio-loudness";
import {
  loadEnabledLintRulesWithSql,
  resolveAudioLoudnessConfig,
  type PublishLintRuleRow,
} from "@/lib/lint/registry";
import type { LintResult } from "@/lib/lint/types";

/**
 * Audio loudness + peak clipping share a single dispatcher because both
 * read the same `audio_detail` row. Returns the audio results once per
 * runner pass — caller dedupes by tracking `audioDispatched`. Per-rule
 * audit fns are async to allow DB reads (audio reads `audio_detail`).
 */
async function auditAudioRow(
  entityId: string,
  _versionId: string,
  rules: PublishLintRuleRow[],
  sql: Sql,
): Promise<LintResult[]> {
  type AudioDetailRow = {
    loudness_lufs: number | null;
    peak_db: number | null;
  };
  // `audio_detail` is keyed by `entity_id` (1:1 with `catalog_entity` per
  // DEC-A8); there is no `version_id` column. Per-entity loudness/peak
  // measurements are shared across versions until per-version detail is
  // introduced.
  const rows = await sql<AudioDetailRow[]>`
    select loudness_lufs, peak_db
    from audio_detail
    where entity_id = ${entityId}
    limit 1
  `;
  if (rows.length === 0) return [];
  const config = resolveAudioLoudnessConfig(rules);
  return auditAudioLoudness(
    {
      loudness_lufs: rows[0].loudness_lufs,
      peak_db: rows[0].peak_db,
    },
    config,
  );
}

/**
 * Non-audio rule_id → stub audit fn returning `[]`. Real audit logic lands
 * in later stages per DEC-A30 §Layer 1; keeping the dispatcher entries here
 * makes the registry contract explicit + avoids "unknown rule" info rows
 * for seeded ids.
 */
const NON_AUDIO_STUB_RULE_IDS: ReadonlySet<string> = new Set([
  "sprite.missing_ppu",
  "sprite.missing_pivot",
  "asset.no_sprite_bound",
  "button.missing_icon",
  "button.missing_label",
  "panel.empty_slot_below_min",
  "panel.unfilled_required_slot",
  "pool.empty",
  "pool.no_primary_subtype",
  "token.no_consumers",
  "archetype.unpinned_dependency",
]);

const AUDIO_RULE_IDS: ReadonlySet<string> = new Set([
  "audio.loudness_out_of_range",
  "audio.peak_clipping",
]);

/**
 * Run Layer 1 (per-entity) lint rules for `kind`. Loads enabled rules from
 * `publish_lint_rule`, dispatches each to its audit fn, returns aggregated
 * `LintResult[]`. Audio rules dispatch once (shared `audio_detail` read);
 * non-audio rules dispatch to stubs returning `[]`; unknown rule_ids
 * surface as `info` rows.
 *
 * @param kind        Catalog kind (8-value union).
 * @param entityId    `catalog_entity.id` as string.
 * @param versionId   `entity_version.id` as string.
 * @param sql         Tx-bound `Sql` instance (from `withAudit` wrapper or
 *                    direct `getSql()` outside a transaction).
 */
export async function runLayer1(
  kind: string,
  entityId: string,
  versionId: string,
  sql: Sql,
): Promise<LintResult[]> {
  const rules = await loadEnabledLintRulesWithSql(kind, sql);
  if (rules.length === 0) return [];

  const out: LintResult[] = [];
  let audioDispatched = false;

  for (const rule of rules) {
    if (AUDIO_RULE_IDS.has(rule.rule_id)) {
      if (audioDispatched) continue;
      audioDispatched = true;
      const audioRules = rules.filter((r) => AUDIO_RULE_IDS.has(r.rule_id));
      const results = await auditAudioRow(entityId, versionId, audioRules, sql);
      out.push(...results);
      continue;
    }
    if (NON_AUDIO_STUB_RULE_IDS.has(rule.rule_id)) {
      // Stub audit fn — real logic lands in later stages per DEC-A30.
      continue;
    }
    // Unknown rule_id — DB seed ahead of code. Forward-compat info row.
    out.push({
      rule_id: rule.rule_id,
      severity: "info",
      message: "unknown rule registered; runner no-op",
    });
  }

  return out;
}
