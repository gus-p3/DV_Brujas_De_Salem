import os
import math
from PIL import Image, ImageDraw

def draw_heart_shape(draw, center_x, center_y, scale, fill_color, outline_color, outline_width=4, angle_deg=0, alpha_mult=1.0):
    # Genera puntos de corazón usando ecuación paramétrica
    # x = 16 sin^3(t)
    # y = 13 cos(t) - 5 cos(2t) - 2 cos(3t) - cos(4t)
    points = []
    steps = 100
    rad_rot = math.radians(angle_deg)
    cos_rot = math.cos(rad_rot)
    sin_rot = math.sin(rad_rot)

    for i in range(steps):
        t = math.pi * 2 * i / steps
        # Invertimos Y para coordenadas de pantalla
        hx = 16 * (math.sin(t) ** 3)
        hy = -(13 * math.cos(t) - 5 * math.cos(2*t) - 2 * math.cos(3*t) - math.cos(4*t))
        
        # Escala
        sx = hx * scale
        sy = hy * scale

        # Rotación
        rx = sx * cos_rot - sy * sin_rot
        ry = sx * sin_rot + sy * cos_rot

        points.append((center_x + rx, center_y + ry))

    # Aplicar alpha multiplier si se requiere
    if len(fill_color) == 4:
        fill = (fill_color[0], fill_color[1], fill_color[2], int(fill_color[3] * alpha_mult))
    else:
        fill = (fill_color[0], fill_color[1], fill_color[2], int(255 * alpha_mult))

    if outline_color and len(outline_color) == 4:
        outline = (outline_color[0], outline_color[1], outline_color[2], int(outline_color[3] * alpha_mult))
    elif outline_color:
        outline = (outline_color[0], outline_color[1], outline_color[2], int(255 * alpha_mult))
    else:
        outline = None

    draw.polygon(points, fill=fill, outline=outline, width=outline_width)

def generate_heart_break():
    # 6 cuadros de 128x128 -> 768x128
    width = 128 * 6
    height = 128
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    
    red_main = (225, 45, 60, 255)
    red_shadow = (160, 20, 35, 255)
    outline = (30, 15, 25, 255)
    highlight = (255, 170, 180, 255)

    for frame in range(6):
        ox = frame * 128
        frame_img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        d = ImageDraw.Draw(frame_img)

        cx, cy = 64, 64

        if frame == 0:
            # Corazón entero normal
            draw_heart_shape(d, cx, cy, 2.5, red_main, outline, 4)
            # Sombra y brillo
            d.ellipse([cx - 24, cy - 22, cx - 10, cy - 8], fill=highlight)
            d.ellipse([cx - 22, cy - 20, cx - 14, cy - 12], fill=(255, 255, 255, 255))
        elif frame == 1:
            # Pequeña grieta central
            draw_heart_shape(d, cx, cy, 2.5, red_main, outline, 4)
            d.ellipse([cx - 24, cy - 22, cx - 10, cy - 8], fill=highlight)
            # Grieta
            crack = [(cx, cy - 25), (cx + 3, cy - 10), (cx - 4, cy + 2), (cx + 2, cy + 15)]
            d.line(crack, fill=outline, width=3)
        elif frame == 2:
            # Grieta que divide de arriba a abajo
            draw_heart_shape(d, cx, cy, 2.5, red_main, outline, 4)
            crack = [(cx, cy - 30), (cx + 5, cy - 12), (cx - 6, cy + 4), (cx + 4, cy + 20), (cx, cy + 35)]
            d.line(crack, fill=outline, width=4)
            # Pequeñas astillas
            d.line([(cx + 5, cy - 12), (cx + 12, cy - 8)], fill=outline, width=2)
            d.line([(cx - 6, cy + 4), (cx - 14, cy + 2)], fill=outline, width=2)
        elif frame == 3:
            # Dos mitades separándose levemente
            # Mitad izquierda
            left_half = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
            ld = ImageDraw.Draw(left_half)
            draw_heart_shape(ld, 64, 64, 2.5, red_main, outline, 4)
            # Cortar lado derecho con máscara
            for x in range(64, 128):
                for y in range(128):
                    left_half.putpixel((x, y), (0, 0, 0, 0))
            # Rotar y desplazar
            left_rot = left_half.rotate(5, center=(64, 64), resample=Image.Resampling.BILINEAR)
            frame_img.paste(left_rot, (-5, 2), left_rot)

            # Mitad derecha
            right_half = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
            rd = ImageDraw.Draw(right_half)
            draw_heart_shape(rd, 64, 64, 2.5, red_shadow, outline, 4)
            for x in range(0, 64):
                for y in range(128):
                    right_half.putpixel((x, y), (0, 0, 0, 0))
            right_rot = right_half.rotate(-5, center=(64, 64), resample=Image.Resampling.BILINEAR)
            frame_img.paste(right_rot, (5, 2), right_rot)
        elif frame == 4:
            # Mitades más separadas y cayendo
            left_half = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
            ld = ImageDraw.Draw(left_half)
            draw_heart_shape(ld, 64, 64, 2.5, red_main, outline, 4, alpha_mult=0.75)
            for x in range(64, 128):
                for y in range(128):
                    left_half.putpixel((x, y), (0, 0, 0, 0))
            left_rot = left_half.rotate(14, center=(64, 64), resample=Image.Resampling.BILINEAR)
            frame_img.paste(left_rot, (-14, 14), left_rot)

            right_half = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
            rd = ImageDraw.Draw(right_half)
            draw_heart_shape(rd, 64, 64, 2.5, red_shadow, outline, 4, alpha_mult=0.75)
            for x in range(0, 64):
                for y in range(128):
                    right_half.putpixel((x, y), (0, 0, 0, 0))
            right_rot = right_half.rotate(-14, center=(64, 64), resample=Image.Resampling.BILINEAR)
            frame_img.paste(right_rot, (14, 14), right_rot)
        elif frame == 5:
            # Mitades casi invisibles cayendo
            left_half = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
            ld = ImageDraw.Draw(left_half)
            draw_heart_shape(ld, 64, 64, 2.5, red_main, outline, 4, alpha_mult=0.25)
            for x in range(64, 128):
                for y in range(128):
                    left_half.putpixel((x, y), (0, 0, 0, 0))
            left_rot = left_half.rotate(24, center=(64, 64), resample=Image.Resampling.BILINEAR)
            frame_img.paste(left_rot, (-24, 28), left_rot)

            right_half = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
            rd = ImageDraw.Draw(right_half)
            draw_heart_shape(rd, 64, 64, 2.5, red_shadow, outline, 4, alpha_mult=0.25)
            for x in range(0, 64):
                for y in range(128):
                    right_half.putpixel((x, y), (0, 0, 0, 0))
            right_rot = right_half.rotate(-24, center=(64, 64), resample=Image.Resampling.BILINEAR)
            frame_img.paste(right_rot, (24, 28), right_rot)

        img.paste(frame_img, (ox, 0), frame_img)

    out_path = "Assets/Art/UI/Heart_Break.png"
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    img.save(out_path)
    print("Guardado:", out_path)

def generate_heart_heal():
    # 4 cuadros de 128x128 -> 512x128
    width = 128 * 4
    height = 128
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))

    outline = (30, 15, 25, 255)
    red_main = (235, 50, 70, 255)
    gold = (255, 215, 0, 255)
    highlight = (255, 180, 190, 255)

    for frame in range(4):
        ox = frame * 128
        frame_img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        d = ImageDraw.Draw(frame_img)
        cx, cy = 64, 64

        if frame == 0:
            # Corazón vacío / silueta tenue
            draw_heart_shape(d, cx, cy, 2.3, (80, 20, 30, 80), outline, 3)
        elif frame == 1:
            # Empieza a llenarse con brillo dorado en el centro
            draw_heart_shape(d, cx, cy, 2.4, red_main, outline, 4)
            d.ellipse([cx - 15, cy - 15, cx + 15, cy + 15], fill=(255, 240, 150, 180))
            d.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], fill=(255, 255, 255, 230))
        elif frame == 2:
            # Pulso expandido (escala 2.75) con destellos mágicos
            # Rayos dorados de fondo
            for angle in [0, 45, 90, 135, 180, 225, 270, 315]:
                rad = math.radians(angle)
                x1 = cx + math.cos(rad) * 36
                y1 = cy + math.sin(rad) * 36
                x2 = cx + math.cos(rad) * 48
                y2 = cy + math.sin(rad) * 48
                d.line([(x1, y1), (x2, y2)], fill=(255, 230, 100, 220), width=3)

            draw_heart_shape(d, cx, cy, 2.75, (255, 60, 80, 255), outline, 4)
            d.ellipse([cx - 26, cy - 24, cx - 12, cy - 10], fill=highlight)
            d.ellipse([cx - 24, cy - 22, cx - 16, cy - 14], fill=(255, 255, 255, 255))
        elif frame == 3:
            # Corazón lleno normal con chispitas estelares
            draw_heart_shape(d, cx, cy, 2.5, red_main, outline, 4)
            d.ellipse([cx - 24, cy - 22, cx - 10, cy - 8], fill=highlight)
            d.ellipse([cx - 22, cy - 20, cx - 14, cy - 12], fill=(255, 255, 255, 255))
            # Estrellitas / destello
            # Estrella 1
            sx, sy = cx + 24, cy - 20
            d.line([(sx - 8, sy), (sx + 8, sy)], fill=gold, width=2)
            d.line([(sx, sy - 8), (sx, sy + 8)], fill=gold, width=2)
            d.ellipse([sx - 2, sy - 2, sx + 2, sy + 2], fill=(255, 255, 255, 255))

        img.paste(frame_img, (ox, 0), frame_img)

    out_path = "Assets/Art/UI/Heart_Heal.png"
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    img.save(out_path)
    print("Guardado:", out_path)

def generate_item_heart():
    # 128x128
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = 64, 64

    # Aura dorada suave
    for r in range(48, 30, -3):
        alpha = int(120 * (1.0 - (r - 30) / 18.0))
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(255, 220, 80, alpha))

    # Corazón
    outline = (30, 15, 25, 255)
    red_main = (235, 45, 65, 255)
    highlight = (255, 185, 195, 255)
    draw_heart_shape(d, cx, cy, 2.3, red_main, outline, 4)
    d.ellipse([cx - 22, cy - 20, cx - 10, cy - 8], fill=highlight)
    d.ellipse([cx - 20, cy - 18, cx - 14, cy - 12], fill=(255, 255, 255, 255))

    # Destellos brillantes estilo mágico
    gold = (255, 235, 120, 255)
    for sx, sy in [(cx + 22, cy - 22), (cx - 24, cy + 18)]:
        d.line([(sx - 7, sy), (sx + 7, sy)], fill=gold, width=2)
        d.line([(sx, sy - 7), (sx, sy + 7)], fill=gold, width=2)
        d.ellipse([sx - 2, sy - 2, sx + 2, sy + 2], fill=(255, 255, 255, 255))

    out_path = "Assets/Art/Environment/Item_Heart.png"
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    img.save(out_path)
    print("Guardado:", out_path)

def draw_lantern_post(d, cx, cy, is_lit, flame_frame=0):
    # Dibuja poste rústico de madera y farol de hierro
    # Dimensiones del cuadro: 192 x 256
    outline = (25, 15, 20, 255)
    wood_brown = (110, 68, 42, 255)
    wood_shadow = (75, 45, 28, 255)
    iron_dark = (50, 52, 60, 255)
    iron_light = (85, 90, 100, 255)

    base_y = 245
    post_top_y = 65
    post_w = 20

    # Base en tierra/piedras
    d.ellipse([cx - 35, base_y - 12, cx + 35, base_y + 8], fill=(60, 65, 55, 255), outline=outline, width=3)
    d.ellipse([cx - 25, base_y - 8, cx + 25, base_y + 4], fill=(95, 100, 85, 255))

    # Poste de madera
    d.rectangle([cx - post_w//2, post_top_y, cx + post_w//2, base_y], fill=wood_brown, outline=outline, width=3)
    # Detalle veta madera
    d.rectangle([cx - post_w//2, post_top_y, cx - 2, base_y], fill=wood_shadow)

    # Brazo horizontal del que cuelga el farol
    arm_y = post_top_y + 15
    arm_end_x = cx + 55
    d.rectangle([cx - post_w//2, arm_y, arm_end_x, arm_y + 12], fill=wood_brown, outline=outline, width=3)
    # Soporte diagonal
    d.polygon([(cx, arm_y + 25), (cx + 30, arm_y + 12), (cx + 22, arm_y + 12), (cx, arm_y + 35)], fill=wood_shadow)

    # Cadena del farol
    chain_x = arm_end_x - 10
    d.line([(chain_x, arm_y + 12), (chain_x, arm_y + 30)], fill=iron_dark, width=3)

    # Farol de hierro
    lx = chain_x
    ly = arm_y + 32
    # Techo del farol (pirámide)
    d.polygon([(lx, ly), (lx - 22, ly + 18), (lx + 22, ly + 18)], fill=iron_dark, outline=outline, width=3)
    # Base del farol
    by = ly + 65
    d.polygon([(lx - 16, by), (lx + 16, by), (lx + 20, by + 10), (lx - 20, by + 10)], fill=iron_dark, outline=outline, width=3)

    # Vidrio del farol
    glass_color = (255, 235, 150, 230) if is_lit else (80, 95, 110, 160)
    d.polygon([(lx - 20, ly + 18), (lx + 20, ly + 18), (lx + 15, by), (lx - 15, by)], fill=glass_color, outline=outline, width=3)

    if is_lit:
        # Resplandor exterior suave
        glow_r = 60 + (flame_frame % 2) * 5
        glow_img = Image.new("RGBA", (192, 256), (0, 0, 0, 0))
        gd = ImageDraw.Draw(glow_img)
        for r in range(glow_r, 20, -5):
            alpha = int(70 * (1.0 - (r - 20) / (glow_r - 20)))
            gd.ellipse([lx - r, ly + 40 - r, lx + r, ly + 40 + r], fill=(255, 210, 60, alpha))
        
        # Llama viva en el interior
        fx_offset = [-2, 2, 0, -1][flame_frame % 4]
        fy_offset = [0, -2, -1, 1][flame_frame % 4]
        fcx = lx + fx_offset
        fcy = ly + 42 + fy_offset

        # Llama exterior naranja
        d.ellipse([fcx - 10, fcy - 12, fcx + 10, fcy + 10], fill=(255, 120, 20, 255))
        # Llama media dorada
        d.ellipse([fcx - 7, fcy - 9, fcx + 7, fcy + 7], fill=(255, 210, 40, 255))
        # Núcleo blanco
        d.ellipse([fcx - 3, fcy - 5, fcx + 3, fcy + 3], fill=(255, 255, 255, 255))
    else:
        # Mecha apagada
        d.line([(lx, ly + 45), (lx, ly + 36)], fill=(35, 25, 25, 255), width=3)

def generate_checkpoint():
    # 6 cuadros de 192x256 -> 1152x256
    # 2 apagados, 4 encendidos
    width = 192 * 6
    height = 256
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))

    for frame in range(6):
        ox = frame * 192
        frame_img = Image.new("RGBA", (192, 256), (0, 0, 0, 0))
        d = ImageDraw.Draw(frame_img)
        cx = 96
        cy = 128

        if frame < 2:
            # Apagado
            draw_lantern_post(d, cx, cy, is_lit=False)
        else:
            # Encendido animado
            flame_idx = frame - 2
            draw_lantern_post(d, cx, cy, is_lit=True, flame_frame=flame_idx)

        img.paste(frame_img, (ox, 0), frame_img)

    out_path = "Assets/Art/Environment/Env_Checkpoint.png"
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    img.save(out_path)
    print("Guardado:", out_path)

def generate_ui_icons():
    # ui_icon_heart_full.png y ui_icon_heart_empty.png (128x128)
    outline = (30, 15, 25, 255)
    red_main = (225, 45, 60, 255)
    highlight = (255, 170, 180, 255)
    
    # Lleno
    img_full = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    df = ImageDraw.Draw(img_full)
    cx, cy = 64, 64
    draw_heart_shape(df, cx, cy, 2.5, red_main, outline, 4)
    df.ellipse([cx - 24, cy - 22, cx - 10, cy - 8], fill=highlight)
    df.ellipse([cx - 22, cy - 20, cx - 14, cy - 12], fill=(255, 255, 255, 255))
    img_full.save("Assets/Art/UI/ui_icon_heart_full.png")
    # Y reemplazar o sincronizar con ui_icon_heart.png
    img_full.save("Assets/Art/UI/ui_icon_heart.png")

    # Vacío
    img_empty = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    de = ImageDraw.Draw(img_empty)
    draw_heart_shape(de, cx, cy, 2.5, (45, 25, 35, 140), outline, 4)
    # Grieta tenue interior
    de.line([(cx, cy - 20), (cx + 3, cy - 8), (cx - 3, cy + 5), (cx, cy + 18)], fill=(30, 15, 25, 160), width=3)
    img_empty.save("Assets/Art/UI/ui_icon_heart_empty.png")
    print("Iconos de UI de corazón generados.")

if __name__ == "__main__":
    generate_heart_break()
    generate_heart_heal()
    generate_item_heart()
    generate_checkpoint()
    generate_ui_icons()
    print("Todos los sprites generados exitosamente.")
