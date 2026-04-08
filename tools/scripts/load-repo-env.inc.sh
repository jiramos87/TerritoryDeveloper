# Territory — shared bash dotenv loader for repo-root scripts.
# Usage: source "${SCRIPT_DIR}/load-repo-env.inc.sh" then territory_load_repo_dotenv_files "$REPO_ROOT"
territory_load_repo_dotenv_files() {
  local root="$1"
  [[ -n "$root" ]] || return 0
  local f
  for f in "${root}/.env" "${root}/.env.local"; do
    if [[ -f "$f" ]]; then
      set -a
      # shellcheck disable=SC1090
      source "$f"
      set +a
    fi
  done
}
