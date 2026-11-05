using System;
using System.Collections;
using Level2;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Level3
{
    /// <summary>
    /// Secuencia y pantalla visual de finalización del Nivel 3 (Level3_Village).
    /// Presenta la imagen artística de la Catedral gótica bajo la luna como telón de fondo,
    /// el mensaje de "NIVEL 3 COMPLETADO", el texto narrativo del cierre hacia el Nivel 4,
    /// y gestiona el bloqueo de control del jugador con una transición suave.
    /// </summary>
    [DisallowMultipleComponent]
    public class Level3VillageEndingCutscene : MonoBehaviour
    {
        private static Level3VillageEndingCutscene _instance;
        public static Level3VillageEndingCutscene Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<Level3VillageEndingCutscene>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Elementos de Canvas")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private Image cathedralBackground;

        [Header("Textos")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI narrativeText;
        [SerializeField] private TextMeshProUGUI loreNoteText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI noticePromptText;

        [Header("Botones")]
        [SerializeField] private Button continueBtn;
        [SerializeField] private Button menuBtn;
        [SerializeField] private Button retryBtn;

        [Header("Configuración Narrativa")]
        [SerializeField] private string completionTitle = "NIVEL 3 COMPLETADO";
        [TextArea(2, 4)]
        [SerializeField] private string narrativeMessage = "La aldea ha quedado atrás. Una antigua catedral espera bajo la luz de la luna...";
        [TextArea(2, 3)]
        [SerializeField] private string loreNoteMessage = "El misterio de la Poción Maestra aguarda en el interior del templo sagrado (Nivel 4).";

        [Header("Transición")]
        [SerializeField] private float fadeDuration = 1.1f;
        [SerializeField] private float contentFadeDuration = 0.85f;

        private bool isTriggered = false;
        private Coroutine runningRoutine;

        public bool IsCompleted => isTriggered;

        private void Awake()
        {
            Instance = this;
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.blocksRaycasts = false;
                rootCanvasGroup.interactable = false;
            }

            SetupButtons();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public void SetupButtons()
        {
            if (continueBtn != null)
            {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(OnContinueClicked);
            }
            if (menuBtn != null)
            {
                menuBtn.onClick.RemoveAllListeners();
                menuBtn.onClick.AddListener(OnMenuClicked);
            }
            if (retryBtn != null)
            {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(OnRetryClicked);
            }
        }

        /// <summary>
        /// Dispara la secuencia final de cierre del Nivel 3. Se ejecuta de forma segura una sola vez.
        /// </summary>
        public void TriggerEndingSequence()
        {
            if (isTriggered) return;
            isTriggered = true;

            Debug.Log("[Level3VillageEndingCutscene] ¡Iniciando secuencia de finalización del Nivel 3!");

            // 1. Bloquear inmediatamente control del jugador y combate
            LockPlayerControls();

            // 2. Ocultar prompts y mensajes flotantes del HUD
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud != null) hud.ClearMessage();
            if (InteractionPromptUI.Instance != null) InteractionPromptUI.Instance.HidePrompt();

            // 3. Audio de logro
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGateUnlockSound();
                AudioManager.Instance.PlayChestOpenSound();
            }

            // 4. Marcar nivel completado en GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }

            // Suprimir el panel genérico de victoria del HUD para dar protagonismo exclusivo a la Catedral
            if (hud != null && hud.levelCompletePanel != null)
            {
                hud.levelCompletePanel.gameObject.SetActive(false);
            }

            // 5. Iniciar corrutina de transición visual
            if (runningRoutine != null) StopCoroutine(runningRoutine);
            runningRoutine = StartCoroutine(EndingSequenceRoutine());
        }

        private void LockPlayerControls()
        {
            PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                pc.EnableScriptedInput(true);
                pc.enabled = false;
                Rigidbody2D rb = pc.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = true;
                }
            }

            TransformationManager tm = UnityEngine.Object.FindFirstObjectByType<TransformationManager>(FindObjectsInactive.Include);
            if (tm != null) tm.enabled = false;

            PotionInventory pi = UnityEngine.Object.FindFirstObjectByType<PotionInventory>(FindObjectsInactive.Include);
            if (pi != null) pi.enabled = false;
        }

        private IEnumerator EndingSequenceRoutine()
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.gameObject.SetActive(true);
                rootCanvasGroup.blocksRaycasts = true;
                rootCanvasGroup.interactable = true;
            }

            // Actualizar textos
            if (titleText != null) titleText.text = completionTitle;
            if (narrativeText != null) narrativeText.text = narrativeMessage;
            if (loreNoteText != null) loreNoteText.text = loreNoteMessage;

            if (statsText != null && GameManager.Instance != null)
            {
                float t = GameManager.Instance.TimeElapsed;
                int mins = Mathf.FloorToInt(t / 60f);
                int secs = Mathf.FloorToInt(t % 60f);
                statsText.text = $"Puntaje: {GameManager.Instance.Score}   ·   Tiempo: {mins:00}:{secs:00}";
            }

            if (noticePromptText != null)
            {
                noticePromptText.text = string.Empty;
            }

            // Paso A: Fundido a oscuro
            float elapsed = 0f;
            if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                if (rootCanvasGroup != null)
                {
                    rootCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);
                }
                yield return null;
            }

            if (rootCanvasGroup != null) rootCanvasGroup.alpha = 1f;

            // Breve pausa dramática
            yield return new WaitForSecondsRealtime(0.25f);

            // Paso B: Aparición del contenido narrativo y fondo de catedral
            elapsed = 0f;
            while (elapsed < contentFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / contentFadeDuration);
                if (contentCanvasGroup != null)
                {
                    contentCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);
                }
                yield return null;
            }

            if (contentCanvasGroup != null) contentCanvasGroup.alpha = 1f;
        }

        public void OnContinueClicked()
        {
            Time.timeScale = 1f;
            Debug.Log("[Level3VillageEndingCutscene] Botón Continuar pulsado.");

            // Si existe la escena de Nivel 4 en Build Settings, cargarla
            if (Application.CanStreamedLevelBeLoaded("Level4"))
            {
                SceneManager.LoadScene("Level4");
                return;
            }

            // Si aún no está desarrollada la escena de Nivel 4, informar amablemente y ofrecer volver al menú
            if (noticePromptText != null)
            {
                noticePromptText.text = "<color=#FFD700>¡Próximamente: Nivel 4 — La Iglesia de Salem!</color>\nVolviendo al Menú Principal...";
            }

            StartCoroutine(DelayedReturnToMenuRoutine(1.8f));
        }

        private IEnumerator DelayedReturnToMenuRoutine(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        public void OnMenuClicked()
        {
            Time.timeScale = 1f;
            Debug.Log("[Level3VillageEndingCutscene] Volviendo al Menú Principal.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        public void OnRetryClicked()
        {
            Time.timeScale = 1f;
            Debug.Log("[Level3VillageEndingCutscene] Reintentando nivel actual.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void SetReferences(
            CanvasGroup rootGroup,
            CanvasGroup contentGroup,
            Image overlay,
            Image cathedralImg,
            TextMeshProUGUI title,
            TextMeshProUGUI narrative,
            TextMeshProUGUI loreNote,
            TextMeshProUGUI stats,
            TextMeshProUGUI notice,
            Button btnContinue,
            Button btnMenu,
            Button btnRetry)
        {
            rootCanvasGroup = rootGroup;
            contentCanvasGroup = contentGroup;
            fadeOverlay = overlay;
            cathedralBackground = cathedralImg;
            titleText = title;
            narrativeText = narrative;
            loreNoteText = loreNote;
            statsText = stats;
            noticePromptText = notice;
            continueBtn = btnContinue;
            menuBtn = btnMenu;
            retryBtn = btnRetry;
            SetupButtons();
        }
    }
}
