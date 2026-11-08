using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Salem.Level4;

namespace Salem.Editor
{
    public static class Level4PrefabBuilder
    {
        [MenuItem("Salem/Build Level 4 Prefabs")]
        public static void BuildAllPrefabs()
        {
            string prefabDir = "Assets/Prefabs/Level4";
            if (!Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
            }

            BuildPewPrefab(prefabDir);
            BuildCarpetPrefab(prefabDir);
            BuildCreakyTilePrefab(prefabDir);
            BuildCathedralWindowPrefab(prefabDir);
            BuildClockworkGearPrefab(prefabDir);
            BuildGreatBellPrefab(prefabDir);
            BuildFatherParrisPrefab(prefabDir);
            BuildInquisitorMinibossPrefab(prefabDir);
            BuildEscapeWindowPrefab(prefabDir);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Level4PrefabBuilder] ¡Todos los prefabs de Nivel 4 construidos con éxito!");
        }

        private static void BuildPewPrefab(string dir)
        {
            GameObject go = new GameObject("Pew_Bench_Obstacle");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Pew_Oak_Bench.png");
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.8f, 1.1f);
            col.offset = new Vector2(0f, -0.1f);

            SaveAndDestroy(go, $"{dir}/Pew_Bench_Obstacle.prefab");
        }

        private static void BuildCarpetPrefab(string dir)
        {
            GameObject go = new GameObject("Carpet_Silent_Runner");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Carpet_Silent_Runner.png");
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3.5f, 0.6f);
            col.isTrigger = true;
            go.AddComponent<SilentCarpet>();

            SaveAndDestroy(go, $"{dir}/Carpet_Silent_Runner.prefab");
        }

        private static void BuildCreakyTilePrefab(string dir)
        {
            GameObject go = new GameObject("Creaky_Floor_Tile");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Creaky_Floor_Tile.png");
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 0.4f);
            col.isTrigger = true;

            var creak = go.AddComponent<CreakyFloorTile>();
            var sfxField = typeof(CreakyFloorTile).GetField("creakSfx", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sfxField != null)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_FloorCreak.wav");
                sfxField.SetValue(creak, clip);
            }

            SaveAndDestroy(go, $"{dir}/Creaky_Floor_Tile.prefab");
        }

        private static void BuildCathedralWindowPrefab(string dir)
        {
            GameObject go = new GameObject("Cathedral_Stained_Glass_Window");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Gothic_High_Window_CatEntry.png");
            sr.sortingOrder = 3;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.2f, 3.4f);
            col.isTrigger = true;

            go.AddComponent<CathedralHighWindowEntry>();

            SaveAndDestroy(go, $"{dir}/Cathedral_Stained_Glass_Window.prefab");
        }

        private static void BuildClockworkGearPrefab(string dir)
        {
            GameObject go = new GameObject("Clockwork_Gear_Platform");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Clockwork_Gear.png");
            sr.sortingOrder = 2;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 1.15f;

            var gearComp = go.AddComponent<ClockworkGearPlatform>();
            var tickField = typeof(ClockworkGearPlatform).GetField("tickSfx", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tickField != null)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_ClockworkTick.wav");
                tickField.SetValue(gearComp, clip);
            }

            SaveAndDestroy(go, $"{dir}/Clockwork_Gear_Platform.prefab");
        }

        private static void BuildGreatBellPrefab(string dir)
        {
            GameObject go = new GameObject("Salem_Great_Bell");
            var bellVisual = new GameObject("BellVisual");
            bellVisual.transform.SetParent(go.transform, false);
            var sr = bellVisual.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Salem_Great_Bell.png");
            sr.sortingOrder = 4;

            var ropeObj = new GameObject("BellRope");
            ropeObj.transform.SetParent(go.transform, false);
            ropeObj.transform.localPosition = new Vector3(0f, -1.8f, 0f);
            var srRope = ropeObj.AddComponent<SpriteRenderer>();
            srRope.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Bell_Rope.png");
            srRope.sortingOrder = 3;

            var shockObj = new GameObject("Shockwave");
            shockObj.transform.SetParent(go.transform, false);
            var srShock = shockObj.AddComponent<SpriteRenderer>();
            srShock.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/FX_Bell_Shockwave.png");
            srShock.sortingOrder = 6;
            shockObj.SetActive(false);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 2.8f);
            col.isTrigger = true;

            var bellTrigger = go.AddComponent<GreatBellRopeTrigger>();
            SetPrivateField(bellTrigger, "bellTransform", bellVisual.transform);
            SetPrivateField(bellTrigger, "shockwaveRenderer", srShock);
            SetPrivateField(bellTrigger, "bellTollSfx", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_GreatBellToll.wav"));

            SaveAndDestroy(go, $"{dir}/Salem_Great_Bell.prefab");
        }

        private static void BuildFatherParrisPrefab(string dir)
        {
            GameObject go = new GameObject("FatherParris_Altar");
            var altarObj = new GameObject("AltarPulpit");
            altarObj.transform.SetParent(go.transform, false);
            var srAltar = altarObj.AddComponent<SpriteRenderer>();
            srAltar.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Church_Altar_Pulpit.png");
            srAltar.sortingOrder = 2;

            var preacherObj = new GameObject("Preacher");
            preacherObj.transform.SetParent(go.transform, false);
            preacherObj.transform.localPosition = new Vector3(-0.4f, 0.8f, 0f);
            var srPreacher = preacherObj.AddComponent<SpriteRenderer>();
            // Use existing preacher sprite or idle frame
            var preacherSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Villagers/preacher/idle_0.png");
            if (preacherSprite == null) preacherSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Villagers/preacher/idle.png");
            srPreacher.sprite = preacherSprite;
            srPreacher.sortingOrder = 3;

            var chaliceObj = new GameObject("Chalice");
            chaliceObj.transform.SetParent(go.transform, false);
            chaliceObj.transform.localPosition = new Vector3(0.5f, 0.4f, 0f);
            var srChalice = chaliceObj.AddComponent<SpriteRenderer>();
            srChalice.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Preacher_Ceremonial_Chalice.png");
            srChalice.sortingOrder = 4;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.5f, 2.0f);
            col.isTrigger = true;

            var encounter = go.AddComponent<PoisonFatherEncounter>();
            SetPrivateField(encounter, "chaliceRenderer", srChalice);
            SetPrivateField(encounter, "preacherRenderer", srPreacher);
            SetPrivateField(encounter, "poisonSfx", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_PoisonGoblet.wav"));
            SetPrivateField(encounter, "alarmMusic", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level4_Boss_Escape.wav"));

            SaveAndDestroy(go, $"{dir}/FatherParris_Altar.prefab");
        }

        private static void BuildInquisitorMinibossPrefab(string dir)
        {
            GameObject go = new GameObject("Inquisitor_Miniboss");
            var sr = go.AddComponent<SpriteRenderer>();
            var idle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Inquisitor/Inquisitor_Idle.png");
            var walk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Inquisitor/Inquisitor_Walk.png");
            var listen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Inquisitor/Inquisitor_Listen.png");
            var charge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Inquisitor/Inquisitor_Charge.png");
            var stunned = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Inquisitor/Inquisitor_Stunned.png");

            sr.sprite = idle;
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 2.0f);
            col.offset = new Vector2(0f, 0.95f);

            var inqAI = go.AddComponent<InquisitorGuardAI>();
            SetPrivateField(inqAI, "spriteRenderer", sr);
            SetPrivateField(inqAI, "idleFrames", new[] { idle });
            SetPrivateField(inqAI, "walkFrames", new[] { walk });
            SetPrivateField(inqAI, "listenFrames", new[] { listen });
            SetPrivateField(inqAI, "chargeFrames", new[] { charge });
            SetPrivateField(inqAI, "stunnedFrames", new[] { stunned });
            SetPrivateField(inqAI, "alertSfx", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_InquisitorAlert.wav"));
            SetPrivateField(inqAI, "stompSfx", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_InquisitorStomp.wav"));

            // Hand Lantern Origin
            var lanternOrigin = new GameObject("LanternOrigin");
            lanternOrigin.transform.SetParent(go.transform, false);
            lanternOrigin.transform.localPosition = new Vector3(0.55f, 1.15f, 0f);

            var sight = go.AddComponent<EnemySight>();
            SetPrivateField(sight, "lanternOrigin", lanternOrigin.transform);

            SaveAndDestroy(go, $"{dir}/Inquisitor_Miniboss.prefab");
        }

        private static void BuildEscapeWindowPrefab(string dir)
        {
            GameObject go = new GameObject("Church_Tower_Broken_Window");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Level4/Church_Tower_Broken_Window.png");
            sr.sortingOrder = 3;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.5f, 3.5f);
            col.isTrigger = true;

            var escape = go.AddComponent<Level4EndingSequence>();
            SetPrivateField(escape, "escapeWindowTransform", go.transform);
            SetPrivateField(escape, "glassShatterSfx", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_GlassShatterEscape.wav"));
            SetPrivateField(escape, "creditsMusic", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Credits_Salem.wav"));

            SaveAndDestroy(go, $"{dir}/Church_Tower_Broken_Window.prefab");
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        private static void SaveAndDestroy(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
        }
    }
}
