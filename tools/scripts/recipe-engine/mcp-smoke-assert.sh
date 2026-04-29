#!/usr/bin/env bash
# tools/scripts/recipe-engine/mcp-smoke-assert.sh
# Assertion helper for tools/recipes/mcp-smoke.yaml. Reads `--count <int>` and
# exits non-zero when count is missing, non-numeric, or < 1. Used by the
# recipe-engine self-test to verify mcp step + injector + bind chain.
set -euo pipefail
count=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --count)
      count="$2"
      shift 2
      ;;
    *)
      shift
      ;;
  esac
done
if [[ -z "$count" ]]; then
  echo "mcp-smoke-assert: missing --count" >&2
  exit 2
fi
if ! [[ "$count" =~ ^[0-9]+$ ]]; then
  echo "mcp-smoke-assert: --count not numeric: '$count'" >&2
  exit 3
fi
if (( count < 1 )); then
  echo "mcp-smoke-assert: list_specs returned 0 rows — registry empty or unwrap broke" >&2
  exit 4
fi
echo "mcp-smoke-assert: ok count=$count"
