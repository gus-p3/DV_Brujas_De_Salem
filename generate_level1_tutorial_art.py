import os
from PIL import Image, ImageDraw

def create_tutorial_art():
    env_dir = "Assets/Art/Environment"
    fx_dir = "Assets/Art/Effects"
    ui_dir = "Assets/Art/UI"
    os.makedirs(env_dir, exist_ok=True)
    os.makedirs(fx_dir, exist_ok=True)
    os.makedirs(ui_dir, exist_ok=True)

    outline_bark = (35, 24, 18, 255)
    bark_main = (92, 64, 45, 255)
    bark_shadow = (68, 46, 32, 255)
    wood_inside = (145, 110, 75, 255)
    wood_inside_shadow = (110, 80, 52, 255)
    moss_green = (95, 138, 55, 255)
    moss_shadow = (70, 105, 38, 255)

    def draw_outline_polygon(draw, points, fill, outline, width=3):
        draw.polygon(points, fill=fill, outline=outline)
        for i in range(len(points)):
            p1 = points[i]
            p2 = points[(i + 1) % len(points)]
            draw.line([p1, p2], fill=outline, width=width)

    # 1. Env_Log_Tunnel_Back.png (512x256) - Parte interior/trasera del tronco
    img_back = Image.new('RGBA', (512, 256), (0, 0, 0, 0))
    d_back = ImageDraw.Draw(img_back)
    # Fondo interior del tronco hueco
    inner_pts = [
        (40, 190), (120, 185), (380, 185), (470, 190),
        (460, 60), (370, 55), (130, 55), (50, 60)
    ]
    draw_outline_polygon(d_back, inner_pts, fill=wood_inside, outline=outline_bark, width=4)
    # Sombra superior interior
    d_back.polygon([(50, 60), (130, 55), (370, 55), (460, 60), (450, 95), (140, 90), (60, 95)], fill=wood_inside_shadow)
    # Anillos de corteza visibles al fondo
    d_back.line([(55, 65), (45, 185)], fill=bark_shadow, width=4)
    d_back.line([(455, 65), (465, 185)], fill=bark_shadow, width=4)
    img_back.save(os.path.join(env_dir, "Env_Log_Tunnel_Back.png"))
    print("Guardado Env_Log_Tunnel_Back.png")

    # 2. Env_Log_Tunnel_Front.png (512x256) - Parte exterior/delantera con orificio de entrada/salida
    img_front = Image.new('RGBA', (512, 256), (0, 0, 0, 0))
    d_front = ImageDraw.Draw(img_front)
    # Tronco superior masivo que tapa al humano
    roof_pts = [
        (30, 75), (256, 60), (480, 75),
        (495, 20), (256, 12), (15, 20)
    ]
    draw_outline_polygon(d_front, roof_pts, fill=bark_main, outline=outline_bark, width=4)
    # Musgo sobre el techo del tronco
    moss_roof = [
        (15, 20), (120, 14), (256, 12), (380, 14), (495, 20),
        (475, 34), (390, 26), (256, 30), (130, 26), (30, 36)
    ]
    draw_outline_polygon(d_front, moss_roof, fill=moss_green, outline=outline_bark, width=3)
    # Sombra inferior del techo
    d_front.polygon([(30, 75), (256, 60), (480, 75), (470, 65), (256, 52), (40, 65)], fill=bark_shadow)

    # Pilares laterales exteriores
    # Pilar izquierdo
    left_post = [(15, 20), (35, 75), (55, 190), (30, 240), (10, 235), (8, 100)]
    draw_outline_polygon(d_front, left_post, fill=bark_main, outline=outline_bark, width=4)
    # Pilar derecho
    right_post = [(495, 20), (475, 75), (455, 190), (480, 240), (502, 235), (504, 100)]
    draw_outline_polygon(d_front, right_post, fill=bark_main, outline=outline_bark, width=4)

    # Base delantera inferior del tronco (para dar profundidad al piso)
    base_pts = [
        (30, 240), (256, 230), (480, 240),
        (470, 215), (256, 210), (40, 215)
    ]
    draw_outline_polygon(d_front, base_pts, fill=bark_shadow, outline=outline_bark, width=3)
    # Detalles de corteza y vetas
    d_front.line([(180, 24), (220, 26)], fill=bark_shadow, width=3)
    d_front.line([(320, 25), (360, 27)], fill=bark_shadow, width=3)
    img_front.save(os.path.join(env_dir, "Env_Log_Tunnel_Front.png"))
    print("Guardado Env_Log_Tunnel_Front.png")

    # 3. Env_Ledge_Rock_A.png (256x96) - Repisa de roca/tronco con musgo
    img_ledge_a = Image.new('RGBA', (256, 96), (0, 0, 0, 0))
    d_la = ImageDraw.Draw(img_ledge_a)
    rock_main = (105, 108, 115, 255)
    rock_shadow = (75, 78, 85, 255)
    pts_la = [
        (12, 24), (244, 24), (235, 78), (200, 86), (128, 88), (45, 84), (16, 72)
    ]
    draw_outline_polygon(d_la, pts_la, fill=rock_main, outline=outline_bark, width=4)
    # Sombra angular en roca
    d_la.polygon([(128, 88), (200, 86), (235, 78), (220, 48), (140, 52)], fill=rock_shadow)
    # Capa superior de musgo
    moss_la = [
        (10, 24), (80, 20), (160, 18), (246, 24),
        (240, 36), (170, 30), (90, 34), (14, 34)
    ]
    draw_outline_polygon(d_la, moss_la, fill=moss_green, outline=outline_bark, width=3)
    img_ledge_a.save(os.path.join(env_dir, "Env_Ledge_Rock_A.png"))
    print("Guardado Env_Ledge_Rock_A.png")

    # 4. Env_Ledge_Rock_B.png (384x96) - Repisa ancha
    img_ledge_b = Image.new('RGBA', (384, 96), (0, 0, 0, 0))
    d_lb = ImageDraw.Draw(img_ledge_b)
    pts_lb = [
        (14, 24), (370, 24), (355, 76), (280, 86), (192, 88), (80, 84), (20, 72)
    ]
    draw_outline_polygon(d_lb, pts_lb, fill=rock_main, outline=outline_bark, width=4)
    d_lb.polygon([(192, 88), (280, 86), (355, 76), (340, 46), (210, 50)], fill=rock_shadow)
    moss_lb = [
        (12, 24), (120, 18), (240, 18), (372, 24),
        (366, 36), (260, 30), (130, 32), (18, 36)
    ]
    draw_outline_polygon(d_lb, moss_lb, fill=moss_green, outline=outline_bark, width=3)
    img_ledge_b.save(os.path.join(env_dir, "Env_Ledge_Rock_B.png"))
    print("Guardado Env_Ledge_Rock_B.png")

    # 5. FX_Sparkle.png (hoja horizontal de 4 cuadros de 128x128: 512x128) - Brillo dorado
    img_sparkle = Image.new('RGBA', (512, 128), (0, 0, 0, 0))
    d_sp = ImageDraw.Draw(img_sparkle)
    gold_center = (255, 248, 170, 255)
    gold_glow = (245, 195, 45, 230)
    sparkle_outline = (180, 125, 20, 255)

    def draw_star(draw, cx, cy, r_outer, r_inner):
        import math
        pts = []
        for i in range(8):
            angle = i * (math.pi / 4.0)
            r = r_outer if (i % 2 == 0) else r_inner
            x = cx + r * math.cos(angle)
            y = cy + r * math.sin(angle)
            pts.append((x, y))
        draw.polygon(pts, fill=gold_center, outline=sparkle_outline)
        # Líneas de brillo
        draw.line([cx - r_outer * 1.3, cy, cx + r_outer * 1.3, cy], fill=gold_glow, width=2)
        draw.line([cx, cy - r_outer * 1.3, cx, cy + r_outer * 1.3], fill=gold_glow, width=2)

    sizes = [(18, 7), (28, 11), (38, 15), (20, 8)]
    for idx, (r_out, r_in) in enumerate(sizes):
        cx = idx * 128 + 64
        cy = 64
        draw_star(d_sp, cx, cy, r_out, r_in)
        # Puntos de destello periféricos
        d_sp.ellipse([cx - 24, cy - 20, cx - 18, cy - 14], fill=gold_glow)
        d_sp.ellipse([cx + 22, cy + 18, cx + 28, cy + 24], fill=gold_glow)

    img_sparkle.save(os.path.join(fx_dir, "FX_Sparkle.png"))
    print("Guardado FX_Sparkle.png")

    # 6. Icon_CatOnly.png (128x128) - Silueta de gato colonial con símbolo de pase exclusivo
    img_cat = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    d_cat = ImageDraw.Draw(img_cat)
    # Círculo base dorado
    d_cat.ellipse([6, 6, 122, 122], fill=(235, 190, 60, 255), outline=outline_bark, width=4)
    # Silueta de gato
    cat_pts = [
        (38, 88), (90, 88), (86, 60), (74, 52), (78, 34), (68, 42),
        (60, 42), (50, 34), (54, 52), (42, 60)
    ]
    draw_outline_polygon(d_cat, cat_pts, fill=(30, 24, 28, 255), outline=outline_bark, width=3)
    # Ojos verdes del gato
    d_cat.ellipse([54, 48, 59, 54], fill=(80, 225, 110, 255))
    d_cat.ellipse([69, 48, 74, 54], fill=(80, 225, 110, 255))
    img_cat.save(os.path.join(ui_dir, "Icon_CatOnly.png"))
    print("Guardado Icon_CatOnly.png")

    # 7. Icon_FlightOnly.png (128x128) - Escoba voladora y alas mágicas
    img_fly = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    d_fly = ImageDraw.Draw(img_fly)
    d_fly.ellipse([6, 6, 122, 122], fill=(130, 185, 245, 255), outline=outline_bark, width=4)
    # Escoba estilizada en diagonal
    draw_outline_polygon(d_fly, [(30, 94), (94, 34), (98, 38), (34, 98)], fill=(125, 80, 45, 255), outline=outline_bark, width=3)
    # Paja de la escoba
    draw_outline_polygon(d_fly, [(22, 102), (38, 88), (42, 94), (26, 108)], fill=(230, 185, 60, 255), outline=outline_bark, width=3)
    # Destello de alas o viento
    d_fly.arc([35, 40, 75, 75], start=180, end=320, fill=(255, 255, 255, 255), width=3)
    d_fly.arc([60, 25, 100, 60], start=180, end=320, fill=(255, 255, 255, 255), width=3)
    img_fly.save(os.path.join(ui_dir, "Icon_FlightOnly.png"))
    print("Guardado Icon_FlightOnly.png")

if __name__ == '__main__':
    create_tutorial_art()
