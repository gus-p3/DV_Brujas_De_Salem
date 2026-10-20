using UnityEngine;

/// <summary>
/// Atmósfera del nivel: luciérnagas que flotan y parpadean, y hojas de otoño que caen girando.
/// Se coloca en la cámara; las partículas viven en el mundo (no se arrastran con la cámara) y se emiten
/// en una caja algo mayor que la vista. Las texturas se generan por código.
/// </summary>
public class AmbientParticles : MonoBehaviour
{
    [Header("Luciérnagas")]
    [SerializeField] private bool fireflies = true;
    [SerializeField] private float firefliesPerSecond = 4f;
    [SerializeField] private Color fireflyColor = new Color(1f, 0.9f, 0.45f, 1f);

    [Header("Hojas")]
    [SerializeField] private bool leaves = true;
    [SerializeField] private float leavesPerSecond = 2.2f;
    [SerializeField] private Color[] leafColors =
    {
        new Color(0.85f, 0.42f, 0.12f), new Color(0.72f, 0.24f, 0.12f), new Color(0.93f, 0.66f, 0.2f)
    };

    [Header("Capas")]
    [SerializeField] private string sortingLayer = "Foreground";
    [SerializeField] private int sortingOrder = 30;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (fireflies) CreateFireflies();
        if (leaves) CreateLeaves();
    }

    private Vector3 ViewBox(float extra)
    {
        float h = cam != null ? cam.orthographicSize * 2f : 10f;
        float w = h * (cam != null ? cam.aspect : 16f / 9f);
        return new Vector3(w + extra, h + extra, 1f);
    }

    private ParticleSystem NewSystem(string systemName, Texture2D tex)
    {
        GameObject go = new GameObject(systemName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 10f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = sortingOrder;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) r.material = new Material(shader) { mainTexture = tex };
        ParticleSystem.MainModule main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = true;
        return ps;
    }

    private void CreateFireflies()
    {
        ParticleSystem ps = NewSystem("Fireflies", Glow());
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
        main.startColor = fireflyColor;
        main.maxParticles = 60;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = firefliesPerSecond;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        Vector3 box = ViewBox(4f);
        shape.scale = new Vector3(box.x, box.y * 0.7f, 1f);
        shape.position = new Vector3(0f, -box.y * 0.1f, 0f);

        // Vuelo errático y lento
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.2f;

        // Aparecen, laten y se apagan
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.15f), new GradientAlphaKey(0.25f, 0.35f),
                new GradientAlphaKey(0.95f, 0.55f), new GradientAlphaKey(0.3f, 0.75f), new GradientAlphaKey(0f, 1f)
            });
        col.color = g;
        ps.Play();
    }

    private void CreateLeaves()
    {
        ParticleSystem ps = NewSystem("Leaves", Leaf());
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.26f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 50;
        Gradient colors = new Gradient();
        GradientColorKey[] keys = new GradientColorKey[leafColors.Length];
        for (int i = 0; i < leafColors.Length; i++) keys[i] = new GradientColorKey(leafColors[i], leafColors.Length == 1 ? 0f : i / (float)(leafColors.Length - 1));
        colors.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = leavesPerSecond;

        // Caen desde arriba de la vista con viento suave hacia la izquierda
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        Vector3 box = ViewBox(6f);
        shape.scale = new Vector3(box.x, 0.5f, 1f);
        shape.position = new Vector3(2f, box.y * 0.5f, 0f);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.9f, -0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.1f, -0.6f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;

        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        col.color = fade;
        ps.Play();
    }

    private static Texture2D Glow()
    {
        const int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            float core = Mathf.Clamp01(1f - d * 3.5f);
            float halo = Mathf.Clamp01(1f - d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core + halo * halo * 0.6f)));
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D Leaf()
    {
        // Hoja ovalada con punta y nervio central oscuro; el color lo pone la partícula
        const int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n * 2f - 1f;
            float v = (y + 0.5f) / n * 2f - 1f;
            float width = 0.55f * (1f - v * v) * (1f - 0.35f * v);
            float inside = Mathf.Clamp01((width - Mathf.Abs(u)) * 12f);
            float rib = Mathf.Abs(u) < 0.06f && v > -0.9f ? 0.55f : 1f;
            float edge = Mathf.Clamp01((width - Mathf.Abs(u)) * 12f) < 1f ? 0.45f : 1f;
            float c = rib * edge;
            tex.SetPixel(x, y, new Color(c, c, c, inside));
        }
        tex.Apply();
        return tex;
    }
}
