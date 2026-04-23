"""Dim-kwargs normalizer for pixel-native Stage 6 primitives (DAS §2, R2)."""

from __future__ import annotations

import warnings

_TILE_PX = 32  # DAS §2.1 universal diamond span


def normalize_dims(
    *,
    w: float | None = None,
    d: float | None = None,
    h: float | None = None,
    w_px: int | None = None,
    d_px: int | None = None,
    h_px: int | None = None,
    prim: str = "iso_*",
) -> tuple[int, int, int]:
    """Resolve mixed tile / pixel dimensions to pixel triple."""

    def pick(
        tile: float | None,
        px: int | None,
        name: str,
        *,
        tile_to_px: bool,
    ) -> int:
        if px is not None and tile is not None:
            warnings.warn(
                f"{prim}: both {name} and {name}_px passed; using {name}_px={px}",
                DeprecationWarning,
                stacklevel=3,
            )
            return int(px)
        if px is not None:
            return int(px)
        if tile is not None:
            if not tile_to_px:
                return int(tile)
            return int(float(tile) * _TILE_PX)
        raise TypeError(f"{prim}: {name} or {name}_px required")

    w_out = pick(w, w_px, "w", tile_to_px=True)
    d_out = pick(d, d_px, "d", tile_to_px=True)
    h_out = pick(h, h_px, "h", tile_to_px=False)
    return w_out, d_out, h_out
