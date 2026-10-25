using System.Collections.Generic;
using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Cofre interactivo del siglo XVII para interiores, balcones, sótanos y áticos del Nivel 2.
    /// - Puede abrirse directamente o requerir una llave específica (KeyInventory).
    /// - Entrega recompensas reales integradas con GameManager (pociones, corazones, puntos)
    ///   e IngredientInventory.
    /// - Registra su apertura en KeyInventory para impedir recompensas duplicadas o infinitas.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TreasureChest : MonoBehaviour, IInteractable
    {
        [Header("Identificación y Bloqueo")]
        [SerializeField] private string chestId = "chest_default";
        [SerializeField] private string chestTitle = "Cofre de Roble";
        [SerializeField] private bool requiresKey = false;
        [SerializeField] private string requiredKeyId = "";
        [SerializeField] private string requiredKeyName = "Llave Antigua";

        [Header("Recompensas Reales")]
        [SerializeField] private int scoreReward = 250;
        [SerializeField] private bool grantPotion = true;
        [SerializeField] private PotionType potionRewardType = PotionType.Blind;
        [SerializeField] private int potionRewardCount = 1;
        [SerializeField] private bool grantHeart = false;
        [SerializeField] private string grantKeyId = "";
        [SerializeField] private string grantKeyDisplayName = "";

        [Header("Visuales")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;

        private bool isOpened;

        public string InteractionId => chestId;
        public int Priority => 18;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.05f;

        public string ChestId => chestId;
        public bool IsOpened => isOpened || (KeyInventory.Instance != null && KeyInventory.Instance.IsChestOpened(chestId));
        public bool RequiresKey => requiresKey;
        public string RequiredKeyId => requiredKeyId;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (KeyInventory.Instance != null && KeyInventory.Instance.IsChestOpened(chestId))
            {
                isOpened = true;
            }
            UpdateVisual();
        }

        public void Configure(
            string id,
            string title,
            int score,
            bool givePotion,
            PotionType potionType,
            bool giveHeart,
            Sprite closed,
            Sprite open,
            bool needsKey = false,
            string keyId = "",
            string keyName = "")
        {
            chestId = id;
            chestTitle = title;
            scoreReward = score;
            grantPotion = givePotion;
            potionRewardType = potionType;
            potionRewardCount = 1;
            grantHeart = giveHeart;
            closedSprite = closed;
            openSprite = open;
            requiresKey = needsKey;
            requiredKeyId = keyId;
            requiredKeyName = keyName;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            UpdateVisual();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null) return false;
            return !IsOpened;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (IsOpened) return string.Empty;

            if (requiresKey)
            {
                bool hasKey = KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId);
                if (hasKey)
                {
                    return $"Q: Abrir {chestTitle} (Usar {requiredKeyName})";
                }
                return $"Q: {chestTitle} (Requiere {requiredKeyName})";
            }

            return $"Q: Abrir {chestTitle}";
        }

        public void Interact(GameObject interactor)
        {
            if (IsOpened) return;

            TransformationManager tm = interactor != null ? interactor.GetComponent<TransformationManager>() : null;
            if (tm != null && tm.IsCatForm)
            {
                ShowBanner("Necesitas forma humana para abrir cofres");
                return;
            }

            if (requiresKey)
            {
                bool hasKey = KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId);
                if (!hasKey)
                {
                    ShowBanner($"Cofre cerrado con llave. Necesitas: {requiredKeyName}");
                    return;
                }
            }

            OpenAndGrantRewards();
        }

        private void OpenAndGrantRewards()
        {
            if (IsOpened) return;
            isOpened = true;

            if (KeyInventory.Instance != null)
            {
                KeyInventory.Instance.MarkChestOpened(chestId);
            }

            UpdateVisual();

            List<string> rewardDescriptions = new List<string>();

            if (GameManager.Instance != null)
            {
                if (scoreReward > 0)
                {
                    GameManager.Instance.AddScore(scoreReward);
                    rewardDescriptions.Add($"+{scoreReward} pts");
                }

                if (grantPotion && potionRewardCount > 0)
                {
                    int b = potionRewardType == PotionType.Blind ? potionRewardCount : 0;
                    int h = potionRewardType == PotionType.Hypnotize ? potionRewardCount : 0;
                    int s = potionRewardType == PotionType.Sleep ? potionRewardCount : 0;
                    GameManager.Instance.AddSpellCharges(b, h, s);
                    string pName = potionRewardType switch
                    {
                        PotionType.Blind => "Poción de Ceguera",
                        PotionType.Hypnotize => "Poción de Hipnosis",
                        PotionType.Sleep => "Poción de Sueño",
                        _ => "Poción"
                    };
                    rewardDescriptions.Add($"+{potionRewardCount} {pName}");
                }

                if (grantHeart)
                {
                    GameManager.Instance.GainHeart(1);
                    rewardDescriptions.Add("+1 Corazón");
                }
            }

            if (!string.IsNullOrEmpty(grantKeyId) && KeyInventory.Instance != null)
            {
                KeyInventory.Instance.AddKey(grantKeyId, grantKeyDisplayName);
                rewardDescriptions.Add(grantKeyDisplayName);
            }

            string summary = rewardDescriptions.Count > 0
                ? $"¡{chestTitle} abierto! ({string.Join(", ", rewardDescriptions)})"
                : $"¡{chestTitle} abierto!";

            ShowBanner(summary);
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return;
            spriteRenderer.sprite = IsOpened && openSprite != null ? openSprite : closedSprite;
        }

        private static void ShowBanner(string msg)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(msg, 2.2f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
        }
    }
}
