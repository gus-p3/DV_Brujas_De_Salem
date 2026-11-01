"""
Extrae los 5 personajes de los 2 sprite sheets de aldeanos de Salem y genera
un sprite sheet uniforme por personaje en Assets/Art/Sprites/Villagers/<rol>_sheet.png.

Layout de salida (5 columnas x 7 filas, celdas de 96x96 px, pivot bottom-center):
  Fila 0 (y=0 en imagen = arriba): idle      (2 frames)
  Fila 1:                          walk      (4 frames)
  Fila 2:                          run       (4 frames)
  Fila 3:                          notice    (1 frame)
  Fila 4:                          scream    (2 frames)
  Fila 5:                          blind     (2 frames)
  Fila 6:                          sleep     (1 frame)

Para los personajes que reutilizaban walk en run (guard, boy), genera fotogramas
de carrera dedicados con inclinación dinámica de persecución, zancada amplia y rebote
para lograr animaciones de villanos mucho más fluidas y expresivas.
"""
from PIL import Image, ImageFilter
import numpy as np
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC1 = os.path.join(ROOT, "ArtSource", "Villagers", "sheet1.png")
SRC2 = os.path.join(ROOT, "ArtSource", "Villagers", "sheet2.png")
OUT_DIR = os.path.join(ROOT, "Assets", "Art", "Sprites", "Villagers")
os.makedirs(OUT_DIR, exist_ok=True)

CELL = 96
COLS = 5
ROWS = 7

im1 = Image.open(SRC1).convert("RGBA")
im2 = Image.open(SRC2).convert("RGBA")


def clean_alpha(img: Image.Image) -> Image.Image:
    """Limpia ruido de bordes semitransparentes del escalado y suaviza siluetas."""
    arr = np.array(img)
    arr[arr[:, :, 3] < 40] = [0, 0, 0, 0]
    mask = (arr[:, :, 3] > 0) & (arr[:, :, 0] > 235) & (arr[:, :, 1] > 235) & (arr[:, :, 2] > 235)
    arr[mask] = [0, 0, 0, 0]
    return Image.fromarray(arr)


im1 = clean_alpha(im1)
im2 = clean_alpha(im2)

BOXES = {
    # Sheet 1 (Aldeano granjero y Aldeana lechera)
    'farmer': [
        [(20, 62, 130, 173), (125, 62, 195, 173), (205, 62, 265, 173), (265, 62, 335, 173)],
        [(20, 194, 98, 297), (98, 194, 172, 297), (172, 194, 244, 297), (244, 194, 320, 297), (320, 194, 395, 297)],
        [(20, 315, 102, 419), (102, 315, 180, 419), (180, 315, 258, 419), (258, 315, 342, 419), (342, 315, 430, 419)],
        [(20, 444, 118, 558), (118, 444, 210, 558), (210, 444, 300, 558), (300, 444, 415, 558)],
    ],
    'milkmaid': [
        [(520, 62, 615, 173), (615, 62, 695, 173), (695, 62, 775, 173), (775, 62, 860, 173)],
        [(460, 194, 535, 297), (535, 194, 610, 297), (610, 194, 685, 297), (685, 194, 755, 297), (755, 194, 835, 297)],
        [(455, 315, 540, 419), (540, 315, 625, 419), (625, 315, 710, 419), (710, 315, 792, 419), (792, 315, 880, 419)],
        [(455, 444, 540, 558), (540, 444, 635, 558), (635, 444, 735, 558), (735, 444, 835, 558)],
    ],
    # Sheet 2 (Predicador, Niño y Guardia)
    'preacher': [
        [(15, 60, 88, 170), (88, 60, 152, 170), (152, 60, 215, 170), (215, 60, 282, 170)],
        [(15, 190, 88, 294), (88, 190, 152, 294), (152, 190, 215, 294), (215, 190, 282, 294)],
        [(15, 312, 88, 417), (88, 312, 152, 417), (152, 312, 215, 417), (215, 312, 282, 417)],
        [(15, 440, 88, 550), (88, 440, 155, 550), (155, 440, 222, 550), (222, 440, 295, 550)],
    ],
    'boy': [
        [(310, 60, 392, 170), (392, 60, 456, 170), (456, 60, 520, 170), (520, 60, 588, 170)],
        [(310, 190, 378, 294), (378, 190, 444, 294), (444, 190, 512, 294), (512, 190, 585, 294)],
        [(310, 312, 385, 417), (385, 312, 455, 417), (455, 312, 525, 417), (525, 312, 600, 417)],
        [(310, 440, 390, 550), (390, 440, 470, 550), (470, 440, 585, 550)],
    ],
    'guard': [
        [(620, 60, 708, 170), (708, 60, 776, 170), (776, 60, 845, 170), (845, 60, 920, 170)],
        [(615, 190, 692, 294), (692, 190, 766, 294), (766, 190, 842, 294), (842, 190, 920, 294)],
        [(615, 312, 692, 417), (692, 312, 766, 417), (766, 312, 842, 417), (842, 312, 920, 417)],
        [(615, 440, 695, 550), (695, 440, 775, 550), (775, 440, 855, 550), (855, 440, 935, 550)],
    ],
}

SRC_FOR = {'farmer': im1, 'milkmaid': im1, 'preacher': im2, 'boy': im2, 'guard': im2}


def extract_sprite(role: str, row: int, col: int) -> Image.Image:
    src = SRC_FOR[role]
    x0, y0, x1, y1 = BOXES[role][row][col]
    crop = src.crop((x0, y0, x1, y1))
    arr = np.array(crop)
    ys, xs = np.where(arr[:, :, 3] > 0)
    if len(xs) == 0:
        return Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    crop = crop.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
    return crop


def make_dynamic_run_variant(sprite: Image.Image, phase: int) -> Image.Image:
    """
    Transforma un frame de caminata en un frame de carrera agresiva/fluida:
    aplica inclinación hacia adelante (shear horizontal), extensión de zancada
    y compresión/extensión elástica según la fase del ciclo (0..3).
    """
    w, h = sprite.size
    pad = 16
    canvas = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    canvas.paste(sprite, (pad, pad))

    # Inclinación hacia adelante (hacia la derecha, que es el frente base del sprite)
    shear_values = [-0.12, -0.07, -0.14, -0.08]
    sx_scale = [1.08, 0.96, 1.10, 0.97]
    sy_scale = [0.94, 1.05, 0.93, 1.04]

    shear = shear_values[phase % 4]
    cw, ch = canvas.size
    # Transformación afín: x' = x + shear * (ch - y)
    x_shift = -shear * ch * 0.5
    warped = canvas.transform(
        (cw, ch),
        Image.AFFINE,
        (1.0, shear, x_shift, 0.0, 1.0, 0.0),
        resample=Image.NEAREST,
    )

    # Escalado elástico de zancada
    nw = max(1, int(round(cw * sx_scale[phase % 4])))
    nh = max(1, int(round(ch * sy_scale[phase % 4])))
    warped = warped.resize((nw, nh), Image.NEAREST)

    arr = np.array(warped)
    ys, xs = np.where(arr[:, :, 3] > 0)
    if len(xs) == 0:
        return sprite
    return warped.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))


def place_in_cell(sprite: Image.Image, target_h: int = 84, y_lift: int = 0) -> Image.Image:
    """Escala manteniendo proporción y ancla los pies en la base de la celda 96x96."""
    w, h = sprite.size
    if h == 0 or w == 0:
        return Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    scale = min(target_h / h, (CELL - 8) / w)
    nw = max(1, int(round(w * scale)))
    nh = max(1, int(round(h * scale)))
    resized = sprite.resize((nw, nh), Image.NEAREST)
    cell = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    ox = (CELL - nw) // 2
    oy = max(0, CELL - nh - 2 - y_lift)
    cell.paste(resized, (ox, oy), resized)
    return cell


MAPPING = {
    'farmer': {
        'idle':   [('farmer', 0, 1), ('farmer', 0, 2)],
        'walk':   [('farmer', 1, 0), ('farmer', 1, 1), ('farmer', 1, 2), ('farmer', 1, 3)],
        'run':    [('farmer', 2, 0), ('farmer', 2, 1), ('farmer', 2, 2), ('farmer', 2, 3)],
        'notice': [('farmer', 3, 0)],
        'scream': [('farmer', 3, 1), ('farmer', 2, 4)],
        'blind':  [('farmer', 3, 2), ('farmer', 1, 4)],
        'sleep':  [('farmer', 3, 3)],
    },
    'milkmaid': {
        'idle':   [('milkmaid', 0, 1), ('milkmaid', 0, 2)],
        'walk':   [('milkmaid', 1, 0), ('milkmaid', 1, 1), ('milkmaid', 1, 2), ('milkmaid', 1, 3)],
        'run':    [('milkmaid', 2, 0), ('milkmaid', 2, 1), ('milkmaid', 2, 2), ('milkmaid', 2, 3)],
        'notice': [('milkmaid', 3, 0)],
        'scream': [('milkmaid', 3, 1), ('milkmaid', 2, 4)],
        'blind':  [('milkmaid', 3, 2), ('milkmaid', 1, 4)],
        'sleep':  [('milkmaid', 3, 3)],
    },
    'preacher': {
        'idle':   [('preacher', 0, 1), ('preacher', 0, 2)],
        'walk':   [('preacher', 1, 0), ('preacher', 1, 1), ('preacher', 1, 2), ('preacher', 1, 3)],
        'run':    [('preacher', 2, 0), ('preacher', 2, 1), ('preacher', 2, 2), ('preacher', 2, 3)],
        'notice': [('preacher', 3, 0)],
        'scream': [('preacher', 3, 1), ('preacher', 3, 0)],
        'blind':  [('preacher', 3, 2), ('preacher', 0, 3)],
        'sleep':  [('preacher', 3, 3)],
    },
    'boy': {
        'idle':   [('boy', 0, 1), ('boy', 0, 2)],
        'walk':   [('boy', 1, 0), ('boy', 1, 1), ('boy', 1, 2), ('boy', 1, 3)],
        'run':    [('boy', 1, 0, 'run0'), ('boy', 1, 1, 'run1'), ('boy', 1, 2, 'run2'), ('boy', 1, 3, 'run3')],
        'notice': [('boy', 2, 0)],
        'scream': [('boy', 2, 1), ('boy', 2, 2)],
        'blind':  [('boy', 3, 0), ('boy', 3, 1)],
        'sleep':  [('boy', 3, 2)],
    },
    'guard': {
        'idle':   [('guard', 0, 1), ('guard', 0, 2)],
        'walk':   [('guard', 1, 0), ('guard', 1, 1), ('guard', 1, 2), ('guard', 1, 3)],
        'run':    [('guard', 1, 0, 'run0'), ('guard', 1, 1, 'run1'), ('guard', 1, 2, 'run2'), ('guard', 1, 3, 'run3')],
        'notice': [('guard', 2, 0)],
        'scream': [('guard', 2, 1), ('guard', 2, 2)],
        'blind':  [('guard', 3, 1), ('guard', 3, 2)],
        'sleep':  [('guard', 3, 3)],
    },
}

ROW_ORDER = ['idle', 'walk', 'run', 'notice', 'scream', 'blind', 'sleep']
TARGET_H = {'farmer': 84, 'milkmaid': 82, 'preacher': 86, 'boy': 70, 'guard': 88}

for role, anims in MAPPING.items():
    sheet = Image.new("RGBA", (COLS * CELL, ROWS * CELL), (0, 0, 0, 0))
    ref = extract_sprite(role, 0, 1)
    ref_h = ref.size[1]
    th = TARGET_H[role]

    for r_idx, state in enumerate(ROW_ORDER):
        frames = anims[state]
        for c_idx, spec in enumerate(frames):
            r_name, sr, sc = spec[0], spec[1], spec[2]
            spr = extract_sprite(r_name, sr, sc)
            y_lift = 0
            if len(spec) > 3 and str(spec[3]).startswith('run'):
                phase = int(str(spec[3])[-1])
                spr = make_dynamic_run_variant(spr, phase)
                y_lift = 3 if phase in (0, 2) else 0
            elif state == 'run' and c_idx in (0, 2):
                y_lift = 2

            ratio = spr.size[1] / max(1, ref_h)
            frame_target_h = int(round(th * min(1.08, max(0.55, ratio))))
            cell_img = place_in_cell(spr, frame_target_h, y_lift=y_lift)
            sheet.paste(cell_img, (c_idx * CELL, r_idx * CELL))

    out_path = os.path.join(OUT_DIR, f"{role}_sheet.png")
    sheet.save(out_path)
    print(f"[OK] {role} -> {out_path} ({sheet.size})")
