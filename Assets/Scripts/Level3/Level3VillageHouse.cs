using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Casa cerrada de la Aldea de Salem (Acto 3) que solo se recorre infiltrándose como gato.
    /// - La puerta principal está atrancada (BarredHouseDoor); se entra por una ventana alta o una gatera.
    /// - Mientras la bruja está dentro, la fachada se vuelve casi transparente y deja ver el interior
    ///   (habitación, cama con el aldeano dormido y el cofre con la llave).
    /// - Dentro, la energía felina se gasta más despacio para poder moverse con sigilo sin prisas.
    /// - La primera vez que entra muestra cómo moverse sin despertar al aldeano.
    /// </summary>
    public class Level3VillageHouse : MonoBehaviour
    {
        [Header("Identificación")]
        [SerializeField] private string houseId = "village_house";
        [SerializeField] private string houseDisplayName = "Casa de la Aldea";

        [Header("Interior (rectángulo en coordenadas de mundo)")]
        [SerializeField] private Vector2 interiorMin;
        [SerializeField] private Vector2 interiorMax;

        [Header("Fachada")]
        [SerializeField] private SpriteRenderer[] facadeRenderers;
        [SerializeField] private float insideFacadeAlpha = 0.08f;
        [SerializeField] private float fadeSpeed = 4f;

        [Header("Sigilo")]
        [SerializeField] private float catEnergyDrainInside = 0.3f;
        [TextArea(2, 3)]
        [SerializeField] private string firstEntryHint =
            "Un aldeano duerme aquí. Muévete como gato manteniendo [Shift] (sigilo). Correr, saltar o volver a forma humana lo despertará.";

        private PlayerController player;
        private TransformationManager transformation;
        private bool hintShown;
        private float[] baseAlpha;
        private float facadeBlend = 1f;

        public string HouseId => houseId;
        public string HouseDisplayName => houseDisplayName;
        public bool PlayerInside { get; private set; }
        public Vector2 InteriorMin => interiorMin;
        public Vector2 InteriorMax => interiorMax;

        public void Configure(string id, string displayName, Vector2 min, Vector2 max, SpriteRenderer[] facade)
        {
            houseId = id;
            houseDisplayName = displayName;
            interiorMin = min;
            interiorMax = max;
            facadeRenderers = facade;
        }

        private void Awake()
        {
            CacheBaseAlpha();
        }

        private void CacheBaseAlpha()
        {
            if (facadeRenderers == null) facadeRenderers = new SpriteRenderer[0];
            baseAlpha = new float[facadeRenderers.Length];
            for (int i = 0; i < facadeRenderers.Length; i++)
            {
                baseAlpha[i] = facadeRenderers[i] != null ? facadeRenderers[i].color.a : 1f;
            }
        }

        /// <summary>¿El punto (en mundo) está dentro del interior de la casa?</summary>
        public bool ContainsPoint(Vector2 p)
        {
            return p.x >= interiorMin.x && p.x <= interiorMax.x && p.y >= interiorMin.y && p.y <= interiorMax.y;
        }

        private void Update()
        {
            if (player == null)
            {
                player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null) transformation = player.GetComponent<TransformationManager>();
            }

            bool inside = player != null && ContainsPoint((Vector2)player.transform.position + Vector2.up * 0.25f);
            if (inside != PlayerInside)
            {
                PlayerInside = inside;
                if (transformation != null)
                {
                    transformation.CatEnergyDrainMultiplier = inside ? catEnergyDrainInside : 1f;
                }

                if (inside && !hintShown && !string.IsNullOrEmpty(firstEntryHint))
                {
                    hintShown = true;
                    HUDController hud = Object.FindFirstObjectByType<HUDController>();
                    if (hud != null) hud.ShowMessage($"{houseDisplayName}: {firstEntryHint}", 4.2f);
                }
            }

            float target = PlayerInside ? 0f : 1f;
            facadeBlend = Mathf.MoveTowards(facadeBlend, target, fadeSpeed * Time.deltaTime);
            if (baseAlpha == null || baseAlpha.Length != facadeRenderers.Length) CacheBaseAlpha();
            for (int i = 0; i < facadeRenderers.Length; i++)
            {
                SpriteRenderer sr = facadeRenderers[i];
                if (sr == null) continue;
                Color c = sr.color;
                c.a = Mathf.Lerp(insideFacadeAlpha, baseAlpha[i], facadeBlend);
                sr.color = c;
            }
        }

        private void OnDisable()
        {
            if (PlayerInside && transformation != null) transformation.CatEnergyDrainMultiplier = 1f;
            PlayerInside = false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.5f);
            Vector2 c = (interiorMin + interiorMax) * 0.5f;
            Gizmos.DrawWireCube(c, interiorMax - interiorMin);
        }
    }
}
