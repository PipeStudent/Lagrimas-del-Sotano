using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Herramienta de editor que arma todo el proyecto con un clic: importa los
/// sprites y audios con la configuración correcta, crea materiales físicos,
/// prefabs, la escena jugable y el build para Windows.
/// Menú: Tools > Sotano Shooter
///
/// Importante: este archivo NO referencia clases de URP directamente (el
/// post-procesado se agrega por reflexión), así compila aunque el proyecto
/// todavía no tenga URP instalado y avisa con un mensaje claro.
/// </summary>
public static class SotanoBuilder
{
    const string SpritesDir = "Assets/Sprites";
    const string AudioDir = "Assets/Audio";
    const string MaterialsDir = "Assets/Materials";
    const string PrefabsDir = "Assets/Prefabs";
    const string ScenesDir = "Assets/Scenes";
    const string SettingsDir = "Assets/Settings";
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string BuildPath = "Builds/Windows/LagrimasDelSotano.exe";

    // ---------------------------------------------------------------- Menús

    [MenuItem("Tools/Sotano Shooter/1 - Construir escena y prefabs")]
    public static void BuildAll()
    {
        if (!CheckPipeline()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Escena vacía: los objetos temporales de los prefabs no tocan tu escena actual.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        EnsureFolders();
        ConfigureSprites();
        ConfigureAudio();
        Physics2D.gravity = Vector2.zero; // juego cenital: sin gravedad

        // Materiales físicos 2D
        PhysicsMaterial2D pmPlayer = MakePhysicsMaterial("PM2D_Player", 0f, 0f);
        PhysicsMaterial2D pmEnemy = MakePhysicsMaterial("PM2D_Enemy", 0f, 0.25f);
        PhysicsMaterial2D pmWall = MakePhysicsMaterial("PM2D_Wall", 0f, 0f);

        // Prefabs
        GameObject tearPlayer = MakeTearPrefab("TearPlayer", "tear_player", 0.15f);
        GameObject tearEnemy = MakeTearPrefab("TearEnemy", "tear_enemy", 0.15f);
        GameObject playerPrefab = MakePlayerPrefab(pmPlayer, tearPlayer);
        GameObject chaser = MakeEnemyPrefab("EnemyChaser", "enemy_chaser", EnemyKind.Chaser, pmEnemy, null,
                                            6f, 2.8f, 0.4f, 1f, 10, 0f, 0f);
        GameObject shooter = MakeEnemyPrefab("EnemyShooter", "enemy_shooter", EnemyKind.Shooter, pmEnemy, tearEnemy,
                                             8f, 1.4f, 0.4f, 1f, 15, 2.2f, 4.5f);
        GameObject boss = MakeEnemyPrefab("Boss", "boss", EnemyKind.Boss, pmEnemy, tearEnemy,
                                          70f, 1.4f, 0.9f, 8f, 100, 1.7f, 4.5f);
        GameObject rock = MakeRockPrefab(pmWall);
        GameObject pickup = MakePickupPrefab();

        // ---- Escena ----
        GameObject cam = CreateCamera();
        TryAddPostProcessing(cam);

        var bg = MakeSpriteObject("Background", S("room_bg"), -10);
        bg.transform.position = Vector3.zero;

        BuildWalls(pmWall);
        Door[] doors = BuildDoors();

        var playerGO = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerGO.transform.position = Vector3.zero;

        // Gestores
        var soundGO = new GameObject("SoundManager");
        var sound = soundGO.AddComponent<SoundManager>();
        sound.bgm = LoadClip("bgm_loop");
        sound.shoot = LoadClip("sfx_shoot");
        sound.enemyHit = LoadClip("sfx_enemy_hit");
        sound.enemyDie = LoadClip("sfx_enemy_die");
        sound.hurt = LoadClip("sfx_hurt");
        sound.pickup = LoadClip("sfx_pickup");
        sound.door = LoadClip("sfx_door");
        sound.win = LoadClip("sfx_win");
        sound.lose = LoadClip("sfx_lose");

        var floorGO = new GameObject("FloorManager");
        var floor = floorGO.AddComponent<FloorManager>();
        floor.rockPrefab = rock;
        floor.chaserPrefab = chaser;
        floor.shooterPrefab = shooter;
        floor.bossPrefab = boss;
        floor.pickupPrefab = pickup;
        floor.pickupSprites = new[] { S("heart_full"), S("item_damage"), S("item_rate"), S("item_speed") };
        floor.doors = doors;

        var gcGO = new GameObject("GameController");
        var gc = gcGO.AddComponent<GameController>();
        gc.player = playerGO.GetComponent<Player>();
        gc.floor = floor;

        var uiGO = new GameObject("HudUI");
        var hud = uiGO.AddComponent<HudUI>();
        hud.heartFull = T("heart_full");
        hud.heartHalf = T("heart_half");
        hud.heartEmpty = T("heart_empty");

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        ConfigurePlayerSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Sotano Shooter] Escena y prefabs construidos en " + ScenePath);
        EditorUtility.DisplayDialog("Lágrimas del Sótano",
            "Escena, prefabs y configuración listos.\n\nPulsa Play para probar (la escena Game ya está abierta).\n" +
            "Luego usa Tools > Sotano Shooter > 2 - Compilar build Windows.", "OK");
    }

    [MenuItem("Tools/Sotano Shooter/2 - Compilar build Windows")]
    public static void BuildWindows()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Lágrimas del Sótano",
                "Primero ejecuta: Tools > Sotano Shooter > 1 - Construir escena y prefabs.", "OK");
            return;
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            EditorUtility.DisplayDialog("Lágrimas del Sótano",
                "Falta el módulo \"Windows Build Support\" de esta versión de Unity.\n" +
                "Instálalo desde Unity Hub > Installs > (tu versión) > Add modules.", "OK");
            return;
        }

        ConfigurePlayerSettings();

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[Sotano Shooter] Build OK: " + report.summary.outputPath +
                      " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
            EditorUtility.RevealInFinder(Path.GetFullPath(BuildPath));
        }
        else
        {
            Debug.LogError("[Sotano Shooter] Falló el build: " + report.summary.result);
        }
    }

    [MenuItem("Tools/Sotano Shooter/3 - Construir todo y compilar")]
    public static void BuildEverything()
    {
        BuildAll();
        BuildWindows();
    }

    // -------------------------------------------------------- Configuración

    static bool CheckPipeline()
    {
        RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
        if (rp != null && rp.GetType().Name.Contains("Universal")) return true;

        EditorUtility.DisplayDialog("Lágrimas del Sótano",
            "Este proyecto no usa URP (Universal Render Pipeline).\n\n" +
            "Crea el proyecto en Unity Hub con la plantilla \"Universal 2D\" " +
            "(o \"Universal 3D\") y copia la carpeta Assets dentro.", "OK");
        return false;
    }

    static void EnsureFolders()
    {
        foreach (string dir in new[] { MaterialsDir, PrefabsDir, ScenesDir, SettingsDir })
        {
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(dir));
        }
    }

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "Estudiante";
        PlayerSettings.productName = "Lagrimas del Sotano";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
    }

    /// <summary>Sprites nítidos (pixel art): 32 px por unidad, filtro Point, sin compresión.</summary>
    static void ConfigureSprites()
    {
        AssetDatabase.Refresh();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    /// <summary>La música se transmite (streaming) y los efectos se descomprimen al cargar.</summary>
    static void ConfigureAudio()
    {
        AssetDatabase.Refresh();
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;

            bool isMusic = Path.GetFileNameWithoutExtension(path).StartsWith("bgm");
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            if (isMusic) s.quality = 0.6f;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }
    }

    static Sprite S(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/" + name + ".png");
        if (sprite == null) Debug.LogWarning("[Sotano Shooter] No se encontró el sprite: " + name);
        return sprite;
    }

    static Texture2D T(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SpritesDir + "/" + name + ".png");
    }

    static AudioClip LoadClip(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/" + name + ".wav");
        if (clip == null) Debug.LogWarning("[Sotano Shooter] No se encontró el audio: " + name);
        return clip;
    }

    // ------------------------------------------------------------ Física 2D

    static PhysicsMaterial2D MakePhysicsMaterial(string name, float friction, float bounciness)
    {
        string path = MaterialsDir + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);

        var pm = new PhysicsMaterial2D(name) { friction = friction, bounciness = bounciness };
        AssetDatabase.CreateAsset(pm, path);
        return pm;
    }

    // --------------------------------------------------------------- Prefabs

    static GameObject MakeSpriteObject(string name, Sprite sprite, int sortingOrder)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    static Rigidbody2D AddBody(GameObject go, float mass, bool rotates = false)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = mass;
        rb.freezeRotation = !rotates;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        return rb;
    }

    static GameObject MakePlayerPrefab(PhysicsMaterial2D pm, GameObject tearPrefab)
    {
        var go = MakeSpriteObject("Player", S("player"), 4);
        AddBody(go, 1f);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.38f;
        col.sharedMaterial = pm;

        var player = go.AddComponent<Player>();
        player.tearPrefab = tearPrefab;
        return SavePrefab(go, "Player");
    }

    static GameObject MakeTearPrefab(string name, string spriteName, float radius)
    {
        var go = MakeSpriteObject(name, S(spriteName), 5);
        var rb = AddBody(go, 0.1f);
        rb.interpolation = RigidbodyInterpolation2D.None;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;

        go.AddComponent<Tear>();
        return SavePrefab(go, name);
    }

    static GameObject MakeEnemyPrefab(string name, string spriteName, EnemyKind kind, PhysicsMaterial2D pm,
                                      GameObject tearPrefab, float hp, float speed, float radius, float mass,
                                      int score, float fireInterval, float tearSpeed)
    {
        var go = MakeSpriteObject(name, S(spriteName), 3);
        var rb = AddBody(go, mass);
        rb.linearDamping = 3f; // el empujón de un golpe se apaga solo

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.sharedMaterial = pm;

        var enemy = go.AddComponent<Enemy>();
        enemy.kind = kind;
        enemy.maxHealth = hp;
        enemy.speed = speed;
        enemy.scoreValue = score;
        enemy.tearPrefab = tearPrefab;
        if (fireInterval > 0f) enemy.fireInterval = fireInterval;
        if (tearSpeed > 0f) enemy.tearSpeed = tearSpeed;
        return SavePrefab(go, name);
    }

    static GameObject MakeRockPrefab(PhysicsMaterial2D pm)
    {
        var go = MakeSpriteObject("Rock", S("rock"), 2);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.95f, 0.95f);
        col.sharedMaterial = pm;
        return SavePrefab(go, "Rock");
    }

    static GameObject MakePickupPrefab()
    {
        var go = MakeSpriteObject("Pickup", S("heart_full"), 2);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;
        go.AddComponent<Pickup>();
        return SavePrefab(go, "Pickup");
    }

    static GameObject SavePrefab(GameObject instance, string name)
    {
        string path = PrefabsDir + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    // --------------------------------------------------------------- Escena

    static GameObject CreateCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f; // la sala completa (18 x 12) cabe en pantalla
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.02f, 0.05f);
        go.AddComponent<AudioListener>();
        go.AddComponent<CameraShake>();
        go.transform.position = new Vector3(0f, 0f, -10f);
        return go;
    }

    // Paredes sólidas alrededor del interior de 15 x 9 unidades (borde de 1.5).
    static void BuildWalls(PhysicsMaterial2D pm)
    {
        var root = new GameObject("Walls");
        MakeWall(root, "WallTop", new Vector2(0f, 5.25f), new Vector2(18f, 1.5f), pm);
        MakeWall(root, "WallBottom", new Vector2(0f, -5.25f), new Vector2(18f, 1.5f), pm);
        MakeWall(root, "WallLeft", new Vector2(-8.25f, 0f), new Vector2(1.5f, 12f), pm);
        MakeWall(root, "WallRight", new Vector2(8.25f, 0f), new Vector2(1.5f, 12f), pm);
    }

    static void MakeWall(GameObject parent, string name, Vector2 position, Vector2 size, PhysicsMaterial2D pm)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = position;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = pm;
    }

    // Las 4 puertas usan el mismo sprite (orientado hacia arriba) rotado según la pared.
    static Door[] BuildDoors()
    {
        Vector2[] positions = { new Vector2(0f, 5.25f), new Vector2(8.25f, 0f), new Vector2(0f, -5.25f), new Vector2(-8.25f, 0f) };
        float[] angles = { 0f, -90f, 180f, 90f };
        string[] names = { "Door_Up", "Door_Right", "Door_Down", "Door_Left" };

        var root = new GameObject("Doors");
        var doors = new Door[4];
        for (int i = 0; i < 4; i++)
        {
            var go = MakeSpriteObject(names[i], S("door_closed"), 1);
            go.transform.SetParent(root.transform, false);
            go.transform.position = positions[i];
            go.transform.rotation = Quaternion.Euler(0f, 0f, angles[i]);

            // Zona de paso en la parte interior de la puerta (en coordenadas locales, "abajo").
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, -0.6f);
            col.size = new Vector2(1.6f, 1f);

            var door = go.AddComponent<Door>();
            door.direction = i;
            door.openSprite = S("door_open");
            door.closedSprite = S("door_closed");
            doors[i] = door;
        }
        return doors;
    }

    // ------------------------------------------- Post-procesado (opcional, URP)

    /// <summary>
    /// Agrega viñeta y un bloom leve con un Volume global y activa el
    /// post-procesado en la cámara. Se hace por reflexión para no depender
    /// de las clases de URP en tiempo de compilación; si algo falla solo avisa.
    /// </summary>
    static void TryAddPostProcessing(GameObject camGO)
    {
        try
        {
            System.Type volumeType = FindType("UnityEngine.Rendering.Volume");
            System.Type profileType = FindType("UnityEngine.Rendering.VolumeProfile");
            System.Type vignetteType = FindType("UnityEngine.Rendering.Universal.Vignette");
            System.Type bloomType = FindType("UnityEngine.Rendering.Universal.Bloom");
            System.Type camDataType = FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (volumeType == null || profileType == null || vignetteType == null || camDataType == null)
            {
                Debug.LogWarning("[Sotano Shooter] No se encontró el post-procesado de URP; se omite (el juego funciona igual).");
                return;
            }

            // Cámara: activar post-procesado.
            Component camData = camGO.GetComponent(camDataType) ?? camGO.AddComponent(camDataType);
            camDataType.GetProperty("renderPostProcessing").SetValue(camData, true);

            // Perfil con viñeta y bloom suave.
            string path = SettingsDir + "/SotanoVolumeProfile.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance(profileType);
            AssetDatabase.CreateAsset(profile, path);

            var add = profileType.GetMethod("Add", new[] { typeof(System.Type), typeof(bool) });
            object vignette = add.Invoke(profile, new object[] { vignetteType, true });
            SetParameter(vignette, "intensity", 0.38f);
            AssetDatabase.AddObjectToAsset((Object)vignette, profile);

            if (bloomType != null)
            {
                object bloom = add.Invoke(profile, new object[] { bloomType, true });
                SetParameter(bloom, "threshold", 1f);
                SetParameter(bloom, "intensity", 0.25f);
                AssetDatabase.AddObjectToAsset((Object)bloom, profile);
            }
            EditorUtility.SetDirty(profile);

            var volGO = new GameObject("Global Volume");
            Component volume = volGO.AddComponent(volumeType);
            volumeType.GetProperty("isGlobal").SetValue(volume, true);
            volumeType.GetProperty("sharedProfile").SetValue(volume, profile);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Sotano Shooter] No se pudo agregar el post-procesado: " + e.Message);
        }
    }

    // Llama a VolumeParameter.Override(valor) en el campo indicado de un VolumeComponent.
    static void SetParameter(object component, string field, float value)
    {
        object parameter = component.GetType().GetField(field).GetValue(component);
        parameter.GetType().GetMethod("Override", new[] { typeof(float) }).Invoke(parameter, new object[] { value });
    }

    static System.Type FindType(string fullName)
    {
        foreach (System.Reflection.Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type t = a.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }
}
