using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level2
{
    /// <summary>
    /// Guía del HUD propio del Nivel 2 que recuerda que los objetos están dentro de los cofres
    /// y muestra en tiempo real tanto los cofres abiertos como el estado de las llaves recolectadas.
    /// - Bajo la guía de controles muestra "Ingredientes: búscalos en los cofres (Q para abrir)",
    ///   "Cofres abiertos: X/Y" y "Llaves: Posada [✓/✗] | Mansión [✓/✗]".
    /// - Si la bruja pulsa Q junto a un cofre ya vaciado, avisa "Este cofre ya está vacío".
    /// Vive en IngredientHUD_Canvas; no toca HUDController ni el HUD principal.
    /// </summary>
    public class Level2ChestGuideHUD : MonoBehaviour
    {
        public const string GuideText = "Ingredientes: búscalos en los cofres (Q para abrir)";
        public const string EmptyChestMessage = "Este cofre ya está vacío";

        [SerializeField] private float dimmedAlpha = 0.72f;
        [SerializeField] private float emptyChestRadius = 1.3f;
        [SerializeField] private float emptyChestCooldown = 1.5f;

        private Level2Chest[] chests = new Level2Chest[0];
        private CanvasGroup panelGroup;
        private TextMeshProUGUI counterText;
        private TextMeshProUGUI keysText;
        private Transform player;
        private PlayerInteractor interactor;
        private float lastEmptyNotice = -10f;

        public int OpenedCount { get; private set; }
        public int TotalCount => chests.Length;
        public string CounterLine => counterText != null ? counterText.text : string.Empty;

        private void Start()
        {
            BuildPanel();
            chests = Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Level2Chest chest in chests) chest.OnOpened += HandleChestOpened;
            if (IngredientInventory.Instance != null) IngredientInventory.Instance.OnInventoryReset += Refresh;
            if (KeyInventory.Instance != null) KeyInventory.Instance.OnKeyCollected += HandleKeyCollected;
            // Los cofres recuperan su estado persistido en su propio Start: se cuenta un frame después
            StartCoroutine(RefreshNextFrame());
        }

        private void OnDestroy()
        {
            foreach (Level2Chest chest in chests)
            {
                if (chest != null) chest.OnOpened -= HandleChestOpened;
            }
            if (IngredientInventory.Instance != null) IngredientInventory.Instance.OnInventoryReset -= Refresh;
            if (KeyInventory.Instance != null) KeyInventory.Instance.OnKeyCollected -= HandleKeyCollected;
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            Refresh();
        }

        private void HandleChestOpened(Level2Chest chest) => Refresh();
        private void HandleKeyCollected(string keyId) => Refresh();

        public void Refresh()
        {
            int opened = 0;
            foreach (Level2Chest chest in chests)
            {
                if (chest != null && chest.HasBeenOpened) opened++;
            }
            OpenedCount = opened;
            if (counterText != null) counterText.text = $"Cofres abiertos: {opened}/{chests.Length}";

            if (keysText != null)
            {
                bool hasTavern = KeyInventory.Instance != null && KeyInventory.Instance.HasKey("key_tavern_chest");
                bool hasManor = KeyInventory.Instance != null && KeyInventory.Instance.HasKey("key_manor_master");
                string tColor = hasTavern ? "#FFD94A" : "#8E8578";
                string mColor = hasManor ? "#FFD94A" : "#8E8578";
                string tMark = hasTavern ? "OBTENIDA" : "Sótano Posada";
                string mMark = hasManor ? "OBTENIDA" : "Cripta Mansión";
                keysText.text = $"Llaves de cofre: <color={tColor}>Posada [{tMark}]</color>  |  <color={mColor}>Mansión [{mMark}]</color>";
            }

            if (panelGroup != null) panelGroup.alpha = opened > 0 ? dimmedAlpha : 1f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(Level2Controls.InteractKey) && !Level2LoreReader.IsOpen) TryNotifyEmptyChest();
        }

        /// <summary>Lo que hace Q junto a un cofre vaciado. Devuelve true si mostró el aviso.</summary>
        public bool TryNotifyEmptyChest()
        {
            if (Time.unscaledTime - lastEmptyNotice < emptyChestCooldown) return false;
            if (player == null)
            {
                PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
                if (pc == null) return false;
                player = pc.transform;
                interactor = pc.GetComponent<PlayerInteractor>();
            }
            // Si hay otra interacción enfocada (puerta, escalera, otro cofre), Q le pertenece a ella
            if (interactor != null && interactor.CurrentFocus != null) return false;

            foreach (Level2Chest chest in chests)
            {
                if (chest == null || !chest.IsFullyLooted) continue;
                if (Vector2.Distance(chest.transform.position, player.position) > emptyChestRadius) continue;
                lastEmptyNotice = Time.unscaledTime;
                ShowMessage(EmptyChestMessage);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayErrorSound();
                return true;
            }
            return false;
        }

        private static void ShowMessage(string message)
        {
            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null) hud.ShowMessage(message, 2.0f);
            if (IngredientHUD.Instance != null) IngredientHUD.Instance.ShowWarning(message, 2.0f);
        }

        /// <summary>Panel bajo la guía de controles de Level2Controls (esquina superior derecha).</summary>
        private void BuildPanel()
        {
            Transform existing = transform.Find("Panel_ChestGuide");
            GameObject panel = existing != null ? existing.gameObject : new GameObject("Panel_ChestGuide", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            RectTransform rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-25f, -106f);
            rt.sizeDelta = new Vector2(436f, 68f);

            Image bg = panel.GetComponent<Image>();
            if (bg == null) bg = panel.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.07f, 0.05f, 0.86f);
            bg.raycastTarget = false;

            panelGroup = panel.GetComponent<CanvasGroup>();
            if (panelGroup == null) panelGroup = panel.AddComponent<CanvasGroup>();
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;

            MakeLine(panel.transform, "GuideText", GuideText, 0.66f, 1f, 13.5f);
            counterText = MakeLine(panel.transform, "CounterText", "Cofres abiertos: 0/0", 0.33f, 0.66f, 14f);
            keysText = MakeLine(panel.transform, "KeysText", "Llaves: Posada [Pendiente]  |  Mansión [Pendiente]", 0f, 0.33f, 13f);
        }

        private static TextMeshProUGUI MakeLine(Transform parent, string name, string text, float yMin, float yMax, float fontSize)
        {
            Transform tr = parent.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, yMin);
            rt.anchorMax = new Vector2(1f, yMax);
            rt.offsetMin = new Vector2(8f, 1f);
            rt.offsetMax = new Vector2(-8f, -1f);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.98f, 0.90f, 0.68f, 0.95f);
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
