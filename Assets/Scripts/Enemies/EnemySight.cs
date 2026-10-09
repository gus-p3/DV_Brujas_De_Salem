using System;
using UnityEngine;

/// <summary>
/// Simula el farol de un guardia con Raycast2D: lanza un abanico de rayos, se detiene en los
/// obstáculos (no ve a través de paredes), detecta objetivos IDetectable, maneja la máquina
/// de estados de alerta y dibuja el cono de luz con un Mesh dinámico.
/// </summary>
public class EnemySight : MonoBehaviour
{
    [Header("Visión")]
    [Tooltip("Alcance máximo del farol (u).")]
    [SerializeField] private float viewDistance = 7f;
    [Tooltip("Ángulo total del cono en grados.")]
    [Range(1f, 180f)]
    [SerializeField] private float viewAngle = 60f;
    [Tooltip("Cantidad de rayos que se reparten dentro del ángulo.")]
    [SerializeField] private int rayCount = 30;
    [Tooltip("Capas que bloquean la visión (Ground).")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Capas donde se busca al objetivo (Player).")]
    [SerializeField] private LayerMask targetMask;

    [Header("Referencias")]
    [Tooltip("Punto desde donde sale la luz del farol. Si está vacío se usa este transform.")]
    [SerializeField] private Transform lanternOrigin;
    [Tooltip("Transform que indica hacia dónde mira el guardia. Se mira a la derecha si su escala X es positiva y a la izquierda si es negativa.")]
    [SerializeField] private Transform facingReference;
    [Tooltip("Ángulo extra (grados) para inclinar el cono, positivo hacia arriba.")]
    [SerializeField] private float coneTiltDegrees = 0f;

    [Header("Estados de alerta")]
    [Tooltip("Segundos viendo al objetivo para pasar de Sospecha a Alerta.")]
    [SerializeField] private float suspicionTime = 1.2f;
    [Tooltip("Segundos sin ver al objetivo para volver a Calma.")]
    [SerializeField] private float lostTargetDelay = 2f;
    [Tooltip("Distancia a la que el objetivo visible es capturado (u).")]
    [SerializeField] private float captureDistance = 1.2f;

    [Header("Cono visible (Mesh)")]
    [Tooltip("Material del cono. Si está vacío se usa Sprites/Default (permite transparencia).")]
    [SerializeField] private Material coneMaterial;
    [SerializeField] private Color calmColor = new Color(1f, 0.85f, 0.3f, 0.45f);
    [SerializeField] private Color suspicionColor = new Color(1f, 0.55f, 0.1f, 0.55f);
    [SerializeField] private Color alertColor = new Color(0.9f, 0.1f, 0.1f, 0.6f);
    [Tooltip("Opacidad mínima permitida para que el cono siempre se vea claramente.")]
    [Range(0.35f, 1f)]
    [SerializeField] private float minConeAlpha = 0.35f;
    [Tooltip("Capa de ordenamiento (Sorting Layer) del cono.")]
    [SerializeField] private string coneSortingLayer = "Default";
    [Tooltip("Orden dentro de la capa de ordenamiento.")]
    [SerializeField] private int coneSortingOrder = 5;
    [Tooltip("Velocidad con la que el color del cono cambia de estado.")]
    [SerializeField] private float colorLerpSpeed = 10f;

    [Header("Depuración")]
    [SerializeField] private bool drawDebugRays = false;

    private const string DefaultConeShader = "Sprites/Default";
    private const string ConeObjectName = "ViewCone";

    // Resultado de los rayos de la última física: ángulo y longitud de cada rayo
    private Vector2[] rayDirections;
    private float[] rayLengths;

    private Mesh coneMesh;
    private Vector3[] vertices;
    private int[] triangles;
    private MeshFilter coneFilter;
    private MeshRenderer coneRenderer;
    private Material runtimeMaterial;
    private Color currentColor;

    private AlertState state = AlertState.Calm;
    private IDetectable currentTarget;
    private bool targetVisible;
    private float suspicionTimer;
    private float lostTimer;

    /// <summary>Se dispara al cambiar el estado de alerta.</summary>
    public event Action<AlertState> OnAlertStateChanged;

    /// <summary>Se dispara una sola vez cuando el objetivo es capturado.</summary>
    public event Action OnTargetCaptured;

    public AlertState CurrentState => state;
    public bool IsTargetVisible => targetVisible;

    private Transform Origin => lanternOrigin != null ? lanternOrigin : transform;

    private void Awake()
    {
        rayCount = Mathf.Max(2, rayCount);
        rayDirections = new Vector2[rayCount];
        rayLengths = new float[rayCount];
        BuildConeObject();
        currentColor = StateColor(AlertState.Calm);
    }

    private void OnEnable()
    {
        // Por defecto, la captura solo escribe en el log (aún no existe GameManager)
        OnTargetCaptured += LogCapture;
    }

    private void OnDisable()
    {
        OnTargetCaptured -= LogCapture;
    }

    private void OnValidate()
    {
        rayCount = Mathf.Max(2, rayCount);
        viewDistance = Mathf.Max(0.1f, viewDistance);
        captureDistance = Mathf.Max(0f, captureDistance);
    }

    private void FixedUpdate()
    {
        CastRays();
        UpdateState(Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        UpdateConeMesh();
        UpdateConeColor();
    }

    private void OnDestroy()
    {
        if (coneMesh != null) Destroy(coneMesh);
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }

    // ------------------------------------------------------------------- Rayos

    /// <summary>Dirección hacia donde mira el guardia (izquierda o derecha) con la inclinación del cono.</summary>
    private Vector2 GetFacingDirection()
    {
        Transform reference = facingReference != null ? facingReference : transform;
        float sign = reference.lossyScale.x >= 0f ? 1f : -1f;
        Vector2 forward = new Vector2(sign, 0f);
        return Rotate(forward, coneTiltDegrees * sign);
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void CastRays()
    {
        if (rayDirections == null || rayDirections.Length != rayCount)
        {
            rayCount = Mathf.Max(2, rayCount);
            rayDirections = new Vector2[rayCount];
            rayLengths = new float[rayCount];
        }

        Vector2 origin = Origin.position;
        Vector2 forward = GetFacingDirection();
        float startAngle = -viewAngle * 0.5f;
        float step = viewAngle / (rayCount - 1);

        bool seen = false;
        IDetectable bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir = Rotate(forward, startAngle + step * i);
            float length = viewDistance;

            // 1) El obstáculo más cercano corta el rayo: no se ve a través de paredes
            RaycastHit2D wall = Physics2D.Raycast(origin, dir, viewDistance, obstacleMask);
            if (wall.collider != null)
            {
                length = wall.distance;
            }

            // 2) Dentro del tramo libre se busca al objetivo
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, length, targetMask);
            if (hit.collider != null)
            {
                IDetectable detectable = hit.collider.GetComponentInParent<IDetectable>();
                if (detectable != null && detectable.IsDetectable)
                {
                    seen = true;
                    if (hit.distance < bestDistance)
                    {
                        bestDistance = hit.distance;
                        bestTarget = detectable;
                    }
                }
            }

            rayDirections[i] = dir;
            rayLengths[i] = length;

            if (drawDebugRays)
            {
                Debug.DrawRay(origin, dir * length, seen ? Color.red : Color.yellow);
            }
        }

        targetVisible = seen;
        if (seen)
        {
            currentTarget = bestTarget;
        }
    }

    // ----------------------------------------------------------------- Estados

    private void UpdateState(float dt)
    {
        switch (state)
        {
            case AlertState.Calm:
                if (targetVisible)
                {
                    suspicionTimer = 0f;
                    lostTimer = 0f;
                    SetState(AlertState.Suspicion);
                }
                break;

            case AlertState.Suspicion:
                if (targetVisible)
                {
                    lostTimer = 0f;
                    suspicionTimer += dt;
                    if (suspicionTimer >= suspicionTime)
                    {
                        SetState(AlertState.Alert);
                    }
                }
                else if (TickLostTimer(dt))
                {
                    SetState(AlertState.Calm);
                }
                break;

            case AlertState.Alert:
                if (targetVisible)
                {
                    lostTimer = 0f;
                    if (IsWithinCaptureDistance())
                    {
                        SetState(AlertState.Capture);
                        OnTargetCaptured?.Invoke();
                    }
                }
                else if (TickLostTimer(dt))
                {
                    SetState(AlertState.Calm);
                }
                break;

            case AlertState.Capture:
                // Estado final: se mantiene hasta que alguien llame a ResetToCalm
                break;
        }
    }

    /// <summary>Acumula el tiempo sin ver al objetivo. Devuelve true al superar lostTargetDelay.</summary>
    private bool TickLostTimer(float dt)
    {
        lostTimer += dt;
        return lostTimer >= lostTargetDelay;
    }

    private bool IsWithinCaptureDistance()
    {
        if (currentTarget == null || currentTarget.DetectionPoint == null)
        {
            return false;
        }

        float distance = Vector2.Distance(Origin.position, currentTarget.DetectionPoint.position);
        return distance < captureDistance;
    }

    private void SetState(AlertState newState)
    {
        if (state == newState)
        {
            return;
        }

        state = newState;
        OnAlertStateChanged?.Invoke(state);
    }

    /// <summary>Devuelve al guardia a Calma (por ejemplo al reiniciar el nivel tras una captura).</summary>
    public void ResetToCalm()
    {
        suspicionTimer = 0f;
        lostTimer = 0f;
        currentTarget = null;
        SetState(AlertState.Calm);
    }

    private void LogCapture()
    {
        Debug.Log($"[EnemySight] {name}: objetivo capturado (pendiente de conectar con GameManager).", this);
    }

    // -------------------------------------------------------------- Cono (Mesh)

    private void BuildConeObject()
    {
        Transform existing = transform.Find(ConeObjectName);
        GameObject coneObject = existing != null ? existing.gameObject : new GameObject(ConeObjectName);
        coneObject.transform.SetParent(transform, false);

        coneFilter = coneObject.GetComponent<MeshFilter>();
        if (coneFilter == null) coneFilter = coneObject.AddComponent<MeshFilter>();
        coneRenderer = coneObject.GetComponent<MeshRenderer>();
        if (coneRenderer == null) coneRenderer = coneObject.AddComponent<MeshRenderer>();

        Material baseMaterial = coneMaterial != null ? coneMaterial : new Material(Shader.Find(DefaultConeShader));
        runtimeMaterial = new Material(baseMaterial);
        coneRenderer.sharedMaterial = runtimeMaterial;
        coneRenderer.sortingLayerName = coneSortingLayer;
        coneRenderer.sortingOrder = coneSortingOrder;
        coneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        coneRenderer.receiveShadows = false;

        coneMesh = new Mesh { name = "EnemySightCone" };
        coneMesh.MarkDynamic();
        coneFilter.sharedMesh = coneMesh;

        vertices = new Vector3[rayCount + 1];
        triangles = new int[(rayCount - 1) * 3];
        for (int i = 0; i < rayCount - 1; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
    }

    private void UpdateConeMesh()
    {
        if (coneMesh == null || rayLengths == null)
        {
            return;
        }

        // Si rayCount cambió en ejecución, se omite el dibujo hasta reiniciar el componente (los buffers se crean en Awake)
        if (vertices == null || vertices.Length != rayCount + 1 || rayLengths.Length != rayCount)
        {
            return;
        }

        Transform coneTransform = coneFilter.transform;
        Vector3 origin = Origin.position;

        // Los vértices se calculan en mundo y se pasan a local: el cono sigue bien aunque el guardia se voltee con escala negativa
        vertices[0] = coneTransform.InverseTransformPoint(origin);
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 worldPoint = origin + (Vector3)(rayDirections[i] * rayLengths[i]);
            vertices[i + 1] = coneTransform.InverseTransformPoint(worldPoint);
        }

        coneMesh.Clear();
        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;

        // Colores por vértice (el shader Sprites/Default los multiplica por el color del material)
        Color32[] colors = new Color32[vertices.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        coneMesh.colors32 = colors;
        coneMesh.RecalculateBounds();
    }

    private Color StateColor(AlertState s)
    {
        Color c;
        switch (s)
        {
            case AlertState.Suspicion: c = suspicionColor; break;
            case AlertState.Alert:
            case AlertState.Capture: c = alertColor; break;
            default: c = calmColor; break;
        }

        c.a = Mathf.Max(c.a, minConeAlpha);
        return c;
    }

    private void UpdateConeColor()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        Color target = StateColor(state);
        currentColor = Color.Lerp(currentColor, target, 1f - Mathf.Exp(-colorLerpSpeed * Time.deltaTime));
        runtimeMaterial.color = currentColor;
    }

    // ----------------------------------------------------------------- Gizmos

    private void OnDrawGizmosSelected()
    {
        Transform origin = Origin;
        Gizmos.color = Color.yellow;
        Vector2 forward = GetFacingDirection();
        Gizmos.DrawLine(origin.position, origin.position + (Vector3)(Rotate(forward, viewAngle * 0.5f) * viewDistance));
        Gizmos.DrawLine(origin.position, origin.position + (Vector3)(Rotate(forward, -viewAngle * 0.5f) * viewDistance));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position, captureDistance);
    }
}
