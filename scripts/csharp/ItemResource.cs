using Godot;

// SpaceFactory

/// <summary>
/// Resource class defining an item type.
/// </summary>
[GlobalClass]
public partial class ItemResource : Resource
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
    /// Item description
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = "";

    /// <summary>
    /// Item category
    /// </summary>
    [Export]
    public Enums.ItemCategory Category { get; set; } = Enums.ItemCategory.RawMaterial;

    /// <summary>
    /// Maximum stack size
    /// </summary>
    [Export]
    public int StackSize { get; set; } = 50;

    /// <summary>
    /// Fuel value in kJ (0 = not a fuel)
    /// </summary>
    [Export]
    public float FuelValue { get; set; } = 0.0f;

    /// <summary>
    /// Whether this item is a fluid
    /// </summary>
    [Export]
    public bool IsFluid { get; set; } = false;

    /// <summary>
    /// Color for sprite generation
    /// </summary>
    [Export]
    public Color SpriteColor { get; set; } = new Color(0.5f, 0.5f, 0.5f);
}
