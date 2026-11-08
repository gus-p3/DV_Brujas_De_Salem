using UnityEngine;

namespace Salem.Level4
{
    [RequireComponent(typeof(Collider2D))]
    public class CreakyFloorTile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float noiseIntensity = 1.0f;
        [SerializeField] private float cooldown = 0.5f;
        [SerializeField] private AudioClip creakSfx;

        private float _lastTriggerTime = -99f;
        private AudioSource _audioSource;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0.5f;
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time - _lastTriggerTime < cooldown) return;

            if (other.CompareTag("Player"))
            {
                var tm = other.GetComponent<TransformationManager>();
                // Only human form triggers loud creaks (cats step lightly)
                bool isCat = tm != null && tm.IsCatForm;
                var pc = other.GetComponent<PlayerController>();
                bool isFlying = pc != null && pc.IsFlying;

                if (!isCat && !isFlying)
                {
                    var rb = other.GetComponent<Rigidbody2D>();
                    if (rb != null && rb.linearVelocity.sqrMagnitude > 0.05f)
                    {
                        TriggerCreak(other.gameObject);
                    }
                }
            }
        }

        public void TriggerCreak(GameObject playerObj)
        {
            _lastTriggerTime = Time.time;

            if (creakSfx != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.85f, 1.15f);
                _audioSource.PlayOneShot(creakSfx, 0.9f);
            }

            var noise = playerObj.GetComponent<NoiseProducer>();
            if (noise != null)
            {
                noise.EmitImpulseNoise(noiseIntensity);
            }
        }
    }
}
