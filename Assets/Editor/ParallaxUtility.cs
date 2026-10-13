using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidad del Editor para preparar capas de paralaje con 3 copias contiguas
/// (izquierda, centro y derecha) para garantizar un scroll infinito sin cortes.
/// </summary>
public static class ParallaxUtility
{
    [MenuItem("Brujas/3. Nivel/Generar 3 Copias de Parallax en Objeto Seleccionado")]
    public static void GenerateCopiesForSelection()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("[ParallaxUtility] Selecciona un GameObject con SpriteRenderer para generar las copias.");
            return;
        }

        SpriteRenderer sr = selected.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("[ParallaxUtility] El GameObject seleccionado debe tener un SpriteRenderer con un Sprite asignado.");
            return;
        }

        SetupThreeCopies(selected, sr.sprite, sr.sortingLayerName, sr.sortingOrder);
    }

    /// <summary>
    /// Crea las 3 copias hijas contiguas en local X (-width, 0, +width) y elimina el renderer de la raíz.
    /// </summary>
    public static void SetupThreeCopies(GameObject parent, Sprite sprite, string sortingLayerName, int sortingOrder)
    {
        float width = sprite.rect.width / sprite.pixelsPerUnit;

        // Limpiar hijos previos si los hay
        while (parent.transform.childCount > 0)
        {
            Object.DestroyImmediate(parent.transform.GetChild(0).gameObject);
        }

        // Eliminar SpriteRenderer del padre para evitar duplicación en el centro
        SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
        if (parentSr != null)
        {
            Object.DestroyImmediate(parentSr);
        }

        CreateChildCopy(parent, "Copy_Left", sprite, new Vector3(-width, 0f, 0f), sortingLayerName, sortingOrder);
        CreateChildCopy(parent, "Copy_Center", sprite, Vector3.zero, sortingLayerName, sortingOrder);
        CreateChildCopy(parent, "Copy_Right", sprite, new Vector3(width, 0f, 0f), sortingLayerName, sortingOrder);

        ParallaxLayer pl = parent.GetComponent<ParallaxLayer>();
        if (pl == null)
        {
            pl = parent.AddComponent<ParallaxLayer>();
        }
        pl.CalculateSpriteWidth();

        EditorUtility.SetDirty(parent);
        Debug.Log($"[ParallaxUtility] Capa '{parent.name}' configurada con 3 copias contiguas (ancho = {width:F2} unidades).");
    }

    private static void CreateChildCopy(GameObject parent, string name, Sprite sprite, Vector3 localPos, string sortingLayer, int order)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = localPos;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
    }
}
