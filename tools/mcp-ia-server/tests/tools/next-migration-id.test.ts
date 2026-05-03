/**
 * next_migration_id — scan a migrations dir, return next 4-digit ordinal.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { computeNextMigrationId } from "../../src/tools/next-migration-id.js";

function makeMigrationsDir(files: string[]): { repoRoot: string; cleanup: () => void } {
  const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), "next-migration-id-"));
  const dir = path.join(repoRoot, "db", "migrations");
  fs.mkdirSync(dir, { recursive: true });
  for (const f of files) {
    fs.writeFileSync(path.join(dir, f), "-- noop\n", "utf8");
  }
  return { repoRoot, cleanup: () => fs.rmSync(repoRoot, { recursive: true, force: true }) };
}

test("returns 0000 + null head on empty dir", () => {
  const { repoRoot, cleanup } = makeMigrationsDir([]);
  try {
    const out = computeNextMigrationId(repoRoot);
    assert.equal(out.next_id, "0000");
    assert.equal(out.next_id_int, 0);
    assert.equal(out.current_head, null);
    assert.equal(out.current_head_int, null);
    assert.equal(out.scanned_count, 0);
    assert.equal(out.migrations_dir, "db/migrations");
  } finally {
    cleanup();
  }
});

test("returns max + 1 zero-padded across realistic head", () => {
  const { repoRoot, cleanup } = makeMigrationsDir([
    "0001_init.sql",
    "0002_add_users.sql",
    "0054_parallel_carcass_runnable_prototype_signal.sql",
  ]);
  try {
    const out = computeNextMigrationId(repoRoot);
    assert.equal(out.next_id, "0055");
    assert.equal(out.next_id_int, 55);
    assert.equal(out.current_head, "0054_parallel_carcass_runnable_prototype_signal.sql");
    assert.equal(out.current_head_int, 54);
    assert.equal(out.scanned_count, 3);
  } finally {
    cleanup();
  }
});

test("ignores non-sql files + non-prefixed sql files + sub-directories", () => {
  const { repoRoot, cleanup } = makeMigrationsDir([
    "0001_init.sql",
    "README.md",
    "manual_fix.sql", // no 4-digit prefix
    "0010_real.sql",
  ]);
  // add a sub-directory to ensure it's skipped
  fs.mkdirSync(path.join(repoRoot, "db", "migrations", "staged"), { recursive: true });
  fs.writeFileSync(
    path.join(repoRoot, "db", "migrations", "staged", "0099_should_be_ignored.sql"),
    "-- noop\n",
    "utf8",
  );
  try {
    const out = computeNextMigrationId(repoRoot);
    assert.equal(out.next_id, "0011");
    assert.equal(out.scanned_count, 2);
    assert.equal(out.current_head, "0010_real.sql");
  } finally {
    cleanup();
  }
});

test("respects custom migrations_dir override", () => {
  const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), "next-migration-id-"));
  const dir = path.join(repoRoot, "alt", "migs");
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(path.join(dir, "0042_alt.sql"), "-- noop\n", "utf8");
  try {
    const out = computeNextMigrationId(repoRoot, "alt/migs");
    assert.equal(out.next_id, "0043");
    assert.equal(out.migrations_dir, "alt/migs");
  } finally {
    fs.rmSync(repoRoot, { recursive: true, force: true });
  }
});

test("throws invalid_input when dir is missing", () => {
  const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), "next-migration-id-"));
  try {
    assert.throws(
      () => computeNextMigrationId(repoRoot),
      (err: { code?: string; message?: string }) => {
        assert.equal(err.code, "invalid_input");
        assert.match(err.message ?? "", /not found/);
        return true;
      },
    );
  } finally {
    fs.rmSync(repoRoot, { recursive: true, force: true });
  }
});

test("throws invalid_input when path is a file, not dir", () => {
  const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), "next-migration-id-"));
  fs.mkdirSync(path.join(repoRoot, "db"), { recursive: true });
  fs.writeFileSync(path.join(repoRoot, "db", "migrations"), "not a dir", "utf8");
  try {
    assert.throws(
      () => computeNextMigrationId(repoRoot),
      (err: { code?: string }) => {
        assert.equal(err.code, "invalid_input");
        return true;
      },
    );
  } finally {
    fs.rmSync(repoRoot, { recursive: true, force: true });
  }
});
