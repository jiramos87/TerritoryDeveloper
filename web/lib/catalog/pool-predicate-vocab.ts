/**
 * Pool member predicate vocab (TECH-1788).
 *
 * Hard-coded canonical list driving the `<PoolConditionsEditor>` dropdown.
 * Kept here (not in glossary yet) per §Pending Decisions — future
 * canonicalization deferred.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1788 §Plan Digest
 */

export type PredicateVocabEntry = {
  /** Key used as a `conditions_json` object key. */
  key: string;
  /** UI label. */
  label: string;
  /** Value type — drives the input widget. */
  type: "int" | "string" | "tag_list";
  /** Optional helper text for the form. */
  hint?: string;
};

export const POOL_PREDICATE_VOCAB: ReadonlyArray<PredicateVocabEntry> = [
  { key: "min_growth_ring", label: "Min growth ring", type: "int", hint: "Inclusive lower bound" },
  { key: "max_growth_ring", label: "Max growth ring", type: "int", hint: "Inclusive upper bound" },
  { key: "biome", label: "Biome", type: "string" },
  { key: "tag_any", label: "Tag (any)", type: "tag_list", hint: "Comma-separated; matches if asset has ANY tag" },
  { key: "tag_all", label: "Tag (all)", type: "tag_list", hint: "Comma-separated; matches if asset has ALL tags" },
];

export const POOL_PREDICATE_KEYS = new Set(POOL_PREDICATE_VOCAB.map((e) => e.key));
