using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// InventoryManager - Handles player inventory and item registry (Autoload singleton).
/// </summary>
public partial class InventoryManager : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static InventoryManager Instance { get; private set; }

    // Signals
    [Signal]
    public delegate void InventoryChangedEventHandler();

    [Signal]
    public delegate void HotbarChangedEventHandler();

    [Signal]
    public delegate void SlotSelectedEventHandler(int index);

    [Signal]
    public delegate void ItemAddedEventHandler(ItemResource item, int count, int slotIndex);

    [Signal]
    public delegate void ItemRemovedEventHandler(ItemResource item, int count, int slotIndex);

    /// <summary>
    /// All registered items by ID
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, ItemResource> _itemRegistry = new();

    /// <summary>
    /// Main player inventory
    /// </summary>
    public Array<ItemStack> Inventory { get; private set; } = new();

    /// <summary>
    /// Hotbar slots (references into inventory or separate)
    /// </summary>
    public Array<ItemStack> Hotbar { get; private set; } = new();

    /// <summary>
    /// Currently selected hotbar slot
    /// </summary>
    public int SelectedHotbarSlot { get; private set; } = 0;

    public override void _EnterTree()
    {
        GD.Print("[InventoryManager] _EnterTree called");
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("[InventoryManager] _Ready called");
        RegisterDefaultItems();
        GD.Print($"[InventoryManager] Registered {_itemRegistry.Count} items");
        InitializeInventory();
        GD.Print($"[InventoryManager] Initialized inventory with {Inventory.Count} slots");
    }

    /// <summary>
    /// Initialize empty inventory
    /// </summary>
    private void InitializeInventory()
    {
        Inventory.Clear();
        for (int i = 0; i < Constants.PlayerInventorySlots; i++)
        {
            Inventory.Add(new ItemStack());
        }

        Hotbar.Clear();
        for (int i = 0; i < Constants.HotbarSlots; i++)
        {
            Hotbar.Add(new ItemStack());
        }
    }

    /// <summary>
    /// Register an item in the registry
    /// </summary>
    public void RegisterItem(ItemResource item)
    {
        if (item != null && !string.IsNullOrEmpty(item.Id))
        {
            _itemRegistry[item.Id] = item;
        }
    }

    /// <summary>
    /// Get item by ID
    /// </summary>
    public ItemResource GetItem(string itemId)
    {
        return _itemRegistry.TryGetValue(itemId, out var item) ? item : null;
    }

    /// <summary>
    /// Get inventory slot
    /// </summary>
    public ItemStack GetSlot(int index)
    {
        if (index >= 0 && index < Inventory.Count)
            return Inventory[index];
        return null;
    }

    /// <summary>
    /// Add item to inventory, returns overflow
    /// </summary>
    public int AddItem(ItemResource item, int count)
    {
        if (item == null || count <= 0)
            return count;

        int remaining = count;

        // Try existing stacks first
        for (int i = 0; i < Inventory.Count && remaining > 0; i++)
        {
            var slot = Inventory[i];
            if (slot.Item == item && !slot.IsFull())
            {
                int beforeCount = slot.Count;
                remaining = slot.Add(remaining);
                if (slot.Count > beforeCount)
                    EmitSignal(SignalName.ItemAdded, item, slot.Count - beforeCount, i);
            }
        }

        // Try empty slots
        for (int i = 0; i < Inventory.Count && remaining > 0; i++)
        {
            var slot = Inventory[i];
            if (slot.IsEmpty())
            {
                slot.Item = item;
                remaining = slot.Add(remaining);
                EmitSignal(SignalName.ItemAdded, item, slot.Count, i);
            }
        }

        EmitSignal(SignalName.InventoryChanged);
        return remaining;
    }

    /// <summary>
    /// Remove item from inventory
    /// </summary>
    public bool RemoveItem(ItemResource item, int count)
    {
        if (item == null || count <= 0)
            return false;

        int remaining = count;

        for (int i = Inventory.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = Inventory[i];
            if (slot.Item == item)
            {
                int toRemove = Mathf.Min(remaining, slot.Count);
                slot.Remove(toRemove);
                remaining -= toRemove;
                EmitSignal(SignalName.ItemRemoved, item, toRemove, i);

                if (slot.Count <= 0)
                    slot.Item = null;
            }
        }

        EmitSignal(SignalName.InventoryChanged);
        return remaining <= 0;
    }

    /// <summary>
    /// Remove item from specific slot
    /// </summary>
    public bool RemoveItemAt(int index, int count = 1)
    {
        if (index < 0 || index >= Inventory.Count)
            return false;

        var slot = Inventory[index];
        if (slot.IsEmpty())
            return false;

        var item = slot.Item;
        int removed = slot.Remove(count);

        if (removed > 0)
        {
            EmitSignal(SignalName.ItemRemoved, item, removed, index);
            if (slot.Count <= 0)
                slot.Item = null;
            EmitSignal(SignalName.InventoryChanged);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if player has enough of an item
    /// </summary>
    public bool HasItem(ItemResource item, int count = 1)
    {
        return GetItemCount(item) >= count;
    }

    /// <summary>
    /// Get total count of an item in inventory
    /// </summary>
    public int GetItemCount(ItemResource item)
    {
        if (item == null)
            return 0;

        int total = 0;
        foreach (var slot in Inventory)
        {
            if (slot.Item == item)
                total += slot.Count;
        }
        return total;
    }

    /// <summary>
    /// Swap two inventory slots
    /// </summary>
    public void SwapSlots(int from, int to)
    {
        if (from < 0 || from >= Inventory.Count || to < 0 || to >= Inventory.Count)
            return;

        var fromSlot = Inventory[from];
        var toSlot = Inventory[to];

        // Try to merge if same item
        if (!fromSlot.IsEmpty() && !toSlot.IsEmpty() && fromSlot.Item == toSlot.Item)
        {
            toSlot.MergeFrom(fromSlot);
        }
        else
        {
            // Swap
            var tempItem = fromSlot.Item;
            int tempCount = fromSlot.Count;

            fromSlot.Item = toSlot.Item;
            fromSlot.Count = toSlot.Count;

            toSlot.Item = tempItem;
            toSlot.Count = tempCount;
        }

        EmitSignal(SignalName.InventoryChanged);
    }

    /// <summary>
    /// Select a hotbar slot
    /// </summary>
    public void SelectHotbar(int index)
    {
        if (index < 0 || index >= Constants.HotbarSlots)
            return;

        SelectedHotbarSlot = index;
        EmitSignal(SignalName.SlotSelected, index);
    }

    /// <summary>
    /// Register all default items
    /// </summary>
    private void RegisterDefaultItems()
    {
        // Raw materials
        RegisterItem(CreateItem("iron_ore", "Iron Ore", Enums.ItemCategory.RawMaterial, new Color(0.6f, 0.5f, 0.45f)));
        RegisterItem(CreateItem("copper_ore", "Copper Ore", Enums.ItemCategory.RawMaterial, new Color(0.8f, 0.5f, 0.3f)));
        RegisterItem(CreateItem("stone", "Stone", Enums.ItemCategory.RawMaterial, new Color(0.5f, 0.5f, 0.5f)));
        RegisterItem(CreateItem("coal", "Coal", Enums.ItemCategory.RawMaterial, new Color(0.2f, 0.2f, 0.2f), fuelValue: 4000));
        RegisterItem(CreateItem("ice", "Ice", Enums.ItemCategory.RawMaterial, new Color(0.7f, 0.8f, 0.9f)));
        RegisterItem(CreateItem("scrap_metal", "Scrap Metal", Enums.ItemCategory.RawMaterial, new Color(0.4f, 0.4f, 0.45f)));

        // Processed materials
        RegisterItem(CreateItem("iron_plate", "Iron Plate", Enums.ItemCategory.Processed, new Color(0.7f, 0.7f, 0.75f)));
        RegisterItem(CreateItem("copper_plate", "Copper Plate", Enums.ItemCategory.Processed, new Color(0.9f, 0.6f, 0.4f)));
        RegisterItem(CreateItem("steel_plate", "Steel Plate", Enums.ItemCategory.Processed, new Color(0.5f, 0.5f, 0.55f)));

        // Components
        RegisterItem(CreateItem("iron_gear", "Iron Gear", Enums.ItemCategory.Component, new Color(0.6f, 0.6f, 0.65f)));
        RegisterItem(CreateItem("copper_cable", "Copper Cable", Enums.ItemCategory.Component, new Color(0.9f, 0.5f, 0.3f)));
        RegisterItem(CreateItem("electronic_circuit", "Electronic Circuit", Enums.ItemCategory.Component, new Color(0.2f, 0.6f, 0.2f)));

        // Science packs
        RegisterItem(CreateItem("automation_science", "Automation Science Pack", Enums.ItemCategory.Science, new Color(0.8f, 0.2f, 0.2f)));
        RegisterItem(CreateItem("logistic_science", "Logistic Science Pack", Enums.ItemCategory.Science, new Color(0.2f, 0.8f, 0.2f)));

        // Building materials
        RegisterItem(CreateItem("foundation", "Foundation", Enums.ItemCategory.Processed, new Color(0.35f, 0.35f, 0.4f)));
    }

    /// <summary>
    /// Helper to create an item
    /// </summary>
    private static ItemResource CreateItem(string id, string name, Enums.ItemCategory category, Color color, int stackSize = 50, float fuelValue = 0)
    {
        var item = new ItemResource
        {
            Id = id,
            Name = name,
            Category = category,
            SpriteColor = color,
            StackSize = stackSize,
            FuelValue = fuelValue
        };
        return item;
    }

    /// <summary>
    /// Clear inventory
    /// </summary>
    public void Clear()
    {
        foreach (var slot in Inventory)
        {
            slot.Clear();
        }
        foreach (var slot in Hotbar)
        {
            slot.Clear();
        }
        EmitSignal(SignalName.InventoryChanged);
    }
}
