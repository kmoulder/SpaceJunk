using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// Resource class defining a building type.
/// </summary>
[GlobalClass]
public partial class BuildingResource : Resource
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
    /// Building description
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = "";

    /// <summary>
    /// Building category for UI organization
    /// </summary>
    [Export]
    public Enums.BuildingCategory Category { get; set; } = Enums.BuildingCategory.Processing;

    /// <summary>
    /// Size in grid tiles (width x height)
    /// </summary>
    [Export]
    public Vector2I Size { get; set; } = new Vector2I(1, 1);

    /// <summary>
    /// Whether the building can be rotated
    /// </summary>
    [Export]
    public bool CanRotate { get; set; } = true;

    /// <summary>
    /// Power consumption in kW
    /// </summary>
    [Export]
    public float PowerConsumption { get; set; } = 0.0f;

    /// <summary>
    /// Power production in kW
    /// </summary>
    [Export]
    public float PowerProduction { get; set; } = 0.0f;

    /// <summary>
    /// Number of storage slots (for containers)
    /// </summary>
    [Export]
    public int StorageSlots { get; set; } = 0;

    /// <summary>
    /// Crafting speed multiplier
    /// </summary>
    [Export]
    public float CraftingSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Maximum number of ingredient types
    /// </summary>
    [Export]
    public int MaxIngredients { get; set; } = 0;

    /// <summary>
    /// Required technology to unlock
    /// </summary>
    [Export]
    public string RequiredTechnology { get; set; } = "";

    /// <summary>
    /// Build cost item IDs
    /// </summary>
    [Export]
    public string[] BuildCostIds { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Build cost amounts
    /// </summary>
    [Export]
    public int[] BuildCostCounts { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Get build cost as array of dictionaries
    /// </summary>
    public Array<Dictionary> GetBuildCost()
    {
        var result = new Array<Dictionary>();
        int count = Mathf.Min(BuildCostIds.Length, BuildCostCounts.Length);

        for (int i = 0; i < count; i++)
        {
            result.Add(new Dictionary
            {
                { "item_id", BuildCostIds[i] },
                { "count", BuildCostCounts[i] }
            });
        }

        return result;
    }

    /// <summary>
    /// Get all grid positions this building occupies relative to origin
    /// </summary>
    public Array<Vector2I> GetFootprint()
    {
        var result = new Array<Vector2I>();

        for (int x = 0; x < Size.X; x++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                result.Add(new Vector2I(x, y));
            }
        }

        return result;
    }
}
