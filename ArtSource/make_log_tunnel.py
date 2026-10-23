"""
Túnel de tronco provisional a partir del tronco largo aprobado (ArtSource/Environment/platforms.png):
- Env_Log_Tunnel_Back.png: el tronco con el interior hueco oscuro a lo largo.
- Env_Log_Tunnel_Front.png: solo la corteza superior y los dos bordes, para que el gato pase "dentro".
Se sustituirá por la versión de ChatGPT (Correcciones_Lote1.md, sección 6) cuando esté lista.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, "ArtSource/Villagers")
from slice_sheet import border_key  # noqa: E402

OUT = Path("Assets/Art/Environment")
W, H = 512, 256

sheet = border_key(Image.open("ArtSource/Environment/platforms.png").convert("RGB"))
log = sheet.crop((885, 365, 1510, 545))
log = log.crop(log.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())
log = log.resize((W, H), Image.LANCZOS)
a = np.asarray(log).astype(np.float32)
alpha = a[..., 3] > 8

# Región del hueco: elipse alargada dentro del tronco
yy, xx = np.mgrid[0:H, 0:W]
cx, cy = W / 2, H * 0.58
rx, ry = W * 0.43, H * 0.33
inside = (((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2) <= 1.0
inside &= alpha

# Fondo: interior oscuro con degradado y vetas
back = a.copy()
t = np.clip((yy - (cy - ry)) / (2 * ry), 0, 1)
dark_top = np.array([46, 27, 17], np.float32)
dark_bot = np.array([22, 12, 8], np.float32)
interior = dark_top * (1 - t[..., None]) + dark_bot * t[..., None]
grain = (np.sin(xx / 9.0 + np.sin(yy / 17.0) * 2.0) * 0.5 + 0.5) * 10.0
interior = np.clip(interior + grain[..., None] * (yy > cy)[..., None] * 0.6, 0, 255)
back[..., :3][inside] = interior[inside]
# Contorno del hueco (línea oscura del estilo)
edge = inside & ~np.asarray(Image.fromarray((inside * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(9))).astype(bool)
back[..., :3][edge] = [29, 15, 12]
Image.fromarray(back.astype(np.uint8), "RGBA").save(OUT / "Env_Log_Tunnel_Back.png")

# Frente: corteza superior, bordes y labio inferior; el hueco queda vacío
front = a.copy()
keep = alpha & ~inside
front[..., 3] = np.where(keep, front[..., 3], 0)
# Sombra interior bajo la corteza para dar profundidad
shadow = inside & (yy < cy - ry * 0.55)
front[..., :3][shadow] = [20, 11, 8]
front[..., 3][shadow] = 140
Image.fromarray(front.astype(np.uint8), "RGBA").save(OUT / "Env_Log_Tunnel_Front.png")
print("ok")
