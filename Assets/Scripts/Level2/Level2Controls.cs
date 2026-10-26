using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Level2
{
    /// <summary>
    /// Fuente única de verdad para el esquema de controles del Nivel 2 (Acto 2 — Pueblo y Mercado).
    /// - Q: Interactuar con puertas, casas, sótano, cofres, llaves y crónicas (entrar, salir, abrir).
    /// - 1 / 2 / 3: Seleccionar poción (Cegar / Hipnotizar / Dormir).
    /// - E (o clic izquierdo): Lanzar la poción seleccionada.
    /// - Garantiza que Q NUNCA lance pociones (suprimiendo PotionInventory en el frame exacto en que se pulsa Q)
    ///   y que E NUNCA abra puertas ni cofres, sin modificar ControlsMap.cs ni PotionInventory.cs.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class Level2Controls : MonoBehaviour
    {
        public const KeyCode InteractKey = KeyCode.Q;
        public const KeyCode ThrowPotionKey = KeyCode.E;
        public const KeyCode SelectBlindKey = KeyCode.Alpha1;
        public const KeyCode SelectHypnotizeKey = KeyCode.Alpha2;
        public const KeyCode SelectSleepKey = KeyCode.Alpha3;

        public const float DefaultInteractCooldown = 0.32f;

        public const string ControlsGuideBanner = "Q: interactuar · 1/2/3: elegir poción · E: lanzar";
        public const string PotionUsageText = "Elige poción con 1, 2 o 3 y lánzala con E";
        public const string CatChestBlockedMessage = "Necesitas forma humana para abrir esto";
        public const string CatPickupBlockedMessage = "Necesitas forma humana para recoger esto";

        private static Level2Controls _instance;
        public static Level2Controls Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<Level2Controls>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private static int lastConsumedFrame = -10;
        private static float lastConsumedTime = -10f;

        private PotionInventory cachedPotionInventory;
        private bool suppressedPotionForQThisFrame;

        public static bool IsInteractKey(KeyCode key) => key == InteractKey;
        public static bool IsPotionThrowKey(KeyCode key) => key == ThrowPotionKey;

        /// <summary>
        /// Comprueba si se pulsó Q en este frame y consume la entrada para que ningún otro
        /// interactuable (ej. entrar y salir de una puerta en el mismo frame) pueda dispararse dos veces.
        /// </summary>
        public static bool TryConsumeInteractDown(float cooldown = DefaultInteractCooldown)
        {
            if (Time.frameCount == lastConsumedFrame)
            {
                return false;
            }

            if (Time.unscaledTime < lastConsumedTime + cooldown)
            {
                return false;
            }

            if (!Input.GetKeyDown(InteractKey))
            {
                return false;
            }

            ConsumeInteractNow();
            return true;
        }

        /// <summary>
        /// Marca la entrada Q como consumida en el frame y tiempo actual.
        /// </summary>
        public static void ConsumeInteractNow()
        {
            lastConsumedFrame = Time.frameCount;
            lastConsumedTime = Time.unscaledTime;
        }

        /// <summary>
        /// Reinicia los contadores de enfriamiento (usado por suites de verificación en Editor).
        /// </summary>
        // Sin recarga de dominio los estáticos sobreviven entre partidas y el reloj vuelve a 0: bloqueaba Q
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetCooldownForTests()
        {
            lastConsumedFrame = -10;
            lastConsumedTime = -10f;
            _instance = null;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            BindPotionInventory();
        }

        private void Start()
        {
            BindPotionInventory();
            EnsureHUDControlsGuide();
        }

        private void OnDisable()
        {
            RestorePotionInventoryAfterQ();
        }

        private void OnDestroy()
        {
            RestorePotionInventoryAfterQ();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void BindPotionInventory()
        {
            if (cachedPotionInventory == null)
            {
                cachedPotionInventory = GetComponent<PotionInventory>();
                if (cachedPotionInventory == null)
                {
                    cachedPotionInventory = Object.FindFirstObjectByType<PotionInventory>();
                }
            }
        }

        private void Update()
        {
            BindPotionInventory();

            // Si en este frame se pulsa Q (y NO se pulsó E ni clic izquierdo),
            // desactivamos PotionInventory durante el ciclo Update() para que la línea compartida
            // `Input.GetKeyDown(KeyCode.Q)` en PotionInventory.cs nunca lance una poción por accidente en el Nivel 2.
            bool qPressed = Input.GetKeyDown(InteractKey) || Input.GetKey(InteractKey);
            bool eOrClickPressed = Input.GetKeyDown(ThrowPotionKey) || Input.GetMouseButtonDown(0);

            if (qPressed && !eOrClickPressed && cachedPotionInventory != null && cachedPotionInventory.enabled)
            {
                ForwardNumberSelectionKeys(cachedPotionInventory);
                cachedPotionInventory.enabled = false;
                suppressedPotionForQThisFrame = true;
            }
        }

        private void LateUpdate()
        {
            RestorePotionInventoryAfterQ();
        }

        private void RestorePotionInventoryAfterQ()
        {
            if (suppressedPotionForQThisFrame)
            {
                suppressedPotionForQThisFrame = false;
                if (cachedPotionInventory != null)
                {
                    cachedPotionInventory.enabled = true;
                }
            }
        }

        private static void ForwardNumberSelectionKeys(PotionInventory inv)
        {
            if (inv == null) return;
            if (Input.GetKeyDown(SelectBlindKey) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                inv.SelectPotion(PotionType.Blind);
            }
            else if (Input.GetKeyDown(SelectHypnotizeKey) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                inv.SelectPotion(PotionType.Hypnotize);
            }
            else if (Input.GetKeyDown(SelectSleepKey) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                inv.SelectPotion(PotionType.Sleep);
            }
        }

        /// <summary>
        /// Crea o actualiza la guía discreta de controles del Nivel 2 bajo la barra de ingredientes en el HUD.
        /// </summary>
        public static void EnsureHUDControlsGuide()
        {
            GameObject canvasGO = GameObject.Find("UI/IngredientHUD_Canvas");
            if (canvasGO == null) return;

            Transform existing = canvasGO.transform.Find("Panel_ControlsGuide");
            GameObject guidePanel = existing != null ? existing.gameObject : new GameObject("Panel_ControlsGuide");
            guidePanel.transform.SetParent(canvasGO.transform, false);

            RectTransform rt = guidePanel.GetComponent<RectTransform>();
            if (rt == null) rt = guidePanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-25f, -74f);
            rt.sizeDelta = new Vector2(420f, 28f);

            Image bg = guidePanel.GetComponent<Image>();
            if (bg == null) bg = guidePanel.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.07f, 0.05f, 0.82f);
            bg.raycastTarget = false;

            Transform textTr = guidePanel.transform.Find("ControlsGuideText");
            GameObject textGO = textTr != null ? textTr.gameObject : new GameObject("ControlsGuideText");
            textGO.transform.SetParent(guidePanel.transform, false);

            RectTransform textRt = textGO.GetComponent<RectTransform>();
            if (textRt == null) textRt = textGO.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 2f);
            textRt.offsetMax = new Vector2(-8f, -2f);

            TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = ControlsGuideBanner;
            tmp.fontSize = 14f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.98f, 0.90f, 0.68f, 0.95f);
            tmp.raycastTarget = false;
        }
    }
}
