using UnityEngine;

namespace Salem.Level4
{
    public class ClockworkGearPlatform : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 30f; // degrees per second
        [SerializeField] private bool clockwise = true;

        [Header("Movement Settings (Optional)")]
        [SerializeField] private bool movingPlatform = false;
        [SerializeField] private Vector3 moveOffset = new Vector3(3f, 0f, 0f);
        [SerializeField] private float moveSpeed = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip tickSfx;
        [SerializeField] private float tickInterval = 1.0f;

        private Vector3 _startPos;
        private float _tickTimer = 0f;
        private AudioSource _audioSource;

        private void Awake()
        {
            _startPos = transform.position;
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && tickSfx != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0.9f;
            }
        }

        private void Update()
        {
            // Rotate gear visual
            float dir = clockwise ? -1f : 1f;
            transform.Rotate(0f, 0f, dir * rotationSpeed * Time.deltaTime);

            // Optional linear movement
            if (movingPlatform)
            {
                float t = Mathf.PingPong(Time.time * moveSpeed, 1f);
                transform.position = Vector3.Lerp(_startPos, _startPos + moveOffset, t);
            }

            // Periodic tick SFX
            if (tickSfx != null && _audioSource != null)
            {
                _tickTimer += Time.deltaTime;
                if (_tickTimer >= tickInterval)
                {
                    _tickTimer = 0f;
                    _audioSource.pitch = Random.Range(0.95f, 1.05f);
                    _audioSource.PlayOneShot(tickSfx, 0.4f);
                }
            }
        }
    }
}
