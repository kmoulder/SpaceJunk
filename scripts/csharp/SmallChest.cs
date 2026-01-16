using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// SmallChest - A 1x1 storage building with 16 slots.
/// Inserters can put and take items from all sides.
/// </summary>
public partial class SmallChest : BuildingEntity
{
    private const int ChestSlots = 16;

    public override void _Ready()
    {
        base._Ready();
    }

    protected override Texture2D GenerateTexture()
    {
        return SpriteGenerator.Instance?.GenerateChest(new Color(0.6f, 0.5f, 0.3f));
    }

    protected override void SetupInventory()
    {
        InternalInventory.Clear();
        for (int i = 0; i < ChestSlots; i++)
        {
            InternalInventory.Add(new ItemStack());
        }
    }

    /// <summary>
    /// Override: Chests can accept any non-fluid item
    /// </summary>
    public override bool CanAcceptItem(ItemResource item, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (item.IsFluid)
            return false;

        foreach (var slot in InternalInventory)
        {
            if (slot.IsEmpty())
                return true;
            if (slot.Item == item && !slot.IsFull())
                return true;
        }

        return false;
    }

    /// <summary>
    /// Override: Insert item into chest
    /// </summary>
    public override bool InsertItem(ItemResource item, int count = 1, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (item.IsFluid)
            return false;

        int remaining = count;

        // Try to add to existing stacks first
        foreach (var slot in InternalInventory)
        {
            if (slot.Item == item && !slot.IsFull())
            {
                remaining = slot.Add(remaining);
                if (remaining <= 0)
                    return true;
            }
        }

        // Try to add to empty slots
        foreach (var slot in InternalInventory)
        {
            if (slot.IsEmpty())
            {
                slot.Item = item;
                remaining = slot.Add(remaining);
                if (remaining <= 0)
                    return true;
            }
        }

        return remaining < count;
    }

    /// <summary>
    /// Override: Check for items to extract
    /// </summary>
    public override ItemResource HasOutputItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        // Return the first non-empty slot's item
        foreach (var slot in InternalInventory)
        {
            if (!slot.IsEmpty())
                return slot.Item;
        }
        return null;
    }

    /// <summary>
    /// Override: Extract item from chest
    /// </summary>
    public override ItemResource ExtractItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        // Extract from first non-empty slot
        foreach (var slot in InternalInventory)
        {
            if (!slot.IsEmpty())
            {
                var item = slot.Item;
                slot.Remove(1);
                if (slot.Count <= 0)
                    slot.Item = null;
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Get total number of a specific item
    /// </summary>
    public int GetItemCount(ItemResource item)
    {
        int total = 0;
        foreach (var slot in InternalInventory)
        {
            if (slot.Item == item)
                total += slot.Count;
        }
        return total;
    }

    /// <summary>
    /// Get total number of items in chest
    /// </summary>
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var slot in InternalInventory)
        {
            total += slot.Count;
        }
        return total;
    }

    /// <summary>
    /// Check how many empty slots remain
    /// </summary>
    public int GetEmptySlotCount()
    {
        int empty = 0;
        foreach (var slot in InternalInventory)
        {
            if (slot.IsEmpty())
                empty++;
        }
        return empty;
    }

    /// <summary>
    /// Check if chest is completely empty
    /// </summary>
    public bool IsChestEmpty()
    {
        foreach (var slot in InternalInventory)
        {
            if (!slot.IsEmpty())
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check if chest is completely full
    /// </summary>
    public bool IsChestFull()
    {
        foreach (var slot in InternalInventory)
        {
            if (slot.IsEmpty() || !slot.IsFull())
                return false;
        }
        return true;
    }
}
