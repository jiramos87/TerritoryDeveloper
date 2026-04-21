#!/usr/bin/env bash
# Run full web validation (lint + typecheck + tests) only when web/ is touched in the
# *current working tree or index* (unstaged or staged), or CI / explicit overrides force it.
# Does NOT scan branch diff vs main — parallel agents on one branch would false-trigger.
# Otherwise: npm run progress (master plans → docs/progress.html).
#
# Override: VALIDATE_WEB_FULL=1 or FORCE_VALIDATE_WEB=1 → always npm run validate:web
# CI=true (GitHub Actions, etc.) → always full web validation
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
cd "${REPO_ROOT}"

web_path_touched() {
  local f
  while IFS= read -r f; do
    [[ -z "${f}" ]] && continue
    if [[ "${f}" == web/* ]] || [[ "${f}" == web ]]; then
      return 0
    fi
  done
  return 1
}

collect_paths() {
  # Unstaged + staged only — same process / same editor session as this validate:all run.
  git diff --name-only HEAD 2>/dev/null || true
  git diff --cached --name-only HEAD 2>/dev/null || true
}

if [[ "${CI:-}" == "true" ]] || [[ "${CI:-}" == "1" ]]; then
  echo "[validate-web-conditional] CI=true — full web validation (npm run validate:web)"
  exec npm run validate:web
fi

if [[ "${VALIDATE_WEB_FULL:-}" == "1" ]] || [[ "${FORCE_VALIDATE_WEB:-}" == "1" ]]; then
  echo "[validate-web-conditional] VALIDATE_WEB_FULL/FORCE_VALIDATE_WEB — full web validation"
  exec npm run validate:web
fi

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "[validate-web-conditional] Not a git repo — full web validation"
  exec npm run validate:web
fi

PATHS="$(collect_paths | sort -u)"
if echo "${PATHS}" | web_path_touched; then
  echo "[validate-web-conditional] web/ in working tree or index — full web validation (npm run validate:web)"
  exec npm run validate:web
fi

echo "[validate-web-conditional] No web/ paths in unstaged or staged changes — skipping lint/typecheck/test."
echo "[validate-web-conditional] Dashboard progress quick check: npm run progress (master plans → docs/progress.html)"
exec npm run progress
