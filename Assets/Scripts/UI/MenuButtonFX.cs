using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Botón de menú solo con texto (estilo Hollow Knight): al pasar el ratón o seleccionarlo con el teclado
/// el texto se ilumina en dorado, crece un poco y aparecen dos adornos a los lados.
/// Si el botón no es interactuable se muestra apagado.
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Color normalColor = new Color(0.93f, 0.88f, 0.78f, 1f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.82f, 0.42f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.6f, 0.56f, 0.5f, 0.45f);
    [SerializeField] private float highlightScale = 1.08f;
    [SerializeField] private float speed = 12f;

    private Button button;
    private TMP_Text label;
    private RectTransform left, right;
    private Image leftImg, rightImg;
    private bool hovered;
    private float t;
    private static Sprite ornament;

    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
        left = CreateOrnament("OrnamentLeft", out leftImg, 1f);
        right = CreateOrnament("OrnamentRight", out rightImg, -1f);
        // La selección la dibuja este componente, no la transición del botón
        button.transition = Selectable.Transition.None;
    }

    private RectTransform CreateOrnament(string objName, out Image img, float side)
    {
        Transform existing = transform.Find(objName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        img = go.GetComponent<Image>();
        img.sprite = Ornament();
        img.raycastTarget = false;
        img.color = new Color(1f, 0.82f, 0.42f, 0f);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(90f, 24f);
        rt.localScale = new Vector3(side, 1f, 1f);
        return rt;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hovered = true;
        if (button.interactable && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData e) => hovered = false;
    public void OnSelect(BaseEventData e) => hovered = true;
    public void OnDeselect(BaseEventData e) => hovered = false;

    private void OnDisable()
    {
        hovered = false;
        t = 0f;
        Apply();
    }

    private void Update()
    {
        bool active = button.interactable && (hovered || (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject));
        t = Mathf.MoveTowards(t, active ? 1f : 0f, Time.unscaledDeltaTime * speed);
        Apply();
    }

    private void Apply()
    {
        if (label == null) return;
        float e = t * t * (3f - 2f * t);
        label.color = !button.interactable ? disabledColor : Color.Lerp(normalColor, highlightColor, e);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, highlightScale, e);

        // Los adornos se colocan junto al texto y se deslizan hacia fuera al aparecer
        float half = label.preferredWidth * 0.5f + 34f;
        float drift = Mathf.Lerp(18f, 0f, e);
        if (left != null)
        {
            left.anchoredPosition = new Vector2(-(half + 45f + drift), 0f);
            leftImg.color = new Color(1f, 0.82f, 0.42f, e);
        }
        if (right != null)
        {
            right.anchoredPosition = new Vector2(half + 45f + drift, 0f);
            rightImg.color = new Color(1f, 0.82f, 0.42f, e);
        }
    }

    /// <summary>Adorno generado: una línea que se afina hacia fuera con un rombo en el extremo interior.</summary>
    private static Sprite Ornament()
    {
        if (ornament != null) return ornament;
        const int w = 128, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float cy = Mathf.Abs(y + 0.5f - h / 2f);
            // Línea: más gruesa cerca del rombo (derecha) y fina hacia la izquierda
            float thickness = Mathf.Lerp(0.6f, 2.2f, x / (float)w);
            float line = Mathf.Clamp01(thickness - cy + 0.5f) * Mathf.Clamp01(x / 12f);
            // Rombo en el extremo derecho
            float dx = Mathf.Abs(x + 0.5f - (w - 14f));
            float diamond = Mathf.Clamp01(9f - (dx + cy) + 0.5f);
            float a = Mathf.Clamp01(Mathf.Max(line, diamond));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        ornament = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return ornament;
    }
}
