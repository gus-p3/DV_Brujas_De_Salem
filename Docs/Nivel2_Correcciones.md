# Correcciones Estructurales y de Arte — Nivel 2 (Pueblo y Mercado)

## 1. Diagnóstico y Solución del Bug de la Escalera del Sótano (Bloque A)

### Causa Raíz Exacta
Al inspeccionar `Assets/Scenes/Level2.unity`, `Assets/Scripts/Level2/Interaction/LadderClimber.cs` y `Assets/Scripts/Level2/Interaction/LadderInteractable.cs`, se identificaron **3 causas combinadas** que impedían salir del sótano en `Interior_House2_Tavern` (`x = 54.0`) y `Interior_House4_Manor` (`x = 141.6`):

1. **Hueco físico de ~2.0 u sin plataforma en el tope (`TopY = 28.75`) bajo el eje de la escalera (`CenterX`):**
   - En `Interior_House2_Tavern`, la losa izquierda `Floor1_Slab` terminaba en `x = 52.95` y la derecha `Floor1_RightLanding` iniciaba en `x = 54.90`, dejando un vacío entre `52.95` y `54.90`.
   - Cuando `LadderClimber` alcanzaba `TopY` (`28.75`), posicionaba a la bruja en `CenterX = 54.0` y llamaba a `StopClimbing()`, reactivando la gravedad (`gravityScale = 3.5`) justo encima del hueco vacío. La bruja volvía a caer inmediatamente al sótano.
2. **Bloqueo explícito de la forma de gato (`IsCatForm`):**
   - `LadderClimber.cs` y `LadderInteractable.cs` rechazaban la interacción cuando `TransformationManager.IsCatForm == true`. Como el sótano tiene `4.0 u` de profundidad (`24.75` a `28.75`) y el salto máximo es `2.8 u`, si el jugador bajaba o se transformaba en gato dentro del sótano quedaba atrapado permanentemente.
3. **Ausencia de peldaños caminables y de respaldo de salida:**
   - No existía una geometría de escalones (`<= 0.5 u` por paso) ni un trigger interactivo de respaldo (`tecla E`) en la base de la escalera.

### Solución Implementada
- **Puente One-Way (`LadderTopBridge_*`):** Se añadió una losa con `BoxCollider2D` (`usedByEffector = true`) y `PlatformEffector2D` (`useOneWay = true`, `surfaceArc = 170°`) en el hueco superior de cada escalera interior (`y = 28.75, 32.75, 36.75`, diferencia `0.000 u <= 0.02 u` respecto al piso de planta baja), registrada en `passThroughPlatforms` de `LadderInteractable`. El hueco libre sobre `TopY` es de `3.75 u` (`>= 1.6 u`).
- **Peldaños caminables (`Cellar_WalkableSteps`):** Se añadieron 8 peldaños one-way (`CellarStep_1` a `CellarStep_8`) con incrementos de `0.50 u` de altura desde `24.75` hasta `28.75`, permitiendo salir también caminando y saltando.
- **Escalado en forma humana y gato:** Se habilitó el uso de `LadderClimber` y `LadderInteractable` tanto en forma humana como en forma de gato.
- **Trigger de respaldo con tecla `E` (`CellarExitBackupInteractable`):** Se añadió `CellarExitBackupTrigger` en la base de las escaleras del sótano (`House 2` y `House 4`) para permitir subir también pulsando `E`.

---

## 2. Inventario de Escaleras y Puertas — Decisiones de Coherencia (Bloque B)

| Objeto | Posición Original | Problema Detectado | Acción Aplicada |
| :--- | :--- | :--- | :--- |
| `Walkway_Ext_Z1_Bridge` | `(23.5, 4.30)` | Pasarela flotando sin casa detrás | **Eliminado** |
| `Ladder_Ext_Z1_Balcony` | `(19.5, 2.24)` | Escalera en el aire lejos de `House1` (`x=15.5`) | **Reemplazada** por `Ladder_Ext_House1_Balcony` (`x=12.9`, `0.17 -> 4.525`) adosada a `House1_Facade_Apothecary` y su `Balcony_House1` |
| `Walkway_Ext_Z2_West` / `East` | `(33.5, 3.80)` / `(53.0, 3.80)` | Pasarelas flotando sobre la calle sin fachada | **Eliminados** |
| `Ladder_Ext_Z2_West` / `East` | `(33.5, 1.99)` / `(53.0, 1.99)` | Escaleras huérfanas sin casa detrás | **Reemplazadas** por `Ladder_Ext_House2_Balcony` (`x=42.4`, `0.17 -> 4.025`) adosada a `House2_Facade_Tavern` y su `Balcony_House2` |
| `Ladder_Ext_Z3_Left` / `Right` | `(64.5, 2.19)` / `(91.5, 2.19)` | Suben a `Bridge_Z3_Upper` (ruta de guardia) pero faltaban fachadas de apoyo | **Reubicadas y respaldadas** con fachadas `House_Struct_Z3_Quarantine` (`x=63.5`) y `House_Struct_Z3_BridgeEast` (`x=91.0`), conectando `0.17 -> 4.425` |
| `Ladder_Ext_Z4_RoofExit` | `(98.5, 2.39)` | Separada `10.5 u` de `House3_Facade_Abandoned` | **Reubicada** a `x=106.2` (`0.17 -> 4.825`), adosada a `House3_Facade_Abandoned` y `Roof_House3_JumpPad` |
| `Ladder_Ext_Z5_West` | `(125.0, 2.34)` | Subía a `Terrace_Z5_West` sin fachada detrás | **Respaldada** con `House_Struct_Z5_TerraceWest` (`x=127.5`), conectando `0.17 -> 4.725` |
| `Ladder_Ext_Z5_HighTower` | `(138.0, 4.50)` | Base desconectada de `Terrace_Z5_West` y sin torre detrás | **Reubicada** a `x=135.0` (`4.725 -> 8.725`) apoyada sobre `Terrace_Z5_West` y respaldada por `House_Struct_Z5_Tower` |
| `Ladder_Ext_Z5_East` | `(161.0, 2.24)` | Separada `12 u` de `House4_Facade_Manor` | **Reubicada** a `x=151.6` (`0.17 -> 4.525`), adosada a `House4_Facade_Manor` y `Balcony_House4` |
| `Door_Ext_Z3_Quarantine` | `(62.8, 0.17)` | Puerta suelta en la calle sin casa detrás | **Respaldada** por `House_Struct_Z3_Quarantine` y alineada al suelo (`minY = 0.170`) |
| `Door_Ext_Z6_Sanctum` | `(173.0, 0.17)` | Puerta suelta sin casa detrás | **Respaldada** por `House_Struct_Z6_Sanctum` y alineada al suelo (`minY = 0.170`) |
| `Stall_Z2_2` / `Stall_Z5_2` | `x=45.0` / `x=149.0` | Puestos de mercado tapando las puertas de `House2` y `House4` | **Desplazados** a `x=38.8` y `x=143.2` para despejar las entradas |
| Todas las puertas (`HouseDoor`) | Exteriores e interiores | Bordes inferiores con pequeñas desviaciones | **Alineadas por raycast** (`sr.bounds.min.y` a `<= 0.001 u` de la superficie del suelo o balcón) |

---

## 3. Reemplazo de Casas Decorativas, Casas Interactivas y Fondo con Parallax (Bloques C, D, E)

- **Atlas de casas (`Assets/Screenshots/NIVEL2/houses_sprite.png.png`):** Configurado en `Sprite Mode = Multiple` (12 casas individuales recortadas al píxel exacto de su base), `PPU = 64`, `Pivot = Bottom Center (0.5, 0)`, `FullRect`, sin compresión. Copia limpia adicional en `Assets/Art/Environment/Level2/houses_sprite_clean.png`.
- **Casas decorativas (`House_Deco_Z1..Z6`):** 17 fachadas distribuidas en las 6 zonas del pueblo usando los 12 sprites nuevos con variación de escala (`0.92–1.06`) y `flipX`, apoyadas exactamente sobre el suelo (`minY = 0.170`), en `Sorting Layer = Midground, Order = 2`, sin colliders sólidos.
- **Casas interactivas (`House1_Facade_Apothecary`, `House2_Facade_Tavern`, `House3_Facade_Abandoned`, `House4_Facade_Manor`):** Actualizadas con sprites de 2 plantas de `houses_sprite.png.png`, apoyadas en `minY = 0.170`, conservando intactos sus `HouseDoor` (`tecla E`) y `InteriorZone`.
- **Fondo con Parallax (`Environment/Parallax`):**
  - `Sky_Far` (`parallaxFactor = (0.90, 0.95)`, `Order = -20`): `Assets/Screenshots/NIVEL2/village_day_fondo.png`.
  - `Village_Far` (`parallaxFactor = (0.72, 0.88)`, `Order = -15`): `Assets/Art/Environment/Level2/Level2_Village_Far.png`.
  - `Facades_Mid` (`parallaxFactor = (0.42, 0.75)`, `Order = -10`): `Assets/Art/Environment/Level2/Level2_Facades_Mid.png`.
  - Cada capa contiene 7 copias (`Copy_0` a `Copy_6`) con escala uniforme idéntica (`1.6`) y espaciado exacto `i * tileWorldWidth` (`54.30 u`), eliminando por completo el salto/desfase al entrar en modo Play.
