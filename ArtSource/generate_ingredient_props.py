import os
from PIL import Image, ImageDraw

out_dir = "Assets/Art/Environment/Level2"
os.makedirs(out_dir, exist_ok=True)

OUTLINE = (24, 18, 22, 255)

def draw_round_rect(draw, box, fill, outline, width=2, radius=4):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)

# 1. Hongo / Mushroom (64 x 64)
def create_mushroom():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    
    # Aura sutil mágica
    d.ellipse([10, 8, 54, 52], fill=(160, 80, 220, 45))
    
    # Tallo
    d.polygon([(26, 32), (38, 32), (41, 56), (23, 56)], fill=(230, 225, 215, 255), outline=OUTLINE)
    d.polygon([(28, 33), (32, 33), (31, 55), (27, 55)], fill=(255, 255, 255, 255))
    # Sombras del tallo
    d.polygon([(34, 33), (37, 33), (40, 55), (36, 55)], fill=(195, 185, 175, 255))
    
    # Láminas bajo el sombrero
    d.chord([14, 26, 50, 42], start=0, end=180, fill=(200, 150, 80, 255), outline=OUTLINE)
    
    # Sombrero de hongo (púrpura / bermellón con volumen)
    d.chord([12, 10, 52, 40], start=180, end=360, fill=(180, 45, 75, 255), outline=OUTLINE)
    d.chord([16, 12, 48, 36], start=180, end=360, fill=(225, 65, 95, 255))
    
    # Motas doradas / crema
    spots = [
        (22, 16, 26, 20),
        (34, 14, 40, 20),
        (18, 24, 23, 28),
        (42, 22, 46, 26),
        (28, 22, 33, 27)
    ]
    for s in spots:
        d.ellipse(s, fill=(255, 235, 170, 255), outline=OUTLINE)
        
    # Destellos mágicos
    d.ellipse([46, 10, 49, 13], fill=(255, 255, 210, 255))
    d.ellipse([12, 14, 15, 17], fill=(255, 255, 210, 255))
    
    img.save(os.path.join(out_dir, "Placeholder_Ingredient_Mushroom.png"))
    print("Created Placeholder_Ingredient_Mushroom.png")

# 2. Araña Estilizada / Stylized Spider (64 x 64) - GDD: estilizada, no realista
def create_spider():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    
    # Hilo de seda plateado
    d.line([32, 0, 32, 22], fill=(220, 230, 245, 200), width=2)
    
    # 8 Patitas arqueadas redondeadas estilo caricatura
    leg_color = (45, 30, 60, 255)
    # Patas izquierdas
    d.line([24, 28, 12, 22, 6, 28], fill=leg_color, width=3)
    d.line([23, 33, 10, 34, 7, 44], fill=leg_color, width=3)
    d.line([24, 38, 11, 44, 9, 56], fill=leg_color, width=3)
    d.line([26, 42, 16, 52, 14, 60], fill=leg_color, width=3)
    # Patas derechas
    d.line([40, 28, 52, 22, 58, 28], fill=leg_color, width=3)
    d.line([41, 33, 54, 34, 57, 44], fill=leg_color, width=3)
    d.line([40, 38, 53, 44, 55, 56], fill=leg_color, width=3)
    d.line([38, 42, 48, 52, 50, 60], fill=leg_color, width=3)
    
    # Abdomen redondo
    d.ellipse([20, 28, 44, 52], fill=(70, 42, 95, 255), outline=OUTLINE, width=2)
    d.ellipse([23, 31, 41, 48], fill=(95, 60, 125, 255))
    # Brillo de terciopelo
    d.ellipse([26, 32, 32, 38], fill=(135, 95, 175, 255))
    
    # Cabeza redonda
    d.ellipse([24, 18, 40, 32], fill=(55, 35, 75, 255), outline=OUTLINE, width=2)
    
    # Grandes ojos caricatura dorados/brillantes
    d.ellipse([26, 21, 31, 27], fill=(255, 215, 60, 255), outline=OUTLINE)
    d.ellipse([33, 21, 38, 27], fill=(255, 215, 60, 255), outline=OUTLINE)
    # Pupilas
    d.ellipse([28, 22, 30, 25], fill=(20, 10, 30, 255))
    d.ellipse([35, 22, 37, 25], fill=(20, 10, 30, 255))
    # Brillo en ojos
    d.point([29, 23], fill=(255, 255, 255, 255))
    d.point([36, 23], fill=(255, 255, 255, 255))
    
    img.save(os.path.join(out_dir, "Placeholder_Ingredient_Spider.png"))
    print("Created Placeholder_Ingredient_Spider.png")

# 3. Flor Marchita / Withered Flower (64 x 64)
def create_withered_flower():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    
    # Tallo curvado marchito
    stem_points = [(32, 58), (34, 48), (31, 38), (28, 26), (30, 22)]
    d.line(stem_points, fill=(65, 85, 55, 255), width=4)
    # Espina y hoja caída
    d.polygon([(33, 44), (43, 42), (34, 48)], fill=(75, 95, 60, 255), outline=OUTLINE)
    d.polygon([(30, 36), (19, 39), (29, 41)], fill=(55, 75, 45, 255), outline=OUTLINE)
    
    # Sépalos
    d.polygon([(26, 22), (30, 26), (34, 22), (30, 18)], fill=(45, 65, 38, 255), outline=OUTLINE)
    
    # Pétalos caídos marchitos (tonos violeta azulado y cenizo)
    petals = [
        [(22, 16), (28, 12), (34, 15), (28, 22)], # pétalo superior
        [(14, 20), (22, 16), (28, 22), (18, 26)], # pétalo izquierdo
        [(36, 18), (46, 22), (38, 28), (30, 22)], # pétalo derecho
        [(18, 28), (24, 23), (28, 30), (20, 36)], # caído izq
        [(38, 26), (42, 34), (32, 32), (30, 24)], # caído der
    ]
    for p in petals:
        d.polygon(p, fill=(85, 70, 120, 255), outline=OUTLINE)
        
    # Corola central marchita con polen oscuro
    d.ellipse([26, 18, 34, 26], fill=(50, 40, 70, 255), outline=OUTLINE)
    d.ellipse([28, 20, 32, 24], fill=(185, 145, 70, 255))
    
    # Polvo mágico azulado flotante
    d.ellipse([14, 12, 17, 15], fill=(140, 190, 255, 200))
    d.ellipse([45, 14, 47, 16], fill=(140, 190, 255, 200))
    d.ellipse([42, 48, 44, 50], fill=(140, 190, 255, 200))
    
    img.save(os.path.join(out_dir, "Placeholder_Ingredient_WitheredFlower.png"))
    print("Created Placeholder_Ingredient_WitheredFlower.png")

# 4. Raíz / Root (64 x 64)
def create_root():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    
    # Pequeño brote verde en la corona
    d.polygon([(32, 8), (28, 16), (36, 16)], fill=(90, 145, 55, 255), outline=OUTLINE)
    d.polygon([(26, 12), (23, 18), (30, 17)], fill=(75, 125, 45, 255), outline=OUTLINE)
    d.polygon([(38, 12), (33, 17), (41, 18)], fill=(110, 165, 65, 255), outline=OUTLINE)
    
    # Cuerpo nudoso principal de raíz retorcida (ocre, madera y tierra)
    body = [
        (26, 16), (38, 16), (42, 26), (36, 36), (40, 46), (35, 56), (32, 58),
        (28, 54), (25, 42), (29, 32), (22, 24)
    ]
    d.polygon(body, fill=(155, 105, 55, 255), outline=OUTLINE)
    
    # Nudos y vetas
    d.line([26, 26, 38, 28], fill=(110, 70, 35, 255), width=2)
    d.line([27, 38, 36, 39], fill=(110, 70, 35, 255), width=2)
    d.line([30, 48, 37, 49], fill=(110, 70, 35, 255), width=2)
    
    # Bifurcaciones / raicillas laterales
    d.line([24, 25, 14, 32, 10, 38], fill=(135, 90, 45, 255), width=3)
    d.line([39, 28, 48, 34, 54, 36], fill=(135, 90, 45, 255), width=3)
    d.line([27, 44, 18, 50, 16, 56], fill=(135, 90, 45, 255), width=2)
    d.line([36, 46, 45, 52, 47, 58], fill=(135, 90, 45, 255), width=2)
    
    # Iluminación frontal
    d.ellipse([28, 20, 34, 26], fill=(190, 135, 75, 255))
    d.ellipse([29, 32, 33, 36], fill=(190, 135, 75, 255))
    
    img.save(os.path.join(out_dir, "Placeholder_Ingredient_Root.png"))
    print("Created Placeholder_Ingredient_Root.png")

# 5. Portón de Ingredientes Cerrado / Ingredient Gate Closed (160 x 224) -> 2.5 u x 3.5 u at 64 PPU
def create_gate_closed():
    img = Image.new("RGBA", (160, 224), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    
    # Pilares de piedra laterales
    draw_round_rect(d, [6, 20, 28, 220], fill=(85, 80, 82, 255), outline=OUTLINE, width=3, radius=4)
    draw_round_rect(d, [132, 20, 154, 220], fill=(85, 80, 82, 255), outline=OUTLINE, width=3, radius=4)
    # Capiteles de piedra
    draw_round_rect(d, [2, 12, 32, 24], fill=(120, 115, 118, 255), outline=OUTLINE, width=2, radius=3)
    draw_round_rect(d, [128, 12, 158, 24], fill=(120, 115, 118, 255), outline=OUTLINE, width=2, radius=3)
    
    # Arco superior de forja
    d.arc([24, 16, 136, 100], start=180, end=360, fill=(35, 32, 38, 255), width=6)
    
    # Hojas de portón de madera colonial oscura reforzada
    draw_round_rect(d, [28, 48, 78, 218], fill=(95, 58, 32, 255), outline=OUTLINE, width=3, radius=2)
    draw_round_rect(d, [82, 48, 132, 218], fill=(95, 58, 32, 255), outline=OUTLINE, width=3, radius=2)
    
    # Tablones verticales
    for x in [42, 56, 68, 94, 108, 120]:
        d.line([x, 50, x, 216], fill=(68, 40, 20, 255), width=2)
        
    # Herrajes de hierro horizontales
    for y in [70, 135, 195]:
        draw_round_rect(d, [26, y, 80, y+14], fill=(45, 42, 50, 255), outline=OUTLINE, width=2, radius=2)
        draw_round_rect(d, [80, y, 134, y+14], fill=(45, 42, 50, 255), outline=OUTLINE, width=2, radius=2)
        # Remaches
        for rx in [34, 52, 72, 88, 108, 126]:
            d.ellipse([rx-2, y+5, rx+2, y+9], fill=(140, 135, 145, 255))
            
    # Gran cadena y candado mágico central con runas violetas
    d.ellipse([64, 118, 96, 150], fill=(30, 28, 34, 255), outline=OUTLINE, width=3)
    draw_round_rect(d, [68, 130, 92, 156], fill=(185, 140, 40, 255), outline=OUTLINE, width=2, radius=4)
    # Sello mágico resplandeciente
    d.ellipse([73, 136, 87, 150], fill=(180, 50, 240, 220))
    d.line([76, 143, 84, 143], fill=(255, 230, 255, 255), width=2)
    d.line([80, 139, 80, 147], fill=(255, 230, 255, 255), width=2)
    
    # Aura mágica bloqueante
    d.ellipse([50, 105, 110, 165], outline=(170, 60, 240, 140), width=3)
    
    img.save(os.path.join(out_dir, "Placeholder_IngredientGate_Closed.png"))
    print("Created Placeholder_IngredientGate_Closed.png")

# 6. Portón Abierto / Ingredient Gate Open (160 x 224)
def create_gate_open():
    img = Image.new("RGBA", (160, 224), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    
    # Pilares de piedra laterales
    draw_round_rect(d, [6, 20, 28, 220], fill=(85, 80, 82, 255), outline=OUTLINE, width=3, radius=4)
    draw_round_rect(d, [132, 20, 154, 220], fill=(85, 80, 82, 255), outline=OUTLINE, width=3, radius=4)
    draw_round_rect(d, [2, 12, 32, 24], fill=(120, 115, 118, 255), outline=OUTLINE, width=2, radius=3)
    draw_round_rect(d, [128, 12, 158, 24], fill=(120, 115, 118, 255), outline=OUTLINE, width=2, radius=3)
    
    # Resplandor dorado/cálido invitando a pasar a la siguiente escena
    d.rectangle([28, 48, 132, 218], fill=(255, 240, 180, 75))
    d.ellipse([45, 70, 115, 200], fill=(255, 250, 210, 95))
    
    # Arco superior
    d.arc([24, 16, 136, 100], start=180, end=360, fill=(35, 32, 38, 255), width=6)
    
    # Hojas de portón abiertas en perspectiva abatidas hacia los costados
    # Hoja izq abatida
    d.polygon([(28, 48), (38, 56), (38, 212), (28, 218)], fill=(85, 50, 26, 255), outline=OUTLINE)
    # Hoja der abatida
    d.polygon([(132, 48), (122, 56), (122, 212), (132, 218)], fill=(85, 50, 26, 255), outline=OUTLINE)
    
    # Partículas doradas de éxito
    stars = [(55, 80), (105, 95), (80, 120), (60, 150), (100, 160), (75, 185)]
    for sx, sy in stars:
        d.ellipse([sx-3, sy-3, sx+3, sy+3], fill=(255, 235, 120, 255))
        d.point([sx, sy], fill=(255, 255, 255, 255))
        
    img.save(os.path.join(out_dir, "Placeholder_IngredientGate_Open.png"))
    print("Created Placeholder_IngredientGate_Open.png")

if __name__ == "__main__":
    create_mushroom()
    create_spider()
    create_withered_flower()
    create_root()
    create_gate_closed()
    create_gate_open()
