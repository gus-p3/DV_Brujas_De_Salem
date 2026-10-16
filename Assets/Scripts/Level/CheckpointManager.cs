using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor de puntos de control (Checkpoints) y reaparición en el nivel.
/// Almacena la última posición segura de guardado y los coleccionables recogidos
/// para prevenir duplicaciones o inconsistencias tras reaparecer.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Estado de Checkpoint")]
    [Tooltip("Posición del último punto de control alcanzado.")]
    [SerializeField] private Vector3 lastCheckpointPosition;

    [Tooltip("Identificadores de partes de escoba y objetos ya asegurados en este checkpoint.")]
    private HashSet<string> collectedItemIds = new HashSet<string>();

    private PlayerController player;
    private TransformationManager transformationManager;
    private PlayerInvulnerability playerInvulnerability;
    private CameraFollow cameraFollow;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        FindPlayerReferences();
    }

    private void Start()
    {
        FindPlayerReferences();

        // Al iniciar Level1, el checkpoint por defecto es la posición inicial de la bruja
        if (player != null)
        {
            lastCheckpointPosition = player.transform.position;
            Debug.Log($"[CheckpointManager] Checkpoint inicial configurado en: {lastCheckpointPosition}");
        }
    }

    private void FindPlayerReferences()
    {
        if (player == null)
        {
            player = Object.FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            if (transformationManager == null)
            {
                transformationManager = player.GetComponent<TransformationManager>();
            }

            if (playerInvulnerability == null)
            {
                playerInvulnerability = player.GetComponent<PlayerInvulnerability>();
                if (playerInvulnerability == null)
                {
                    playerInvulnerability = player.gameObject.AddComponent<PlayerInvulnerability>();
                }
            }
        }

        if (cameraFollow == null)
        {
            cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
        }
    }

    /// <summary>
    /// Actualiza la posición del último checkpoint activo.
    /// </summary>
    public void SetCheckpoint(Vector3 newPosition)
    {
        lastCheckpointPosition = newPosition;
        Debug.Log($"[CheckpointManager] Nuevo checkpoint registrado en: {lastCheckpointPosition}");
    }

    /// <summary>
    /// Reaparece a la bruja en el último checkpoint registrado con forma humana,
    /// velocidad cero, vuelo cancelado, cámara centrada e invulnerabilidad activa.
    /// </summary>
    public void RespawnAtCheckpoint()
    {
        FindPlayerReferences();

        if (player == null)
        {
            Debug.LogWarning("[CheckpointManager] No se encontró PlayerController para reaparecer.");
            return;
        }

        // 1. Reiniciar velocidad física y referencia de caída antes de mover transform.position
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        player.ResetFallReference();
        player.ResetPhysics(Vector2.zero);

        // 2. Colocar a la bruja en la última posición segura y sincronizar físicas
        player.transform.position = lastCheckpointPosition;
        Physics2D.SyncTransforms();

        // 3. Forzar retorno a forma humana si estaba en forma de gato
        if (transformationManager != null)
        {
            transformationManager.ResetToHumanForm();
        }

        // 4. Centrar la cámara de inmediato sin suavizado ese frame
        if (cameraFollow != null)
        {
            cameraFollow.SnapToTarget();
        }

        // 5. Activar 2 segundos de invulnerabilidad con parpadeo
        if (playerInvulnerability != null)
        {
            playerInvulnerability.TriggerInvulnerability();
        }

        Debug.Log($"[CheckpointManager] Bruja reaparecida exitosamente en {lastCheckpointPosition}.");
    }

    /// <summary>
    /// Registra un coleccionable asegurado para no duplicarlo al reaparecer.
    /// </summary>
    public void RegisterCollectedItem(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
        {
            collectedItemIds.Add(itemId);
        }
    }

    public bool IsItemCollected(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && collectedItemIds.Contains(itemId);
    }
}
