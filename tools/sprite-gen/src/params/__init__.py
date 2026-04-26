"""params — Pydantic v2 schemas + ui_hints sidecar (TECH-1434).

Public surface re-exports the typed body models so the FastAPI service
(`src.serve`) can validate `/render` and `/promote` payloads without
depending on the internal module layout.
"""

from __future__ import annotations

from .schema import PromoteParams, RenderParams

__all__ = ["RenderParams", "PromoteParams"]
