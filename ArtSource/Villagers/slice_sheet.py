"""
Corta una hoja de animación (cuadrícula de personaje sobre magenta) en fotogramas alineados por los pies.
Uso: python ArtSource/Villagers/slice_sheet.py <hoja.png> <salida_prefijo> [--cols 6 --rows 2]
Imprime métricas de consistencia: altura, ancho y diferencia entre fotogramas consecutivos.
"""
import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, ".")
import gemini_art as g  # noqa: E402


def border_key(img):
    """Fondo = magenta conectado con el borde (protege morados y rosas internos rodeados de contorno)."""
    keyed = np.asarray(g.key_background(img, g.CHROMA, 90)).copy()
    rgb = np.asarray(img.convert("RGB")).astype(int)
    near = np.sqrt(((rgb - np.array(g.CHROMA)) ** 2).sum(-1)) < 120
    lab, _ = ndimage.label(near)
    edge_ids = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    # Fondo: magenta conectado al borde, más los huecos de magenta casi puro encerrados por el personaje
    pure = np.sqrt(((rgb - np.array(g.CHROMA)) ** 2).sum(-1)) < 70
    background = np.isin(lab, list(edge_ids)) | pure
    original = np.asarray(img.convert("RGBA")).copy()
    # Dentro del objeto se conserva el color original; en el borde se usa el recorte suave
    inside = ~ndimage.binary_dilation(background, iterations=2)
    keyed[inside] = original[inside]
    return Image.fromarray(keyed, "RGBA")


def remove_grid_lines(img):
    """Borra líneas de cuadrícula dibujadas por la IA (filas/columnas casi completas que no son magenta)."""
    a = np.asarray(img).astype(int).copy()
    not_bg = np.sqrt(((a - np.array(g.CHROMA)) ** 2).sum(-1)) > 120
    def thin_lines(frac):
        # Una línea de cuadrícula cruza casi toda la imagen y mide pocos píxeles de ancho
        hits = np.nonzero(frac > 0.95)[0]
        runs, start = [], None
        for k, v in enumerate(hits):
            if start is None:
                start = v
            if k == len(hits) - 1 or hits[k + 1] != v + 1:
                if v - start <= 4:
                    runs.extend(range(start, v + 1))
                start = None
        return runs

    cols = thin_lines(not_bg.mean(0))
    rows = thin_lines(not_bg.mean(1))
    for x in cols:
        a[:, max(0, x - 1):x + 2] = g.CHROMA
    for y in rows:
        a[max(0, y - 1):y + 2] = g.CHROMA
    if len(cols) or len(rows):
        print(f"Cuadrícula borrada: {len(cols)} columnas, {len(rows)} filas")
    return Image.fromarray(a.astype(np.uint8))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("sheet")
    ap.add_argument("prefix")
    ap.add_argument("--cols", type=int, default=6)
    ap.add_argument("--rows", type=int, default=2)
    ap.add_argument("--cell", type=int, nargs=2, default=[460, 460], help="Tamaño de salida de cada fotograma.")
    args = ap.parse_args()

    img = remove_grid_lines(Image.open(args.sheet).convert("RGB"))
    keyed = np.asarray(border_key(img))
    alpha = keyed[..., 3] > 8
    W, H = img.size
    cw, ch = W / args.cols, H / args.rows

    frames, metrics = [], []
    cuts = None
    # Un personaje por componente (la utilería puede salirse de su celda); se ordenan por fila y columna
    lab, n = ndimage.label(ndimage.binary_dilation(alpha, iterations=4))
    sizes = ndimage.sum(alpha, lab, range(1, n + 1))
    ids = [i + 1 for i, v in enumerate(sizes) if v > 0.15 * max(sizes)]
    boxes = []
    for i in ids:
        ys, xs = np.nonzero(lab == i)
        boxes.append((i, ys.max(), (xs.min() + xs.max()) / 2))
    widths = []
    for i, _, _ in boxes:
        xs = np.nonzero((lab == i).any(axis=0))[0]
        widths.append(xs.max() - xs.min())
    merged = len(widths) > 2 and max(widths) > 1.6 * float(np.median(widths))
    if len(boxes) < args.cols * args.rows and merged:
        # Personajes que se tocan: se separan por la cuadrícula nominal
        print(f"Aviso: {len(boxes)} personajes detectados, se esperaban {args.cols * args.rows}; se corta por cuadrícula")
        lab = np.zeros(alpha.shape, dtype=np.int32)
        boxes = []
        for r in range(args.rows):
            for c in range(args.cols):
                k = r * args.cols + c + 1
                cell = np.zeros_like(alpha)
                cell[int(r * ch):int((r + 1) * ch), int(c * cw):int((c + 1) * cw)] = True
                lab[cell & alpha] = k
                ys, xs = np.nonzero(cell & alpha)
                if len(ys):
                    boxes.append((k, int(r * ch) + ys.max() - int(r * ch), (xs.min() + xs.max()) / 2))
        cuts = [(r + 1) * ch for r in range(args.rows - 1)]
    elif len(boxes) != args.cols * args.rows:
        print(f"Aviso: {len(boxes)} personajes detectados, se esperaban {args.cols * args.rows}")
    # Filas reales: se agrupan los pies por saltos verticales grandes (las filas pueden traer distinto número)
    bottoms = sorted(b[1] for b in boxes)
    if cuts is None:
        cuts = [(bottoms[k] + bottoms[k + 1]) / 2 for k in range(len(bottoms) - 1) if bottoms[k + 1] - bottoms[k] > 0.3 * ch]
    row_of = lambda b: sum(b[1] > c for c in cuts)
    boxes.sort(key=lambda b: (row_of(b), b[2]))
    for i, _, _ in boxes:
        mask = (lab == i) & alpha
        ys, xs = np.nonzero(mask)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        crop = keyed[y0:y1, x0:x1].copy()
        crop[..., 3] = np.where(mask[y0:y1, x0:x1], crop[..., 3], 0)
        bottom = ys.max() - y0
        # Ancla horizontal en el torso (banda 25-55 % desde arriba): los pies se separan al caminar y harían saltar el encuadre
        top, hgt = ys.min(), ys.max() - ys.min()
        band = xs[(ys > top + 0.25 * hgt) & (ys < top + 0.55 * hgt)] - x0
        anchor_x = int(np.median(band))
        frames.append((Image.fromarray(crop, "RGBA"), anchor_x, bottom))
        metrics.append((ys.max() - ys.min(), xs.max() - xs.min(), mask))

    out_w, out_h = args.cell
    out = []
    for crop, ax, bottom in frames:
        canvas = Image.new("RGBA", (out_w, out_h), (0, 0, 0, 0))
        canvas.alpha_composite(crop, (out_w // 2 - ax, out_h - 20 - bottom))
        out.append(canvas)

    prefix = Path(args.prefix)
    prefix.parent.mkdir(parents=True, exist_ok=True)
    for i, f in enumerate(out):
        f.save(f"{prefix}_{i:02d}.png")

    heights = np.array([m[0] for m in metrics])
    print(f"fotogramas={len(out)} altura media={heights.mean():.0f}px variación={heights.std() / heights.mean() * 100:.1f}%")
    masks = [np.asarray(f)[..., 3] > 8 for f in out]
    diffs = []
    for i in range(len(masks)):
        a, b = masks[i], masks[(i + 1) % len(masks)]
        diffs.append(1 - (a & b).sum() / max((a | b).sum(), 1))
    print("cambio entre consecutivos (%):", " ".join(f"{d * 100:.0f}" for d in diffs))
    d = np.array(diffs)
    pops = [i + 1 for i, v in enumerate(d) if v > d.mean() + 1.6 * d.std()]
    print("posibles saltos después del fotograma:", pops if pops else "ninguno")


if __name__ == "__main__":
    main()
