### Stage 5 — Unsigned packaging + `/download` publication + in-game notifier / `/download` web surface + latest.json

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land the Vercel `/download` page that lists artifacts + Gatekeeper/SmartScreen bypass steps, the `latest.json` manifest schema + asset, and the private-route disallow. Serve artifacts statically; set `Cache-Control: no-cache` on `latest.json` so testers see new versions without CDN lag.

**Exit:**

- `web/public/download/latest.json` matches Example B schema — `version`, `releasedAt`, `notes`, `downloads.{mac,win}.{url,size,sha256}`, `bypass.{mac,win}`.
- `web/app/download/page.tsx` Server Component reads `latest.json` at build time via `fs.readFile`, renders artifact table (platform, filename, size, SHA256) + bypass section anchors (`#gatekeeper`, `#smartscreen`).
- `web/content/pages/download.mdx` carries full-English bypass copy (Gatekeeper right-click-Open steps + SmartScreen "More info → Run anyway" steps) with inline screenshot slots — caveman-exception per `ia/rules/agent-output-caveman.md` §exceptions.
- `web/app/robots.ts` disallows `/download` — covered by an `if (private)` gate wired to a single env var or const so the MVP-ship flip is a one-liner.
- `web/vercel.json` `headers` config sets `Cache-Control: no-cache, must-revalidate` for `/download/latest.json`.
- `npm run validate:web` green; Vercel preview deploy via `npm run deploy:web:preview` loads `/download` correctly (with a placeholder `latest.json`).
- Phase 1 — Author latest.json schema + placeholder manifest committed.
- Phase 2 — Author `/download` page + MDX bypass copy + robots + cache header.
- Phase 3 — Preview-deploy validation.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | latest.json schema + placeholder | _pending_ | _pending_ | Author `web/public/download/latest.json` matching Design Expansion Example B verbatim. Seed with `version: "0.0.0-dev-placeholder"`, `releasedAt` = current UTC, `notes` = "Placeholder — not a shipped build.", `downloads.mac.url` + `downloads.win.url` pointing at `/download/` paths that will exist post-first-release, placeholder zeroed `size` + `sha256: "pending"`. Schema is the contract `UpdateNotifier` reads at Stage 2.3. |
| T5.2 | /download page RSC + artifact table | _pending_ | _pending_ | Author `web/app/download/page.tsx` Next.js Server Component: `const manifest = JSON.parse(await fs.readFile("web/public/download/latest.json", "utf8"))`, render version + releasedAt + notes heading, render a `<table>` row per platform (mac / win) with filename, size (formatted via existing `web/lib/` helper if present, else inline KB formatter), SHA256 (truncated 8+8), download link. Anchor links to `#gatekeeper` + `#smartscreen` bypass sections imported from `web/content/pages/download.mdx`. Backend-derives/frontend-renders pattern per `ia/rules/web-backend-logic.md`. |
| T5.3 | download.mdx bypass copy + robots + cache | _pending_ | _pending_ | Author `web/content/pages/download.mdx` with two sections: `## Gatekeeper (macOS)` step-by-step right-click-Open flow with screenshot placeholders, `## SmartScreen (Windows)` More-info-Run-anyway flow with screenshot placeholders — full English per caveman-exception. Edit `web/app/robots.ts` (create if missing) to `disallow: ["/download", "/download/*"]` gated on a `DOWNLOAD_PUBLIC` const default `false`. Edit `web/vercel.json` to add `{ "source": "/download/latest.json", "headers": [{ "key": "Cache-Control", "value": "no-cache, must-revalidate" }] }`. |
| T5.4 | Preview deploy + /download smoke | _pending_ | _pending_ | Run `npm run validate:web` + `npm run deploy:web:preview`. Load the preview `/download` URL — confirm artifact table renders from the placeholder manifest, bypass MDX renders, `curl -I {preview}/download/latest.json` shows `Cache-Control: no-cache`. Confirm Google prod site does NOT show `/download` (robots disallow). Note preview URL in the handoff for Stage 2.3 kickoff — the notifier fetches this URL during dev. |
