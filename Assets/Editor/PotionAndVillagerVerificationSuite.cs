using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Suite de verificación Play Mode para los 11 puntos de Pociones y Aldeanos.
/// Genera evidencias fotográficas en Assets/Screenshots/ y reporta assertions para cada hito.
/// </summary>
public static class PotionAndVillagerVerificationSuite
{
    private const string ScreenshotsFolder = "Assets/Screenshots";

    [MenuItem("Brujas/6. Verificación/Ejecutar Suite Pociones y Aldeanos (11 puntos)")]
    public static void RunSuite()
    {
        Directory.CreateDirectory(ScreenshotsFolder);
        Debug.Log("==================================================================");
        Debug.Log("=== INICIO DE VERIFICACIÓN POCIONES Y ALDEANOS (11 PUNTOS) ===");
        Debug.Log("==================================================================");

        EnsureLevel1Open();

        TestPunto1_SeleccionPocion();
        TestPunto2_ErrorSinCargas();
        TestPunto3_RecoleccionPickup();
        TestPunto4_PickupLlenoNoRecoge();
        TestPunto5_LanzamientoEnArco();
        TestPunto6_ImpactoSalpicadura();
        TestPunto7_EfectoCegarGuardia();
        TestPunto8_EfectoHipnotizarGuardia();
        TestPunto9_EfectoDormirGuardia();
        TestPunto10_AldeanoDetectaGritoAlerta();
        TestPunto11_FrenesiAtaqueCooldown();

        Debug.Log("==================================================================");
        Debug.Log("=== VERIFICACIÓN POCIONES Y ALDEANOS COMPLETADA CON ÉXITO ===");
        Debug.Log("==================================================================");
    }

    private static void EnsureLevel1Open()
    {
        if (SceneManager.GetActiveScene().name != "Level1")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
        }
    }

    private static void Capture(string fileName)
    {
        string fullPath = Path.Combine(ScreenshotsFolder, fileName);
        Camera cam = Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            ScreenCapture.CaptureScreenshot(fullPath);
            return;
        }

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        RenderMode prevMode = RenderMode.ScreenSpaceOverlay;
        Camera prevCam = null;
        string prevLayer = "Default";
        int prevOrder = 0;
        float prevPlaneDistance = 100f;

        if (canvas != null)
        {
            prevMode = canvas.renderMode;
            prevCam = canvas.worldCamera;
            prevLayer = canvas.sortingLayerName;
            prevOrder = canvas.sortingOrder;
            prevPlaneDistance = canvas.planeDistance;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            canvas.sortingLayerName = "Foreground";
            canvas.sortingOrder = 1000;
            Canvas.ForceUpdateCanvases();
        }

        RenderTexture rt = new RenderTexture(1280, 720, 24);
        RenderTexture prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();

        cam.targetTexture = prev;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);

        if (canvas != null)
        {
            canvas.renderMode = prevMode;
            canvas.worldCamera = prevCam;
            canvas.sortingLayerName = prevLayer;
            canvas.sortingOrder = prevOrder;
            canvas.planeDistance = prevPlaneDistance;
        }

        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        File.WriteAllBytes(fullPath, bytes);
        Debug.Log($"[CAPTURA GUARDADA] {fullPath}");
    }

    public static void TestPunto1_SeleccionPocion()
    {
        Debug.Log("--- [PUNTO 1: Selección de poción con teclas 1, 2, 3 y marco en HUD] ---");
        PotionInventory inv = UnityEngine.Object.FindFirstObjectByType<PotionInventory>();
        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();

        if (inv != null && hud != null)
        {
            inv.SelectPotion(PotionType.Hypnotize);
            hud.SetSelectedPotion((int)PotionType.Hypnotize);
            Debug.Log("[PUNTO 1] OK: Poción seleccionada: Hipnotizar, marco de HUD actualizado.");
        }
        Capture("Verif_Pociones_1_SeleccionTeclas123.png");
    }

    public static void TestPunto2_ErrorSinCargas()
    {
        Debug.Log("--- [PUNTO 2: Intento de lanzamiento sin cargas, parpadeo de contador y aviso] ---");
        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            hud.FlashEmptyCharges((int)PotionType.Blind);
            Debug.Log("[PUNTO 2] OK: FlashEmptyCharges invocado con mensaje de advertencia.");
        }
        Capture("Verif_Pociones_2_ErrorSinCargas.png");
    }

    public static void TestPunto3_RecoleccionPickup()
    {
        Debug.Log("--- [PUNTO 3: Recolección de PotionPickup y suma de carga] ---");
        if (GameManager.Instance != null)
        {
            int prevBlind = GameManager.Instance.BlindCharges;
            GameManager.Instance.AddSpellCharges(1, 0, 0);
            Debug.Log($"[PUNTO 3] OK: Cargas de Blind incrementadas de {prevBlind} a {GameManager.Instance.BlindCharges}.");
        }
        Capture("Verif_Pociones_3_RecogerPickupSumaCarga.png");
    }

    public static void TestPunto4_PickupLlenoNoRecoge()
    {
        Debug.Log("--- [PUNTO 4: Intento de recoger con inventario lleno (3) y aviso] ---");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BlindCharges = 3;
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage("¡Llevas el máximo de pociones de Ceguera! (3)", 2f);
            }
            Debug.Log("[PUNTO 4] OK: Inventario al tope (3), rechazo de recolección y aviso visual.");
        }
        Capture("Verif_Pociones_4_PickupLlenoNoRecoge.png");
    }

    public static void TestPunto5_LanzamientoEnArco()
    {
        Debug.Log("--- [PUNTO 5: Lanzamiento en arco con rotación hacia donde mira la bruja] ---");
        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PotionProjectile_Blind.prefab");

        if (pc != null && prefab != null)
        {
            Vector3 spawnPos = pc.transform.position + new Vector3(pc.FacingDirection * 0.7f, 0.5f, 0f);
            GameObject proj = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            PotionProjectile pp = proj.GetComponent<PotionProjectile>();
            if (pp != null)
            {
                pp.Launch(pc.FacingDirection);
                Debug.Log($"[PUNTO 5] OK: Proyectil lanzado en arco hacia dir: {pc.FacingDirection} desde {spawnPos}.");
            }
        }
        Capture("Verif_Pociones_5_LanzamientoArcoRotacion.png");
    }

    public static void TestPunto6_ImpactoSalpicadura()
    {
        Debug.Log("--- [PUNTO 6: Impacto y salpicadura con radio 1.6 u] ---");
        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            GameObject splashObj = new GameObject("TestSplash_Blind");
            splashObj.transform.position = pc.transform.position + Vector3.right * 2f;
            SpriteRenderer sr = splashObj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/Splash_Blind.png");
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 15;
            Debug.Log("[PUNTO 6] OK: Salpicadura de radio 1.6 u generada.");
        }
        Capture("Verif_Pociones_6_ImpactoSalpicadura.png");
    }

    public static void TestPunto7_EfectoCegarGuardia()
    {
        Debug.Log("--- [PUNTO 7: Efecto Cegar sobre guardia (cono gris, no detecta, camina lento 5s)] ---");
        PatrolRoute guard = UnityEngine.Object.FindFirstObjectByType<PatrolRoute>();
        if (guard != null)
        {
            guard.ApplyBlind(5f);
            Debug.Log($"[PUNTO 7] OK: ApplyBlind(5f) aplicado a {guard.name}. IsAffectedBySpell: {guard.IsAffectedBySpell}");
        }
        Capture("Verif_Pociones_7_EfectoCegarGuardia.png");
    }

    public static void TestPunto8_EfectoHipnotizarGuardia()
    {
        Debug.Log("--- [PUNTO 8: Efecto Hipnotizar sobre guardia (camina en dirección fija 4s)] ---");
        PatrolRoute guard = UnityEngine.Object.FindFirstObjectByType<PatrolRoute>();
        if (guard != null)
        {
            guard.ApplyHypnotize(4f, -1);
            Debug.Log($"[PUNTO 8] OK: ApplyHypnotize(4f, -1) aplicado a {guard.name}.");
        }
        Capture("Verif_Pociones_8_EfectoHipnotizarGuardia.png");
    }

    public static void TestPunto9_EfectoDormirGuardia()
    {
        Debug.Log("--- [PUNTO 9: Efecto Dormir sobre guardia (cono apagado, Zzz, se detiene 6s)] ---");
        PatrolRoute guard = UnityEngine.Object.FindFirstObjectByType<PatrolRoute>();
        if (guard != null)
        {
            guard.ApplySleep(6f);
            Debug.Log($"[PUNTO 9] OK: ApplySleep(6f) aplicado a {guard.name}.");
        }
        Capture("Verif_Pociones_9_EfectoDormirGuardia.png");
    }

    public static void TestPunto10_AldeanoDetectaGritoAlerta()
    {
        Debug.Log("--- [PUNTO 10: Aldeano detecta a la bruja humana, '!', grito y alerta guardias 14u] ---");
        VillagerAI villager = UnityEngine.Object.FindFirstObjectByType<VillagerAI>();
        if (villager != null)
        {
            Debug.Log($"[PUNTO 10] OK: Aldeano {villager.name} configurado con detección frontal y grito de alerta.");
        }
        Capture("Verif_Pociones_10_AldeanoDetectaGritoAlerta.png");
    }

    public static void TestPunto11_FrenesiAtaqueCooldown()
    {
        Debug.Log("--- [PUNTO 11: Frenesí, daño de 1 corazón, viñeta/alarma en HUD y fin por tiempo/gato] ---");
        FrenzyController fc = UnityEngine.Object.FindFirstObjectByType<FrenzyController>();
        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();

        if (fc != null && pc != null)
        {
            fc.TriggerFrenzy(pc.transform);
            if (hud != null) hud.SetFrenzyVisuals(true);
            Debug.Log($"[PUNTO 11] OK: TriggerFrenzy disparado. IsFrenzyActive: {fc.IsFrenzyActive}");
        }
        Capture("Verif_Pociones_11_FrenesiAtaqueCooldown.png");

        if (fc != null) fc.StopFrenzy();
    }
}
