/**
 * validate:skill-recipe-drift — parity gate between recipified skill surfaces and recipe YAMLs.
 *
 * For each recipified skill (tools/recipes/{slug}.yaml exists):
 *   - Loads baseline from ia/state/skill-recipe-baselines/{slug}.json (produced by snapshot-baseline).
 *   - If baseline exists and hash matches → no drift.
 *   - If baseline exists and hash differs → drift: emit missing_in_skill / missing_in_agent.
 *   - If no baseline → skip with warning (run snapshot-baseline first).
 *
 * Exit 0: no drift across all recipified skills.
 * Exit 1: drift detected on one or more skills.
 * Exit 2: schema/IO error.
 *
 * Flags:
 *   --json   Print full JSON report (one entry per slug) instead of compact summary.
 */

import process from "node:process";
import {
  listRecipifiedSlugs,
  buildBundle,
  loadBaseline,
} from "./recipe-step-extract.js";

interface DriftEntry {
  slug: string;
  status: "ok" | "drift" | "no_baseline";
  recipe_steps: string[];
  skill_steps: string[];
  agent_steps: string[];
  missing_in_skill: string[];
  missing_in_agent: string[];
  hash_current: string;
  hash_baseline: string | null;
}

async function validate(): Promise<{ ok: boolean; drifts: DriftEntry[] }> {
  const slugs = listRecipifiedSlugs();
  const drifts: DriftEntry[] = [];

  for (const slug of slugs) {
    let bundle;
    try {
      bundle = buildBundle(slug);
    } catch (err) {
      console.error(`[ERROR] ${slug}: failed to extract bundle — ${(err as Error).message}`);
      process.exit(2);
    }

    const baseline = loadBaseline(slug);
    const entry: DriftEntry = {
      slug,
      status: "ok",
      recipe_steps: bundle.recipe_step_ids,
      skill_steps: bundle.skill_phase_ids,
      agent_steps: bundle.agent_step_refs,
      missing_in_skill: [],
      missing_in_agent: [],
      hash_current: bundle.hash,
      hash_baseline: baseline?.hash ?? null,
    };

    if (!baseline) {
      entry.status = "no_baseline";
    } else if (bundle.hash !== baseline.hash) {
      // Compute what changed relative to baseline.
      const baselineStepSet = new Set(baseline.recipe_step_ids);
      const currentStepSet = new Set(bundle.recipe_step_ids);

      // Steps added in recipe but not reflected in skill phases (by phase count)
      const phaseCount = bundle.skill_phase_ids.length;
      const stepCount = bundle.recipe_step_ids.length;
      if (phaseCount < stepCount) {
        const addedSteps = bundle.recipe_step_ids.filter((id) => !baselineStepSet.has(id));
        entry.missing_in_skill = addedSteps;
      }

      // Steps added in recipe but not referenced in agent body
      const agentRefSet = new Set(bundle.agent_step_refs);
      entry.missing_in_agent = bundle.recipe_step_ids.filter(
        (id) => !baselineStepSet.has(id) && !agentRefSet.has(id),
      );

      // If nothing specific identified but hash still differs, flag all new recipe steps
      if (entry.missing_in_skill.length === 0 && entry.missing_in_agent.length === 0) {
        const removedFromRecipe = [...baselineStepSet].filter((id) => !currentStepSet.has(id));
        const addedToRecipe = [...currentStepSet].filter((id) => !baselineStepSet.has(id));
        if (removedFromRecipe.length > 0 || addedToRecipe.length > 0) {
          entry.missing_in_skill = addedToRecipe;
          entry.missing_in_agent = addedToRecipe;
        }
      }

      entry.status = "drift";
    }

    drifts.push(entry);
  }

  const hasDrift = drifts.some((e) => e.status === "drift");
  return { ok: !hasDrift, drifts };
}

const jsonMode = process.argv.includes("--json");

validate()
  .then(({ ok, drifts }) => {
    if (jsonMode) {
      console.log(JSON.stringify(drifts, null, 2));
    } else {
      let totalDrift = 0;
      for (const e of drifts) {
        if (e.status === "ok") {
          console.log(`  ok   ${e.slug}`);
        } else if (e.status === "no_baseline") {
          console.log(`  skip ${e.slug} (no baseline — run snapshot-baseline first)`);
        } else {
          totalDrift++;
          console.log(
            `  DRIFT ${e.slug}` +
              (e.missing_in_skill.length ? `  missing_in_skill=${e.missing_in_skill.join(",")}` : "") +
              (e.missing_in_agent.length ? `  missing_in_agent=${e.missing_in_agent.join(",")}` : ""),
          );
        }
      }
      console.log(`\nskill-recipe-drift: ${totalDrift} drift(s) across ${drifts.length} recipified skill(s)`);
    }
    process.exit(ok ? 0 : 1);
  })
  .catch((err) => {
    console.error(`[ERROR] validate-skill-recipe-drift: ${(err as Error).message}`);
    process.exit(2);
  });
