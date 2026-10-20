# Guía de fábricas y herramientas de nivel

Level1 es la plantilla para los niveles siguientes. Todo lo de esta guía es reutilizable: se arma un nivel nuevo con las mismas piezas y estas herramientas lo dejan pulido.

## Flujo para crear un nivel

1. **Maqueta** con los prefabs: suelos (`BoxCollider2D` en la capa *Ground*, con nombre `Ground...`), plataformas, objetos, guardias, aldeanos, checkpoints, letreros y la meta. Agrupa cada tramo en `_Zones/Zone_N` (un hijo por pieza).
2. **Tools > Nivel > Reconstruir suelo**: cada suelo recibe un `GroundStrip` que dibuja el pasto en mosaico, con la línea de pasto denso exactamente sobre la superficie física. Además añade una franja de pasto delante de los personajes, para que los pies queden *dentro* del pasto y no flotando encima.
3. **Tools > Nivel > Anclar objetos al suelo**: añade `GroundAnchor` a objetos, checkpoints, letreros, meta y maizales, y los apoya sobre la superficie que tienen debajo (o sobre la más cercana, dentro de un túnel). Los postes se clavan un poco; los objetos se posan en el pasto.
4. **Tools > Nivel > Auditar apoyo en el suelo**: lista en la consola todo lo que flote (más de 0.03 u por encima) o esté hundido. En Level1 el resultado es 74 objetos y **0 con problemas**.
5. **Tools > Nivel > Maquetar HUD**: distribuye el HUD para 1920×1080 con márgenes y textos legibles.

## Ampliar o reordenar un nivel

`LevelLayoutTools` (desde código o desde el ejecutor de comandos):
- `InsertSpace(x, cantidad)`: abre espacio en una X; todo lo que está a la derecha se recorre.
- `ScaleRange(x0, x1, factor)`: separa las piezas de un tramo amontonado y recorre lo que sigue.

Ambas mueven las piezas enteras. Solo **estiran** los suelos sin arte propio. También actualizan la zona de muerte y el límite derecho de la cámara, y todo queda en *Undo*. Level1 creció de unas 235 u a unas 440 u: la zona de aldeanos se separó 35 % y la del reto combinado 25 %, hay 6 u de respiro entre zonas y se añadieron las dos zonas de reto (72 u y 66 u). Ambas herramientas desplazan cada pieza según dónde está realmente (su collider o su dibujo), no según su transform.

## Componentes reutilizables

| Componente | Dónde | Qué hace |
|---|---|---|
| `GroundStrip` | Suelos | Genera el arte del suelo alineado con su collider (baldosa y franja delantera). |
| `GroundAnchor` | Objetos y props | Apoya la base visible del sprite en el suelo, en el editor y al iniciar el juego. |
| `PickupGlint` | Coleccionables | Brillo que late y destello ocasional. Reemplaza la antigua flotación, que hacía parecer que los objetos flotaban. |
| `AmbientParticles` | Cámara | Luciérnagas y hojas de otoño cayendo (texturas generadas por código). |
| `CameraFollow` | Cámara | Cámara de plataformas: anticipación hacia donde mira la bruja, zona muerta vertical (los saltos pequeños no la mueven), caída rápida y temblor de pantalla con `CameraFollow.Shake(amplitud, duración)`. |
| `VillagerSpriteAnimator` | Aldeanos y guardias | Animación cuadro a cuadro según la IA. |
| `CatAnimationDriver` | Bruja | Sigilo del gato en el maizal y sentarse tras 3 s quieto. |

## Estructura de Level1 (≈ 440 u)

| Zona | Tipo | Qué enseña o pone a prueba |
|---|---|---|
| 1 Bienvenida | Tutorial | Movimiento, salto y plataformas |
| 2 Salto y guardia | Tutorial | Conos de visión, rama de la escoba |
| 3 Escondite en el maizal | Tutorial | Gato y maizal, paja de la escoba |
| 4 Pociones y guardia | Tutorial | Recoger y lanzar pociones |
| 5 Aldeanos | Tutorial | Aldeanos, grito y frenesí |
| 6 Túnel del gato | Tutorial | Pasaje estrecho, cuerda de la escoba |
| **6B Reto del sigilo** | Reto | Maizal vigilado y túnel largo (26 u) con guardia en el techo: obliga a administrar la energía del gato |
| 7 Primer vuelo | Tutorial | Vuelo con la escoba completa |
| 8 Reto combinado | Reto | Guardias, aldeanos y maizal juntos |
| **8B Último reto** | Reto | Guardias cruzados, ruta alta opcional, aldeanos y pociones antes del vuelo final |
| 9 Vuelo a la meta | Final | Isla de descanso y puerta de la aldea |

Los tutoriales siguen apareciendo en sus zonas. Las zonas de reto llevan un letrero que explica el desafío. **Tools > Nivel > Construir zonas de reto** (`ChallengeZoneBuilder`) las arma clonando piezas del propio nivel: sirve de ejemplo para crear retos en niveles nuevos.

## Límite de la forma de gato

- Dura **8 s** con la energía llena y se recarga a 0.15 por segundo en forma humana (unos 6.7 s para llenarse).
- Hace falta al menos un 25 % de energía para convertirse en gato.
- Por debajo del 30 % aparece un aviso y la barra parpadea en rojo.
- Al agotarse, la bruja vuelve a su forma en cuanto hay espacio. Dentro de un túnel sigue como gato hasta salir, para no quedar atrapada ni morir.
- La barra morada aparece bajo los corazones mientras es gato o se recarga. Se pone gris si no alcanza para transformarse.
- La primera vez que se transforma se muestra el tutorial "Forma Felina Limitada".
- Ajustes en `TransformationManager` (Witch.prefab): `catMaxDuration`, `catRechargePerSecond`, `catMinEnergyToTransform`, `catLowEnergyWarning`.

## Comportamiento de los enemigos

- **Aldeanos:** se detienen a mirar al gato cuando pasa cerca (no lo delatan). Si la bruja humana se pega a su espalda, tras 0.9 s se giran, y la visión decide si la ven. Cada uno camina a un ritmo distinto (±10 %).
- **Guardias:** en cada punto de patrulla esperan un tiempo variable (±25 %) y vigilan: miran hacia atrás a mitad de la espera y vuelven. Cada uno patrulla a una velocidad ligeramente distinta.

## Impacto (sensación de juego)

- Temblor de cámara al recibir daño, al aterrizar de una caída fuerte y al romperse una poción.
- El aterrizaje de la bruja levanta polvo (rig animado).

## Menú principal

**Tools > Menú > Maquetar menú principal** (`MainMenuStyler`) deja el menú con estilo Hollow Knight y lo guarda en `MainMenu.unity`. Se puede volver a ejecutar sin problema.
- El fondo de otoño queda atenuado, con viñeta, y hace un zoom y paneo lentos.
- Título grande con subtítulo "SALEM · 1692" y adornos dorados.
- Botones solo de texto (`MenuButtonFX`): al pasar el ratón o seleccionarlos con el teclado se iluminan en dorado, crecen y muestran adornos a los lados.
- Luciérnagas, hojas que caen y el gato animado junto al camino (`MenuAmbience`).
- Opciones y Créditos van en paneles oscuros con marco dorado.
- Navegación con flechas y Enter. Esc vuelve al menú desde Opciones o Créditos.
- Las texturas (viñeta, adorno, marco) se generan en `Assets/Art/UI/menu_*.png`.

## Pendientes de arte

- Túnel de tronco definitivo: hoy es una versión provisional generada a partir del tronco aprobado (`ArtSource/make_log_tunnel.py`). Se reemplaza con la imagen de la sección 6 de `Docs/Arte/prompts/Correcciones_Lote1.md`.
- Fondos de bosque y aldea con fondo verde (misma guía, sección 7).
