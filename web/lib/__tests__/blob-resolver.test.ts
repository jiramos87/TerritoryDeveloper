import { describe, it, expect, beforeEach, afterEach } from "vitest";
import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";
import {
  BlobResolver,
  UnsupportedSchemeError,
  MalformedBlobUriError,
} from "../blob-resolver";

describe("BlobResolver (TECH-1435)", () => {
  let prevBlobRoot: string | undefined;

  beforeEach(() => {
    prevBlobRoot = process.env.BLOB_ROOT;
  });

  afterEach(() => {
    if (prevBlobRoot === undefined) {
      delete process.env.BLOB_ROOT;
    } else {
      process.env.BLOB_ROOT = prevBlobRoot;
    }
  });

  it("test_resolves_gen_uri_to_default_root — joins blob root with run id and variant suffix", () => {
    delete process.env.BLOB_ROOT;
    const resolver = new BlobResolver();
    const result = resolver.resolve("gen://run123/2");
    expect(result.endsWith(path.join("run123", "2.png"))).toBe(true);
    expect(path.isAbsolute(result)).toBe(true);
  });

  it("test_resolves_gen_uri_with_blob_root_env — env override anchors path", () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "blobroot-"));
    process.env.BLOB_ROOT = tmp;
    try {
      const resolver = new BlobResolver();
      const result = resolver.resolve("gen://abc/0");
      expect(result).toBe(path.join(tmp, "abc", "0.png"));
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it("test_resolves_gen_uri_with_explicit_constructor_arg", () => {
    const resolver = new BlobResolver({ blobRoot: "/custom/root" });
    expect(resolver.resolve("gen://r/4")).toBe("/custom/root/r/4.png");
  });

  it("test_rejects_non_gen_scheme — typed error", () => {
    const resolver = new BlobResolver({ blobRoot: "/x" });
    expect(() => resolver.resolve("s3://bucket/key")).toThrow(
      UnsupportedSchemeError,
    );
  });

  it("test_rejects_malformed_gen_uri", () => {
    const resolver = new BlobResolver({ blobRoot: "/x" });
    expect(() => resolver.resolve("gen://only-run-id")).toThrow(
      MalformedBlobUriError,
    );
    expect(() => resolver.resolve("gen://run/notanumber")).toThrow(
      MalformedBlobUriError,
    );
  });

  it("test_read_returns_file_bytes", async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "blobroot-read-"));
    process.env.BLOB_ROOT = tmp;
    try {
      const runDir = path.join(tmp, "run42");
      fs.mkdirSync(runDir, { recursive: true });
      const filePath = path.join(runDir, "0.png");
      fs.writeFileSync(filePath, Buffer.from([0x89, 0x50, 0x4e, 0x47]));
      const resolver = new BlobResolver();
      const data = await resolver.read("gen://run42/0");
      expect(data.equals(Buffer.from([0x89, 0x50, 0x4e, 0x47]))).toBe(true);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });

  it("test_from_env_factory_mirrors_constructor", () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "blobroot-fromenv-"));
    process.env.BLOB_ROOT = tmp;
    try {
      const resolver = BlobResolver.fromEnv();
      expect(resolver.blobRoot).toBe(tmp);
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});
