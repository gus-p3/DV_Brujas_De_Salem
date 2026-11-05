using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level3
{
    /// <summary>
    /// Plataforma móvil rúnica que oscila entre waypoints (PingPong) sobre abismos o zonas de peligro.
    /// Garantiza:
    /// - Soporte físico sobre layer Ground con BoxCollider2D y PlatformEffector2D (one-way platform).
    /// - Arrastre suave del jugador (PlayerController) sin deslizamiento ni temblores en FixedUpdate.
    /// - Brillo rúnico pulsante (Light2D / SpriteRenderer) para una estética coherente con la magia de la bruja.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Level3MovingPlatform : MonoBehaviour
    {
        [Header("Puntos de Recorrido")]
        [Tooltip("Punto de inicio en coordenadas locales o de mundo.")]
        [SerializeField] private Vector3 startPoint;
        [Tooltip("Punto de destino en coordenadas locales o de mundo.")]
        [SerializeField] private Vector3 endPoint;
        [Tooltip("Tiempo de pausa en cada extremo (segundos).")]
        [SerializeField] private float waitTimeAtEnds = 0.8f;
        [Tooltip("Velocidad de desplazamiento (unidades/s).")]
        [SerializeField] private float speed = 2.2f;

        [Header("Efectos Visuales")]
        [SerializeField] private Color runeGlowColor = new Color(0.45f, 0.85f, 1f, 1f);
        [SerializeField] private float glowPulseSpeed = 2.5f;

        private Vector3 currentTarget;
        private float pauseTimer = 0f;
        private bool isMovingToEnd = true;
        private Rigidbody2D rb2d;
        private Light2D runeLight;
        private SpriteRenderer runeRend;
        private Transform passengerPlayer;
        private Vector3 lastPosition;

        public Vector3 StartPoint => startPoint;
        public Vector3 EndPoint => endPoint;
        public float Speed => speed;

        private void Awake()
        {
            rb2d = GetComponent<Rigidbody2D>();
            if (rb2d == null)
            {
                rb2d = gameObject.AddComponent<Rigidbody2D>();
                rb2d.bodyType = RigidbodyType2D.Kinematic;
                rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) gameObject.layer = groundLayer;

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.usedByEffector = true;

            PlatformEffector2D effector = GetComponent<PlatformEffector2D>();
            if (effector == null) effector = gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;

            if (startPoint == Vector3.zero && endPoint == Vector3.zero)
            {
                startPoint = transform.position;
                endPoint = transform.position + new Vector3(5f, 0f, 0f);
            }

            currentTarget = endPoint;
            lastPosition = transform.position;
        }

        private void Start()
        {
            runeLight = GetComponentInChildren<Light2D>();
            runeRend = GetComponentInChildren<SpriteRenderer>();
        }

        public void Configure(Vector3 start, Vector3 end, float moveSpeed = 2.2f, float waitTime = 0.8f)
        {
            startPoint = start;
            endPoint = end;
            speed = moveSpeed;
            waitTimeAtEnds = waitTime;
            transform.position = startPoint;
            currentTarget = endPoint;
            lastPosition = startPoint;
        }

        private void FixedUpdate()
        {
            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.fixedDeltaTime;
                lastPosition = transform.position;
                return;
            }

            Vector3 currentPos = transform.position;
            Vector3 newPos = Vector3.MoveTowards(currentPos, currentTarget, speed * Time.fixedDeltaTime);
            Vector3 delta = newPos - currentPos;

            if (rb2d != null)
            {
                rb2d.MovePosition(newPos);
            }
            else
            {
                transform.position = newPos;
            }

            // Mover al jugador si está sobre la plataforma
            if (passengerPlayer != null)
            {
                passengerPlayer.position += delta;
            }

            if (Vector3.Distance(newPos, currentTarget) < 0.05f)
            {
                isMovingToEnd = !isMovingToEnd;
                currentTarget = isMovingToEnd ? endPoint : startPoint;
                pauseTimer = waitTimeAtEnds;
            }

            lastPosition = newPos;
        }

        private void Update()
        {
            if (runeLight != null)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * glowPulseSpeed);
                runeLight.intensity = pulse;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.collider.GetComponentInParent<PlayerController>() != null)
            {
                // Solo si el jugador está por encima de la plataforma
                if (collision.transform.position.y > transform.position.y + 0.1f)
                {
                    passengerPlayer = collision.transform;
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (passengerPlayer == null && (collision.gameObject.CompareTag("Player") || collision.collider.GetComponentInParent<PlayerController>() != null))
            {
                if (collision.transform.position.y > transform.position.y + 0.1f)
                {
                    passengerPlayer = collision.transform;
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (passengerPlayer != null && (collision.transform == passengerPlayer || collision.transform.IsChildOf(passengerPlayer)))
            {
                passengerPlayer = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 p1 = Application.isPlaying ? startPoint : transform.position;
            Vector3 p2 = Application.isPlaying ? endPoint : (endPoint != Vector3.zero ? endPoint : transform.position + new Vector3(5f, 0f, 0f));
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawWireCube(p1, Vector3.one * 0.5f);
            Gizmos.DrawWireCube(p2, Vector3.one * 0.5f);
        }
    }
}
