using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// BuildingEntity - Base class for all placeable buildings.
/// Provides common functionality for buildings including grid positioning,
/// power network integration, tick-based processing, and inventory management.
/// </summary>
public partial class BuildingEntity : Node2D
{
    // Signals
    [Signal]
    public delegate void BuildingDestroyedEventHandler(BuildingEntity building);

    /// <summary>
    /// The building definition resource
    /// </summary>
    public BuildingResource Definition { get; protected set; }

    /// <summary>
    /// Grid position of the building's origin tile
    /// </summary>
    public Vector2I GridPosition { get; protected set; } = Vector2I.Zero;

    /// <summary>
    /// Rotation index (0=North, 1=East, 2=South, 3=West)
    /// </summary>
    public int RotationIndex { get; protected set; } = 0;

    /// <summary>
    /// Whether the building is powered (if it requires power)
    /// </summary>
    public bool IsPowered { get; set; } = true;

    /// <summary>
    /// Internal inventory for buildings with storage
    /// </summary>
    protected Array<ItemStack> InternalInventory { get; set; } = new();

    /// <summary>
    /// Current crafting progress (0.0 to 1.0) for processing buildings
    /// </summary>
    public float CraftingProgress { get; protected set; } = 0.0f;

    /// <summary>
    /// Sprite for the building
    /// </summary>
    protected Sprite2D Sprite;

    public override void _Ready()
    {
        // Connect to game tick for processing
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameTick += OnGameTick;
        }

        // Set up sprite if we have a definition
        if (Definition != null)
        {
            SetupSprite();
            SetupInventory();
            RegisterPower();
        }
    }

    /// <summary>
    /// Initialize the building with a definition and position
    /// </summary>
    public virtual void Initialize(BuildingResource def, Vector2I pos, int rotation = 0)
    {
        Definition = def;
        GridPosition = pos;
        RotationIndex = rotation;

        // Position the building in world space
        Position = GridManager.Instance.GridToWorld(pos);
        ZIndex = Constants.ZBuildings;

        SetupSprite();
        SetupInventory();
        RegisterPower();
    }

    protected virtual void SetupSprite()
    {
        Sprite?.QueueFree();

        Sprite = new Sprite2D
        {
            Centered = false,
            Texture = GenerateTexture()
        };

        // Apply rotation
        Sprite.Rotation = RotationIndex * Mathf.Pi / 2;
        if (RotationIndex == 1) // East
            Sprite.Position = new Vector2(Definition.Size.Y * Constants.TileSize, 0);
        else if (RotationIndex == 2) // South
            Sprite.Position = new Vector2(Definition.Size.X * Constants.TileSize, Definition.Size.Y * Constants.TileSize);
        else if (RotationIndex == 3) // West
            Sprite.Position = new Vector2(0, Definition.Size.X * Constants.TileSize);

        AddChild(Sprite);
    }

    protected virtual Texture2D GenerateTexture()
    {
        // Override in subclasses for custom textures
        return SpriteGenerator.Instance?.GenerateBuilding(new Color(0.4f, 0.4f, 0.5f), Definition.Size);
    }

    protected virtual void SetupInventory()
    {
        if (Definition != null && Definition.StorageSlots > 0)
        {
            InternalInventory.Clear();
            for (int i = 0; i < Definition.StorageSlots; i++)
            {
                InternalInventory.Add(new ItemStack());
            }
        }
    }

    protected virtual void RegisterPower()
    {
        if (Definition == null)
            return;

        if (Definition.PowerConsumption > 0)
            PowerManager.Instance?.RegisterConsumer(this, Definition.PowerConsumption);
        if (Definition.PowerProduction > 0)
            PowerManager.Instance?.RegisterProducer(this, Definition.PowerProduction);
    }

    /// <summary>
    /// Get the building definition
    /// </summary>
    public BuildingResource GetDefinition()
    {
        return Definition;
    }

    /// <summary>
    /// Get the current rotation direction
    /// </summary>
    public Enums.Direction GetDirection()
    {
        return (Enums.Direction)RotationIndex;
    }

    /// <summary>
    /// Rotate the building clockwise
    /// </summary>
    public virtual void RotateCw()
    {
        if (Definition != null && Definition.CanRotate)
        {
            RotationIndex = (RotationIndex + 1) % 4;
            SetupSprite();
        }
    }

    /// <summary>
    /// Rotate the building counter-clockwise
    /// </summary>
    public virtual void RotateCcw()
    {
        if (Definition != null && Definition.CanRotate)
        {
            RotationIndex = (RotationIndex + 3) % 4;
            SetupSprite();
        }
    }

    /// <summary>
    /// Called each game tick
    /// </summary>
    private void OnGameTick(int tick)
    {
        if (!IsPowered && Definition != null && Definition.PowerConsumption > 0)
            return;

        ProcessBuilding();
    }

    /// <summary>
    /// Override in subclasses to implement building logic
    /// </summary>
    protected virtual void ProcessBuilding()
    {
    }

    /// <summary>
    /// Check if building can accept items (for inserters)
    /// </summary>
    public virtual bool CanAcceptItem(ItemResource item, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (InternalInventory.Count == 0)
            return false;

        foreach (var slot in InternalInventory)
        {
            if (slot.IsEmpty() || (slot.Item == item && !slot.IsFull()))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Insert an item into the building (returns true if successful)
    /// </summary>
    public virtual bool InsertItem(ItemResource item, int count = 1, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (InternalInventory.Count == 0)
            return false;

        int remaining = count;

        // Try existing stacks first
        foreach (var slot in InternalInventory)
        {
            if (slot.Item == item && !slot.IsFull())
            {
                remaining = slot.Add(remaining);
                if (remaining <= 0)
                    return true;
            }
        }

        // Try empty slots
        foreach (var slot in InternalInventory)
        {
            if (slot.IsEmpty())
            {
                slot.Item = item;
                remaining = slot.Add(remaining);
                if (remaining <= 0)
                    return true;
            }
        }

        return remaining < count;
    }

    /// <summary>
    /// Check if building has items to output (for inserters)
    /// </summary>
    public virtual ItemResource HasOutputItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        // Override in subclasses to specify output logic
        return null;
    }

    /// <summary>
    /// Extract an item from the building (returns the item if successful)
    /// </summary>
    public virtual ItemResource ExtractItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        // Override in subclasses
        return null;
    }

    /// <summary>
    /// Get the total count of a specific item in internal inventory
    /// </summary>
    public int GetInternalItemCount(ItemResource item)
    {
        int total = 0;
        foreach (var slot in InternalInventory)
        {
            if (slot.Item == item)
                total += slot.Count;
        }
        return total;
    }

    /// <summary>
    /// Remove items from internal inventory
    /// </summary>
    public bool RemoveInternalItem(ItemResource item, int count)
    {
        int remaining = count;

        foreach (var slot in InternalInventory)
        {
            if (slot.Item == item && remaining > 0)
            {
                int removed = slot.Remove(remaining);
                remaining -= removed;
                if (slot.Count <= 0)
                    slot.Item = null;
            }
        }

        return remaining <= 0;
    }

    /// <summary>
    /// Get a specific slot
    /// </summary>
    public ItemStack GetSlot(int index)
    {
        if (index >= 0 && index < InternalInventory.Count)
            return InternalInventory[index];
        return null;
    }

    /// <summary>
    /// Called when the building is removed
    /// </summary>
    public virtual void OnRemoved()
    {
        // Unregister from power network
        if (Definition != null)
        {
            if (Definition.PowerConsumption > 0)
                PowerManager.Instance?.UnregisterConsumer(this);
            if (Definition.PowerProduction > 0)
                PowerManager.Instance?.UnregisterProducer(this);
        }

        // Disconnect from game tick
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameTick -= OnGameTick;
        }

        EmitSignal(SignalName.BuildingDestroyed, this);
    }

    public override void _ExitTree()
    {
        OnRemoved();
    }
}
