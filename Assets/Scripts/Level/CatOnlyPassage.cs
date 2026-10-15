using UnityEngine;

/// <summary>
/// Trigger informativo situado en la entrada y salida de pasajes estrechos (ej. tronco hueco).
/// No bloquea físicamente mediante código (el bloqueo lo ejerce la altura del collider de techo),
/// sino que advierte a la bruja si intenta cruzar en forma humana con un aviso cada 3 segundos.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CatOnlyPassage : MonoBehaviour
{
    [Header("Configuración de Aviso")]
    [Tooltip("Mensaje mostrado si la bruja intenta entrar en forma humana.")]
    [SerializeField] private string humanWarningMessage = "Eres muy grande para pasar";
    [Tooltip("Intervalo mínimo entre avisos consecutivos (s).")]
    [SerializeField] private float messageCooldown = 3.0f;

    private float lastMessageTime = -10f;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckHumanAttempt(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckHumanAttempt(other);
    }

    private void CheckHumanAttempt(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        TransformationManager tm = player.GetComponent<TransformationManager>();
        if (tm == null) tm = UnityEngine.Object.FindFirstObjectByType<TransformationManager>();

        // Si la bruja está en forma humana (no es gato)
        if (tm != null && !tm.IsCatForm)
        {
            if (Time.time >= lastMessageTime + messageCooldown)
            {
                lastMessageTime = Time.time;
                HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                if (hud != null)
                {
                    hud.ShowMessage(humanWarningMessage, 2.0f);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.5f);
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 0f));
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.55f, 0.1f, 1f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, "[Pasaje Solo Gato]");
#endif
    }
}
