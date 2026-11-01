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

        [Header("Sprites de Ingredientes")]
        [SerializeField] private Sprite mushroomSprite;
        [SerializeField] private Sprite spiderSprite;
        [SerializeField] private Sprite flowerSprite;
        [SerializeField] private Sprite rootSprite;

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

            EnsureIconSpritesAssigned();
        }

        private void Start()
        {
            EnsureIconSpritesAssigned();

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

            if (mushroomIcon != null && mushroomIcon.GetComponent<Image>() != null && mushroomIcon.GetComponent<Image>().sprite != null)
                mushroomSprite = mushroomIcon.GetComponent<Image>().sprite;
            if (spiderIcon != null && spiderIcon.GetComponent<Image>() != null && spiderIcon.GetComponent<Image>().sprite != null)
                spiderSprite = spiderIcon.GetComponent<Image>().sprite;
            if (flowerIcon != null && flowerIcon.GetComponent<Image>() != null && flowerIcon.GetComponent<Image>().sprite != null)
                flowerSprite = flowerIcon.GetComponent<Image>().sprite;
            if (rootIcon != null && rootIcon.GetComponent<Image>() != null && rootIcon.GetComponent<Image>().sprite != null)
                rootSprite = rootIcon.GetComponent<Image>().sprite;

            bannerText = banner;
            bannerPanel = bannerObj;
            EnsureIconSpritesAssigned();
        }

        public Sprite GetIngredientSprite(IngredientType type)
        {
            return type switch
            {
                IngredientType.Mushroom => mushroomSprite != null ? mushroomSprite : GetIconImageSprite(mushroomIconTransform),
                IngredientType.Spider => spiderSprite != null ? spiderSprite : GetIconImageSprite(spiderIconTransform),
                IngredientType.WitheredFlower => flowerSprite != null ? flowerSprite : GetIconImageSprite(flowerIconTransform),
                IngredientType.Root => rootSprite != null ? rootSprite : GetIconImageSprite(rootIconTransform),
                _ => mushroomSprite
            };
        }

        private void OnValidate()
        {
            EnsureIconSpritesAssigned();
        }

        public void EnsureIconSpritesAssigned()
        {
#if UNITY_EDITOR
            if (mushroomSprite == null)
                mushroomSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level2/Placeholder_Ingredient_Mushroom.png");
            if (spiderSprite == null)
                spiderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level2/Placeholder_Ingredient_Spider.png");
            if (flowerSprite == null)
                flowerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level2/Placeholder_Ingredient_WitheredFlower.png");
            if (rootSprite == null)
                rootSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level2/Placeholder_Ingredient_Root.png");
#endif
            BindOrRestoreIcon(mushroomIconTransform, ref mushroomSprite);
            BindOrRestoreIcon(spiderIconTransform, ref spiderSprite);
            BindOrRestoreIcon(flowerIconTransform, ref flowerSprite);
            BindOrRestoreIcon(rootIconTransform, ref rootSprite);
        }

        private static Sprite GetIconImageSprite(RectTransform rt)
        {
            if (rt == null) return null;
            Image img = rt.GetComponent<Image>();
            return img != null ? img.sprite : null;
        }

        private static void BindOrRestoreIcon(RectTransform rt, ref Sprite storedSprite)
        {
            if (rt == null) return;
            Image img = rt.GetComponent<Image>();
            if (img == null) return;

            if (img.sprite != null && storedSprite == null)
            {
                storedSprite = img.sprite;
            }
            else if (img.sprite == null && storedSprite != null)
            {
                img.sprite = storedSprite;
            }
            img.preserveAspect = true;
            img.color = Color.white;
        }
    }
}
