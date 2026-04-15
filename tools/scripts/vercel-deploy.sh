#!/usr/bin/env bash
# Deploy web/ to Vercel production, then auto-prune old deployments.
# Usage: npm run deploy:web              (prod, forced rebuild, prune)
#        npm run deploy:web:preview      (preview alias)
#
# Auth token: ~/Library/Application Support/com.vercel.cli/auth.json
# Project link: .vercel/project.json (gitignored, local only).
# Auto-prune: keeps newest KEEP_N deployments (default 3), deletes older ones.
# Override prune depth: PRUNE_KEEP=5 npm run deploy:web
# Skip prune: PRUNE_KEEP=0 npm run deploy:web

set -euo pipefail

cd "$(dirname "$0")/../.."

AUTH_PATH="$HOME/Library/Application Support/com.vercel.cli/auth.json"
if [[ ! -f "$AUTH_PATH" ]]; then
  echo "Vercel auth file missing at: $AUTH_PATH" >&2
  echo "Run: npm i -g vercel && vercel login" >&2
  exit 1
fi

TOKEN=$(python3 -c "import json; print(json.load(open('$AUTH_PATH'))['token'])")

if [[ ! -f .vercel/project.json ]]; then
  echo "Vercel project not linked. Run: npx vercel link --yes" >&2
  exit 1
fi

PROJECT_ID=$(python3 -c "import json; print(json.load(open('.vercel/project.json'))['projectId'])")
ORG_ID=$(python3 -c "import json; print(json.load(open('.vercel/project.json'))['orgId'])")

TARGET="--prod"
LABEL="production"
if [[ "${1:-}" == "--preview" ]]; then
  TARGET=""
  LABEL="preview"
fi

echo "Deploying $LABEL to Vercel..."
npx vercel $TARGET --force --archive=tgz --token "$TOKEN" --yes

KEEP_N="${PRUNE_KEEP:-3}"
if [[ "$KEEP_N" -le 0 ]]; then
  echo "Prune skipped (PRUNE_KEEP=$KEEP_N)"
  exit 0
fi

echo ""
echo "Pruning deployments older than newest $KEEP_N ..."

DEPLOYMENTS_TMP=$(mktemp)
trap 'rm -f "$DEPLOYMENTS_TMP"' EXIT

curl -s -H "Authorization: Bearer $TOKEN" \
  "https://api.vercel.com/v6/deployments?projectId=$PROJECT_ID&teamId=$ORG_ID&limit=50" \
  > "$DEPLOYMENTS_TMP"

TO_DELETE=$(KEEP_N="$KEEP_N" python3 -c "
import json, os, sys
with open('$DEPLOYMENTS_TMP') as f:
    data = json.load(f)
deps = sorted(data.get('deployments', []), key=lambda d: d.get('created', 0), reverse=True)
keep = int(os.environ['KEEP_N'])
for d in deps[keep:]:
    print(d['uid'])
")

if [[ -z "$TO_DELETE" ]]; then
  echo "Nothing to prune."
  exit 0
fi

COUNT=0
while IFS= read -r DEP_ID; do
  [[ -z "$DEP_ID" ]] && continue
  curl -s -X DELETE -H "Authorization: Bearer $TOKEN" \
    "https://api.vercel.com/v13/deployments/$DEP_ID?teamId=$ORG_ID" > /dev/null
  echo "  deleted $DEP_ID"
  COUNT=$((COUNT + 1))
done <<< "$TO_DELETE"

echo "Pruned $COUNT old deployment(s); kept newest $KEEP_N."
