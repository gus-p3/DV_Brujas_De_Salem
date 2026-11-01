using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Categorías visuales de recompensas para la animación de obtención de objetos (UX).
/// </summary>
public enum LootVisualCategory
{
    Key,
    Ingredient,
    Potion,
    Heart
}

/// <summary>
/// Sistema visual de retroalimentación (UX) al obtener objetos en cofres o en el mundo.
/// Cuando la bruja consigue una llave, un ingrediente, una poción o un corazón:
/// 1. Engendra en el mundo (sobre el cofre o la bruja) el icono real del objeto emergiendo en arco
///    con un halo mágico giratorio, destellos de partículas y una etiqueta flotante ("+1 ...").
/// 2. Muestra en pantalla una tarjeta ilustrada (Toast HUD) con el icono del objeto, su categoría
///    y su nombre para que el jugador siempre vea con total claridad qué acaba de obtener.
/// </summary>
public class LootPopupVisualizer : MonoBehaviour
{
    private static LootPopupVisualizer instance;

    private Canvas toastCanvas;
    private RectTransform toastContainer;
    private readonly List<RectTransform> activeToasts = new List<RectTransform>();

    private static Sprite cachedHaloSprite;
    private static Sprite cachedStarSprite;
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public static LootPopupVisualizer Instance
    {
        get
        {
            if (!Application.isPlaying) return null;
            if (instance == null)
            {
                instance = FindFirstObjectByType<LootPopupVisualizer>();
                if (instance == null)
                {
                    GameObject go = new GameObject("LootPopupVisualizer_Runtime");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<LootPopupVisualizer>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        EnsureToastCanvas();
    }

    /// <summary>
    /// Muestra la animación de obtención de una llave.
    /// </summary>
    public static void ShowKeyReward(Vector3 worldPos, string keyDisplayName, Sprite customSprite = null, float delay = 0f, float xOffset = 0f)
    {
        if (!Application.isPlaying) return;
        Sprite icon = customSprite != null ? customSprite : ResolveSprite("Interactive_Key", LootVisualCategory.Key);
        Color accent = new Color(1f, 0.84f, 0.18f, 1f);
        Instance.TriggerRewardPopup(worldPos, LootVisualCategory.Key, "¡LLAVE OBTENIDA!", $"+1 {keyDisplayName}", icon, accent, delay, xOffset);
    }

    /// <summary>
    /// Muestra la animación de obtención de un ingrediente de poción.
    /// </summary>
    public static void ShowIngredientReward(Vector3 worldPos, Level2.IngredientType type, int amount = 1, Sprite customSprite = null, float delay = 0f, float xOffset = 0f)
    {
        if (!Application.isPlaying) return;
        string spriteKey = type switch
        {
            Level2.IngredientType.Mushroom => "Ingredient_Mushroom",
            Level2.IngredientType.Spider => "Ingredient_Spider",
            Level2.IngredientType.WitheredFlower => "Ingredient_WitheredFlower",
            Level2.IngredientType.Root => "Ingredient_Root",
            _ => "Ingredient_Mushroom"
        };
        string displayName = type switch
        {
            Level2.IngredientType.Mushroom => "Hongo Nocturno",
            Level2.IngredientType.Spider => "Araña de Cripta",
            Level2.IngredientType.WitheredFlower => "Flor Marchita",
            Level2.IngredientType.Root => "Raíz de Mandrágora",
            _ => "Ingrediente"
        };
        Color accent = type switch
        {
            Level2.IngredientType.Mushroom => new Color(0.95f, 0.38f, 0.32f, 1f),
            Level2.IngredientType.Spider => new Color(0.72f, 0.52f, 1.0f, 1f),
            Level2.IngredientType.WitheredFlower => new Color(0.55f, 0.82f, 1.0f, 1f),
            Level2.IngredientType.Root => new Color(0.52f, 0.92f, 0.45f, 1f),
            _ => new Color(0.45f, 0.95f, 0.65f, 1f)
        };

        Sprite icon = customSprite != null ? customSprite : ResolveIngredientSprite(type, spriteKey);
        string progressSuffix = "";
        if (Level2.IngredientInventory.Instance != null)
        {
            var inv = Level2.IngredientInventory.Instance;
            int cur = type switch
            {
                Level2.IngredientType.Mushroom => inv.CurrentMushrooms,
                Level2.IngredientType.Spider => inv.CurrentSpiders,
                Level2.IngredientType.WitheredFlower => inv.CurrentFlowers,
                Level2.IngredientType.Root => inv.CurrentRoots,
                _ => 0
            };
            int req = type switch
            {
                Level2.IngredientType.Mushroom => inv.RequiredMushrooms,
                Level2.IngredientType.Spider => inv.RequiredSpiders,
                Level2.IngredientType.WitheredFlower => inv.RequiredFlowers,
                Level2.IngredientType.Root => inv.RequiredRoots,
                _ => 2
            };
            progressSuffix = $" ({cur}/{req})";
        }

        Instance.TriggerRewardPopup(worldPos, LootVisualCategory.Ingredient, "¡INGREDIENTE DE POCIÓN!", $"+{amount} {displayName}{progressSuffix}", icon, accent, delay, xOffset);
    }

    /// <summary>
    /// Muestra la animación de obtención de una poción.
    /// </summary>
    public static void ShowPotionReward(Vector3 worldPos, PotionType type, int amount = 1, Sprite customSprite = null, float delay = 0f, float xOffset = 0f)
    {
        if (!Application.isPlaying) return;
        string spriteKey = type switch
        {
            PotionType.Blind => "Potion_Blind",
            PotionType.Hypnotize => "Potion_Hypnotize",
            PotionType.Sleep => "Potion_Sleep",
            _ => "Potion_Blind"
        };
        string displayName = type switch
        {
            PotionType.Blind => "Poción de Ceguera",
            PotionType.Hypnotize => "Poción de Hipnosis",
            PotionType.Sleep => "Poción de Sueño",
            _ => "Poción"
        };
        Color accent = type switch
        {
            PotionType.Blind => new Color(1f, 0.90f, 0.35f, 1f),
            PotionType.Hypnotize => new Color(0.85f, 0.42f, 1f, 1f),
            PotionType.Sleep => new Color(0.38f, 0.72f, 1f, 1f),
            _ => new Color(0.45f, 0.85f, 1f, 1f)
        };

        Sprite icon = customSprite != null ? customSprite : ResolveSprite(spriteKey, LootVisualCategory.Potion, type);
        Instance.TriggerRewardPopup(worldPos, LootVisualCategory.Potion, "¡POCIÓN ADQUIRIDA!", $"+{amount} {displayName}", icon, accent, delay, xOffset);
    }

    /// <summary>
    /// Muestra la animación de obtención de un corazón.
    /// </summary>
    public static void ShowHeartReward(Vector3 worldPos, int amount = 1, Sprite customSprite = null, float delay = 0f, float xOffset = 0f)
    {
        if (!Application.isPlaying) return;
        Sprite icon = customSprite != null ? customSprite : ResolveSprite("Heart_Full", LootVisualCategory.Heart);
        Color accent = new Color(1f, 0.32f, 0.42f, 1f);
        Instance.TriggerRewardPopup(worldPos, LootVisualCategory.Heart, "¡VIDA RECUPERADA!", $"+{amount} Corazón", icon, accent, delay, xOffset);
    }

    private void TriggerRewardPopup(Vector3 worldPos, LootVisualCategory category, string header, string label, Sprite icon, Color accent, float delay, float xOffset)
    {
        StartCoroutine(SpawnRewardRoutine(worldPos, category, header, label, icon, accent, delay, xOffset));
    }

    private IEnumerator SpawnRewardRoutine(Vector3 worldPos, LootVisualCategory category, string header, string label, Sprite icon, Color accent, float delay, float xOffset)
    {
        if (delay > 0.001f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        // Reproducir sonido específico si hubo retraso escalonado (p. ej. múltiples objetos saliendo de un cofre)
        if (delay > 0.02f && AudioManager.Instance != null)
        {
            switch (category)
            {
                case LootVisualCategory.Key: AudioManager.Instance.PlayKeyPickupSound(); break;
                case LootVisualCategory.Ingredient: AudioManager.Instance.PlayIngredientPickupSound(); break;
                case LootVisualCategory.Potion: AudioManager.Instance.PlayPotionPickupSound(); break;
                case LootVisualCategory.Heart: AudioManager.Instance.PlayHeartHealSound(); break;
            }
        }

        SpawnWorldPopup(worldPos + new Vector3(xOffset, 0.35f, 0f), label, icon, accent);
        SpawnToastCard(header, label, icon, accent);
    }

    private void SpawnWorldPopup(Vector3 startPos, string label, Sprite icon, Color accent)
    {
        GameObject root = new GameObject("WorldLootPopup");
        root.transform.position = startPos;

        // 1. Halo mágico giratorio detrás del objeto
        GameObject haloGo = new GameObject("HaloGlow");
        haloGo.transform.SetParent(root.transform, false);
        SpriteRenderer haloSr = haloGo.AddComponent<SpriteRenderer>();
        haloSr.sprite = GetHaloSprite();
        haloSr.color = new Color(accent.r, accent.g, accent.b, 0.72f);
        haloSr.sortingLayerName = "Foreground";
        haloSr.sortingOrder = 58;
        haloGo.transform.localScale = Vector3.one * 1.45f;

        // 2. Estrella de destello giratoria
        GameObject starGo = new GameObject("StarBurst");
        starGo.transform.SetParent(root.transform, false);
        SpriteRenderer starSr = starGo.AddComponent<SpriteRenderer>();
        starSr.sprite = GetStarSprite();
        starSr.color = new Color(1f, 0.98f, 0.82f, 0.85f);
        starSr.sortingLayerName = "Foreground";
        starSr.sortingOrder = 59;
        starGo.transform.localScale = Vector3.one * 1.15f;

        // 3. Icono real del objeto obtenido
        GameObject iconGo = new GameObject("LootIcon");
        iconGo.transform.SetParent(root.transform, false);
        SpriteRenderer iconSr = iconGo.AddComponent<SpriteRenderer>();
        iconSr.sprite = icon;
        iconSr.sortingLayerName = "Foreground";
        iconSr.sortingOrder = 61;

        float baseIconScale = 1.0f;
        if (icon != null && icon.bounds.size.magnitude > 0.01f)
        {
            float maxDim = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
            baseIconScale = Mathf.Clamp(0.82f / maxDim, 0.45f, 2.4f);
        }
        iconGo.transform.localScale = Vector3.one * baseIconScale;

        // Sombra/contorno detrás del icono para máximo contraste sobre cualquier fondo
        GameObject rimGo = new GameObject("IconRim");
        rimGo.transform.SetParent(iconGo.transform, false);
        SpriteRenderer rimSr = rimGo.AddComponent<SpriteRenderer>();
        rimSr.sprite = icon;
        rimSr.color = new Color(0f, 0f, 0f, 0.75f);
        rimSr.sortingLayerName = "Foreground";
        rimSr.sortingOrder = 60;
        rimGo.transform.localScale = Vector3.one * 1.16f;

        // 4. Etiqueta de texto flotante en World Space sobre el icono
        GameObject canvasGo = new GameObject("WorldLabelCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(root.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0.68f, 0f);
        canvasGo.transform.localScale = Vector3.one * 0.012f;

        Canvas c = canvasGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        c.sortingLayerName = "Foreground";
        c.sortingOrder = 65;

        RectTransform crt = (RectTransform)canvasGo.transform;
        crt.sizeDelta = new Vector2(360f, 54f);

        GameObject bgGo = new GameObject("BadgeBg", typeof(RectTransform));
        bgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRt = (RectTransform)bgGo.transform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.04f, 0.08f, 0.88f);
        bgImg.raycastTarget = false;

        Outline bgOutline = bgGo.AddComponent<Outline>();
        bgOutline.effectColor = accent;
        bgOutline.effectDistance = new Vector2(2f, -2f);

        GameObject txtGo = new GameObject("LabelTMP", typeof(RectTransform));
        txtGo.transform.SetParent(canvasGo.transform, false);
        RectTransform txtRt = (RectTransform)txtGo.transform;
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(8f, 2f);
        txtRt.offsetMax = new Vector2(-8f, -2f);

        TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.96f, 0.82f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        // 5. Chispa de partículas alrededor
        Transform[] sparks = new Transform[6];
        SpriteRenderer[] sparkRenderers = new SpriteRenderer[6];
        Vector2[] sparkDirs = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            GameObject sp = new GameObject($"Spark_{i}");
            sp.transform.SetParent(root.transform, false);
            SpriteRenderer ssr = sp.AddComponent<SpriteRenderer>();
            ssr.sprite = GetStarSprite();
            ssr.color = i % 2 == 0 ? accent : new Color(1f, 0.96f, 0.75f, 1f);
            ssr.sortingLayerName = "Foreground";
            ssr.sortingOrder = 62;
            sp.transform.localScale = Vector3.one * 0.35f;
            sparks[i] = sp.transform;
            sparkRenderers[i] = ssr;
            float angle = (i * 60f + 25f) * Mathf.Deg2Rad;
            sparkDirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        StartCoroutine(AnimateWorldPopupRoutine(root, haloSr, starSr, iconSr, rimSr, bgImg, tmp, sparks, sparkRenderers, sparkDirs, startPos));
    }

    private IEnumerator AnimateWorldPopupRoutine(
        GameObject root,
        SpriteRenderer haloSr,
        SpriteRenderer starSr,
        SpriteRenderer iconSr,
        SpriteRenderer rimSr,
        Image badgeBg,
        TextMeshProUGUI tmp,
        Transform[] sparks,
        SpriteRenderer[] sparkRenderers,
        Vector2[] sparkDirs,
        Vector3 startPos)
    {
        float duration = 1.85f;
        float elapsed = 0f;
        Color haloBase = haloSr != null ? haloSr.color : Color.white;
        Color starBase = starSr != null ? starSr.color : Color.white;
        Color bgBase = badgeBg != null ? badgeBg.color : Color.black;
        Color txtBase = tmp != null ? tmp.color : Color.white;

        while (elapsed < duration && root != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Salto elástico inicial y ascenso flotante
            float scaleMult;
            if (t < 0.16f)
            {
                float p = t / 0.16f;
                scaleMult = Mathf.Lerp(0.15f, 1.32f, Mathf.Sin(p * Mathf.PI * 0.5f));
            }
            else if (t < 0.28f)
            {
                float p = (t - 0.16f) / 0.12f;
                scaleMult = Mathf.Lerp(1.32f, 1.0f, p);
            }
            else
            {
                scaleMult = 1.0f + Mathf.Sin((t - 0.28f) * 10f) * 0.06f;
            }

            // Ascenso suave en curva
            float riseY = t < 0.25f
                ? Mathf.Lerp(0f, 0.85f, Mathf.Sin((t / 0.25f) * Mathf.PI * 0.5f))
                : 0.85f + (t - 0.25f) * 0.65f;

            root.transform.position = startPos + new Vector3(0f, riseY, 0f);
            root.transform.localScale = Vector3.one * scaleMult;

            if (haloSr != null) haloSr.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * 90f);
            if (starSr != null) starSr.transform.localRotation = Quaternion.Euler(0f, 0f, -elapsed * 140f);
            if (iconSr != null) iconSr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 9f) * 8f);

            // Expansión de chispas
            for (int i = 0; i < sparks.Length; i++)
            {
                if (sparks[i] == null) continue;
                float spDist = Mathf.Lerp(0.15f, 0.95f, Mathf.Sqrt(t));
                sparks[i].localPosition = (Vector3)(sparkDirs[i] * spDist);
                sparks[i].localRotation = Quaternion.Euler(0f, 0f, elapsed * 220f + i * 45f);
                float spAlpha = Mathf.Clamp01(1f - (t * 1.25f));
                if (sparkRenderers[i] != null)
                {
                    Color sc = sparkRenderers[i].color;
                    sc.a = spAlpha;
                    sparkRenderers[i].color = sc;
                }
            }

            // Desvanecimiento suave en el tramo final
            float fade = t > 0.72f ? Mathf.Clamp01(1f - (t - 0.72f) / 0.28f) : 1f;
            if (haloSr != null) haloSr.color = new Color(haloBase.r, haloBase.g, haloBase.b, haloBase.a * fade);
            if (starSr != null) starSr.color = new Color(starBase.r, starBase.g, starBase.b, starBase.a * fade);
            if (iconSr != null) iconSr.color = new Color(1f, 1f, 1f, fade);
            if (rimSr != null) rimSr.color = new Color(0f, 0f, 0f, 0.75f * fade);
            if (badgeBg != null) badgeBg.color = new Color(bgBase.r, bgBase.g, bgBase.b, bgBase.a * fade);
            if (tmp != null) tmp.color = new Color(txtBase.r, txtBase.g, txtBase.b, fade);

            yield return null;
        }

        if (root != null)
        {
            Destroy(root);
        }
    }

    private void EnsureToastCanvas()
    {
        if (toastCanvas != null) return;

        GameObject cGo = new GameObject("LootToast_Canvas", typeof(RectTransform));
        cGo.transform.SetParent(transform, false);

        toastCanvas = cGo.AddComponent<Canvas>();
        toastCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        toastCanvas.sortingOrder = 140;

        CanvasScaler scaler = cGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject contGo = new GameObject("ToastStackContainer", typeof(RectTransform));
        contGo.transform.SetParent(cGo.transform, false);
        toastContainer = (RectTransform)contGo.transform;
        toastContainer.anchorMin = new Vector2(0.5f, 1f);
        toastContainer.anchorMax = new Vector2(0.5f, 1f);
        toastContainer.pivot = new Vector2(0.5f, 1f);
        toastContainer.anchoredPosition = new Vector2(0f, -92f);
        toastContainer.sizeDelta = new Vector2(520f, 300f);
    }

    private void SpawnToastCard(string header, string label, Sprite icon, Color accent)
    {
        EnsureToastCanvas();
        if (toastContainer == null) return;

        // Limitar tarjetas simultáneas para mantener limpia la UI
        while (activeToasts.Count >= 3)
        {
            RectTransform oldest = activeToasts[0];
            activeToasts.RemoveAt(0);
            if (oldest != null) Destroy(oldest.gameObject);
        }

        GameObject cardGo = new GameObject("LootToastCard", typeof(RectTransform));
        cardGo.transform.SetParent(toastContainer, false);
        RectTransform cardRt = (RectTransform)cardGo.transform;
        cardRt.anchorMin = new Vector2(0.5f, 1f);
        cardRt.anchorMax = new Vector2(0.5f, 1f);
        cardRt.pivot = new Vector2(0.5f, 1f);
        cardRt.sizeDelta = new Vector2(460f, 74f);

        CanvasGroup cg = cardGo.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;

        Image bg = cardGo.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.11f, 0.94f);
        bg.raycastTarget = false;

        Outline outline = cardGo.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // Marco brillante del icono a la izquierda
        GameObject iconFrameGo = new GameObject("IconFrame", typeof(RectTransform));
        iconFrameGo.transform.SetParent(cardGo.transform, false);
        RectTransform frameRt = (RectTransform)iconFrameGo.transform;
        frameRt.anchorMin = new Vector2(0f, 0.5f);
        frameRt.anchorMax = new Vector2(0f, 0.5f);
        frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.anchoredPosition = new Vector2(44f, 0f);
        frameRt.sizeDelta = new Vector2(56f, 56f);

        Image frameImg = iconFrameGo.AddComponent<Image>();
        frameImg.sprite = GetHaloSprite();
        frameImg.color = new Color(accent.r, accent.g, accent.b, 0.42f);
        frameImg.raycastTarget = false;

        GameObject iconImgGo = new GameObject("ItemIcon", typeof(RectTransform));
        iconImgGo.transform.SetParent(iconFrameGo.transform, false);
        RectTransform iconRt = (RectTransform)iconImgGo.transform;
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(44f, 44f);
        Image iconImg = iconImgGo.AddComponent<Image>();
        iconImg.sprite = icon;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Encabezado de categoría
        GameObject hdrGo = new GameObject("Header", typeof(RectTransform));
        hdrGo.transform.SetParent(cardGo.transform, false);
        RectTransform hdrRt = (RectTransform)hdrGo.transform;
        hdrRt.anchorMin = new Vector2(0f, 0.52f);
        hdrRt.anchorMax = new Vector2(1f, 0.94f);
        hdrRt.offsetMin = new Vector2(84f, 0f);
        hdrRt.offsetMax = new Vector2(-14f, 0f);

        TextMeshProUGUI hdrTmp = hdrGo.AddComponent<TextMeshProUGUI>();
        hdrTmp.text = header;
        hdrTmp.fontSize = 15f;
        hdrTmp.fontStyle = FontStyles.Bold;
        hdrTmp.color = accent;
        hdrTmp.alignment = TextAlignmentOptions.BottomLeft;
        hdrTmp.raycastTarget = false;

        // Nombre del objeto obtenido
        GameObject subGo = new GameObject("ItemName", typeof(RectTransform));
        subGo.transform.SetParent(cardGo.transform, false);
        RectTransform subRt = (RectTransform)subGo.transform;
        subRt.anchorMin = new Vector2(0f, 0.08f);
        subRt.anchorMax = new Vector2(1f, 0.54f);
        subRt.offsetMin = new Vector2(84f, 0f);
        subRt.offsetMax = new Vector2(-14f, 0f);

        TextMeshProUGUI subTmp = subGo.AddComponent<TextMeshProUGUI>();
        subTmp.text = label;
        subTmp.fontSize = 20f;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.color = new Color(0.98f, 0.96f, 0.90f, 1f);
        subTmp.alignment = TextAlignmentOptions.TopLeft;
        subTmp.raycastTarget = false;

        activeToasts.Add(cardRt);
        RepositionActiveToasts();

        StartCoroutine(AnimateToastCardRoutine(cardRt, cg, iconRt));
    }

    private void RepositionActiveToasts()
    {
        activeToasts.RemoveAll(t => t == null);
        for (int i = 0; i < activeToasts.Count; i++)
        {
            activeToasts[i].anchoredPosition = new Vector2(0f, -i * 82f);
        }
    }

    private IEnumerator AnimateToastCardRoutine(RectTransform cardRt, CanvasGroup cg, RectTransform iconRt)
    {
        float duration = 2.4f;
        float elapsed = 0f;

        while (elapsed < duration && cardRt != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            if (t < 0.12f)
            {
                float p = t / 0.12f;
                float s = Mathf.Lerp(0.75f, 1.06f, p);
                cardRt.localScale = new Vector3(s, s, 1f);
                cg.alpha = p;
            }
            else if (t < 0.20f)
            {
                float p = (t - 0.12f) / 0.08f;
                float s = Mathf.Lerp(1.06f, 1.0f, p);
                cardRt.localScale = new Vector3(s, s, 1f);
                cg.alpha = 1f;
            }
            else if (t > 0.80f)
            {
                float p = (t - 0.80f) / 0.20f;
                cg.alpha = 1f - p;
            }
            else
            {
                cardRt.localScale = Vector3.one;
                cg.alpha = 1f;
            }

            if (iconRt != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * 9f) * 0.10f;
                iconRt.localScale = new Vector3(pulse, pulse, 1f);
            }

            yield return null;
        }

        if (cardRt != null)
        {
            activeToasts.Remove(cardRt);
            Destroy(cardRt.gameObject);
            RepositionActiveToasts();
        }
    }

    // --- Resolución y respaldo procedural de iconos ---
    private static Sprite ResolveIngredientSprite(Level2.IngredientType type, string spriteKey)
    {
        if (Level2.IngredientHUD.Instance != null)
        {
            Sprite hudSp = Level2.IngredientHUD.Instance.GetIngredientSprite(type);
            if (hudSp != null)
            {
                spriteCache[spriteKey] = hudSp;
                return hudSp;
            }
        }

        if (spriteCache.TryGetValue(spriteKey, out Sprite cached) && cached != null) return cached;

        string altName = type switch
        {
            Level2.IngredientType.Mushroom => "Placeholder_Ingredient_Mushroom",
            Level2.IngredientType.Spider => "Placeholder_Ingredient_Spider",
            Level2.IngredientType.WitheredFlower => "Placeholder_Ingredient_WitheredFlower",
            Level2.IngredientType.Root => "Placeholder_Ingredient_Root",
            _ => "Placeholder_Ingredient_Mushroom"
        };

        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (Sprite s in allSprites)
        {
            if (s != null && (s.name == spriteKey || s.name == altName || s.name.StartsWith(altName)))
            {
                spriteCache[spriteKey] = s;
                return s;
            }
        }

#if UNITY_EDITOR
        Sprite editorSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Environment/Level2/{altName}.png");
        if (editorSp != null)
        {
            spriteCache[spriteKey] = editorSp;
            return editorSp;
        }
#endif

        Sprite fallback = CreateProceduralItemSprite(LootVisualCategory.Ingredient, (int)type);
        spriteCache[spriteKey] = fallback;
        return fallback;
    }

    private static Sprite ResolveSprite(string spriteName, LootVisualCategory category, PotionType potionType = PotionType.Blind)
    {
        if (spriteCache.TryGetValue(spriteName, out Sprite cached) && cached != null) return cached;

        string[] candidates = category switch
        {
            LootVisualCategory.Key => new[] { "Interactive_Key" },
            LootVisualCategory.Heart => new[] { "ui_icon_heart_full", "Item_Heart", "Heart_Heal", "ui_icon_heart" },
            LootVisualCategory.Potion => potionType switch
            {
                PotionType.Blind => new[] { "PotionItem_Blind", "ui_icon_flask_blind", "PotionProjectile_Blind" },
                PotionType.Hypnotize => new[] { "PotionItem_Hypnotize", "ui_icon_flask_hypnotize", "PotionProjectile_Hypnotize" },
                PotionType.Sleep => new[] { "PotionItem_Sleep", "ui_icon_flask_sleep", "PotionProjectile_Sleep" },
                _ => new[] { "PotionItem_Blind", "ui_icon_flask_blind" }
            },
            _ => new[] { spriteName }
        };

        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (string cand in candidates)
        {
            foreach (Sprite s in allSprites)
            {
                if (s != null && (s.name == cand || s.name.StartsWith(cand)))
                {
                    spriteCache[spriteName] = s;
                    return s;
                }
            }
        }

        Sprite fallback = CreateProceduralItemSprite(category, (int)potionType);
        spriteCache[spriteName] = fallback;
        return fallback;
    }

    private static Sprite CreateProceduralItemSprite(LootVisualCategory category, int subType)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color main = category switch
        {
            LootVisualCategory.Key => new Color(1f, 0.85f, 0.22f, 1f),
            LootVisualCategory.Heart => new Color(0.95f, 0.22f, 0.32f, 1f),
            LootVisualCategory.Potion => subType == 1 ? new Color(0.82f, 0.38f, 1f, 1f) : (subType == 2 ? new Color(0.35f, 0.68f, 1f, 1f) : new Color(1f, 0.88f, 0.32f, 1f)),
            _ => new Color(0.45f, 0.92f, 0.55f, 1f)
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - 15.5f) / 14f;
                float dy = (y - 15.5f) / 14f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 0.92f) { tex.SetPixel(x, y, Color.clear); continue; }
                if (r > 0.78f) { tex.SetPixel(x, y, new Color(0.1f, 0.08f, 0.05f, 1f)); continue; }
                float highlight = (dx < -0.15f && dy > 0.15f && r < 0.55f) ? 0.35f : 0f;
                tex.SetPixel(x, y, Color.Lerp(main, Color.white, highlight));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    public static Sprite GetHaloSprite()
    {
        if (cachedHaloSprite != null) return cachedHaloSprite;
        int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float core = Mathf.Clamp01(1f - r);
                float ring = Mathf.Exp(-Mathf.Pow((r - 0.65f) * 6.5f, 2f)) * 0.55f;
                float a = Mathf.Clamp01(core * core * 0.85f + ring);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        cachedHaloSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        return cachedHaloSprite;
    }

    public static Sprite GetStarSprite()
    {
        if (cachedStarSprite != null) return cachedStarSprite;
        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        int c = size / 2;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - c);
                int dy = Mathf.Abs(y - c);
                float a = 0f;
                if (dx == 0 && dy <= 10) a = 1f - dy / 11f;
                else if (dy == 0 && dx <= 10) a = 1f - dx / 11f;
                else if (dx == dy && dx <= 5) a = 0.8f - dx / 7f;
                else if (dx + dy <= 4) a = 0.9f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
        }
        tex.Apply();
        cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
        return cachedStarSprite;
    }
}
