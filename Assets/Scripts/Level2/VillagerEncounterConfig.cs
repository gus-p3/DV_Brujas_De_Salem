using System.Reflection;
using UnityEngine;

/// <summary>
/// Configuración por instancia para aldeanos en el Nivel 2.
/// Permite fijar el WanderPattern pedagógico (WalkAndReturn, LookAround, ShortStops)
/// sin modificar el script compartido VillagerAI ni su prefab.
/// </summary>
public class VillagerEncounterConfig : MonoBehaviour
{
    [Tooltip("Patrón de paseo errático asignado a este aldeano.")]
    [SerializeField] private WanderPattern assignedPattern = WanderPattern.WalkAndReturn;

    private static readonly FieldInfo PatternField = typeof(VillagerAI).GetField("currentPattern", BindingFlags.NonPublic | BindingFlags.Instance);

    public WanderPattern AssignedPattern
    {
        get => assignedPattern;
        set
        {
            assignedPattern = value;
            ApplyPattern();
        }
    }

    private void Start()
    {
        ApplyPattern();
    }

    public void ApplyPattern()
    {
        VillagerAI ai = GetComponent<VillagerAI>();
        if (ai != null && PatternField != null)
        {
            PatternField.SetValue(ai, assignedPattern);
        }
    }
}
