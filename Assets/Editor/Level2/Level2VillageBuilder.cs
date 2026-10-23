using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Level2.Editor
{
    /// <summary>
    /// Constructor e integrador de la Aldea Explorable del Siglo XVII para el Nivel 2.
    /// Evoluciona Level2.unity añadiendo:
    /// - 4 Casas explorables con fachadas, puertas de planta baja y puertas de balcón en 2º piso.
    /// - 4 Interiores independientes de múltiples pisos (2 y 3 niveles: sótanos, plantas bajas,
    ///   segundos pisos y ático secreto) con muros sólidos, techos, suelos de tablones, muebles de época,
    ///   iluminación cálida 2D y límites propios de cámara.
    /// - Sistema de escaleras exteriores e interiores (LadderInteractable) y plataformas/pasarelas.
    /// - Cofres de tesoro (abiertos y con llave), llaves ocultas y grimorios/documentos examinables.
    /// Conserva intactos todos los sistemas existentes del Nivel 2 y Nivel 1.
    /// </summary>
    public static class Level2VillageBuilder
    {
        private const string ScenePath = "Assets/Scenes/Level2.unity";
        private const string ArtDir = "Assets/Art/Environment/Level2";

        [MenuItem("Brujas/Nivel 2/Construir Aldea Explorable e Interiores (Siglo XVII)")]
        public static void BuildExplorableVillage()
        {
            ConfigureVillageSprites();

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0) groundLayer = 0;

            Transform managersRoot = GameObject.Find("Managers")?.transform;
            Transform envRoot = GameObject.Find("Environment")?.transform;
            Transform gameplayRoot = GameObject.Find("Gameplay")?.transform;
            Transform uiRoot = GameObject.Find("UI")?.transform;

            if (managersRoot == null || envRoot == null || gameplayRoot == null || uiRoot == null)
            {
                Debug.LogError("[Level2VillageBuilder] Faltan raíces canónicas en Level2.unity.");
                return;
            }

            // 1. Asegurar componentes en Witch y Managers
            SetupPlayerAndManagers(managersRoot, gameplayRoot, uiRoot);

            // 2. Limpiar construcciones previas de aldea/interiores si se re-ejecuta la herramienta
            DestroyIfExists(envRoot, "Village_Exterior_Structures");
            DestroyIfExists(envRoot, "Interiors");

            Transform extRoot = new GameObject("Village_Exterior_Structures").transform;
            extRoot.SetParent(envRoot, false);

            Transform interiorsRoot = new GameObject("Interiors").transform;
            interiorsRoot.SetParent(envRoot, false);

            // Cargar sprites generados
            Sprite facadeA = LoadSprite("Village_House_Facade_A.png");
            Sprite facadeB = LoadSprite("Village_House_Facade_B.png");
            Sprite doorClosed = LoadSprite("Village_Door_Closed.png");
            Sprite doorOpen = LoadSprite("Village_Door_Open.png");
            Sprite doorLocked = LoadSprite("Village_Door_Locked.png");
            Sprite ladderSprite = LoadSprite("Village_Ladder.png");
            Sprite balconySprite = LoadSprite("Village_Balcony_Platform.png");
            Sprite roofPlatSprite = LoadSprite("Placeholder_Market_RoofPlatform.png");
            Sprite wallTimber = LoadSprite("Interior_Wall_Timber.png");
            Sprite wallStone = LoadSprite("Interior_Wall_Stone.png");
            Sprite floorPlanks = LoadSprite("Interior_Floor_Planks.png");
            Sprite fireplaceSprite = LoadSprite("Interior_Fireplace.png");
            Sprite bookshelfSprite = LoadSprite("Interior_Bookshelf.png");
            Sprite tableSprite = LoadSprite("Interior_Table_Chairs.png");
            Sprite bedSprite = LoadSprite("Interior_Bed.png");
            Sprite wardrobeSprite = LoadSprite("Interior_Wardrobe.png");
            Sprite cauldronSprite = LoadSprite("Interior_Cauldron_Rack.png");
            Sprite windowSprite = LoadSprite("Interior_Window_Shutters.png");
            Sprite chandelierSprite = LoadSprite("Interior_Chandelier.png");
            Sprite chestClosed = LoadSprite("Interactive_Chest_Closed.png");
            Sprite chestOpen = LoadSprite("Interactive_Chest_Open.png");
            Sprite keySprite = LoadSprite("Interactive_Key.png");
            Sprite loreSprite = LoadSprite("Interactive_LoreBook.png");
            Sprite barrelSprite = LoadSprite("Placeholder_Market_Barrel.png");
            Sprite crateSprite = LoadSprite("Placeholder_Market_Crate.png");

            // -------------------------------------------------------------------------
            // CASA 1: LA BOTICA DE HERBOLARIA (ZONA 1, x = 15.5) — 2 PISOS + BALCÓN
            // -------------------------------------------------------------------------
            CreateVisualProp(extRoot, "House1_Facade_Apothecary", new Vector3(15.5f, 3.65f, 0f), facadeA, "Gameplay", -2);
            GameObject balH1 = CreateOneWayPlatform(extRoot, "Balcony_Ext_House1", new Vector2(15.5f, 3.85f), balconySprite, groundLayer, 0);
            CreateOneWayPlatform(extRoot, "Walkway_Ext_Z1_Bridge", new Vector2(11.6f, 3.85f), roofPlatSprite, groundLayer, 0);
            CreateLadder(extRoot, "Ladder_Ext_Z1_Balcony", 10.2f, 0.17f, 4.02f, ladderSprite, balH1.GetComponent<Collider2D>());

            HouseDoor extDoorH1Ground = CreateDoor(extRoot, "Door_Ext_House1_Ground", new Vector3(15.5f, 1.28f, 0f),
                "Entrar a la Botica", 0.17f, null, doorClosed, doorOpen, doorLocked);
            HouseDoor extDoorH1Balcony = CreateDoor(extRoot, "Door_Ext_House1_Balcony", new Vector3(15.5f, 5.12f, 0f),
                "Entrar al 2º Piso (Botica)", 4.02f, null, doorClosed, doorOpen, doorLocked);

            BuildInteriorHouse1Apothecary(
                interiorsRoot, groundLayer, extDoorH1Ground, extDoorH1Balcony,
                wallTimber, floorPlanks, doorClosed, doorOpen, doorLocked, ladderSprite,
                cauldronSprite, tableSprite, bookshelfSprite, bedSprite, wardrobeSprite,
                windowSprite, chandelierSprite, chestClosed, chestOpen, loreSprite);

            // -------------------------------------------------------------------------
            // CASA 2: POSADA DEL CUERVO (ZONA 2, x = 45.0) — 3 NIVELES (SÓTANO + PB + 2º PISO)
            // -------------------------------------------------------------------------
            CreateVisualProp(extRoot, "House2_Facade_Tavern", new Vector3(45.0f, 3.65f, 0f), facadeB, "Gameplay", -2);
            GameObject walkZ2W = CreateOneWayPlatform(extRoot, "Walkway_Ext_Z2_West", new Vector2(34.5f, 3.8f), balconySprite, groundLayer, 0);
            GameObject walkZ2E = CreateOneWayPlatform(extRoot, "Walkway_Ext_Z2_East", new Vector2(51.5f, 3.8f), balconySprite, groundLayer, 0);
            CreateLadder(extRoot, "Ladder_Ext_Z2_West", 32.2f, 0.17f, 3.98f, ladderSprite, walkZ2W.GetComponent<Collider2D>());
            CreateLadder(extRoot, "Ladder_Ext_Z2_East", 51.8f, 0.17f, 3.98f, ladderSprite, walkZ2E.GetComponent<Collider2D>());

            // Cofre exterior sobre el balcón oriental de la Zona 2
            CreateChest(extRoot, "Chest_Ext_Z2_Balcony", new Vector3(53.0f, 4.48f, 0f), "Cofre del Balcón",
                200, true, PotionType.Blind, false, chestClosed, chestOpen);

            HouseDoor extDoorH2Ground = CreateDoor(extRoot, "Door_Ext_House2_Ground", new Vector3(45.0f, 1.28f, 0f),
                "Entrar a la Posada del Cuervo", 0.17f, null, doorClosed, doorOpen, doorLocked);
            HouseDoor extDoorH2Balcony = CreateDoor(extRoot, "Door_Ext_House2_Balcony", new Vector3(45.0f, 5.05f, 0f),
                "Entrar al 2º Piso (Posada)", 4.02f, null, doorClosed, doorOpen, doorLocked);

            BuildInteriorHouse2Tavern(
                interiorsRoot, groundLayer, extDoorH2Ground, extDoorH2Balcony,
                wallTimber, wallStone, floorPlanks, doorClosed, doorOpen, doorLocked, ladderSprite,
                fireplaceSprite, tableSprite, bookshelfSprite, bedSprite, wardrobeSprite,
                windowSprite, chandelierSprite, barrelSprite, crateSprite,
                chestClosed, chestOpen, keySprite, loreSprite);

            // -------------------------------------------------------------------------
            // ZONA 3: MIRADORES VERTICALES DEL CANAL Y PUERTA EN CUARENTENA
            // -------------------------------------------------------------------------
            CreateLadder(extRoot, "Ladder_Ext_Z3_Left", 69.2f, 0.17f, 4.22f, ladderSprite);
            CreateLadder(extRoot, "Ladder_Ext_Z3_Right", 88.8f, 0.17f, 4.42f, ladderSprite);
            CreateChest(extRoot, "Chest_Ext_Z3_CanalOverlook", new Vector3(71.2f, 4.70f, 0f), "Cofre del Mirador",
                220, true, PotionType.Sleep, false, chestClosed, chestOpen);

            // Puerta tapiada por cuarentena (no todas las puertas se abren igual)
            HouseDoor quarantineDoor = CreateDoor(extRoot, "Door_Ext_Z3_Quarantine", new Vector3(62.8f, 1.28f, 0f),
                "Casa en Cuarentena", 0.17f, null, doorClosed, doorOpen, doorLocked);
            quarantineDoor.ConfigureLock(true, "", "", false, true, "Clausurada por edicto del magistrado: prohibido el paso.");

            // -------------------------------------------------------------------------
            // CASA 3: CASA ABANDONADA DEL TEJEDOR (ZONA 4, x = 109) — PB + 2º PISO + ÁTICO SECRETO
            // -------------------------------------------------------------------------
            CreateVisualProp(extRoot, "House3_Facade_Abandoned", new Vector3(109.0f, 3.65f, 0f), facadeB, "Gameplay", -2);
            CreateLadder(extRoot, "Ladder_Ext_Z4_RoofExit", 117.5f, 0.17f, 4.62f, ladderSprite);

            HouseDoor extDoorH3Ground = CreateDoor(extRoot, "Door_Ext_House3_Ground", new Vector3(102.2f, 1.28f, 0f),
                "Entrar a la Casa Abandonada", 0.17f, null, doorClosed, doorOpen, doorLocked);
            HouseDoor extDoorH3Roof = CreateDoor(extRoot, "Door_Ext_House3_Roof", new Vector3(114.2f, 5.70f, 0f),
                "Entrar desde el Tejado (2º Piso)", 4.62f, null, doorClosed, doorOpen, doorLocked);

            BuildInteriorHouse3Abandoned(
                interiorsRoot, groundLayer, extDoorH3Ground, extDoorH3Roof,
                wallTimber, floorPlanks, doorClosed, doorOpen, doorLocked, ladderSprite,
                tableSprite, bookshelfSprite, wardrobeSprite, windowSprite, crateSprite,
                chestClosed, chestOpen, loreSprite);

            // -------------------------------------------------------------------------
            // CASA 4: MANSIÓN DEL MAGISTRADO (ZONA 5, x = 149) — SÓTANO + GRAN SALÓN + GALERÍA
            // -------------------------------------------------------------------------
            CreateVisualProp(extRoot, "House4_Facade_Manor", new Vector3(149.0f, 3.65f, 0f), facadeA, "Gameplay", -2);
            CreateLadder(extRoot, "Ladder_Ext_Z5_West", 134.2f, 0.17f, 4.02f, ladderSprite);
            CreateLadder(extRoot, "Ladder_Ext_Z5_HighTower", 140.8f, 4.02f, 6.42f, ladderSprite);
            CreateLadder(extRoot, "Ladder_Ext_Z5_East", 153.2f, 0.17f, 4.62f, ladderSprite);

            HouseDoor extDoorH4Ground = CreateDoor(extRoot, "Door_Ext_House4_Ground", new Vector3(146.5f, 1.28f, 0f),
                "Entrar a la Mansión del Magistrado", 0.17f, null, doorClosed, doorOpen, doorLocked);
            HouseDoor extDoorH4Balcony = CreateDoor(extRoot, "Door_Ext_House4_Balcony", new Vector3(149.0f, 5.70f, 0f),
                "Entrar a la Galería Superior", 4.62f, null, doorClosed, doorOpen, doorLocked);

            BuildInteriorHouse4Manor(
                interiorsRoot, groundLayer, extDoorH4Ground, extDoorH4Balcony,
                wallTimber, wallStone, floorPlanks, doorClosed, doorOpen, doorLocked, ladderSprite,
                fireplaceSprite, tableSprite, bookshelfSprite, bedSprite, wardrobeSprite,
                windowSprite, chandelierSprite, barrelSprite, crateSprite,
                chestClosed, chestOpen, keySprite, loreSprite);

            // -------------------------------------------------------------------------
            // ZONA 6: CASETA SELLADA POR MISIÓN (REQUIERE LOS 8 INGREDIENTES)
            // -------------------------------------------------------------------------
            HouseDoor missionDoor = CreateDoor(extRoot, "Door_Ext_Z6_Sanctum", new Vector3(173.0f, 1.28f, 0f),
                "Cámara del Sello Eclesiástico", 0.17f, null, doorClosed, doorOpen, doorLocked);
            missionDoor.ConfigureLock(false, "", "", needsIngredients: true, barred: false);
            // Conectada con el estudio alto de la Mansión como atajo arcano una vez reunidos los ingredientes
            HouseDoor manorArcaneExit = GameObject.Find("Door_Int_House4_Balcony")?.GetComponent<HouseDoor>();
            if (manorArcaneExit != null)
            {
                missionDoor.TargetDoor = manorArcaneExit;
            }

            Physics2D.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Level2VillageBuilder] ¡Aldea del siglo XVII con 4 casas interactivas, interiores multinivel, escaleras y secretos construida exitosamente!");
        }

        // =========================================================================
        // CONSTRUCCIÓN DE LOS 4 INTERIORES MULTINIVEL
        // =========================================================================

        private static void BuildInteriorHouse1Apothecary(
            Transform parent, int groundLayer, HouseDoor extGroundDoor, HouseDoor extBalconyDoor,
            Sprite wallSprite, Sprite floorSprite, Sprite doorClosed, Sprite doorOpen, Sprite doorLocked, Sprite ladderSprite,
            Sprite cauldron, Sprite table, Sprite shelf, Sprite bed, Sprite wardrobe,
            Sprite window, Sprite chandelier, Sprite chestClosed, Sprite chestOpen, Sprite loreSprite)
        {
            // Ubicación aislada: x = 10..26 (ancho 16 u), y = 28..37 (2 pisos de 4 u cada uno)
            float centerX = 18f;
            float baseFloorY = 28.5f;
            float secondFloorY = 32.5f;
            float ceilingY = 36.8f;
            float width = 16f;

            InteriorZone zone = CreateInteriorShell(
                parent, "Interior_House1_Apothecary", "Botica de Herbolaria", 2,
                centerX, baseFloorY, ceilingY, width, groundLayer, wallSprite, null, floorSprite);

            Transform root = zone.transform;

            // Forjado del Segundo Piso (con hueco de escalera a la derecha en x = 22.5)
            GameObject secondFloorSlab = CreateInteriorFloorSlab(root, "Floor2_Slab", new Vector2(16.8f, secondFloorY), 12.2f, groundLayer, floorSprite, oneWay: true);
            CreateInteriorFloorSlab(root, "Floor2_RightLanding", new Vector2(24.8f, secondFloorY), 2.0f, groundLayer, floorSprite, oneWay: true);

            // Escalera interior Planta Baja -> Segundo Piso (x = 23.2)
            CreateLadder(root, "Ladder_Int_House1_F1ToF2", 23.2f, baseFloorY + 0.25f, secondFloorY + 0.25f,
                ladderSprite, secondFloorSlab.GetComponent<Collider2D>());

            // Puertas Interiores conectadas con el Exterior
            HouseDoor intGroundDoor = CreateDoor(root, "Door_Int_House1_Ground", new Vector3(12.2f, baseFloorY + 1.35f, 0f),
                "Salir a la Calle (Zona 1)", baseFloorY + 0.25f, zone, doorClosed, doorOpen, doorLocked);
            LinkDoors(extGroundDoor, intGroundDoor);

            HouseDoor intBalconyDoor = CreateDoor(root, "Door_Int_House1_Balcony", new Vector3(12.2f, secondFloorY + 1.35f, 0f),
                "Salir al Balcón (2º Piso)", secondFloorY + 0.25f, zone, doorClosed, doorOpen, doorLocked);
            LinkDoors(extBalconyDoor, intBalconyDoor);

            // Mobiliario Planta Baja (Alquimia y Botica)
            CreateVisualProp(root, "Cauldron_Alch", new Vector3(15.5f, baseFloorY + 1.0f, 0f), cauldron, "Gameplay", 0);
            CreateVisualProp(root, "Table_Alch", new Vector3(18.5f, baseFloorY + 1.0f, 0f), table, "Gameplay", 0);
            CreateVisualProp(root, "Shelf_Alch_1", new Vector3(21.0f, baseFloorY + 1.5f, 0f), shelf, "Gameplay", 0);
            CreateVisualProp(root, "Window_F1", new Vector3(17.0f, baseFloorY + 2.2f, 0f), window, "Gameplay", -1);
            CreateVisualProp(root, "Chandelier_F1", new Vector3(18.0f, secondFloorY - 0.6f, 0f), chandelier, "Gameplay", 1);
            CreateInteriorLight(root, "Light_F1", new Vector3(18.0f, baseFloorY + 2.5f, 0f), 9f, 0.95f);

            // Objeto Examinable en Planta Baja
            CreateLoreObject(root, "Lore_House1_Recipe", new Vector3(18.5f, baseFloorY + 0.9f, 0f),
                "Recetario de la Botica",
                "«Para quebrar el sello del portón eclesiástico se precisan dos hongos de humedad, dos arañas, dos flores marchitas y dos raíces.»",
                loreSprite);

            // Mobiliario y Recompensas Segundo Piso (Dormitorio del Boticario)
            CreateVisualProp(root, "Bed_F2", new Vector3(15.5f, secondFloorY + 1.0f, 0f), bed, "Gameplay", 0);
            CreateVisualProp(root, "Wardrobe_F2", new Vector3(18.2f, secondFloorY + 1.5f, 0f), wardrobe, "Gameplay", 0);
            CreateVisualProp(root, "Window_F2", new Vector3(20.5f, secondFloorY + 2.2f, 0f), window, "Gameplay", -1);
            CreateVisualProp(root, "Chandelier_F2", new Vector3(18.0f, ceilingY - 0.6f, 0f), chandelier, "Gameplay", 1);
            CreateInteriorLight(root, "Light_F2", new Vector3(18.0f, secondFloorY + 2.5f, 0f), 9f, 0.95f);

            // Cofre de recompensa en el 2º piso
            CreateChest(root, "Chest_Int_House1_Upper", new Vector3(20.6f, secondFloorY + 0.75f, 0f),
                "Cofre del Boticario", 250, true, PotionType.Blind, false, chestClosed, chestOpen);
        }

        private static void BuildInteriorHouse2Tavern(
            Transform parent, int groundLayer, HouseDoor extGroundDoor, HouseDoor extBalconyDoor,
            Sprite wallTimber, Sprite wallStone, Sprite floorSprite, Sprite doorClosed, Sprite doorOpen, Sprite doorLocked, Sprite ladderSprite,
            Sprite fireplace, Sprite table, Sprite shelf, Sprite bed, Sprite wardrobe,
            Sprite window, Sprite chandelier, Sprite barrel, Sprite crate,
            Sprite chestClosed, Sprite chestOpen, Sprite keySprite, Sprite loreSprite)
        {
            // 3 Niveles: Sótano (y = 24.5), Planta Baja (y = 28.5), Segundo Piso (y = 32.5), Techo (y = 36.8)
            float centerX = 48f;
            float cellarFloorY = 24.5f;
            float groundFloorY = 28.5f;
            float secondFloorY = 32.5f;
            float ceilingY = 36.8f;
            float width = 18f;

            InteriorZone zone = CreateInteriorShell(
                parent, "Interior_House2_Tavern", "Posada del Cuervo", 3,
                centerX, cellarFloorY, ceilingY, width, groundLayer, wallTimber, wallStone, floorSprite, stoneBelowY: groundFloorY);

            Transform root = zone.transform;

            // Forjado de Planta Baja (separa Sótano y Planta Baja, con hueco de escalera en x = 54.2)
            GameObject groundSlab = CreateInteriorFloorSlab(root, "Floor1_Slab", new Vector2(46.2f, groundFloorY), 13.5f, groundLayer, floorSprite, oneWay: true);
            CreateInteriorFloorSlab(root, "Floor1_RightLanding", new Vector2(55.8f, groundFloorY), 1.8f, groundLayer, floorSprite, oneWay: true);

            // Forjado del Segundo Piso (con hueco de escalera en x = 44.5)
            CreateInteriorFloorSlab(root, "Floor2_LeftLanding", new Vector2(41.2f, secondFloorY), 3.6f, groundLayer, floorSprite, oneWay: true);
            GameObject secondSlab = CreateInteriorFloorSlab(root, "Floor2_Slab", new Vector2(51.2f, secondFloorY), 11.2f, groundLayer, floorSprite, oneWay: true);

            // Escaleras Interiores:
            // 1. Sótano <-> Planta Baja (x = 54.0)
            CreateLadder(root, "Ladder_Int_House2_CellarToF1", 54.0f, cellarFloorY + 0.25f, groundFloorY + 0.25f,
                ladderSprite, groundSlab.GetComponent<Collider2D>());
            // 2. Planta Baja <-> Segundo Piso (x = 44.2)
            CreateLadder(root, "Ladder_Int_House2_F1ToF2", 44.2f, groundFloorY + 0.25f, secondFloorY + 0.25f,
                ladderSprite, secondSlab.GetComponent<Collider2D>());

            // Puertas Interiores
            HouseDoor intGroundDoor = CreateDoor(root, "Door_Int_House2_Ground", new Vector3(41.2f, groundFloorY + 1.35f, 0f),
                "Salir a la Calle (Zona 2)", groundFloorY + 0.25f, zone, doorClosed, doorOpen, doorLocked);
            LinkDoors(extGroundDoor, intGroundDoor);

            HouseDoor intBalconyDoor = CreateDoor(root, "Door_Int_House2_Balcony", new Vector3(54.8f, secondFloorY + 1.35f, 0f),
                "Salir a la Pasarela Alta", secondFloorY + 0.25f, zone, doorClosed, doorOpen, doorLocked);
            LinkDoors(extBalconyDoor, intBalconyDoor);

            // Nivel 0: Sótano Secreto de Piedra (Barriles, Cajas y Llave de la Posada)
            CreateVisualProp(root, "Cellar_Barrel_1", new Vector3(45.5f, cellarFloorY + 0.75f, 0f), barrel, "Gameplay", 0);
            CreateVisualProp(root, "Cellar_Barrel_2", new Vector3(46.5f, cellarFloorY + 0.75f, 0f), barrel, "Gameplay", 0);
            CreateSolidBoxProp(root, "Cellar_Crate_Step", new Vector2(49.0f, cellarFloorY + 0.75f), crate, groundLayer);
            CreateInteriorLight(root, "Light_Cellar", new Vector3(48.0f, cellarFloorY + 2.2f, 0f), 8f, 0.7f);

            // Llave en el rincón occidental del sótano
            CreateKeyItem(root, "Key_Tavern_Chest", new Vector3(42.0f, cellarFloorY + 0.85f, 0f),
                "key_tavern_chest", "Llave de Hierro de la Posada", keySprite);

            // Nivel 1: Planta Baja (Salón de la Posada con Chimenea)
            CreateVisualProp(root, "Tavern_Fireplace", new Vector3(48.2f, groundFloorY + 1.7f, 0f), fireplace, "Gameplay", 0);
            CreateVisualProp(root, "Tavern_Table", new Vector3(51.2f, groundFloorY + 1.0f, 0f), table, "Gameplay", 0);
            CreateVisualProp(root, "Tavern_Window", new Vector3(45.8f, groundFloorY + 2.2f, 0f), window, "Gameplay", -1);
            CreateVisualProp(root, "Tavern_Chandelier", new Vector3(48.5f, secondFloorY - 0.6f, 0f), chandelier, "Gameplay", 1);
            CreateInteriorLight(root, "Light_Tavern_F1", new Vector3(48.2f, groundFloorY + 2.4f, 0f), 10f, 1.0f);

            CreateLoreObject(root, "Lore_House2_Diary", new Vector3(51.2f, groundFloorY + 0.9f, 0f),
                "Diario del Posadero",
                "«Escondí la llave del cofre del piso de arriba en la bodega subterránea, detrás de los toneles de sidra.»",
                loreSprite);

            // Nivel 2: Segundo Piso (Dormitorio y Cofre Cerrado con Llave)
            CreateVisualProp(root, "Tavern_Bed_1", new Vector3(47.5f, secondFloorY + 1.0f, 0f), bed, "Gameplay", 0);
            CreateVisualProp(root, "Tavern_Shelf", new Vector3(50.5f, secondFloorY + 1.5f, 0f), shelf, "Gameplay", 0);
            CreateVisualProp(root, "Tavern_Wardrobe", new Vector3(41.5f, secondFloorY + 1.5f, 0f), wardrobe, "Gameplay", 0);
            CreateInteriorLight(root, "Light_Tavern_F2", new Vector3(48.5f, secondFloorY + 2.4f, 0f), 10f, 0.9f);

            CreateChest(root, "Chest_Int_House2_Locked", new Vector3(52.4f, secondFloorY + 0.75f, 0f),
                "Cofre del Posadero", 350, true, PotionType.Sleep, true, chestClosed, chestOpen,
                needsKey: true, keyId: "key_tavern_chest", keyName: "Llave de Hierro de la Posada");
        }

        private static void BuildInteriorHouse3Abandoned(
            Transform parent, int groundLayer, HouseDoor extGroundDoor, HouseDoor extRoofDoor,
            Sprite wallTimber, Sprite floorSprite, Sprite doorClosed, Sprite doorOpen, Sprite doorLocked, Sprite ladderSprite,
            Sprite table, Sprite shelf, Sprite wardrobe, Sprite window, Sprite crate,
            Sprite chestClosed, Sprite chestOpen, Sprite loreSprite)
        {
            // 3 Niveles: Planta Baja (y = 28.5), Segundo Piso (y = 32.5), Ático Secreto (y = 36.5), Techo (y = 40.8)
            float centerX = 107f;
            float groundFloorY = 28.5f;
            float secondFloorY = 32.5f;
            float atticFloorY = 36.5f;
            float ceilingY = 40.8f;
            float width = 18f;

            InteriorZone zone = CreateInteriorShell(
                parent, "Interior_House3_Abandoned", "Casa Abandonada del Tejedor", 3,
                centerX, groundFloorY, ceilingY, width, groundLayer, wallTimber, null, floorSprite);

            Transform root = zone.transform;

            // Forjado 2º Piso (hueco de escalera a la derecha x = 112.5)
            GameObject secondSlab = CreateInteriorFloorSlab(root, "Floor2_Slab", new Vector2(105.0f, secondFloorY), 13.0f, groundLayer, floorSprite, oneWay: true);
            CreateInteriorFloorSlab(root, "Floor2_RightLanding", new Vector2(114.6f, secondFloorY), 2.2f, groundLayer, floorSprite, oneWay: true);

            // Forjado del Ático Secreto (3er nivel, accesible por escalera en x = 102.5)
            CreateInteriorFloorSlab(root, "Attic_LeftLanding", new Vector2(100.0f, atticFloorY), 2.8f, groundLayer, floorSprite, oneWay: true);
            GameObject atticSlab = CreateInteriorFloorSlab(root, "Attic_Slab", new Vector2(109.5f, atticFloorY), 12.0f, groundLayer, floorSprite, oneWay: true);

            // Escaleras Interiores:
            // 1. Planta Baja <-> Segundo Piso (x = 112.5)
            CreateLadder(root, "Ladder_Int_House3_F1ToF2", 112.5f, groundFloorY + 0.25f, secondFloorY + 0.25f,
                ladderSprite, secondSlab.GetComponent<Collider2D>());
            // 2. Segundo Piso <-> Ático Secreto (x = 102.5)
            CreateLadder(root, "Ladder_Int_House3_F2ToAttic", 102.5f, secondFloorY + 0.25f, atticFloorY + 0.25f,
                ladderSprite, atticSlab.GetComponent<Collider2D>());

            // Puertas Interiores (permiten atravesar el edificio desde la calle hasta el tejado del callejón)
            HouseDoor intGroundDoor = CreateDoor(root, "Door_Int_House3_Ground", new Vector3(100.2f, groundFloorY + 1.35f, 0f),
                "Salir al Callejón (Planta Baja)", groundFloorY + 0.25f, zone, doorClosed, doorOpen, doorLocked);
            LinkDoors(extGroundDoor, intGroundDoor);

            HouseDoor intRoofDoor = CreateDoor(root, "Door_Int_House3_Roof", new Vector3(114.4f, secondFloorY + 1.35f, 0f),
                "Salir al Tejado del Callejón (2º Piso)", secondFloorY + 0.25f, zone, doorClosed, doorOpen, doorLocked);
            LinkDoors(extRoofDoor, intRoofDoor);

            // Mobiliario y plataformas
            CreateVisualProp(root, "Abandoned_Wardrobe", new Vector3(104.5f, groundFloorY + 1.5f, 0f), wardrobe, "Gameplay", 0);
            CreateSolidBoxProp(root, "Abandoned_Crate_1", new Vector2(108.0f, groundFloorY + 0.75f), crate, groundLayer);
            CreateVisualProp(root, "Abandoned_Table_F2", new Vector3(106.5f, secondFloorY + 1.0f, 0f), table, "Gameplay", 0);
            CreateVisualProp(root, "Abandoned_Window_F2", new Vector3(109.5f, secondFloorY + 2.2f, 0f), window, "Gameplay", -1);
            CreateVisualProp(root, "Attic_Shelf", new Vector3(106.5f, atticFloorY + 1.5f, 0f), shelf, "Gameplay", 0);

            CreateInteriorLight(root, "Light_Abandoned_F1", new Vector3(107f, groundFloorY + 2.3f, 0f), 9f, 0.75f);
            CreateInteriorLight(root, "Light_Abandoned_F2", new Vector3(107f, secondFloorY + 2.3f, 0f), 9f, 0.8f);
            CreateInteriorLight(root, "Light_Abandoned_Attic", new Vector3(108f, atticFloorY + 2.0f, 0f), 9f, 0.85f);

            // Recompensas en el Ático Secreto
            CreateLoreObject(root, "Lore_House3_Grimoire", new Vector3(108.8f, atticFloorY + 0.9f, 0f),
                "Grimorio del Tejedor",
                "«Desde el balcón del segundo piso se domina el callejón entero sin despertar a los guardias.»",
                loreSprite);

            CreateChest(root, "Chest_Int_House3_Attic", new Vector3(112.2f, atticFloorY + 0.75f, 0f),
                "Cofre Oculto del Ático", 300, true, PotionType.Hypnotize, false, chestClosed, chestOpen);
        }

        private static void BuildInteriorHouse4Manor(
            Transform parent, int groundLayer, HouseDoor extGroundDoor, HouseDoor extBalconyDoor,
            Sprite wallTimber, Sprite wallStone, Sprite floorSprite, Sprite doorClosed, Sprite doorOpen, Sprite doorLocked, Sprite ladderSprite,
            Sprite fireplace, Sprite table, Sprite shelf, Sprite bed, Sprite wardrobe,
            Sprite window, Sprite chandelier, Sprite barrel, Sprite crate,
            Sprite chestClosed, Sprite chestOpen, Sprite keySprite, Sprite loreSprite)
        {
            // 3 Niveles: Sótano de Archivos (y = 24.5), Gran Salón (y = 28.5), Galería/Biblioteca (y = 32.5), Techo (y = 37.2)
            float centerX = 148f;
            float cellarFloorY = 24.5f;
            float groundFloorY = 28.5f;
            float secondFloorY = 32.5f;
            float ceilingY = 37.2f;
            float width = 20f;

            InteriorZone zone = CreateInteriorShell(
                parent, "Interior_House4_Manor", "Mansión del Magistrado", 3,
                centerX, cellarFloorY, ceilingY, width, groundLayer, wallTimber, wallStone, floorSprite, stoneBelowY: groundFloorY);

            Transform root = zone.transform;

            // Forjado de Planta Baja (con hueco hacia el Sótano en x = 141.5)
            CreateInteriorFloorSlab(root, "Floor1_LeftLanding", new Vector2(139.5f, groundFloorY), 2.4f, groundLayer, floorSprite, oneWay: true);
            GameObject groundSlab = CreateInteriorFloorSlab(root, "Floor1_Slab", new Vector2(150.2f, groundFloorY), 15.0f, groundLayer, floorSprite, oneWay: true);

            // Forjado del Segundo Piso (con hueco de escalera en x = 154.2)
            GameObject secondSlab = CreateInteriorFloorSlab(root, "Floor2_Slab", new Vector2(145.8f, secondFloorY), 14.5f, groundLayer, floorSprite, oneWay: true);
            CreateInteriorFloorSlab(root, "Floor2_RightLanding", new Vector2(156.4f, secondFloorY), 2.4f, groundLayer, floorSprite, oneWay: true);

            // Altillo de lectura en la Biblioteca (plataforma de salto en y = 34.4)
            CreateInteriorFloorSlab(root, "Library_Mezzanine", new Vector2(142.5f, 34.3f), 5.0f, groundLayer, floorSprite, oneWay: true);

            // Escaleras Interiores:
            // 1. Sótano <-> Gran Salón (x = 141.6)
            CreateLadder(root, "Ladder_Int_House4_CellarToF1", 141.6f, cellarFloorY + 0.25f, groundFloorY + 0.25f,
                ladderSprite, groundSlab.GetComponent<Collider2D>());
            // 2. Gran Salón <-> Galería 2º Piso (x = 154.0)
            CreateLadder(root, "Ladder_Int_House4_F1ToF2", 154.0f, groundFloorY + 0.25f, secondFloorY + 0.25f,
                ladderSprite, secondSlab.GetComponent<Collider2D>());

            // Puertas Interiores
            HouseDoor intGroundDoor = CreateDoor(root, "Door_Int_House4_Ground", new Vector3(144.2f, groundFloorY + 1.35f, 0f),
                "Salir al Gran Mercado (Zona 5)", groundFloorY + 0.25f, zone, doorClosed, doorOpen, doorLocked);
            LinkDoors(extGroundDoor, intGroundDoor);

            HouseDoor intBalconyDoor = CreateDoor(root, "Door_Int_House4_Balcony", new Vector3(151.0f, secondFloorY + 1.35f, 0f),
                "Salir al Balcón Señorial (2º Piso)", secondFloorY + 0.25f, zone, doorClosed, doorOpen, doorLocked);
            LinkDoors(extBalconyDoor, intBalconyDoor);

            // Nivel 0: Sótano de Archivos (Plataformas de salto, Actas y Llave Maestra del Magistrado)
            CreateSolidBoxProp(root, "Manor_Cellar_Crate1", new Vector2(146.0f, cellarFloorY + 0.75f), crate, groundLayer);
            CreateSolidBoxProp(root, "Manor_Cellar_Crate2", new Vector2(150.0f, cellarFloorY + 0.75f), crate, groundLayer);
            CreateVisualProp(root, "Manor_Cellar_Shelf", new Vector3(153.0f, cellarFloorY + 1.5f, 0f), shelf, "Gameplay", 0);
            CreateInteriorLight(root, "Light_Manor_Cellar", new Vector3(148f, cellarFloorY + 2.2f, 0f), 10f, 0.8f);

            CreateKeyItem(root, "Key_Manor_Master", new Vector3(155.5f, cellarFloorY + 0.85f, 0f),
                "key_manor_master", "Llave Maestra del Magistrado", keySprite);

            CreateLoreObject(root, "Lore_House4_Records", new Vector3(148.0f, cellarFloorY + 0.9f, 0f),
                "Actas Secretas de Salem",
                "«El magistrado guarda bajo llave en la galería superior las reliquias confiscadas en el mercado.»",
                loreSprite);

            // Nivel 1: Gran Salón
            CreateVisualProp(root, "Manor_Fireplace", new Vector3(148.0f, groundFloorY + 1.7f, 0f), fireplace, "Gameplay", 0);
            CreateVisualProp(root, "Manor_Table", new Vector3(151.2f, groundFloorY + 1.0f, 0f), table, "Gameplay", 0);
            CreateVisualProp(root, "Manor_Shelf_F1", new Vector3(139.8f, groundFloorY + 1.5f, 0f), shelf, "Gameplay", 0);
            CreateVisualProp(root, "Manor_Chandelier_F1", new Vector3(148.0f, secondFloorY - 0.6f, 0f), chandelier, "Gameplay", 1);
            CreateInteriorLight(root, "Light_Manor_F1", new Vector3(148f, groundFloorY + 2.5f, 0f), 11f, 1.0f);

            // Nivel 2: Galería Superior y Altillo de Biblioteca
            CreateVisualProp(root, "Manor_Shelf_F2_A", new Vector3(141.5f, secondFloorY + 1.5f, 0f), shelf, "Gameplay", 0);
            CreateVisualProp(root, "Manor_Shelf_F2_B", new Vector3(144.0f, secondFloorY + 1.5f, 0f), shelf, "Gameplay", 0);
            CreateVisualProp(root, "Manor_Bed_F2", new Vector3(147.5f, secondFloorY + 1.0f, 0f), bed, "Gameplay", 0);
            CreateVisualProp(root, "Manor_Window_F2", new Vector3(153.0f, secondFloorY + 2.2f, 0f), window, "Gameplay", -1);
            CreateInteriorLight(root, "Light_Manor_F2", new Vector3(148f, secondFloorY + 2.6f, 0f), 11f, 0.95f);

            // Cofre Real del Magistrado sobre el altillo/galería (requiere la Llave Maestra del sótano)
            CreateChest(root, "Chest_Int_House4_Royal", new Vector3(142.5f, 34.3f + 0.75f, 0f),
                "Cofre Real del Magistrado", 450, true, PotionType.Hypnotize, true, chestClosed, chestOpen,
                needsKey: true, keyId: "key_manor_master", keyName: "Llave Maestra del Magistrado");
        }

        // =========================================================================
        // HELPERS DE CONSTRUCCIÓN
        // =========================================================================

        private static void SetupPlayerAndManagers(Transform managersRoot, Transform gameplayRoot, Transform uiRoot)
        {
            if (managersRoot.GetComponentInChildren<KeyInventory>(true) == null)
            {
                GameObject kiObj = new GameObject("KeyInventory");
                kiObj.transform.SetParent(managersRoot, false);
                kiObj.AddComponent<KeyInventory>();
            }

            if (managersRoot.GetComponentInChildren<InteriorTransitionManager>(true) == null)
            {
                GameObject itmObj = new GameObject("InteriorTransitionManager");
                itmObj.transform.SetParent(managersRoot, false);
                itmObj.AddComponent<InteriorTransitionManager>();
            }

            if (uiRoot.GetComponentInChildren<InteractionPromptUI>(true) == null)
            {
                GameObject promptCanvasObj = new GameObject("InteractionPrompt_Canvas");
                promptCanvasObj.transform.SetParent(uiRoot, false);
                InteractionPromptUI promptUI = promptCanvasObj.AddComponent<InteractionPromptUI>();
                promptUI.EnsureUIHierarchy();
            }

            GameObject witch = gameplayRoot.Find("Witch")?.gameObject ?? GameObject.Find("Witch");
            if (witch != null)
            {
                if (witch.GetComponent<LadderClimber>() == null)
                {
                    witch.AddComponent<LadderClimber>();
                }
                if (witch.GetComponent<PlayerInteractor>() == null)
                {
                    witch.AddComponent<PlayerInteractor>();
                }
            }
        }

        private static InteriorZone CreateInteriorShell(
            Transform parent, string name, string displayName, int floors,
            float centerX, float bottomY, float topY, float width, int groundLayer,
            Sprite timberWall, Sprite stoneWall, Sprite floorPlanks, float stoneBelowY = -999f)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(centerX, bottomY, 0f);

            InteriorZone zone = root.AddComponent<InteriorZone>();
            zone.Configure(
                name.ToLowerInvariant(),
                displayName,
                floors,
                new Vector2(centerX - width * 0.5f - 1.2f, bottomY - 1.2f),
                new Vector2(centerX + width * 0.5f + 1.2f, topY + 1.2f));

            float height = topY - bottomY;

            // Fondo de paredes en mosaico
            int cols = Mathf.CeilToInt(width / 4f);
            int rows = Mathf.CeilToInt(height / 4f);
            for (int r = 0; r < rows; r++)
            {
                float wy = bottomY + r * 4f + 2f;
                Sprite wallMat = (stoneWall != null && wy < stoneBelowY) ? stoneWall : timberWall;
                for (int c = 0; c < cols; c++)
                {
                    float wx = (centerX - width * 0.5f) + c * 4f + 2f;
                    GameObject tile = new GameObject($"Wall_{r}_{c}");
                    tile.transform.SetParent(root.transform, false);
                    tile.transform.position = new Vector3(wx, wy, 0f);
                    SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = wallMat;
                    sr.sortingLayerName = "Midground";
                    sr.sortingOrder = 10;
                }
            }

            // Suelo inferior sólido (no one-way)
            CreateInteriorFloorSlab(root.transform, "Bottom_Floor_Solid", new Vector2(centerX, bottomY), width + 1.2f, groundLayer, floorPlanks, oneWay: false);

            // Techo superior sólido
            CreateInteriorFloorSlab(root.transform, "Ceiling_Solid", new Vector2(centerX, topY), width + 1.2f, groundLayer, floorPlanks, oneWay: false);

            // Pared izquierda sólida
            GameObject leftWall = new GameObject("LeftWall_Solid");
            leftWall.transform.SetParent(root.transform, false);
            leftWall.layer = groundLayer;
            leftWall.transform.position = new Vector3(centerX - width * 0.5f - 0.4f, bottomY + height * 0.5f, 0f);
            BoxCollider2D lCol = leftWall.AddComponent<BoxCollider2D>();
            lCol.size = new Vector2(0.8f, height + 1.0f);

            // Pared derecha sólida
            GameObject rightWall = new GameObject("RightWall_Solid");
            rightWall.transform.SetParent(root.transform, false);
            rightWall.layer = groundLayer;
            rightWall.transform.position = new Vector3(centerX + width * 0.5f + 0.4f, bottomY + height * 0.5f, 0f);
            BoxCollider2D rCol = rightWall.AddComponent<BoxCollider2D>();
            rCol.size = new Vector2(0.8f, height + 1.0f);

            return zone;
        }

        private static GameObject CreateInteriorFloorSlab(
            Transform parent, string name, Vector2 pos, float width, int groundLayer, Sprite plankSprite, bool oneWay)
        {
            GameObject slab = new GameObject(name);
            slab.transform.SetParent(parent, false);
            slab.layer = groundLayer;
            slab.transform.position = pos;

            SpriteRenderer sr = slab.AddComponent<SpriteRenderer>();
            sr.sprite = plankSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(width, 0.5f);
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 2;

            BoxCollider2D col = slab.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.5f);

            if (oneWay)
            {
                col.usedByEffector = true;
                PlatformEffector2D eff = slab.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                eff.useOneWayGrouping = true;
                eff.surfaceArc = 170f;
            }

            return slab;
        }

        private static GameObject CreateOneWayPlatform(
            Transform parent, string name, Vector2 pos, Sprite sprite, int groundLayer, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = groundLayer;
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = order;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3.8f, 0.36f);
            col.offset = new Vector2(0f, -0.02f);
            col.usedByEffector = true;

            PlatformEffector2D eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.useOneWayGrouping = true;
            eff.surfaceArc = 170f;

            return go;
        }

        private static LadderInteractable CreateLadder(
            Transform parent, string name, float x, float bottomY, float topY, Sprite sprite, Collider2D topPlatform = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            float height = Mathf.Max(1.5f, topY - bottomY);
            go.transform.position = new Vector3(x, bottomY + height * 0.5f, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(1.0f, height);
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 1;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            LadderInteractable ladder = go.AddComponent<LadderInteractable>();
            ladder.Configure(name.ToLowerInvariant(), bottomY, topY, 0.95f, topPlatform);
            return ladder;
        }

        private static HouseDoor CreateDoor(
            Transform parent, string name, Vector3 pos, string actionLabel, float surfaceY,
            InteriorZone interior, Sprite closed, Sprite open, Sprite locked)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = closed;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 0;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.45f, 2.2f);

            HouseDoor door = go.AddComponent<HouseDoor>();
            door.Configure(name.ToLowerInvariant(), actionLabel, surfaceY, interior, closed, open, locked);
            return door;
        }

        private static void LinkDoors(HouseDoor doorA, HouseDoor doorB)
        {
            if (doorA == null || doorB == null) return;
            doorA.TargetDoor = doorB;
            doorB.TargetDoor = doorA;
        }

        private static TreasureChest CreateChest(
            Transform parent, string name, Vector3 pos, string title, int score,
            bool givePotion, PotionType potionType, bool giveHeart,
            Sprite closed, Sprite open, bool needsKey = false, string keyId = "", string keyName = "")
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = closed;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.3f, 1.1f);

            TreasureChest chest = go.AddComponent<TreasureChest>();
            chest.Configure(name.ToLowerInvariant(), title, score, givePotion, potionType, giveHeart, closed, open, needsKey, keyId, keyName);
            return chest;
        }

        private static KeyItem CreateKeyItem(
            Transform parent, string name, Vector3 pos, string keyId, string displayName, Sprite sprite)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 3;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.65f;

            KeyItem key = go.AddComponent<KeyItem>();
            key.Configure(keyId, displayName, 150);
            return key;
        }

        private static LoreObject CreateLoreObject(
            Transform parent, string name, Vector3 pos, string title, string content, Sprite sprite)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.3f);

            LoreObject lore = go.AddComponent<LoreObject>();
            lore.Configure(name.ToLowerInvariant(), title, content, 100);
            return lore;
        }

        private static GameObject CreateVisualProp(
            Transform parent, string name, Vector3 pos, Sprite sprite, string sortingLayer, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            return go;
        }

        private static GameObject CreateSolidBoxProp(
            Transform parent, string name, Vector2 pos, Sprite sprite, int groundLayer)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = groundLayer;
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 1;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.95f, 0.95f);
            return go;
        }

        private static void CreateInteriorLight(Transform parent, string name, Vector3 pos, float radius, float intensity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            Light2D light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.86f, 0.58f, 1f);
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
        }

        private static void DestroyIfExists(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{fileName}");
        }

        public static void ConfigureVillageSprites()
        {
            string[] files =
            {
                "Village_Ladder.png",
                "Village_Balcony_Platform.png",
                "Village_Door_Closed.png",
                "Village_Door_Open.png",
                "Village_Door_Locked.png",
                "Village_House_Facade_A.png",
                "Village_House_Facade_B.png",
                "Interior_Wall_Timber.png",
                "Interior_Wall_Stone.png",
                "Interior_Floor_Planks.png",
                "Interior_Fireplace.png",
                "Interior_Bookshelf.png",
                "Interior_Table_Chairs.png",
                "Interior_Bed.png",
                "Interior_Wardrobe.png",
                "Interior_Cauldron_Rack.png",
                "Interior_Window_Shutters.png",
                "Interior_Chandelier.png",
                "Interactive_Chest_Closed.png",
                "Interactive_Chest_Open.png",
                "Interactive_Key.png",
                "Interactive_LoreBook.png"
            };

            foreach (string f in files)
            {
                string path = $"{ArtDir}/{f}";
                TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                TextureImporterSettings s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);

                bool dirty = false;
                if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
                if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (!Mathf.Approximately(imp.spritePixelsPerUnit, 64f)) { imp.spritePixelsPerUnit = 64f; dirty = true; }
                if (s.spriteMeshType != SpriteMeshType.FullRect) { s.spriteMeshType = SpriteMeshType.FullRect; dirty = true; }

                if (dirty)
                {
                    imp.SetTextureSettings(s);
                    EditorUtility.SetDirty(imp);
                    imp.SaveAndReimport();
                }
            }
        }
    }
}
