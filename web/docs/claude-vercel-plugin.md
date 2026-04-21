# Claude Code — Vercel marketplace plugin (off in this repo)

## Why it is disabled

The official **vercel@claude-plugins-official** plugin ships many agent skills. Listing them in every fresh Claude Code session costs a large token block for little benefit in a Unity-first repo. This workspace turns that plugin **off** in `.claude/settings.json` via `enabledPlugins`.

**Caveman** stays on: the same file sets `caveman@caveman` to `true` so project-level plugin config does not drop your caveman marketplace entry if your host merges settings by replacement.

## Re-enable for yourself (local only)

Use gitignored `.claude/settings.local.json` at the repo root:

```json
{
  "enabledPlugins": {
    "vercel@claude-plugins-official": true
  }
}
```

If skills still do not appear, confirm the plugin is installed for your Claude Code profile (`claude plugin list`).

## Deploy and CLI without the plugin

- Production / preview from repo root: `npm run deploy:web`, `npm run deploy:web:preview` (see `tools/scripts/vercel-deploy.sh`).
- This app’s hosting notes: `web/README.md` (Vercel, env vars, dashboard).

## Official docs

- [Claude Code plugins](https://docs.anthropic.com/en/docs/claude-code/plugins-reference)
- [Vercel docs](https://vercel.com/docs)

## Skill topics in the plugin (for search / manual read)

Bundled skill folders in **vercel@claude-plugins-official** (names only — open the plugin cache under `~/.claude/plugins/cache/claude-plugins-official/vercel/<version>/skills/` after install):

| Skill folder | Typical use |
| --- | --- |
| `ai-gateway` | AI Gateway routing / providers |
| `ai-sdk` | Vercel AI SDK |
| `auth` | Auth vendors on Vercel |
| `bootstrap` | Repo bootstrap with Vercel-linked resources |
| `chat-sdk` | Multi-platform chat bots |
| `deployments-cicd` | Deploy, promote, CI |
| `env-vars` | Environment variables |
| `knowledge-update` | Platform product notes |
| `marketplace` | Vercel Marketplace integrations |
| `next-cache-components` | Next.js cache / PPR patterns |
| `next-forge` | next-forge monorepo starter |
| `next-upgrade` | Next.js version upgrades |
| `nextjs` | Next.js App Router |
| `react-best-practices` | React / TSX review checklist |
| `routing-middleware` | Routing middleware |
| `runtime-cache` | Runtime Cache API |
| `shadcn` | shadcn/ui |
| `turbopack` | Turbopack |
| `vercel-agent` | Vercel Agent product |
| `vercel-cli` | Vercel CLI |
| `vercel-functions` | Functions / Edge / Cron |
| `vercel-sandbox` | Vercel Sandbox |
| `vercel-storage` | Blob, Edge Config, marketplace storage |
| `verification` | End-to-end product verification |
| `workflow` | Workflow DevKit |

Version in the path changes with plugin updates; the table is a stable index of topics.
