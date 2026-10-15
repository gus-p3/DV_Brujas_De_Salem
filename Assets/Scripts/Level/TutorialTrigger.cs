using UnityEngine;

/// <summary>
/// Trigger de activación para tutoriales contextuales en el mundo.
/// Al entrar la bruja despliega el consejo en TutorialManager.
/// Dibuja un Gizmo azul translúcido con su identificador en el Editor.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("Configuración del Tutorial")]
    [Tooltip("Identificador único del tip (p. ej. 'tip_salto').")]
    [SerializeField] private string tipId = "tip_tutorial";

    [Tooltip("Título del cuadro del tutorial.")]
    [SerializeField] private string tipTitle = "Tutorial";

    [Tooltip("Cuerpo del texto. Admite marcadores como {Transformar}, {Volar}, {Saltar}.")]
    [TextArea(3, 6)]
    [SerializeField] private string tipBody = "Presiona {Saltar} para saltar.";

    [Tooltip("Icono opcional para el cuadro.")]
    [SerializeField] private Sprite tipIcon;

    [Tooltip("Si se marca, el trigger se desactiva tras dispararse.")]
    [SerializeField] private bool oneShot = true;

    private BoxCollider2D boxCollider;

    public string TipId => tipId;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null && !other.CompareTag("Player")) return;

        if (TutorialManager.Instance == null) return;

        if (TutorialManager.Instance.HasSeen(tipId))
        {
            if (oneShot) gameObject.SetActive(false);
            return;
        }

        TutorialTip tip = new TutorialTip(tipId, tipTitle, tipBody, tipIcon, true);
        TutorialManager.Instance.ShowTip(tip);

        if (oneShot)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.45f, 1f, 0.35f);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.9f);
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"[Tip: {tipId}]");
#endif
    }
}
