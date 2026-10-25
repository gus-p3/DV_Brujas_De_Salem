using System.Linq;
using System.Text;
using Level2;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Textos del Nivel 2 que informan que ingredientes y pociones están dentro de cofres (calle, casas y sótanos).
/// Usa TutorialManager solo a través de TutorialTrigger, y componentes propios (Level2InfoSign, Level2ChestGuideHUD);
/// no edita ControlsMap, TutorialManager ni WorldSign. Tabla completa en Docs/Nivel2_Textos.md.
/// </summary>
public static class Level2ChestTextsBuilder
{
    public const string StartTipTitle = "Busca en los cofres";
    public const string StartTipBody = "Los ingredientes y las pociones están guardados dentro de cofres, tanto en las casas como en la calle. Acércate a un cofre y pulsa Q para abrirlo. Solo puedes abrirlos en forma humana.";
    public const string StreetSignText = "Los cofres de la calle guardan ingredientes y pociones. →";
    public const string HouseSignText = "Aquí dentro hay un cofre";
    public const string GateLockedText = "Aún te faltan ingredientes. Revisa los cofres de las casas y de la calle.";

    // Casas con cofre en su interior: letrero junto a la puerta de calle, sin tapar la puerta ni su aviso "Q: Entrar"
    public static readonly (string sign, string door, Vector2 pos)[] HouseSigns =
    {
        ("Sign_House1_Chest", "Door_Ext_House1_Ground", new Vector2(15.28f, 1.45f)),
        ("Sign_House2_Chest", "Door_Ext_House2_Ground", new Vector2(46.45f, 1.45f)),
        ("Sign_House3_Chest", "Door_Ext_House3_Ground", new Vector2(108.3f, 0.6f)),  // bajo los banderines del callejón, lejos del guardia del tejado
        ("Sign_House4_Chest", "Door_Ext_House4_Ground", new Vector2(148.0f, 1.45f)),
    };

    [MenuItem("Tools/Nivel 2/Puestos y puertas/5. Textos de cofres")]
    public static void TextsMenu() => Level2StallsDoorsFixer.Run("Textos", AddChestTexts);

    public static void AddChestTexts(StringBuilder log)
    {
        // 1. Aviso inicial: el tutorial de ingredientes de la Zona 1 pasa junto al spawn y explica los cofres
        TutorialTrigger tut = Object.FindObjectsByType<TutorialTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.TipId == "tip_ingredientes_mercado");
        if (tut != null)
        {
            SerializedObject so = new SerializedObject(tut);
            so.FindProperty("tipTitle").stringValue = StartTipTitle;
            so.FindProperty("tipBody").stringValue = StartTipBody;
            so.ApplyModifiedPropertiesWithoutUndo();
            tut.transform.position = new Vector3(3.5f, tut.transform.position.y, 0f);
            log.AppendLine("  Aviso inicial 'Busca en los cofres' en x=3.5 (una vez por sesión vía TutorialManager)");
        }
        TutorialPanel panel = Object.FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("autoCloseDelay").floatValue = 8f; // tiempo para leer el aviso completo; Enter o clic lo cierran antes
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 2 y 6. Guía permanente y contador de cofres en el HUD propio
        GameObject hudCanvas = GameObject.Find("UI/IngredientHUD_Canvas");
        if (hudCanvas.GetComponent<Level2ChestGuideHUD>() == null) hudCanvas.AddComponent<Level2ChestGuideHUD>();
        log.AppendLine("  Level2ChestGuideHUD en IngredientHUD_Canvas (guía + 'Cofres abiertos: X/Y' + aviso de cofre vacío)");

        // 3a. Letrero de calle junto al spawn, señalando al cofre del primer puesto
        Transform zone1 = GameObject.Find("Environment/Zone_1_Entrada").transform;
        Transform post = Level2StallsDoorsFixer.Child(zone1, "Sign_Z1_ChestsStreet");
        Level2StallsDoorsFixer.SetSprite(post, AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Environment/Env_Sign.png").OfType<Sprite>().First(), 0, true);
        post.localScale = new Vector3(0.55f, 0.55f, 1f);
        post.position = new Vector3(4.3f, 1.3f, 0f);
        Physics2D.SyncTransforms();
        Level2StallsDoorsFixer.Ground(post, post.GetComponent<SpriteRenderer>(), log);
        Transform board = Level2StallsDoorsFixer.Child(post, "InfoSign");
        board.localScale = new Vector3(1f / 0.55f, 1f / 0.55f, 1f);
        board.position = new Vector3(4.3f, post.GetComponent<SpriteRenderer>().bounds.min.y + 1.6f, 0f);
        ConfigureSign(board, StreetSignText, new Vector2(2.3f, 0.95f), 0.17f, 0.6f);
        log.AppendLine("  Letrero de calle en x=4.3 → cofre del primer puesto");

        // 3b. Casas con cofre dentro (Botica, Posada, Casa Abandonada, Mansión). Cuarentena y Santuario no tienen.
        // 3c. Los sótanos (Bodega y Cripta) guardan llaves, no cofres: no llevan letrero de cofre.
        Transform ext = GameObject.Find("Environment/Village_Exterior_Structures").transform;
        foreach (var (signName, doorName, pos) in HouseSigns)
        {
            Transform sign = Level2StallsDoorsFixer.Child(ext, signName);
            sign.localScale = Vector3.one;
            sign.position = pos;
            ConfigureSign(sign, HouseSignText, new Vector2(1.5f, 0.62f), 0.17f, 0.35f);
            log.AppendLine($"  {signName} junto a {doorName} en ({pos.x:F2}, {pos.y:F2})");
        }

        // 7. Salida sin ingredientes
        IngredientGate gate = Object.FindFirstObjectByType<IngredientGate>(FindObjectsInactive.Include);
        if (gate != null)
        {
            SerializedObject so = new SerializedObject(gate);
            so.FindProperty("lockedMessage").stringValue = GateLockedText;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  IngredientGate: recuerda revisar los cofres de las casas y de la calle");
        }
    }

    private static void ConfigureSign(Transform t, string text, Vector2 size, float fontSize, float idleAlpha)
    {
        Level2InfoSign sign = t.GetComponent<Level2InfoSign>();
        if (sign == null) sign = t.gameObject.AddComponent<Level2InfoSign>();
        SerializedObject so = new SerializedObject(sign);
        so.FindProperty("idleAlpha").floatValue = idleAlpha;
        so.ApplyModifiedPropertiesWithoutUndo();
        sign.Configure(text, size, fontSize);
    }
}
