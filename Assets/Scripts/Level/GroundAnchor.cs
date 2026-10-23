using UnityEngine;

/// <summary>
/// Mantiene la base visible de un objeto (el píxel opaco más bajo de su sprite) apoyada en la superficie
/// física que tiene debajo, con un ligero hundimiento en el pasto. Sustituye a los ajustes de altura
/// fijos por objeto: funciona con cualquier sprite y en cualquier nivel.
/// Se ancla en Awake (antes que los scripts que guardan su posición inicial) y desde el editor
/// con Tools > Nivel > Anclar objetos al suelo.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class GroundAnchor : MonoBehaviour
{
    [Tooltip("Cuánto se hunde la base en el suelo (u). Un poco de hundimiento hace que el pasto la cubra.")]
    [SerializeField] private float sink = 0.04f;
    [Tooltip("Capas que cuentan como suelo.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Distancia máxima hacia arriba y hacia abajo en la que se busca el suelo (u).")]
    [SerializeField] private float searchDistance = 2.5f;
    [Tooltip("Sprite cuya base se apoya. Si está vacío se usa el más grande del objeto.")]
    [SerializeField] private SpriteRenderer target;

    public float Sink
    {
        get => sink;
        set => sink = value;
    }

    private void Awake()
    {
        if (Application.isPlaying) Snap();
    }

    /// <summary>Recoloca el objeto en Y para que su base visible quede sobre el suelo. Devuelve si lo encontró.</summary>
    public bool Snap()
    {
        SpriteRenderer sr = ResolveTarget();
        if (sr == null) return false;
        // En el editor los colliders movidos no actualizan sus límites hasta sincronizar la física
        if (!Application.isPlaying) Physics2D.SyncTransforms();
        int mask = groundMask.value != 0 ? groundMask.value : LayerMask.GetMask("Ground");

        float bottom = VisualBottom(sr, out float centerX);
        // Superficie más cercana a la base (arriba o abajo): así un objeto dentro de un túnel se apoya en el
        // suelo interior y no en el techo, y uno semienterrado sube hasta la superficie que le corresponde
        float best = float.NaN;
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(centerX, bottom + searchDistance), Vector2.down, searchDistance * 2f, mask);
        foreach (RaycastHit2D hit in hits)
        {
            // Se ignora el propio collider (p. ej. la plataforma que lleva este componente)
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform.IsChildOf(transform)) continue;
            // Si el rayo empieza dentro del collider, su superficie es el borde superior
            float surface = hit.distance <= 0f ? hit.collider.bounds.max.y : hit.point.y;
            if (float.IsNaN(best) || Mathf.Abs(surface - bottom) < Mathf.Abs(best - bottom)) best = surface;
        }
        if (float.IsNaN(best)) return false;

        Vector3 p = transform.position;
        p.y += best - sink - bottom;
        transform.position = p;
        return true;
    }

    private SpriteRenderer ResolveTarget()
    {
        if (target != null) return target;
        SpriteRenderer best = null;
        float bestArea = -1f;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr.sprite == null) continue;
            float area = sr.bounds.size.x * sr.bounds.size.y;
            if (area > bestArea)
            {
                bestArea = area;
                best = sr;
            }
        }
        return best;
    }

    /// <summary>Base visible del sprite en mundo: vértice más bajo de su malla ajustada (ignora el margen transparente).</summary>
    public static float VisualBottom(SpriteRenderer sr, out float centerX)
    {
        Vector2[] verts = sr.sprite.vertices;
        Matrix4x4 m = sr.transform.localToWorldMatrix;
        float bottom = float.MaxValue, minX = float.MaxValue, maxX = float.MinValue;
        foreach (Vector2 v in verts)
        {
            Vector3 w = m.MultiplyPoint3x4(new Vector3(sr.flipX ? -v.x : v.x, sr.flipY ? -v.y : v.y, 0f));
            bottom = Mathf.Min(bottom, w.y);
            minX = Mathf.Min(minX, w.x);
            maxX = Mathf.Max(maxX, w.x);
        }
        if (verts.Length == 0)
        {
            centerX = sr.bounds.center.x;
            return sr.bounds.min.y;
        }
        centerX = (minX + maxX) * 0.5f;
        return bottom;
    }
}
