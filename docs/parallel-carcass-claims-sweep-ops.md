# Claims Sweep — Ops Guide

Operator + agent guide for the scheduled `claim_swept` cron tick that releases stale `ia_section_claims` + `ia_stage_claims` rows.

Linked specs: `docs/parallel-carcass-exploration.md` §6.2 (D4); `tools/scripts/claims-sweep-tick.mjs` (runner); `tools/mcp-ia-server/src/tools/claim-heartbeat.ts` (`applySweep` + `claims_sweep` MCP tool); migration `0049_parallel_carcass_primitives.sql` + `0052_carcass_claims_v2_row_only.sql` (claim tables).

---

## §1 Cadence + activation

System `cron` at `* * * * *` (every minute) is canonical cadence — zero dev-host daemon dep, survives shell exit. `node-cron` is fallback only for hosts without system cron.

Default timeout: `carcass_config.claim_heartbeat_timeout_minutes` row, `value='10'`. Tick releases rows whose `last_heartbeat < now() - 10 minutes`.

### Linux activation

Drop `/etc/cron.d/claims-sweep` with one line (replace `<user>` + `/repo`):

```cron
* * * * * <user> cd /repo && npm run claims:sweep:tick >> /var/log/claims-sweep.log 2>&1
```

User crontab variant (`crontab -e`):

```cron
* * * * * cd /repo && /usr/local/bin/npm run claims:sweep:tick >> $HOME/claims-sweep.log 2>&1
```

### macOS activation

`launchd` plist at `~/Library/LaunchAgents/dev.bacayo.claims-sweep.plist`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key><string>dev.bacayo.claims-sweep</string>
  <key>ProgramArguments</key>
  <array>
    <string>/bin/bash</string>
    <string>-lc</string>
    <string>cd /repo && npm run claims:sweep:tick</string>
  </array>
  <key>StartInterval</key><integer>60</integer>
  <key>StandardOutPath</key><string>/tmp/claims-sweep.log</string>
  <key>StandardErrorPath</key><string>/tmp/claims-sweep.log</string>
  <key>RunAtLoad</key><true/>
</dict>
</plist>
```

Load: `launchctl load ~/Library/LaunchAgents/dev.bacayo.claims-sweep.plist`.

### `node-cron` fallback

When system cron unavailable (containerized dev / CI runner without cron), wire `node-cron` schedule `* * * * *` calling the same `runSweepTick()` exported from `tools/scripts/claims-sweep-tick.mjs`. Single long-lived process — supervisor restart on crash.

---

## §2 Sweep activity query

Read recent `claim_swept` rows from `ia_master_plan_change_log`:

```sql
SELECT slug, ts, body, metadata
  FROM ia_master_plan_change_log
 WHERE kind = 'claim_swept'
 ORDER BY ts DESC
 LIMIT 20;
```

`body` is JSON of `{ released_count, section_released[], stage_released[], timeout_minutes }`. One row per affected slug per tick. Zero releases → zero rows (silent no-op).

Filter by slug: add `AND slug = '<plan-slug>'`.

---

## §3 Manual override

Direct MCP invocation (agent-driven, no cron wait):

```
mcp__territory-ia__claims_sweep
```

Input shape: empty object `{}`. Returns `{ timeout_minutes, section_claims_released, stage_claims_released }`. Same SQL UPDATE pair as the tick — `kind='claim_swept'` change_log row NOT emitted by the raw MCP call (only the tick wrapper emits structured per-slug rows). Use direct call for incident sweep where audit trail not required.

CLI variant: `npm run claims:sweep:tick` — runs the wrapper end-to-end (sweep + per-slug change_log emit), exits 0 on success, exits 1 on error.

---

## §4 Stuck-claim troubleshooting

Default timeout = 10 min. Escalation gate:

- Row stuck > 30 min past timeout (last_heartbeat older than 40 min) AND zero `claim_swept` change_log rows for the affected slug → **engineer review** (cron not firing, sweep tool broken, DB connection wedged). Page before manual UPDATE.
- Otherwise (single stuck row, sweep firing on other slugs, claim recently abandoned) → manual UPDATE recipe authorized.

### Manual release recipe

One slug + key pair at a time — never bulk-release. Section claim:

```sql
UPDATE ia_section_claims
   SET released_at = now()
 WHERE slug = '<plan-slug>'
   AND section_id = '<section-id>'
   AND released_at IS NULL;
```

Stage claim:

```sql
UPDATE ia_stage_claims
   SET released_at = now()
 WHERE slug = '<plan-slug>'
   AND stage_id = '<stage-id>'
   AND released_at IS NULL;
```

Audit row (recommended — keeps timeline visible to agents):

```sql
INSERT INTO ia_master_plan_change_log (slug, kind, body, actor)
VALUES (
  '<plan-slug>',
  'claim_swept',
  '{"released_count":1,"section_released":["<section-id>"],"stage_released":[],"timeout_minutes":10,"manual":true}',
  'ops-manual'
);
```

### Diagnostic queries

Currently held claims (active rows only):

```sql
SELECT slug, section_id, last_heartbeat, age(now(), last_heartbeat) AS staleness
  FROM ia_section_claims
 WHERE released_at IS NULL
 ORDER BY last_heartbeat ASC;

SELECT slug, stage_id, last_heartbeat, age(now(), last_heartbeat) AS staleness
  FROM ia_stage_claims
 WHERE released_at IS NULL
 ORDER BY last_heartbeat ASC;
```

Last sweep run timestamp:

```sql
SELECT max(ts) AS last_sweep_emit
  FROM ia_master_plan_change_log
 WHERE kind = 'claim_swept';
```

Gap > 2 minutes from `now()` + claims older than timeout → cron likely not firing. Check `/var/log/claims-sweep.log` (linux) or `/tmp/claims-sweep.log` (macOS launchd).

---

## §5 See also

- `tools/scripts/claims-sweep-tick.mjs` — runner source.
- `tools/mcp-ia-server/tests/tools/claims-sweep.scheduled.test.ts` — integration test (TECH-5253).
- `tools/mcp-ia-server/tests/tools/claim-heartbeat.test.ts` — `applySweep` + `applyHeartbeat` semantics (TECH-4829).
- `docs/parallel-carcass-exploration.md` §6.2 — design rationale + MCP tool delta table.
