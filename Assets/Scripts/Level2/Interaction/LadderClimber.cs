using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Controlador de escalada vertical y descenso de plataformas para la bruja en el Nivel 2.
    /// Se acopla de forma no invasiva junto a PlayerController ([DefaultExecutionOrder(50)]) sin
    /// modificar el script original ni desactivar globalmente las colisiones:
    /// - Limita el cambio de gravityScale = 0 estrictamente al estado IsClimbing y lo restaura al salir.
    /// - Ignora colisiones únicamente con las plataformas de forjado superior asociadas a la escalera activa
    ///   para permitir subir y bajar entre pisos sin atascos.
    /// - Permite detenerse en mitad de la escalera sin deslizarse, cambiar de sentido, saltar o apearse.
    /// - Permite descender voluntariamente de plataformas de un solo sentido pulsando S/↓ + Espacio.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class LadderClimber : MonoBehaviour
    {
        [Header("Parámetros de Escalada")]
        [Tooltip("Velocidad vertical al subir o bajar por escaleras (u/s).")]
        [SerializeField] private float climbSpeed = 4.5f;
        [Tooltip("Velocidad de centrado horizontal hacia el eje de la escalera (u/s).")]
        [SerializeField] private float horizontalSnapSpeed = 8.0f;
        [Tooltip("Velocidad horizontal permitida mientras se está en la escalera (u/s).")]
        [SerializeField] private float ladderHorizontalAllowance = 1.8f;
        [Tooltip("Impulso vertical al saltar desde una escalera (u/s).")]
        [SerializeField] private float ladderJumpVelocity = 8.5f;
        [Tooltip("Tiempo mínimo tras soltar la escalera antes de volver a engancharse (s).")]
        [SerializeField] private float regrabCooldown = 0.2f;

        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private PlayerController playerController;
        private TransformationManager transformationManager;

        private readonly List<LadderInteractable> overlappingLadders = new List<LadderInteractable>();
        private readonly List<Collider2D> ignoredPlatforms = new List<Collider2D>();

        private LadderInteractable currentLadder;
        private bool isClimbing;
        private float savedGravityScale = 2.2f;
        private float lastDismountTime = -10f;

        // Entrada programática para pruebas automatizadas
        private bool useScriptedClimbInput;
        private float scriptedVertical;
        private float scriptedHorizontal;
        private bool scriptedJumpRequested;

        public bool IsClimbing => isClimbing;
        public LadderInteractable CurrentLadder => currentLadder;
        public LadderInteractable NearestLadder => GetBestOverlappingLadder();

        public event Action<bool> OnClimbingStateChanged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
            playerController = GetComponent<PlayerController>();
            transformationManager = GetComponent<TransformationManager>();

            if (rb != null && rb.gravityScale > 0.01f)
            {
                savedGravityScale = rb.gravityScale;
            }
        }

        private void OnDisable()
        {
            if (isClimbing)
            {
                StopClimbing(restoreVelocityZero: true);
            }
            RestoreIgnoredPlatforms();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // Si la bruja se transforma en gato o activa el vuelo, cancelar escalada limpiamente
            if (isClimbing)
            {
                bool isCat = transformationManager != null && transformationManager.IsCatForm;
                bool isFlying = playerController != null && playerController.IsFlying;
                if (isCat || isFlying)
                {
                    StopClimbing(restoreVelocityZero: false);
                    return;
                }
            }

            ReadClimbInputs(out float horiz, out float vert, out bool jumpPressed);

            if (!isClimbing)
            {
                // Descenso voluntario en plataformas one-way (S/↓ + Espacio) cuando no hay escalera debajo
                if (vert < -0.5f && jumpPressed && GetBestOverlappingLadder() == null)
                {
                    TryDropThroughOneWayPlatform();
                    return;
                }

                if (Time.time >= lastDismountTime + regrabCooldown)
                {
                    TryEngageLadder(vert);
                }
            }
            else
            {
                // Saltar desde la escalera con Espacio
                if (jumpPressed)
                {
                    JumpOffLadder(horiz);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isClimbing || currentLadder == null)
            {
                return;
            }

            ReadClimbInputs(out float horiz, out float vert, out _);

            // Asegurar gravedad cero mientras está en estado de escalada
            rb.gravityScale = 0f;

            float feetOffset = GetFeetOffsetFromTransformY();
            float currentFeetY = transform.position.y + feetOffset;

            // 1. Verificar llegada al extremo superior subiendo
            if (vert > 0.05f && currentFeetY >= currentLadder.TopY - 0.02f)
            {
                float targetTransformY = currentLadder.TopY - feetOffset + 0.03f;
                transform.position = new Vector3(transform.position.x, targetTransformY, transform.position.z);
                StopClimbing(restoreVelocityZero: true);
                return;
            }

            // 2. Verificar llegada al extremo inferior bajando
            if (vert < -0.05f && currentFeetY <= currentLadder.BottomY + 0.04f)
            {
                float targetTransformY = currentLadder.BottomY - feetOffset + 0.02f;
                transform.position = new Vector3(transform.position.x, targetTransformY, transform.position.z);
                StopClimbing(restoreVelocityZero: true);
                return;
            }

            // 3. Permitir apearse lateralmente si está en los extremos y camina decididamente hacia un lado sin pulsar arriba/abajo
            if (Mathf.Abs(vert) < 0.1f && Mathf.Abs(horiz) > 0.75f)
            {
                bool nearBottom = Mathf.Abs(currentFeetY - currentLadder.BottomY) <= 0.25f;
                bool nearTop = Mathf.Abs(currentFeetY - currentLadder.TopY) <= 0.25f;
                if (nearBottom || nearTop)
                {
                    StopClimbing(restoreVelocityZero: false);
                    return;
                }
            }

            // 4. Movimiento vertical controlado y centrado suave en el eje X de la escalera
            float targetVy = vert * climbSpeed;
            float dx = currentLadder.CenterX - transform.position.x;
            float targetVx;

            if (Mathf.Abs(horiz) > 0.1f)
            {
                // Permite ligero ajuste lateral sin salirse del cuerpo de la escalera
                float maxOffset = 0.35f;
                if ((horiz > 0f && dx < -maxOffset) || (horiz < 0f && dx > maxOffset))
                {
                    targetVx = 0f;
                }
                else
                {
                    targetVx = horiz * ladderHorizontalAllowance;
                }
            }
            else
            {
                targetVx = Mathf.Clamp(dx * horizontalSnapSpeed, -4.5f, 4.5f);
                if (Mathf.Abs(dx) < 0.02f) targetVx = 0f;
            }

            // Prevenir que sobrepase los límites físicos de la escalera en este paso de física
            float nextFeetY = currentFeetY + targetVy * Time.fixedDeltaTime;
            if (targetVy > 0f && nextFeetY > currentLadder.TopY + 0.03f)
            {
                float targetTransformY = currentLadder.TopY - feetOffset + 0.03f;
                transform.position = new Vector3(currentLadder.CenterX, targetTransformY, transform.position.z);
                StopClimbing(restoreVelocityZero: true);
                return;
            }
            if (targetVy < 0f && nextFeetY < currentLadder.BottomY)
            {
                float targetTransformY = currentLadder.BottomY - feetOffset + 0.02f;
                transform.position = new Vector3(currentLadder.CenterX, targetTransformY, transform.position.z);
                StopClimbing(restoreVelocityZero: true);
                return;
            }

            rb.linearVelocity = new Vector2(targetVx, targetVy);
        }

        private void ReadClimbInputs(out float horiz, out float vert, out bool jumpPressed)
        {
            if (useScriptedClimbInput)
            {
                horiz = scriptedHorizontal;
                vert = scriptedVertical;
                jumpPressed = scriptedJumpRequested;
                scriptedJumpRequested = false;
                return;
            }

            horiz = Input.GetAxisRaw("Horizontal");
            vert = Input.GetAxisRaw("Vertical");
            jumpPressed = Input.GetButtonDown("Jump");
        }

        private void TryEngageLadder(float verticalInput)
        {
            if (Mathf.Abs(verticalInput) < 0.25f) return;

            if (transformationManager != null && transformationManager.IsCatForm) return;
            if (playerController != null && playerController.IsFlying) return;

            LadderInteractable ladder = GetBestOverlappingLadder();
            if (ladder == null || !ladder.CanInteract(gameObject)) return;

            float feetY = GetFeetWorldY();

            // Subir: debe estar por debajo del tope superior de la escalera
            if (verticalInput > 0.25f && feetY < ladder.TopY - 0.12f)
            {
                StartClimbing(ladder, verticalInput);
            }
            // Bajar: debe estar por encima de la base inferior de la escalera
            else if (verticalInput < -0.25f && feetY > ladder.BottomY + 0.15f)
            {
                StartClimbing(ladder, verticalInput);
            }
        }

        /// <summary>
        /// Inicia el estado controlado de escalada sobre la escalera indicada.
        /// </summary>
        public void StartClimbing(LadderInteractable ladder, float initialDirection = 0f)
        {
            if (ladder == null) return;

            if (!isClimbing && rb != null && rb.gravityScale > 0.01f)
            {
                savedGravityScale = rb.gravityScale;
            }

            currentLadder = ladder;
            isClimbing = true;

            if (playerController != null && playerController.IsFlying)
            {
                playerController.CancelFlight();
            }

            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;

            // Ignorar temporalmente las plataformas de un solo sentido en el tope de esta escalera
            ApplyPassThroughPlatforms(ladder);

            float feetOffset = GetFeetOffsetFromTransformY();
            float feetY = transform.position.y + feetOffset;

            // Si empieza a bajar desde encima de la plataforma superior, bajar ligeramente los pies
            // para entrar suavemente en el tramo de la escalera
            if (initialDirection < -0.1f && feetY >= ladder.TopY - 0.1f)
            {
                float targetTransformY = (ladder.TopY - 0.14f) - feetOffset;
                transform.position = new Vector3(ladder.CenterX, targetTransformY, transform.position.z);
            }
            else
            {
                float clampedFeetY = Mathf.Clamp(feetY, ladder.BottomY + 0.02f, ladder.TopY - 0.02f);
                transform.position = new Vector3(
                    Mathf.Lerp(transform.position.x, ladder.CenterX, 0.65f),
                    clampedFeetY - feetOffset,
                    transform.position.z);
            }

            Physics2D.SyncTransforms();
            OnClimbingStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Finaliza la escalada y restaura de inmediato los parámetros originales del Rigidbody2D y las colisiones.
        /// </summary>
        public void StopClimbing(bool restoreVelocityZero = true)
        {
            if (!isClimbing && currentLadder == null) return;

            isClimbing = false;
            currentLadder = null;
            lastDismountTime = Time.time;

            if (rb != null)
            {
                rb.gravityScale = savedGravityScale > 0.01f ? savedGravityScale : 2.2f;
                if (restoreVelocityZero)
                {
                    rb.linearVelocity = new Vector2(0f, 0f);
                }
            }

            RestoreIgnoredPlatforms();
            Physics2D.SyncTransforms();
            OnClimbingStateChanged?.Invoke(false);
        }

        private void JumpOffLadder(float horizInput)
        {
            StopClimbing(restoreVelocityZero: false);
            if (rb != null)
            {
                float vx = horizInput * 5.5f;
                rb.linearVelocity = new Vector2(vx, ladderJumpVelocity);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJumpSound();
            }
        }

        private void ApplyPassThroughPlatforms(LadderInteractable ladder)
        {
            RestoreIgnoredPlatforms();
            if (ladder == null || boxCollider == null) return;

            ladder.AutoDetectPassThroughPlatforms();
            foreach (Collider2D plat in ladder.PassThroughPlatforms)
            {
                if (plat != null && plat != boxCollider)
                {
                    Physics2D.IgnoreCollision(boxCollider, plat, true);
                    ignoredPlatforms.Add(plat);
                }
            }
        }

        private void RestoreIgnoredPlatforms()
        {
            if (boxCollider != null)
            {
                foreach (Collider2D plat in ignoredPlatforms)
                {
                    if (plat != null)
                    {
                        Physics2D.IgnoreCollision(boxCollider, plat, false);
                    }
                }
            }
            ignoredPlatforms.Clear();
        }

        private void TryDropThroughOneWayPlatform()
        {
            if (boxCollider == null) return;
            Bounds b = boxCollider.bounds;
            Vector2 checkCenter = new Vector2(b.center.x, b.min.y - 0.08f);
            Vector2 checkSize = new Vector2(b.size.x * 0.85f, 0.2f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, LayerMask.GetMask("Ground"));
            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.isTrigger) continue;
                if (hit.usedByEffector && hit.GetComponent<PlatformEffector2D>() != null)
                {
                    StartCoroutine(TemporaryDropThroughRoutine(hit, 0.28f));
                }
            }
        }

        private IEnumerator TemporaryDropThroughRoutine(Collider2D platformCollider, float duration)
        {
            if (platformCollider == null || boxCollider == null) yield break;
            Physics2D.IgnoreCollision(boxCollider, platformCollider, true);
            yield return new WaitForSeconds(duration);
            if (platformCollider != null && boxCollider != null && !isClimbing)
            {
                Physics2D.IgnoreCollision(boxCollider, platformCollider, false);
            }
        }

        private LadderInteractable GetBestOverlappingLadder()
        {
            overlappingLadders.RemoveAll(l => l == null || !l.gameObject.activeInHierarchy);

            // Verificación adicional por física en caso de que el personaje haya sido teletransportado
            if (overlappingLadders.Count == 0 && boxCollider != null)
            {
                Collider2D[] hits = Physics2D.OverlapBoxAll(boxCollider.bounds.center, boxCollider.bounds.size, 0f);
                foreach (Collider2D h in hits)
                {
                    if (h == null) continue;
                    LadderInteractable li = h.GetComponent<LadderInteractable>();
                    if (li != null && !overlappingLadders.Contains(li))
                    {
                        overlappingLadders.Add(li);
                    }
                }
            }

            LadderInteractable best = null;
            float bestDist = float.MaxValue;
            float px = transform.position.x;

            foreach (LadderInteractable l in overlappingLadders)
            {
                float d = Mathf.Abs(l.CenterX - px);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = l;
                }
            }
            return best;
        }

        public float GetFeetWorldY()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                return transform.position.y + boxCollider.offset.y - boxCollider.size.y * 0.5f;
            }
            return transform.position.y;
        }

        private float GetFeetOffsetFromTransformY()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                return boxCollider.offset.y - boxCollider.size.y * 0.5f;
            }
            return 0f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            LadderInteractable ladder = other.GetComponent<LadderInteractable>();
            if (ladder != null && !overlappingLadders.Contains(ladder))
            {
                overlappingLadders.Add(ladder);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null) return;
            LadderInteractable ladder = other.GetComponent<LadderInteractable>();
            if (ladder != null)
            {
                overlappingLadders.Remove(ladder);
                if (isClimbing && currentLadder == ladder)
                {
                    StopClimbing(restoreVelocityZero: false);
                }
            }
        }

        // --- API de Entrada Programática para Pruebas Automatizadas ---

        public void EnableScriptedClimbInput(bool enable)
        {
            useScriptedClimbInput = enable;
            scriptedHorizontal = 0f;
            scriptedVertical = 0f;
            scriptedJumpRequested = false;
        }

        public void SetScriptedClimbInput(float horizontal, float vertical, bool jump = false)
        {
            useScriptedClimbInput = true;
            scriptedHorizontal = horizontal;
            scriptedVertical = vertical;
            if (jump) scriptedJumpRequested = true;
        }
    }
}
