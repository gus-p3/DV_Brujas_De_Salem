"""
Exporta los fotogramas finales de los aldeanos a Assets/Art/Characters/Villagers/<personaje>/<animación>_NN.png.

- Usa los fotogramas ya cortados y alineados por slice_sheet.py (ArtSource/Villagers/cut).
- Selecciona los fotogramas buenos de cada hoja (descarta gestos de un solo fotograma y utilería que desaparece).
- Normaliza la escala entre hojas de distinta resolución usando un fotograma de referencia en pie.
- Lienzo fijo con los pies en el pivote (PIVOT_Y px desde abajo) y el torso centrado.

Ejecutar desde la raíz:  python ArtSource/Villagers/export_villagers.py
"""
from pathlib import Path

import numpy as np
from PIL import Image

FRAMES = Path("ArtSource/Villagers/cut")
OUT = Path("Assets/Art/Characters/Villagers")
PPU = 128
CANVAS = (420, 320)
PIVOT_Y = 8  # px desde abajo

# Altura en pie de cada personaje en unidades de mundo (la bruja mide ~1.6)
HEIGHT_UNITS = {"farmer": 1.85, "milkmaid": 1.75, "preacher": 2.0, "boy": 1.35}

# Por hoja: fotograma(s) de referencia en pie ("max" = el más alto) y factor si la referencia va agachada.
# Animaciones: nombre -> (hoja, fotogramas 1-based)
CHARACTERS = {
    "farmer": {
        "sheets": {"idle": ("median", 1.0), "walk": ("max", 1.0), "run": ("max", 1.04), "react": (1, 1.0)},
        "anims": {
            "idle": ("idle", [1, 2, 3, 4, 5, 6, 7, 10, 12]),
            "walk": ("walk", list(range(1, 13))),
            "run": ("run", list(range(1, 13))),
            "notice": ("react", [1, 2]),
            "scream": ("react", [3, 4, 5, 6, 7, 8]),
            "blind": ("react", [9, 10, 11]),
            "hypnotize": ("react", [12, 13, 14]),
            "sleep": ("react", [15, 16]),
            "action": ("react", list(range(17, 25))),
        },
    },
    "milkmaid": {
        "sheets": {"idle": ("median", 1.0), "walk": ("max", 1.0), "run": ("max", 1.04), "react": (1, 1.0)},
        "anims": {
            "idle": ("idle", [1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12]),
            "walk": ("walk", list(range(1, 13))),
            "run": ("run", list(range(1, 13))),
            "notice": ("react", [1]),
            "scream": ("react", [2, 3, 4, 5, 6]),
            "blind": ("react", [7, 8, 9]),
            "hypnotize": ("react", [10, 11, 12]),
            "sleep": ("react", [13, 14]),
            "action": ("react", [15, 16, 17, 18, 19, 20]),
        },
    },
    "preacher": {
        "sheets": {"idle": ("median", 1.0), "walk": ("max", 1.0), "run": ("max", 1.03), "react": (1, 1.0)},
        "anims": {
            "idle": ("idle", list(range(1, 13))),
            "walk": ("walk", list(range(1, 13))),
            "run": ("run", [1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12]),
            "notice": ("react", [2, 3, 4]),
            "scream": ("react", [5, 6, 7, 8]),
            "blind": ("react", [9, 10]),
            "hypnotize": ("react", [11, 12, 13]),
            "sleep": ("react", [14, 15, 16]),
            "action": ("react", list(range(17, 25))),
        },
    },
    "boy": {
        "sheets": {"idle": ("median", 1.0), "walk": ("max", 1.0), "react": (17, 1.0)},
        "anims": {
            "idle": ("idle", [1, 2, 3, 4, 5, 6, 7, 8, 9, 12]),
            "walk": ("walk", list(range(1, 13))),
            # Su hoja de carrera pierde la resortera en varios fotogramas: persigue con la caminata acelerada
            "run": ("walk", list(range(1, 13))),
            "notice": ("react", [1, 2]),
            "scream": ("react", [3, 4, 5, 6, 7, 8]),
            "blind": ("react", [9, 10, 11]),
            "hypnotize": ("react", [12, 13, 14]),
            "sleep": ("react", [15, 16]),
            "action": ("react", list(range(17, 25))),
        },
    },
}


def load(char, sheet, idx):
    return Image.open(FRAMES / f"{char}_{sheet}_{idx - 1:02d}.png").convert("RGBA")


def bbox_height(img):
    ys = np.nonzero(np.asarray(img)[..., 3] > 8)[0]
    return ys.max() - ys.min()


def sheet_scale(char, sheet, rule, target_px):
    files = sorted(FRAMES.glob(f"{char}_{sheet}_*.png"))
    heights = [bbox_height(Image.open(f)) for f in files]
    ref, factor = rule
    if ref == "median":
        h = float(np.median(heights))
    elif ref == "max":
        h = float(max(heights))
    else:
        h = float(heights[ref - 1])
    return target_px / (h * factor)


def despill(img):
    """Neutraliza el tinte magenta que la IA deja en piezas delgadas (asas, resorteras). Los aldeanos no usan morados."""
    a = np.asarray(img).astype(np.float32).copy()
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    spill = np.minimum(r, b) - g
    m = (spill > 12) & (a[..., 3] > 0)
    a[..., 0][m] = np.maximum(g[m], r[m] - spill[m] * 0.85)
    a[..., 2][m] = np.maximum(g[m], b[m] - spill[m] * 0.85)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def place(img, scale):
    """Escala el fotograma (pies en la base del lienzo de slice_sheet, torso centrado) al lienzo final."""
    w, h = img.size
    scaled = img.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS)
    # slice_sheet deja los pies a 20 px de la base y el torso en el centro horizontal
    feet_y = (h - 20) * scale
    center_x = (w / 2) * scale
    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    canvas.alpha_composite(scaled, (round(CANVAS[0] / 2 - center_x), round(CANVAS[1] - PIVOT_Y - feet_y)))
    return canvas


def main():
    for char, cfg in CHARACTERS.items():
        target_px = HEIGHT_UNITS[char] * PPU
        scales = {s: sheet_scale(char, s, rule, target_px) for s, rule in cfg["sheets"].items()}
        out_dir = OUT / char
        out_dir.mkdir(parents=True, exist_ok=True)
        for anim, (sheet, frames) in cfg["anims"].items():
            for n, idx in enumerate(frames):
                despill(place(load(char, sheet, idx), scales[sheet])).save(out_dir / f"{anim}_{n:02d}.png")
        print(f"{char:9s} escalas " + " ".join(f"{k}={v:.3f}" for k, v in scales.items()))


if __name__ == "__main__":
    main()
