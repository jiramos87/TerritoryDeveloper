---
purpose: Operator runbook index for catalog operations + DR.
audience: operator
last_walkthrough: 2026-05-01
---

# Operator runbooks

Step-by-step copy-pasteable procedures for catalog ops + disaster recovery. Each runbook is walkthrough-tested on a clean checkout.

| Runbook | Purpose |
| --- | --- |
| [catalog-recovery.md](catalog-recovery.md) | Restore catalog DB + blob mirror from nightly backup; re-publish; smoke. |
| [catalog-publish-flow.md](catalog-publish-flow.md) | Draft → diff → publish → version bump → consumer ref refresh cycle. |
| [catalog-archetype-authoring.md](catalog-archetype-authoring.md) | Spawn new archetype via MCP → fill fields → wire refs → publish → consume. |
| [playmode-catalog-roundtrip-extension.md](playmode-catalog-roundtrip-extension.md) | Per-kind PlayMode roundtrip extension once runtime catalog ships. |

## Conventions

- `$REPO_ROOT` = `/Users/javier/bacayo-studio/territory-developer` (or whichever local clone).
- `$DRILL_DATE` = today UTC (`date -u +%Y-%m-%d`).
- Each runbook records actual output snippets in `### Drift notes` if any command needed adjustment from spec.
- Update `last_walkthrough:` frontmatter on every walkthrough.

## Failure escalation

If any runbook step fails AND no recovery branch matches → file `BUG-` issue; cite runbook step number; copy the failing command + actual output verbatim.
