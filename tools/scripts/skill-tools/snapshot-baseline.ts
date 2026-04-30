/**
 * snapshot-baseline — captures Day-1 hash per recipified skill.
 *
 * Writes ia/state/skill-recipe-baselines/{slug}.json for each recipified skill
 * so validate-skill-recipe-drift avoids false positives on already-drifted skills.
 *
 * Flags:
 *   --update {slug}   Rewrite a single slug's baseline only.
 *
 * Idempotent: re-running with no changes produces byte-identical files (sorted keys + LF).
 * Exit 0 always (read/write tool, not validator).
 */

import process from "node:process";
import { listRecipifiedSlugs, buildBundle, writeBaseline } from "./recipe-step-extract.js";

async function run(): Promise<void> {
  const updateIdx = process.argv.indexOf("--update");
  const singleSlug = updateIdx !== -1 ? process.argv[updateIdx + 1] : undefined;

  const slugs = singleSlug ? [singleSlug] : listRecipifiedSlugs();

  for (const slug of slugs) {
    try {
      const bundle = buildBundle(slug);
      writeBaseline(slug, bundle);
      console.log(`  snapped ${slug}  hash=${bundle.hash}  steps=${bundle.recipe_step_ids.length}`);
    } catch (err) {
      console.warn(`  WARN ${slug}: ${(err as Error).message} — skipping`);
    }
  }

  console.log(`\nsnapshot-baseline: done (${slugs.length} slug(s))`);
}

run().catch((err) => {
  console.error(`[ERROR] snapshot-baseline: ${(err as Error).message}`);
  process.exit(2);
});
