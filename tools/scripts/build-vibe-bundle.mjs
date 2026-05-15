// Compose master_plan_bundle_apply payload for vibe-coding-safety.
// Reads handoff yaml from docs/explorations/vibe-coding-safety.md frontmatter,
// emits bundle.json under /tmp.

import { readFileSync, writeFileSync } from "node:fs";
import yaml from "js-yaml";

const repoRoot = "/Users/javier/bacayo-studio/territory-developer";
const src = readFileSync(`${repoRoot}/docs/explorations/vibe-coding-safety.md`, "utf8");
const m = src.match(/^---\n([\s\S]*?)\n---\n/);
if (!m) throw new Error("no yaml frontmatter");
const front = yaml.load(m[1]);

const slug = front.slug;
const version = front.target_version ?? 1;

const skipClause = "_skipped — source absent_";

function emitDigest(task, stage) {
  const goal = (task.digest_outline ?? "").trim();
  const rp = stage.red_stage_proof_block ?? {};
  const anchor = rp.red_test_anchor ?? "design_only";
  const targetKind = rp.target_kind ?? "design_only";
  const proofId = rp.proof_artifact_id ?? "—";
  const proofStatus = rp.proof_status ?? "not_applicable";
  const paths = (task.touched_paths ?? []).map((p) => `- ${p}`).join("\n") || "- (none — meta task)";
  const deps = (task.depends_on ?? []).join(", ") || "(none)";

  // task_key format: T{stage_id}.{seq_within_stage} — but handoff carries `id` like "1.0.1".
  // Strip the leading stage prefix to produce a clean numeric step. We use the full id as task_key.
  const taskKey = `T${task.id}`;

  return `<!-- task_key: ${taskKey} -->
# ${taskKey} — ${task.title}

## §Goal

${goal}

Depends on: ${deps}

## §Red-Stage Proof

- red_test_anchor: \`${anchor}\`
- target_kind: \`${targetKind}\`
- proof_artifact_id: \`${proofId}\`
- proof_status: \`${proofStatus}\`

Stage-scoped proof anchor inherited from Stage ${stage.id}; this Task contributes one or more red assertions to that test file until Stage close turns it green.

## §Work Items

Touched paths (preview):
${paths}

Kind: \`${task.kind ?? "code"}\`. Stage ${stage.id} red→green protocol: this Task extends the Stage test file with assertions tied to its surface; Stage closes when the file is fully green.

## §Visual Mockup

${skipClause}

## §Before / After

${skipClause}

## §Edge Cases

${skipClause}

## §Glossary Anchors

${skipClause}

## §Failure Modes

${skipClause}

## §Decision Dependencies

${skipClause}

## §Shared Seams

${skipClause}

## §Touched Paths Preview

${paths}
`;
}

const stages = [];
const tasks = [];

for (const stage of front.stages) {
  const stageId = `stage-${stage.id.replace(".", "-")}`;
  stages.push({
    stage_id: stageId,
    title: stage.title,
    exit_criteria: stage.exit ?? "",
    objective: "",
    red_stage_proof_block: stage.red_stage_proof_block ?? null,
    // body NOT supplied — SQL fn renders from red_stage_proof_block per mig 0136
  });

  for (const t of stage.tasks ?? []) {
    tasks.push({
      stage_id: stageId,
      prefix: t.prefix ?? "TECH",
      title: t.title,
      task_key: `T${t.id}`,
      digest_body: emitDigest(t, stage),
      type: t.kind ?? "code",
    });
  }
}

const bundle = {
  plan: {
    slug,
    version,
    parent_plan_slug: front.parent_plan_id ?? null,
    title: "Vibe-coding safety — 7-proposal bundle",
    description:
      "Hook-layer tracer + EARS rubric + adaptive verify-loop iterations + feature flag DB + multi-agent critic pipeline. Ship order A→B→C→D→E.",
    preamble: "",
  },
  stages,
  tasks,
};

writeFileSync("/tmp/vibe-bundle.json", JSON.stringify(bundle, null, 2));
console.log(`bundle written: stages=${stages.length} tasks=${tasks.length}`);
