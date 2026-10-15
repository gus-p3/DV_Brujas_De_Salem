using UnityEngine;

/// <summary>
/// Escucha los eventos de captura de los guardias (EnemySight).
/// Aplica la deducción de corazones, reaparece a la bruja en el checkpoint,
/// reinicia al guardia captor y activa la invulnerabilidad temporal.
/// </summary>
public class CaptureListener : MonoBehaviour
{
    private void Awake()
    {
        ConnectEnemies();
    }

    private void Start()
    {
        ConnectEnemies();
    }

    public void ConnectEnemies()
    {
        EnemySight[] enemies = FindObjectsByType<EnemySight>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.OnTargetCapturedByGuard -= HandleGuardCaptured;
                enemy.OnTargetCapturedByGuard += HandleGuardCaptured;
            }
        }
    }

    private void OnDestroy()
    {
        EnemySight[] enemies = FindObjectsByType<EnemySight>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.OnTargetCapturedByGuard -= HandleGuardCaptured;
            }
        }
    }

    private void HandleGuardCaptured(EnemySight capturingGuard)
    {
        Debug.Log($"[CaptureListener] Bruja capturada por el guardia: {(capturingGuard != null ? capturingGuard.name : "Desconocido")}");

        if (GameManager.Instance == null)
        {
            return;
        }

        // 1. Deducir un corazón por captura de guardia
        bool damageApplied = GameManager.Instance.LoseHeart(1, "Guardia");
        if (!damageApplied)
        {
            // Ignorado por invulnerabilidad u otro estado
            return;
        }

        // 2. Si aún quedan corazones, reaparecer en checkpoint y restablecer guardia
        if (GameManager.Instance.CurrentHearts > 0)
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RespawnAtCheckpoint();
            }

            if (capturingGuard != null)
            {
                capturingGuard.ResetState();

                PatrolRoute patrol = capturingGuard.GetComponent<PatrolRoute>();
                if (patrol != null)
                {
                    patrol.ResetToFirstWaypoint();
                }
            }
        }
    }
}
