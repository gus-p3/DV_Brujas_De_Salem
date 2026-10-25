using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Salida de respaldo interactiva (tecla E) en la base de la escalera del sótano.
    /// Además de poder salir caminando/saltando por los peldaños o trepando con W/↑
    /// (tanto en forma humana como en forma de gato), permite pulsar E en la base de la
    /// escalera para subir directamente al piso de planta baja sin riesgo de atasco.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CellarExitBackupInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionId = "cellar_exit_backup";
        [SerializeField] private int priority = 8;
        [SerializeField] private float targetExitX = 54.0f;
        [SerializeField] private float targetFloorSurfaceY = 28.75f;
        [SerializeField] private string promptLabel = "Subir a Planta Baja";

        public string InteractionId => interactionId;
        public int Priority => priority;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.2f;

        public void Configure(string id, float exitX, float floorSurfaceY, string label = "Subir a Planta Baja")
        {
            interactionId = id;
            targetExitX = exitX;
            targetFloorSurfaceY = floorSurfaceY;
            promptLabel = label;

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null) return false;
            BoxCollider2D col = interactor.GetComponent<BoxCollider2D>();
            float feetY = col != null ? col.bounds.min.y : interactor.transform.position.y;
            return feetY < targetFloorSurfaceY - 0.5f;
        }

        public string GetPromptText(GameObject interactor)
        {
            return $"E — {promptLabel} (o W / ↑ para trepar)";
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null) return;

            LadderClimber climber = interactor.GetComponent<LadderClimber>();
            if (climber != null && climber.IsClimbing)
            {
                climber.StopClimbing(restoreVelocityZero: true);
            }

            BoxCollider2D col = interactor.GetComponent<BoxCollider2D>();
            float feetOffset = col != null ? (col.offset.y - col.size.y * 0.5f) : 0f;
            float targetY = targetFloorSurfaceY - feetOffset + 0.02f;

            interactor.transform.position = new Vector3(targetExitX, targetY, interactor.transform.position.z);

            Rigidbody2D rb = interactor.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            Physics2D.SyncTransforms();
        }
    }
}
