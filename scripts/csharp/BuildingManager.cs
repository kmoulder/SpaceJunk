using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// BuildingManager - Handles building definitions, placement, and removal (Autoload singleton).
/// </summary>
public partial class BuildingManager : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static BuildingManager Instance { get; private set; }

    // Signals
    [Signal]
    public delegate void BuildingRegisteredEventHandler(BuildingResource buildingDef);

    [Signal]
    public delegate void BuildingPlacedEventHandler(BuildingEntity building);

    [Signal]
    public delegate void BuildingRemovedEventHandler(BuildingEntity building);

    [Signal]
    public delegate void BuildModeChangedEventHandler(bool enabled, BuildingResource buildingDef);

    /// <summary>
    /// All registered building resources by ID
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, BuildingResource> _buildingRegistry = new();

    /// <summary>
    /// Currently selected building for placement
    /// </summary>
    public BuildingResource SelectedBuilding { get; private set; }

    /// <summary>
    /// Current rotation for placement
    /// </summary>
    public int PlacementRotation { get; private set; } = 0;

    /// <summary>
    /// Ghost preview node
    /// </summary>
    public Node2D GhostPreview { get; private set; }

    /// <summary>
    /// Reference to the buildings layer (set by Main)
    /// </summary>
    public Node2D BuildingsLayer { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        RegisterDefaultBuildings();
    }

    /// <summary>
    /// Register a building resource
    /// </summary>
    public void RegisterBuilding(BuildingResource buildingDef)
    {
        if (buildingDef != null && !string.IsNullOrEmpty(buildingDef.Id))
        {
            _buildingRegistry[buildingDef.Id] = buildingDef;
            EmitSignal(SignalName.BuildingRegistered, buildingDef);
        }
    }

    /// <summary>
    /// Get a building resource by ID
    /// </summary>
    public BuildingResource GetBuilding(string buildingId)
    {
        return _buildingRegistry.TryGetValue(buildingId, out var building) ? building : null;
    }

    /// <summary>
    /// Get all registered buildings
    /// </summary>
    public Array<BuildingResource> GetAllBuildings()
    {
        var result = new Array<BuildingResource>();
        foreach (var building in _buildingRegistry.Values)
        {
            result.Add(building);
        }
        return result;
    }

    /// <summary>
    /// Get buildings by category
    /// </summary>
    public Array<BuildingResource> GetBuildingsByCategory(Enums.BuildingCategory category)
    {
        var result = new Array<BuildingResource>();
        foreach (var building in _buildingRegistry.Values)
        {
            if (building.Category == category)
                result.Add(building);
        }
        return result;
    }

    /// <summary>
    /// Enter build mode with a specific building
    /// </summary>
    public void EnterBuildMode(BuildingResource buildingDef)
    {
        SelectedBuilding = buildingDef;
        PlacementRotation = 0;
        GameManager.Instance?.SetGameState(Enums.GameState.Building);
        EmitSignal(SignalName.BuildModeChanged, true, buildingDef);
        CreateGhostPreview();
    }

    /// <summary>
    /// Exit build mode
    /// </summary>
    public void ExitBuildMode()
    {
        SelectedBuilding = null;
        RemoveGhostPreview();
        GameManager.Instance?.SetGameState(Enums.GameState.Playing);
        EmitSignal(SignalName.BuildModeChanged, false, (BuildingResource)null);
    }

    /// <summary>
    /// Check if currently in build mode
    /// </summary>
    public bool IsInBuildMode()
    {
        return SelectedBuilding != null;
    }

    /// <summary>
    /// Rotate placement clockwise
    /// </summary>
    public void RotatePlacementCw()
    {
        if (SelectedBuilding != null && SelectedBuilding.CanRotate)
        {
            PlacementRotation = (PlacementRotation + 1) % 4;
            UpdateGhostPreview();
        }
    }

    /// <summary>
    /// Rotate placement counter-clockwise
    /// </summary>
    public void RotatePlacementCcw()
    {
        if (SelectedBuilding != null && SelectedBuilding.CanRotate)
        {
            PlacementRotation = (PlacementRotation + 3) % 4;
            UpdateGhostPreview();
        }
    }

    /// <summary>
    /// Try to place building at grid position
    /// </summary>
    public bool TryPlaceBuilding(Vector2I gridPos)
    {
        if (SelectedBuilding == null)
            return false;

        if (!GridManager.Instance.CanPlaceBuilding(gridPos, SelectedBuilding))
            return false;

        // Check if player has resources
        if (!CanAffordBuilding(SelectedBuilding))
            return false;

        // Consume resources
        ConsumeBuildingCost(SelectedBuilding);

        // Create the building
        var building = CreateBuilding(SelectedBuilding, gridPos, PlacementRotation);
        if (building == null)
            return false;

        // Add to grid and scene
        BuildingsLayer?.AddChild(building);

        GridManager.Instance.PlaceBuilding(gridPos, building, SelectedBuilding);
        EmitSignal(SignalName.BuildingPlaced, building);

        // Notify adjacent buildings
        NotifyNeighbors(gridPos, SelectedBuilding);

        return true;
    }

    /// <summary>
    /// Remove building at grid position
    /// </summary>
    public BuildingEntity RemoveBuilding(Vector2I gridPos)
    {
        var building = GridManager.Instance.GetBuilding(gridPos) as BuildingEntity;
        if (building == null)
            return null;

        var origin = GridManager.Instance.GetBuildingOrigin(gridPos);
        var buildingDef = building.GetDefinition();

        // Remove from grid
        GridManager.Instance.RemoveBuilding(gridPos);

        // Refund items
        if (buildingDef != null)
        {
            RefundBuilding(buildingDef);
            NotifyNeighbors(origin, buildingDef);
        }

        // Remove from scene
        building.OnRemoved();
        building.QueueFree();

        EmitSignal(SignalName.BuildingRemoved, building);

        return building;
    }

    /// <summary>
    /// Update ghost preview position
    /// </summary>
    public void UpdateGhostPosition(Vector2I gridPos)
    {
        if (GhostPreview == null)
            return;

        GhostPreview.Position = GridManager.Instance.GridToWorld(gridPos);

        // Update color based on placement validity
        bool canPlace = GridManager.Instance.CanPlaceBuilding(gridPos, SelectedBuilding);
        canPlace = canPlace && CanAffordBuilding(SelectedBuilding);

        var sprite = GhostPreview.GetNodeOrNull<Sprite2D>("Sprite");
        if (sprite != null)
        {
            sprite.Modulate = canPlace
                ? new Color(0.5f, 1.0f, 0.5f, Constants.BuildingGhostAlpha)
                : new Color(1.0f, 0.5f, 0.5f, Constants.BuildingGhostAlpha);
        }
    }

    private void CreateGhostPreview()
    {
        RemoveGhostPreview();

        if (SelectedBuilding == null)
            return;

        GhostPreview = new Node2D();
        GhostPreview.ZIndex = Constants.ZGhost;

        var sprite = new Sprite2D
        {
            Name = "Sprite",
            Centered = false,
            Texture = GetBuildingTexture(SelectedBuilding),
            Modulate = new Color(1.0f, 1.0f, 1.0f, Constants.BuildingGhostAlpha)
        };

        // Apply rotation
        sprite.Rotation = PlacementRotation * Mathf.Pi / 2;
        if (PlacementRotation == 1) // East
            sprite.Position = new Vector2(SelectedBuilding.Size.Y * Constants.TileSize, 0);
        else if (PlacementRotation == 2) // South
            sprite.Position = new Vector2(SelectedBuilding.Size.X * Constants.TileSize, SelectedBuilding.Size.Y * Constants.TileSize);
        else if (PlacementRotation == 3) // West
            sprite.Position = new Vector2(0, SelectedBuilding.Size.X * Constants.TileSize);

        GhostPreview.AddChild(sprite);

        BuildingsLayer?.AddChild(GhostPreview);
    }

    private void UpdateGhostPreview()
    {
        if (GhostPreview == null)
            return;

        var sprite = GhostPreview.GetNodeOrNull<Sprite2D>("Sprite");
        if (sprite == null)
            return;

        // Update rotation
        sprite.Rotation = PlacementRotation * Mathf.Pi / 2;
        sprite.Position = Vector2.Zero;

        if (PlacementRotation == 1) // East
            sprite.Position = new Vector2(SelectedBuilding.Size.Y * Constants.TileSize, 0);
        else if (PlacementRotation == 2) // South
            sprite.Position = new Vector2(SelectedBuilding.Size.X * Constants.TileSize, SelectedBuilding.Size.Y * Constants.TileSize);
        else if (PlacementRotation == 3) // West
            sprite.Position = new Vector2(0, SelectedBuilding.Size.X * Constants.TileSize);
    }

    private void RemoveGhostPreview()
    {
        GhostPreview?.QueueFree();
        GhostPreview = null;
    }

    private Texture2D GetBuildingTexture(BuildingResource buildingDef)
    {
        return buildingDef.Id switch
        {
            "stone_furnace" => SpriteGenerator.Instance.GenerateFurnace(false),
            "electric_furnace" => SpriteGenerator.Instance.GenerateFurnace(true),
            "small_chest" => SpriteGenerator.Instance.GenerateChest(new Color(0.6f, 0.5f, 0.3f)),
            "transport_belt" => SpriteGenerator.Instance.GenerateBelt((Enums.Direction)PlacementRotation),
            "inserter" => SpriteGenerator.Instance.GenerateInserter(false),
            "long_inserter" => SpriteGenerator.Instance.GenerateInserter(true),
            "solar_panel" => SpriteGenerator.Instance.GenerateSolarPanel(),
            _ => SpriteGenerator.Instance.GenerateBuilding(new Color(0.4f, 0.4f, 0.5f), buildingDef.Size)
        };
    }

    private BuildingEntity CreateBuilding(BuildingResource buildingDef, Vector2I pos, int rotation)
    {
        BuildingEntity building = buildingDef.Id switch
        {
            "stone_furnace" => new StoneFurnace(),
            "small_chest" => new SmallChest(),
            "transport_belt" => new ConveyorBelt(),
            "inserter" => new Inserter(),
            "long_inserter" => new Inserter { IsLong = true },
            _ => new BuildingEntity()
        };

        building.Initialize(buildingDef, pos, rotation);
        return building;
    }

    private bool CanAffordBuilding(BuildingResource buildingDef)
    {
        var cost = buildingDef.GetBuildCost();
        foreach (var entry in cost)
        {
            string itemId = entry["item_id"].AsString();
            int count = entry["count"].AsInt32();
            var item = InventoryManager.Instance?.GetItem(itemId);
            if (item == null || !InventoryManager.Instance.HasItem(item, count))
                return false;
        }
        return true;
    }

    private void ConsumeBuildingCost(BuildingResource buildingDef)
    {
        var cost = buildingDef.GetBuildCost();
        foreach (var entry in cost)
        {
            string itemId = entry["item_id"].AsString();
            int count = entry["count"].AsInt32();
            var item = InventoryManager.Instance?.GetItem(itemId);
            InventoryManager.Instance?.RemoveItem(item, count);
        }
    }

    private void RefundBuilding(BuildingResource buildingDef)
    {
        // Refund 100% of materials
        var cost = buildingDef.GetBuildCost();
        foreach (var entry in cost)
        {
            string itemId = entry["item_id"].AsString();
            int count = entry["count"].AsInt32();
            var item = InventoryManager.Instance?.GetItem(itemId);
            InventoryManager.Instance?.AddItem(item, count);
        }
    }

    private void NotifyNeighbors(Vector2I pos, BuildingResource buildingDef)
    {
        var footprint = buildingDef.GetFootprint();
        var directions = new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

        foreach (Vector2I offset in footprint)
        {
            Vector2I tilePos = pos + offset;
            foreach (var dir in directions)
            {
                Vector2I neighborPos = tilePos + dir;
                var neighbor = GridManager.Instance.GetBuilding(neighborPos);
                if (neighbor != null && neighbor.HasMethod("OnNeighborChanged"))
                {
                    neighbor.Call("OnNeighborChanged");
                }
            }
        }
    }

    /// <summary>
    /// Register default buildings
    /// </summary>
    private void RegisterDefaultBuildings()
    {
        // Stone Furnace
        RegisterBuilding(new BuildingResource
        {
            Id = "stone_furnace",
            Name = "Stone Furnace",
            Description = "Smelts ores into plates using coal as fuel",
            Size = new Vector2I(2, 2),
            Category = Enums.BuildingCategory.Processing,
            CraftingSpeed = Constants.FurnaceStoneSpeed,
            MaxIngredients = 2,
            BuildCostIds = new[] { "stone" },
            BuildCostCounts = new[] { 5 }
        });

        // Small Chest
        RegisterBuilding(new BuildingResource
        {
            Id = "small_chest",
            Name = "Small Chest",
            Description = "Stores items. 16 slots.",
            Size = new Vector2I(1, 1),
            Category = Enums.BuildingCategory.Storage,
            StorageSlots = 16,
            CanRotate = false,
            BuildCostIds = new[] { "iron_plate" },
            BuildCostCounts = new[] { 2 }
        });

        // Transport Belt
        RegisterBuilding(new BuildingResource
        {
            Id = "transport_belt",
            Name = "Transport Belt",
            Description = "Moves items in a direction",
            Size = new Vector2I(1, 1),
            Category = Enums.BuildingCategory.Transport,
            BuildCostIds = new[] { "iron_gear", "iron_plate" },
            BuildCostCounts = new[] { 1, 1 }
        });

        // Inserter
        RegisterBuilding(new BuildingResource
        {
            Id = "inserter",
            Name = "Inserter",
            Description = "Moves items between buildings",
            Size = new Vector2I(1, 1),
            Category = Enums.BuildingCategory.Transport,
            BuildCostIds = new[] { "iron_gear", "iron_plate", "electronic_circuit" },
            BuildCostCounts = new[] { 1, 1, 1 }
        });

        // Long Inserter
        RegisterBuilding(new BuildingResource
        {
            Id = "long_inserter",
            Name = "Long Inserter",
            Description = "Moves items over 2 tiles",
            Size = new Vector2I(1, 1),
            Category = Enums.BuildingCategory.Transport,
            RequiredTechnology = "automation",
            BuildCostIds = new[] { "iron_gear", "iron_plate", "electronic_circuit" },
            BuildCostCounts = new[] { 1, 1, 1 }
        });

        // Solar Panel
        RegisterBuilding(new BuildingResource
        {
            Id = "solar_panel",
            Name = "Solar Panel",
            Description = "Generates power from starlight",
            Size = new Vector2I(2, 2),
            Category = Enums.BuildingCategory.Power,
            PowerProduction = Constants.SolarPanelOutput,
            CanRotate = false,
            RequiredTechnology = "solar_energy",
            BuildCostIds = new[] { "steel_plate", "electronic_circuit", "copper_plate" },
            BuildCostCounts = new[] { 5, 15, 5 }
        });
    }
}
