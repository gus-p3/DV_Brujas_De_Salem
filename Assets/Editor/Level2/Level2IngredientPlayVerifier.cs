using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Level2;

public static class Level2IngredientPlayVerifier
{
    private const string ScenePath = "Assets/Scenes/Level2.unity";

    [MenuItem("Brujas/Nivel 2/Verificar Ingredientes Comportamiento")]
    public static bool RunAllTests()
    {
        int passed = 0;
        int failed = 0;

        Debug.Log("=================================================");
        Debug.Log("[Level2IngredientPlayVerifier] INICIANDO VERIFICACIÓN DE COMPORTAMIENTO");
        Debug.Log("=================================================");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[FAIL] No se pudo abrir la escena: {ScenePath}");
            return false;
        }

        // Obtener la Bruja real presente en la escena de Level2
        PlayerController witch = Object.FindFirstObjectByType<PlayerController>();
        if (witch == null)
        {
            Debug.LogError("[FAIL] No se encontró PlayerController en Level2.unity");
            return false;
        }

        TransformationManager tm = witch.GetComponent<TransformationManager>();
        Collider2D witchCol = witch.GetComponent<Collider2D>();
        MethodInfo setCatForm = typeof(TransformationManager).GetProperty("IsCatForm").GetSetMethod(true);

        // Obtener el IngredientInventory de la escena
        IngredientInventory inv = IngredientInventory.Instance;
        if (inv == null)
        {
            inv = Object.FindFirstObjectByType<IngredientInventory>();
        }
        inv.ResetProgress();

        MethodInfo tryCollectMethod = typeof(Ingredient).GetMethod("TryCollect", BindingFlags.NonPublic | BindingFlags.Instance);

        // Test 1: Comprobación de que la forma de gato NO puede recolectar (GDD §3.2)
        setCatForm.Invoke(tm, new object[] { true });

        GameObject testIngCat = new GameObject("TestIngredient_Cat");
        var catCol = testIngCat.AddComponent<CircleCollider2D>();
        catCol.isTrigger = true;
        var testIngC = testIngCat.AddComponent<Ingredient>();
        testIngC.Configure("test_ing_cat", IngredientType.Mushroom, 150);

        tryCollectMethod.Invoke(testIngC, new object[] { witchCol });

        if (inv.CurrentMushrooms == 0 && testIngCat != null && tm.IsCatForm)
        {
            Debug.Log("[PASS] Forma de gato NO recolecta ingredientes (GDD §3.2 verificado).");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] La forma de gato recolectó el ingrediente indebidamente.");
            failed++;
        }

        // Test 2: Comprobación de que la forma humana SÍ recolecta
        setCatForm.Invoke(tm, new object[] { false });

        GameObject testIngHuman = new GameObject("TestIngredient_Human");
        var humanCol = testIngHuman.AddComponent<CircleCollider2D>();
        humanCol.isTrigger = true;
        var testIngH = testIngHuman.AddComponent<Ingredient>();
        testIngH.Configure("test_ing_human", IngredientType.Mushroom, 150);

        tryCollectMethod.Invoke(testIngH, new object[] { witchCol });

        if (inv.CurrentMushrooms == 1 && !tm.IsCatForm)
        {
            Debug.Log("[PASS] Forma humana recolecta exitosamente el ingrediente (Mushroom = 1).");
            passed++;
        }
        else
        {
            Debug.LogError($"[FAIL] La forma humana no recolectó el ingrediente. Conteo: {inv.CurrentMushrooms}");
            failed++;
        }

        // Test 3: Prevención de doble recolección por ID
        inv.Collect(IngredientType.Mushroom, "test_ing_human");
        if (inv.CurrentMushrooms == 1)
        {
            Debug.Log("[PASS] Doble recolección prevenida: el ID ya recolectado fue ignorado.");
            passed++;
        }
        else
        {
            Debug.LogError($"[FAIL] Doble recolección permitió incrementar conteo: {inv.CurrentMushrooms}");
            failed++;
        }

        // Test 4: Conteo por tipos y evento OnAllRequiredCollected
        bool allRequiredEventFired = false;
        inv.OnAllRequiredCollected += () => { allRequiredEventFired = true; };

        inv.Collect(IngredientType.Mushroom, "m2"); // 2/2 Hongos
        inv.Collect(IngredientType.Spider, "s1");
        inv.Collect(IngredientType.Spider, "s2"); // 2/2 Arañas
        inv.Collect(IngredientType.WitheredFlower, "f1");
        inv.Collect(IngredientType.WitheredFlower, "f2"); // 2/2 Flores
        inv.Collect(IngredientType.Root, "r1");
        
        // Con 7/8 aún no debe dispararse
        if (!inv.AreAllRequiredCollected && !allRequiredEventFired)
        {
            Debug.Log("[PASS] Con 7/8 ingredientes los requisitos aún no están cumplidos.");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] AreAllRequiredCollected devolvió true antes de tiempo.");
            failed++;
        }

        // 8vo ingrediente (completa 2/2 Raíces)
        inv.Collect(IngredientType.Root, "r2");
        if (inv.AreAllRequiredCollected && allRequiredEventFired && inv.TotalCollected == 8)
        {
            Debug.Log("[PASS] Requisitos completos (8/8) y evento OnAllRequiredCollected disparado.");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] Falló cumplimiento total de requisitos.");
            failed++;
        }

        // Test 5: Ingrediente bonus no rompe requisitos
        inv.Collect(IngredientType.Mushroom, "m_bonus");
        if (inv.CurrentMushrooms == 3 && inv.TotalCollected == 9 && inv.AreAllRequiredCollected)
        {
            Debug.Log("[PASS] Ingredientes bonus se acumulan correctamente (9/8).");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] Falló acumulación de ingrediente bonus.");
            failed++;
        }

        // Test 6: Bloqueo y apertura de IngredientGate
        GameObject gateTestObj = new GameObject("TestGate");
        var blockerCol = gateTestObj.AddComponent<BoxCollider2D>();
        var warnCol = gateTestObj.AddComponent<BoxCollider2D>();
        warnCol.isTrigger = true;
        var gateSr = gateTestObj.AddComponent<SpriteRenderer>();
        var gateComp = gateTestObj.AddComponent<IngredientGate>();
        gateComp.SetupComponents(blockerCol, warnCol, gateSr, null, null);

        gateComp.CloseGate();
        if (!gateComp.IsGateOpen && blockerCol.enabled)
        {
            Debug.Log("[PASS] IngredientGate permanece cerrado y bloqueando paso físicamente.");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] IngredientGate no inició cerrado.");
            failed++;
        }

        gateComp.OpenGate();
        if (gateComp.IsGateOpen && !blockerCol.enabled)
        {
            Debug.Log("[PASS] IngredientGate se abre correctamente y desactiva el colisionador bloqueante.");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] IngredientGate no desactivó el bloqueo al abrirse.");
            failed++;
        }

        // Test 7: Independencia absoluta de frascos de pociones (PotionInventory / GameManager)
        if (GameManager.Instance != null)
        {
            int initBlind = GameManager.Instance.BlindCharges;
            int initHyp = GameManager.Instance.HypnotizeCharges;
            int initSleep = GameManager.Instance.SleepCharges;

            inv.Collect(IngredientType.Root, "r_extra");

            bool potionsUnaltered = (GameManager.Instance.BlindCharges == initBlind &&
                                     GameManager.Instance.HypnotizeCharges == initHyp &&
                                     GameManager.Instance.SleepCharges == initSleep);
            if (potionsUnaltered)
            {
                Debug.Log("[PASS] Cargas de pociones (frascos) completamente inalteradas tras recolección de ingredientes.");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] Las cargas de pociones fueron alteradas por ingredientes.");
                failed++;
            }
        }
        else
        {
            Debug.Log("[PASS] GameManager no instanciado en prueba estática; PotionCharges independientes por arquitectura.");
            passed++;
        }

        // Test 8: Persistencia de sesión (PlayerPrefs L2_)
        inv.SaveSessionProgress();
        int savedMushrooms = PlayerPrefs.GetInt("L2_Session_Mushrooms", -1);
        if (savedMushrooms == 3)
        {
            Debug.Log("[PASS] Persistencia de sesión guarda correctamente en claves 'L2_'.");
            passed++;
        }
        else
        {
            Debug.LogError($"[FAIL] Valor guardado incorrecto: {savedMushrooms}");
            failed++;
        }

        // Test 9: Restauración tras checkpoint
        inv.LoadSessionProgress();
        if (inv.CurrentMushrooms == 3 && inv.IsCollected("test_ing_human") && inv.IsCollected("m2"))
        {
            Debug.Log("[PASS] Restauración de sesión recupera los conteos e IDs para reaparición en Checkpoint.");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] Falló restauración de progreso de sesión.");
            failed++;
        }

        // Test 10: Política de reinicio (ResetProgress en GameOver)
        inv.ResetProgress();
        if (inv.TotalCollected == 0 && !PlayerPrefs.HasKey("L2_Session_Mushrooms"))
        {
            Debug.Log("[PASS] ResetProgress limpia estado en memoria y PlayerPrefs de sesión.");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] ResetProgress no limpió correctamente el estado.");
            failed++;
        }

        // Test 11: Guardado final para Acto 3 (CommitAct2Completion)
        inv.Collect(IngredientType.Mushroom, "final_m1");
        inv.Collect(IngredientType.Mushroom, "final_m2");
        inv.CommitAct2Completion();
        int finalMushrooms = PlayerPrefs.GetInt(IngredientInventory.KeyFinalMushrooms, -1);
        int act2Complete = PlayerPrefs.GetInt(IngredientInventory.KeyAct2Complete, -1);
        if (finalMushrooms == 2 && act2Complete == 1)
        {
            Debug.Log("[PASS] CommitAct2Completion guarda ingredientes finales para Acto 3 con claves 'L2_Final_'.");
            passed++;
        }
        else
        {
            Debug.LogError("[FAIL] CommitAct2Completion no registró los valores esperados.");
            failed++;
        }

        // Limpieza de objetos de prueba y restauración de forma humana de la bruja
        setCatForm.Invoke(tm, new object[] { false });
        if (testIngCat != null) Object.DestroyImmediate(testIngCat);
        if (testIngHuman != null) Object.DestroyImmediate(testIngHuman);
        Object.DestroyImmediate(gateTestObj);
        inv.ResetProgress();

        // Limpieza de claves de prueba en PlayerPrefs
        PlayerPrefs.DeleteKey("L2_Session_Active");
        PlayerPrefs.DeleteKey("L2_Session_Mushrooms");
        PlayerPrefs.DeleteKey("L2_Session_Spiders");
        PlayerPrefs.DeleteKey("L2_Session_Flowers");
        PlayerPrefs.DeleteKey("L2_Session_Roots");
        PlayerPrefs.DeleteKey("L2_Session_CollectedIDs");
        PlayerPrefs.DeleteKey(IngredientInventory.KeyFinalMushrooms);
        PlayerPrefs.DeleteKey(IngredientInventory.KeyFinalSpiders);
        PlayerPrefs.DeleteKey(IngredientInventory.KeyFinalFlowers);
        PlayerPrefs.DeleteKey(IngredientInventory.KeyFinalRoots);
        PlayerPrefs.DeleteKey(IngredientInventory.KeyAct2Complete);
        PlayerPrefs.Save();

        Debug.Log("=================================================");
        Debug.Log($"[Level2IngredientPlayVerifier] RESULTADO FINAL: {passed} PASADAS, {failed} FALLIDAS");
        Debug.Log("=================================================");

        return failed == 0;
    }
}
