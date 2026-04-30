#!/usr/bin/env bash
# master-plan-new-phase-a — predicate: assert seeded_count >= min_count.
#
# Stage 2.4 / TECH-5248 — Phase A architecture-lock-seal verify gate.
# Trips with exit 1 when count short (boundaries + end-state-contract +
# shared-seams minimum cluster missing). Trips with exit 2 on argv shape
# mismatch (caller bug).
#
# Argv: --seeded_count N --min_count M (both required; flag-pair shape per
# recipe-engine bash step flattenArgs convention).

set -euo pipefail

seeded_count=""
min_count=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --seeded_count)
      seeded_count="$2"
      shift 2
      ;;
    --min_count)
      min_count="$2"
      shift 2
      ;;
    *)
      echo "verify-seeded-count: unknown arg '$1'" >&2
      exit 2
      ;;
  esac
done

if [[ -z "$seeded_count" || -z "$min_count" ]]; then
  echo "verify-seeded-count: missing --seeded_count or --min_count" >&2
  exit 2
fi

if ! [[ "$seeded_count" =~ ^[0-9]+$ ]]; then
  echo "verify-seeded-count: seeded_count='$seeded_count' is not an integer" >&2
  exit 2
fi

if ! [[ "$min_count" =~ ^[0-9]+$ ]]; then
  echo "verify-seeded-count: min_count='$min_count' is not an integer" >&2
  exit 2
fi

if (( seeded_count < min_count )); then
  echo "verify-seeded-count: count_mismatch — seeded_count=$seeded_count < min_count=$min_count" >&2
  exit 1
fi

echo "verify-seeded-count: ok — seeded_count=$seeded_count >= min_count=$min_count"
exit 0
