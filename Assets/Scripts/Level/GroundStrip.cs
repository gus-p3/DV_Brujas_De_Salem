using UnityEngine;

/// <summary>
/// Fábrica de suelo: genera el arte de un tramo de suelo a partir de su BoxCollider2D.
/// - "Visual": la baldosa en mosaico, alineada para que la línea de pasto denso coincida con la superficie física.
/// - "Fringe": la misma franja de pasto dibujada delante de los personajes, para que los pies queden dentro del pasto.
/// Ambas capas comparten el mismo mosaico, así que la franja solo se nota encima de los personajes.
/// Se reconstruye con Tools > Nivel > Reconstruir suelo (o el menú contextual del componente).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class GroundStrip : MonoBehaviour
{
    [Header("Arte")]
    [SerializeField] private Sprite tile;
    [SerializeField] private Sprite fringe;

    [Header("Alineación (u, medidas desde el borde superior de cada sprite)")]
    [Tooltip("Distancia desde el borde superior de la baldosa hasta la línea donde se apoyan los pies.")]
    [SerializeField] private float surfaceFromTop = 1.11f;
    [Tooltip("Distancia desde el borde superior de la baldosa hasta el borde superior de la franja delantera.")]
    [SerializeField] private float fringeFromTop = 0.625f;
    [Tooltip("Margen extra a cada lado del collider (u) para que el arte cubra los bordes.")]
    [SerializeField] private float sideMargin = 0.15f;

    [Header("Capas")]
    [SerializeField] private string visualLayer = "Midground";
    [SerializeField] private int visualOrder = 0;
    [SerializeField] private string fringeLayer = "Foreground";
    [SerializeField] private int fringeOrder = 1;

    public void Configure(Sprite tileSprite, Sprite fringeSprite)
    {
        tile = tileSprite;
        fringe = fringeSprite;
    }

    [ContextMenu("Reconstruir arte del suelo")]
    public void Rebuild()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (tile == null || col == null) return;

        Bounds b = col.bounds;
        float width = b.size.x + sideMargin * 2f;
        float tileHeight = tile.bounds.size.y;
        float tileTop = b.max.y + surfaceFromTop;

        SpriteRenderer visual = EnsureChild("Visual");
        Place(visual, tile, width, tileHeight, new Vector3(b.center.x, tileTop - tileHeight * 0.5f, 0f), visualLayer, visualOrder);

        if (fringe != null)
        {
            float fringeHeight = fringe.bounds.size.y;
            SpriteRenderer front = EnsureChild("Fringe");
            Place(front, fringe, width, fringeHeight, new Vector3(b.center.x, tileTop - fringeFromTop - fringeHeight * 0.5f, 0f), fringeLayer, fringeOrder);
        }
    }

    private SpriteRenderer EnsureChild(string childName)
    {
        Transform t = transform.Find(childName);
        if (t == null)
        {
            t = new GameObject(childName).transform;
            t.SetParent(transform, false);
        }
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
        return sr;
    }

    private void Place(SpriteRenderer sr, Sprite sprite, float width, float height, Vector3 worldCenter, string layer, int order)
    {
        Transform t = sr.transform;
        // El arte no hereda la escala del collider: se mide en unidades de mundo
        Vector3 parentScale = transform.lossyScale;
        t.localScale = new Vector3(1f / Mathf.Max(0.0001f, parentScale.x), 1f / Mathf.Max(0.0001f, parentScale.y), 1f);
        t.position = worldCenter;
        t.rotation = Quaternion.identity;
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, height);
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
    }
}
