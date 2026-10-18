using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Puente entre el SpriteRenderer original de la bruja ("Visual") y el rig por piezas.
/// PlayerController (volteo), PlayerInvulnerability (parpadeo), CornFoliageFade (tinte y capa) y
/// BroomVisual siguen trabajando sobre ese SpriteRenderer; aquí se copia su estado al rig.
/// En forma humana el sprite original no se dibuja y se muestra el rig; en forma de gato, al revés.
/// </summary>
[DefaultExecutionOrder(-10)]
public class RigSpriteProxy : MonoBehaviour
{
    [Tooltip("SpriteRenderer original (objeto Visual con el Animator).")]
    [SerializeField] private SpriteRenderer source;
    [Tooltip("Rig por piezas que sustituye al sprite en forma humana.")]
    [SerializeField] private WitchRigAnimator rig;

    private TransformationManager transformation;
    private SortingGroup group;
    private SpriteRenderer[] parts;
    private Color[] partColors;
    private Color lastTint = Color.white;

    public void Setup(SpriteRenderer sourceRenderer, WitchRigAnimator rigAnimator)
    {
        source = sourceRenderer;
        rig = rigAnimator;
    }

    private void Awake()
    {
        transformation = GetComponentInParent<TransformationManager>();
        if (rig == null) rig = GetComponentInChildren<WitchRigAnimator>(true);
        if (source == null)
        {
            Animator a = GetComponentInChildren<Animator>(true);
            if (a != null) source = a.GetComponent<SpriteRenderer>();
        }
        if (rig == null) return;

        group = rig.GetComponent<SortingGroup>();
        parts = rig.GetComponentsInChildren<SpriteRenderer>(true);
        partColors = new Color[parts.Length];
        for (int i = 0; i < parts.Length; i++) partColors[i] = parts[i].color;
    }

    private void LateUpdate()
    {
        if (rig == null || source == null) return;

        bool cat = transformation != null && transformation.IsCatForm;
        if (rig.gameObject.activeSelf == cat) rig.gameObject.SetActive(!cat);
        source.forceRenderingOff = !cat;
        if (cat) return;

        rig.FacingRight = !source.flipX;

        if (group != null)
        {
            group.sortingLayerID = source.sortingLayerID;
            group.sortingOrder = source.sortingOrder;
        }

        Color tint = source.color;
        if (tint != lastTint)
        {
            for (int i = 0; i < parts.Length; i++) parts[i].color = partColors[i] * tint;
            lastTint = tint;
        }
    }

    private void OnDisable()
    {
        if (source != null) source.forceRenderingOff = false;
    }
}
