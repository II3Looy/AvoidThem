using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class AvoidThemGame : MonoBehaviour
{
    private enum GameState
    {
        StartScreen,
        Playing,
        GameOver
    }

    [Header("Arena")]
    [SerializeField] private float arenaHalfSize = 8f;
    [SerializeField] private float wallThickness = 0.6f;
    [SerializeField] private float cursorRadius = 0.45f;
    [SerializeField] private float hazardRadius = 0.35f;

    [Header("Spawning")]
    [SerializeField] private float initialSpawnInterval = 1.2f;
    [SerializeField] private float minSpawnInterval = 0.24f;
    [SerializeField] private float spawnIntervalRampPerSecond = 0.03f;
    [SerializeField] private float baseHazardSpeed = 5.4f;
    [SerializeField] private float hazardSpeedRampPerSecond = 0.18f;
    [SerializeField] private int maxHazards = 120;

    [Header("UI")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("Visuals")]
    [SerializeField] private string backgroundTextureName = "lava";
    [SerializeField] private float backgroundTextureTiling = 2f;
    [SerializeField] private Color floorFallbackColor = new Color(0.12f, 0.16f, 0.2f);
    [SerializeField] private string enemyTextureFolderName = "Enemies";
    [SerializeField] private Color hazardFallbackColor = new Color(1f, 0.31f, 0.2f);

    private readonly List<Rigidbody> hazards = new List<Rigidbody>();
    private readonly List<Texture2D> enemyTextures = new List<Texture2D>();
    private Camera gameCamera;
    private Transform cursorTransform;
    private PhysicsMaterial bounceMaterial;
    private Text scoreText;
    private GameObject startPanel;
    private GameObject gameOverPanel;
    private Text gameOverText;
    private GameState state;
    private float elapsed;
    private float bestScore;
    private float spawnTimer;
    private Plane cursorPlane;
    private Font uiFont;
    private Vector2 lastPointerPosition;

    private void Awake()
    {
        cursorPlane = new Plane(Vector3.up, Vector3.zero);
        uiFont = ResolveFont();
        lastPointerPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        LoadEnemyTextures();
        ConfigureCamera();
        BuildArena();
        BuildCursor();
        BuildUi();
        EnterStartScreen();
        ConfigureCursor(true);
    }

    private void OnDestroy()
    {
        ConfigureCursor(false);
    }

    private void Update()
    {
        UpdateCursorPosition();
        HandleMenuInput();

        if (state != GameState.Playing)
        {
            return;
        }

        elapsed += Time.deltaTime;
        UpdateScoreText(elapsed);
        RunSpawner(Time.deltaTime);
        PurgeDestroyedHazards();
        CheckLoseCondition();
    }

    private void ConfigureCamera()
    {
        gameCamera = Camera.main;

        if (gameCamera == null)
        {
            gameCamera = Object.FindFirstObjectByType<Camera>();
        }

        if (gameCamera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        gameCamera.transform.position = new Vector3(0f, 18f, 0f);
        gameCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = arenaHalfSize + 1.1f;
        gameCamera.nearClipPlane = 0.1f;
        gameCamera.farClipPlane = 100f;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.06f, 0.09f, 0.12f);
    }

    private void BuildArena()
    {
        bounceMaterial = new PhysicsMaterial("ArenaBounce")
        {
            bounciness = 1f,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "ArenaFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(arenaHalfSize / 5f, 1f, arenaHalfSize / 5f);
        var floorRenderer = floor.GetComponent<Renderer>();
        ApplyFloorVisuals(floorRenderer);
        var floorCollider = floor.GetComponent<Collider>();
        if (floorCollider != null)
        {
            Destroy(floorCollider);
        }

        CreateBoundaryWall("WallLeft", new Vector3(-(arenaHalfSize + wallThickness * 0.5f), 1f, 0f), new Vector3(wallThickness, 2f, arenaHalfSize * 2f + wallThickness * 2f));
        CreateBoundaryWall("WallRight", new Vector3(arenaHalfSize + wallThickness * 0.5f, 1f, 0f), new Vector3(wallThickness, 2f, arenaHalfSize * 2f + wallThickness * 2f));
        CreateBoundaryWall("WallTop", new Vector3(0f, 1f, arenaHalfSize + wallThickness * 0.5f), new Vector3(arenaHalfSize * 2f + wallThickness * 2f, 2f, wallThickness));
        CreateBoundaryWall("WallBottom", new Vector3(0f, 1f, -(arenaHalfSize + wallThickness * 0.5f)), new Vector3(arenaHalfSize * 2f + wallThickness * 2f, 2f, wallThickness));

        var directionalLight = Object.FindFirstObjectByType<Light>();
        if (directionalLight != null)
        {
            directionalLight.intensity = 1.25f;
            directionalLight.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
        }
    }

    private void ApplyFloorVisuals(Renderer floorRenderer)
    {
        var floorMaterial = floorRenderer.material;
        if (TryGetBackgroundTexture(out var backgroundTexture))
        {
            floorMaterial.color = Color.white;
            backgroundTexture.wrapMode = TextureWrapMode.Repeat;
            floorMaterial.mainTexture = backgroundTexture;
            floorMaterial.mainTextureScale = new Vector2(backgroundTextureTiling, backgroundTextureTiling);
            return;
        }

        floorMaterial.color = floorFallbackColor;
    }

    private bool TryGetBackgroundTexture(out Texture2D texture)
    {
        texture = null;

        if (string.IsNullOrWhiteSpace(backgroundTextureName))
        {
            return false;
        }

        texture = Resources.Load<Texture2D>($"Art/Backgrounds/{backgroundTextureName}");
        if (texture != null)
        {
            return true;
        }

#if UNITY_EDITOR
        var basePath = $"Assets/Art/Backgrounds/{backgroundTextureName}";
        var possibleExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".tga", ".tif", ".tiff", ".psd", ".exr", ".hdr" };

        for (var i = 0; i < possibleExtensions.Length; i++)
        {
            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"{basePath}{possibleExtensions[i]}");
            if (texture != null)
            {
                return true;
            }
        }
#endif

        return false;
    }

    private void LoadEnemyTextures()
    {
        enemyTextures.Clear();
        LoadEnemyTexturesFromFolder(enemyTextureFolderName);

        if (enemyTextureFolderName != "Enemy")
        {
            LoadEnemyTexturesFromFolder("Enemy");
        }

        if (enemyTextureFolderName != "Enemies")
        {
            LoadEnemyTexturesFromFolder("Enemies");
        }
    }

    private void LoadEnemyTexturesFromFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return;
        }

        AddUniqueTextures(Resources.LoadAll<Texture2D>($"Art/{folderName}"));

#if UNITY_EDITOR
        var textureGuids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { $"Assets/Art/{folderName}" });
        for (var i = 0; i < textureGuids.Length; i++)
        {
            var texturePath = UnityEditor.AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                AddUniqueTexture(texture);
            }
        }
#endif
    }

    private void AddUniqueTextures(Texture2D[] textures)
    {
        for (var i = 0; i < textures.Length; i++)
        {
            AddUniqueTexture(textures[i]);
        }
    }

    private void AddUniqueTexture(Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        for (var i = 0; i < enemyTextures.Count; i++)
        {
            if (enemyTextures[i] == texture)
            {
                return;
            }
        }

        enemyTextures.Add(texture);
    }

    private void CreateBoundaryWall(string wallName, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.position = position;
        wall.transform.localScale = scale;

        var wallRenderer = wall.GetComponent<Renderer>();
        wallRenderer.enabled = false;

        var collider = wall.GetComponent<BoxCollider>();
        collider.sharedMaterial = bounceMaterial;
    }

    private void BuildCursor()
    {
        var cursorObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cursorObject.name = "CursorCircle";
        cursorTransform = cursorObject.transform;
        cursorTransform.position = new Vector3(0f, 0.06f, 0f);
        cursorTransform.localScale = new Vector3(cursorRadius * 2f, 0.03f, cursorRadius * 2f);

        var renderer = cursorObject.GetComponent<Renderer>();
        renderer.material.color = new Color(0.2f, 0.95f, 1f);

        var collider = cursorObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        scoreText = CreateText(
            canvas.transform,
            "Score: 0.00s",
            44,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(500f, 100f)
        );

        startPanel = CreateOverlayPanel(canvas.transform, "StartPanel", new Color(0f, 0f, 0f, 0.55f));
        CreateText(
            startPanel.transform,
            "Avoid Them",
            88,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.62f),
            new Vector2(0.5f, 0.62f),
            Vector2.zero,
            new Vector2(1400f, 180f)
        );
        CreateText(
            startPanel.transform,
            "Move your mouse to dodge incoming hazards.\nPress SPACE or Left Click to start.",
            42,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.46f),
            new Vector2(0.5f, 0.46f),
            Vector2.zero,
            new Vector2(1400f, 240f)
        );

        gameOverPanel = CreateOverlayPanel(canvas.transform, "GameOverPanel", new Color(0f, 0f, 0f, 0.68f));
        gameOverText = CreateText(
            gameOverPanel.transform,
            string.Empty,
            54,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1400f, 600f)
        );
    }

    private GameObject CreateOverlayPanel(Transform parent, string panelName, Color color)
    {
        var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        var rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panelObject.GetComponent<Image>();
        image.color = color;

        return panelObject;
    }

    private Text CreateText(
        Transform parent,
        string value,
        int size,
        TextAnchor anchor,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 textSize)
    {
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = textSize;

        var text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private Font ResolveFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private void ConfigureCursor(bool gameplayCursor)
    {
        Cursor.visible = !gameplayCursor;
        Cursor.lockState = gameplayCursor ? CursorLockMode.Confined : CursorLockMode.None;
    }

    private void UpdateCursorPosition()
    {
        if (gameCamera == null || cursorTransform == null)
        {
            return;
        }

        if (!TryGetPointerScreenPosition(out var pointerPosition))
        {
            return;
        }

        var ray = gameCamera.ScreenPointToRay(pointerPosition);
        if (!cursorPlane.Raycast(ray, out var rayDistance))
        {
            return;
        }

        var hitPoint = ray.GetPoint(rayDistance);
        var clampedX = Mathf.Clamp(hitPoint.x, -arenaHalfSize + cursorRadius, arenaHalfSize - cursorRadius);
        var clampedZ = Mathf.Clamp(hitPoint.z, -arenaHalfSize + cursorRadius, arenaHalfSize - cursorRadius);
        cursorTransform.position = new Vector3(clampedX, 0.06f, clampedZ);
    }

    private void HandleMenuInput()
    {
        if (state == GameState.StartScreen && IsStartPressedThisFrame())
        {
            StartRound();
            return;
        }

        if (state == GameState.GameOver && IsRestartPressedThisFrame())
        {
            StartRound();
        }
    }

    private bool TryGetPointerScreenPosition(out Vector2 pointerPosition)
    {
        var pointer = Pointer.current;
        if (pointer != null)
        {
            pointerPosition = pointer.position.ReadValue();
            lastPointerPosition = pointerPosition;
            return true;
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            pointerPosition = mouse.position.ReadValue();
            lastPointerPosition = pointerPosition;
            return true;
        }

        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            pointerPosition = touchscreen.primaryTouch.position.ReadValue();
            lastPointerPosition = pointerPosition;
            return true;
        }

        pointerPosition = lastPointerPosition;
        return true;
    }

    private bool IsStartPressedThisFrame()
    {
        return IsKeyPressedThisFrame(Key.Space) || WasPrimaryPointerPressedThisFrame();
    }

    private bool IsRestartPressedThisFrame()
    {
        return IsKeyPressedThisFrame(Key.R) || IsKeyPressedThisFrame(Key.Space) || WasPrimaryPointerPressedThisFrame();
    }

    private bool IsKeyPressedThisFrame(Key key)
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].wasPressedThisFrame;
    }

    private bool WasPrimaryPointerPressedThisFrame()
    {
        var pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame)
        {
            return true;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        var touchscreen = Touchscreen.current;
        return touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame;
    }

    private void EnterStartScreen()
    {
        state = GameState.StartScreen;
        elapsed = 0f;
        spawnTimer = initialSpawnInterval;
        UpdateScoreText(elapsed);
        startPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        scoreText.gameObject.SetActive(true);
        ClearHazards();
    }

    private void StartRound()
    {
        ClearHazards();
        state = GameState.Playing;
        elapsed = 0f;
        spawnTimer = 0.4f;
        UpdateScoreText(elapsed);
        startPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    private void EndRound()
    {
        state = GameState.GameOver;
        bestScore = Mathf.Max(bestScore, elapsed);
        gameOverText.text =
            $"Game Over\n\nScore: {elapsed:0.00}s\nBest: {bestScore:0.00}s\n\nPress R, SPACE, or Left Click to restart";
        gameOverPanel.SetActive(true);
    }

    private void RunSpawner(float deltaTime)
    {
        spawnTimer -= deltaTime;

        if (spawnTimer > 0f || hazards.Count >= maxHazards)
        {
            return;
        }

        SpawnHazard();
        var currentInterval = Mathf.Max(minSpawnInterval, initialSpawnInterval - elapsed * spawnIntervalRampPerSecond);
        spawnTimer = currentInterval;
    }

    private void SpawnHazard()
    {
        var side = Random.Range(0, 4);
        var spread = Random.Range(-arenaHalfSize + 0.8f, arenaHalfSize - 0.8f);
        var spawnOffset = arenaHalfSize - hazardRadius - 0.05f;

        Vector3 position;
        Vector3 direction;

        switch (side)
        {
            case 0:
                position = new Vector3(-spawnOffset, hazardRadius, spread);
                direction = new Vector3(1f, 0f, Random.Range(-0.65f, 0.65f));
                break;
            case 1:
                position = new Vector3(spawnOffset, hazardRadius, spread);
                direction = new Vector3(-1f, 0f, Random.Range(-0.65f, 0.65f));
                break;
            case 2:
                position = new Vector3(spread, hazardRadius, spawnOffset);
                direction = new Vector3(Random.Range(-0.65f, 0.65f), 0f, -1f);
                break;
            default:
                position = new Vector3(spread, hazardRadius, -spawnOffset);
                direction = new Vector3(Random.Range(-0.65f, 0.65f), 0f, 1f);
                break;
        }

        var hazardObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hazardObject.name = "Hazard";
        hazardObject.transform.position = position;
        hazardObject.transform.localScale = Vector3.one * (hazardRadius * 2f);
        ApplyHazardVisuals(hazardObject.GetComponent<Renderer>());

        var collider = hazardObject.GetComponent<SphereCollider>();
        collider.sharedMaterial = bounceMaterial;

        var rb = hazardObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var speed = baseHazardSpeed + elapsed * hazardSpeedRampPerSecond + Random.Range(-0.5f, 1.25f);
        rb.linearVelocity = direction.normalized * Mathf.Max(2.5f, speed);

        hazards.Add(rb);
    }

    private void ApplyHazardVisuals(Renderer hazardRenderer)
    {
        var hazardMaterial = hazardRenderer.material;
        if (enemyTextures.Count == 0)
        {
            hazardMaterial.color = hazardFallbackColor;
            return;
        }

        var texture = enemyTextures[Random.Range(0, enemyTextures.Count)];
        hazardMaterial.color = Color.white;
        hazardMaterial.mainTexture = texture;
    }

    private void PurgeDestroyedHazards()
    {
        for (var i = hazards.Count - 1; i >= 0; i--)
        {
            if (hazards[i] == null)
            {
                hazards.RemoveAt(i);
            }
        }
    }

    private void CheckLoseCondition()
    {
        var cursorPosition = cursorTransform.position;
        var hitDistance = cursorRadius + hazardRadius;
        var hitDistanceSquared = hitDistance * hitDistance;

        for (var i = hazards.Count - 1; i >= 0; i--)
        {
            var hazard = hazards[i];
            if (hazard == null)
            {
                hazards.RemoveAt(i);
                continue;
            }

            var delta = hazard.position - cursorPosition;
            delta.y = 0f;

            if (delta.sqrMagnitude <= hitDistanceSquared)
            {
                EndRound();
                return;
            }
        }
    }

    private void UpdateScoreText(float score)
    {
        scoreText.text = $"Score: {score:0.00}s";
    }

    private void ClearHazards()
    {
        for (var i = 0; i < hazards.Count; i++)
        {
            var hazard = hazards[i];
            if (hazard != null)
            {
                Destroy(hazard.gameObject);
            }
        }

        hazards.Clear();
    }
}

public static class AvoidThemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureGame()
    {
        if (Object.FindFirstObjectByType<AvoidThemGame>() != null)
        {
            return;
        }

        var gameObject = new GameObject("AvoidThemGame");
        gameObject.AddComponent<AvoidThemGame>();
    }
}
