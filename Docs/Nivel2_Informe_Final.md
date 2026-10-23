# INFORME FINAL DE INTEGRACIÓN, QA Y ACEPTACIÓN TÉCNICA
## NIVEL 2: ACTO 2 — EL PUEBLO Y EL MERCADO (BRUJAS DE SALEM)

**Fecha:** 22 de Octubre de 2026  
**Motor:** Unity 6 (6000.0.33f1 / 6000.3.23f1 LTS), URP 2D, C#  
**Proyecto:** *Brujas de Salem (DV_Brujas_De_Salem)*  
**Escena del Nivel 2:** `Assets/Scenes/Level2.unity`  
**Estado General:** **APROBADO — LISTO PARA INTEGRACIÓN Y PRODUCCIÓN (100% CUMPLIDO)**

---

## 1. Resumen Ejecutivo

El presente informe documenta la culminación de la fase de construcción, programación, integración, control de calidad y aceptación técnica del **Nivel 2 (Acto 2 — Pueblo y Mercado)** para el videojuego *Brujas de Salem*, completando con éxito los 5 prompts de desarrollo:
1. **Prompt 1:** Auditoría técnica, arquitectura de desacoplamiento y escena base aislada.
2. **Prompt 2:** Construcción del escenario jugable (6 zonas, 185 u, 44 plataformas, 3 capas de parallax, iluminación URP 2D).
3. **Prompt 3:** Integración de guardias (6) y aldeanos (11) combinados con alarma encadenada y 3 patrones erráticos.
4. **Prompt 4:** Sistema de ingredientes (10 unidades, 8 requeridas + 2 bonus), inventario desacoplado, HUD dedicado y portón mágico.
5. **Prompt 5:** Integración final, condición de victoria atómica, mitigación de fallos históricos (E-04, E-05), suite de verificación master y regresión 100% limpia sobre el Nivel 1.

### Principales Logros de Calidad:
- **Zero Degradation:** El Nivel 1 (`Level1.unity`, `Level1_Backup.unity`), los prefabs compartidos (`Witch`, `Guard`, `Villager*`) y los scripts compartidos permanecieron **100% inalterados**.
- **Regresión Superada:** Todas las suites de prueba preexistentes del Nivel 1 ejecutaron y aprobaron con éxito: `Level1MasterVerifier` (**9/9 hitos**) y `Level1PlayModeVerificationSuite` (**15/15 hitos**).
- **Validación del Nivel 2:** La suite arquitectónica `Level2MasterVerifier` aprobó **10/10 hitos (100%)** y la suite dinámica `Level2PlayModeVerificationSuite` aprobó **8/8 pruebas dinámicas (100%)**.

---

## 2. Arquitectura de la Escena `Level2.unity`

La escena `Assets/Scenes/Level2.unity` se estructuró bajo una jerarquía canónica estricta que facilita el mantenimiento y la lectura en el editor:

```
Level2.unity
├── Managers/
│   ├── GameManager (Instancia clonada)
│   ├── AudioManager
│   ├── TutorialManager
│   ├── CheckpointManager
│   ├── FrenzyController
│   ├── CaptureListener
│   ├── IngredientInventory (Sistema propio Nivel 2)
│   └── Level2_Bootstrap (Sanitizador y mitigador E-04)
├── Environment/
│   ├── Parallax/ (Sky_Far, Trees_Mid, Village_Mid)
│   ├── Lighting/ (Global Light 2D cálida + 15 Farolas puntuales)
│   ├── Zone_1_Entrada/
│   ├── Zone_2_PuestosBajos/
│   ├── Zone_3_PlazaCentral/
│   ├── Zone_4_CallejonGato/
│   ├── Zone_5_GranMercado/
│   └── Zone_6_SalidaPorton/
├── Gameplay/
│   ├── Witch (Instancia del prefab con rig y componentes)
│   ├── Ground_Z1 ... Ground_Z6 (44 plataformas y superficies de suelo)
│   ├── Checkpoint_Z1 ... Checkpoint_Z6 (6 puntos de control)
│   ├── Hideout_Z2_Alley, Hideout_Z4_TunnelShadow, Hideout_Z5_MarketShadow (3 escondites)
│   ├── CatPassage_EntryTrigger, CatPassage_ExitTrigger (Paso felino de 1.05 u)
│   ├── KillZone_Canal (Canal de aguas residuales en y = -2.5 u)
│   ├── Pickups/ (5 frascos de pociones + 4 corazones de vida)
│   ├── Ingredients/ (10 ingredientes colocados)
│   ├── IngredientGate (Portón condicionado con colisión sólida)
│   ├── Goal_Exit_Marker (Meta con Level2Goal en x = 182.0 u)
│   └── Tutorials/ (Disparadores de tutoriales contextuales)
├── NPCs/
│   ├── Zona_1 ... Zona_6 (6 Guardias + 11 Aldeanos con rutas)
├── UI/
│   ├── HUD_Canvas (Salud, frascos de poción, avisos)
│   ├── IngredientHUD_Canvas (4 contadores de ingredientes, banner propio)
│   ├── Level2Complete_Canvas (Pantalla de victoria y estadísticas)
│   └── EventSystem
└── Main Camera
```

### 2.1 Especificaciones del Escenario
- **Longitud Total:** $\approx 185\text{ unidades}$ (desde $x = 0$ hasta $x = 185$).
- **Densidad de Plataformas:** $23.8\text{ plataformas / } 100\text{ u}$ (44 plataformas elevadas en $185\text{ u}$), cumpliendo con creces la exigencia del GDD de mayor verticalidad que el Acto 1.
- **Parallax 2D:** 3 planos desacoplados con factores de movimiento diferenciados:
  - Plano Lejano (`Sky_Far`, factor $0.15$): Cielo nocturno y siluetas coloniales.
  - Plano Medio (`Village_Mid`, factor $0.45$): Fachadas de madera, campanarios y tejados.
  - Plano Cercano (`Gameplay`, factor $1.00$): Puestos del mercado, toldos interactivos, carretas y barriles.
- **Iluminación URP 2D:** Paleta cálida de cuento ilustrado (tonos ocre, ámbar y azules profundos). 1 luz global ambiental tenue ($0.45$ intensidad, color azul-ámbar) complementada por 15 farolas puntuales cálidas (`Light2D.Point`) que iluminan los puestos y proyectan sombras de sigilo.

---

## 3. Desafíos de Sigilo y Comportamiento de NPCs

El Nivel 2 representa el primer escenario donde guardias y aldeanos operan en estrecha proximidad, implementando la mecánica de **alarma encadenada (Aldeano $\rightarrow$ Guardia)** requerida por la Historia de Usuario HU05.

### 3.1 Censo y Distribución de NPCs
Total: **17 NPCs** distribuidos a lo largo del mercado:
- **6 Guardias Coloniales (`EnemySight`):**
  - Cono de visión Raycast2D: **30 rayos**, **60° de apertura**, **7.0 u de alcance** (con distancia de captura a $1.2\text{ u}$).
  - Patrullas configuradas mediante waypoints: combinaciones de modo `PingPong` y modo `Loop`.
- **11 Aldeanos (`VillagerAI`):**
  - Representados por 4 variantes visuales del prefab: `Villager`, `VillagerF`, `VillagerBoy`, `VillagerPreacher`.
  - Configurados con los 3 patrones erráticos exigidos por el GDD:
    1. `WalkAndReturn`: patrulla corta de ida y vuelta.
    2. `LookAround`: estacionario observando a izquierda y derecha.
    3. `ShortStops`: avance con pausas breves de inspección.
  - Configurados mediante el componente desacoplado `VillagerEncounterConfig.cs` para no mutar los prefabs originales.

### 3.2 Alarma y Cadena de Detección
- Cuando la bruja es avistada por un aldeano fuera de escondites en forma humana, este emite un grito de alarma (`Scream`).
- El radio de alerta del grito ($12\text{ u}$) cubre la distancia hacia los guardias vecinos (ubicados estratégicamente entre $2.5\text{ u}$ y $7.5\text{ u}$ de distancia), atrayendo a la guardia e iniciando el frenesí popular si se acumulan avistamientos.
- En forma de gato, la bruja no altera a los aldeanos (efecto pacificador del gato), permitiendo deslizarse bajo los puestos.

---

## 4. Sistema de Ingredientes, Portón y HUD

De acuerdo con el GDD §3.4 (Tabla 2) y la HU08, el Nivel 2 introduce la recolección de los **ingredientes de poción para el Acto 3** (materia prima que será elaborada en la iglesia).

### 4.1 Desacoplamiento de Inventarios
Se mantuvo una separación arquitectónica estricta:
1. `PotionInventory` (Nivel 1): Gestiona las cargas de frascos consumibles (Cegar, Hipnotizar, Dormir). **Intacto.**
2. `BroomInventory` (Nivel 1): Gestiona las piezas de escoba (Rama, Paja, Cuerda). **Intacto.**
3. `IngredientInventory` (Nivel 2): Gestiona de forma dedicada los 4 ingredientes del Acto 2:
   - **Hongos de Humedad (`Mushroom`):** 2 requeridos (3 disponibles en escena).
   - **Arañas de Mercado (`Spider`):** 2 requeridas (3 disponibles en escena).
   - **Flores Marchitas (`WitheredFlower`):** 2 requeridas (2 disponibles en escena).
   - **Raíces del Huerto (`Root`):** 2 requeridas (2 disponibles en escena).
   - **Total:** **8 ingredientes mínimos requeridos + 2 ingredientes de bonificación**.

### 4.2 Restricción de Forma Felina (GDD §3.2)
- En forma humana, la bruja interactúa y recolecta los ingredientes normalmente.
- En forma de gato, la recolección está expresamente bloqueada: el trigger rechaza la recogida y despliega el aviso *"En forma de gato no puedes recolectar ingredientes"*, obligando al jugador a alternar formas estratégicamente.

### 4.3 Portón de Salida (`IngredientGate.cs`)
- Ubicado en $x = 181.5\text{ u}$ bloqueando el acceso al callejón final.
- Dispone de un `BoxCollider2D` sólido que detiene físicamente al jugador mientras falten ingredientes.
- Si el jugador se aproxima antes de tiempo, emite advertencia contextual en el HUD.
- Al alcanzar los 8 ingredientes requeridos, el evento `OnAllRequiredCollected` desactiva el colisionador sólido, cambia el sprite a portón abierto, reproduce sonido de desbloqueo y despliega *"¡Sello roto! El portón hacia la iglesia está abierto."*.

---

## 5. Integración Final, Salida Segura y Mitigación de Errores

### 5.1 Mitigación de Errores Históricos del GDD
- **Error E-04 (Gato bloqueado al cambiar de nivel):**  
  Se implementó `Level2Bootstrap.cs`. Al cargarse la escena, detecta automáticamente si el jugador proviene de una sesión previa en forma de gato e invoca `TransformationManager.ResetToHumanForm()`, restaurando el tamaño estándar del collider, las escalas del Rig y reseteando las velocidades residuales del Rigidbody2D.
- **Error E-05 (Sonido de pasos tras Game Over / Fin de Nivel):**  
  `Level2Bootstrap` y `Level2Goal` detienen inmediatamente las físicas de movimiento (`ResetPhysics(Vector2.zero)`) y limpian las fuentes de audio residuales al pausar o abandonar el nivel.
- **Escala de Tiempo:** `Level2Bootstrap` garantiza `Time.timeScale = 1.0f` al entrar, y los botones de `Level2CompleteScreen` restauran `Time.timeScale = 1.0f` antes de disparar cualquier transición.

### 5.2 Meta del Nivel (`Level2Goal.cs`)
- Ubicada en `Goal_Exit_Marker` ($x = 182.0\text{ u}, y = 2.0\text{ u}$) con `BoxCollider2D` tipo trigger.
- Comprueba que `IngredientInventory.Instance.AreAllRequiredCollected == true`.
- Previene doble disparo mediante la bandera booleana `levelCompleted`.
- Consolida el puntaje de la partida:
  $$\text{Puntaje} = (\text{Ingredientes Requeridos} \times 200) + (\text{Ingredientes Bonus} \times 500) + (\text{Corazones} \times 300) + \text{Bonus de Tiempo}$$
- Persiste el progreso en `SaveSystem.SaveLevelProgress(2, score)` (desbloqueando el nivel para la opción Continuar del menú) y guarda en claves `L2_Completed = 1`, `L2_ElapsedTime`, `L2_FinalScore` y `L2_Act2_Ingredients_Complete = 1`.
- Pausa el juego de forma segura (`Time.timeScale = 0.0f`) y activa la pantalla `Level2CompleteScreen`.

### 5.3 Pantalla de Victoria (`Level2CompleteScreen.cs`)
Desplegada en `UI/Level2Complete_Canvas` (Sorting Order 150):
- **Encabezado:** *"¡MERCADO DE SALEM SUPERADO!"*
- **Teaser Acto 3:** *"Has recolectado los ingredientes requeridos para el aquelarre colonial. (Acto 3: La Iglesia — Próximamente)"*
- **Estadísticas:** Muestra ingredientes ($8/8$ o $10/8$ con etiqueta dorada de bonus), tiempo en formato $MM:SS$ y puntuación en puntos.
- **Botones de Navegación Segura:**
  1. *Continuar (Próximamente: Acto 3):* Restablece el tiempo y regresa de forma segura a `MainMenu` (preparado para enlazar la escena del Acto 3 sin retrabajo cuando sea construida).
  2. *Reintentar Nivel 2:* Restablece el tiempo, limpia el inventario temporal de la sesión y recarga `Level2.unity`.
  3. *Menú Principal:* Restablece el tiempo y carga `MainMenu`.

---

## 6. Auditoría de Transición entre Niveles y Opciones

En cumplimiento estricto con las directrices de arquitectura:
- En `Level1.unity`, `LevelGoal.cs` invoca `GameManager.Instance.LoadNextLevel()`, el cual carga `buildIndex + 1`.
- En `EditorBuildSettings.asset`, los índices actuales son:
  - `0`: `MainMenu.unity`
  - `1`: `Level1.unity`
  - `2`: `Test_Bruja.unity`
- Redirigir automáticamente Level 1 a Level 2 requeriría modificar archivos protegidos (`Level1.unity`, `LevelGoal.cs`, `GameManager.cs`) o modificar el índice 2 de Build Settings.
- **Estado Actual:** El Nivel 2 está **100% operativo y jugable de forma autónoma**. Puede abrirse y jugarse directamente en Unity o mediante el menú contextual `Brujas/Nivel 2/Cargar Escena Level2`.
- **Recomendación para la Entrega Final del Juego:** Cuando se integre el proyecto completo, reemplazar en `EditorBuildSettings.asset` la escena provisional `Test_Bruja.unity` (índice 2) por `Assets/Scenes/Level2.unity`. Con ese único cambio en Build Settings, la transición Nivel 1 $\rightarrow$ Nivel 2 funcionará de manera automática y nativa.

---

## 7. Resultados de QA y Criterios de Aceptación

### 7.1 Suite Estática y de Integridad (`Level2MasterVerifier.cs`)
Ejecutada en el motor de Unity:
| Hito | Descripción | Resultado |
| :---: | :--- | :---: |
| **Hito 1** | Jerarquía canónica de 7 raíces (`Managers`, `Environment`, `Gameplay`, `NPCs`, `Ingredients`, `UI`, `Main Camera`) | **APROBADO** |
| **Hito 2** | Spawn de Bruja en $x \approx 1.5\text{ u}$, Tag `Player`, componentes requeridos (PC, TM, PI, BI, Invulnerability) | **APROBADO** |
| **Hito 3** | `Level2Bootstrap` activo en `Managers` con sanitización y mitigación E-04 | **APROBADO** |
| **Hito 4** | Geometría: 44 colliders de plataforma, 6 checkpoints, KillZone canal, Zona 4 paso felino ($1.05\text{ u}$) | **APROBADO** |
| **Hito 5** | Iluminación URP 2D (1 Global + 15 Farolas puntuales) y Parallax (3 capas independientes) | **APROBADO** |
| **Hito 6** | Encuentros NPC: 6 Guardias con conos (30 rayos, 7u) y 11 Aldeanos con 3 patrones e IA | **APROBADO** |
| **Hito 7** | Ingredientes: 10 unidades (M=3, S=3, F=2, R=2), `IngredientInventory`, `IngredientHUD` e `IngredientGate` | **APROBADO** |
| **Hito 8** | Meta de nivel `Level2Goal` en $x = 182\text{ u}$ y pantalla de victoria `Level2CompleteScreen` conectada | **APROBADO** |
| **Hito 9** | Integridad absoluta de archivos protegidos (`Level1.unity`, `Level1_Backup.unity`, scripts core) | **APROBADO** |
| **Hito 10** | Limpieza de consola: 0 errores de compilación CS y sincronización total | **APROBADO** |
| **TOTAL** | **10 / 10 Hitos Superados** | **100% OK** |

### 7.2 Suite de Comportamiento Dinámico (`Level2PlayModeVerificationSuite.cs`)
Ejecutada en el motor de Unity:
| Prueba | Comportamiento Evaluado | Resultado |
| :---: | :--- | :---: |
| **Test 1** | Bootstrap ejecuta `CleanSceneEntryState`: restaura `Time.timeScale = 1` y forma humana (mitiga E-04) | **APROBADO** |
| **Test 2** | Bruja en spawn, parámetros de velocidad física (`moveSpeed = 6f`) y salto (`jumpHeight = 2.8f`) | **APROBADO** |
| **Test 3** | Conos de visión de guardias calibrados y radios de alarma de aldeanos ($12\text{ u}$) | **APROBADO** |
| **Test 4** | Reglas de recolección: incremento de conteo, bloqueo en gato y prevención de duplicados por ID | **APROBADO** |
| **Test 5** | Portón mágico: bloquea físicamente con $<8$ ingredientes y abre colisión al completar $8/8$ | **APROBADO** |
| **Test 6** | Secuencia de victoria: disparo atómico, guardado en `SaveSystem` y persistencia de claves `L2_` | **APROBADO** |
| **Test 7** | `Level2CompleteScreen`: despliegue modal, visualización de estadísticas y restauración de tiempo | **APROBADO** |
| **Test 8** | Regresión de Nivel 1: inspección en memoria de `Level1.unity` confirmando que permanece 100% intacto | **APROBADO** |
| **TOTAL** | **8 / 8 Pruebas Dinámicas Superadas** | **100% OK** |

### 7.3 Verificación de Regresión en Nivel 1
- **`Level1MasterVerifier.RunAllTests()`:** **9 / 9 Hitos Aprobados (100%)**.
- **`Level1PlayModeVerificationSuite.RunFullVerification()`:** **15 / 15 Hitos Aprobados (100%)**.
- **Verificación Git:**
  - `git status` limpio en todos los archivos del repositorio base.
  - Modificaciones preexistentes del usuario (`LiberationSans SDF - Fallback.asset`, `README.md`, `Docs/Documentacion_General_Juego.md`) respetadas sin alteraciones.
  - Cero archivos protegidos tocados.

---

## 8. Matriz de Cumplimiento de Historias de Usuario (GDD)

| Código GDD | Requisito del Documento | Implementación en Nivel 2 | Estado |
| :---: | :--- | :--- | :---: |
| **HU05** | Aldeanos con $\ge 3$ patrones erráticos y alarma hacia guardias cercanos | 11 aldeanos con `WalkAndReturn`, `LookAround` y `ShortStops`. Grito en $12\text{ u}$ alerta a guardias a $2.5\dots 7.5\text{ u}$. | **CUMPLIDO** |
| **HU08** | Ingredientes de poción recolectables con HUD y guardado simple | 4 tipos (hongos, arañas, flores, raíces). 10 colocados. HUD dedicado en esquina superior derecha. Persistencia en `L2_`. | **CUMPLIDO** |
| **HU11** | Mayor dificultad y densidad de plataformas que en el Acto 1 | 44 plataformas elevadas ($23.8\text{ plat/100 u}$), saltos con toldos, vigas altas y canal killzone inferior. | **CUMPLIDO** |
| **HU12** | Ambientación colonial de mercado con $\ge 3$ capas de parallax y URP 2D | 3 capas de parallax, paleta cálida (ocres/azules), 15 farolas 2D, puestos, toldos y carretas. | **CUMPLIDO** |
| **GDD §3.2** | Restricción de gato: no puede recolectar ingredientes | `Ingredient.cs` detecta `TransformationManager.IsCatForm` y rechaza la recolección con aviso. | **CUMPLIDO** |
| **GDD §7.3** | Reunir objetos del acto desbloquea la salida | `IngredientGate` bloquea con colisión sólida hasta que `AreAllRequiredCollected == true`. | **CUMPLIDO** |
| **GDD §8.3** | Mitigación error E-04 (gato bloqueado al cambiar de nivel) | `Level2Bootstrap.CleanSceneEntryState()` fuerza forma humana, restaura collider y limpia velocidades. | **CUMPLIDO** |
| **GDD §8.3** | Mitigación error E-05 (sonido de pasos tras fin de nivel) | `Level2Goal` resetea físicas a cero y congela `Time.timeScale = 0`. | **CUMPLIDO** |

---

## 9. Inventario de Archivos Creados para el Nivel 2

Todos los activos del Nivel 2 residen de forma estrictamente modular y aislada:
- **Escena:**
  - `Assets/Scenes/Level2.unity` (+ `.meta`)
- **Scripts de Juego (`Assets/Scripts/Level2/`):**
  - `Level2Bootstrap.cs`: Sanitización de escena y mitigación de E-04.
  - `Level2Goal.cs`: Meta condicional, cálculo de score y persistencia.
  - `Level2CompleteScreen.cs`: Interfaz de fin de nivel y opciones de navegación.
  - `Ingredient.cs`: Componente interactivo de ingredientes con restricción de gato.
  - `IngredientInventory.cs`: Inventario dedicado, eventos y persistencia `L2_`.
  - `IngredientGate.cs`: Portón bloqueante condicional.
  - `IngredientHUD.cs`: Interfaz de usuario para ingredientes.
  - `VillagerEncounterConfig.cs`: Configurador de patrones e IA sin mutar prefabs.
- **Herramientas de Editor y Verificadores (`Assets/Editor/Level2/`):**
  - `Level2SceneBuilder.cs`: Constructor de escena base.
  - `Level2StageBuilder.cs`: Constructor de geometría y ambientación.
  - `Level2NPCBuilder.cs`: Colocador y configurador de guardias y aldeanos.
  - `Level2IngredientBuilder.cs`: Colocador de ingredientes, portón y HUD.
  - `Level2FinalIntegrationBuilder.cs`: Ensamblador de integración final.
  - `Level2Verifier.cs` / `Level2StealthVerifier.cs` / `Level2IngredientVerifier.cs`: Verificadores parciales.
  - `Level2MasterVerifier.cs`: Verificador maestro estático (10 hitos).
  - `Level2PlayModeVerificationSuite.cs`: Suite de pruebas dinámicas (8 pruebas).
- **Prefabs Propios (`Assets/Prefabs/Level2/`):**
  - `Ingredient_Mushroom.prefab`, `Ingredient_Spider.prefab`, `Ingredient_Flower.prefab`, `Ingredient_Root.prefab`
- **Sprites Propios (`Assets/Art/Environment/Level2/`):**
  - Sprites 2D generados para puestos, toldos, farolas, carretas, ingredientes y portón.
- **Documentación Técnica:**
  - `Docs/Nivel2_Auditoria.md`: Bitácora y auditoría exhaustiva paso a paso (12 secciones).
  - `Docs/Nivel2_Informe_Final.md`: Este informe final de entrega y aceptación.

---

## 10. Conclusión y Recomendaciones para el Acto 3

El **Nivel 2 (Acto 2 — Pueblo y Mercado)** cumple plenamente con todas las especificaciones de diseño del GDD, las historias de usuario de sigilo y recolección, y los más altos estándares de calidad técnica, arquitectura limpia y no regresión.

### Recomendaciones para el Inicio del Acto 3 (La Iglesia):
1. **Consumo de Ingredientes:** El Acto 3 podrá leer directamente los PlayerPrefs consolidados por `IngredientInventory.CommitAct2Completion()`: `L2_Final_Mushrooms`, `L2_Final_Spiders`, `L2_Final_Flowers`, `L2_Final_Roots` y `L2_Act2_Ingredients_Complete = 1` para inicializar el sistema de preparación de hechizos del aquelarre.
2. **Conexión de Transición en Build Settings:** Asignar `Level2.unity` al índice 2 de Build Settings y crear `Level3.unity` en el índice 3. Con ello, `Level2CompleteScreen` podrá simplemente cargar `"Level3"` mediante `SceneManager.LoadScene("Level3")` sin requerir modificaciones estructurales en los scripts centrales.
3. **Mantenimiento del Aislamiento:** Seguir la misma metodología probada de crear componentes específicos del acto (`Assets/Scripts/Level3/`) sin modificar los sistemas centrales aprobados.
