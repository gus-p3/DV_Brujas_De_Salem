using UnityEngine;

/// <summary>
/// Trigger de aviso colocado bajo repisas altas y brechas de vuelo.
/// Muestra "Necesitas volar" si la escoba no está completa y "Pulsa {Volar}" si ya lo está,
/// utilizando ControlsMap para resolver las teclas dinámicamente.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FlightOnlyHint : MonoBehaviour
{
    [Header("Mensajes Contextuales")]
    [Tooltip("Mensaje mostrado si el jugador aún no reúne la escoba completa.")]
    [SerializeField] private string noFlightMessage = "Necesitas volar";
    [Tooltip("Mensaje mostrado si el jugador ya puede volar.")]
    [SerializeField] private string canFlyMessage = "Pulsa {Volar}";
    [Tooltip("Tiempo de enfriamiento entre avisos sucesivos (s).")]
    [SerializeField] private float messageCooldown = 3.0f;

    private float lastMessageTime = -10f;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckFlightHint(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckFlightHint(other);
    }

    private void CheckFlightHint(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (Time.time < lastMessageTime + messageCooldown) return;

        bool hasFlight = player.CanFly;
        BroomInventory broom = UnityEngine.Object.FindFirstObjectByType<BroomInventory>();
        if (broom != null && broom.IsComplete)
        {
            hasFlight = true;
        }

        string rawText = hasFlight ? canFlyMessage : noFlightMessage;
        string resolvedText = ControlsMap.Resolve(rawText);

        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            lastMessageTime = Time.time;
            hud.ShowMessage(resolvedText, 2.0f);
        }
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.45f);
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(2f, 3f, 0f));
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(0.2f, 0.7f, 1f, 1f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "[Zona Solo Vuelo]");
#endif
    }
}
