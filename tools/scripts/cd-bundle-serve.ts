/**
 * Tiny static HTTP server for CD bundle Playwright extraction.
 *
 * The cd-bundle HTML loads sibling `.jsx` files via in-browser Babel +
 * XMLHttpRequest. Under `file://` Chromium blocks those XHRs as cross-origin,
 * leaving the DOM empty — the extractors then walk zero nodes. Routing through
 * a localhost loopback server gives us an `http://` origin with no CORS gate.
 *
 * This module exports `serveBundle(dir)` which binds an ephemeral port and
 * returns `{ url, close }`. The caller composes the final URL via
 * `${url}/<encoded-html-filename>`.
 *
 * Read-only: serves files under the supplied root, refuses path traversal.
 *
 * @packageDocumentation
 */
import * as fs from 'node:fs';
import * as http from 'node:http';
import * as path from 'node:path';
import { URL } from 'node:url';

const MIME: Record<string, string> = {
  '.html': 'text/html; charset=utf-8',
  '.htm': 'text/html; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.mjs': 'application/javascript; charset=utf-8',
  '.jsx': 'application/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.ttf': 'font/ttf',
  '.otf': 'font/otf',
};

export interface ServedBundle {
  /** Base URL — `http://127.0.0.1:<port>` (no trailing slash). */
  url: string;
  /** Stop the server and free the port. */
  close: () => Promise<void>;
}

export async function serveBundle(rootDir: string): Promise<ServedBundle> {
  const rootAbs = path.resolve(rootDir);
  if (!fs.existsSync(rootAbs)) {
    throw new Error(`bundle_root_not_found: ${rootAbs}`);
  }

  const server = http.createServer((req, res) => {
    try {
      const reqUrl = new URL(req.url ?? '/', 'http://x');
      const decoded = decodeURIComponent(reqUrl.pathname);
      const target = path.normalize(path.join(rootAbs, decoded));
      if (!target.startsWith(rootAbs)) {
        res.statusCode = 403;
        res.end('forbidden');
        return;
      }
      if (!fs.existsSync(target) || !fs.statSync(target).isFile()) {
        res.statusCode = 404;
        res.end('not_found');
        return;
      }
      const ext = path.extname(target).toLowerCase();
      res.setHeader('Content-Type', MIME[ext] ?? 'application/octet-stream');
      res.setHeader('Cache-Control', 'no-store');
      fs.createReadStream(target).pipe(res);
    } catch (e) {
      res.statusCode = 500;
      res.end(e instanceof Error ? e.message : String(e));
    }
  });

  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
  const addr = server.address();
  if (!addr || typeof addr === 'string') {
    server.close();
    throw new Error('serve_bundle_no_port');
  }
  const url = `http://127.0.0.1:${addr.port}`;
  return {
    url,
    close: () =>
      new Promise<void>((resolve, reject) => {
        server.close((err) => (err ? reject(err) : resolve()));
      }),
  };
}
