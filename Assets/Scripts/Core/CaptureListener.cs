using UnityEngine;

public class CaptureListener : MonoBehaviour
{
    private void Start()
    {
        EnemySight[] enemies = FindObjectsByType<EnemySight>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.OnTargetCaptured += HandleTargetCaptured;
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

    private void HandleTargetCaptured(Transform target)
    {
        if (target.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}
