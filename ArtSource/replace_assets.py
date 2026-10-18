"""
Reemplaza los assets aprobados del nivel 1 por el arte nuevo de ArtSource como sustitución directa:
mismo archivo (y por tanto mismo GUID y referencias), mismo tamaño de lienzo y contenido alineado con el anterior.

- Plataformas: misma anchura de contenido y superficie caminable a la misma altura (las raíces pueden
  extender el lienzo hacia abajo; el pivote se recalcula para no mover nada).
- Maíz, letrero, checkpoint y meta: apoyados en el mismo suelo y a la misma altura que antes.
- Objetos e íconos: centrados en la misma zona. Animaciones con una sola escala para todos sus fotogramas.
- Suelo y cielo: repetibles horizontalmente sin costura.

Escribe ArtSource/replace_assets_pivots.json con los pivotes que cambian (los aplica Tools > Arte > Aplicar pivotes).
Ejecutar desde la raíz:  python ArtSource/replace_assets.py
"""
import colorsys
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, ".")
sys.path.insert(0, "ArtSource/Villagers")
from slice_sheet import border_key  # noqa: E402

ART = Path("Assets/Art")
SRC = Path("ArtSource")
PIVOTS = {}


# ----------------------------------------------------------------------------- utilidades

def keyed(sheet):
    return border_key(Image.open(SRC / sheet).convert("RGB"))


def region(img, box):
    """Recorta una región de la hoja y la ajusta a su contenido."""
    crop = img.crop(box)
    bb = crop.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    return crop.crop(bb) if bb else crop


def despill(img, strength=0.85, remove_above=None):
    a = np.asarray(img).astype(np.float32).copy()
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    spill = np.minimum(r, b) - g
    if remove_above is not None:
        a[..., 3] = np.where(spill > remove_above, 0, a[..., 3])
    m = (spill > 12) & (a[..., 3] > 0)
    a[..., 0][m] = np.maximum(g[m], r[m] - spill[m] * strength)
    a[..., 2][m] = np.maximum(g[m], b[m] - spill[m] * strength)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def pink_to_orange(img):
    """El maíz tomó reflejos rosas del fondo magenta: se pasan a naranja otoñal."""
    a = np.asarray(img).astype(np.float32) / 255.0
    out = a.copy()
    rgb = a[..., :3].reshape(-1, 3)
    hsv = np.array([colorsys.rgb_to_hsv(*p) for p in rgb])
    h, s, v = hsv[:, 0], hsv[:, 1], hsv[:, 2]
    pink = ((h > 0.80) | (h < 0.02)) & (s > 0.15)
    h = np.where(pink, 0.07, h)
    new = np.array([colorsys.hsv_to_rgb(*p) for p in np.stack([h, s, v], 1)])
    out[..., :3] = new.reshape(a.shape[0], a.shape[1], 3)
    return Image.fromarray((out * 255).astype(np.uint8), "RGBA")


def content_bbox(img, thr=8):
    return img.getchannel("A").point(lambda a: 255 if a > thr else 0).getbbox()


def surface_row(img, frac=0.6):
    """Primera fila (desde arriba) con cobertura opaca densa: la superficie caminable."""
    a = np.asarray(img)[..., 3] > 128
    x0, y0, x1, y1 = content_bbox(img)
    width = max(1, x1 - x0)
    for y in range(y0, y1):
        if a[y, x0:x1].sum() >= frac * width:
            return y
    return y0


def read_pivot(path):
    meta = Path(str(path) + ".meta").read_text(encoding="utf-8")
    m = re.search(r"spritePivot: \{x: ([\d.\-e]+), y: ([\d.\-e]+)\}", meta)
    align = re.search(r"\n  alignment: (\d+)", meta)
    px, py = (float(m.group(1)), float(m.group(2))) if m else (0.5, 0.5)
    # alignment 0 = centro (Unity ignora spritePivot en ese caso)
    if align and align.group(1) == "0":
        px, py = 0.5, 0.5
    elif align and align.group(1) == "7":  # BottomCenter
        px, py = 0.5, 0.0
    return px, py


def scaled(img, s):
    return img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)


def save(img, rel):
    path = ART / rel
    img.save(path)
    print(f"  {rel} {img.size}")


# ----------------------------------------------------------------------------- reglas de colocación

def replace_platform(piece, rel):
    """Misma anchura de contenido y superficie caminable a la misma altura."""
    path = ART / rel
    old = Image.open(path).convert("RGBA")
    W, H = old.size
    ox0, oy0, ox1, oy1 = content_bbox(old)
    old_surface = surface_row(old)
    s = (ox1 - ox0) / piece.width
    p = scaled(piece, s)
    new_surface = surface_row(p)
    top = old_surface - new_surface
    new_h = max(H, top + p.height)
    canvas = Image.new("RGBA", (W, new_h), (0, 0, 0, 0))
    canvas.alpha_composite(p, (ox0 + ((ox1 - ox0) - p.width) // 2, top))
    if new_h != H:
        px, py = read_pivot(path)
        pivot_from_top = H * (1 - py)
        PIVOTS[str(path).replace("\\", "/")] = [px, 1 - pivot_from_top / new_h]
    save(canvas, rel)


def replace_filled(piece, rel):
    """Ocupa exactamente la caja del contenido anterior (para vegetación densa que forma hileras continuas)."""
    old = Image.open(ART / rel).convert("RGBA")
    ox0, oy0, ox1, oy1 = content_bbox(old)
    p = piece.resize((ox1 - ox0, oy1 - oy0), Image.LANCZOS)
    canvas = Image.new("RGBA", old.size, (0, 0, 0, 0))
    canvas.alpha_composite(p, (ox0, oy0))
    save(canvas, rel)


def replace_grounded(piece, rel, match="height", pad=8):
    """Apoyado en el mismo suelo (base del contenido) y con la misma altura (o anchura) de contenido."""
    path = ART / rel
    old = Image.open(path).convert("RGBA")
    W, H = old.size
    ox0, oy0, ox1, oy1 = content_bbox(old)
    s = (oy1 - oy0) / piece.height if match == "height" else (ox1 - ox0) / piece.width
    p = scaled(piece, s)
    cx = (ox0 + ox1) / 2
    new_w = max(W, p.width + 2 * pad)
    new_h = max(H, oy1)  # el contenido nuevo ocupa la misma altura que el anterior
    dx = (new_w - W) / 2
    canvas = Image.new("RGBA", (int(new_w), new_h), (0, 0, 0, 0))
    canvas.alpha_composite(p, (round(cx + dx - p.width / 2), oy1 - p.height))
    if new_w != W:
        px, py = read_pivot(path)
        PIVOTS[str(path).replace("\\", "/")] = [(px * W + dx) / new_w, py]
    save(canvas, rel)


def replace_centered(piece, rel, fill=1.0):
    """Centrado en la zona del contenido anterior, sin salirse de ella."""
    old = Image.open(ART / rel).convert("RGBA")
    W, H = old.size
    bb = content_bbox(old) or (0, 0, W, H)
    bw, bh = (bb[2] - bb[0]) * fill, (bb[3] - bb[1]) * fill
    s = min(bw / piece.width, bh / piece.height)
    p = scaled(piece, s)
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    canvas.alpha_composite(p, (round((bb[0] + bb[2]) / 2 - p.width / 2), round((bb[1] + bb[3]) / 2 - p.height / 2)))
    save(canvas, rel)


def replace_sheet(frames, rel, cells, align="center", fill=0.92, alphas=None):
    """Hoja de animación de celdas iguales: una sola escala para todos los fotogramas."""
    old = Image.open(ART / rel).convert("RGBA")
    W, H = old.size
    cw = W // cells
    if align == "bottom":
        # Altura del contenido anterior (máxima entre celdas) y base común
        heights, bottoms = [], []
        for c in range(cells):
            bb = content_bbox(old.crop((c * cw, 0, (c + 1) * cw, H)))
            if bb:
                heights.append(bb[3] - bb[1])
                bottoms.append(bb[3])
        s = max(heights) / max(f.height for f in frames)
        base = max(bottoms)
    else:
        s = min(cw * fill / max(f.width for f in frames), H * fill / max(f.height for f in frames))
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for c, f in enumerate(frames[:cells]):
        p = scaled(f, s)
        if alphas and alphas[c] < 1:
            arr = np.asarray(p).copy()
            arr[..., 3] = (arr[..., 3] * alphas[c]).astype(np.uint8)
            p = Image.fromarray(arr, "RGBA")
        x = c * cw + (cw - p.width) // 2
        y = base - p.height if align == "bottom" else (H - p.height) // 2
        canvas.alpha_composite(p, (x, y))
    save(canvas, rel)


def seamless(img, k):
    """Toma una ventana de (ancho - k) que se repite sin costura mezclando el final con el inicio."""
    a = np.asarray(img).astype(np.float32)
    w = a.shape[1] - k
    out = a[:, :w].copy()
    t = np.linspace(0, 1, k)[None, :, None]
    out[:, :k] = a[:, w:w + k] * (1 - t) + a[:, :k] * t
    return Image.fromarray(out.astype(np.uint8), "RGBA")


# ----------------------------------------------------------------------------- lotes

def ground():
    print("Suelo")
    strip = region(keyed("Environment/ground.png"), (0, 150, 1536, 480))
    old = Image.open(ART / "Environment/Env_Ground_Tile.png").convert("RGBA")
    W, H = old.size
    old_surface = surface_row(old, 0.95)
    new_surface = surface_row(strip, 0.95)
    s = (H - old_surface) / (strip.height - new_surface)
    k = 96
    p = scaled(strip, s)
    win = p.crop((0, 0, W + k, p.height))
    tile = seamless(win, k)
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    canvas.alpha_composite(tile, (0, old_surface - round(new_surface * s)))
    save(canvas, "Environment/Env_Ground_Tile.png")


def platforms():
    print("Rocas y troncos")
    sheet = keyed("Environment/platforms.png")
    pieces = {
        "Environment/Env_Ledge_Rock_A.png": (100, 60, 450, 320),
        "Environment/Env_Ledge_Rock_B.png": (620, 60, 1440, 330),
        "Environment/Env_Platform_Log_A.png": (40, 365, 320, 545),
        "Environment/Env_Platform_Log_B.png": (395, 365, 835, 545),
        "Environment/Env_Platform_Log_C.png": (885, 365, 1510, 545),
    }
    for rel, box in pieces.items():
        replace_platform(despill(region(sheet, box)), rel)


def corn():
    print("Maizal")
    sheet = keyed("Environment/corn.png")
    stalks = {
        "Environment/Env_Corn_Stalk_A.png": (40, 40, 270, 705),
        "Environment/Env_Corn_Stalk_B.png": (290, 40, 520, 705),
        "Environment/Env_Corn_Stalk_C.png": (555, 40, 775, 705),
        "Environment/Env_Corn_Cluster.png": (805, 95, 1515, 705),
    }
    for rel, box in stalks.items():
        piece = pink_to_orange(despill(region(sheet, box)))
        # El grupo forma hileras continuas en el maizal: ocupa el mismo ancho que el anterior
        if rel.endswith("Cluster.png"):
            replace_filled(piece, rel)
        else:
            replace_grounded(piece, rel)


def items():
    print("Pociones, íconos y escoba")
    pot = keyed("Items/potions.png")
    cols = [(0, 512), (512, 1024), (1024, 1536)]
    names = ["Blind", "Hypnotize", "Sleep"]
    for (x0, x1), n in zip(cols, names):
        bottle = region(pot, (x0, 40, x1, 440))
        replace_centered(bottle, f"Items/PotionItem_{n}.png")
        replace_centered(bottle, f"UI/ui_icon_flask_{n.lower()}.png")
        replace_centered(region(pot, (x0, 470, x1, 710)), f"Items/PotionProjectile_{n}.png")
    for (x0, x1), n in zip(cols, ["Icon_Blinded", "Icon_Spiral", "Icon_Zzz"]):
        replace_centered(region(pot, (x0, 750, x1, 1000)), f"Effects/{n}.png")

    broom = keyed("Items/broom.png")
    replace_centered(despill(region(broom, (130, 160, 1410, 470))), "Characters/Witch/Witch_Broom.png")
    replace_centered(despill(region(broom, (50, 640, 650, 800))), "Environment/Item_Branch.png")
    replace_centered(despill(region(broom, (690, 610, 1010, 850))), "Environment/Item_Rope.png")
    replace_centered(despill(region(broom, (1040, 610, 1490, 840))), "Environment/Item_Straw.png")


def hearts():
    print("Corazones")
    sheet = keyed("Items/hearts.png")
    pickup = despill(region(sheet, (290, 80, 545, 305)))
    full = despill(region(sheet, (650, 85, 885, 305)))
    empty = despill(region(sheet, (990, 85, 1235, 305)))
    replace_centered(pickup, "Environment/Item_Heart.png")
    replace_centered(full, "UI/ui_icon_heart_full.png")
    replace_centered(full, "UI/ui_icon_heart.png")
    replace_centered(empty, "UI/ui_icon_heart_empty.png")
    brk = [(20, 265), (265, 506), (506, 748), (748, 990), (990, 1240), (1240, 1530)]
    replace_sheet([despill(region(sheet, (a, 375, b, 645))) for a, b in brk], "UI/Heart_Break.png", 6)
    heal = [(0, 380), (380, 755), (755, 1130), (1130, 1536)]
    replace_sheet([despill(region(sheet, (a, 680, b, 965))) for a, b in heal], "UI/Heart_Heal.png", 4)


def splashes():
    print("Salpicaduras")
    sheet = keyed("Items/splashes.png")
    rows = {"Blind": (20, 360), "Hypnotize": (360, 640), "Sleep": (640, 1010)}
    for n, (y0, y1) in rows.items():
        burst1 = region(sheet, (380, y0, 725, y1))
        burst2 = region(sheet, (775, y0, 1115, y1))
        small = scaled(burst1, 0.6)
        # La 4.ª imagen original era humo mezclado con el fondo: se sustituye por la salpicadura que se desvanece
        replace_sheet([small, burst1, burst2, burst2], f"Effects/Splash_{n}.png", 4, alphas=[1, 1, 1, 0.45])


def props():
    print("Letrero, checkpoint y meta")
    sheet = keyed("Environment/props.png")
    replace_grounded(despill(region(sheet, (30, 55, 305, 465))), "Environment/Env_Sign.png")
    cells = [(650, 795), (795, 937), (937, 1075), (1075, 1222), (1222, 1370), (1370, 1536)]
    # Halo de luz mezclado con el magenta: se elimina (la luz la pone el juego)
    frames = [despill(region(sheet, (a, 165, b, 435)), remove_above=25) for a, b in cells]
    replace_sheet(frames, "Environment/Env_Checkpoint.png", 6, align="bottom")
    replace_grounded(despill(region(sheet, (245, 470, 1295, 965))), "Environment/Env_Goal_Gate.png")


def sky():
    print("Cielo")
    src = Image.open(SRC / "Environment/bg_sky.png").convert("RGBA")
    old = Image.open(ART / "Environment/Env_Sky_Far.png")
    W, H = old.size
    k = 320
    s = (W + k) / src.width
    big = scaled(src, s)
    top = max(0, (big.height - H) // 3)
    win = big.crop((0, top, W + k, top + H))
    save(seamless(win, k), "Environment/Env_Sky_Far.png")


def main():
    ground()
    platforms()
    corn()
    items()
    hearts()
    splashes()
    props()
    sky()
    out = SRC / "replace_assets_pivots.json"
    items = [{"path": k, "x": v[0], "y": v[1]} for k, v in PIVOTS.items()]
    out.write_text(json.dumps({"items": items}, indent=2), encoding="utf-8")
    print(f"Pivotes a actualizar: {len(PIVOTS)} -> {out}")


if __name__ == "__main__":
    main()
