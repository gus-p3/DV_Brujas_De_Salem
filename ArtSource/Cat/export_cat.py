"""
Exporta las animaciones del gato (hoja aprobada ArtSource/Cat/sheets/cat_all_chatgpt.png) a
Assets/Art/Characters/CatSprites/<animación>_NN.png con el pivote en las patas.

Primero se corta la hoja:
  python ArtSource/Villagers/slice_sheet.py ArtSource/Cat/sheets/cat_all_chatgpt.png ArtSource/Cat/cut/cat --cols 8 --rows 7 --cell 460 300
Después:
  python ArtSource/Cat/export_cat.py
"""
from pathlib import Path

import numpy as np
from PIL import Image

CUT = Path("ArtSource/Cat/cut")
OUT = Path("Assets/Art/Characters/CatSprites")
PPU = 128
CANVAS = (300, 200)
PIVOT_Y = 8
STANDING_HEIGHT_UNITS = 0.62  # ~40 % de la bruja (1.6 u)

# Fotogramas (1-based, en orden de lectura de la hoja) por animación
ANIMS = {
    "idle": range(1, 9),
    "walk": range(9, 17),
    "sneak": range(17, 25),
    "rise": [27, 28],
    "fall": [29],
    "land": [30, 31],
    "sit": range(34, 41),
}


# La fila de salto de la hoja se dibujó más pequeña que el resto: se compensa
ROW_SCALE = {"rise": 1.2, "fall": 1.2, "land": 1.2}


def frame(i):
    return Image.open(CUT / f"cat_{i - 1:02d}.png").convert("RGBA")


def height(img):
    ys = np.nonzero(np.asarray(img)[..., 3] > 8)[0]
    return ys.max() - ys.min()


def despill(img):
    a = np.asarray(img).astype(np.float32).copy()
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    spill = np.minimum(r, b) - g
    # El gato tiene brillos violeta legítimos: solo se neutraliza el halo fuerte del borde
    m = (spill > 60) & (a[..., 3] > 0) & (a[..., 3] < 250)
    a[..., 0][m] = np.maximum(g[m], r[m] - spill[m] * 0.85)
    a[..., 2][m] = np.maximum(g[m], b[m] - spill[m] * 0.85)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def place(img, scale):
    w, h = img.size
    s = img.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS)
    feet_y = (h - 20) * scale  # slice_sheet deja las patas a 20 px de la base
    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    canvas.alpha_composite(s, (round(CANVAS[0] / 2 - w * scale / 2), round(CANVAS[1] - PIVOT_Y - feet_y)))
    return canvas


def main():
    idle_h = float(np.median([height(frame(i)) for i in ANIMS["idle"]]))
    scale = STANDING_HEIGHT_UNITS * PPU / idle_h
    OUT.mkdir(parents=True, exist_ok=True)
    for anim, frames in ANIMS.items():
        for n, i in enumerate(frames):
            despill(place(frame(i), scale * ROW_SCALE.get(anim, 1.0))).save(OUT / f"{anim}_{n:02d}.png")
    print(f"escala {scale:.3f}; exportadas {sum(len(list(f)) for f in ANIMS.values())} imágenes")


if __name__ == "__main__":
    main()
