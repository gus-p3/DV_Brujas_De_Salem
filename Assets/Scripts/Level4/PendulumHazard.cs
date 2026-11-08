using UnityEngine;

namespace Salem.Level4
{
    /// <summary>
    /// Péndulo del mecanismo del reloj: oscila sobre su pivote y el contrapeso (colliders hijos en modo trigger)
    /// quita un corazón a la bruja al tocarla, respetando la invulnerabilidad tras el golpe.
    /// Va en el pivote con un Rigidbody2D cinemático para que los triggers de los hijos lleguen aquí.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PendulumHazard : MonoBehaviour
    {
        [SerializeField] private float maxAngle = 50f;
        [SerializeField] private float period = 2.8f;
        [Tooltip("Desfase 0..1 para que dos péndulos no vayan sincronizados.")]
        [SerializeField] private float phase;
        [SerializeField] private int damageAmount = 1;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        private void FixedUpdate()
        {
            _rb.MoveRotation(maxAngle * Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f));
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            PlayerInvulnerability inv = player.GetComponent<PlayerInvulnerability>();
            if (inv != null && inv.IsInvulnerable) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
                GameManager.Instance.LoseHeart(damageAmount, "Péndulo del reloj");
        }
    }
}
