using System;
using UnityEngine;

/// <summary>
/// Zona de escondite (maizal u hojas densas). Al entrar el jugador, si está en forma de gato,
/// activa IsInHideout en TransformationManager haciéndolo completamente indetectable.
/// Maneja zonas solapadas mediante un contador para no apagar el estado prematuramente.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class HideoutZone : MonoBehaviour
{
    // Eventos
    public event Action OnPlayerEntered;
    public event Action OnPlayerExited;

    [Header("Visualización")]
    [Tooltip("Color del Gizmo en la vista de escena.")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.3f, 0.35f);

    private BoxCollider2D boxCollider;
    private static int activeHideoutCount = 0;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        int hideoutLayer = LayerMask.NameToLayer("Hideout");
        if (hideoutLayer >= 0)
        {
            gameObject.layer = hideoutLayer;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TransformationManager tm = GetTransformationManager(collision);
        if (tm != null)
        {
            activeHideoutCount++;
            tm.IsInHideout = true;
            OnPlayerEntered?.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        TransformationManager tm = GetTransformationManager(collision);
        if (tm != null)
        {
            activeHideoutCount = Mathf.Max(0, activeHideoutCount - 1);
            if (activeHideoutCount == 0)
            {
                tm.IsInHideout = false;
            }
            OnPlayerExited?.Invoke();
        }
    }

    private TransformationManager GetTransformationManager(Collider2D col)
    {
        if (col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            TransformationManager tm = col.GetComponent<TransformationManager>();
            if (tm == null)
            {
                tm = col.GetComponentInParent<TransformationManager>();
            }
            return tm;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        if (boxCollider != null)
        {
            Gizmos.color = gizmoColor;
            Vector3 center = transform.TransformPoint(boxCollider.offset);
            Vector3 size = Vector3.Scale(boxCollider.size, transform.lossyScale);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
