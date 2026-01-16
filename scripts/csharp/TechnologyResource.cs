using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// Resource class defining a technology that can be researched.
/// </summary>
[GlobalClass]
public partial class TechnologyResource : Resource
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Export]
    public string Id { get; set; } = "";

    /// <summary>
    /// Display name
    /// </summary>
    [Export]
    public string Name { get; set; } = "";

    /// <summary>
    /// Technology description
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = "";

    /// <summary>
    /// Prerequisite technology IDs
    /// </summary>
    [Export]
    public string[] Prerequisites { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Science pack IDs required
    /// </summary>
    [Export]
    public string[] SciencePackIds { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Science pack counts required
    /// </summary>
    [Export]
    public int[] SciencePackCounts { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Building IDs unlocked by this technology
    /// </summary>
    [Export]
    public string[] UnlocksBuildingIds { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Recipe IDs unlocked by this technology
    /// </summary>
    [Export]
    public string[] UnlocksRecipeIds { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Get science cost as dictionary
    /// </summary>
    public Dictionary<string, int> GetScienceCost()
    {
        var result = new Dictionary<string, int>();
        int count = Mathf.Min(SciencePackIds.Length, SciencePackCounts.Length);

        for (int i = 0; i < count; i++)
        {
            result[SciencePackIds[i]] = SciencePackCounts[i];
        }

        return result;
    }

    /// <summary>
    /// Get total science packs required
    /// </summary>
    public int GetTotalScienceRequired()
    {
        int total = 0;
        foreach (int count in SciencePackCounts)
        {
            total += count;
        }
        return total;
    }
}
