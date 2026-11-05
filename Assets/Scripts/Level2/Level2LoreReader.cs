using TMPro;
using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Lector de crónicas del Nivel 2: diarios, grimorios y actas se leen sobre una hoja de pergamino quemado
    /// con el juego en pausa (Time.timeScale = 0), en lugar de un banner que se perdía sobre el fondo.
    /// Se cierra con la tecla de interactuar (Q en el Nivel 2, E en el Nivel 3), Espacio, Enter, Esc o clic;
    /// al cerrar restaura la escala de tiempo y consume la tecla para que no se reabra el mismo objeto.
    /// </summary>
    public class Level2LoreReader : MonoBehaviour
    {
        public static Level2LoreReader Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;

        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;

        private bool open;
        private float previousTimeScale = 1f;
        private int openedFrame;

        public string CurrentTitle => titleText != null ? titleText.text : string.Empty;

        public void SetupReferences(GameObject panelRoot, TextMeshProUGUI title, TextMeshProUGUI body)
        {
            panel = panelRoot;
            titleText = title;
            bodyText = body;
        }

        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (open) Time.timeScale = previousTimeScale;
            Instance = null;
        }

        public void Show(string title, string body)
        {
            if (panel == null) return;
            titleText.text = title;
            bodyText.text = body;
            panel.SetActive(true);
            if (!open)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            open = true;
            openedFrame = Time.frameCount;
        }

        // Se cierra en LateUpdate: así, el frame en que se pulsa Espacio todos los scripts del jugador
        // ya leyeron la entrada con el juego en pausa y la bruja no salta al cerrar la hoja.
        private void LateUpdate()
        {
            // La misma tecla que abrió la hoja no debe cerrarla
            if (!open || Time.frameCount == openedFrame) return;
            if (Input.GetKeyDown(Level2Controls.InteractKey) || Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                Close();
            }
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            panel.SetActive(false);
            // Si mientras se leía se pausó y reanudó el juego, no se restaura una escala de tiempo vieja
            bool stillPlaying = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;
            Time.timeScale = stillPlaying ? (previousTimeScale > 0f ? previousTimeScale : 1f) : 0f;
            Level2Controls.ConsumeInteractNow();

            // Espacio, E o el clic con que se cerró no deben convertirse en salto ni en lanzamiento
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) player.ClearBufferedInput();
            PotionInventory potions = Object.FindFirstObjectByType<PotionInventory>();
            if (potions != null) potions.BlockThrowForFrames(2);
        }
    }
}
