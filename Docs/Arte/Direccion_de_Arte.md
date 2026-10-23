# Dirección de arte — Personajes

Decisiones aprobadas (2026-10-17):

- **Técnica:** rig por piezas con Unity 2D Animation (huesos + Sprite Skin). Animación a 60 fps interpolada; las curvas usan easing y nunca tangentes lineales.
- **Estilo:** cartoon ilustrado. Es el de la bruja actual: contorno oscuro de 3–4 px a 1024 px de alto, relleno plano con una sombra y un brillo.
- **Bruja:** curiosa y astuta. Se mueve con cautela, se asoma antes de avanzar y reacciona con picardía.
- **Luz:** desde arriba a la izquierda. La sombra cae abajo a la derecha de cada volumen y el brillo va en el borde superior izquierdo.
- **Vista:** perfil 3/4 mirando a la derecha (el juego voltea en X).

## Paleta (hex)

| Uso | Colores |
|---|---|
| Contorno | `#1D0F0C` |
| Tela marrón (bruja) | `#4A2A20` sombra · `#6E452D` base oscura · `#905C37` base · `#B97E4F` brillo |
| Bufanda / acentos cálidos | `#7A2E1F` sombra · `#A8452B` base · `#D2734A` brillo |
| Piel clara | `#CD9A7D` sombra · `#F8C0A2` base · `#FFE0CC` brillo |
| Piel media | `#9C6A4C` sombra · `#C98F6B` base · `#E8B591` brillo |
| Cabello castaño | `#2E1A14` sombra · `#4E2C1F` base · `#76452F` brillo |
| Gato | `#14111A` base · `#2A2433` brillo · `#E8A93A` ojos · `#F2C46B` brillo de ojos |
| Puritanos (negro/gris) | `#1E1C22` · `#34313A` · `#55515C` |
| Lino / cuellos blancos | `#B9B2A3` sombra · `#E6E0D2` base · `#FFFBF0` brillo |
| Ocre (hebillas) | `#8A6A1E` · `#C99A2E` · `#EBC45A` |
| Verde musgo / azul pizarra (variantes) | `#3F4A2E` · `#5E6B42` / `#2E3A4A` · `#4A5B70` |
| Magia (pociones) | Ceguera `#6B4FD8` · Hipnosis `#D84FB8` · Sueño `#4FB8D8` · resplandor `#F4F0FF` |

## Siluetas

- **Bruja:** capucha puntiaguda caída hacia atrás, coleta, túnica con bordes rasgados.
- **Gato:** orejas grandes, cola larga en S.
- **Aldeanos:** cada uno con un rasgo dominante: sombrero de copa alto, cofia, barriga, espalda encorvada con bastón, niño bajito o delantal ancho.

## Piezas del rig

**Bruja:** `head`, `eye_open`, `eye_half`, `eye_closed`, `mouth_smile`, `mouth_o`, `hood`, `hair_tail`, `scarf`, `scarf_tail`, `torso`, `robe_skirt`, `arm_upper_front`, `arm_lower_front`, `arm_upper_back`, `arm_lower_back`, `leg_upper_front`, `leg_lower_front`, `leg_upper_back`, `leg_lower_back`.

La física secundaria (cadenas de huesos con resorte) mueve `hood`, `hair_tail`, `scarf_tail` y el borde de `robe_skirt`.

## Proceso por sprint

1. Generar la hoja de piezas con `gemini_art.py generate` (prompts en `Docs/Arte/prompts/`). El fondo es magenta y se quita automáticamente.
2. Separar las piezas con `gemini_art.py split` y nombrarlas según la lista.
3. Armar el rig en Unity (PSB o prefab con huesos) y animar.
4. QA visual frame a frame y QA de rendimiento.
5. **Aprobación del personaje** antes de pasar al resto.

## Implementación en Unity (Sprint 1: bruja)

El rig es por piezas con transforms: cada pieza es un `SpriteRenderer` que rota sobre su pivote. No usa deformación de malla de 2D Animation.

| Archivo | Función |
|---|---|
| `ArtSource/Witch/witch_parts_chatgpt.png` | Hoja de piezas original (generada con ChatGPT). |
| `ArtSource/Witch/split_parts.py` | Quita el fondo, separa y nombra las piezas, recorta el interior de la capucha y genera el ojo cerrado a partir del abierto. |
| `ArtSource/Witch/witch_rig.json` | Configuración del rig: jerarquía, pivotes, capas, escalas y punto de apoyo de los pies. Se copia a `Assets/Art/Characters/WitchRig/`. |
| `ArtSource/Witch/rig_preview.py` / `rig_anim.py` | Previsualización rápida en Python (GIF) usada en el Sprint 0. |
| `Assets/Editor/CharacterRigBuilder.cs` | **Tools > Rig > Construir rig de la Bruja**: importa las piezas (PPU y pivotes del JSON) y crea `Assets/Prefabs/WitchRig.prefab`. **Tools > Rig > Integrar rig en Witch.prefab**: lo agrega al personaje jugable. |
| `Assets/Scripts/Player/Rig/WitchRigAnimator.cs` | Animación procedural por capas (es la fuente de verdad de la animación). |
| `Assets/Scripts/Player/Rig/RigSpriteProxy.cs` | Puente con el sprite original: copia volteo, parpadeo de invulnerabilidad, tinte del maizal y capa; en forma de gato muestra el sprite del gato. |
| `Assets/Editor/CharacterTestSceneBuilder.cs` | **Tools > Rig > Crear escena de prueba de personajes**: crea `Assets/Scenes/Test_Personajes.unity`. |
| `Assets/Editor/RigQaCapture.cs` | **Tools > Rig > QA visual**: hojas de contacto fotograma a fotograma en `Docs/Arte/sprint0/qa/`. |

### Animaciones de la bruja

| Estado | Detalle |
|---|---|
| Idle | Respiración, cambio de peso, se asoma con curiosidad, parpadeo aleatorio (2–5 s, a veces doble). |
| Caminar / correr | Mezcla según la velocidad. La fase avanza con la distancia recorrida (los pies no patinan) y el pie de apoyo queda pegado al suelo automáticamente. |
| Salto | Anticipación al despegar, piernas recogidas al subir, ápice, extensión al caer y aterrizaje con squash y polvo. |
| Vuelo | Sentada en la escoba con balanceo. |
| Hechizo | Anticipación, lanzamiento con sobrepaso y follow-through. Se dispara con el evento `PotionInventory.OnPotionThrown`. |
| Daño | Retroceso, ojos cerrados y boca en "o". Se dispara con `GameManager.OnHeartLost`. |
| Muerte | Las rodillas ceden y luego se desploma con rebote. Usa tiempo sin escalar porque Game Over pausa el juego. |

Física secundaria: resortes amortiguados en coleta, bufanda, capucha y falda. Reaccionan a la cabeza, la velocidad, la caída y la aceleración.

### QA

- **Visual** (agente QA sobre las hojas de contacto): se corrigieron el encuadre de salto y muerte, las piernas en tijera al subir, los pies que se hundían al morir, el desplome brusco, el idle y el vuelo demasiado estáticos y el bulto del brazo trasero.
- **Rendimiento**: escena de prueba con 8 brujas animadas a la vez en el editor: 228 FPS de promedio y 152 de mínimo (objetivo: 60).

## Aldeanos y guardia (cuadro a cuadro)

El rig por piezas no funcionó para el gato (cuerpo redondo sin articulaciones claras), así que los aldeanos, el guardia y el gato usan **hojas de animación generadas con ChatGPT**: una imagen por animación, 12 fotogramas a 12 fps. Los prompts están en `Docs/Arte/prompts/Correcciones_Lote1.md`.

| Archivo | Función |
|---|---|
| `ArtSource/Villagers/<personaje>_<animación>.png` | Hojas originales (idle, walk, run, react). |
| `ArtSource/Villagers/slice_sheet.py` | Quita el fondo (incluido el magenta encerrado y las líneas de cuadrícula que dibuja la IA), separa los fotogramas, los alinea por los pies y el torso, y mide la consistencia. Escribe en `ArtSource/Villagers/cut/` (ignorado por git, se regenera). |
| `ArtSource/Villagers/export_villagers.py` | Elige los fotogramas buenos de cada hoja, iguala la escala entre hojas de distinta resolución, neutraliza el tinte magenta y exporta a `Assets/Art/Characters/Villagers/<personaje>/`. |
| `Assets/Editor/VillagerSpriteSetBuilder.cs` | **Tools > Aldeanos > Importar animaciones y aplicar a prefabs**: crea un `VillagerAnimationSet` por personaje y lo aplica a `Villager` (granjero), `VillagerF` (lechera), `VillagerPreacher`, `VillagerBoy` y `Guard`. |
| `Assets/Scripts/Enemies/VillagerSpriteAnimator.cs` | Reproduce la animación según el estado de `VillagerAI` o `PatrolRoute`/`EnemySight`: paseo con cadencia ligada a la velocidad, aviso, grito, persecución, hechizos y acción propia; tempo distinto por instancia. |

Level1 usa solo **granjero y lechera** como aldeanos, más el **guardia** nuevo. El predicador y el niño quedan listos como prefabs para otros niveles.

## Reemplazo de assets aprobados

`ArtSource/replace_assets.py` sustituye el arte del nivel conservando cada archivo (mismo GUID y referencias), su tamaño de lienzo y la colocación del contenido anterior. Las plataformas mantienen la superficie caminable a la misma altura, el maíz y los letreros quedan apoyados en el suelo y los objetos centrados. Cada animación usa una sola escala, y el suelo y el cielo se repiten sin costura. Cuando un lienzo crece (raíces de las rocas, puerta de meta), el pivote se recalcula y se aplica con **Tools > Arte > Aplicar pivotes del reemplazo de assets**.

Reemplazados: suelo, rocas, troncos, maizal, pociones (objeto, proyectil, íconos del HUD), íconos de estado, salpicaduras, corazones (objeto, HUD, romperse y curarse), escoba y sus piezas, letrero, checkpoint, puerta de meta y cielo.

## Pendientes

- **Anciana:** reenviar sus 4 hojas (no quedaron guardadas) y exportarla como los demás aldeanos.
- **Guardia:** rehacer idle (con gesto), carrera (piernas alternadas) y acción propia con la regla de un solo farol. Hoy persigue con la caminata acelerada y su idle no tiene gesto.
- **Niño y predicador:** el niño necesita una carrera nueva (la resortera desaparece).
- **Gato:** integrar la hoja aprobada `ArtSource/Cat/sheets/cat_all_chatgpt.png` (o las de 12 fotogramas, si se generan).
- **Por regenerar:** túnel de tronco (dos capas del mismo tronco) y fondos de bosque y aldea (fondo verde, sin neblina).
- La escoba (`BroomVisual`) conserva su posición anterior; revisar su alineación con la pose de vuelo del rig.
- Partículas de hechizo y screen shake.
