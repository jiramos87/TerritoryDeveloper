"""test_aseprite_io.py — TECH-182 layered emission tests."""

from __future__ import annotations

import contextlib
import io
from pathlib import Path

import pytest
from PIL import Image

pytest.importorskip("aseprite")

from src import aseprite_io as io_mod
from src import cli as _cli
from src.aseprite_io import layer_order, write_layered_aseprite


def _read_layer_names(path: Path) -> list[str]:
    from aseprite import AsepriteFile

    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        ase = AsepriteFile(path.read_bytes())
    return [layer.name for layer in ase.layers]


def test_layer_order_flat() -> None:
    assert layer_order(False) == ("east", "south", "top")


def test_layer_order_foundation() -> None:
    assert layer_order(True) == ("foundation", "east", "south", "top")


def test_layered_flat_terrain(tmp_path: Path) -> None:
    layers = {
        "top": Image.new("RGBA", (64, 64), (0, 0, 0, 0)),
        "south": Image.new("RGBA", (64, 64), (0, 0, 0, 0)),
        "east": Image.new("RGBA", (64, 64), (0, 0, 0, 0)),
    }
    dest = tmp_path / "flat.aseprite"
    write_layered_aseprite(dest, layers, (64, 64))
    names = _read_layer_names(dest)
    assert names == ["east", "south", "top"]


def test_layered_with_foundation(tmp_path: Path) -> None:
    layers = {
        "foundation": Image.new("RGBA", (64, 80), (0, 0, 0, 0)),
        "top": Image.new("RGBA", (64, 80), (0, 0, 0, 0)),
        "south": Image.new("RGBA", (64, 80), (0, 0, 0, 0)),
        "east": Image.new("RGBA", (64, 80), (0, 0, 0, 0)),
    }
    dest = tmp_path / "sloped.aseprite"
    write_layered_aseprite(dest, layers, (64, 80))
    names = _read_layer_names(dest)
    assert names == ["foundation", "east", "south", "top"]


def test_flat_png_co_emit(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.setattr(_cli, "_OUT_DIR", tmp_path)
    rc = _cli.main(["render", "building_residential_small", "--layered"])
    assert rc == 0
    assert list(tmp_path.glob("*.png"))
    assert list(tmp_path.glob("*.aseprite"))


def test_layer_alpha_preserved(tmp_path: Path) -> None:
    top = Image.new("RGBA", (4, 4), (255, 0, 0, 255))
    south = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
    east = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
    dest = tmp_path / "alpha.aseprite"
    write_layered_aseprite(dest, {"top": top, "south": south, "east": east}, (4, 4))
    from aseprite import AsepriteFile

    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        ase = AsepriteFile(dest.read_bytes())
    assert len(ase.layers) == 3


def test_no_layered_flag_default(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.setattr(_cli, "_OUT_DIR", tmp_path)
    rc = _cli.main(["render", "building_residential_small"])
    assert rc == 0
    assert not list(tmp_path.glob("*.aseprite"))


def test_atomic_write_failure(tmp_path: Path, monkeypatch) -> None:
    def boom_compress(*a, **k):
        raise RuntimeError("boom")

    monkeypatch.setattr(io_mod.zlib, "compress", boom_compress)
    layers = {
        "top": Image.new("RGBA", (4, 4), (0, 0, 0, 0)),
        "south": Image.new("RGBA", (4, 4), (0, 0, 0, 0)),
        "east": Image.new("RGBA", (4, 4), (0, 0, 0, 0)),
    }
    dest = tmp_path / "bad.aseprite"
    with pytest.raises(RuntimeError):
        write_layered_aseprite(dest, layers, (4, 4))
    assert not dest.exists()
    assert not dest.with_suffix(".tmp.aseprite").exists()
