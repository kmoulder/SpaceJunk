using Godot;

// SpaceFactory

/// <summary>
/// Inserter - A 1x1 building that moves items between buildings/belts.
/// Picks up items from behind (input side) and drops them in front (output side).
/// Has a swing animation as it transfers items.
/// </summary>
public partial class Inserter : BuildingEntity
{
    /// <summary>
    /// Item currently being held by the inserter
    /// </summary>
    public ItemResource HeldItem { get; private set; }

    /// <summary>
    /// Current arm angle (0 = pickup position, 1 = drop position)
    /// </summary>
    public float ArmPosition { get; private set; } = 0.0f;

    /// <summary>
    /// Whether the inserter is swinging (moving the arm)
    /// </summary>
    public bool IsSwinging { get; private set; } = false;

    /// <summary>
    /// Direction of swing (1 = forward to drop, -1 = backward to pickup)
    /// </summary>
    private int _swingDirection = 1;

    /// <summary>
    /// Whether this is a long inserter (can reach 2 tiles)
    /// </summary>
    public bool IsLong { get; set; } = false;

    /// <summary>
    /// Filter: only pick up this item (null = no filter)
    /// </summary>
    public ItemResource ItemFilter { get; set; }

    /// <summary>
    /// Visual sprites
    /// </summary>
    private Sprite2D _baseSprite;
    private Sprite2D _armSprite;
    private Sprite2D _handSprite;

    /// <summary>
    /// Reference to source building
    /// </summary>
    private Node2D _sourceBuilding;

    /// <summary>
    /// Reference to destination building
    /// </summary>
    private Node2D _destinationBuilding;

    private const float TicksPerSecond = 60.0f;

    public override void _Ready()
    {
        base._Ready();
        ZIndex = Constants.ZInserters;
        UpdateTargets();
    }

    protected override Texture2D GenerateTexture()
    {
        // We'll create custom visuals instead
        return null;
    }

    protected override void SetupSprite()
    {
        // Clear existing sprites
        _baseSprite?.QueueFree();
        _armSprite?.QueueFree();
        _handSprite?.QueueFree();

        // Create base platform
        var baseImg = Image.CreateEmpty(Constants.TileSize, Constants.TileSize, false, Image.Format.Rgba8);
        baseImg.Fill(Colors.Transparent);

        var baseColor = new Color(0.5f, 0.5f, 0.2f);
        for (int x = 8; x < 24; x++)
        {
            for (int y = 20; y < 28; y++)
            {
                baseImg.SetPixel(x, y, baseColor);
            }
        }

        _baseSprite = new Sprite2D
        {
            Texture = ImageTexture.CreateFromImage(baseImg),
            Centered = false
        };
        AddChild(_baseSprite);

        // Create arm
        var armImg = Image.CreateEmpty(Constants.TileSize, Constants.TileSize, false, Image.Format.Rgba8);
        armImg.Fill(Colors.Transparent);

        var armColor = new Color(0.6f, 0.6f, 0.3f);
        int armLength = IsLong ? 20 : 14;
        for (int y = 16 - armLength; y < 20; y++)
        {
            if (y >= 0)
            {
                for (int x = 14; x < 18; x++)
                {
                    armImg.SetPixel(x, y, armColor);
                }
            }
        }

        _armSprite = new Sprite2D
        {
            Texture = ImageTexture.CreateFromImage(armImg),
            Centered = false
        };
        AddChild(_armSprite);

        // Create hand/gripper
        var handImg = Image.CreateEmpty(16, 8, false, Image.Format.Rgba8);
        handImg.Fill(Colors.Transparent);

        for (int x = 2; x < 14; x++)
        {
            for (int y = 2; y < 6; y++)
            {
                handImg.SetPixel(x, y, armColor);
            }
        }

        _handSprite = new Sprite2D
        {
            Texture = ImageTexture.CreateFromImage(handImg),
            Centered = true,
            Position = new Vector2(Constants.TileSize / 2.0f, 8)
        };
        AddChild(_handSprite);

        UpdateArmVisual();
    }

    public override void Initialize(BuildingResource def, Vector2I pos, int rotation = 0)
    {
        base.Initialize(def, pos, rotation);
        ZIndex = Constants.ZInserters;
        UpdateTargets();
    }

    protected override void ProcessBuilding()
    {
        if (!IsPowered)
            return;

        UpdateTargets();

        if (IsSwinging)
        {
            ProcessSwing();
        }
        else
        {
            TryStartAction();
        }

        UpdateArmVisual();
    }

    private void TryStartAction()
    {
        if (HeldItem == null)
        {
            // Try to pick up item
            TryPickup();
        }
        else
        {
            // Try to drop item
            TryDrop();
        }
    }

    private void TryPickup()
    {
        if (_sourceBuilding == null)
            return;

        // Check if source has items
        var pickupDir = GetDirection(); // We pick up from our facing direction's opposite
        ItemResource availableItem = null;

        if (_sourceBuilding.HasMethod("HasOutputItem"))
        {
            availableItem = _sourceBuilding.Call("HasOutputItem", (int)pickupDir).As<ItemResource>();
        }

        if (availableItem == null)
            return;

        // Check filter
        if (ItemFilter != null && availableItem != ItemFilter)
            return;

        // Check if destination can accept
        if (_destinationBuilding != null && _destinationBuilding.HasMethod("CanAcceptItem"))
        {
            var dropDir = Enums.OppositeDirection(GetDirection());
            bool canAccept = _destinationBuilding.Call("CanAcceptItem", availableItem, (int)dropDir).AsBool();
            if (!canAccept)
                return;
        }

        // Start swinging to pickup position
        IsSwinging = true;
        _swingDirection = -1; // Swing backward to pickup
        ArmPosition = 0.5f; // Start from middle
    }

    private void TryDrop()
    {
        if (_destinationBuilding == null)
            return;

        // Start swinging to drop position
        IsSwinging = true;
        _swingDirection = 1; // Swing forward to drop
        ArmPosition = 0.0f; // Start from pickup position
    }

    private void ProcessSwing()
    {
        float swingSpeed = 1.0f / (Constants.InserterSwingTime * TicksPerSecond);
        ArmPosition += _swingDirection * swingSpeed;

        // Complete pickup
        if (_swingDirection == -1 && ArmPosition <= 0.0f)
        {
            ArmPosition = 0.0f;
            CompletePickup();
        }

        // Complete drop
        if (_swingDirection == 1 && ArmPosition >= 1.0f)
        {
            ArmPosition = 1.0f;
            CompleteDrop();
        }
    }

    private void CompletePickup()
    {
        IsSwinging = false;

        if (_sourceBuilding == null)
            return;

        if (_sourceBuilding.HasMethod("ExtractItem"))
        {
            var pickupDir = Enums.OppositeDirection(GetDirection());
            HeldItem = _sourceBuilding.Call("ExtractItem", (int)pickupDir).As<ItemResource>();
        }

        // If we got an item, start moving to drop
        if (HeldItem != null)
        {
            IsSwinging = true;
            _swingDirection = 1;
        }
    }

    private void CompleteDrop()
    {
        IsSwinging = false;

        if (HeldItem == null)
            return;

        if (_destinationBuilding == null)
            return;

        if (_destinationBuilding.HasMethod("InsertItem"))
        {
            var dropDir = Enums.OppositeDirection(GetDirection());
            bool inserted = _destinationBuilding.Call("InsertItem", HeldItem, 1, (int)dropDir).AsBool();
            if (inserted)
                HeldItem = null;
        }

        // If we couldn't drop, we'll try again next tick
    }

    private void UpdateTargets()
    {
        var myPos = GridPosition;
        var myDir = GetDirection();

        // Source is behind us (opposite of facing direction)
        int sourceOffset = IsLong ? 2 : 1;
        var sourceDir = Enums.OppositeDirection(myDir);
        var sourcePos = myPos + Enums.DirectionToVector(sourceDir) * sourceOffset;
        _sourceBuilding = GridManager.Instance?.GetBuilding(sourcePos);

        // Destination is in front of us (facing direction)
        int destOffset = IsLong ? 2 : 1;
        var destPos = myPos + Enums.DirectionToVector(myDir) * destOffset;
        _destinationBuilding = GridManager.Instance?.GetBuilding(destPos);
    }

    private void UpdateArmVisual()
    {
        if (_armSprite == null || _handSprite == null)
            return;

        // Rotate arm based on facing direction and arm position
        float baseRotation = RotationIndex * Mathf.Pi / 2;
        float swingAngle = Mathf.Lerp(-Mathf.Pi / 3, Mathf.Pi / 3, ArmPosition);

        _armSprite.Rotation = baseRotation;
        _armSprite.Position = Vector2.Zero;

        // Update hand position
        float armLength = IsLong ? 20.0f : 14.0f;
        var pivot = new Vector2(Constants.TileSize / 2.0f, 24);
        var handOffset = new Vector2(0, -armLength).Rotated(baseRotation + swingAngle);
        _handSprite.Position = pivot + handOffset;
        _handSprite.Rotation = baseRotation + swingAngle;
    }

    /// <summary>
    /// Override rotation to update visuals
    /// </summary>
    public override void RotateCw()
    {
        base.RotateCw();
        SetupSprite();
        UpdateTargets();
    }

    public override void RotateCcw()
    {
        base.RotateCcw();
        SetupSprite();
        UpdateTargets();
    }

    /// <summary>
    /// Set filter for this inserter
    /// </summary>
    public void SetFilter(ItemResource item)
    {
        ItemFilter = item;
    }

    /// <summary>
    /// Clear filter
    /// </summary>
    public void ClearFilter()
    {
        ItemFilter = null;
    }

    /// <summary>
    /// Get current filter
    /// </summary>
    public ItemResource GetFilter()
    {
        return ItemFilter;
    }

    /// <summary>
    /// Check if inserter is idle
    /// </summary>
    public bool IsIdle()
    {
        return !IsSwinging && HeldItem == null;
    }

    /// <summary>
    /// Check if inserter is holding an item
    /// </summary>
    public bool IsHolding()
    {
        return HeldItem != null;
    }

    /// <summary>
    /// Called when adjacent buildings change
    /// </summary>
    public void OnNeighborChanged()
    {
        UpdateTargets();
    }
}
