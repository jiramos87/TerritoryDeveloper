# Human resume — no AI, no MCP, no skills

Cheat-sheet for resuming master-plan roadmap progress without any AI tooling. Use when MCP server down, skill cache broken, offline, or AI quota exhausted.

Caveman prose throughout (project default). Code/commits/SQL = normal English.

---

## Survival kit — read once before starting

| File | Why |
|---|---|
| `docs/MASTER-PLAN-STRUCTURE.md` | Canonical Stage block + Task table shape |
| `docs/PROJECT-SPEC-STRUCTURE.md` | Per-task spec §1–§10 schema |
| `docs/agent-lifecycle.md` §1 + §2 | Flow diagram + seam → surface matrix |
| `AGENTS.md` | Cross-harness workflow (root) |
| `ia/templates/project-spec-template.md` | Spec skeleton to copy |
| `ia/specs/architecture/README.md` | Architecture sub-spec index |
| `ia/specs/glossary.md` | Domain vocabulary — match terms verbatim |
| `ia/rules/invariants.md` + `ia/rules/unity-invariants.md` | 13 invariants + IF→THEN guardrails |

---

## Step 1 — Locate current position

```bash
# Open issues
cat BACKLOG.md

# Master plan state (pick the slug)
ls docs/*master-plan*.md
cat docs/{slug}-master-plan.md

# Ephemeral blockers
cat MEMORY.md

# Last verify run + bridge preflight
cat ia/state/runtime-state.json

# Last shipped Stage
git log --oneline -20 | grep -E "feat\(.+-stage-"
```

Find Stage X.Y where `Status = pending` and all dependency Stages `= done`.

---

## Step 2 — Pick next Stage

Read Stage block in `docs/{slug}-master-plan.md`:
- **Objectives** — outcome statements
- **Exit criteria** — done-when conditions
- **Task table** — 5 columns: `Task | Issue | Status | Notes | Acceptance`

If Task table empty (skeleton Stage) → decompose first (Step 2b). Else jump to Step 3.

### Step 2b — Decompose skeleton Stage

Apply rules from `docs/MASTER-PLAN-STRUCTURE.md`:
- ≥2 Tasks per Stage (cardinality gate)
- Each Task = H1–H6 sized (1–3 day chunk)
- Task title = imperative phrase, glossary terms verbatim
- Acceptance = testable predicate

Write Task rows directly into the Stage block. Status column = `pending` for all new rows.

---

## Step 3 — File tasks (skeleton → BACKLOG)

For each Task row needing a BACKLOG id:

```bash
# Reserve next id (returns e.g. TECH-87)
bash tools/scripts/reserve-id.sh TECH

# Copy spec template
cp ia/templates/project-spec-template.md ia/projects/TECH-87.md

# Author yaml record
$EDITOR ia/backlog/TECH-87.yaml
# Schema fields: id, title, type, priority, status, depends_on, related, master_plan, stage

# Regenerate BACKLOG.md from yaml records
bash tools/scripts/materialize-backlog.sh

# Validate
npm run validate:all
```

Update Stage Task table — fill `Issue` column with new id (e.g. `TECH-87`).

---

## Step 4 — Author spec body

Edit `ia/projects/{ID}.md` §1–§10 by hand. Per `docs/PROJECT-SPEC-STRUCTURE.md`:

- §1 Goal — one paragraph, why this task exists
- §2 Acceptance — testable predicates (mirror BACKLOG row)
- §3 Pending Decisions — open questions
- §4 Implementer Latitude — what implementer may decide vs must follow
- §5 Work Items — file-level change list
- §6 Test Blueprint — how to verify
- §7 Invariants & Gate — which invariants touched
- §8–§10 — context as needed

**Glossary alignment (manual):** grep `ia/specs/glossary.md` for surface terms appearing in your spec. Mismatched terms → fix to glossary spelling.

```bash
grep -i "{your term}" ia/specs/glossary.md
```

**Caveman prose** for §1–§10 narrative. Code blocks + identifiers = normal.

---

## Step 5 — Implement

Edit code per §5 Work Items. Minimal diffs.

```bash
# After C# edits (mandatory)
npm run unity:compile-check

# After IA / MCP / fixture / rules edits
npm run validate:all
```

Touching `Assets/Scripts/**/*.cs`, `GridManager`, `HeightMap`, roads, water, cliffs → re-read `ia/rules/unity-invariants.md` before commit.

---

## Step 6 — Verify

```bash
# Full local chain (CI parity)
npm run verify:local
```

Runs: `validate:all` + `unity:compile-check` + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`.

Failure → read stdout, fix root cause, re-run. Do not skip steps.

If Unity bridge needed for evidence and unavailable → document gap in §6 Test Blueprint with note "deferred — bridge unavailable", flag in PR description.

---

## Step 7 — Close Stage

When all Tasks in Stage X.Y green:

### 7a — Flip statuses (manual edit `docs/{slug}-master-plan.md`)

- Each Task row: `Status` column `pending → done`
- Stage rollup row: `Status` column `pending → done` (only after all Tasks done)

### 7b — Append change-log row

In master-plan `## Change Log` section:

```markdown
| 2026-04-28 | Stage X.Y closed | {commit-sha-placeholder} | {one-line summary} |
```

### 7c — Commit

```bash
git add -A
git commit -m "feat({slug}-stage-X.Y): {one-line summary}"

# Capture SHA, update change-log row
git log -1 --format=%H
$EDITOR docs/{slug}-master-plan.md   # replace placeholder with SHA
git add docs/{slug}-master-plan.md
git commit --amend --no-edit
```

### 7d — DB sync (post-migration only)

If `ia_stages` / `ia_task_specs.body` / `ia_master_plans` migrated to Postgres, hand-write SQL:

```sql
-- Flip task statuses
UPDATE ia_tasks SET status = 'done' WHERE issue_id IN ('TECH-87', 'TECH-88') AND stage_id = '{stage-uuid}';

-- Flip stage
UPDATE ia_stages SET status = 'done', closed_at = NOW() WHERE id = '{stage-uuid}';

-- Append change-log
INSERT INTO ia_master_plan_change_log (master_plan_id, kind, payload, created_at)
VALUES ('{plan-uuid}', 'stage_closed', '{"stage": "X.Y", "commit": "{sha}"}'::jsonb, NOW());
```

Get UUIDs from existing rows: `SELECT id, slug FROM ia_master_plans;` + `SELECT id, stage_number FROM ia_stages WHERE master_plan_id = '...';`.

---

## Step 8 — Final validation

```bash
npm run validate:all              # master-plan-status + backlog-yaml + frontmatter
npm run validate:claude-imports   # CLAUDE.md drift gate
```

CI red on any plan blocks all ships — green here means commit is shippable.

---

## Hard blockers without AI — manual substitutes

| Skill/MCP function | Manual substitute |
|---|---|
| `glossary_discover` / `glossary_lookup` | `grep -i {term} ia/specs/glossary.md` |
| `router_for_task` | Read `ia/rules/agent-router.md` |
| `invariant_preflight` | Read `ia/rules/invariants.md` + `ia/rules/unity-invariants.md` end-to-end |
| `plan_digest_lint` | Eyeball against `docs/MASTER-PLAN-STRUCTURE.md` §Task Spec Shape |
| `task_status_flip` | Hand-edit master-plan `.md` Task table OR `UPDATE ia_tasks SET status=...` |
| `stage_closeout_apply` | Steps 7a–7c above + optional 7d SQL |
| `task_insert` | Step 3 (`reserve-id.sh` + yaml + `materialize-backlog.sh`) |
| `master_plan_change_log_append` | Step 7b (manual row append) |
| `stage_verification_flip` | UPDATE ia_stages SET verified_at = NOW(), commit_sha = '...' |
| `unity_bridge_command` | Run Unity Editor manually, perform mutation by hand |

---

## Pre-flight checklist (every Stage)

- [ ] Master-plan slug + Stage X.Y identified
- [ ] Dependencies all `done`
- [ ] Survival kit docs read (or skimmed if recent)
- [ ] `npm run validate:all` green at start (clean baseline)
- [ ] No uncommitted changes from prior Stage
- [ ] `runtime-state.json` shows recent successful `verify:local`

## Post-flight checklist (every Stage)

- [ ] All Tasks in Stage flipped `done`
- [ ] Stage row flipped `done`
- [ ] Change-log row appended with SHA
- [ ] Single stage commit on branch
- [ ] `npm run validate:all` green
- [ ] `npm run verify:local` green
- [ ] DB rows sync'd (post-migration)

---

## When to bail and wait for AI

- Multi-stage refactor with cross-cutting glossary impact (manual grep insufficient)
- Unity bridge required for verification + bridge broken
- Skeleton Stage with ambiguous Exit criteria — decomposition guesswork unsafe
- Plan-review drift scan needed (mechanical + semantic) — no manual substitute scales

For these → park work, document blocker in `MEMORY.md`, resume when AI tooling restored.
