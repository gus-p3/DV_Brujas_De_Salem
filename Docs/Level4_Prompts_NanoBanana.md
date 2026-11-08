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

## 11. Cripta en ruinas escalable (Mausoleo C) — pendiente
**Problema:** el Mausoleo C (x 41–47, antes del portón) sigue armado con `DarkForest/env_mon_0`: una silueta negra
de bloques sueltos que no se lee junto al arte nuevo. Es la plataforma de dos niveles que lleva hacia el portón.
**Archivo nuevo:** `Assets/Art/Environment/Level4/Garden/Panteon_Ruined_Crypt.png` · 704×640 px, fondo transparente

```
A ruined gothic crypt / collapsed family chapel seen from the front, built as TWO STEPPED TIERS a character can
climb: a WIDE lower tier spanning the full width of the image whose top is a clean FLAT HORIZONTAL stone ledge at
exactly HALF the object's height, and on top of it a NARROWER upper chamber (about 78% of the width, centered)
whose roof is also a clean FLAT HORIZONTAL slab at the very top. Broken pointed arch window in the upper chamber,
a sealed crypt door with rusty bars in the lower tier, collapsed stones, cracks, moss and dead ivy, a small leaning
cross on one corner of the roof. Same stone and moss palette as the mausoleum reference. Flat ground line at the
bottom edge, no floating debris.
```
Adjunta `Panteon_Mausoleum.png` como referencia de estilo y paleta.

---

# Pisos 2 y 3 de la torre

El builder ya construye los dos pisos con arte de respaldo (piezas de DarkForest, bancas, estantes y rectángulos).
Cada archivo de esta lista sustituye su respaldo solo al existir, igual que los anteriores. Usa las mismas reglas
comunes de arriba (pixel art Dark Forest, vista lateral, fondo transparente, 64 ppu, filtro Point).

## 12. Órgano de tubos del coro
**Dónde:** Piso 2, al fondo del Balcón del Coro. Es el obstáculo grande antes de la escalera al desván.
**Archivo:** `Assets/Art/Environment/Level4/Choir_Pipe_Organ.png` · 768×896 px

```
A huge gothic church pipe organ seen strictly from the front/side view: a carved dark oak case with pointed-arch
gothic tracery, three towers of tall tin and brass organ pipes of different heights (tallest in the middle),
a wooden console at the bottom with two ivory keyboards, stops and a small bench. Dust, cobwebs between pipes,
a few dented pipes, two lit candles on the console casting warm light. The TOP of the case must be a clean FLAT
HORIZONTAL cornice (a character can stand on it). Flat ground line at the bottom edge.
```

## 13. Sillería del coro (escondite)
**Dónde:** Piso 2. Tres bloques de sillas talladas donde la bruja se esconde en forma de gato.
**Archivo:** `Assets/Art/Environment/Level4/Choir_Stalls.png` · 512×288 px

```
A row of carved gothic wooden choir stalls seen from the side: four connected high-backed oak seats with pointed
arch canopies and carved finials, a hymn book left open on one seat, a dark gap UNDER the seats where a cat could
hide. Dark polished oak, worn red velvet cushions. The top of the canopies is a straight horizontal line.
Flat ground line at the bottom edge.
```

## 14. Balaustrada del balcón (tileable)
**Dónde:** Piso 2, a todo lo largo del frente del balcón, en primer plano (deja ver a la bruja detrás).
**Archivo:** `Assets/Art/Environment/Level4/Choir_Balustrade_Tile.png` · 256×128 px, **tileable horizontal**

```
Seamless horizontally tileable stone balustrade for a gothic church balcony seen from the side: a thick flat top
handrail, a row of turned stone balusters with clear GAPS between them (transparent background visible through the
gaps), and a bottom plinth. Grey worn stone with moss traces and cracks. Left and right edges must match perfectly.
```

## 15. Esfera del reloj vista desde dentro (retroiluminada)
**Dónde:** Piso 3, en el muro del último tramo de la torre del reloj. Detrás está la luna.
**Archivo:** `Assets/Art/Environment/Level4/Clock_Face_Backlit.png` · 1024×1024 px

```
The inside of a giant gothic clock tower face seen from behind: a huge round translucent milky glass dial glowing
pale green-white from moonlight behind it, MIRRORED roman numerals (seen from the inside), an iron ring frame with
rivets and radial iron spokes, a central brass hub with an empty square axle hole. DO NOT draw the clock hands
(they are separate sprites). Transparent background outside the circle.
```

## 16. Manecillas del reloj (hoja de 2)
**Archivo:** `Assets/Art/Environment/Level4/Clock_Hands_Sheet.png` · 256×512 px, 2 celdas de 128×512 (hora y minutos)

```
Sprite sheet with 2 cells side by side, each 128x512: LEFT cell the HOUR hand, RIGHT cell the MINUTE hand of a
gothic clock, wrought iron with ornate fleur-de-lis tips, each hand pointing straight UP with its pivot ring centered
at the BOTTOM of its cell. Dark iron with subtle brass highlights. Hour hand shorter and thicker.
```

## 17. Péndulo del reloj (peligro)
**Dónde:** Piso 3. Oscila de lado a lado; si el contrapeso toca a la bruja, le quita un corazón.
**Archivo:** `Assets/Art/Environment/Level4/Clock_Pendulum.png` · 256×1024 px

```
A giant clock pendulum hanging straight down: a long thin dark iron rod with a ring at the very TOP (pivot), ending
in a heavy round brass bob shaped like a disc with a sharpened crescent BLADE edge at the bottom, engraved with a
gothic sun pattern, scratches and rust. Perfectly vertical and centered horizontally.
```

---

# Piso 4: el campanario
El campanario ya es jugable con arte existente: altar `Church_Altar_Pulpit_AI`, cáliz `Preacher_Chalice_AI`, campana
`Salem_Great_Bell_AI`, ventana `Gothic_Window_Empty_BrokenBars` y el Padre Parris con los cuadros del predicador.
Faltan estas 4 piezas para los momentos clave. Mientras no existan, el builder usa respaldos: el cáliz no cambia al
envenenarlo, el Padre se encorva (`sleep_02`) en lugar de caer, la onda de la campana es niebla dorada y la bruja de la
huida se arma con `Witch_Fly` + `Witch_Broom`.

## 18. Cáliz envenenado
**Dónde:** Piso 4. Reemplaza al cáliz del altar al verter la Poción Maestra.
**Archivo:** `Assets/Art/Environment/Level4/Preacher_Chalice_Poisoned.png` · 1024×1024 px, mismo encuadre que `Preacher_Chalice_AI.png`

```
The exact same ornate gothic golden chalice on its small dark wooden stand as the reference image (same size, same
position, same pixel art style, same framing), but now filled to the brim with a bubbling toxic GREEN potion: glowing
emerald liquid, small bubbles popping over the rim, thin wisps of green vapor rising, one drip running down the cup.
A soft green glow replaces the warm golden halo. Transparent background.
```
*(Adjunta `Preacher_Chalice_AI.png` como referencia para que conserve forma y encuadre.)*

## 19. Padre Parris desplomado
**Dónde:** Piso 4. El Padre cae tras beber del cáliz.
**Archivo:** `Assets/Art/Environment/Level4/Father_Parris_Collapsed.png` · **420×320 px** (el mismo lienzo que los cuadros de `Assets/Art/Characters/Villagers/preacher/`)

```
The same puritan preacher character as the reference frames (cel-shaded cartoon style, clean dark outlines, black
long coat, white bands at the collar, tall black wide-brimmed hat, brown hair), now COLLAPSED on the floor seen from
the side: lying on his back, one arm stretched out, eyes closed, his hat fallen off beside his head. Same character
scale as the reference so he fits the same 420x320 canvas, body resting on the BOTTOM edge of the canvas.
Transparent background, no shadow, no floor.
```
*(Adjunta `idle_00.png` y `sleep_02.png` del predicador como referencia.)*

## 20. Bruja en escoba (cinemática de huida)
**Dónde:** Piso 4. Sale volando por la ventana rota antes de los créditos.
**Archivo:** `Assets/Art/Environment/Level4/Witch_Broom_Flight.png` · 512×384 px

```
The same young witch as the reference (cel-shaded cartoon style: brown hood, patched ragged brown cloak, brown hair in
a ponytail, red scarf, bare feet) now RIDING a wooden broom side view, flying to the RIGHT: seated sideways on the
broom handle, cloak and hair streaming back in the wind, a confident smile, the broom's straw bristles at the LEFT
trailing a few purple-gold magic sparkles. Full body, centered. Transparent background.
```
*(Adjunta `Witch_Fly.png` y `Witch_Broom.png` como referencia del personaje y de la escoba.)*

## 21. Onda expansiva de la campana
**Dónde:** Piso 4. Se expande al repicar la Gran Campana y aturde a los guardias.
**Archivo:** `Assets/Art/Environment/Level4/FX_Bell_Shockwave.png` · 512×512 px

```
A single circular sound shockwave ring for a 2D game effect: a thin bright pale-gold ring with a softer outer glow
and faint concentric echo ripples inside, perfectly round and centered, the center fully transparent. No text, no
background, transparent PNG.
```

---

### Revisión del lote recibido (8 nov 2026)
Las 12 imágenes de los puntos 1–10 llegaron con el tamaño pedido, transparencia real y bordes que empatan al repetirse.
El builder ya las usa todas. Detalles menores que no impiden usarlas:
- `Inquisitor_Sheet.png`, fila 3 (escuchar): los 3 cuadros tienen un corte horizontal visible a la altura del cinturón.
  Si se regenera, pedir *"no seams, no cut lines across the body"*.
- `Panteon_Mausoleum.png`: una línea punteada blanca y negra en el borde inferior. Casi no se nota porque queda
  dentro del suelo.
- **Corregido (8 nov, 15:25):** Antigravity regeneró ambas. El Inquisidor ya no tiene cortes (21 cuadros limpios, pies
  alineados) y el mausoleo tiene el borde inferior sólido.

### Lo que no es imagen (para buscar aparte)
- `SFX_Thunder` (trueno lejano y cercano, 2-3 variantes) → campo `thunderSfx` de `StormWeatherController`.
- Bucle de lluvia ambiental (`SFX_RainLoop`).

### Después de generar
Guarda cada PNG con su nombre y ruta exactos y ejecuta **Salem → Build Level 4 Scene** (fuera de Play).
El builder detecta cada archivo por sí solo: lo importa (64 ppu; Point para pixel art, Bilinear para niebla, cielo
e Inquisidor; el Padre desplomado a 128 ppu como los cuadros del predicador), corta las hojas (lápidas 128×256,
Inquisidor 256×384, manecillas 128×512) y lo usa. Lo que falte sigue con el arte actual.
Si un asset nuevo no se ve bien, bórralo y vuelve a construir: regresa el de respaldo.
