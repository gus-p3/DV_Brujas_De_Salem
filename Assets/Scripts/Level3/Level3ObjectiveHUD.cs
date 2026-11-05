using System.Collections.Generic;
using Level2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level3
{
    /// <summary>
    /// Panel de objetivos del Nivel 3 (esquina superior derecha, bajo el contador de escoba).
    /// Sigue el estilo visual de IngredientHUD / Level2ChestGuideHUD (fondo madera oscura, texto pergamino).
    /// Muestra en tiempo real:
    /// - Acto y objetivo activo (portones abiertos, pistas leídas, cofres abiertos).
    /// - Chips de estado de las llaves conocidas del Nivel 3 (KeyInventory.Instance).
    /// - Estado de la Poción Maestra y de la Llave de la Iglesia.
    /// Vive en su propio Canvas (Level3ObjectiveHUD_Canvas); no modifica HUDController.
    /// </summary>
    [DisallowMultipleComponent]
    public class Level3ObjectiveHUD : MonoBehaviour
    {
        public const string PanelName = "Panel_Level3Objective";

        /// <summary>Llaves conocidas del Nivel 3 (id, etiqueta corta del chip).</summary>
        public static readonly KeyValuePair<string, string>[] TrackedKeys =
        {
            new KeyValuePair<string, string>(Level3StateController.ForestGateKey1Id, "Portón I"),
            new KeyValuePair<string, string>(Level3StateController.ForestGateKey2Id, "Portón II"),
            new KeyValuePair<string, string>("crypt_chest_key", "Cripta"),
            new KeyValuePair<string, string>("sanctuary_gate_key", "Atrio"),
            new KeyValuePair<string, string>("inquisitor_chest_key", "Inquisidor"),
            new KeyValuePair<string, string>(Level3StateController.ChurchMasterKeyId, "Iglesia"),
        };

        private static readonly Color PanelColor = new Color(0.10f, 0.07f, 0.05f, 0.86f);
        private static readonly Color BorderColor = new Color(0.83f, 0.69f, 0.22f, 0.85f);
        private static readonly Color ParchmentColor = new Color(0.98f, 0.90f, 0.68f, 0.95f);
        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.29f, 1f);
        private static readonly Color ChipOffColor = new Color(0.22f, 0.18f, 0.14f, 0.92f);
        private static readonly Color ChipOnColor = new Color(0.78f, 0.60f, 0.12f, 0.95f);

        [SerializeField] private float refreshInterval = 0.4f;
        [SerializeField] private float pulseDuration = 0.6f;

        private TextMeshProUGUI headerText;
        private TextMeshProUGUI objectiveText;
        private TextMeshProUGUI statusText;
        private readonly List<Image> keyChipImages = new List<Image>();
        private readonly List<TextMeshProUGUI> keyChipLabels = new List<TextMeshProUGUI>();
        private RectTransform panelRect;

        private LockedGateInteractable[] forestGates = new LockedGateInteractable[0];
        private TreasureChest[] chests = new TreasureChest[0];
        private Level3StateController boundState;
        private KeyInventory boundKeys;

        private float nextRefreshTime;
        private float pulseTimer;
        private string lastObjective = string.Empty;

        public string HeaderLine => headerText != null ? headerText.text : string.Empty;
        public string ObjectiveLine => objectiveText != null ? objectiveText.text : string.Empty;
        public string StatusLine => statusText != null ? statusText.text : string.Empty;
        public int ForestGatesOpened { get; private set; }
        public int ChestsOpened { get; private set; }
        public int TotalChests => chests.Length;

        private void Awake()
        {
            EnsureUIHierarchy();
        }

        private void Start()
        {
            forestGates = System.Array.FindAll(
                Object.FindObjectsByType<LockedGateInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                g => g != null && (g.GateId == "forest_gate_1" || g.GateId == "forest_gate_2"));
            chests = Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (LockedGateInteractable gate in forestGates) gate.OnGateUnlocked += HandleGateUnlocked;
            foreach (TreasureChest chest in chests) chest.OnChestOpened += HandleChestOpened;

            BindSingletons();
            Refresh();
        }

        private void OnDestroy()
        {
            foreach (LockedGateInteractable gate in forestGates)
            {
                if (gate != null) gate.OnGateUnlocked -= HandleGateUnlocked;
            }
            foreach (TreasureChest chest in chests)
            {
                if (chest != null) chest.OnChestOpened -= HandleChestOpened;
            }
            UnbindSingletons();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + refreshInterval;
                if (boundState == null || boundKeys == null) BindSingletons();
                Refresh();
            }

            AnimatePulse();
        }

        private void BindSingletons()
        {
            if (boundState == null && Level3StateController.Instance != null)
            {
                boundState = Level3StateController.Instance;
                boundState.OnPhaseChanged += HandlePhaseChanged;
                boundState.OnMasterPotionBrewed += Refresh;
                boundState.OnObjectiveUpdated += HandleObjectiveUpdated;
            }

            if (boundKeys == null && KeyInventory.Instance != null)
            {
                boundKeys = KeyInventory.Instance;
                boundKeys.OnKeyCollected += HandleKeyCollected;
            }
        }

        private void UnbindSingletons()
        {
            if (boundState != null)
            {
                boundState.OnPhaseChanged -= HandlePhaseChanged;
                boundState.OnMasterPotionBrewed -= Refresh;
                boundState.OnObjectiveUpdated -= HandleObjectiveUpdated;
                boundState = null;
            }

            if (boundKeys != null)
            {
                boundKeys.OnKeyCollected -= HandleKeyCollected;
                boundKeys = null;
            }
        }

        private void HandlePhaseChanged(Level3Phase phase) => Refresh();
        private void HandleObjectiveUpdated(string objective) => Refresh();
        private void HandleKeyCollected(string keyId) => Refresh();
        private void HandleGateUnlocked(LockedGateInteractable gate) => Refresh();
        private void HandleChestOpened(TreasureChest chest) => Refresh();

        /// <summary>Recalcula y pinta todas las líneas del panel.</summary>
        public void Refresh()
        {
            EnsureUIHierarchy();

            Level3StateController state = Level3StateController.Instance;
            KeyInventory keys = KeyInventory.Instance;
            Level3Phase phase = state != null ? state.CurrentPhase : Level3Phase.Phase1_EscapeToForestCabins;
            bool hasPotion = state != null && state.HasBrewedMasterPotion;
            bool hasChurchKey = keys != null && keys.HasKey(Level3StateController.ChurchMasterKeyId);

            ForestGatesOpened = 0;
            foreach (LockedGateInteractable gate in forestGates)
            {
                if (gate != null && (gate.IsUnlocked || (keys != null && keys.IsDoorUnlocked(gate.GateId)))) ForestGatesOpened++;
            }

            ChestsOpened = 0;
            foreach (TreasureChest chest in chests)
            {
                if (chest != null && (chest.IsOpened || (keys != null && keys.IsChestOpened(chest.ChestId)))) ChestsOpened++;
            }

            int clues = state != null ? state.CluesReadCount : 0;
            int totalClues = state != null ? state.TotalExpectedClues : 3;
            int totalChests = Mathf.Max(3, chests.Length);

            string header;
            string objective;
            bool villagePart = state != null && state.IsVillagePart;
            switch (phase)
            {
                case Level3Phase.Phase2_BrewMasterPotion:
                    header = "NIVEL 3  ·  PARTE 1 — La Cabaña de la Bruja";
                    objective = "Objetivo: Prepara la Poción Maestra en tu Caldero [E]";
                    break;
                case Level3Phase.Phase3_ReturnToVillageAndChurch when !villagePart:
                    header = "NIVEL 3  ·  PARTE 1 — El Sendero de Regreso";
                    objective = "Objetivo: Cruza el Portón del Sendero de Regreso hacia la Aldea de Salem";
                    break;
                case Level3Phase.Phase3_ReturnToVillageAndChurch:
                    header = "NIVEL 3  ·  PARTE 2 — La Aldea de Salem";
                    objective = $"Objetivo: Regresa a la Aldea, lee las pistas ({Mathf.Min(clues, totalClues)}/{totalClues}), abre los cofres ({Mathf.Min(ChestsOpened, totalChests)}/{totalChests}) y entra a la Iglesia";
                    break;
                default:
                    header = "NIVEL 3  ·  PARTE 1 — El Sendero del Bosque";
                    objective = $"Objetivo: Cruza el bosque hasta tu Cabaña (Portones: {Mathf.Min(ForestGatesOpened, 2)}/2) — Duerme a los guardias brillantes [Q / clic]";
                    break;
            }

            if (headerText != null) headerText.text = header;
            if (objectiveText != null) objectiveText.text = objective;

            for (int i = 0; i < TrackedKeys.Length && i < keyChipImages.Count; i++)
            {
                bool owned = keys != null && keys.HasKey(TrackedKeys[i].Key);
                // En la Parte 2 los portones del bosque ya quedaron atrás (se abrieron en la escena anterior)
                if (villagePart && (TrackedKeys[i].Key == Level3StateController.ForestGateKey1Id ||
                                    TrackedKeys[i].Key == Level3StateController.ForestGateKey2Id)) owned = true;
                keyChipImages[i].color = owned ? ChipOnColor : ChipOffColor;
                keyChipLabels[i].color = owned ? new Color(1f, 0.98f, 0.86f, 1f) : new Color(0.62f, 0.57f, 0.50f, 0.95f);
                keyChipLabels[i].fontStyle = owned ? FontStyles.Bold : FontStyles.Normal;
            }

            if (statusText != null)
            {
                string potionTag = hasPotion ? "<color=#7CFF9E>LISTA</color>" : "<color=#8E8578>Pendiente</color>";
                string keyTag = hasChurchKey ? "<color=#FFD94A>OBTENIDA</color>" : "<color=#8E8578>Pendiente</color>";
                statusText.text = $"Poción Maestra: [{potionTag}]     Llave de la Iglesia: [{keyTag}]";
            }

            if (objective != lastObjective)
            {
                if (!string.IsNullOrEmpty(lastObjective)) pulseTimer = pulseDuration;
                lastObjective = objective;
            }
        }

        private void AnimatePulse()
        {
            if (panelRect == null) return;
            if (pulseTimer > 0f)
            {
                pulseTimer -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(pulseTimer / Mathf.Max(0.01f, pulseDuration));
                panelRect.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(k * Mathf.PI));
            }
            else if (panelRect.localScale != Vector3.one)
            {
                panelRect.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Construye (o reutiliza) la jerarquía UI del panel. Idempotente; usable desde el Editor.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Transform existing = transform.Find(PanelName);
            GameObject panel = existing != null ? existing.gameObject : new GameObject(PanelName, typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-25f, -186f);
            panelRect.sizeDelta = new Vector2(720f, 190f);

            Image border = panel.GetComponent<Image>();
            if (border == null) border = panel.AddComponent<Image>();
            border.color = BorderColor;
            border.raycastTarget = false;

            GameObject inner = GetOrCreateChild(panel.transform, "Inner");
            RectTransform innerRt = (RectTransform)inner.transform;
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2f, 2f);
            innerRt.offsetMax = new Vector2(-2f, -2f);
            Image innerBg = inner.GetComponent<Image>();
            if (innerBg == null) innerBg = inner.AddComponent<Image>();
            innerBg.color = PanelColor;
            innerBg.raycastTarget = false;

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            headerText = MakeText(inner.transform, "HeaderText", "NIVEL 3  ·  ACTO 1 — El Sendero del Bosque",
                new Vector2(0f, 0.80f), new Vector2(1f, 1f), 22f, GoldColor, FontStyles.Bold);
            objectiveText = MakeText(inner.transform, "ObjectiveText",
                "Objetivo: Cruza el bosque hasta tu Cabaña (Portones: 0/2) — Duerme a los guardias brillantes [Q / clic]",
                new Vector2(0f, 0.44f), new Vector2(1f, 0.80f), 19f, ParchmentColor, FontStyles.Normal);
            objectiveText.textWrappingMode = TextWrappingModes.Normal;

            GameObject row = GetOrCreateChild(inner.transform, "KeyChipsRow");
            RectTransform rowRt = (RectTransform)row.transform;
            rowRt.anchorMin = new Vector2(0f, 0.22f);
            rowRt.anchorMax = new Vector2(1f, 0.44f);
            rowRt.offsetMin = new Vector2(10f, 2f);
            rowRt.offsetMax = new Vector2(-10f, -2f);

            TextMeshProUGUI rowLabel = MakeText(row.transform, "KeysLabel", "Llaves:",
                new Vector2(0f, 0f), new Vector2(0.12f, 1f), 17f, ParchmentColor, FontStyles.Bold);
            rowLabel.alignment = TextAlignmentOptions.MidlineLeft;
            // Sin márgenes ni ajuste de línea: antes "Llaves:" no cabía y se partía en dos renglones.
            ((RectTransform)rowLabel.transform).offsetMin = Vector2.zero;
            ((RectTransform)rowLabel.transform).offsetMax = Vector2.zero;
            rowLabel.textWrappingMode = TextWrappingModes.NoWrap;

            keyChipImages.Clear();
            keyChipLabels.Clear();
            float startX = 0.125f;
            float chipW = (1f - startX) / TrackedKeys.Length;
            for (int i = 0; i < TrackedKeys.Length; i++)
            {
                GameObject chip = GetOrCreateChild(row.transform, "KeyChip_" + TrackedKeys[i].Key);
                RectTransform crt = (RectTransform)chip.transform;
                crt.anchorMin = new Vector2(startX + chipW * i, 0f);
                crt.anchorMax = new Vector2(startX + chipW * (i + 1), 1f);
                crt.offsetMin = new Vector2(2f, 0f);
                crt.offsetMax = new Vector2(-2f, 0f);
                Image chipImg = chip.GetComponent<Image>();
                if (chipImg == null) chipImg = chip.AddComponent<Image>();
                chipImg.color = ChipOffColor;
                chipImg.raycastTarget = false;

                TextMeshProUGUI chipLabel = MakeText(chip.transform, "Label", TrackedKeys[i].Value,
                    Vector2.zero, Vector2.one, 15f, new Color(0.62f, 0.57f, 0.50f, 0.95f), FontStyles.Normal);
                ((RectTransform)chipLabel.transform).offsetMin = new Vector2(3f, 0f);
                ((RectTransform)chipLabel.transform).offsetMax = new Vector2(-3f, 0f);
                // Los nombres largos ("Inquisidor") se recortaban: se reducen solos para caber en el chip.
                chipLabel.textWrappingMode = TextWrappingModes.NoWrap;
                chipLabel.enableAutoSizing = true;
                chipLabel.fontSizeMin = 10f;
                chipLabel.fontSizeMax = 15f;
                keyChipImages.Add(chipImg);
                keyChipLabels.Add(chipLabel);
            }

            statusText = MakeText(inner.transform, "StatusText",
                "Poción Maestra: [Pendiente]     Llave de la Iglesia: [Pendiente]",
                new Vector2(0f, 0f), new Vector2(1f, 0.22f), 18f, ParchmentColor, FontStyles.Normal);
            statusText.richText = true;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform tr = parent.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, string text, Vector2 aMin, Vector2 aMax,
            float fontSize, Color color, FontStyles style)
        {
            GameObject go = GetOrCreateChild(parent, name);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = new Vector2(10f, 1f);
            rt.offsetMax = new Vector2(-10f, -1f);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.richText = true;
            return tmp;
        }
    }
}
