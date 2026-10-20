using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Vida del menú principal:
/// - Aparición suave (fundido desde negro y el título bajando un poco).
/// - Movimiento lento del fondo (zoom y desplazamiento sutiles) para que no se sienta estático.
/// - Luciérnagas que flotan y parpadean sobre el fondo, y hojas que caen.
/// - Un gato animado sentado junto al camino.
/// - Selección inicial para jugar con teclado o mando.
/// </summary>
public class MenuAmbience : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform title;
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private RectTransform particleLayer;
    [SerializeField] private Image cat;
    [SerializeField] private Sprite[] catFrames;
    [SerializeField] private GameObject firstSelected;

    [Header("Ajustes")]
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField] private int fireflyCount = 26;
    [SerializeField] private int leafCount = 10;
    [SerializeField] private float catFps = 7f;

    private class Mote
    {
        public RectTransform rt;
        public Image img;
        public Vector2 basePos, velocity;
        public float phase, speed, size;
        public bool leaf;
    }

    private readonly List<Mote> motes = new List<Mote>();
    private Vector2 titleBase;
    private float time;
    private static Sprite glow, leaf;

    private void Start()
    {
        if (title != null) titleBase = title.anchoredPosition;
        if (fadeGroup != null) fadeGroup.alpha = 0f;
        if (particleLayer != null)
        {
            for (int i = 0; i < fireflyCount; i++) motes.Add(CreateMote(false));
            for (int i = 0; i < leafCount; i++) motes.Add(CreateMote(true));
        }
        if (firstSelected != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    private Mote CreateMote(bool isLeaf)
    {
        GameObject go = new GameObject(isLeaf ? "Leaf" : "Firefly", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(particleLayer, false);
        Image img = go.GetComponent<Image>();
        img.sprite = isLeaf ? Leaf() : Glow();
        img.raycastTarget = false;
        Rect r = particleLayer.rect;
        Mote m = new Mote
        {
            rt = (RectTransform)go.transform,
            img = img,
            leaf = isLeaf,
            basePos = new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax)),
            phase = Random.value * 10f,
            speed = Random.Range(0.6f, 1.4f),
            size = isLeaf ? Random.Range(16f, 26f) : Random.Range(10f, 22f),
            velocity = isLeaf ? new Vector2(Random.Range(-40f, -15f), Random.Range(-55f, -30f)) : Vector2.zero
        };
        m.rt.sizeDelta = new Vector2(m.size, m.size);
        img.color = isLeaf
            ? Color.Lerp(new Color(0.85f, 0.42f, 0.12f), new Color(0.95f, 0.68f, 0.22f), Random.value)
            : new Color(1f, 0.9f, 0.5f, 0f);
        return m;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        time += dt;

        // Aparición
        float k = Mathf.Clamp01(time / fadeDuration);
        float e = 1f - (1f - k) * (1f - k);
        if (fadeGroup != null) fadeGroup.alpha = e;
        if (title != null) title.anchoredPosition = titleBase + new Vector2(0f, Mathf.Lerp(40f, 0f, e) + Mathf.Sin(time * 0.8f) * 4f);

        // Fondo: zoom y paneo lentos
        if (background != null)
        {
            float s = 1.06f + Mathf.Sin(time * 0.07f) * 0.025f;
            background.localScale = new Vector3(s, s, 1f);
            background.anchoredPosition = new Vector2(Mathf.Sin(time * 0.05f) * 22f, Mathf.Cos(time * 0.04f) * 10f);
        }

        // Luciérnagas y hojas
        if (particleLayer != null)
        {
            Rect r = particleLayer.rect;
            foreach (Mote m in motes)
            {
                if (m.leaf)
                {
                    m.basePos += m.velocity * dt;
                    if (m.basePos.y < r.yMin - 30f || m.basePos.x < r.xMin - 30f)
                        m.basePos = new Vector2(Random.Range(r.xMin, r.xMax + 200f), r.yMax + 30f);
                    m.rt.anchoredPosition = m.basePos + new Vector2(Mathf.Sin(time * 1.3f + m.phase) * 24f, 0f);
                    m.rt.localRotation = Quaternion.Euler(0f, 0f, (time * 60f * m.speed + m.phase * 30f) % 360f);
                }
                else
                {
                    m.rt.anchoredPosition = m.basePos + new Vector2(
                        Mathf.Sin(time * 0.35f * m.speed + m.phase) * 40f,
                        Mathf.Cos(time * 0.27f * m.speed + m.phase * 1.3f) * 28f);
                    float pulse = Mathf.Clamp01(Mathf.Sin(time * 1.6f * m.speed + m.phase) * 0.7f + 0.4f);
                    m.img.color = new Color(1f, 0.9f, 0.5f, pulse * 0.85f * e);
                }
            }
        }

        // Gato
        if (cat != null && catFrames != null && catFrames.Length > 0)
        {
            int f = Mathf.FloorToInt(time * catFps) % catFrames.Length;
            cat.sprite = catFrames[f];
        }
    }

    private static Sprite Glow()
    {
        if (glow != null) return glow;
        const int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            float a = Mathf.Clamp01(1f - d * 3.2f) + Mathf.Pow(Mathf.Clamp01(1f - d), 2f) * 0.55f;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
        }
        tex.Apply();
        glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return glow;
    }

    private static Sprite Leaf()
    {
        if (leaf != null) return leaf;
        const int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
            float width = 0.55f * (1f - v * v) * (1f - 0.35f * v);
            float a = Mathf.Clamp01((width - Mathf.Abs(u)) * 12f);
            float c = Mathf.Abs(u) < 0.06f ? 0.6f : 1f;
            tex.SetPixel(x, y, new Color(c, c, c, a));
        }
        tex.Apply();
        leaf = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return leaf;
    }
}
