#!/usr/bin/env bash
# master-plan-new-phase-a — helper: emit current HEAD commit SHA on stdout.
# Used to bind commit_sha for master_plan_lock_arch in Phase A recipe.
set -euo pipefail
git rev-parse HEAD
