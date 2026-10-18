# Correcciones y animaciones más fluidas

**La idea clave:** una imagen por animación, con **12 fotogramas** en celdas grandes. A 12 fps eso da un bucle fluido de 1 segundo. Meter más fotogramas en una misma imagen los hace pequeños y la IA pierde consistencia. En el juego yo añado el suavizado restante (rebote, escala, mezcla entre animaciones).

**Cómo usarlo:**
- En cada imagen pega primero el **bloque de estilo** y después el prompt.
- Adjunta **siempre** `ArtSource/Witch/witch_parts_chatgpt.png` (estilo) **más la referencia indicada** en cada caso. Todas las referencias están en `ArtSource/Refs/`.
- Pide **una imagen por conversación nueva** o recuérdale "only this one character". Si mezcla personajes, regenera.

### Bloque de estilo (siempre primero)
```
Match EXACTLY the art style of the attached reference images: polished 2D cartoon illustration for a side-scrolling stealth game set in 1692 Salem, warm autumn night mood. Dark outline #1D0F0C about 4 px thick, flat fills with exactly one soft shadow tone and one highlight tone per material, light from the TOP-LEFT. No gradients, no glow, no transparency, no motion blur, no text, no numbers, no grid lines, no frame borders. Perfectly flat solid magenta #FF00FF background, and no pink or magenta tones anywhere in the artwork.
```

---

## 1. Aldeanos: 4 imágenes por personaje

Adjunta: estilo + `ArtSource/Refs/ref_<personaje>.png`.
Orden: **granjero → lechera → guardia** (reemplazan a los del nivel) y luego anciana → niño → predicador.

### Plantilla de animación (imágenes 1, 2 y 3)
```
Animation sprite sheet of ONLY the character in the attached character reference (<NAME>). Draw nobody else. Identical face, hair, clothes, colors, prop and proportions in every frame. Image 1536x1024, grid of 6 columns x 2 rows = 12 frames, read left to right then the second row. Every frame: same character size, side view facing RIGHT, centered in its cell, feet on the same invisible ground line near the bottom of the cell. Leave an empty margin around the character: long props must fit completely inside the cell without touching the neighbouring frames; nothing cropped. The 12 frames are consecutive in-between drawings of ONE continuous motion that loops smoothly from frame 12 back to frame 1, each frame only a small change from the previous one, with squash and stretch, follow-through on clothes and hat, and the <PROP> kept in the same hand.
Animation: <ANIMATION>
```
Pon en `<ANIMATION>` cada una de estas (una imagen por cada una):
| Imagen | `<ANIMATION>` | Archivo |
|---|---|---|
| 1 | `IDLE loop with this exact frame breakdown: frames 1-3 breathe in (chest and shoulders rise slightly), frames 4-6 breathe out, frames 7-10 the gesture (<GESTURE>) spread over 4 frames (7 hand starts rising, 8 reaches, 9 holds, 10 lowers), frame 11 a single blink with eyes closed, frame 12 almost identical to frame 1 so the loop closes. Every frame must differ slightly from the previous one; never repeat the same drawing.` | `<nombre>_idle.png` |
| 2 | `WALK CYCLE: two full steps (left and right leg), contact, down, passing and up positions, arms and prop swinging, gentle body bob.` | `<nombre>_walk.png` |
| 3 | `RUN / CHASE CYCLE: angry frantic run toward the witch, two full strides, leaning forward, prop raised, mouth shouting, both feet off the ground in the flight frames.` | `<nombre>_run.png` |

### Imagen 4: reacciones (8 fotogramas por fila)
```
Sprite sheet of ONLY the character in the attached character reference (<NAME>). Draw nobody else, identical design in every frame. Image 1536x1024, grid of 8 columns x 3 rows = 24 frames, same size and ground line in every frame, side view facing RIGHT, small changes between consecutive frames.
Row 1 NOTICE + SCREAM: 1-2 freeze in surprise with a small hop, 3-8 point forward and scream for the guards.
Row 2 SPELLS: 1-3 BLINDED rubbing eyes and stumbling, 4-6 HYPNOTIZED swaying with spiral eyes, 7-8 ASLEEP standing, head drooped.
Row 3 PERSONAL ACTION loop: <ACTION>.
The <PROP> stays with the character in every frame (it can only be set down during the personal action).
```
Archivo: `<nombre>_react.png`

### Datos de cada personaje
| nombre | Referencia | `<NAME>` | `<PROP>` | `<GESTURE>` | `<ACTION>` |
|---|---|---|---|---|---|
| farmer | `ref_farmer.png` | the stocky farmer | pitchfork | scratching his beard | lifting a hay bale onto his shoulder and carrying it |
| milkmaid | `ref_milkmaid.png` | the young milkmaid | wooden bucket | fixing her bonnet | setting the bucket down and wiping her brow |
| guard | `ref_guard.png` | the town guard | ONE lantern always in his left hand at hip height and the halberd always in his right hand (never more than one lantern, never swap hands, no glow around the lantern) | a wide yawn with the mouth and eyes only, hands do not move | leaning slightly on the halberd and turning his head left and right suspiciously, the lantern stays low in his left hand |
| oldwoman | `ref_oldwoman.png` | the old woman | walking cane | rubbing her back | scattering grain for invisible chickens |
| boy | `ref_boy.png` | the mischievous boy | slingshot | bouncing on his heels | kicking a pebble and laughing |
| preacher | `ref_preacher.png` | the tall preacher | bible | adjusting his collar | reading the bible and raising a finger while preaching |

Guarda todo en `ArtSource/Villagers/`.

---

### Correcciones del guardia
Conservo su **caminata** y las filas 1–2 de **reacciones**. Rehaz estas tres hojas con la plantilla y los datos de la tabla (ya incluyen la regla de un solo farol):
- `guard_idle.png`: idle con el desglose fotograma a fotograma.
- `guard_run.png`: añade al final de la plantilla `Frames 1-6 the LEFT leg strides forward and back, frames 7-12 the RIGHT leg, so the legs clearly alternate. Leave wide empty space between frames: the halberd must not overlap the neighbouring frames.`
- `guard_action.png`: solo la acción propia, con la plantilla de animación (12 fotogramas) y `<ANIMATION>` = la acción de la tabla.

Pide siempre **1536x1024**. Las últimas imágenes llegaron a 1024x687, más pequeñas.

## 2. Gato: animaciones más fluidas (opcional)

Adjunta: estilo + `ArtSource/Refs/ref_cat.png`.
```
Animation sprite sheet of ONLY the black cat in the attached reference, identical design in every frame. Image 1536x1024, grid of 4 columns x 3 rows = 12 frames, read left to right, top to bottom. Same cat size in every frame, side view facing RIGHT, centered in its cell, paws on the same invisible ground line. 12 consecutive in-between drawings of ONE continuous motion that loops smoothly, small change between frames, overlapping action on tail and ears.
Animation: <ANIMATION>
```
- `IDLE: breathing, slow blink in frames 6-7, one ear twitch, tail swaying in a soft S-curve.` → `ArtSource/Cat/sheets/cat_idle.png`
- `WALK CYCLE: diagonal gait (two full steps of each pair of legs), gentle bob, tail up and swaying.` → `cat_walk.png`
- `SNEAK: body low to the ground, ears flat, half-closed eyes, slow careful steps, only the tail tip flicking.` → `cat_sneak.png`

---

## 3. Salpicaduras de poción (8 fotogramas)

Adjunta: estilo + `ArtSource/Items/splashes.png` (la versión anterior, como referencia de diseño).
```
Redraw the attached potion splash effects as smoother animations. Image 1536x1024, grid of 8 columns x 3 rows, one row per potion: row 1 milky white, row 2 purple, row 3 night blue with tiny stars. Each row is 8 consecutive frames of the bottle shattering on impact: glass cracks, burst of liquid and shards outward, splash peaks, drops fly and fall, drops get smaller and fewer, last frame only 3-4 tiny drops. Draw the drops as SOLID opaque shapes that shrink, NOT transparent smoke or mist. Same frame size, centered on the same impact point.
```
Archivo: `ArtSource/Items/splashes.png` (reemplaza a la anterior).

---

## 4. Corazones (8 fotogramas)

Adjunta: estilo + `ArtSource/Items/hearts.png`.
```
Redraw the attached stitched cloth heart animations with more in-between frames. Image 1536x1024, grid of 8 columns x 2 rows, same heart size and center in every frame, solid opaque pieces only (no glow, no transparency).
Row 1 BREAK (8 frames): small crack, crack grows, seam tears, stuffing pops out, heart splits in two, pieces fly apart, pieces fall, only a few small pieces left.
Row 2 HEAL (8 frames): pieces fly back together, golden thread starts sewing, stitches close the tear, last stitch pulled tight, heart squashes, heart stretches, settles to normal, small solid gold sparkle shapes around it.
```
Archivo: `ArtSource/Items/hearts_anim.png`. Los íconos fijos de la versión anterior ya sirven.

---

## 5. Checkpoint (12 fotogramas)

Adjunta: estilo + `ArtSource/Refs/ref_checkpoint.png`.
```
Redraw the attached lantern checkpoint post as a smoother animation. Image 1536x1024, grid of 6 columns x 2 rows = 12 frames, identical post and lantern size and position in every frame, solid opaque glass (dark, not see-through), NO glow halo or light disc around it (light is added in the game).
Row 1 LIGHTING UP (6 frames): unlit, a spark, tiny flame, flame grows, flame flares up, fully lit.
Row 2 LIT LOOP (6 frames): the flame flickering and swaying gently, loops back smoothly.
```
Archivo: `ArtSource/Environment/checkpoint.png`.

---

## 6. Túnel de tronco

Adjunta: estilo + `ArtSource/Refs/ref_log.png`.
```
Draw ONE hollow fallen log tunnel matching the attached log, side view, as TWO aligned layers of the SAME log, drawn one above the other with space between them, both exactly the same width and height:
Layer 1 (BACK): the whole log with the dark hollow interior visible along its full length, as if the near side of the bark was cut away.
Layer 2 (FRONT): only the near bark shell of the same log: the upper bark arch and the two round rims at the ends, EMPTY inside (background visible through it), so a small cat walking between the two layers looks like it is inside the log.
Moss and autumn leaves on top.
```
Archivo: `ArtSource/Environment/log_tunnel.png`.

---

## 7. Fondos de bosque y aldea (fondo verde y sin neblina)

Adjunta: `ArtSource/Environment/bg_sky.png` (para que combinen colores con el cielo).
```
Wide background layer for a 2D side-scroller parallax, 1536x1024, horizontally SEAMLESS (left and right edges match perfectly). Colors that match the attached night sky. Every shape FULLY OPAQUE with flat solid colors: no fog, no haze, no glow, no transparency. Show depth with 2-3 flat color steps (farther = lighter blue-violet, nearer = darker navy). Background behind the shapes: perfectly flat solid pure green #00FF00, and no green anywhere in the artwork.
```
- Bosque: `+ silhouettes of pine trees and twisted bare oaks, the bottom 20% a continuous solid dark band.` → `ArtSource/Environment/bg_trees.png`
- Aldea: `+ distant 1692 Salem: wooden saltbox houses with small warm lit windows, a meeting house with a steeple, fences, chimney smoke drawn as solid flat shapes, the bottom 20% a continuous solid dark band of hills.` → `ArtSource/Environment/bg_village.png`

---

## No hay que rehacerlos (los corrijo yo)

- **Maizal:** le quito el tinte rosa de las hojas.
- **Pociones:** cambio la técnica de recorte para que el morado no se borre.
- **Cielo:** lo hago repetible sin costura.
- **Checkpoint y letreros:** quito el halo y pongo la luz desde el juego (si no rehaces el checkpoint).
- **Suelo, rocas, troncos, escoba, letreros, meta e íconos de estado:** sirven tal cual.
