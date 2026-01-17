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
    public bool CanCraft(RecipeResource recipe, int craftCount = 1)
    {
        if (recipe == null || craftCount <= 0)
            return false;

        var ingredients = recipe.GetIngredients();
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int count = ing["count"].AsInt32() * craftCount;
            var item = InventoryManager.Instance?.GetItem(itemId);

            if (item == null || !InventoryManager.Instance.HasItem(item, count))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if a recipe can be crafted, including crafting intermediate items if needed.
    /// Returns true if we have all raw materials to craft precursors.
    /// </summary>
    public bool CanCraftWithIntermediates(RecipeResource recipe, int craftCount = 1)
    {
        if (recipe == null || craftCount <= 0)
            return false;

        // If we can craft directly, we're good
        if (CanCraft(recipe, craftCount))
            return true;

        // Try to see if we can craft the missing ingredients
        // Build a virtual inventory to track what we'd have after crafting intermediates
        var virtualInventory = new System.Collections.Generic.Dictionary<string, int>();

        // Copy current inventory by scanning all slots
        if (InventoryManager.Instance != null)
        {
            foreach (var slot in InventoryManager.Instance.Inventory)
            {
                if (!slot.IsEmpty() && slot.Item != null)
                {
                    string itemId = slot.Item.Id;
                    if (!virtualInventory.ContainsKey(itemId))
                        virtualInventory[itemId] = 0;
                    virtualInventory[itemId] += slot.Count;
                }
            }
        }

        // Check if we can satisfy the recipe with intermediate crafting
        return CanSatisfyRecipeRecursively(recipe, craftCount, virtualInventory, new System.Collections.Generic.HashSet<string>());
    }

    /// <summary>
    /// Recursively check if we can satisfy a recipe's ingredients, crafting intermediates as needed.
    /// </summary>
    private bool CanSatisfyRecipeRecursively(RecipeResource recipe, int craftCount,
        System.Collections.Generic.Dictionary<string, int> virtualInventory,
        System.Collections.Generic.HashSet<string> visitedRecipes)
    {
        // Prevent infinite loops
        if (visitedRecipes.Contains(recipe.Id))
            return false;
        visitedRecipes.Add(recipe.Id);

        var ingredients = recipe.GetIngredients();
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int needed = ing["count"].AsInt32() * craftCount;

            // Check what we have
            int have = virtualInventory.TryGetValue(itemId, out int val) ? val : 0;
            int missing = needed - have;

            if (missing <= 0)
                continue; // We have enough

            // Try to find a player-craftable recipe for this item
            var itemRecipe = FindRecipeForItem(itemId);
            if (itemRecipe == null || itemRecipe.CraftingType != Enums.CraftingType.Player)
                return false; // Can't hand-craft this item

            // Calculate how many times we need to craft the recipe
            var results = itemRecipe.GetResults();
            int outputPerCraft = 1;
            foreach (var result in results)
            {
                if (result["item_id"].AsString() == itemId)
                {
                    outputPerCraft = result["count"].AsInt32();
                    break;
                }
            }

            int craftTimes = (missing + outputPerCraft - 1) / outputPerCraft; // Ceiling division

            // Recursively check if we can craft the intermediate
            if (!CanSatisfyRecipeRecursively(itemRecipe, craftTimes, virtualInventory,
                    new System.Collections.Generic.HashSet<string>(visitedRecipes)))
                return false;

            // Simulate crafting: consume ingredients
            var subIngredients = itemRecipe.GetIngredients();
            foreach (var subIng in subIngredients)
            {
                string subItemId = subIng["item_id"].AsString();
                int subNeeded = subIng["count"].AsInt32() * craftTimes;
                if (virtualInventory.ContainsKey(subItemId))
                    virtualInventory[subItemId] -= subNeeded;
            }

            // Add the crafted items
            int produced = outputPerCraft * craftTimes;
            if (!virtualInventory.ContainsKey(itemId))
                virtualInventory[itemId] = 0;
            virtualInventory[itemId] += produced;
        }

        return true;
    }

    /// <summary>
    /// Find a recipe that produces the given item ID
    /// </summary>
    public RecipeResource FindRecipeForItem(string itemId)
    {
        foreach (var recipe in _recipeRegistry.Values)
        {
            var results = recipe.GetResults();
            foreach (var result in results)
            {
                if (result["item_id"].AsString() == itemId)
                    return recipe;
            }
        }
        return null;
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
    /// Queue a recipe for crafting, automatically crafting intermediate items as needed.
    /// This allows crafting complex items when you have raw materials but not intermediates.
    /// </summary>
    public bool QueueCraftWithIntermediates(RecipeResource recipe, int count = 1)
    {
        if (recipe == null || count <= 0)
            return false;

        if (recipe.CraftingType != Enums.CraftingType.Player)
            return false;

        // If we can craft directly, just do that
        if (CanCraft(recipe, count))
            return QueueCraft(recipe, count);

        // Check if we can craft with intermediates
        if (!CanCraftWithIntermediates(recipe, count))
            return false;

        // Build the list of crafts needed, in order
        var craftList = new System.Collections.Generic.List<(RecipeResource recipe, int count)>();

        // Build virtual inventory for planning
        var virtualInventory = new System.Collections.Generic.Dictionary<string, int>();
        if (InventoryManager.Instance != null)
        {
            foreach (var slot in InventoryManager.Instance.Inventory)
            {
                if (!slot.IsEmpty() && slot.Item != null)
                {
                    string itemId = slot.Item.Id;
                    if (!virtualInventory.ContainsKey(itemId))
                        virtualInventory[itemId] = 0;
                    virtualInventory[itemId] += slot.Count;
                }
            }
        }

        // Recursively build the craft list
        if (!BuildCraftList(recipe, count, virtualInventory, craftList, new System.Collections.Generic.HashSet<string>()))
            return false;

        // Now execute all the crafts in order
        foreach (var (craftRecipe, craftCount) in craftList)
        {
            // At this point we should have ingredients for each craft
            // because we planned it out with the virtual inventory
            if (!QueueCraft(craftRecipe, craftCount))
            {
                GD.PrintErr($"Failed to queue intermediate craft: {craftRecipe.Name}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Build a list of crafts needed to satisfy a recipe, in the correct order.
    /// </summary>
    private bool BuildCraftList(RecipeResource recipe, int craftCount,
        System.Collections.Generic.Dictionary<string, int> virtualInventory,
        System.Collections.Generic.List<(RecipeResource recipe, int count)> craftList,
        System.Collections.Generic.HashSet<string> visitedRecipes)
    {
        // Prevent infinite loops
        if (visitedRecipes.Contains(recipe.Id))
            return false;
        visitedRecipes.Add(recipe.Id);

        var ingredients = recipe.GetIngredients();

        // First, check each ingredient and queue intermediate crafts
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int needed = ing["count"].AsInt32() * craftCount;

            // Check what we have
            int have = virtualInventory.TryGetValue(itemId, out int val) ? val : 0;
            int missing = needed - have;

            if (missing <= 0)
                continue; // We have enough

            // Find a recipe to craft this item
            var itemRecipe = FindRecipeForItem(itemId);
            if (itemRecipe == null || itemRecipe.CraftingType != Enums.CraftingType.Player)
                return false;

            // Calculate how many times we need to craft
            var results = itemRecipe.GetResults();
            int outputPerCraft = 1;
            foreach (var result in results)
            {
                if (result["item_id"].AsString() == itemId)
                {
                    outputPerCraft = result["count"].AsInt32();
                    break;
                }
            }

            int craftTimes = (missing + outputPerCraft - 1) / outputPerCraft;

            // Recursively build craft list for the intermediate
            if (!BuildCraftList(itemRecipe, craftTimes, virtualInventory, craftList,
                    new System.Collections.Generic.HashSet<string>(visitedRecipes)))
                return false;

            // Simulate crafting: consume ingredients from virtual inventory
            var subIngredients = itemRecipe.GetIngredients();
            foreach (var subIng in subIngredients)
            {
                string subItemId = subIng["item_id"].AsString();
                int subNeeded = subIng["count"].AsInt32() * craftTimes;
                if (virtualInventory.ContainsKey(subItemId))
                    virtualInventory[subItemId] -= subNeeded;
            }

            // Add produced items to virtual inventory
            // Note: The intermediate recipe was already added to craftList by the recursive BuildCraftList call
            int produced = outputPerCraft * craftTimes;
            if (!virtualInventory.ContainsKey(itemId))
                virtualInventory[itemId] = 0;
            virtualInventory[itemId] += produced;
        }

        // Now consume ingredients for the final recipe from virtual inventory
        foreach (var ing in ingredients)
        {
            string itemId = ing["item_id"].AsString();
            int needed = ing["count"].AsInt32() * craftCount;
            if (virtualInventory.ContainsKey(itemId))
                virtualInventory[itemId] -= needed;
        }

        // Add the final recipe to craft list
        craftList.Add((recipe, craftCount));

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
            new[] { "steel_plate" }, new[] { 1 }, 16.0f,
            "steel_processing"));

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

        // Building materials
        RegisterRecipe(CreateRecipe("foundation", "Foundation", Enums.CraftingType.Player,
            new[] { "steel_plate", "stone" }, new[] { 1, 2 },
            new[] { "foundation" }, new[] { 1 }, 2.0f,
            "station_expansion"));
    }

    /// <summary>
    /// Helper to create a recipe
    /// </summary>
    private static RecipeResource CreateRecipe(string id, string name, Enums.CraftingType type,
        string[] ingredientIds, int[] ingredientCounts,
        string[] resultIds, int[] resultCounts, float craftingTime,
        string requiredTechnology = null)
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
            CraftingTime = craftingTime,
            RequiredTechnology = requiredTechnology ?? ""
        };
    }
}
