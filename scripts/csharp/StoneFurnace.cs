using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// StoneFurnace - A 2x2 building that smelts ores into plates using coal.
/// Input: 1 ore + fuel (coal)
/// Output: 1 plate (based on recipe)
/// </summary>
public partial class StoneFurnace : BuildingEntity
{
    // Signals
    [Signal]
    public delegate void SmeltingStartedEventHandler(RecipeResource recipe);

    [Signal]
    public delegate void SmeltingProgressChangedEventHandler(float progress);

    [Signal]
    public delegate void SmeltingCompletedEventHandler(RecipeResource recipe);

    /// <summary>
    /// Fuel slot (separate from internal inventory)
    /// </summary>
    public ItemStack FuelSlot { get; private set; }

    /// <summary>
    /// Input slot for ore
    /// </summary>
    public ItemStack InputSlot { get; private set; }

    /// <summary>
    /// Output slot for smelted items
    /// </summary>
    public ItemStack OutputSlot { get; private set; }

    /// <summary>
    /// Current recipe being processed
    /// </summary>
    public RecipeResource CurrentRecipe { get; private set; }

    /// <summary>
    /// Remaining fuel burn time
    /// </summary>
    private float _fuelBurnRemaining = 0.0f;

    /// <summary>
    /// Is the furnace currently burning?
    /// </summary>
    public bool IsBurning { get; private set; } = false;

    private const float TicksPerSecond = 60.0f;

    public override void _Ready()
    {
        base._Ready();
        InitFurnaceSlots();
    }

    private void InitFurnaceSlots()
    {
        FuelSlot = new ItemStack();
        InputSlot = new ItemStack();
        OutputSlot = new ItemStack();
    }

    protected override Texture2D GenerateTexture()
    {
        return SpriteGenerator.Instance?.GenerateFurnace(false);
    }

    protected override void ProcessBuilding()
    {
        // Check if we should start a new recipe
        if (CurrentRecipe == null)
        {
            TryStartSmelting();
        }

        // Process burning and smelting
        if (CurrentRecipe != null)
        {
            ProcessSmelting();
        }
    }

    private void TryStartSmelting()
    {
        if (InputSlot.IsEmpty())
            return;

        // Find a matching furnace recipe for the input
        var recipes = CraftingManager.Instance?.GetRecipesForBuilding(Enums.CraftingType.Furnace);
        if (recipes == null)
            return;

        foreach (var recipe in recipes)
        {
            if (CanStartRecipe(recipe))
            {
                CurrentRecipe = recipe;
                CraftingProgress = 0.0f;
                EmitSignal(SignalName.SmeltingStarted, recipe);
                return;
            }
        }
    }

    private bool CanStartRecipe(RecipeResource recipe)
    {
        // Check if we have the required input
        var ingredients = recipe.GetIngredients();
        if (ingredients.Count == 0)
            return false;

        string requiredItemId = ingredients[0]["item_id"].AsString();
        int requiredCount = ingredients[0]["count"].AsInt32();

        var requiredItem = InventoryManager.Instance?.GetItem(requiredItemId);
        if (requiredItem == null)
            return false;

        if (InputSlot.Item != requiredItem || InputSlot.Count < requiredCount)
            return false;

        // Check if output can accept the result
        var results = recipe.GetResults();
        if (results.Count == 0)
            return false;

        string resultItemId = results[0]["item_id"].AsString();
        var resultItem = InventoryManager.Instance?.GetItem(resultItemId);
        if (resultItem == null)
            return false;

        if (!OutputSlot.IsEmpty() && OutputSlot.Item != resultItem)
            return false;
        if (!OutputSlot.IsEmpty() && OutputSlot.IsFull())
            return false;

        return true;
    }

    private void ProcessSmelting()
    {
        // Make sure we have fuel
        if (!EnsureFuel())
            return;

        // Progress the smelting
        float tickProgress = 1.0f / (CurrentRecipe.CraftingTime * TicksPerSecond);
        tickProgress *= Definition?.CraftingSpeed ?? 1.0f;
        CraftingProgress += tickProgress;

        // Consume fuel
        _fuelBurnRemaining -= 1.0f / TicksPerSecond;

        EmitSignal(SignalName.SmeltingProgressChanged, CraftingProgress);

        // Check if smelting is complete
        if (CraftingProgress >= 1.0f)
        {
            CompleteSmelting();
        }
    }

    private bool EnsureFuel()
    {
        // If we have burn time remaining, we're good
        if (_fuelBurnRemaining > 0)
        {
            IsBurning = true;
            return true;
        }

        // Try to consume fuel from fuel slot
        if (FuelSlot.IsEmpty())
        {
            IsBurning = false;
            return false;
        }

        if (FuelSlot.Item.FuelValue <= 0)
        {
            IsBurning = false;
            return false;
        }

        // Consume one fuel item
        _fuelBurnRemaining = FuelSlot.Item.FuelValue / 1000.0f; // Convert kJ to seconds
        FuelSlot.Remove(1);
        if (FuelSlot.Count <= 0)
            FuelSlot.Item = null;
        IsBurning = true;
        return true;
    }

    private void CompleteSmelting()
    {
        if (CurrentRecipe == null)
            return;

        // Consume input
        var ingredients = CurrentRecipe.GetIngredients();
        if (ingredients.Count > 0)
        {
            int consumeCount = ingredients[0]["count"].AsInt32();
            InputSlot.Remove(consumeCount);
            if (InputSlot.Count <= 0)
                InputSlot.Item = null;
        }

        // Produce output
        var results = CurrentRecipe.GetResults();
        if (results.Count > 0)
        {
            string resultItemId = results[0]["item_id"].AsString();
            int resultCount = results[0]["count"].AsInt32();
            var resultItem = InventoryManager.Instance?.GetItem(resultItemId);

            if (resultItem != null)
            {
                if (OutputSlot.IsEmpty())
                {
                    OutputSlot.Item = resultItem;
                    OutputSlot.Count = 0;
                }
                OutputSlot.Add(resultCount);
            }
        }

        EmitSignal(SignalName.SmeltingCompleted, CurrentRecipe);
        CurrentRecipe = null;
        CraftingProgress = 0.0f;
    }

    /// <summary>
    /// Override: Check if building can accept items
    /// </summary>
    public override bool CanAcceptItem(ItemResource item, Enums.Direction fromDirection = Enums.Direction.North)
    {
        // Accept fuel
        if (item.FuelValue > 0)
        {
            if (FuelSlot.IsEmpty() || (FuelSlot.Item == item && !FuelSlot.IsFull()))
                return true;
        }

        // Accept smeltable items
        var recipes = CraftingManager.Instance?.GetRecipesForBuilding(Enums.CraftingType.Furnace);
        if (recipes == null)
            return false;

        foreach (var recipe in recipes)
        {
            var ingredients = recipe.GetIngredients();
            if (ingredients.Count > 0)
            {
                string requiredItemId = ingredients[0]["item_id"].AsString();
                if (item.Id == requiredItemId)
                {
                    if (InputSlot.IsEmpty() || (InputSlot.Item == item && !InputSlot.IsFull()))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Override: Insert item into furnace
    /// </summary>
    public override bool InsertItem(ItemResource item, int count = 1, Enums.Direction fromDirection = Enums.Direction.North)
    {
        // Fuel goes to fuel slot
        if (item.FuelValue > 0)
        {
            if (FuelSlot.IsEmpty())
            {
                FuelSlot.Item = item;
                FuelSlot.Count = 0;
            }
            if (FuelSlot.Item == item)
            {
                int overflow = FuelSlot.Add(count);
                return overflow < count;
            }
        }

        // Ore goes to input slot
        if (InputSlot.IsEmpty())
        {
            InputSlot.Item = item;
            InputSlot.Count = 0;
        }
        if (InputSlot.Item == item)
        {
            int overflow = InputSlot.Add(count);
            return overflow < count;
        }

        return false;
    }

    /// <summary>
    /// Override: Check for output items
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
    /// Get current smelting progress (0.0 to 1.0)
    /// </summary>
    public float GetSmeltingProgress()
    {
        return CraftingProgress;
    }

    /// <summary>
    /// Get current fuel level (0.0 to 1.0)
    /// </summary>
    public float GetFuelLevel()
    {
        if (FuelSlot.IsEmpty())
            return 0.0f;
        return (float)FuelSlot.Count / FuelSlot.Item.StackSize;
    }

    /// <summary>
    /// Check if furnace is currently active
    /// </summary>
    public bool IsActive()
    {
        return IsBurning && CurrentRecipe != null;
    }
}
