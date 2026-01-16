using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// Assembler - A 2x2 building that automatically crafts items from recipes.
/// Accepts ingredients via inserters and outputs finished products.
/// </summary>
public partial class Assembler : BuildingEntity
{
    // Signals
    [Signal]
    public delegate void CraftingStartedEventHandler(RecipeResource recipe);

    [Signal]
    public delegate void CraftingCompletedEventHandler(RecipeResource recipe);

    /// <summary>
    /// Assembler tier (affects speed)
    /// </summary>
    public int Tier { get; set; } = 1;

    /// <summary>
    /// Current recipe being crafted
    /// </summary>
    public RecipeResource CurrentRecipe { get; private set; }

    /// <summary>
    /// Input slots for ingredients
    /// </summary>
    public ItemStack[] InputSlots { get; private set; } = new ItemStack[4];

    /// <summary>
    /// Output slot for finished products
    /// </summary>
    public ItemStack OutputSlot { get; private set; } = new ItemStack();

    /// <summary>
    /// Crafting progress (0.0 to 1.0)
    /// </summary>
    public new float CraftingProgress { get; private set; } = 0.0f;

    /// <summary>
    /// Whether currently crafting
    /// </summary>
    public bool IsCrafting { get; private set; } = false;

    /// <summary>
    /// Crafting speed multiplier based on tier
    /// </summary>
    private float CraftingSpeed => Tier switch
    {
        1 => 0.5f,
        2 => 0.75f,
        _ => 1.0f
    };

    public override void _Ready()
    {
        base._Ready();
        InitSlots();
    }

    private void InitSlots()
    {
        for (int i = 0; i < InputSlots.Length; i++)
        {
            InputSlots[i] = new ItemStack();
        }
    }

    protected override Texture2D GenerateTexture()
    {
        return SpriteGenerator.Instance?.GenerateAssembler(Tier);
    }

    protected override void ProcessBuilding()
    {
        if (CurrentRecipe == null)
            return;

        // Check if we can start/continue crafting
        if (!IsCrafting)
        {
            if (CanStartCrafting())
            {
                StartCrafting();
            }
        }

        if (IsCrafting)
        {
            // Progress crafting
            float progressPerTick = CraftingSpeed / (CurrentRecipe.CraftingTime * 60.0f);
            CraftingProgress += progressPerTick;

            if (CraftingProgress >= 1.0f)
            {
                CompleteCrafting();
            }
        }
    }

    /// <summary>
    /// Set the recipe for this assembler
    /// </summary>
    public void SetRecipe(RecipeResource recipe)
    {
        // Accept both Assembler and Player recipes (assemblers can automate hand-craftable items)
        if (recipe == null || (recipe.CraftingType != Enums.CraftingType.Assembler && recipe.CraftingType != Enums.CraftingType.Player))
            return;

        // Can only change recipe when not crafting
        if (IsCrafting)
            return;

        CurrentRecipe = recipe;
        CraftingProgress = 0.0f;
    }

    /// <summary>
    /// Clear the current recipe
    /// </summary>
    public void ClearRecipe()
    {
        if (IsCrafting)
            return;

        CurrentRecipe = null;
        CraftingProgress = 0.0f;
    }

    private bool CanStartCrafting()
    {
        if (CurrentRecipe == null)
            return false;

        // Check if output slot can accept result
        var results = CurrentRecipe.GetResults();
        if (results.Count > 0)
        {
            var result = results[0];
            string resultId = result["item_id"].AsString();
            int resultCount = result["count"].AsInt32();
            var resultItem = InventoryManager.Instance?.GetItem(resultId);

            if (resultItem != null)
            {
                if (!OutputSlot.IsEmpty() && OutputSlot.Item != resultItem)
                    return false;
                if (OutputSlot.Count + resultCount > 64)
                    return false;
            }
        }

        // Check if we have all ingredients
        var ingredients = CurrentRecipe.GetIngredients();
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int required = ing["count"].AsInt32();

            int available = GetInputCount(itemId);
            if (available < required)
                return false;
        }

        return true;
    }

    private void StartCrafting()
    {
        if (CurrentRecipe == null)
            return;

        // Consume ingredients
        var ingredients = CurrentRecipe.GetIngredients();
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int required = ing["count"].AsInt32();
            ConsumeInput(itemId, required);
        }

        IsCrafting = true;
        CraftingProgress = 0.0f;
        EmitSignal(SignalName.CraftingStarted, CurrentRecipe);
    }

    private void CompleteCrafting()
    {
        if (CurrentRecipe == null)
            return;

        // Add result to output
        var results = CurrentRecipe.GetResults();
        if (results.Count > 0)
        {
            var result = results[0];
            string resultId = result["item_id"].AsString();
            int resultCount = result["count"].AsInt32();
            var resultItem = InventoryManager.Instance?.GetItem(resultId);

            if (resultItem != null)
            {
                if (OutputSlot.IsEmpty())
                    OutputSlot.Item = resultItem;
                OutputSlot.Add(resultCount);
            }
        }

        IsCrafting = false;
        CraftingProgress = 0.0f;
        EmitSignal(SignalName.CraftingCompleted, CurrentRecipe);
    }

    private int GetInputCount(string itemId)
    {
        int total = 0;
        foreach (var slot in InputSlots)
        {
            if (!slot.IsEmpty() && slot.Item?.Id == itemId)
                total += slot.Count;
        }
        return total;
    }

    private void ConsumeInput(string itemId, int count)
    {
        int remaining = count;
        foreach (var slot in InputSlots)
        {
            if (remaining <= 0)
                break;

            if (!slot.IsEmpty() && slot.Item?.Id == itemId)
            {
                int toRemove = Mathf.Min(remaining, slot.Count);
                slot.Remove(toRemove);
                if (slot.Count <= 0)
                    slot.Item = null;
                remaining -= toRemove;
            }
        }
    }

    /// <summary>
    /// Override: Check if building can accept items
    /// </summary>
    public override bool CanAcceptItem(ItemResource item, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (item == null)
            return false;

        // Check if this item is an ingredient for current recipe
        if (CurrentRecipe == null)
            return false;

        var ingredients = CurrentRecipe.GetIngredients();
        bool isIngredient = false;
        foreach (var ing in ingredients)
        {
            if (ing["item_id"].AsString() == item.Id)
            {
                isIngredient = true;
                break;
            }
        }

        if (!isIngredient)
            return false;

        // Check if there's space in input slots
        foreach (var slot in InputSlots)
        {
            if (slot.IsEmpty())
                return true;
            if (slot.Item == item && !slot.IsFull())
                return true;
        }

        return false;
    }

    /// <summary>
    /// Override: Insert item into assembler input
    /// </summary>
    public override bool InsertItem(ItemResource item, int count = 1, Enums.Direction fromDirection = Enums.Direction.North)
    {
        if (!CanAcceptItem(item, fromDirection))
            return false;

        // Find best slot
        // First try to stack with existing
        foreach (var slot in InputSlots)
        {
            if (!slot.IsEmpty() && slot.Item == item && !slot.IsFull())
            {
                int overflow = slot.Add(count);
                return overflow < count;
            }
        }

        // Then try empty slot
        foreach (var slot in InputSlots)
        {
            if (slot.IsEmpty())
            {
                slot.Item = item;
                slot.Add(count);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Override: Check for output item
    /// </summary>
    public override ItemResource HasOutputItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        if (!OutputSlot.IsEmpty())
            return OutputSlot.Item;
        return null;
    }

    /// <summary>
    /// Override: Extract item from output
    /// </summary>
    public override ItemResource ExtractItem(Enums.Direction toDirection = Enums.Direction.North)
    {
        if (OutputSlot.IsEmpty())
            return null;

        var item = OutputSlot.Item;
        OutputSlot.Remove(1);
        if (OutputSlot.Count <= 0)
            OutputSlot.Item = null;
        return item;
    }

    /// <summary>
    /// Get crafting progress (0.0 to 1.0)
    /// </summary>
    public float GetCraftingProgress()
    {
        return CraftingProgress;
    }

    /// <summary>
    /// Get slot by index (for UI)
    /// </summary>
    public override ItemStack GetSlot(int index)
    {
        if (index < InputSlots.Length)
            return InputSlots[index];
        if (index == InputSlots.Length)
            return OutputSlot;
        return null;
    }
}
