using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// Collector - Automated debris collection building.
/// Extends a robotic arm to grab debris within range and stores collected items.
/// </summary>
public partial class Collector : BuildingEntity
{
    /// <summary>
    /// Collector tier (1 = basic, 2 = advanced)
    /// </summary>
    public int Tier { get; private set; } = 1;

    /// <summary>
    /// Collection range in tiles
    /// </summary>
    public int CollectionRange => Tier == 1 ? Constants.CollectorTier1Range : Constants.CollectorTier2Range;

    /// <summary>
    /// Items that can be collected simultaneously
    /// </summary>
    public int CollectionCapacity => Tier == 1 ? 1 : 2;

    /// <summary>
    /// Current state of the collector arm
    /// </summary>
    private enum CollectorState { Idle, Extending, Grabbing, Retracting }
    private CollectorState _state = CollectorState.Idle;

    /// <summary>
    /// Target debris being collected
    /// </summary>
    private Node2D _targetDebris;

    /// <summary>
    /// Animation progress (0.0 to 1.0)
    /// </summary>
    private float _animationProgress = 0.0f;

    /// <summary>
    /// Arm sprite for animation
    /// </summary>
    private Sprite2D _armSprite;
    private Line2D _armLine;

    /// <summary>
    /// Claw/grabber node at the end of the arm
    /// </summary>
    private Sprite2D _clawSprite;

    /// <summary>
    /// Storage slots for collected items (4 slots)
    /// </summary>
    public ItemStack[] OutputSlots { get; private set; } = new ItemStack[4];

    /// <summary>
    /// Time between collection cycles
    /// </summary>
    private float _cooldownTimer = 0.0f;

    /// <summary>
    /// World position of the building center
    /// </summary>
    private Vector2 _centerPosition;

    /// <summary>
    /// Initialize as a specific tier
    /// </summary>
    public void InitializeCollector(int tier)
    {
        Tier = tier;
    }

    public override void _Ready()
    {
        base._Ready();
        InitSlots();
        SetupCollectorVisuals();
    }

    private void InitSlots()
    {
        for (int i = 0; i < OutputSlots.Length; i++)
        {
            OutputSlots[i] = new ItemStack();
        }
    }

    public override void Initialize(BuildingResource def, Vector2I pos, int rotation = 0)
    {
        base.Initialize(def, pos, rotation);
        _centerPosition = GridManager.Instance.GridToWorldCenter(pos);
        SetupCollectorVisuals();
    }

    private void SetupCollectorVisuals()
    {
        // Create the arm line (extends from center to target)
        _armLine = new Line2D
        {
            Width = 3.0f,
            DefaultColor = new Color(0.5f, 0.5f, 0.55f),
            ZIndex = Constants.ZInserters,
            Visible = false
        };
        AddChild(_armLine);

        // Create the claw sprite at the arm's end
        _clawSprite = new Sprite2D
        {
            Texture = SpriteGenerator.Instance?.GenerateCollectorClaw(),
            Centered = true,
            ZIndex = Constants.ZInserters + 1,
            Visible = false
        };
        AddChild(_clawSprite);
    }

    protected override Texture2D GenerateTexture()
    {
        return SpriteGenerator.Instance?.GenerateCollector(Tier);
    }

    protected override void ProcessBuilding()
    {
        if (!IsPowered)
            return;

        switch (_state)
        {
            case CollectorState.Idle:
                ProcessIdle();
                break;
            case CollectorState.Extending:
                ProcessExtending();
                break;
            case CollectorState.Grabbing:
                ProcessGrabbing();
                break;
            case CollectorState.Retracting:
                ProcessRetracting();
                break;
        }
    }

    private void ProcessIdle()
    {
        // Wait for cooldown
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= 1.0f / Constants.TickRate;
            return;
        }

        // Check if all output slots are full
        if (AreOutputSlotsFull())
            return;

        // Look for debris in range
        _targetDebris = FindNearestDebris();
        if (_targetDebris != null)
        {
            _state = CollectorState.Extending;
            _animationProgress = 0.0f;
            _armLine.Visible = true;
            _clawSprite.Visible = true;
        }
    }

    /// <summary>
    /// Check if all output slots are full
    /// </summary>
    private bool AreOutputSlotsFull()
    {
        foreach (var slot in OutputSlots)
        {
            if (slot.IsEmpty() || !slot.IsFull())
                return false;
        }
        return true;
    }

    private void ProcessExtending()
    {
        // Animate arm extending toward target
        _animationProgress += Constants.CollectorArmSpeed / Constants.TickRate;

        if (!IsInstanceValid(_targetDebris))
        {
            // Target disappeared, retract
            _state = CollectorState.Retracting;
            _targetDebris = null;
            return;
        }

        UpdateArmVisual();

        if (_animationProgress >= 1.0f)
        {
            _animationProgress = 1.0f;
            _state = CollectorState.Grabbing;
        }
    }

    private void ProcessGrabbing()
    {
        // Collect the debris
        if (IsInstanceValid(_targetDebris))
        {
            CollectTargetDebris();
        }

        _state = CollectorState.Retracting;
        _animationProgress = 1.0f;
    }

    private void ProcessRetracting()
    {
        // Animate arm retracting
        _animationProgress -= Constants.CollectorArmSpeed / Constants.TickRate;

        UpdateArmVisual();

        if (_animationProgress <= 0.0f)
        {
            _animationProgress = 0.0f;
            _state = CollectorState.Idle;
            _cooldownTimer = Constants.CollectorCooldown;
            _armLine.Visible = false;
            _clawSprite.Visible = false;
            _targetDebris = null;
        }
    }

    private void UpdateArmVisual()
    {
        if (_armLine == null || _clawSprite == null)
            return;

        // Calculate arm positions
        Vector2 basePos = new Vector2(Constants.TileSize / 2.0f, Constants.TileSize / 2.0f);
        Vector2 targetPos;

        if (IsInstanceValid(_targetDebris))
        {
            // Arm goes toward target debris (in local coordinates)
            targetPos = _targetDebris.GlobalPosition - GlobalPosition;
        }
        else
        {
            // Just retract toward center
            targetPos = basePos;
        }

        // Lerp based on animation progress
        Vector2 currentEndPos = basePos.Lerp(targetPos, _animationProgress);

        // Update arm line
        _armLine.ClearPoints();
        _armLine.AddPoint(basePos);
        _armLine.AddPoint(currentEndPos);

        // Update claw position
        _clawSprite.Position = currentEndPos;

        // Rotate claw to face the direction of movement
        if (_animationProgress > 0)
        {
            float angle = (targetPos - basePos).Angle();
            _clawSprite.Rotation = angle + Mathf.Pi / 2; // Adjust for sprite orientation
        }
    }

    private Node2D FindNearestDebris()
    {
        var debrisInRange = DebrisManager.Instance?.GetDebrisInRange(_centerPosition, CollectionRange * Constants.TileSize);
        if (debrisInRange == null || debrisInRange.Count == 0)
            return null;

        // Find the nearest one
        Node2D nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var debris in debrisInRange)
        {
            if (!IsInstanceValid(debris))
                continue;

            float dist = _centerPosition.DistanceSquaredTo(debris.GlobalPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = debris;
            }
        }

        return nearest;
    }

    private void CollectTargetDebris()
    {
        if (!IsInstanceValid(_targetDebris))
            return;

        // Get contents from debris
        var contents = new Array();
        if (_targetDebris.HasMethod("GetContents"))
        {
            contents = _targetDebris.Call("GetContents").AsGodotArray();
        }

        // Add items to output slots
        foreach (Dictionary content in contents)
        {
            string itemId = content["item_id"].AsString();
            int count = content["count"].AsInt32();
            var item = InventoryManager.Instance?.GetItem(itemId);

            if (item != null)
            {
                int remaining = count;

                // First try to stack with existing items of the same type
                foreach (var slot in OutputSlots)
                {
                    if (remaining <= 0)
                        break;
                    if (!slot.IsEmpty() && slot.Item == item && !slot.IsFull())
                    {
                        int canAdd = item.StackSize - slot.Count;
                        int toAdd = Mathf.Min(remaining, canAdd);
                        slot.Add(toAdd);
                        remaining -= toAdd;
                    }
                }

                // Then try empty slots
                foreach (var slot in OutputSlots)
                {
                    if (remaining <= 0)
                        break;
                    if (slot.IsEmpty())
                    {
                        slot.Item = item;
                        int toAdd = Mathf.Min(remaining, item.StackSize);
                        slot.Add(toAdd);
                        remaining -= toAdd;
                    }
                }

                // If we couldn't fit everything, items are lost (collector is full)
                // This encourages players to set up proper extraction
            }
        }

        // Remove the debris
        _targetDebris.QueueFree();
    }

    /// <summary>
    /// Check if collector has items to output
    /// </summary>
    public override ItemResource HasOutputItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        // Return the first non-empty slot's item
        foreach (var slot in OutputSlots)
        {
            if (!slot.IsEmpty())
                return slot.Item;
        }
        return null;
    }

    /// <summary>
    /// Extract an item from the collector output
    /// </summary>
    public override ItemResource ExtractItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        // Extract from the first non-empty slot
        foreach (var slot in OutputSlots)
        {
            if (!slot.IsEmpty())
            {
                var item = slot.Item;
                slot.Remove(1);
                if (slot.Count <= 0)
                    slot.Clear();
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Get current collection state for UI display
    /// </summary>
    public string GetStateDescription()
    {
        return _state switch
        {
            CollectorState.Idle => _cooldownTimer > 0 ? "Cooling down..." : "Searching...",
            CollectorState.Extending => "Extending arm...",
            CollectorState.Grabbing => "Grabbing debris!",
            CollectorState.Retracting => "Retracting...",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get the output slot for building UI
    /// </summary>
    public override ItemStack GetSlot(int index)
    {
        if (index >= 0 && index < OutputSlots.Length)
            return OutputSlots[index];
        return null;
    }

    /// <summary>
    /// Get total item count across all slots
    /// </summary>
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var slot in OutputSlots)
        {
            if (!slot.IsEmpty())
                total += slot.Count;
        }
        return total;
    }
}
