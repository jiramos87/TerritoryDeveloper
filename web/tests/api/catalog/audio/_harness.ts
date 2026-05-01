// Audio catalog API integration harness (TECH-8608 / Stage 19.1).
//
// Audio surface is read-only via routes (GET list, GET detail, POST promote).
// Promote needs filesystem + lint-rule DB seed and is exercised in Stage 9.1
// fixtures — this harness covers list + detail. Pre-seed audio_detail rows
// directly via SQL (no POST endpoint exists for create).

import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

export const AUDIO_TEST_USER_ID = "66666666-6666-4666-8666-666666666666";

export async function resetAudioTables(): Promise<void> {
  const sql = getSql();
  // CASCADE handles audio_detail child of catalog_entity.
  await sql.unsafe(
    "truncate audio_detail, entity_version, catalog_entity restart identity cascade",
  );
  await sql.unsafe("delete from audit_log where action like 'catalog.audio.%'");
}

export async function seedAudioTestUser(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${AUDIO_TEST_USER_ID}::uuid, 'audio-tests@example.com', 'audio tests', 'admin')
    on conflict (id) do nothing
  `;
  return AUDIO_TEST_USER_ID;
}

export interface SeedAudioOpts {
  slug: string;
  display_name: string;
  duration_ms?: number;
  sample_rate?: number;
  channels?: number;
  source_uri?: string;
  loudness_lufs?: number | null;
  peak_db?: number | null;
  assets_path?: string | null;
  retired?: boolean;
}

/** Seed an audio entity + audio_detail row directly (bypass routes). */
export async function seedAudioEntity(opts: SeedAudioOpts): Promise<string> {
  const sql = getSql();
  const inserted = (await sql`
    insert into catalog_entity (kind, slug, display_name, retired_at)
    values ('audio', ${opts.slug}, ${opts.display_name}, ${opts.retired ? sql`now()` : null})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const idStr = inserted[0]!.id;
  const idNum = Number.parseInt(idStr, 10);
  await sql`
    insert into audio_detail (
      entity_id, source_uri, assets_path, duration_ms, sample_rate, channels,
      loudness_lufs, peak_db, fingerprint
    ) values (
      ${idNum},
      ${opts.source_uri ?? `gen://run-${opts.slug}/0`},
      ${opts.assets_path ?? null},
      ${opts.duration_ms ?? 1000},
      ${opts.sample_rate ?? 48000},
      ${opts.channels ?? 2},
      ${opts.loudness_lufs ?? null},
      ${opts.peak_db ?? null},
      ${`fp_${opts.slug}`}
    )
  `;
  return idStr;
}

export async function invokeAudioRoute(
  handler: RouteHandler,
  method: "GET" | "POST" | "PATCH" | "DELETE",
  url: string,
  init?: { body?: unknown; headers?: Record<string, string>; params?: Record<string, string> },
): Promise<Response> {
  const reqInit: { method: string; body?: string; headers?: Record<string, string> } = { method };
  const headers: Record<string, string> = { ...(init?.headers ?? {}) };
  if (init?.body !== undefined) {
    reqInit.body = JSON.stringify(init.body);
    headers["content-type"] = "application/json";
  }
  if (Object.keys(headers).length > 0) reqInit.headers = headers;
  const req = new NextRequest(new URL(url, "http://localhost"), reqInit);
  const ctx = init?.params ? { params: Promise.resolve(init.params) } : undefined;
  return handler(req, ctx);
}
