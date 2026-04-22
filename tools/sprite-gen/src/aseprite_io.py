"""Layered .aseprite writer (TECH-182) — minimal ASE binary (magic 0xA5E0)."""

from __future__ import annotations

import struct
import zlib
from pathlib import Path
from typing import Mapping

from PIL import Image

_LAYER_ORDER_NON_FLAT = ("foundation", "east", "south", "top")
_LAYER_ORDER_FLAT = ("east", "south", "top")


def layer_order(has_foundation: bool) -> tuple[str, ...]:
    return _LAYER_ORDER_NON_FLAT if has_foundation else _LAYER_ORDER_FLAT


def _pack_string(s: str) -> bytes:
    raw = s.encode("utf-8")
    return struct.pack("<H", len(raw)) + raw


def _pack_header(file_size: int, width: int, height: int, num_frames: int = 1) -> bytes:
    # 128-byte header per aseprite.headers.Header
    return struct.pack(
        "<IHHHHHIH8xB3xHBBhhHH84x",
        file_size,
        0xA5E0,
        num_frames,
        width,
        height,
        32,
        1,
        0,
        0,
        0,
        1,
        1,
        0,
        0,
        0,
        0,
    )


def _pack_layer_chunk(layer_index: int, name: str) -> bytes:
    flags = 0
    layer_type = 0
    child_level = 0
    default_w = 0
    default_h = 0
    blend_mode = 0
    opacity = 255
    body = struct.pack(
        "<HHHHHHB",
        flags,
        layer_type,
        child_level,
        default_w,
        default_h,
        blend_mode,
        opacity,
    )
    body += b"\x00\x00\x00"
    body += _pack_string(name)
    chunk_size = 6 + len(body)
    return struct.pack("<IH", chunk_size, 0x2004) + body


def _rgba_to_bgra(img: Image.Image) -> bytes:
    rgba = img.convert("RGBA")
    raw = rgba.tobytes()
    out = bytearray(len(raw))
    for i in range(0, len(raw), 4):
        r, g, b, a = raw[i], raw[i + 1], raw[i + 2], raw[i + 3]
        out[i : i + 4] = bytes((b, g, r, a))
    return bytes(out)


def _pack_cel_chunk(layer_index: int, img: Image.Image) -> bytes:
    w, h = img.size
    bgra = _rgba_to_bgra(img)
    compressed = zlib.compress(bgra, 6)
    cel_head = struct.pack("<HhhBH7x", layer_index, 0, 0, 255, 2)
    cel_rest = struct.pack("<HH", w, h) + compressed
    body = cel_head + cel_rest
    chunk_size = 6 + len(body)
    return struct.pack("<IH", chunk_size, 0x2005) + body


def _build_frame(width: int, height: int, ordered_layers: list[tuple[str, Image.Image]]) -> bytes:
    chunks = b""
    for i, (name, _) in enumerate(ordered_layers):
        chunks += _pack_layer_chunk(i, name)
    for i, (_, im) in enumerate(ordered_layers):
        chunks += _pack_cel_chunk(i, im)
    num_chunks = len(ordered_layers) * 2
    frame_size = 16 + len(chunks)
    frame_header = struct.pack("<IHHH6x", frame_size, 0xF1FA, num_chunks, 100)
    return frame_header + chunks


def write_layered_aseprite(
    dest_path: str | Path,
    layers: Mapping[str, Image.Image],
    canvas_size: tuple[int, int],
) -> Path:
    """Write *layers* to `{dest_path}`; atomic via `.tmp` + replace."""
    dest_path = Path(dest_path)
    w, h = canvas_size
    has_foundation = "foundation" in layers
    order = layer_order(has_foundation)
    ordered: list[tuple[str, Image.Image]] = [(n, layers[n]) for n in order]

    tmp_path = dest_path.with_suffix(".tmp.aseprite")
    try:
        frame_data = _build_frame(w, h, ordered)
        file_size = 128 + len(frame_data)
        header = _pack_header(file_size, w, h, 1)
        blob = header + frame_data
        tmp_path.write_bytes(blob)
        tmp_path.replace(dest_path)
    except Exception:
        if tmp_path.exists():
            tmp_path.unlink()
        if dest_path.exists():
            dest_path.unlink()
        raise
    return dest_path
