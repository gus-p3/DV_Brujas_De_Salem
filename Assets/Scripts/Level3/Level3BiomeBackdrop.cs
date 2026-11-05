using System;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Funde los fondos del Nivel 3 según la X de la cámara para que el bosque no se vea repetitivo.
    /// Cada zona (p. ej. "Bosque Dorado", "Bosque Muerto", "Claro de la Cabaña", "Aldea") tiene su propio
    /// conjunto de capas de paralaje y un tramo de entrada y de salida donde su opacidad sube o baja.
    /// </summary>
    public class Level3BiomeBackdrop : MonoBehaviour
    {
        [Serializable]
        public class BackdropZone
        {
            public string name = "Zona";
            public SpriteRenderer[] renderers = new SpriteRenderer[0];

            [Tooltip("Si es falso, la zona está visible desde el inicio del nivel.")]
            public bool fadeIn = true;
            public float fadeInStartX;
            public float fadeInEndX;

            [Tooltip("Si es falso, la zona sigue visible hasta el final del nivel.")]
            public bool fadeOut = true;
            public float fadeOutStartX;
            public float fadeOutEndX;

            public float Weight(float x)
            {
                float a = fadeIn ? Mathf.InverseLerp(fadeInStartX, fadeInEndX, x) : 1f;
                float b = fadeOut ? 1f - Mathf.InverseLerp(fadeOutStartX, fadeOutEndX, x) : 1f;
                return Mathf.SmoothStep(0f, 1f, Mathf.Min(a, b));
            }
        }

        [SerializeField] private BackdropZone[] zones = new BackdropZone[0];

        private float[][] baseAlpha;
        private Transform cam;

        public BackdropZone[] Zones => zones;

        /// <summary>Compatibilidad: 2 zonas (bosque que se desvanece y aldea que aparece entre startX y endX).</summary>
        public void Configure(SpriteRenderer[] forest, SpriteRenderer[] village, float startX, float endX)
        {
            zones = new[]
            {
                new BackdropZone { name = "Bosque", renderers = forest, fadeIn = false, fadeOutStartX = startX, fadeOutEndX = endX },
                new BackdropZone { name = "Aldea", renderers = village, fadeInStartX = startX, fadeInEndX = endX, fadeOut = false },
            };
        }

        public void ConfigureZones(BackdropZone[] newZones)
        {
            zones = newZones ?? new BackdropZone[0];
        }

        private void Awake()
        {
            baseAlpha = new float[zones.Length][];
            for (int z = 0; z < zones.Length; z++)
            {
                SpriteRenderer[] list = zones[z].renderers ?? new SpriteRenderer[0];
                baseAlpha[z] = new float[list.Length];
                for (int i = 0; i < list.Length; i++) baseAlpha[z][i] = list[i] != null ? list[i].color.a : 1f;
            }
        }

        private void LateUpdate()
        {
            if (cam == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                cam = main.transform;
            }

            float x = cam.position.x;
            for (int z = 0; z < zones.Length; z++)
            {
                float k = zones[z].Weight(x);
                SpriteRenderer[] list = zones[z].renderers;
                if (list == null) continue;
                for (int i = 0; i < list.Length; i++)
                {
                    SpriteRenderer sr = list[i];
                    if (sr == null) continue;
                    Color c = sr.color;
                    c.a = baseAlpha[z][i] * k;
                    sr.color = c;
                    sr.enabled = c.a > 0.003f;
                }
            }
        }
    }
}
