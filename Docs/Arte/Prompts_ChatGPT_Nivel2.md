# Prompts para Generación de Arte en ChatGPT — Nivel 2 (Pueblo y Mercado de Salem)

Este documento reúne los prompts listos para copiar y pegar en ChatGPT (GPT-4o / DALL·E Image Generation) para sustituir los placeholders del Nivel 2 por spritesheets animados y fondos tileables definitivos, siguiendo estrictamente la Dirección de Arte del GDD.

---

## 1. Cofres de Madera Colonial / Boticario (Spritesheet de 4 Cuadros × 2 Variantes)

### 1A. Variante 1 — Cofre de Madera y Hierro Forjado (Calle y Mercado, 4 frames)
```text
Genera un spritesheet horizontal de 4 frames (1 fila x 4 columnas, proporción 4:1, resolución recomendada 2048x512 px) de un cofre de madera colonial del siglo XVII con herrajes de hierro forjado y cerradura de bronce, en vista lateral/frontal 2D ortográfica:
- Frame 1 (Cerrado): cofre completamente cerrado con su candado de bronce intacto.
- Frame 2 (Entreabierto): la tapa de madera se levanta ligeramente con un destello cálido ámbar asomando por la rendija.
- Frame 3 (Abierto con brillo): la tapa completamente abierta mostrando un resplandor dorado cálido de frascos alquímicos y hierbas en su interior.
- Frame 4 (Abierto vacío): la tapa permanece abierta en reposo tras haber recogido su contenido, sin resplandor interior.
Mismo objeto y mismo encuadre en todos los frames, centrados y alineados exactamente a la misma línea base inferior, mismo tamaño en cada frame. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/Chest_Colonial_Iron_Sheet.png`
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `Pixels Per Unit = 64`, `Filter Mode = Bilinear`, `Compression = None`
  - Sprite Editor: `Slice -> Grid By Cell Count (4 cols x 1 row)`, `Pivot = Bottom Center (0.5, 0)`

### 1B. Variante 2 — Cofre Arcano de Boticario con Herrajes Dorados (Interiores y Altillos, 4 frames)
```text
Genera un spritesheet horizontal de 4 frames (1 fila x 4 columnas, proporción 4:1, resolución recomendada 2048x512 px) de un cofre de boticario de madera oscura rojiza con esquineras de latón dorado grabado y sello alquímico, en vista lateral/frontal 2D ortográfica:
- Frame 1 (Cerrado): cofre cerrado con sello dorado.
- Frame 2 (Entreabierto): la tapa se abre levemente dejando escapar chispas cálidas color ámbar.
- Frame 3 (Abierto con brillo): tapa abierta con cálido fulgor dorado y frascos de vidrio visibles dentro.
- Frame 4 (Abierto vacío): tapa abierta en reposo sin brillo interior.
Mismo objeto y mismo encuadre en todos los frames, centrados y alineados exactamente a la misma línea base inferior, mismo tamaño en cada frame. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/Chest_Apothecary_Gold_Sheet.png`
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `Pixels Per Unit = 64`, `Filter Mode = Bilinear`, `Compression = None`
  - Sprite Editor: `Slice -> Grid By Cell Count (4 cols x 1 row)`, `Pivot = Bottom Center (0.5, 0)`

---

## 2. Sombrilla / Toldo de Puesto de Mercado, Postes y Mercancía

### 2A. Toldos de Tela a Rayas con Flecos Ondulantes (4 Variantes de Color × 3 Cuadros de Mecido)
```text
Genera un spritesheet en cuadrícula de 4 filas x 3 columnas (proporción 3:2, resolución recomendada 1536x1024 px) de toldos trapezoidales de tela a rayas con faldón festoneado (flecos redondeados inferiores) para puestos de mercado medieval-colonial en vista lateral 2D ortográfica.
Cada fila corresponde a una variante cálida de color:
- Fila 1: Rayas rojo carmesí cálido y crema pergamino.
- Fila 2: Rayas ocre dorado y amarillo miel.
- Fila 3: Rayas naranja terracota quemado y crema cálido.
- Fila 4: Rayas verde oliva cálido y ocre tostado (estilo remiendo/patchwork artesanal).
Las 3 columnas de cada fila muestran 3 frames sutiles de mecido por el viento (Columna 1: brisa a la izquierda; Columna 2: centro en reposo; Columna 3: brisa a la derecha). El toldo y su faldón inferior están unidos sin huecos transparentes intermedios. Mismo objeto y encuadre en cada celda. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/MarketStall_Awnings_4x3_Sheet.png` (o individuales `Placeholder_MarketStall_Awning_Red/Gold/Orange/Olive.png`)
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple` (para hoja 4×3) o `Single` (`Mesh Type = Full Rect` para sprites individuales), `Pixels Per Unit = 64`, `Filter Mode = Bilinear`, `Compression = None`, `Pivot = Center (0.5, 0.5)`

### 2B. Estructura de Mostrador y Postes de Madera (Compatibles con Puestos Bajos y Elevados)
```text
Genera un sprite 2D en vista lateral ortográfica (proporción 3:2, 768x512 px) de la estructura de madera de un puesto de mercado colonial del siglo XVII: mostrador frontal de tablones verticales de roble con encimera gruesa y dos postes laterales robustos de madera que suben para sostener el toldo superior. Base inferior completamente plana y horizontal para apoyar a ras del suelo o sobre vigas elevadas. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/Placeholder_MarketStall_WoodPosts.png`
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `Mesh Type = Full Rect`, `Pixels Per Unit = 64`, `Filter Mode = Bilinear`, `Compression = None`, `Pivot = Bottom Center (0.5, 0)`

### 2C. Mercancía sobre la Mesa del Puesto (Cestas, Panes, Frascos y Telas — 4 Props en 1 Fila)
```text
Genera un atlas horizontal de 4 sprites en una sola fila (1 fila x 4 columnas, proporción 4:1, 2048x512 px) de mercancía para colocar sobre el mostrador de los puestos del mercado de Salem en vista lateral 2D:
1) Cesta de mimbre llena de manzanas rojas, hierbas y raíces secas.
2) Conjunto de frascos de vidrio ámbar, mortero de piedra y velas de cera de abeja.
3) Hogazas de pan rústico, queso sobre tabla de madera y saco de grano atado con cuerda.
4) Rollos de tela cálida (ocre, carmesí y oliva) apilados junto a una balanza de latón.
Mismo tamaño de celda y alineados exactamente a la línea base inferior. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco, sin elementos extra.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/Market_CounterWares_Atlas.png`
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `Pixels Per Unit = 64`, `Filter Mode = Bilinear`, `Compression = None`, `Slice -> Grid By Cell Count (4 cols x 1 row)`, `Pivot = Bottom Center (0.5, 0)`

---

## 3. Fondo Continuo de Aldea Medieval / Colonial (3 Capas Separadas 3:1 Tileables)

### 3A. Capa 1 (Lejana): Cielo Crepuscular / Atardecer con Nubes Suaves (`Level2_Sky_Far.png`)
```text
Genera una panorámica horizontal 2D perfectamente TILEABLE sin costuras (seamless horizontal loop: el borde izquierdo debe coincidir exactamente en color, altura e iluminación con el borde derecho, proporción 3:1, 2172x724 px) de un cielo de atardecer/crepúsculo sobre Nueva Inglaterra con transición suave de azul índigo en lo alto a dorado ámbar cálido en el horizonte y nubes pintadas suaves. Vista lateral 2D. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Sin edificios, sin árboles que lleguen al borde superior, sin texto, sin marco.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/Level2_Sky_Far.png`
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `Mesh Type = Full Rect`, `Wrap Mode = Repeat`, `Pixels Per Unit = 100`, `Filter Mode = Bilinear`, `Compression = None`, `Pivot = Center (0.5, 0.5)`

### 3B. Capa 2 (Media-Lejana): Silueta Continua de Aldea con Tejados, Campanario y Colinas (`Level2_Village_Far.png`)
```text
Genera una panorámica horizontal 2D perfectamente TILEABLE sin costuras (seamless horizontal loop: el borde izquierdo y el borde derecho deben terminar en una arboleda continua de la misma altura y tono sin cortar ninguna casa ni tejado a la mitad, proporción 3:1, 2172x724 px) de una aldea medieval-colonial completa al atardecer con colinas arboladas al fondo, un campanario sobre la colina, casas variadas de entramado de madera y piedra con tejados de teja y pizarra, chimeneas humeantes, farolas cálidas encendidas y un murete continuo de piedra en la base. Vista lateral 2D. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Sin texto, sin marco, sin casas cortadas en los extremos laterales.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/Level2_Village_Far.png` (y variante `Level2_Village_Far_VarB.png`)
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `Mesh Type = Full Rect`, `Wrap Mode = Repeat`, `Pixels Per Unit = 100`, `Filter Mode = Bilinear`, `Compression = None`, `Pivot = Center (0.5, 0.5)`

### 3C. Capa 3 (Media-Cercana): Cercas de Madera, Farolas y Arbustos de Calle (`Level2_Facades_Mid.png`)
```text
Genera una franja panorámica horizontal 2D perfectamente TILEABLE sin costuras (seamless horizontal loop: las vallas de madera y arbustos del borde izquierdo conectan exactamente con el borde derecho, proporción 3:1, 2172x724 px) con cercas bajas de madera colonial, farolas de hierro forjado encendidas con halo cálido, barriles y arbustos bajos en el tercio inferior de la imagen, dejando los dos tercios superiores completamente limpios en blanco sólido uniforme para recorte transparente. Vista lateral 2D. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme en la zona superior para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto, sin marco.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/Level2_Facades_Mid.png`
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, `Mesh Type = Full Rect`, `Wrap Mode = Repeat`, `Pixels Per Unit = 100`, `Filter Mode = Bilinear`, `Compression = None`, `Pivot = Center (0.5, 0.5)`

---

## 4. Icono de Tecla `Q` para el Indicador de Interacción (2 Cuadros: Normal y Pulsado)

```text
Genera un spritesheet horizontal de 2 frames (1 fila x 2 columnas, proporción 2:1, resolución recomendada 1024x512 px) de un icono de botón/tecla "Q" estilo libro de cuentos ilustrado en madera tallada con borde de bronce dorado y la letra "Q" en serif clara color crema:
- Frame 1 (Normal): la tecla elevada en reposo con borde inferior de grosor visible y brillo suave dorado.
- Frame 2 (Pulsado): la tecla hundida hacia abajo con resplandor ámbar cálido alrededor del marco de bronce.
Mismo encuadre en ambos frames, centrados. Cuento ilustrado 2D, contornos gruesos tipo tinta, paleta cálida (ocres, dorados, naranjas, azules profundos de noche), toque travieso no tenebroso, ambientación de aldea colonial de Salem siglo XVII. Fondo blanco sólido uniforme para recorte limpio. Sin sombras proyectadas sobre el fondo, sin texto adicional, sin marco exterior, sin elementos extra.
```
- **Importación en Unity:**
  - Ruta: `Assets/Art/Environment/Level2/UI_Key_Q_Sheet.png`
  - `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `Pixels Per Unit = 64`, `Filter Mode = Bilinear`, `Compression = None`, `Slice -> Grid By Cell Count (2 cols x 1 row)`, `Pivot = Center (0.5, 0.5)`

---

## 5. Lista de Assets Temporales (`Placeholder_*` / `Interactive_Chest_*`) que Sustituyen

| Asset Actual en `Assets/Art/Environment/Level2/` | Reemplazado por Prompt |
| :--- | :--- |
| `Interactive_Chest_Closed.png`, `Interactive_Chest_Open.png` | **Sección 1A y 1B** (`Chest_Colonial_Iron_Sheet.png`, `Chest_Apothecary_Gold_Sheet.png`) |
| `Placeholder_MarketStall_Awning_Red.png`, `Gold.png`, `Orange.png`, `Olive.png`, `Large.png` | **Sección 2A** (`MarketStall_Awnings_4x3_Sheet.png`) |
| `Placeholder_MarketStall_WoodPosts.png` | **Sección 2B** (`Placeholder_MarketStall_WoodPosts.png`) |
| `Placeholder_Market_Crate.png`, `Placeholder_Market_Barrel.png` (en mostradores) | **Sección 2C** (`Market_CounterWares_Atlas.png`) |
| `Level2_Sky_Far.png`, `Level2_Village_Far.png`, `Level2_Village_Far_VarB.png`, `Level2_Facades_Mid.png` | **Sección 3A, 3B y 3C** (Capas de fondo tileables 3:1) |
| Emblema de texto `"Q:"` en `Level2DoorIndicator` e `InteractionPromptUI` | **Sección 4** (`UI_Key_Q_Sheet.png`) |
