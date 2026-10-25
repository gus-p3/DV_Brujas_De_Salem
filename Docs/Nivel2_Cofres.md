# Nivel 2 (Acto 2 — Pueblo y Mercado): Tabla de Cofres Interactivos (`Level2Chest`)

En el Nivel 2 (`Assets/Scenes/Level2.unity`), **todos los ingredientes del ritual, frascos de poción y corazones** se encuentran resguardados dentro de **15 cofres interactivos (`Level2Chest`)** repartidos por las calles, toldos, pasarelas, callejones e interiores de casas. Ya no existen objetos ni frascos flotando sueltos en la escena (`0` instancias sueltas de `Ingredient`, `PotionPickup` y `HeartPickup`).

---

## 1. Reglas de Interacción y Persistencia

1. **Tecla exclusiva (`Q`):**
   - Todos los cofres usan `Level2Controls.InteractKey` (`KeyCode.Q`) y muestran el aviso contextual `"Q: Abrir cofre"` mediante `Level2DoorIndicator` e `InteractionPromptUI`.
   - `Q` nunca lanza pociones (`Level2Controls` suprime `PotionInventory` durante el frame de pulsación de `Q`), y `E` nunca abre cofres ni puertas.
2. **Restricción de forma felina (GDD §3.2):**
   - Si la bruja intenta abrir un cofre en forma de gato (`TransformationManager.IsCatForm == true`), el cofre **no se abre** y muestra en el HUD `"Necesitas forma humana para abrir esto"` con enfriamiento anti-spam (`2.0 s`).
3. **Tope de 3 frascos por tipo de poción (`MaxPotionChargesPerType = 3`):**
   - Si el cofre contiene una poción cuyo tipo ya está al máximo (`3/3` en `GameManager.Instance`), se muestra `"Frasco al máximo"` y ese frasco **permanece pendiente dentro del cofre** (`HasPendingItems == true`, aviso `"Q: Abrir cofre (Contenido pendiente)"`) para que el jugador pueda regresar por él más adelante sin perderlo.
4. **Política de persistencia (`L2_Chest_<ID>`):**
   - Cada cofre persiste su estado (`0` = cerrado, `1` = abierto con contenido pendiente, `2` = completamente vaciado) y su máscara de ítems entregados (`L2_Chest_<ID>_Mask`) en `PlayerPrefs` y memoria de sesión.
   - El estado sobrevive a reapariciones en checkpoints y transiciones entre calle e interiores, y se reinicia automáticamente cuando se invoca `IngredientInventory.Instance.ResetProgress()` al reiniciar el nivel.

---

## 2. Tabla Completa de Cofres (`Level2.unity`)

| # | Objeto en Escena | `chestId` (`L2_Chest_<ID>`) | Zona / Ubicación | Coordenadas `(x, y)` | Contenido Exacto | Requisito Especial |
| :-: | :--- | :--- | :--- | :---: | :--- | :--- |
| **1** | `Chest_Z1_Intro` | `chest_z1_intro` | Zona 1 — Entrada al Pueblo (calle frente a la Botica) | `(18.5, 0.17)` | • **Hongo** (`L2_Ing_Mushroom_Z1`) ×1<br>• **Corazón** ×1 | Ninguno |
| **2** | `Chest_Z1_StallRoof` | `chest_z1_stall_roof` | Zona 1 — Sobre el toldo del puesto `Stall_Z1_1` | `(9.7, 2.70)` | • **Poción de Dormir** ×1 | Salto al toldo |
| **3** | `Chest_Int_House1_Upper` | `chest_int_house1_upper` | Interior Casa 1 (Botica de Herbolaria, 2º piso) | `(20.6, 32.75)` | • **Hongo** (`L2_Ing_Mushroom_H1`) ×1<br>• **Poción de Cegar** ×1 | Subir escalera interior |
| **4** | `Chest_Z2_MarketStreet` | `chest_z2_market_street` | Zona 2 — Sobre el toldo del puesto `Stall_Z2_2` | `(50.9, 2.70)` | • **Araña** (`L2_Ing_Spider_Z2`) ×1<br>• **Flor Marchita** (`L2_Ing_Flower_Z2`) ×1 | Ruta de toldos en Zona 2 |
| **5** | `Chest_Ext_Z2_Balcony` | `chest_ext_z2_balcony` | Zona 2 — Balcón / Pasarela exterior de la Posada (`RoofWalk_Z2`) | `(46.4, 4.02)` | • **Poción de Cegar** ×1<br>• **Corazón** ×1 | Salir al balcón 2º piso o escalera exterior |
| **6** | `Chest_Int_House2_Locked` | `chest_int_house2_locked` | Interior Casa 2 (Posada del Cuervo, 2º piso) | `(52.4, 32.75)` | • **Poción de Dormir** ×1<br>• **Corazón** ×1 | Requiere `key_tavern_chest` (Bodega subterránea) |
| **7** | `Chest_Z3_PlazaStreet` | `chest_z3_plaza_street` | Zona 3 — Entrada de la Plaza Central | `(67.5, 0.17)` | • **Araña** (`L2_Ing_Spider_Z3`) ×1<br>• **Poción de Cegar** ×1 | Ninguno |
| **8** | `Chest_Ext_Z3_CanalOverlook` | `chest_ext_z3_canal_overlook` | Zona 3 — Puesto elevado izquierdo sobre el canal (`Roof_Z3_LeftHigh`) | `(71.2, 4.22)` | • **Poción de Dormir** ×1 | Escalera `Ladder_Ext_Z3_Left` |
| **9** | `Chest_Z3_EastOverlook` | `chest_z3_east_overlook` | Zona 3 — Puesto elevado derecho tras el canal (`Roof_Z3_RightHigh`) | `(87.0, 4.42)` | • **Raíz** (`L2_Ing_Root_Z3`) ×1<br>• **Corazón** ×1 | Cruzar el canal por toldo alto o escalera `Ladder_Ext_Z3_Right` |
| **10** | `Chest_Z4_AlleyEntry` | `chest_z4_alley_entry` | Zona 4 — Entrada del Callejón antes del puesto elevado | `(97.5, 0.17)` | • **Flor Marchita** (`L2_Ing_Flower_Z4`) ×1<br>• **Poción de Dormir** ×1 | Sigilo en entrada de Zona 4 |
| **11** | `Chest_Z4_CatTunnelExit` | `chest_z4_cat_tunnel_exit` | Zona 4 — Salida del pasaje exclusivo de gato | `(115.2, 0.17)` | • **Raíz** (`L2_Ing_Root_Z4`) ×1 | Cruzar el túnel en forma de gato y volver a forma humana para abrir con `Q` |
| **12** | `Chest_Int_House3_Attic` | `chest_int_house3_attic` | Interior Casa 3 (Casa Abandonada, Ático Secreto 3er piso) | `(112.2, 36.75)` | • **Raíz** (`L2_Ing_Root_H3`) ×1<br>• **Poción de Hipnotizar** ×1 | Subir al ático secreto de Casa 3 |
| **13** | `Chest_Z5_MarketPlaza` | `chest_z5_market_plaza` | Zona 5 — Calle del Gran Mercado | `(133.2, 0.17)` | • **Hongo** (`L2_Ing_Mushroom_Z5`) ×1<br>• **Poción de Hipnotizar** ×1<br>• **Corazón** ×1 | Sigilo entre aldeanos y guardia callejero |
| **14** | `Chest_Z5_HighTower` | `chest_z5_high_tower` | Zona 5 — Pasarela superior del Gran Mercado (`Roof_Z5_HighWalk`) | `(143.5, 5.99)` | • **Hongo** (`L2_Ing_Mushroom_Z5_Bonus`) ×1<br>• **Araña** (`L2_Ing_Spider_Z5_Bonus`) ×1 | Subir por `Ladder_Ext_Z5_West` y `Ladder_Ext_Z5_HighTower` |
| **15** | `Chest_Int_House4_Royal` | `chest_int_house4_royal` | Interior Casa 4 (Mansión del Magistrado, Altillo de Biblioteca) | `(142.5, 34.55)` | • **Poción de Hipnotizar** ×1<br>• **Corazón** ×1 | Requiere `key_manor_master` (Sótano de Archivos) |

---

> Los cofres 2 y 4 acompañan a su puesto, que se reubicó para que no se empalmara con otros. Todos los cofres apoyan su base en el suelo, el toldo o la plataforma (±0.02 u). Los textos que avisan que los objetos están en cofres se describen en `Docs/Nivel2_Textos.md`.

## 3. Balance Total de Recursos en Cofres

- **Ingredientes para `IngredientGate` (requisito: 2 de cada tipo = 8 en total):**
  - **Hongos (`Mushroom`):** 4 unidades repartidas en cofres (mínimo requerido: 2).
  - **Arañas (`Spider`):** 3 unidades repartidas en cofres (mínimo requerido: 2).
  - **Flores Marchitas (`WitheredFlower`):** 2 unidades repartidas en cofres (mínimo requerido: 2).
  - **Raíces (`Root`):** 3 unidades repartidas en cofres (mínimo requerido: 2).
  - **Total de ingredientes en cofres:** **12 unidades** (`100%` dentro de `Level2Chest`).
- **Frascos de Poción (tope activo: 3 por tipo):**
  - **Cegar (`Blind`):** 3 frascos.
  - **Hipnotizar (`Hypnotize`):** 3 frascos.
  - **Dormir (`Sleep`):** 4 frascos.
  - **Total de frascos en cofres:** **10 frascos**.
- **Corazones de Salud (`Heart`):** **6 corazones**.
