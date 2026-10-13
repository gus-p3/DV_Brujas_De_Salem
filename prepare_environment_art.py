import os
import numpy as np
from PIL import Image
from scipy.ndimage import binary_erosion

ARTIFACT_DIR = r"C:\Users\brand\.gemini\antigravity\brain\4516ea65-716b-4682-bd15-bffa27cc1a5c"
OUT_DIR = r"Assets\Art\Environment"
os.makedirs(OUT_DIR, exist_ok=True)

def remove_white(img_pil, threshold=235, erode=1):
    arr = np.array(img_pil.convert('RGB'))
    r, g, b = arr[:,:,0], arr[:,:,1], arr[:,:,2]
    is_white = (r > threshold) & (g > threshold) & (b > threshold)
    alpha = np.where(is_white, 0, 255).astype(np.uint8)
    
    if erode > 0:
        alpha = binary_erosion(alpha > 0, iterations=erode).astype(np.uint8) * 255
        
    rgba = np.zeros((arr.shape[0], arr.shape[1], 4), dtype=np.uint8)
    rgba[:,:,0] = r
    rgba[:,:,1] = g
    rgba[:,:,2] = b
    rgba[:,:,3] = alpha
    return Image.fromarray(rgba)

def get_bbox(img_rgba):
    # find bounding box where alpha > 0
    alpha = np.array(img_rgba)[:, :, 3]
    rows = np.any(alpha > 0, axis=1)
    cols = np.any(alpha > 0, axis=0)
    if not np.any(rows) or not np.any(cols):
        return None
    rmin, rmax = np.where(rows)[0][[0, -1]]
    cmin, cmax = np.where(cols)[0][[0, -1]]
    return (cmin, rmin, cmax + 1, rmax + 1)

def fit_into(img_rgba, target_w, target_h, pad_color=(0,0,0,0), align='center'):
    # tight crop first
    bbox = get_bbox(img_rgba)
    if bbox:
        cropped = img_rgba.crop(bbox)
    else:
        cropped = img_rgba
        
    cw, ch = cropped.size
    scale = min(target_w / cw, target_h / ch)
    nw, nh = max(1, int(cw * scale)), max(1, int(ch * scale))
    resized = cropped.resize((nw, nh), Image.Resampling.LANCZOS)
    
    canvas = Image.new('RGBA', (target_w, target_h), pad_color)
    if align == 'bottom':
        px = (target_w - nw) // 2
        py = target_h - nh
    elif align == 'center':
        px = (target_w - nw) // 2
        py = (target_h - nh) // 2
    else: # stretch
        return cropped.resize((target_w, target_h), Image.Resampling.LANCZOS)
        
    canvas.paste(resized, (px, py), resized)
    return canvas

# 1. Env_Sky_Far.png (3840x1080)
print("1. Processing Env_Sky_Far.png...")
sky_img = Image.open(os.path.join(ARTIFACT_DIR, "env_sky_far_1791862309229.jpg")).convert('RGB')
sky_resized = sky_img.resize((3840, 1080), Image.Resampling.LANCZOS)
# Make seamless horizontal blend over 240 pixels
arr_sky = np.array(sky_resized)
w = 3840
blend_w = 240
for i in range(blend_w):
    alpha = i / float(blend_w)
    # blend left edge into right edge
    arr_sky[:, w - blend_w + i] = (arr_sky[:, w - blend_w + i] * (1 - alpha) + arr_sky[:, i] * alpha).astype(np.uint8)
Image.fromarray(arr_sky).save(os.path.join(OUT_DIR, "Env_Sky_Far.png"))

# 2. Env_Trees_Mid.png (3840x1080)
print("2. Processing Env_Trees_Mid.png...")
trees_raw = Image.open(os.path.join(ARTIFACT_DIR, "env_trees_mid_1791862333767.jpg"))
trees_clean = remove_white(trees_raw, threshold=240, erode=1)
# Seamless blend horizontally:
trees_scaled = trees_clean.resize((3840, 1080), Image.Resampling.LANCZOS)
arr_trees = np.array(trees_scaled)
w = 3840
blend_w = 240
for i in range(blend_w):
    alpha = i / float(blend_w)
    col_l = arr_trees[:, i].astype(float)
    col_r = arr_trees[:, w - blend_w + i].astype(float)
    blended = col_r * (1 - alpha) + col_l * alpha
    arr_trees[:, w - blend_w + i] = blended.astype(np.uint8)
Image.fromarray(arr_trees).save(os.path.join(OUT_DIR, "Env_Trees_Mid.png"))

# 3. Ground and Platforms
print("3. Processing Ground & Platforms...")
gp_raw = Image.open(os.path.join(ARTIFACT_DIR, "env_ground_platforms_1791862359008.jpg"))
# Ground block (bottom left: x 50..820, y 480..730)
ground_crop = gp_raw.crop((60, 480, 820, 730))
ground_clean = remove_white(ground_crop, threshold=240, erode=1)
ground_tile = ground_clean.resize((512, 256), Image.Resampling.LANCZOS)
# Make seamless horizontal blend for tile
arr_g = np.array(ground_tile)
gw = 512
gb = 60
for i in range(gb):
    a = i / float(gb)
    arr_g[:, gw - gb + i] = (arr_g[:, gw - gb + i] * (1 - a) + arr_g[:, i] * a).astype(np.uint8)
Image.fromarray(arr_g).save(os.path.join(OUT_DIR, "Env_Ground_Tile.png"))

# Platform Log A (256x96): Top left log (x 80..670, y 150..370)
log_a_crop = gp_raw.crop((80, 150, 670, 370))
log_a_clean = remove_white(log_a_crop, threshold=240, erode=1)
log_a = fit_into(log_a_clean, 256, 96, align='center')
log_a.save(os.path.join(OUT_DIR, "Env_Platform_Log_A.png"))

# Platform Log B (384x96): Top right log (x 740..1320, y 50..260)
log_b_crop = gp_raw.crop((740, 50, 1320, 260))
log_b_clean = remove_white(log_b_crop, threshold=240, erode=1)
log_b = fit_into(log_b_clean, 384, 96, align='center')
log_b.save(os.path.join(OUT_DIR, "Env_Platform_Log_B.png"))

# Platform Log C (512x96): Middle right log (x 850..1320, y 300..480)
log_c_crop = gp_raw.crop((850, 300, 1320, 480))
log_c_clean = remove_white(log_c_crop, threshold=240, erode=1)
log_c = fit_into(log_c_clean, 512, 96, align='center')
log_c.save(os.path.join(OUT_DIR, "Env_Platform_Log_C.png"))

# 4. Corn Stalks & Cluster
print("4. Processing Corn Stalks & Cluster...")
corn_raw = Image.open(os.path.join(ARTIFACT_DIR, "env_corn_stalks_1791862383489.jpg"))
# Stalk A (x 30..260, y 40..730)
stalk_a_clean = remove_white(corn_raw.crop((30, 40, 260, 730)), threshold=240, erode=1)
fit_into(stalk_a_clean, 256, 512, align='bottom').save(os.path.join(OUT_DIR, "Env_Corn_Stalk_A.png"))

# Stalk B (x 280..520, y 50..730)
stalk_b_clean = remove_white(corn_raw.crop((280, 50, 520, 730)), threshold=240, erode=1)
fit_into(stalk_b_clean, 256, 512, align='bottom').save(os.path.join(OUT_DIR, "Env_Corn_Stalk_B.png"))

# Stalk C (x 530..760, y 50..730)
stalk_c_clean = remove_white(corn_raw.crop((530, 50, 760, 730)), threshold=240, erode=1)
fit_into(stalk_c_clean, 256, 512, align='bottom').save(os.path.join(OUT_DIR, "Env_Corn_Stalk_C.png"))

# Cluster (x 780..1350, y 60..730)
cluster_clean = remove_white(corn_raw.crop((780, 60, 1350, 730)), threshold=240, erode=1)
fit_into(cluster_clean, 768, 512, align='bottom').save(os.path.join(OUT_DIR, "Env_Corn_Cluster.png"))

# 5. Props and Items
print("5. Processing Props and Items...")
props_raw = Image.open(os.path.join(ARTIFACT_DIR, "env_props_items_1791862411104.jpg"))
# Sign (x 50..580, y 40..460) -> 192x256
sign_clean = remove_white(props_raw.crop((50, 40, 580, 460)), threshold=240, erode=1)
fit_into(sign_clean, 192, 256, align='bottom').save(os.path.join(OUT_DIR, "Env_Sign.png"))

# Goal Gate (x 740..1300, y 40..460) -> 384x384
gate_clean = remove_white(props_raw.crop((740, 40, 1300, 460)), threshold=240, erode=1)
fit_into(gate_clean, 384, 384, align='bottom').save(os.path.join(OUT_DIR, "Env_Goal_Gate.png"))

# Item Branch (x 50..520, y 470..730) -> 128x128
branch_clean = remove_white(props_raw.crop((50, 470, 520, 730)), threshold=240, erode=1)
fit_into(branch_clean, 128, 128, align='center').save(os.path.join(OUT_DIR, "Item_Branch.png"))

# Item Straw (x 530..940, y 470..730) -> 128x128
straw_clean = remove_white(props_raw.crop((530, 470, 940, 730)), threshold=240, erode=1)
fit_into(straw_clean, 128, 128, align='center').save(os.path.join(OUT_DIR, "Item_Straw.png"))

# Item Rope (x 960..1340, y 470..730) -> 128x128
rope_clean = remove_white(props_raw.crop((960, 470, 1340, 730)), threshold=240, erode=1)
fit_into(rope_clean, 128, 128, align='center').save(os.path.join(OUT_DIR, "Item_Rope.png"))

print("ALL ASSETS GENERATED AND SAVED TO", OUT_DIR)
