using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador visual del contador de escoba en el HUD ("Escoba: X/3")
/// y los iconos de rama, paja y cuerda que se iluminan al recolectarlos.
/// </summary>
public class BroomHUD : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Texto para mostrar 'Escoba: X/3'.")]
    [SerializeField] private TextMeshProUGUI counterText;
    [Tooltip("Icono de la rama.")]
    [SerializeField] private Image branchIcon;
    [Tooltip("Icono de la paja.")]
    [SerializeField] private Image strawIcon;
    [Tooltip("Icono de la cuerda.")]
    [SerializeField] private Image ropeIcon;

    [Header("Colores")]
    [Tooltip("Color del icono cuando la parte ha sido recolectada.")]
    [SerializeField] private Color collectedColor = Color.white;
    [Tooltip("Color tenue del icono cuando la parte aún no se ha recolectado.")]
    [SerializeField] private Color uncollectedColor = new Color(1f, 1f, 1f, 0.25f);

    private void Start()
    {
        if (BroomInventory.Instance != null)
        {
            BroomInventory.Instance.OnPartCollected += HandlePartCollected;
            BroomInventory.Instance.OnBroomCompleted += HandleBroomCompleted;
        }

        UpdateVisuals();
    }

    private void OnDestroy()
    {
        if (BroomInventory.Instance != null)
        {
            BroomInventory.Instance.OnPartCollected -= HandlePartCollected;
            BroomInventory.Instance.OnBroomCompleted -= HandleBroomCompleted;
        }
    }

    private void HandlePartCollected(BroomPart part, int count)
    {
        UpdateVisuals();
    }

    private void HandleBroomCompleted()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        int count = BroomInventory.Instance != null ? BroomInventory.Instance.CollectedCount : 0;
        if (counterText != null)
        {
            counterText.text = $"Escoba: {count}/3";
        }

        bool hasBranch = BroomInventory.Instance != null && BroomInventory.Instance.HasPart(BroomPart.Branch);
        bool hasStraw = BroomInventory.Instance != null && BroomInventory.Instance.HasPart(BroomPart.Straw);
        bool hasRope = BroomInventory.Instance != null && BroomInventory.Instance.HasPart(BroomPart.Rope);

        if (branchIcon != null) branchIcon.color = hasBranch ? collectedColor : uncollectedColor;
        if (strawIcon != null) strawIcon.color = hasStraw ? collectedColor : uncollectedColor;
        if (ropeIcon != null) ropeIcon.color = hasRope ? collectedColor : uncollectedColor;
    }
}
