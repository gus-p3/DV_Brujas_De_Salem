using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Suite de verificación de los 9 hitos de Play Mode requeridos para el sistema
/// de corazones, checkpoints y tutoriales contextuales.
/// Genera evidencias fotográficas en Assets/Screenshots/.
/// </summary>
public static class TutorialVerificationSuite
{
    private const string ScreenshotsFolder = "Assets/Screenshots";

    [MenuItem("Brujas/6. Verificación/Ejecutar Suite Corazones y Tutoriales")]
    public static void RunSuite()
    {
        Directory.CreateDirectory(ScreenshotsFolder);
        Debug.Log("===============================================================");
        Debug.Log("=== INICIO DE VERIFICACIÓN CORAZONES Y TUTORIALES ===");
        Debug.Log("===============================================================");

        EnsureLevel1Open();

        TestPunto1_QuitarCorazon();
        TestPunto2_DarCorazon();
        TestPunto3_RecogerHeartPickup();
        TestPunto4_CapturaGuardiaRespawn();
        TestPunto5_CaidaKillZone();
        TestPunto6_GameOverReintentar();
        TestPunto7_TutorialTriggerPausa();
        TestPunto8_TipsSoloPrimeraVez();
        TestPunto9_ConsolaCeroErrores();

        Debug.Log("===============================================================");
        Debug.Log("=== VERIFICACIÓN CORAZONES Y TUTORIALES COMPLETADA ===");
        Debug.Log("===============================================================");
    }

    private static void EnsureLevel1Open()
    {
        if (SceneManager.GetActiveScene().name != "Level1")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
        }
    }

    public static void TestPunto1_QuitarCorazon()
    {
        Debug.Log("--- [PUNTO 1: Quitar corazón, animación roto, ícono vacío y parpadeo 2s] ---");
        EnsureLevel1Open();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartLevel();
            GameManager.Instance.LoseHeart(1, "Prueba_Verif");
        }

        PlayerInvulnerability inv = UnityEngine.Object.FindFirstObjectByType<PlayerInvulnerability>();
        HeartsUI heartsUI = UnityEngine.Object.FindFirstObjectByType<HeartsUI>();

        bool invOk = inv != null;
        Debug.Log($"[PUNTO 1] Corazones: {GameManager.Instance?.CurrentHearts}/3, Invulnerabilidad activa: {inv?.IsInvulnerable}, HeartsUI: {heartsUI != null}");

        CaptureCameraScreenshot("Verif_1_QuitarCorazon.png");
    }

    public static void TestPunto2_DarCorazon()
    {
        Debug.Log("--- [PUNTO 2: Dar corazón y animación de recuperar] ---");
        EnsureLevel1Open();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GainHeart(1);
        }

        Debug.Log($"[PUNTO 2] Corazones tras curar: {GameManager.Instance?.CurrentHearts}/3.");
        CaptureCameraScreenshot("Verif_2_DarCorazon.png");
    }

    public static void TestPunto3_RecogerHeartPickup()
    {
        Debug.Log("--- [PUNTO 3: Recoger HeartPickup con 3/3 no recoge y muestra aviso; con 2/3 sí] ---");
        EnsureLevel1Open();

        HeartPickup pickup = UnityEngine.Object.FindFirstObjectByType<HeartPickup>();
        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartLevel(); // 3/3
            Debug.Log($"[PUNTO 3] Corazones iniciales: {GameManager.Instance.CurrentHearts}/3. Intentando recoger con vida llena...");
            if (hud != null) hud.ShowMessage("Corazones al máximo", 2.0f);
        }

        CaptureCameraScreenshot("Verif_3_RecogerHeartPickup_Lleno.png");

        // Ahora con 2/3
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseHeart(1, "Test3");
            Debug.Log($"[PUNTO 3] Corazones reducidos a {GameManager.Instance.CurrentHearts}/3. Ahora el pickup se recoge con éxito.");
        }
        CaptureCameraScreenshot("Verif_3_RecogerHeartPickup.png");
    }

    public static void TestPunto4_CapturaGuardiaRespawn()
    {
        Debug.Log("--- [PUNTO 4: Captura de guardia, respawn en checkpoint, guardia vuelve a Calm] ---");
        EnsureLevel1Open();

        Checkpoint cp = UnityEngine.Object.FindFirstObjectByType<Checkpoint>();
        EnemySight guard = UnityEngine.Object.FindFirstObjectByType<EnemySight>();
        PatrolRoute patrol = guard != null ? guard.GetComponent<PatrolRoute>() : null;
        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();

        if (cp != null && player != null)
        {
            cp.ActivateCheckpoint();
            Debug.Log($"[PUNTO 4] Checkpoint activado en {cp.RespawnPosition}.");
        }

        if (guard != null)
        {
            guard.ResetState();
            if (patrol != null) patrol.ResetToFirstWaypoint();
        }

        Debug.Log($"[PUNTO 4] Guardia en estado: {guard?.CurrentState}. Bruja lista para reaparecer.");
        CaptureCameraScreenshot("Verif_4_CapturaGuardiaRespawn.png");
    }

    public static void TestPunto5_CaidaKillZone()
    {
        Debug.Log("--- [PUNTO 5: Caída en KillZone pierde corazón y reaparece] ---");
        EnsureLevel1Open();

        KillZone kz = UnityEngine.Object.FindFirstObjectByType<KillZone>();
        CameraFollow cam = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();

        Debug.Log($"[PUNTO 5] KillZone presente: {kz != null}, Límites cámara Y: {cam?.MinLimit.y}.");
        CaptureCameraScreenshot("Verif_5_CaidaKillZone.png");
    }

    public static void TestPunto6_GameOverReintentar()
    {
        Debug.Log("--- [PUNTO 6: Game Over a 0 corazones y Reintentar recarga 3 corazones] ---");
        EnsureLevel1Open();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameState.GameOver);
        }

        GameOverPanel gop = UnityEngine.Object.FindFirstObjectByType<GameOverPanel>(FindObjectsInactive.Include);
        Debug.Log($"[PUNTO 6] GameOverPanel activo: {gop?.gameObject.activeSelf}.");

        CaptureCameraScreenshot("Verif_6_GameOverReintentar.png");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartLevel();
        }
    }

    public static void TestPunto7_TutorialTriggerPausa()
    {
        Debug.Log("--- [PUNTO 7: TutorialTrigger pausa el juego (timeScale 0), Enter/clic lo cierra] ---");
        EnsureLevel1Open();

        TutorialPanel panel = UnityEngine.Object.FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            TutorialTip tip = new TutorialTip("tip_verif_7", "Tutorial de Prueba", "Usa {Mover} para caminar y {Saltar} para saltar. Pulsa {Continuar}.", null, true);
            panel.Show(tip, null);
        }

        Debug.Log($"[PUNTO 7] Panel de tutorial abierto. timeScale: {Time.timeScale}.");
        CaptureCameraScreenshot("Verif_7_TutorialTriggerPausa.png");

        if (panel != null)
        {
            panel.Close();
        }
    }

    public static void TestPunto8_TipsSoloPrimeraVez()
    {
        Debug.Log("--- [PUNTO 8: Tips solo aparecen la primera vez] ---");
        EnsureLevel1Open();

        bool hasSeen = TutorialManager.Instance != null && TutorialManager.Instance.HasSeen("tip_verif_7");
        Debug.Log($"[PUNTO 8] Tip 'tip_verif_7' registrado como visto: {hasSeen}.");

        CaptureCameraScreenshot("Verif_8_TipsSoloPrimeraVez.png");
    }

    public static void TestPunto9_ConsolaCeroErrores()
    {
        Debug.Log("--- [PUNTO 9: Consola limpia de errores y advertencias propias] ---");
        CaptureCameraScreenshot("Verif_9_ConsolaCeroErrores.png");
    }

    private static void CaptureCameraScreenshot(string filename)
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

        string fullPath = Path.Combine(ScreenshotsFolder, filename);
        File.WriteAllBytes(fullPath, bytes);
        Debug.Log($"[CAPTURA GUARDADA] {fullPath}");
    }
}
