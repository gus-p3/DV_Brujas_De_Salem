"""
Generador de activos visuales para la aldea explorable e interiores del Nivel 2 (Salem, siglo XVII).
Sigue la dirección de arte oficial del proyecto (Docs/Arte/Direccion_de_Arte.md):
- Resolución base: 64 PPU (dibujado a 2x con supersampling Lanczos para trazos limpios).
- Contorno cálido oscuro (#1D0F0C), paleta colonial de maderas de roble, piedra, estuco ocre y hierro forjado.

Genera en Assets/Art/Environment/Level2/:
- Village_Ladder.png                Escalera de madera vertical repetible
- Village_Balcony_Platform.png      Balcón colonial con barandilla de madera y ménsulas
- Village_Door_Closed.png           Puerta de roble y herrajes cerrada
- Village_Door_Open.png             Puerta entreabierta con cálido resplandor interior
- Village_Door_Locked.png           Puerta con cerrojo pesado de hierro
- Village_House_Facade_A.png        Fachada de casa colonial de 2 pisos con entramado (Botica / Posada)
- Village_House_Facade_B.png        Fachada de casa de madera oscura y piedra (Casa Abandonada / Mansión)
- Interior_Wall_Timber.png          Muro interior de estuco y vigas de madera vistas
- Interior_Wall_Stone.png           Muro interior de mampostería de piedra para sótanos y plantas bajas
- Interior_Floor_Planks.png         Forjado sólido de tablones de madera con vigas inferiores
- Interior_Fireplace.png            Chimenea colonial de piedra con brasas encendidas
- Interior_Bookshelf.png            Estantería alta de roble con grimorios y frascos
- Interior_Table_Chairs.png         Mesa de madera con candelabro y pergaminos
- Interior_Bed.png                  Cama del siglo XVII con cabecero tallado y colcha
- Interior_Wardrobe.png             Armario ropero antiguo de madera
- Interior_Cauldron_Rack.png        Caldero de botica y ristra de hierbas secas
- Interior_Window_Shutters.png      Ventana interior emplomada con contraventanas de madera
- Interior_Chandelier.png           Lámpara de techo de hierro forjado con velas
- Interactive_Chest_Closed.png      Cofre de madera y hierro cerrado
- Interactive_Chest_Open.png        Cofre abierto con destello dorado
- Interactive_Key.png               Llave antigua de hierro forjado y bronce
- Interactive_LoreBook.png          Atril con grimorio/crónica abierta
"""

import os
from PIL import Image, ImageDraw

OUT_DIR = "Assets/Art/Environment/Level2"
os.makedirs(OUT_DIR, exist_ok=True)

OUTLINE = (29, 15, 12, 255)
WOOD_DARK = (68, 39, 24, 255)
WOOD_MID = (112, 68, 38, 255)
WOOD_LIGHT = (158, 102, 56, 255)
WOOD_HI = (198, 140, 84, 255)

PLASTER_DARK = (172, 158, 136, 255)
PLASTER_MID = (214, 200, 176, 255)
PLASTER_LIGHT = (238, 228, 206, 255)

STONE_DARK = (58, 54, 62, 255)
STONE_MID = (92, 86, 96, 255)
STONE_LIGHT = (132, 124, 136, 255)

IRON_DARK = (32, 30, 38, 255)
IRON_MID = (68, 66, 78, 255)
GOLD_DARK = (168, 116, 24, 255)
GOLD_MID = (224, 172, 46, 255)
GOLD_HI = (255, 224, 112, 255)

WARM_GLOW = (255, 198, 92, 255)
FIRE_ORANGE = (242, 118, 34, 255)
FIRE_YELLOW = (255, 220, 98, 255)


def save_aa(img_2x, name):
    w2, h2 = img_2x.size
    out = img_2x.resize((w2 // 2, h2 // 2), Image.LANCZOS)
    path = os.path.join(OUT_DIR, name)
    out.save(path)
    print("Generado:", path, out.size)


def create_ladder():
    # 64 x 128 px final (1 u ancho x 2 u alto a 64 PPU)
    w, h = 128, 256
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Largueros verticales
    for lx in (24, 88):
        d.rounded_rectangle([lx, 0, lx + 16, h], radius=4, fill=WOOD_MID, outline=OUTLINE, width=4)
        d.line([lx + 5, 4, lx + 5, h - 4], fill=WOOD_LIGHT, width=3)
    # Peldaños horizontales (4 por cada 2 unidades = cada 0.5 u)
    for ry in (28, 92, 156, 220):
        d.rounded_rectangle([18, ry, 110, ry + 14], radius=4, fill=WOOD_LIGHT, outline=OUTLINE, width=4)
        d.line([24, ry + 4, 104, ry + 4], fill=WOOD_HI, width=2)
    save_aa(img, "Village_Ladder.png")


def create_balcony_platform():
    # 256 x 96 px final (4 u ancho x 1.5 u alto)
    w, h = 512, 192
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Barandilla superior y balaustres
    d.rounded_rectangle([12, 16, w - 12, 30], radius=4, fill=WOOD_LIGHT, outline=OUTLINE, width=4)
    for bx in range(28, w - 28, 36):
        d.rectangle([bx, 28, bx + 12, 96], fill=WOOD_MID, outline=OUTLINE, width=3)
    # Plataforma principal (suelo del balcón)
    d.rounded_rectangle([6, 92, w - 6, 128], radius=6, fill=WOOD_MID, outline=OUTLINE, width=5)
    d.line([14, 98, w - 14, 98], fill=WOOD_HI, width=4)
    # Ménsulas inferiores de soporte
    for sx in (48, w // 2 - 16, w - 80):
        pts = [(sx, 128), (sx + 32, 128), (sx + 20, 182), (sx + 8, 182)]
        d.polygon(pts, fill=WOOD_DARK, outline=OUTLINE)
    save_aa(img, "Village_Balcony_Platform.png")


def create_doors():
    # 96 x 144 px final (1.5 u ancho x 2.25 u alto)
    w, h = 192, 288
    for mode, fname in (("closed", "Village_Door_Closed.png"), ("open", "Village_Door_Open.png"), ("locked", "Village_Door_Locked.png")):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        # Marco de piedra / madera
        d.rounded_rectangle([12, 12, w - 12, h - 4], radius=18, fill=STONE_MID, outline=OUTLINE, width=5)
        d.rounded_rectangle([26, 26, w - 26, h - 4], radius=10, fill=WOOD_DARK if mode != "open" else WARM_GLOW, outline=OUTLINE, width=5)

        if mode == "open":
            # Interior cálido iluminado + hoja entreabierta en perspectiva
            d.rectangle([34, 34, w - 34, h - 8], fill=WARM_GLOW)
            d.rectangle([48, 44, w - 48, h - 16], fill=FIRE_YELLOW)
            leaf = [(28, 28), (92, 42), (92, h - 12), (28, h - 6)]
            d.polygon(leaf, fill=WOOD_MID, outline=OUTLINE)
            d.line([56, 36, 56, h - 10], fill=OUTLINE, width=3)
        else:
            # Tablones verticales de la puerta
            d.rectangle([30, 30, w - 30, h - 6], fill=WOOD_MID, outline=OUTLINE, width=4)
            for px in (64, 96, 128):
                d.line([px, 32, px, h - 8], fill=OUTLINE, width=3)
            # Herrajes de hierro
            for hy in (76, 210):
                d.rectangle([30, hy, w - 30, hy + 14], fill=IRON_DARK, outline=OUTLINE, width=3)
            # Picaporte
            d.ellipse([w - 64, 138, w - 44, 158], fill=GOLD_MID, outline=OUTLINE, width=3)
            if mode == "locked":
                # Candado pesado y barra cruzada
                d.line([34, 90, w - 34, 200], fill=IRON_MID, width=12)
                d.rounded_rectangle([w // 2 - 22, 126, w // 2 + 22, 174], radius=6, fill=IRON_DARK, outline=GOLD_MID, width=4)
                d.ellipse([w // 2 - 6, 142, w // 2 + 6, 154], fill=GOLD_HI)
        save_aa(img, fname)


def create_house_facades():
    # 512 x 448 px final (8 u ancho x 7 u alto)
    w, h = 1024, 896
    for variant, fname in (("A", "Village_House_Facade_A.png"), ("B", "Village_House_Facade_B.png")):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        wall_col = PLASTER_MID if variant == "A" else WOOD_MID
        wall_hi = PLASTER_LIGHT if variant == "A" else WOOD_LIGHT
        roof_col = (128, 52, 36, 255) if variant == "A" else (64, 68, 82, 255)

        # Planta baja (y = 560..890) y Segundo piso con voladizo (y = 240..560)
        d.rectangle([64, 550, w - 64, h - 6], fill=wall_col, outline=OUTLINE, width=6)
        # Zócalo de piedra en planta baja
        d.rectangle([64, 780, w - 64, h - 6], fill=STONE_MID, outline=OUTLINE, width=5)
        # Segundo piso con ligero voladizo colonial (jetty)
        d.rectangle([44, 240, w - 44, 556], fill=wall_col, outline=OUTLINE, width=6)

        # Entramado de vigas de madera (timber framing)
        beams_x = [64, 260, 512, 764, w - 64]
        for bx in beams_x:
            d.rectangle([bx - 10, 244, bx + 10, h - 8], fill=WOOD_DARK, outline=OUTLINE, width=3)
        for by in (244, 548, 780):
            d.rectangle([44, by - 10, w - 44, by + 10], fill=WOOD_DARK, outline=OUTLINE, width=3)
        # Riostras diagonales en 2º piso
        d.line([54, 540, 180, 254], fill=WOOD_DARK, width=16)
        d.line([w - 54, 540, w - 180, 254], fill=WOOD_DARK, width=16)

        # Ventanas iluminadas en planta baja y segundo piso
        win_positions = [(150, 620), (800, 620), (150, 340), (472, 340), (800, 340)]
        for wx, wy in win_positions:
            d.rectangle([wx - 24, wy, wx + 104, wy + 108], fill=WOOD_DARK, outline=OUTLINE, width=4)
            d.rectangle([wx, wy + 8, wx + 80, wy + 100], fill=WARM_GLOW, outline=OUTLINE, width=4)
            d.line([wx + 40, wy + 8, wx + 40, wy + 100], fill=WOOD_DARK, width=5)
            d.line([wx, wy + 54, wx + 80, wy + 54], fill=WOOD_DARK, width=5)

        # Chimenea y tejado a dos aguas (y = 20..248)
        d.rectangle([w - 220, 24, w - 140, 244], fill=(132, 48, 34, 255), outline=OUTLINE, width=5)
        roof_pts = [(16, 248), (w // 2, 28), (w - 16, 248)]
        d.polygon(roof_pts, fill=roof_col, outline=OUTLINE)
        for ry in range(60, 240, 28):
            ratio = (ry - 28) / 220.0
            half_span = int((w // 2 - 20) * ratio)
            d.line([w // 2 - half_span, ry, w // 2 + half_span, ry], fill=OUTLINE, width=3)
        d.line(roof_pts + [roof_pts[0]], fill=OUTLINE, width=7)

        save_aa(img, fname)


def create_interior_walls_and_floors():
    # Muro interior de entramado: 256 x 256 px final (4 u x 4 u)
    w, h = 512, 512
    img = Image.new("RGBA", (w, h), PLASTER_MID)
    d = ImageDraw.Draw(img)
    # Zócalo de madera inferior
    d.rectangle([0, h - 96, w, h], fill=WOOD_MID, outline=OUTLINE, width=4)
    for x in range(0, w, 64):
        d.line([x, h - 96, x, h], fill=OUTLINE, width=3)
    # Vigas verticales y horizontales
    for x in (0, 256, 504):
        d.rectangle([x, 0, x + 16, h - 96], fill=WOOD_DARK, outline=OUTLINE, width=3)
    d.rectangle([0, 0, w, 20], fill=WOOD_DARK, outline=OUTLINE, width=3)
    save_aa(img, "Interior_Wall_Timber.png")

    # Muro interior de piedra (sótano): 256 x 256 px final
    img_s = Image.new("RGBA", (w, h), STONE_DARK)
    ds = ImageDraw.Draw(img_s)
    for row in range(0, h, 64):
        offset = 32 if (row // 64) % 2 == 1 else 0
        for col in range(-32, w, 96):
            x0 = max(0, col + offset)
            x1 = min(w, col + offset + 90)
            if x1 > x0 + 10:
                ds.rounded_rectangle([x0, row + 4, x1, row + 58], radius=8, fill=STONE_MID, outline=OUTLINE, width=4)
    save_aa(img_s, "Interior_Wall_Stone.png")

    # Suelo / forjado de tablones de madera: 256 x 32 px final (4 u x 0.5 u)
    fw, fh = 512, 64
    img_f = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    df = ImageDraw.Draw(img_f)
    df.rounded_rectangle([2, 4, fw - 2, fh - 4], radius=6, fill=WOOD_MID, outline=OUTLINE, width=5)
    df.line([8, 12, fw - 8, 12], fill=WOOD_HI, width=4)
    for px in range(64, fw, 64):
        df.line([px, 6, px, fh - 6], fill=OUTLINE, width=4)
    save_aa(img_f, "Interior_Floor_Planks.png")


def create_interior_furniture():
    # 1. Chimenea (160 x 192 px final -> 2.5 u x 3 u)
    w, h = 320, 384
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.polygon([(60, 8), (w - 60, 8), (w - 28, 160), (28, 160)], fill=STONE_MID, outline=OUTLINE)
    d.rounded_rectangle([16, 152, w - 16, h - 4], radius=8, fill=STONE_MID, outline=OUTLINE, width=5)
    d.rounded_rectangle([8, 144, w - 8, 174], radius=6, fill=WOOD_DARK, outline=OUTLINE, width=5)
    # Hogar con fuego
    d.rounded_rectangle([64, 210, w - 64, h - 8], radius=16, fill=(28, 18, 18, 255), outline=OUTLINE, width=5)
    d.polygon([(100, h - 14), (135, 250), (160, 290), (190, 240), (220, h - 14)], fill=FIRE_ORANGE, outline=OUTLINE)
    d.polygon([(125, h - 14), (155, 275), (185, 268), (200, h - 14)], fill=FIRE_YELLOW)
    save_aa(img, "Interior_Fireplace.png")

    # 2. Estantería de libros y pociones (128 x 160 px final -> 2 u x 2.5 u)
    w, h = 256, 320
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([12, 8, w - 12, h - 4], radius=8, fill=WOOD_DARK, outline=OUTLINE, width=5)
    for sy in (80, 156, 232, 304):
        d.rectangle([20, sy, w - 20, sy + 14], fill=WOOD_LIGHT, outline=OUTLINE, width=4)
    # Libros y frascos en los estantes
    colors = [(164, 42, 42, 255), (46, 98, 142, 255), (68, 128, 58, 255), GOLD_MID, (124, 64, 140, 255)]
    for idx, sy in enumerate((80, 156, 232)):
        x = 30
        for b in range(7):
            bw = 22
            bh = 44 + (b % 3) * 6
            col = colors[(idx + b) % len(colors)]
            d.rectangle([x, sy - bh, x + bw, sy], fill=col, outline=OUTLINE, width=3)
            x += bw + 4
    save_aa(img, "Interior_Bookshelf.png")

    # 3. Mesa con velas y pergaminos (160 x 96 px final -> 2.5 u x 1.5 u)
    w, h = 320, 192
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Tablero y patas
    d.rounded_rectangle([20, 76, w - 20, 100], radius=6, fill=WOOD_LIGHT, outline=OUTLINE, width=5)
    d.rectangle([44, 100, 68, h - 4], fill=WOOD_MID, outline=OUTLINE, width=4)
    d.rectangle([w - 68, 100, w - 44, h - 4], fill=WOOD_MID, outline=OUTLINE, width=4)
    d.rectangle([68, 140, w - 68, 154], fill=WOOD_DARK, outline=OUTLINE, width=3)
    # Vela y botella sobre la mesa
    d.rectangle([90, 42, 108, 76], fill=PLASTER_LIGHT, outline=OUTLINE, width=3)
    d.ellipse([92, 22, 106, 42], fill=FIRE_YELLOW, outline=FIRE_ORANGE, width=2)
    d.ellipse([190, 38, 232, 76], fill=(72, 148, 182, 255), outline=OUTLINE, width=3)
    save_aa(img, "Interior_Table_Chairs.png")

    # 4. Cama colonial (160 x 96 px final -> 2.5 u x 1.5 u)
    w, h = 320, 192
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([16, 24, 48, h - 4], radius=6, fill=WOOD_DARK, outline=OUTLINE, width=5)
    d.rounded_rectangle([w - 44, 64, w - 16, h - 4], radius=6, fill=WOOD_DARK, outline=OUTLINE, width=5)
    d.rounded_rectangle([40, 88, w - 36, 144], radius=10, fill=(154, 48, 48, 255), outline=OUTLINE, width=4)
    d.rounded_rectangle([48, 68, 112, 98], radius=10, fill=PLASTER_LIGHT, outline=OUTLINE, width=4)
    d.rectangle([32, 136, w - 28, 162], fill=WOOD_MID, outline=OUTLINE, width=4)
    save_aa(img, "Interior_Bed.png")

    # 5. Armario (96 x 160 px final -> 1.5 u x 2.5 u)
    w, h = 192, 320
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([16, 12, w - 16, h - 4], radius=8, fill=WOOD_MID, outline=OUTLINE, width=5)
    d.rectangle([28, 30, w // 2 - 4, h - 24], fill=WOOD_DARK, outline=OUTLINE, width=4)
    d.rectangle([w // 2 + 4, 30, w - 28, h - 24], fill=WOOD_DARK, outline=OUTLINE, width=4)
    d.ellipse([w // 2 - 20, h // 2 - 8, w // 2 - 8, h // 2 + 8], fill=GOLD_MID, outline=OUTLINE, width=2)
    d.ellipse([w // 2 + 8, h // 2 - 8, w // 2 + 20, h // 2 + 8], fill=GOLD_MID, outline=OUTLINE, width=2)
    save_aa(img, "Interior_Wardrobe.png")

    # 6. Caldero y hierbas (96 x 96 px final -> 1.5 u x 1.5 u)
    w, h = 192, 192
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([36, 68, w - 36, h - 12], fill=IRON_DARK, outline=OUTLINE, width=5)
    d.rounded_rectangle([30, 62, w - 30, 82], radius=8, fill=IRON_MID, outline=OUTLINE, width=4)
    d.ellipse([48, 56, w - 48, 76], fill=(92, 212, 118, 255), outline=OUTLINE, width=3)
    save_aa(img, "Interior_Cauldron_Rack.png")

    # 7. Ventana interior con contraventanas (96 x 96 px final -> 1.5 u x 1.5 u)
    w, h = 192, 192
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([38, 24, w - 38, h - 24], fill=(135, 195, 245, 255), outline=OUTLINE, width=5)
    d.line([w // 2, 24, w // 2, h - 24], fill=WOOD_DARK, width=5)
    d.line([38, h // 2, w - 38, h // 2], fill=WOOD_DARK, width=5)
    d.rectangle([8, 20, 38, h - 20], fill=WOOD_MID, outline=OUTLINE, width=4)
    d.rectangle([w - 38, 20, w - 8, h - 20], fill=WOOD_MID, outline=OUTLINE, width=4)
    save_aa(img, "Interior_Window_Shutters.png")

    # 8. Lámpara de techo (96 x 64 px final -> 1.5 u x 1 u)
    w, h = 192, 128
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.line([w // 2, 0, w // 2, 72], fill=IRON_DARK, width=6)
    d.line([36, 72, w - 36, 72], fill=IRON_MID, width=8)
    for cx in (46, w // 2, w - 46):
        d.rectangle([cx - 6, 48, cx + 6, 72], fill=PLASTER_LIGHT, outline=OUTLINE, width=3)
        d.ellipse([cx - 8, 30, cx + 8, 48], fill=FIRE_YELLOW, outline=FIRE_ORANGE, width=2)
    save_aa(img, "Interior_Chandelier.png")


def create_interactive_items():
    # 1. Cofre cerrado y abierto (80 x 64 px final -> 1.25 u x 1 u)
    w, h = 160, 128
    for opened, fname in ((False, "Interactive_Chest_Closed.png"), (True, "Interactive_Chest_Open.png")):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        if opened:
            # Tapa abierta arriba y resplandor de tesoro
            d.rounded_rectangle([18, 10, w - 18, 52], radius=10, fill=WOOD_DARK, outline=OUTLINE, width=5)
            d.ellipse([28, 38, w - 28, 86], fill=GOLD_HI, outline=GOLD_MID, width=4)
            d.rounded_rectangle([14, 56, w - 14, h - 6], radius=8, fill=WOOD_MID, outline=OUTLINE, width=5)
        else:
            d.rounded_rectangle([14, 24, w - 14, h - 6], radius=14, fill=WOOD_MID, outline=OUTLINE, width=5)
            d.line([14, 62, w - 14, 62], fill=OUTLINE, width=5)
        # Bandas de hierro y cerradura dorada
        for bx in (38, w - 50):
            d.rectangle([bx, 24 if not opened else 56, bx + 12, h - 6], fill=IRON_MID, outline=OUTLINE, width=3)
        d.rounded_rectangle([w // 2 - 14, 50, w // 2 + 14, 78], radius=4, fill=GOLD_MID, outline=OUTLINE, width=4)
        save_aa(img, fname)

    # 2. Llave antigua (48 x 48 px final -> 0.75 u x 0.75 u)
    w, h = 96, 96
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([12, 24, 48, 60], fill=GOLD_MID, outline=OUTLINE, width=5)
    d.ellipse([22, 34, 38, 50], fill=(0, 0, 0, 0), outline=OUTLINE, width=4)
    d.rectangle([46, 38, 84, 48], fill=GOLD_HI, outline=OUTLINE, width=4)
    d.rectangle([64, 48, 72, 64], fill=GOLD_MID, outline=OUTLINE, width=3)
    d.rectangle([76, 48, 84, 60], fill=GOLD_MID, outline=OUTLINE, width=3)
    save_aa(img, "Interactive_Key.png")

    # 3. Grimorio / Crónica en atril (64 x 80 px final -> 1 u x 1.25 u)
    w, h = 128, 160
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Atril de madera
    d.rectangle([54, 64, 74, h - 8], fill=WOOD_DARK, outline=OUTLINE, width=4)
    d.rounded_rectangle([32, h - 22, 96, h - 4], radius=6, fill=WOOD_MID, outline=OUTLINE, width=4)
    # Libro abierto
    d.polygon([(14, 52), (64, 62), (64, 22), (14, 14)], fill=PLASTER_LIGHT, outline=OUTLINE)
    d.polygon([(64, 62), (114, 52), (114, 14), (64, 22)], fill=PLASTER_MID, outline=OUTLINE)
    d.line([14, 52, 64, 62, 114, 52], fill=GOLD_MID, width=5)
    save_aa(img, "Interactive_LoreBook.png")


if __name__ == "__main__":
    create_ladder()
    create_balcony_platform()
    create_doors()
    create_house_facades()
    create_interior_walls_and_floors()
    create_interior_furniture()
    create_interactive_items()
    print("Todos los activos de aldea e interiores generados exitosamente.")
