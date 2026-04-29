#!/usr/bin/env bash
# stage-authoring — compose §Plan Digest markdown body from seam output sections.
#
# Args (all required, all literal markdown body fragments):
#   --goal                     <string>
#   --acceptance               <string>
#   --pending-decisions        <string>
#   --implementer-latitude     <string>
#   --work-items               <string>
#   --test-blueprint           <string>
#   --invariants-and-gate      <string>
#
# Stdout: full §Plan Digest markdown body, ready for task_spec_section_write
#         (section arg `§Plan Digest`).
#
# Idempotent: deterministic output for identical input.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec node "${SCRIPT_DIR}/compose-body.mjs" "$@"
