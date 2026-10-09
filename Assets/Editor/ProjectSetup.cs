using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura el proyecto para "Brujas de Salem": crea los Layers (Ground, Player, Enemy, Hideout,
/// Collectible), confirma el Tag "Player" y ajusta la matriz de colisiones de Physics2D.
/// Es idempotente: se puede ejecutar varias veces sin duplicar nada.
/// </summary>
public static class ProjectSetup
{
    public const string PlayerTag = "Player";
    public const string GroundLayer = "Ground";
    public const string PlayerLayer = "Player";
    public const string EnemyLayer = "Enemy";
    public const string HideoutLayer = "Hideout";
    public const string CollectibleLayer = "Collectible";

    private const int FirstUserLayerIndex = 6;
    private const int TotalLayers = 32;
    private const string TagManagerPath = "ProjectSettings/TagManager.asset";

    private static readonly string[] RequiredLayers =
    {
        GroundLayer, PlayerLayer, EnemyLayer, HideoutLayer, CollectibleLayer
    };

    [MenuItem("Brujas/Proyecto/Configurar layers, tag y físicas")]
    public static void Configure()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(TagManagerPath)[0]);

        EnsureLayers(tagManager);
        EnsureTag(tagManager, PlayerTag);
        tagManager.ApplyModifiedProperties();

        ConfigureCollisionMatrix();
        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectSetup] Layers, tag Player y matriz de colisiones 2D configurados.");
    }

    private static void EnsureLayers(SerializedObject tagManager)
    {
        SerializedProperty layers = tagManager.FindProperty("layers");

        foreach (string layerName in RequiredLayers)
        {
            if (LayerExists(layers, layerName))
            {
                continue;
            }

            for (int i = FirstUserLayerIndex; i < TotalLayers; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    break;
                }
            }
        }
    }

    private static bool LayerExists(SerializedProperty layers, string layerName)
    {
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
            {
                return true;
            }
        }
        return false;
    }

    private static void EnsureTag(SerializedObject tagManager, string tag)
    {
        // "Player" es un tag integrado de Unity; solo se agrega si por algún motivo no existe
        foreach (string existing in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (existing == tag)
            {
                return;
            }
        }

        SerializedProperty tags = tagManager.FindProperty("tags");
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
    }

    /// <summary>
    /// La bruja choca solo con el suelo. Enemigos, escondites y coleccionables se resuelven por
    /// distancia, raycasts o triggers, no por colisión física.
    /// </summary>
    private static void ConfigureCollisionMatrix()
    {
        int player = LayerMask.NameToLayer(PlayerLayer);
        int enemy = LayerMask.NameToLayer(EnemyLayer);
        int hideout = LayerMask.NameToLayer(HideoutLayer);
        int collectible = LayerMask.NameToLayer(CollectibleLayer);

        int[] nonPhysical = { enemy, hideout, collectible };
        foreach (int layer in nonPhysical)
        {
            if (layer < 0 || player < 0)
            {
                continue;
            }
            Physics2D.IgnoreLayerCollision(player, layer, true);
        }

        // Entre sí, estas tres capas tampoco necesitan colisionar
        for (int i = 0; i < nonPhysical.Length; i++)
        {
            for (int j = i + 1; j < nonPhysical.Length; j++)
            {
                if (nonPhysical[i] >= 0 && nonPhysical[j] >= 0)
                {
                    Physics2D.IgnoreLayerCollision(nonPhysical[i], nonPhysical[j], true);
                }
            }
        }
    }
}
