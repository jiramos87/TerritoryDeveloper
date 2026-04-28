/**
 * Publish-lint rule registry (TECH-1959 / Stage 9.1).
 *
 * Backs DEC-A30 publish lint framework. Loads enabled rule rows for a given
 * `kind` from `publish_lint_rule` and exposes typed lookup helpers for
 * `auditAudioLoudness` + the publish dialog. Rules ship hard-coded in TS
 * modules; the DB table carries severity + `config_json` so admins can tune
 * thresholds without code change.
 *
 * @see web/lib/lint/audio-loudness.ts — audit module (kind=audio)
 * @see db/migrations/0033_publish_lint_rule_audio_seed.sql — seed source
 */

import type { Sql } from "postgres";

import { getSql } from "@/lib/db/client";
import {
  DEFAULT_AUDIO_LOUDNESS_CONFIG,
  type AudioLoudnessConfig,
  type LintSeverity,
} from "@/lib/lint/types";

export type PublishLintRuleRow = {
  rule_id: string;
  kind: string;
  severity: LintSeverity;
  enabled: boolean;
  config_json: Record<string, unknown>;
};

/**
 * Load all enabled lint rules for `kind`. Empty array when the table has no
 * rows for that kind (e.g. before the migration runs). Convenience overload
 * that uses the lazy singleton `getSql()` — for tx-bound paths use
 * `loadEnabledLintRulesWithSql(kind, sql)` to thread the tx connection.
 */
export async function loadEnabledLintRules(
  kind: string,
): Promise<PublishLintRuleRow[]> {
  return loadEnabledLintRulesWithSql(kind, getSql());
}

/**
 * Load all enabled lint rules for `kind` using a caller-supplied `Sql`
 * (e.g. one bound to a `withAudit` transaction). Same row shape as the
 * singleton variant.
 */
export async function loadEnabledLintRulesWithSql(
  kind: string,
  sql: Sql,
): Promise<PublishLintRuleRow[]> {
  const rows = await sql<PublishLintRuleRow[]>`
    select rule_id, kind, severity, enabled, config_json
    from publish_lint_rule
    where kind = ${kind}
      and enabled = true
    order by rule_id asc
  `;
  return rows;
}

/**
 * Resolve the audio loudness config from DB rule rows (or fall through to
 * `DEFAULT_AUDIO_LOUDNESS_CONFIG` when rows are missing / partial). Lets
 * `auditAudioLoudness` accept a single typed config object rather than
 * re-parsing JSON at every call site.
 */
export function resolveAudioLoudnessConfig(
  rules: PublishLintRuleRow[],
): AudioLoudnessConfig {
  const out: AudioLoudnessConfig = { ...DEFAULT_AUDIO_LOUDNESS_CONFIG };
  for (const row of rules) {
    if (row.rule_id === "audio.loudness_out_of_range") {
      const cfg = row.config_json ?? {};
      const minVal = (cfg as { min_loudness_lufs?: unknown }).min_loudness_lufs;
      const maxVal = (cfg as { max_loudness_lufs?: unknown }).max_loudness_lufs;
      if (typeof minVal === "number") out.min_loudness_lufs = minVal;
      if (typeof maxVal === "number") out.max_loudness_lufs = maxVal;
    }
    if (row.rule_id === "audio.peak_clipping") {
      const cfg = row.config_json ?? {};
      const peakVal = (cfg as { max_peak_db?: unknown }).max_peak_db;
      if (typeof peakVal === "number") out.max_peak_db = peakVal;
    }
  }
  return out;
}
