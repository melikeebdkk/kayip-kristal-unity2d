using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class KayipKristalSetup
{
    private const string ScenePath = "Assets/Scenes/SampleLevel.unity";
    private const string Level2Path = "Assets/Scenes/Level2_GreenRuins.unity";
    private const string Level3Path = "Assets/Scenes/Level3_CrimsonKeep.unity";
    private const string BuildPath = "Builds/KayipKristalDemo/KayipKristal.exe";
    private const string ExitCastleSpritePath = "Assets/Graphics/Artworks/Generated/ExitCastle_WIN.png";
    private static readonly Vector2 PreferredPlayerStart = new(-3.5f, -1.9f);

    [MenuItem("Kayip Kristal/Prepare Demo Scene")]
    public static void PrepareDemoScene()
    {
        ConfigureExitCastleAsset();
        EditorSceneManager.OpenScene(ScenePath);

        RemoveCleanReplacementLayout();
        RestoreOriginalAtmosphere();

        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = Object.FindObjectOfType<Platformer2DController>()?.gameObject;
        RemoveExtraPlayerActors(player);

        if (player != null)
        {
            player.tag = "Player";
            RespawnOnHazard respawnOnHazard = player.GetComponent<RespawnOnHazard>();
            if (respawnOnHazard == null)
                respawnOnHazard = player.AddComponent<RespawnOnHazard>();
            respawnOnHazard.ShowGameOverInsteadOfRespawn = false;
            respawnOnHazard.IgnoreHazardsWhileAscending = true;
            ConfigurePlayerCombat(player);
            EnsurePlayerStartsOnGround(player);
            RestoreOriginalPlayerLook(player);
            ConfigureCrouch(player);
            RemovePlayerAttachedLampArtifacts(player);
        }

        EnsureGameManager();
        ConfigureAudio(player);
        ConfigureDarkAmbience();
        MissionState mission = EnsureMissionState();
        int crystalCount = ConfigureCollectables();
        mission.RequiredCrystals = Mathf.Max(1, Mathf.Min(5, crystalCount));
        mission.ScorePerCrystal = 10;
        mission.WinSceneName = "";

        RestorePushableBoxes();
        RemoveJumpAssistBlocks();
        RemoveMisplacedPressureCrate();
        RemoveUnscopedSpikeCrateBridge();
        FixEarlySpikeJump();
        FixOnlyBlockingSecondFloorDoor();
        EnsureCheckpoint(player);
        EnsureExitZone(player);
        EnsureHud();
        ConfigureCamera(player);
        RemoveLooseProjectiles();
        ApplyLevelTheme(1);
        ConfigureBuildScenes();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Kayip Kristal demo scene prepared.");
    }

    public static void BuildWindows()
    {
        PrepareDemoScene();
        CreateThemedLevel(ScenePath, Level2Path, 2);
        CreateThemedLevel(ScenePath, Level3Path, 3);
        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/TitleScreen.unity", ScenePath, Level2Path, Level3Path },
            BuildPath,
            BuildTarget.StandaloneWindows64,
            BuildOptions.None);
    }

    [MenuItem("Kayip Kristal/Build Saved Scenes")]
    public static void BuildSavedScenes()
    {
        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/TitleScreen.unity", ScenePath, Level2Path, Level3Path },
            BuildPath,
            BuildTarget.StandaloneWindows64,
            BuildOptions.None);
    }

    [MenuItem("Kayip Kristal/Validate Player Start")]
    public static void ValidatePlayerStart()
    {
        EditorSceneManager.OpenScene(ScenePath);

        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = Object.FindObjectOfType<Platformer2DController>()?.gameObject;
        RemoveExtraPlayerActors(player);

        if (player == null)
        {
            Debug.LogError("Kayip Kristal validation failed: Player was not found.");
            return;
        }

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            Debug.LogError("Kayip Kristal validation failed: Player has no Collider2D.");
            return;
        }

        Vector2 origin = new(playerCollider.bounds.center.x, playerCollider.bounds.min.y + 0.05f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 2f);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider == playerCollider || hit.collider.isTrigger)
                continue;

            Debug.Log($"Kayip Kristal validation passed: Player start is supported by '{hit.collider.name}' at distance {hit.distance:0.00}.");
            return;
        }

        Debug.LogError($"Kayip Kristal validation failed: Player start at {player.transform.position} has no solid support below it.");
    }

    private static void RemoveCleanReplacementLayout()
    {
        GameObject replacement = GameObject.Find("KayipKristal_DemoLayout");
        if (replacement != null)
            Object.DestroyImmediate(replacement);
    }

    private static void RemoveExtraPlayerActors(GameObject mainPlayer)
    {
        foreach (Platformer2DController controller in Object.FindObjectsOfType<Platformer2DController>())
        {
            GameObject playerObject = controller.gameObject;
            if (playerObject == mainPlayer)
                continue;

            if (playerObject.CompareTag("Player") || playerObject.name.Contains("Player"))
                Object.DestroyImmediate(playerObject);
        }
    }

    private static void RestoreOriginalAtmosphere()
    {
        string[] originalGroups = { "Map", "Jewels", "Doors & Switches", "Enemies", "Spikes" };
        foreach (string groupName in originalGroups)
        {
            GameObject group = FindSceneObjectByName(groupName);
            if (group != null)
                group.SetActive(true);
        }
    }

    private static void RestoreOriginalPlayerLook(GameObject player)
    {
        SpriteRenderer bodyRenderer = player.GetComponent<SpriteRenderer>();
        if (bodyRenderer != null)
        {
            bodyRenderer.enabled = true;
            bodyRenderer.color = Color.white;
            bodyRenderer.sortingOrder = Mathf.Max(bodyRenderer.sortingOrder, 20);
        }

        Transform customLook = player.transform.Find("KayipKristal_ExplorerStyle");
        if (customLook != null)
            Object.DestroyImmediate(customLook.gameObject);
    }

    private static GameObject CreateStyledSprite(Transform parent, Sprite sprite, string name, Vector2 localPosition, Vector2 localScale, float rotationZ, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.03f);
        go.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    private static void ConfigureCrouch(GameObject player)
    {
        Platformer2D platformer = player.GetComponent<Platformer2D>();
        if (platformer == null) return;

        platformer.CrouchColliderHeightMultiplier = 0.42f;
        platformer.CrouchSpeedMultiplier = 0.46f;
        platformer.StandUpCheckSkin = 0.04f;
        platformer.CrouchVisualRoot = null;
        platformer.CrouchVisualScale = new Vector2(1.08f, 0.62f);
        platformer.CrouchVisualOffset = new Vector2(0f, -0.24f);
    }

    private static void ConfigurePlayerCombat(GameObject player)
    {
        Actor actor = player.GetComponent<Actor>();
        if (actor != null)
        {
            actor.DamageSourcesTags = new[] { "EnemyDamage" };
            actor.IgnoreEnemyContactDamageWhileAscending = true;
            EditorUtility.SetDirty(actor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
        }

        Thrower thrower = player.GetComponent<Thrower>();
        if (thrower == null)
            thrower = player.AddComponent<Thrower>();

        thrower.Projectile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerProjectile.prefab");
        thrower.ProjectileLocalScale = new Vector3(0.36f, 0.36f, 1f);
        thrower.ThrowingLocalOrigin = new Vector3(0.58f, 0.45f, 0f);
        thrower.ThrowingForce = Vector3.right;
        thrower.UsePhysics2D = true;
        thrower.AutoThrow = false;
        thrower.AimAtPlayer = false;
        thrower.ProjectileSpeed = 6.2f;
        thrower.ProjectileActorDamage = 3;
        thrower.OverrideProjectileColor = true;
        thrower.ProjectileColor = new Color(1f, 0.72f, 0.12f, 1f);
        EditorUtility.SetDirty(thrower);
        PrefabUtility.RecordPrefabInstancePropertyModifications(thrower);

        if (player.GetComponent<ThrowerController>() == null)
            player.AddComponent<ThrowerController>();
    }

    private static void RemovePlayerAttachedLampArtifacts(GameObject player)
    {
        if (player == null) return;

        foreach (Light2D light in player.GetComponentsInChildren<Light2D>(true))
            Object.DestroyImmediate(light.gameObject);

        foreach (ShadowCaster2D shadowCaster in player.GetComponentsInChildren<ShadowCaster2D>(true))
            Object.DestroyImmediate(shadowCaster);

        string[] playerOnlyLampNames = { "Light 2D", "Lantern", "Lamp", "Lamp_Handle", "Lamp_Glow", "Torch", "Glow" };
        foreach (string lampName in playerOnlyLampNames)
        {
            Transform lamp = player.transform.Find(lampName);
            if (lamp != null)
                Object.DestroyImmediate(lamp.gameObject);
        }
    }

    private static void ConfigureDarkAmbience()
    {
        Camera camera = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (camera != null)
            camera.backgroundColor = new Color(0.015f, 0.016f, 0.018f, 1f);

        foreach (Light2D light in Object.FindObjectsOfType<Light2D>())
        {
            if (light.name.Contains("Global"))
            {
                light.color = new Color(0.28f, 0.27f, 0.25f, 1f);
                light.intensity = 0.5f;
            }
        }

        foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>())
        {
            if (renderer.GetComponentInParent<Platformer2DController>() != null) continue;
            if (renderer.name.Contains("Jewel") || renderer.name.Contains("Crystal") || renderer.name.Contains("ExitMarker")) continue;

            if (renderer.name.Contains("Tilemap") || renderer.transform.root.name.Contains("Map"))
                renderer.color = Color.Lerp(renderer.color, new Color(0.08f, 0.08f, 0.08f, renderer.color.a), 0.55f);
        }

        foreach (Collectable collectable in Object.FindObjectsOfType<Collectable>())
        {
            SpriteRenderer renderer = collectable.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(1f, 0.08f, 0.06f, 1f);
                renderer.sortingOrder = 32;
            }

            AddPointLight(collectable.transform, "Crystal_Red_Light", new Color(1f, 0.04f, 0.03f, 1f), 1.35f, 2.4f);
        }
    }

    private static Light2D AddPointLight(Transform parent, string name, Color color, float intensity, float radius)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        Light2D light = go.AddComponent<Light2D>();
        light.color = color;
        light.intensity = intensity;
        light.pointLightInnerRadius = radius * 0.25f;
        light.pointLightOuterRadius = radius;
        light.falloffIntensity = 0.72f;
        return light;
    }

    private static void RestorePushableBoxes()
    {
        GameObject boxes = FindSceneObjectByName("Boxes");
        if (boxes != null)
            boxes.SetActive(true);
    }

    private static void EnsurePlayerStartsOnGround(GameObject player)
    {
        if (player == null) return;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null) return;

        float playerCenterOffsetX = player.transform.position.x - playerCollider.bounds.center.x;
        float playerBottomOffset = player.transform.position.y - playerCollider.bounds.min.y;
        float rayStartY = PreferredPlayerStart.y + 4f;
        float rayDistance = 20f;

        if (TryMovePlayerToSupport(player, playerCollider, PreferredPlayerStart.x, rayStartY, rayDistance, playerCenterOffsetX, playerBottomOffset, requireTilemap: true))
            return;

        for (float offset = 0.5f; offset <= 12f; offset += 0.5f)
        {
            if (TryMovePlayerToSupport(player, playerCollider, PreferredPlayerStart.x - offset, rayStartY, rayDistance, playerCenterOffsetX, playerBottomOffset, requireTilemap: true))
                return;
            if (TryMovePlayerToSupport(player, playerCollider, PreferredPlayerStart.x + offset, rayStartY, rayDistance, playerCenterOffsetX, playerBottomOffset, requireTilemap: true))
                return;
        }

        if (TryMovePlayerToSupport(player, playerCollider, PreferredPlayerStart.x, rayStartY, rayDistance, playerCenterOffsetX, playerBottomOffset, requireTilemap: false))
            return;

        Debug.LogWarning("Kayip Kristal could not find a solid start platform for Player.");
    }

    private static bool TryMovePlayerToSupport(GameObject player, Collider2D playerCollider, float x, float rayStartY, float rayDistance, float playerCenterOffsetX, float playerBottomOffset, bool requireTilemap)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, rayStartY), Vector2.down, rayDistance);
        foreach (RaycastHit2D hit in hits)
        {
            if (!IsSolidStartSupport(hit, playerCollider)) continue;
            if (requireTilemap && hit.collider.name != "Tilemap") continue;

            player.transform.position = new Vector3(x + playerCenterOffsetX, hit.point.y + playerBottomOffset + 0.08f, player.transform.position.z);
            PlayerRespawnPoint.Set(player.transform.position);
            Debug.Log($"Kayip Kristal player start moved to {player.transform.position} above '{hit.collider.name}'.");
            return true;
        }

        return false;
    }

    private static bool IsSolidStartSupport(RaycastHit2D hit, Collider2D playerCollider)
    {
        Collider2D candidate = hit.collider;
        if (candidate == null || candidate == playerCollider || candidate.isTrigger) return false;
        if (!candidate.gameObject.activeInHierarchy) return false;
        if (candidate.attachedRigidbody != null && candidate.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) return false;
        if (hit.normal.y < 0.5f) return false;
        return true;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name != objectName) continue;
            if (!go.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            return go;
        }

        return null;
    }

    private static MissionState EnsureMissionState()
    {
        MissionState mission = Object.FindObjectOfType<MissionState>();
        if (mission != null) return mission;

        GameObject go = new GameObject("MissionState");
        return go.AddComponent<MissionState>();
    }

    private static void EnsureGameManager()
    {
        GameManager manager = Object.FindObjectOfType<GameManager>();
        if (manager != null)
        {
            manager.EscapeQuitsApplication = true;
            return;
        }

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>().EscapeQuitsApplication = true;
    }

    private static void ConfigureAudio(GameObject player)
    {
        AudioClip jump = LoadSound("Jump.wav");
        AudioClip landing = LoadSound("Landing.wav");
        AudioClip footstep1 = LoadSound("Footstep1.wav");
        AudioClip footstep2 = LoadSound("Footstep2.wav");
        AudioClip footstep3 = LoadSound("Footstep3.wav");
        AudioClip coin = LoadSound("Coin.wav");
        AudioClip hit = LoadSound("Hit.wav");
        AudioClip blip1 = LoadSound("Blip1.wav");
        AudioClip blip2 = LoadSound("Blip2.wav");
        AudioClip zap = LoadSound("Zap.wav");
        AudioClip ambience = LoadSound("Sewer.mp3");

        EnsureAudioManager(ambience);

        if (player != null)
        {
            Platformer2D platformer = player.GetComponent<Platformer2D>();
            if (platformer != null)
            {
                platformer.AudioEnabled = true;
                platformer.JumpingSoundFX = jump;
                platformer.LandingSoundFX = landing;
                platformer.MovingSoundFXs = new[] { footstep1, footstep2, footstep3 };
            }

            RespawnOnHazard respawn = player.GetComponent<RespawnOnHazard>();
            if (respawn != null)
            {
                respawn.ShowGameOverInsteadOfRespawn = false;
                respawn.IgnoreHazardsWhileAscending = true;
                respawn.RespawnSoundFX = hit;
            }
        }

        foreach (Collectable collectable in Object.FindObjectsOfType<Collectable>())
            collectable.CollectingSoundFX = coin;

        foreach (Switch pressureSwitch in Object.FindObjectsOfType<Switch>())
        {
            pressureSwitch.TurnOnSoundFX = blip1;
            pressureSwitch.TurnOffSoundFX = blip2;
        }

        foreach (Door door in Object.FindObjectsOfType<Door>())
        {
            door.OpenSoundFX = blip2;
            door.CloseSoundFX = zap;
        }
    }

    private static void EnsureAudioManager(AudioClip ambience)
    {
        AudioManager manager = Object.FindObjectOfType<AudioManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("AudioManager");
            manager = go.AddComponent<AudioManager>();
        }

        AudioSource music = EnsureChildAudioSource(manager.transform, "Music");
        music.clip = ambience;
        music.loop = true;
        music.playOnAwake = ambience != null;
        music.volume = 0.25f;

        AudioSource soundFX = EnsureChildAudioSource(manager.transform, "SoundFX");
        soundFX.playOnAwake = false;
        soundFX.volume = 0.85f;
    }

    private static AudioSource EnsureChildAudioSource(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);

        AudioSource audioSource = go.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = go.AddComponent<AudioSource>();

        return audioSource;
    }

    private static AudioClip LoadSound(string fileName) =>
        AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/SoundFX/{fileName}");

    private static int ConfigureCollectables()
    {
        Collectable[] collectables = Object.FindObjectsOfType<Collectable>();
        foreach (Collectable collectable in collectables)
        {
            collectable.CrystalValue = 1;
            collectable.AttractDistance = 1.8f;
            collectable.AttractSpeed = 10f;

            Rigidbody2D body = collectable.GetComponent<Rigidbody2D>();
            if (body == null) continue;

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        return collectables.Length;
    }

    private static void RemoveJumpAssistBlocks()
    {
        GameObject existing = GameObject.Find("KayipKristal_JumpAssistBlocks");
        if (existing != null)
            Object.DestroyImmediate(existing);
    }

    private static void RemoveMisplacedPressureCrate()
    {
        GameObject existing = GameObject.Find("KayipKristal_PressureCrate");
        if (existing != null)
            Object.DestroyImmediate(existing);
    }

    private static void RemoveUnscopedSpikeCrateBridge()
    {
        GameObject existing = GameObject.Find("KayipKristal_SpikeCrateBridge");
        if (existing != null)
            Object.DestroyImmediate(existing);
    }

    private static void FixEarlySpikeJump()
    {
        foreach (Transform spike in FindSceneTransformsByNamePrefix("Spikes"))
        {
            Vector3 position = spike.position;
            if (position.x > 5f && position.x < 8f && position.y > 3f && position.y < 6f)
                spike.gameObject.SetActive(false);
        }
    }

    private static void FixOnlyBlockingSecondFloorDoor()
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            if (!go.name.StartsWith("Door&Switch")) continue;

            go.SetActive(true);

            foreach (Door door in go.GetComponentsInChildren<Door>(true))
            {
                door.IsOpen = false;
                Animator animator = door.GetComponent<Animator>();
                if (animator != null)
                    animator.enabled = true;
            }

            foreach (Collider2D collider in go.GetComponentsInChildren<Collider2D>(true))
                collider.enabled = true;
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
        }
    }

    private static System.Collections.Generic.IEnumerable<Transform> FindSceneTransformsByNamePrefix(string prefix)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            if (!go.name.StartsWith(prefix)) continue;
            yield return go.transform;
        }
    }

    private static void CreateAssistBlock(Transform parent, Sprite sprite, string name, Vector2 position, Vector2 size)
    {
        GameObject block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.position = position;
        block.transform.localScale = new Vector3(size.x, size.y, 1f);
        block.layer = LayerMask.NameToLayer("Obstacle");

        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.32f, 0.34f, 0.35f, 1f);
        renderer.sortingOrder = 20;

        BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private static void EnsureCheckpoint(GameObject player)
    {
        Checkpoint2D existingCheckpoint = Object.FindObjectOfType<Checkpoint2D>();
        if (existingCheckpoint != null) return;

        Vector3 position = player != null ? player.transform.position + new Vector3(6f, 1f, 0f) : new Vector3(0f, 1f, 0f);
        GameObject checkpoint = new GameObject("Checkpoint_MidLevel");
        checkpoint.transform.position = position;
        BoxCollider2D collider = checkpoint.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.5f, 2f);
        checkpoint.AddComponent<Checkpoint2D>();
    }

    private static void EnsureExitZone(GameObject player)
    {
        Bounds bounds = ResolvePlayableBounds(player);
        LevelExit existingExit = Object.FindObjectOfType<LevelExit>();
        GameObject exit = existingExit != null ? existingExit.gameObject : new GameObject("CrystalExit_Trigger");
        exit.transform.position = ResolveExitPosition(bounds, player);

        BoxCollider2D collider = exit.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = exit.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.8f, 2.8f);
        if (exit.GetComponent<LevelExit>() == null)
            exit.AddComponent<LevelExit>();

        RemoveExitAreaClutter(exit.transform.position);
        EnsureExitCastle(exit.transform);
    }

    private static Vector3 ResolveExitPosition(Bounds playableBounds, GameObject player)
    {
        float top = playableBounds.max.y + 4f;
        float distance = playableBounds.size.y + 8f;
        float goalX = Mathf.Min(playableBounds.max.x - 4f, ResolveLastCollectableX(player) + 4.5f);
        if (player != null)
            goalX = Mathf.Min(goalX, player.transform.position.x + 32f);
        goalX = Mathf.Max(goalX, playableBounds.min.x + 8f);

        for (float x = goalX; x >= playableBounds.min.x + 3f; x -= 1f)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, top), Vector2.down, distance);
            foreach (RaycastHit2D hit in hits)
            {
                if (!IsSolidExitSupport(hit)) continue;
                return new Vector3(x, hit.point.y + 1.25f, 0f);
            }
        }

        return new Vector3(goalX, playableBounds.center.y, 0f);
    }

    private static float ResolveLastCollectableX(GameObject player)
    {
        float fallback = player != null ? player.transform.position.x + 18f : 18f;
        Collectable[] collectables = Object.FindObjectsOfType<Collectable>();
        if (collectables.Length == 0) return fallback;

        float maxX = collectables[0].transform.position.x;
        foreach (Collectable collectable in collectables)
            maxX = Mathf.Max(maxX, collectable.transform.position.x);
        return maxX;
    }

    private static bool IsSolidExitSupport(RaycastHit2D hit)
    {
        Collider2D candidate = hit.collider;
        if (candidate == null || candidate.isTrigger) return false;
        if (!candidate.gameObject.activeInHierarchy) return false;
        if (candidate.attachedRigidbody != null && candidate.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) return false;
        if (hit.normal.y < 0.5f) return false;
        return candidate.GetComponent<TilemapCollider2D>() != null || candidate.name == "Tilemap";
    }

    private static void EnsureExitMarker(Transform exitTransform)
    {
        Transform existingMarker = exitTransform.Find("ExitMarker");
        GameObject marker = existingMarker != null ? existingMarker.gameObject : new GameObject("ExitMarker");
        marker.transform.SetParent(exitTransform, false);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(0.75f, 1.5f, 1f);

        SpriteRenderer renderer = marker.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = marker.AddComponent<SpriteRenderer>();

        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphics/Artworks/Sprites/Square.png");
        renderer.enabled = true;
        renderer.color = new Color(1f, 0.76f, 0.15f, 0.55f);
        renderer.sortingOrder = 30;
    }

    private static void CreateThemedLevel(string sourcePath, string targetPath, int level)
    {
        if (File.Exists(targetPath))
            AssetDatabase.DeleteAsset(targetPath);

        AssetDatabase.CopyAsset(sourcePath, targetPath);
        EditorSceneManager.OpenScene(targetPath);
        ApplyLevelTheme(level);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void ApplyLevelTheme(int level)
    {
        ResolveTheme(level, out Color background, out Color mapTint, out Color crystalColor, out Color crystalLight, out Color exitColor, out Color enemyColor, out Color stone, out Color roof, out string objective);

        Camera camera = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (camera != null)
            camera.backgroundColor = background;

        foreach (Light2D light in Object.FindObjectsOfType<Light2D>())
        {
            if (light.name.Contains("Global"))
            {
                light.color = Color.Lerp(Color.white, crystalLight, 0.28f);
                light.intensity = 0.86f;
            }
        }

        foreach (Tilemap tilemap in Object.FindObjectsOfType<Tilemap>())
            tilemap.color = mapTint;

        foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>())
        {
            if (renderer.GetComponentInParent<Platformer2DController>() != null) continue;
            if (renderer.name.Contains("ExitMarker") || renderer.name.Contains("ExitCastle")) continue;

            if (renderer.GetComponentInParent<Collectable>() != null)
            {
                renderer.color = crystalColor;
                continue;
            }

            Actor actor = renderer.GetComponentInParent<Actor>();
            if (actor != null && actor.GetComponent<Platformer2DController>() == null)
            {
                renderer.color = enemyColor;
                continue;
            }

            if (renderer.transform.root.name.Contains("Map") || renderer.name.Contains("Tilemap"))
                renderer.color = mapTint;
        }

        foreach (Collectable collectable in Object.FindObjectsOfType<Collectable>())
        {
            AddPointLight(collectable.transform, "Crystal_Theme_Light", crystalLight, 1.35f, 2.6f);
        }

        ConfigureThemedEnemies(enemyColor, level);
        ConfigureLevelDoorSwitchColors(level);
        AddDifficultyProgression(level, crystalColor, crystalLight, enemyColor);

        MissionState mission = Object.FindObjectOfType<MissionState>();
        if (mission != null)
        {
            mission.RequiredCrystals = level == 2 ? 6 : level == 3 ? 7 : 5;
            mission.ScorePerCrystal = level == 1 ? 10 : level == 2 ? 15 : 20;
            mission.WinSceneName = "";
        }

        MissionHUD hud = Object.FindObjectOfType<MissionHUD>();
        if (hud != null && hud.ObjectiveText != null)
            hud.ObjectiveText.text = objective;

        LevelExit exit = Object.FindObjectOfType<LevelExit>();
        if (exit != null)
        {
            RemoveExitAreaClutter(exit.transform.position);
            EnsureExitCastle(exit.transform);
            RemoveLegacyCastleGate(exit.transform);
        }

        AddHazardMood(level);
    }

    private static void ResolveTheme(int level, out Color background, out Color mapTint, out Color crystalColor, out Color crystalLight, out Color exitColor, out Color enemyColor, out Color stone, out Color roof, out string objective)
    {
        if (level == 2)
        {
            background = new Color(0.055f, 0.095f, 0.15f, 1f);
            mapTint = new Color(0.24f, 0.43f, 0.62f, 1f);
            crystalColor = new Color(0.24f, 0.68f, 1f, 1f);
            crystalLight = new Color(0.16f, 0.62f, 1f, 1f);
            exitColor = new Color(0.22f, 0.62f, 1f, 0.35f);
            enemyColor = new Color(0.08f, 0.32f, 0.86f, 1f);
            stone = new Color(0.22f, 0.34f, 0.48f, 0.98f);
            roof = new Color(0.08f, 0.45f, 0.88f, 0.98f);
            objective = "BLUE RUINS: Collect 5 sapphires, then enter the castle gate.";
            return;
        }

        if (level == 3)
        {
            background = new Color(0.14f, 0.055f, 0.045f, 1f);
            mapTint = new Color(0.54f, 0.2f, 0.16f, 1f);
            crystalColor = new Color(1f, 0.18f, 0.12f, 1f);
            crystalLight = new Color(1f, 0.08f, 0.06f, 1f);
            exitColor = new Color(1f, 0.16f, 0.12f, 0.35f);
            enemyColor = new Color(0.9f, 0.08f, 0.06f, 1f);
            stone = new Color(0.42f, 0.16f, 0.14f, 0.98f);
            roof = new Color(0.86f, 0.06f, 0.04f, 0.98f);
            objective = "CRIMSON KEEP: Collect 5 rubies, then enter the final castle gate.";
            return;
        }

        background = new Color(0.055f, 0.115f, 0.075f, 1f);
        mapTint = new Color(0.24f, 0.48f, 0.25f, 1f);
        crystalColor = new Color(0.2f, 1f, 0.42f, 1f);
        crystalLight = new Color(0.12f, 0.95f, 0.32f, 1f);
        exitColor = new Color(0.16f, 0.95f, 0.34f, 0.35f);
        enemyColor = new Color(0.08f, 0.68f, 0.18f, 1f);
        stone = new Color(0.22f, 0.42f, 0.24f, 0.98f);
        roof = new Color(0.08f, 0.58f, 0.18f, 0.98f);
        objective = "GREEN GROVE: Collect 5 emerald crystals, then enter the castle gate.";
    }

    private static void ConfigureThemedEnemies(Color enemyColor, int level)
    {
        GameObject enemyProjectile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyProjectile.prefab");
        foreach (Actor actor in Object.FindObjectsOfType<Actor>())
        {
            if (actor.GetComponent<Platformer2DController>() != null) continue;

            actor.Health = level == 3 ? 4 : 3;
            actor.DamageSourcesTags = new[] { "PlayerDamage" };
            EditorUtility.SetDirty(actor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);

            foreach (SpriteRenderer renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.color = enemyColor;
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 18);
            }

            Vector3 scale = actor.transform.localScale;
            float maxAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            if (maxAxis > 1.35f)
            {
                float clamp = 1.35f / maxAxis;
                actor.transform.localScale = new Vector3(scale.x * clamp, scale.y * clamp, scale.z);
            }

            Thrower thrower = actor.GetComponent<Thrower>();
            if (thrower == null)
                thrower = actor.gameObject.AddComponent<Thrower>();

            thrower.Projectile = enemyProjectile;
            thrower.ProjectileLocalScale = new Vector3(0.34f, 0.34f, 1f);
            thrower.ThrowingLocalOrigin = new Vector3(0f, 0.85f, 0f);
            thrower.ThrowingForce = Vector3.left;
            thrower.UsePhysics2D = true;
            thrower.AutoThrow = true;
            thrower.AutoThrowDelaySeconds = level == 3 ? 1.35f : 1.85f;
            thrower.AimAtPlayer = true;
            thrower.ProjectileSpeed = level == 3 ? 4.6f : 3.7f;
            thrower.AutoThrowMaxDistance = level == 3 ? 7.4f : 6.2f;
            thrower.AutoThrowRequiresLineOfSight = true;
            thrower.AutoThrowRequiresFacingTarget = false;
            thrower.ProjectileActorDamage = level == 3 ? 2 : 1;
            thrower.OverrideProjectileColor = true;
            thrower.ProjectileColor = enemyColor;
            EditorUtility.SetDirty(thrower);
            PrefabUtility.RecordPrefabInstancePropertyModifications(thrower);
        }
    }

    private static void ConfigureLevelDoorSwitchColors(int level)
    {
        if (level <= 1) return;

        Color doorColor = level == 2 ? new Color(0.05f, 0.7f, 0.9f, 1f) : new Color(0.95f, 0.18f, 0.05f, 1f);
        Color switchColor = level == 2 ? new Color(0.2f, 0.95f, 1f, 1f) : new Color(1f, 0.58f, 0.08f, 1f);

        foreach (Door door in Object.FindObjectsOfType<Door>(true))
        {
            foreach (SpriteRenderer renderer in door.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.color = doorColor;
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 28);
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        foreach (Switch pressureSwitch in Object.FindObjectsOfType<Switch>(true))
        {
            foreach (SpriteRenderer renderer in pressureSwitch.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.color = switchColor;
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 30);
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
    }

    private static void AddDifficultyProgression(int level, Color jewelColor, Color jewelLight, Color enemyColor)
    {
        const string rootName = "DifficultyProgression_Additions";
        GameObject existingRoot = GameObject.Find(rootName);
        if (existingRoot != null)
            Object.DestroyImmediate(existingRoot);

        if (level <= 1) return;

        GameObject root = new GameObject(rootName);

        Vector3[] enemyPositions = level == 2
            ? new[]
            {
                new Vector3(-6.6f, 2.65f, 0f),
                new Vector3(5.4f, 5.0f, 0f),
                new Vector3(13.2f, -0.15f, 0f),
                new Vector3(22.4f, 4.85f, 0f)
            }
            : new[]
            {
                new Vector3(-9.5f, 2.65f, 0f),
                new Vector3(-1.2f, 6.35f, 0f),
                new Vector3(7.4f, 4.95f, 0f),
                new Vector3(21.8f, 7.25f, 0f),
                new Vector3(23.2f, 8.8f, 0f),
                new Vector3(26.0f, 8.9f, 0f)
            };

        Vector3[] spikePositions = level == 2
            ? new[]
            {
                new Vector3(-2.2f, 1.18f, 0f),
                new Vector3(8.8f, 3.48f, 0f),
                new Vector3(15.6f, -1.4f, 0f),
                new Vector3(21.6f, 4.55f, 0f),
                new Vector3(24.4f, 4.55f, 0f)
            }
            : new[]
            {
                new Vector3(-7.1f, 1.18f, 0f),
                new Vector3(0.8f, 4.82f, 0f),
                new Vector3(8.8f, 3.48f, 0f),
                new Vector3(14.9f, -1.4f, 0f),
                new Vector3(20.6f, 4.55f, 0f),
                new Vector3(24.2f, 7.85f, 0f),
                new Vector3(26.4f, 7.85f, 0f),
                new Vector3(28.2f, 7.85f, 0f)
            };

        Vector3[] jewelPositions = level == 2
            ? new[]
            {
                new Vector3(3.2f, 6.2f, 0f),
                new Vector3(18.2f, 1.35f, 0f)
            }
            : new[]
            {
                new Vector3(3.2f, 6.2f, 0f),
                new Vector3(18.2f, 1.35f, 0f),
                new Vector3(25.4f, 9.55f, 0f)
            };

        foreach (Vector3 position in enemyPositions)
            AddProgressionEnemy(root.transform, level, position, enemyColor);

        Color spikeColor = level == 2 ? new Color(0.35f, 0.78f, 1f, 1f) : new Color(1f, 0.26f, 0.1f, 1f);
        foreach (Vector3 position in spikePositions)
            AddProgressionSpike(root.transform, level, position, spikeColor);

        foreach (Vector3 position in jewelPositions)
            AddProgressionJewel(root.transform, level, position, jewelColor, jewelLight);
    }

    private static void AddProgressionEnemy(Transform parent, int level, Vector3 position, Color enemyColor)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
        GameObject enemy = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (enemy == null) return;

        enemy.name = level == 2 ? "Progression_BlueGuard" : "Progression_CrimsonGuard";
        enemy.transform.SetParent(parent);
        enemy.transform.position = position;
        float scale = level == 2 ? 0.95f : 1.05f;
        enemy.transform.localScale = new Vector3(scale, scale, 1f);

        ConfigureThemedEnemy(enemy, level, enemyColor);
    }

    private static void ConfigureThemedEnemy(GameObject enemy, int level, Color enemyColor)
    {
        Actor actor = enemy.GetComponent<Actor>();
        if (actor != null)
        {
            actor.Health = level == 3 ? 4 : 3;
            actor.DamageSourcesTags = new[] { "PlayerDamage" };
            actor.DamageInvincibilitySeconds = level == 3 ? 0.28f : 0.36f;
            EditorUtility.SetDirty(actor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
        }

        foreach (SpriteRenderer renderer in enemy.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.color = enemyColor;
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 22);
            EditorUtility.SetDirty(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        GameObject enemyProjectile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyProjectile.prefab");
        Thrower thrower = enemy.GetComponent<Thrower>();
        if (thrower == null)
            thrower = enemy.AddComponent<Thrower>();

        thrower.Projectile = enemyProjectile;
        thrower.ProjectileLocalScale = level == 2 ? new Vector3(0.34f, 0.34f, 1f) : new Vector3(0.42f, 0.42f, 1f);
        thrower.ThrowingLocalOrigin = new Vector3(0f, 0.82f, 0f);
        thrower.ThrowingForce = Vector3.left;
        thrower.UsePhysics2D = true;
        thrower.AutoThrow = true;
        thrower.AutoThrowDelaySeconds = level == 3 ? 1.35f : 1.85f;
        thrower.AimAtPlayer = true;
        thrower.ProjectileSpeed = level == 3 ? 4.6f : 3.7f;
        thrower.AutoThrowMaxDistance = level == 3 ? 7.4f : 6.2f;
        thrower.AutoThrowRequiresLineOfSight = true;
        thrower.AutoThrowRequiresFacingTarget = false;
        thrower.ProjectileActorDamage = level == 3 ? 2 : 1;
        thrower.OverrideProjectileColor = true;
        thrower.ProjectileColor = enemyColor;
        EditorUtility.SetDirty(thrower);
        PrefabUtility.RecordPrefabInstancePropertyModifications(thrower);
    }

    private static void AddProgressionSpike(Transform parent, int level, Vector3 position, Color spikeColor)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spikes.prefab");
        GameObject spike = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (spike == null) return;

        spike.name = level == 2 ? "Progression_BlueSpikes" : "Progression_CrimsonSpikes";
        spike.transform.SetParent(parent);
        spike.transform.position = position;
        float scale = level == 2 ? 0.85f : 1.05f;
        spike.transform.localScale = new Vector3(scale, scale, 1f);

        foreach (SpriteRenderer renderer in spike.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.color = spikeColor;
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 26);
            EditorUtility.SetDirty(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }

    private static void AddProgressionJewel(Transform parent, int level, Vector3 position, Color jewelColor, Color jewelLight)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Jewel.prefab");
        GameObject jewel = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (jewel == null) return;

        jewel.name = level == 2 ? "Progression_Sapphire" : "Progression_Ruby";
        jewel.transform.SetParent(parent);
        jewel.transform.position = position;
        jewel.transform.localScale = new Vector3(0.58f, 0.58f, 1f);

        foreach (SpriteRenderer renderer in jewel.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.color = jewelColor;
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 34);
            EditorUtility.SetDirty(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        AnimationPicker picker = jewel.GetComponent<AnimationPicker>();
        if (picker != null)
        {
            picker.Randomize = false;
            picker.AnimIntParamValue = level == 2 ? 1 : 2;
            EditorUtility.SetDirty(picker);
            PrefabUtility.RecordPrefabInstancePropertyModifications(picker);
        }

        AddPointLight(jewel.transform, "Progression_Jewel_Light", jewelLight, 1.35f, level == 2 ? 2.4f : 2.8f);
    }

    private static void RemoveLegacyCastleGate(Transform exitTransform)
    {
        Transform existing = exitTransform.Find("LevelCastleGate");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }

    private static void ConfigureExitCastleAsset()
    {
        TextureImporter importer = AssetImporter.GetAtPath(ExitCastleSpritePath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = 180f;
        importer.SaveAndReimport();
    }

    private static void EnsureExitCastle(Transform exitTransform)
    {
        Transform oldMarker = exitTransform.Find("ExitMarker");
        if (oldMarker != null)
            Object.DestroyImmediate(oldMarker.gameObject);

        Transform existingCastle = exitTransform.Find("ExitCastle_WIN");
        if (existingCastle != null)
            Object.DestroyImmediate(existingCastle.gameObject);
    }

    private static void RemoveExitAreaClutter(Vector3 exitPosition)
    {
        System.Collections.Generic.List<GameObject> clutter = new();

        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null) continue;
            if (!go.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(go)) continue;

            bool isClutter = go.name.StartsWith("Spikes") ||
                             go.name.Contains("Enemy Boss") ||
                             go.name.Contains("GreenGlow") ||
                             go.name.Contains("BlueMist") ||
                             go.name.Contains("Flame_Glow");
            if (!isClutter) continue;

            float distance = Vector2.Distance(go.transform.position, exitPosition);
            if (distance <= 13.5f)
                clutter.Add(go);
        }

        foreach (GameObject go in clutter)
            if (go != null)
                Object.DestroyImmediate(go);
    }

    private static TextMeshPro CreateWorldLabel(Transform parent, string name, string text, Vector2 localPosition, float fontSize, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.08f);
        go.transform.localScale = Vector3.one;

        TextMeshPro label = go.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.sortingOrder = sortingOrder;
        label.enableWordWrapping = false;
        return label;
    }

    private static void AddHazardMood(int level)
    {
        GameObject root = GameObject.Find("KayipKristal_LevelMood");
        if (root != null)
            Object.DestroyImmediate(root);
    }

    private static void RemoveLooseProjectiles()
    {
        foreach (Projectile projectile in Object.FindObjectsOfType<Projectile>())
            Object.DestroyImmediate(projectile.gameObject);
    }

    private static void ConfigureCamera(GameObject player)
    {
        Camera camera = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (camera == null) return;

        camera.orthographic = true;
        camera.orthographicSize = 6.2f;

        Camera2DController controller = camera.GetComponent<Camera2DController>();
        if (controller == null)
            controller = camera.gameObject.AddComponent<Camera2DController>();

        controller.TargetTransform = player != null ? player.transform : controller.TargetTransform;
        controller.FollowTarget = true;
        controller.Easing = true;
        controller.EasingSeconds = 0.12f;
        controller.Offset = new Vector2(0f, 1.15f);

        if (player != null)
            camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 1.15f, camera.transform.position.z);
    }

    private static Bounds ResolvePlayableBounds(GameObject fallback)
    {
        TilemapRenderer[] tilemapRenderers = Object.FindObjectsOfType<TilemapRenderer>();
        if (tilemapRenderers.Length == 0)
            return new Bounds(fallback != null ? fallback.transform.position : Vector3.zero, new Vector3(20f, 8f, 1f));

        Bounds bounds = tilemapRenderers[0].bounds;
        foreach (TilemapRenderer renderer in tilemapRenderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void EnsureHud()
    {
        MissionHUD existingHud = Object.FindObjectOfType<MissionHUD>();
        GameObject canvasGo = existingHud != null
            ? existingHud.gameObject
            : new GameObject("KayipKristal_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        MissionHUD hud = existingHud != null ? existingHud : canvasGo.AddComponent<MissionHUD>();
        hud.CrystalText = EnsureText(canvasGo.transform, "CrystalText", "Crystals: 0/5", new Vector2(28f, -28f), new Vector2(360f, 48f), 30f, TextAlignmentOptions.Left, Color.white);
        hud.ScoreText = EnsureText(canvasGo.transform, "ScoreText", "Score: 0", new Vector2(28f, -76f), new Vector2(360f, 48f), 30f, TextAlignmentOptions.Left, Color.white);
        hud.HealthText = EnsureText(canvasGo.transform, "HealthText", "Health: 3", new Vector2(28f, -124f), new Vector2(360f, 48f), 30f, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.72f, 1f));
        hud.ObjectiveText = EnsureText(canvasGo.transform, "ObjectiveText", "GREEN GROVE: Collect 5 emerald crystals, then enter the castle gate.", new Vector2(28f, -172f), new Vector2(960f, 58f), 28f, TextAlignmentOptions.Left, Color.white);
        hud.ExitHintText = EnsureText(canvasGo.transform, "ExitHintText", "", new Vector2(-34f, -34f), new Vector2(360f, 56f), 34f, TextAlignmentOptions.Right, new Color(1f, 0.78f, 0.2f, 1f));
        hud.ExitHintText.rectTransform.anchorMin = new Vector2(1f, 1f);
        hud.ExitHintText.rectTransform.anchorMax = new Vector2(1f, 1f);
        hud.ExitHintText.rectTransform.pivot = new Vector2(1f, 1f);
        hud.ExitHintText.gameObject.SetActive(false);
        hud.ToastText = EnsureText(canvasGo.transform, "ToastText", "", new Vector2(0f, -92f), new Vector2(880f, 56f), 30f, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.52f, 1f));
        hud.ToastText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        hud.ToastText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        hud.ToastText.rectTransform.pivot = new Vector2(0.5f, 1f);
        hud.ToastText.gameObject.SetActive(false);
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, Vector2 anchoredPosition)
    {
        return EnsureText(parent, name, text, anchoredPosition, new Vector2(760f, 48f), 30f, TextAlignmentOptions.Left, Color.white);
    }

    private static TMP_Text EnsureText(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (label == null)
            label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        return label;
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/TitleScreen.unity", true),
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(Level2Path, true),
            new EditorBuildSettingsScene(Level3Path, true)
        };
    }
}
