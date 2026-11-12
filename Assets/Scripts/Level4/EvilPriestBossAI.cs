using System;
using System.Collections;
using UnityEngine;

namespace Salem.Level4
{
    /// <summary>
    /// Jefe del Piso 4 (Campanario): el Padre Parris que, al ver a la bruja en forma humana,
    /// se transforma en su forma maligna, la persigue, ataca cuerpo a cuerpo y lanza agua bendita.
    ///
    /// Flujo de estados:
    ///   Idle → (ve a la bruja) → Transforming → Chasing
    ///   Chasing → (melee range)  → MeleeAttack → Chasing
    ///   Chasing → (throw range)  → Throwing    → Chasing
    ///   Cualquier estado (recibe poción) → Stunned → Chasing
    ///   Chasing/etc (cáliz envenenado)  → Collapsed
    ///
    /// La bruja NO puede derrotar al jefe con pociones: solo lo aturde para escapar
    /// y llegar al cáliz con [E].
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class EvilPriestBossAI : MonoBehaviour, ISpellTarget
    {
        // ------------------------------------------------------------------ //
        //  Estados                                                            //
        // ------------------------------------------------------------------ //
        public enum BossState
        {
            Idle,           // Padre normal, no detectó a la bruja
            Transforming,   // Animación de transformación (~2 s)
            Chasing,        // Persigue a la bruja
            MeleeAttack,    // Golpe cuerpo a cuerpo
            Throwing,       // Lanza agua bendita
            Stunned,        // Aturdido por poción (temporal)
            Collapsed       // Colapsado tras envenenar el cáliz
        }

        // ------------------------------------------------------------------ //
        //  Configuración                                                      //
        // ------------------------------------------------------------------ //
        [Header("Visión")]
        [Tooltip("Distancia horizontal de detección (solo forma humana de la bruja).")]
        [SerializeField] private float detectionRangeX = 9f;
        [Tooltip("Distancia vertical de detección.")]
        [SerializeField] private float detectionRangeY = 3f;

        [Header("Movimiento")]
        [SerializeField] private float chaseSpeed = 3.4f;
        [SerializeField] private float transformDuration = 2.0f;

        [Header("Melee")]
        [Tooltip("Distancia X máxima para atacar cuerpo a cuerpo.")]
        [SerializeField] private float meleeRange = 1.5f;
        [SerializeField] private float meleeCooldown = 1.6f;
        [Tooltip("Fuerza de empuje al golpear.")]
        [SerializeField] private float meleeKnockback = 5f;
        [Tooltip("Duración de la animación de golpe antes de volver a perseguir.")]
        [SerializeField] private float meleeAnimDuration = 0.65f;

        [Header("Lanzamiento de Agua Bendita")]
        [Tooltip("Distancia X mínima desde la que lanza (si está más cerca, hace melee).")]
        [SerializeField] private float throwMinRange = 1.8f;
        [Tooltip("Distancia X máxima desde la que lanza.")]
        [SerializeField] private float throwMaxRange = 7f;
        [SerializeField] private float throwCooldown = 3.2f;
        [Tooltip("Velocidad horizontal del proyectil de agua bendita.")]
        [SerializeField] private float projectileHorzSpeed = 7f;
        [Tooltip("Sprite del frasco de agua bendita. Si es null se usa un círculo blanco.")]
        [SerializeField] private Sprite holyWaterSprite;

        [Header("Stun")]
        [Tooltip("Duración mínima del aturdimiento (el max es el parametro recibido de la poción).")]
        [SerializeField] private float minStunDuration = 3.0f;

        [Header("Arte — Padre Normal (Idle)")]
        [SerializeField] private SpriteRenderer normalRenderer;
        [SerializeField] private Sprite[] normalIdleFrames;
        [SerializeField] private float normalAnimFps = 6f;

        [Header("Arte — Forma Maligna")]
        [SerializeField] private SpriteRenderer evilRenderer;
        [SerializeField] private Sprite[] evilTransformFrames;
        [SerializeField] private Sprite[] evilIdleFrames;
        [SerializeField] private Sprite[] evilRunFrames;
        [SerializeField] private Sprite[] evilAttackFrames;
        [SerializeField] private Sprite[] evilThrowFrames;
        [SerializeField] private Sprite[] evilStunFrames;
        [SerializeField] private Sprite   evilCollapsedSprite;
        [SerializeField] private float    evilAnimFps = 8f;
        [Tooltip("El sprite de la forma maligna mira a la derecha (true) o a la izquierda (false).")]
        [SerializeField] private bool spriteFacesRight = false;

        [Header("Audio")]
        [SerializeField] private AudioClip transformSfx;
        [SerializeField] private AudioClip roarSfx;
        [SerializeField] private AudioClip meleeSfx;
        [SerializeField] private AudioClip throwSfx;
        [SerializeField] private AudioSource audioSource;

        // ------------------------------------------------------------------ //
        //  Estado interno                                                     //
        // ------------------------------------------------------------------ //
        public BossState CurrentState { get; private set; } = BossState.Idle;
        public event Action OnTransformed;

        private Rigidbody2D _rb;
        private GameObject  _player;
        private TransformationManager _playerForm;
        private bool  _facingRight = false;   // Inicialmente mira hacia el jugador (izquierda)
        private float _lastMeleeTime  = -99f;
        private float _lastThrowTime  = -99f;
        private float _stateTimer;
        private bool  _meleeHitDelivered;

        // ------------------------------------------------------------------ //
        //  Unity                                                              //
        // ------------------------------------------------------------------ //
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player != null)
                _playerForm = _player.GetComponent<TransformationManager>();

            // Asegurarse de que el renderer maligno empieza oculto
            if (evilRenderer != null) evilRenderer.enabled = false;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            switch (CurrentState)
            {
                case BossState.Idle:         UpdateIdle();         break;
                case BossState.Transforming: UpdateTransforming(); break;
                case BossState.Chasing:      UpdateChasing();      break;
                case BossState.MeleeAttack:  UpdateMelee();        break;
                case BossState.Throwing:     UpdateThrowing();     break;
                case BossState.Stunned:      UpdateStunned();      break;
                // Collapsed: no hace nada
            }
        }

        // ------------------------------------------------------------------ //
        //  Estados — Idle                                                     //
        // ------------------------------------------------------------------ //
        private void UpdateIdle()
        {
            // Animar el renderer normal
            PlayFrames(normalRenderer, normalIdleFrames, normalAnimFps);

            if (_player == null) return;
            if (CanSeePlayer()) EnterTransforming();
        }

        // ------------------------------------------------------------------ //
        //  Estados — Transforming                                             //
        // ------------------------------------------------------------------ //
        private void EnterTransforming()
        {
            CurrentState = BossState.Transforming;
            _stateTimer  = transformDuration;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (transformSfx != null) PlaySFX(transformSfx);

            var hud = FindFirstObjectByType<HUDController>();
            hud?.ShowMessage("¡El Padre Parris revela su verdadera naturaleza! ¡Envenena el cáliz y escapa!", 4f);
        }

        private void UpdateTransforming()
        {
            // Primera mitad: animar el renderer normal (frames de alarma/grito)
            Sprite[] transFrames = (evilTransformFrames != null && evilTransformFrames.Length > 0)
                ? evilTransformFrames : normalIdleFrames;

            if (_stateTimer > transformDuration * 0.5f)
            {
                PlayFrames(normalRenderer, transFrames, normalAnimFps * 1.5f);
            }
            else
            {
                // Segunda mitad: activar forma maligna
                if (normalRenderer != null && normalRenderer.enabled)
                {
                    normalRenderer.enabled = false;
                    if (evilRenderer != null) evilRenderer.enabled = true;
                    CameraFollow.Shake(0.18f, 0.5f);
                    if (roarSfx != null) PlaySFX(roarSfx, 1.1f);
                    OnTransformed?.Invoke();
                }
                PlayFrames(evilRenderer, evilIdleFrames, evilAnimFps);
            }

            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
                CurrentState = BossState.Chasing;
        }

        // ------------------------------------------------------------------ //
        //  Estados — Chasing                                                  //
        // ------------------------------------------------------------------ //
        private void UpdateChasing()
        {
            PlayFrames(evilRenderer, evilRunFrames, evilAnimFps);
            if (_player == null) return;

            Vector2 toPlayer = (Vector2)(_player.transform.position - transform.position);
            float distX = Mathf.Abs(toPlayer.x);

            FaceDirection(toPlayer.x);

            // ¿Ataque melee?
            if (distX <= meleeRange && Time.time >= _lastMeleeTime + meleeCooldown)
            {
                EnterMelee(); return;
            }

            // ¿Lanzar agua bendita?
            if (distX >= throwMinRange && distX <= throwMaxRange && Time.time >= _lastThrowTime + throwCooldown)
            {
                EnterThrowing(); return;
            }

            // Mover hacia el jugador
            if (_rb != null)
                _rb.linearVelocity = new Vector2(Mathf.Sign(toPlayer.x) * chaseSpeed, _rb.linearVelocity.y);
        }

        // ------------------------------------------------------------------ //
        //  Estados — MeleeAttack                                              //
        // ------------------------------------------------------------------ //
        private void EnterMelee()
        {
            CurrentState          = BossState.MeleeAttack;
            _stateTimer           = meleeAnimDuration;
            _meleeHitDelivered    = false;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (meleeSfx != null) PlaySFX(meleeSfx);
        }

        private void UpdateMelee()
        {
            PlayFrames(evilRenderer, evilAttackFrames, evilAnimFps);
            _stateTimer -= Time.deltaTime;

            // Entregar el golpe en el último tercio de la animación
            if (!_meleeHitDelivered && _stateTimer <= meleeAnimDuration * 0.35f && _player != null)
            {
                float distX = Mathf.Abs(_player.transform.position.x - transform.position.x);
                if (distX <= meleeRange + 0.5f)
                {
                    _meleeHitDelivered = true;
                    _lastMeleeTime = Time.time;
                    GameManager.Instance?.LoseHeart(1, "Sacerdote Maligno");
                    var rb = _player.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 knock = (Vector2)(_player.transform.position - transform.position).normalized * meleeKnockback;
                        knock.y = Mathf.Max(knock.y, 2f);
                        rb.AddForce(knock, ForceMode2D.Impulse);
                    }
                    CameraFollow.Shake(0.14f, 0.3f);
                }
            }

            if (_stateTimer <= 0f)
                CurrentState = BossState.Chasing;
        }

        // ------------------------------------------------------------------ //
        //  Estados — Throwing                                                 //
        // ------------------------------------------------------------------ //
        private void EnterThrowing()
        {
            CurrentState = BossState.Throwing;
            _stateTimer  = 0.9f;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (throwSfx != null) PlaySFX(throwSfx);
        }

        private void UpdateThrowing()
        {
            PlayFrames(evilRenderer, evilThrowFrames, evilAnimFps);
            _stateTimer -= Time.deltaTime;

            // Lanzar el proyectil en el momento de máximo backswing (60 % del tiempo)
            if (_stateTimer <= 0.9f * 0.4f && _stateTimer > 0f && _lastThrowTime < 0f)
            {
                _lastThrowTime = Time.time;
                SpawnHolyWater();
                _stateTimer = 0f;  // terminar la animación inmediatamente
            }

            if (_stateTimer <= 0f)
                CurrentState = BossState.Chasing;
        }

        // ------------------------------------------------------------------ //
        //  Estados — Stunned                                                  //
        // ------------------------------------------------------------------ //
        private void UpdateStunned()
        {
            PlayFrames(evilRenderer, evilStunFrames, evilAnimFps * 0.5f);
            if (evilRenderer != null)
                evilRenderer.color = Color.Lerp(Color.white,
                    new Color(0.55f, 0.65f, 1f), Mathf.PingPong(Time.time * 3f, 1f));

            if (_rb != null) _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                if (evilRenderer != null) evilRenderer.color = Color.white;
                CurrentState = BossState.Chasing;
            }
        }

        // ------------------------------------------------------------------ //
        //  Colapsado (llamado por PoisonFatherEncounter tras envenenar cáliz) //
        // ------------------------------------------------------------------ //
        /// <summary>
        /// Derrumba al jefe cuando la bruja envenena el cáliz del Padre Parris.
        /// </summary>
        public void Collapse()
        {
            if (CurrentState == BossState.Collapsed) return;
            CurrentState = BossState.Collapsed;

            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            GetComponent<Collider2D>().enabled = false;

            if (evilRenderer != null)
            {
                evilRenderer.color = Color.white;
                if (evilCollapsedSprite != null) evilRenderer.sprite = evilCollapsedSprite;
            }
            else if (normalRenderer != null)
            {
                normalRenderer.enabled = true;
            }

            var hud = FindFirstObjectByType<HUDController>();
            hud?.ShowMessage("¡El sacerdote maligno ha caído! ¡Sube al campanario!", 3f);
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                            //
        // ------------------------------------------------------------------ //
        private bool CanSeePlayer()
        {
            if (_player == null) return false;
            // En idle solo detecta forma humana
            if (_playerForm != null && _playerForm.IsCatForm) return false;

            Vector2 d  = _player.transform.position - transform.position;
            bool inRange = Mathf.Abs(d.x) <= detectionRangeX && Mathf.Abs(d.y) <= detectionRangeY;
            if (!inRange) return false;

            // Raycast para evitar ver a través de paredes
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position + Vector3.up * 0.9f,
                d.normalized, d.magnitude,
                LayerMask.GetMask("Ground", "Default"));
            return hit.collider == null;
        }

        private void SpawnHolyWater()
        {
            if (_player == null) return;

            Vector3 spawnPos = transform.position + new Vector3(_facingRight ? 0.7f : -0.7f, 0.9f, 0f);
            var projGO = new GameObject("HolyWater_Projectile");
            projGO.transform.position = spawnPos;

            var rb  = projGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.4f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col  = projGO.AddComponent<CircleCollider2D>();
            col.radius = 0.22f;

            var sr  = projGO.AddComponent<SpriteRenderer>();
            sr.sprite           = holyWaterSprite;
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder     = 15;
            sr.color            = new Color(0.8f, 0.95f, 1f);
            projGO.transform.localScale = Vector3.one * 0.5f;

            var hw = projGO.AddComponent<HolyWaterProjectile>();
            hw.SetOwner(gameObject);

            // Calcular velocidad de arco parabólico hacia la última posición del jugador
            Vector2 toTarget  = (Vector2)(_player.transform.position - spawnPos);
            float   timeToHit = Mathf.Max(Mathf.Abs(toTarget.x) / projectileHorzSpeed, 0.3f);
            float   vx        = Mathf.Sign(toTarget.x) * projectileHorzSpeed;
            float   vy        = (toTarget.y - 0.5f * Physics2D.gravity.y * rb.gravityScale * timeToHit * timeToHit) / timeToHit;
            vy = Mathf.Clamp(vy, -8f, 14f);
            rb.linearVelocity = new Vector2(vx, vy);
            rb.angularVelocity = _facingRight ? -300f : 300f;
        }

        private void PlayFrames(SpriteRenderer sr, Sprite[] frames, float fps)
        {
            if (sr == null || frames == null || frames.Length == 0) return;
            sr.sprite = frames[(int)(Time.time * fps) % frames.Length];
        }

        private void FaceDirection(float dx)
        {
            if (Mathf.Abs(dx) < 0.05f) return;
            _facingRight = dx > 0f;
            if (evilRenderer != null)
                evilRenderer.flipX = _facingRight != spriteFacesRight;
        }

        private void PlaySFX(AudioClip clip, float pitch = 1f)
        {
            if (audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip, 0.9f);
            }
            else
            {
                AudioManager.Instance?.PlaySFX(clip);
            }
        }

        // ------------------------------------------------------------------ //
        //  ISpellTarget — las pociones aturden pero NO derrotan al jefe       //
        // ------------------------------------------------------------------ //
        public bool IsAffectedBySpell => CurrentState == BossState.Stunned;

        public void ApplyBlind(float seconds)      => EnterStun(Mathf.Max(seconds, minStunDuration));
        public void ApplyHypnotize(float seconds, int _) => EnterStun(Mathf.Max(seconds * 0.75f, minStunDuration));
        public void ApplySleep(float seconds)      => EnterStun(Mathf.Max(seconds, minStunDuration + 1f));

        private void EnterStun(float duration)
        {
            if (CurrentState == BossState.Collapsed) return;
            CurrentState = BossState.Stunned;
            _stateTimer  = duration;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
    }
}
