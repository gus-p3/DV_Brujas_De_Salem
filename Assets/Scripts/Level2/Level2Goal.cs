using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Level2
{
    /// <summary>
    /// Meta de salida del Nivel 2 (Acto 2 — Pueblo y Mercado).
    /// Controla la condición de victoria final del nivel:
    /// 1. Requiere que el jugador alcance el punto de salida habiendo reunido los ingredientes requeridos.
    /// 2. Evita dobles activaciones mediante flag 'levelCompleted'.
    /// 3. Detiene el movimiento del jugador y calcula estadísticas finales.
    /// 4. Notifica a SaveSystem y IngredientInventory.CommitAct2Completion().
    /// 5. Detiene el bucle de juego y despliega Level2CompleteScreen.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Level2Goal : MonoBehaviour
    {
        [Header("Configuración de Meta")]
        [Tooltip("Mensaje de advertencia si intenta salir sin los ingredientes requeridos.")]
        [SerializeField] private string missingIngredientsMessage = "Aún te faltan ingredientes para el aquelarre.";

        [Tooltip("Enfriamiento entre avisos (s).")]
        [SerializeField] private float messageCooldown = 2.0f;

        [Header("Audio")]
        [SerializeField] private AudioClip victorySound;

        [Header("Referencias")]
        [SerializeField] private Level2CompleteScreen completeScreen;

        private Collider2D triggerCollider;
        private bool levelCompleted = false;
        private float cooldownTimer = 0f;
        private float levelStartTime = 0f;

        public bool IsCompleted => levelCompleted;
        public void ResetVictoryState() => levelCompleted = false;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
            levelStartTime = Time.time;
        }

        private void Start()
        {
            if (completeScreen == null)
            {
                completeScreen = UnityEngine.Object.FindFirstObjectByType<Level2CompleteScreen>(FindObjectsInactive.Include);
            }
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActiveAndEnabled || levelCompleted) return;

            PlayerController pc = other.GetComponentInParent<PlayerController>();
            if (pc == null) return;

            // Comprobar si los ingredientes requeridos están completos
            bool canComplete = IngredientInventory.Instance != null && IngredientInventory.Instance.AreAllRequiredCollected;

            if (canComplete)
            {
                TriggerLevelVictory(pc);
            }
            else
            {
                NotifyMissingIngredients();
            }
        }

        /// <summary>
        /// Ejecuta la secuencia atómica de victoria del Nivel 2.
        /// </summary>
        public void TriggerLevelVictory(PlayerController player = null)
        {
            if (levelCompleted) return;
            levelCompleted = true;

            // 1. Deshabilitar físicas de movimiento del jugador
            if (player != null)
            {
                player.ResetPhysics(Vector2.zero);
            }

            // 2. Calcular puntuación y estadísticas
            float elapsedTime = Time.time - levelStartTime;
            int totalIngredients = IngredientInventory.Instance != null ? IngredientInventory.Instance.TotalCollected : 8;
            int requiredIngredients = IngredientInventory.Instance != null ? IngredientInventory.Instance.TotalRequired : 8;
            int heartsRemaining = GameManager.Instance != null ? GameManager.Instance.CurrentHearts : 3;

            // Cálculo de puntuación base:
            // 200 pts por ingrediente requerido (1600 pts)
            // 500 pts por cada ingrediente bonus extra (hasta 1000 pts)
            // 300 pts por cada corazón restante (hasta 900 pts)
            // Bonus de tiempo (si termina en menos de 300s)
            int score = (requiredIngredients * 200);
            int bonusIngredients = Mathf.Max(0, totalIngredients - requiredIngredients);
            score += bonusIngredients * 500;
            score += heartsRemaining * 300;
            if (elapsedTime < 300f)
            {
                score += Mathf.RoundToInt((300f - elapsedTime) * 5f);
            }

            // Si GameManager existe, sumar al puntaje global
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(score);
            }

            // 3. Registrar persistencia oficial del Acto 2
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.CommitAct2Completion();
            }

            // 4. Guardar progreso con SaveSystem para desbloquear Acto 3
            SaveSystem.SaveLevelProgress(2, score);
            PlayerPrefs.SetInt("L2_Completed", 1);
            PlayerPrefs.SetFloat("L2_ElapsedTime", elapsedTime);
            PlayerPrefs.SetInt("L2_FinalScore", score);
            PlayerPrefs.Save();

            // 5. Reproducir sonido de victoria si está disponible
            if (victorySound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(victorySound);
            }
            else if (AudioManager.Instance != null && AudioManager.Instance.checkpointClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.checkpointClip);
            }

            // 6. Mostrar pantalla de nivel completado
            if (completeScreen != null)
            {
                completeScreen.Show(totalIngredients, requiredIngredients, elapsedTime, score);
            }
            else
            {
                Debug.Log($"[Level2Goal] ¡Victoria Nivel 2! Score: {score}, Tiempo: {elapsedTime:F1}s");
            }

            // 7. Pausar juego de forma segura
            Time.timeScale = 0f;
        }

        private void NotifyMissingIngredients()
        {
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = messageCooldown;
                HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                if (hud != null)
                {
                    hud.ShowMessage(missingIngredientsMessage, 2.5f);
                }
                else
                {
                    Debug.LogWarning($"[Level2Goal] {missingIngredientsMessage}");
                }
            }
        }

        public void SetCompleteScreen(Level2CompleteScreen screen)
        {
            completeScreen = screen;
        }
    }
}
