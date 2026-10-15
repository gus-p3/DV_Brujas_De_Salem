using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de la interfaz de corazones de vida en el HUD.
/// Muestra los corazones anclados en la esquina superior izquierda, reproduce
/// la animación de fractura cuadro a cuadro al perder un corazón (con sacudón y sonido)
/// y la animación de curación al recuperarlo.
/// </summary>
public class HeartsUI : MonoBehaviour
{
    [Header("Sprites de Corazones")]
    [Tooltip("Sprite del corazón lleno en estado normal.")]
    [SerializeField] private Sprite fullHeartSprite;

    [Tooltip("Sprite del corazón vacío.")]
    [SerializeField] private Sprite emptyHeartSprite;

    [Tooltip("Cuadros de la animación de corazón roto (6 cuadros a 10 fps).")]
    [SerializeField] private Sprite[] breakFrames;

    [Tooltip("Cuadros de la animación de corazón curado (4 cuadros).")]
    [SerializeField] private Sprite[] healFrames;

    [Header("Configuración Visual")]
    [Tooltip("Tamaño de cada corazón en píxeles (ancho x alto).")]
    [SerializeField] private Vector2 heartSize = new Vector2(36f, 36f);

    [Tooltip("Espaciado horizontal entre corazones.")]
    [SerializeField] private float spacing = 8f;

    [Tooltip("Margen superior e izquierdo (px).")]
    [SerializeField] private Vector2 margin = new Vector2(24f, 24f);

    [Header("Animaciones y Efectos")]
    [Tooltip("Velocidad de reproducción de cuadros de animación (fps).")]
    [SerializeField] private float animationFps = 10f;

    [Tooltip("Duración del sacudón leve al perder un corazón (s).")]
    [SerializeField] private float shakeDuration = 0.15f;

    [Tooltip("Intensidad del sacudón en píxeles.")]
    [SerializeField] private float shakeMagnitude = 4.0f;

    private readonly List<Image> heartImages = new List<Image>();
    private readonly List<Vector2> basePositions = new List<Vector2>();
    private Coroutine[] activeCoroutines;

    private int previousHearts = 3;

    private void Awake()
    {
        ConfigureAnchors();
        LoadSpritesIfMissing();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartsChanged -= HandleHeartsChanged;
            GameManager.Instance.OnHeartsChanged += HandleHeartsChanged;
            InitializeHearts(GameManager.Instance.CurrentHearts, GameManager.Instance.MaxHearts);
        }
        else
        {
            InitializeHearts(3, 3);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartsChanged -= HandleHeartsChanged;
        }
    }

    private void ConfigureAnchors()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            // Anclar a esquina superior izquierda: pivote (0, 1), anclas (0, 1)
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(margin.x, -margin.y);
        }
    }

    private void LoadSpritesIfMissing()
    {
        if (fullHeartSprite == null)
        {
            fullHeartSprite = Resources.Load<Sprite>("ui_icon_heart_full");
        }

        if (emptyHeartSprite == null)
        {
            emptyHeartSprite = Resources.Load<Sprite>("ui_icon_heart_empty");
        }
    }

    public void SetFrames(Sprite full, Sprite empty, Sprite[] breaks, Sprite[] heals)
    {
        fullHeartSprite = full;
        emptyHeartSprite = empty;
        breakFrames = breaks;
        healFrames = heals;
    }

    public void InitializeHearts(int current, int max)
    {
        // Limpiar corazones existentes
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear();
        basePositions.Clear();
        activeCoroutines = new Coroutine[max];

        for (int i = 0; i < max; i++)
        {
            GameObject heartObj = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
            heartObj.transform.SetParent(transform, false);

            RectTransform rt = heartObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = heartSize;

            Vector2 pos = new Vector2(i * (heartSize.x + spacing), 0f);
            rt.anchoredPosition = pos;
            basePositions.Add(pos);

            Image img = heartObj.GetComponent<Image>();
            img.sprite = (i < current) ? fullHeartSprite : emptyHeartSprite;
            img.preserveAspect = true;

            heartImages.Add(img);
        }

        previousHearts = current;
    }

    private void HandleHeartsChanged(int current, int max)
    {
        if (heartImages.Count != max)
        {
            InitializeHearts(current, max);
            return;
        }

        if (current < previousHearts)
        {
            // Perdió corazones: reproducir animación de fractura en los corazones perdidos
            for (int i = previousHearts - 1; i >= current; i--)
            {
                if (i >= 0 && i < heartImages.Count)
                {
                    if (activeCoroutines[i] != null) StopCoroutine(activeCoroutines[i]);
                    activeCoroutines[i] = StartCoroutine(AnimateHeartBreakRoutine(i));
                }
            }
        }
        else if (current > previousHearts)
        {
            // Recuperó corazones: reproducir animación de curación
            for (int i = previousHearts; i < current; i++)
            {
                if (i >= 0 && i < heartImages.Count)
                {
                    if (activeCoroutines[i] != null) StopCoroutine(activeCoroutines[i]);
                    activeCoroutines[i] = StartCoroutine(AnimateHeartHealRoutine(i));
                }
            }
        }

        previousHearts = current;
    }

    private IEnumerator AnimateHeartBreakRoutine(int index)
    {
        Image heartImage = heartImages[index];
        RectTransform rt = heartImage.GetComponent<RectTransform>();
        Vector2 basePos = basePositions[index];

        // Sonido de corazón roto
        if (AudioManager.Instance != null && AudioManager.Instance.heartBreakClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.heartBreakClip);
        }

        // Sacudón y animación cuadro a cuadro simultáneos
        float frameTime = 1f / animationFps;
        int frameCount = (breakFrames != null && breakFrames.Length > 0) ? breakFrames.Length : 6;

        float totalDuration = frameCount * frameTime;
        float elapsed = 0f;
        int currentFrame = 0;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Sacudón leve
            if (elapsed < shakeDuration)
            {
                float shakeOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
                rt.anchoredPosition = basePos + new Vector2(shakeOffset, shakeOffset * 0.5f);
            }
            else
            {
                rt.anchoredPosition = basePos;
            }

            // Cuadro de animación
            int targetFrame = Mathf.Clamp(Mathf.FloorToInt(elapsed / frameTime), 0, frameCount - 1);
            if (targetFrame != currentFrame && breakFrames != null && targetFrame < breakFrames.Length)
            {
                currentFrame = targetFrame;
                heartImage.sprite = breakFrames[currentFrame];
            }

            yield return null;
        }

        rt.anchoredPosition = basePos;
        heartImage.sprite = emptyHeartSprite;
        activeCoroutines[index] = null;
    }

    private IEnumerator AnimateHeartHealRoutine(int index)
    {
        Image heartImage = heartImages[index];
        RectTransform rt = heartImage.GetComponent<RectTransform>();
        Vector2 basePos = basePositions[index];

        // Sonido de corazón curado
        if (AudioManager.Instance != null && AudioManager.Instance.heartHealClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.heartHealClip);
        }

        float frameTime = 1f / animationFps;
        int frameCount = (healFrames != null && healFrames.Length > 0) ? healFrames.Length : 4;
        float totalDuration = frameCount * frameTime;
        float elapsed = 0f;
        int currentFrame = 0;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            int targetFrame = Mathf.Clamp(Mathf.FloorToInt(elapsed / frameTime), 0, frameCount - 1);
            if (targetFrame != currentFrame && healFrames != null && targetFrame < healFrames.Length)
            {
                currentFrame = targetFrame;
                heartImage.sprite = healFrames[currentFrame];
            }

            yield return null;
        }

        rt.anchoredPosition = basePos;
        heartImage.sprite = fullHeartSprite;
        activeCoroutines[index] = null;
    }
}
