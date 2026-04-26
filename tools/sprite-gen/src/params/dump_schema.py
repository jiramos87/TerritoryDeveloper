"""dump_schema.py — JSON Schema dump helper for the params package.

Invocation:
    python -m src.params.dump_schema

Emits a single JSON object on stdout:
    {
      "render":  <RenderParams JSON Schema>,
      "promote": <PromoteParams JSON Schema>
    }

Consumed by `tools/scripts/validate-sprite-gen-schema.ts` (TECH-1434) which
asserts every Pydantic field path is covered by `ui_hints.json` and vice
versa.
"""

from __future__ import annotations

import json
import sys

from .schema import PromoteParams, RenderParams


def main() -> int:
    payload = {
        "render": RenderParams.model_json_schema(),
        "promote": PromoteParams.model_json_schema(),
    }
    json.dump(payload, sys.stdout, indent=2)
    sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
