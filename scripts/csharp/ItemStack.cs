using Godot;

// SpaceFactory

/// <summary>
/// Represents a stack of items in an inventory slot.
/// Contains an item reference and count.
/// </summary>
[GlobalClass]
public partial class ItemStack : RefCounted
{
    /// <summary>
    /// The item in this stack (null if empty)
    /// </summary>
    public ItemResource Item { get; set; }

    /// <summary>
    /// How many items are in the stack
    /// </summary>
    public int Count { get; set; }

    public ItemStack()
    {
        Item = null;
        Count = 0;
    }

    public ItemStack(ItemResource item, int count = 1)
    {
        Item = item;
        Count = count;
    }

    /// <summary>
    /// Check if the stack is empty
    /// </summary>
    public bool IsEmpty()
    {
        return Item == null || Count <= 0;
    }

    /// <summary>
    /// Check if the stack is at max capacity
    /// </summary>
    public bool IsFull()
    {
        if (Item == null) return false;
        return Count >= Item.StackSize;
    }

    /// <summary>
    /// Get remaining capacity
    /// </summary>
    public int GetRemainingSpace()
    {
        if (Item == null) return Constants.DefaultStackSize;
        return Item.StackSize - Count;
    }

    /// <summary>
    /// Add items to the stack, returns overflow amount
    /// </summary>
    public int Add(int amount)
    {
        if (Item == null) return amount;

        int maxStack = Item.StackSize;
        int canAdd = maxStack - Count;
        int toAdd = Mathf.Min(amount, canAdd);

        Count += toAdd;
        return amount - toAdd; // Return overflow
    }

    /// <summary>
    /// Remove items from the stack, returns amount actually removed
    /// </summary>
    public int Remove(int amount)
    {
        int toRemove = Mathf.Min(amount, Count);
        Count -= toRemove;
        return toRemove;
    }

    /// <summary>
    /// Create a copy of this stack
    /// </summary>
    public ItemStack Duplicate()
    {
        return new ItemStack(Item, Count);
    }

    /// <summary>
    /// Split the stack, returning a new stack with the specified count
    /// </summary>
    public ItemStack Split(int splitCount)
    {
        if (splitCount <= 0 || splitCount >= Count)
            return null;

        int actualSplit = Mathf.Min(splitCount, Count);
        Count -= actualSplit;

        return new ItemStack(Item, actualSplit);
    }

    /// <summary>
    /// Clear the stack
    /// </summary>
    public void Clear()
    {
        Item = null;
        Count = 0;
    }

    /// <summary>
    /// Try to merge another stack into this one
    /// Returns the remaining count in the source stack
    /// </summary>
    public int MergeFrom(ItemStack source)
    {
        if (source == null || source.IsEmpty())
            return 0;

        // Can only merge same items
        if (!IsEmpty() && Item != source.Item)
            return source.Count;

        // If empty, take the item type
        if (IsEmpty())
            Item = source.Item;

        // Add as much as we can
        int overflow = Add(source.Count);
        source.Count = overflow;

        if (source.Count <= 0)
            source.Item = null;

        return overflow;
    }
}
