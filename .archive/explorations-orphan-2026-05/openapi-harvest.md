---
slug: openapi-harvest
target_version: 1
parent_plan_id: null
notes: "Framework-agnostic OpenAPI 3.1 spec generator wrapped as MCP tools. Pluggable adapter per backend shape (pure Node http, Express, Fastify, Hono, Next.js App Router). Deterministic AST + Zod/TS introspection covers shape (~80%); LLM fallback fills dynamic types and prose (descriptions, examples, summaries). Output feeds existing downstream pipelines (SDK gen via openapi-generator/orval → Pyodide-hosted SDK clients for in-browser endpoint linting). Cache by handler-source hash to skip re-LLM on unchanged code."
stages: []
tasks:
  - prefix: TECH
    depends_on: []
    digest_outline: "Placeholder — stages not yet authored."
    touched_paths: []
    kind: code
---

# OpenAPI-Harvest — exploration

Caveman-tech default per `ia/rules/agent-output-caveman.md`.

---

## 1. Problem

Pure Node and Next.js App Router backends lack the decorator metadata that Nest.js exposes for Swagger generation. Hand-maintained OpenAPI specs drift from actual handlers within days. Downstream consumers — generated SDK clients, Pyodide-hosted in-browser editors that lint endpoint usage, public API docs — break silently or stay stale.

Goal: a pluggable harvest layer that reads handler source as the source of truth, infers OpenAPI 3.1 deterministically where possible, falls back to LLM only for prose and dynamic-type gaps, and exposes the result as MCP tools so any agent or build step can fetch a fresh spec.

Reuse pattern: existing pipeline already does `OpenAPI → openapi-generator → Python SDK → Pyodide → endpoint-method linter`. New layer keeps OpenAPI accurate without manual decorators.

---

## 2. Scope

- In: framework adapters (Express, Fastify, Hono, Next.js App Router, pure Node http); AST-based route discovery; Zod/TS schema inference; LLM fallback for descriptions/examples/dynamic shapes; OpenAPI 3.1 emitter; diff tool; MCP tool wrappers.
- Out: GraphQL, gRPC, tRPC (separate skill); runtime-only frameworks without AST source; non-JS/TS backends.
- Non-goals: replacing existing decorator-based generators (Nest swagger module) where they already work; editor UI; SDK generation itself (downstream concern).

---

## 3. Adapter shape

```ts
interface OpenApiAdapter {
  framework: "express" | "fastify" | "hono" | "next-app-router" | "node-http";
  scanRoutes(rootDir: string): Promise<RouteCandidate[]>;
  extractHandler(route: RouteCandidate): HandlerSource;
  inferRequestSchema(handler: HandlerSource): Schema | "needs-llm";
  inferResponseSchema(handler: HandlerSource): Schema | "needs-llm";
}

interface RouteCandidate {
  method: "GET" | "POST" | ... ;
  path: string;
  handlerFile: string;
  handlerSymbol: string;
  middlewares: string[];
}
```

Add a framework → drop a new adapter implementing the interface. Core engine stays untouched.

---

## 4. Per-framework discovery strategy

| Framework | Source of truth | Strategy |
|---|---|---|
| Next.js App Router | filesystem (`app/**/route.ts` exports) | walk dir, parse named exports (`GET`, `POST`...), read params from path segments |
| Express | imperative chain (`app.get(path, handler)`) | AST scan for `app.<method>(...)` and `router.<method>(...)` calls |
| Fastify | declarative routes object | AST scan for `fastify.route({...})` and shorthand methods |
| Hono | builder pattern (`new Hono().get(path, handler)`) | AST scan for chain of method calls on Hono instances |
| Pure Node http | manual `req.url` switch | JSDoc-tag fallback (`@route GET /path`) — no AST inference reliable |

Each adapter ships a fixture suite (~10 representative shapes) so regression-testing is mechanical.

---

## 5. Schema inference layers

| Tier | Cost | What it does | Coverage |
|---|---|---|---|
| Zod/Yup/Valibot introspection | cheap | call `.toJSON()` on schema where library exposes it | ~95% accurate when schema present |
| TS type extraction | cheap | use `ts-morph` or `typescript` compiler API to read parameter types | ~80% accurate, breaks on generics + `any` |
| Runtime probe | medium | spin handler in test harness, send sample request, capture shape | brittle, side-effect risk |
| LLM fallback | expensive | feed handler source to LLM, ask for OpenAPI fragment | fills `any`/dynamic gaps, generates prose |

Pipeline: try cheap → medium → LLM in order. Stop at first acceptable result.

---

## 6. MCP tool surface

| Tool | Purpose |
|---|---|
| `openapi_scan_routes(framework, rootDir?)` | enumerate route candidates |
| `openapi_extract_handler(routeId)` | pull handler source + signature |
| `openapi_infer_schema(handlerSource, kind)` | request or response schema |
| `openapi_describe(endpoint)` | LLM-generated summary/description/examples |
| `openapi_render_spec(routes[])` | assemble OpenAPI 3.1 JSON |
| `openapi_diff(prev, curr)` | breaking vs non-breaking changes |
| `openapi_validate(spec)` | run against OpenAPI 3.1 schema |

Agents can call these in any order. CI calls `scan → render → validate` end-to-end.

---

## 7. Cache strategy

- Key: SHA-256 of handler source + adapter version + LLM model id.
- Store: SQLite under `.openapi-harvest/cache.db` (single-file, repo-local).
- Hit → skip LLM call, reuse last output.
- Miss → re-infer + persist.
- Invalidate on adapter version bump or model swap.

Without cache, LLM tier dominates cost on monorepos. With cache, only changed handlers re-run.

---

## 8. Comparison axes (for design-explore Phase 1)

| Axis | Option A — One spec for whole repo | Option B — One spec per route group | Option C — Hierarchical (root + tagged subsets) |
|---|---|---|---|
| Tooling fit | universal | better for microservices | flexible |
| Diff noise | high on big repos | low | medium |

| Axis | Option A — AST only | Option B — AST + LLM fallback | Option C — LLM first, AST verification |
|---|---|---|---|
| Cost | low | medium | high |
| Coverage | 80% | 98% | 99% |
| Determinism | high | medium | low |

| Axis | Option A — Build-time only | Option B — On-demand via MCP | Option C — Both (build artifact + live tool) |
|---|---|---|---|
| Freshness | stale between builds | live | live + cached |
| CI complexity | low | medium | medium |
| Best fit | static docs site | agent-driven workflows | most projects |

---

## 9. Pipeline tie-in (downstream)

Existing flow Javier already runs:

```
OpenAPI 3.1 spec
  → openapi-generator (or orval)
    → Python SDK
      → Pyodide bundle
        → in-browser code editor
          → endpoint methods exposed as linting hints
```

This skill replaces the **first arrow's input freshness problem** — spec stays in sync with handler source automatically. Downstream stages unchanged.

---

## 10. Open questions

- **Schema library coverage.** Zod first; do we ship Yup/Valibot/io-ts adapters in v1 or stub them? Lean: Zod only v1.
- **TS generics.** Inference breaks on `Response<T>` patterns. LLM fallback or manual annotation? Lean: annotation hint via JSDoc `@returns {Schema}` first, LLM second.
- **Authentication schemes.** OpenAPI `securitySchemes` rarely inferable from source. Manual config block in adapter? Lean: yes, `.openapi-harvest.json` with security definitions.
- **Versioning.** Spec version (1.0.0 → 1.1.0) — auto-bump from diff classification or manual? Lean: tool emits suggestion, human commits.
- **Streaming responses.** SSE / chunked responses — OpenAPI has limited shape for this. Defer to v2.
- **Examples generation.** LLM-generated examples can hallucinate; require fixture seed? Lean: require at least one real example per endpoint, LLM extrapolates from there.

---

## 11. MVP scope estimate

- Adapter interface + Express adapter (most common): 2 days
- AST route scanner + Zod introspection: 2 days
- OpenAPI 3.1 emitter + validator: 1 day
- MCP tool wrappers: 1 day
- Cache layer (SQLite): 1 day

Total MVP (one framework, AST-only, no LLM): ~1 week.
Add Next.js App Router + Fastify adapters: +1 week.
Add LLM fallback tier + describe tool + cache integration: +1 week.
Add diff tool + breaking-change classifier: +3 days.

Full v1 (3 frameworks + LLM + cache + diff): ~3-4 weeks.

---

## 12. Non-goals

- Replacing Nest.js Swagger module where it already produces good output.
- Generating SDKs — downstream tools (`openapi-generator`, `orval`) own that.
- UI for spec editing — output is JSON, humans use existing OpenAPI editors.
- Runtime hot-reload of spec — build/CI step or on-demand MCP call only.
- Cross-language backends (Go, Rust, Python) — separate adapter family if needed later.
