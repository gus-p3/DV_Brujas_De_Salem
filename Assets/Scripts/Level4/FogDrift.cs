using UnityEngine;

namespace Salem.Level4
{
    /// <summary>Banco de niebla que deriva de lado a lado y "respira" su opacidad.</summary>
    public class FogDrift : MonoBehaviour
    {
        [SerializeField] private float amplitude = 1.6f;
        [SerializeField] private float speed = 0.18f;
        [SerializeField] private float alphaPulse = 0.25f;

        private SpriteRenderer _sr;
        private Vector3 _localStart;
        private float _baseAlpha;
        private float _phase;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _localStart = transform.localPosition;
            _baseAlpha = _sr != null ? _sr.color.a : 1f;
            _phase = Random.value * 10f;
        }

        private void Update()
        {
            float t = Time.time * speed + _phase;
            transform.localPosition = _localStart + new Vector3(Mathf.Sin(t) * amplitude, Mathf.Sin(t * 0.7f) * 0.12f, 0f);
            if (_sr == null) return;
            Color c = _sr.color;
            c.a = _baseAlpha * (1f - alphaPulse * 0.5f + Mathf.Sin(t * 1.3f) * alphaPulse * 0.5f);
            _sr.color = c;
        }
    }
}
