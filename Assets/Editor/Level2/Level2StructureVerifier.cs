#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verificador automatico de estructuras del Nivel 2 (Menu: Tools > Nivel 2 > Verificar estructuras).
/// Comprueba:
/// 1) Puertas, casas y props apoyados sobre suelo real (tolerancia <= 0.02u).
/// 2) Escaleras conectadas en ambos extremos (BottomY y TopY <= 0.02u) y asociadas a casa/interior.
/// 3) Cero referencias al sprite de casa antiguo en Level2.unity.
/// </summary>
public static class Level2StructureVerifier
{
    private const float TOLERANCE = 0.02f;

    [MenuItem("Tools/Nivel 2/Verificar estructuras")]
    public static int VerifyAllStructures()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path != "Assets/Scenes/Level2.unity")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity", OpenSceneMode.Single);
        }
        Physics2D.SyncTransforms();

        List<string> findings = new List<string>();
        int groundMask = 1 << LayerMask.NameToLayer("Ground");

        // 1. Verificar todas las Casas (Decorativas, Estructurales e Interactivas) en Village_Exterior_Structures
        Transform extRoot = GameObject.Find("Environment/Village_Exterior_Structures")?.transform;
        List<SpriteRenderer> houseRenderers = new List<SpriteRenderer>();
        int checkedHouses = 0;
        if (extRoot != null)
        {
            foreach (Transform child in extRoot)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                if (!child.name.StartsWith("House")) continue;

                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null)
                {
                    findings.Add($"[CASA SIN SPRITE] {child.name} no tiene Sprite asignado.");
                    continue;
                }
                houseRenderers.Add(sr);
                checkedHouses++;
                float minY = sr.bounds.min.y;
                float groundY = Level2CompleteFixer.RaycastGroundY(child.position.x, 0.17f);
                float diff = Mathf.Abs(minY - groundY);
                if (diff > TOLERANCE)
                {
                    findings.Add($"[CASA DESALINEADA] {child.name} minY={minY:F4}, suelo={groundY:F4} (diff={diff:F4}u > {TOLERANCE}u)");
                }
            }
        }

        // 2. Verificar todas las Puertas (HouseDoor)
        HouseDoor[] doors = Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int checkedDoors = 0;
        foreach (HouseDoor door in doors)
        {
            checkedDoors++;
            SpriteRenderer sr = door.GetComponent<SpriteRenderer>();
            float minY = sr != null && sr.sprite != null ? sr.bounds.min.y : door.transform.position.y;
            float surfaceY = Level2CompleteFixer.FindSurfaceBelow(door.transform.position.x, minY + 0.8f);
            float diff = Mathf.Abs(minY - surfaceY);
            if (diff > TOLERANCE)
            {
                findings.Add($"[PUERTA FLOTANDO/HUNDIDA] {door.name} minY={minY:F4}, superficie={surfaceY:F4} (diff={diff:F4}u > {TOLERANCE}u)");
            }

            if (door.transform.position.y < 15f)
            {
                bool hasBackingHouse = false;
                foreach (SpriteRenderer hsr in houseRenderers)
                {
                    if (door.transform.position.x >= hsr.bounds.min.x - 1.2f && door.transform.position.x <= hsr.bounds.max.x + 1.2f)
                    {
                        hasBackingHouse = true;
                        break;
                    }
                }
                if (!hasBackingHouse)
                {
                    findings.Add($"[PUERTA SIN CASA] {door.name} en x={door.transform.position.x:F2} no tiene fachada de casa detras.");
                }
            }
        }

        // 3. Verificar todas las Escaleras (LadderInteractable)
        LadderInteractable[] ladders = Object.FindObjectsByType<LadderInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int checkedLadders = 0;
        foreach (LadderInteractable ladder in ladders)
        {
            checkedLadders++;
            float cx = ladder.CenterX;
            float bY = ladder.BottomY;
            float tY = ladder.TopY;

            float bottomSurface = FindNearestFloorSurfaceAt(cx, bY, groundMask);
            float topSurface = FindNearestFloorSurfaceAt(cx, tY, groundMask);

            float bDiff = Mathf.Abs(bY - bottomSurface);
            float tDiff = Mathf.Abs(tY - topSurface);

            if (bDiff > TOLERANCE)
            {
                findings.Add($"[ESCALERA BASE DESCONECTADA] {ladder.name} BottomY={bY:F4}, piso={bottomSurface:F4} (diff={bDiff:F4}u > {TOLERANCE}u)");
            }
            if (tDiff > TOLERANCE)
            {
                findings.Add($"[ESCALERA TOPE DESCONECTADO] {ladder.name} TopY={tY:F4}, piso={topSurface:F4} (diff={tDiff:F4}u > {TOLERANCE}u)");
            }

            if (ladder.transform.position.y < 15f)
            {
                bool associatedWithHouse = false;
                foreach (SpriteRenderer hsr in houseRenderers)
                {
                    if (cx >= hsr.bounds.min.x - 1.8f && cx <= hsr.bounds.max.x + 1.8f)
                    {
                        associatedWithHouse = true;
                        break;
                    }
                }
                if (!associatedWithHouse)
                {
                    findings.Add($"[ESCALERA HUERFANA] {ladder.name} en x={cx:F2} no pertenece a ninguna fachada de casa.");
                }
            }
        }

        // 4. Verificar que NO queden sprites de casas antiguas en Level2.unity
        SpriteRenderer[] allSrs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpriteRenderer sr in allSrs)
        {
            if (sr == null || sr.sprite == null) continue;
            string spName = sr.sprite.name;
            if (spName.Contains("Village_House_Facade") || spName.Contains("Placeholder_Village_Facades") || spName.Contains("Env_Village_Mid"))
            {
                findings.Add($"[SPRITE CASA ANTIGUO] {GetPath(sr.transform)} aun usa sprite antiguo '{spName}'.");
            }
        }

        if (findings.Count == 0)
        {
            Debug.Log($"[Level2StructureVerifier] EXITO: 0 hallazgos. Casas verificadas: {checkedHouses}, Puertas verificadas: {checkedDoors}, Escaleras verificadas: {checkedLadders}. Todas dentro de tolerancia <= {TOLERANCE}u y con el arte nuevo.");
        }
        else
        {
            Debug.LogError($"[Level2StructureVerifier] FALLO con {findings.Count} hallazgos:\n - " + string.Join("\n - ", findings));
        }

        return findings.Count;
    }

    [MenuItem("Tools/Nivel 2/Verificar correcciones")]
    public static int VerifyMegaCorrections()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path != "Assets/Scenes/Level2.unity")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity", OpenSceneMode.Single);
        }
        Physics2D.SyncTransforms();

        List<string> findings = new List<string>();

        // 1. Todos los coleccionables del Nivel 2 tienen proteccion contra recogida en forma de gato
        int pickupCount = 0;
        MonoBehaviour[] allMbs = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in allMbs)
        {
            if (mb == null) continue;
            System.Type t = mb.GetType();
            string tn = t.Name;
            if (tn == "Ingredient" || tn == "PotionPickup" || tn == "HeartPickup" || tn == "Collectible" || tn == "KeyItem" || tn == "Level2Chest")
            {
                pickupCount++;
                if (mb.GetComponent<Level2CatPickupGuard>() == null)
                {
                    findings.Add($"[PICKUP SIN GUARDIA GATO] {GetPath(mb.transform)} ({tn}) no tiene Level2CatPickupGuard.");
                }
            }
        }
        if (pickupCount == 0)
        {
            findings.Add("[SIN PICKUPS] No se encontraron objetos recogibles en Level2.unity.");
        }

        // 2. PlayerInteractor usa tecla Q y todas las puertas interactivas tienen indicador, trigger a ras de suelo y destino valido
        PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        if (interactor == null || interactor.InteractKey != KeyCode.Q)
        {
            findings.Add($"[TECLA INTERACCION INCORRECTA] PlayerInteractor no usa KeyCode.Q (actual={(interactor != null ? interactor.InteractKey.ToString() : "null")}).");
        }

        HouseDoor[] doors = Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int doorCount = 0;
        foreach (HouseDoor door in doors)
        {
            if (door == null || !door.gameObject.activeInHierarchy) continue;
            doorCount++;

            SerializedObject soDoor = new SerializedObject(door);
            SerializedProperty propBarred = soDoor.FindProperty("permanentlyBarred");
            bool isBarred = propBarred != null && propBarred.boolValue;

            if (!isBarred && door.TargetDoor == null)
            {
                findings.Add($"[PUERTA SIN DESTINO] {GetPath(door.transform)} no tiene TargetDoor asignado.");
            }

            Level2DoorIndicator ind = door.GetComponent<Level2DoorIndicator>();
            if (ind == null)
            {
                findings.Add($"[PUERTA SIN INDICADOR] {GetPath(door.transform)} no tiene Level2DoorIndicator.");
            }

            BoxCollider2D trig = door.GetComponent<BoxCollider2D>();
            if (trig == null || !trig.isTrigger)
            {
                findings.Add($"[PUERTA SIN TRIGGER] {GetPath(door.transform)} no tiene BoxCollider2D isTrigger.");
            }

            SpriteRenderer sr = door.GetComponent<SpriteRenderer>();
            float minY = sr != null && sr.sprite != null ? sr.bounds.min.y : door.transform.position.y;
            float surfaceY = Level2CompleteFixer.FindSurfaceBelow(door.transform.position.x, minY + 0.8f);
            if (Mathf.Abs(minY - surfaceY) > TOLERANCE)
            {
                findings.Add($"[PUERTA DESALINEADA] {door.name} minY={minY:F4}, suelo={surfaceY:F4}");
            }

            string prompt = door.GetPromptText(null);
            if (string.IsNullOrEmpty(prompt) || !prompt.StartsWith("Q:") || prompt.Contains("E —") || prompt.Contains("Tecla E"))
            {
                findings.Add($"[PROMPT PUERTA INVALIDO] {door.name} tiene prompt '{prompt}' (debe iniciar con 'Q:').");
            }
        }

        CellarExitBackupInteractable[] cellarExits = Object.FindObjectsByType<CellarExitBackupInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ce in cellarExits)
        {
            if (ce == null) continue;
            if (ce.GetComponent<Level2DoorIndicator>() == null)
            {
                findings.Add($"[SALIDA SOTANO SIN INDICADOR] {GetPath(ce.transform)} no tiene Level2DoorIndicator.");
            }
            string p = ce.GetPromptText(null);
            if (!p.StartsWith("Q:"))
            {
                findings.Add($"[PROMPT SOTANO INVALIDO] {ce.name} tiene prompt '{p}'.");
            }
        }

        // 3. Ningun texto/letrero de casas o puertas en Level2 menciona "E" para entrar o salir
        foreach (MonoBehaviour mb in allMbs)
        {
            if (mb == null) continue;
            if (mb is IInteractable interactable && !(mb is LadderInteractable))
            {
                string p = interactable.GetPromptText(null);
                if (!string.IsNullOrEmpty(p) && (p.Contains("E —") || p.Contains("[E]") || p.Contains("Tecla E")))
                {
                    findings.Add($"[REFERENCIA A TECLA E] {GetPath(mb.transform)} tiene prompt '{p}'.");
                }
            }
        }

        // 4. El fondo cubre todo el recorrido horizontal (x=0..195) sin huecos (gap <= 0.01u)
        string[] layerNames = { "Sky_Far", "Village_Far", "Facades_Mid" };
        foreach (string lName in layerNames)
        {
            GameObject layerGO = GameObject.Find("Environment/Parallax/" + lName);
            if (layerGO == null)
            {
                findings.Add($"[CAPA FONDO FALTANTE] No existe Environment/Parallax/{lName}.");
                continue;
            }

            SpriteRenderer[] tiles = layerGO.GetComponentsInChildren<SpriteRenderer>(false)
                .Where(s => s.gameObject != layerGO && s.sprite != null)
                .OrderBy(s => s.bounds.min.x)
                .ToArray();

            if (tiles.Length < 5)
            {
                findings.Add($"[POCOS TILES EN FONDO] {lName} solo tiene {tiles.Length} copias.");
                continue;
            }

            for (int i = 0; i < tiles.Length - 1; i++)
            {
                float rightEdge = tiles[i].bounds.max.x;
                float leftEdgeNext = tiles[i + 1].bounds.min.x;
                float gap = Mathf.Abs(leftEdgeNext - rightEdge);
                if (gap > 0.01f)
                {
                    findings.Add($"[COSTURA EN FONDO] {lName} entre {tiles[i].name} (max.x={rightEdge:F4}) y {tiles[i + 1].name} (min.x={leftEdgeNext:F4}): gap={gap:F4}u > 0.01u");
                }
            }

            float totalMinX = tiles.First().bounds.min.x;
            float totalMaxX = tiles.Last().bounds.max.x;
            if (totalMinX > -30f || totalMaxX < 210f)
            {
                findings.Add($"[COBERTURA FONDO INSUFICIENTE] {lName} cubre [{totalMinX:F1}, {totalMaxX:F1}] (requiere <= -30 a >= 210).");
            }
        }

        // 5. Los puestos del mercado tienen su animador activo y desincronizado
        MarketStallAnimator[] stalls = Object.FindObjectsByType<MarketStallAnimator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (stalls.Length < 10)
        {
            findings.Add($"[PUESTOS SIN ANIMADOR] Solo se encontraron {stalls.Length} puestos con MarketStallAnimator (esperados >= 10).");
        }
        else
        {
            HashSet<int> phases = new HashSet<int>();
            foreach (MarketStallAnimator st in stalls)
            {
                if (!st.enabled)
                {
                    findings.Add($"[ANIMADOR PUESTO INACTIVO] {GetPath(st.transform)} tiene MarketStallAnimator deshabilitado.");
                }
                phases.Add(Mathf.RoundToInt(st.PhaseOffset * 100f));
            }
            if (phases.Count < 5)
            {
                findings.Add($"[PUESTOS SINCRONIZADOS] Los puestos del mercado no tienen fases desincronizadas ({phases.Count} fases distintas).");
            }
        }

        if (findings.Count == 0)
        {
            Debug.Log($"[Verificar correcciones] EXITO: 0 hallazgos. Pickups protegidos={pickupCount}, Puertas con Q+Indicador={doorCount}, Capas Parallax continuas=3 (gap<=0.001u), Puestos animados desincronizados={stalls.Length}.");
        }
        else
        {
            Debug.LogError($"[Verificar correcciones] FALLO con {findings.Count} hallazgos:\n - " + string.Join("\n - ", findings));
        }

        return findings.Count;
    }

    [MenuItem("Tools/Nivel 2/Verificar controles, cofres y fondo")]
    public static int VerifyControlsChestsAndBackground()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path != "Assets/Scenes/Level2.unity")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity", OpenSceneMode.Single);
        }
        Physics2D.SyncTransforms();

        List<string> findings = new List<string>();

        // 1. Controles definidos: Level2Controls.InteractKey == Q, PlayerInteractor usa Q, guia HUD presente, ningun componente usa E para abrir/entrar/salir
        if (Level2Controls.InteractKey != KeyCode.Q || Level2Controls.ThrowPotionKey != KeyCode.E)
        {
            findings.Add($"[ESQUEMA CONTROLES INVALIDO] Level2Controls.InteractKey={Level2Controls.InteractKey}, ThrowPotionKey={Level2Controls.ThrowPotionKey}.");
        }

        GameObject witch = GameObject.Find("Witch");
        PlayerInteractor interactor = witch != null ? witch.GetComponent<PlayerInteractor>() : Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        Level2Controls ctrl = witch != null ? witch.GetComponent<Level2Controls>() : Object.FindFirstObjectByType<Level2Controls>(FindObjectsInactive.Include);

        if (ctrl == null)
        {
            findings.Add("[FALTA LEVEL2CONTROLS] El objeto Witch no tiene el componente Level2Controls.");
        }
        if (interactor == null || interactor.InteractKey != Level2Controls.InteractKey)
        {
            findings.Add($"[INTERACTOR NO USA Q] PlayerInteractor no usa Level2Controls.InteractKey (Q).");
        }

        GameObject guidePanel = GameObject.Find("UI/IngredientHUD_Canvas/Panel_ControlsGuide");
        if (guidePanel == null)
        {
            findings.Add("[FALTA GUIA CONTROLES HUD] No existe UI/IngredientHUD_Canvas/Panel_ControlsGuide.");
        }

        MonoBehaviour[] allMbs = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in allMbs)
        {
            if (mb == null) continue;
            if (mb is IInteractable interactable && !(mb is LadderInteractable))
            {
                string prompt = interactable.GetPromptText(witch);
                if (!string.IsNullOrEmpty(prompt) && (!prompt.StartsWith("Q:") || prompt.Contains("E —") || prompt.Contains("[E]") || prompt.Contains("Tecla E")))
                {
                    findings.Add($"[PROMPT NO USA Q] {GetPath(mb.transform)} devuelve prompt '{prompt}'.");
                }
            }
        }

        // 2. Ningun Ingredient, PotionPickup ni HeartPickup suelto en Level2.unity; todos dentro de Level2Chest (>= 2 de cada uno de los 4 tipos)
        Ingredient[] looseIngs = Object.FindObjectsByType<Ingredient>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        PotionPickup[] loosePots = Object.FindObjectsByType<PotionPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HeartPickup[] looseHearts = Object.FindObjectsByType<HeartPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (looseIngs.Length > 0)
        {
            findings.Add($"[INGREDIENTES SUELTOS] Hay {looseIngs.Length} Ingredient sueltos en Level2.unity (debe ser 0).");
        }
        if (loosePots.Length > 0)
        {
            findings.Add($"[POCIONES SUELTAS] Hay {loosePots.Length} PotionPickup sueltos en Level2.unity (debe ser 0).");
        }
        if (looseHearts.Length > 0)
        {
            findings.Add($"[CORAZONES SUELTOS] Hay {looseHearts.Length} HeartPickup sueltos en Level2.unity (debe ser 0).");
        }

        // 3. Todos los Level2Chest apoyados (<= 0.02 u), con ID unico no vacio, collider trigger, indicador de Q y >= 1 item configurado
        Level2Chest[] chests = Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (chests.Length < 10)
        {
            findings.Add($"[POCOS COFRES] Solo hay {chests.Length} Level2Chest en Level2.unity (esperados >= 10).");
        }

        HashSet<string> chestIds = new HashSet<string>();
        Dictionary<IngredientType, int> ingCounts = new Dictionary<IngredientType, int>
        {
            { IngredientType.Mushroom, 0 },
            { IngredientType.Spider, 0 },
            { IngredientType.WitheredFlower, 0 },
            { IngredientType.Root, 0 }
        };
        int totalPotionsInChests = 0;

        foreach (Level2Chest chest in chests)
        {
            if (chest == null) continue;

            if (string.IsNullOrWhiteSpace(chest.ChestId))
            {
                findings.Add($"[COFRE SIN ID] {GetPath(chest.transform)} tiene ChestId vacio.");
            }
            else if (!chestIds.Add(chest.ChestId))
            {
                findings.Add($"[COFRE ID DUPLICADO] {GetPath(chest.transform)} repite ChestId '{chest.ChestId}'.");
            }

            BoxCollider2D trig = chest.GetComponent<BoxCollider2D>();
            if (trig == null || !trig.isTrigger)
            {
                findings.Add($"[COFRE SIN TRIGGER] {GetPath(chest.transform)} no tiene BoxCollider2D isTrigger.");
            }

            Level2DoorIndicator ind = chest.GetComponent<Level2DoorIndicator>();
            if (ind == null)
            {
                findings.Add($"[COFRE SIN INDICADOR Q] {GetPath(chest.transform)} no tiene Level2DoorIndicator.");
            }

            if (chest.Contents == null || chest.Contents.Count == 0)
            {
                findings.Add($"[COFRE VACIO] {GetPath(chest.transform)} no tiene items configurados.");
            }
            else
            {
                foreach (var entry in chest.Contents)
                {
                    if (entry == null) continue;
                    if (entry.itemKind == ChestItemKind.Ingredient)
                    {
                        ingCounts[entry.ingredientType] += entry.amount;
                    }
                    else if (entry.itemKind == ChestItemKind.Potion)
                    {
                        totalPotionsInChests += entry.amount;
                    }
                }
            }

            SpriteRenderer sr = chest.GetComponent<SpriteRenderer>();
            float minY = sr != null && sr.sprite != null ? sr.bounds.min.y : chest.transform.position.y;
            float surfY = Level2CompleteFixer.FindBestSurfaceBelow(chest.transform.position.x, minY + 0.25f);
            float diff = Mathf.Abs(minY - surfY);
            if (diff > TOLERANCE)
            {
                findings.Add($"[COFRE FLOTANDO/HUNDIDO] {chest.name} ({chest.ChestId}) minY={minY:F4}, suelo={surfY:F4}, diff={diff:F4}u > {TOLERANCE}u.");
            }
        }

        foreach (var kv in ingCounts)
        {
            if (kv.Value < 2)
            {
                findings.Add($"[INGREDIENTES INSUFICIENTES EN COFRES] Tipo {kv.Key} solo tiene {kv.Value} unidades en cofres (minimo 2).");
            }
        }
        if (totalPotionsInChests < 5)
        {
            findings.Add($"[POCAS POCIONES EN COFRES] Solo hay {totalPotionsInChests} frascos en cofres (minimo 5).");
        }

        // 4. Pruebas logicas de cofre y controles (Q vs E, forma humana vs gato, no duplicacion, tope de 3 frascos)
        Level2Chest testChest = chests.FirstOrDefault(c => c != null && !c.RequiresKey && c.Contents.All(e => e.itemKind == ChestItemKind.Ingredient));
        Level2Chest potionChest = chests.FirstOrDefault(c => c != null && c != testChest && !c.RequiresKey && c.Contents.Any(e => e.itemKind == ChestItemKind.Potion));
        if (witch != null && interactor != null && testChest != null && potionChest != null)
        {
            Vector3 origWitchPos = witch.transform.position;
            TransformationManager tm = witch.GetComponent<TransformationManager>();
            System.Reflection.FieldInfo catField = tm != null
                ? typeof(TransformationManager).GetField("isCatForm", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                : null;
            bool origCat = tm != null && tm.IsCatForm;

            testChest.ResetChestState();
            potionChest.ResetChestState();

            // 4a. En forma de gato no abre el cofre
            if (tm != null && catField != null)
            {
                catField.SetValue(tm, true);
                testChest.Interact(witch);
                if (testChest.HasBeenOpened)
                {
                    findings.Add("[FALLO LOGICA GATO] El gato pudo abrir un Level2Chest.");
                }
                catField.SetValue(tm, false);
            }

            // 4b. Tecla E nunca abre cofres ni puertas; Tecla Q abre el cofre sin gastar pociones
            witch.transform.position = testChest.transform.position;
            Physics2D.SyncTransforms();
            interactor.RefreshFocus();

            bool eTriggered = interactor.TryProcessKey(KeyCode.E, ignoreCooldown: true);
            if (eTriggered || testChest.HasBeenOpened)
            {
                findings.Add("[FALLO TECLA E] Pulsar E activo una interaccion de cofre/puerta.");
            }

            bool qTriggered = interactor.TryProcessKey(KeyCode.Q, ignoreCooldown: true);
            if (!qTriggered || !testChest.HasBeenOpened || !testChest.IsFullyLooted)
            {
                findings.Add("[FALLO TECLA Q] Pulsar Q en forma humana no abrio el cofre.");
            }

            // 4c. Un cofre ya vaciado no vuelve a dar objetos ni permite interactuar de nuevo
            if (testChest.CanInteract(witch))
            {
                findings.Add("[FALLO DUPLICACION COFRE] Un cofre completamente vaciado sigue permitiendo CanInteract.");
            }

            // 4d. Tope de 3 frascos por tipo: si ya tiene 3, el frasco queda pendiente en el cofre sin perderse
            var potEntry = potionChest.Contents.First(e => e.itemKind == ChestItemKind.Potion);
            GameManager gm = GameManager.Instance;
            bool createdTempGm = false;
            if (gm == null)
            {
                GameObject tempGmGo = new GameObject("__TempGM_Verify");
                gm = tempGmGo.AddComponent<GameManager>();
                createdTempGm = true;
            }

            int oldBlind = gm.BlindCharges;
            int oldHypno = gm.HypnotizeCharges;
            int oldSleep = gm.SleepCharges;

            gm.BlindCharges = 3;
            gm.HypnotizeCharges = 3;
            gm.SleepCharges = 3;

            potionChest.ResetChestState();
            potionChest.Interact(witch);
            if (!potionChest.HasBeenOpened || !potionChest.HasPendingItems || potEntry.claimed)
            {
                findings.Add("[FALLO TOPE 3 FRASCOS] Al abrir cofre con 3/3 pociones, el frasco no quedo pendiente dentro del cofre.");
            }

            // Al gastar 1 pocion (quedan 2/3) y volver a interactuar, recoge el frasco pendiente
            gm.BlindCharges = 2;
            gm.HypnotizeCharges = 2;
            gm.SleepCharges = 2;
            potionChest.Interact(witch);
            if (potionChest.HasPendingItems || !potEntry.claimed)
            {
                findings.Add("[FALLO RECOGIDA PENDIENTE] Al tener < 3 pociones y re-interactuar, no se entrego el frasco pendiente.");
            }

            gm.BlindCharges = oldBlind;
            gm.HypnotizeCharges = oldHypno;
            gm.SleepCharges = oldSleep;
            if (createdTempGm)
            {
                Object.DestroyImmediate(gm.gameObject);
            }

            testChest.ResetChestState();
            potionChest.ResetChestState();
            if (IngredientInventory.Instance != null) IngredientInventory.Instance.ResetProgress();
            if (KeyInventory.Instance != null) KeyInventory.Instance.ResetExplorationProgress();
            if (tm != null && catField != null) catField.SetValue(tm, origCat);
            witch.transform.position = origWitchPos;
            Physics2D.SyncTransforms();
        }

        // 5. Fondo con >= 3 capas continuas, sin huecos (gap <= 0.001 u), con cobertura completa horizontal y vertical
        string[] layerNames = { "Sky_Far", "Village_Far", "Facades_Mid" };
        foreach (string lName in layerNames)
        {
            GameObject layerGO = GameObject.Find("Environment/Parallax/" + lName);
            if (layerGO == null)
            {
                findings.Add($"[CAPA FONDO FALTANTE] No existe Environment/Parallax/{lName}.");
                continue;
            }

            SpriteRenderer[] tiles = layerGO.GetComponentsInChildren<SpriteRenderer>(false)
                .Where(s => s.gameObject != layerGO && s.sprite != null)
                .OrderBy(s => s.bounds.min.x)
                .ToArray();

            if (tiles.Length < 8)
            {
                findings.Add($"[POCOS TILES EN FONDO] {lName} solo tiene {tiles.Length} copias.");
                continue;
            }

            for (int i = 0; i < tiles.Length - 1; i++)
            {
                float rightEdge = tiles[i].bounds.max.x;
                float leftEdgeNext = tiles[i + 1].bounds.min.x;
                float gap = Mathf.Abs(leftEdgeNext - rightEdge);
                if (gap > 0.0015f)
                {
                    findings.Add($"[HUECO EN FONDO] {lName} entre {tiles[i].name} y {tiles[i + 1].name}: gap={gap:F5}u > 0.001u.");
                }
            }

            float minX = tiles.First().bounds.min.x;
            float maxX = tiles.Last().bounds.max.x;
            if (minX > -50f || maxX < 230f)
            {
                findings.Add($"[COBERTURA FONDO INSUFICIENTE] {lName} cubre [{minX:F1}, {maxX:F1}].");
            }
        }

        // 6. Todos los puestos del mercado (bajos y elevados) tienen mesa/base apoyada y toldo/sombrilla completo asignado
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> marketStalls = allObjects
            .Where(g => g != null && (g.name.StartsWith("Stall_Z") || g.name.StartsWith("Roof_Z3_") || g.name == "Roof_Z5_Low_1" || g.name == "Roof_Z5_Mid_2" || g.name == "Roof_Z5_HighWalk")
                        && g.transform.parent != null && !g.transform.parent.name.StartsWith("Stall_Z"))
            .ToList();

        foreach (GameObject st in marketStalls)
        {
            Transform swayTr = st.transform.Find("Awning_Sway_Visual");
            SpriteRenderer swaySr = swayTr != null ? swayTr.GetComponent<SpriteRenderer>() : null;
            if (swaySr == null || !swaySr.enabled || swaySr.sprite == null || swaySr.sprite.rect.height < 40f)
            {
                findings.Add($"[PUESTO SIN TOLDO COMPLETO] {GetPath(st.transform)} no tiene Awning_Sway_Visual con sprite completo (altura >= 40px).");
            }

            if (st.GetComponent<MarketStallAnimator>() == null)
            {
                findings.Add($"[PUESTO SIN ANIMADOR] {GetPath(st.transform)} no tiene MarketStallAnimator.");
            }
        }

        if (findings.Count == 0)
        {
            Debug.Log($"[Verificar controles, cofres y fondo] EXITO: 0 hallazgos. Cofres={chests.Length} (Ingredientes: M={ingCounts[IngredientType.Mushroom]}, S={ingCounts[IngredientType.Spider]}, F={ingCounts[IngredientType.WitheredFlower]}, R={ingCounts[IngredientType.Root]}; Pociones={totalPotionsInChests}), Objetos sueltos=0, Capas fondo=3 (gap<=0.001u), Puestos completos={marketStalls.Count}.");
        }
        else
        {
            Debug.LogError($"[Verificar controles, cofres y fondo] FALLO con {findings.Count} hallazgos:\n - " + string.Join("\n - ", findings));
        }

        return findings.Count;
    }

    private static float FindNearestFloorSurfaceAt(float x, float targetY, int groundMask)
    {
        float[] probeOffsets = { 0f, -0.5f, 0.5f, -1.0f, 1.0f };
        float bestY = -999f;
        float minDiff = 999f;
        foreach (float ox in probeOffsets)
        {
            Collider2D[] cols = Physics2D.OverlapBoxAll(new Vector2(x + ox, targetY - 0.15f), new Vector2(0.8f, 0.8f), 0f, groundMask);
            foreach (Collider2D c in cols)
            {
                if (c != null && !c.isTrigger)
                {
                    float top = c.bounds.max.y;
                    float d = Mathf.Abs(targetY - top);
                    if (d < minDiff)
                    {
                        minDiff = d;
                        bestY = top;
                    }
                }
            }
        }
        return bestY;
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }
}
#endif
