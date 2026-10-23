using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level2
{
    /// <summary>
    /// Parpadeo sutil de llama para las farolas del mercado: varía la intensidad y el radio de su Light2D con ruido
    /// Perlin (sin saltos bruscos). Cada farola arranca en un punto distinto del ruido para que no parpadeen a la vez.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class LanternFlicker : MonoBehaviour
    {
        [Tooltip("Variación máxima de intensidad respecto a la base (fracción).")]
        [SerializeField, Range(0f, 0.5f)] private float intensityVariation = 0.12f;
        [Tooltip("Variación máxima del radio exterior respecto a la base (fracción).")]
        [SerializeField, Range(0f, 0.3f)] private float radiusVariation = 0.04f;
        [Tooltip("Velocidad del parpadeo.")]
        [SerializeField] private float speed = 2.2f;

        private Light2D light2D;
        private float baseIntensity;
        private float baseRadius;
        private float seed;

        private void Awake()
        {
            light2D = GetComponent<Light2D>();
            baseIntensity = light2D.intensity;
            baseRadius = light2D.pointLightOuterRadius;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float t = Time.time * speed + seed;
            // Dos octavas: oscilación lenta de la llama y temblor rápido más pequeño
            float n = (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f * 0.75f + (Mathf.PerlinNoise(t * 3.1f, seed + 7f) - 0.5f) * 2f * 0.25f;
            light2D.intensity = baseIntensity * (1f + n * intensityVariation);
            light2D.pointLightOuterRadius = baseRadius * (1f + n * radiusVariation);
        }

        private void OnDisable()
        {
            if (light2D == null) return;
            light2D.intensity = baseIntensity;
            light2D.pointLightOuterRadius = baseRadius;
        }
    }
}
