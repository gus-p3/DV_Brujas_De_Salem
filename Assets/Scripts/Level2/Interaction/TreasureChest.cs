using System;
using System.Collections;
using UnityEngine;

namespace Level2
{
    public enum ChestRewardType
    {
        Ingredient = 0,
        PotionBlind = 1,
        PotionHypnotize = 2,
        PotionSleep = 3,
        Heart = 4,
        Key = 5,
        ScoreBonus = 6
    }

    /// <summary>
    /// Cofre interactivo situado en el interior de las casas coloniales del Nivel 2.
    /// Soporta cofres abiertos directamente con E/Q y cofres cerrados que exigen una llave específica
    /// de KeyInventory. Otorga recompensas (ingredientes, pociones, corazones, llaves o puntos)
    /// y dispara la animación de recompensa visual (LootPopupVisualizer).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TreasureChest : MonoBehaviour, IInteractable
    {
        [Header("Identificación y Bloqueo")]
        [SerializeField] private string chestId = "chest_default";
        [SerializeField] private string chestDisplayName = "Cofre Colonial";
        [SerializeField] private bool requiresKey = false;
        [SerializeField] private string requiredKeyId = "";
        [SerializeField] private string requiredKeyDisplayName = "Llave del Cofre";

        [Header("Recompensa Principal")]
        [SerializeField] private ChestRewardType primaryReward = ChestRewardType.PotionSleep;
        [SerializeField] private IngredientType ingredientReward = IngredientType.Mushroom;
        [SerializeField] private string ingredientUniqueId = "ing_chest_bonus";
        [SerializeField] private string rewardKeyId = "";
        [SerializeField] private string rewardKeyDisplayName = "";
        [SerializeField] private int rewardAmount = 1;
        [SerializeField] private int bonusScore = 200;

        [Header("Visuales")]
        [SerializeField] private SpriteRenderer chestRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;

        [Header("Presentación opcional (Nivel 3)")]
        [Tooltip("Si es verdadero, la bruja puede abrirlo en forma de gato (cofres dentro de casas con aldeanos dormidos).")]
        [SerializeField] private bool allowCatForm = false;
        [Tooltip("Fotogramas de la animación de apertura (del cerrado al abierto). Vacío = cambio directo de sprite.")]
        [SerializeField] private Sprite[] openAnimationFrames;
        [SerializeField] private float openFrameDuration = 0.07f;
        [Tooltip("Sprite que emerge del cofre al abrirlo (p. ej. la llave). Vacío = solo el aviso de recompensa.")]
        [SerializeField] private Sprite rewardRevealSprite;

        private bool isOpened = false;

        public string InteractionId => chestId;
        public int Priority => 25;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.15f;

        public string ChestId => chestId;
        public bool IsOpened => isOpened;
        public bool RequiresKey => requiresKey;
        public string RequiredKeyId => requiredKeyId;
        public bool AllowCatForm => allowCatForm;

        public event Action<TreasureChest> OnChestOpened;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
            if (chestRenderer == null) chestRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (KeyInventory.Instance != null && KeyInventory.Instance.IsChestOpened(chestId))
            {
                isOpened = true;
            }
            UpdateVisuals();
        }

        public void Configure(
            string id,
            string displayName,
            Sprite closed,
            Sprite open,
            bool locked = false,
            string keyId = "",
            string keyName = "")
        {
            chestId = id;
            chestDisplayName = displayName;
            closedSprite = closed;
            openSprite = open;
            requiresKey = locked;
            requiredKeyId = keyId;
            requiredKeyDisplayName = keyName;

            if (chestRenderer == null) chestRenderer = GetComponent<SpriteRenderer>();
            UpdateVisuals();
        }

        public void Configure(
            string id,
            string displayName,
            int score,
            bool givePotion,
            PotionType potionType,
            bool giveHeart,
            Sprite closed,
            Sprite open,
            bool locked = false,
            string keyId = "",
            string keyName = "")
        {
            Configure(id, displayName, closed, open, locked, keyId, keyName);
            bonusScore = score;
            if (givePotion)
            {
                primaryReward = potionType switch
                {
                    PotionType.Blind => ChestRewardType.PotionBlind,
                    PotionType.Hypnotize => ChestRewardType.PotionHypnotize,
                    _ => ChestRewardType.PotionSleep
                };
            }
            else if (giveHeart)
            {
                primaryReward = ChestRewardType.Heart;
            }
            else
            {
                primaryReward = ChestRewardType.ScoreBonus;
            }
        }

        /// <summary>Animación de apertura, objeto que emerge y acceso en forma de gato (usado por el Nivel 3).</summary>
        public void ConfigurePresentation(bool catCanOpen, Sprite[] frames, Sprite revealSprite, float frameDuration = 0.07f)
        {
            allowCatForm = catCanOpen;
            openAnimationFrames = frames;
            rewardRevealSprite = revealSprite;
            openFrameDuration = Mathf.Max(0.02f, frameDuration);
        }

        public void ConfigureReward(
            ChestRewardType type,
            int amount = 1,
            int score = 200,
            IngredientType ingType = IngredientType.Mushroom,
            string ingId = "ing_chest_01",
            string grantedKeyId = "",
            string grantedKeyName = "")
        {
            primaryReward = type;
            rewardAmount = Mathf.Max(1, amount);
            bonusScore = score;
            ingredientReward = ingType;
            ingredientUniqueId = ingId;
            rewardKeyId = grantedKeyId;
            rewardKeyDisplayName = grantedKeyName;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (isOpened || interactor == null) return false;
            return true;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (isOpened) return "";

            if (requiresKey)
            {
                if (!string.IsNullOrEmpty(requiredKeyId) && KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId))
                {
                    return $"{Level2Controls.InteractKey}: Abrir {chestDisplayName} ({requiredKeyDisplayName})";
                }
                return $"{Level2Controls.InteractKey}: {chestDisplayName} cerrado (Requiere {requiredKeyDisplayName})";
            }

            return $"{Level2Controls.InteractKey}: Abrir {chestDisplayName}";
        }

        public void Interact(GameObject interactor)
        {
            if (isOpened || interactor == null) return;

            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            if (tm != null && tm.IsCatForm && !allowCatForm)
            {
                Level2CatPickupGuard.NotifyCatBlocked(forceShow: true);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayErrorSound();
                return;
            }

            if (requiresKey)
            {
                if (string.IsNullOrEmpty(requiredKeyId) || KeyInventory.Instance == null || !KeyInventory.Instance.HasKey(requiredKeyId))
                {
                    ShowMessage($"Cerrado con llave. Necesitas: {requiredKeyDisplayName}");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayLockedSound();
                    return;
                }
            }

            OpenChest();
        }

        public void OpenChest()
        {
            if (isOpened) return;
            isOpened = true;

            if (KeyInventory.Instance != null)
            {
                KeyInventory.Instance.MarkChestOpened(chestId);
            }

            bool animate = Application.isPlaying && gameObject.activeInHierarchy &&
                           openAnimationFrames != null && openAnimationFrames.Length > 1;
            if (animate) StartCoroutine(OpenAnimationRoutine());
            else UpdateVisuals();
            DimIdleGlow();
            if (Application.isPlaying && gameObject.activeInHierarchy && rewardRevealSprite != null)
            {
                StartCoroutine(RewardRevealRoutine());
            }
            string rewardDescription = GrantReward();

            if (GameManager.Instance != null && bonusScore > 0)
            {
                GameManager.Instance.AddScore(bonusScore);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayChestOpenSound();
            }

            ShowMessage($"¡{chestDisplayName} abierto! Obtenido: {rewardDescription}");
            OnChestOpened?.Invoke(this);
        }

        private string GrantReward()
        {
            switch (primaryReward)
            {
                case ChestRewardType.Ingredient:
                    if (IngredientInventory.Instance != null)
                    {
                        IngredientInventory.Instance.Collect(ingredientReward, ingredientUniqueId);
                    }
                    LootPopupVisualizer.ShowIngredientReward(transform.position, ingredientReward, 1);
                    return $"Ingrediente ({GetIngredientName(ingredientReward)})";

                case ChestRewardType.PotionBlind:
                    if (GameManager.Instance != null) GameManager.Instance.AddSpellCharges(rewardAmount, 0, 0);
                    LootPopupVisualizer.ShowPotionReward(transform.position, PotionType.Blind, rewardAmount);
                    return $"+{rewardAmount} Poción de Ceguera";

                case ChestRewardType.PotionHypnotize:
                    if (GameManager.Instance != null) GameManager.Instance.AddSpellCharges(0, rewardAmount, 0);
                    LootPopupVisualizer.ShowPotionReward(transform.position, PotionType.Hypnotize, rewardAmount);
                    return $"+{rewardAmount} Poción de Hipnosis";

                case ChestRewardType.PotionSleep:
                    if (GameManager.Instance != null) GameManager.Instance.AddSpellCharges(0, 0, rewardAmount);
                    LootPopupVisualizer.ShowPotionReward(transform.position, PotionType.Sleep, rewardAmount);
                    return $"+{rewardAmount} Poción de Sueño";

                case ChestRewardType.Heart:
                    if (GameManager.Instance != null) GameManager.Instance.GainHeart(rewardAmount);
                    LootPopupVisualizer.ShowHeartReward(transform.position, rewardAmount);
                    return $"+{rewardAmount} Corazón de Vida";

                case ChestRewardType.Key:
                    if (!string.IsNullOrEmpty(rewardKeyId) && KeyInventory.Instance != null)
                    {
                        KeyInventory.Instance.AddKey(rewardKeyId, rewardKeyDisplayName);
                    }
                    LootPopupVisualizer.ShowKeyReward(transform.position, rewardKeyDisplayName);
                    return rewardKeyDisplayName;

                default:
                    return $"+{bonusScore} Puntos de Tesoro";
            }
        }

        private string GetIngredientName(IngredientType type) => type switch
        {
            IngredientType.Mushroom => "Hongo Místico",
            IngredientType.Spider => "Araña de Cripta",
            IngredientType.WitheredFlower => "Flor Marchita",
            IngredientType.Root => "Raíz Antigua",
            _ => "Ingrediente"
        };

        private IEnumerator OpenAnimationRoutine()
        {
            if (chestRenderer == null) chestRenderer = GetComponent<SpriteRenderer>();
            if (chestRenderer == null) yield break;
            for (int i = 0; i < openAnimationFrames.Length; i++)
            {
                if (openAnimationFrames[i] != null) chestRenderer.sprite = openAnimationFrames[i];
                yield return new WaitForSeconds(openFrameDuration);
            }
            UpdateVisuals();
        }

        /// <summary>La llave (u otro premio) sale del cofre, sube girando con un destello y se desvanece.</summary>
        private IEnumerator RewardRevealRoutine()
        {
            GameObject reveal = new GameObject("Chest_RewardReveal");
            reveal.transform.SetParent(transform, false);
            reveal.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            SpriteRenderer sr = reveal.AddComponent<SpriteRenderer>();
            sr.sprite = rewardRevealSprite;
            SpriteRenderer chestSr = chestRenderer != null ? chestRenderer : GetComponent<SpriteRenderer>();
            sr.sortingLayerName = chestSr != null ? chestSr.sortingLayerName : "Gameplay";
            sr.sortingOrder = (chestSr != null ? chestSr.sortingOrder : 4) + 6;

            // Tamaño en mundo fijo (~0.7 u) aunque el cofre esté escalado
            float native = Mathf.Max(0.01f, Mathf.Max(rewardRevealSprite.bounds.size.x, rewardRevealSprite.bounds.size.y));
            float parentScale = Mathf.Max(0.01f, transform.lossyScale.y);
            float targetScale = 0.7f / native / parentScale;

            GameObject halo = new GameObject("Chest_RewardHalo");
            halo.transform.SetParent(reveal.transform, false);
            SpriteRenderer haloSr = halo.AddComponent<SpriteRenderer>();
            haloSr.sprite = LootPopupVisualizer.GetHaloSprite();
            haloSr.color = new Color(1f, 0.88f, 0.35f, 0.75f);
            haloSr.sortingLayerName = sr.sortingLayerName;
            haloSr.sortingOrder = sr.sortingOrder - 1;
            float haloNative = haloSr.sprite != null ? Mathf.Max(0.01f, haloSr.sprite.bounds.size.x) : 1f;
            halo.transform.localScale = Vector3.one * (1.6f * native / haloNative);

            const float rise = 0.75f;
            float t = 0f;
            while (t < rise)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / rise);
                float ease = 1f - (1f - k) * (1f - k);
                reveal.transform.localPosition = new Vector3(0f, 0.25f + ease * (1.3f / parentScale), 0f);
                float pop = targetScale * (0.4f + 0.6f * ease + Mathf.Sin(k * Mathf.PI) * 0.25f);
                reveal.transform.localScale = new Vector3(pop * Mathf.Cos(k * Mathf.PI * 2f), pop, 1f);
                yield return null;
            }
            reveal.transform.localScale = new Vector3(targetScale, targetScale, 1f);

            float hold = 0f;
            Vector3 basePos = reveal.transform.localPosition;
            while (hold < 0.9f)
            {
                hold += Time.deltaTime;
                reveal.transform.localPosition = basePos + new Vector3(0f, Mathf.Sin(hold * 6f) * 0.05f, 0f);
                haloSr.color = new Color(1f, 0.88f, 0.35f, 0.45f + Mathf.Sin(hold * 10f) * 0.3f);
                yield return null;
            }

            float fade = 0f;
            while (fade < 0.45f)
            {
                fade += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(fade / 0.45f);
                sr.color = new Color(1f, 1f, 1f, a);
                haloSr.color = new Color(1f, 0.88f, 0.35f, 0.6f * a);
                reveal.transform.localPosition += new Vector3(0f, Time.deltaTime * 0.8f, 0f);
                yield return null;
            }
            Destroy(reveal);
        }

        /// <summary>Un cofre abierto deja de brillar: así se distingue de un vistazo de los que faltan.</summary>
        private void DimIdleGlow()
        {
            PickupGlint glint = GetComponent<PickupGlint>();
            if (glint != null) glint.enabled = false;
            foreach (UnityEngine.Rendering.Universal.Light2D l in GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>())
            {
                if (l != null) l.intensity *= 0.3f;
            }
        }

        private void UpdateVisuals()
        {
            if (chestRenderer == null) chestRenderer = GetComponent<SpriteRenderer>();
            if (chestRenderer == null) return;

            if (isOpened && openSprite != null)
            {
                chestRenderer.sprite = openSprite;
            }
            else if (!isOpened && closedSprite != null)
            {
                chestRenderer.sprite = closedSprite;
            }
        }

        private void ShowMessage(string message)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(message, 2.8f);
            }
        }
    }
}
