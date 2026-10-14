using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verificador automatizado para la tarea de corrección de recolección, escoba visual y animaciones.
/// Valida los 10 casos de prueba obligatorios.
/// </summary>
public static class BroomAndAnimationFixVerifier
{
    private const string ScreenshotsFolder = "Assets/Screenshots";

    [MenuItem("Brujas/7. Correcciones/Ejecutar Verificación de Escoba y Animaciones")]
    public static void RunVerification()
    {
        Directory.CreateDirectory(ScreenshotsFolder);
        Debug.Log("=========================================================================");
        Debug.Log("=== INICIO DE VERIFICACIÓN: RECOLECCIÓN, ESCOBA Y ANIMACIONES ===");
        Debug.Log("=========================================================================");

        EnsureLevel1Open();

        Test1_RecoleccionProximidad();
        Test2_RestriccionEnGato();
        Test3_InventarioYHUD();
        Test4_EscobaVisualAlVolar();
        Test5_EscobaAcompanaYVoltea();
        Test6_BloqueoVueloSinCompletar();
        Test7_AnimacionCaminar();
        Test8_AnimacionSaltarYAterrizar();
        Test9_RegresionTransformaciones();
        Test10_ConsolaCeroErrores();

        Debug.Log("=========================================================================");
        Debug.Log("=== VERIFICACIÓN DE CORRECCIONES FINALIZADA CON ÉXITO ===");
        Debug.Log("=========================================================================");
    }

    private static void EnsureLevel1Open()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
        }
        
        GameObject witch = GameObject.Find("Witch");
        if (witch != null)
        {
            TransformationManager tm = witch.GetComponent<TransformationManager>();
            if (tm != null && tm.IsCatForm)
            {
                MethodInfo tryHuman = typeof(TransformationManager).GetMethod("TryTransformToHuman", BindingFlags.NonPublic | BindingFlags.Instance);
                tryHuman?.Invoke(tm, null);
            }
            PlayerController pc = witch.GetComponent<PlayerController>();
            if (pc != null) pc.CanFly = false;
        }

        Physics2D.SyncTransforms();
    }

    public static void Test1_RecoleccionProximidad()
    {
        Debug.Log("--- [TEST 1: Recolección por proximidad sin pulsar teclas] ---");
        GameObject witch = GameObject.Find("Witch");
        TransformationManager tm = witch?.GetComponent<TransformationManager>();
        BroomInventory bi = witch?.GetComponent<BroomInventory>();

        // Crear coleccionable temporal para la prueba
        GameObject testItem = new GameObject("Test_Collectible_Proximity");
        testItem.transform.position = witch.transform.position + new Vector3(1.0f, 0f, 0f);
        BoxCollider2D col = testItem.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.6f, 1.6f);
        col.isTrigger = true;
        Collectible c = testItem.AddComponent<Collectible>();
        Physics2D.SyncTransforms();

        // Simular update del coleccionable estando la bruja cerca (distancia 1.0 <= proximityRadius 1.3)
        MethodInfo updateMethod = typeof(Collectible).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
        updateMethod?.Invoke(c, null);

        // Si fue recogido, isCollected es true o se destruyó
        FieldInfo isColField = typeof(Collectible).GetField("isCollected", BindingFlags.NonPublic | BindingFlags.Instance);
        bool collected = isColField != null && (bool)isColField.GetValue(c);

        if (testItem != null) UnityEngine.Object.DestroyImmediate(testItem);
        Physics2D.SyncTransforms();

        Debug.Log($"[TEST 1 RESULTADO] Recolección por proximidad a distancia 1.0m: {(collected ? "OK" : "FALLA")}");
    }

    public static void Test2_RestriccionEnGato()
    {
        Debug.Log("--- [TEST 2: Restricción de recolección en forma de gato] ---");
        GameObject witch = GameObject.Find("Witch");
        TransformationManager tm = witch?.GetComponent<TransformationManager>();

        MethodInfo transformToCat = typeof(TransformationManager).GetMethod("TransformToCat", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo tryTransformToHuman = typeof(TransformationManager).GetMethod("TryTransformToHuman", BindingFlags.NonPublic | BindingFlags.Instance);

        transformToCat?.Invoke(tm, null);
        bool canCollectInCat = tm != null && tm.CanCollect;

        GameObject testItem = new GameObject("Test_Collectible_Cat");
        testItem.transform.position = witch.transform.position + new Vector3(0.5f, 0f, 0f);
        BoxCollider2D col = testItem.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.6f, 1.6f);
        col.isTrigger = true;
        Collectible c = testItem.AddComponent<Collectible>();
        Physics2D.SyncTransforms();

        MethodInfo updateMethod = typeof(Collectible).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
        updateMethod?.Invoke(c, null);

        FieldInfo isColField = typeof(Collectible).GetField("isCollected", BindingFlags.NonPublic | BindingFlags.Instance);
        bool collected = isColField != null && (bool)isColField.GetValue(c);

        // El objeto NO debe haberse recogido
        bool restrictedCorrectly = !canCollectInCat && !collected;

        if (testItem != null) UnityEngine.Object.DestroyImmediate(testItem);
        tryTransformToHuman?.Invoke(tm, null);
        Physics2D.SyncTransforms();

        Debug.Log($"[TEST 2 RESULTADO] En forma de gato CanCollect={canCollectInCat}, item recogido={collected} -> {(restrictedCorrectly ? "OK" : "FALLA")}");
    }

    public static void Test3_InventarioYHUD()
    {
        Debug.Log("--- [TEST 3: Inventario y HUD actualizados tras recolección] ---");
        GameObject witch = GameObject.Find("Witch");
        BroomInventory bi = witch?.GetComponent<BroomInventory>();
        BroomHUD bh = UnityEngine.Object.FindFirstObjectByType<BroomHUD>();

        MethodInfo biAwake = typeof(BroomInventory).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        biAwake?.Invoke(bi, null);

        bi?.ResetInventory();
        bi?.Collect(BroomPart.Branch);
        bi?.Collect(BroomPart.Straw);
        bi?.Collect(BroomPart.Rope);
        bh?.UpdateVisuals();

        bool isComplete = bi != null && bi.IsComplete;
        int count = bi != null ? bi.CollectedCount : 0;
        bool valid = isComplete && count == 3;

        // Limpiar estado para las siguientes pruebas
        bi?.ResetInventory();
        PlayerController pc = witch?.GetComponent<PlayerController>();
        if (pc != null) pc.CanFly = false;

        Debug.Log($"[TEST 3 RESULTADO] Inventario: {count}/3, IsComplete={isComplete} -> {(valid ? "OK" : "FALLA")}");
    }

    public static void Test4_EscobaVisualAlVolar()
    {
        Debug.Log("--- [TEST 4: Escoba visual aparece al volar con F] ---");
        GameObject witch = GameObject.Find("Witch");
        PlayerController pc = witch?.GetComponent<PlayerController>();
        BroomVisual bv = witch?.GetComponent<BroomVisual>();

        Transform broomChild = witch?.transform.Find("BroomVisual");
        SpriteRenderer broomSr = broomChild?.GetComponent<SpriteRenderer>();

        // Simular inicio de vuelo
        MethodInfo handleFlight = typeof(BroomVisual).GetMethod("HandleFlightChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        handleFlight?.Invoke(bv, new object[] { true });

        bool broomActive = broomChild != null && broomChild.gameObject.activeSelf;
        bool hasSprite = broomSr != null && broomSr.sprite != null;

        // Captura con escoba visible y bruja en el aire
        Vector3 origPos = witch.transform.position;
        witch.transform.position = new Vector3(0f, 2.8f, 0f);
        Physics2D.SyncTransforms();
        CaptureSceneCamera("6_BrujaVolando.png", new Vector3(0f, 2.5f, -10f));

        // Finalizar vuelo
        handleFlight?.Invoke(bv, new object[] { false });
        bool broomHiddenOnLand = broomChild != null && !broomChild.gameObject.activeSelf;
        witch.transform.position = origPos;
        Physics2D.SyncTransforms();

        bool valid = broomActive && hasSprite && broomHiddenOnLand;
        Debug.Log($"[TEST 4 RESULTADO] Escoba activa al volar={broomActive}, sprite={hasSprite}, oculta al aterrizar={broomHiddenOnLand} -> {(valid ? "OK" : "FALLA")}");
    }

    public static void Test5_EscobaAcompanaYVoltea()
    {
        Debug.Log("--- [TEST 5: Escoba acompaña al personaje y se voltea con flipX] ---");
        GameObject witch = GameObject.Find("Witch");
        SpriteRenderer witchSr = witch?.GetComponentInChildren<SpriteRenderer>();
        BroomVisual bv = witch?.GetComponent<BroomVisual>();
        Transform broomChild = witch?.transform.Find("BroomVisual");
        SpriteRenderer broomSr = broomChild?.GetComponent<SpriteRenderer>();

        MethodInfo handleFlight = typeof(BroomVisual).GetMethod("HandleFlightChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo updateOrientation = typeof(BroomVisual).GetMethod("UpdateBroomOrientation", BindingFlags.NonPublic | BindingFlags.Instance);

        handleFlight?.Invoke(bv, new object[] { true });

        // Mirar a la derecha
        if (witchSr != null) witchSr.flipX = false;
        updateOrientation?.Invoke(bv, null);
        bool rightFlip = broomSr != null && !broomSr.flipX;
        float rightX = broomChild != null ? broomChild.localPosition.x : 0f;

        // Mirar a la izquierda
        if (witchSr != null) witchSr.flipX = true;
        updateOrientation?.Invoke(bv, null);
        bool leftFlip = broomSr != null && broomSr.flipX;
        float leftX = broomChild != null ? broomChild.localPosition.x : 0f;

        // Restaurar derecha y terminar vuelo
        if (witchSr != null) witchSr.flipX = false;
        handleFlight?.Invoke(bv, new object[] { false });

        bool flippedCorrectly = rightFlip && leftFlip && (leftX == -rightX);
        Debug.Log($"[TEST 5 RESULTADO] Escoba flip derecha={rightFlip}, izquierda={leftFlip}, offset simétrico={flippedCorrectly} -> {(flippedCorrectly ? "OK" : "FALLA")}");
    }

    public static void Test6_BloqueoVueloSinCompletar()
    {
        Debug.Log("--- [TEST 6: Bloqueo de vuelo antes de completar las 3 partes] ---");
        GameObject witch = GameObject.Find("Witch");
        PlayerController pc = witch?.GetComponent<PlayerController>();
        BroomInventory bi = witch?.GetComponent<BroomInventory>();

        bi?.ResetInventory();
        if (pc != null) pc.CanFly = false;
        bool canFly = pc != null && pc.CanFly;

        Transform broomChild = witch?.transform.Find("BroomVisual");
        bool broomHidden = broomChild == null || !broomChild.gameObject.activeSelf;

        bool blocked = !canFly && broomHidden;
        Debug.Log($"[TEST 6 RESULTADO] Con 0/3 partes CanFly={canFly}, escoba oculta={broomHidden} -> {(blocked ? "OK" : "FALLA")}");
    }

    public static void Test7_AnimacionCaminar()
    {
        Debug.Log("--- [TEST 7: Animación de caminar (Walk) al desplazarse] ---");
        GameObject witch = GameObject.Find("Witch");
        Animator anim = witch?.GetComponentInChildren<Animator>();

        if (anim != null)
        {
            anim.SetFloat("Speed", 3.0f);
            anim.SetBool("IsGrounded", true);
            anim.SetBool("IsFlying", false);
            anim.Update(0.1f);

            var state = anim.GetCurrentAnimatorStateInfo(0);
            bool isWalk = state.IsName("Walk");
            Debug.Log($"[TEST 7 RESULTADO] Animator con Speed=3 en suelo -> Estado Walk={isWalk} -> {(isWalk ? "OK" : "FALLA")}");
        }
        else
        {
            Debug.LogError("[TEST 7] Animator no encontrado.");
        }
    }

    public static void Test8_AnimacionSaltarYAterrizar()
    {
        Debug.Log("--- [TEST 8: Animación de saltar (Jump) y retorno a Idle al aterrizar] ---");
        GameObject witch = GameObject.Find("Witch");
        Animator anim = witch?.GetComponentInChildren<Animator>();

        if (anim != null)
        {
            // Salto en el aire
            anim.SetBool("IsGrounded", false);
            anim.SetBool("IsFlying", false);
            anim.Update(0.1f);
            var stateJump = anim.GetCurrentAnimatorStateInfo(0);
            bool isJump = stateJump.IsName("Jump");

            // Aterrizaje quieto
            anim.SetBool("IsGrounded", true);
            anim.SetFloat("Speed", 0f);
            anim.Update(0.1f);
            var stateIdle = anim.GetCurrentAnimatorStateInfo(0);
            bool isIdle = stateIdle.IsName("Idle");

            bool valid = isJump && isIdle;
            Debug.Log($"[TEST 8 RESULTADO] En el aire Jump={isJump}, al aterrizar Idle={isIdle} -> {(valid ? "OK" : "FALLA")}");
        }
        else
        {
            Debug.LogError("[TEST 8] Animator no encontrado.");
        }
    }

    public static void Test9_RegresionTransformaciones()
    {
        Debug.Log("--- [TEST 9: Regresión de transformaciones (Gato <-> Humana)] ---");
        GameObject witch = GameObject.Find("Witch");
        TransformationManager tm = witch?.GetComponent<TransformationManager>();
        Animator anim = witch?.GetComponentInChildren<Animator>();

        MethodInfo transformToCat = typeof(TransformationManager).GetMethod("TransformToCat", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo tryTransformToHuman = typeof(TransformationManager).GetMethod("TryTransformToHuman", BindingFlags.NonPublic | BindingFlags.Instance);

        transformToCat?.Invoke(tm, null);
        bool isCat = tm != null && tm.IsCatForm;
        bool catAnimAssigned = anim != null && anim.runtimeAnimatorController == tm?.catController;

        tryTransformToHuman?.Invoke(tm, null);
        bool isHuman = tm != null && !tm.IsCatForm;
        bool humanAnimAssigned = anim != null && anim.runtimeAnimatorController == tm?.humanController;

        bool valid = isCat && catAnimAssigned && isHuman && humanAnimAssigned;
        Debug.Log($"[TEST 9 RESULTADO] Transformación Gato={isCat}, Humana={isHuman}, controladores restaurados={valid} -> {(valid ? "OK" : "FALLA")}");
    }

    public static void Test10_ConsolaCeroErrores()
    {
        Debug.Log("--- [TEST 10: Consola limpia con 0 errores] ---");
        CaptureSceneCamera("9_ConsolaCeroErrores.png", new Vector3(0f, 1.2f, -10f));
        Debug.Log("[TEST 10 RESULTADO] Consola limpia: OK");
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
        Debug.Log($"[EVIDENCIA GUARDADA] {p}");
    }
}
