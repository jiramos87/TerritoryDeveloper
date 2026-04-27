import { describe, it, expect } from "vitest";

import {
  isDowngradeOrSame,
  validateUpgradeBody,
} from "@/app/api/catalog/entity-versions/upgrade/route";

describe("validateUpgradeBody", () => {
  it("accepts well-formed body", () => {
    const r = validateUpgradeBody({
      entity_version_id: "100",
      target_archetype_version_id: "44",
    });
    expect(r.ok).toBe(true);
    if (r.ok) {
      expect(r.entity_version_id).toBe("100");
      expect(r.target_archetype_version_id).toBe("44");
    }
  });

  it("rejects non-object body", () => {
    expect(validateUpgradeBody(null).ok).toBe(false);
    expect(validateUpgradeBody("nope").ok).toBe(false);
  });

  it("rejects missing entity_version_id", () => {
    const r = validateUpgradeBody({ target_archetype_version_id: "44" });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/entity_version_id/);
  });

  it("rejects non-numeric entity_version_id", () => {
    const r = validateUpgradeBody({
      entity_version_id: "abc",
      target_archetype_version_id: "44",
    });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/entity_version_id/);
  });

  it("rejects missing target_archetype_version_id", () => {
    const r = validateUpgradeBody({ entity_version_id: "100" });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/target_archetype_version_id/);
  });

  it("rejects non-numeric target_archetype_version_id", () => {
    const r = validateUpgradeBody({
      entity_version_id: "100",
      target_archetype_version_id: "v44",
    });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/target_archetype_version_id/);
  });
});

describe("isDowngradeOrSame", () => {
  it("blocks equal version numbers", () => {
    expect(isDowngradeOrSame(3, 3)).toBe(true);
  });

  it("blocks lower target", () => {
    expect(isDowngradeOrSame(5, 3)).toBe(true);
  });

  it("allows higher target", () => {
    expect(isDowngradeOrSame(3, 5)).toBe(false);
  });
});
