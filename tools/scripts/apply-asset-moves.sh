#!/usr/bin/env bash
# apply-asset-moves.sh — execute git mv for asset reorg passes
# Usage: apply-asset-moves.sh [--pass=1|2] [--family=<name>] [--dry-run]
#
# Pass 1: reads manifest.csv — folder moves only (current_path != target_path, name unchanged).
# Pass 2: reads manifest-pass2.csv — file renames only (current_name != target_name).
#         Run build-asset-manifest.mjs --pass=2 first to regenerate manifest-pass2.csv.
# Each git mv is paired: asset + asset.meta (GUID preservation).

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MANIFEST_PASS1="$REPO_ROOT/tools/scripts/asset-tree-reorg/manifest.csv"
MANIFEST_PASS2="$REPO_ROOT/tools/scripts/asset-tree-reorg/manifest-pass2.csv"

PASS=""
FAMILY=""
DRY_RUN=false

for arg in "$@"; do
  case "$arg" in
    --pass=*) PASS="${arg#--pass=}" ;;
    --family=*) FAMILY="${arg#--family=}" ;;
    --dry-run) DRY_RUN=true ;;
    *) echo "Unknown argument: $arg" >&2; exit 1 ;;
  esac
done

if [[ -z "$PASS" ]]; then
  echo "ERROR: --pass=1 or --pass=2 required" >&2
  exit 1
fi

# Select manifest per pass
case "$PASS" in
  1) MANIFEST="$MANIFEST_PASS1" ;;
  2) MANIFEST="$MANIFEST_PASS2" ;;
  *) echo "ERROR: --pass must be 1 or 2" >&2; exit 1 ;;
esac

if [[ ! -f "$MANIFEST" ]]; then
  echo "ERROR: manifest not found at $MANIFEST" >&2
  if [[ "$PASS" == "2" ]]; then
    echo "  Run: node tools/scripts/build-asset-manifest.mjs --pass=2" >&2
  fi
  exit 1
fi

move_count=0
skip_count=0
error_count=0

# Read CSV: current_path,target_path,current_name,target_name,family,reason,meta_guid
while IFS=, read -r current_path target_path current_name target_name family reason meta_guid; do
  # Strip surrounding quotes
  current_path="${current_path//\"/}"
  target_path="${target_path//\"/}"
  current_name="${current_name//\"/}"
  target_name="${target_name//\"/}"
  family="${family//\"/}"

  # Skip header
  [[ "$current_path" == "current_path" ]] && continue

  # Family filter
  if [[ -n "$FAMILY" && "$family" != "$FAMILY" ]]; then
    continue
  fi

  # Determine if this row applies to this pass
  folder_move=false
  name_change=false

  if [[ "$current_path" != "$target_path" ]]; then
    folder_move=true
  fi
  if [[ "$current_name" != "$target_name" ]]; then
    name_change=true
  fi

  case "$PASS" in
    1)
      # Pass 1: folder moves only — skip rows already in canonical folder
      if ! $folder_move; then
        skip_count=$((skip_count + 1))
        continue
      fi
      src="$REPO_ROOT/$current_path"
      dst="$REPO_ROOT/$target_path"
      src_meta="${src}.meta"
      dst_meta="${dst}.meta"
      ;;
    2)
      # Pass 2: file renames only — manifest-pass2.csv supplies correct target_path.
      # Rows are pre-filtered by build-asset-manifest.mjs --pass=2 (current_name != target_name).
      # Skip defensive check for name_change == false (manifest already filtered).
      if ! $name_change; then
        skip_count=$((skip_count + 1))
        continue
      fi
      src="$REPO_ROOT/$current_path"
      # target_path from manifest-pass2.csv = dirname(current_path)/target_name
      dst="$REPO_ROOT/$target_path"
      src_meta="${src}.meta"
      dst_meta="${dst}.meta"
      ;;
  esac

  # Validate source exists
  if [[ ! -f "$src" ]]; then
    echo "WARN: source not found, skipping: $src" >&2
    skip_count=$((skip_count + 1))
    continue
  fi

  # Skip if src == dst (already moved)
  if [[ "$src" == "$dst" ]]; then
    skip_count=$((skip_count + 1))
    continue
  fi

  # Ensure target directory exists
  dst_dir="$(dirname "$dst")"

  if $DRY_RUN; then
    echo "[DRY-RUN] git mv $current_path $target_path"
    echo "[DRY-RUN] git mv ${current_path}.meta ${target_path}.meta"
    move_count=$((move_count + 1))
  else
    # Create target dir if needed (git won't create dirs)
    mkdir -p "$dst_dir"

    # Move asset
    cd "$REPO_ROOT"
    if git mv "$current_path" "$target_path"; then
      echo "OK: $current_path -> $target_path"
    else
      echo "ERROR: git mv failed for $current_path" >&2
      error_count=$((error_count + 1))
      continue
    fi

    # Move meta (required to preserve GUID)
    if [[ -f "${src_meta}" ]]; then
      git mv "${current_path}.meta" "${target_path}.meta"
      echo "OK: ${current_path}.meta -> ${target_path}.meta"
    else
      echo "WARN: .meta not found for $current_path — GUID at risk" >&2
    fi

    move_count=$((move_count + 1))
  fi

done < "$MANIFEST"

echo ""
echo "--- apply-asset-moves.sh pass=$PASS summary ---"
echo "Moves applied : $move_count"
echo "Skipped       : $skip_count"
echo "Errors        : $error_count"

if [[ $error_count -gt 0 ]]; then
  echo "FAILED: $error_count errors encountered" >&2
  exit 1
fi

echo "Done."
