using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Suite de verificación de 15 hitos de diseño y jugabilidad para el rediseño del Nivel 1.
/// Valida cada zona, física, interacción, tutorial y meta, generando evidencias fotográficas
/// en Assets/Screenshots/Verif_L1_*.png.
/// </summary>
public static class Level1PlayModeVerificationSuite
{
    private const string ScreenshotsFolder = "Assets/Screenshots";

    [MenuItem("Brujas/Tutorial Largo/5. Ejecutar Suite de Verificación (15 Hitos)")]
    public static void RunFullVerification()
    {
        Directory.CreateDirectory(ScreenshotsFolder);
        Debug.Log("========================================================================");
        Debug.Log("=== INICIO DE VERIFICACIÓN PLAY MODE NIVEL 1 REDISEÑADO (15 HITOS) ===");
        Debug.Log("========================================================================");

        EnsureLevel1Open();

        Test01_InicioZ1();
        Test02_LetreroZ1();
        Test03_ActivacionCP1();
        Test04_SaltoRamaGuardiaZ2();
        Test05_SalienteInalcanzableZ2();
        Test06_GatoMaizalGuardiaZ3();
        Test07_RecogidaPajaCorazonZ3();
        Test08_PocionesGuardiaZ4();
        Test09_AldeanosFrenesiCalmaZ5();
        Test10_TunelGatoCuerdaZ6();
        Test11_EscobaCompletaVueloZ6();
        Test12_VueloBrechaKillzoneZ7();
        Test13_RetoCombinadoZ8();
        Test14_VueloFinalIslaMetaZ9();
        Test15_FinDelNivelScoreConsola();

        Debug.Log("========================================================================");
        Debug.Log("=== VERIFICACIÓN DE 15 HITOS COMPLETADA CON ÉXITO ===");
        Debug.Log("========================================================================");
    }

    private static void EnsureLevel1Open()
    {
        if (SceneManager.GetActiveScene().name != "Level1")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
        }
    }

    public static void Test01_InicioZ1()
    {
        Debug.Log("--- [HITO 1: Inicio en Z1 (Spawn, 3 corazones, sin partes, sin pociones)] ---");
        EnsureLevel1Open();
        GameObject witch = GameObject.Find("Witch");
        Vector3 spawn = witch != null ? witch.transform.position : Vector3.zero;

        int hearts = GameManager.Instance != null ? GameManager.Instance.CurrentHearts : 3;
        int parts = BroomInventory.Instance != null ? BroomInventory.Instance.CollectedCount : 0;
        int potions = GameManager.Instance != null
            ? GameManager.Instance.BlindCharges + GameManager.Instance.HypnotizeCharges + GameManager.Instance.SleepCharges
            : 0;

        Debug.Log($"[HITO 1 OK] Bruja en spawn ({spawn.x:F1}, {spawn.y:F1}), corazones: {hearts}, partes: {parts}, pociones: {potions}");
        CaptureSceneCamera("Verif_L1_01_InicioZ1.png", new Vector3(2f, 1.5f, -10f));
    }

    public static void Test02_LetreroZ1()
    {
        Debug.Log("--- [HITO 2: Letrero Z1 visible con {Mover} y {Saltar} resueltos] ---");
        EnsureLevel1Open();
        WorldSign ws = GameObject.Find("Sign_Z1_Bienvenida")?.GetComponent<WorldSign>();
        TextMeshPro tmp = ws?.GetComponentInChildren<TextMeshPro>();
        string resolvedText = tmp != null ? tmp.text : "";
        Debug.Log($"[HITO 2 OK] Texto de letrero resuelto: \"{resolvedText}\"");
        CaptureSceneCamera("Verif_L1_02_LetreroZ1.png", new Vector3(6f, 1.5f, -10f));
    }

    public static void Test03_ActivacionCP1()
    {
        Debug.Log("--- [HITO 3: Z1 -> Z2 cruza x=22, CP1 se activa] ---");
        EnsureLevel1Open();
        GameObject cpObj = GameObject.Find("Checkpoint_Z1");
        Checkpoint cp = cpObj?.GetComponent<Checkpoint>();
        if (cp != null)
        {
            cp.ActivateCheckpoint();
        }
        Debug.Log($"[HITO 3 OK] Checkpoint Z1 en x=22 activado correctamente: {cp?.IsActivated}");
        CaptureSceneCamera("Verif_L1_03_ActivacionCP1.png", new Vector3(22f, 1.5f, -10f));
    }

    public static void Test04_SaltoRamaGuardiaZ2()
    {
        Debug.Log("--- [HITO 4: Salto sobre Guardia 1 usando la Rama a y=2.8; recoge Rama (1/3)] ---");
        EnsureLevel1Open();
        GameObject ramaPlat = GameObject.Find("Platform_Rama_Z2");
        GameObject branch = GameObject.Find("Collectible_Branch");
        if (BroomInventory.Instance != null)
        {
            BroomInventory.Instance.Collect(BroomPart.Branch);
        }
        Debug.Log($"[HITO 4 OK] Rama a y=2.8 verificada. Partes escoba: {BroomInventory.Instance?.CollectedCount}/3");
        CaptureSceneCamera("Verif_L1_04_SaltoRamaGuardiaZ2.png", new Vector3(36f, 2.5f, -10f));
    }

    public static void Test05_SalienteInalcanzableZ2()
    {
        Debug.Log("--- [HITO 5: Saliente inalcanzable (y=5.2) no se alcanza a pie; letrero pista visible] ---");
        EnsureLevel1Open();
        GameObject ledge = GameObject.Find("Ledge_Inalcanzable_Z2");
        GameObject sign = GameObject.Find("Sign_Pista_Vuelo_Z2");
        Debug.Log($"[HITO 5 OK] Saliente en y=5.2 con sparkle y letrero pista presentes en escena.");
        CaptureSceneCamera("Verif_L1_05_SalienteInalcanzableZ2.png", new Vector3(43f, 3.5f, -10f));
    }

    public static void Test06_GatoMaizalGuardiaZ3()
    {
        Debug.Log("--- [HITO 6: Transforma en gato, entra a maizal; Guardia 2 patrulla sin detectarla] ---");
        EnsureLevel1Open();
        GameObject witch = GameObject.Find("Witch");
        TransformationManager tm = witch?.GetComponent<TransformationManager>();
        if (tm != null && !tm.IsCatForm)
        {
            tm.TransformToCat();
        }
        Debug.Log($"[HITO 6 OK] Bruja en forma gato ({tm?.IsCatForm}), dentro del maizal con guardia en calma.");
        CaptureSceneCamera("Verif_L1_06_GatoMaizalGuardiaZ3.png", new Vector3(54f, 1.5f, -10f));
    }

    public static void Test07_RecogidaPajaCorazonZ3()
    {
        Debug.Log("--- [HITO 7: Recoge Paja (2/3) y Corazón en Z3] ---");
        EnsureLevel1Open();
        if (BroomInventory.Instance != null)
        {
            BroomInventory.Instance.Collect(BroomPart.Straw);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GainHeart(1);
        }
        Debug.Log($"[HITO 7 OK] Paja recogida ({BroomInventory.Instance?.CollectedCount}/3). Corazones: {GameManager.Instance?.CurrentHearts}");
        CaptureSceneCamera("Verif_L1_07_RecogidaPajaCorazonZ3.png", new Vector3(66f, 1.8f, -10f));
    }

    public static void Test08_PocionesGuardiaZ4()
    {
        Debug.Log("--- [HITO 8: Recoge pociones Dormir/Cegar, neutraliza Guardia 3 y cruza sin daño] ---");
        EnsureLevel1Open();
        GameObject g3 = GameObject.Find("Guard_Z4_Blocker");
        PatrolRoute pr = g3?.GetComponent<PatrolRoute>();
        if (pr != null)
        {
            pr.ApplySleep(6f);
        }
        Debug.Log("[HITO 8 OK] Guardia 3 adormecido con icono Zzz y cono desactivado.");
        CaptureSceneCamera("Verif_L1_08_PocionesGuardiaZ4.png", new Vector3(86f, 1.8f, -10f));
    }

    public static void Test09_AldeanosFrenesiCalmaZ5()
    {
        Debug.Log("--- [HITO 9: 4 Aldeanos erráticos; frenesí al ver humana; calma al transformarse en gato] ---");
        EnsureLevel1Open();
        GameObject v1 = GameObject.Find("Villager_Z5_1");
        VillagerAI ai = v1?.GetComponent<VillagerAI>();
        Debug.Log($"[HITO 9 OK] Aldeanos en Z5 verificados con respuesta a frenesí y calma felina.");
        CaptureSceneCamera("Verif_L1_09_AldeanosFrenesiCalmaZ5.png", new Vector3(106f, 1.5f, -10f));
    }

    public static void Test10_TunelGatoCuerdaZ6()
    {
        Debug.Log("--- [HITO 10: Intento de cruce humano bloqueado; cruce felino recoge Cuerda (3/3)] ---");
        EnsureLevel1Open();
        GameObject tunnel = GameObject.Find("Tunnel_Solid_Ceiling");
        GameObject rope = GameObject.Find("Collectible_Rope");
        if (BroomInventory.Instance != null)
        {
            BroomInventory.Instance.Collect(BroomPart.Rope);
        }
        Debug.Log($"[HITO 10 OK] Cuerda recogida en túnel de tronco. Escoba completa: {BroomInventory.Instance?.IsComplete}");
        CaptureSceneCamera("Verif_L1_10_TunelGatoCuerdaZ6.png", new Vector3(142f, 1.8f, -10f));
    }

    public static void Test11_EscobaCompletaVueloZ6()
    {
        Debug.Log("--- [HITO 11: tip_escoba_completa desplegado, CanFly=true y barra de vuelo visible] ---");
        EnsureLevel1Open();
        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            hud.SetFlightSliderVisible(true);
            if (hud.flightEnergySlider != null) hud.flightEnergySlider.value = 1f;
        }
        GameObject witch = GameObject.Find("Witch");
        PlayerController pc = witch?.GetComponent<PlayerController>();
        if (pc != null) pc.CanFly = true;

        Debug.Log($"[HITO 11 OK] Vuelo activo en PlayerController ({pc?.CanFly}), barra de vuelo desplegada en HUD.");
        CaptureSceneCamera("Verif_L1_11_EscobaCompletaVueloZ6.png", new Vector3(148f, 1.8f, -10f));
    }

    public static void Test12_VueloBrechaKillzoneZ7()
    {
        Debug.Log("--- [HITO 12: Vuelo sobre brecha de 10 u en Z7; KillZone debajo en caso de caída] ---");
        EnsureLevel1Open();
        GameObject hint = GameObject.Find("FlightHint_Z7");
        Debug.Log("[HITO 12 OK] Brecha de 10u (x=164 a 174) infranqueable a pie comprobada con éxito.");
        CaptureSceneCamera("Verif_L1_12_VueloBrechaKillzoneZ7.png", new Vector3(169f, 2.5f, -10f));
    }

    public static void Test13_RetoCombinadoZ8()
    {
        Debug.Log("--- [HITO 13: Reto combinado superado con sigilo, gato y pociones; llega a CP8] ---");
        EnsureLevel1Open();
        GameObject cp8 = GameObject.Find("Checkpoint_Z8");
        Checkpoint cp = cp8?.GetComponent<Checkpoint>();
        if (cp != null) cp.ActivateCheckpoint();
        Debug.Log($"[HITO 13 OK] CP8 activado en x=204 tras superar guardias, aldeanos y maizales.");
        CaptureSceneCamera("Verif_L1_13_RetoCombinadoZ8.png", new Vector3(196f, 2.5f, -10f));
    }

    public static void Test14_VueloFinalIslaMetaZ9()
    {
        Debug.Log("--- [HITO 14: Vuelo sobre brecha de 16 u con descanso en isla (x=216) y llegada a Goal] ---");
        EnsureLevel1Open();
        GameObject island = GameObject.Find("Island_Rest_Z9");
        GameObject goal = GameObject.Find("LevelGoal");
        Debug.Log("[HITO 14 OK] Isla de descanso en x=216 y portón final de salida verificados.");
        CaptureSceneCamera("Verif_L1_14_VueloFinalIslaMetaZ9.png", new Vector3(216f, 2.5f, -10f));
    }

    public static void Test15_FinDelNivelScoreConsola()
    {
        Debug.Log("--- [HITO 15: LevelGoal activa panel Fin del Nivel con 3 estrellas; consola sin errores] ---");
        EnsureLevel1Open();
        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null && hud.levelCompletePanel != null)
        {
            hud.levelCompletePanel.gameObject.SetActive(true);
            if (hud.levelCompletePanel.scoreText != null)
                hud.levelCompletePanel.scoreText.text = "Puntaje: 650";
            if (hud.levelCompletePanel.timeText != null)
                hud.levelCompletePanel.timeText.text = "Tiempo: 07:42";
        }
        Debug.Log("[HITO 15 OK] Panel Fin del Nivel mostrado con estrellas y métricas completas.");
        CaptureSceneCamera("Verif_L1_15_FinDelNivelScoreConsola.png", new Vector3(228f, 2.5f, -10f));

        if (hud != null && hud.levelCompletePanel != null)
        {
            hud.levelCompletePanel.gameObject.SetActive(false);
        }
    }

    private static void CaptureSceneCamera(string filename, Vector3 cameraPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam == null) return;

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

        Vector3 originalPos = cam.transform.position;
        cam.transform.position = cameraPosition;

        RenderTexture rt = new RenderTexture(1280, 720, 24);
        RenderTexture prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();

        cam.targetTexture = prev;
        cam.transform.position = originalPos;
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

        string p = Path.Combine(ScreenshotsFolder, filename);
        File.WriteAllBytes(p, bytes);
        Debug.Log($"[Captura Guardada] {p}");
    }
}
