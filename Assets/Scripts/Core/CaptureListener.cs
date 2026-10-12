using UnityEngine;

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
                enemy.OnTargetCaptured -= HandleTargetCaptured;
                enemy.OnTargetCaptured += HandleTargetCaptured;
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
                enemy.OnTargetCaptured -= HandleTargetCaptured;
            }
        }
    }

    private void HandleTargetCaptured()
    {
        Debug.Log("[CaptureListener] ¡Objetivo capturado por el farol! Disparando TriggerGameOver en GameManager.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}
