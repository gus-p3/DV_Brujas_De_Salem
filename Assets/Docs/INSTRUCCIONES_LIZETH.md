# Guía del Proyecto: Brujas de Salem (Avance 2)
**Para: Lizeth Ramírez Ramírez**
**De: Brandon Gustavo Mendoza Amaro**

¡Hola Lizeth! Aquí tienes el resumen de cómo dejé configurado el proyecto hasta nuestro Avance 2, para que puedas probarlo y continuar con el desarrollo sin problemas.

---

## 🎮 Controles Actuales

El juego utiliza el sistema mixto de Inputs de Unity (funciona tanto con teclado clásico como con Input System).

- **Moverse:** Flechas del teclado o teclas `A` / `D`
- **Saltar:** Barra Espaciadora
- **Volar:** Tecla `F` (La bruja flota, usa flechas direccionales para moverte en el aire. La energía se agota y se muestra en la barra amarilla de la UI. Toca el suelo para recargar).
- **Transformarse en Gato:** Tecla `T` (Tiene un tiempo de enfriamiento de 0.75 segundos).
- **Lanzar Hechizos:** *(Próximamente en el Avance 3)*
- **Pausa:** Tecla `Escape` (Congela el tiempo y abre el menú de opciones).

---

## 🐈 Mecánica de Transformación (Gato)

La transformación está controlada por el script `TransformationManager.cs` que le agregué al prefab de la bruja.
- **¿Qué hace?** Al pulsar `T`, cambias al instante de forma. 
- **Ventajas del gato:** Corres un **30% más rápido** y tu "hitbox" (el área de colisión) es mucho más bajita, ideal para pasar por huecos. 
- **Sigilo:** Reemplacé el sistema de detección básico. Ahora usamos `WitchDetectable.cs`. Cuando eres gato y estás fuera del maizal, el guardia se vuelve medio ciego: su cono de visión no te detectará a menos que le pases por las narices (a muy corta distancia).
- **Limitaciones:** El gato NO puede volar ni recoger objetos. Para volver a ser humana, el script comprueba primero que haya espacio suficiente hacia arriba usando físicas.

---

## 🖼️ Interfaz de Usuario (UI) y Sistemas Core

He configurado la estructura central del juego para que no tengas que preocuparte por conectar las pantallas:

### 1. Escena `MainMenu`
Es la pantalla inicial ilustrada. Tiene los botones listados abajo. El botón "Continuar" solo se habilita si el `SaveSystem.cs` detecta que hay progreso guardado usando `PlayerPrefs`.
*Nota: Si necesitas editar la interfaz visualmente, el Canvas está configurado a 1920x1080 con Scale With Screen Size (Match 0.5).*

### 2. Prefabs Gestores (`GameManager` y `AudioManager`)
Estos dos son **Singletons** que no se destruyen al cambiar de escena (`DontDestroyOnLoad`). 
- **GameManager:** Controla el estado del juego (`Playing`, `Paused`, `LevelComplete`, `GameOver`). Aquí es donde llevamos la cuenta de los puntos, las 3 vidas, el cronómetro del nivel y la munición de hechizos (Cegar, Hipnotizar, Dormir).
- **CaptureListener:** Este script chiquito busca automáticamente a todos los enemigos de la escena y se suscribe a su evento `OnTargetCaptured`. Si un enemigo te ve, le avisa al GameManager para que detone la pantalla de Game Over de inmediato.

### 3. Paneles del HUD (Escena de Juego)
En la escena `Test_Bruja` está el HUD principal. `HUDController.cs` se actualiza solo cada vez que el GameManager emite un evento de cambio de estadísticas. Lo mismo aplica para las pantallas de Pausa, Nivel Completado y Game Over (que muestra la hoguera).

---

## 🛠️ Menú de Herramientas Rápidas (Editor)

Si se te llega a romper la escena o necesitas configurarlo en una escena nueva, creé un menú especial en Unity. Ve arriba en la barra de tareas a **`Brujas`** y encontrarás:

- `5. Escenas > Generar Escena MainMenu`: Te rearma la escena del menú principal desde cero.
- `6. Core > Crear Prefabs Managers`: Te recrea los Singletons si los borras por error.
- `7. Player > Configurar Prefab Bruja (Gato)`: Le reinyecta los poderes del gato a la bruja si el prefab se corrompe.

¡Cualquier duda con el código puedes revisar los scripts en `Assets/Scripts/`! Todo está escrito en C# puro, sin Visual Scripting.
