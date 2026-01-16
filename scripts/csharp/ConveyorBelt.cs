using Godot;

// SpaceFactory

/// <summary>
/// ConveyorBelt - A 1x1 transport building that moves items in a direction.
/// Items move along the belt at a configurable speed.
/// Belts automatically connect to adjacent belts.
/// </summary>
public partial class ConveyorBelt : BuildingEntity
{
    /// <summary>
    /// Item on the belt (null if empty)
    /// </summary>
    public ItemResource BeltItem { get; set; }

    /// <summary>
    /// Progress of item along belt (0.0 = start, 1.0 = end)
    /// </summary>
    public float ItemProgress { get; set; } = 0.0f;

    /// <summary>
    /// Belt speed in tiles per second
    /// </summary>
    public float BeltSpeed { get; set; } = Constants.BeltSpeedTier1;

    /// <summary>
    /// Visual representation of item on belt
    /// </summary>
    private Sprite2D _itemSprite;

    /// <summary>
    /// Connected input belt (belt feeding into this one)
    /// </summary>
    public ConveyorBelt InputBelt { get; set; }

    /// <summary>
    /// Connected output belt (belt this feeds into)
    /// </summary>
    public ConveyorBelt OutputBelt { get; set; }

    /// <summary>
    /// Reference to adjacent building at output (if not a belt)
    /// </summary>
    public Node2D OutputBuilding { get; set; }

    private const float TicksPerSecond = 60.0f;

    public override void _Ready()
    {
        base._Ready();
        ZIndex = Constants.ZBelts;
        UpdateConnections();
    }

    protected override Texture2D GenerateTexture()
    {
        return SpriteGenerator.Instance?.GenerateBelt(GetDirection());
    }

    public override void Initialize(BuildingResource def, Vector2I pos, int rotation = 0)
    {
        base.Initialize(def, pos, rotation);
        ZIndex = Constants.ZBelts;
        UpdateConnections();
    }

    protected override void ProcessBuilding()
    {
        ProcessBeltMovement();
        UpdateItemVisual();
    }

    private void ProcessBeltMovement()
    {
        if (BeltItem == null)
        {
            // Try to receive item from input
            TryReceiveFromInput();
            return;
        }

        // Move item along belt
        float tickProgress = BeltSpeed / TicksPerSecond;
        ItemProgress += tickProgress;

        // Item reached end of belt
        if (ItemProgress >= 1.0f)
        {
            TryTransferItem();
        }
    }

    private void TryReceiveFromInput()
    {
        if (InputBelt != null && InputBelt.BeltItem != null && InputBelt.ItemProgress >= 1.0f)
        {
            // Transfer from input belt
            BeltItem = InputBelt.BeltItem;
            ItemProgress = 0.0f;
            InputBelt.BeltItem = null;
            InputBelt.ItemProgress = 0.0f;
            CreateItemSprite();
        }
    }

    private void TryTransferItem()
    {
        if (BeltItem == null)
            return;

        bool transferred = false;

        // Try to transfer to output belt
        if (OutputBelt != null)
        {
            if (OutputBelt.BeltItem == null)
            {
                OutputBelt.BeltItem = BeltItem;
                OutputBelt.ItemProgress = 0.0f;
                OutputBelt.CreateItemSprite();
                transferred = true;
            }
        }
        // Try to transfer to output building (chest, furnace, etc.)
        else if (OutputBuilding != null)
        {
            if (OutputBuilding.HasMethod("CanAcceptItem"))
            {
                var inputDir = Enums.OppositeDirection(GetDirection());
                bool canAccept = OutputBuilding.Call("CanAcceptItem", BeltItem, (int)inputDir).AsBool();
                if (canAccept)
                {
                    bool inserted = OutputBuilding.Call("InsertItem", BeltItem, 1, (int)inputDir).AsBool();
                    if (inserted)
                        transferred = true;
                }
            }
        }

        if (transferred)
        {
            BeltItem = null;
            ItemProgress = 0.0f;
            RemoveItemSprite();
        }
        else
        {
            // Item blocked, stay at end of belt
            ItemProgress = 1.0f;
        }
    }

    public void CreateItemSprite()
    {
        _itemSprite?.QueueFree();

        if (BeltItem == null)
            return;

        _itemSprite = new Sprite2D
        {
            Texture = GetItemTexture(BeltItem),
            ZIndex = Constants.ZItems,
            Scale = new Vector2(0.5f, 0.5f) // Items on belts are smaller
        };
        AddChild(_itemSprite);
        UpdateItemVisual();
    }

    private void RemoveItemSprite()
    {
        _itemSprite?.QueueFree();
        _itemSprite = null;
    }

    private Texture2D GetItemTexture(ItemResource item)
    {
        return item.Category switch
        {
            Enums.ItemCategory.RawMaterial => SpriteGenerator.Instance?.GenerateOre(item.SpriteColor, item.Id.GetHashCode()),
            Enums.ItemCategory.Processed => SpriteGenerator.Instance?.GeneratePlate(item.SpriteColor),
            Enums.ItemCategory.Component when item.Id.Contains("gear") => SpriteGenerator.Instance?.GenerateGear(item.SpriteColor),
            Enums.ItemCategory.Component when item.Id.Contains("cable") => SpriteGenerator.Instance?.GenerateCable(item.SpriteColor),
            Enums.ItemCategory.Component when item.Id.Contains("circuit") => SpriteGenerator.Instance?.GenerateCircuit(item.SpriteColor, 1),
            _ => SpriteGenerator.Instance?.GeneratePlate(item.SpriteColor)
        };
    }

    private void UpdateItemVisual()
    {
        if (_itemSprite == null || BeltItem == null)
            return;

        // Calculate item position along belt
        var dirVec = (Vector2)Enums.DirectionToVector(GetDirection());
        var startPos = new Vector2(Constants.TileSize / 2.0f, Constants.TileSize / 2.0f);
        var endPos = startPos + dirVec * Constants.TileSize * 0.4f;

        _itemSprite.Position = startPos.Lerp(endPos, ItemProgress);
    }

    private void UpdateConnections()
    {
        var myDir = GetDirection();
        var myPos = GridPosition;

        // Check for input belt (belt pointing at us)
        var inputDir = Enums.OppositeDirection(myDir);
        var inputPos = myPos + Enums.DirectionToVector(inputDir);
        var inputNode = GridManager.Instance?.GetBuilding(inputPos);

        if (inputNode is ConveyorBelt inputBelt)
        {
            // Only connect if the belt is pointing at us
            if (inputBelt.GetDirection() == myDir)
            {
                InputBelt = inputBelt;
                inputBelt.OutputBelt = this;
            }
        }
        else
        {
            InputBelt = null;
        }

        // Check for output belt or building
        var outputPos = myPos + Enums.DirectionToVector(myDir);
        var outputNode = GridManager.Instance?.GetBuilding(outputPos);

        if (outputNode is ConveyorBelt outputBelt)
        {
            OutputBelt = outputBelt;
            OutputBuilding = null;
            // Tell the other belt about us
            if (outputBelt.InputBelt == null)
            {
                var expectedInputDir = Enums.OppositeDirection(outputBelt.GetDirection());
                if (myDir == outputBelt.GetDirection() || Enums.OppositeDirection(myDir) == expectedInputDir)
                {
                    outputBelt.InputBelt = this;
                }
            }
        }
        else if (outputNode != null)
        {
            OutputBelt = null;
            OutputBuilding = outputNode;
        }
        else
        {
            OutputBelt = null;
            OutputBuilding = null;
        }
    }

    /// <summary>
    /// Update sprite when rotation changes
    /// </summary>
    public override void RotateCw()
    {
        base.RotateCw();
        SetupSprite();
        UpdateConnections();
    }

    public override void RotateCcw()
    {
        base.RotateCcw();
        SetupSprite();
        UpdateConnections();
    }

    /// <summary>
    /// Override: Belts accept items being dropped on them (from inserters)
    /// </summary>
    public override bool CanAcceptItem(ItemResource item, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (item.IsFluid)
            return false;
        return BeltItem == null;
    }

    /// <summary>
    /// Override: Insert item onto belt
    /// </summary>
    public override bool InsertItem(ItemResource item, int count = 1, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (BeltItem != null || item.IsFluid)
            return false;

        BeltItem = item;
        ItemProgress = 0.0f;
        CreateItemSprite();
        return true;
    }

    /// <summary>
    /// Override: Check for items to pick up
    /// </summary>
    public override ItemResource HasOutputItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        // Items can be picked up from belt if they're far enough along
        if (BeltItem != null && ItemProgress >= 0.5f)
            return BeltItem;
        return null;
    }

    /// <summary>
    /// Override: Extract item from belt
    /// </summary>
    public override ItemResource ExtractItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        if (BeltItem == null)
            return null;

        var item = BeltItem;
        BeltItem = null;
        ItemProgress = 0.0f;
        RemoveItemSprite();
        return item;
    }

    /// <summary>
    /// Called when adjacent buildings change
    /// </summary>
    public void OnNeighborChanged()
    {
        UpdateConnections();
    }

    /// <summary>
    /// Check if belt is empty
    /// </summary>
    public bool IsBeltEmpty()
    {
        return BeltItem == null;
    }

    /// <summary>
    /// Check if belt output is blocked
    /// </summary>
    public bool IsBlocked()
    {
        return BeltItem != null && ItemProgress >= 1.0f;
    }
}
