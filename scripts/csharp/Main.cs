using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// Main - Primary game scene controller.
/// Sets up the game world, camera, and coordinates between systems.
/// </summary>
public partial class Main : Node2D
{
    [Export]
    public ParallaxBackground Background { get; set; }

    [Export]
    public Node2D GameWorld { get; set; }

    [Export]
    public Node2D DebrisLayer { get; set; }

    [Export]
    public Node2D StationLayer { get; set; }

    [Export]
    public Node2D FoundationTiles { get; set; }

    [Export]
    public Node2D BuildingsLayer { get; set; }

    [Export]
    public Camera2D Camera { get; set; }

    [Export]
    public HUD Hud { get; set; }

    [Export]
    public InventoryUI InventoryUi { get; set; }

    [Export]
    public BuildMenuUI BuildMenuUi { get; set; }

    [Export]
    public BuildingUI BuildingUi { get; set; }

    // Camera movement
    private Vector2 _cameraTargetPosition = Vector2.Zero;
    private float _cameraZoomTarget = 1.0f;

    // Foundation tile sprites cache
    private readonly Dictionary<Vector2I, Sprite2D> _foundationSprites = new();

    // Last known mouse grid position (for ghost updates)
    private Vector2I _lastMouseGridPos = Vector2I.Zero;

    public override void _Ready()
    {
        GD.Print("[Main] _Ready called");

        // Fetch node references (C# exports with NodePath don't auto-resolve)
        Background ??= GetNodeOrNull<ParallaxBackground>("Background");
        GameWorld ??= GetNodeOrNull<Node2D>("GameWorld");
        DebrisLayer ??= GetNodeOrNull<Node2D>("GameWorld/DebrisLayer");
        StationLayer ??= GetNodeOrNull<Node2D>("GameWorld/StationLayer");
        FoundationTiles ??= GetNodeOrNull<Node2D>("GameWorld/StationLayer/FoundationTiles");
        BuildingsLayer ??= GetNodeOrNull<Node2D>("GameWorld/StationLayer/Buildings");
        Camera ??= GetNodeOrNull<Camera2D>("Camera2D");
        Hud ??= GetNodeOrNull<HUD>("HUD");
        InventoryUi ??= GetNodeOrNull<InventoryUI>("InventoryUI");
        BuildMenuUi ??= GetNodeOrNull<BuildMenuUI>("BuildMenuUI");
        BuildingUi ??= GetNodeOrNull<BuildingUI>("BuildingUI");

        GD.Print($"[Main] Camera: {Camera != null}, Background: {Background != null}, GameWorld: {GameWorld != null}");
        GD.Print($"[Main] Hud: {Hud != null}, InventoryUi: {InventoryUi != null}, BuildMenuUi: {BuildMenuUi != null}");
        GD.Print($"[Main] GameManager.Instance: {GameManager.Instance != null}");
        GD.Print($"[Main] GridManager.Instance: {GridManager.Instance != null}");
        GD.Print($"[Main] SpriteGenerator.Instance: {SpriteGenerator.Instance != null}");

        SetupCamera();
        GD.Print("[Main] SetupCamera complete");
        SetupBackground();
        GD.Print("[Main] SetupBackground complete");
        SetupDebrisSystem();
        GD.Print("[Main] SetupDebrisSystem complete");
        SetupStation();
        GD.Print("[Main] SetupStation complete");
        SetupBuildingSystem();
        GD.Print("[Main] SetupBuildingSystem complete");
        StartGame();
        GD.Print("[Main] StartGame complete - Game ready!");
    }

    private void SetupCamera()
    {
        if (Camera == null)
            return;

        Camera.Position = Vector2.Zero;
        Camera.Zoom = Vector2.One;
        _cameraZoomTarget = 1.0f;
    }

    private void SetupBackground()
    {
        if (Background == null)
            return;

        // Create starfield background
        var starsLayer = new ParallaxLayer { MotionScale = new Vector2(0.1f, 0.1f) };
        Background.AddChild(starsLayer);

        var starsSprite = CreateStarfield();
        starsLayer.AddChild(starsSprite);
    }

    private Sprite2D CreateStarfield()
    {
        // Create a procedural starfield texture
        const int size = 512;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(new Color(0.02f, 0.02f, 0.05f));

        var rng = new RandomNumberGenerator { Seed = 12345 };

        // Add stars
        for (int i = 0; i < 200; i++)
        {
            int x = rng.RandiRange(0, size - 1);
            int y = rng.RandiRange(0, size - 1);
            float brightness = rng.RandfRange(0.3f, 1.0f);
            var starColor = new Color(brightness, brightness, brightness * 1.1f, 1.0f);
            img.SetPixel(x, y, starColor);

            // Some stars are slightly larger
            if (rng.Randf() < 0.1f)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int sx = Mathf.Clamp(x + dx, 0, size - 1);
                        int sy = Mathf.Clamp(y + dy, 0, size - 1);
                        float dim = brightness * 0.3f;
                        var current = img.GetPixel(sx, sy);
                        img.SetPixel(sx, sy, new Color(
                            Mathf.Max(current.R, dim),
                            Mathf.Max(current.G, dim),
                            Mathf.Max(current.B, dim * 1.1f),
                            1.0f
                        ));
                    }
                }
            }
        }

        var texture = ImageTexture.CreateFromImage(img);

        return new Sprite2D
        {
            Texture = texture,
            Centered = false,
            RegionEnabled = true,
            RegionRect = new Rect2(0, 0, 2048, 2048), // Tile the texture
            Position = new Vector2(-1024, -1024)
        };
    }

    private void SetupDebrisSystem()
    {
        // Tell debris manager where to spawn debris
        DebrisManager.Instance?.SetDebrisContainer(DebrisLayer);
    }

    private void SetupStation()
    {
        // Create visual foundation tiles for starting station
        UpdateFoundationVisuals();

        // Connect to grid manager for updates
        if (GridManager.Instance != null)
        {
            GridManager.Instance.FoundationAdded += OnFoundationAdded;
            GridManager.Instance.FoundationRemoved += OnFoundationRemoved;
        }
    }

    private void SetupBuildingSystem()
    {
        // Give BuildingManager reference to buildings layer
        if (BuildingManager.Instance != null)
            BuildingManager.Instance.BuildingsLayer = BuildingsLayer;
    }

    private void UpdateFoundationVisuals()
    {
        // Generate foundation texture
        var foundationTexture = SpriteGenerator.Instance?.GenerateFoundation();

        // Create sprites for each foundation tile
        var allFoundation = GridManager.Instance?.GetAllFoundation();
        if (allFoundation == null)
            return;

        foreach (var pos in allFoundation)
        {
            AddFoundationSprite(pos, foundationTexture);
        }
    }

    private void AddFoundationSprite(Vector2I pos, Texture2D texture = null)
    {
        if (_foundationSprites.ContainsKey(pos))
            return;

        texture ??= SpriteGenerator.Instance?.GenerateFoundation();

        var sprite = new Sprite2D
        {
            Texture = texture,
            Centered = false,
            Position = GridManager.Instance?.GridToWorld(pos) ?? Vector2.Zero,
            ZIndex = Constants.ZFoundation
        };

        FoundationTiles?.AddChild(sprite);
        _foundationSprites[pos] = sprite;
    }

    private void RemoveFoundationSprite(Vector2I pos)
    {
        if (_foundationSprites.TryGetValue(pos, out var sprite))
        {
            sprite.QueueFree();
            _foundationSprites.Remove(pos);
        }
    }

    private void OnFoundationAdded(Vector2I pos)
    {
        AddFoundationSprite(pos);
    }

    private void OnFoundationRemoved(Vector2I pos)
    {
        RemoveFoundationSprite(pos);
    }

    private void StartGame()
    {
        // Initialize game state
        GameManager.Instance?.StartNewGame();

        // Give player some starting items for testing
        GiveStartingItems();
    }

    private void GiveStartingItems()
    {
        // Give starter resources for testing Phase 2 buildings
        var items = new[]
        {
            ("iron_ore", 50),
            ("copper_ore", 30),
            ("coal", 20),
            ("stone", 30),
            ("iron_plate", 20),
            ("iron_gear", 10),
            ("electronic_circuit", 10)
        };

        foreach (var (itemId, count) in items)
        {
            var item = InventoryManager.Instance?.GetItem(itemId);
            if (item != null)
            {
                InventoryManager.Instance.AddItem(item, count);
            }
        }
    }

    public override void _Process(double delta)
    {
        HandleCameraInput((float)delta);
        UpdateCamera((float)delta);
        UpdateBuildGhost();
    }

    private void HandleCameraInput(float delta)
    {
        // Pan camera with WASD/arrows
        var panDirection = Vector2.Zero;

        if (Input.IsActionPressed("move_up"))
            panDirection.Y -= 1;
        if (Input.IsActionPressed("move_down"))
            panDirection.Y += 1;
        if (Input.IsActionPressed("move_left"))
            panDirection.X -= 1;
        if (Input.IsActionPressed("move_right"))
            panDirection.X += 1;

        if (panDirection != Vector2.Zero)
        {
            panDirection = panDirection.Normalized();
            _cameraTargetPosition += panDirection * Constants.CameraPanSpeed * delta / Camera.Zoom.X;
        }
    }

    private void UpdateBuildGhost()
    {
        // Update ghost preview position when in build mode
        if (BuildingManager.Instance != null && BuildingManager.Instance.IsInBuildMode())
        {
            var gridPos = GetMouseGridPosition();
            if (gridPos != _lastMouseGridPos)
            {
                _lastMouseGridPos = gridPos;
                BuildingManager.Instance.UpdateGhostPosition(gridPos);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Zoom with mouse wheel
        if (@event.IsActionPressed("zoom_in"))
        {
            _cameraZoomTarget = Mathf.Min(_cameraZoomTarget + Constants.CameraZoomStep, Constants.CameraZoomMax);
        }
        else if (@event.IsActionPressed("zoom_out"))
        {
            _cameraZoomTarget = Mathf.Max(_cameraZoomTarget - Constants.CameraZoomStep, Constants.CameraZoomMin);
        }

        // Rotation for building placement
        if (@event.IsActionPressed("rotate"))
        {
            if (BuildingManager.Instance != null && BuildingManager.Instance.IsInBuildMode())
            {
                BuildingManager.Instance.RotatePlacementCw();
                GetViewport().SetInputAsHandled();
            }
        }

        // Cancel build mode or close menus
        if (@event.IsActionPressed("cancel"))
        {
            if (BuildingManager.Instance != null && BuildingManager.Instance.IsInBuildMode())
            {
                BuildingManager.Instance.ExitBuildMode();
                GetViewport().SetInputAsHandled();
            }
        }

        // Handle clicking on game world (only if no UI is blocking)
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (IsUiBlocking())
                return;

            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                HandleLeftClick();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
            {
                HandleRightClick();
            }
        }

        // Handle mouse motion for ghost preview
        if (@event is InputEventMouseMotion)
        {
            if (BuildingManager.Instance != null && BuildingManager.Instance.IsInBuildMode())
            {
                var gridPos = GetMouseGridPosition();
                BuildingManager.Instance.UpdateGhostPosition(gridPos);
            }
        }
    }

    private void UpdateCamera(float delta)
    {
        if (Camera == null)
            return;

        // Smooth camera movement
        Camera.Position = Camera.Position.Lerp(_cameraTargetPosition, 5.0f * delta);

        // Smooth zoom
        float currentZoom = Camera.Zoom.X;
        float newZoom = Mathf.Lerp(currentZoom, _cameraZoomTarget, 5.0f * delta);
        Camera.Zoom = new Vector2(newZoom, newZoom);
    }

    private void HandleLeftClick()
    {
        var gridPos = GetMouseGridPosition();

        // If in build mode, try to place building
        if (BuildingManager.Instance != null && BuildingManager.Instance.IsInBuildMode())
        {
            BuildingManager.Instance.TryPlaceBuilding(gridPos);
            return;
        }

        // Otherwise, check for interactions
        if (GridManager.Instance != null && GridManager.Instance.HasBuilding(gridPos))
        {
            HandleBuildingClick(gridPos);
        }
    }

    private void HandleRightClick()
    {
        var gridPos = GetMouseGridPosition();

        // If in build mode, exit it
        if (BuildingManager.Instance != null && BuildingManager.Instance.IsInBuildMode())
        {
            BuildingManager.Instance.ExitBuildMode();
            return;
        }

        // Otherwise, try to remove building
        if (GridManager.Instance != null && GridManager.Instance.HasBuilding(gridPos))
        {
            BuildingManager.Instance?.RemoveBuilding(gridPos);
        }
    }

    private void HandleBuildingClick(Vector2I gridPos)
    {
        var building = GridManager.Instance?.GetBuilding(gridPos);
        if (building == null)
            return;

        // Open building UI for buildings with inventory
        if (building is SmallChest or StoneFurnace)
        {
            BuildingUi?.OpenForBuilding(building);
        }
    }

    public Vector2 GetMouseWorldPosition()
    {
        return Camera?.GetGlobalMousePosition() ?? Vector2.Zero;
    }

    public Vector2I GetMouseGridPosition()
    {
        return GridManager.Instance?.WorldToGrid(GetMouseWorldPosition()) ?? Vector2I.Zero;
    }

    private bool IsUiBlocking()
    {
        // Check if any UI panel is open that should block world clicks
        if (InventoryUi != null && InventoryUi.Visible && InventoryUi.IsOpen)
            return true;
        if (BuildingUi != null && BuildingUi.Visible && BuildingUi.IsOpen)
            return true;
        if (BuildMenuUi != null && BuildMenuUi.Visible)
            return true;
        return false;
    }
}
