"""
Generación y post-proceso de arte 2D con Gemini para Brujas de Salem.

La API key se lee SOLO de la variable de entorno GEMINI_API_KEY (nunca se guarda en el repo).
Modelo de imagen configurable con --model o GEMINI_IMAGE_MODEL.

Subcomandos:
  models                      Lista los modelos disponibles para la key (útil para elegir el de imagen).
  generate                    Genera una imagen a partir de un prompt y referencias opcionales.
  key                         Quita el fondo croma (magenta por defecto) y recorta al contenido.
  split                       Separa una hoja de piezas (fondo transparente) en un PNG por pieza.

Ejemplos:
  python gemini_art.py models
  python gemini_art.py generate --prompt-file Docs/Arte/prompts/bruja_piezas.txt \
      --ref Assets/Art/Characters/Witch/Witch_Idle.png --out ArtSource/Witch/witch_parts_raw.png --key-bg
  python gemini_art.py split ArtSource/Witch/witch_parts.png --out-dir ArtSource/Witch/parts
"""

import argparse
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image

DEFAULT_MODEL = os.environ.get("GEMINI_IMAGE_MODEL", "gemini-2.5-flash-image")
CHROMA = (255, 0, 255)


def get_client():
    key = os.environ.get("GEMINI_API_KEY")
    if not key and sys.platform == "win32":
        # Variable creada con setx en esta sesión: aún no está en el entorno heredado, leerla del registro
        import winreg
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as reg:
                key = winreg.QueryValueEx(reg, "GEMINI_API_KEY")[0]
        except OSError:
            key = None
    if not key:
        sys.exit("Falta la variable de entorno GEMINI_API_KEY (configúrala con setx).")
    from google import genai
    return genai.Client(api_key=key)


# ----------------------------------------------------------------------------- models

def cmd_models(_args):
    client = get_client()
    for m in client.models.list():
        actions = getattr(m, "supported_actions", None) or []
        print(f"{m.name}\t{','.join(actions)}")


# ----------------------------------------------------------------------------- generate

def cmd_generate(args):
    from google.genai import types

    prompt = args.prompt or ""
    if args.prompt_file:
        prompt = Path(args.prompt_file).read_text(encoding="utf-8") + ("\n" + prompt if prompt else "")
    if not prompt.strip():
        sys.exit("Indica --prompt o --prompt-file.")

    contents = [prompt]
    for ref in args.ref or []:
        contents.append(Image.open(ref))

    config = types.GenerateContentConfig(
        response_modalities=["IMAGE", "TEXT"],
        image_config=types.ImageConfig(aspect_ratio=args.aspect) if args.aspect else None,
    )

    client = get_client()
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)

    saved = 0
    for i in range(args.count):
        response = client.models.generate_content(model=args.model, contents=contents, config=config)
        for part in response.candidates[0].content.parts:
            if getattr(part, "text", None):
                print(f"[gemini] {part.text.strip()}")
            data = getattr(part, "inline_data", None)
            if data is None or not data.data:
                continue
            target = out if args.count == 1 and saved == 0 else out.with_name(f"{out.stem}_{saved:02d}{out.suffix}")
            target.write_bytes(data.data)
            if args.key_bg:
                img = key_background(Image.open(target), CHROMA, args.tolerance)
                img = trim(img, args.padding)
                img.save(target)
            print(f"Guardado: {target}")
            saved += 1
    if saved == 0:
        sys.exit("El modelo no devolvió imágenes (revisa el prompt o el modelo con 'models').")


# ----------------------------------------------------------------------------- post-proceso

def key_background(img, color=CHROMA, tolerance=90):
    """Convierte el fondo croma en transparencia con borde suave y elimina el halo de color."""
    rgba = np.asarray(img.convert("RGBA")).astype(np.float32)
    rgb = rgba[..., :3]
    dist = np.sqrt(((rgb - np.array(color, dtype=np.float32)) ** 2).sum(axis=-1))
    inner = tolerance * 0.6
    alpha = np.clip((dist - inner) / max(tolerance - inner, 1.0), 0.0, 1.0)
    rgba[..., 3] = np.minimum(rgba[..., 3], alpha * 255.0)

    # Despill: la paleta no usa magenta, así que todo píxel con R y B muy por encima de G es halo.
    # Halo fuerte -> transparente; halo leve -> se neutraliza bajando R y B hacia G.
    g = rgb[..., 1]
    spill = np.minimum(rgb[..., 0], rgb[..., 2]) - g
    rgba[..., 3] = np.where(spill > 110, 0.0, rgba[..., 3] * np.clip(1.0 - (spill - 60) / 50.0, 0.0, 1.0))
    soft = spill > 15
    for c in (0, 2):
        ch = rgb[..., c]
        ch[soft] = np.maximum(g[soft], ch[soft] - (spill[soft] - 15))
    rgba[..., :3] = rgb
    return Image.fromarray(rgba.astype(np.uint8), "RGBA")


def trim(img, padding=4):
    bbox = img.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    if not bbox:
        return img
    l, t, r, b = bbox
    l, t = max(0, l - padding), max(0, t - padding)
    r, b = min(img.width, r + padding), min(img.height, b + padding)
    return img.crop((l, t, r, b))


def cmd_key(args):
    img = key_background(Image.open(args.input), CHROMA, args.tolerance)
    if not args.no_trim:
        img = trim(img, args.padding)
    out = Path(args.out or args.input)
    out.parent.mkdir(parents=True, exist_ok=True)
    img.save(out)
    print(f"Guardado: {out} {img.size}")


def cmd_split(args):
    from scipy import ndimage

    img = Image.open(args.input).convert("RGBA")
    if args.key_bg:
        img = key_background(img, CHROMA, args.tolerance)
    alpha = np.asarray(img)[..., 3] > 8
    # Unir trazos cercanos de la misma pieza (contornos con huecos) antes de etiquetar
    grown = ndimage.binary_dilation(alpha, iterations=args.merge)
    labels, count = ndimage.label(grown)
    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    pieces = []
    for idx, sl in enumerate(ndimage.find_objects(labels), start=1):
        mask = (labels[sl] == idx) & alpha[sl]
        if mask.sum() < args.min_pixels:
            continue
        crop = np.asarray(img)[sl].copy()
        crop[..., 3] = np.where(mask, crop[..., 3], 0)
        pieces.append((sl[1].start, sl[0].start, Image.fromarray(crop, "RGBA")))

    # Orden de lectura: filas de arriba abajo, luego izquierda a derecha
    pieces.sort(key=lambda p: (p[1] // args.row_height, p[0]))
    for n, (_, _, piece) in enumerate(pieces):
        path = out_dir / f"{args.prefix}{n:02d}.png"
        trim(piece, args.padding).save(path)
        print(f"{path} {piece.size}")
    print(f"{len(pieces)} piezas")


# ----------------------------------------------------------------------------- CLI

def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)

    sub.add_parser("models").set_defaults(func=cmd_models)

    g = sub.add_parser("generate")
    g.add_argument("--prompt")
    g.add_argument("--prompt-file")
    g.add_argument("--ref", action="append", help="Imagen de referencia (se puede repetir).")
    g.add_argument("--out", required=True)
    g.add_argument("--model", default=DEFAULT_MODEL)
    g.add_argument("--aspect", help="Relación de aspecto, p. ej. 1:1, 16:9.")
    g.add_argument("--count", type=int, default=1)
    g.add_argument("--key-bg", action="store_true", help="Quitar fondo magenta y recortar.")
    g.add_argument("--tolerance", type=float, default=90)
    g.add_argument("--padding", type=int, default=4)
    g.set_defaults(func=cmd_generate)

    k = sub.add_parser("key")
    k.add_argument("input")
    k.add_argument("--out")
    k.add_argument("--tolerance", type=float, default=90)
    k.add_argument("--padding", type=int, default=4)
    k.add_argument("--no-trim", action="store_true")
    k.set_defaults(func=cmd_key)

    s = sub.add_parser("split")
    s.add_argument("input")
    s.add_argument("--out-dir", required=True)
    s.add_argument("--prefix", default="part_")
    s.add_argument("--key-bg", action="store_true")
    s.add_argument("--tolerance", type=float, default=90)
    s.add_argument("--merge", type=int, default=3, help="Dilatación (px) para unir trazos de una misma pieza.")
    s.add_argument("--min-pixels", type=int, default=150)
    s.add_argument("--row-height", type=int, default=200)
    s.add_argument("--padding", type=int, default=4)
    s.set_defaults(func=cmd_split)

    args = p.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
