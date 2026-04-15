"""One-off script to generate tests/fixtures/palette_smoke.png.

Run: python tests/create_palette_fixture.py
(8x8 RGBA PNG with 3 solid colours + transparent region)
"""
import numpy as np
from PIL import Image
from pathlib import Path

RED   = (200,  50,  50, 255)
GREEN = ( 50, 180,  50, 255)
BLUE  = ( 50,  50, 200, 255)
TRANS = (  0,   0,   0,   0)

arr = np.zeros((8, 8, 4), dtype=np.uint8)
# rows 0-1: red, rows 2-3: green, rows 4-5: blue, rows 6-7: transparent
for r in range(2):
    arr[r, :] = RED
for r in range(2, 4):
    arr[r, :] = GREEN
for r in range(4, 6):
    arr[r, :] = BLUE
# rows 6-7 stay zero (transparent)

img = Image.fromarray(arr, mode="RGBA")
out = Path(__file__).parent / "fixtures" / "palette_smoke.png"
img.save(out)
print(f"Saved {out}")
