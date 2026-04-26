/**
 * render-worker.ts — DEC-A40 single-FIFO render worker (TECH-1468).
 *
 * Long-lived Node process that drains the `job_queue` FIFO of kind
 * `render_run`, dispatches each job to the sprite-gen FastAPI render
 * endpoint (which writes blobs under BLOB_ROOT itself, DEC-A25),
 * writes a sidecar manifest, inserts a `render_run` provenance row,
 * and marks the job done.
 *
 * Lifecycle:
 *   1. boot → stale-run sweep (heartbeat_at older than 2 min → failed)
 *   2. claim oldest queued render_run row (FOR UPDATE SKIP LOCKED LIMIT 1)
 *   3. dispatch payload to sprite-gen `POST /render`
 *   4. write sidecar manifest under {blob_root}/{run_id}/manifest.json
 *   5. INSERT render_run row + flip job_queue.status='done' (single tx)
 *   6. emit audit row `render.run.completed`
 *   7. loop; sleep WORKER_POLL_INTERVAL_MS when queue empty
 *
 * Failure path: marks job failed, removes per-run blob dir if present,
 * emits audit `render.run.failed`. No retries — DEC-A40 user retries
 * via the retry button (TECH-1469 / TECH-1470).
 *
 * Heartbeat: while a job is running, refresh `heartbeat_at` every 30s
 * so the boot-time sweep can detect orphaned `running` rows after a
 * worker crash.
 *
 * Graceful shutdown: SIGINT / SIGTERM finish the in-flight job before
 * exit; abandoned jobs get caught by the next boot's stale sweep.
 */

import { execSync } from "node:child_process";
import { createHash } from "node:crypto";
import * as fs from "node:fs/promises";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

import postgres, { type Sql } from "postgres";

// ---------------------------------------------------------------------------
// Config + types
// ---------------------------------------------------------------------------

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");

const SPRITE_GEN_URL =
  process.env.SPRITE_GEN_URL?.trim() || "http://127.0.0.1:8765";
const WORKER_POLL_INTERVAL_MS = Number(
  process.env.WORKER_POLL_INTERVAL_MS ?? "1000",
);
const HEARTBEAT_INTERVAL_MS = Number(
  process.env.WORKER_HEARTBEAT_INTERVAL_MS ?? "30000",
);
const STALE_HEARTBEAT_MS = Number(
  process.env.WORKER_STALE_HEARTBEAT_MS ?? `${2 * 60_000}`,
);
const FASTAPI_TIMEOUT_MS = Number(
  process.env.WORKER_FASTAPI_TIMEOUT_MS ?? "120000",
);
// Reserved for future MAX_PARALLEL_RENDERS config — DEC-A40 fixes
// concurrency at 1 for MVP.
const CONCURRENCY = 1;

const KIND_RENDER_RUN = "render_run";

type Mode = "standard" | "identical" | "replay";

type RenderJobPayload = {
  archetype_id: string;
  archetype_version_id: string;
  params_json: Record<string, unknown>;
  parent_run_id?: string | null;
  mode?: Mode;
};

type ClaimedJob = {
  job_id: string;
  payload: RenderJobPayload;
  actor_user_id: string | null;
};

type SpriteGenVariant = {
  idx: number;
  blob_ref: string;
  path?: string;
};

type SpriteGenResponse = {
  run_id: string;
  fingerprint: string;
  variants: SpriteGenVariant[];
};

type LogEvent =
  | "boot"
  | "shutdown"
  | "sweep"
  | "claim"
  | "dispatch"
  | "complete"
  | "fail"
  | "heartbeat"
  | "idle";

// ---------------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------------

function log(event: LogEvent, fields: Record<string, unknown> = {}): void {
  const line = JSON.stringify({
    ts: new Date().toISOString(),
    component: "render-worker",
    event,
    ...fields,
  });
  // eslint-disable-next-line no-console
  console.log(line);
}

// ---------------------------------------------------------------------------
// DB helpers
// ---------------------------------------------------------------------------

export function buildSql(databaseUrl: string): Sql {
  return postgres(databaseUrl, { max: CONCURRENCY + 1 });
}

async function resolveDatabaseUrl(): Promise<string> {
  if (process.env.DATABASE_URL?.trim()) return process.env.DATABASE_URL.trim();
  const mod = (await import(
    path.join(REPO_ROOT, "tools/postgres-ia/resolve-database-url.mjs")
  )) as { resolveDatabaseUrl: (root: string) => string | null };
  const url = mod.resolveDatabaseUrl(REPO_ROOT);
  if (!url) {
    throw new Error(
      "DATABASE_URL not resolvable — set DATABASE_URL or config/postgres-dev.json.",
    );
  }
  return url;
}

// ---------------------------------------------------------------------------
// Build fingerprint
// ---------------------------------------------------------------------------

let _cachedFingerprint: string | null = null;

export function computeBuildFingerprint(): string {
  if (_cachedFingerprint) return _cachedFingerprint;
  const gitSha =
    process.env.GIT_SHA?.trim() ||
    safeExec("git rev-parse HEAD") ||
    "unknown";
  const toolVersion =
    process.env.SPRITE_GEN_VERSION?.trim() || "unknown";
  _cachedFingerprint = `${gitSha}:${toolVersion}`;
  return _cachedFingerprint;
}

function safeExec(cmd: string): string | null {
  try {
    return execSync(cmd, { cwd: REPO_ROOT, stdio: ["ignore", "pipe", "ignore"] })
      .toString()
      .trim();
  } catch {
    return null;
  }
}

// ---------------------------------------------------------------------------
// Canonical JSON + hash
// ---------------------------------------------------------------------------

export function canonicalJson(value: unknown): string {
  if (value === null || typeof value !== "object") return JSON.stringify(value);
  if (Array.isArray(value)) {
    return `[${value.map((v) => canonicalJson(v)).join(",")}]`;
  }
  const keys = Object.keys(value as Record<string, unknown>).sort();
  const parts = keys.map(
    (k) => `${JSON.stringify(k)}:${canonicalJson((value as Record<string, unknown>)[k])}`,
  );
  return `{${parts.join(",")}}`;
}

export function paramsHash(params: Record<string, unknown>): string {
  return createHash("sha256").update(canonicalJson(params)).digest("hex");
}

// ---------------------------------------------------------------------------
// Blob root
// ---------------------------------------------------------------------------

function defaultBlobRoot(): string {
  return path.resolve(REPO_ROOT, "var", "blobs");
}

function blobRoot(): string {
  return process.env.BLOB_ROOT?.trim() || defaultBlobRoot();
}

// ---------------------------------------------------------------------------
// Stale-run sweep
// ---------------------------------------------------------------------------

export async function staleSweep(sql: Sql): Promise<number> {
  const rows = (await sql`
    update job_queue
       set status = 'failed',
           error = 'stale_heartbeat',
           finished_at = now()
     where kind = ${KIND_RENDER_RUN}
       and status = 'running'
       and (heartbeat_at is null or heartbeat_at < now() - (${STALE_HEARTBEAT_MS} || ' milliseconds')::interval)
     returning job_id
  `) as unknown as Array<{ job_id: string }>;
  return rows.length;
}

// ---------------------------------------------------------------------------
// Claim
// ---------------------------------------------------------------------------

export async function claimNextRenderJob(sql: Sql): Promise<ClaimedJob | null> {
  // FOR UPDATE SKIP LOCKED guarantees no double-claim across N workers.
  const rows = (await sql.begin(async (tx) => {
    const candidate = (await tx`
      select job_id, payload_json, actor_user_id
        from job_queue
       where kind = ${KIND_RENDER_RUN}
         and status = 'queued'
       order by enqueued_at asc
       limit 1
       for update skip locked
    `) as unknown as Array<{
      job_id: string;
      payload_json: RenderJobPayload;
      actor_user_id: string | null;
    }>;
    if (candidate.length === 0) return [];
    const row = candidate[0]!;
    await tx`
      update job_queue
         set status = 'running',
             started_at = now(),
             heartbeat_at = now()
       where job_id = ${row.job_id}
    `;
    return [row];
  })) as Array<{
    job_id: string;
    payload_json: RenderJobPayload;
    actor_user_id: string | null;
  }>;
  if (rows.length === 0) return null;
  const row = rows[0]!;
  return {
    job_id: row.job_id,
    payload: row.payload_json,
    actor_user_id: row.actor_user_id,
  };
}

// ---------------------------------------------------------------------------
// Heartbeat scheduler
// ---------------------------------------------------------------------------

function startHeartbeat(sql: Sql, jobId: string): () => Promise<void> {
  const handle = setInterval(() => {
    sql`update job_queue set heartbeat_at = now() where job_id = ${jobId}`
      .then(() => log("heartbeat", { job_id: jobId }))
      .catch((err: Error) =>
        log("heartbeat", { job_id: jobId, error: err.message }),
      );
  }, HEARTBEAT_INTERVAL_MS);
  return async () => {
    clearInterval(handle);
  };
}

// ---------------------------------------------------------------------------
// Sprite-gen dispatch
// ---------------------------------------------------------------------------

export async function dispatchSpriteGen(
  payload: RenderJobPayload,
  signal: AbortSignal,
): Promise<SpriteGenResponse> {
  // For MVP the sprite-gen render body is `{ archetype, params }` — the
  // worker maps the payload's `params_json` (which the API accepts as
  // an opaque blob per DEC-A45) into the sprite-gen schema. The
  // archetype slug lives inside `params_json.archetype` (callers
  // supply it; future revs may pull it from `archetype_id` lookup).
  const archetype =
    (payload.params_json["archetype"] as string | undefined) ?? "";
  if (!archetype) {
    throw new Error(
      "render-worker dispatch: payload.params_json.archetype required",
    );
  }
  const body = {
    archetype,
    terrain: payload.params_json["terrain"] ?? null,
    layered: Boolean(payload.params_json["layered"] ?? false),
    params: (payload.params_json["params"] as Record<string, unknown>) ?? {},
  };
  const res = await fetch(`${SPRITE_GEN_URL}/render`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
    signal,
  });
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`sprite-gen render ${res.status}: ${text.slice(0, 300)}`);
  }
  return (await res.json()) as SpriteGenResponse;
}

// ---------------------------------------------------------------------------
// Manifest sidecar
// ---------------------------------------------------------------------------

export async function writeManifest(
  runId: string,
  fields: {
    archetype_version_id: string;
    params_json: Record<string, unknown>;
    build_fingerprint: string;
    duration_ms: number;
  },
): Promise<string> {
  const dir = path.join(blobRoot(), runId);
  await fs.mkdir(dir, { recursive: true });
  const manifestPath = path.join(dir, "manifest.json");
  const manifest = {
    schema: "render-run-manifest/1",
    run_id: runId,
    archetype_version_id: fields.archetype_version_id,
    params_json: fields.params_json,
    build_fingerprint: fields.build_fingerprint,
    duration_ms: fields.duration_ms,
    written_at: new Date().toISOString(),
  };
  await fs.writeFile(manifestPath, JSON.stringify(manifest, null, 2), "utf8");
  return manifestPath;
}

// ---------------------------------------------------------------------------
// Audit emission
// ---------------------------------------------------------------------------

async function emitAudit(
  sql: Sql,
  action: string,
  actorUserId: string | null,
  targetId: string,
  payload: Record<string, unknown>,
): Promise<void> {
  await sql`
    insert into audit_log (actor_user_id, action, target_kind, target_id, payload, created_at)
    values (${actorUserId}, ${action}, 'render_run', ${targetId}, ${sql.json(payload as never)}, now())
  `;
}

// ---------------------------------------------------------------------------
// Job execution
// ---------------------------------------------------------------------------

export async function executeJob(
  sql: Sql,
  job: ClaimedJob,
  abortSignal: AbortSignal,
): Promise<void> {
  log("dispatch", { job_id: job.job_id, mode: job.payload.mode ?? "standard" });
  const stopHeartbeat = startHeartbeat(sql, job.job_id);
  const startedAt = Date.now();
  try {
    const sg = await dispatchSpriteGen(job.payload, abortSignal);
    const durationMs = Date.now() - startedAt;
    const fingerprint = computeBuildFingerprint();
    const outputUris = sg.variants.map((v) => v.blob_ref);
    await writeManifest(sg.run_id, {
      archetype_version_id: job.payload.archetype_version_id,
      params_json: job.payload.params_json,
      build_fingerprint: fingerprint,
      duration_ms: durationMs,
    });
    const hash = paramsHash(job.payload.params_json);
    const parentRunId = job.payload.parent_run_id ?? null;
    await sql.begin(async (tx) => {
      const inserted = (await tx`
        insert into render_run (
          archetype_id, archetype_version_id, params_json, params_hash,
          output_uris, build_fingerprint, duration_ms, triggered_by,
          parent_run_id
        ) values (
          ${job.payload.archetype_id},
          ${job.payload.archetype_version_id},
          ${tx.json(job.payload.params_json as never)},
          ${hash},
          ${outputUris as unknown as string[]},
          ${fingerprint},
          ${durationMs},
          ${job.actor_user_id},
          ${parentRunId}
        )
        returning run_id
      `) as unknown as Array<{ run_id: string }>;
      const runId = inserted[0]!.run_id;
      await tx`
        update job_queue
           set status = 'done',
               finished_at = now(),
               heartbeat_at = now(),
               payload_json = payload_json || ${tx.json({ run_id: runId } as never)}
         where job_id = ${job.job_id}
      `;
      await emitAudit(tx, "render.run.completed", job.actor_user_id, runId, {
        job_id: job.job_id,
        archetype_id: job.payload.archetype_id,
        archetype_version_id: job.payload.archetype_version_id,
        params_hash: hash,
        duration_ms: durationMs,
        output_count: outputUris.length,
        mode: job.payload.mode ?? "standard",
        parent_run_id: parentRunId,
      });
    });
    log("complete", {
      job_id: job.job_id,
      sprite_run_id: sg.run_id,
      output_count: outputUris.length,
      duration_ms: durationMs,
    });
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    await sql`
      update job_queue
         set status = 'failed',
             error = ${message},
             finished_at = now()
       where job_id = ${job.job_id}
    `;
    // Best-effort blob dir cleanup: the sprite-gen run-id (when known)
    // is not echoed back on failure, so we cannot target the dir
    // precisely. The DEC-A41 GC pass owns orphan-blob cleanup.
    await emitAudit(sql, "render.run.failed", job.actor_user_id, job.job_id, {
      job_id: job.job_id,
      error: message,
      mode: job.payload.mode ?? "standard",
    });
    log("fail", { job_id: job.job_id, error: message });
  } finally {
    await stopHeartbeat();
  }
}

// ---------------------------------------------------------------------------
// Main loop
// ---------------------------------------------------------------------------

async function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

export type WorkerHandle = {
  shutdown: () => Promise<void>;
  done: Promise<void>;
};

export async function runWorker(sql: Sql): Promise<WorkerHandle> {
  const abort = new AbortController();
  let stopRequested = false;
  let inFlight: Promise<void> | null = null;

  const swept = await staleSweep(sql);
  log("boot", {
    sprite_gen_url: SPRITE_GEN_URL,
    blob_root: blobRoot(),
    fingerprint: computeBuildFingerprint(),
    swept_count: swept,
  });

  const loop = (async () => {
    while (!stopRequested) {
      const job = await claimNextRenderJob(sql).catch((err: Error) => {
        log("fail", { phase: "claim", error: err.message });
        return null;
      });
      if (!job) {
        log("idle", { interval_ms: WORKER_POLL_INTERVAL_MS });
        await sleep(WORKER_POLL_INTERVAL_MS);
        continue;
      }
      log("claim", { job_id: job.job_id });
      // Wrap with a per-job timeout via AbortSignal.
      const jobAbort = new AbortController();
      const timer = setTimeout(() => jobAbort.abort(), FASTAPI_TIMEOUT_MS);
      inFlight = executeJob(sql, job, jobAbort.signal);
      try {
        await inFlight;
      } finally {
        clearTimeout(timer);
        inFlight = null;
      }
    }
  })();

  const shutdown = async () => {
    if (stopRequested) return;
    stopRequested = true;
    abort.abort();
    if (inFlight) await inFlight.catch(() => {});
    log("shutdown", {});
  };

  return { shutdown, done: loop };
}

// ---------------------------------------------------------------------------
// Entrypoint
// ---------------------------------------------------------------------------

async function main(): Promise<number> {
  const dbUrl = await resolveDatabaseUrl();
  const sql = buildSql(dbUrl);
  const handle = await runWorker(sql);
  const exit = async (code: number) => {
    await handle.shutdown();
    await sql.end({ timeout: 5 }).catch(() => {});
    process.exit(code);
  };
  process.on("SIGINT", () => void exit(0));
  process.on("SIGTERM", () => void exit(0));
  await handle.done;
  await sql.end({ timeout: 5 }).catch(() => {});
  return 0;
}

const isDirect = process.argv[1] && process.argv[1].endsWith("render-worker.ts");
if (isDirect) {
  main()
    .then((code) => process.exit(code))
    .catch((err) => {
      // eslint-disable-next-line no-console
      console.error(`[render-worker] fatal: ${(err as Error).message}`);
      process.exit(1);
    });
}
