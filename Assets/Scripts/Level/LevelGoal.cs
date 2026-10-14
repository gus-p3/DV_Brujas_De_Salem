using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Meta del nivel (portón de salida).
/// Verifica si la bruja ha reunido las 3 partes de la escoba (BroomInventory.IsComplete).
/// Si es así, completa el nivel en GameManager y guarda el progreso mediante SaveSystem.
/// Si aún faltan partes, muestra un mensaje de advertencia con enfriamiento de 2 segundos.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelGoal : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Enfriamiento entre avisos si la escoba no está completa (s).")]
    [SerializeField] private float messageCooldown = 2.0f;

    private Collider2D triggerCollider;
    private bool levelCompleted = false;
    private float cooldownTimer = 0f;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
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
        if (levelCompleted) return;

        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        bool isBroomReady = BroomInventory.Instance != null && BroomInventory.Instance.IsComplete;

        if (isBroomReady)
        {
            levelCompleted = true;

            int currentScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;
            int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Guardar progreso con SaveSystem
            SaveSystem.SaveLevelProgress(activeSceneIndex > 0 ? activeSceneIndex : 1, currentScore);
            PlayerPrefs.SetInt("UnlockedLevel", Mathf.Max(PlayerPrefs.GetInt("UnlockedLevel", 1), 2));
            PlayerPrefs.SetInt("HighScore_Level1", Mathf.Max(PlayerPrefs.GetInt("HighScore_Level1", 0), currentScore));
            PlayerPrefs.Save();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
            else
            {
                Debug.Log($"[LevelGoal] Nivel completado. Puntaje: {currentScore}");
            }
        }
        else
        {
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = messageCooldown;
                HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                if (hud != null)
                {
                    hud.ShowMessage("Aún te falta material para la escoba", 2.0f);
                }
            }
        }
    }
}
