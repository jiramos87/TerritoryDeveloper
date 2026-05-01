---
purpose: Restore catalog DB + blob mirror from nightly backup; verify; re-publish; smoke.
audience: operator
last_walkthrough: 2026-04-30
---

# Catalog disaster recovery

Restore catalog DB + blob mirror after data loss / corruption / restore-drill failure. References scripts shipped in TECH-8603 (backup) + TECH-8605 (restore drill).

## Pre-conditions

- `pg_dump` / `pg_restore` / `createdb` / `dropdb` on `$PATH`.
- `${BACKUP_ROOT:-$REPO_ROOT/data/backups}/db/{YYYY-MM-DD}/territory_ia_dev.dump` exists.
- `${BACKUP_ROOT:-$REPO_ROOT/data/backups}/blobs/` mirror is populated.
- `$REPO_ROOT/config/postgres-dev.json` resolves to the target Postgres instance.

## Steps

1. **Locate latest dump.**
   ```bash
   cd $REPO_ROOT
   find data/backups/db -maxdepth 2 -name 'territory_ia_dev.dump' | sort | tail -n 1
   ```
   Expected: a path like `data/backups/db/2026-04-30/territory_ia_dev.dump`. None → escalate to backup-cron failure (see TECH-8603 logs at `data/backups/.log/`).

2. **Sanity-check dump integrity.**
   ```bash
   pg_restore --list "$DUMP_PATH" | head -20
   ```
   Expected: TOC entries listing schemas + tables (`catalog_entity`, `sprite_detail`, etc). Empty → dump is corrupt; pick the prior-day dump.

3. **Stop services that touch the live DB.** Quit Unity Editor; stop the dev web server (`Ctrl-C` on `npm --prefix web run dev`).

4. **Snapshot the live DB before destructive restore (safety net).**
   ```bash
   npm run db:snapshot:freeze
   ```
   Expected: snapshot tag printed. Failure → live DB is already too broken to dump; skip with operator approval.

5. **Drop + recreate the live DB.**
   ```bash
   DB_URL=$(node -e 'console.log(JSON.parse(require("fs").readFileSync("config/postgres-dev.json","utf8")).database_url)')
   ADMIN_URL="${DB_URL%/*}/postgres"
   dropdb --if-exists --dbname="$ADMIN_URL" territory_ia_dev
   createdb --dbname="$ADMIN_URL" territory_ia_dev
   ```
   Expected: silent success. Failure → another connection holds DB; quit all clients + retry.

6. **Restore from dump.**
   ```bash
   pg_restore --dbname="$DB_URL" --no-owner --no-privileges "$DUMP_PATH"
   ```
   Expected: completes within minutes; non-fatal warnings on extension comments are normal.

7. **Re-mount blob mirror.**
   ```bash
   rsync -a --delete data/backups/blobs/ var/blobs/
   ```
   Expected: blob tree under `var/blobs/{run_id}/{variant}.png` rebuilt.

8. **Validate catalog spine.**
   ```bash
   npm run validate:catalog-spine
   ```
   Expected: exit 0. Failure → escalate; capture stderr verbatim.

9. **Run full validate chain.**
   ```bash
   npm run validate:all
   ```
   Expected: exit 0.

10. **Smoke dashboard.**
    ```bash
    npm --prefix web run dev
    ```
    Open `http://localhost:3000/dashboard`; assert no red restore-drill banner; assert orphan-blob count not pinned at the pre-restore value.

## Failure-recovery branches

- **Step 6 fails on FK violation** → restore failed mid-way; drop + recreate DB (Step 5) and retry with `pg_restore --single-transaction`.
- **Step 8 reports stale entity refs** → run a fresh `npm run gc:catalog` retired sweep (post-restore aged rows pre-existed the dump); rerun validate.
- **Step 10 banner stays red** → manually run `bash tools/scripts/verify-db-restore.sh` once to refresh the drill JSON; reload dashboard.

### Drift notes

(Record any command that needed adjustment during the most recent walkthrough here.)
