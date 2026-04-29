#!/usr/bin/env bash
# tools/scripts/recipe-engine/echo.sh
# Smoke helper for tools/recipes/noop-smoke.yaml. Reads `--message <str>` and
# prints it on stdout. Used by validate:recipe-drift smoke + recipe-engine
# self-tests.
set -euo pipefail
msg=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --message)
      msg="$2"
      shift 2
      ;;
    *)
      shift
      ;;
  esac
done
echo "$msg"
