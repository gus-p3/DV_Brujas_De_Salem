# Prompts de arte — Nivel 1 (aldeanos, escenario y objetos)

Objetivo: que todo el juego comparta el estilo de la bruja y del gato aprobados.

## Cómo usarlos

1. En cada imagen **adjunta las dos referencias de estilo**:
   - `ArtSource/Witch/witch_parts_chatgpt.png` (estilo de la bruja)
   - `ArtSource/Cat/sheets/cat_all_chatgpt.png` (gato aprobado: tamaño de línea y acabado)
2. Pega el **bloque de estilo común** y luego el prompt del asset.
3. Tamaños de ChatGPT: 1536×1024 (horizontal), 1024×1536 (vertical) o 1024×1024. Yo recorto, escalo y ajusto cada pieza al tamaño exacto que usa el juego, así que **no hace falta que salgan al píxel**.
4. Pásame cada imagen aquí con su nombre de archivo. Yo la guardo en `ArtSource/...` y la integro.
5. Orden recomendado: **Lote 1 (aldeanos)** → 2 (suelo y rocas) → 3 (maizal) → 4 (objetos) → 5 (letreros y checkpoints) → 6 (fondos).
6. Si una imagen sale mal, pide solo esa otra vez y no sigas hasta tener la buena: el resto depende de que el estilo sea consistente.

### Bloque de estilo común (pégalo siempre primero)

```
Match EXACTLY the art style of the attached reference images (the witch parts sheet and the black cat sprite sheet): polished 2D cartoon illustration for a side-scrolling stealth game set in 1692 Salem, warm autumn night mood. Dark outline #1D0F0C about 4 px thick, flat fills with exactly one soft shadow tone and one highlight tone per material, light coming from the TOP-LEFT (shadows bottom-right, highlights on top-left edges). Slightly chunky, appealing, readable shapes; no realism, no gradients, no painterly noise, no glow outlines or stickers effects. Muted autumn palette: browns #4A2A20 #6E452D #905C37, rust #A8452B, ochre #C99A2E, moss #5E6B42, slate #4A5B70, puritan blacks #1E1C22 #34313A, linen #E6E0D2. Perfectly flat solid magenta #FF00FF background unless stated otherwise, no text, no labels, no numbers, no frame borders.
```

---

## Lote 1 — Aldeanos y guardia (sprites cuadro a cuadro, como el gato)

Los aldeanos actuales son figuras planas. La IA del juego tiene estos estados, y cada hoja los cubre:
- **Paseo:** idle y caminar.
- **Aviso:** "¡!" al ver a la bruja.
- **Grito:** alerta.
- **Frenesí:** corren tras la bruja.
- **Hechizos:** cegado, hipnotizado o dormido.

Además, cada aldeano tiene una **acción propia** para que no todos se muevan igual.

### 1.0 Hoja de modelo (primero, una sola imagen 1536×1024)

```
Draw a character lineup (model sheet) of 6 inhabitants of 1692 Salem, full body, side view facing RIGHT, all standing on the same ground line, same scale (the witch girl from the reference is 100% height; adults are 110-125% of her height, the child 85%). Each must be recognizable by silhouette alone:
1. Stocky farmer: broad belly, rolled sleeves, straw hat, carries a pitchfork.
2. Old woman: hunched back, white bonnet and shawl, walking cane.
3. Young milkmaid: apron, rolled-up skirt, carries a wooden bucket.
4. Mischievous boy: short, oversized cap, suspenders, holds a slingshot.
5. Tall thin preacher: tall black capotain hat, white collar, long black coat, holds a bible.
6. Town guard: buff coat, helmet or wide hat, carries a lantern and a halberd.
Spread them evenly with generous space between them, nothing overlapping.
```
Guárdala como `ArtSource/Villagers/model_sheet.png`. **Pásamela antes de seguir**, para aprobar los diseños.

### 1.1 Animaciones por personaje (2 imágenes por personaje, 1536×1024)

Adjunta también la hoja de modelo y cambia `<CHARACTER>` por el personaje (por ejemplo "the stocky farmer, character 1 from the attached model sheet").

**Hoja A: movimiento**
```
Create a sprite sheet of <CHARACTER>, EXACT same design and proportions as in the attached model sheet. Grid: 8 columns x 3 rows (24 frames), read left to right. The character is EXACTLY the same size in every frame, side view facing RIGHT, horizontally centered in its cell, feet on the same invisible ground line near the bottom of each cell. Smooth in-between poses, squash and stretch, anticipation, follow-through on clothes, hat and held props.
Row 1 IDLE loop: breathing, a blink, small personality gesture (scratching head / adjusting hat / shifting weight).
Row 2 WALK cycle loop: relaxed walk with body bob and arm swing, props held naturally.
Row 3 RUN / CHASE loop: angry frantic run after the witch, leaning forward, prop raised (pitchfork, cane, bucket, slingshot, bible, lantern), mouth shouting.
Nothing cropped, nothing crossing into other cells.
```
**Hoja B: reacciones**
```
Create a sprite sheet of <CHARACTER>, EXACT same design and proportions as in the attached model sheet. Grid: 8 columns x 3 rows (24 frames), same size and ground line in every frame, side view facing RIGHT.
Row 1 NOTICE + SCREAM: frames 1-3 freeze in surprise (jump slightly, eyes wide), frames 4-8 point forward and scream to raise the alarm, mouth wide open.
Row 2 SPELLS: frames 1-3 BLINDED (rubbing eyes, stumbling), frames 4-6 HYPNOTIZED (dazed swaying, spiral eyes, arms loose), frames 7-8 ASLEEP standing (head drooped, eyes closed).
Row 3 PERSONAL ACTION loop: <ACTION>.
Nothing cropped, nothing crossing into other cells.
```
Acción propia (`<ACTION>`) de cada uno:
| Personaje | Acción | Archivo |
|---|---|---|
| Granjero | lifting and carrying a hay bale on his shoulder | `ArtSource/Villagers/farmer_A.png` / `farmer_B.png` |
| Anciana | scattering grain for invisible chickens | `oldwoman_A.png` / `oldwoman_B.png` |
| Lechera | setting down the bucket and wiping her brow | `milkmaid_A.png` / `milkmaid_B.png` |
| Niño | kicking a pebble and laughing | `boy_A.png` / `boy_B.png` |
| Predicador | reading the bible and raising a finger while preaching | `preacher_A.png` / `preacher_B.png` |
| Guardia | swinging the lantern side to side, looking around suspiciously | `guard_A.png` / `guard_B.png` |

---

## Lote 2 — Suelo, rocas y troncos (1536×1024)

### 2.1 Suelo

En el juego mide 8×4 unidades y se repite en horizontal.
```
Create a seamless horizontally TILEABLE ground strip for a 2D side-scroller, filling the full width of the image edge to edge (the left and right edges must match perfectly). Top 30%: thick golden autumn grass with fallen orange and brown leaves, irregular blades breaking the top silhouette. Below: rich brown soil layers with small roots, pebbles and stones, getting darker toward the bottom. Draw 2 variant strips stacked vertically with magenta between them: strip 1 normal, strip 2 with a few mushrooms and a pumpkin root.
```
Archivo: `ArtSource/Environment/ground.png`

### 2.2 Rocas y troncos (plataformas)
```
Draw a set of separate platform pieces for a 2D side-scroller, each one with a flat walkable top surface, arranged in a grid with generous space between them:
1-2. Two mossy stone ledges (short and long), slate grey stone with moss and grass on top, dangling roots underneath.
3-5. Three fallen log platforms (short, medium, long), bark texture, moss and autumn leaves on top, cut rings visible at the ends.
6. A hollow log tunnel shown in TWO separate pieces: the BACK half (dark interior) and the FRONT half (the rim and upper bark that the player passes behind), same size, so a small cat can walk through.
7. Three small decorative rocks of different sizes.
```
Archivo: `ArtSource/Environment/platforms.png`

---

## Lote 3 — Maizal (1024×1536 vertical)

Mecánica: la bruja y el gato se esconden dentro del maizal. Los tallos del frente se vuelven semitransparentes, así que deben leerse bien en capas.
```
Draw separate corn field pieces for a 2D side-scroller stealth game, arranged with generous space between them:
1-3. Three tall single corn stalks (different leaf shapes), dry golden-orange autumn corn with ripe cobs, leaves curling, roots at the base. Each stalk about 2.5x the witch's height.
4. A wide dense corn cluster (5-6 overlapping stalks) as one piece, for the background row.
5-6. Two short foreground corn tufts (bushy, leaves only) to place in front of the player.
Consistent bottom edge so they all sit on the same ground line.
```
Archivo: `ArtSource/Environment/corn.png`

---

## Lote 4 — Objetos: pociones, corazones y escoba (1536×1024)

### 4.1 Pociones
```
Draw a game items sheet, each item separated with generous space, arranged in rows:
Row 1: three magic potion bottles as pickups (big, readable): BLIND potion (milky white liquid, eye-shaped stopper), HYPNOTIZE potion (purple swirling liquid, spiral label), SLEEP potion (night blue liquid with tiny stars, moon-shaped stopper). Hand-made glass with cork, rope and small paper label, no glow.
Row 2: the same three potions as small thrown projectiles (simpler, slightly tilted).
Row 3: three small status icons to float over enemy heads: blinded (white eye with a cross), hypnotized (purple spiral), asleep ("Zzz" as shapes, no real letters needed).
```
Archivo: `ArtSource/Items/potions.png`

### 4.2 Salpicaduras (efecto al romperse la poción)
```
Draw 3 rows of 4-frame splash effect animations (a glass potion shattering on impact, liquid bursting outward then fading into drops and mist), one row per color: milky white, purple, night blue with stars. Same size per frame, read left to right.
```
Archivo: `ArtSource/Items/splashes.png`

### 4.3 Corazones
```
Draw a game items sheet with generous space between items:
Row 1: a heart pickup to collect in the world (hand-stitched red cloth heart with a small patch and button, readable at small size), a FULL heart UI icon, an EMPTY heart UI icon (dark outline only with faint stitches).
Row 2: a 6-frame animation of the heart BREAKING (crack, tear at the seam, stuffing pops out, pieces fall).
Row 3: a 4-frame animation of a heart being HEALED (pieces sew themselves together with a golden thread, then a small pulse).
```
Archivo: `ArtSource/Items/hearts.png`

### 4.4 Escoba y sus piezas
```
Draw a game items sheet with generous space between items:
1. The witch's flying broom, side view horizontal, gnarled wooden handle, thick bundled straw tied with rope, a small patch of cloth, as the mount she sits on.
2-4. The three collectible broom parts as separate items: a gnarled branch (handle), a coiled rope, a bundle of straw tied in the middle.
No glow outlines.
```
Archivo: `ArtSource/Items/broom.png`

---

## Lote 5 — Letreros, puntos de guardado y meta (1536×1024)
```
Draw separate level props for a 2D side-scroller, generous space between them:
1. A wooden signpost with an empty weathered plank (no text), nails, a small lantern hook, autumn leaves at the base.
2. A small wooden arrow sign pointing right (no text).
3. A CHECKPOINT lantern post shown as a 6-frame animation in a row: unlit iron lantern on a wooden post, a spark appears, flame grows, fully lit warm lantern with a soft light disc behind it, flame flicker frames. Same size every frame.
4. The level goal: a rustic wooden village gate with ropes and a hanging lantern, open in the middle.
```
Archivo: `ArtSource/Environment/props.png`

---

## Lote 6 — Fondos de paralaje (1536×1024, uno por imagen)

Yo los extiendo a lo ancho y los hago repetibles. Pide cada capa por separado:
```
Wide background layer for a 2D side-scroller parallax, horizontally seamless (left and right edges match), same art style but softer contrast and fewer details than the gameplay layer:
```
- **Cielo (lejano):** `deep blue-purple night sky with a huge warm full moon with subtle swirls, a few stars and thin clouds. No ground.` → `ArtSource/Environment/bg_sky.png`
- **Bosque medio:** `silhouettes of dark blue pine trees and twisted bare oaks, darker at the bottom, transparent-ready magenta sky above them.` → `bg_trees.png`
- **Aldea lejana:** `distant silhouettes of 1692 Salem: wooden saltbox houses with lit windows, a meeting house with a steeple, fences and smoke from chimneys, magenta sky above.` → `bg_village.png`

---

## Después de las imágenes (lo hago yo)

- Recorte, escala exacta, pivotes y reemplazo de los sprites conservando el tamaño de los colisionadores, para no romper la jugabilidad.
- Aldeanos: 5 variantes en el nivel, cada una con su paseo y su acción propia. Miran a la bruja cuando se acerca, tienen velocidades y tiempos ligeramente distintos, y sus reacciones a los hechizos quedan conectadas a la IA actual.
- Gato: integrar la hoja aprobada (`ArtSource/Cat/sheets/cat_all_chatgpt.png`).
- Escoba: alinearla con la pose de vuelo del rig de la bruja.
