/**
 * Audio loudness + peak clipping lint rules (TECH-1959 / Stage 9.1).
 *
 * Two rules — both hard-gates per DEC-A31 audio carve-out (override of
 * DEC-A30 default `warn` severity for new rules):
 *   - `audio.loudness_out_of_range` — integrated LUFS outside the configured
 *     window blocks publish.
 *   - `audio.peak_clipping` — `peak_db` above `max_peak_db` blocks publish.
 *
 * Reads measurements from `audio_detail.loudness_lufs` + `audio_detail.peak_db`
 * (populated at render-time by TECH-1957 + re-measured at promote-time per
 * DEC-A31 follow-up). NULL measurements skip the rule (pre-promote draft).
 *
 * @see ia/projects/asset-pipeline/stage-9.1 — TECH-1959 §Plan Digest
 * @see web/lib/lint/types.ts — shared LintResult shape
 */

import {
  DEFAULT_AUDIO_LOUDNESS_CONFIG,
  type AudioLoudnessConfig,
  type LintResult,
} from "@/lib/lint/types";

export type AudioMeasurements = {
  loudness_lufs: number | null;
  peak_db: number | null;
};

/**
 * Run both audio lint rules against a measured audio entity. Returns an empty
 * array when measurements are clean OR when both measurements are NULL (entity
 * is still draft / pre-promote). NULL on a single field skips that rule only.
 */
export function auditAudioLoudness(
  measurements: AudioMeasurements,
  config: AudioLoudnessConfig = DEFAULT_AUDIO_LOUDNESS_CONFIG,
): LintResult[] {
  const results: LintResult[] = [];
  const { loudness_lufs, peak_db } = measurements;
  const { min_loudness_lufs, max_loudness_lufs, max_peak_db } = config;

  if (loudness_lufs !== null && Number.isFinite(loudness_lufs)) {
    if (
      loudness_lufs < min_loudness_lufs ||
      loudness_lufs > max_loudness_lufs
    ) {
      results.push({
        rule_id: "audio.loudness_out_of_range",
        severity: "block",
        message: `Integrated LUFS ${loudness_lufs.toFixed(2)} outside target window [${min_loudness_lufs}, ${max_loudness_lufs}].`,
        measured: loudness_lufs,
        threshold: `[${min_loudness_lufs}, ${max_loudness_lufs}] LUFS`,
      });
    }
  }

  if (peak_db !== null && Number.isFinite(peak_db)) {
    if (peak_db > max_peak_db) {
      results.push({
        rule_id: "audio.peak_clipping",
        severity: "block",
        message: `Peak ${peak_db.toFixed(2)} dB above ceiling ${max_peak_db} dB; reduce gain or apply limiter.`,
        measured: peak_db,
        threshold: `${max_peak_db} dB`,
      });
    }
  }

  return results;
}

/**
 * True when `auditAudioLoudness` returned at least one `block` row. Sugar
 * for the publish dialog and promote handler gate.
 */
export function hasBlockingAudioLint(results: LintResult[]): boolean {
  return results.some((r) => r.severity === "block");
}
