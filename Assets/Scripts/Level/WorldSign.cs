using TMPro;
using UnityEngine;

/// <summary>
/// Letrero fijo en el mundo (World-Space) que revela su mensaje suavemente mediante
/// fundido alfa cuando la bruja se aproxima a una distancia definida.
/// </summary>
public class WorldSign : MonoBehaviour
{
    [Header("Texto y Apariencia")]
    [Tooltip("Componente TextMeshPro en espacio de mundo.")]
    [SerializeField] private TextMeshPro textMesh;

    [Tooltip("Plantilla de mensaje a mostrar (admite marcadores de ControlsMap).")]
    [TextArea(2, 4)]
    [SerializeField] private string message = "Usa {Mover} para explorar.";

    [Header("Proximidad")]
    [Tooltip("Radio de proximidad para activar la visibilidad del texto.")]
    [SerializeField] private float detectionRadius = 3.5f;

    [Tooltip("Velocidad de fundido alfa al acercarse/alejarse.")]
    [SerializeField] private float fadeSpeed = 4.0f;

    [Tooltip("Si es verdadero, una vez que el letrero se vuelve visible se mantiene visible permanentemente.")]
    [SerializeField] private bool stayVisibleOnceSeen = true;

    private Transform playerTransform;
    private float currentAlpha = 0f;
    private bool hasBeenSeen = false;

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshPro>();
        }

        if (textMesh != null)
        {
            textMesh.text = ControlsMap.Resolve(message);
            SetTextAlpha(0f);
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
        if (hasBeenSeen && stayVisibleOnceSeen && Mathf.Approximately(currentAlpha, 1f))
        {
            return;
        }

        if (playerTransform == null)
        {
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) playerTransform = pc.transform;
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = distance <= detectionRadius;

        if (inRange)
        {
            hasBeenSeen = true;
        }

        float targetAlpha = (inRange || (hasBeenSeen && stayVisibleOnceSeen)) ? 1.0f : 0.0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        SetTextAlpha(currentAlpha);
    }

    private void SetTextAlpha(float alpha)
    {
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = alpha;
            textMesh.color = c;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
