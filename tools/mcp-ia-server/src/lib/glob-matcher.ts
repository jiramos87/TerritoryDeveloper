/**
 * Pure-TS minimatch-style glob matcher (db-lifecycle-extensions Stage 3 /
 * TECH-3406).
 *
 * Supports:
 *   - `*`        — any chars except `/`
 *   - `**`       — any chars including `/`
 *   - `?`        — single char except `/`
 *   - `[abc]`    — character class
 *   - `[!abc]` / `[^abc]` — negated char class
 *
 * No npm dep — vendored to honor shop preference for small in-tree utilities.
 *
 * Compilation strategy: per-call regex compile (caching could be added if
 * profiler shows hot path; for `task_diff_anomaly_scan` Tasks-per-slug is
 * O(10²) which keeps regex compile cost below p99 budget).
 */

/**
 * Convert a minimatch-style glob to a RegExp anchored on full string.
 *
 * Internal — exported for unit tests.
 */
export function globToRegExp(glob: string): RegExp {
  let re = "";
  let i = 0;
  while (i < glob.length) {
    const c = glob[i]!;
    if (c === "*") {
      // Lookahead: `**` matches any path including `/`; single `*` excludes `/`.
      if (glob[i + 1] === "*") {
        re += ".*";
        i += 2;
        // Consume optional trailing `/` after `**` so `**/foo` ↔ `foo` is acceptable.
        if (glob[i] === "/") i++;
        continue;
      }
      re += "[^/]*";
      i++;
      continue;
    }
    if (c === "?") {
      re += "[^/]";
      i++;
      continue;
    }
    if (c === "[") {
      // Character class — copy verbatim with `!` → `^` translation.
      let j = i + 1;
      let cls = "[";
      if (glob[j] === "!" || glob[j] === "^") {
        cls += "^";
        j++;
      }
      while (j < glob.length && glob[j] !== "]") {
        cls += glob[j];
        j++;
      }
      if (j >= glob.length) {
        // Unclosed `[` — treat as literal.
        re += "\\[";
        i++;
        continue;
      }
      cls += "]";
      re += cls;
      i = j + 1;
      continue;
    }
    // Regex metacharacter escape.
    if (/[.+^${}()|\\]/.test(c)) {
      re += "\\" + c;
      i++;
      continue;
    }
    re += c;
    i++;
  }
  return new RegExp("^" + re + "$");
}

/**
 * True iff `path` matches `glob` per minimatch convention.
 */
export function matchGlob(path: string, glob: string): boolean {
  return globToRegExp(glob).test(path);
}

/**
 * True iff `path` matches at least one glob in `globs`. Empty array → false.
 */
export function matchesAny(path: string, globs: string[]): boolean {
  for (const g of globs) {
    if (matchGlob(path, g)) return true;
  }
  return false;
}
