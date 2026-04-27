/**
 * Catalog publish-lint shared types (TECH-1959 / Stage 9.1).
 *
 * DEC-A30 publish lint framework: each rule reads `entity` + `version` and
 * returns zero or more `LintResult` rows. The publish dialog groups results
 * by `severity` and disables submit when any `block` row is present.
 *
 * @see ia/projects/asset-pipeline/stage-9.1 — TECH-1959 §Plan Digest
 * @see docs/asset-pipeline-architecture.md DEC-A30
 */

export type LintSeverity = "block" | "warn" | "info";

export type LintResult = {
  /** Stable rule id (e.g. `audio.loudness_out_of_range`). */
  rule_id: string;
  /** Severity at config-time; hard gates when `block`. */
  severity: LintSeverity;
  /** Caveman summary surfaced in the publish dialog row. */
  message: string;
  /** Optional measured value to render alongside the failure. */
  measured?: string | number | null;
  /** Optional human-readable threshold or window for the row. */
  threshold?: string | number | null;
};

/**
 * Loudness window + peak threshold sourced from `publish_lint_rule.config_json`.
 * Defaults match the DEC-A31 audio carve-out / migration `0033`.
 */
export type AudioLoudnessConfig = {
  min_loudness_lufs: number;
  max_loudness_lufs: number;
  max_peak_db: number;
};

export const DEFAULT_AUDIO_LOUDNESS_CONFIG: AudioLoudnessConfig = {
  min_loudness_lufs: -23,
  max_loudness_lufs: -10,
  max_peak_db: -1.0,
};
