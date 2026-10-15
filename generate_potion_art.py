import os
import math
from PIL import Image, ImageDraw

def draw_potion_bottle(draw, cx, cy, liquid_color, outline_color=(25, 15, 20, 255), width_scale=1.0):
    # Frasco de poción alquímica estilo cuento: base esférica, cuello estilizado y corcho
    glass_outline = outline_color
    glass_shine = (255, 255, 255, 200)
    cork_color = (160, 110, 70, 255)
    cork_dark = (110, 70, 40, 255)

    r = int(32 * width_scale)
    base_cy = cy + 12

    # Líquido interior (círculo)
    draw.ellipse([cx - r + 3, base_cy - r + 3, cx + r - 3, base_cy + r - 3], fill=liquid_color)

    # Brillo de líquido
    liq_highlight = (min(255, liquid_color[0] + 60), min(255, liquid_color[1] + 60), min(255, liquid_color[2] + 60), 220)
    draw.ellipse([cx - r + 8, base_cy - r + 8, cx - r + 24, base_cy - r + 24], fill=liq_highlight)

    # Contorno del bulbo de vidrio
    draw.ellipse([cx - r, base_cy - r, cx + r, base_cy + r], outline=glass_outline, width=4)

    # Cuello de vidrio
    neck_w = int(12 * width_scale)
    neck_top = cy - 26
    neck_bottom = base_cy - r + 6
    draw.rectangle([cx - neck_w, neck_top, cx + neck_w, neck_bottom], fill=(220, 235, 245, 140), outline=glass_outline, width=3)

    # Borde superior del cuello
    draw.ellipse([cx - neck_w - 3, neck_top - 4, cx + neck_w + 3, neck_top + 4], fill=(200, 220, 235, 255), outline=glass_outline, width=3)

    # Corcho
    cork_top = neck_top - 12
    draw.polygon([(cx - neck_w + 2, neck_top), (cx + neck_w - 2, neck_top), (cx + neck_w, cork_top), (cx - neck_w, cork_top)], fill=cork_color, outline=glass_outline, width=3)
    draw.line([(cx - neck_w + 2, cork_top + 4), (cx + neck_w - 2, cork_top + 4)], fill=cork_dark, width=2)

    # Reflejo curvo de luz sobre el bulbo de vidrio
    draw.arc([cx - r + 6, base_cy - r + 6, cx + r - 6, base_cy + r - 6], 130, 230, fill=glass_shine, width=3)

def generate_potion_items():
    # 128x128 frascos para el mundo
    os.makedirs("Assets/Art/Items", exist_ok=True)
    colors = {
        "Blind": (220, 225, 230, 255),        # Blanco plateado / niebla
        "Hypnotize": (175, 65, 225, 255),     # Violeta hipnótico
        "Sleep": (45, 130, 235, 255)          # Azul ensueño
    }

    for name, color in colors.items():
        img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)

        # Aura mágica suave
        cx, cy = 64, 60
        for radius in range(48, 25, -3):
            alpha = int(90 * (1.0 - (radius - 25) / 23.0))
            aura_col = (color[0], color[1], color[2], alpha)
            d.ellipse([cx - radius, cy - radius + 8, cx + radius, cy + radius + 8], fill=aura_col)

        draw_potion_bottle(d, cx, cy, color, width_scale=1.1)

        # Destellitos
        sparkle_col = (255, 255, 255, 240)
        for sx, sy in [(cx + 26, cy - 10), (cx - 24, cy + 24)]:
            d.line([(sx - 5, sy), (sx + 5, sy)], fill=sparkle_col, width=2)
            d.line([(sx, sy - 5), (sx, sy + 5)], fill=sparkle_col, width=2)

        out_path = f"Assets/Art/Items/PotionItem_{name}.png"
        img.save(out_path)
        print("Guardado:", out_path)

def generate_potion_projectiles():
    # 64x64 frascos compactos rotando
    os.makedirs("Assets/Art/Items", exist_ok=True)
    colors = {
        "Blind": (220, 225, 230, 255),
        "Hypnotize": (175, 65, 225, 255),
        "Sleep": (45, 130, 235, 255)
    }

    for name, color in colors.items():
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        cx, cy = 32, 30
        draw_potion_bottle(d, cx, cy, color, width_scale=0.6)
        out_path = f"Assets/Art/Items/PotionProjectile_{name}.png"
        img.save(out_path)
        print("Guardado:", out_path)

def generate_splashes():
    # Hoja horizontal de 4 cuadros de 256x256 -> 1024x256
    os.makedirs("Assets/Art/Effects", exist_ok=True)
    colors = {
        "Blind": (220, 230, 240, 255),
        "Hypnotize": (180, 70, 235, 255),
        "Sleep": (50, 140, 245, 255)
    }

    outline = (25, 15, 25, 255)

    for name, base_col in colors.items():
        img = Image.new("RGBA", (1024, 256), (0, 0, 0, 0))

        for frame in range(4):
            ox = frame * 256
            frame_img = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
            d = ImageDraw.Draw(frame_img)
            cx, cy = 128, 160

            progress = (frame + 1) / 4.0
            r_inner = int(20 + progress * 65)
            r_outer = int(35 + progress * 85)
            alpha = int(255 * (1.0 - frame * 0.22))
            col = (base_col[0], base_col[1], base_col[2], alpha)
            out_col = (outline[0], outline[1], outline[2], alpha)

            if frame == 0:
                # Impacto inicial: gotas brotando hacia arriba
                d.ellipse([cx - 25, cy - 20, cx + 25, cy + 20], fill=col, outline=out_col, width=4)
                # Gotas
                for dx, dy in [(-20, -35), (0, -45), (20, -32)]:
                    d.ellipse([cx + dx - 8, cy + dy - 8, cx + dx + 8, cy + dy + 8], fill=col, outline=out_col, width=3)
            elif frame == 1:
                # Onda expansiva con gotas salpicando
                d.ellipse([cx - r_inner, cy - int(r_inner * 0.6), cx + r_inner, cy + int(r_inner * 0.6)], fill=col, outline=out_col, width=4)
                # Gotas hacia afuera
                for ang in [30, 70, 110, 150, 210, 250, 290, 330]:
                    rad = math.radians(ang)
                    gx = cx + int(math.cos(rad) * r_outer)
                    gy = cy + int(math.sin(rad) * (r_outer * 0.6))
                    d.ellipse([gx - 9, gy - 9, gx + 9, gy + 9], fill=col, outline=out_col, width=3)
            elif frame == 2:
                # Salpicadura amplia dispersándose
                d.ellipse([cx - r_inner - 10, cy - int(r_inner * 0.5), cx + r_inner + 10, cy + int(r_inner * 0.5)], fill=(col[0], col[1], col[2], int(alpha*0.6)), outline=out_col, width=3)
                for ang in [20, 60, 100, 140, 180, 220, 260, 300, 340]:
                    rad = math.radians(ang)
                    gx = cx + int(math.cos(rad) * (r_outer + 15))
                    gy = cy + int(math.sin(rad) * ((r_outer + 15) * 0.55))
                    d.ellipse([gx - 7, gy - 7, gx + 7, gy + 7], fill=col, outline=out_col, width=2)
            elif frame == 3:
                # Gotitas tenues desvaneciéndose
                fade_col = (col[0], col[1], col[2], int(alpha * 0.35))
                fade_out = (out_col[0], out_col[1], out_col[2], int(alpha * 0.35))
                for ang in [30, 80, 130, 230, 280, 330]:
                    rad = math.radians(ang)
                    gx = cx + int(math.cos(rad) * (r_outer + 25))
                    gy = cy + int(math.sin(rad) * ((r_outer + 25) * 0.5))
                    d.ellipse([gx - 5, gy - 5, gx + 5, gy + 5], fill=fade_col, outline=fade_out, width=2)

            img.paste(frame_img, (ox, 0), frame_img)

        out_path = f"Assets/Art/Effects/Splash_{name}.png"
        img.save(out_path)
        print("Guardado:", out_path)

def generate_status_icons():
    # 64x64 cada uno: Icon_Zzz, Icon_Spiral, Icon_Blinded
    os.makedirs("Assets/Art/Effects", exist_ok=True)
    outline = (25, 15, 20, 255)

    # 1. Icon_Zzz (Dormir)
    img_zzz = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img_zzz)
    blue_cyan = (100, 210, 255, 255)
    # Dibujar tres Z de distinto tamaño
    def draw_z(dx, dy, size, color):
        pts = [
            (dx, dy), (dx + size, dy),
            (dx, dy + size), (dx + size, dy + size)
        ]
        d.line([pts[0], pts[1]], fill=color, width=3)
        d.line([pts[1], pts[2]], fill=color, width=3)
        d.line([pts[2], pts[3]], fill=color, width=3)

    draw_z(14, 34, 12, (80, 170, 240, 255))
    draw_z(26, 20, 16, blue_cyan)
    draw_z(40, 6, 20, (180, 240, 255, 255))
    img_zzz.save("Assets/Art/Effects/Icon_Zzz.png")
    print("Guardado: Assets/Art/Effects/Icon_Zzz.png")

    # 2. Icon_Spiral (Hipnotizado)
    img_spiral = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img_spiral)
    cx, cy = 32, 32
    violet = (210, 100, 255, 255)
    pts = []
    for deg in range(0, 720, 10):
        rad = math.radians(deg)
        r = (deg / 720.0) * 24.0
        x = cx + math.cos(rad) * r
        y = cy + math.sin(rad) * r
        pts.append((x, y))
    for i in range(len(pts) - 1):
        d.line([pts[i], pts[i+1]], fill=violet, width=3)
    img_spiral.save("Assets/Art/Effects/Icon_Spiral.png")
    print("Guardado: Assets/Art/Effects/Icon_Spiral.png")

    # 3. Icon_Blinded (Cegado - Ojos tapados/cruzados)
    img_blind = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img_blind)
    gray_white = (240, 240, 245, 255)
    # Silueta de ojo
    d.arc([14, 18, 50, 46], 0, 360, fill=gray_white, width=3)
    # Pupila tenue
    d.ellipse([27, 27, 37, 37], fill=(120, 130, 140, 255))
    # Barra diagonal roja de bloqueo
    d.line([(12, 14), (52, 50)], fill=(230, 45, 55, 255), width=4)
    img_blind.save("Assets/Art/Effects/Icon_Blinded.png")
    print("Guardado: Assets/Art/Effects/Icon_Blinded.png")

if __name__ == "__main__":
    generate_potion_items()
    generate_potion_projectiles()
    generate_splashes()
    generate_status_icons()
    print("Todo el arte de pociones y efectos generado exitosamente.")
