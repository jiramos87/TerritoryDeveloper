---
name: fixture-below-floor-agent
description: Fixture — agent body with cache_control block BELOW F2 floor (tiny @-load).
tools: Read
model: opus
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@tools/scripts/test-fixtures/cache-block-sizing/tiny-content.md

# Mission

Below-floor fixture. @-loaded content is tiny — below Opus 4.7 F2 floor (4,096 tok = 16,384 bytes).
