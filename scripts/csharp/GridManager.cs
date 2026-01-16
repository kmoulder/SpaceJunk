using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// GridManager - Handles the station grid and building placement (Autoload singleton).
/// Manages foundation tiles and tracks placed buildings.
/// </summary>
public partial class GridManager : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static GridManager Instance { get; private set; }

    // Signals
    [Signal]
    public delegate void FoundationAddedEventHandler(Vector2I pos);

    [Signal]
    public delegate void FoundationRemovedEventHandler(Vector2I pos);

    [Signal]
    public delegate void BuildingPlacedEventHandler(Vector2I pos, Node2D building);

    [Signal]
    public delegate void BuildingRemovedEventHandler(Vector2I pos);

    /// <summary>
    /// Set of foundation tile positions
    /// </summary>
    private readonly Dictionary<Vector2I, bool> _foundationTiles = new();

    /// <summary>
    /// Dictionary of grid position -> building node
    /// </summary>
    private readonly Dictionary<Vector2I, Node2D> _buildings = new();

    /// <summary>
    /// Dictionary of building -> origin position
    /// </summary>
    private readonly Dictionary<Node2D, Vector2I> _buildingOrigins = new();

    public override void _EnterTree()
    {
        GD.Print("[GridManager] _EnterTree called");
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("[GridManager] _Ready called");
        InitializeStartingStation();
        GD.Print($"[GridManager] Initialized {_foundationTiles.Count} foundation tiles");
    }

    /// <summary>
    /// Create the starting station foundation
    /// </summary>
    private void InitializeStartingStation()
    {
        int halfSize = Constants.StartingStationSize / 2;

        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                AddFoundation(new Vector2I(x, y), false);
            }
        }
    }

    /// <summary>
    /// Convert world position to grid position
    /// </summary>
    public Vector2I WorldToGrid(Vector2 worldPos)
    {
        return new Vector2I(
            Mathf.FloorToInt(worldPos.X / Constants.TileSize),
            Mathf.FloorToInt(worldPos.Y / Constants.TileSize)
        );
    }

    /// <summary>
    /// Convert grid position to world position (top-left corner)
    /// </summary>
    public Vector2 GridToWorld(Vector2I gridPos)
    {
        return new Vector2(
            gridPos.X * Constants.TileSize,
            gridPos.Y * Constants.TileSize
        );
    }

    /// <summary>
    /// Convert grid position to world center
    /// </summary>
    public Vector2 GridToWorldCenter(Vector2I gridPos)
    {
        return GridToWorld(gridPos) + new Vector2(Constants.TileSize / 2.0f, Constants.TileSize / 2.0f);
    }

    /// <summary>
    /// Add a foundation tile
    /// </summary>
    public void AddFoundation(Vector2I pos, bool emitSignal = true)
    {
        if (_foundationTiles.ContainsKey(pos))
            return;

        _foundationTiles[pos] = true;

        if (emitSignal)
            EmitSignal(SignalName.FoundationAdded, pos);
    }

    /// <summary>
    /// Remove a foundation tile
    /// </summary>
    public void RemoveFoundation(Vector2I pos)
    {
        if (!_foundationTiles.ContainsKey(pos))
            return;

        // Don't remove if there's a building on it
        if (_buildings.ContainsKey(pos))
            return;

        _foundationTiles.Remove(pos);
        EmitSignal(SignalName.FoundationRemoved, pos);
    }

    /// <summary>
    /// Check if a position has foundation
    /// </summary>
    public bool HasFoundation(Vector2I pos)
    {
        return _foundationTiles.ContainsKey(pos);
    }

    /// <summary>
    /// Get all foundation positions
    /// </summary>
    public Array<Vector2I> GetAllFoundation()
    {
        var result = new Array<Vector2I>();
        foreach (var pos in _foundationTiles.Keys)
        {
            result.Add(pos);
        }
        return result;
    }

    /// <summary>
    /// Check if a building can be placed at the given position
    /// </summary>
    public bool CanPlaceBuilding(Vector2I pos, BuildingResource buildingDef)
    {
        if (buildingDef == null)
            return false;

        var footprint = buildingDef.GetFootprint();

        foreach (Vector2I offset in footprint)
        {
            Vector2I tilePos = pos + offset;

            // Must have foundation
            if (!HasFoundation(tilePos))
                return false;

            // Must be empty
            if (_buildings.ContainsKey(tilePos))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Place a building at the given position
    /// </summary>
    public void PlaceBuilding(Vector2I pos, Node2D building, BuildingResource buildingDef)
    {
        if (building == null || buildingDef == null)
            return;

        var footprint = buildingDef.GetFootprint();

        // Mark all tiles as occupied
        foreach (Vector2I offset in footprint)
        {
            Vector2I tilePos = pos + offset;
            _buildings[tilePos] = building;
        }

        _buildingOrigins[building] = pos;
        EmitSignal(SignalName.BuildingPlaced, pos, building);
    }

    /// <summary>
    /// Remove a building at the given position
    /// </summary>
    public void RemoveBuilding(Vector2I pos)
    {
        if (!_buildings.ContainsKey(pos))
            return;

        var building = _buildings[pos];
        if (building == null)
            return;

        // Get the origin position
        if (!_buildingOrigins.TryGetValue(building, out Vector2I origin))
        {
            origin = pos;
        }

        // Get building definition to find footprint
        BuildingResource buildingDef = null;
        if (building.HasMethod("GetDefinition"))
        {
            buildingDef = building.Call("GetDefinition").As<BuildingResource>();
        }

        // Remove from all occupied tiles
        if (buildingDef != null)
        {
            var footprint = buildingDef.GetFootprint();
            foreach (Vector2I offset in footprint)
            {
                Vector2I tilePos = origin + offset;
                _buildings.Remove(tilePos);
            }
        }
        else
        {
            _buildings.Remove(pos);
        }

        _buildingOrigins.Remove(building);
        EmitSignal(SignalName.BuildingRemoved, pos);
    }

    /// <summary>
    /// Get building at position
    /// </summary>
    public Node2D GetBuilding(Vector2I pos)
    {
        return _buildings.TryGetValue(pos, out Node2D building) ? building : null;
    }

    /// <summary>
    /// Check if there's a building at position
    /// </summary>
    public bool HasBuilding(Vector2I pos)
    {
        return _buildings.ContainsKey(pos);
    }

    /// <summary>
    /// Get the origin position of a building
    /// </summary>
    public Vector2I GetBuildingOrigin(Vector2I pos)
    {
        if (!_buildings.TryGetValue(pos, out Node2D building))
            return pos;

        return _buildingOrigins.TryGetValue(building, out Vector2I origin) ? origin : pos;
    }

    /// <summary>
    /// Clear all buildings
    /// </summary>
    public void ClearAllBuildings()
    {
        _buildings.Clear();
        _buildingOrigins.Clear();
    }

    /// <summary>
    /// Clear all data
    /// </summary>
    public void Clear()
    {
        _foundationTiles.Clear();
        _buildings.Clear();
        _buildingOrigins.Clear();
    }

    /// <summary>
    /// Check if a position is adjacent to existing foundation (valid for expansion)
    /// </summary>
    public bool IsAdjacentToFoundation(Vector2I pos)
    {
        // Already has foundation - not a valid expansion spot
        if (HasFoundation(pos))
            return false;

        // Check all 4 cardinal directions
        var directions = new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
        foreach (var dir in directions)
        {
            if (HasFoundation(pos + dir))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get all valid expansion positions (tiles adjacent to foundation but not foundation themselves)
    /// </summary>
    public Array<Vector2I> GetExpansionPositions()
    {
        var result = new Array<Vector2I>();
        var checked_ = new Dictionary<Vector2I, bool>();
        var directions = new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

        foreach (var foundationPos in _foundationTiles.Keys)
        {
            foreach (var dir in directions)
            {
                var adjacent = foundationPos + dir;
                if (!HasFoundation(adjacent) && !checked_.ContainsKey(adjacent))
                {
                    result.Add(adjacent);
                    checked_[adjacent] = true;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Try to place foundation at position (must be adjacent to existing foundation)
    /// Returns true if successful
    /// </summary>
    public bool TryPlaceFoundation(Vector2I pos)
    {
        if (!IsAdjacentToFoundation(pos))
            return false;

        AddFoundation(pos, true);
        return true;
    }
}
