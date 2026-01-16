using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// DebrisManager - Handles spawning and managing drifting debris (Autoload singleton).
/// </summary>
public partial class DebrisManager : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static DebrisManager Instance { get; private set; }

    // Signals
    [Signal]
    public delegate void DebrisSpawnedEventHandler(Node2D debris);

    [Signal]
    public delegate void DebrisCollectedEventHandler(Node2D debris, Array items);

    [Signal]
    public delegate void DespawnedEventHandler(Node2D debris);

    /// <summary>
    /// Active debris entities
    /// </summary>
    private readonly Array<Node2D> _activeDebris = new();

    /// <summary>
    /// Debris spawn timer
    /// </summary>
    private float _spawnTimer = 0.0f;

    /// <summary>
    /// Current spawn rate (seconds between spawns)
    /// </summary>
    public float SpawnRate { get; private set; } = Constants.DebrisBaseSpawnRate;

    /// <summary>
    /// Reference to the debris container node
    /// </summary>
    public Node2D DebrisContainer { get; private set; }

    /// <summary>
    /// Screen/viewport bounds for spawning
    /// </summary>
    private Rect2 _spawnBounds;

    /// <summary>
    /// Debris type weights for spawning
    /// </summary>
    private readonly Dictionary<string, int> _debrisWeights = new()
    {
        { "iron_asteroid", 30 },
        { "copper_asteroid", 25 },
        { "stone_asteroid", 20 },
        { "coal_asteroid", 15 },
        { "scrap_metal", 20 },
        { "ice_chunk", 10 }
    };

    private RandomNumberGenerator _rng = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameTick += OnGameTick;
        }

        GetViewport().SizeChanged += UpdateSpawnBounds;
        UpdateSpawnBounds();
    }

    private void UpdateSpawnBounds()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        _spawnBounds = new Rect2(
            -Constants.DebrisSpawnMargin,
            -Constants.DebrisSpawnMargin,
            viewportSize.X + Constants.DebrisSpawnMargin * 2,
            viewportSize.Y + Constants.DebrisSpawnMargin * 2
        );
    }

    private void OnGameTick(int tick)
    {
        // Debris spawning is handled in _Process for smoother timing
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying())
            return;

        // Update spawn timer
        _spawnTimer -= (float)delta * GameManager.Instance.GameSpeed;
        if (_spawnTimer <= 0)
        {
            SpawnDebris();
            // Randomize next spawn time
            float variance = _rng.RandfRange(-Constants.DebrisSpawnVariance, Constants.DebrisSpawnVariance);
            _spawnTimer = SpawnRate + variance;
        }

        // Update and check debris
        UpdateDebris((float)delta);
    }

    private void UpdateDebris(float delta)
    {
        var toRemove = new Array<Node2D>();

        foreach (var debris in _activeDebris)
        {
            if (!IsInstanceValid(debris))
            {
                toRemove.Add(debris);
                continue;
            }

            // Move debris
            if (debris.HasMethod("UpdateMovement"))
            {
                debris.Call("UpdateMovement", delta * GameManager.Instance.GameSpeed);
            }

            // Check if debris is off-screen (despawn)
            var despawnRect = new Rect2(
                -Constants.DespawnMargin,
                -Constants.DespawnMargin,
                _spawnBounds.Size.X + Constants.DespawnMargin,
                _spawnBounds.Size.Y + Constants.DespawnMargin
            );

            // Get camera-adjusted position
            var screenPos = debris.GlobalPosition;
            var camera = GetViewport().GetCamera2D();
            if (camera != null)
            {
                screenPos = debris.GlobalPosition - camera.GlobalPosition + _spawnBounds.Size / 2;
            }

            if (!despawnRect.HasPoint(screenPos))
            {
                toRemove.Add(debris);
            }
        }

        // Remove despawned debris
        foreach (var debris in toRemove)
        {
            DespawnDebris(debris);
        }
    }

    /// <summary>
    /// Spawn a new piece of debris
    /// </summary>
    public Node2D SpawnDebris()
    {
        if (DebrisContainer == null)
            return null;

        // Choose debris type based on weights
        string debrisType = ChooseDebrisType();

        // Create debris entity
        var debris = CreateDebrisEntity(debrisType);
        if (debris == null)
            return null;

        // Position at screen edge
        var spawnPos = GetSpawnPosition();
        debris.GlobalPosition = spawnPos;

        // Set velocity toward/across the screen
        var velocity = GetDriftVelocity(spawnPos);
        if (debris.HasMethod("SetDriftVelocity"))
        {
            debris.Call("SetDriftVelocity", velocity);
        }

        // Add to container and track
        DebrisContainer.AddChild(debris);
        _activeDebris.Add(debris);

        EmitSignal(SignalName.DebrisSpawned, debris);
        return debris;
    }

    private string ChooseDebrisType()
    {
        int totalWeight = 0;
        foreach (int weight in _debrisWeights.Values)
        {
            totalWeight += weight;
        }

        int roll = _rng.RandiRange(0, totalWeight - 1);
        int accumulated = 0;

        foreach (var kvp in _debrisWeights)
        {
            accumulated += kvp.Value;
            if (roll < accumulated)
                return kvp.Key;
        }

        return "iron_asteroid"; // Fallback
    }

    private Node2D CreateDebrisEntity(string debrisType)
    {
        var debris = new DebrisEntity();
        debris.Name = $"Debris_{debrisType}";

        // Initialize with type and contents
        var contents = GenerateContents(debrisType);
        debris.Initialize(debrisType, contents, (int)_rng.Randi());

        // Connect input for clicking
        debris.InputEvent += (viewport, @event, shapeIdx) => OnDebrisInput(viewport, @event, (int)shapeIdx, debris);

        return debris;
    }

    private Array GenerateContents(string debrisType)
    {
        var contents = new Array();
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        var entry = new Dictionary();

        switch (debrisType)
        {
            case "iron_asteroid":
                entry["item_id"] = "iron_ore";
                entry["count"] = rng.RandiRange(1, 3);
                break;
            case "copper_asteroid":
                entry["item_id"] = "copper_ore";
                entry["count"] = rng.RandiRange(1, 3);
                break;
            case "stone_asteroid":
                entry["item_id"] = "stone";
                entry["count"] = rng.RandiRange(1, 2);
                break;
            case "coal_asteroid":
                entry["item_id"] = "coal";
                entry["count"] = rng.RandiRange(1, 3);
                break;
            case "scrap_metal":
                entry["item_id"] = "scrap_metal";
                entry["count"] = rng.RandiRange(1, 2);
                break;
            case "ice_chunk":
                entry["item_id"] = "ice";
                entry["count"] = rng.RandiRange(1, 2);
                break;
        }

        contents.Add(entry);
        return contents;
    }

    private Vector2 GetSpawnPosition()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;

        // Get camera position for world coordinates
        var cameraOffset = Vector2.Zero;
        var camera = GetViewport().GetCamera2D();
        if (camera != null)
        {
            cameraOffset = camera.GlobalPosition - viewportSize / 2;
        }

        // Choose a random edge
        int edge = _rng.RandiRange(0, 3);
        var pos = Vector2.Zero;

        switch (edge)
        {
            case 0: // Top
                pos.X = _rng.RandfRange(0, viewportSize.X);
                pos.Y = -Constants.DebrisSpawnMargin;
                break;
            case 1: // Right
                pos.X = viewportSize.X + Constants.DebrisSpawnMargin;
                pos.Y = _rng.RandfRange(0, viewportSize.Y);
                break;
            case 2: // Bottom
                pos.X = _rng.RandfRange(0, viewportSize.X);
                pos.Y = viewportSize.Y + Constants.DebrisSpawnMargin;
                break;
            case 3: // Left
                pos.X = -Constants.DebrisSpawnMargin;
                pos.Y = _rng.RandfRange(0, viewportSize.Y);
                break;
        }

        return pos + cameraOffset;
    }

    private Vector2 GetDriftVelocity(Vector2 spawnPos)
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;

        // Get camera position
        var cameraOffset = Vector2.Zero;
        var camera = GetViewport().GetCamera2D();
        if (camera != null)
        {
            cameraOffset = camera.GlobalPosition - viewportSize / 2;
        }

        // Target somewhere on screen (with some randomness)
        var center = cameraOffset + viewportSize / 2;
        var target = center + new Vector2(
            _rng.RandfRange(-viewportSize.X * 0.3f, viewportSize.X * 0.3f),
            _rng.RandfRange(-viewportSize.Y * 0.3f, viewportSize.Y * 0.3f)
        );

        // Direction and speed
        var direction = (target - spawnPos).Normalized();
        float speed = _rng.RandfRange(Constants.DebrisMinSpeed, Constants.DebrisMaxSpeed);

        // Add some randomness to direction
        direction = direction.Rotated(_rng.RandfRange(-0.3f, 0.3f));

        return direction * speed;
    }

    private void OnDebrisInput(Node viewport, InputEvent @event, int shapeIdx, Node2D debris)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                CollectDebris(debris);
            }
        }
    }

    /// <summary>
    /// Collect a debris entity
    /// </summary>
    public Array CollectDebris(Node2D debris)
    {
        if (!IsInstanceValid(debris))
            return new Array();

        var contents = new Array();
        if (debris.HasMethod("GetContents"))
        {
            contents = debris.Call("GetContents").AsGodotArray();
        }

        // Add items to inventory
        var collectedItems = new Array();
        foreach (Dictionary content in contents)
        {
            string itemId = content["item_id"].AsString();
            int count = content["count"].AsInt32();
            var item = InventoryManager.Instance?.GetItem(itemId);
            if (item != null)
            {
                int overflow = InventoryManager.Instance.AddItem(item, count);
                if (overflow < count)
                {
                    collectedItems.Add(new Dictionary
                    {
                        { "item", item },
                        { "count", count - overflow }
                    });
                }
            }
        }

        EmitSignal(SignalName.DebrisCollected, debris, collectedItems);
        DespawnDebris(debris);

        return collectedItems;
    }

    private void DespawnDebris(Node2D debris)
    {
        if (!IsInstanceValid(debris))
        {
            _activeDebris.Remove(debris);
            return;
        }

        EmitSignal(SignalName.Despawned, debris);
        _activeDebris.Remove(debris);
        debris.QueueFree();
    }

    /// <summary>
    /// Set the debris container node
    /// </summary>
    public void SetDebrisContainer(Node2D container)
    {
        DebrisContainer = container;
    }

    /// <summary>
    /// Clear all debris
    /// </summary>
    public void ClearAllDebris()
    {
        foreach (var debris in _activeDebris.Duplicate())
        {
            DespawnDebris(debris);
        }
        _activeDebris.Clear();
    }

    /// <summary>
    /// Get current debris count
    /// </summary>
    public int GetDebrisCount()
    {
        return _activeDebris.Count;
    }

    /// <summary>
    /// Adjust spawn rate (for progression)
    /// </summary>
    public void SetSpawnRate(float rate)
    {
        SpawnRate = Mathf.Max(0.5f, rate);
    }

    /// <summary>
    /// Add a new debris type with weight
    /// </summary>
    public void AddDebrisType(string type, int weight)
    {
        _debrisWeights[type] = weight;
    }

    /// <summary>
    /// Modify weight of existing debris type
    /// </summary>
    public void SetDebrisWeight(string type, int weight)
    {
        if (_debrisWeights.ContainsKey(type))
        {
            _debrisWeights[type] = weight;
        }
    }
}
