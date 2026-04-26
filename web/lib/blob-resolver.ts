/**
 * blob-resolver.ts — TS half of the canonical `gen://` BlobResolver
 * (TECH-1435). Single swap point per DEC-A25; future hosted blob stores plug
 * in behind the same `resolve()` / `read()` signatures via `BLOB_ROOT` env.
 *
 * URI grammar (MVP):
 *     gen://{run_id}/{variant_idx}
 *
 * Resolution:
 *     {blob_root}/{run_id}/{variant_idx}.png
 *
 * The Python sibling `tools/sprite-gen/src/blob_resolver.py` mirrors this
 * contract one-for-one so a single env-var flip moves both halves to a
 * hosted store later.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

/** Thrown when the URI scheme is not `gen://`. */
export class UnsupportedSchemeError extends Error {
  constructor(uri: string) {
    super(`unsupported blob URI scheme: ${uri}`);
    this.name = "UnsupportedSchemeError";
  }
}

/** Thrown when the URI body fails to parse into `{run_id, variant_idx}`. */
export class MalformedBlobUriError extends Error {
  constructor(uri: string) {
    super(`malformed gen:// URI: ${uri}`);
    this.name = "MalformedBlobUriError";
  }
}

const _GEN_URI_RE = /^gen:\/\/([A-Za-z0-9_-]+)\/(\d+)$/;

/**
 * Resolve the repo root from this file (`web/lib/blob-resolver.ts`) so the
 * default `var/blobs/` root is cwd-independent.
 */
function _defaultBlobRoot(): string {
  const here = path.dirname(fileURLToPath(import.meta.url));
  // web/lib/ → web/ → repo-root.
  return path.resolve(here, "..", "..", "var", "blobs");
}

export interface BlobResolverOptions {
  /** Override the blob root; falls back to `BLOB_ROOT` env, then `var/blobs/`. */
  blobRoot?: string;
}

export class BlobResolver {
  readonly blobRoot: string;

  constructor(options: BlobResolverOptions = {}) {
    this.blobRoot =
      options.blobRoot ?? process.env.BLOB_ROOT ?? _defaultBlobRoot();
  }

  /** Convenience factory mirroring future env-driven config layers. */
  static fromEnv(): BlobResolver {
    return new BlobResolver();
  }

  /**
   * Resolve a `gen://` URI to an absolute on-disk path.
   *
   * @throws UnsupportedSchemeError when scheme is not `gen://`
   * @throws MalformedBlobUriError when the URI body cannot be parsed
   */
  resolve(uri: string): string {
    if (!uri.startsWith("gen://")) {
      throw new UnsupportedSchemeError(uri);
    }
    const match = _GEN_URI_RE.exec(uri);
    if (!match) {
      throw new MalformedBlobUriError(uri);
    }
    const [, runId, variantIdx] = match;
    return path.join(this.blobRoot, runId, `${variantIdx}.png`);
  }

  /** Read the bytes of a `gen://` URI; rejects when the file is absent. */
  async read(uri: string): Promise<Buffer> {
    const target = this.resolve(uri);
    return fs.promises.readFile(target);
  }
}
