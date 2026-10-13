import os
import numpy as np
from PIL import Image
from scipy.ndimage import binary_erosion

ARTIFACT_DIR = r"C:\Users\brand\.gemini\antigravity\brain\4516ea65-716b-4682-bd15-bffa27cc1a5c"
OUT_DIR = r"Assets\Art\Environment"
os.makedirs(OUT_DIR, exist_ok=True)

def remove_white_tight(img_crop, threshold=235, erode=1):
    arr = np.array(img_crop.convert('RGB'))
    r, g, b = arr[:,:,0], arr[:,:,1], arr[:,:,2]
    is_white = (r > threshold) & (g > threshold) & (b > threshold)
    alpha = np.where(is_white, 0, 255).astype(np.uint8)
    
    if erode > 0:
        alpha = (binary_erosion(alpha > 0, iterations=erode).astype(np.uint8)) * 255
        
    rows = np.any(alpha > 0, axis=1)
    cols = np.any(alpha > 0, axis=0)
    if not np.any(rows) or not np.any(cols):
        return None
    rmin, rmax = np.where(rows)[0][[0, -1]]
    cmin, cmax = np.where(cols)[0][[0, -1]]
    
    rgba = np.dstack([r, g, b, alpha])
    return Image.fromarray(rgba).crop((cmin, rmin, cmax + 1, rmax + 1))

# ==============================================================================
# 1. PLATAFORMAS DE TRONCO (Env_Platform_Log_A, B, C)
# ==============================================================================
print("Procesando plataformas de tronco...")
gp_raw = Image.open(os.path.join(ARTIFACT_DIR, "env_ground_platforms_1791862359008.jpg"))

# Log A (Pequeño): Tronco corto de abajo a la derecha (w=300, h=154) -> 256x96
log_a_raw = gp_raw.crop((985, 531, 1287, 687))
log_a_clean = remove_white_tight(log_a_raw, threshold=235, erode=1)
log_a = log_a_clean.resize((256, 96), Image.Resampling.LANCZOS)
log_a.save(os.path.join(OUT_DIR, "Env_Platform_Log_A.png"))
print(" -> Env_Platform_Log_A.png guardado (256x96) - 0 padding")

# Log B (Mediano): Tronco mediano de arriba a la izquierda (w=562, h=222) -> 384x96
log_b_raw = gp_raw.crop((103, 131, 664, 352))
log_b_clean = remove_white_tight(log_b_raw, threshold=235, erode=1)
log_b = log_b_clean.resize((384, 96), Image.Resampling.LANCZOS)
log_b.save(os.path.join(OUT_DIR, "Env_Platform_Log_B.png"))
print(" -> Env_Platform_Log_B.png guardado (384x96) - 0 padding")

# Log C (Grande): Tronco largo de arriba a la derecha (w=545, h=196) -> 512x96
log_c_raw = gp_raw.crop((748, 40, 1292, 235))
log_c_clean = remove_white_tight(log_c_raw, threshold=235, erode=1)
log_c = log_c_clean.resize((512, 96), Image.Resampling.LANCZOS)
log_c.save(os.path.join(OUT_DIR, "Env_Platform_Log_C.png"))
print(" -> Env_Platform_Log_C.png guardado (512x96) - 0 padding")

# ==============================================================================
# 2. SUELO (Env_Ground_Tile)
# ==============================================================================
print("Procesando suelo repetible...")
# Bloque de suelo: x 74..832, y 436..702
ground_raw = gp_raw.crop((74, 436, 832, 702))
ground_clean = remove_white_tight(ground_raw, threshold=235, erode=1)
ground_tile = ground_clean.resize((512, 256), Image.Resampling.LANCZOS)

# Asegurar continuidad horizontal perfecta (seamless blending en los bordes izquierdo y derecho)
arr_g = np.array(ground_tile)
gw = 512
gb = 64
for i in range(gb):
    a = i / float(gb)
    arr_g[:, gw - gb + i] = (arr_g[:, gw - gb + i] * (1 - a) + arr_g[:, i] * a).astype(np.uint8)

# Asegurar que el fondo del suelo (y=180..255) sea totalmente opaco
arr_g[180:, :, 3] = 255
Image.fromarray(arr_g).save(os.path.join(OUT_DIR, "Env_Ground_Tile.png"))
print(" -> Env_Ground_Tile.png guardado (512x256) - seamless horizontal")

# ==============================================================================
# 3. MAIZALES (Env_Corn_Cluster, Env_Corn_Stalk_A, B, C)
# ==============================================================================
print("Procesando maizales y tallos...")
corn_raw = Image.open(os.path.join(ARTIFACT_DIR, "env_corn_stalks_1791862383489.jpg"))

# Cluster denso (x 777..1335, y 86..710) -> 768x512
cluster_raw = corn_raw.crop((777, 86, 1335, 710))
cluster_clean = remove_white_tight(cluster_raw, threshold=235, erode=1)
cluster_img = cluster_clean.resize((768, 512), Image.Resampling.LANCZOS)
cluster_img.save(os.path.join(OUT_DIR, "Env_Corn_Cluster.png"))
print(" -> Env_Corn_Cluster.png guardado (768x512) - denso, 0 padding lateral")

# Tallos individuales A, B, C
stalk_a_raw = corn_raw.crop((46, 52, 260, 709))
stalk_a_clean = remove_white_tight(stalk_a_raw, threshold=235, erode=1)
stalk_a = stalk_a_clean.resize((192, 512), Image.Resampling.LANCZOS)
stalk_a.save(os.path.join(OUT_DIR, "Env_Corn_Stalk_A.png"))

stalk_b_raw = corn_raw.crop((300, 63, 492, 711))
stalk_b_clean = remove_white_tight(stalk_b_raw, threshold=235, erode=1)
stalk_b = stalk_b_clean.resize((192, 512), Image.Resampling.LANCZOS)
stalk_b.save(os.path.join(OUT_DIR, "Env_Corn_Stalk_B.png"))

stalk_c_raw = corn_raw.crop((537, 60, 748, 709))
stalk_c_clean = remove_white_tight(stalk_c_raw, threshold=235, erode=1)
stalk_c = stalk_c_clean.resize((192, 512), Image.Resampling.LANCZOS)
stalk_c.save(os.path.join(OUT_DIR, "Env_Corn_Stalk_C.png"))
print(" -> Tallos A, B, C guardados (192x512) - 0 padding lateral")

# ==============================================================================
# 4. ELEMENTOS Y PROPS (Sign, Gate, Items)
# ==============================================================================
print("Procesando letrero, portón e ítems...")
props_raw = Image.open(os.path.join(ARTIFACT_DIR, "env_props_items_1791862411104.jpg"))

# Letrero de madera (x 117..571, y 38..481) -> 192x256
sign_raw = props_raw.crop((117, 38, 571, 481))
sign_clean = remove_white_tight(sign_raw, threshold=235, erode=1)
sign_img = sign_clean.resize((192, 256), Image.Resampling.LANCZOS)
sign_img.save(os.path.join(OUT_DIR, "Env_Sign.png"))
print(" -> Env_Sign.png guardado (192x256)")

# Portón con farol (x 771..1263, y 39..479) -> 384x384
gate_raw = props_raw.crop((771, 39, 1263, 479))
gate_clean = remove_white_tight(gate_raw, threshold=235, erode=1)
gate_img = gate_clean.resize((384, 384), Image.Resampling.LANCZOS)
gate_img.save(os.path.join(OUT_DIR, "Env_Goal_Gate.png"))
print(" -> Env_Goal_Gate.png guardado (384x384)")

# Ítems coleccionables (128x128 con margen sutil de 4px para centrado balanceado)
def format_item(crop, target_size=(128, 128), margin=4):
    clean = remove_white_tight(crop, threshold=235, erode=1)
    cw, ch = clean.size
    avail_w = target_size[0] - (margin * 2)
    avail_h = target_size[1] - (margin * 2)
    scale = min(avail_w / cw, avail_h / ch)
    nw, nh = max(1, int(cw * scale)), max(1, int(ch * scale))
    resized = clean.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', target_size, (0, 0, 0, 0))
    px = (target_size[0] - nw) // 2
    py = (target_size[1] - nh) // 2
    canvas.paste(resized, (px, py), resized)
    return canvas

branch_raw = props_raw.crop((80, 504, 506, 732))
format_item(branch_raw).save(os.path.join(OUT_DIR, "Item_Branch.png"))

straw_raw = props_raw.crop((539, 525, 926, 725))
format_item(straw_raw).save(os.path.join(OUT_DIR, "Item_Straw.png"))

rope_raw = props_raw.crop((971, 527, 1294, 733))
format_item(rope_raw).save(os.path.join(OUT_DIR, "Item_Rope.png"))
print(" -> Item_Branch, Straw, Rope guardados (128x128)")

# ==============================================================================
# 5. FONDOS (Sky, Trees)
# ==============================================================================
print("Verificando capas de fondo...")
# Env_Trees_Mid: silueta de árboles y cerca, 3840x1080 transparente y anclada al suelo
trees_raw = Image.open(os.path.join(ARTIFACT_DIR, "env_trees_mid_1791862333767.jpg"))
trees_clean = remove_white_tight(trees_raw, threshold=240, erode=1)
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
print(" -> Env_Trees_Mid.png verificado y actualizado")

print("\n¡TODAS LAS IMÁGENES HAN SIDO MEJORADAS Y RECORTADAS EXITOSAMENTE!")
