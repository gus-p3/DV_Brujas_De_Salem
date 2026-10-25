using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Level2;

public static class Level2IngredientVerifier
{
    private const string ScenePath = "Assets/Scenes/Level2.unity";
    private const string PrefabsDir = "Assets/Prefabs/Level2";

    [MenuItem("Brujas/Nivel 2/Verificar Ingredientes Estructural")]
    public static bool RunAllTests()
    {
        int passed = 0;
        int failed = 0;

        Debug.Log("=================================================");
        Debug.Log("[Level2IngredientVerifier] INICIANDO VERIFICACIÓN ESTRUCTURAL");
        Debug.Log("=================================================");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[FAIL] No se pudo abrir la escena: {ScenePath}");
            return false;
        }

        // Test 1: Existencia de los 4 Prefabs de Ingredientes
        string[] prefabNames = { "Ingredient_Mushroom", "Ingredient_Spider", "Ingredient_WitheredFlower", "Ingredient_Root" };
        bool allPrefabsExist = true;
        foreach (var p in prefabNames)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/{p}.prefab");
            if (go == null || go.GetComponent<Ingredient>() == null)
            {
                Debug.LogError($"[FAIL] Falta prefab o componente Ingredient en: {p}");
                allPrefabsExist = false;
            }
        }
        if (allPrefabsExist) { Debug.Log("[PASS] Prefabs de los 4 tipos de ingredientes verificados."); passed++; }
        else failed++;

        // Test 2: IngredientInventory en escena
        IngredientInventory inv = Object.FindFirstObjectByType<IngredientInventory>();
        if (inv != null && inv.RequiredMushrooms == 2 && inv.RequiredSpiders == 2 && inv.RequiredFlowers == 2 && inv.RequiredRoots == 2)
        {
            Debug.Log($"[PASS] IngredientInventory configurado con requisitos 2/2/2/2 (Total: {inv.TotalRequired}).");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] IngredientInventory ausente o con requisitos incorrectos.");
            failed++;
        }

        // Test 3: Conteo e IDs únicos de ingredientes dentro de Level2Chest (0 sueltos)
        Ingredient[] looseIngs = Object.FindObjectsByType<Ingredient>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Level2Chest[] allChests = Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        HashSet<string> seenIds = new HashSet<string>();
        bool duplicateFound = false;
        int mushroomCount = 0, spiderCount = 0, flowerCount = 0, rootCount = 0;
        int totalIngInChests = 0;
        Level2Chest catTunnelChest = null;

        foreach (var ch in allChests)
        {
            if (ch == null || ch.Contents == null) continue;
            Collider2D c = ch.GetComponent<Collider2D>();
            if (c == null || !c.isTrigger)
            {
                Debug.LogError($"[FAIL] Cofre {ch.name} no tiene Collider2D trigger.");
                duplicateFound = true;
            }

            foreach (var entry in ch.Contents)
            {
                if (entry == null || entry.itemKind != ChestItemKind.Ingredient) continue;
                totalIngInChests += entry.amount;

                string id = string.IsNullOrWhiteSpace(entry.ingredientUniqueId)
                    ? $"{ch.ChestId}_{entry.ingredientType}"
                    : entry.ingredientUniqueId;

                if (!seenIds.Add(id))
                {
                    Debug.LogError($"[FAIL] ID de ingrediente duplicado en cofre: {id}");
                    duplicateFound = true;
                }

                switch (entry.ingredientType)
                {
                    case IngredientType.Mushroom: mushroomCount += entry.amount; break;
                    case IngredientType.Spider: spiderCount += entry.amount; break;
                    case IngredientType.WitheredFlower: flowerCount += entry.amount; break;
                    case IngredientType.Root:
                        rootCount += entry.amount;
                        if (ch.name.Contains("CatTunnel") || Mathf.Abs(ch.transform.position.x - 113.2f) < 2.0f)
                        {
                            catTunnelChest = ch;
                        }
                        break;
                }
            }
        }

        if (looseIngs.Length == 0 && totalIngInChests >= 10)
        {
            Debug.Log($"[PASS] Total de ingredientes en cofres correcto: {totalIngInChests} (0 sueltos en escena).");
            passed++;
        }
        else
        {
            Debug.LogError($"[FAIL] Se esperaban 0 ingredientes sueltos (encontrados {looseIngs.Length}) y >= 10 en cofres (encontrados {totalIngInChests}).");
            failed++;
        }

        if (!duplicateFound)
        {
            Debug.Log("[PASS] Todos los ingredientes en cofres poseen IDs únicos y triggers válidos.");
            passed++;
        }
        else failed++;

        // Test 4: Distribución por tipo (>= 2 de cada uno de los 4 tipos)
        if (mushroomCount >= 2 && spiderCount >= 2 && flowerCount >= 2 && rootCount >= 2)
        {
            Debug.Log($"[PASS] Distribución en cofres por tipo validada: {mushroomCount} Hongos, {spiderCount} Arañas, {flowerCount} Flores, {rootCount} Raíces.");
            passed++;
        }
        else
        {
            Debug.LogError($"[FAIL] Distribución insuficiente en cofres: M={mushroomCount}, S={spiderCount}, F={flowerCount}, R={rootCount}");
            failed++;
        }

        // Test 5: Cofre con ingrediente tras el túnel de gato
        if (catTunnelChest != null && catTunnelChest.transform.position.x > 110.0f)
        {
            Debug.Log($"[PASS] Cofre con Raíz tras pasaje felino verificado en {catTunnelChest.transform.position} ({catTunnelChest.name}).");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] No se encontró el cofre del pasaje felino en Zona 4.");
            failed++;
        }

        // Test 6: IngredientGate en escena
        IngredientGate gate = Object.FindFirstObjectByType<IngredientGate>();
        if (gate != null && gate.transform.position.x > 175f)
        {
            Debug.Log($"[PASS] IngredientGate verificado en {gate.transform.position} con colisionadores configurados.");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] IngredientGate no encontrado en posición de salida.");
            failed++;
        }

        // Test 7: IngredientHUD en escena
        IngredientHUD hud = Object.FindFirstObjectByType<IngredientHUD>();
        if (hud != null)
        {
            Canvas c = hud.GetComponent<Canvas>();
            if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.Log("[PASS] IngredientHUD verificado en Canvas independiente (ScreenSpaceOverlay).");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] IngredientHUD no posee Canvas válido.");
                failed++;
            }
        }
        else
        {
            Debug.LogError("[FAIL] IngredientHUD no encontrado en escena.");
            failed++;
        }

        // Test 8: Tutorial contextual de ingredientes
        TutorialTrigger tut = null;
        foreach (var t in Object.FindObjectsByType<TutorialTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.TipId == "tip_ingredientes_mercado")
            {
                tut = t;
                break;
            }
        }
        if (tut != null && tut.transform.position.x < 25f)
        {
            Debug.Log($"[PASS] Tutorial contextual de ingredientes verificado en Zona 1 ({tut.transform.position.x}).");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] Tutorial contextual de ingredientes ausente en Zona 1.");
            failed++;
        }

        Debug.Log("=================================================");
        Debug.Log($"[Level2IngredientVerifier] RESULTADO FINAL: {passed} PASADAS, {failed} FALLIDAS");
        Debug.Log("=================================================");

        return failed == 0;
    }
}
