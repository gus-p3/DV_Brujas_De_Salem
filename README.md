# Brujas de Salem (`DV_Brujas_De_Salem`)

**Motor:** Unity 6 (6000.3 LTS) — 2D URP  
**Género:** Plataformas 2D, Sigilo Táctico y Acción Mágica  

> Documentación completa disponible en [`Docs/Documentacion_General_Juego.md`](Docs/Documentacion_General_Juego.md).  
> Informe final del Nivel 2 en [`Docs/Nivel2_Informe_Final.md`](Docs/Nivel2_Informe_Final.md).

---

## Estado del Proyecto

| Nivel | Nombre | Estado |
| :---: | :--- | :---: |
| **Nivel 1** | Acto 1 — La Aldea | ✅ Completo |
| **Nivel 2** | Acto 2 — El Pueblo y el Mercado | ✅ Completo |
| **Nivel 3** | Acto 3 — La Iglesia | 🔜 Próximamente |

---

## 1. Explicación del Juego

**Brujas de Salem** es un juego 2D de plataformas y sigilo donde controlas a una joven bruja que intenta escapar de una aldea colonial vigilada por guardias con faroles y aldeanos paranoicos.

### Nivel 1 — La Aldea
La bruja debe explorar **9 zonas progresivas (~232 unidades)**, transformarse en gato para infiltrarse por maizales y troncos estrechos, emplear pociones mágicas arrojables para neutralizar amenazas y **recolectar las 3 partes de su escoba (*Rama*, *Paja* y *Cuerda*)**. Una vez ensamblada la escoba, desbloquea el vuelo para cruzar los abismos finales y llegar al **Portón de Salida (`LevelGoal`)** antes de perder sus **3 corazones de vida**.

### Nivel 2 — El Pueblo y el Mercado
La bruja avanza por **6 zonas de mercado colonial (~185 unidades)** enfrentando mayor densidad de enemigos con **alarma encadenada** (aldeanos alertan a guardias cercanos). Debe recolectar **8 de 10 ingredientes de poción** (Hongos, Arañas, Flores Marchitas y Raíces) distribuidos por los puestos del mercado para desbloquear el **Portón Mágico** de salida. En forma de gato, la recolección está bloqueada, obligando a alternar formas estratégicamente. Al completar el nivel se desbloquea el **Acto 3: La Iglesia**.

---

## 2. Controles

Todos los controles se gestionan desde [`ControlsMap.cs`](Assets/Scripts/Core/ControlsMap.cs):

| Acción | Control Principal | Alternativo | Detalles |
| :--- | :--- | :--- | :--- |
| **Moverse** | `A` / `D` | `←` / `→` | Desplazamiento horizontal (`6 u/s` humana, `7.8 u/s` gato) |
| **Saltar** | `Espacio` | — | Salto de altura variable (hasta `2.8 u`) con *Coyote Time* y *Jump Buffer* |
| **Transformarse (Humana ↔ Gato)** | `T` | — | Alterna entre bruja y gato (cooldown de `0.75 s`) |
| **Activar / Desactivar Vuelo** | `F` | `Fire2` | Disponible tras recoger las 3 piezas de la escoba (solo Nivel 1) |
| **Subir / Bajar en Vuelo** | `W` / `S` | `↑` / `↓` | Control vertical en gravedad cero (máx. `4 s` de energía) |
| **Elegir Poción** | `1`, `2`, `3` | `Teclado Num 1-3` | `1`: Cegar \| `2`: Hipnotizar \| `3`: Dormir |
| **Lanzar Poción** | `E` | `Clic Izquierdo` | Lanza frasco en arco parabólico (solo en forma humana, cooldown `0.5 s`) |
| **Continuar (Tutorial)** | `Enter` | `Clic Izquierdo` | Cierra el consejo de tutorial activo y reanuda el tiempo |
| **Pausar** | `Esc` | — | Abre o cierra el menú de pausa |

---

## 3. Acciones que se Generan y Respuesta del Sistema

### A. Transformación (`T`)
* **Humana → Gato:**
  * Reduce el collider de `0.6 x 1.6 u` a `0.8 x 0.5 u` para entrar en troncos huecos y pasajes bajos ([`CatOnlyPassage`](Assets/Scripts/Level/CatOnlyPassage.cs)).
  * Aumenta la velocidad de movimiento en un **30%** (`1.3x`).
  * Hace que los **aldeanos ignoren por completo al jugador** y detiene cualquier persecución de **frenesí** en curso.
  * Al entrar en un maizal/escondite ([`HideoutZone`](Assets/Scripts/Level/HideoutZone.cs)), la vuelve **100% invisible para los guardias** (incluso dentro de su cono de luz).
  * Desactiva el vuelo si estaba volando, impide lanzar pociones y **bloquea la recolección de ingredientes** (Nivel 2).
* **Gato → Humana:**
  * Comprueba mediante físicas (`OverlapBoxAll`) si hay espacio superior suficiente. Si el techo es bajo, bloquea la acción y muestra *"Eres muy grande para pasar"*.
  * Restaura la capacidad de lanzar pociones, volar en escoba y recolectar ingredientes.

### B. Selección y Lanzamiento de Pociones (`1`, `2`, `3` + `E` / `Clic`)
* Consume `1` carga del inventario en [`GameManager`](Assets/Scripts/Core/GameManager.cs) y lanza un proyectil físico ([`PotionProjectile`](Assets/Scripts/Level/PotionProjectile.cs)) que al impactar genera una **salpicadura de `1.6 u` de radio**, aplicando estados alterados a guardias y aldeanos (`ISpellTarget`):
  1. **Poción de Ceguera (`1` - `5s`):** Suprime la visión del enemigo (cono gris), cancela su alerta y lo hace caminar lentamente y desorientado.
  2. **Poción de Hipnosis (`2` - `4s`):** Suprime la visión (cono morado) y fuerza al enemigo a caminar en la dirección en que fue lanzado el frasco.
  3. **Poción de Sueño (`3` - `6s`):** Apaga el cono de visión e inmoviliza por completo al enemigo mostrando el icono `Zzz`.

### C. Recolección y Vuelo en Escoba (`F`) — Nivel 1
* **Piezas de escoba ([`Collectible`](Assets/Scripts/Level/Collectible.cs)):** Recoger la *Rama*, la *Paja* y la *Cuerda* suma `+100 puntos` cada una y actualiza el HUD de la escoba. Al reunir las 3, se desbloquea el vuelo.
* **Vuelo (`F`):** Anula la gravedad (`gravityScale = 0`), muestra la barra de energía y consume batería durante un máximo de `4 segundos`. Al tocar el suelo, la energía se recarga automáticamente (`60%/s`).

### D. Recolección de Ingredientes — Nivel 2
* Recolectar los ingredientes del mercado con el trigger [`Ingredient`](Assets/Scripts/Level2/Ingredient.cs) incrementa el contador en [`IngredientInventory`](Assets/Scripts/Level2/IngredientInventory.cs) y actualiza el [`IngredientHUD`](Assets/Scripts/Level2/IngredientHUD.cs).
* **Tipos de ingredientes (10 disponibles, 8 requeridos):**
  - 🍄 **Hongos de Humedad:** 2 req. / 3 disp.
  - 🕷️ **Arañas de Mercado:** 2 req. / 3 disp.
  - 🌸 **Flores Marchitas:** 2 req. / 2 disp.
  - 🌿 **Raíces del Huerto:** 2 req. / 2 disp.
* Al completar 8/8, [`IngredientGate`](Assets/Scripts/Level2/IngredientGate.cs) destruye la colisión del portón y habilita la zona de salida.

### E. Detección, Combate, Daño y Reaparición
* **Guardias ([`EnemySight`](Assets/Scripts/Enemies/EnemySight.cs) + [`PatrolRoute`](Assets/Scripts/Enemies/PatrolRoute.cs)):**
  * Proyectan un cono de luz con `30 Raycast2D` que se detiene en muros.
  * Pasan de **Calma (Amarillo)** → **Sospecha (Naranja, `1.2s`)** → **Alerta/Persecución (Rojo)**.
  * Si alcanzan a la bruja ($\le 1.2\text{ u}$), le restan **1 corazón**, reinician su patrulla y devuelven a la bruja al último **Checkpoint**.
* **Aldeanos y Frenesí ([`VillagerAI`](Assets/Scripts/Enemies/VillagerAI.cs) + [`FrenzyController`](Assets/Scripts/Enemies/FrenzyController.cs)):**
  * Caminan con 3 patrones erráticos aleatorios (`WalkAndReturn`, `LookAround`, `ShortStops`).
  * Si ven a la bruja en forma humana, muestran `!` (`0.5s`), **gritan (`2s`) alertando a los guardias en un radio de `14 u`** (Nivel 1) / `12 u` (Nivel 2) y desatan un **Frenesí de multitud (`8s`)** reclutando hasta 5 aldeanos cercanos con viñeta roja en pantalla.
  * Si un aldeano en frenesí toca a la bruja, le quita **1 corazón**.
* **Salud, Invulnerabilidad y Checkpoints:**
  * La bruja cuenta con **3 corazones**. Recibir daño o caer a un abismo ([`KillZone`](Assets/Scripts/Level/KillZone.cs)) resta `1 corazón`, la reaparece en el último farol encendido ([`Checkpoint`](Assets/Scripts/Level/Checkpoint.cs)) y activa **2 segundos de invulnerabilidad** con parpadeo visual.
  * Perder los 3 corazones despliega la pantalla de **Game Over**.

---

## 4. Estructura del Proyecto

```
Assets/
├── Scripts/
│   ├── Core/          # GameManager, ControlsMap, SaveSystem…
│   ├── Enemies/       # EnemySight, PatrolRoute, VillagerAI, FrenzyController…
│   ├── Level/         # Collectible, HideoutZone, PotionProjectile, LevelGoal… (Nivel 1)
│   └── Level2/        # Ingredient, IngredientInventory, IngredientGate,
│                      # Level2Goal, Level2Bootstrap, Level2CompleteScreen… (Nivel 2)
├── Scenes/
│   ├── MainMenu.unity
│   ├── Level1.unity   # Acto 1 — La Aldea
│   └── Level2.unity   # Acto 2 — El Pueblo y el Mercado
├── Prefabs/
│   ├── Level2/        # Ingredient_Mushroom, Ingredient_Spider, Market_Barrel…
│   └── …
└── Art/Environment/
    ├── Level2/        # Sprites generados para puestos, toldos, farolas y portón
    └── …
Docs/
├── Documentacion_General_Juego.md
├── Nivel2_Informe_Final.md
└── Nivel2_Auditoria.md
```

---

## 5. Documentación Técnica

| Documento | Descripción |
| :--- | :--- |
| [`Docs/Documentacion_General_Juego.md`](Docs/Documentacion_General_Juego.md) | Documentación general del juego: GDD, mecánicas y arquitectura |
| [`Docs/Nivel2_Informe_Final.md`](Docs/Nivel2_Informe_Final.md) | Informe final de QA, aceptación técnica y auditoría del Nivel 2 |
| [`Docs/Nivel2_Auditoria.md`](Docs/Nivel2_Auditoria.md) | Bitácora paso a paso de construcción e integración del Nivel 2 |
| [`CREDITOS_AUDIO.md`](CREDITOS_AUDIO.md) | Créditos de los activos de audio utilizados |
