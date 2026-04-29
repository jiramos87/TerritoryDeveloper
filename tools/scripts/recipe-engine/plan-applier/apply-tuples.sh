#!/usr/bin/env bash
# plan-applier — bash wrapper for apply-tuples.mjs.
#
# Recipe engine bash step spawns `bash {scriptPath} ...argv`, which won't
# honor a #!/usr/bin/env node shebang on a .mjs file. Wrapper exec's node.
#
# Args (forwarded verbatim):
#   --slug <slug>       --stage-id <X.Y>
#   --body <markdown>   OR --body-file <path>
#   --repo-root <path>  (optional)
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec node "${SCRIPT_DIR}/apply-tuples.mjs" "$@"
