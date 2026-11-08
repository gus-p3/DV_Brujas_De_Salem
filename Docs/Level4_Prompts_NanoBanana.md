# Nivel 4: assets a regenerar con Antigravity + Nano Banana

Son los sprites que hoy no encajan en el Patio/Panteón y la Planta Baja de la iglesia, o que se tuvieron que sustituir
por arte provisional en `Assets/Editor/Level4/Level4SceneBuilder.cs`.

## Reglas comunes (pégalas al inicio de cada prompt)

```
2D side-scrolling platformer game asset, strict orthographic SIDE VIEW (no perspective, no 3/4 view),
dark gothic pixel art matching the "Dark Forest" tileset by Szadi art: muted desaturated browns, mossy greens,
1px dark outlines, limited palette, hand-placed pixels, no anti-aliasing, no gradients smoother than 4 tones.
Night scene lighting baked softly from the upper left. TRANSPARENT BACKGROUND (real alpha PNG, no checkerboard,
no white box, no drop shadow, no text, no watermark, no frame). Centered single object with 16px empty margin.
```

Al importar en Unity: `Sprite Mode: Single`, `Pixels Per Unit: 64`, `Filter Mode: Point`, `Compression: None`,
pivote abajo al centro (Bottom). Guarda cada archivo con el nombre exacto indicado: el builder lo carga por ruta.

---

## 1. Ventana gótica con barrotes rotos (urgente)
**Problema:** `Garden/Gothic_Cat_Entry_Window.png` tiene una mancha negra en el centro (quedó el hueco del gato borrado).
**Archivo:** `Assets/Art/Environment/Level4/Garden/Gothic_Cat_Entry_Window.png` (reemplazar) · 512×832 px

```
Tall gothic pointed-arch church window set in a weathered grey stone frame with keystones and a stone sill.
Stained glass glowing warm amber and violet from candlelight inside (rose/quatrefoil tracery at the top).
Rusty black iron bars cover the window; in the LOWER CENTER two bars are bent apart and one is snapped,
leaving a small dark gap only big enough for a cat to squeeze through. The gap shows the warm glow behind.
No characters, no animals, no silhouettes inside the window. Clean and readable at small size.
```

## 2. Portón monumental de hierro forjado (sellado)
**Problema:** `Garden/Panteon_Iron_Gate.png` mide 83×104 px y parece una puerta pequeña, no un portón de panteón.
**Archivo:** `Assets/Art/Environment/Level4/Garden/Panteon_Iron_Gate.png` (reemplazar) · 512×640 px

```
Monumental cemetery gate: two tall wrought-iron gate leaves with spear-tipped bars and ornate scrollwork,
closed and sealed with a heavy rusted chain and a big padlock in the middle, between two thick mossy stone pillars
topped with small stone urns. A pointed iron arch above with a faded cross. Rust streaks, moss, a few dead vines.
Full object from the ground line up, ground line flat at the bottom edge.
```

## 3. Lápidas góticas (hoja de 6 variantes)
**Problema:** `Garden/Panteon_Gothic_Tombstone_Arch.png` en realidad contiene ventanitas recortadas, no lápidas. Hoy se usan las lápidas de Cainos.
**Archivo nuevo:** `Assets/Art/Environment/Level4/Garden/Panteon_Tombstones_Sheet.png` · 768×256 px, 6 lápidas en fila con 32 px de separación (Sprite Mode Multiple, cortar por celdas de 128×256)

```
Sprite sheet of 6 different gothic tombstones in a single row, evenly spaced: pointed-arch headstone,
celtic cross, broken cracked slab leaning, small obelisk, rounded headstone with skull relief,
weeping angel statue on a pedestal. Old grey stone, moss, cracks, no readable text (illegible carved lines only).
Each tombstone stands on the same flat ground line.
```

## 4. Mausoleo / cripta escalable
**Problema:** `Garden/Panteon_Mausoleum_Crypt_Wall.png` son fragmentos de ladrillo sueltos. Hoy se arma con piezas de `castle_env`/`env_mon`.
**Archivo nuevo:** `Assets/Art/Environment/Level4/Garden/Panteon_Mausoleum.png` · 640×448 px

```
Small gothic family mausoleum seen from the front: dark stone walls, two short columns, a flat stone roof slab
that a character could stand on (roof edge clearly flat and horizontal), a barred iron crypt door in the middle,
a faded family crest above the door, moss on the roof, cracks, dead ivy. Flat ground line at the bottom.
```

## 5. Alfombra de terciopelo rojo (tileable)
**Problema:** hoy es un rectángulo rojo con una línea dorada (atajo en `Carpet()`).
**Archivo nuevo:** `Assets/Art/Environment/Level4/Carpet_Velvet_Runner.png` · 256×32 px, **tileable horizontal**

```
Seamless horizontally tileable strip of a worn crimson velvet church carpet runner seen from the side as a thin
floor layer: deep red fabric with a gold embroidered border line on top, slight folds, dust, frayed spots.
Left and right edges must match perfectly for tiling. Very thin (fits on top of wooden floor planks).
```

## 6. Duela de madera floja / crujiente
**Problema:** hoy es la duela normal aclarada e inclinada (atajo en `CreakyTile()`).
**Archivo nuevo:** `Assets/Art/Environment/Level4/Creaky_Floorboard.png` · 96×64 px

```
A single loose old wooden floorboard section matching dark oak church floor planks: one plank warped and lifted
at one end, cracked, with a popped rusty nail and splinters, lighter worn wood so the player can spot it.
Side view, same height as the regular floor plank tile.
```
(Opcional, para animación: 3 frames en hoja de 288×64 → reposo, hundida, rebote.)

## 7. Inquisidor: hoja de animación
**Problema:** `Characters/Inquisitor/Inquisitor_Boss_Stylized.png` es una sola pose: no camina, no escucha, no embiste.
**Archivo nuevo:** `Assets/Art/Characters/Inquisitor/Inquisitor_Sheet.png` · celdas de 256×384 px, 6 columnas × 5 filas (1536×1920)
Estilo: **el mismo cel-shaded vectorial del sprite actual y de los guardias** (no pixel art), mirando a la DERECHA.

```
Character sprite sheet, same character in every cell, facing RIGHT, full body, consistent size and proportions,
feet on the same baseline in every cell. A heavy armored Salem Inquisitor: dark steel armor, open kettle helmet,
black beard, dark tabard with a pale cross, black cape, holding a lit iron lantern in the left hand and a
spiked mace in the right. 2D cel-shaded cartoon style with thick dark outlines (same style as the reference image).
Row 1: idle breathing, 2 frames (rest of row empty).
Row 2: slow heavy patrol walk cycle, 6 frames.
Row 3: alert/listening, 3 frames: stops, turns head, raises lantern (exclamation is added in-engine, do not draw it).
Row 4: furious charge run, 6 frames, leaning forward, cape flowing, mace raised.
Row 5: stunned/dazed, 4 frames: staggering, small swirl of stars around the helmet.
```
Adjunta `Inquisitor_Boss_Stylized.png` como imagen de referencia.

## 8. Niebla en franjas (tileable)
**Problema:** la niebla usa un círculo difuso escalado (`FX_Level3_SoftFog.png`); funciona, pero se ve en manchas.
**Archivo nuevo:** `Assets/Art/Environment/Level4/FX_GroundFog_Strip.png` · 1024×192 px, tileable horizontal

```
Seamless horizontally tileable strip of low ground fog / mist wisps, soft white-grey with transparent alpha
that fades out at the top and bottom, irregular wispy curls, no hard edges. Pure white on transparent
(it will be tinted emerald in-engine).
```

## 9. Cielo de tormenta en capas de paralaje
**Problema:** `Garden/Stormy_Cemetery_GreenNight_BG.png` tiene poco detalle (siluetas solo con contorno).
**Archivos nuevos:** `Assets/Art/Environment/Level4/Garden/Storm_Parallax_Sky.png` (1920×1080, opaco) +
`Storm_Parallax_Far.png` y `Storm_Parallax_Near.png` (1920×540, fondo transparente, tileables horizontalmente)

```
Layered parallax background for a gothic cemetery at night during a thunderstorm, emerald green moonlight.
SKY layer (opaque): heavy swirling storm clouds, dark green-teal palette, a pale spectral full moon partly
covered by clouds, faint rain streaks. FAR layer (transparent): distant hills crowded with mausoleums,
crosses and a church spire silhouette, very dark green. NEAR layer (transparent): row of twisted dead trees and
iron cemetery fences as a dark silhouette. Far and near layers must tile seamlessly left-right.
```

## 10. Suelo marchito propio (opcional)
Ahora se usa el set seco de `DarkForest/env_ground.png` (franja `env_ground_32` + tierra `env_ground_40`).
Si quieres uno exclusivo del panteón:
**Archivo nuevo:** `Assets/Art/Environment/Level4/Garden/Withered_Ground_Tile.png` · 256×128 px, tileable horizontal

```
Seamless horizontally tileable ground tile for a haunted cemetery: top edge of dead, dry, yellow-brown withered
grass and thorny weeds, below it cracked dark earth with small bones, pebbles and dead roots, fading to near
black at the bottom. The walkable top surface must be a clean flat line 16px below the top of the image.
```

---

### Lo que no es imagen (para buscar aparte)
- `SFX_Thunder` (trueno lejano y cercano, 2-3 variantes) → campo `thunderSfx` de `StormWeatherController`.
- Bucle de lluvia ambiental (`SFX_RainLoop`).

### Después de generar
1, 2 y 9 reemplazan archivos existentes o se cambian con una ruta en el builder. 3–8 y 10 necesitan una pequeña
conexión en `Level4SceneBuilder.cs` (sustituir los atajos marcados con `shortcut:` y el Inquisidor de una sola pose).
Luego: menú **Salem → Build Level 4 Scene**.
