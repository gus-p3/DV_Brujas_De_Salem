using UnityEngine;

[RequireComponent(typeof(TransformationManager))]
public class WitchDetectable : MonoBehaviour, IDetectable
{
    private TransformationManager tm;
    private PlayerController pc;
    
    [Tooltip("Punto que usan los guardias para medir distancia. Si está vacío se usa el transform.")]
    [SerializeField] private Transform detectionPoint;
    
    [Tooltip("Distancia a la que el gato es detectable fuera de un escondite.")]
    public float catDetectionDistance = 2.5f;

    private void Awake()
    {
        tm = GetComponent<TransformationManager>();
        pc = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (pc != null)
        {
            pc.SetDetectable(IsDetectable);
        }
    }

    public bool IsDetectable
    {
        get
        {
            if (tm == null) return true;
            
            if (tm.IsCatForm)
            {
                if (tm.IsInHideout) return false;
                
                // Gato fuera de la zona -> detectable solo a corta distancia.
                EnemySight[] enemies = Object.FindObjectsByType<EnemySight>(FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    if (enemy != null && Vector2.Distance(transform.position, enemy.transform.position) <= catDetectionDistance)
                    {
                        return true;
                    }
                }
                return false;
            }
            
            return true; // Forma humana siempre detectable
        }
    }

    public Transform DetectionPoint => detectionPoint != null ? detectionPoint : transform;
}
