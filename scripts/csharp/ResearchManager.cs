using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// ResearchManager - Handles the technology tree and research progress (Autoload singleton).
/// </summary>
public partial class ResearchManager : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static ResearchManager Instance { get; private set; }

    // Signals
    [Signal]
    public delegate void TechnologyRegisteredEventHandler(TechnologyResource tech);

    [Signal]
    public delegate void ResearchStartedEventHandler(TechnologyResource tech);

    [Signal]
    public delegate void ResearchProgressEventHandler(TechnologyResource tech, float progress);

    [Signal]
    public delegate void ResearchCompletedEventHandler(TechnologyResource tech);

    [Signal]
    public delegate void TechnologyUnlockedEventHandler(TechnologyResource tech);

    /// <summary>
    /// All registered technologies by ID
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, TechnologyResource> _techRegistry = new();

    /// <summary>
    /// Set of unlocked technology IDs
    /// </summary>
    private readonly System.Collections.Generic.HashSet<string> _unlockedTechnologies = new();

    /// <summary>
    /// Currently researching technology
    /// </summary>
    public TechnologyResource CurrentResearch { get; private set; }

    /// <summary>
    /// Progress on current research (science packs consumed)
    /// </summary>
    private readonly Dictionary<string, int> _researchProgressPacks = new();

    /// <summary>
    /// Whether research is currently active
    /// </summary>
    public bool IsResearching { get; private set; } = false;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        RegisterDefaultTechnologies();
    }

    /// <summary>
    /// Register a technology
    /// </summary>
    public void RegisterTechnology(TechnologyResource tech)
    {
        if (tech != null && !string.IsNullOrEmpty(tech.Id))
        {
            _techRegistry[tech.Id] = tech;
            EmitSignal(SignalName.TechnologyRegistered, tech);
        }
    }

    /// <summary>
    /// Get technology by ID
    /// </summary>
    public TechnologyResource GetTechnology(string techId)
    {
        return _techRegistry.TryGetValue(techId, out var tech) ? tech : null;
    }

    /// <summary>
    /// Get all technologies
    /// </summary>
    public Array<TechnologyResource> GetAllTechnologies()
    {
        var result = new Array<TechnologyResource>();
        foreach (var tech in _techRegistry.Values)
        {
            result.Add(tech);
        }
        return result;
    }

    /// <summary>
    /// Check if a technology is unlocked
    /// </summary>
    public bool IsTechnologyUnlocked(string techId)
    {
        return _unlockedTechnologies.Contains(techId);
    }

    /// <summary>
    /// Check if a technology can be researched
    /// </summary>
    public bool CanResearch(TechnologyResource tech)
    {
        if (tech == null)
            return false;

        // Already unlocked
        if (IsTechnologyUnlocked(tech.Id))
            return false;

        // Check prerequisites
        foreach (string prereqId in tech.Prerequisites)
        {
            if (!IsTechnologyUnlocked(prereqId))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Get available technologies (can be researched now)
    /// </summary>
    public Array<TechnologyResource> GetAvailableTechnologies()
    {
        var result = new Array<TechnologyResource>();
        foreach (var tech in _techRegistry.Values)
        {
            if (CanResearch(tech))
                result.Add(tech);
        }
        return result;
    }

    /// <summary>
    /// Start researching a technology
    /// </summary>
    public bool StartResearch(TechnologyResource tech)
    {
        if (!CanResearch(tech))
            return false;

        // Cancel current research if any
        if (CurrentResearch != null)
            CancelResearch();

        CurrentResearch = tech;
        _researchProgressPacks.Clear();

        // Initialize progress for each science pack type
        foreach (string packId in tech.SciencePackIds)
        {
            _researchProgressPacks[packId] = 0;
        }

        IsResearching = true;
        EmitSignal(SignalName.ResearchStarted, tech);
        return true;
    }

    /// <summary>
    /// Cancel current research
    /// </summary>
    public void CancelResearch()
    {
        if (CurrentResearch == null)
            return;

        // Refund consumed science packs
        foreach (var kvp in _researchProgressPacks)
        {
            int consumed = kvp.Value;
            if (consumed > 0)
            {
                var item = InventoryManager.Instance?.GetItem(kvp.Key);
                InventoryManager.Instance?.AddItem(item, consumed);
            }
        }

        CurrentResearch = null;
        _researchProgressPacks.Clear();
        IsResearching = false;
    }

    /// <summary>
    /// Add science packs to current research (called by labs when they consume science packs)
    /// </summary>
    public bool AddScience(string packId, int count = 1)
    {
        if (CurrentResearch == null)
            return false;

        if (!_researchProgressPacks.ContainsKey(packId))
            return false;

        // Get required amount for this pack type
        int required = 0;
        var cost = CurrentResearch.GetScienceCost();
        if (cost.ContainsKey(packId))
            required = cost[packId];

        // Add progress
        int current = _researchProgressPacks[packId];
        int toAdd = Mathf.Min(count, required - current);
        _researchProgressPacks[packId] = current + toAdd;

        // Emit progress
        float progress = GetResearchProgress();
        EmitSignal(SignalName.ResearchProgress, CurrentResearch, progress);

        // Check if research is complete
        if (progress >= 1.0f)
            CompleteResearch();

        return toAdd > 0;
    }

    /// <summary>
    /// Get current research progress (0.0 to 1.0)
    /// </summary>
    public float GetResearchProgress()
    {
        if (CurrentResearch == null)
            return 0.0f;

        var cost = CurrentResearch.GetScienceCost();
        if (cost.Count == 0)
            return 1.0f;

        int totalRequired = 0;
        int totalConsumed = 0;

        foreach (var kvp in cost)
        {
            totalRequired += kvp.Value;
            totalConsumed += _researchProgressPacks.ContainsKey(kvp.Key) ? _researchProgressPacks[kvp.Key] : 0;
        }

        if (totalRequired == 0)
            return 1.0f;

        return (float)totalConsumed / totalRequired;
    }

    /// <summary>
    /// Complete the current research
    /// </summary>
    private void CompleteResearch()
    {
        if (CurrentResearch == null)
            return;

        var completed = CurrentResearch;

        // Mark as unlocked
        _unlockedTechnologies.Add(completed.Id);

        // Clear current research
        CurrentResearch = null;
        _researchProgressPacks.Clear();
        IsResearching = false;

        EmitSignal(SignalName.ResearchCompleted, completed);
        EmitSignal(SignalName.TechnologyUnlocked, completed);
    }

    /// <summary>
    /// Unlock a technology directly (for cheats/debugging)
    /// </summary>
    public void UnlockTechnology(string techId)
    {
        if (_techRegistry.TryGetValue(techId, out var tech))
        {
            _unlockedTechnologies.Add(techId);
            EmitSignal(SignalName.TechnologyUnlocked, tech);
        }
    }

    /// <summary>
    /// Register default technologies
    /// </summary>
    private void RegisterDefaultTechnologies()
    {
        // Basic automation
        RegisterTechnology(CreateTech("automation_1", "Automation",
            "Unlocks Assembler Mk1 and Long Inserter",
            System.Array.Empty<string>(), new[] { "automation_science" }, new[] { 10 },
            new[] { "assembler_mk1", "long_inserter" }));

        RegisterTechnology(CreateTech("logistics_1", "Logistics",
            "Unlocks Underground Belt and Splitter",
            System.Array.Empty<string>(), new[] { "automation_science" }, new[] { 10 },
            new[] { "underground_belt", "splitter" }));

        RegisterTechnology(CreateTech("electronics", "Electronics",
            "Unlocks Electronic Circuit crafting",
            System.Array.Empty<string>(), new[] { "automation_science" }, new[] { 15 }));

        RegisterTechnology(CreateTech("steel_processing", "Steel Processing",
            "Unlocks Steel Plate smelting",
            new[] { "automation_1" }, new[] { "automation_science" }, new[] { 20 },
            null, new[] { "steel_smelting" }));

        RegisterTechnology(CreateTech("automation_2", "Automation 2",
            "Unlocks Assembler Mk2 and Fast Inserter",
            new[] { "automation_1", "electronics" }, new[] { "automation_science", "logistic_science" }, new[] { 40, 40 },
            new[] { "assembler_mk2", "fast_inserter" }));

        RegisterTechnology(CreateTech("logistics_2", "Logistics 2",
            "Unlocks Fast Transport Belt",
            new[] { "logistics_1" }, new[] { "automation_science", "logistic_science" }, new[] { 30, 30 },
            new[] { "fast_belt" }));

        RegisterTechnology(CreateTech("solar_energy", "Solar Energy",
            "Unlocks Solar Panel",
            new[] { "electronics" }, new[] { "automation_science" }, new[] { 20 },
            new[] { "solar_panel" }));

        RegisterTechnology(CreateTech("electric_energy_accumulators", "Electric Energy Accumulators",
            "Unlocks Accumulator",
            new[] { "solar_energy" }, new[] { "automation_science", "logistic_science" }, new[] { 30, 30 },
            new[] { "accumulator" }));

        RegisterTechnology(CreateTech("station_expansion", "Station Expansion",
            "Unlocks Foundation crafting for station expansion",
            new[] { "steel_processing" }, new[] { "automation_science" }, new[] { 25 },
            new[] { "foundation" }, new[] { "foundation" }));

        // Debris collection technologies
        RegisterTechnology(CreateTech("advanced_collection", "Advanced Collection",
            "Unlocks Collector Mk2 with extended range (4 tiles)",
            new[] { "automation_1" }, new[] { "automation_science", "logistic_science" }, new[] { 30, 30 },
            new[] { "collector_mk2" }));
    }

    /// <summary>
    /// Helper to create a technology
    /// </summary>
    private static TechnologyResource CreateTech(string id, string name, string description,
        string[] prerequisites, string[] packIds, int[] packCounts,
        string[] buildingUnlocks = null, string[] recipeUnlocks = null)
    {
        return new TechnologyResource
        {
            Id = id,
            Name = name,
            Description = description,
            Prerequisites = prerequisites,
            SciencePackIds = packIds,
            SciencePackCounts = packCounts,
            UnlocksBuildingIds = buildingUnlocks ?? System.Array.Empty<string>(),
            UnlocksRecipeIds = recipeUnlocks ?? System.Array.Empty<string>()
        };
    }
}
