"""_dispatch.py — Primitive dispatch tables for the compose layer."""

from __future__ import annotations

from ..primitives import (
    iso_bush,
    iso_cube,
    iso_fence,
    iso_grass_tuft,
    iso_ground_diamond,
    iso_ground_noise,
    iso_path,
    iso_pavement_patch,
    iso_pool,
    iso_prism,
    iso_stepped_foundation,
    iso_tree_deciduous,
    iso_tree_fir,
)

_DISPATCH: dict[str, object] = {
    "iso_cube": iso_cube,
    "iso_ground_diamond": iso_ground_diamond,
    "iso_ground_noise": iso_ground_noise,
    "iso_prism": iso_prism,
    "iso_stepped_foundation": iso_stepped_foundation,
}

_DECORATION_DISPATCH: dict[str, object] = {
    "iso_bush": iso_bush,
    "iso_fence": iso_fence,
    "iso_grass_tuft": iso_grass_tuft,
    "iso_path": iso_path,
    "iso_pavement_patch": iso_pavement_patch,
    "iso_pool": iso_pool,
    "iso_tree_deciduous": iso_tree_deciduous,
    "iso_tree_fir": iso_tree_fir,
}
