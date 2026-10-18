using TMPro;
using UnityEngine;

/// <summary>
/// Letrero fijo en el mundo. Cuando la bruja se aproxima horizontalmente, muestra su mensaje
/// en el banner del HUD (sin pausar la acción). El texto en espacio de mundo se oculta en
/// ejecución para que no se corte detrás de carteles o troncos.
/// </summary>
public class WorldSign : MonoBehaviour
{
    [Header("Texto y Apariencia")]
    [Tooltip("Componente TextMeshPro en espacio de mundo (se oculta en ejecución).")]
    [SerializeField] private TextMeshPro textMesh;

    [Tooltip("Plantilla de mensaje a mostrar (admite marcadores de ControlsMap).")]
    [TextArea(2, 4)]
    [SerializeField] private string message = "Usa {Mover} para explorar.";

    [Header("Proximidad")]
    [Tooltip("Distancia horizontal a la que se muestra el mensaje en el HUD (u).")]
    [SerializeField] private float detectionRadius = 2.6f;

    [Tooltip("Duración del mensaje en el HUD (s).")]
    [SerializeField] private float messageDuration = 3.0f;

    private const float MaxDetectionRadius = 3.0f;
    private const float MaxVerticalDiff = 2.5f;

    private Transform playerTransform;
    private bool wasInRange = false;

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshPro>(true);
        }

        // Los letreros de la escena se serializaron con radios grandes; limitar para que no se solapen
        if (detectionRadius > MaxDetectionRadius) detectionRadius = 2.6f;

        if (textMesh != null)
        {
            textMesh.text = ControlsMap.Resolve(message);
            MeshRenderer mr = textMesh.GetComponent<MeshRenderer>();
            if (mr != null && Application.isPlaying)
            {
                mr.enabled = false;
            }
        }
    }

    private void Start()
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) playerTransform = pc.transform;
            return;
        }

        float distance = Mathf.Abs(transform.position.x - playerTransform.position.x);
        float verticalDiff = Mathf.Abs(transform.position.y - playerTransform.position.y);
        bool inRange = distance <= detectionRadius && verticalDiff <= MaxVerticalDiff;

        if (inRange && !wasInRange)
        {
            // Mostrar el mensaje del cartel en el HUD limpio sin tapar el mundo ni pausar la acción
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsOpen)
            {
                HUDController hud = Object.FindFirstObjectByType<HUDController>();
                if (hud != null && !string.IsNullOrWhiteSpace(message))
                {
                    hud.ShowMessage(ControlsMap.Resolve(message), messageDuration);
                }
            }
        }
        wasInRange = inRange;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.35f);
        Vector3 size = new Vector3(detectionRadius * 2f, MaxVerticalDiff * 2f, 0f);
        Gizmos.DrawWireCube(transform.position, size);
    }
}
