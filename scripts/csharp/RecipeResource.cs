using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// Resource class defining a crafting recipe.
/// </summary>
[GlobalClass]
public partial class RecipeResource : Resource
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
    /// Recipe description
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = "";

    /// <summary>
    /// Where this recipe can be crafted
    /// </summary>
    [Export]
    public Enums.CraftingType CraftingType { get; set; } = Enums.CraftingType.Player;

    /// <summary>
    /// Time to craft in seconds
    /// </summary>
    [Export]
    public float CraftingTime { get; set; } = 1.0f;

    /// <summary>
    /// Input item IDs
    /// </summary>
    [Export]
    public string[] IngredientIds { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Input item counts
    /// </summary>
    [Export]
    public int[] IngredientCounts { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Output item IDs
    /// </summary>
    [Export]
    public string[] ResultIds { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Output item counts
    /// </summary>
    [Export]
    public int[] ResultCounts { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Required technology to unlock
    /// </summary>
    [Export]
    public string RequiredTechnology { get; set; } = "";

    /// <summary>
    /// Get ingredients as dictionaries
    /// </summary>
    public Array<Dictionary> GetIngredients()
    {
        var result = new Array<Dictionary>();
        int count = Mathf.Min(IngredientIds.Length, IngredientCounts.Length);

        for (int i = 0; i < count; i++)
        {
            result.Add(new Dictionary
            {
                { "item_id", IngredientIds[i] },
                { "count", IngredientCounts[i] }
            });
        }

        return result;
    }

    /// <summary>
    /// Get results as dictionaries
    /// </summary>
    public Array<Dictionary> GetResults()
    {
        var result = new Array<Dictionary>();
        int count = Mathf.Min(ResultIds.Length, ResultCounts.Length);

        for (int i = 0; i < count; i++)
        {
            result.Add(new Dictionary
            {
                { "item_id", ResultIds[i] },
                { "count", ResultCounts[i] }
            });
        }

        return result;
    }

    /// <summary>
    /// Get the primary result item ID
    /// </summary>
    public string GetPrimaryResultId()
    {
        if (ResultIds.Length > 0)
            return ResultIds[0];
        return "";
    }
}
