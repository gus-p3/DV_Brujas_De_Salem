"""
Animaciones procedurales para previsualizar el rig en Python (Sprint 0).
La versión definitiva y ampliada vive en Assets/Scripts/Player/Rig/WitchRigAnimator.cs.

Convenciones: rot en grados (positivo = antihorario en pantalla). Para piernas y brazos que cuelgan,
rot positivo = balanceo hacia delante (el personaje mira a la derecha). dx/dy en px de la hoja
(dy positivo = hacia abajo). sx/sy = squash & stretch.
"""
import math
import random

TAU = math.tau


def ease_in_out(x):
    """Suavizado senoidal 0..1 -> 0..1."""
    x = min(max(x, 0.0), 1.0)
    return 0.5 - 0.5 * math.cos(math.pi * x)


def wave(t, period, phase=0.0):
    return math.sin(TAU * (t / period) + phase)


class State:
    """Estado persistente entre frames (parpadeo aleatorio, resortes)."""

    def __init__(self, seed=7):
        self.rng = random.Random(seed)
        self.next_blink = 0.8
        self.blink_start = -1.0
        self.springs = {}


def blink_sprite(t, state):
    if state.blink_start < 0 and t >= state.next_blink:
        state.blink_start = t
    if state.blink_start >= 0:
        k = (t - state.blink_start) / 0.16
        if k >= 1.0:
            state.blink_start = -1.0
            # A veces doble parpadeo, el resto de veces intervalo aleatorio 2-5 s
            state.next_blink = t + (0.18 if state.rng.random() < 0.2 else state.rng.uniform(2.0, 5.0))
            return "eye_open"
        if k < 0.25 or k > 0.75:
            return "eye_half"
        return "eye_closed"
    return "eye_open"


def spring(state, key, target, dt, freq=3.2, damping=0.35):
    """Resorte críticamente amortiguado para seguimiento secundario (capa, cabello, cola)."""
    x, v = state.springs.get(key, (target, 0.0))
    w = TAU * freq
    a = w * w * (target - x) - 2.0 * damping * w * v
    v += a * dt
    x += v * dt
    state.springs[key] = (x, v)
    return x


def idle(t, state, dt):
    breath = wave(t, 2.6)                          # respiración
    look = wave(t, 5.2, 0.6)                       # inclinación curiosa de la cabeza
    peek = ease_in_out((wave(t, 7.8) + 1) * 0.5)   # asomarse lentamente hacia delante

    head_rot = 2.2 * look - 2.5 * peek
    pose = {
        "hips": {"dy": 1.5 * breath},
        "torso": {"sy": 1.0 + 0.012 * breath, "sx": 1.0 - 0.006 * breath, "rot": -1.2 * peek},
        "neck": {"dy": -2.0 * breath},
        "head": {"rot": head_rot},
        "arm_upper_front": {"rot": 2.0 * wave(t, 2.6, -0.6) + 1.5 * peek},
        "arm_lower_front": {"rot": 3.0 * wave(t, 2.6, -1.1)},
        "arm_upper_back": {"rot": 1.6 * wave(t, 2.6, -0.8)},
        "arm_lower_back": {"rot": 2.5 * wave(t, 2.6, -1.3)},
        "robe_skirt": {"rot": 0.6 * wave(t, 2.6, -0.9)},
        "eye": {"sprite": blink_sprite(t, state)},
    }
    # Secundarios: siguen a la cabeza con retraso (follow-through / overlapping)
    pose["hair_tail"] = {"rot": spring(state, "hair", -head_rot * 1.4, dt) + 2.5 * wave(t, 2.9, -1.2)}
    pose["hood"] = {"rot": spring(state, "hood", -head_rot * 0.25, dt, freq=4.0)}
    pose["hood_inner"] = pose["hood"]
    pose["scarf_tail"] = {"rot": spring(state, "scarf", -head_rot * 0.8, dt, freq=2.6) + 3.0 * wave(t, 3.1, -1.6)}
    return pose


def walk(t, state, dt, cycle=0.8):
    p = TAU * t / cycle
    s, c = math.sin(p), math.cos(p)
    bob = math.cos(2 * p)                     # 2 rebotes por ciclo: alto al pasar, bajo al contacto
    contact = max(0.0, -bob)                  # 0..1 en el contacto

    thigh_f = 24 * s
    thigh_b = -24 * s
    # La rodilla se dobla en el balanceo (pierna que va hacia delante) y casi recta al apoyar
    knee_f = -28 * ease_in_out(max(0.0, c)) - 4
    knee_b = -28 * ease_in_out(max(0.0, -c)) - 4

    pose = {
        "_ground": True,
        "root": {"sy": 1.0 - 0.025 * contact, "sx": 1.0 + 0.018 * contact},
        "torso": {"rot": -4.0 + 1.2 * math.sin(2 * p + 0.6)},
        "neck": {"dy": 1.5 * bob},
        "head": {"rot": 2.0 - 1.5 * math.sin(2 * p + 1.0)},
        "leg_upper_front": {"rot": thigh_f},
        "leg_lower_front": {"rot": knee_f},
        "leg_upper_back": {"rot": thigh_b},
        "leg_lower_back": {"rot": knee_b},
        "arm_upper_front": {"rot": -18 * s},
        "arm_lower_front": {"rot": 12 + 8 * math.sin(p - 0.5)},
        "arm_upper_back": {"rot": 18 * s},
        "arm_lower_back": {"rot": 12 - 8 * math.sin(p - 0.5)},
        "robe_skirt": {"rot": 2.5 * math.sin(2 * p - 0.8) - 1.5},
        "eye": {"sprite": blink_sprite(t, state)},
    }
    # El movimiento hacia delante empuja hacia atrás (negativo) cabello, bufanda y punta de la capucha
    pose["hair_tail"] = {"rot": spring(state, "hair", -10 + 6 * math.sin(2 * p - 1.4), dt, freq=3.5)}
    pose["scarf_tail"] = {"rot": spring(state, "scarf", -16 + 7 * math.sin(2 * p - 1.8), dt, freq=3.0)}
    pose["hood"] = {"rot": spring(state, "hood", -2 + 1.5 * math.sin(2 * p - 1.2), dt, freq=4.0)}
    pose["hood_inner"] = pose["hood"]
    return pose


class Clip:
    def __init__(self, length, fn):
        self.length = length
        self.fn = fn

    def sample(self, t, state, dt):
        return self.fn(t, state, dt)


CLIPS = {
    "rest": Clip(1.0, lambda t, s, dt: {}),
    "idle": Clip(7.8, idle),
    "walk": Clip(1.6, walk),
}
