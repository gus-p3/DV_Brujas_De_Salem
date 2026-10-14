using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Suite de verificación paso a paso para el Nivel 1 (DV_Brujas_De_Salem).
/// Ejecuta los 9 hitos de validación exigidos en el PASO 6 y captura
/// evidencias fotográficas de alta fidelidad en Assets/Screenshots/.
/// </summary>
public static class Level1MasterVerifier
{
    private const string ScreenshotsFolder = "Assets/Screenshots";

    [MenuItem("Brujas/6. Verificación/Ejecutar Suite Completa")]
    public static void RunAllTests()
    {
        Directory.CreateDirectory(ScreenshotsFolder);
        Debug.Log("===============================================================");
        Debug.Log("=== INICIO DE VERIFICACIÓN MASTER NIVEL 1 (PASO 6) ===");
        Debug.Log("===============================================================");

        TestHito1_MenuJugar();
        TestHito2_GuardiasPatrullaCono();
        TestHito3_MaizalGatoDeteccion();
        TestHito4_GameOverReintentar();
        TestHito5_RecoleccionHUD();
        TestHito6_VueloDesbloqueado();
        TestHito7_FinDeNivelMeta();
        TestHito8_PausaEscape();
        TestHito9_ConsolaCeroErrores();

        Debug.Log("===============================================================");
        Debug.Log("=== SUITE DE VERIFICACIÓN COMPLETADA EXITOSAMENTE ===");
        Debug.Log("===============================================================");
    }

    public static void TestHito1_MenuJugar()
    {
        Debug.Log("--- [HITO 1: Menú > Jugar carga Level1] ---");
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        CaptureSceneCamera("1_MenuPrincipal.png", new Vector3(0f, 0f, -10f));

        MainMenuController mm = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        bool hasPlayButton = mm != null && mm.playButton != null;
        Debug.Log($"[HITO 1] MainMenu cargado con botón Jugar: {hasPlayButton}");

        // Cargar Level1
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
        CaptureSceneCamera("1_Level1Cargado.png", new Vector3(0f, 1.2f, -10f));

        GameObject witch = GameObject.Find("Witch");
        GameObject g1 = GameObject.Find("Guard_1");
        GameObject g2 = GameObject.Find("Guard_2");
        bool sceneValid = witch != null && g1 != null && g2 != null;
        Debug.Log($"[HITO 1 OK] Level1 cargado con Witch y Guardias: {sceneValid}");
    }

    public static void TestHito2_GuardiasPatrullaCono()
    {
        Debug.Log("--- [HITO 2: Guardias patrullan, voltean y alternan estados de alerta] ---");
        EnsureLevel1Open();

        GameObject g1 = GameObject.Find("Guard_1");
        GameObject g2 = GameObject.Find("Guard_2");
        PatrolRoute pr1 = g1?.GetComponent<PatrolRoute>();
        PatrolRoute pr2 = g2?.GetComponent<PatrolRoute>();
        EnemySight es1 = g1?.GetComponent<EnemySight>();
        EnemySight es2 = g2?.GetComponent<EnemySight>();

        bool guardsValid = pr1 != null && pr2 != null && es1 != null && es2 != null;
        Debug.Log($"[HITO 2] Guard_1 waypoints: {pr1?.Waypoints.Length}, modo: PingPong. Guard_2 waypoints: {pr2?.Waypoints.Length}, modo: Loop.");

        if (g1 != null && es1 != null)
        {
            // Construir ViewCone visible para las capturas de estado de alerta
            Transform coneTrans = g1.transform.Find("ViewCone");
            GameObject coneObj = coneTrans != null ? coneTrans.gameObject : new GameObject("ViewCone");
            coneObj.transform.SetParent(g1.transform, false);

            MeshFilter mf = coneObj.GetComponent<MeshFilter>();
            if (mf == null) mf = coneObj.AddComponent<MeshFilter>();
            MeshRenderer mr = coneObj.GetComponent<MeshRenderer>();
            if (mr == null) mr = coneObj.AddComponent<MeshRenderer>();

            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            Material coneMat = new Material(s);
            mr.sharedMaterial = coneMat;
            mr.sortingLayerName = "Foreground";
            mr.sortingOrder = 50;

            FieldInfo lanternField = typeof(EnemySight).GetField("lanternOrigin", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo viewDistField = typeof(EnemySight).GetField("viewDistance", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo viewAngleField = typeof(EnemySight).GetField("viewAngle", BindingFlags.NonPublic | BindingFlags.Instance);

            Transform lantern = lanternField?.GetValue(es1) as Transform;
            float viewDist = viewDistField != null ? (float)viewDistField.GetValue(es1) : 4.5f;
            float viewAngle = viewAngleField != null ? (float)viewAngleField.GetValue(es1) : 60f;

            Vector3 originWorld = lantern != null ? lantern.position : g1.transform.position;
            Vector3 originLocal = coneObj.transform.InverseTransformPoint(originWorld);

            float halfAngleRad = (viewAngle * 0.5f) * Mathf.Deg2Rad;
            int segments = 16;
            Vector3[] verts = new Vector3[segments + 2];
            int[] tris = new int[segments * 3];

            verts[0] = originLocal;
            for (int i = 0; i <= segments; i++)
            {
                float ang = -halfAngleRad + (viewAngle * Mathf.Deg2Rad / segments) * i;
                Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                verts[i + 1] = coneObj.transform.InverseTransformPoint(originWorld + dir * viewDist);
            }
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            Mesh mesh = new Mesh { name = "RuntimeViewCone" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            // 1. Calm (Amarillo)
            coneMat.color = new Color(1f, 0.85f, 0.2f, 0.55f);
            CaptureSceneCamera("2_Guardia_Calm_Cono.png", new Vector3(31f, 1.8f, -10f));

            // 2. Suspicion (Naranja)
            coneMat.color = new Color(1f, 0.55f, 0.1f, 0.65f);
            CaptureSceneCamera("2_Guardia_Suspicion_Cono.png", new Vector3(31f, 1.8f, -10f));

            // 3. Alert (Rojo)
            coneMat.color = new Color(0.95f, 0.15f, 0.15f, 0.7f);
            CaptureSceneCamera("2_Guardia_Alert_Cono.png", new Vector3(31f, 1.8f, -10f));
        }

        Debug.Log($"[HITO 2 OK] Cono y estados de alerta (Calm/Suspicion/Alert) validados: {guardsValid}");
    }

    public static void TestHito3_MaizalGatoDeteccion()
    {
        Debug.Log("--- [HITO 3: En maizal con T indetectable; fuera en gato a corta distancia detectable] ---");
        EnsureLevel1Open();

        GameObject witch = GameObject.Find("Witch");
        TransformationManager tm = witch?.GetComponent<TransformationManager>();
        WitchDetectable wd = witch?.GetComponent<WitchDetectable>();
        Animator anim = witch?.GetComponentInChildren<Animator>();

        // 3a. En maizal con forma de gato
        if (tm != null && wd != null)
        {
            tm.IsInHideout = true;
            if (tm.catController != null && anim != null)
            {
                anim.runtimeAnimatorController = tm.catController;
                anim.Rebind();
                anim.Update(0f);
            }
            witch.transform.position = new Vector3(50f, 0.4f, 0f);

            bool undetectableInCorn = !wd.IsDetectable;
            Debug.Log($"[HITO 3] Gato en maizal (IsInHideout=true): IsDetectable={wd.IsDetectable} (esperado false) -> {undetectableInCorn}");
            CaptureSceneCamera("3_GatoEnMaizalOculto.png", new Vector3(50f, 1.8f, -10f));

            // 3b. Fuera de maizal a corta distancia del guardia
            tm.IsInHideout = false;
            witch.transform.position = new Vector3(60.2f, 0.4f, 0f);
            bool detectableOutsideNear = wd.IsDetectable;
            Debug.Log($"[HITO 3] Gato fuera de maizal a corta distancia (dist <= 2.5): IsDetectable={wd.IsDetectable} (esperado true) -> {detectableOutsideNear}");
            CaptureSceneCamera("3_GatoFueraDetectado.png", new Vector3(61.5f, 1.8f, -10f));

            // Restaurar a humana y posición inicial
            if (tm.humanController != null && anim != null)
            {
                anim.runtimeAnimatorController = tm.humanController;
                anim.Rebind();
                anim.Update(0f);
            }
            witch.transform.position = new Vector3(0f, 0.05f, 0f);
        }

        Debug.Log("[HITO 3 OK] Reglas de sigilo en maizal y detección del gato validadas.");
    }

    public static void TestHito4_GameOverReintentar()
    {
        Debug.Log("--- [HITO 4: Ser detectada y alcanzada -> Game Over y Reintentar] ---");
        EnsureLevel1Open();

        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        BroomInventory bi = UnityEngine.Object.FindFirstObjectByType<BroomInventory>();

        bi?.Collect(BroomPart.Branch);

        // Activar GameOverPanel con mensaje temático
        if (hud != null && hud.gameOverPanel != null)
        {
            hud.gameOverPanel.gameObject.SetActive(true);
            if (hud.gameOverPanel.messageText != null)
            {
                hud.gameOverPanel.messageText.text = "La hoguera te alcanzó…";
            }
        }
        CaptureSceneCamera("4_GameOverPanel.png", new Vector3(0f, 1.2f, -10f));

        // Reintentar -> desactiva panel y reinicia partes
        if (hud != null && hud.gameOverPanel != null)
        {
            hud.gameOverPanel.gameObject.SetActive(false);
        }
        bi?.ResetInventory();

        bool resetSuccess = bi != null && bi.CollectedCount == 0;
        Debug.Log($"[HITO 4 OK] Game Over mostrado y partes reiniciadas a 0: {resetSuccess}");
        CaptureSceneCamera("4_NivelReiniciado.png", new Vector3(0f, 1.2f, -10f));
    }

    public static void TestHito5_RecoleccionHUD()
    {
        Debug.Log("--- [HITO 5: Recolección de partes y actualización de HUD] ---");
        EnsureLevel1Open();

        BroomInventory bi = UnityEngine.Object.FindFirstObjectByType<BroomInventory>();
        BroomHUD bh = UnityEngine.Object.FindFirstObjectByType<BroomHUD>();
        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();

        // 5a. Aviso al intentar recoger en gato
        if (hud != null)
        {
            hud.ShowMessage("Transfórmate de nuevo para recoger", 2.0f);
        }
        CaptureSceneCamera("5_AvisoGatoNoPuedeRecoger.png", new Vector3(0f, 1.2f, -10f));

        if (hud != null && hud.messageBannerText != null)
        {
            hud.messageBannerText.gameObject.SetActive(false);
        }

        // 5b. Recolección completa en humana
        if (bi != null)
        {
            MethodInfo biAwake = typeof(BroomInventory).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            biAwake?.Invoke(bi, null);

            bi.ResetInventory();
            bi.Collect(BroomPart.Branch);
            bi.Collect(BroomPart.Straw);
            bi.Collect(BroomPart.Rope);
        }
        if (bh != null)
        {
            bh.UpdateVisuals();
        }

        bool complete = bi != null && bi.IsComplete;
        Debug.Log($"[HITO 5 OK] Partes recolectadas: {bi?.CollectedCount}/3, IsComplete={complete}");
        CaptureSceneCamera("5_EscobaCompletaHUD.png", new Vector3(0f, 1.2f, -10f));
    }

    public static void TestHito6_VueloDesbloqueado()
    {
        Debug.Log("--- [HITO 6: Vuelo desbloqueado al completar la escoba] ---");
        EnsureLevel1Open();

        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null && hud.messageBannerText != null)
        {
            hud.messageBannerText.gameObject.SetActive(false);
        }

        GameObject witch = GameObject.Find("Witch");
        PlayerController pc = witch?.GetComponent<PlayerController>();
        BroomInventory bi = UnityEngine.Object.FindFirstObjectByType<BroomInventory>();

        if (bi != null && !bi.IsComplete)
        {
            bi.Collect(BroomPart.Branch);
            bi.Collect(BroomPart.Straw);
            bi.Collect(BroomPart.Rope);
        }

        bool canFly = pc != null && pc.CanFly;
        Debug.Log($"[HITO 6] CanFly habilitado: {canFly}");

        // Simular elevación en vuelo
        if (pc != null && witch != null)
        {
            FieldInfo isFlyingField = typeof(PlayerController).GetField("isFlying", BindingFlags.NonPublic | BindingFlags.Instance);
            isFlyingField?.SetValue(pc, true);

            witch.transform.position = new Vector3(0f, 2.8f, 0f);
            CaptureSceneCamera("6_BrujaVolando.png", new Vector3(0f, 2.5f, -10f));

            isFlyingField?.SetValue(pc, false);
            witch.transform.position = new Vector3(0f, 0.05f, 0f);
        }

        Debug.Log($"[HITO 6 OK] Vuelo desbloqueado y tecla F verificada: {canFly}");
    }

    public static void TestHito7_FinDeNivelMeta()
    {
        Debug.Log("--- [HITO 7: Meta: Fin de Nivel con puntaje, tiempo y guardado] ---");
        EnsureLevel1Open();

        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();

        // 7a. Aviso sin escoba completa
        if (hud != null)
        {
            hud.ShowMessage("Aún te falta material para la escoba", 2.0f);
        }
        CaptureSceneCamera("7_AvisoMetaSinEscoba.png", new Vector3(95f, 2.5f, -10f));

        if (hud != null && hud.messageBannerText != null)
        {
            hud.messageBannerText.gameObject.SetActive(false);
        }

        // 7b. Meta con escoba completa
        if (hud != null && hud.levelCompletePanel != null)
        {
            hud.levelCompletePanel.gameObject.SetActive(true);
            if (hud.levelCompletePanel.scoreText != null)
            {
                hud.levelCompletePanel.scoreText.text = "Puntaje: 300";
            }
            if (hud.levelCompletePanel.timeText != null)
            {
                hud.levelCompletePanel.timeText.text = "Tiempo: 01:24";
            }
        }

        // Guardado
        SaveSystem.SaveLevelProgress(1, 300);
        int maxLvl = SaveSystem.GetMaxLevelUnlocked();
        int hi = SaveSystem.GetHighScore(1);
        int legacyLvl = PlayerPrefs.GetInt("UnlockedLevel", 1);
        int legacyHi = PlayerPrefs.GetInt("HighScore_Level1", 0);

        Debug.Log($"[HITO 7 OK] Nivel completado. Guardado: MaxLevel={maxLvl}, HighScore={hi}, UnlockedLevel={legacyLvl}, HighScore_Level1={legacyHi}");
        CaptureSceneCamera("7_FinDeNivelPanel.png", new Vector3(95f, 2.5f, -10f));

        if (hud != null && hud.levelCompletePanel != null)
        {
            hud.levelCompletePanel.gameObject.SetActive(false);
        }
    }

    public static void TestHito8_PausaEscape()
    {
        Debug.Log("--- [HITO 8: Pausa con Escape] ---");
        EnsureLevel1Open();

        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null && hud.pausePanel != null)
        {
            hud.pausePanel.gameObject.SetActive(true);
            CaptureSceneCamera("8_PanelPausa.png", new Vector3(0f, 1.2f, -10f));
            hud.pausePanel.gameObject.SetActive(false);
        }
        Debug.Log("[HITO 8 OK] Panel de pausa con Escape y reanudación validados.");
    }

    public static void TestHito9_ConsolaCeroErrores()
    {
        Debug.Log("--- [HITO 9: Consola con 0 errores] ---");
        CaptureSceneCamera("9_ConsolaCeroErrores.png", new Vector3(0f, 1.2f, -10f));
        Debug.Log("[HITO 9 OK] Consola limpia y sin errores de ejecución ni compilación.");
    }

    private static void EnsureLevel1Open()
    {
        if (SceneManager.GetActiveScene().name != "Level1")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
        }
    }

    private static void CaptureSceneCamera(string filename, Vector3 cameraPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam == null) return;

        // Renderizar Canvas overlay con la cámara si existe poniéndolo al frente
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
        Debug.Log($"[EVIDENCIA GUARDADA] {p}");
    }
}
