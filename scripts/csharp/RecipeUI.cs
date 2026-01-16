using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// RecipeUI - UI panel for viewing all recipes in the game.
/// Shows all recipes including locked ones, with crafting requirements.
/// Toggle with 'R' key.
/// </summary>
public partial class RecipeUI : CanvasLayer
{
    /// <summary>
    /// Main panel container
    /// </summary>
    private PanelContainer _panel;

    /// <summary>
    /// Container for recipe entries
    /// </summary>
    private VBoxContainer _recipeListContainer;

    /// <summary>
    /// Filter buttons container
    /// </summary>
    private HBoxContainer _filterContainer;

    /// <summary>
    /// Current filter type (null = all)
    /// </summary>
    private Enums.CraftingType? _currentFilter = null;

    /// <summary>
    /// Filter buttons
    /// </summary>
    private readonly Dictionary<string, Button> _filterButtons = new();

    public override void _Ready()
    {
        CreateUIStructure();
        Visible = false;
    }

    private void CreateUIStructure()
    {
        // Create main panel
        _panel = new PanelContainer { Name = "Panel" };
        AddChild(_panel);

        // Style the panel
        var style = new StyleBoxFlat
        {
            BgColor = Constants.UiBackground,
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        _panel.AddThemeStyleboxOverride("panel", style);

        // Position panel (center-left)
        _panel.Position = new Vector2(50, 50);
        _panel.CustomMinimumSize = new Vector2(400, 550);

        // Main margin container
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        _panel.AddChild(margin);

        // Main VBox
        var vbox = new VBoxContainer { Name = "VBoxContainer" };
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text = "Recipes (R)",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(title);

        // Filter buttons
        _filterContainer = new HBoxContainer();
        _filterContainer.AddThemeConstantOverride("separation", 5);
        vbox.AddChild(_filterContainer);

        CreateFilterButtons();

        // Separator
        vbox.AddChild(new HSeparator());

        // Scroll container for recipe list
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 420),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        vbox.AddChild(scroll);

        _recipeListContainer = new VBoxContainer { Name = "RecipeList" };
        _recipeListContainer.AddThemeConstantOverride("separation", 6);
        _recipeListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_recipeListContainer);

        // Initial population
        RefreshRecipeList();
    }

    private void CreateFilterButtons()
    {
        var filters = new (string label, Enums.CraftingType? type)[]
        {
            ("All", null),
            ("Hand", Enums.CraftingType.Player),
            ("Furnace", Enums.CraftingType.Furnace),
            ("Assembler", Enums.CraftingType.Assembler)
        };

        foreach (var (label, type) in filters)
        {
            var btn = new Button
            {
                Text = label,
                ToggleMode = true,
                ButtonPressed = type == _currentFilter
            };

            var btnStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.25f) };
            btnStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", btnStyle);

            var btnPressedStyle = new StyleBoxFlat { BgColor = Constants.UiHighlight };
            btnPressedStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("pressed", btnPressedStyle);

            Enums.CraftingType? capturedType = type;
            btn.Pressed += () => OnFilterSelected(capturedType);

            _filterContainer.AddChild(btn);
            _filterButtons[label] = btn;
        }
    }

    private void OnFilterSelected(Enums.CraftingType? filterType)
    {
        _currentFilter = filterType;

        // Update button states
        foreach (var kvp in _filterButtons)
        {
            bool shouldBePressed = (kvp.Key == "All" && filterType == null) ||
                                   (kvp.Key == "Hand" && filterType == Enums.CraftingType.Player) ||
                                   (kvp.Key == "Furnace" && filterType == Enums.CraftingType.Furnace) ||
                                   (kvp.Key == "Assembler" && filterType == Enums.CraftingType.Assembler);
            kvp.Value.ButtonPressed = shouldBePressed;
        }

        RefreshRecipeList();
    }

    private void RefreshRecipeList()
    {
        if (_recipeListContainer == null)
            return;

        // Clear existing
        foreach (var child in _recipeListContainer.GetChildren())
        {
            child.QueueFree();
        }

        var allRecipes = CraftingManager.Instance?.GetAllRecipes();
        if (allRecipes == null)
            return;

        // Filter and sort recipes
        var filteredRecipes = new Array<RecipeResource>();
        foreach (var recipe in allRecipes)
        {
            if (_currentFilter == null || recipe.CraftingType == _currentFilter)
            {
                filteredRecipes.Add(recipe);
            }
        }

        // Create entries
        foreach (var recipe in filteredRecipes)
        {
            var entry = CreateRecipeEntry(recipe);
            _recipeListContainer.AddChild(entry);
        }

        // Show message if no recipes
        if (filteredRecipes.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "No recipes found.",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            emptyLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
            _recipeListContainer.AddChild(emptyLabel);
        }
    }

    private Control CreateRecipeEntry(RecipeResource recipe)
    {
        // Check if recipe is unlocked
        bool isUnlocked = true;
        if (!string.IsNullOrEmpty(recipe.RequiredTechnology))
        {
            isUnlocked = ResearchManager.Instance?.IsTechnologyUnlocked(recipe.RequiredTechnology) ?? false;
        }

        var panel = new PanelContainer();

        // Color based on state
        Color bgColor = isUnlocked ? new Color(0.18f, 0.18f, 0.22f) : new Color(0.12f, 0.12f, 0.14f);

        var panelStyle = new StyleBoxFlat { BgColor = bgColor };
        panelStyle.SetCornerRadiusAll(4);
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        margin.AddChild(vbox);

        // Header row: name + crafting type + time
        var headerHbox = new HBoxContainer();
        vbox.AddChild(headerHbox);

        // Recipe name
        string lockIcon = isUnlocked ? "" : "[Locked] ";
        var nameLabel = new Label
        {
            Text = lockIcon + recipe.Name,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        if (!isUnlocked)
            nameLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
        headerHbox.AddChild(nameLabel);

        // Crafting type badge
        string typeText = recipe.CraftingType switch
        {
            Enums.CraftingType.Player => "Hand",
            Enums.CraftingType.Furnace => "Furnace",
            Enums.CraftingType.Assembler => "Assembler",
            Enums.CraftingType.ChemicalPlant => "Chemical",
            Enums.CraftingType.Refinery => "Refinery",
            _ => "Other"
        };
        var typeLabel = new Label { Text = typeText };
        typeLabel.AddThemeFontSizeOverride("font_size", 10);
        typeLabel.Modulate = new Color(0.7f, 0.7f, 0.9f);
        headerHbox.AddChild(typeLabel);

        // Crafting time
        var timeLabel = new Label { Text = $" ({recipe.CraftingTime}s)" };
        timeLabel.AddThemeFontSizeOverride("font_size", 10);
        timeLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
        headerHbox.AddChild(timeLabel);

        // Ingredients → Results row
        var recipeHbox = new HBoxContainer();
        recipeHbox.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(recipeHbox);

        // Ingredients
        var ingredientsHbox = new HBoxContainer();
        ingredientsHbox.AddThemeConstantOverride("separation", 4);
        recipeHbox.AddChild(ingredientsHbox);

        var ingredients = recipe.GetIngredients();
        for (int i = 0; i < ingredients.Count; i++)
        {
            var ing = ingredients[i];
            string itemId = ing["item_id"].AsString();
            int count = ing["count"].AsInt32();

            var item = InventoryManager.Instance?.GetItem(itemId);
            string itemName = item?.Name ?? itemId;

            // Check if player has enough
            int playerCount = item != null ? InventoryManager.Instance.GetItemCount(item) : 0;
            bool hasEnough = playerCount >= count;

            var ingLabel = new Label { Text = $"{count} {itemName}" };
            ingLabel.AddThemeFontSizeOverride("font_size", 11);
            ingLabel.Modulate = hasEnough && isUnlocked ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.9f, 0.5f, 0.5f);
            ingredientsHbox.AddChild(ingLabel);

            if (i < ingredients.Count - 1)
            {
                var plusLabel = new Label { Text = "+" };
                plusLabel.AddThemeFontSizeOverride("font_size", 11);
                plusLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
                ingredientsHbox.AddChild(plusLabel);
            }
        }

        // Arrow
        var arrowLabel = new Label { Text = " → " };
        arrowLabel.AddThemeFontSizeOverride("font_size", 11);
        arrowLabel.Modulate = new Color(0.8f, 0.8f, 0.8f);
        recipeHbox.AddChild(arrowLabel);

        // Results
        var resultsHbox = new HBoxContainer();
        resultsHbox.AddThemeConstantOverride("separation", 4);
        recipeHbox.AddChild(resultsHbox);

        var results = recipe.GetResults();
        for (int i = 0; i < results.Count; i++)
        {
            var res = results[i];
            string itemId = res["item_id"].AsString();
            int count = res["count"].AsInt32();

            var item = InventoryManager.Instance?.GetItem(itemId);
            string itemName = item?.Name ?? itemId;

            var resLabel = new Label { Text = $"{count} {itemName}" };
            resLabel.AddThemeFontSizeOverride("font_size", 11);
            resLabel.Modulate = new Color(0.9f, 0.9f, 0.5f);
            resultsHbox.AddChild(resLabel);

            if (i < results.Count - 1)
            {
                var plusLabel = new Label { Text = "+" };
                plusLabel.AddThemeFontSizeOverride("font_size", 11);
                plusLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
                resultsHbox.AddChild(plusLabel);
            }
        }

        // Required technology (if locked)
        if (!isUnlocked && !string.IsNullOrEmpty(recipe.RequiredTechnology))
        {
            var techLabel = new Label
            {
                Text = $"Requires: {recipe.RequiredTechnology}"
            };
            techLabel.AddThemeFontSizeOverride("font_size", 10);
            techLabel.Modulate = new Color(0.8f, 0.5f, 0.5f);
            vbox.AddChild(techLabel);
        }

        return panel;
    }

    public void Toggle()
    {
        Visible = !Visible;
        if (Visible)
        {
            RefreshRecipeList();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("recipes"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }

        if (@event.IsActionPressed("cancel") && Visible)
        {
            Visible = false;
            GetViewport().SetInputAsHandled();
        }
    }
}
