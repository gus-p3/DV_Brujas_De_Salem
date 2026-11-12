using UnityEngine;

namespace Salem.Level4
{
    /// <summary>
    /// Proyectil de agua bendita lanzado por el Padre Parris en su forma maligna.
    /// Vuela en arco hacia la bruja y, al impactar, le quita un corazón (si está en forma humana)
    /// y la empuja. Contra el suelo u otras superficies simplemente se destruye.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class HolyWaterProjectile : MonoBehaviour
    {
        private GameObject _owner;
        private bool _hasHit;
        private SpriteRenderer _sr;

        public void SetOwner(GameObject owner)
        {
            _owner = owner;
            // Ignorar colisiones con el propio jefe mientras esté vivo
            Collider2D myCol = GetComponent<Collider2D>();
            if (myCol != null && _owner != null)
            {
                foreach (Collider2D oc in _owner.GetComponentsInChildren<Collider2D>(true))
                    Physics2D.IgnoreCollision(myCol, oc, true);
            }
        }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            GetComponent<Rigidbody2D>().freezeRotation = false;   // rota en vuelo
        }

        private void Start() => Destroy(gameObject, 5f);

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (_hasHit) return;
            if (_owner != null && col.gameObject == _owner) return;

            // Buscar al jugador en el impacto
            var pc = col.gameObject.GetComponent<PlayerController>()
                     ?? col.gameObject.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                var tf = pc.GetComponent<TransformationManager>();
                bool isCat = tf != null && tf.IsCatForm;
                // El agua bendita solo daña en forma humana
                if (!isCat && GameManager.Instance != null)
                {
                    GameManager.Instance.LoseHeart(1, "Agua Bendita");
                    var rb = pc.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 knock = ((Vector2)(pc.transform.position - transform.position)).normalized * 4f;
                        knock.y = Mathf.Max(knock.y, 1.5f);
                        rb.AddForce(knock, ForceMode2D.Impulse);
                    }
                    CameraFollow.Shake(0.10f, 0.22f);
                }
            }

            DestroyProjectile();
        }

        private void DestroyProjectile()
        {
            _hasHit = true;
            if (_sr != null) _sr.enabled = false;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
            Destroy(gameObject, 0.1f);
        }
    }
}
