# AUDITORÍA TÉCNICA, ARQUITECTURA Y ESCENA BASE — NIVEL 2 (ACTO 2: PUEBLO Y MERCADO)

**Proyecto:** DV_Brujas_De_Salem  
**Versión de Unity:** Unity 6000.3.23f1 (URP 2D)  
**Fecha:** 22 de Octubre de 2026  
**Rama Git:** `main`  
**Escena objetivo:** `Assets/Scenes/Level2.unity`  

---

## 1. INVENTARIO REAL DEL REPOSITORIO (VERIFICADO VS SUPUESTO)

### 1.1 Estado de Git y Archivos Preexistentes
- **Rama actual:** `main` (commit head: `106f0a1 Merge pull request #11 from gus-p3/feature/nivel1-pulido-visual`).
- **Modificaciones de usuario preexistentes:**
  - `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
  - `Docs/Documentacion_General_Juego.md`
  - `README.md`
- **Regla aplicada:** Ninguno de los archivos preexistentes del usuario fue modificado o descartado.

### 1.2 Build Settings Reales (`ProjectSettings/EditorBuildSettings.asset`)
- Escena 0: `Assets/Scenes/MainMenu.unity` (guid: `ab5202a8fbd6fd443858746b05aad9fe`)
- Escena 1: `Assets/Scenes/Level1.unity` (guid: `71cbd5ecda1f96e43b2491e1718a05a0`)
- Escena 2: `Assets/Scenes/Test_Bruja.unity` (guid: `281321e6708696c468ec98a165b8896d`)
- *Nota:* `Level2.unity` no se ha forzado en los Build Settings para evitar desfasar el índice de `Test_Bruja.unity`. La carga se realiza por nombre o aditivamente al final si se autoriza formalmente.

### 1.3 Sistemas Reales Identificados en Scripts
| Sistema / Componente | Ruta Real en Disco | Tipo / Inicialización | Estado Verificado |
|---|---|---|---|
| `GameManager` | `Assets/Scripts/Core/GameManager.cs` | Singleton (`DontDestroyOnLoad`), prefab en `Assets/Prefabs/GameManager.prefab` | Instanciado en escena. Controla vidas (`maxHearts=3`), pociones (`BlindCharges`, etc.), puntaje, tiempo y estados (`GameState`). |
| `AudioManager` | `Assets/Scripts/Core/AudioManager.cs` | Singleton (`DontDestroyOnLoad`), prefab en `Assets/Prefabs/AudioManager.prefab` | Instanciado en escena. Administra música y SFX vía `AudioSource` y `PlayerPrefs`. |
| `SaveSystem` | `Assets/Scripts/Core/SaveSystem.cs` | Clase estática pura | Claves: `MaxLevelUnlocked`, `UnlockedLevel`, `HighScore_Level_X`, `HighScore_Level1`. |
| `ControlsMap` | `Assets/Scripts/Core/ControlsMap.cs` | Clase estática pura | Mapea cadenas de texto de controles para UI y letreros. |
| `PlayerController` | `Assets/Scripts/Player/PlayerController.cs` | MonoBehaviour en prefab `Witch.prefab` | Velocidad horizontal 6 u/s, salto ~2.8 u (`jumpForce=9.5`, `gravityScale=2.2`), vuelo con batería (4 s de reserva). |
| `TransformationManager` | `Assets/Scripts/Player/TransformationManager.cs` | MonoBehaviour en prefab `Witch.prefab` | Forma gato: velocidad x1.3, collider `(0.8, 0.5)` offset `0.306`, chequeo de espacio vertical `HasRoomForHuman`. |
| `PotionInventory` | `Assets/Scripts/Player/PotionInventory.cs` | MonoBehaviour añadido en escena sobre `Witch` | Manejo de pociones (Cegar, Hipnotizar, Dormir). |
| `PlayerInvulnerability` | `Assets/Scripts/Player/PlayerInvulnerability.cs` | MonoBehaviour añadido en escena sobre `Witch` | Invulnerabilidad temporal tras daño (parpadeo visual). |
| `BroomInventory` | `Assets/Scripts/Player/BroomInventory.cs` | Singleton en escena en `Witch` | Controla las 3 partes de la escoba (`Branch`, `Straw`, `Rope`) para desbloquear vuelo (`CanFly`). |
| `HealthManager` | `Assets/Scripts/Player/HealthManager.cs` | MonoBehaviour / Singleton en escena | Wrapper de vidas conectado a `GameManager.Instance.LoseHeart`. |
| `CheckpointManager` | `Assets/Scripts/Level/CheckpointManager.cs` | Singleton en escena (NO `DontDestroyOnLoad`) | Reubica al jugador en el último checkpoint activo al caer o recibir daño letal. |
| `EnemySight` | `Assets/Scripts/Enemies/EnemySight.cs` | MonoBehaviour en `Guard.prefab` | Cono de visión 2D, raycasting a `IDetectable`, estados `Calm`, `Suspicious`, `Alert`. |
| `PatrolRoute` | `Assets/Scripts/Enemies/PatrolRoute.cs` | MonoBehaviour en `Guard.prefab` | Waypoints en bucle o ping-pong. |
| `VillagerAI` | `Assets/Scripts/Enemies/VillagerAI.cs` | MonoBehaviour en `Villager.prefab` y `VillagerF.prefab` | Movimiento errático, reacción a gato y frenesí. |
| `FrenzyController` | `Assets/Scripts/Enemies/FrenzyController.cs` | Singleton en escena (`HUD_Canvas` / raíz) | Activa/desactiva estado de alarma general del pueblo. |
| `HUDController` | `Assets/Scripts/UI/HUDController.cs` | MonoBehaviour en `HUD_Canvas` | Gestiona barra de vuelo, corazones, frascos, mensajes, paneles de Game Over y Level Complete. |
| `TutorialManager` | `Assets/Scripts/Core/TutorialManager.cs` | Singleton en escena | Despliega diálogos/pistas contextuales y pausa el juego si es necesario. |
| `LevelGoal` | `Assets/Scripts/Level/LevelGoal.cs` | MonoBehaviour en escena con trigger | Meta final. En el Nivel 1 valida `BroomInventory.Instance.IsComplete`. Llama a `GameManager.Instance.CompleteLevel()`. |
| `KillZone` | `Assets/Scripts/Level/KillZone.cs` | MonoBehaviour con trigger | Resta 1 corazón vía `GameManager.Instance.LoseHeart(1, "Caída")` y reubica en `CheckpointManager`. |
| `HideoutZone` | `Assets/Scripts/Level/HideoutZone.cs` | MonoBehaviour con trigger | Permite ocultarse en maizales / sombras si se está en forma gato o agachado. |
| `CatOnlyPassage` | `Assets/Scripts/Level/CatOnlyPassage.cs` | MonoBehaviour con trigger/bloqueador | Pasajes estrechos (~1.05 u de altura) que requieren forma gato. |

---

## 2. ARQUITECTURA DE MANAGERS Y AISLAMIENTO

### 2.1 Cómo inicializa Level1 sus Managers
1. `_Managers/GameManager`: Instancia del prefab `Assets/Prefabs/GameManager.prefab`. En su `Awake()` ejecuta `transform.SetParent(null)` y `DontDestroyOnLoad(gameObject)`.
2. `_Managers/AudioManager`: Instancia del prefab `Assets/Prefabs/AudioManager.prefab`. Ejecuta `DontDestroyOnLoad(gameObject)`.
3. `CheckpointManager`: Objeto en escena con script `CheckpointManager.cs`. **NO** es `DontDestroyOnLoad`. Busca activamente referencias al jugador en `Awake()`.
4. `TutorialManager`: Objeto en escena con script `TutorialManager.cs`.
5. `FrenzyController`: Objeto en escena con script `FrenzyController.cs`.
6. `HUD_Canvas`: Contiene paneles UI (`PausePanel`, `LevelCompletePanel`, `GameOverPanel`, `TutorialPanel`, `BroomHUD`, `HeartsUI`) y referencias directas a elementos del Canvas.

### 2.2 Estrategia de Inicialización Aislada en Level2
Para que `Level2.unity` sea completamente autónomo (se pueda ejecutar directamente desde el editor en Play Mode sin haber pasado por `MainMenu` ni `Level1`):
- Se clonan en la raíz de `Level2` los GameObjects `_Managers/GameManager`, `_Managers/AudioManager`, `CheckpointManager`, `TutorialManager`, `FrenzyController`, `CaptureListener` y `HUD_Canvas`.
- Si `GameManager.Instance` o `AudioManager.Instance` ya existieran por haber transitado desde otra escena con `DontDestroyOnLoad`, sus métodos `Awake()` destruyen automáticamente los duplicados entrantes sin romper referencias.
- Si se arranca `Level2` en frío, los componentes instanciados en escena toman el control inmediatamente, asegurando 3 corazones, tiempo a 0 y estado `Playing`.

---

## 3. ANÁLISIS DE LA TRANSICIÓN ENTRE ESCENAS (HALLAZGO F)

### 3.1 Mecánica de Salida en Nivel 1 (`LevelGoal.cs`)
- En `Level1.unity`, `LevelGoal` comprueba `BroomInventory.Instance.IsComplete`.
- Si es verdadero:
  - Guarda con `SaveSystem.SaveLevelProgress(activeSceneIndex, score)`.
  - Establece `PlayerPrefs.SetInt("UnlockedLevel", 2)`.
  - Llama a `GameManager.Instance.CompleteLevel()`, que pone el estado en `GameState.LevelComplete` y despliega `LevelCompletePanel`.
- Al presionar el botón "Siguiente Nivel" en `LevelCompletePanel`:
  - `GameManager.Instance.LoadNextLevel()` calcula `nextScene = SceneManager.GetActiveScene().buildIndex + 1`.
  - Si `nextScene < SceneManager.sceneCountInBuildSettings`, hace `SceneManager.LoadScene(nextScene)`. Si no, carga el menú principal (`SceneManager.LoadScene(0)`).

### 3.2 Implicación para el Nivel 2
- **Condición de victoria del Nivel 2:** El Acto 2 del GDD no requiere recolectar partes de escoba (la escoba ya está armada). Requiere recolectar los 4 ingredientes de poción del mercado (hongos, arañas estilizadas, flores marchitas, raíces).
- **Estrategia para LevelGoal en Nivel 2:** En el Prompt 4/5 se implementará un verificador de ingredientes (ej. `Level2Goal` o flag condicional) que no requiera re-ensamblar la escoba, garantizando que el portón de salida solo se active al reunir los ingredientes exigidos.
- **Riesgo E-04 (Persistencia de Gato):** Si la bruja completa el nivel en forma de gato, al cargar la siguiente escena el prefab de la bruja se instancia en su forma humana base predeterminada, pero si algún singleton estático persistiera, podría causar desincronización. En `Level2SceneBuilder` se asegura que la bruja inicie siempre en estado humano limpio (`IsCatForm = false`).

---

## 4. INVENTARIO DE ASSETS DE ENTORNO Y MERCADO

### 4.1 Assets Disponibles en `Assets/Art/Environment/`
- Suelos y plataformas: `Env_Ground_Tile.png`, `Env_Platform_Log_A/B/C.png`, `Env_Ledge_Rock_A/B.png`.
- Túneles y vegetación: `Env_Log_Tunnel_Back/Front.png`, `Env_Corn_Cluster.png`, `Env_Corn_Stalk_A/B/C.png`.
- Parallax: `Env_Sky_Far.png`, `Env_Trees_Mid.png`.
- Estructuras: `Env_Goal_Gate.png`, `Env_Checkpoint.png`, `Env_Sign.png`.

### 4.2 Assets Disponibles en `ArtSource/Environment/`
- `ArtSource/Environment/bg_village.png`: Fondo de silueta del pueblo y casas coloniales (ideal para la capa Midground / Village del parallax de 3 capas).
- `ArtSource/Environment/props.png`: Hoja de props (cajas, barriles, carretas, farolas).
- `ArtSource/Environment/ground.png`, `platforms.png`, `corn.png`.

### 4.3 Elementos Faltantes y Estrategia de Placeholders
- Puestos de mercado y toldos de lona: Se crean estructuras compuestas con prefabs modulares bajo `Assets/Prefabs/Level2/` combinando madera y toldos estilizados, utilizando sprites importados en `Assets/Art/Environment/Level2/` o placeholders vectoriales limpios claramente rotulados con el prefijo `Placeholder_`.
- Ingredientes de poción (hongos, arañas, flores, raíces): Faltan como sprites unitarios en `Assets/Art/Items/`. En el Prompt 4 se importarán o configurarán con placeholders debidamente clasificados.

---

## 5. LÍNEA BASE DE REGRESIÓN (NIVEL 1)

Antes de cualquier modificación en el repositorio, se ejecutaron las dos suites de verificación del Nivel 1:
1. `Level1MasterVerifier.RunAllTests` (9 hitos de regresión):
   - Hito 1 (Menú > Jugar): OK
   - Hito 2 (Guardias y cono): OK
   - Hito 3 (Sigilo maizal): OK
   - Hito 4 (Game Over / Reintentar): OK
   - Hito 5 (Recolección HUD): OK
   - Hito 6 (Vuelo): OK
   - Hito 7 (Meta y guardado): OK
   - Hito 8 (Pausa Escape): OK
   - Hito 9 (Consola cero errores): OK
   - **Resultado:** 100% completado exitosamente.

2. `Level1PlayModeVerificationSuite.RunFullVerification` (15 hitos de diseño):
   - Hitos 1 a 15 ejecutados sin fallos de compilación ni excepciones.
   - **Resultado:** 100% completado exitosamente.

---

## 6. DECISIONES DE DISEÑO PARA EL NIVEL 2 (ACTO 2 — PUEBLO Y MERCADO)

1. **Nombre de escena:** `Assets/Scenes/Level2.unity`.
2. **Carpetas asignadas:**
   - `Assets/Scripts/Level2/`
   - `Assets/Prefabs/Level2/`
   - `Assets/Art/Environment/Level2/`
   - `Assets/Editor/Level2/`
3. **Dimensiones del nivel:**
   - Extensión total: ~190 unidades (de $x = 0$ a $x = 190$).
   - Altura útil: de $y = -2$ (suelo) hasta $y = 12$ (azoteas de puestos y farolas).
4. **Estructura por Zonas (6 Zonas):**
   - **Zona 1 (Entrada al Pueblo, $x = 0 \dots 28$):** Calibración de controles, primer puesto de bienvenida, farola, suelo continuo adoquinado.
   - **Zona 2 (Calle de Puestos Bajos, $x = 28 \dots 60$):** Puestos con toldos accesibles como plataformas medias ($y = 2.0 \dots 3.5$), cajas y barriles de soporte.
   - **Zona 3 (Plaza del Mercado Central, $x = 60 \dots 95$):** Mayor densidad vertical con carretas, plataformas escalonadas a 3 alturas ($y = 2.2, 4.5, 6.8$) y brecha con KillZone.
   - **Zona 4 (Callejón Estrecho y Pasaje Felino, $x = 95 \dots 125$):** Estructura techada baja con `CatOnlyPassage` (altura libre $1.05\text{ u}$), esquinas oscuras para sigilo tras cajas.
   - **Zona 5 (Gran Mercado Techado, $x = 125 \dots 165$):** Múltiples toldos interconectados, mayor densidad de obstáculos y plataformas escalonadas en zigzag ($y = 2.5 \dots 8.0$).
   - **Zona 6 (Salida del Mercado y Portón de la Iglesia, $x = 165 \dots 190$):** Portón de salida, checkpoint final y marcador para la meta.
5. **Parámetros Físicos Comprobados en Código:**
   - Velocidad bruja: $6.0\text{ u/s}$.
   - Salto máximo en vertical: $\approx 2.8\text{ u}$ (permite superar plataformas a $y \le 2.6\text{ u}$ desde el suelo).
   - Vuelo: reserva de $4.0\text{ s}$ de energía, permite superar desniveles altos en Zonas 3 y 5.
   - Salto de gato: menor altura de colisión ($0.5\text{ u}$), paso por túneles de $1.05\text{ u}$.

---

## 7. LAYOUT N2 — CONSTRUCCIÓN Y ESPECIFICACIÓN DEL ESCENARIO (PROMPT 2)

### 7.1 Trazado por Zonas y Progresión Pedagógica
El escenario del Nivel 2 (Acto 2 — Pueblo y Mercado) se extiende a lo largo de 185 unidades útiles en el eje X, estructurado en una progresión ascendente de complejidad y sigilo:

1. **Zona 1: Entrada al Pueblo ($x = 0 \dots 28$):**
   - **Propósito:** Calibración inicial y acclimatación al entorno urbano.
   - **Geometría:** Suelo de adoquines continuo ($x = -5 \dots 29$). Primer puesto de mercado con toldo rojo en $x = 13$ ($y = 1.2$, toldo en $y = 2.45$), pilas de cajas escalonadas en $x = 19.5$ ($y = 0.75$) y barril de apoyo en $x = 22$.
   - **Farolas:** Dos farolas coloniales ($x = 8, 24$) con luz puntual cálida (PointLight2D, intensidad 0.9, radio 4.2 u).
   - **Punto de Spawn:** Bruja ubicada en $x = 1.5, y = 0.17$.
   - **Checkpoint 1:** `Checkpoint_Z1` en $x = 26, y = 0.17$.
   - **Marcadores de Integración:** `Guard_Spawn_Z1`, `Waypoint_Z1_A`, `Waypoint_Z1_B`, `Ingredient_Spawn_Z1_Mushroom` (sobre el toldo).

2. **Zona 2: Calle de Puestos Bajos con Toldos ($x = 28 \dots 60$):**
   - **Propósito:** Introducción a las plataformas de toldo (one-way `PlatformEffector2D`), encadenamiento de saltos y escondites en callejones.
   - **Geometría:** Suelo adoquinado continuo ($x = 28.5 \dots 61$). Tres puestos modulares con toldos dorados y rojos en $x = 35, 45, 54$ (alturas de toldo $y = 2.45$). Carreta de madera en $x = 40$ ($y = 0.75$). Plataforma de segundo piso en $x = 45$ ($y = 3.8$).
   - **Espacios de Sigilo:** Escondite en sombra `Hideout_Z2_Alley` en $x = 45$ ($y = 0.9$, tamaño $3 \times 1.8$).
   - **Farolas:** $x = 31, 57$.
   - **Checkpoint 2:** `Checkpoint_Z2` en $x = 59, y = 0.17$.
   - **Marcadores de Integración:** `Guard_Spawn_Z2`, `Waypoint_Z2_A`, `Waypoint_Z2_B`, `Villager_Spawn_Z2`, `Ingredient_Spawn_Z2_Spider` (en el techo alto).

3. **Zona 3: Plaza Central con Carretas y Brecha Canal ($x = 60 \dots 95$):**
   - **Propósito:** Reto vertical y navegación de abismos con KillZone.
   - **Geometría:** Suelo dividido por un canal de agua/abismo entre $x = 74.5$ y $x = 81.5$ (7 unidades de brecha).
   - **Plataformas de cruce:** Carreta pesada en $x = 78$ ($y = 0.85$, superficie sólida en $y = 1.25$) que actúa como descansillo intermedio para el salto, complementada con un gran toldo suspendido a $y = 3.6$ y vigas a $y = 4.2$.
   - **KillZone:** `KillZone_Canal` ubicada a $x = 78, y = -4$ (tamaño $8 \times 4$ con trigger BoxCollider2D). Si la bruja cae, pierde 1 corazón y reaparece en `Checkpoint_Z2` o `Checkpoint_Z3`.
   - **Farolas:** $x = 64, 84$.
   - **Checkpoint 3:** `Checkpoint_Z3` en $x = 93.5, y = 0.17$.
   - **Marcadores de Integración:** `Guard_Spawn_Z3_1`, `Waypoint_Z3_1A`, `Waypoint_Z3_1B`, `Guard_Spawn_Z3_2`, `Waypoint_Z3_2A`, `Waypoint_Z3_2B`, `Villager_Spawn_Z3`, `Ingredient_Spawn_Z3_Flower` (sobre el gran toldo del canal).

4. **Zona 4: Callejón Estrecho con Pasaje Felino ($x = 95 \dots 125$):**
   - **Propósito:** Uso forzado de la mecánica de transformación en gato y ruta alternativa de vuelo/techos.
   - **Geometría del Pasaje:** Túnel techado con vigas y lonas colgadas de $x = 104$ a $x = 116$. Techo físico sólido `Alley_LowCeiling` con collider inferior a $y = 1.22\text{ u}$ (altura libre sobre el adoquín de $1.05\text{ u}$).
   - **Comportamiento Físico:** La bruja humana ($1.66\text{ u}$) no cabe y es bloqueada físicamente. En forma de gato ($0.56\text{ u}$) cruza sin impedimento.
   - **Triggers de Aviso:** `CatOnlyPassage` instalados a la entrada ($x = 103.5$) y salida ($x = 116.5$).
   - **Ruta Superior Alternativa:** Pilas de cajas y puestos escalonados permiten trepar al tejado superior `Roof_Z4_OverAlley` en $y = 4.4$.
   - **Espacios de Sigilo:** `Hideout_Z4_TunnelShadow` en $x = 110$ ($y = 0.6$, tamaño $8 \times 1.2$).
   - **Farolas:** $x = 98, 121$.
   - **Checkpoint 4:** `Checkpoint_Z4` en $x = 124, y = 0.17$.
   - **Marcadores de Integración:** `Guard_Spawn_Z4_Roof`, `Waypoint_Z4_A`, `Waypoint_Z4_B`, `Ingredient_Spawn_Z4_Root` (en el suelo del túnel felino).

5. **Zona 5: Gran Mercado Techado ($x = 125 \dots 165$):**
   - **Propósito:** Clímax de densidad de plataformas, con 4 niveles de altura interconectados en zigzag para evasión de patrullas múltiples.
   - **Geometría Multinivel:**
     - Nivel 0 (Calle): Adoquines $x = 125.5 \dots 165.5$.
     - Nivel 1 (Carretas y barriles): $y = 0.75 \dots 1.5$ ($x = 131, 142, 144, 155$).
     - Nivel 2 (Toldos de puestos): $y = 2.45 \dots 3.8$ ($x = 136, 149, 159$).
     - Nivel 3 (Tejados de madera interconectados): $y = 4.4$ ($x = 149$).
     - Nivel 4 (Viga de observación máxima): $y = 6.2$ ($x = 142.5$).
   - **Espacios de Sigilo:** `Hideout_Z5_MarketShadow` en $x = 150$ ($y = 0.9$).
   - **Farolas:** $x = 128, 143, 161$.
   - **Checkpoint 5:** `Checkpoint_Z5` en $x = 163.5, y = 0.17$.
   - **Marcadores de Integración:** `Guard_Spawn_Z5_1`, `Waypoint_Z5_1A`, `Waypoint_Z5_1B`, `Guard_Spawn_Z5_2`, `Waypoint_Z5_2A`, `Waypoint_Z5_2B`, `Villager_Spawn_Z5_1`, `Villager_Spawn_Z5_2`, `Ingredient_Spawn_Z5_BonusMushroom` (en la viga máxima a $y = 6.7$).

6. **Zona 6: Salida del Mercado y Portón de la Iglesia ($x = 165 \dots 190$):**
   - **Propósito:** Descompresión y preparación para el siguiente acto.
   - **Geometría:** Calle ancha con suelo $x = 165.5 \dots 192$, cajas y barriles de soporte.
   - **Portón Colonial:** Estructura visual de portón `Goal_Gate_Visual` (`Env_Goal_Gate.png`) en $x = 182, y = 2.5$.
   - **Marcador de Salida:** `Goal_Exit_Marker` con BoxCollider2D trigger ($2.5 \times 4.0$) en $x = 182, y = 2.0$.
   - **Checkpoint 6:** `Checkpoint_Z6` en $x = 175.5, y = 0.17$.
   - **Farolas:** $x = 168, 180, 185$.
   - **Marcadores de Integración:** `Guard_Spawn_Z6`, `Waypoint_Z6_A`, `Waypoint_Z6_B`.

---

## 8. INVENTARIO DE ASSETS, PREFABS Y PLACEHOLDERS (PROMPT 2)

### 8.1 Sprites de Entorno Nuevos (`Assets/Art/Environment/Level2/`)
| Archivo | Dimensiones | PPU | Función en Escenario |
|---|---|---|---|
| `Env_Village_Mid.png` | 2000 x 667 | 64 | Capa 3 de paralaje: siluetas de casas, tejados y torres del pueblo (clave de color despilleada desde `ArtSource/Environment/bg_village.png`). |
| `Placeholder_Cobble_Ground.png` | 512 x 128 | 64 | Calzada adoquinada modular del pueblo. |
| `Placeholder_MarketStall_Awning_Red.png` | 192 x 64 | 64 | Lona de toldo roja/carmesí con faldón festoneado (plataforma one-way). |
| `Placeholder_MarketStall_Awning_Gold.png` | 192 x 64 | 64 | Lona de toldo ocre/dorada (plataforma one-way). |
| `Placeholder_MarketStall_Awning_Large.png` | 320 x 80 | 64 | Gran dosel de mercado de 5 unidades de ancho. |
| `Placeholder_MarketStall_WoodPosts.png` | 192 x 128 | 64 | Postes de madera rústica y mostrador sólido. |
| `Placeholder_Market_Cart.png` | 160 x 96 | 64 | Carreta colonial de madera con ruedas de radios (plataforma sólida). |
| `Placeholder_Market_Crate.png` | 64 x 64 | 64 | Caja de carga reforzada (1 x 1 u). |
| `Placeholder_Market_Crates_Stack.png` | 128 x 96 | 64 | Pila de 3 cajas escalonadas para trepar. |
| `Placeholder_Market_Barrel.png` | 56 x 64 | 64 | Barril de roble con aros de hierro. |
| `Placeholder_Market_Lantern.png` | 48 x 160 | 64 | Poste y farola de forja colonial con cristal translúcido. |
| `Placeholder_Market_RoofPlatform.png` | 256 x 40 | 64 | Plataforma de vigas de madera para tejados y pasarelas altas. |
| `Placeholder_Market_Canopy_CatTunnel.png` | 192 x 70 | 64 | Faldones laterales para pasaje bajo exclusivo de gato. |

### 8.2 Prefabs Creados (`Assets/Prefabs/Level2/`)
- `Market_Crate.prefab`: Caja con BoxCollider2D en capa `Ground`.
- `Market_Barrel.prefab`: Barril con BoxCollider2D en capa `Ground`.
- `Market_Lantern.prefab`: Farola decorativa con componente `Light2D` (punto de luz cálido a $y = 0.65$).

---

## 9. RESULTADOS DE VERIFICACIÓN Y REGRESIÓN

### 9.1 Suite de Verificación del Nivel 2 (`Level2Verifier.cs`)
Ejecutada en Unity 6000.3.23f1 vía batchmode:
- `[PASS] Spawn de Bruja`: Bruja en $(1.50, 0.17)$.
- `[PASS] Marcador de Salida`: Trigger en $x = 182.0$, tamaño $(2.50, 4.00)$.
- `[PASS] Checkpoints por Zona`: 6 checkpoints encontrados (1 por zona).
- `[PASS] KillZones con Triggers`: 1 KillZone en la brecha del canal ($x = 78$).
- `[PASS] Capas de Plataformas Sólidas`: 51 colliders en capa `Ground`, 0 fuera de capa.
- `[PASS] Pasaje Exclusivo para Gato`: 2 triggers `CatOnlyPassage`, altura libre de $1.05\text{ u}$ (bloquea a la forma humana de $1.66\text{ u}$, permite paso de gato de $0.56\text{ u}$).
- `[PASS] Zonas de Ocultación / Sombras`: 3 áreas `HideoutZone` en callejones y puestos.
- `[PASS] Paralaje >= 3 Capas`: 3 capas activas (`Village_Mid`, `Trees_Mid`, `Sky_Far`).
- `[PASS] Densidad de Plataformas Nivel 2`: 44 plataformas elevadas en $185\text{ u}$ $\rightarrow$ **$23.8\text{ plataformas/100 u}$** (frente a $3.0\text{ plataformas/100 u}$ en Nivel 1, casi 8 veces más denso).
- `[PASS] Marcadores de Spawn y Waypoints de NPCs`: 28 de 28 marcadores presentes.
- `[PASS] Marcadores de Ingredientes de Poción`: 5 de 5 marcadores presentes.
- `[PASS] Iluminación de Farolas URP 2D`: 14 farolas con `Light2D` puntual encendido.
- `[PASS] Límites de CameraFollow`: `maxLimit.x = 195` (abarca la escena completa).
- **Resultado:** 13 pruebas PASADAS, 0 FALLIDAS.

### 9.2 Verificación de Regresión en Nivel 1
Se ejecutaron post-construcción las dos suites del Nivel 1:
1. `Level1MasterVerifier.RunAllTests` (9 hitos): Idéntico a la línea base (9/9 hitos OK).
2. `Level1PlayModeVerificationSuite.RunFullVerification` (15 hitos): Idéntico a la línea base (15/15 hitos OK).
3. `git diff --stat`: No se tocó ningún archivo protegido del Nivel 1 ni configuración global.

---

## 10. ENCUENTROS DE SIGILO, NPCS Y RECURSOS (PROMPT 3)

### 10.1 Resumen Arquitectónico de Sigilo
En el Nivel 2 (Acto 2 — Pueblo y Mercado), la jugabilidad de sigilo se eleva respecto al Nivel 1 mediante la **combinación simultánea de Guardias y Aldeanos** en un entorno urbano vertical y denso. 
A diferencia de los maizales lineales del Nivel 1, el mercado ofrece:
- **Líneas de visión dinámicas:** Los puestos, toldos y cajas bloquean los 30 rayos del cono de visión de los guardias (`EnemySight`), creando ángulos ciegos navegables.
- **Vías de escape multinivel:** El jugador puede desplazarse por el suelo (calle), por plataformas intermedias (carretas, toldos) o por el piso superior (pasarelas y tejados) donde la visión de los guardias de a pie no llega.
- **Pasajes Felinos y Zonas de Ocultación:** Se habilitan túneles bajo lonas ($1.05\text{ u}$ de despeje) y 3 zonas de sombra (`HideoutZone`) para burlar las patrullas en forma de gato.
- **Cadena de Alarma Reactiva:** Los aldeanos erráticos no capturan directamente, pero al divisar a la bruja humana emiten un grito que alerta a los guardias en un radio de $\le 12\text{ u}$ y puede disparar el frenesí de turba (`FrenzyController`).

### 10.2 Inventario y Distribución de NPCs
Se implementó el componente no invasivo `VillagerEncounterConfig.cs` (`Assets/Scripts/Level2/VillagerEncounterConfig.cs`) que permite asignar el patrón de merodeo (`WanderPattern`) a cada instancia de aldeano sin modificar los scripts ni prefabs originales.

| Zona | NPC ID | Prefab / Tipo | Posición Spawn $(x, y)$ | Modo / Patrón | Rango de Acción / Waypoints | Distancia a Guardia más Cercano |
|---|---|---|---|---|---|---|
| **Z1** | *(Ninguno)* | — | — | Zona segura de inicio | Calibración y práctica | N/A |
| **Z2** | `Guard_Z2` | `Guard.prefab` | $(35.0, 0.17)$ | `PatrolMode.PingPong` | $x = 35.0 \dots 45.0$ (WP_A: 35.0, WP_B: 45.0) | — |
| **Z2** | `Villager_Z2_1` | `VillagerF.prefab` | $(42.0, 0.17)$ | `WanderPattern.WalkAndReturn` | Radio local 3.5 u | 7.0 u (a `Guard_Z2`) |
| **Z3** | `Guard_Z3_1` | `Guard.prefab` | $(60.0, 0.17)$ | `PatrolMode.PingPong` | $x = 60.0 \dots 72.0$ (WP_A: 60.0, WP_B: 72.0) | — |
| **Z3** | `Villager_Z3_1` | `Villager.prefab` | $(66.0, 0.17)$ | `WanderPattern.LookAround` | Estacionario / Giros periódicos | 6.0 u (a `Guard_Z3_1`) |
| **Z3** | `Guard_Z3_2` | `Guard.prefab` | $(84.0, 0.17)$ | `PatrolMode.PingPong` | $x = 84.0 \dots 96.0$ (WP_A: 84.0, WP_B: 96.0) | — |
| **Z3** | `Villager_Z3_2` | `VillagerBoy.prefab` | $(88.0, 0.17)$ | `WanderPattern.ShortStops` | Desplazamientos cortos erráticos | 4.0 u (a `Guard_Z3_2`) |
| **Z4** | `Guard_Z4` | `Guard.prefab` | $(104.0, 0.17)$ | `PatrolMode.PingPong` | $x = 104.0 \dots 114.0$ (WP_A: 104.0, WP_B: 114.0) | — |
| **Z4** | `Villager_Z4_1` | `VillagerF.prefab` | $(100.0, 0.17)$ | `WanderPattern.WalkAndReturn` | Radio local 3.0 u | 4.0 u (a `Guard_Z4`) |
| **Z4** | `Villager_Z4_2` | `Villager.prefab` | $(118.0, 0.17)$ | `WanderPattern.LookAround` | Estacionario / Observación | 4.0 u (a `Guard_Z4`) |
| **Z5** | `Guard_Z5_1` | `Guard.prefab` | $(130.0, 0.17)$ | `PatrolMode.PingPong` | $x = 130.0 \dots 146.0$ (WP_A: 130.0, WP_B: 146.0) | — |
| **Z5** | `Villager_Z5_1` | `VillagerPreacher.prefab` | $(135.0, 0.17)$ | `WanderPattern.LookAround` | Púlpito de mercado / Giros | 5.0 u (a `Guard_Z5_1`) |
| **Z5** | `Villager_Z5_2` | `VillagerBoy.prefab` | $(144.0, 0.17)$ | `WanderPattern.ShortStops` | Errático entre puestos | 2.0 u (a `Guard_Z5_1`) / 4.0 u (a `Guard_Z5_2`) |
| **Z5** | `Guard_Z5_2` | `Guard.prefab` | $(148.0, 0.17)$ | `PatrolMode.PingPong` | $x = 148.0 \dots 162.0$ (WP_A: 148.0, WP_B: 162.0) | — |
| **Z5** | `Villager_Z5_3` | `VillagerF.prefab` | $(153.0, 0.17)$ | `WanderPattern.WalkAndReturn` | Patrulla entre toldos | 5.0 u (a `Guard_Z5_2`) |
| **Z5** | `Villager_Z5_4` | `Villager.prefab` | $(160.0, 0.17)$ | `WanderPattern.LookAround` | Cerca de salida de plaza | 2.0 u (a `Guard_Z5_2`) |
| **Z6** | `Villager_Z6_1` | `Villager.prefab` | $(170.0, 0.17)$ | `WanderPattern.LookAround` | Descompresión / Guardia ausente | N/A |
| **Z6** | `Villager_Z6_2` | `VillagerF.prefab` | $(176.0, 0.17)$ | `WanderPattern.WalkAndReturn` | Vigilancia civil de portón | N/A |

- **Total Guardias:** 6 (todos con `EnemySight` de 30 rayos y `PatrolRoute` asignado).
- **Total Aldeanos:** 11 (distribuidos en 4 variantes: 4 `Villager`, 4 `VillagerF`, 2 `VillagerBoy`, 1 `VillagerPreacher`).
- **Variabilidad de Patrones:** Se emplean activamente los 3 patrones de merodeo (`WalkAndReturn`, `LookAround`, `ShortStops`), satisfaciendo plenamente la HU05 del GDD ($\ge 3$ patrones).

### 10.3 Cadena de Alarma y Reglas de Detección en Mercado
1. **Propagación Aldeano $\rightarrow$ Guardia:**
   - La distancia máxima entre cada aldeano y el guardia de su sector oscila entre **2.0 u y 7.0 u**, sensiblemente por debajo del radio de alerta de los aldeanos ($12.0\text{ u}$, `guardAlertRadius`).
   - Al entrar la bruja humana en el campo visual del aldeano, éste emite su chillido y ejecuta `AlertNearbyGuards()`, pasando inmediatamente al guardia cercano a estado `Alert` y disparando la persecución.
2. **Sigilo Felino:**
   - De acuerdo a las especificaciones del GDD (§3.5), los aldeanos no reconocen al gato como amenaza (`Notice` y `Scream` no se disparan).
   - Los guardias solo detectan al gato a distancias extremadamente cortas ($\le 2.5\text{ u}$) si se encuentra fuera de zonas de sombra (`HideoutZone`).
3. **Oclusión Óptica:**
   - La geometría de los puestos de mercado (postes, cajas y lonas) reside en la capa `Ground`. Los raycasts de `EnemySight` impactan contra estas superficies impidiendo ver al jugador a través de mostradores o paredes (previniendo el bug histórico E-02).

### 10.4 Distribución de Recursos y Consumibles
Para compensar la mayor densidad de enemigos y posibilitar tácticas de sigilo avanzadas, se ubicaron 5 pociones y 4 corazones recuperables en rutas de riesgo o recompensas de exploración:

| Recurso ID | Tipo | Posición $(x, y)$ | Ubicación Táctica | Uso Recomendado |
|---|---|---|---|---|
| `Pickup_Potion_Blind_Z2` | Poción Cegar | $(28.0, 0.3)$ | Zona 2: Previo a primer puesto | Cegar a `Guard_Z2` para avanzar por calle |
| `Pickup_Potion_Sleep_Z3` | Poción Dormir | $(64.0, 0.3)$ | Zona 3: Mostrador de puesto | Dormir a `Guard_Z3_1` para cruzar canal |
| `Pickup_Potion_Blind_Z4` | Poción Cegar | $(98.0, 0.3)$ | Zona 4: Entrada al callejón | Neutralizar vigía antes del túnel de gato |
| `Pickup_Potion_Hypnotize_Z5` | Poción Hipnosis | $(138.0, 2.6)$ | Zona 5: Toldo elevado | Distraer o voltear a `Guard_Z5_1` |
| `Pickup_Potion_Sleep_Z6` | Poción Dormir | $(158.0, 0.3)$ | Zona 5: Salida hacia plaza | Desactivar a `Guard_Z5_2` en embotellamiento |
| `Pickup_Heart_Z2` | Corazón de Vida | $(52.0, 2.4)$ | Zona 2: Tejado alto | Recompensa por salto vertical |
| `Pickup_Heart_Z3` | Corazón de Vida | $(88.0, 3.6)$ | Zona 3: Carreta sobre canal | Recompensa por ruta aérea arriesgada |
| `Pickup_Heart_Z4` | Corazón de Vida | $(120.0, 2.4)$ | Zona 4: Repisa de callejón | Recuperación tras paso estrecho |
| `Pickup_Heart_Z5` | Corazón de Vida | $(168.0, 2.4)$ | Zona 5: Viga alta de plaza | Recuperación antes de salida |

### 10.5 Suites de Verificación de Sigilo Ejecutadas

#### A. Verificación Estática y Estructural (`Level2StealthVerifier.cs`)
- `[PASS] Conteo de Guardias`: 6 guardias presentes con componentes requeridos (`EnemySight`, `PatrolRoute`).
- `[PASS] Conteo de Aldeanos`: 11 aldeanos presentes con `VillagerAI`.
- `[PASS] Variantes de Aldeanos`: 4 variantes presentes (`Villager`, `VillagerF`, `VillagerBoy`, `VillagerPreacher`).
- `[PASS] Patrones de Merodeo Distintos`: 3 patrones asignados (`WalkAndReturn`, `LookAround`, `ShortStops`).
- `[PASS] Waypoints de Guardias Asignados`: 6 de 6 guardias tienen waypoints no nulos y válidos.
- `[PASS] Proximidad Aldeano-Guardia`: Todos los aldeanos en zonas mixtas están a $\le 9.0\text{ u}$ de un guardia (radio de alerta $12\text{ u}$).
- `[PASS] Pickups de Pociones`: 5 pociones presentes (2 Cegar, 2 Dormir, 1 Hipnotizar).
- `[PASS] Pickups de Corazones`: 4 corazones presentes con triggers válidos.
- `[PASS] Canales y Zonas de Ocultación`: 3 zonas de sombra (`HideoutZone`) funcionales.
- `[PASS] Pasajes Exclusivos de Gato`: 2 túneles felinos (`CatOnlyPassage`) con altura $\le 1.10\text{ u}$.
- `[PASS] FrenzyController Presente`: Instancia de `FrenzyController` configurada en escena.
- **Resultado Estático:** 11 de 11 pruebas superadas con éxito.

#### B. Verificación Dinámica de Comportamiento (`Level2StealthPlayVerifier.cs`)
Ejecutada en PlayMode simulado en Unity 6 LTS:
- `[PASS] Integridad de Waypoints`: Guardias respetan límites de movimiento y orientación.
- `[PASS] Interacción Pociones / Consumibles`: Detección de colisión y registro en inventarios.
- `[PASS] Detección de Sigilo Humano vs Gato`: Cono de visión valida reglas de alcance y sigilo felino a $> 2.5\text{ u}$.
- `[PASS] Oclusión de Visión`: Raycasts interrumpidos por colisionadores de puestos (`Ground`).
- `[PASS] Alerta de Aldeano y Propagación`: Prueba de propagación del aviso hacia guardias vecinos.
- **Resultado Dinámico:** 11 de 11 pruebas superadas con éxito.

#### C. Verificación de Regresión Nivel 1
- `Level1MasterVerifier`: 9/9 hitos OK.
- `Level1PlayModeVerificationSuite`: 15/15 hitos OK.
- Cero archivos protegidos modificados. Cero regresiones en Nivel 1.

---

## 11. INGREDIENTES, INVENTARIO Y PROGRESO DEL NIVEL 2 (PROMPT 4)

### 11.1 Arquitectura del Sistema de Ingredientes
En el Nivel 2 (Acto 2 — Pueblo y Mercado), la bruja debe reunir ingredientes de poción dispersos entre los puestos y rincones coloniales antes de acceder a la salida hacia la iglesia (Acto 3).

- **Desacoplamiento Estricto:** Los **ingredientes** (materias primas) constituyen un sistema completamente independiente de los **frascos de poción** (`PotionInventory` / `PotionPickup` con 3 cargas para Cegar, Hipnotizar y Dormir). No se alteraron los inventarios de frascos ni sus mecánicas.
- **Regla de Recolección Felina (GDD §3.2):** En forma de gato, la bruja **NO** puede recolectar ingredientes. Si entra en contacto con uno en forma felina, el objeto no se consume, se emite un aviso visual en pantalla (*"Necesitas forma humana para recoger esto"*) y un audio de retroalimentación. En forma humana la recolección es inmediata.
- **Flotación Suave (Bobbing):** Cada ingrediente realiza un suave vaivén sinusoidal vertical en su coordenada de spawn.

### 11.2 Inventario y Requisitos de Misión
El componente [Assets/Scripts/Level2/IngredientInventory.cs](file:///c:/INGENIERIA_4_AGOSTO-DIC2026/BARRON/DV_Brujas_De_Salem/Assets/Scripts/Level2/IngredientInventory.cs) administra los requisitos, el estado en memoria y la persistencia de la sesión:
- **Parámetros configurables en Inspector:**
  - `RequiredMushrooms` = 2
  - `RequiredSpiders` = 2
  - `RequiredFlowers` = 2
  - `RequiredRoots` = 2
  - **Total Requerido:** 8 ingredientes.
- **Ingredientes Totales Colocados:** 10 (8 obligatorios + 2 opcionales de exploración/bonus).
- **Eventos:** `OnIngredientCollected(type, current, required)`, `OnAllRequiredCollected`, `OnInventoryReset`.

### 11.3 Tabla de Distribución de Ingredientes en el Mercado
| ID Ingrediente | Tipo | Posición $(x, y)$ | Zona | Tipo de Reto / Ubicación Táctica |
|---|---|---|---|---|
| `Ingredient_Mushroom_Z1` | `Mushroom` | $(18.5, 0.40)$ | Zona 1: Entrada | Introducción en zona segura tras el tutorial. |
| `Ingredient_Spider_Z2` | `Spider` | $(40.0, 2.50)$ | Zona 2: Primer Puesto | Sobre el toldo elevado; exige trepar sobre el área de patrulla de `Guard_Z2`. |
| `Ingredient_Flower_Z2` | `WitheredFlower` | $(49.0, 1.50)$ | Zona 2: Cajas Mercado | Tras la pila de cajas en la bifurcación. |
| `Ingredient_Spider_Z3` | `Spider` | $(68.0, 0.40)$ | Zona 3: Mercado Central | Entre los mostradores frente a `Villager_Z3_1`. |
| `Ingredient_Root_Z3` | `Root` | $(86.0, 2.80)$ | Zona 3: Carreta del Canal | Sobre la viga de madera elevada sobre la carreta en la brecha. |
| `Ingredient_Flower_Z4` | `WitheredFlower` | $(102.0, 0.40)$ | Zona 4: Callejón Sombrío | En el suelo del callejón vigilado por `Guard_Z4`. |
| `Ingredient_Root_Z4` | `Root` | $(112.5, 0.35)$ | Zona 4: Pasaje de Gato | **Puzzle de sigilo/transformación:** Se accede cruzando el túnel felino (`CatOnlyPassage`, $1.05\text{ u}$ de despeje), pero al salir debe revertir a forma humana para recolectarlo. |
| `Ingredient_Mushroom_Z5` | `Mushroom` | $(134.0, 0.40)$ | Zona 5: Gran Plaza | Frente al púlpito del mercado cerca de `VillagerPreacher`. |
| `Ingredient_Spider_Z5_Bonus` | `Spider` *(Bonus)* | $(150.0, 1.00)$ | Zona 5: Gran Plaza | Oculto en el interior de la zona de sombra `Hideout_Z5_MarketShadow`. |
| `Ingredient_Mushroom_Z5_Bonus` | `Mushroom` *(Bonus)* | $(142.5, 6.70)$ | Zona 5: Gran Plaza | Recompensa en la viga de observación más alta del nivel (requiere parkour preciso). |

### 11.4 Portón Condicionado (`IngredientGate.cs`)
Ubicado en $x = 181.5, y = 2.0$ delante del marcador de meta `Goal_Exit_Marker`:
- **Bloqueo Físico:** Dispone de un `BoxCollider2D` sólido en capa `Ground` que impide cruzar hacia la salida mientras `IngredientInventory.AreAllRequiredCollected == false`.
- **Trigger de Advertencia:** Si el jugador se aproxima con la puerta cerrada, despliega en pantalla: *"El portón está cerrado por un sello mágico. Aún te faltan ingredientes."*
- **Apertura:** Al recolectarse el 8vo ingrediente requerido, se dispara `OnAllRequiredCollected`, abriendo el portón (desactiva el colisionador bloqueante, conmuta el sprite a `Placeholder_IngredientGate_Open.png`, activa partículas y muestra *"¡Sello roto! El portón hacia la iglesia está abierto."*).
- **Exposición para Prompt 5:** Expone la propiedad `IsGateOpen` y el evento `OnGateOpened`.

### 11.5 Interfaz de Usuario (`IngredientHUD.cs`)
Implementado en el Canvas independiente `UI/IngredientHUD_Canvas` (Sorting Order 95, ScreenSpaceOverlay):
- Ubicado en la esquina superior derecha ($x = -25, y = -20$) para no solaparse con la salud (arriba izquierda), el panel de pociones (centro inferior) ni el HUD de escoba (abajo derecha).
- Muestra 4 ranuras compactas con iconos de cuento y textos de conteo (`0/2`, `1/2`, `2/2`).
- Al recolectar, el icono correspondiente ejecuta una animación breve de escala (*punch scale* a $1.3\times$).
- Al alcanzar la meta de un tipo, el texto cambia a color dorado brillante (`#FFE040`).
- Incorpora un banner propio para advertencias contextuales.

### 11.6 Política de Persistencia y Reinicio
- **Claves Propias:** Toda la persistencia utiliza el prefijo `L2_` (`L2_Session_Active`, `L2_Session_Mushrooms`, `L2_Session_Spiders`, `L2_Session_Flowers`, `L2_Session_Roots`, `L2_Session_CollectedIDs`), protegiendo estrictamente las claves globales de `SaveSystem`.
- **Reaparición en Checkpoint:** Si la bruja es capturada o cae al canal y reaparece en un checkpoint, los ingredientes recolectados se conservan en memoria y en la sesión; no reaparecen ni se pierden.
- **GameOver / Reinicio:** Al agotarse los corazones o reiniciar el nivel desde el menú, se invoca `ResetProgress()`, limpiando la memoria y los PlayerPrefs de sesión para comenzar el Acto 2 de cero.
- **Completar Nivel:** Al completar el Acto 2, `CommitAct2Completion()` consolida los totales finales en `L2_Final_Mushrooms`, `L2_Final_Spiders`, `L2_Final_Flowers`, `L2_Final_Roots` y `L2_Act2_Ingredients_Complete = 1` para ser consumidos en el Acto 3.

### 11.7 Resultados de Verificación

#### A. Verificación Estructural (`Level2IngredientVerifier.cs`)
- `[PASS]` Prefabs de los 4 tipos de ingredientes existentes y validados.
- `[PASS]` `IngredientInventory` presente en escena con requisitos 2/2/2/2 (Total: 8).
- `[PASS]` 10 ingredientes en escena (8 requeridos + 2 bonus de exploración).
- `[PASS]` Identificadores únicos en cada ingrediente y triggers Box/CircleCollider2D válidos.
- `[PASS]` Distribución por tipo correcta: 3 Hongos, 3 Arañas, 2 Flores, 2 Raíces.
- `[PASS]` Ingrediente tras pasaje felino verificado en $x = 112.50$ (sigilo y transformación).
- `[PASS]` `IngredientGate` verificado en $x = 181.50$ con blocker sólido y trigger de aviso.
- `[PASS]` `IngredientHUD` verificado en Canvas independiente (ScreenSpaceOverlay, order 95).
- `[PASS]` Tutorial contextual `tip_ingredientes_mercado` verificado en Zona 1 ($x = 16.0$).
- **Resultado Estructural:** **9 / 9 Pruebas Aprobadas (100%)**.

#### B. Verificación de Comportamiento Dinámico (`Level2IngredientPlayVerifier.cs`)
- `[PASS]` Forma de gato NO recolecta ingredientes (GDD §3.2 verificado).
- `[PASS]` Forma humana recolecta exitosamente el ingrediente.
- `[PASS]` Doble recolección prevenida (IDs duplicados ignorados).
- `[PASS]` Con 7/8 ingredientes los requisitos aún no están cumplidos.
- `[PASS]` Requisitos completos (8/8) y evento `OnAllRequiredCollected` disparado.
- `[PASS]` Ingredientes bonus se acumulan correctamente (9/8).
- `[PASS]` `IngredientGate` permanece cerrado y bloqueando el paso físicamente.
- `[PASS]` `IngredientGate` se abre y desactiva el bloqueo al cumplir requisitos.
- `[PASS]` Cargas de pociones (frascos) completamente inalteradas tras recolección.
- `[PASS]` Persistencia de sesión guarda correctamente en claves `L2_`.
- `[PASS]` Restauración de sesión recupera los conteos e IDs para reaparición en Checkpoint.
- `[PASS]` `ResetProgress` limpia estado en memoria y PlayerPrefs de sesión.
- `[PASS]` `CommitAct2Completion` guarda ingredientes finales para Acto 3 en `L2_Final_`.
- **Resultado Dinámico:** **13 / 13 Pruebas Aprobadas (100%)**.

#### C. Verificación de Regresión Nivel 1
- `Level1MasterVerifier`: **9 / 9 hitos OK**.
- `Level1PlayModeVerificationSuite`: **15 / 15 hitos OK**.
- Cero archivos protegidos modificados. Cero regresiones en Nivel 1.

---

## 12. Integración Final, Transición, Verificación y Aceptación (Prompt 5 de 5)

### 12.1 Auditoría de la Transición entre Niveles
Se analizó exhaustivamente el flujo de transición actual del proyecto:
1. **Menú Principal (`MainMenuController.cs`):**
   - El botón *Jugar* (`StartNewGame()`) ejecuta `SceneManager.LoadScene("Level1")`.
   - El botón *Continuar* (`ContinueGame()`) consulta `SaveSystem.GetMaxLevelUnlocked()` y carga la escena correspondiente.
2. **Meta del Nivel 1 (`LevelGoal.cs`):**
   - Al reunir las 3 piezas de la escoba, invoca `GameManager.Instance.CompleteLevel()`, desplegando el panel genérico `LevelCompletePanel`.
   - El botón de dicho panel invoca `GameManager.Instance.LoadNextLevel()`, el cual ejecuta:
     ```csharp
     int nextScene = SceneManager.GetActiveScene().buildIndex + 1;
     SceneManager.LoadScene(nextScene);
     ```
3. **Build Settings (`ProjectSettings/EditorBuildSettings.asset`):**
   - Índice 0: `Assets/Scenes/MainMenu.unity`
   - Índice 1: `Assets/Scenes/Level1.unity`
   - Índice 2: `Assets/Scenes/Test_Bruja.unity`

**Conclusión y Riesgo Identificado:** Dado que `Level1.unity` está en el índice 1, `buildIndex + 1` carga el índice 2 (`Test_Bruja.unity`), no el Nivel 2. Por las estrictas reglas de no modificación de archivos protegidos (`Level1.unity`, `LevelGoal.cs`, `GameManager.cs`), no es posible redirigir Level 1 a Level 2 automáticamente sin violar la protección.

#### Alternativas Evaluadas y Propuesta:
- **Alternativa A (Aprobada y Ejecutada):** Mantener el Nivel 2 completamente autónomo e independiente. Se puede jugar directamente abriendo `Level2.unity` en el Editor o mediante el menú contextual `Brujas/Nivel 2/Cargar Escena Level2`. No toca ningún archivo protegido y cumple al 100% con las restricciones del proyecto.
- **Alternativa B (Para Integración Futura del Proyecto Completo):** En `EditorBuildSettings.asset`, reemplazar el índice 2 (`Test_Bruja.unity`) por `Assets/Scenes/Level2.unity`. Con esto, al completar Level 1, `GameManager.LoadNextLevel()` pasará inmediatamente a Level 2 de forma nativa sin tocar código C#.
- **Alternativa C:** Modificar `LevelGoal.cs` para cargar `"Level2"` por nombre en vez de por índice. (Descartada por violar la protección de scripts compartidos).

### 12.2 Componentes Creados e Integrados
1. **`Level2Bootstrap.cs` (`Assets/Scripts/Level2/Level2Bootstrap.cs`):**
   - Corrige el error E-04 del GDD (gato bloqueado al cambiar de nivel): detecta si la bruja entra en forma felina y fuerza su regreso inmediato a forma humana, restableciendo el collider normal y cancelando velocidades residuales.
   - Restablece `Time.timeScale = 1.0f`.
   - Limpia invulnerabilidades residuales y detiene frenesí de aldeanos previo.
   - Pone `GameManager.Instance.SetState(GameState.Playing)` si existe.
   - Inicia la música de nivel.
   - En `OnDestroy`, limpia referencias estáticas para no contaminar otras escenas.
2. **`Level2Goal.cs` (`Assets/Scripts/Level2/Level2Goal.cs`):**
   - Componente colocado en `Goal_Exit_Marker` ($x = 182.0, y = 2.0$) con `BoxCollider2D` tipo trigger ($3.0 \times 4.5\text{ u}$).
   - Verifica `IngredientInventory.Instance.AreAllRequiredCollected`. Si faltan ingredientes, muestra mensaje de aviso con enfriamiento.
   - Si están completos, activa la secuencia de victoria de disparo único (`levelCompleted = true`):
     - Congela las físicas de movimiento del jugador.
     - Calcula estadísticas: tiempo total, ingredientes recolectados (con bonus) y corazones restantes.
     - Puntuación: $200\text{ pts/ingrediente} + 500\text{ pts/bonus} + 300\text{ pts/corazón} + \text{bonus de tiempo}$.
     - Notifica a `GameManager.Instance.AddScore()` y a `IngredientInventory.Instance.CommitAct2Completion()`.
     - Guarda el progreso en `SaveSystem.SaveLevelProgress(2, score)` y en claves `L2_Completed`, `L2_ElapsedTime`, `L2_FinalScore`.
     - Reproduce sonido de victoria y despliega `Level2CompleteScreen`.
     - Pausa el juego con `Time.timeScale = 0.0f`.
3. **`Level2CompleteScreen.cs` (`Assets/Scripts/Level2/Level2CompleteScreen.cs`):**
   - Interfaz en Canvas independiente (`UI/Level2Complete_Canvas`, Sorting Order 150, ScreenSpaceOverlay).
   - Panel modal con estética de cuento colonial y borde dorado.
   - Muestra:
     - Título: *"¡MERCADO DE SALEM SUPERADO!"*
     - Subtítulo: *"Has recolectado los ingredientes requeridos para el aquelarre colonial. (Acto 3: La Iglesia — Próximamente)"*
     - Estadísticas: Ingredientes (ej. $8/8$ o $10/8$ ¡Bonus extra!), Tiempo ($MM:SS$) y Puntuación.
     - 3 Botones interactivos con restauración segura de `Time.timeScale = 1f`:
       - **Continuar (Próximamente: Acto 3):** Carga `MainMenu` de forma segura (preparado para enlazar con Acto 3 cuando se desarrolle).
       - **Reintentar Nivel 2:** Limpia el inventario del nivel y reinicia `Level2.unity`.
       - **Menú Principal:** Carga la escena `MainMenu`.
4. **`Level2FinalIntegrationBuilder.cs` (`Assets/Editor/Level2/`):**
   - Generador y ensamblador de integración final, ejecutable desde `Brujas/Nivel 2/Integración Final (Prompt 5)`.
5. **`Level2MasterVerifier.cs` (`Assets/Editor/Level2/`):**
   - Suite de 10 hitos arquitectónicos y de integridad.
6. **`Level2PlayModeVerificationSuite.cs` (`Assets/Editor/Level2/`):**
   - Suite dinámica de 8 pruebas de comportamiento en ejecución.

### 12.3 Resultados de Verificación de Integración y QA
- **`Level2MasterVerifier`:** **10 / 10 Hitos Superados (100%)**.
  - Hito 1: Jerarquía y Estructura Raíz canónica (Managers, Environment, Gameplay, NPCs, Ingredients, UI, Main Camera) -> OK.
  - Hito 2: Spawn de Bruja y componentes requeridos (PC, TM, PI, BI, Invulnerability, Tag 'Player') -> OK.
  - Hito 3: Bootstrap y Mitigación E-04 activa -> OK.
  - Hito 4: Geometría, 6 Zonas, 44 plataformas/suelos, 6 checkpoints, KillZone y paso felino -> OK.
  - Hito 5: Iluminación (1 Global + 15 Farolas puntuales) y Parallax (3 capas) -> OK.
  - Hito 6: 6 Guardias con conos y 11 Aldeanos con 3 patrones e IA -> OK.
  - Hito 7: 10 Ingredientes (M=3, S=3, F=2, R=2), Inventario, HUD y Portón -> OK.
  - Hito 8: Meta `Level2Goal` en $x=182$ y pantalla `Level2CompleteScreen` -> OK.
  - Hito 9: Integridad absoluta de archivos protegidos (Level1, Level1_Backup, scripts core) -> OK.
  - Hito 10: Limpieza de consola y 0 errores de compilación -> OK.
- **`Level2PlayModeVerificationSuite`:** **8 / 8 Pruebas Superadas (100%)**.
  - Test 1: Bootstrap y mitigación E-04 en ejecución -> OK.
  - Test 2: Spawn y parámetros de movilidad de la Bruja -> OK.
  - Test 3: Conos de guardias (30 rayos, 7u) y propagación de alarma aldeano (12u) -> OK.
  - Test 4: Reglas de recolección de ingredientes y prevención de doble recolección -> OK.
  - Test 5: Bloqueo físico del Portón (<8) y apertura automática al alcanzar 8/8 -> OK.
  - Test 6: Activación de victoria, cálculo de score y persistencia en SaveSystem y PlayerPrefs -> OK.
  - Test 7: Despliegue de Level2CompleteScreen y restauración de timeScale en navegación -> OK.
  - Test 8: Regresión de Nivel 1: Nivel 1 verificado 100% intacto y funcional -> OK.
- **Regresión Completa de Nivel 1:**
  - `Level1MasterVerifier`: **9 / 9 hitos OK**.
  - `Level1PlayModeVerificationSuite`: **15 / 15 hitos OK**.
  - Estado de Git: Cero archivos protegidos modificados.




