"""
Placeholders de fondo del Nivel 2 (Pueblo y mercado de Salem, 1692).

Se usan mientras no haya arte final generado con gemini_art.py (la cuota de imagen de la key está agotada).
Sigue Docs/Arte/Direccion_de_Arte.md: contorno #1D0F0C grueso, rellenos planos con una sombra y un brillo,
luz desde arriba a la izquierda y paleta otoñal apagada.

Genera, a 64 PPU (se dibuja al doble y se reduce para suavizar bordes):
- Placeholder_Village_Facades_Mid.png       fila de casas con entramado y tablas, techos de paja y tejamanil,
                                            chimeneas, letreros de hierro con pictogramas y faroles de pared.
                                            Repetible en horizontal (los bordes izquierdo y derecho casan).
- Placeholder_Village_Facades_Mid_Glow.png  solo ventanas y faroles, para dibujarlos con material sin luz
                                            encima de la capa base y que brillen de noche.
- Placeholder_Canal_Water.png               agua del canal de la plaza (antes el empedrado tapaba el hueco).

Uso (desde la raíz del repo):  python ArtSource/Environment/Level2/generate_level2_background.py
"""

import os
import random

from PIL import Image, ImageDraw

OUT_DIR = "Assets/Art/Environment/Level2"
PPU = 64
S = 2  # supersampling

WIDTH_U, HEIGHT_U = 32, 7
W, H = WIDTH_U * PPU, HEIGHT_U * PPU

OUTLINE = (29, 15, 12, 255)
SLATE = (74, 91, 112)


def tone(rgb, k=0.28):
    """Acerca un color al azul pizarra: la capa media queda detrás de la jugabilidad sin perder la paleta."""
    return tuple(int(c * (1 - k) + s * k) for c, s in zip(rgb, SLATE)) + (255,)


# Materiales: (sombra, base, brillo)
PLASTER = [tone((185, 178, 163)), tone((230, 224, 210)), tone((255, 251, 240))]
OCHRE_PLASTER = [tone((150, 112, 52)), tone((201, 154, 46)), tone((235, 196, 90))]
BEAM = [tone((46, 26, 20)), tone((74, 42, 32)), tone((110, 69, 45))]
CLAPBOARD = [tone((74, 42, 32)), tone((110, 69, 45)), tone((144, 92, 55))]
SLATE_BOARD = [tone((46, 58, 74)), tone((74, 91, 112)), tone((104, 122, 146))]
THATCH = [tone((112, 82, 40)), tone((150, 116, 60)), tone((196, 158, 92))]
SHINGLE = [tone((52, 49, 58)), tone((85, 81, 92)), tone((118, 112, 124))]
BRICK = [tone((90, 36, 26)), tone((122, 46, 31)), tone((168, 69, 43))]
DOOR = [tone((46, 26, 20)), tone((74, 42, 32)), tone((110, 69, 45))]
IRON = (30, 28, 34, 255)
SIGN_ICON = tone((201, 154, 46), 0.15)

GLOW_PANE = (255, 196, 96, 255)
GLOW_CORE = (255, 232, 160, 255)
GLOW_FRAME = (120, 64, 30, 255)


def sc(v):
    return int(round(v * S))


def box(x0, y0, x1, y1):
    return [sc(x0), sc(y0), sc(x1), sc(y1)]


def poly(pts):
    return [(sc(x), sc(y)) for x, y in pts]


LINE = 4  # grosor del contorno a 64 PPU


class Painter:
    def __init__(self):
        self.base = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
        self.glow = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.base)
        self.g = ImageDraw.Draw(self.glow)
        self.signs = 0

    def rect(self, x0, y0, x1, y1, fill, width=LINE):
        self.d.rectangle(box(x0, y0, x1, y1), fill=fill, outline=OUTLINE, width=sc(width / 2))

    def polygon(self, pts, fill, width=LINE):
        self.d.polygon(poly(pts), fill=fill)
        self.d.line(poly(pts + [pts[0]]), fill=OUTLINE, width=sc(width / 2), joint="curve")

    def line(self, pts, fill=OUTLINE, width=LINE):
        self.d.line(poly(pts), fill=fill, width=sc(width / 2), joint="curve")

    # ------------------------------------------------------------ piezas

    def window(self, x, y, w, h, diamond=True, shutters=None):
        """Ventana con cristales encendidos (en la capa glow) y marco de madera."""
        if shutters:
            self.rect(x - w * 0.45, y, x, y + h, shutters[1], width=3)
            self.rect(x + w, y, x + w * 1.45, y + h, shutters[1], width=3)
            for sx in (x - w * 0.45, x + w):
                self.line([(sx + 3, y + h * 0.5), (sx + w * 0.45 - 3, y + h * 0.5)], shutters[0], 2)
        self.rect(x - 3, y - 3, x + w + 3, y + h + 3, BEAM[1], width=3)
        self.d.rectangle(box(x, y, x + w, y + h), fill=(40, 30, 34, 255))
        # El cristal se pinta en su propia imagen para que el emplomado no se salga del hueco
        pane = Image.new("RGBA", (sc(w), sc(h)), GLOW_PANE)
        pd = ImageDraw.Draw(pane)
        pd.rectangle([sc(w * 0.2), sc(h * 0.2), sc(w * 0.8), sc(h * 0.85)], fill=GLOW_CORE)
        if diamond:
            step = max(7, w / 3)
            k = -h
            while k < w + h:
                pd.line([(sc(k), 0), (sc(k + h), sc(h))], fill=GLOW_FRAME, width=sc(1))
                pd.line([(sc(k + h), 0), (sc(k), sc(h))], fill=GLOW_FRAME, width=sc(1))
                k += step
        else:
            pd.line([(sc(w / 2), 0), (sc(w / 2), sc(h))], fill=GLOW_FRAME, width=sc(1.5))
            pd.line([(0, sc(h / 2)), (sc(w), sc(h / 2))], fill=GLOW_FRAME, width=sc(1.5))
        self.glow.paste(pane, (sc(x), sc(y)))
        self.line([(x - 5, y + h + 4), (x + w + 5, y + h + 4)], BEAM[2], 3)

    def door(self, x, w, h, ground):
        y = ground - h
        self.d.rectangle(box(x, y + w / 2, x + w, ground), fill=DOOR[1])
        self.d.pieslice(box(x, y, x + w, y + w), 180, 360, fill=DOOR[1])
        self.d.arc(box(x, y, x + w, y + w), 180, 360, fill=OUTLINE, width=sc(2))
        self.line([(x, y + w / 2), (x, ground)], width=4)
        self.line([(x + w, y + w / 2), (x + w, ground)], width=4)
        for k in range(1, 3):
            self.line([(x + w * k / 3, y + w * 0.3), (x + w * k / 3, ground)], DOOR[0], 2)
        self.line([(x + 3, y + w * 0.55), (x + 3, ground - 4)], DOOR[2], 2)
        self.d.ellipse(box(x + w * 0.72, ground - h * 0.48, x + w * 0.84, ground - h * 0.40), fill=SIGN_ICON)

    def chimney(self, x, top, w, h):
        self.rect(x, top, x + w, top + h, BRICK[1])
        self.rect(x - 4, top - 6, x + w + 4, top + 2, BRICK[0])
        for r in range(1, int(h // 10)):
            yy = top + r * 10
            self.line([(x + 2, yy), (x + w - 2, yy)], BRICK[0], 1.5)
        self.line([(x + 3, top + 4), (x + 3, top + h - 2)], BRICK[2], 2)

    def gable_roof(self, x0, x1, base, peak, mat, thatch):
        mid = (x0 + x1) / 2
        pts = [(x0 - 14, base), (mid, peak), (x1 + 14, base)]
        self.polygon(pts, mat[1])
        # Sombra en el faldón derecho, brillo en el borde superior izquierdo
        self.d.polygon(poly([(mid, peak + 6), (x1 + 10, base - 2), (mid + 6, base - 2)]), fill=mat[0])
        self.line([(x0 - 6, base - 6), (mid - 4, peak + 8)], mat[2], 4)
        self.texture_roof(pts, mat, thatch)
        self.line(pts, width=LINE)

    def side_roof(self, x0, x1, base, top, mat, thatch):
        inset = (base - top) * 0.55
        pts = [(x0 - 14, base), (x0 - 14 + inset, top), (x1 + 14 - inset, top), (x1 + 14, base)]
        self.polygon(pts, mat[1])
        self.d.polygon(poly([(x1 + 14 - inset, top + 4), (x1 + 10, base - 2), (x1 - inset * 0.5, base - 2)]), fill=mat[0])
        self.line([(x0 - 8 + inset, top + 4), (x1 + 6 - inset, top + 4)], mat[2], 4)
        self.texture_roof(pts, mat, thatch)
        self.line(pts + [pts[0]], width=LINE)

    def texture_roof(self, pts, mat, thatch):
        ys = [p[1] for p in pts]
        xs = [p[0] for p in pts]
        top, bottom = min(ys), max(ys)
        rnd = random.Random(int(xs[0] * 7 + top))
        if thatch:
            # Haces de paja: trazos cortos en diagonal
            for _ in range(int((max(xs) - min(xs)) * (bottom - top) / 260)):
                px = rnd.uniform(min(xs) + 10, max(xs) - 10)
                py = rnd.uniform(top + 10, bottom - 6)
                if self.inside(pts, px, py) and self.inside(pts, px - 6, py + 10):
                    self.line([(px, py), (px - 6, py + 12)], mat[0], 2)
            # Borde inferior redondeado y grueso
            self.line([(pts[0][0] + 4, bottom - 3), (pts[-1][0] - 4, bottom - 3)], mat[2], 3)
        else:
            # Hileras de tejamanil
            row = top + 12
            while row < bottom - 4:
                self.clip_line(pts, row, mat[0])
                row += 11

    def clip_line(self, pts, y, color):
        xs = []
        n = len(pts)
        for i in range(n):
            (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
            if (y0 - y) * (y1 - y) < 0:
                xs.append(x0 + (y - y0) * (x1 - x0) / (y1 - y0))
        if len(xs) >= 2:
            xs.sort()
            self.line([(xs[0] + 6, y), (xs[-1] - 6, y)], color, 2)
            for k in range(int(xs[0]) + 18, int(xs[-1]) - 6, 20):
                off = 10 if int(y) % 22 < 11 else 0
                self.line([(k + off, y), (k + off, y - 9)], color, 1.5)

    @staticmethod
    def inside(pts, x, y):
        c = False
        n = len(pts)
        for i in range(n):
            (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
            if (y0 > y) != (y1 > y) and x < (x1 - x0) * (y - y0) / (y1 - y0) + x0:
                c = not c
        return c

    def hanging_sign(self, x, y, icon, facing=1):
        arm = 40 * facing
        self.line([(x, y), (x + arm, y)], IRON, 4)
        self.line([(x, y + 14), (x + arm * 0.6, y)], IRON, 3)
        sx = x + arm * 0.55
        self.line([(sx - 10, y), (sx - 10, y + 8)], IRON, 2)
        self.line([(sx + 10, y), (sx + 10, y + 8)], IRON, 2)
        self.rect(sx - 18, y + 8, sx + 18, y + 40, (52, 40, 44, 255), width=3)
        cx, cy = sx, y + 24
        d = self.d
        if icon == "boot":
            self.polygon([(cx - 6, cy - 10), (cx + 2, cy - 10), (cx + 2, cy + 2), (cx + 11, cy + 4), (cx + 11, cy + 10), (cx - 6, cy + 10)], SIGN_ICON, 2)
        elif icon == "bread":
            d.ellipse(box(cx - 12, cy - 6, cx + 12, cy + 8), fill=SIGN_ICON, outline=OUTLINE, width=sc(1))
            for k in (-5, 0, 5):
                self.line([(cx + k - 2, cy - 3), (cx + k + 2, cy + 4)], OUTLINE, 1.5)
        elif icon == "mug":
            self.rect(cx - 8, cy - 9, cx + 5, cy + 10, SIGN_ICON, 2)
            d.arc(box(cx + 1, cy - 5, cx + 12, cy + 6), 270, 90, fill=SIGN_ICON, width=sc(2.5))
        elif icon == "key":
            d.ellipse(box(cx - 12, cy - 6, cx - 2, cy + 4), outline=SIGN_ICON, width=sc(2.5))
            self.line([(cx - 2, cy - 1), (cx + 12, cy - 1)], SIGN_ICON, 3)
            self.line([(cx + 8, cy - 1), (cx + 8, cy + 6)], SIGN_ICON, 3)
            self.line([(cx + 12, cy - 1), (cx + 12, cy + 5)], SIGN_ICON, 3)

    def wall_lantern(self, x, y):
        self.line([(x, y), (x + 14, y)], IRON, 3)
        self.line([(x + 14, y), (x + 14, y + 6)], IRON, 2)
        self.rect(x + 7, y + 6, x + 21, y + 24, (40, 30, 34, 255), width=2.5)
        self.d.polygon(poly([(x + 5, y + 7), (x + 14, y + 1), (x + 23, y + 7)]), fill=IRON)
        self.g.rectangle(box(x + 9, y + 8, x + 19, y + 22), fill=GLOW_PANE)
        self.g.rectangle(box(x + 11, y + 11, x + 17, y + 19), fill=GLOW_CORE)

    # ------------------------------------------------------------ casas

    def house(self, x, w, rnd):
        ground = H
        floors = rnd.choice([2, 2, 3])
        floor_h = rnd.uniform(78, 92)
        body_top = ground - floors * floor_h
        timber = rnd.random() < 0.6
        wall = rnd.choice([PLASTER, PLASTER, OCHRE_PLASTER]) if timber else rnd.choice([CLAPBOARD, CLAPBOARD, SLATE_BOARD])
        jetty = timber and floors >= 2 and rnd.random() < 0.7

        # Planta baja y plantas superiores (con voladizo opcional)
        self.rect(x, ground - floor_h, x + w, ground, wall[1])
        ux0, ux1 = (x - 10, x + w + 10) if jetty else (x, x + w)
        self.rect(ux0, body_top, ux1, ground - floor_h, wall[1])
        if jetty:
            self.d.rectangle(box(x, ground - floor_h, x + w, ground - floor_h + 8), fill=wall[0])
            for k in range(int(ux0) + 12, int(ux1) - 6, 26):
                self.polygon([(k, ground - floor_h), (k + 8, ground - floor_h), (k + 8, ground - floor_h + 10), (k + 2, ground - floor_h + 10)], BEAM[1], 2)
        # Sombra a la derecha del volumen (luz arriba a la izquierda) y brillo arriba a la izquierda
        self.d.rectangle(box(ux1 - 12, body_top + 3, ux1 - 2, ground - 2), fill=wall[0])
        self.line([(ux0 + 5, body_top + 5), (ux0 + 5, body_top + 40)], wall[2], 3)

        if timber:
            for fy in range(floors + 1):
                yy = ground - fy * floor_h
                self.line([(ux0 if fy else x, yy), (ux1 if fy else x + w, yy)], BEAM[1], 7)
            for k in range(int(ux0), int(ux1) + 1, max(40, int(w / rnd.choice([4, 5, 6])))):
                self.line([(k, body_top), (k, ground - floor_h)], BEAM[1], 6)
            for k in range(int(x), int(x + w) + 1, max(40, int(w / 4))):
                self.line([(k, ground - floor_h), (k, ground)], BEAM[1], 6)
            for fy in range(1, floors):
                y0, y1 = ground - (fy + 1) * floor_h, ground - fy * floor_h
                self.line([(ux0 + 4, y1 - 4), (ux0 + 30, y0 + 6)], BEAM[1], 5)
                self.line([(ux1 - 4, y1 - 4), (ux1 - 30, y0 + 6)], BEAM[1], 5)
            self.rect(ux0, body_top, ux1, ground - floor_h, None)
        else:
            yy = body_top + 12
            while yy < ground - 6:
                self.line([(ux0 + 3 if yy < ground - floor_h else x + 3, yy), (ux1 - 3 if yy < ground - floor_h else x + w - 3, yy)], wall[0], 2)
                yy += 12
            self.line([(ux0 + 4, body_top), (ux0 + 4, ground)], BEAM[1], 6)
            self.line([(ux1 - 4, body_top), (ux1 - 4, ground)], BEAM[1], 6)

        # Ventanas
        for fy in range(floors):
            yy = ground - (fy + 1) * floor_h + floor_h * 0.28
            n = max(1, int(w // 85))
            span = (ux1 - ux0) if fy else w
            left = ux0 if fy else x
            for i in range(n):
                cx = left + span * (i + 0.5) / n
                if fy == 0 and i == n // 2 and rnd.random() < 0.85:
                    continue  # hueco para la puerta
                ww, wh = rnd.choice([(26, 34), (30, 30), (22, 38)])
                self.window(cx - ww / 2, yy, ww, wh, diamond=timber,
                            shutters=(BEAM if rnd.random() < 0.35 else None))
        door_w = 34
        self.door(x + w / 2 - door_w / 2, door_w, 62, ground)

        # Tejado
        mat_thatch = rnd.random() < 0.5
        mat = THATCH if mat_thatch else SHINGLE
        peak = body_top - rnd.uniform(90, 140)
        if rnd.random() < 0.55:
            self.gable_roof(ux0, ux1, body_top + 4, peak, mat, mat_thatch)
            if rnd.random() < 0.7:
                cx = ux0 + (ux1 - ux0) * rnd.uniform(0.62, 0.78)
                self.chimney(cx, peak + (cx - (ux0 + ux1) / 2) * (body_top - peak) / ((ux1 - ux0) / 2) - 26, 22, 44)
        else:
            top = body_top - rnd.uniform(60, 90)
            cx = ux0 + (ux1 - ux0) * rnd.uniform(0.2, 0.75)
            self.chimney(cx, top - 34, 24, 50)
            self.side_roof(ux0, ux1, body_top + 4, top, mat, mat_thatch)
            if rnd.random() < 0.6:  # buhardilla
                dx = ux0 + (ux1 - ux0) * rnd.uniform(0.3, 0.6)
                dy = body_top - (body_top - top) * 0.55
                self.rect(dx, dy, dx + 34, body_top + 2, wall[1], 3)
                self.gable_roof(dx, dx + 34, dy + 2, dy - 26, mat, mat_thatch)
                self.window(dx + 8, dy + 8, 18, 20, diamond=True)

        # Detalles de calle
        if rnd.random() < 0.6:
            facing = rnd.choice([-1, 1])
            sx = ux0 + 6 if facing < 0 else ux1 - 6
            self.signs += 1  # rotan los oficios para que no se repita el mismo letrero
            self.hanging_sign(sx, ground - floor_h - 34, ["boot", "bread", "mug", "key"][self.signs % 4], facing)
        if rnd.random() < 0.45:
            self.wall_lantern(x + w * rnd.choice([0.15, 0.8]), ground - floor_h * 0.95)


def canal_water():
    """Agua del canal (256 x 192 px = 4 x 3 u), repetible en horizontal: borde superior con espuma y ondas."""
    cw, ch = 256, 192
    img = Image.new("RGBA", (cw * S, ch * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    deep = tone((34, 46, 66), 0.1)
    mid = tone((52, 70, 96), 0.1)
    light = tone((96, 122, 150), 0.1)
    foam = tone((176, 190, 204), 0.1)
    # Superficie ondulada: dos periodos exactos en el ancho para que case al repetir
    import math
    top = [(x, 10 + 3 * math.sin(x / cw * 4 * math.pi)) for x in range(0, cw + 1, 4)]
    d.polygon([(sc(x), sc(y)) for x, y in top] + [(sc(cw), sc(ch)), (0, sc(ch))], fill=mid)
    d.rectangle([0, sc(46), sc(cw), sc(ch)], fill=deep)
    d.line([(sc(x), sc(y)) for x, y in top], fill=OUTLINE, width=sc(2))
    d.line([(sc(x), sc(y + 3)) for x, y in top], fill=foam, width=sc(1.5))
    rnd = random.Random(78)
    for _ in range(14):
        x = rnd.uniform(6, cw - 30)
        y = rnd.uniform(20, ch - 14)
        w = rnd.uniform(14, 28)
        d.line([(sc(x), sc(y)), (sc(x + w), sc(y))], fill=light if y < 46 else mid, width=sc(1.5))
    out = img.resize((cw, ch), Image.LANCZOS)
    out.save(os.path.join(OUT_DIR, "Placeholder_Canal_Water.png"))
    print("Guardado", os.path.join(OUT_DIR, "Placeholder_Canal_Water.png"), out.size)


def main():
    rnd = random.Random(1692)
    p = Painter()
    # Casas y callejones; empieza y termina con medio callejón para que la capa se repita sin costura
    alley_half = 14
    x = alley_half
    widths = []
    budget = W - 2 * alley_half
    while budget > 0:
        w = rnd.choice([170, 190, 210, 230, 250])
        gap = rnd.choice([0, 0, 0, 28])
        if budget - (w + gap) < 170:
            w = budget
            gap = 0
        widths.append((w, gap))
        budget -= w + gap
    for w, gap in widths:
        p.house(x, w - 20, rnd)  # 20 px para el alero de los tejados
        x += w + gap

    os.makedirs(OUT_DIR, exist_ok=True)
    for img, name in ((p.base, "Placeholder_Village_Facades_Mid.png"), (p.glow, "Placeholder_Village_Facades_Mid_Glow.png")):
        out = img.resize((W, H), Image.LANCZOS)
        out.save(os.path.join(OUT_DIR, name))
        print("Guardado", os.path.join(OUT_DIR, name), out.size)
    canal_water()


if __name__ == "__main__":
    main()
