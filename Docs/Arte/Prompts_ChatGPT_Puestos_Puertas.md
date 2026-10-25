# Prompts para ChatGPT: puestos, puertas y letreros del Nivel 2 (Pueblo y Mercado)

Hoy el Nivel 2 funciona completo con **placeholders** y **animación procedural**: el toldo se balancea, la mercancía colgante se mece como un péndulo y la luz del farol parpadea. Las imágenes de esta lista lo harán verse definitivo. **No hay que cambiar código**: al colocarlas, se sustituye el sprite en los objetos indicados y se reemplaza el `MarketStallAnimator` por un Animator con los clips nuevos.

**Estilo común (va dentro de cada prompt):** «Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, aldea colonial de Salem siglo XVII. Vista lateral 2D. Fondo blanco sólido uniforme para recorte limpio. Sin texto, sin marco, sin sombras proyectadas sobre el fondo.»

**Importación común:** carpeta `Assets/Art/Environment/Level2/`, `Texture Type = Sprite (2D and UI)`, `Filter Mode = Bilinear`, `Compression = None`. El **PPU** se calcula para que la pieza mida en el mundo lo que mide su placeholder: `PPU = ancho del cuadro en píxeles / ancho en unidades`. Así no hay que reescalar nada en la escena.

## Placeholders en uso (los que hay que sustituir)

| Placeholder | Dónde se usa | Medida en el mundo |
| :--- | :--- | :--- |
| `Placeholder_MarketStall_Awning_Red/Gold/Orange/Olive` | `Awning_Sway_Visual` de los 15 puestos | 3.0 × 1.0 u |
| `Placeholder_MarketStall_WoodPosts` | `PostsAndCounter` (calle) y `Elevated_CanopyPosts` (elevados) | 3.0 × 2.0 u |
| `Placeholder_Market_Lantern` | `Hanging_Lantern_Visual` sobre cada mostrador y faroles de calle | 0.45 × 2.15 u |
| `Placeholder_Market_Crate`, `Placeholder_Market_Barrel` | `Counter_Wares_Visual` (mercancía del mostrador) | ≈0.45 u |
| `Placeholder_Ingredient_Root`, `Placeholder_Ingredient_WitheredFlower` | `Hanging_Wares_Pivot/Hanging_Wares_Visual` (colgantes) | ≈0.4 × 0.5 u |
| `Placeholder_Market_RoofPlatform` | Plataformas de los puestos elevados | 3.9 × 0.5 u |
| Panel de UI (sin sprite) | `Level2InfoSign` de los letreros de cofres | 1.5 × 0.6 u y 2.3 × 0.95 u |

---

## 1. Toldo de puesto meciéndose (6 frames × 4 colores)

```text
Genera un spritesheet horizontal de 6 frames (1 fila x 6 columnas, proporción 18:6, cada cuadro 384x128 px) de un toldo de tela de puesto de mercado colonial con borde festoneado y un par de remiendos cosidos, visto de lado. Los 6 frames muestran un ciclo suave de balanceo por el viento: la tela se infla levemente, el festón ondula y vuelve a reposo; el borde superior (la barra de madera) no se mueve. Variante de color: [ROJO TERRACOTA | DORADO OCRE | NARANJA CALABAZA | VERDE OLIVA] con rayas crema. Mismo encuadre y tamaño en todos los frames, centrado y alineado a la misma línea base. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, aldea colonial de Salem siglo XVII. Vista lateral 2D. Fondo blanco sólido uniforme para recorte limpio. Sin texto, sin marco, sin sombras proyectadas sobre el fondo.
```

- **Archivos:** `L2_Stall_Awning_Red_Sheet.png`, `L2_Stall_Awning_Gold_Sheet.png`, `L2_Stall_Awning_Orange_Sheet.png`, `L2_Stall_Awning_Olive_Sheet.png`. Pide una imagen por color, cambiando solo el texto entre corchetes.
- **Unity:** `Sprite Mode = Multiple`, `Slice = Grid By Cell Count 6×1`, `Pivot = Center`, `PPU = 128` (3 u de ancho).

## 2. Llama de farola parpadeando (5 frames)

```text
Genera un spritesheet horizontal de 5 frames (1 fila x 5 columnas, cada cuadro 128x128 px) de la llama de un farol de aceite colonial, solo la llama y su halo cálido, sin el farol. Los frames muestran un parpadeo suave: la llama se estira, se inclina un poco a cada lado y vuelve. Mismo encuadre, centrada y alineada a la misma base en todos los frames. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, aldea colonial de Salem siglo XVII. Vista lateral 2D. Fondo blanco sólido uniforme para recorte limpio. Sin texto, sin marco, sin sombras proyectadas sobre el fondo.
```

- **Archivo:** `L2_Lantern_Flame_Sheet.png`
- **Unity:** `Sprite Mode = Multiple`, `Slice = Grid By Cell Count 5×1`, `Pivot = Bottom Center`, `PPU = 640` (llama de 0.2 u). Va como hijo del farol, a la altura del cristal. La luz `Light2D` + `LanternFlicker` se mantiene.

## 3. Mercancía colgante oscilando (6 frames × 3 objetos)

```text
Genera un spritesheet horizontal de 6 frames (1 fila x 6 columnas, cada cuadro 128x160 px) de [UN MANOJO DE HIERBAS SECAS ATADO CON CORDEL | UN CANDIL PEQUEÑO DE LATÓN | UNA TELA DE LINO DOBLADA] colgando de un cordel desde el borde superior del cuadro. Los frames muestran una oscilación de péndulo suave, de izquierda a derecha y de vuelta, con el punto de cuelgue fijo arriba al centro. Mismo encuadre y tamaño en todos los frames. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, aldea colonial de Salem siglo XVII. Vista lateral 2D. Fondo blanco sólido uniforme para recorte limpio. Sin texto, sin marco, sin sombras proyectadas sobre el fondo.
```

- **Archivos:** `L2_Hanging_Herbs_Sheet.png`, `L2_Hanging_Candle_Sheet.png`, `L2_Hanging_Cloth_Sheet.png`
- **Unity:** `Slice = Grid By Cell Count 6×1`, `Pivot = Top Center` (cuelga del pivote `Hanging_Wares_Pivot`), `PPU = 300` (0.43 × 0.53 u).

## 4. Mercader con idle (6 frames × 2 personajes)

```text
Genera un spritesheet horizontal de 6 frames (1 fila x 6 columnas, cada cuadro 256x384 px) de [UN MERCADER PURITANO DE MEDIANA EDAD CON SOMBRERO DE ALA ANCHA Y DELANTAL DE CUERO | UNA MERCADERA PURITANA CON COFIA BLANCA Y CHAL OCRE] de pie detrás de un mostrador (solo de cintura para arriba y visible por encima del mostrador), mirando hacia la calle. Ciclo idle sutil: respira, parpadea una vez y acomoda la mercancía con una mano. Mismo personaje, mismo encuadre y misma línea base en todos los frames. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, aldea colonial de Salem siglo XVII. Vista lateral 2D. Fondo blanco sólido uniforme para recorte limpio. Sin texto, sin marco, sin sombras proyectadas sobre el fondo.
```

- **Archivos:** `L2_Merchant_Man_Idle_Sheet.png`, `L2_Merchant_Woman_Idle_Sheet.png`
- **Unity:** `Slice = Grid By Cell Count 6×1`, `Pivot = Bottom Center`, `PPU = 256` (1.0 × 1.5 u, asoma sobre el mostrador). Será decorativo: sin collider y sin `VillagerAI`, para que el jugador no lo confunda con un aldeano que detecta.

## 5. Puertas de madera para fachadas (3 variantes + luz por la rendija)

```text
Genera 4 puertas de madera colonial del siglo XVII como piezas separadas en una sola imagen (1 fila x 4 columnas, cada cuadro 256x512 px, proporción de puerta 1:2): (1) puerta recta de tablones verticales con dos bisagras de hierro forjado y pomo de latón; (2) puerta con arco de medio punto y marco de piedra clara; (3) puerta reforzada con travesaños en diagonal y cerrojo de hierro; (4) la misma puerta (1) entreabierta con luz cálida ámbar saliendo por la rendija. Madera marrón cálido con vetas, igual a las fachadas de entramado de la aldea. Cada puerta centrada y apoyada en la misma línea base. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, aldea colonial de Salem siglo XVII. Vista lateral 2D. Fondo blanco sólido uniforme para recorte limpio. Sin texto, sin marco, sin sombras proyectadas sobre el fondo.
```

- **Archivo:** `L2_Village_Doors_Sheet.png`
- **Unity:** `Slice = Grid By Cell Count 4×1`, `Pivot = Bottom Center`, `PPU = 256` (1.0 × 2.0 u; las puertas jugables miden 1.85–2.2 u). Uso sugerido: (1) Botica, Posada y Cuarentena; (2) Casa Abandonada y Mansión, que tienen arco pintado; (3) Santuario; (4) `openSprite` de las puertas con interior.

## 6. Postes, mostrador y mercancía (piezas separadas)

```text
Genera como piezas separadas en una sola imagen (fondo blanco, separadas entre sí): (a) estructura de puesto de mercado colonial vista de frente-lateral, con dos postes de madera rústica y un mostrador de tablones con faldón, sin toldo (proporción 3:2, 384x256 px); (b) cesta de mimbre con manzanas; (c) fila de frascos de vidrio con hierbas y semillas; (d) rollo de tela de colores cálidos; (e) caja de madera abierta con hongos y raíces. Cada pieza apoyada en su base, sin cortes. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, aldea colonial de Salem siglo XVII. Vista lateral 2D. Fondo blanco sólido uniforme para recorte limpio. Sin texto, sin marco, sin sombras proyectadas sobre el fondo.
```

- **Archivo:** `L2_Stall_Parts_Sheet.png`
- **Unity:** `Sprite Mode = Multiple`, `Slice = Automatic`, `Pivot = Bottom Center`, `PPU = 128` (estructura de 3.0 × 2.0 u). El mostrador debe quedar a la mitad de la altura, como en el placeholder: el farol y la mercancía se apoyan ahí.

## 7. Carteles de madera para los letreros de cofres

```text
Genera como piezas separadas en una sola imagen: (a) un cartel colgante pequeño de madera con dos cadenitas de hierro, panel vacío sin letras (proporción 5:2); (b) un panel de madera rectangular vacío para clavar en una fachada, con clavos de hierro en las esquinas (proporción 5:2); (c) un icono de flecha de madera pintada estilo cuento apuntando a la derecha; (d) un icono pequeño de cofre de madera con herrajes dorados. Paneles completamente vacíos: el texto lo pone el juego. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, aldea colonial de Salem siglo XVII. Vista lateral 2D. Fondo blanco sólido uniforme para recorte limpio. Sin texto, sin marco, sin sombras proyectadas sobre el fondo.
```

- **Archivo:** `L2_Info_Signs_Sheet.png`
- **Unity:** `Slice = Automatic`, `Pivot = Center` (paneles e iconos), `PPU = 200`. Se usarán como fondo del `Image` de `Level2InfoSign` (hoy es un color sólido de madera) y como icono junto al texto.

---

## Orden recomendado

1. **(6) Postes y mostrador** y **(1) Toldos**: son los placeholders más visibles, en los 15 puestos.
2. **(5) Puertas**: hoy se usan las puertas genéricas `Village_Door_*` con un tinte cálido.
3. **(7) Carteles**, **(3) Colgantes** y **(2) Llama**.
4. **(4) Mercaderes**: hoy no hay mercaderes en los puestos; se agregarían como decoración nueva.
