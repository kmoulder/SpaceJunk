using Godot;

// SpaceFactory

/// <summary>
/// All game enumerations defined in one place for easy reference.
/// </summary>
public static class Enums
{
    /// <summary>
    /// Game states
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Building,
        Inventory,
        Research,
        GameOver
    }

    /// <summary>
    /// Item categories for organization
    /// </summary>
    public enum ItemCategory
    {
        RawMaterial,
        Processed,
        Component,
        Intermediate,
        Science,
        Tool,
        Building,
        Consumable
    }

    /// <summary>
    /// Building categories for the build menu
    /// </summary>
    public enum BuildingCategory
    {
        Collection,
        Transport,
        Processing,
        Storage,
        Power,
        Research,
        Logistics,
        Foundation
    }

    /// <summary>
    /// Crafting building types
    /// </summary>
    public enum CraftingType
    {
        Player,
        Assembler,
        Furnace,
        ChemicalPlant,
        Refinery
    }

    /// <summary>
    /// Compass directions
    /// </summary>
    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    /// <summary>
    /// Get the opposite direction
    /// </summary>
    public static Direction OppositeDirection(Direction dir)
    {
        return (Direction)(((int)dir + 2) % 4);
    }

    /// <summary>
    /// Convert direction to a vector
    /// </summary>
    public static Vector2I DirectionToVector(Direction dir)
    {
        return dir switch
        {
            Direction.North => Vector2I.Up,
            Direction.East => Vector2I.Right,
            Direction.South => Vector2I.Down,
            Direction.West => Vector2I.Left,
            _ => Vector2I.Zero
        };
    }

    /// <summary>
    /// Convert vector to direction
    /// </summary>
    public static Direction VectorToDirection(Vector2I vec)
    {
        if (vec.Y < 0) return Direction.North;
        if (vec.X > 0) return Direction.East;
        if (vec.Y > 0) return Direction.South;
        if (vec.X < 0) return Direction.West;
        return Direction.North;
    }
}
