using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de visualización de consejos de tutorial contextuales en el Canvas del HUD.
/// Presenta fondo tipo pergamino, texto formateado mediante ControlsMap, animación de
/// aparición no escalada y cierre mediante Enter o clic tras un breve bloqueo de seguridad.
/// </summary>
public class TutorialPanel : MonoBehaviour
{
    [Header("Elementos de UI")]
    [Tooltip("Texto para el título del tutorial.")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Texto para el cuerpo de la explicación con marcadores.")]
    [SerializeField] private TextMeshProUGUI bodyText;

    [Tooltip("Imagen opcional para ilustrar el consejo.")]
    [SerializeField] private Image iconImage;

    [Tooltip("Texto indicador de pie para continuar.")]
    [SerializeField] private TextMeshProUGUI continuePromptText;

    [Tooltip("CanvasGroup para animar el fundido y escala.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Transform del contenedor visual que se escala al abrir.")]
    [SerializeField] private RectTransform contentContainer;

    [Header("Tiempos")]
    [Tooltip("Tiempo mínimo (s) que ignora pulsaciones para evitar cierres accidentales.")]
    [SerializeField] private float inputBlockDuration = 0.3f;

    [Tooltip("Duración de la animación de aparición (s).")]
    [SerializeField] private float popInDuration = 0.2f;

    [Tooltip("Tiempo de cierre automático (s) para no interrumpir la acción.")]
    [SerializeField] private float autoCloseDelay = 4.5f;

    private Action onCloseCallback;
    private bool canClose = false;
    private float openElapsed = 0f;
    private Coroutine activeAnimationRoutine;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (contentContainer == null)
        {
            Transform db = transform.Find("DialogBox");
            contentContainer = db != null ? db.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        }
        ApplyCleanBannerLayout();
    }

    private void ApplyCleanBannerLayout()
    {
        // Desactivar cualquier fondo modal de pantalla completa para no tapar el juego
        Image rootImg = GetComponent<Image>();
        if (rootImg != null && contentContainer != null && contentContainer.gameObject != gameObject)
        {
            rootImg.color = new Color(0f, 0f, 0f, 0f);
            rootImg.raycastTarget = false;
        }

        if (contentContainer != null && contentContainer.gameObject != gameObject)
        {
            contentContainer.anchorMin = new Vector2(0.5f, 1f);
            contentContainer.anchorMax = new Vector2(0.5f, 1f);
            contentContainer.pivot = new Vector2(0.5f, 1f);
            contentContainer.anchoredPosition = new Vector2(0f, -86f);
            contentContainer.sizeDelta = new Vector2(720f, 145f);

            Image boxImg = contentContainer.GetComponent<Image>();
            if (boxImg != null)
            {
                boxImg.color = new Color(0.11f, 0.08f, 0.06f, 0.90f);
                boxImg.raycastTarget = false;
            }
        }

        if (titleText != null)
        {
            titleText.fontSize = 24f;
            titleText.color = new Color(0.96f, 0.82f, 0.42f, 1f);
            titleText.alignment = TextAlignmentOptions.Top;
            RectTransform rt = titleText.rectTransform;
            rt.anchorMin = new Vector2(0.05f, 0.64f);
            rt.anchorMax = new Vector2(0.95f, 0.94f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        if (bodyText != null)
        {
            bodyText.fontSize = 19f;
            bodyText.color = new Color(0.95f, 0.93f, 0.88f, 1f);
            bodyText.alignment = TextAlignmentOptions.Center;
            RectTransform rt = bodyText.rectTransform;
            rt.anchorMin = new Vector2(0.05f, 0.22f);
            rt.anchorMax = new Vector2(0.95f, 0.64f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        if (continuePromptText != null)
        {
            continuePromptText.fontSize = 14f;
            continuePromptText.color = new Color(0.75f, 0.70f, 0.58f, 0.85f);
            continuePromptText.alignment = TextAlignmentOptions.Bottom;
            RectTransform rt = continuePromptText.rectTransform;
            rt.anchorMin = new Vector2(0.05f, 0.04f);
            rt.anchorMax = new Vector2(0.95f, 0.22f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// Muestra el panel con los datos del tip y ejecuta onClosed al terminar.
    /// </summary>
    public void Show(TutorialTip tip, Action onClosed)
    {
        onCloseCallback = onClosed;
        canClose = false;
        openElapsed = 0f;
        ApplyCleanBannerLayout();

        if (titleText != null)
        {
            titleText.text = tip.title;
        }

        if (bodyText != null)
        {
            bodyText.text = ControlsMap.Resolve(tip.body);
        }

        if (iconImage != null)
        {
            if (tip.icon != null)
            {
                iconImage.sprite = tip.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (continuePromptText != null)
        {
            continuePromptText.text = "[Enter] Cerrar";
        }

        gameObject.SetActive(true);

        if (activeAnimationRoutine != null) StopCoroutine(activeAnimationRoutine);
        activeAnimationRoutine = StartCoroutine(ShowAnimationRoutine());
    }

    private IEnumerator ShowAnimationRoutine()
    {
        float elapsed = 0f;
        Vector3 initialScale = Vector3.one * 0.94f;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (contentContainer != null) contentContainer.localScale = initialScale;

        while (elapsed < popInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popInDuration);

            if (canvasGroup != null) canvasGroup.alpha = t;
            if (contentContainer != null) contentContainer.localScale = Vector3.Lerp(initialScale, Vector3.one, t);

            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (contentContainer != null) contentContainer.localScale = Vector3.one;

        float blockTimer = 0f;
        while (blockTimer < inputBlockDuration)
        {
            blockTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        canClose = true;
    }

    private void Update()
    {
        if (!canClose) return;

        openElapsed += Time.unscaledDeltaTime;

        if (continuePromptText != null)
        {
            float alpha = 0.6f + 0.35f * Mathf.Sin(Time.unscaledTime * 4.0f);
            Color c = continuePromptText.color;
            c.a = alpha;
            continuePromptText.color = c;
        }

        // Cierre con Enter, Clic o automáticamente tras autoCloseDelay segundos
        if (openElapsed >= autoCloseDelay || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0))
        {
            Close();
        }
    }

    /// <summary>
    /// Oculta el panel y dispara el callback de cierre.
    /// </summary>
    public void Close()
    {
        canClose = false;
        gameObject.SetActive(false);

        Action callback = onCloseCallback;
        onCloseCallback = null;
        callback?.Invoke();
    }
}
