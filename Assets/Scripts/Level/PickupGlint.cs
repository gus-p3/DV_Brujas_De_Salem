using UnityEngine;

/// <summary>
/// Hace notar un objeto coleccionable sin hacerlo flotar: un resplandor suave detrás que late
/// lentamente y un destello de estrella de vez en cuando sobre el objeto. Los sprites se generan
/// por código (no requiere arte adicional).
/// </summary>
public class PickupGlint : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private float glowSize = 1.7f;
    [SerializeField] private Vector2 glowAlpha = new Vector2(0.10f, 0.30f);
    [SerializeField] private float pulsePeriod = 1.8f;
    [SerializeField] private Vector2 sparkleInterval = new Vector2(2.2f, 4.2f);

    private static Sprite softCircle;
    private static Sprite star;

    private SpriteRenderer item;
    private SpriteRenderer glow;
    private SpriteRenderer sparkle;
    private float phase;
    private float nextSparkle;
    private float sparkleTime = -1f;

    public void SetColor(Color c) => glowColor = c;

    private void Start()
    {
        item = GetComponentInChildren<SpriteRenderer>();
        if (item == null || item.sprite == null)
        {
            enabled = false;
            return;
        }
        phase = Random.value * pulsePeriod;
        nextSparkle = Time.time + Random.Range(sparkleInterval.x, sparkleInterval.y);

        Bounds b = item.bounds;
        float size = Mathf.Max(b.size.x, b.size.y) * glowSize;

        glow = CreateChild("Glow", SoftCircle(), item.sortingOrder - 1);
        glow.transform.position = b.center;
        glow.transform.localScale = Vector3.one * size / Mathf.Max(0.0001f, transform.lossyScale.x);

        sparkle = CreateChild("Sparkle", Star(), item.sortingOrder + 1);
        sparkle.enabled = false;
    }

    private SpriteRenderer CreateChild(string childName, Sprite sprite, int order)
    {
        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = item.sortingLayerID;
        sr.sortingOrder = order;
        return sr;
    }

    private void Update()
    {
        float t = (Mathf.Sin((Time.time + phase) * Mathf.PI * 2f / pulsePeriod) + 1f) * 0.5f;
        Color g = glowColor;
        g.a = Mathf.Lerp(glowAlpha.x, glowAlpha.y, t);
        glow.color = g;

        if (sparkleTime < 0f && Time.time >= nextSparkle)
        {
            sparkleTime = 0f;
            Bounds b = item.bounds;
            sparkle.transform.position = new Vector3(
                Random.Range(b.min.x + b.size.x * 0.2f, b.max.x - b.size.x * 0.2f),
                Random.Range(b.center.y, b.max.y - b.size.y * 0.1f), 0f);
            sparkle.enabled = true;
        }
        if (sparkleTime >= 0f)
        {
            sparkleTime += Time.deltaTime;
            float k = sparkleTime / 0.45f;
            float s = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 0.35f;
            sparkle.transform.localScale = Vector3.one * s / Mathf.Max(0.0001f, transform.lossyScale.x);
            sparkle.transform.rotation = Quaternion.Euler(0f, 0f, k * 45f);
            if (k >= 1f)
            {
                sparkle.enabled = false;
                sparkleTime = -1f;
                nextSparkle = Time.time + Random.Range(sparkleInterval.x, sparkleInterval.y);
            }
        }
    }

    private static Sprite SoftCircle()
    {
        if (softCircle != null) return softCircle;
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            float a = Mathf.Clamp01(1f - d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        softCircle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return softCircle;
    }

    private static Sprite Star()
    {
        if (star != null) return star;
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = Mathf.Abs(x + 0.5f - n / 2f) / (n / 2f);
            float dy = Mathf.Abs(y + 0.5f - n / 2f) / (n / 2f);
            // Estrella de cuatro puntas: brazos finos que se afinan hacia la punta
            float arm = Mathf.Max(Mathf.Clamp01(1f - dx - dy * 6f), Mathf.Clamp01(1f - dy - dx * 6f));
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 3f);
            float a = Mathf.Clamp01(arm + core);
            tex.SetPixel(x, y, new Color(1f, 0.97f, 0.85f, a));
        }
        tex.Apply();
        star = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return star;
    }
}
