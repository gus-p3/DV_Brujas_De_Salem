import sys
import os
from PIL import Image
import numpy as np
from scipy.ndimage import label, find_objects

def process_spritesheet(input_path, output_path, num_frames, cell_size=256):
    img = Image.open(input_path).convert("RGBA")
    data = np.array(img)

    # Make white background transparent
    r, g, b, a = data.T
    white_areas = (r > 240) & (g > 240) & (b > 240)
    data[..., 3][white_areas.T] = 0
    img_clean = Image.fromarray(data)

    # Find isolated components (sprites)
    alpha = data[..., 3]
    mask = alpha > 0
    labeled, num_features = label(mask)
    slices = find_objects(labeled)

    # Filter out very small noise
    valid_slices = []
    for s in slices:
        if s is not None:
            dy = s[0].stop - s[0].start
            dx = s[1].stop - s[1].start
            if dx > 20 and dy > 20:
                valid_slices.append(s)

    # Sort slices left to right
    valid_slices.sort(key=lambda s: s[1].start)
    
    # We expect `num_frames` components, if more, we might need to merge or just take the biggest ones
    # For now, let's just take the first `num_frames` from left to right.
    if len(valid_slices) > num_frames:
        valid_slices = valid_slices[:num_frames]
    
    # Create the final strip
    out_width = cell_size * num_frames
    out_height = cell_size
    out_img = Image.new("RGBA", (out_width, out_height), (0, 0, 0, 0))

    # Find global bottom to align them correctly (optional, or local bottom)
    # Actually, let's align each sprite to the bottom center of its cell
    for i, s in enumerate(valid_slices):
        if i >= num_frames: break
        sprite_box = img_clean.crop((s[1].start, s[0].start, s[1].stop, s[0].stop))
        
        sw, sh = sprite_box.size
        # Resize if it's too big for the cell (leave some margin)
        max_h = cell_size - 16
        max_w = cell_size - 16
        if sh > max_h or sw > max_w:
            ratio = min(max_w / sw, max_h / sh)
            new_w = int(sw * ratio)
            new_h = int(sh * ratio)
            sprite_box = sprite_box.resize((new_w, new_h), Image.Resampling.LANCZOS)
            sw, sh = new_w, new_h
        
        # Paste into cell
        cell_x = i * cell_size
        paste_x = cell_x + (cell_size - sw) // 2
        paste_y = cell_size - sh - 8 # 8 pixels padding from bottom
        
        out_img.paste(sprite_box, (paste_x, paste_y), sprite_box)
        
    out_img.save(output_path)
    print(f"Saved {output_path}")

def process_simple(input_path, output_path):
    img = Image.open(input_path).convert("RGBA")
    data = np.array(img)
    r, g, b, a = data.T
    white_areas = (r > 240) & (g > 240) & (b > 240)
    data[..., 3][white_areas.T] = 0
    img_clean = Image.fromarray(data)
    img_clean.save(output_path)
    print(f"Saved {output_path}")

base_dir = r"c:\Users\brand\Documentos\GIDS6102\Video Juegos\Unidad 1\Ejercicio\brujas_de_salem"
os.makedirs(os.path.join(base_dir, "Assets", "Art", "UI"), exist_ok=True)
os.makedirs(os.path.join(base_dir, "Assets", "Art", "Characters", "Cat"), exist_ok=True)

brain_dir = r"C:\Users\brand\.gemini\antigravity\brain\4516ea65-716b-4682-bd15-bffa27cc1a5c"

# Process BG
bg_path = os.path.join(brain_dir, "main_menu_bg_1791520371473.jpg")
Image.open(bg_path).save(os.path.join(base_dir, "Assets", "Art", "UI", "main_menu_bg.png"))

# Process Fire
process_simple(os.path.join(brain_dir, "game_over_fire_1791520383526.jpg"), os.path.join(base_dir, "Assets", "Art", "UI", "game_over_fire.png"))

# Process Cat
process_spritesheet(os.path.join(brain_dir, "cat_idle_sheet_1791520416639.jpg"), os.path.join(base_dir, "Assets", "Art", "Characters", "Cat", "Cat_Idle.png"), 4)
process_spritesheet(os.path.join(brain_dir, "cat_walk_sheet_1791520432570.jpg"), os.path.join(base_dir, "Assets", "Art", "Characters", "Cat", "Cat_Walk.png"), 6)
process_spritesheet(os.path.join(brain_dir, "cat_sneak_sheet_1791520446714.jpg"), os.path.join(base_dir, "Assets", "Art", "Characters", "Cat", "Cat_Sneak.png"), 6)
