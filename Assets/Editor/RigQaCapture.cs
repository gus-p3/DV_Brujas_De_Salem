using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// QA visual del rig: simula cada estado fotograma a fotograma (60 fps) y guarda una hoja de contacto
/// por estado en Docs/Arte/sprint0/qa para revisar rigidez, saltos de pose y separación de piezas.
/// </summary>
public static class RigQaCapture
{
    private const string WitchRigPrefab = "Assets/Prefabs/WitchRig.prefab";
    private const int FrameW = 260, FrameH = 340, Columns = 8;
    private const float Dt = 1f / 60f;

    private enum State { Idle, Walk, Run, Jump, Cast, Hurt, Die, Fly }

    [MenuItem("Tools/Rig/QA visual (capturas por estado)")]
    public static void CaptureAll()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Arte/sprint0/qa"));
        Directory.CreateDirectory(outDir);
        foreach (State s in System.Enum.GetValues(typeof(State)))
        {
            string path = Path.Combine(outDir, $"qa_{s.ToString().ToLowerInvariant()}.png");
            Capture(s, path);
        }
        Debug.Log($"[RigQaCapture] Hojas de QA guardadas en {outDir}");
    }

    private static void Capture(State state, string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchRigPrefab);
        GameObject inst = Object.Instantiate(prefab, new Vector3(5000f, 0f, 0f), Quaternion.identity);
        inst.hideFlags = HideFlags.HideAndDontSave;
        WitchRigAnimator rig = inst.GetComponent<WitchRigAnimator>();
        rig.EnsureInitialized();
        rig.LandingDust = false;
        rig.demoMode = true;

        GameObject camGo = new GameObject("QA Camera") { hideFlags = HideFlags.HideAndDontSave };
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 1.1f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.46f, 0.47f, 0.52f);
        cam.transform.position = new Vector3(5000f, 0.95f, -10f);
        cam.enabled = false;
        RenderTexture rt = new RenderTexture(FrameW, FrameH, 24);
        cam.targetTexture = rt;

        // Duración y fotogramas capturados por estado
        float duration = state switch
        {
            State.Idle => 7.8f,
            State.Walk => 1.15f / 2.2f,
            State.Run => 2.4f / 6f,
            State.Jump => 1.45f,
            State.Cast => 0.5f,
            State.Hurt => 0.45f,
            State.Die => 1.3f,
            State.Fly => 1.4f,
            _ => 1f
        };
        int frames = state == State.Idle || state == State.Jump || state == State.Die ? 16 : 8;
        float warmup = state == State.Walk || state == State.Run || state == State.Fly ? 1.5f : 0.5f;

        Texture2D sheet = new Texture2D(FrameW * Columns, FrameH * Mathf.CeilToInt(frames / (float)Columns), TextureFormat.RGB24, false);
        float vy = 0f, height = 0f;
        bool air = false;

        int totalSteps = Mathf.RoundToInt((warmup + duration) / Dt);
        int warmSteps = Mathf.RoundToInt(warmup / Dt);
        int captured = 0;
        for (int step = 0; step <= totalSteps && captured < frames; step++)
        {
            float tState = (step - warmSteps) * Dt;
            if (step == warmSteps)
            {
                if (state == State.Cast) rig.TriggerCast();
                if (state == State.Hurt) rig.TriggerHurt();
                if (state == State.Die) rig.TriggerDie();
                if (state == State.Jump) { air = true; vy = Mathf.Sqrt(2f * 30f * 2f); }
            }

            rig.demoGrounded = true;
            rig.demoFlying = state == State.Fly;
            rig.demoVelocity = state switch
            {
                State.Walk => new Vector2(2.2f, 0f),
                State.Run => new Vector2(6f, 0f),
                State.Fly => new Vector2(3f, 0f),
                _ => Vector2.zero
            };
            if (air)
            {
                vy -= 30f * (vy < 0f ? 1.5f : 1f) * Dt;
                height += vy * Dt;
                if (height <= 0f) { height = 0f; air = false; }
                rig.demoGrounded = !air;
                rig.demoVelocity = new Vector2(0f, air ? vy : 0f);
            }
            inst.transform.position = new Vector3(5000f, height, 0f);
            cam.transform.position = new Vector3(state == State.Die ? 5000.45f : 5000f, 0.95f + height, -10f);
            cam.orthographicSize = state == State.Die ? 1.25f : 1.1f;
            rig.Tick(Dt);

            if (step >= warmSteps)
            {
                float slot = duration / frames;
                if (tState + 1e-4f >= captured * slot)
                {
                    cam.Render();
                    RenderTexture.active = rt;
                    int col = captured % Columns, row = captured / Columns;
                    int y = sheet.height - (row + 1) * FrameH;
                    sheet.ReadPixels(new Rect(0, 0, FrameW, FrameH), col * FrameW, y);
                    RenderTexture.active = null;
                    captured++;
                }
            }
        }

        sheet.Apply();
        File.WriteAllBytes(path, sheet.EncodeToPNG());

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(sheet);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(inst);
    }
}
