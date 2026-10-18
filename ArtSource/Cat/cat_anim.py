"""
Animaciones procedurales del gato para previsualizar (Sprint 2). El gato mira a la derecha.

Convenciones: rot en grados, positivo = antihorario en pantalla.
- Patas (cuelgan): positivo = hacia delante.
- Cola (sale hacia la izquierda): negativo = hacia arriba.
- Cabeza: positivo = levanta el hocico.  Orejas: positivo = hacia atrás.
"""
import math
import random

TAU = math.tau

# Curva de reposo de la cola (signo de interrogación)
TAIL_REST = {"tail_base": -38.0, "tail_mid": -26.0, "tail_tip": 28.0}


def ease(x):
    x = min(max(x, 0.0), 1.0)
    return 0.5 - 0.5 * math.cos(math.pi * x)


def wave(t, period, phase=0.0):
    return math.sin(TAU * t / period + phase)


class State:
    def __init__(self, seed=11):
        self.rng = random.Random(seed)
        self.next_blink = 1.2
        self.blink_start = -1.0
        self.next_twitch = 0.7
        self.twitch_start = -1.0
        self.twitch_ear = "ear_front"


def blink(t, s):
    if s.blink_start < 0 and t >= s.next_blink:
        s.blink_start = t
    if s.blink_start >= 0:
        k = (t - s.blink_start) / 0.2  # los gatos parpadean lento
        if k >= 1:
            s.blink_start = -1
            s.next_blink = t + s.rng.uniform(2.5, 6.0)
            return "eye_open"
        return "eye_half" if k < 0.25 or k > 0.75 else "eye_closed"
    return "eye_open"


def ear_twitch(t, s):
    """Sacudida rápida de una oreja a intervalos aleatorios: devuelve (oreja, ángulo)."""
    if s.twitch_start < 0 and t >= s.next_twitch:
        s.twitch_start = t
        s.twitch_ear = "ear_front" if s.rng.random() < 0.6 else "ear_back"
    if s.twitch_start >= 0:
        k = (t - s.twitch_start) / 0.22
        if k >= 1:
            s.twitch_start = -1
            s.next_twitch = t + s.rng.uniform(1.5, 4.0)
            return s.twitch_ear, 0.0
        return s.twitch_ear, 16.0 * math.sin(k * math.pi) * (1 - k * 0.4)
    return s.twitch_ear, 0.0


def add(pose, bone, **kw):
    p = pose.setdefault(bone, {})
    for k, v in kw.items():
        if k in ("sx", "sy"):
            p[k] = p.get(k, 1.0) * v
        elif k == "sprite":
            p[k] = v
        else:
            p[k] = p.get(k, 0.0) + v


def tail(pose, t, amp=1.0, period=3.0, base=None):
    base = base or TAIL_REST
    # Onda que viaja de la base a la punta (overlapping action)
    add(pose, "tail_base", rot=base["tail_base"] + 7 * amp * wave(t, period))
    add(pose, "tail_mid", rot=base["tail_mid"] + 11 * amp * wave(t, period, -0.9))
    add(pose, "tail_tip", rot=base["tail_tip"] + 16 * amp * wave(t, period, -1.8))


def face(pose, t, s):
    add(pose, "eye", sprite=blink(t, s))
    ear, ang = ear_twitch(t, s)
    add(pose, ear, rot=ang)
    add(pose, "whiskers", rot=2.5 * wave(t, 2.2, -0.5))


def idle(t, s, dt):
    pose = {"_ground": True}
    b = wave(t, 2.2)
    add(pose, "body", sy=1 + 0.018 * b, sx=1 - 0.008 * b)
    add(pose, "neck", dy=-3 * b)
    add(pose, "head", rot=3 * wave(t, 4.6, 0.4) + 2 * wave(t, 9.1))
    tail(pose, t)
    face(pose, t, s)
    return pose


def gait(t, s, cycle, crouch=0.0, amp=22.0):
    pose = {"_ground": True}
    p = TAU * t / cycle
    # Marcha diagonal: delantera cercana con trasera lejana, y viceversa
    legs = {
        "fleg_upper_front": (0.0, "fleg_lower_front"),
        "hleg_upper_back": (0.0, "hleg_lower_back"),
        "fleg_upper_back": (math.pi, "fleg_lower_back"),
        "hleg_upper_front": (math.pi, "hleg_lower_front"),
    }
    for upper, (ph, lower) in legs.items():
        add(pose, upper, rot=amp * math.sin(p + ph) + 32 * crouch)
        # La pata se dobla al levantarse (fase de balanceo hacia delante)
        lift = ease(max(0.0, math.cos(p + ph)))
        add(pose, lower, rot=-38 * lift - 58 * crouch)
    bob = math.cos(2 * p)
    add(pose, "body", rot=1.5 * math.sin(2 * p + 0.5), sy=1 - 0.015 * max(0, -bob))
    add(pose, "head", rot=-1.5 * math.sin(2 * p + 1.2) - 9 * crouch)
    add(pose, "neck", dy=2 * bob + 26 * crouch, dx=14 * crouch)
    return pose


def walk(t, s, dt):
    pose = gait(t, s, cycle=0.75)
    tail(pose, t, amp=0.8, period=0.75)
    face(pose, t, s)
    return pose


def sneak(t, s, dt):
    pose = gait(t, s, cycle=1.3, crouch=1.0, amp=16.0)
    low = {"tail_base": 8.0, "tail_mid": 4.0, "tail_tip": -16.0}  # cola baja, punta que se agita
    tail(pose, t, amp=0.5, period=1.3, base=low)
    add(pose, "tail_tip", rot=8 * wave(t, 0.45))
    add(pose, "ear_front", rot=24)
    add(pose, "ear_back", rot=24)
    add(pose, "eye", sprite="eye_half")
    return pose


def sit(t, s, dt):
    pose = {"_ground": True}
    k = ease(min(t / 0.6, 1.0))  # transición a sentado
    b = wave(t, 2.6)
    # El cuerpo se inclina hacia atrás hasta apoyar la grupa; las patas delanteras quedan verticales
    add(pose, "body", rot=40 * k, sy=1 + 0.015 * b)
    add(pose, "hleg_upper_front", rot=82 * k)
    add(pose, "hleg_lower_front", rot=-128 * k)
    add(pose, "hleg_upper_back", rot=78 * k)
    add(pose, "hleg_lower_back", rot=-124 * k)
    add(pose, "fleg_upper_front", rot=-40 * k)
    add(pose, "fleg_upper_back", rot=-40 * k)
    add(pose, "head", rot=-30 * k + 3 * wave(t, 4.8))
    # Cola apoyada en el suelo detrás del gato, con la punta curvada hacia arriba
    lay = {"tail_base": 44.0, "tail_mid": -36.0, "tail_tip": -42.0}
    tail(pose, t, amp=0.3, period=3.4, base={key: TAIL_REST[key] * (1 - k) + lay[key] * k for key in lay})
    face(pose, t, s)
    return pose


def stretch(t, s, dt):
    pose = {"_ground": True}
    # Se estira (0-0.9 s), mantiene (0.9-1.6 s) y vuelve (1.6-2.4 s)
    k = ease(t / 0.9) if t < 0.9 else (1.0 if t < 1.6 else 1 - ease((t - 1.6) / 0.8))
    add(pose, "body", rot=-14 * k, sx=1 + 0.06 * k, sy=1 - 0.03 * k)
    add(pose, "fleg_upper_front", rot=48 * k)
    add(pose, "fleg_lower_front", rot=-6 * k)
    add(pose, "fleg_upper_back", rot=42 * k)
    add(pose, "fleg_lower_back", rot=-6 * k)
    add(pose, "hleg_upper_front", rot=-8 * k)
    add(pose, "hleg_upper_back", rot=-8 * k)
    add(pose, "head", rot=20 * k)
    add(pose, "ear_front", rot=18 * k)
    add(pose, "ear_back", rot=18 * k)
    up = {"tail_base": -62.0, "tail_mid": -12.0, "tail_tip": 12.0}
    tail(pose, t, amp=0.4, base={key: TAIL_REST[key] * (1 - k) + up[key] * k for key in up})
    add(pose, "eye", sprite="eye_closed" if k > 0.6 else ("eye_half" if k > 0.2 else blink(t, s)))
    return pose


def react(t, s, dt):
    pose = {"_ground": True}
    # Reconoce a la bruja: respingo, orejas al frente, cola erguida con sacudida en la punta
    k = t % 1.8
    hop = math.sin(min(k / 0.25, 1.0) * math.pi) if k < 0.25 else 0.0
    perk = ease(min(k / 0.2, 1.0)) * (1 - ease(max(0.0, (k - 1.2) / 0.6)))
    add(pose, "root", dy=-40 * hop)
    add(pose, "body", sy=1 + 0.05 * hop - 0.03 * (1 - hop) * perk)
    add(pose, "head", rot=14 * perk)
    add(pose, "ear_front", rot=-12 * perk)
    add(pose, "ear_back", rot=-12 * perk)
    proud = {"tail_base": -78.0, "tail_mid": -14.0, "tail_tip": 30.0}
    tail(pose, t, amp=0.3, period=1.0, base={key: TAIL_REST[key] * (1 - perk) + proud[key] * perk for key in proud})
    add(pose, "tail_tip", rot=14 * perk * wave(t, 0.3))
    add(pose, "eye", sprite="eye_open")
    pose["_ground"] = hop <= 0.0
    return pose


class Clip:
    def __init__(self, length, fn):
        self.length = length
        self.fn = fn

    def sample(self, t, state, dt):
        return self.fn(t, state, dt)


def rest(t, s, dt):
    pose = {"_ground": True}
    tail(pose, 0.0, amp=0.0)
    return pose


CLIPS = {
    "rest": Clip(1.0, rest),
    "idle": Clip(9.2, idle),
    "walk": Clip(1.5, walk),
    "sneak": Clip(2.6, sneak),
    "sit": Clip(4.0, sit),
    "stretch": Clip(2.6, stretch),
    "react": Clip(3.6, react),
}
