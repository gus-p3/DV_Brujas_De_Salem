# Nivel 2 (Acto 2: Pueblo y Mercado). Textos sobre los cofres

En el Nivel 2, los ingredientes, los frascos de poción y los corazones están **dentro de cofres** (`Level2Chest`). Ya no hay objetos sueltos por el mapa. Estos textos se lo cuentan al jugador sin necesidad de ayuda externa. Las teclas son las mismas en todos ellos:

- **Q**: abrir cofres, puertas, casas y sótano.
- **1, 2 o 3**: elegir la poción.
- **E**: lanzar la poción.

Ningún texto pide **E** para abrir algo o interactuar; el verificador lo comprueba.

Para aplicar los textos se usa **Tools > Nivel 2 > Puestos y puertas > 5. Textos de cofres** (`Level2ChestTextsBuilder`). Para comprobarlos se usa **Tools > Nivel 2 > Verificar puestos y puertas** (`Level2StallsVerifier`).

| Texto | Dónde aparece | Cuándo se muestra | Componente que lo muestra |
| :--- | :--- | :--- | :--- |
| **Busca en los cofres**: «Los ingredientes y las pociones están guardados dentro de cofres, tanto en las casas como en la calle. Acércate a un cofre y pulsa Q para abrirlo. Solo puedes abrirlos en forma humana.» | Aviso inicial, en el cuadro de tutorial del HUD. El trigger está en la Zona 1, x = 3.5, junto al spawn (x = 1.5). | Al pisar el trigger. Sale **una sola vez por sesión** y no se repite al morir ni al reaparecer; se reinicia al volver al menú principal. No pausa el juego: se cierra con Enter o clic, o sola a los 8 s. Si hay otro tutorial abierto, espera en cola. | `TutorialTrigger` (`tip_ingredientes_mercado`) → `TutorialManager.ShowTip`, solo por su API pública. |
| «Ingredientes: búscalos en los cofres (Q para abrir)» | HUD propio del nivel, en la esquina superior derecha, bajo la guía de controles. | Siempre. Se atenúa al 55 % después de abrir el primer cofre. | `Level2ChestGuideHUD` (en `IngredientHUD_Canvas`) |
| «Cofres abiertos: X/Y» (Y = 15) | HUD propio, en la segunda línea de la guía. | Siempre. Se actualiza al abrir cada cofre y conserva el estado de los cofres tras un checkpoint. | `Level2ChestGuideHUD` |
| «Los cofres de la calle guardan ingredientes y pociones. →» | Letrero de calle en la Zona 1, x = 4.3, sobre el poste `Env_Sign`. Señala el cofre del toldo del primer puesto (x = 9.7). | De lejos se ve al 60 %; a 4.5 u o menos se ve completo, con fundido. | `Level2InfoSign` (`Sign_Z1_ChestsStreet/InfoSign`) |
| «Aquí dentro hay un cofre» | Junto a la puerta de calle de las 4 casas que guardan un cofre: Botica (x = 15.28), Posada del Cuervo (x = 46.45), Casa Abandonada (x = 108.3, bajo los banderines del callejón) y Mansión (x = 148.0). | De lejos se ve al 35 %; a 4.5 u o menos se ve completo, con fundido. | `Level2InfoSign` (`Sign_House1..4_Chest`) |
| Sin letrero | La Casa en Cuarentena y el Santuario. | Nunca: no tienen cofre, así que el aviso siempre es fiable. | No aplica |
| Sin letrero de cofre | Los sótanos (Bodega de la Posada y Cripta de la Mansión). | Nunca: guardan llaves (`KeyItem`), no cofres. | No aplica |
| «Q: Abrir cofre», «Q: Abrir cofre (Llave…)», «Q: Cofre cerrado (Requiere…)», «Q: Abrir cofre (Contenido pendiente)» | Sobre cada cofre. | Al acercarse. | `Level2Chest` + `Level2DoorIndicator` / `InteractionPromptUI` (ya existían) |
| «Necesitas forma humana para abrir esto» | Banner del HUD. | Al pulsar Q junto a un cofre en forma de gato. Tiene un enfriamiento de 2 s para no repetirse. | `Level2Chest` (ya existía) |
| «Obtuviste: Hongo x1, Poción de Dormir x1» | Banner del HUD. | Al abrir un cofre. | `Level2Chest` (ya existía) |
| «Este cofre ya está vacío» | Banner del HUD. | Al pulsar Q junto a un cofre ya vaciado, siempre que no haya otra interacción enfocada. Tiene un enfriamiento de 1.5 s. | `Level2ChestGuideHUD` |
| «Aún te faltan ingredientes. Revisa los cofres de las casas y de la calle.» | Banner del HUD, en el portón de salida y la meta (Zona 6). | Al llegar sin los 8 ingredientes. Tiene un enfriamiento de 2 s. | `IngredientGate` y `Level2Goal` (propios del Nivel 2), con el mismo texto para que uno no pise al otro |
| «Sello arcano activo (x/8). Revisa los cofres de las casas y de la calle.» | Banner del HUD, en la puerta del Santuario (Zona 6). | Al pulsar Q sin los ingredientes. | `HouseDoor` (propio del Nivel 2) |
| «Q: interactuar · 1/2/3: elegir poción · E: lanzar» | HUD propio, en la guía de controles. | Siempre. | `Level2Controls` (ya existía) |

## Legibilidad

- **Letreros en el mundo:** son un tablón de madera oscura semitransparente con borde dorado, como los indicadores de puertas. El texto es crema, en negrita y con contorno oscuro para leerse sobre el atardecer. Se dibujan en la capa `Gameplay` con orden 0: quedan delante de las fachadas pero detrás de la bruja y de los NPC, así que nunca tapan al personaje. Tampoco tapan puertas, cofres, puestos ni la patrulla del guardia del tejado del callejón.
- **Fuente:** `LiberationSans SDF` incluye tildes, ñ, ¿ y ¡. La flecha → sale de su fuente de respaldo. El verificador comprueba que no falte ningún glifo.
