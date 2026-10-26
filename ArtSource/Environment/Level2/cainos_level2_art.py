"""
Arte del Nivel 2 derivado del paquete gratuito de Cainos (Pixel Art Platformer - Village Props):

1. Toldo del puesto (rayas azules) recoloreado a los tonos cálidos de los puestos del Nivel 2: rojo terracota,
   dorado, naranja y verde oliva. Conserva la luminosidad de cada píxel (sombras, contornos y rayas claras).
   Salida: Assets/Art/Environment/Level2/Cainos_Stall_Tent_<Color>.png (al doble, 64 PPU: mismo tamaño en el mundo).
2. Escalera "Ladder 01": sus tres piezas (remate de arriba, tramo y remate de abajo) en una sola imagen para usarla
   con 9-slice como Village_Ladder (los remates no se repiten; el tramo central se adapta a la altura).
   Salida: Assets/Art/Environment/Level2/Cainos_Ladder.png

Uso (desde la raíz del proyecto): python ArtSource/Environment/Level2/cainos_level2_art.py
"""
import colorsys
import os

import numpy as np
from PIL import Image

SRC = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png"
OUT_DIR = "Assets/Art/Environment/Level2"
TENT = (387, 676, 90, 28)  # x, y (desde abajo, como Unity), ancho, alto del sprite "Stall Tent"
LADDER = {"T": (320, 512), "M": (320, 480), "B": (320, 448)}  # piezas de 32x32 de "Ladder 01"

TARGETS = {  # tono (0-1) y saturación de las rayas: apagada, como el resto del atlas de Cainos
    "Red": (0.015, 0.45),
    "Gold": (0.11, 0.50),
    "Orange": (0.065, 0.52),
    "Olive": (0.17, 0.32),
}


def crop(atlas, x, y, w, h):
    top = atlas.height - (y + h)
    return atlas.crop((x, top, x + w, top + h))


def tents(atlas):
    tent = np.asarray(crop(atlas, *TENT), dtype=np.float32) / 255.0
    h, w = tent.shape[:2]
    for name, (hue, sat) in TARGETS.items():
        out = tent.copy()
        for j in range(h):
            for i in range(w):
                r, g, b, a = tent[j, i]
                if a == 0:
                    continue
                hh, ll, ss = colorsys.rgb_to_hls(r, g, b)
                # Las rayas son un gris azulado muy apagado (tono ~0.52, saturación ~0.06): se detectan por tono frío;
                # la tela crema (tono ~0.08), la madera y el contorno quedan igual
                if 0.35 <= hh <= 0.75 and ss > 0.005:
                    out[j, i, :3] = colorsys.hls_to_rgb(hue, ll * 0.95, sat)  # conserva el sombreado del pliegue
        path = os.path.join(OUT_DIR, f"Cainos_Stall_Tent_{name}.png")
        # Al doble sin suavizar (se importa a 64 PPU: mismo tamaño en el mundo); Level2CompleteFixer exige >= 40 px de alto
        img = Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").resize((w * 2, h * 2), Image.NEAREST)
        img.save(path)
        print("OK", path, img.size)


def ladder(atlas):
    img = Image.new("RGBA", (32, 96))
    for row, piece in enumerate("TMB"):
        img.paste(crop(atlas, *LADDER[piece], 32, 32), (0, row * 32))
    path = os.path.join(OUT_DIR, "Cainos_Ladder.png")
    img.save(path)
    print("OK", path, img.size)


def main():
    atlas = Image.open(SRC).convert("RGBA")
    tents(atlas)
    ladder(atlas)


if __name__ == "__main__":
    main()
