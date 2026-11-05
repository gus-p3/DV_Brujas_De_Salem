using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Level2
{
    public enum ChestItemKind
    {
        Ingredient = 0,
        Potion = 1,
        Heart = 2,
        Key = 3
    }

    [Serializable]
    public class ChestContentEntry
    {
        public ChestItemKind itemKind = ChestItemKind.Ingredient;
        public IngredientType ingredientType = IngredientType.Mushroom;
        public string ingredientUniqueId = "";
        public PotionType potionType = PotionType.Blind;
        public int amount = 1;
        public string keyId = "";
        public string keyDisplayName = "";
        public bool claimed = false;
    }

    /// <summary>
    /// Cofre interactivo propio del Nivel 2 (Acto 2 — Pueblo y Mercado).
    /// - Se abre exclusivamente con la tecla Q (Level2Controls.InteractKey) en forma humana.
    /// - En forma de gato muestra "Necesitas forma humana para abrir esto" con enfriamiento anti-spam y no se abre.
    /// - Contiene ingredientes (IngredientInventory), frascos de poción (GameManager.AddSpellCharges con tope 3),
    ///   corazones (GameManager.GainHeart si la vida no está llena) y llaves de exploración (KeyInventory).
    /// - Al abrirse, reproduce sonido de cofre y muestra la animación emergente de cada objeto obtenido
    ///   (LootPopupVisualizer: icono real en el mundo + tarjeta ilustrada en el HUD).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class Level2Chest : MonoBehaviour, IInteractable
    {
        public const int MaxPotionChargesPerType = 3;

        [Header("Identificación y Estado")]
        [SerializeField] private string chestId = "chest_z1_intro";
        [SerializeField] private string chestTitle = "Cofre del Mercado";
        [SerializeField] private int scoreReward = 250;
        [SerializeField] private bool requiresKey = false;
        [SerializeField] private string requiredKeyId = "";
        [SerializeField] private string requiredKeyName = "Llave Antigua";

        [Header("Contenido del Cofre")]
        [SerializeField] private List<ChestContentEntry> contents = new List<ChestContentEntry>();

        [Header("Visuales")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [Tooltip("Cuadros de la tapa al abrirse (opcional); el último debería ser openSprite.")]
        [SerializeField] private Sprite[] openFrames = new Sprite[0];
        [SerializeField] private float openFrameTime = 0.06f;

        [Header("Enfriamiento Aviso Gato")]
        [SerializeField] private float catWarningCooldown = 2.0f;

        private bool hasBeenOpened;
        private float lastCatWarningTime = -10f;
        private Vector3 baseLocalScale = Vector3.one;
        private Coroutine lidPopCoroutine;

        public event Action<Level2Chest> OnOpened;

        public string InteractionId => chestId;
        public int Priority => 19;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.15f;

        public string ChestId => chestId;
        public string ChestTitle => chestTitle;
        public bool RequiresKey => requiresKey;
        public string RequiredKeyId => requiredKeyId;
        public IReadOnlyList<ChestContentEntry> Contents => contents;

        public bool HasBeenOpened => hasBeenOpened;
        public bool HasPendingItems
        {
            get
            {
                if (contents == null || contents.Count == 0) return false;
                for (int i = 0; i < contents.Count; i++)
                {
                    if (contents[i] != null && !contents[i].claimed && contents[i].amount > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsFullyLooted => hasBeenOpened && !HasPendingItems;

        private string PrefKeyState => $"L2_Chest_{chestId}";
        private string PrefKeyMask => $"L2_Chest_{chestId}_Mask";

        private void Awake()
        {
            baseLocalScale = transform.localScale;
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.OnInventoryReset -= HandleInventoryReset;
                IngredientInventory.Instance.OnInventoryReset += HandleInventoryReset;
            }
        }

        private void Start()
        {
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.OnInventoryReset -= HandleInventoryReset;
                IngredientInventory.Instance.OnInventoryReset += HandleInventoryReset;
            }

            LoadPersistedState();
            UpdateVisual();
        }

        private void OnDisable()
        {
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.OnInventoryReset -= HandleInventoryReset;
            }
        }

        private void HandleInventoryReset()
        {
            ResetChestState();
        }

        public void ConfigureChest(
            string id,
            string title,
            Sprite closed,
            Sprite open,
            List<ChestContentEntry> entries,
            bool needsKey = false,
            string keyId = "",
            string keyName = "")
        {
            chestId = id;
            chestTitle = title;
            closedSprite = closed;
            openSprite = open;
            requiresKey = needsKey;
            requiredKeyId = keyId;
            requiredKeyName = keyName;
            contents = entries ?? new List<ChestContentEntry>();
            hasBeenOpened = false;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            UpdateVisual();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null) return false;
            return !IsFullyLooted;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (IsFullyLooted) return string.Empty;

            if (!hasBeenOpened && requiresKey)
            {
                bool hasKey = KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId);
                if (hasKey)
                {
                    return $"Q: Abrir cofre ({requiredKeyName})";
                }
                return $"Q: Cofre cerrado (Requiere {requiredKeyName})";
            }

            if (hasBeenOpened)
            {
                return string.Empty;
            }

            return "Q: Abrir cofre";
        }

        public void Interact(GameObject interactor)
        {
            if (IsFullyLooted) return;

            TransformationManager tm = interactor != null ? interactor.GetComponent<TransformationManager>() : null;
            if (tm == null)
            {
                tm = UnityEngine.Object.FindFirstObjectByType<TransformationManager>();
            }

            // GDD §3.2: En forma de gato no se puede abrir ningún cofre
            if (tm != null && tm.IsCatForm)
            {
                if (Time.unscaledTime - lastCatWarningTime >= catWarningCooldown)
                {
                    lastCatWarningTime = Time.unscaledTime;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayErrorSound();
                    ShowBanner(Level2Controls.CatChestBlockedMessage);
                }
                return;
            }

            if (!hasBeenOpened && requiresKey)
            {
                bool hasKey = KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId);
                if (!hasKey)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayLockedSound();
                    ShowBanner($"Cofre cerrado con llave. Necesitas: {requiredKeyName}");
                    return;
                }
            }

            OpenAndDeliverContents();
        }

        private void OpenAndDeliverContents()
        {
            bool wasClosed = !hasBeenOpened;
            hasBeenOpened = true;

            List<string> obtainedLabels = new List<string>();
            List<string> pendingReasons = new List<string>();
            int popupOrder = 0;

            if (contents != null)
            {
                for (int i = 0; i < contents.Count; i++)
                {
                    ChestContentEntry entry = contents[i];
                    if (entry == null || entry.claimed || entry.amount <= 0) continue;

                    switch (entry.itemKind)
                    {
                        case ChestItemKind.Ingredient:
                            DeliverIngredientEntry(entry, i, obtainedLabels, ref popupOrder);
                            break;

                        case ChestItemKind.Potion:
                            DeliverPotionEntry(entry, obtainedLabels, pendingReasons, ref popupOrder);
                            break;

                        case ChestItemKind.Heart:
                            DeliverHeartEntry(entry, obtainedLabels, pendingReasons, ref popupOrder);
                            break;

                        case ChestItemKind.Key:
                            DeliverKeyEntry(entry, obtainedLabels, ref popupOrder);
                            break;
                    }
                }
            }

            if (IsFullyLooted && KeyInventory.Instance != null)
            {
                KeyInventory.Instance.MarkChestOpened(chestId);
            }

            SavePersistedState();
            UpdateVisual();

            if (wasClosed)
            {
                if (GameManager.Instance != null && scoreReward > 0)
                {
                    GameManager.Instance.AddScore(scoreReward);
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayChestOpenSound();
                }
                PlayLidPopAnimation();
                OnOpened?.Invoke(this);
            }

            if (IngredientHUD.Instance != null)
            {
                IngredientHUD.Instance.RefreshAllDisplays();
            }

            string finalMessage;
            if (obtainedLabels.Count > 0 && pendingReasons.Count > 0)
            {
                finalMessage = $"Obtuviste: {string.Join(", ", obtainedLabels)} ({string.Join(", ", pendingReasons)})";
            }
            else if (obtainedLabels.Count > 0)
            {
                finalMessage = $"Obtuviste: {string.Join(", ", obtainedLabels)}";
            }
            else if (pendingReasons.Count > 0)
            {
                finalMessage = string.Join(" · ", pendingReasons);
            }
            else
            {
                finalMessage = "Cofre abierto";
            }

            ShowBanner(finalMessage);
        }

        private Vector3 GetPopupPosition(int popupOrder)
        {
            float xOffset = popupOrder == 0 ? 0f : ((popupOrder % 2 == 1 ? 1f : -1f) * 0.48f * ((popupOrder + 1) / 2));
            float yOffset = 0.25f + popupOrder * 0.36f;
            return transform.position + new Vector3(xOffset, yOffset, 0f);
        }

        private void DeliverIngredientEntry(ChestContentEntry entry, int index, List<string> obtainedLabels, ref int popupOrder)
        {
            int count = Mathf.Max(1, entry.amount);
            if (IngredientInventory.Instance != null)
            {
                for (int u = 0; u < count; u++)
                {
                    string baseId = string.IsNullOrEmpty(entry.ingredientUniqueId)
                        ? $"ing_{chestId}_{index}"
                        : entry.ingredientUniqueId;
                    string uniqueId = count > 1 ? $"{baseId}_{u}" : baseId;
                    IngredientInventory.Instance.Collect(entry.ingredientType, uniqueId);
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(150 * count);
            }

            entry.claimed = true;
            string ingName = GetIngredientSpanishName(entry.ingredientType);
            obtainedLabels.Add($"{ingName} x{count}");

            if (Application.isPlaying)
            {
                LootPopupVisualizer.ShowIngredientReward(GetPopupPosition(popupOrder), entry.ingredientType, count);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayIngredientPickupSound();
            }
            popupOrder++;
        }

        private void DeliverPotionEntry(ChestContentEntry entry, List<string> obtainedLabels, List<string> pendingReasons, ref int popupOrder)
        {
            if (GameManager.Instance == null)
            {
                entry.claimed = true;
                obtainedLabels.Add($"{GetPotionSpanishName(entry.potionType)} x{entry.amount}");
                if (Application.isPlaying)
                {
                    LootPopupVisualizer.ShowPotionReward(GetPopupPosition(popupOrder), entry.potionType, entry.amount);
                }
                popupOrder++;
                return;
            }

            int currentCharges = GetCurrentCharges(entry.potionType);
            int room = Mathf.Max(0, MaxPotionChargesPerType - currentCharges);
            int toGrant = Mathf.Min(entry.amount, room);
            int overflow = entry.amount - toGrant;

            if (toGrant > 0)
            {
                int b = entry.potionType == PotionType.Blind ? toGrant : 0;
                int h = entry.potionType == PotionType.Hypnotize ? toGrant : 0;
                int s = entry.potionType == PotionType.Sleep ? toGrant : 0;
                GameManager.Instance.AddSpellCharges(b, h, s);
            }

            if (overflow > 0)
            {
                int bonus = 150 * overflow;
                GameManager.Instance.AddScore(bonus);
                if (toGrant > 0)
                {
                    obtainedLabels.Add($"{GetPotionSpanishName(entry.potionType)} x{toGrant} (+{bonus} pts)");
                }
                else
                {
                    obtainedLabels.Add($"{GetPotionSpanishName(entry.potionType)} (+{bonus} pts)");
                }
            }
            else
            {
                obtainedLabels.Add($"{GetPotionSpanishName(entry.potionType)} x{toGrant}");
            }

            if (Application.isPlaying)
            {
                LootPopupVisualizer.ShowPotionReward(GetPopupPosition(popupOrder), entry.potionType, entry.amount);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPotionPickupSound();
            }
            popupOrder++;
            entry.claimed = true;
        }

        private void DeliverHeartEntry(ChestContentEntry entry, List<string> obtainedLabels, List<string> pendingReasons, ref int popupOrder)
        {
            if (GameManager.Instance == null || !Application.isPlaying)
            {
                entry.claimed = true;
                obtainedLabels.Add($"Corazón x{entry.amount}");
                if (Application.isPlaying)
                {
                    LootPopupVisualizer.ShowHeartReward(GetPopupPosition(popupOrder), entry.amount);
                }
                popupOrder++;
                return;
            }

            int currentHearts = GameManager.Instance.CurrentHearts;
            int maxHearts = GameManager.Instance.MaxHearts;
            int missingHearts = Mathf.Max(0, maxHearts - currentHearts);
            int toHeal = Mathf.Min(entry.amount, missingHearts);
            int overflow = entry.amount - toHeal;

            for (int h = 0; h < toHeal; h++)
            {
                GameManager.Instance.GainHeart(1);
            }

            if (overflow > 0)
            {
                int bonus = 150 * overflow;
                GameManager.Instance.AddScore(bonus);
                if (toHeal > 0)
                {
                    obtainedLabels.Add($"Corazón x{toHeal} (+{bonus} pts)");
                }
                else
                {
                    obtainedLabels.Add($"Corazón (+{bonus} pts)");
                }
            }
            else
            {
                obtainedLabels.Add($"Corazón x{toHeal}");
            }

            if (Application.isPlaying)
            {
                LootPopupVisualizer.ShowHeartReward(GetPopupPosition(popupOrder), entry.amount);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPotionPickupSound();
            }
            popupOrder++;
            entry.claimed = true;
        }

        private void DeliverKeyEntry(ChestContentEntry entry, List<string> obtainedLabels, ref int popupOrder)
        {
            string display = string.IsNullOrEmpty(entry.keyDisplayName) ? "Llave Antigua" : entry.keyDisplayName;
            if (KeyInventory.Instance != null && !string.IsNullOrEmpty(entry.keyId))
            {
                KeyInventory.Instance.AddKey(entry.keyId, display);
            }
            entry.claimed = true;
            obtainedLabels.Add($"{display} x1");

            if (Application.isPlaying)
            {
                LootPopupVisualizer.ShowKeyReward(GetPopupPosition(popupOrder), display);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayKeyPickupSound();
            }
            popupOrder++;
        }

        private static int GetCurrentCharges(PotionType type)
        {
            if (GameManager.Instance == null) return 0;
            return type switch
            {
                PotionType.Blind => GameManager.Instance.BlindCharges,
                PotionType.Hypnotize => GameManager.Instance.HypnotizeCharges,
                PotionType.Sleep => GameManager.Instance.SleepCharges,
                _ => 0
            };
        }

        public static string GetIngredientSpanishName(IngredientType type)
        {
            return type switch
            {
                IngredientType.Mushroom => "Hongo",
                IngredientType.Spider => "Araña",
                IngredientType.WitheredFlower => "Flor marchita",
                IngredientType.Root => "Raíz",
                _ => "Ingrediente"
            };
        }

        public static string GetPotionSpanishName(PotionType type)
        {
            return type switch
            {
                PotionType.Blind => "Poción de Cegar",
                PotionType.Hypnotize => "Poción de Hipnotizar",
                PotionType.Sleep => "Poción de Dormir",
                _ => "Poción"
            };
        }

        public void SavePersistedState()
        {
            int stateVal = !hasBeenOpened ? 0 : (IsFullyLooted ? 2 : 1);
            PlayerPrefs.SetInt(PrefKeyState, stateVal);

            int mask = 0;
            if (contents != null)
            {
                for (int i = 0; i < contents.Count && i < 30; i++)
                {
                    if (contents[i] != null && contents[i].claimed)
                    {
                        mask |= (1 << i);
                    }
                }
            }
            PlayerPrefs.SetInt(PrefKeyMask, mask);
            PlayerPrefs.Save();
        }

        public void LoadPersistedState()
        {
            if (PlayerPrefs.GetInt("L2_Session_Active", 0) != 1)
            {
                return;
            }

            int stateVal = PlayerPrefs.GetInt(PrefKeyState, 0);
            int mask = PlayerPrefs.GetInt(PrefKeyMask, 0);

            hasBeenOpened = stateVal > 0;
            if (contents != null)
            {
                for (int i = 0; i < contents.Count && i < 30; i++)
                {
                    if (contents[i] != null)
                    {
                        contents[i].claimed = stateVal == 2 || ((mask & (1 << i)) != 0);
                    }
                }
            }
        }

        public void ResetChestState()
        {
            hasBeenOpened = false;
            if (contents != null)
            {
                for (int i = 0; i < contents.Count; i++)
                {
                    if (contents[i] != null)
                    {
                        contents[i].claimed = false;
                        if (contents[i].amount <= 0) contents[i].amount = 1;
                    }
                }
            }

            PlayerPrefs.DeleteKey(PrefKeyState);
            PlayerPrefs.DeleteKey(PrefKeyMask);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (spriteRenderer == null) return;

            spriteRenderer.sprite = hasBeenOpened && openSprite != null ? openSprite : closedSprite;
        }

        private void PlayLidPopAnimation()
        {
            if (!gameObject.activeInHierarchy || !Application.isPlaying) return;
            if (lidPopCoroutine != null)
            {
                StopCoroutine(lidPopCoroutine);
            }
            lidPopCoroutine = StartCoroutine(LidPopRoutine());
        }

        private IEnumerator LidPopRoutine()
        {
            if (openFrames != null && openFrames.Length > 0 && spriteRenderer != null)
            {
                foreach (Sprite frame in openFrames)
                {
                    if (frame != null) spriteRenderer.sprite = frame;
                    yield return new WaitForSecondsRealtime(openFrameTime);
                }
                UpdateVisual();
            }

            float duration = 0.24f;
            float elapsed = 0f;
            Vector3 peakScale = new Vector3(baseLocalScale.x * 1.18f, baseLocalScale.y * 1.22f, baseLocalScale.z);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curve = Mathf.Sin(t * Mathf.PI);
                transform.localScale = Vector3.Lerp(baseLocalScale, peakScale, curve);
                yield return null;
            }

            transform.localScale = baseLocalScale;
            lidPopCoroutine = null;
        }

        private static void ShowBanner(string message)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(message, 2.4f);
            }
            if (IngredientHUD.Instance != null)
            {
                IngredientHUD.Instance.ShowWarning(message, 2.4f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
        }
    }
}
