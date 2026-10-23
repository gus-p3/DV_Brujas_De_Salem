using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Prueba automática en Play Mode de Level2.unity (escena base).
/// Usa la entrada programada de PlayerController (SetScriptedInput) para verificar
/// caminar, saltar, transformarse (gato/humana) y caída en el vacío de prueba (KillZone).
/// Escribe el resultado en Temp/Level2_PlayTest.txt. Pensado para -executeMethod (sin -quit).
/// </summary>
[InitializeOnLoad]
public static class Level2PlayTest
{
    private const string Key = "Level2PlayTest.Running";
    private const string OutPath = "Logs/Level2_PlayTest.txt";

    private static double t0;
    private static int step;
    private static float x0, y0, maxY;
    private static int hearts0;
    private static readonly List<string> errors = new List<string>();
    private static readonly StringBuilder sb = new StringBuilder();

    static Level2PlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    [MenuItem("Brujas/Nivel 2/Prueba automática Play Level2")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(Level2SceneBuilder.Level2Path, OpenSceneMode.Single);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.EnteredPlayMode)
        {
            Application.logMessageReceived += OnLog;
            t0 = EditorApplication.timeSinceStartup;
            step = 0;
            EditorApplication.update += Tick;
        }
    }

    private static void OnLog(string msg, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg);
    }

    private static void Check(string name, bool ok, string detail) => sb.AppendLine($"[{(ok ? "PASS" : "FAIL")}] {name} :: {detail}");

    private static void Tick()
    {
        double t = EditorApplication.timeSinceStartup - t0;
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var tm = pc != null ? pc.GetComponent<TransformationManager>() : null;
        if (pc == null) { Check("Witch presente", false, "no hay PlayerController"); Finish(); return; }

        switch (step)
        {
            case 0 when t > 2.0:
                Check("Estado inicial", GameManager.Instance != null, $"GM={(GameManager.Instance != null)} state={GameManager.Instance?.CurrentState} hearts={GameManager.Instance?.CurrentHearts} grounded={pc.IsGrounded} pos={pc.transform.position}");
                x0 = pc.transform.position.x;
                pc.EnableScriptedInput(true);
                pc.SetScriptedInput(1f, 0f);
                step++; break;
            case 1 when t > 3.5:
                pc.SetScriptedInput(0f, 0f);
                Check("Caminar", pc.transform.position.x > x0 + 1f, $"x {x0:F2} -> {pc.transform.position.x:F2}");
                step++; break;
            case 2 when t > 4.2:
                y0 = pc.transform.position.y; maxY = y0;
                pc.ScriptedJumpPressed();
                step++; break;
            case 3:
                maxY = Mathf.Max(maxY, pc.transform.position.y);
                if (t > 5.2) { pc.ScriptedJumpReleased(); Check("Saltar", maxY > y0 + 0.5f, $"y0={y0:F2} maxY={maxY:F2}"); step++; }
                break;
            case 4 when t > 6.2:
                tm.TransformToCat();
                step++; break;
            case 5 when t > 7.2:
                Check("Transformar a gato", tm.IsCatForm, $"IsCatForm={tm.IsCatForm}");
                tm.TryTransformToHuman();
                step++; break;
            case 6 when t > 8.2:
                Check("Volver a humana", !tm.IsCatForm, $"IsCatForm={tm.IsCatForm}");
                hearts0 = GameManager.Instance != null ? GameManager.Instance.CurrentHearts : -1;
                pc.transform.position = new Vector3(27f, 1f, 0f); // sobre el vacío de prueba
                step++; break;
            case 7 when t > 11.0:
                int h = GameManager.Instance != null ? GameManager.Instance.CurrentHearts : -1;
                Check("Caída al vacío (KillZone)", h == hearts0 - 1 && pc.transform.position.y > -3f, $"hearts {hearts0} -> {h}, pos={pc.transform.position}");
                Finish(); break;
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        Check("Consola sin errores", errors.Count == 0, errors.Count + " errores");
        foreach (var e in errors) sb.AppendLine("  " + e);
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[Level2PlayTest]\n" + sb);
        SessionState.SetBool(Key, false);
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.Exit(sb.ToString().Contains("[FAIL]") ? 1 : 0);
    }
}
