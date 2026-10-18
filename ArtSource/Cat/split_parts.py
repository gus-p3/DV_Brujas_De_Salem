"""
Separa la hoja de piezas del gato (cat_parts_chatgpt.png) en un PNG por pieza del rig.
Ejecutar desde la raíz del proyecto:  python ArtSource/Cat/split_parts.py
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageOps
from scipy import ndimage

sys.path.insert(0, ".")
import gemini_art as g  # noqa: E402

SRC = Path("ArtSource/Cat/cat_parts_chatgpt.png")
OUT = Path("ArtSource/Cat/parts")

# Centro aproximado (x, y) de cada pieza en la hoja de origen -> nombre en el rig.
# Una pieza puede reunir varios componentes (los bigotes son tres trazos sueltos).
CENTERS = {
    "head": [(167, 189)],
    "eye_open": [(387, 176)], "eye_half": [(522, 177)], "eye_closed": [(654, 177)],
    "ear_front": [(828, 169)], "ear_back": [(998, 166)],
    "whiskers": [(1172, 180), (1176, 199), (1178, 218)],
    "body": [(481, 458)],
    "tail_base": [(813, 483)], "tail_mid": [(1004, 474)], "tail_tip": [(1188, 452)],
    "fleg_upper_front": [(223, 768)], "fleg_lower_front": [(438, 791)],
    "fleg_upper_back": [(665, 771)], "fleg_lower_back": [(878, 780)],
    "hleg_upper_front": [(243, 1027)], "hleg_lower_front": [(464, 1044)],
    "hleg_upper_back": [(738, 1030)], "hleg_lower_back": [(955, 1056)],
}

# La cola está dibujada hacia la derecha; el gato mira a la derecha, así que la cola debe salir hacia atrás
MIRROR = {"tail_base", "tail_mid", "tail_tip"}


def main():
    img = g.key_background(Image.open(SRC), g.CHROMA, 90)
    img.save(SRC.with_name("cat_parts_keyed.png"))
    arr = np.asarray(img)
    alpha = arr[..., 3] > 8
    labels, _ = ndimage.label(ndimage.binary_dilation(alpha, iterations=3))
    ys, xs = np.nonzero(labels)
    OUT.mkdir(parents=True, exist_ok=True)

    for name, centers in CENTERS.items():
        ids = set()
        for cx, cy in centers:
            k = np.argmin((xs - cx) ** 2 + (ys - cy) ** 2)
            ids.add(labels[ys[k], xs[k]])
        mask_all = np.isin(labels, list(ids))
        rows, cols = np.nonzero(mask_all)
        sl = (slice(rows.min(), rows.max() + 1), slice(cols.min(), cols.max() + 1))
        crop = arr[sl].copy()
        crop[..., 3] = np.where(mask_all[sl] & alpha[sl], crop[..., 3], 0)
        piece = g.trim(Image.fromarray(crop, "RGBA"), 2)
        if name in MIRROR:
            piece = ImageOps.mirror(piece)
        piece.save(OUT / f"{name}.png")
        print(f"{name:18s} {piece.size}")


if __name__ == "__main__":
    main()
