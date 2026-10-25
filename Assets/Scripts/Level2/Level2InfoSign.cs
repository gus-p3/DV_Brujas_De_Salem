using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level2
{
    /// <summary>
    /// Letrero informativo propio del Nivel 2 (avisos de cofres en la calle, fachadas y sótanos).
    /// - Dibuja un panel de madera en World Space con el texto (contorno oscuro para leerse sobre el atardecer).
    /// - Se ve atenuado de lejos y aparece con fundido al acercarse la bruja, como los indicadores de puertas.
    /// - No toca WorldSign, ControlsMap ni TutorialManager: es un componente independiente por composición.
    /// </summary>
    public class Level2InfoSign : MonoBehaviour
    {
        [TextArea(2, 4)]
        [SerializeField] private string message = "Aquí dentro hay un cofre";
        [SerializeField] private Vector2 panelSize = new Vector2(1.6f, 0.62f);
        [SerializeField] private float fontSize = 0.2f;
        [SerializeField] private float revealRadius = 4.5f;
        [SerializeField] private float idleAlpha = 0.35f;
        [SerializeField] private float fadeSpeed = 4f;

        private const string CanvasName = "InfoSign_Canvas";
        // El canvas trabaja en píxeles de 0.01 u para que el texto TMP se rasterice nítido
        private const float PixelsPerUnit = 100f;

        private CanvasGroup canvasGroup;
        private Transform player;

        public string Message => message;
        public Vector2 PanelSize => panelSize;
        public float CurrentAlpha => canvasGroup != null ? canvasGroup.alpha : 0f;

        public void Configure(string text, Vector2 size, float textSize = 0.2f)
        {
            message = text;
            panelSize = size;
            fontSize = textSize;
            Build();
        }

        private void Awake()
        {
            Build();
            if (Application.isPlaying && canvasGroup != null) canvasGroup.alpha = idleAlpha;
        }

        /// <summary>Crea o actualiza el panel de madera con su texto (reutilizable desde herramientas de Editor).</summary>
        public void Build()
        {
            Transform existing = transform.Find(CanvasName);
            GameObject canvasGO = existing != null ? existing.gameObject : new GameObject(CanvasName, typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = Vector3.zero;
            canvasGO.transform.localScale = Vector3.one / PixelsPerUnit;

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            // Delante de fachadas (-4..-1) y detrás de la bruja y los NPC (≥1): nunca tapa al personaje
            canvas.sortingLayerName = "Gameplay";
            canvas.sortingOrder = 0;
            ((RectTransform)canvasGO.transform).sizeDelta = panelSize * PixelsPerUnit;

            canvasGroup = canvasGO.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Transform bgTr = canvasGO.transform.Find("Panel");
            GameObject bg = bgTr != null ? bgTr.gameObject : new GameObject("Panel", typeof(RectTransform));
            bg.transform.SetParent(canvasGO.transform, false);
            Stretch((RectTransform)bg.transform, Vector2.zero);
            Image img = bg.GetComponent<Image>();
            if (img == null) img = bg.AddComponent<Image>();
            // Tablón de madera oscura semitransparente con borde dorado, mismo estilo que los indicadores de puerta
            img.color = new Color(0.24f, 0.15f, 0.08f, 0.9f);
            img.raycastTarget = false;
            Outline outline = bg.GetComponent<Outline>();
            if (outline == null) outline = bg.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.75f, 0.28f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            Transform txtTr = bg.transform.Find("Text");
            GameObject txt = txtTr != null ? txtTr.gameObject : new GameObject("Text", typeof(RectTransform));
            txt.transform.SetParent(bg.transform, false);
            Stretch((RectTransform)txt.transform, new Vector2(6f, 4f));
            TextMeshProUGUI tmp = txt.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            // Reduce la letra hasta un 70 % si el texto no cabe en el tablón
            tmp.fontSize = fontSize * PixelsPerUnit;
            tmp.fontSizeMax = fontSize * PixelsPerUnit;
            tmp.fontSizeMin = fontSize * PixelsPerUnit * 0.7f;
            tmp.enableAutoSizing = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.93f, 0.74f, 1f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineColor = new Color32(30, 16, 6, 255);
            tmp.outlineWidth = 0.18f;
            tmp.raycastTarget = false;
        }

        private static void Stretch(RectTransform rt, Vector2 padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = padding;
            rt.offsetMax = -padding;
        }

        private void Update()
        {
            if (canvasGroup == null) return;
            if (player == null)
            {
                PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
                if (pc == null) return;
                player = pc.transform;
            }

            float dist = Vector2.Distance(player.position, transform.position);
            float target = dist <= revealRadius ? 1f : idleAlpha;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
        }
    }
}
