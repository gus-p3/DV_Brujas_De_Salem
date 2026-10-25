"""
Arte procedural del Nivel 2 que reemplaza piezas cortadas o ilegibles:

- UI_Lore_Parchment.png   Hoja de pergamino con bordes quemados para leer diarios y crónicas (lector con pausa).
- Village_Ladder.png      Escalera con remates arriba y abajo y un tramo central sin cortes. Se importa con
                          bordes de 9-slice (remates) para que el SpriteRenderer en modo Tiled solo repita
                          el tramo central: antes se repetía la escalera entera y los remates partían los largueros.

Uso (desde la raíz del proyecto): python ArtSource/Environment/Level2/generate_level2_ui_and_ladder.py
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT_DIR = "Assets/Art/Environment/Level2"
RNG = np.random.default_rng(1692)

OUTLINE = (29, 15, 12, 255)
WOOD_DARK = (68, 39, 24, 255)
WOOD_MID = (112, 68, 38, 255)
WOOD_LIGHT = (158, 102, 56, 255)


def smooth_noise(w, h, scale, octaves=4):
    """Ruido de valor suavizado en [0, 1] (suma de octavas reescaladas)."""
    acc = np.zeros((h, w), dtype=np.float32)
    amp, total = 1.0, 0.0
    for o in range(octaves):
        cw, ch = max(2, int(w / scale * 2 ** o)), max(2, int(h / scale * 2 ** o))
        grid = RNG.random((ch, cw)).astype(np.float32)
        layer = np.asarray(Image.fromarray((grid * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC), dtype=np.float32) / 255.0
        acc += layer * amp
        total += amp
        amp *= 0.5
    return acc / total


def create_parchment():
    w, h = 1200, 900
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)

    # Distancia normalizada al borde de un rectángulo redondeado, deformada con ruido: borde irregular quemado
    dx = np.minimum(xx, w - 1 - xx) / (w * 0.5)
    dy = np.minimum(yy, h - 1 - yy) / (h * 0.5)
    edge = np.minimum(dx * (w / h) ** 0.5, dy)
    edge = edge + (smooth_noise(w, h, 90, 5) - 0.5) * 0.16
    burn_line = 0.06

    alpha = np.clip((edge - burn_line) / 0.012, 0, 1)
    char = np.clip(1 - (edge - burn_line) / 0.07, 0, 1)        # zona chamuscada junto al borde
    glow = np.clip(1 - np.abs(edge - burn_line - 0.012) / 0.012, 0, 1) * 0.55

    # Papel: ocre claro con manchas, vetas y oscurecimiento hacia los bordes
    stains = smooth_noise(w, h, 260, 4)
    fibers = smooth_noise(w, h, 18, 2)
    base = np.array([236, 218, 178], dtype=np.float32)
    dark = np.array([196, 160, 104], dtype=np.float32)
    t = np.clip(0.55 * stains + 0.15 * fibers + 0.45 * (1 - np.clip(edge / 0.5, 0, 1)), 0, 1)[..., None]
    rgb = base * (1 - t) + dark * t

    burnt = np.array([58, 30, 14], dtype=np.float32)
    rgb = rgb * (1 - char[..., None] ** 1.4) + burnt * (char[..., None] ** 1.4)
    ember = np.array([214, 108, 34], dtype=np.float32)
    rgb = rgb * (1 - glow[..., None]) + ember * glow[..., None]

    img = np.dstack([np.clip(rgb, 0, 255), alpha * 255]).astype(np.uint8)
    out = Image.fromarray(img, "RGBA").filter(ImageFilter.SMOOTH)
    path = os.path.join(OUT_DIR, "UI_Lore_Parchment.png")
    out.save(path)
    print("OK", path, out.size)


def rail(d, x0, x1, y0, y1):
    d.rectangle([x0, y0, x1, y1], fill=WOOD_MID, outline=OUTLINE, width=3)
    d.line([x0 + 4, y0, x0 + 4, y1], fill=WOOD_LIGHT, width=3)


def create_ladder():
    # 54 x 96 px finales a 64 PPU: remate inferior 16 px, tramo central 64 px (1 peldaño cada 1 u), remate superior 16 px
    s = 2  # se dibuja al doble y se reduce con antialias
    w, h = 54 * s, 96 * s
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    lx0, lx1 = 4 * s, 13 * s
    rx0, rx1 = w - 13 * s, w - 4 * s
    cap = 16 * s

    # Largueros: recorren todo el sprite; los remates redondeados quedan en las zonas de borde (no se repiten)
    rail(d, lx0, lx1, 0, h - 1)
    rail(d, rx0, rx1, 0, h - 1)
    for x0, x1 in ((lx0, lx1), (rx0, rx1)):
        d.rounded_rectangle([x0 - s, 0, x1 + s, 10 * s], radius=4 * s, fill=WOOD_DARK, outline=OUTLINE, width=3)
        d.rounded_rectangle([x0 - s, h - 10 * s, x1 + s, h - 1], radius=4 * s, fill=WOOD_DARK, outline=OUTLINE, width=3)

    # Un peldaño centrado en el tramo repetible: al repetirse queda uno por unidad, nunca cortado
    mid_y = h // 2
    d.rounded_rectangle([lx1 - s, mid_y - 5 * s, rx0 + s, mid_y + 5 * s], radius=3 * s, fill=WOOD_LIGHT, outline=OUTLINE, width=3)
    d.line([lx1 + 2 * s, mid_y - 2 * s, rx0 - 2 * s, mid_y - 2 * s], fill=(190, 132, 74, 255), width=2)

    out = img.resize((w // s, h // s), Image.LANCZOS)
    path = os.path.join(OUT_DIR, "Village_Ladder.png")
    out.save(path)
    print("OK", path, out.size, "bordes 9-slice: arriba/abajo 16 px")


if __name__ == "__main__":
    create_parchment()
    create_ladder()
