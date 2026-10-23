using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Level2
{
    /// <summary>
    /// Pantalla de victoria del Nivel 2 (Acto 2 — Pueblo y Mercado).
    /// Muestra estadísticas de la partida, informa que el Acto 3 está en desarrollo,
    /// y ofrece opciones seguras de navegación (Continuar/Menú, Reintentar).
    /// </summary>
    public class Level2CompleteScreen : MonoBehaviour
    {
        [Header("Contenedor Principal")]
        [SerializeField] private GameObject rootPanel;

        [Header("Textos TMP")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI ingredientsStatText;
        [SerializeField] private TextMeshProUGUI timeStatText;
        [SerializeField] private TextMeshProUGUI scoreStatText;

        [Header("Botones")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        [Header("Configuración de Destino")]
        [Tooltip("Nombre de la escena a cargar al pulsar Continuar. Si no existe en Build Settings, carga MainMenu.")]
        [SerializeField] private string nextSceneName = "MainMenu";

        private void Awake()
        {
            if (rootPanel == null)
            {
                rootPanel = gameObject;
            }

            SetupButtons();

            // Ocultar por defecto si rootPanel es distinto del objeto con el script
            if (rootPanel != null && rootPanel != gameObject)
            {
                rootPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            SetupButtons();
        }

        private void OnDestroy()
        {
            // Garantizar que la escala de tiempo se restaure siempre al salir de la pantalla
            Time.timeScale = 1f;
            RemoveButtonListeners();
        }

        private void SetupButtons()
        {
            RemoveButtonListeners();

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void RemoveButtonListeners()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);

            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.RemoveListener(OnMenuClicked);
        }

        /// <summary>
        /// Despliega la pantalla de victoria con los datos calculados del Acto 2.
        /// </summary>
        public void Show(int totalIngredients, int requiredIngredients, float elapsedTime, int score)
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = "¡MERCADO DE SALEM SUPERADO!";
            }

            if (subtitleText != null)
            {
                subtitleText.text = "Has recolectado los ingredientes requeridos para el aquelarre colonial.\n<color=#D4AF37>(Acto 3: La Iglesia — Próximamente)</color>";
            }

            if (ingredientsStatText != null)
            {
                string bonusTag = totalIngredients > requiredIngredients ? " <color=#FFD700>(¡Bonus extra!)</color>" : "";
                ingredientsStatText.text = $"Ingredientes: {totalIngredients} / {requiredIngredients}{bonusTag}";
            }

            if (timeStatText != null)
            {
                int minutes = Mathf.FloorToInt(elapsedTime / 60f);
                int seconds = Mathf.FloorToInt(elapsedTime % 60f);
                timeStatText.text = $"Tiempo: {minutes:00}:{seconds:00}";
            }

            if (scoreStatText != null)
            {
                scoreStatText.text = $"Puntuación: {score} pts";
            }

            SetupButtons();
        }

        public void OnContinueClicked()
        {
            Time.timeScale = 1f;
            Debug.Log("[Level2CompleteScreen] Continuar pulsado. Volviendo a MainMenu (Acto 3 en desarrollo).");
            if (Application.isPlaying)
            {
                if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
                {
                    SceneManager.LoadScene(nextSceneName);
                }
                else
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }

        public void OnRetryClicked()
        {
            Time.timeScale = 1f;
            Debug.Log("[Level2CompleteScreen] Reintentando Nivel 2...");
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.ResetProgress();
            }
            if (Application.isPlaying)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void OnMenuClicked()
        {
            Time.timeScale = 1f;
            Debug.Log("[Level2CompleteScreen] Saliendo al Menú Principal.");
            if (Application.isPlaying)
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        // Asignación de referencias por inspector o generador
        public void SetReferences(GameObject panel, TextMeshProUGUI title, TextMeshProUGUI subtitle,
            TextMeshProUGUI ingr, TextMeshProUGUI time, TextMeshProUGUI score,
            Button btnContinue, Button btnRetry, Button btnMenu)
        {
            rootPanel = panel;
            titleText = title;
            subtitleText = subtitle;
            ingredientsStatText = ingr;
            timeStatText = time;
            scoreStatText = score;
            continueButton = btnContinue;
            retryButton = btnRetry;
            menuButton = btnMenu;
        }
    }
}
