"""
Separa la hoja de piezas de la bruja (witch_parts_chatgpt.png) en un PNG por pieza del rig.
Ejecutar desde la raíz del proyecto:  python ArtSource/Witch/split_parts.py
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, ".")
import gemini_art as g  # noqa: E402

SRC = Path("ArtSource/Witch/witch_parts_chatgpt.png")
OUT = Path("ArtSource/Witch/parts")

# Centro aproximado (x, y) de cada pieza en la hoja de origen -> nombre en el rig
CENTERS = {
    "hood": (826, 228), "head": (155, 240),
    "eye_open": (366, 196), "eye_half": (486, 195), "eye_closed": (607, 194),
    "hair_tail": (1081, 250), "mouth_smile": (380, 340), "mouth_o": (522, 338),
    "scarf": (128, 548), "scarf_tail": (265, 576), "torso": (453, 602), "robe_skirt": (708, 635),
    "arm_upper_front": (980, 524), "arm_upper_back": (1143, 524),
    "arm_lower_front": (974, 764), "arm_lower_back": (1134, 762),
    "leg_upper_front": (208, 1020), "leg_lower_front": (446, 1046),
    "leg_upper_back": (726, 1025), "leg_lower_back": (976, 1050),
}


def main():
    img = g.key_background(Image.open(SRC), g.CHROMA, 90)
    img.save(SRC.with_name("witch_parts_keyed.png"))
    arr = np.asarray(img)
    alpha = arr[..., 3] > 8
    labels, _ = ndimage.label(ndimage.binary_dilation(alpha, iterations=3))
    OUT.mkdir(parents=True, exist_ok=True)

    for name, (cx, cy) in CENTERS.items():
        # Etiqueta del píxel opaco más cercano al centro indicado
        ys, xs = np.nonzero(labels)
        k = np.argmin((xs - cx) ** 2 + (ys - cy) ** 2)
        idx = labels[ys[k], xs[k]]
        sl = ndimage.find_objects((labels == idx).astype(np.int32))[0]
        crop = arr[sl].copy()
        mask = (labels[sl] == idx) & alpha[sl]
        crop[..., 3] = np.where(mask, crop[..., 3], 0)
        piece = Image.fromarray(crop, "RGBA")

        if name == "head":
            # Quitar la coleta pegada (debajo de la liga); la anima la pieza hair_tail
            h = np.asarray(g.trim(piece, 2)).copy()
            yy, xx = np.mgrid[0:h.shape[0], 0:h.shape[1]]
            h[..., 3] = np.where((yy > 211) & (xx < 100), 0, h[..., 3])
            piece = Image.fromarray(h, "RGBA")

        piece = g.trim(piece, 2)

        if name.startswith(("eye_", "mouth_")):
            # Los parches de cara se superponen a la cabeza: borde contraído y difuminado
            # para que no se note la costura (ni restos del halo del fondo).
            p = np.asarray(piece).copy()
            solid = ndimage.binary_erosion(p[..., 3] > 128, iterations=2)
            soft = ndimage.gaussian_filter(solid.astype(np.float32), 1.0)
            p[..., 3] = (np.minimum(p[..., 3] / 255.0, soft) * 255).astype(np.uint8)
            piece = Image.fromarray(p, "RGBA")

        if name == "hood":
            # Separar el interior oscuro: la concha va delante de la cabeza y el interior detrás.
            # Ambos conservan el mismo lienzo para compartir pivote y posición.
            h = np.asarray(piece).astype(int)
            dark = (h[..., 0] >= 48) & (h[..., 0] <= 110) & (h[..., 1] <= 55) & (h[..., 3] > 200)
            lab, _ = ndimage.label(dark)
            inner = lab == lab[160, 200]
            shell = h.copy().astype(np.uint8)
            shell[..., 3] = np.where(inner, 0, shell[..., 3])
            back = h.copy().astype(np.uint8)
            back[..., 3] = np.where(inner, back[..., 3], 0)
            Image.fromarray(back, "RGBA").save(OUT / "hood_inner.png")
            piece = Image.fromarray(shell, "RGBA")

        piece.save(OUT / f"{name}.png")
        print(f"{name:16s} {piece.size}")

    make_closed_eye(Image.open(OUT / "eye_open.png")).save(OUT / "eye_closed.png")
    print("eye_closed       (derivado de eye_open)")


def make_closed_eye(eye_open):
    """
    Ojo cerrado derivado del abierto: mismo contorno de perfil (sin saltos al parpadear).
    Se rellena el ojo con piel y se dibuja un párpado curvo con pestaña en el extremo exterior.
    """
    from PIL import ImageDraw

    e = np.asarray(eye_open.convert("RGBA")).astype(np.float32)
    skin = np.array([252, 182, 139], dtype=np.float32)
    region = np.zeros(e.shape[:2], bool)
    region[20:83, 8:76] = True
    not_skin = np.sqrt(((e[..., :3] - skin) ** 2).sum(-1)) > 24
    fill = ndimage.binary_dilation(region & not_skin & (e[..., 3] > 0), iterations=2) & region & (e[..., 3] > 0)
    e[fill, :3] = skin
    out = Image.fromarray(e.astype(np.uint8), "RGBA")

    # Párpado con antialias (dibujo a 4x y reducción)
    k = 4
    w, h = out.size
    layer = Image.new("RGBA", (w * k, h * k), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    ink = (38, 18, 14, 255)
    d.arc([12 * k, 38 * k, 72 * k, 66 * k], start=15, end=165, fill=ink, width=4 * k)    # párpado
    d.line([(15 * k, 56 * k), (6 * k, 52 * k)], fill=ink, width=3 * k)                    # pestaña exterior
    d.line([(19 * k, 59 * k), (10 * k, 60 * k)], fill=ink, width=2 * k)
    layer = layer.resize((w, h), Image.LANCZOS)
    out.alpha_composite(layer)
    return out


if __name__ == "__main__":
    main()
