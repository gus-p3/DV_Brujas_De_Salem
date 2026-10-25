# Documentación Técnica: Fondo Continuo de Aldea Medieval al Atardecer (Nivel 2)

## 1. Resumen Ejecutivo
Se implementó y aplicó la recomposición técnica del fondo del Nivel 2 utilizando como imagen fuente original de alta fidelidad:
`Assets/Screenshots/NIVEL2/Aldea medieval al atardecer.png` (2172 × 724 px, RGB, GUID: `e7039019275a37c429185f5fc3422d44`).

El nuevo fondo panorámico reemplaza la versión anterior (que contenía casas de primer plano impresas en el arte que solapaban con la arquitectura interactiva del nivel), manteniendo de forma matemática y visual la composición de referencia previa (`Environment/Parallax`), su jerarquía de 3 capas con factores de paralaje progresivos, escala uniforme `2.00`, orden de renderizado en `Background` y cobertura continua tanto horizontal como vertical para el vuelo de la bruja.

---

## 2. Parámetros Técnicos de la Composición

### 2.1 Jerarquía en Escena (`Level2.unity`)
- **Raíz**: `Environment/Parallax`
  - Posición: `(0.00, 0.00, 0.00)`
  - Rotación: `(0.00, 0.00, 0.00)`
  - Escala: `(1.00, 1.00, 1.00)`

### 2.2 Especificación de Capas de Paralaje
Todas las capas pertenecen a `Sorting Layer: Background` y carecen totalmente de componentes `Collider2D` para garantizar nula interferencia física con la jugabilidad.

| Capa | Posición Y | Sorting Order | Factor Paralaje X | Lock Y | Textura(s) Asignada(s) | Resolución (px) | Pivot Sprite | Cobertura Vertical (World Y) |
| :--- | :---: | :---: | :---: | :---: | :--- | :---: | :---: | :---: |
| **Sky_Far** | `6.20` | `-20` | `0.88` | `true` | `Level2_Sky_Far.png` | `2172 × 1320` | `(0.5, 0.378788)` | `y = -3.80` a `+22.60 u` |
| **Village_Far** | `4.40` | `-15` | `0.70` | `true` | `Level2_Village_Far.png`<br>`Level2_Village_Far_VarB.png` (Alternadas) | `2172 × 744` | `(0.5, 0.500000)` | `y = -3.04` a `+11.84 u` |
| **Facades_Mid** | `3.60` | `-10` | `0.42` | `true` | `Level2_Facades_Mid.png` | `2172 × 744` | `(0.5, 0.500000)` | `y = -3.84` a `+11.04 u` |

### 2.3 Cobertura Horizontal y Continuidad
- **Pixels Per Unit (PPU)**: `100`
- **Escala uniforme por copia**: `(2.00, 2.00, 1.00)`
- **Ancho efectivo por tile (`tileWorldWidth`)**: `43.44 unidades`
- **Número de copias**: 14 instancias (`Copy_0` a `Copy_13`), con índices relativos `i = -3` a `+10`.
- **Posición X de copias**: `localPosition.x = i * 43.44 u` (desde `-130.32 u` hasta `+434.40 u`).
- **Rango horizontal total cubierto**: `X = -152.04 u` hasta `X = +456.12 u` (amplitud de `608.16 u`), cubriendo con holgura extrema todo el nivel jugable (`X = -4.5 u` a `X = 180.0 u`).
- **Separación entre uniones (Gap)**: `0.000 u` (continuidad matemática perfecta sin fisuras).
- **Modo de Envoltura UV (Wrap Mode)**: `Clamp` para eliminar líneas residuales en el borde superior al volar alto y mantener suavizado bilineal puro en las copias adyacentes.

---

## 3. Generación y Procesamiento de Texturas (`Level2BackgroundProcessor.cs`)

El script editor `Assets/Editor/Level2/Level2BackgroundProcessor.cs` procesa proceduralmente la imagen fuente:
1. **Normalización Lumínica**: Muestreo bilateral en franjas de 220 px y gradiente horizontal suavizado para eliminar discrepancias entre el extremo oriental y occidental del cielo.
2. **Fundido Cíclico Suave (Seamless Blending)**: Ventana de fundido de 180 px con función coseno suavizada $S(t) = \frac{1}{2}(1 - \cos(\pi t))$, garantizando igualdad exacta entre el píxel `0` y el píxel `W - 1`.
3. **Capa `Sky_Far`**:
   - Extensión vertical a 1320 px mediante interpolación cenital suave.
   - Posicionamiento de nubes y tonalidad de atardecer cálido en el horizonte.
   - Pivot personalizado `(0.5, 1000/2640)` para cubrir hasta `Y = +22.60 u` al elevar la cámara a `Y = 15.0 u` en vuelo libre con escoba.
4. **Capa `Village_Far` y Variante B**:
   - Transición de transparencia gradual en el cielo superior (`alpha = 0` sobre `y = 126 px`) para que el cielo lejano respire con su velocidad de paralaje independiente.
   - Generación de `Level2_Village_Far_VarB.png` con modulación sinusoidal cálida interior que rompe la monotonía visual del horizonte sin alterar los bordes idénticos de unión.
5. **Capa `Facades_Mid`**:
   - Aislamiento del muro de piedra inferior, setos verdes y arbustos de base.
   - Offset vertical calibrado para coincidir con la cota de calle y aceras.

---

## 4. Evidencias Comparativas (Antes vs Después)

Las capturas de verificación en alta resolución se encuentran archivadas en `Assets/Screenshots/NIVEL2/Evidencias/`:

| Posición / Escenario | Coordenadas de Cámara | Captura ANTES | Captura DESPUÉS |
| :--- | :---: | :--- | :--- |
| **Inicio Nivel (Zona 1)** | `(5.0, 3.5)`, Ortho `6.0` | `BEFORE_Fondo_Inicio_Z1.png` | `AFTER_Fondo_Inicio_Z1.png` |
| **Unión 1 (Zona 2)** | `(43.44, 4.0)`, Ortho `6.0` | `BEFORE_Fondo_Union1_X43.png` | `AFTER_Fondo_Union1_X43.png` |
| **Plaza Central (Zona 3)** | `(86.88, 4.0)`, Ortho `6.0` | `BEFORE_Fondo_Medio_Union2_X86.png` | `AFTER_Fondo_Medio_Union2_X86.png` |
| **Unión 3 (Zona 4/5)** | `(130.32, 4.0)`, Ortho `6.0` | `BEFORE_Fondo_Union3_X130.png` | `AFTER_Fondo_Union3_X130.png` |
| **Meta Santuario (Zona 6)** | `(173.76, 4.0)`, Ortho `6.0` | `BEFORE_Fondo_Fin_Union4_X173.png` | `AFTER_Fondo_Fin_Union4_X173.png` |
| **Vuelo Alto de la Bruja** | `(86.88, 10.5)`, Ortho `6.5` | `BEFORE_Fondo_VueloAlto.png` | `AFTER_Fondo_VueloAlto.png` |

---

## 5. Integración con Herramientas de Mantenimiento

Para evitar regresiones futuras en la escena:
- `Assets/Editor/Level2/Level2CompleteFixer.cs`:
  - `Tools > Nivel 2 > 5. Aplicar Bloque E` delega en `Level2BackgroundProcessor.ImportAndProcessSourceImage()` y `Level2BackgroundProcessor.ApplyBackgroundToLevel2()`.
  - `Tools > Nivel 2 > Cofres y Controles > 4. Recomponer Fondo Continuo de Aldea` delega directamente en el mismo procesador.
- La configuración del importador de `"Assets/Screenshots/NIVEL2/Aldea medieval al atardecer.png"` preserva intacto el asset original, su metadata y su GUID en disco.
