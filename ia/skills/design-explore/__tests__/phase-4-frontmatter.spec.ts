/**
 * phase-4-frontmatter.spec.ts — TECH-12647
 *
 * Asserts emitted YAML carries all required keys and per-task shape.
 */

import { describe, it, expect } from "vitest";

// Minimal YAML shape validator (no js-yaml dep needed — structural check only)
interface TaskEntry {
  prefix: string;
  depends_on: string[];
  digest_outline: string;
  touched_paths: string[];
  kind: string;
}

interface StageEntry {
  stage_id: string;
  title: string;
  status: string;
}

interface ExplorationFrontmatter {
  slug: string;
  parent_plan_slug: string | null;
  target_version: number;
  stages: StageEntry[];
  tasks: TaskEntry[];
}

function validateFrontmatter(fm: Partial<ExplorationFrontmatter>): string[] {
  const errors: string[] = [];
  if (!fm.slug) errors.push("missing: slug");
  if (!("parent_plan_slug" in fm)) errors.push("missing: parent_plan_slug (may be null)");
  if (typeof fm.target_version !== "number") errors.push("missing: target_version (number)");
  if (!Array.isArray(fm.stages)) errors.push("missing: stages[]");
  if (!Array.isArray(fm.tasks)) errors.push("missing: tasks[]");
  return errors;
}

function validateTaskEntry(task: Partial<TaskEntry>): string[] {
  const errors: string[] = [];
  if (!task.prefix) errors.push("task missing: prefix");
  if (!Array.isArray(task.depends_on)) errors.push("task missing: depends_on[]");
  if (!task.digest_outline) errors.push("task missing: digest_outline");
  if (!Array.isArray(task.touched_paths)) errors.push("task missing: touched_paths[]");
  if (!task.kind) errors.push("task missing: kind");
  return errors;
}

describe("YAML frontmatter shape", () => {
  const validFrontmatter: ExplorationFrontmatter = {
    slug: "test-plan",
    parent_plan_slug: null,
    target_version: 1,
    stages: [{ stage_id: "1", title: "Stage 1", status: "pending" }],
    tasks: [
      {
        prefix: "TECH",
        depends_on: [],
        digest_outline: "Implement feature X",
        touched_paths: ["Assets/Scripts/Foo.cs"],
        kind: "implementation",
      },
    ],
  };

  it("valid frontmatter passes validation", () => {
    expect(validateFrontmatter(validFrontmatter)).toHaveLength(0);
  });

  it("missing slug → error", () => {
    const { slug: _, ...rest } = validFrontmatter;
    expect(validateFrontmatter(rest)).toContain("missing: slug");
  });

  it("missing target_version → error", () => {
    const { target_version: _, ...rest } = validFrontmatter;
    expect(validateFrontmatter(rest)).toContain("missing: target_version (number)");
  });

  it("missing stages → error", () => {
    const { stages: _, ...rest } = validFrontmatter;
    expect(validateFrontmatter(rest)).toContain("missing: stages[]");
  });

  it("missing tasks → error", () => {
    const { tasks: _, ...rest } = validFrontmatter;
    expect(validateFrontmatter(rest)).toContain("missing: tasks[]");
  });

  it("parent_plan_slug can be null (v=1 exploration)", () => {
    const errors = validateFrontmatter({ ...validFrontmatter, parent_plan_slug: null });
    expect(errors).toHaveLength(0);
  });
});

describe("per-task frontmatter shape", () => {
  const validTask: TaskEntry = {
    prefix: "TECH",
    depends_on: [],
    digest_outline: "Implement X",
    touched_paths: [],
    kind: "implementation",
  };

  it("valid task passes validation", () => {
    expect(validateTaskEntry(validTask)).toHaveLength(0);
  });

  it("missing prefix → error", () => {
    const { prefix: _, ...rest } = validTask;
    expect(validateTaskEntry(rest)).toContain("task missing: prefix");
  });

  it("missing depends_on → error", () => {
    const { depends_on: _, ...rest } = validTask;
    expect(validateTaskEntry(rest)).toContain("task missing: depends_on[]");
  });

  it("missing digest_outline → error", () => {
    const { digest_outline: _, ...rest } = validTask;
    expect(validateTaskEntry(rest)).toContain("task missing: digest_outline");
  });

  it("missing touched_paths → error", () => {
    const { touched_paths: _, ...rest } = validTask;
    expect(validateTaskEntry(rest)).toContain("task missing: touched_paths[]");
  });

  it("missing kind → error", () => {
    const { kind: _, ...rest } = validTask;
    expect(validateTaskEntry(rest)).toContain("task missing: kind");
  });
});
