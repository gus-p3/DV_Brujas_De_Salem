import os
import math
import random
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ENV_DIR = os.path.join(ROOT, "Assets", "Art", "Environment", "Level2")
CAINOS_TEX = os.path.join(ROOT, "Assets", "Cainos", "Pixel Art Platformer - Village Props", "Texture", "TX Village Props.png")
CAINOS_CHEST_TEX = os.path.join(ROOT, "Assets", "Cainos", "Pixel Art Platformer - Village Props", "Texture", "TX Chest Animation.png")

os.makedirs(ENV_DIR, exist_ok=True)


def crop_cainos(unity_x, unity_y, w, h):
    if not os.path.exists(CAINOS_TEX):
        return None
    atlas = Image.open(CAINOS_TEX).convert("RGBA")
    top = atlas.height - unity_y - h
    return atlas.crop((unity_x, top, unity_x + w, top + h))


def sync_cainos_barrels_crates_chests():
    """
    Syncs Placeholder_Market_Barrel.png, Placeholder_Market_Crate.png,
    Interactive_Chest_Closed.png, and Interactive_Chest_Open.png from Cainos.
    """
    barrel = crop_cainos(195, 960, 28, 36)
    if barrel is not None:
        canvas = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        scaled = barrel.resize((44, 56), Image.BICUBIC)
        canvas.paste(scaled, ((64 - 44) // 2, 64 - 56), scaled)
        canvas.save(os.path.join(ENV_DIR, "Placeholder_Market_Barrel.png"))

    crate = crop_cainos(42, 960, 45, 45)
    if crate is not None:
        canvas = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        scaled = crate.resize((54, 54), Image.BICUBIC)
        canvas.paste(scaled, ((64 - 54) // 2, 64 - 54), scaled)
        canvas.save(os.path.join(ENV_DIR, "Placeholder_Market_Crate.png"))

    if os.path.exists(CAINOS_CHEST_TEX):
        chest_atlas = Image.open(CAINOS_CHEST_TEX).convert("RGBA")
        c_closed = chest_atlas.crop((16, 16, 80, 80))
        c_open = chest_atlas.crop((16 + 6 * 64, 16, 80 + 6 * 64, 80))
        c_closed.save(os.path.join(ENV_DIR, "Interactive_Chest_Closed.png"))
        c_open.save(os.path.join(ENV_DIR, "Interactive_Chest_Open.png"))


def generate_smooth_colonial_floors_and_structures():
    """
    Generates smooth, painterly colonial structural assets matching houses_sprite.png.png:
    1. Interior_Floor_Planks.png (256x64 at 64 PPU -> seamless horizontal oak beam & floorboards)
    2. Interior_Rafter_Loft.png (244x36 at 64 PPU -> 3.81u x 0.56u solid suspended oak loft beam)
    3. Interior_Cellar_CatArch.png (136x186 at 64 PPU -> 2.125u x 2.90u stone cellar wall with arched crawlspace)
    4. Placeholder_Market_Canopy_CatTunnel.png (184x64 at 64 PPU -> colonial timber & slate low alley overhang)
    """
    # 1. Interior_Floor_Planks.png (256x64)
    # Seamless horizontal colonial dark-oak floor beam with warm floorboards on top and soft shadow below.
    w, h = 256, 64
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = img.load()

    for y in range(h):
        for x in range(w):
            grain = math.sin(x * 0.09 + math.sin(y * 0.35) * 2.0) * 5.0 + math.cos(x * 0.23 - y * 0.4) * 3.0
            if y < 3:
                # Top dark bevel edge
                px[x, y] = (26, 16, 10, 255)
            elif y < 16:
                # Upper polished oak floorboards
                t = (y - 3) / 13.0
                r = int(146 - t * 28 + grain)
                g = int(96 - t * 20 + grain * 0.7)
                b = int(56 - t * 14 + grain * 0.5)
                # Subtle plank joint every 64px
                if x % 64 in (0, 1):
                    r, g, b = int(r * 0.55), int(g * 0.55), int(b * 0.55)
                px[x, y] = (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), 255)
            elif y < 19:
                # Shadow groove under floorboards
                px[x, y] = (32, 19, 11, 255)
            elif y < 50:
                # Main structural oak girder (warm dark brown with smooth horizontal wood grain)
                t = (y - 19) / 31.0
                arch_shade = 1.0 - 0.18 * abs(t - 0.35)
                r = int((96 * arch_shade) + grain)
                g = int((60 * arch_shade) + grain * 0.65)
                b = int((35 * arch_shade) + grain * 0.45)
                # Subtle recessed panel molding
                if 24 <= y <= 44 and (x % 64) in range(8, 56):
                    if y in (24, 25) or (x % 64) in (8, 9):
                        r, g, b = int(r * 0.68), int(g * 0.68), int(b * 0.68)
                    elif y in (43, 44) or (x % 64) in (54, 55):
                        r, g, b = int(r * 1.15), int(g * 1.15), int(b * 1.15)
                    else:
                        r, g, b = int(r * 0.88), int(g * 0.88), int(b * 0.88)
                px[x, y] = (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), 255)
            elif y < 60:
                # Lower dark timber trim
                t = (y - 50) / 10.0
                r = int(58 - t * 24 + grain * 0.5)
                g = int(35 - t * 15 + grain * 0.35)
                b = int(20 - t * 9 + grain * 0.2)
                px[x, y] = (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), 255)
            else:
                # Soft bottom drop shadow edge
                alpha = int(255 * (1.0 - (y - 60) / 4.5))
                px[x, y] = (18, 10, 6, max(0, min(255, alpha)))

    img.save(os.path.join(ENV_DIR, "Interior_Floor_Planks.png"))

    # 2. Interior_Rafter_Loft.png (244x36 at 64 PPU -> 3.81u x 0.56u)
    # Solid suspended colonial oak loft beam with wrought-iron chain rings at x=42 and x=202.
    lw, lh = 244, 36
    loft = Image.new("RGBA", (lw, lh), (0, 0, 0, 0))
    lpx = loft.load()
    for y in range(lh):
        for x in range(lw):
            # Rounded ends (taper first/last 8px)
            edge_dist = min(x, lw - 1 - x)
            if edge_dist < 4 and (y < 4 or y > lh - 8):
                continue
            grain = math.sin(x * 0.11 + y * 0.25) * 5.0
            if y < 3:
                lpx[x, y] = (28, 17, 10, 255)
            elif y < 11:
                t = (y - 3) / 8.0
                r = int(156 - t * 26 + grain)
                g = int(104 - t * 18 + grain * 0.7)
                b = int(60 - t * 12 + grain * 0.5)
                lpx[x, y] = (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), 255)
            elif y < 28:
                t = (y - 11) / 17.0
                r = int(104 - t * 28 + grain)
                g = int(64 - t * 18 + grain * 0.7)
                b = int(36 - t * 10 + grain * 0.5)
                # Wrought-iron strap bands where the chains attach (around x=38..48 and x=196..206)
                if 36 <= x <= 48 or 196 <= x <= 208:
                    shine = math.exp(-((x % 160 - 42) ** 2) / 18.0) * 35.0
                    r = int(58 + shine)
                    g = int(62 + shine)
                    b = int(70 + shine)
                lpx[x, y] = (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), 255)
            else:
                # Carved underside corbel & shadow
                lpx[x, y] = (34, 20, 12, 240 if y < 33 else 120)

    loft.save(os.path.join(ENV_DIR, "Interior_Rafter_Loft.png"))

    # 3. Interior_Cellar_CatArch.png (136x186 at 64 PPU -> 2.125u x 2.90u)
    # Smoothly shaded stone cellar partition wall with heavy oak side-posts and a stone arch at the bottom.
    cw, ch = 136, 186
    arch = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    ad = ImageDraw.Draw(arch)
    rng = random.Random(2026)

    # Base dark mortar fill
    ad.rounded_rectangle([2, 2, cw - 3, ch - 3], radius=6, fill=(38, 34, 31, 255), outline=(22, 18, 15, 255), width=2)

    # Smooth ashlar stone blocks inside (x=14..cw-15, y=14..ch-22)
    stone_pal = [(96, 90, 83), (84, 78, 72), (108, 101, 93), (76, 71, 65)]
    y = 14
    row = 0
    while y < ch - 24:
        rh = 20 if row % 2 == 0 else 24
        if y + rh > ch - 22:
            rh = (ch - 22) - y
        x = 16
        while x < cw - 16:
            sw = rng.randint(24, 38)
            if x + sw > cw - 16 or (cw - 16) - (x + sw) < 12:
                sw = (cw - 16) - x
            if sw >= 8 and rh >= 8:
                sc = rng.choice(stone_pal)
                # Soft stone block with top highlight and bottom shadow
                ad.rounded_rectangle([x + 1, y + 1, x + sw - 2, y + rh - 2], radius=3,
                                     fill=sc + (255,), outline=(32, 28, 25, 255))
                ad.line([x + 3, y + 2, x + sw - 4, y + 2], fill=(sc[0] + 18, sc[1] + 16, sc[2] + 14, 200))
                ad.line([x + 3, y + rh - 3, x + sw - 4, y + rh - 3], fill=(sc[0] - 18, sc[1] - 16, sc[2] - 14, 220))
            x += sw
        y += rh
        row += 1

    # Left & Right heavy oak timber framing posts
    for px0, px1 in [(3, 15), (cw - 16, cw - 4)]:
        ad.rectangle([px0, 4, px1, ch - 4], fill=(82, 50, 28, 255), outline=(26, 15, 9, 255))
        ad.line([px0 + 3, 6, px0 + 3, ch - 6], fill=(108, 68, 38, 220), width=2)

    # Top oak lintel
    ad.rectangle([3, 3, cw - 4, 15], fill=(92, 56, 32, 255), outline=(26, 15, 9, 255))
    ad.line([5, 5, cw - 6, 5], fill=(122, 78, 44, 220), width=2)

    # Bottom stone arch lintel (voussoirs) framing the cat passage underneath
    ad.rectangle([3, ch - 24, cw - 4, ch - 3], fill=(64, 58, 52, 255), outline=(24, 20, 17, 255))
    for vx in range(10, cw - 14, 16):
        ad.rounded_rectangle([vx, ch - 22, vx + 14, ch - 4], radius=2,
                             fill=(114, 106, 96, 255), outline=(34, 30, 26, 255))
        ad.line([vx + 2, ch - 20, vx + 12, ch - 20], fill=(140, 130, 118, 220))

    arch = arch.filter(ImageFilter.SMOOTH_MORE)
    arch.save(os.path.join(ENV_DIR, "Interior_Cellar_CatArch.png"))

    # 4. Placeholder_Market_Canopy_CatTunnel.png (184x64 at 64 PPU)
    # Replaces the old brown-and-red blocky sprite in Zone 4 with a colonial timber-and-slate low overhang.
    tw, th = 184, 64
    tun = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    td = ImageDraw.Draw(tun)
    # Upper weathered slate roof strip
    td.rounded_rectangle([0, 4, tw - 1, 28], radius=3, fill=(68, 72, 82, 255), outline=(24, 22, 20, 255), width=2)
    for sx in range(6, tw - 10, 18):
        td.rounded_rectangle([sx, 7, sx + 16, 25], radius=2, fill=(82, 86, 96, 255), outline=(36, 38, 44, 255))
        td.line([sx + 2, 9, sx + 14, 9], fill=(106, 110, 122, 200))
    # Heavy oak support beam underneath
    td.rectangle([0, 26, tw - 1, 46], fill=(92, 56, 32, 255), outline=(26, 15, 9, 255), width=2)
    td.line([2, 29, tw - 3, 29], fill=(124, 78, 46, 220), width=2)
    # Carved wooden brackets at left and right ends (dark oak, NOT red trapezoids!)
    for bx0 in [8, tw - 28]:
        td.polygon([(bx0, 46), (bx0 + 20, 46), (bx0 + 16, 61), (bx0 + 4, 61)],
                   fill=(72, 44, 24, 255), outline=(26, 15, 9, 255))
    tun = tun.filter(ImageFilter.SMOOTH)
    tun.save(os.path.join(ENV_DIR, "Placeholder_Market_Canopy_CatTunnel.png"))


def generate_smooth_colonial_doors_and_props():
    """
    Generates high-resolution (64 PPU, smoothly shaded, anti-aliased) colonial doors and interior props
    that blend seamlessly with houses_sprite.png.png:
    - Village_Door_Closed.png (96x144 at 64 PPU -> 1.50u x 2.25u)
    - Village_Door_Open.png (96x144 at 64 PPU -> 1.50u x 2.25u)
    - Village_Door_Locked.png (96x144 at 64 PPU -> 1.50u x 2.25u)
    - Interior_Bookshelf.png (124x160 at 64 PPU)
    - Interior_Window_Shutters.png (96x84 at 64 PPU)
    - Interior_Fireplace.png (160x192 at 64 PPU)
    """
    dw, dh = 96, 144

    def make_door_base(is_open=False, is_locked=False):
        # Render at 2x resolution (192x288) and downsample with LANCZOS for crisp painterly quality
        W, H = dw * 2, dh * 2
        im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)

        # Outer weathered stone/timber arch casing
        d.rounded_rectangle([8, 6, W - 9, H - 1], radius=24, fill=(68, 60, 54, 255), outline=(24, 18, 14, 255), width=4)
        # Stone arch blocks along outer frame
        stone_c = [(112, 102, 92, 255), (98, 88, 78, 255), (124, 114, 102, 255)]
        for sy in range(28, H - 16, 32):
            d.rounded_rectangle([10, sy, 26, sy + 28], radius=3, fill=stone_c[(sy // 32) % 3], outline=(36, 30, 26, 255), width=2)
            d.rounded_rectangle([W - 27, sy, W - 11, sy + 28], radius=3, fill=stone_c[(sy // 32 + 1) % 3], outline=(36, 30, 26, 255), width=2)
        # Top stone arch header
        d.rounded_rectangle([12, 8, W - 13, 30], radius=10, fill=(108, 98, 88, 255), outline=(32, 26, 22, 255), width=3)

        # Inner heavy oak door frame
        d.rounded_rectangle([24, 24, W - 25, H - 2], radius=14, fill=(58, 34, 18, 255), outline=(22, 12, 7, 255), width=3)

        if not is_open:
            # Closed aged oak planks with vertical wood grain and wrought-iron hinges
            plank_w = (W - 60) // 4
            for pi in range(4):
                px0 = 30 + pi * plank_w
                px1 = px0 + plank_w - 2
                base_r = 104 + (pi % 2) * 8
                base_g = 62 + (pi % 2) * 5
                base_b = 34 + (pi % 2) * 3
                d.rectangle([px0, 30, px1, H - 6], fill=(base_r, base_g, base_b, 255), outline=(32, 18, 10, 255), width=2)
                # Subtle vertical highlight
                d.line([px0 + 4, 34, px0 + 4, H - 10], fill=(base_r + 18, base_g + 12, base_b + 8, 180), width=2)

            # Horizontal oak cross-battens
            for by in [76, H - 78]:
                d.rectangle([30, by, W - 31, by + 20], fill=(78, 46, 24, 255), outline=(26, 14, 8, 255), width=2)
                d.line([32, by + 3, W - 33, by + 3], fill=(106, 64, 36, 200), width=2)

            # Wrought-iron strap hinges
            for hy in [60, H - 62]:
                d.rectangle([30, hy, 96, hy + 10], fill=(42, 44, 48, 255), outline=(18, 18, 20, 255), width=2)
                for rx in [42, 64, 86]:
                    d.ellipse([rx - 3, hy + 2, rx + 3, hy + 8], fill=(130, 136, 144, 255))

            # Brass/iron ring handle on right side
            hx, hy = W - 46, H // 2 + 6
            d.ellipse([hx - 10, hy - 10, hx + 10, hy + 10], fill=(48, 44, 40, 255), outline=(20, 18, 16, 255), width=2)
            d.arc([hx - 8, hy - 2, hx + 8, hy + 16], start=0, end=360, fill=(196, 154, 72, 255), width=3)

            if is_locked:
                # Heavy iron chains & golden padlock across the door
                d.line([30, 90, W - 31, H - 70], fill=(24, 26, 30, 255), width=12)
                d.line([30, 90, W - 31, H - 70], fill=(108, 114, 124, 255), width=6)
                d.line([30, H - 70, W - 31, 90], fill=(24, 26, 30, 255), width=12)
                d.line([30, H - 70, W - 31, 90], fill=(108, 114, 124, 255), width=6)
                cx, cy = W // 2, H // 2 + 8
                d.arc([cx - 14, cy - 26, cx + 14, cy + 2], start=180, end=0, fill=(190, 196, 206, 255), width=5)
                d.rounded_rectangle([cx - 18, cy - 6, cx + 18, cy + 26], radius=5, fill=(214, 166, 62, 255), outline=(38, 24, 10, 255), width=3)
                d.ellipse([cx - 4, cy + 4, cx + 4, cy + 12], fill=(32, 20, 10, 255))
        else:
            # Open doorway: warm amber interior glow fading smoothly into depth + angled open door leaf on left
            px = im.load()
            for y in range(30, H - 6):
                for x in range(30, W - 30):
                    # Radial warm lantern glow inside doorway
                    dx = (x - (W * 0.58)) / float(W * 0.4)
                    dy = (y - (H * 0.48)) / float(H * 0.5)
                    dist = math.sqrt(dx * dx + dy * dy)
                    glow = max(0.0, 1.0 - dist * 0.78)
                    r = int(52 + glow * 158)
                    g = int(32 + glow * 102)
                    b = int(18 + glow * 44)
                    px[x, y] = (r, g, b, 255)

            # Open wooden door leaf swung inward on the left side (x=30..68)
            d.polygon([(30, 30), (70, 42), (70, H - 16), (30, H - 6)],
                      fill=(84, 48, 26, 255), outline=(26, 14, 8, 255))
            d.line([(48, 36), (48, H - 11)], fill=(54, 30, 16, 255), width=2)
            # Stone threshold step at bottom
            d.rectangle([20, H - 10, W - 21, H - 1], fill=(94, 86, 78, 255), outline=(28, 24, 20, 255), width=2)

        return im.resize((dw, dh), Image.LANCZOS)

    make_door_base(is_open=False, is_locked=False).save(os.path.join(ENV_DIR, "Village_Door_Closed.png"))
    make_door_base(is_open=True, is_locked=False).save(os.path.join(ENV_DIR, "Village_Door_Open.png"))
    make_door_base(is_open=False, is_locked=True).save(os.path.join(ENV_DIR, "Village_Door_Locked.png"))

    # Smooth colonial bookshelf (124x160 at 64 PPU)
    bw, bh = 124, 160
    W, H = bw * 2, bh * 2
    bs = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bd = ImageDraw.Draw(bs)
    bd.rounded_rectangle([6, 12, W - 7, H - 2], radius=6, fill=(76, 44, 24, 255), outline=(24, 14, 8, 255), width=4)
    bd.rounded_rectangle([2, 4, W - 3, 22], radius=4, fill=(98, 58, 32, 255), outline=(24, 14, 8, 255), width=3)
    bd.rectangle([18, 24, W - 19, H - 14], fill=(42, 24, 14, 255))

    rng = random.Random(77)
    book_cols = [
        (138, 48, 38), (52, 88, 118), (64, 104, 62), (148, 112, 48),
        (98, 62, 108), (118, 74, 46), (78, 92, 104)
    ]
    shelf_ys = [84, 148, 212, 276]
    for si, sy in enumerate(shelf_ys):
        bd.rectangle([12, sy, W - 13, sy + 12], fill=(96, 56, 30, 255), outline=(26, 14, 8, 255), width=2)
        bx = 22
        while bx < W - 28:
            bw_book = rng.randint(10, 16)
            bh_book = rng.randint(36, 48)
            col = rng.choice(book_cols)
            bd.rounded_rectangle([bx, sy - bh_book, bx + bw_book, sy - 1], radius=2,
                                 fill=col + (255,), outline=(24, 16, 12, 255), width=2)
            bd.line([bx + 2, sy - bh_book + 6, bx + bw_book - 2, sy - bh_book + 6],
                    fill=(206, 178, 112, 200), width=2)
            bx += bw_book + rng.randint(1, 4)

    bs.resize((bw, bh), Image.LANCZOS).save(os.path.join(ENV_DIR, "Interior_Bookshelf.png"))

    # Smooth colonial window with shutters (96x84 at 64 PPU)
    ww, wh = 96, 84
    W, H = ww * 2, wh * 2
    win = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    wd = ImageDraw.Draw(win)
    # Night sky glass pane in center
    wd.rounded_rectangle([42, 14, W - 43, H - 16], radius=8, fill=(36, 48, 78, 255), outline=(24, 14, 8, 255), width=4)
    # Soft moonlight glow
    wd.ellipse([W // 2 + 4, 24, W // 2 + 24, 44], fill=(238, 228, 188, 230))
    # Diamond lead muntins
    for offset in range(-40, 60, 22):
        wd.line([46, 20 + offset, W - 47, 70 + offset], fill=(28, 22, 18, 190), width=2)
        wd.line([46, 70 + offset, W - 47, 20 + offset], fill=(28, 22, 18, 190), width=2)
    # Center wooden mullion cross
    wd.rectangle([W // 2 - 3, 16, W // 2 + 3, H - 18], fill=(78, 46, 26, 255))
    wd.rectangle([44, H // 2 - 3, W - 45, H // 2 + 3], fill=(78, 46, 26, 255))
    # Left & Right wooden louvered shutters
    for sx0, sx1 in [(6, 40), (W - 41, W - 7)]:
        wd.rounded_rectangle([sx0, 14, sx1, H - 16], radius=4, fill=(88, 52, 28, 255), outline=(24, 14, 8, 255), width=3)
        for ly in range(24, H - 24, 14):
            wd.line([sx0 + 5, ly, sx1 - 5, ly], fill=(48, 28, 15, 255), width=3)
    # Bottom wooden windowsill
    wd.rounded_rectangle([32, H - 16, W - 33, H - 6], radius=3, fill=(102, 62, 34, 255), outline=(24, 14, 8, 255), width=3)
    win.resize((ww, wh), Image.LANCZOS).save(os.path.join(ENV_DIR, "Interior_Window_Shutters.png"))


def draw_colonial_cutaway_room(filename, world_w, world_h, floor_rel_ys, has_cellar=False, theme="apothecary"):
    """
    Draws a high-resolution (64 PPU), smoothly shaded, painterly colonial cutaway backdrop
    that aligns to the EXACT world Y coordinates of the house's real floors and keeps
    a dark, warm, atmospheric value (65-75%) so foreground platforms, furniture, chests,
    keys, and the player stand out with clear visual hierarchy.
    """
    PPU = 64
    width_px = int(round(world_w * PPU))
    height_px = int(round(world_h * PPU))

    img = Image.new("RGBA", (width_px, height_px), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rng = random.Random(hash(filename) & 0xFFFFFFFF)

    # Dark atmospheric colonial palettes (muted so foreground gameplay pops!)
    if theme == "apothecary":
        plaster_top = (148, 126, 96)
        plaster_bot = (122, 102, 76)
        wainscot_col = (74, 44, 25)
        shingle_col = (124, 52, 28)
    elif theme == "tavern":
        plaster_top = (144, 120, 90)
        plaster_bot = (118, 96, 70)
        wainscot_col = (68, 40, 22)
        shingle_col = (116, 48, 26)
    elif theme == "abandoned":
        plaster_top = (112, 98, 82)
        plaster_bot = (88, 76, 62)
        wainscot_col = (56, 36, 22)
        shingle_col = (68, 56, 46)
    else:  # manor
        plaster_top = (152, 130, 100)
        plaster_bot = (124, 104, 78)
        wainscot_col = (66, 36, 22)
        shingle_col = (58, 62, 74)

    beam_dark = (46, 27, 15, 255)
    beam_mid = (72, 43, 24, 255)
    beam_hi = (98, 60, 34, 255)
    outline_dark = (22, 13, 7, 255)

    border_w = int(0.45 * PPU)  # 28px clean timber/stone outer wall frame
    roof_h = int(0.45 * PPU)    # 28px roof crown at top

    # Convert floor_rel_ys (in world units from bottom of backdrop) to pixel Y (from top of image)
    # Note: floor_rel_ys[0] is the bottom floor (~0.25u from bottom), floor_rel_ys[1] is 4.25u, etc.
    story_bounds_py = [roof_h]
    for rel_y in sorted(floor_rel_ys, reverse=True):
        py = height_px - int(round(rel_y * PPU))
        if roof_h + 40 < py < height_px - 20:
            story_bounds_py.append(py)
    story_bounds_py.append(height_px - int(0.25 * PPU))

    # 1. Paint smooth gradient plaster / cellar stone per story with soft ambient occlusion
    px = img.load()
    cellar_top_py = story_bounds_py[-2] if has_cellar and len(story_bounds_py) >= 3 else height_px

    for si in range(len(story_bounds_py) - 1):
        y0 = story_bounds_py[si]
        y1 = story_bounds_py[si + 1]
        sh = max(1, y1 - y0)
        is_cellar_story = has_cellar and (y0 >= cellar_top_py - 4)

        for y in range(y0, y1):
            ty = (y - y0) / float(sh)
            # Soft vertical AO near ceiling (ty->0) and floor (ty->1)
            ao_y = 0.72 + 0.28 * math.sin(ty * math.pi)
            for x in range(border_w, width_px - border_w):
                tx = (x - border_w) / float(max(1, width_px - 2 * border_w))
                ao_x = 0.78 + 0.22 * math.sin(tx * math.pi)
                ao = ao_y * ao_x
                noise = math.sin(x * 0.06 + y * 0.05) * 3.0

                if is_cellar_story:
                    r = int(52 * ao + noise)
                    g = int(48 * ao + noise)
                    b = int(44 * ao + noise)
                else:
                    r = int((plaster_top[0] * (1.0 - ty) + plaster_bot[0] * ty) * ao + noise)
                    g = int((plaster_top[1] * (1.0 - ty) + plaster_bot[1] * ty) * ao + noise)
                    b = int((plaster_top[2] * (1.0 - ty) + plaster_bot[2] * ty) * ao + noise)
                px[x, y] = (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), 255)

    # 2. If cellar story exists, draw subtle recessed stone masonry & 3 vaulted arches
    if has_cellar and cellar_top_py < height_px - 40:
        y0 = cellar_top_py
        y1 = story_bounds_py[-1]
        stone_cols = [(72, 66, 60), (64, 59, 54), (80, 74, 67), (58, 53, 49)]
        cy = y0 + 6
        crow = 0
        while cy < y1 - 8:
            rh = 24 if crow % 2 == 0 else 28
            if cy + rh > y1 - 4:
                rh = (y1 - 4) - cy
            cx = border_w + 4
            while cx < width_px - border_w - 6:
                sw = rng.randint(36, 58)
                if cx + sw > width_px - border_w - 4 or (width_px - border_w - 4) - (cx + sw) < 12:
                    sw = (width_px - border_w - 4) - cx
                if sw >= 8 and rh >= 8:
                    sc = rng.choice(stone_cols)
                    d.rounded_rectangle([cx, cy, cx + sw - 2, cy + rh - 2], radius=3,
                                        fill=sc + (255,), outline=(32, 28, 25, 255))
                cx += sw
            cy += rh
            crow += 1

        # 3 subtle recessed masonry arches in cellar back wall
        inner_w = width_px - 2 * border_w
        arch_span = inner_w // 3
        for ai in range(3):
            ax0 = border_w + ai * arch_span + 48
            ax1 = border_w + (ai + 1) * arch_span - 48
            ay0 = y0 + 28
            ay1 = y1 - 6
            d.rounded_rectangle([ax0, ay0, ax1, ay1], radius=44,
                                fill=(36, 32, 29, 255), outline=(24, 21, 18, 255), width=3)
            d.arc([ax0 - 4, ay0 - 4, ax1 + 4, ay0 + 76], start=180, end=0,
                  fill=(88, 80, 72, 255), width=6)

    # 3. For each upper story, draw clean wainscoting, vertical oak posts, and smooth ceiling beams
    inner_w = width_px - 2 * border_w
    num_bays = 4
    bay_w = inner_w / float(num_bays)

    for si in range(len(story_bounds_py) - 1):
        y_top = story_bounds_py[si]
        y_bot = story_bounds_py[si + 1]
        if has_cellar and y_top >= cellar_top_py - 4:
            continue

        # Subtle wood wainscoting along bottom 58px (~0.9u) of the story
        wain_h = min(58, (y_bot - y_top) // 3)
        wain_top = y_bot - wain_h
        d.rectangle([border_w, wain_top, width_px - border_w - 1, y_bot], fill=wainscot_col + (255,))
        d.line([border_w, wain_top, width_px - border_w - 1, wain_top], fill=outline_dark, width=3)
        d.line([border_w, wain_top + 3, width_px - border_w - 1, wain_top + 3], fill=beam_hi, width=2)

        for wx in range(border_w + 14, width_px - border_w - 30, 44):
            d.rounded_rectangle([wx, wain_top + 10, wx + 34, y_bot - 8], radius=2,
                                fill=(wainscot_col[0] - 10, wainscot_col[1] - 7, wainscot_col[2] - 5, 255),
                                outline=(34, 20, 11, 255), width=2)

        # Ceiling beam at top of story
        d.rectangle([border_w, y_top, width_px - border_w - 1, y_top + 16], fill=beam_mid, outline=outline_dark, width=2)
        d.line([border_w, y_top + 3, width_px - border_w - 1, y_top + 3], fill=beam_hi, width=2)

        # Vertical oak posts dividing the wall into 4 clean architectural bays
        for bi in range(num_bays + 1):
            bx = int(round(border_w + bi * bay_w))
            d.rectangle([bx - 8, y_top, bx + 8, y_bot], fill=beam_mid, outline=outline_dark, width=2)
            d.line([bx - 3, y_top + 4, bx - 3, y_bot - 4], fill=beam_hi, width=2)

            # Curved/smooth wooden corbels in upper corners of each bay (no jagged diagonal pixel stairs!)
            if bi < num_bays:
                nbx = int(round(border_w + (bi + 1) * bay_w))
                d.polygon([(bx + 8, y_top + 16), (bx + 44, y_top + 16), (bx + 8, y_top + 52)],
                          fill=beam_dark, outline=outline_dark)
                d.polygon([(nbx - 8, y_top + 16), (nbx - 44, y_top + 16), (nbx - 8, y_top + 52)],
                          fill=beam_dark, outline=outline_dark)

    # 4. Outer architectural frame (clean solid oak & dressed stone border, NO checkerboard blocks!)
    for sx0, sx1 in [(0, border_w), (width_px - border_w, width_px - 1)]:
        d.rectangle([sx0, roof_h, sx1, height_px - 1], fill=(54, 34, 20, 255), outline=outline_dark, width=3)
        d.line([sx0 + 6, roof_h + 4, sx0 + 6, height_px - 4], fill=(82, 52, 30, 255), width=3)
        d.line([sx1 - 6, roof_h + 4, sx1 - 6, height_px - 4], fill=(34, 20, 12, 255), width=3)

    # Top Roof Eave Crown
    d.rectangle([0, 0, width_px - 1, roof_h], fill=shingle_col + (255,), outline=outline_dark, width=3)
    for ry in range(8, roof_h - 6, 10):
        d.line([4, ry, width_px - 5, ry], fill=(max(0, shingle_col[0] - 24), max(0, shingle_col[1] - 18), max(0, shingle_col[2] - 14), 255), width=2)
    d.rectangle([0, roof_h - 6, width_px - 1, roof_h + 2], fill=beam_dark, outline=outline_dark, width=2)

    # Bottom Stone Foundation Base
    d.rectangle([0, height_px - 18, width_px - 1, height_px - 1], fill=(56, 52, 48, 255), outline=outline_dark, width=3)

    # Slight smooth filter so diagonal/curved corbels have zero pixel stair-stepping
    img = img.filter(ImageFilter.SMOOTH)
    img.save(os.path.join(ENV_DIR, filename))


def generate_all_four_house_cutaways():
    # House 1: Apothecary (world width 17.2u, world height 11.6u: y=28.25..39.85)
    # Real floors at y=28.50 (rel 0.25u) and y=32.50 (rel 4.25u). NO fake floor at loft!
    draw_colonial_cutaway_room(
        "RoomCutaway_House1_Apothecary.png",
        17.2, 11.6,
        floor_rel_ys=[0.25, 4.25],
        has_cellar=False, theme="apothecary"
    )

    # House 2: Tavern (world width 19.2u, world height 12.8u: y=24.25..37.05)
    # Real floors at y=24.50 (rel 0.25u Cellar), y=28.50 (rel 4.25u F1), y=32.50 (rel 8.25u F2)
    draw_colonial_cutaway_room(
        "RoomCutaway_House2_Tavern.png",
        19.2, 12.8,
        floor_rel_ys=[0.25, 4.25, 8.25],
        has_cellar=True, theme="tavern"
    )

    # House 3: Abandoned (world width 19.2u, world height 12.8u: y=28.25..41.05)
    # Real floors at y=28.50 (rel 0.25u F1), y=32.50 (rel 4.25u F2), y=36.50 (rel 8.25u Attic)
    draw_colonial_cutaway_room(
        "RoomCutaway_House3_Abandoned.png",
        19.2, 12.8,
        floor_rel_ys=[0.25, 4.25, 8.25],
        has_cellar=False, theme="abandoned"
    )

    # House 4: Manor (world width 21.2u, world height 13.2u: y=24.25..37.45)
    # Real floors at y=24.50 (rel 0.25u Crypt), y=28.50 (rel 4.25u F1), y=32.50 (rel 8.25u F2)
    draw_colonial_cutaway_room(
        "RoomCutaway_House4_Manor.png",
        21.2, 13.2,
        floor_rel_ys=[0.25, 4.25, 8.25],
        has_cellar=True, theme="manor"
    )


def composite_cainos_furniture_sprites():
    if not os.path.exists(CAINOS_TEX):
        return

    table = crop_cainos(315, 794, 74, 30)
    chair = crop_cainos(395, 794, 22, 42)
    bottle = crop_cainos(330, 829, 8, 19)
    cup = crop_cainos(339, 828, 9, 8)
    bread = crop_cainos(354, 829, 16, 9)
    apple = crop_cainos(341, 838, 8, 9)

    if table and chair:
        tc = Image.new("RGBA", (112, 64), (0, 0, 0, 0))
        chair_l = chair.transpose(Image.FLIP_LEFT_RIGHT)
        tc.paste(chair_l, (2, 64 - 42), chair_l)
        tc.paste(chair, (112 - 24, 64 - 42), chair)
        tc.paste(table, (19, 64 - 30), table)
        table_top_y = 64 - 24
        if bottle:
            tc.paste(bottle, (34, table_top_y - 18), bottle)
        if cup:
            tc.paste(cup, (48, table_top_y - 7), cup)
        if bread:
            tc.paste(bread, (60, table_top_y - 8), bread)
        if apple:
            tc.paste(apple, (80, table_top_y - 8), apple)
        tc_full = Image.new("RGBA", (160, 96), (0, 0, 0, 0))
        tc_scaled = tc.resize((148, 84), Image.BICUBIC)
        tc_full.paste(tc_scaled, (6, 96 - 84), tc_scaled)
        tc_full.save(os.path.join(ENV_DIR, "Interior_Table_Chairs.png"))

    pot1 = crop_cainos(264, 960, 17, 29)
    pot2 = crop_cainos(294, 960, 21, 20)
    pot3 = crop_cainos(328, 960, 20, 29)
    kettle = crop_cainos(329, 608, 14, 15)
    wbottle = crop_cainos(364, 625, 7, 11)
    stall_table = crop_cainos(390, 608, 86, 33)

    if stall_table:
        cr = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
        cr.paste(stall_table, (5, 96 - 33), stall_table)
        counter_top_y = 96 - 31
        if pot1:
            cr.paste(pot1, (12, counter_top_y - 28), pot1)
        if pot2:
            cr.paste(pot2, (32, counter_top_y - 19), pot2)
        if kettle:
            k_big = kettle.resize((21, 22), Image.BICUBIC)
            cr.paste(k_big, (55, counter_top_y - 21), k_big)
        if wbottle:
            wb_big = wbottle.resize((12, 18), Image.BICUBIC)
            cr.paste(wb_big, (77, counter_top_y - 17), wb_big)
        if pot3:
            cr.paste(pot3, (74, 96 - 29), pot3)
        cr.save(os.path.join(ENV_DIR, "Interior_Cauldron_Rack.png"))


if __name__ == "__main__":
    sync_cainos_barrels_crates_chests()
    generate_smooth_colonial_floors_and_structures()
    generate_smooth_colonial_doors_and_props()
    generate_all_four_house_cutaways()
    composite_cainos_furniture_sprites()
    print("Generated smooth colonial cutaways (64 PPU), solid floors, loft, cat arch, doors, and furniture!")
