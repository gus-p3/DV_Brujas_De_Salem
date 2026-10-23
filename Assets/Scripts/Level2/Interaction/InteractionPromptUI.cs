using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level2
{
    /// <summary>
    /// Interfaz contextual que muestra en pantalla una única indicación de interacción
    /// ("E — Entrar", "E — Abrir cofre", "W / ↑ — Subir", etc.) cuando la bruja se acerca
    /// a un elemento interactivo en el Nivel 2.
    /// Desaparece automáticamente al alejarse y nunca superpone múltiples avisos.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        private static InteractionPromptUI _instance;
        public static InteractionPromptUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<InteractionPromptUI>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Referencias UI")]
        [SerializeField] private Canvas promptCanvas;
        [SerializeField] private RectTransform badgeRect;
        [SerializeField] private Image badgeBackground;
        [SerializeField] private TextMeshProUGUI promptText;

        private Transform targetTransform;
        private float targetHeightOffset = 1.2f;
        private Camera mainCam;
        private bool isVisible;

        public bool IsVisible => isVisible && badgeRect != null && badgeRect.gameObject.activeSelf;
        public string CurrentText => promptText != null ? promptText.text : string.Empty;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureUIHierarchy();
            HidePrompt();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnsureUIHierarchy()
        {
            if (promptCanvas == null)
            {
                promptCanvas = GetComponent<Canvas>();
                if (promptCanvas == null)
                {
                    promptCanvas = gameObject.AddComponent<Canvas>();
                }
            }

            promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            promptCanvas.sortingOrder = 110;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (badgeRect == null)
            {
                Transform existing = transform.Find("PromptBadge");
                if (existing != null)
                {
                    badgeRect = existing.GetComponent<RectTransform>();
                }
            }

            if (badgeRect == null)
            {
                GameObject badgeObj = new GameObject("PromptBadge");
                badgeObj.transform.SetParent(transform, false);
                badgeRect = badgeObj.AddComponent<RectTransform>();
                badgeRect.sizeDelta = new Vector2(460f, 56f);
                badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
                badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);

                badgeBackground = badgeObj.AddComponent<Image>();
                badgeBackground.color = new Color(0.11f, 0.07f, 0.06f, 0.88f);
                badgeBackground.raycastTarget = false;

                Outline outline = badgeObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.88f, 0.68f, 0.24f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);

                GameObject textObj = new GameObject("PromptText");
                textObj.transform.SetParent(badgeObj.transform, false);
                RectTransform tr = textObj.AddComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(16f, 4f);
                tr.offsetMax = new Vector2(-16f, -4f);

                promptText = textObj.AddComponent<TextMeshProUGUI>();
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.fontSize = 26f;
                promptText.color = new Color(1f, 0.94f, 0.78f, 1f);
                promptText.fontStyle = FontStyles.Bold;
                promptText.raycastTarget = false;
            }
            else
            {
                if (badgeBackground == null) badgeBackground = badgeRect.GetComponent<Image>();
                if (promptText == null) promptText = badgeRect.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        public void ShowPrompt(string text, Transform worldTarget, float heightOffset = 1.25f)
        {
            if (string.IsNullOrEmpty(text))
            {
                HidePrompt();
                return;
            }

            EnsureUIHierarchy();
            targetTransform = worldTarget;
            targetHeightOffset = heightOffset;

            if (promptText != null && promptText.text != text)
            {
                promptText.text = text;
                float preferredWidth = Mathf.Clamp(promptText.preferredWidth + 48f, 240f, 680f);
                badgeRect.sizeDelta = new Vector2(preferredWidth, 54f);
            }

            if (!badgeRect.gameObject.activeSelf)
            {
                badgeRect.gameObject.SetActive(true);
            }
            isVisible = true;
            UpdateBadgePosition();
        }

        public void HidePrompt()
        {
            isVisible = false;
            targetTransform = null;
            if (badgeRect != null && badgeRect.gameObject.activeSelf)
            {
                badgeRect.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (!isVisible || badgeRect == null) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                HidePrompt();
                return;
            }

            UpdateBadgePosition();
        }

        private void UpdateBadgePosition()
        {
            if (badgeRect == null) return;

            if (targetTransform == null)
            {
                badgeRect.anchoredPosition = new Vector2(0f, -340f);
                return;
            }

            if (mainCam == null)
            {
                mainCam = Camera.main;
                if (mainCam == null) mainCam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            if (mainCam == null)
            {
                badgeRect.anchoredPosition = new Vector2(0f, 120f);
                return;
            }

            Vector3 worldPos = targetTransform.position + new Vector3(0f, targetHeightOffset, 0f);
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

            RectTransform canvasRect = promptCanvas.GetComponent<RectTransform>();
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 localPoint))
            {
                float halfW = canvasRect.rect.width * 0.5f - 180f;
                float halfH = canvasRect.rect.height * 0.5f - 90f;
                localPoint.x = Mathf.Clamp(localPoint.x, -halfW, halfW);
                localPoint.y = Mathf.Clamp(localPoint.y, -halfH, halfH);
                badgeRect.anchoredPosition = localPoint;
            }
        }
    }
}
