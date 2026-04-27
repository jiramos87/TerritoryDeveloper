import { describe, it, expect } from "vitest";

import {
  auditAudioLoudness,
  hasBlockingAudioLint,
} from "@/lib/lint/audio-loudness";
import { resolveAudioLoudnessConfig } from "@/lib/lint/registry";
import type { PublishLintRuleRow } from "@/lib/lint/registry";
import { DEFAULT_AUDIO_LOUDNESS_CONFIG } from "@/lib/lint/types";

describe("auditAudioLoudness — TECH-1959 §Test Blueprint", () => {
  it("audio_loudness_in_range_passes — clean -16 LUFS / -3 dB peak", () => {
    const results = auditAudioLoudness({
      loudness_lufs: -16,
      peak_db: -3,
    });
    expect(results).toEqual([]);
    expect(hasBlockingAudioLint(results)).toBe(false);
  });

  it("audio_loudness_below_window_blocks — -30 LUFS triggers out_of_range", () => {
    const results = auditAudioLoudness({
      loudness_lufs: -30,
      peak_db: -6,
    });
    const rule = results.find(
      (r) => r.rule_id === "audio.loudness_out_of_range",
    );
    expect(rule).toBeDefined();
    expect(rule?.severity).toBe("block");
    expect(rule?.measured).toBe(-30);
  });

  it("audio_loudness_above_window_blocks — -5 LUFS triggers out_of_range", () => {
    const results = auditAudioLoudness({
      loudness_lufs: -5,
      peak_db: -6,
    });
    const rule = results.find(
      (r) => r.rule_id === "audio.loudness_out_of_range",
    );
    expect(rule).toBeDefined();
    expect(rule?.severity).toBe("block");
  });

  it("audio_peak_clipping_blocks — peak_db -0.5 triggers peak_clipping", () => {
    const results = auditAudioLoudness({
      loudness_lufs: -16,
      peak_db: -0.5,
    });
    const rule = results.find((r) => r.rule_id === "audio.peak_clipping");
    expect(rule).toBeDefined();
    expect(rule?.severity).toBe("block");
    expect(hasBlockingAudioLint(results)).toBe(true);
  });

  it("audio_peak_clipping_at_ceiling_passes — peak_db -1.0 exactly", () => {
    // max_peak_db = -1.0; rule fires only when peak_db > -1.0.
    const results = auditAudioLoudness({
      loudness_lufs: -16,
      peak_db: -1.0,
    });
    expect(results).toEqual([]);
  });

  it("audio_window_config_respected — override [-30, -8] honors -28 LUFS", () => {
    const results = auditAudioLoudness(
      { loudness_lufs: -28, peak_db: -3 },
      { min_loudness_lufs: -30, max_loudness_lufs: -8, max_peak_db: -1.0 },
    );
    expect(results).toEqual([]);
  });

  it("null loudness skips loudness rule (draft / pre-promote)", () => {
    const results = auditAudioLoudness({
      loudness_lufs: null,
      peak_db: -6,
    });
    expect(
      results.find((r) => r.rule_id === "audio.loudness_out_of_range"),
    ).toBeUndefined();
  });

  it("null peak skips peak rule (draft / pre-promote)", () => {
    const results = auditAudioLoudness({
      loudness_lufs: -16,
      peak_db: null,
    });
    expect(
      results.find((r) => r.rule_id === "audio.peak_clipping"),
    ).toBeUndefined();
  });

  it("non-finite measurements skip the rule (NaN / Infinity)", () => {
    const results = auditAudioLoudness({
      loudness_lufs: Number.NaN,
      peak_db: Number.POSITIVE_INFINITY,
    });
    expect(results).toEqual([]);
  });

  it("hasBlockingAudioLint — true when any block row present", () => {
    const results = auditAudioLoudness({
      loudness_lufs: -30,
      peak_db: -0.5,
    });
    expect(results.length).toBeGreaterThanOrEqual(1);
    expect(hasBlockingAudioLint(results)).toBe(true);
  });
});

describe("resolveAudioLoudnessConfig — DB row → typed config", () => {
  it("returns defaults when rules array empty", () => {
    const cfg = resolveAudioLoudnessConfig([]);
    expect(cfg).toEqual(DEFAULT_AUDIO_LOUDNESS_CONFIG);
  });

  it("applies window override from publish_lint_rule.config_json", () => {
    const rows: PublishLintRuleRow[] = [
      {
        rule_id: "audio.loudness_out_of_range",
        kind: "audio",
        severity: "block",
        enabled: true,
        config_json: { min_loudness_lufs: -30, max_loudness_lufs: -8 },
      },
    ];
    const cfg = resolveAudioLoudnessConfig(rows);
    expect(cfg.min_loudness_lufs).toBe(-30);
    expect(cfg.max_loudness_lufs).toBe(-8);
    // Peak default unchanged.
    expect(cfg.max_peak_db).toBe(DEFAULT_AUDIO_LOUDNESS_CONFIG.max_peak_db);
  });

  it("applies peak threshold override from peak_clipping rule", () => {
    const rows: PublishLintRuleRow[] = [
      {
        rule_id: "audio.peak_clipping",
        kind: "audio",
        severity: "block",
        enabled: true,
        config_json: { max_peak_db: -2.0 },
      },
    ];
    const cfg = resolveAudioLoudnessConfig(rows);
    expect(cfg.max_peak_db).toBe(-2.0);
  });

  it("ignores non-numeric config values silently (defensive)", () => {
    const rows: PublishLintRuleRow[] = [
      {
        rule_id: "audio.loudness_out_of_range",
        kind: "audio",
        severity: "block",
        enabled: true,
        config_json: { min_loudness_lufs: "not a number" },
      },
    ];
    const cfg = resolveAudioLoudnessConfig(rows);
    expect(cfg.min_loudness_lufs).toBe(
      DEFAULT_AUDIO_LOUDNESS_CONFIG.min_loudness_lufs,
    );
  });
});
