/**
 * Strip RectTransform property overrides + repoint duplicate-slug PrefabInstances
 * in MainScene.unity for baked Generated UI prefabs.
 *
 * Why: scene PrefabInstances accumulated stale m_Modifications (Modal-default
 * 0.5/0.5/600x800 rect) from the pre-anti-loss bake era. These clobber prefab
 * anchors at instantiation, masking the correct layout-rects-derived rects.
 * Also: 5 panels (pause-menu, new-game-screen, save-load-screen, settings-screen,
 * onboarding-overlay) reference scene-name prefab variants whose bake skipped
 * layout-rects lookup (slug mismatch vs CD slug). Repoint to cd-slug variant.
 *
 * Run once: `npx tsx tools/scripts/strip-ui-prefab-instance-rect-overrides.ts`
 * Idempotent: safe to re-run.
 */
import { promises as fs } from "node:fs";
import path from "node:path";

const REPO_ROOT = path.resolve(__dirname, "..", "..");
const SCENE_PATH = path.join(REPO_ROOT, "Assets", "Scenes", "MainScene.unity");
const PREFAB_DIR = path.join(REPO_ROOT, "Assets", "UI", "Prefabs", "Generated");

// Property paths on RectTransform that must NOT carry scene overrides — prefab is truth.
const RECT_PROPERTY_PATHS = new Set<string>([
  "m_AnchorMin.x",
  "m_AnchorMin.y",
  "m_AnchorMax.x",
  "m_AnchorMax.y",
  "m_AnchoredPosition.x",
  "m_AnchoredPosition.y",
  "m_AnchoredPosition.z",
  "m_SizeDelta.x",
  "m_SizeDelta.y",
  "m_Pivot.x",
  "m_Pivot.y",
  "m_OffsetMin.x",
  "m_OffsetMin.y",
  "m_OffsetMax.x",
  "m_OffsetMax.y",
  "m_LocalPosition.x",
  "m_LocalPosition.y",
  "m_LocalPosition.z",
  "m_LocalRotation.w",
  "m_LocalRotation.x",
  "m_LocalRotation.y",
  "m_LocalRotation.z",
  "m_LocalScale.x",
  "m_LocalScale.y",
  "m_LocalScale.z",
  "m_LocalEulerAnglesHint.x",
  "m_LocalEulerAnglesHint.y",
  "m_LocalEulerAnglesHint.z",
]);

// scene-name guid → cd-slug guid (so PrefabInstance picks up prefab with layout-rects-derived anchors)
const SLUG_REPOINT_MAP: Record<string, { fromSlug: string; toSlug: string }> = {};

async function readGuid(metaPath: string): Promise<string | null> {
  try {
    const txt = await fs.readFile(metaPath, "utf8");
    const match = txt.match(/^guid:\s*([0-9a-f]+)/m);
    return match ? match[1] : null;
  } catch {
    return null;
  }
}

async function buildBakedGuidSet(): Promise<{ guids: Set<string>; slugByGuid: Map<string, string>; guidBySlug: Map<string, string> }> {
  const entries = await fs.readdir(PREFAB_DIR);
  const guids = new Set<string>();
  const slugByGuid = new Map<string, string>();
  const guidBySlug = new Map<string, string>();
  for (const entry of entries) {
    if (!entry.endsWith(".prefab.meta")) continue;
    const slug = entry.slice(0, -".prefab.meta".length);
    const guid = await readGuid(path.join(PREFAB_DIR, entry));
    if (!guid) continue;
    guids.add(guid);
    slugByGuid.set(guid, slug);
    guidBySlug.set(slug, guid);
  }
  return { guids, slugByGuid, guidBySlug };
}

function buildRepointMap(guidBySlug: Map<string, string>): Map<string, { toGuid: string; fromSlug: string; toSlug: string }> {
  const pairs: Array<[string, string]> = [
    ["pause-menu", "pause"],
    ["new-game-screen", "new-game"],
    ["save-load-screen", "save-load"],
    ["settings-screen", "settings"],
    ["onboarding-overlay", "onboarding"],
  ];
  const map = new Map<string, { toGuid: string; fromSlug: string; toSlug: string }>();
  for (const [fromSlug, toSlug] of pairs) {
    const fromGuid = guidBySlug.get(fromSlug);
    const toGuid = guidBySlug.get(toSlug);
    if (!fromGuid || !toGuid) {
      console.warn(`[skip] repoint ${fromSlug}→${toSlug}: missing guid (from=${fromGuid}, to=${toGuid})`);
      continue;
    }
    map.set(fromGuid, { toGuid, fromSlug, toSlug });
  }
  return map;
}

interface PrefabInstanceBlock {
  startLine: number; // 0-indexed line of `--- !u!1001 &xxxx`
  endLine: number; // 0-indexed line just before next `--- ` block (or EOF)
  modificationsStart: number; // 0-indexed line of `m_Modifications:`
  modificationsEnd: number; // 0-indexed line just before next sibling key (m_RemovedComponents, etc.)
  sourcePrefabGuid: string | null;
  sourcePrefabLine: number; // 0-indexed line of `m_SourcePrefab:` (for repoint)
}

function findPrefabInstanceBlocks(lines: string[]): PrefabInstanceBlock[] {
  const blocks: PrefabInstanceBlock[] = [];
  for (let i = 0; i < lines.length; i++) {
    if (!lines[i].startsWith("--- !u!1001 &")) continue;
    const startLine = i;
    let endLine = lines.length;
    for (let j = i + 1; j < lines.length; j++) {
      if (lines[j].startsWith("--- ")) {
        endLine = j;
        break;
      }
    }
    let modificationsStart = -1;
    let modificationsEnd = -1;
    let sourcePrefabGuid: string | null = null;
    let sourcePrefabLine = -1;
    for (let j = startLine + 1; j < endLine; j++) {
      const line = lines[j];
      if (line.match(/^\s{4}m_Modifications:\s*$/)) {
        modificationsStart = j;
        for (let k = j + 1; k < endLine; k++) {
          // End of sequence = next sibling key at same 4-space indent.
          // Sequence items start with `    -` (4 spaces + dash); siblings start with a letter.
          if (lines[k].match(/^\s{4}[A-Za-z]/)) {
            modificationsEnd = k;
            break;
          }
        }
        if (modificationsEnd === -1) modificationsEnd = endLine;
      }
      const sourceMatch = line.match(/^\s{2}m_SourcePrefab:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]+),\s*type:\s*\d+\}/);
      if (sourceMatch) {
        sourcePrefabGuid = sourceMatch[1];
        sourcePrefabLine = j;
      }
    }
    blocks.push({ startLine, endLine, modificationsStart, modificationsEnd, sourcePrefabGuid, sourcePrefabLine });
    i = endLine - 1;
  }
  return blocks;
}

function stripRectModifications(lines: string[], block: PrefabInstanceBlock): { stripped: number; newLines: string[] } {
  if (block.modificationsStart === -1 || block.modificationsEnd === -1) {
    return { stripped: 0, newLines: lines };
  }
  // m_Modifications block uses YAML sequence: each entry starts with `    - target:` and has 3 lines (target, propertyPath, value, objectReference).
  // Some entries may have 4 lines if value spans. We parse entries as 4-line groups starting with `    - target:`.
  const out: string[] = lines.slice(0, block.modificationsStart + 1);
  let i = block.modificationsStart + 1;
  let stripped = 0;
  while (i < block.modificationsEnd) {
    const line = lines[i];
    if (!line.match(/^\s{4}- target:/)) {
      // Not a list-entry head — copy as-is.
      out.push(line);
      i++;
      continue;
    }
    // Collect entry lines: head + continuation lines (indented further than `    -`).
    const entryStart = i;
    let entryEnd = i + 1;
    while (entryEnd < block.modificationsEnd) {
      const next = lines[entryEnd];
      if (next.match(/^\s{4}- target:/) || next.match(/^\s{4}\S/)) break;
      entryEnd++;
    }
    const entryLines = lines.slice(entryStart, entryEnd);
    const propLine = entryLines.find((l) => l.match(/^\s{6}propertyPath:/));
    const propMatch = propLine?.match(/propertyPath:\s*(\S+)/);
    const propPath = propMatch ? propMatch[1] : null;
    if (propPath && RECT_PROPERTY_PATHS.has(propPath)) {
      stripped++;
    } else {
      out.push(...entryLines);
    }
    i = entryEnd;
  }
  // Append the rest of the file beyond this block.
  out.push(...lines.slice(block.modificationsEnd));
  return { stripped, newLines: out };
}

function repointSourcePrefab(lines: string[], block: PrefabInstanceBlock, fromGuid: string, toGuid: string): string[] {
  const out = [...lines];
  // Repoint m_SourcePrefab line.
  if (block.sourcePrefabLine >= 0) {
    out[block.sourcePrefabLine] = out[block.sourcePrefabLine].replace(fromGuid, toGuid);
  }
  // Repoint every `target: {fileID: N, guid: <fromGuid>, type: 3}` and
  // `m_CorrespondingSourceObject: {fileID: N, guid: <fromGuid>, type: 3}` in the block.
  for (let i = block.startLine; i < block.endLine; i++) {
    if (out[i].includes(fromGuid)) {
      out[i] = out[i].replaceAll(fromGuid, toGuid);
    }
  }
  // Also repoint stripped/m_CorrespondingSourceObject entries that follow the PrefabInstance block
  // and reference the prefab guid (separate top-level YAML docs declared near the PrefabInstance).
  // Keep within whole-file scope but only update lines that are within ±200 lines of block.endLine
  // AND contain the guid (defensive — Unity scene refs are local to PrefabInstance).
  const scanStart = block.endLine;
  const scanEnd = Math.min(out.length, block.endLine + 1500);
  let stop = false;
  for (let i = scanStart; i < scanEnd && !stop; i++) {
    if (out[i].startsWith("--- !u!1001 &")) {
      // Hit the next PrefabInstance — stop re-pointing for this block.
      stop = true;
      break;
    }
    if (out[i].includes(fromGuid)) {
      out[i] = out[i].replaceAll(fromGuid, toGuid);
    }
  }
  return out;
}

async function main() {
  const { guids: bakedGuids, guidBySlug, slugByGuid } = await buildBakedGuidSet();
  const repointMap = buildRepointMap(guidBySlug);

  let txt = await fs.readFile(SCENE_PATH, "utf8");
  let lines = txt.split("\n");

  let totalStripped = 0;
  let totalRepointed = 0;
  let blocksTouched = 0;

  // Repass loop because line numbers shift as we strip; rebuild block table per pass until clean.
  let pass = 0;
  while (pass < 25) {
    pass++;
    const blocks = findPrefabInstanceBlocks(lines);
    let changedThisPass = false;

    for (const block of blocks) {
      if (!block.sourcePrefabGuid) continue;
      const isBakedTarget = bakedGuids.has(block.sourcePrefabGuid);
      const repoint = repointMap.get(block.sourcePrefabGuid);
      if (!isBakedTarget && !repoint) continue;

      // Repoint first (changes guids in m_Modifications target lines too).
      if (repoint) {
        const before = lines.length;
        lines = repointSourcePrefab(lines, block, block.sourcePrefabGuid, repoint.toGuid);
        if (before !== lines.length || true) {
          totalRepointed++;
          changedThisPass = true;
          console.log(`[repoint] ${repoint.fromSlug} → ${repoint.toSlug} (PrefabInstance @ scene line ${block.startLine + 1})`);
          break; // line indices possibly invalid after repoint; rebuild
        }
      }

      // Strip rect overrides.
      const result = stripRectModifications(lines, block);
      if (result.stripped > 0) {
        lines = result.newLines;
        totalStripped += result.stripped;
        blocksTouched++;
        changedThisPass = true;
        const slug = slugByGuid.get(block.sourcePrefabGuid) ?? "(unknown)";
        console.log(`[strip ] ${slug.padEnd(22)} stripped=${result.stripped} (PrefabInstance @ scene line ${block.startLine + 1})`);
        break; // rebuild block table — line indices shifted
      }
    }

    if (!changedThisPass) break;
  }

  if (pass >= 25) {
    console.error(`[error] strip pass cap (25) reached; bailing`);
    process.exit(1);
  }

  if (totalStripped > 0 || totalRepointed > 0) {
    await fs.writeFile(SCENE_PATH, lines.join("\n"), "utf8");
  }

  console.log("");
  console.log(`Done. Passes: ${pass}. Strips: ${totalStripped} (across ${blocksTouched} PrefabInstance blocks). Repoints: ${totalRepointed}.`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
