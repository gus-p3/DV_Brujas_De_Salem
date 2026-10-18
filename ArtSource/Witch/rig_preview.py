"""
Previsualiza el rig por piezas de un personaje a partir de su JSON (misma configuración que usa Unity).

  python ArtSource/Witch/rig_preview.py ArtSource/Witch/witch_rig.json --out pose.png
  python ArtSource/Witch/rig_preview.py ArtSource/Witch/witch_rig.json --anim idle --out idle.gif

Convenciones del JSON (espacio de ensamblaje en px de la hoja, x a la derecha, y hacia abajo,
origen en el suelo entre los pies):
  pos    posición absoluta del pivote del hueso en la pose de reposo
  pivot  pivote dentro del sprite (px desde la esquina superior izquierda)
  order  orden de dibujo (mayor = delante)
  scale  escala uniforme del hueso (se hereda)
  rot    rotación de reposo en grados (positivo = antihorario en pantalla, como en Unity)
"""
import argparse
import json
import math
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import rig_anim  # noqa: E402


def load_rig(path):
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    base = Path(path).parent / data.get("spriteDir", "parts")
    bones = {b["name"]: b for b in data["bones"]}
    # Todas las imágenes de la carpeta, para permitir cambios de sprite (parpadeo, bocas)
    library = {p.stem: Image.open(p).convert("RGBA") for p in base.glob("*.png")}
    sprites = {b["name"]: b["sprite"] for b in data["bones"] if b.get("sprite")}
    data["_library"] = library
    return data, bones, sprites


def world_transforms(data, bones, pose):
    """Devuelve {hueso: (x, y, rot_deg, scale)} en espacio de pantalla (y hacia abajo)."""
    out = {}
    rest_rot = {}

    def rest(name):
        # Rotación de reposo acumulada: las posiciones del JSON ya están en la pose de reposo
        if name not in rest_rot:
            b = bones[name]
            rest_rot[name] = b.get("rot", 0.0) + (rest(b["parent"]) if b.get("parent") else 0.0)
        return rest_rot[name]

    def solve(name):
        if name in out:
            return out[name]
        b = bones[name]
        p = pose.get(name, {})
        local_rot = b.get("rot", 0.0) + p.get("rot", 0.0)
        local_scale = b.get("scale", 1.0)
        sx, sy = p.get("sx", 1.0), p.get("sy", 1.0)
        dx, dy = p.get("dx", 0.0), p.get("dy", 0.0)
        if b.get("parent") is None:
            res = (b["pos"][0] + dx, b["pos"][1] + dy, local_rot, local_scale, sx, sy)
        else:
            px, py, prot, pscale, psx, psy = solve(b["parent"])
            parent = bones[b["parent"]]
            ox = (b["pos"][0] - parent["pos"][0]) * psx
            oy = (b["pos"][1] - parent["pos"][1]) * psy
            # Solo se aplica la rotación añadida sobre el reposo del padre.
            # La rotación positiva es antihoraria en pantalla (y hacia abajo => ángulo negativo)
            a = math.radians(-(prot - rest(b["parent"])))
            rx = ox * math.cos(a) - oy * math.sin(a)
            ry = ox * math.sin(a) + oy * math.cos(a)
            res = (px + rx + dx, py + ry + dy, prot + local_rot,
                   pscale * local_scale, psx * sx, psy * sy)
        out[name] = res
        return res

    for n in bones:
        solve(n)
    return out


def tip_world(bones, wt, name):
    """Posición en pantalla del punto 'tip' (p. ej. planta del pie) de un hueso."""
    b = bones[name]
    x, y, rot, scale, sx, sy = wt[name]
    ox = (b["tip"][0] - b["pivot"][0]) * scale * sx
    oy = (b["tip"][1] - b["pivot"][1]) * scale * sy
    a = math.radians(-rot)
    return x + ox * math.cos(a) - oy * math.sin(a), y + ox * math.sin(a) + oy * math.cos(a)


def grounded(data, bones, pose):
    """Desplaza la raíz para que el pie más bajo quede apoyado en el suelo (y = 0)."""
    wt = world_transforms(data, bones, pose)
    feet = [tip_world(bones, wt, n) for n, b in bones.items() if "tip" in b]
    if not feet:
        return pose, wt
    lowest = max(f[1] for f in feet)
    pose = dict(pose)
    root = next(n for n, b in bones.items() if b.get("parent") is None)
    r = dict(pose.get(root, {}))
    r["dy"] = r.get("dy", 0.0) - lowest
    pose[root] = r
    return pose, world_transforms(data, bones, pose)


def render(data, bones, sprites, pose, canvas=(900, 1150), origin=(450, 1080), bg=(118, 120, 132, 255),
           hidden=()):
    img = Image.new("RGBA", canvas, bg)
    if pose.get("_ground"):
        pose, wt = grounded(data, bones, pose)
    else:
        wt = world_transforms(data, bones, pose)
    order = sorted(sprites, key=lambda n: bones[n].get("order", 0))
    for name in order:
        if name in hidden:
            continue
        b = bones[name]
        sprite_name = pose.get(name, {}).get("sprite", sprites[name])
        spr = data["_library"][sprite_name]
        pivot = next((e["pivot"] for e in data.get("extraSprites", []) if e["name"] == sprite_name), b["pivot"])
        x, y, rot, scale, sx, sy = wt[name]
        w, h = spr.size
        sw, sh = max(1, round(w * scale * sx)), max(1, round(h * scale * sy))
        piv = (pivot[0] * scale * sx, pivot[1] * scale * sy)
        s = spr.resize((sw, sh), Image.LANCZOS) if (sw, sh) != (w, h) else spr
        big = Image.new("RGBA", (sw * 3, sh * 3), (0, 0, 0, 0))
        big.paste(s, (sw, sh))
        cx, cy = sw + piv[0], sh + piv[1]
        rotated = big.rotate(rot, resample=Image.BICUBIC, center=(cx, cy))
        img.alpha_composite(rotated, (round(origin[0] + x - cx), round(origin[1] + y - cy)))
    return img


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("rig")
    ap.add_argument("--out", required=True)
    ap.add_argument("--anim", help="Nombre de la animación de rig_anim (idle, walk...).")
    ap.add_argument("--fps", type=int, default=30)
    ap.add_argument("--seconds", type=float, default=0)
    ap.add_argument("--scale", type=float, default=0.5, help="Escala de salida del preview.")
    ap.add_argument("--hide", default="", help="Piezas a ocultar, separadas por coma.")
    ap.add_argument("--anim-module", help="Archivo .py con CLIPS y State (por defecto rig_anim.py).")
    args = ap.parse_args()

    anim = rig_anim
    if args.anim_module:
        import importlib.util
        spec = importlib.util.spec_from_file_location("anim_module", args.anim_module)
        anim = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(anim)

    data, bones, sprites = load_rig(args.rig)
    hidden = set(filter(None, args.hide.split(",")))

    def shrink(im):
        return im.resize((round(im.width * args.scale), round(im.height * args.scale)), Image.LANCZOS)

    if not args.anim:
        rest = anim.CLIPS["rest"].sample(0.0, anim.State(), 1 / 30) if "rest" in anim.CLIPS else {}
        shrink(render(data, bones, sprites, rest, hidden=hidden)).save(args.out)
        print(f"Guardado: {args.out}")
        return

    clip = anim.CLIPS[args.anim]
    seconds = args.seconds or clip.length
    n = max(1, round(seconds * args.fps))
    frames = []
    state = anim.State()
    for i in range(n):
        t = i / args.fps
        pose = clip.sample(t, state, 1.0 / args.fps)
        frames.append(shrink(render(data, bones, sprites, pose, hidden=hidden)).convert("P", palette=Image.ADAPTIVE))
    frames[0].save(args.out, save_all=True, append_images=frames[1:], duration=round(1000 / args.fps), loop=0,
                   disposal=2)
    print(f"Guardado: {args.out} ({n} frames)")


if __name__ == "__main__":
    main()
