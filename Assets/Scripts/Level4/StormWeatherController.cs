using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Salem.Level4
{
    /// <summary>
    /// Tormenta del Panteón: lluvia que sigue a la cámara, relámpagos (rayo dentado + destello de pantalla
    /// + pulso de la luz global 2D) y temblor de cámara con el trueno.
    /// </summary>
    public class StormWeatherController : MonoBehaviour
    {
        [Header("Lightning Settings")]
        [SerializeField] private SpriteRenderer lightningFlashOverlay;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Color normalAmbientColor = Color.white;
        [SerializeField] private Color lightningColor = new Color(0.85f, 1f, 0.92f, 0.55f);
        [SerializeField] private float minLightningInterval = 4.0f;
        [SerializeField] private float maxLightningInterval = 9.0f;

        [Header("Luz 2D")]
        [SerializeField] private Light2D globalLight;
        [SerializeField] private float flashIntensityBoost = 1.4f;
        [SerializeField] private Light2D boltLight;

        [Header("Rayo")]
        [SerializeField] private LineRenderer boltLine;
        [SerializeField] private float boltTopY = 13f;
        [SerializeField] private float boltBottomY = 3f;

        [Header("Rain Settings")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private Vector3 rainOffset = new Vector3(0f, 8f, 0f);

        [Header("Audio")]
        [SerializeField] private AudioClip thunderSfx;

        private Transform _cam;
        private float _baseGlobalIntensity = 1f;

        private void Start()
        {
            if (lightningFlashOverlay != null) lightningFlashOverlay.color = new Color(1f, 1f, 1f, 0f);
            if (backgroundRenderer != null) backgroundRenderer.color = normalAmbientColor;
            if (globalLight != null) _baseGlobalIntensity = globalLight.intensity;
            if (boltLine != null) boltLine.enabled = false;
            if (boltLight != null) boltLight.intensity = 0f;
            StartCoroutine(LightningCycle());
        }

        private void OnDisable()
        {
            // Al entrar a la iglesia el patio se desactiva: no dejar la luz global "encendida" por un destello
            if (globalLight != null && _baseGlobalIntensity > 0f) globalLight.intensity = _baseGlobalIntensity;
        }

        private void LateUpdate()
        {
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam == null) return;
            Vector3 c = new Vector3(_cam.position.x, _cam.position.y, 0f);
            if (rainParticles != null) rainParticles.transform.position = c + rainOffset;
            if (lightningFlashOverlay != null) lightningFlashOverlay.transform.position = c;
        }

        private IEnumerator LightningCycle()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minLightningInterval, maxLightningInterval));
                yield return FlashLightning(0.22f, true);
                if (Random.value > 0.4f)
                {
                    yield return new WaitForSeconds(0.09f);
                    yield return FlashLightning(0.14f, false);
                }
            }
        }

        private IEnumerator FlashLightning(float duration, bool thunder)
        {
            if (boltLine != null && _cam != null) DrawBolt(_cam.position.x + Random.Range(-8f, 8f));
            if (thunder)
            {
                CameraFollow.Shake(0.14f, 0.35f);
                if (thunderSfx != null) AudioManager.Instance?.PlaySFX(thunderSfx);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(elapsed / duration);
                if (lightningFlashOverlay != null)
                {
                    Color c = lightningColor;
                    c.a = lightningColor.a * k;
                    lightningFlashOverlay.color = c;
                }
                if (backgroundRenderer != null) backgroundRenderer.color = Color.Lerp(normalAmbientColor, Color.white, k);
                if (globalLight != null) globalLight.intensity = _baseGlobalIntensity + flashIntensityBoost * k;
                if (boltLight != null) boltLight.intensity = 3f * k;
                yield return null;
            }

            if (lightningFlashOverlay != null) lightningFlashOverlay.color = new Color(1f, 1f, 1f, 0f);
            if (backgroundRenderer != null) backgroundRenderer.color = normalAmbientColor;
            if (globalLight != null) globalLight.intensity = _baseGlobalIntensity;
            if (boltLight != null) boltLight.intensity = 0f;
            if (boltLine != null) boltLine.enabled = false;
        }

        /// <summary>Rayo dentado con una rama, generado al azar en cada destello.</summary>
        private void DrawBolt(float x)
        {
            const int segments = 9;
            boltLine.positionCount = segments + 1;
            float y = boltTopY;
            float step = (boltTopY - boltBottomY) / segments;
            for (int i = 0; i <= segments; i++)
            {
                boltLine.SetPosition(i, new Vector3(x, y, 0f));
                x += Random.Range(-0.9f, 0.9f);
                y -= step * Random.Range(0.8f, 1.2f);
            }
            boltLine.enabled = true;
            if (boltLight != null) boltLight.transform.position = boltLine.GetPosition(segments / 2);
        }
    }
}
