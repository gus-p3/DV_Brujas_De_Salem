using UnityEngine;

/// <summary>
/// Zona de muerte al vacío situada bajo el nivel. Si la bruja cae en este trigger,
/// pierde un corazón por caída y reaparece en el último checkpoint activo.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class KillZone : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Daño infligido en corazones al caer al abismo.")]
    [SerializeField] private int damageAmount = 1;

    [Tooltip("Etiqueta del jugador.")]
    [SerializeField] private string playerTag = "Player";

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar si es la bruja
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null && !other.CompareTag(playerTag))
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            return;
        }

        // Restar corazón por caída
        bool heartLost = GameManager.Instance.LoseHeart(damageAmount, "Caída");

        // Si aún le quedan corazones, reaparecer en el último checkpoint
        if (GameManager.Instance.CurrentHearts > 0)
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RespawnAtCheckpoint();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.45f);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
        }
    }
}
