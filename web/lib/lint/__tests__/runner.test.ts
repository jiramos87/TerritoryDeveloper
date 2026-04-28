/**
 * Layer 1 lint runner unit tests (TECH-2568 / Stage 12.1).
 *
 * Pure unit coverage with a stubbed `Sql` template fn — no DB required.
 * Asserts dispatcher behavior:
 *   - audio kind delegates to `auditAudioLoudness` (block on out-of-range LUFS).
 *   - non-audio stub rule_ids return `[]`.
 *   - unknown rule_id surfaces an `info` row (forward-compat).
 *
 * @see ia/projects/asset-pipeline/stage-12.1 — TECH-2568 §Test Blueprint
 */

import { describe, expect, it } from "vitest";

import type { Sql } from "postgres";

import { runLayer1 } from "@/lib/lint/runner";
import type { PublishLintRuleRow } from "@/lib/lint/registry";

type AudioRow = { loudness_lufs: number | null; peak_db: number | null };

/**
 * Build a minimal `Sql`-compatible template fn that returns canned responses.
 * Inspects the first SQL fragment to route between `publish_lint_rule` and
 * `audio_detail` queries.
 */
function buildSqlStub(opts: {
  rules: PublishLintRuleRow[];
  audioRow?: AudioRow;
}): Sql {
  const fn = (strings: TemplateStringsArray) => {
    const text = strings.join("?");
    if (text.includes("publish_lint_rule")) {
      return Promise.resolve(opts.rules);
    }
    if (text.includes("audio_detail")) {
      return Promise.resolve(opts.audioRow ? [opts.audioRow] : []);
    }
    return Promise.resolve([]);
  };
  return fn as unknown as Sql;
}

describe("runLayer1 — TECH-2568 §Test Blueprint", () => {
  it("audio kind path — clean LUFS + peak returns []", async () => {
    const sql = buildSqlStub({
      rules: [
        {
          rule_id: "audio.loudness_out_of_range",
          kind: "audio",
          severity: "block",
          enabled: true,
          config_json: { min_loudness_lufs: -23, max_loudness_lufs: -10 },
        },
        {
          rule_id: "audio.peak_clipping",
          kind: "audio",
          severity: "block",
          enabled: true,
          config_json: { max_peak_db: -1.0 },
        },
      ],
      audioRow: { loudness_lufs: -16, peak_db: -3 },
    });
    const out = await runLayer1("audio", "1", "1", sql);
    expect(out).toEqual([]);
  });

  it("audio kind path — out-of-window LUFS returns 1 block row", async () => {
    const sql = buildSqlStub({
      rules: [
        {
          rule_id: "audio.loudness_out_of_range",
          kind: "audio",
          severity: "block",
          enabled: true,
          config_json: { min_loudness_lufs: -23, max_loudness_lufs: -10 },
        },
      ],
      audioRow: { loudness_lufs: -30, peak_db: -6 },
    });
    const out = await runLayer1("audio", "1", "1", sql);
    expect(out.length).toBe(1);
    expect(out[0].rule_id).toBe("audio.loudness_out_of_range");
    expect(out[0].severity).toBe("block");
  });

  it("non-audio kind path — sprite stubs return empty array", async () => {
    const sql = buildSqlStub({
      rules: [
        {
          rule_id: "sprite.missing_ppu",
          kind: "sprite",
          severity: "warn",
          enabled: true,
          config_json: {},
        },
        {
          rule_id: "sprite.missing_pivot",
          kind: "sprite",
          severity: "warn",
          enabled: true,
          config_json: {},
        },
      ],
    });
    const out = await runLayer1("sprite", "1", "1", sql);
    expect(out).toEqual([]);
  });

  it("unknown rule_id surfaces info row (forward-compat)", async () => {
    const sql = buildSqlStub({
      rules: [
        {
          rule_id: "sprite.fictional",
          kind: "sprite",
          severity: "warn",
          enabled: true,
          config_json: {},
        },
      ],
    });
    const out = await runLayer1("sprite", "1", "1", sql);
    expect(out.length).toBe(1);
    expect(out[0].rule_id).toBe("sprite.fictional");
    expect(out[0].severity).toBe("info");
    expect(out[0].message).toMatch(/unknown rule/i);
  });

  it("empty rules returns empty array", async () => {
    const sql = buildSqlStub({ rules: [] });
    const out = await runLayer1("token", "1", "1", sql);
    expect(out).toEqual([]);
  });

  it("audio rules dispatch once even when both rule_ids enabled", async () => {
    // Two audio rules in the registry — runner must read audio_detail once
    // and dedupe via audioDispatched flag. Out-of-window LUFS yields 1 row.
    const sql = buildSqlStub({
      rules: [
        {
          rule_id: "audio.loudness_out_of_range",
          kind: "audio",
          severity: "block",
          enabled: true,
          config_json: {},
        },
        {
          rule_id: "audio.peak_clipping",
          kind: "audio",
          severity: "block",
          enabled: true,
          config_json: {},
        },
      ],
      audioRow: { loudness_lufs: -30, peak_db: -6 },
    });
    const out = await runLayer1("audio", "1", "1", sql);
    expect(out.length).toBe(1);
  });
});
