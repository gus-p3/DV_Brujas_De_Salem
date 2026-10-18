using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Panel de rendimiento para escenas de prueba: FPS promedio, mínimo de la última ventana y memoria.
/// </summary>
public class PerfOverlay : MonoBehaviour
{
    [SerializeField] private float window = 1f;

    private float elapsed, worstDelta;
    private int frames;
    private float fps, minFps;
    private GUIStyle style;

    /// <summary>Último promedio de FPS medido (para verificaciones automáticas).</summary>
    public float AverageFps => fps;
    public float MinFps => minFps;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        frames++;
        worstDelta = Mathf.Max(worstDelta, dt);
        if (elapsed >= window)
        {
            fps = frames / elapsed;
            minFps = 1f / Mathf.Max(worstDelta, 1e-4f);
            elapsed = 0f;
            frames = 0;
            worstDelta = 0f;
        }
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            style.normal.textColor = Color.white;
        }
        long mem = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        GUI.Label(new Rect(12, 8, 600, 28), $"FPS {fps:0} (mín {minFps:0})   Memoria {mem} MB", style);
    }
}
