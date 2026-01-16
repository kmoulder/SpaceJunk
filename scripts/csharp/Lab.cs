using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// Lab - A 2x2 building that consumes science packs to progress research.
/// Accepts science packs via inserters or manual insertion.
/// When research is active, consumes required science packs over time.
/// </summary>
public partial class Lab : BuildingEntity
{
    // Signals
    [Signal]
    public delegate void ScienceConsumedEventHandler(string packId);

    [Signal]
    public delegate void ResearchingEventHandler(bool isActive);

    /// <summary>
    /// Science pack storage slots (one per pack type)
    /// </summary>
    public Dictionary<string, ItemStack> ScienceSlots { get; private set; } = new();

    /// <summary>
    /// Known science pack IDs
    /// </summary>
    private readonly string[] _sciencePackIds = { "automation_science", "logistic_science" };

    /// <summary>
    /// Ticks between consuming science packs
    /// </summary>
    private const int TicksPerConsumption = 60; // 1 second per pack

    /// <summary>
    /// Counter for consumption timing
    /// </summary>
    private int _consumptionCounter = 0;

    /// <summary>
    /// Whether currently consuming science
    /// </summary>
    public bool IsResearching { get; private set; } = false;

    public override void _Ready()
    {
        base._Ready();
        InitScienceSlots();
    }

    private void InitScienceSlots()
    {
        foreach (string packId in _sciencePackIds)
        {
            ScienceSlots[packId] = new ItemStack();
        }
    }

    protected override Texture2D GenerateTexture()
    {
        return SpriteGenerator.Instance?.GenerateLab();
    }

    protected override void ProcessBuilding()
    {
        // Check if there's active research
        if (ResearchManager.Instance?.CurrentResearch == null)
        {
            if (IsResearching)
            {
                IsResearching = false;
                EmitSignal(SignalName.Researching, false);
            }
            return;
        }

        var currentTech = ResearchManager.Instance.CurrentResearch;
        var requiredPacks = currentTech.GetScienceCost();

        // Check if we have any required packs
        bool hasRequiredPacks = false;
        foreach (var kvp in requiredPacks)
        {
            string packId = kvp.Key;
            if (ScienceSlots.TryGetValue(packId, out var slot) && !slot.IsEmpty())
            {
                hasRequiredPacks = true;
                break;
            }
        }

        if (!hasRequiredPacks)
        {
            if (IsResearching)
            {
                IsResearching = false;
                EmitSignal(SignalName.Researching, false);
            }
            return;
        }

        // We have packs and research is active
        if (!IsResearching)
        {
            IsResearching = true;
            EmitSignal(SignalName.Researching, true);
        }

        // Increment consumption counter
        _consumptionCounter++;

        if (_consumptionCounter >= TicksPerConsumption)
        {
            _consumptionCounter = 0;
            ConsumeScience(requiredPacks);
        }
    }

    private void ConsumeScience(Dictionary<string, int> requiredPacks)
    {
        // Try to consume one of each required pack type
        foreach (var kvp in requiredPacks)
        {
            string packId = kvp.Key;

            if (!ScienceSlots.TryGetValue(packId, out var slot))
                continue;

            if (slot.IsEmpty())
                continue;

            // Consume one pack
            slot.Remove(1);
            if (slot.Count <= 0)
                slot.Item = null;

            // Report to ResearchManager
            ResearchManager.Instance?.AddScience(packId, 1);
            EmitSignal(SignalName.ScienceConsumed, packId);
        }
    }

    /// <summary>
    /// Override: Check if building can accept items (only science packs)
    /// </summary>
    public override bool CanAcceptItem(ItemResource item, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (item == null)
            return false;

        // Only accept science packs
        if (item.Category != Enums.ItemCategory.Science)
            return false;

        // Check if we have a slot for this pack type
        if (!ScienceSlots.TryGetValue(item.Id, out var slot))
            return false;

        // Check if slot can accept more
        if (slot.IsEmpty())
            return true;

        return slot.Item == item && !slot.IsFull();
    }

    /// <summary>
    /// Override: Insert item into lab
    /// </summary>
    public override bool InsertItem(ItemResource item, int count = 1, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (item == null || item.Category != Enums.ItemCategory.Science)
            return false;

        if (!ScienceSlots.TryGetValue(item.Id, out var slot))
            return false;

        if (slot.IsEmpty())
        {
            slot.Item = item;
            slot.Count = 0;
        }

        if (slot.Item == item)
        {
            int overflow = slot.Add(count);
            return overflow < count;
        }

        return false;
    }

    /// <summary>
    /// Override: Labs don't output items
    /// </summary>
    public override ItemResource HasOutputItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        return null;
    }

    /// <summary>
    /// Override: Labs don't output items
    /// </summary>
    public override ItemResource ExtractItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        return null;
    }

    /// <summary>
    /// Get count of a specific science pack in this lab
    /// </summary>
    public int GetScienceCount(string packId)
    {
        if (ScienceSlots.TryGetValue(packId, out var slot))
            return slot.Count;
        return 0;
    }

    /// <summary>
    /// Check if lab is actively consuming science
    /// </summary>
    public bool IsActive()
    {
        return IsResearching && ResearchManager.Instance?.CurrentResearch != null;
    }
}
