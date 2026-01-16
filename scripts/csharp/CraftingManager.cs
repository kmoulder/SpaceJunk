using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// CraftingManager - Handles crafting recipes and player crafting queue (Autoload singleton).
/// </summary>
public partial class CraftingManager : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static CraftingManager Instance { get; private set; }

    // Signals
    [Signal]
    public delegate void RecipeRegisteredEventHandler(RecipeResource recipe);

    [Signal]
    public delegate void CraftStartedEventHandler(RecipeResource recipe);

    [Signal]
    public delegate void CraftProgressChangedEventHandler(RecipeResource recipe, float progress);

    [Signal]
    public delegate void CraftCompletedEventHandler(RecipeResource recipe);

    [Signal]
    public delegate void CraftCancelledEventHandler(RecipeResource recipe);

    [Signal]
    public delegate void QueueChangedEventHandler();

    /// <summary>
    /// All registered recipes by ID
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, RecipeResource> _recipeRegistry = new();

    /// <summary>
    /// Player's crafting queue
    /// </summary>
    public Array<RecipeResource> CraftQueue { get; private set; } = new();

    /// <summary>
    /// Current crafting progress (0.0 to 1.0)
    /// </summary>
    public float CraftProgress { get; private set; } = 0.0f;

    /// <summary>
    /// Whether currently crafting
    /// </summary>
    public bool IsCrafting { get; private set; } = false;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        RegisterDefaultRecipes();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameTick += OnGameTick;
        }
    }

    /// <summary>
    /// Register a recipe
    /// </summary>
    public void RegisterRecipe(RecipeResource recipe)
    {
        if (recipe != null && !string.IsNullOrEmpty(recipe.Id))
        {
            _recipeRegistry[recipe.Id] = recipe;
            EmitSignal(SignalName.RecipeRegistered, recipe);
        }
    }

    /// <summary>
    /// Get recipe by ID
    /// </summary>
    public RecipeResource GetRecipe(string recipeId)
    {
        return _recipeRegistry.TryGetValue(recipeId, out var recipe) ? recipe : null;
    }

    /// <summary>
    /// Get all recipes
    /// </summary>
    public Array<RecipeResource> GetAllRecipes()
    {
        var result = new Array<RecipeResource>();
        foreach (var recipe in _recipeRegistry.Values)
        {
            result.Add(recipe);
        }
        return result;
    }

    /// <summary>
    /// Get recipes by crafting type
    /// </summary>
    public Array<RecipeResource> GetRecipesForBuilding(Enums.CraftingType craftingType)
    {
        var result = new Array<RecipeResource>();
        foreach (var recipe in _recipeRegistry.Values)
        {
            if (recipe.CraftingType == craftingType)
                result.Add(recipe);
        }
        return result;
    }

    /// <summary>
    /// Check if a recipe can be crafted (player has ingredients)
    /// </summary>
    public bool CanCraft(RecipeResource recipe)
    {
        if (recipe == null)
            return false;

        var ingredients = recipe.GetIngredients();
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int count = ing["count"].AsInt32();
            var item = InventoryManager.Instance?.GetItem(itemId);

            if (item == null || !InventoryManager.Instance.HasItem(item, count))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Queue a recipe for crafting
    /// </summary>
    public bool QueueCraft(RecipeResource recipe, int count = 1)
    {
        if (recipe == null || count <= 0)
            return false;

        if (recipe.CraftingType != Enums.CraftingType.Player)
            return false;

        // Check ingredients for entire batch
        var ingredients = recipe.GetIngredients();
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int needed = ing["count"].AsInt32() * count;
            var item = InventoryManager.Instance?.GetItem(itemId);

            if (item == null || !InventoryManager.Instance.HasItem(item, needed))
                return false;
        }

        // Consume ingredients
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int needed = ing["count"].AsInt32() * count;
            var item = InventoryManager.Instance?.GetItem(itemId);
            InventoryManager.Instance?.RemoveItem(item, needed);
        }

        // Add to queue
        for (int i = 0; i < count; i++)
        {
            CraftQueue.Add(recipe);
        }

        EmitSignal(SignalName.QueueChanged);

        if (!IsCrafting)
            StartNextCraft();

        return true;
    }

    /// <summary>
    /// Cancel current craft and refund
    /// </summary>
    public void CancelCraft()
    {
        if (!IsCrafting || CraftQueue.Count == 0)
            return;

        var recipe = CraftQueue[0];
        CraftQueue.RemoveAt(0);

        // Refund ingredients
        var ingredients = recipe.GetIngredients();
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int count = ing["count"].AsInt32();
            var item = InventoryManager.Instance?.GetItem(itemId);
            InventoryManager.Instance?.AddItem(item, count);
        }

        IsCrafting = false;
        CraftProgress = 0.0f;

        EmitSignal(SignalName.CraftCancelled, recipe);
        EmitSignal(SignalName.QueueChanged);

        if (CraftQueue.Count > 0)
            StartNextCraft();
    }

    /// <summary>
    /// Handle game tick
    /// </summary>
    private void OnGameTick(int tick)
    {
        if (!IsCrafting || CraftQueue.Count == 0)
            return;

        var recipe = CraftQueue[0];
        float tickProgress = 1.0f / (recipe.CraftingTime * Constants.TickRate);
        CraftProgress += tickProgress;

        EmitSignal(SignalName.CraftProgressChanged, recipe, CraftProgress);

        if (CraftProgress >= 1.0f)
            CompleteCraft();
    }

    /// <summary>
    /// Start crafting next item in queue
    /// </summary>
    private void StartNextCraft()
    {
        if (CraftQueue.Count == 0)
        {
            IsCrafting = false;
            return;
        }

        IsCrafting = true;
        CraftProgress = 0.0f;
        EmitSignal(SignalName.CraftStarted, CraftQueue[0]);
    }

    /// <summary>
    /// Complete current craft
    /// </summary>
    private void CompleteCraft()
    {
        if (CraftQueue.Count == 0)
            return;

        var recipe = CraftQueue[0];
        CraftQueue.RemoveAt(0);

        // Give results
        var results = recipe.GetResults();
        foreach (var result in results)
        {
            string itemId = result["item_id"].AsString();
            int count = result["count"].AsInt32();
            var item = InventoryManager.Instance?.GetItem(itemId);
            InventoryManager.Instance?.AddItem(item, count);
        }

        IsCrafting = false;
        CraftProgress = 0.0f;

        EmitSignal(SignalName.CraftCompleted, recipe);
        EmitSignal(SignalName.QueueChanged);

        if (CraftQueue.Count > 0)
            StartNextCraft();
    }

    /// <summary>
    /// Register default recipes
    /// </summary>
    private void RegisterDefaultRecipes()
    {
        // Smelting recipes (Furnace)
        RegisterRecipe(CreateRecipe("iron_smelting", "Iron Plate", Enums.CraftingType.Furnace,
            new[] { "iron_ore" }, new[] { 1 },
            new[] { "iron_plate" }, new[] { 1 }, 3.2f));

        RegisterRecipe(CreateRecipe("copper_smelting", "Copper Plate", Enums.CraftingType.Furnace,
            new[] { "copper_ore" }, new[] { 1 },
            new[] { "copper_plate" }, new[] { 1 }, 3.2f));

        RegisterRecipe(CreateRecipe("steel_smelting", "Steel Plate", Enums.CraftingType.Furnace,
            new[] { "iron_plate" }, new[] { 5 },
            new[] { "steel_plate" }, new[] { 1 }, 16.0f));

        // Player/Assembler recipes
        RegisterRecipe(CreateRecipe("iron_gear", "Iron Gear", Enums.CraftingType.Player,
            new[] { "iron_plate" }, new[] { 2 },
            new[] { "iron_gear" }, new[] { 1 }, 0.5f));

        RegisterRecipe(CreateRecipe("copper_cable", "Copper Cable", Enums.CraftingType.Player,
            new[] { "copper_plate" }, new[] { 1 },
            new[] { "copper_cable" }, new[] { 2 }, 0.5f));

        RegisterRecipe(CreateRecipe("electronic_circuit", "Electronic Circuit", Enums.CraftingType.Player,
            new[] { "iron_plate", "copper_cable" }, new[] { 1, 3 },
            new[] { "electronic_circuit" }, new[] { 1 }, 0.5f));

        // Science pack recipes
        RegisterRecipe(CreateRecipe("automation_science_pack", "Automation Science Pack", Enums.CraftingType.Player,
            new[] { "copper_plate", "iron_gear" }, new[] { 1, 1 },
            new[] { "automation_science" }, new[] { 1 }, 5.0f));

        RegisterRecipe(CreateRecipe("logistic_science_pack", "Logistic Science Pack", Enums.CraftingType.Player,
            new[] { "iron_gear", "electronic_circuit" }, new[] { 1, 1 },
            new[] { "logistic_science" }, new[] { 1 }, 6.0f));
    }

    /// <summary>
    /// Helper to create a recipe
    /// </summary>
    private static RecipeResource CreateRecipe(string id, string name, Enums.CraftingType type,
        string[] ingredientIds, int[] ingredientCounts,
        string[] resultIds, int[] resultCounts, float craftingTime)
    {
        return new RecipeResource
        {
            Id = id,
            Name = name,
            CraftingType = type,
            IngredientIds = ingredientIds,
            IngredientCounts = ingredientCounts,
            ResultIds = resultIds,
            ResultCounts = resultCounts,
            CraftingTime = craftingTime
        };
    }
}
