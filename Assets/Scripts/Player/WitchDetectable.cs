using UnityEngine;

[RequireComponent(typeof(TransformationManager))]
public class WitchDetectable : MonoBehaviour, IDetectable
{
    private TransformationManager tm;
    
    [Tooltip("Punto que usan los guardias para medir distancia. Si está vacío se usa el transform.")]
    [SerializeField] private Transform detectionPoint;
    
    [Tooltip("Distancia a la que el gato es detectable fuera de un escondite.")]
    public float catDetectionDistance = 2.5f;

    private void Awake()
    {
        tm = GetComponent<TransformationManager>();
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
                // Ya que no podemos cambiar la firma de IDetectable ni EnemySight, 
                // verificamos si hay algún guardia cerca. Si no hay guardias cerca, devolvemos false.
                Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, catDetectionDistance, LayerMask.GetMask("Enemy"));
                foreach (var col in enemies)
                {
                    if (col.GetComponent<EnemySight>() != null)
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
