using UnityEngine;

namespace Salem.Level4
{
    /// <summary>
    /// Miniboss de la nave: patrulla el pasillo, oye el ruido de pasos (NoiseProducer), ve al frente con su
    /// farol y embiste. Si alcanza a la bruja le quita un corazón y la devuelve al último checkpoint.
    /// Las pociones (ISpellTarget) lo aturden.
    /// </summary>
    public class InquisitorGuardAI : MonoBehaviour, ISpellTarget
    {
        public enum InquisitorState
        {
            Patrolling,
            Listening,
            Charging,
            Searching,
            Stunned
        }

        [Header("Movement Settings")]
        [SerializeField] private float patrolSpeed = 2.0f;
        [SerializeField] private float chargeSpeed = 6.5f;
        [SerializeField] private Transform[] patrolWaypoints;
        [SerializeField] private float arrivalDistance = 0.5f;

        [Header("Hearing Settings")]
        [SerializeField] private float hearingRadius = 12f;
        [Tooltip("Caminar con sigilo (0.3) o sobre alfombra no llega; caminar normal (0.8) o una duela floja (1.0) sí.")]
        [SerializeField] private float minNoiseThreshold = 0.5f;
        [SerializeField] private float listeningDuration = 1.2f;

        [Header("Vision Settings")]
        [SerializeField] private float visionRange = 6.5f;
        [SerializeField] private float visionHeight = 2.2f;
        [SerializeField] private Transform lanternOrigin;

        [Header("Capture")]
        [SerializeField] private Vector2 catchDistance = new Vector2(1.1f, 1.8f);
        [SerializeField] private float catchCooldown = 2f;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip alertSfx;
        [SerializeField] private AudioClip stompSfx;
        [SerializeField] private AudioSource audioSource;

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("El arte mira a la derecha; si mirara a la izquierda, desmarcar.")]
        [SerializeField] private bool spriteFacesRight = true;
        [SerializeField] private GameObject alertIcon;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite walkSprite;
        [SerializeField] private Sprite listenSprite;
        [SerializeField] private Sprite chargeSprite;
        [SerializeField] private Sprite stunnedSprite;

        [Header("Status")]
        [SerializeField] private InquisitorState currentState = InquisitorState.Patrolling;

        private Rigidbody2D _rb;
        private int _currentWaypointIndex = 0;
        private Vector3 _targetNoisePosition;
        private float _stateTimer = 0f;
        private float _stompTimer = 0f;
        private float _lastCatchTime = -99f;
        private bool _facingRight = true;
        private Vector3 _startPosition;
        private PlayerController _player;
        private TransformationManager _playerForm;

        public InquisitorState CurrentState => currentState;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _startPosition = transform.position;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.8f;
            }
            if (alertIcon != null) alertIcon.SetActive(false);
        }

        private void OnEnable()
        {
            NoiseProducer.OnAnyNoise += HandleNoiseHeard;
        }

        private void OnDisable()
        {
            NoiseProducer.OnAnyNoise -= HandleNoiseHeard;
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
                if (_player != null) _playerForm = _player.GetComponent<TransformationManager>();
            }

            if (currentState != InquisitorState.Stunned)
            {
                CheckVision();
                CheckCatch();
            }

            switch (currentState)
            {
                case InquisitorState.Patrolling: UpdatePatrol(); break;
                case InquisitorState.Listening: UpdateListening(); break;
                case InquisitorState.Charging: UpdateCharging(); break;
                case InquisitorState.Searching: UpdateSearching(); break;
                case InquisitorState.Stunned: UpdateStunned(); break;
            }

            if (alertIcon != null)
            {
                alertIcon.SetActive(currentState == InquisitorState.Listening || currentState == InquisitorState.Charging);
            }
        }

        private bool PlayerHidden => _playerForm != null && _playerForm.IsCatForm && _playerForm.IsInHideout;

        private void CheckVision()
        {
            if (_player == null || PlayerHidden || currentState == InquisitorState.Charging) return;
            Vector2 d = _player.transform.position - transform.position;
            bool inFront = _facingRight ? d.x > 0f : d.x < 0f;
            if (inFront && Mathf.Abs(d.x) <= visionRange && Mathf.Abs(d.y) <= visionHeight)
            {
                _targetNoisePosition = _player.transform.position;
                PlayAlert();
                EnterChargeState();
            }
        }

        private void CheckCatch()
        {
            if (_player == null || PlayerHidden || Time.time - _lastCatchTime < catchCooldown) return;
            Vector2 d = _player.transform.position - transform.position;
            if (Mathf.Abs(d.x) > catchDistance.x || Mathf.Abs(d.y) > catchDistance.y) return;

            _lastCatchTime = Time.time;
            if (GameManager.Instance == null || !GameManager.Instance.LoseHeart(1, "Inquisidor")) return;
            if (GameManager.Instance.CurrentHearts > 0) CheckpointManager.Instance?.RespawnAtCheckpoint();
            ResetToStart();
        }

        private void ResetToStart()
        {
            transform.position = _startPosition;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            _currentWaypointIndex = 0;
            currentState = InquisitorState.Patrolling;
        }

        private void HandleNoiseHeard(Vector3 noiseOrigin, float intensity)
        {
            if (currentState == InquisitorState.Stunned) return;

            float dist = Vector2.Distance(transform.position, noiseOrigin);
            if (dist > hearingRadius || intensity < minNoiseThreshold) return;

            _targetNoisePosition = noiseOrigin;
            FaceDirection(noiseOrigin.x - transform.position.x);

            if (intensity > 0.9f || currentState == InquisitorState.Listening)
            {
                // Una duela floja o un segundo ruido: embestida inmediata
                if (currentState != InquisitorState.Charging) PlayAlert();
                EnterChargeState();
            }
            else if (currentState == InquisitorState.Patrolling || currentState == InquisitorState.Searching)
            {
                EnterListeningState();
            }
        }

        private void PlayAlert()
        {
            if (alertSfx != null && audioSource != null)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(alertSfx, 0.9f);
            }
        }

        private void EnterListeningState()
        {
            currentState = InquisitorState.Listening;
            _stateTimer = listeningDuration;
            if (_rb != null) _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            PlayAlert();
            SetSprite(listenSprite);
        }

        private void UpdateListening()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f) EnterChargeState();
        }

        private void EnterChargeState()
        {
            currentState = InquisitorState.Charging;
            SetSprite(chargeSprite);
        }

        private void UpdateCharging()
        {
            float dx = _targetNoisePosition.x - transform.position.x;
            FaceDirection(dx);

            if (_rb != null) _rb.linearVelocity = new Vector2(Mathf.Sign(dx) * chargeSpeed, _rb.linearVelocity.y);

            _stompTimer += Time.deltaTime;
            if (_stompTimer >= 0.28f)
            {
                _stompTimer = 0f;
                CameraFollow.Shake(0.05f, 0.12f);
                if (stompSfx != null && audioSource != null)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(stompSfx, 0.75f);
                }
            }

            if (Mathf.Abs(dx) < arrivalDistance)
            {
                currentState = InquisitorState.Searching;
                _stateTimer = 3.5f;
                if (_rb != null) _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            }
        }

        private void UpdateSearching()
        {
            _stateTimer -= Time.deltaTime;
            SetSprite(idleSprite);
            // Mira a ambos lados mientras busca
            if (Mathf.Repeat(_stateTimer, 1.6f) < Time.deltaTime) FaceDirection(_facingRight ? -1f : 1f);
            if (_stateTimer <= 0f) currentState = InquisitorState.Patrolling;
        }

        private void UpdatePatrol()
        {
            SetSprite(walkSprite);
            if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

            Transform targetWp = patrolWaypoints[_currentWaypointIndex];
            if (targetWp == null) return;

            float dx = targetWp.position.x - transform.position.x;
            FaceDirection(dx);

            if (_rb != null) _rb.linearVelocity = new Vector2(Mathf.Sign(dx) * patrolSpeed, _rb.linearVelocity.y);

            if (Mathf.Abs(dx) < arrivalDistance)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % patrolWaypoints.Length;
            }
        }

        private void UpdateStunned()
        {
            _stateTimer -= Time.deltaTime;
            if (_rb != null) _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            SetSprite(stunnedSprite);
            if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(Color.white, new Color(0.6f, 0.7f, 1f), Mathf.PingPong(Time.time * 2f, 1f));
            if (_stateTimer <= 0f)
            {
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
                currentState = InquisitorState.Patrolling;
            }
        }

        private void SetSprite(Sprite s)
        {
            if (spriteRenderer != null && s != null) spriteRenderer.sprite = s;
        }

        public void ApplyStun(float duration)
        {
            currentState = InquisitorState.Stunned;
            _stateTimer = duration;
        }

        #region ISpellTarget Implementation
        public bool IsAffectedBySpell => currentState == InquisitorState.Stunned;

        public void ApplyBlind(float seconds)
        {
            ApplyStun(seconds);
        }

        public void ApplyHypnotize(float seconds, int direction)
        {
            FaceDirection(direction);
            ApplyStun(seconds * 0.75f);
        }

        public void ApplySleep(float seconds)
        {
            ApplyStun(seconds);
        }
        #endregion

        /// <summary>Voltea solo el sprite y el farol (no la escala: el icono "!" no debe salir en espejo).</summary>
        private void FaceDirection(float directionX)
        {
            if (Mathf.Abs(directionX) <= 0.05f) return;
            _facingRight = directionX > 0f;
            if (spriteRenderer != null) spriteRenderer.flipX = _facingRight != spriteFacesRight;
            if (lanternOrigin != null)
            {
                Vector3 lp = lanternOrigin.localPosition;
                lp.x = Mathf.Abs(lp.x) * (_facingRight ? 1f : -1f);
                lanternOrigin.localPosition = lp;
                lanternOrigin.localRotation = Quaternion.Euler(0f, 0f, _facingRight ? -90f : 90f);
            }
        }
    }
}
