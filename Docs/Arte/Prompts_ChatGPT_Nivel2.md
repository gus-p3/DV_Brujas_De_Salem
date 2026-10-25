# Prompts para Generación de Arte en ChatGPT — Nivel 2 (Pueblo y Mercado de Salem)

Este documento reúne los prompts listos para copiar y pegar en ChatGPT (DALL·E / GPT-4o Image Generation) para sustituir los placeholders del Nivel 2 por spritesheets animados y fondos tileables definitivos, siguiendo estrictamente la Dirección de Arte del GDD.

---

## 1. Puestos del Mercado Animados (Spritesheets Horizontales)

### 1A. Toldo de Puesto del Mercado Meciéndose al Viento (6 frames)
```text
Genera un spritesheet horizontal de 6 frames (1 fila x 6 columnas, proporción 6:1, resolución recomendada 3072x512 px) de un toldo de madera y tela a rayas rojas y crema de un puesto de mercado medieval-colonial. Vista lateral 2D ortográfica. En los 6 frames se muestra una animación cíclica suave del faldón de tela del toldo ondulando con la brisa de la tarde. Mismo objeto y mismo encuadre en todos los frames, centrados y alineados exactamente a la misma línea base inferior, mismo tamaño en cada frame. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/MarketStall_Awning_Sheet.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `PPU = 64`, `Filter Mode = Bilinear`, Slice `Grid By Cell Count (6 cols x 1 row)`, `Pivot = Bottom Center (0.5, 0)`.

### 1B. Farola de Hierro Forjado con Llama Parpadeando (6 frames)
```text
Genera un spritesheet horizontal de 6 frames (1 fila x 6 columnas, proporción 4:1, resolución recomendada 2048x512 px) de una farola colonial de hierro forjado con cristal ámbar y una llama cálida parpadeando en su interior. Vista lateral 2D ortográfica. La estructura de hierro permanece fija en la misma posición en los 6 frames mientras la llama interior oscila suavemente en altura y brillo cálido. Mismo encuadre en todos los frames, centrados y alineados a la base, fondo blanco. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/Market_Lantern_Flame_Sheet.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `PPU = 64`, Slice `Grid By Cell Count (6 cols x 1 row)`, `Pivot = Bottom Center (0.5, 0)`.

### 1C. Mercancía Colgante Oscilando — Manojos de Hierbas, Frascos y Candiles (6 frames)
```text
Genera un spritesheet horizontal de 6 frames (1 fila x 6 columnas, proporción 6:1, resolución recomendada 3072x512 px) de un travesaño de madera del que cuelgan manojos de hierbas secas, ajos, pequeños frascos de vidrio ámbar y un candil de bronce, oscilando suavemente como un péndulo de izquierda a derecha en un ciclo continuo. Vista lateral 2D ortográfica. Mismo encuadre en todos los frames, centrados y alineados al borde superior de sujeción, fondo blanco. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/Market_HangingWares_Sheet.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `PPU = 64`, Slice `Grid By Cell Count (6 cols x 1 row)`, `Pivot = Top Center (0.5, 1.0)`.

### 1D. Mercader Colonial en Idle Tras el Mostrador (6 frames)
```text
Genera un spritesheet horizontal de 6 frames (1 fila x 6 columnas, proporción 6:1, resolución recomendada 3072x512 px) de un mercader aldeano del siglo XVII (delantal de cuero, camisa color crema, sombrero de fieltro) en animación idle suave (respiración, parpadeo y ligero gesto de manos ofreciendo mercancía). Vista lateral 2D ortográfica. Mismo personaje y encuadre en todos los frames, centrados y alineados a la base, fondo blanco. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/Market_Merchant_Idle_Sheet.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `PPU = 64`, Slice `Grid By Cell Count (6 cols x 1 row)`, `Pivot = Bottom Center (0.5, 0)`.

---

## 2. Puertas de Salida Interiores y Trampilla de Sótano (4 Variantes)

```text
Genera un atlas horizontal con 4 sprites separados en una sola fila (1 fila x 4 columnas, proporción 4:1, resolución recomendada 2048x512 px) de puertas interiores de salida vistas desde dentro de una casa colonial del siglo XVII en vista 2D frontal/lateral:
1) Puerta de madera de roble con marco grueso y luz dorada cálida filtrándose claramente por las rendijas laterales e inferior (salida a la calle).
2) Puerta acristalada de cuarterones con cortina recogida y resplandor cálido exterior (salida a balcón del segundo piso).
3) Puerta pesada con herrajes de hierro forjado, cerrojo abierto y halo ámbar en el umbral (salida de mansión/taberna).
4) Trampilla de madera abierta en el suelo con barandilla y los primeros peldaños de una escalera de madera que baja a un sótano iluminado por antorcha.
Mismo tamaño de celda y alineados exactamente a la línea base inferior. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/Interior_ExitDoors_Atlas.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `PPU = 64`, Slice `Grid By Cell Count (4 cols x 1 row)`, `Pivot = Bottom Center (0.5, 0)`.

---

## 3. Fondo del Pueblo Tileable sin Costuras (3 Capas Separadas, Proporción 3:1)

### 3A. Capa 1 (Lejana): Cielo Crepuscular, Luna y Siluetas de Colinas (`Sky_Far`)
```text
Genera una panorámica horizontal 2D perfectamente TILEABLE sin costuras (seamless horizontal loop: el borde izquierdo debe coincidir exactamente en altura, color y trazos con el borde derecho, proporción 3:1, 2172x724 px) del cielo crepuscular sobre Salem con nubes suaves y siluetas lejanas de colinas y un campanario distante. Vista lateral 2D. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Sin texto, sin marco, sin elementos cortados en los bordes laterales.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/Level2_Sky_Far.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `Mesh Type = Full Rect`, `Wrap Mode = Repeat`, `PPU = 64`, `Pivot = Center (0.5, 0.5)`.

### 3B. Capa 2 (Media-Lejana): Tejados y Colinas Arboladas de la Aldea (`Village_Far`)
```text
Genera una panorámica horizontal 2D perfectamente TILEABLE sin costuras (seamless horizontal loop: el borde izquierdo debe coincidir exactamente en altura y silueta con el borde derecho, proporción 3:1, 2172x724 px) de una hilera lejana de tejados coloniales de teja y pizarra, chimeneas humeantes y pinos sobre colinas. Vista lateral 2D. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme en la mitad superior del cielo para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/Level2_Village_Far.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `Mesh Type = Full Rect`, `Wrap Mode = Repeat`, `PPU = 64`, `Pivot = Center (0.5, 0.5)`.

### 3C. Capa 3 (Media): Fachadas de Madera Entramada y Farolas Encendidas (`Facades_Mid`)
```text
Genera una panorámica horizontal 2D perfectamente TILEABLE sin costuras (seamless horizontal loop: el borde izquierdo debe coincidir exactamente con el borde derecho, proporción 3:1, 2172x724 px) de fachadas coloniales de madera con entramado visto, ventanas iluminadas con luz cálida ámbar, pequeños jardines y farolas encendidas. Vista lateral 2D. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme por encima de los tejados para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/Level2_Facades_Mid.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `Mesh Type = Full Rect`, `Wrap Mode = Repeat`, `PPU = 64`, `Pivot = Center (0.5, 0.5)`.

---

## 4. Icono de Interacción — Tecla `Q` Ilustrada (Opcional)

```text
Genera un icono cuadrado 2D (proporción 1:1, 512x512 px) de un medallón o tecla de madera y bronce dorado grabado con la letra "Q" en tipografía serif de cuento clásico, rodeado de una discreta filigrana cálida. Vista frontal 2D. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto adicional, sin marco exterior, sin elementos extra.
```
- **Importación en Unity:** Guardar en `Assets/Art/Environment/Level2/UI_Key_Q_Icon.png`, `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `PPU = 64`, `Pivot = Center (0.5, 0.5)`.

---

## 5. Lista de Placeholders en Uso (`Placeholder_*`)
Mientras se importan los spritesheets definitivos generados con estos prompts, el Nivel 2 utiliza animación procedural (`MarketStallAnimator`, `LanternFlicker`, `Level2DoorIndicator`) sobre los siguientes assets en `Assets/Art/Environment/Level2/`:
- `Placeholder_MarketStall_Awning_Red.png` (toldo rojo animado con `MarketStallAnimator`)
- `Placeholder_MarketStall_Awning_Gold.png` (toldo dorado animado con `MarketStallAnimator`)
- `Placeholder_MarketStall_Awning_Large.png` (toldo grande animado con `MarketStallAnimator`)
- `Placeholder_MarketStall_WoodPosts.png` (estructura y mostrador del puesto)
- `Placeholder_Market_Lantern.png` (farolas y colgantes animados con `LanternFlicker` y `MarketStallAnimator`)
- `Placeholder_Market_Crate.png`, `Placeholder_Market_Crates_Stack.png`, `Placeholder_Market_Barrel.png`, `Placeholder_Market_Cart.png` (props del mercado)
