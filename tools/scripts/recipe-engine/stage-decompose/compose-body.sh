#!/usr/bin/env bash
# stage-decompose — thin wrapper for compose-body.mjs.
#
# Args (forwarded verbatim):
#   --existing-body <string>    Verbatim body from stage_render.
#   --stage-id <X.Y>            Stage id used to mint TX.Y.{i} task ids.
#   --seam-output <json>        JSON object matching decompose-skeleton-stage seam output.
#
# Stdout: full Stage body markdown ready for stage_body_write.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec node "${SCRIPT_DIR}/compose-body.mjs" "$@"
