using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Level2
{
    /// <summary>
    /// Controlador del HUD propio de ingredientes del Nivel 2.
    /// Opera en un Canvas independiente y se ubica en la esquina superior derecha
    /// para no interferir ni solaparse con el HUD principal (corazones a la izquierda,
    /// frascos de pociones en el centro/abajo y escoba a la derecha abajo).
    /// </summary>
    public class IngredientHUD : MonoBehaviour
    {
        public static IngredientHUD Instance { get; private set; }

        [Header("Elementos de Contador")]
        [SerializeField] private TextMeshProUGUI mushroomCountText;
        [SerializeField] private TextMeshProUGUI spiderCountText;
        [SerializeField] private TextMeshProUGUI flowerCountText;
        [SerializeField] private TextMeshProUGUI rootCountText;

        [Header("Iconos para Animación")]
        [SerializeField] private RectTransform mushroomIconTransform;
        [SerializeField] private RectTransform spiderIconTransform;
        [SerializeField] private RectTransform flowerIconTransform;
        [SerializeField] private RectTransform rootIconTransform;

        [Header("Colores")]
        [SerializeField] private Color normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color completeColor = new Color(1f, 0.85f, 0.25f, 1f);

        [Header("Banner de Mensajes Propios")]
        [SerializeField] private TextMeshProUGUI bannerText;
        [SerializeField] private GameObject bannerPanel;

        private Coroutine bannerCoroutine;
        private Coroutine punchMushroom;
        private Coroutine punchSpider;
        private Coroutine punchFlower;
        private Coroutine punchRoot;

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

            if (bannerPanel != null)
            {
                bannerPanel.SetActive(false);
            }
        }

        private void Start()
        {
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.OnIngredientCollected += HandleIngredientCollected;
                IngredientInventory.Instance.OnAllRequiredCollected += HandleAllCollected;
                IngredientInventory.Instance.OnInventoryReset += HandleInventoryReset;
            }

            RefreshAllDisplays();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.OnIngredientCollected -= HandleIngredientCollected;
                IngredientInventory.Instance.OnAllRequiredCollected -= HandleAllCollected;
                IngredientInventory.Instance.OnInventoryReset -= HandleInventoryReset;
            }
        }

        private void HandleIngredientCollected(IngredientType type, int current, int required)
        {
            UpdateSlotDisplay(type, current, required);
            TriggerIconPunch(type);
        }

        private void HandleAllCollected()
        {
            ShowWarning("¡Ingredientes completos! El portón hacia la iglesia está abierto", 4.0f);
        }

        private void HandleInventoryReset()
        {
            RefreshAllDisplays();
        }

        public void RefreshAllDisplays()
        {
            if (IngredientInventory.Instance == null) return;

            UpdateSlotDisplay(IngredientType.Mushroom,
                IngredientInventory.Instance.CurrentMushrooms,
                IngredientInventory.Instance.RequiredMushrooms);

            UpdateSlotDisplay(IngredientType.Spider,
                IngredientInventory.Instance.CurrentSpiders,
                IngredientInventory.Instance.RequiredSpiders);

            UpdateSlotDisplay(IngredientType.WitheredFlower,
                IngredientInventory.Instance.CurrentFlowers,
                IngredientInventory.Instance.RequiredFlowers);

            UpdateSlotDisplay(IngredientType.Root,
                IngredientInventory.Instance.CurrentRoots,
                IngredientInventory.Instance.RequiredRoots);
        }

        private void UpdateSlotDisplay(IngredientType type, int current, int required)
        {
            TextMeshProUGUI targetText = null;

            switch (type)
            {
                case IngredientType.Mushroom: targetText = mushroomCountText; break;
                case IngredientType.Spider: targetText = spiderCountText; break;
                case IngredientType.WitheredFlower: targetText = flowerCountText; break;
                case IngredientType.Root: targetText = rootCountText; break;
            }

            if (targetText != null)
            {
                targetText.text = $"{current}/{required}";
                targetText.color = current >= required ? completeColor : normalColor;
            }
        }

        private void TriggerIconPunch(IngredientType type)
        {
            RectTransform target = null;
            switch (type)
            {
                case IngredientType.Mushroom: target = mushroomIconTransform; break;
                case IngredientType.Spider: target = spiderIconTransform; break;
                case IngredientType.WitheredFlower: target = flowerIconTransform; break;
                case IngredientType.Root: target = rootIconTransform; break;
            }

            if (target != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(PunchScaleRoutine(target));
            }
        }

        private IEnumerator PunchScaleRoutine(RectTransform target)
        {
            Vector3 originalScale = Vector3.one;
            Vector3 punchScale = new Vector3(1.3f, 1.3f, 1f);

            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                if (t < 0.5f)
                {
                    target.localScale = Vector3.Lerp(originalScale, punchScale, t * 2f);
                }
                else
                {
                    target.localScale = Vector3.Lerp(punchScale, originalScale, (t - 0.5f) * 2f);
                }
                yield return null;
            }

            target.localScale = originalScale;
        }

        /// <summary>
        /// Muestra un banner emergente en pantalla para avisos de recolección o estado del portón.
        /// </summary>
        public void ShowWarning(string message, float duration = 2.5f)
        {
            if (bannerText == null && bannerPanel == null)
            {
                // Si no hay panel propio asignado, recurrir al banner global de HUDController
                HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                if (hud != null)
                {
                    hud.ShowMessage(message, duration);
                }
                return;
            }

            if (bannerCoroutine != null)
            {
                StopCoroutine(bannerCoroutine);
            }

            if (gameObject.activeInHierarchy)
            {
                bannerCoroutine = StartCoroutine(ShowBannerRoutine(message, duration));
            }
        }

        private IEnumerator ShowBannerRoutine(string message, float duration)
        {
            if (bannerText != null) bannerText.text = message;
            if (bannerPanel != null) bannerPanel.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (bannerPanel != null) bannerPanel.SetActive(false);
            if (bannerText != null) bannerText.text = "";
            bannerCoroutine = null;
        }

        /// <summary>
        /// Asigna referencias programáticamente desde builders o tests de editor.
        /// </summary>
        public void SetupReferences(
            TextMeshProUGUI mushroomText, TextMeshProUGUI spiderText,
            TextMeshProUGUI flowerText, TextMeshProUGUI rootText,
            RectTransform mushroomIcon, RectTransform spiderIcon,
            RectTransform flowerIcon, RectTransform rootIcon,
            TextMeshProUGUI banner = null, GameObject bannerObj = null)
        {
            mushroomCountText = mushroomText;
            spiderCountText = spiderText;
            flowerCountText = flowerText;
            rootCountText = rootText;

            mushroomIconTransform = mushroomIcon;
            spiderIconTransform = spiderIcon;
            flowerIconTransform = flowerIcon;
            rootIconTransform = rootIcon;

            bannerText = banner;
            bannerPanel = bannerObj;
        }
    }
}
