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

    private Action onCloseCallback;
    private bool canClose = false;
    private Coroutine activeAnimationRoutine;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (contentContainer == null) contentContainer = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Muestra el panel con los datos del tip y ejecuta onClosed al terminar.
    /// </summary>
    public void Show(TutorialTip tip, Action onClosed)
    {
        onCloseCallback = onClosed;
        canClose = false;

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
            continuePromptText.text = "Continuar: Enter o clic";
        }

        gameObject.SetActive(true);

        if (activeAnimationRoutine != null) StopCoroutine(activeAnimationRoutine);
        activeAnimationRoutine = StartCoroutine(ShowAnimationRoutine());
    }

    private IEnumerator ShowAnimationRoutine()
    {
        float elapsed = 0f;
        Vector3 initialScale = Vector3.one * 0.9f;

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

        // Esperar el bloqueo de entrada de 0.3 segundos sin escalar
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

        // Efecto de parpadeo suave del texto de continuar
        if (continuePromptText != null)
        {
            float alpha = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4.0f);
            Color c = continuePromptText.color;
            c.a = alpha;
            continuePromptText.color = c;
        }

        // Cierre con Enter o Clic izquierdo
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0))
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
