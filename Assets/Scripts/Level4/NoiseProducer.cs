using System;
using UnityEngine;

namespace Salem.Level4
{
    public class NoiseProducer : MonoBehaviour
    {
        public static event Action<Vector3, float> OnAnyNoise;

        [Header("Footstep Noise Settings")]
        [SerializeField] private float walkNoise = 0.3f;
        [SerializeField] private float runNoise = 0.8f;
        [SerializeField] private float jumpLandingNoise = 0.9f;
        [SerializeField] private float stepRate = 0.35f;

        [Header("Status")]
        [SerializeField] private bool isOnCarpet = false;
        [SerializeField] private float currentNoiseLevel = 0f;

        private Rigidbody2D _rb;
        private PlayerController _player;
        private TransformationManager _transformManager;
        private float _stepTimer = 0f;
        private bool _wasGrounded = true;

        public bool IsOnCarpet => isOnCarpet;
        public float CurrentNoiseLevel => currentNoiseLevel;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _player = GetComponent<PlayerController>();
            _transformManager = GetComponent<TransformationManager>();
        }

        public void SetOnCarpet(bool onCarpet)
        {
            isOnCarpet = onCarpet;
        }

        public void EmitImpulseNoise(float intensity)
        {
            currentNoiseLevel = Mathf.Max(currentNoiseLevel, intensity);
            OnAnyNoise?.Invoke(transform.position, intensity);
        }

        private void Update()
        {
            currentNoiseLevel = Mathf.MoveTowards(currentNoiseLevel, 0f, Time.deltaTime * 1.5f);

            bool isCat = _transformManager != null && _transformManager.IsCatForm;
            bool isFlying = _player != null && _player.IsFlying;

            // Cats and flying broom are completely silent
            if (isCat || isFlying)
            {
                return;
            }

            if (_rb == null) return;

            // Landing noise
            bool isGrounded = Mathf.Abs(_rb.linearVelocity.y) < 0.05f;
            if (isGrounded && !_wasGrounded)
            {
                float landingIntensity = jumpLandingNoise * (isOnCarpet ? 0.2f : 1f);
                EmitImpulseNoise(landingIntensity);
            }
            _wasGrounded = isGrounded;

            // Running / walking footsteps
            float speed = Mathf.Abs(_rb.linearVelocity.x);
            if (isGrounded && speed > 0.5f)
            {
                _stepTimer += Time.deltaTime;
                if (_stepTimer >= stepRate)
                {
                    _stepTimer = 0f;
                    float baseNoise = speed > 4f ? runNoise : walkNoise;
                    if (isOnCarpet) baseNoise *= 0.15f;
                    EmitImpulseNoise(baseNoise);
                }
            }
            else
            {
                _stepTimer = stepRate * 0.8f;
            }
        }
    }
}
