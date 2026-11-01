using System;
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
                    return $"Q: Abrir {chestDisplayName} ({requiredKeyDisplayName})";
                }
                return $"Q: {chestDisplayName} cerrado (Requiere {requiredKeyDisplayName})";
            }

            return $"Q: Abrir {chestDisplayName}";
        }

        public void Interact(GameObject interactor)
        {
            if (isOpened || interactor == null) return;

            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            if (tm != null && tm.IsCatForm)
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

            UpdateVisuals();
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
