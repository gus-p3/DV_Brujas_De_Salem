import os
from PIL import Image, ImageDraw

out_dir = "Assets/Art/Environment/Level2"
os.makedirs(out_dir, exist_ok=True)

OUTLINE = (28, 20, 18, 255)
WOOD_DARK = (92, 54, 26, 255)
WOOD_MID = (138, 86, 42, 255)
WOOD_LIGHT = (182, 122, 68, 255)
WOOD_HIGHLIGHT = (214, 158, 102, 255)

RED_AWNING_DARK = (130, 24, 24, 255)
RED_AWNING_MID = (184, 42, 42, 255)
RED_AWNING_LIGHT = (226, 76, 76, 255)

GOLD_AWNING_DARK = (175, 120, 20, 255)
GOLD_AWNING_MID = (225, 168, 38, 255)
GOLD_AWNING_LIGHT = (252, 208, 92, 255)

COBBLE_DARK = (48, 44, 42, 255)
COBBLE_MID = (84, 76, 72, 255)
COBBLE_LIGHT = (124, 114, 108, 255)
COBBLE_EARTH = (58, 42, 30, 255)

IRON_DARK = (32, 30, 36, 255)
IRON_MID = (65, 62, 72, 255)
LANTERN_GLOW = (255, 220, 110, 240)
LANTERN_CORE = (255, 250, 190, 255)

def draw_round_rect(draw, box, fill, outline, width=2):
    draw.rounded_rectangle(box, radius=4, fill=fill, outline=outline, width=width)

# 1. Cobblestone ground tile (512 x 128) -> 8 u wide, 2 u tall at 64 PPU
def create_cobble_ground():
    img = Image.new("RGBA", (512, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Earth base
    d.rectangle([0, 32, 512, 128], fill=COBBLE_EARTH)
    # Cobblestone layers
    stones = [
        (0, 8, 40, 36), (42, 6, 88, 38), (90, 8, 134, 35), (136, 6, 192, 38),
        (194, 8, 246, 36), (248, 6, 298, 38), (300, 8, 350, 36), (352, 6, 406, 38),
        (408, 8, 460, 36), (462, 6, 512, 38),
        # row 2
        (16, 32, 64, 62), (66, 30, 118, 64), (120, 32, 172, 62), (174, 30, 228, 64),
        (230, 32, 282, 62), (284, 30, 338, 64), (340, 32, 394, 62), (396, 30, 448, 64),
        (450, 32, 502, 62),
        # row 3
        (0, 58, 48, 88), (50, 56, 102, 90), (104, 58, 156, 88), (158, 56, 210, 90),
        (212, 58, 264, 88), (266, 56, 318, 90), (320, 58, 372, 88), (374, 56, 426, 90),
        (428, 58, 480, 88), (482, 56, 512, 90)
    ]
    for b in stones:
        draw_round_rect(d, b, fill=COBBLE_MID, outline=OUTLINE, width=3)
        # highlight inside
        d.line([b[0]+4, b[1]+4, b[2]-4, b[1]+4], fill=COBBLE_LIGHT, width=2)
    # bottom fill
    d.rectangle([0, 88, 512, 128], fill=(38, 28, 20, 255))
    img.save(os.path.join(out_dir, "Placeholder_Cobble_Ground.png"))

# 2. Market Stall Red Awning (192 x 64) -> 3 u wide, 1 u tall
def create_red_awning():
    img = Image.new("RGBA", (192, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Awning slope
    points = [(10, 50), (30, 8), (162, 8), (182, 50)]
    d.polygon(points, fill=RED_AWNING_MID, outline=OUTLINE)
    # Stripes
    stripe_w = 26
    for i in range(7):
        x = 10 + i * stripe_w
        if i % 2 == 0:
            sub_pts = [(max(10, x), 50), (max(30, x+8), 8), (min(162, x+stripe_w+8), 8), (min(182, x+stripe_w), 50)]
            d.polygon(sub_pts, fill=RED_AWNING_LIGHT)
    d.polygon(points, outline=OUTLINE)
    # Scalloped bottom
    scallop_w = 24
    for i in range(8):
        sx = i * scallop_w
        d.chord([sx, 44, sx + scallop_w, 62], start=0, end=180, fill=RED_AWNING_DARK, outline=OUTLINE, width=2)
    img.save(os.path.join(out_dir, "Placeholder_MarketStall_Awning_Red.png"))

# 3. Market Stall Gold Awning (192 x 64)
def create_gold_awning():
    img = Image.new("RGBA", (192, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    points = [(10, 50), (30, 8), (162, 8), (182, 50)]
    d.polygon(points, fill=GOLD_AWNING_MID, outline=OUTLINE)
    stripe_w = 26
    for i in range(7):
        x = 10 + i * stripe_w
        if i % 2 == 0:
            sub_pts = [(max(10, x), 50), (max(30, x+8), 8), (min(162, x+stripe_w+8), 8), (min(182, x+stripe_w), 50)]
            d.polygon(sub_pts, fill=GOLD_AWNING_LIGHT)
    d.polygon(points, outline=OUTLINE)
    scallop_w = 24
    for i in range(8):
        sx = i * scallop_w
        d.chord([sx, 44, sx + scallop_w, 62], start=0, end=180, fill=GOLD_AWNING_DARK, outline=OUTLINE, width=2)
    img.save(os.path.join(out_dir, "Placeholder_MarketStall_Awning_Gold.png"))

# 4. Large Market Canopy (320 x 80) -> 5 u wide, 1.25 u tall
def create_large_awning():
    img = Image.new("RGBA", (320, 80), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    points = [(12, 64), (40, 10), (280, 10), (308, 64)]
    d.polygon(points, fill=RED_AWNING_MID, outline=OUTLINE)
    stripe_w = 32
    for i in range(10):
        x = 12 + i * stripe_w
        color = GOLD_AWNING_MID if i % 2 == 0 else RED_AWNING_MID
        sub_pts = [(max(12, x), 64), (max(40, x+8), 10), (min(280, x+stripe_w+8), 10), (min(308, x+stripe_w), 64)]
        d.polygon(sub_pts, fill=color)
    d.polygon(points, outline=OUTLINE, width=3)
    for i in range(13):
        sx = i * 24 + 4
        d.chord([sx, 58, sx + 24, 78], start=0, end=180, fill=GOLD_AWNING_DARK, outline=OUTLINE, width=2)
    img.save(os.path.join(out_dir, "Placeholder_MarketStall_Awning_Large.png"))

# 5. Wooden Stall Posts & Counter (192 x 128) -> 3 u wide, 2 u tall
def create_stall_structure():
    img = Image.new("RGBA", (192, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Vertical timber posts
    draw_round_rect(d, [20, 8, 36, 124], fill=WOOD_MID, outline=OUTLINE, width=3)
    draw_round_rect(d, [156, 8, 172, 124], fill=WOOD_MID, outline=OUTLINE, width=3)
    # Highlight on posts
    d.line([24, 12, 24, 120], fill=WOOD_LIGHT, width=2)
    d.line([160, 12, 160, 120], fill=WOOD_LIGHT, width=2)
    # Countertop table
    draw_round_rect(d, [12, 60, 180, 84], fill=WOOD_LIGHT, outline=OUTLINE, width=3)
    d.line([16, 64, 176, 64], fill=WOOD_HIGHLIGHT, width=2)
    # Lower shelf / skirts
    draw_round_rect(d, [26, 84, 166, 122], fill=WOOD_DARK, outline=OUTLINE, width=2)
    # Vertical slats on skirt
    for x in range(46, 150, 20):
        d.line([x, 86, x, 120], fill=OUTLINE, width=2)
    img.save(os.path.join(out_dir, "Placeholder_MarketStall_WoodPosts.png"))

# 6. Market Crate (64 x 64) -> 1 u x 1 u
def create_crate():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    draw_round_rect(d, [4, 4, 60, 60], fill=WOOD_MID, outline=OUTLINE, width=3)
    # Planks inside
    d.line([4, 22, 60, 22], fill=OUTLINE, width=2)
    d.line([4, 42, 60, 42], fill=OUTLINE, width=2)
    # Diagonal brace
    d.line([8, 8, 56, 56], fill=WOOD_DARK, width=6)
    d.line([8, 8, 56, 56], fill=WOOD_LIGHT, width=2)
    draw_round_rect(d, [4, 4, 60, 60], fill=None, outline=OUTLINE, width=3)
    img.save(os.path.join(out_dir, "Placeholder_Market_Crate.png"))

# 7. Stack of Crates (128 x 96) -> 2 u wide, 1.5 u tall
def create_crates_stack():
    img = Image.new("RGBA", (128, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Base crate 1
    draw_round_rect(d, [6, 34, 66, 92], fill=WOOD_MID, outline=OUTLINE, width=3)
    d.line([12, 40, 60, 86], fill=WOOD_DARK, width=4)
    # Base crate 2
    draw_round_rect(d, [64, 34, 122, 92], fill=WOOD_MID, outline=OUTLINE, width=3)
    d.line([70, 40, 116, 86], fill=WOOD_DARK, width=4)
    # Top crate
    draw_round_rect(d, [34, 6, 94, 64], fill=WOOD_LIGHT, outline=OUTLINE, width=3)
    d.line([40, 12, 88, 58], fill=WOOD_DARK, width=4)
    draw_round_rect(d, [34, 6, 94, 64], fill=None, outline=OUTLINE, width=3)
    img.save(os.path.join(out_dir, "Placeholder_Market_Crates_Stack.png"))

# 8. Market Barrel (56 x 64) -> ~0.9 u wide, 1 u tall
def create_barrel():
    img = Image.new("RGBA", (56, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Barrel curved body
    d.ellipse([4, 4, 52, 60], fill=WOOD_DARK, outline=OUTLINE, width=3)
    # Planks & highlights
    d.line([14, 8, 14, 56], fill=WOOD_MID, width=2)
    d.line([28, 6, 28, 58], fill=WOOD_LIGHT, width=3)
    d.line([42, 8, 42, 56], fill=WOOD_MID, width=2)
    # Iron hoops
    d.line([6, 18, 50, 18], fill=IRON_MID, width=4)
    d.line([4, 32, 52, 32], fill=IRON_DARK, width=4)
    d.line([6, 46, 50, 46], fill=IRON_MID, width=4)
    img.save(os.path.join(out_dir, "Placeholder_Market_Barrel.png"))

# 9. Market Handcart (160 x 96) -> 2.5 u wide, 1.5 u tall
def create_cart():
    img = Image.new("RGBA", (160, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Cart body
    draw_round_rect(d, [24, 18, 148, 50], fill=WOOD_MID, outline=OUTLINE, width=3)
    # Vertical slats
    for x in range(36, 140, 18):
        d.line([x, 20, x, 48], fill=OUTLINE, width=2)
    # Cart handle
    d.line([24, 36, 4, 52], fill=WOOD_DARK, width=5)
    d.line([24, 36, 4, 52], fill=OUTLINE, width=1)
    # Wheel 1 (left)
    d.ellipse([34, 40, 82, 88], fill=WOOD_DARK, outline=OUTLINE, width=3)
    d.ellipse([40, 46, 76, 82], fill=None, outline=IRON_MID, width=3)
    d.line([36, 64, 80, 64], fill=WOOD_LIGHT, width=2)
    d.line([58, 42, 58, 86], fill=WOOD_LIGHT, width=2)
    d.ellipse([54, 60, 62, 68], fill=IRON_DARK, outline=OUTLINE)
    # Wheel 2 (right)
    d.ellipse([98, 40, 146, 88], fill=WOOD_DARK, outline=OUTLINE, width=3)
    d.ellipse([104, 46, 140, 82], fill=None, outline=IRON_MID, width=3)
    d.line([100, 64, 144, 64], fill=WOOD_LIGHT, width=2)
    d.line([122, 42, 122, 86], fill=WOOD_LIGHT, width=2)
    d.ellipse([118, 60, 126, 68], fill=IRON_DARK, outline=OUTLINE)
    img.save(os.path.join(out_dir, "Placeholder_Market_Cart.png"))

# 10. Street Lantern Post (48 x 160) -> 0.75 u wide, 2.5 u tall
def create_lantern():
    img = Image.new("RGBA", (48, 160), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Wooden post
    draw_round_rect(d, [20, 48, 28, 156], fill=WOOD_DARK, outline=OUTLINE, width=2)
    d.line([24, 52, 24, 152], fill=WOOD_LIGHT, width=1)
    # Iron arm
    d.line([24, 48, 38, 24], fill=IRON_DARK, width=3)
    d.line([38, 24, 38, 34], fill=IRON_DARK, width=2)
    # Lantern housing
    d.polygon([(28, 34), (48, 34), (44, 58), (32, 58)], fill=LANTERN_GLOW, outline=OUTLINE, width=2)
    d.ellipse([34, 42, 42, 50], fill=LANTERN_CORE)
    # Lantern cap
    d.polygon([(26, 34), (50, 34), (38, 22)], fill=IRON_DARK, outline=OUTLINE)
    img.save(os.path.join(out_dir, "Placeholder_Market_Lantern.png"))

# 11. Market Roof / Timber Platform (256 x 40) -> 4 u wide, ~0.6 u tall
def create_roof_platform():
    img = Image.new("RGBA", (256, 40), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    draw_round_rect(d, [4, 6, 252, 34], fill=WOOD_MID, outline=OUTLINE, width=3)
    d.line([8, 10, 248, 10], fill=WOOD_HIGHLIGHT, width=2)
    for x in range(36, 240, 36):
        d.line([x, 8, x, 32], fill=OUTLINE, width=2)
    img.save(os.path.join(out_dir, "Placeholder_Market_RoofPlatform.png"))

# 12. Low Market Tunnel / Canopy for Cat Passage (192 x 70)
def create_cat_tunnel():
    img = Image.new("RGBA", (192, 70), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Timber beam on top
    draw_round_rect(d, [6, 4, 186, 22], fill=WOOD_DARK, outline=OUTLINE, width=2)
    # Draped cloth hanging down on sides leaving open center
    d.polygon([(10, 22), (48, 22), (40, 64), (12, 60)], fill=RED_AWNING_DARK, outline=OUTLINE, width=2)
    d.polygon([(144, 22), (182, 22), (180, 60), (152, 64)], fill=RED_AWNING_DARK, outline=OUTLINE, width=2)
    # Top awning trim
    d.line([6, 8, 186, 8], fill=WOOD_LIGHT, width=2)
    img.save(os.path.join(out_dir, "Placeholder_Market_Canopy_CatTunnel.png"))

create_cobble_ground()
create_red_awning()
create_gold_awning()
create_large_awning()
create_stall_structure()
create_crate()
create_crates_stack()
create_barrel()
create_cart()
create_lantern()
create_roof_platform()
create_cat_tunnel()

print("All market placeholder sprites generated in:", out_dir)
