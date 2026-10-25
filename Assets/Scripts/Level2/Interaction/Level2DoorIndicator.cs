using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level2
{
    /// <summary>
    /// Indicador en World Space para las puertas interactivas (exteriores, interiores y sótano) del Nivel 2.
    /// - Muestra el aviso claro ("Q: Entrar...", "Q: Salir a la calle", "Q: Salir del sótano") sobre la puerta
    ///   con fundido suave (CanvasGroup) al acercarse.
    /// - En las puertas de salida interiores mantiene además un discreto emblema luminoso cálido para que
    ///   el jugador identifique siempre dónde está la salida de cada habitación o sótano.
    /// </summary>
    public class Level2DoorIndicator : MonoBehaviour
    {
        [SerializeField] private float triggerRadius = 2.2f;
        [SerializeField] private float badgeHeightOffset = 1.65f;
        [SerializeField] private float fadeSpeed = 8.0f;
        [SerializeField] private bool isInteriorExit = false;

        private IInteractable linkedInteractable;
        private Canvas worldCanvas;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI labelText;
        private Transform playerTransform;
        private PlayerInteractor playerInteractor;

        public bool IsInteriorExit => isInteriorExit;
        public string DisplayedText => labelText != null ? labelText.text : string.Empty;
        public float CurrentAlpha => canvasGroup != null ? canvasGroup.alpha : 0f;

        private void Awake()
        {
            linkedInteractable = GetComponent<IInteractable>();
            EnsureWorldBadge();
        }

        public void Configure(bool interiorExit, float heightOffset = 1.65f)
        {
            isInteriorExit = interiorExit;
            badgeHeightOffset = heightOffset;
            linkedInteractable = GetComponent<IInteractable>();
            EnsureWorldBadge();
            RefreshText(null);
        }

        public void EnsureWorldBadge()
        {
            Transform badgeTr = transform.Find("DoorWorldIndicator_Canvas");
            GameObject canvasGO;
            if (badgeTr != null)
            {
                canvasGO = badgeTr.gameObject;
            }
            else
            {
                canvasGO = new GameObject("DoorWorldIndicator_Canvas");
                canvasGO.transform.SetParent(transform, false);
            }

            canvasGO.transform.localPosition = new Vector3(0f, badgeHeightOffset, 0f);
            canvasGO.transform.localRotation = Quaternion.identity;
            canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            worldCanvas = canvasGO.GetComponent<Canvas>();
            if (worldCanvas == null) worldCanvas = canvasGO.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingLayerName = "Foreground";
            worldCanvas.sortingOrder = 25;

            RectTransform rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320f, 54f);

            canvasGroup = canvasGO.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Transform bgTr = canvasGO.transform.Find("BadgeBG");
            GameObject bgGO = bgTr != null ? bgTr.gameObject : new GameObject("BadgeBG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            RectTransform bgRt = bgGO.GetComponent<RectTransform>();
            if (bgRt == null) bgRt = bgGO.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            Image bgImg = bgGO.GetComponent<Image>();
            if (bgImg == null) bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.08f, 0.05f, 0.92f);
            bgImg.raycastTarget = false;

            Outline outline = bgGO.GetComponent<Outline>();
            if (outline == null) outline = bgGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.75f, 0.28f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            Transform txtTr = bgGO.transform.Find("Label");
            GameObject txtGO = txtTr != null ? txtTr.gameObject : new GameObject("Label");
            txtGO.transform.SetParent(bgGO.transform, false);
            RectTransform txtRt = txtGO.GetComponent<RectTransform>();
            if (txtRt == null) txtRt = txtGO.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8f, 2f);
            txtRt.offsetMax = new Vector2(-8f, -2f);

            labelText = txtGO.GetComponent<TextMeshProUGUI>();
            if (labelText == null) labelText = txtGO.AddComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 24f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = new Color(1f, 0.95f, 0.80f, 1f);
            labelText.raycastTarget = false;

            RefreshText(null);
            canvasGroup.alpha = 0f;
        }

        public void RefreshText(GameObject interactor)
        {
            if (linkedInteractable == null) linkedInteractable = GetComponent<IInteractable>();
            if (linkedInteractable != null && labelText != null)
            {
                labelText.text = linkedInteractable.GetPromptText(interactor);
            }
        }

        private void Update()
        {
            if (canvasGroup == null) return;

            if (playerTransform == null)
            {
                GameObject witch = GameObject.Find("Witch");
                if (witch != null)
                {
                    playerTransform = witch.transform;
                    playerInteractor = witch.GetComponent<PlayerInteractor>();
                }
            }

            bool shouldShow = false;
            if (playerTransform != null)
            {
                float dist = Vector2.Distance(playerTransform.position, transform.position);
                bool canInteract = linkedInteractable == null || linkedInteractable.CanInteract(playerTransform.gameObject);
                bool isFocused = playerInteractor != null && playerInteractor.CurrentFocus == linkedInteractable;
                shouldShow = canInteract && (isFocused || (dist <= triggerRadius && (playerInteractor == null || playerInteractor.CurrentFocus == null)));
                if (shouldShow)
                {
                    RefreshText(playerTransform.gameObject);
                }
            }

            float targetAlpha = shouldShow ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }
}
