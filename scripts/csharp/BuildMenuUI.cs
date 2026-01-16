using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// BuildMenuUI - UI for selecting buildings to place.
/// Shows available buildings organized by category.
/// Toggle with 'B' key.
/// </summary>
public partial class BuildMenuUI : CanvasLayer
{
    [Export]
    public PanelContainer Panel { get; set; }

    [Export]
    public HBoxContainer CategoriesContainer { get; set; }

    [Export]
    public GridContainer BuildingsContainer { get; set; }

    [Export]
    public PanelContainer InfoPanel { get; set; }

    [Export]
    public RichTextLabel InfoLabel { get; set; }

    /// <summary>
    /// Currently selected category
    /// </summary>
    private Enums.BuildingCategory _selectedCategory = Enums.BuildingCategory.Processing;

    /// <summary>
    /// Category buttons
    /// </summary>
    private readonly Dictionary<Enums.BuildingCategory, Button> _categoryButtons = new();

    /// <summary>
    /// Building buttons
    /// </summary>
    private readonly Array<Button> _buildingButtons = new();

    public override void _Ready()
    {
        SetupUI();
        ConnectSignals();
        Visible = false;
    }

    private void SetupUI()
    {
        // Create main panel if not in scene
        if (Panel == null)
        {
            CreateUIStructure();
        }

        SetupCategories();
        UpdateBuildingsDisplay();
    }

    private void CreateUIStructure()
    {
        // Create the UI programmatically
        Panel = new PanelContainer { Name = "Panel" };
        AddChild(Panel);

        // Style the panel
        var style = new StyleBoxFlat
        {
            BgColor = Constants.UiBackground,
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        Panel.AddThemeStyleboxOverride("panel", style);

        // Position panel
        Panel.Position = new Vector2(20, 100);
        Panel.CustomMinimumSize = new Vector2(300, 400);

        // Main margin container
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        Panel.AddChild(margin);

        // Main VBox
        var vbox = new VBoxContainer { Name = "VBoxContainer" };
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text = "Build Menu (B)",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(title);

        // Categories
        CategoriesContainer = new HBoxContainer { Name = "Categories" };
        CategoriesContainer.AddThemeConstantOverride("separation", 5);
        vbox.AddChild(CategoriesContainer);

        // Separator
        vbox.AddChild(new HSeparator());

        // Buildings grid
        BuildingsContainer = new GridContainer { Name = "Buildings", Columns = 4 };
        BuildingsContainer.AddThemeConstantOverride("h_separation", 8);
        BuildingsContainer.AddThemeConstantOverride("v_separation", 8);
        vbox.AddChild(BuildingsContainer);

        // Info panel
        InfoPanel = new PanelContainer
        {
            Name = "InfoPanel",
            CustomMinimumSize = new Vector2(0, 80)
        };
        vbox.AddChild(InfoPanel);

        var infoStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f)
        };
        infoStyle.SetCornerRadiusAll(4);
        InfoPanel.AddThemeStyleboxOverride("panel", infoStyle);

        InfoLabel = new RichTextLabel
        {
            Name = "InfoLabel",
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false
        };
        InfoLabel.AddThemeFontSizeOverride("normal_font_size", 12);
        InfoPanel.AddChild(InfoLabel);
    }

    private void SetupCategories()
    {
        if (CategoriesContainer == null)
            return;

        // Clear existing
        foreach (var child in CategoriesContainer.GetChildren())
        {
            child.QueueFree();
        }
        _categoryButtons.Clear();

        // Add category buttons for categories that have buildings
        var categoriesToShow = new[]
        {
            Enums.BuildingCategory.Processing,
            Enums.BuildingCategory.Storage,
            Enums.BuildingCategory.Transport,
            Enums.BuildingCategory.Power,
            Enums.BuildingCategory.Research
        };

        foreach (var category in categoriesToShow)
        {
            var btn = new Button
            {
                Text = GetCategoryName(category),
                ToggleMode = true,
                ButtonPressed = category == _selectedCategory
            };
            btn.Pressed += () => OnCategorySelected(category);

            var btnStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.25f) };
            btnStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", btnStyle);

            var btnStylePressed = new StyleBoxFlat { BgColor = Constants.UiHighlight };
            btnStylePressed.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("pressed", btnStylePressed);

            CategoriesContainer.AddChild(btn);
            _categoryButtons[category] = btn;
        }
    }

    private static string GetCategoryName(Enums.BuildingCategory category)
    {
        return category switch
        {
            Enums.BuildingCategory.Collection => "Collect",
            Enums.BuildingCategory.Transport => "Transport",
            Enums.BuildingCategory.Processing => "Process",
            Enums.BuildingCategory.Storage => "Storage",
            Enums.BuildingCategory.Power => "Power",
            Enums.BuildingCategory.Research => "Research",
            Enums.BuildingCategory.Logistics => "Logistics",
            Enums.BuildingCategory.Foundation => "Foundation",
            _ => "Unknown"
        };
    }

    private void UpdateBuildingsDisplay()
    {
        if (BuildingsContainer == null)
            return;

        // Clear existing
        foreach (var child in BuildingsContainer.GetChildren())
        {
            child.QueueFree();
        }
        _buildingButtons.Clear();

        // Get buildings for selected category
        var buildings = BuildingManager.Instance?.GetBuildingsByCategory(_selectedCategory);
        if (buildings == null)
            return;

        foreach (var building in buildings)
        {
            // Check if technology is unlocked (skip tech-locked buildings)
            if (!string.IsNullOrEmpty(building.RequiredTechnology))
            {
                if (ResearchManager.Instance != null && !ResearchManager.Instance.IsTechnologyUnlocked(building.RequiredTechnology))
                    continue;
            }

            var btn = CreateBuildingButton(building);
            BuildingsContainer.AddChild(btn);
            _buildingButtons.Add(btn);
        }

        // Update info to show first building or empty
        if (_buildingButtons.Count > 0)
        {
            UpdateInfo(buildings[0]);
        }
        else
        {
            if (InfoLabel != null)
                InfoLabel.Text = "No buildings available in this category.";
        }
    }

    private Button CreateBuildingButton(BuildingResource building)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(56, 56),
            TooltipText = building.Name
        };

        // Add icon
        var icon = new TextureRect
        {
            Texture = GetBuildingIcon(building),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(48, 48),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        btn.AddChild(icon);

        // Style
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.25f),
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.3f, 0.3f, 0.35f),
            BorderColor = Constants.UiHighlight
        };
        hoverStyle.SetBorderWidthAll(2);
        hoverStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);

        // Connect signals
        btn.Pressed += () => OnBuildingSelected(building);
        btn.MouseEntered += () => OnBuildingHovered(building);

        return btn;
    }

    private Texture2D GetBuildingIcon(BuildingResource building)
    {
        return building.Id switch
        {
            "stone_furnace" => SpriteGenerator.Instance?.GenerateFurnace(false),
            "electric_furnace" => SpriteGenerator.Instance?.GenerateFurnace(true),
            "small_chest" => SpriteGenerator.Instance?.GenerateChest(new Color(0.6f, 0.5f, 0.3f)),
            "transport_belt" => SpriteGenerator.Instance?.GenerateBelt(Enums.Direction.East),
            "inserter" => SpriteGenerator.Instance?.GenerateInserter(false),
            "long_inserter" => SpriteGenerator.Instance?.GenerateInserter(true),
            "assembler_mk1" => SpriteGenerator.Instance?.GenerateAssembler(1),
            "assembler_mk2" => SpriteGenerator.Instance?.GenerateAssembler(2),
            "solar_panel" => SpriteGenerator.Instance?.GenerateSolarPanel(),
            "lab" => SpriteGenerator.Instance?.GenerateLab(),
            _ => SpriteGenerator.Instance?.GenerateBuilding(new Color(0.4f, 0.4f, 0.5f), building.Size)
        };
    }

    private void UpdateInfo(BuildingResource building)
    {
        if (InfoLabel == null)
            return;

        string text = $"[b]{building.Name}[/b]\n";
        text += building.Description + "\n\n";

        // Show build cost
        var cost = building.GetBuildCost();
        if (cost.Count > 0)
        {
            text += "Cost: ";
            var costParts = new Array<string>();
            foreach (Dictionary entry in cost)
            {
                string itemId = entry["item_id"].AsString();
                int count = entry["count"].AsInt32();
                var item = InventoryManager.Instance?.GetItem(itemId);
                string itemName = item != null ? item.Name : itemId;
                int hasCount = item != null ? InventoryManager.Instance.GetItemCount(item) : 0;
                string color = hasCount >= count ? "green" : "red";
                costParts.Add($"[color={color}]{count}[/color] {itemName}");
            }
            text += string.Join(", ", costParts);
        }

        InfoLabel.Text = text;
    }

    private void ConnectSignals()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.BuildModeChanged += OnBuildModeChanged;
        }
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.TechnologyUnlocked += OnTechnologyUnlocked;
        }
    }

    private void OnTechnologyUnlocked(TechnologyResource tech)
    {
        // Refresh display when a technology is unlocked to show newly available buildings
        if (Visible)
        {
            UpdateBuildingsDisplay();
        }
    }

    private void OnCategorySelected(Enums.BuildingCategory category)
    {
        _selectedCategory = category;

        // Update button states
        foreach (var kvp in _categoryButtons)
        {
            kvp.Value.ButtonPressed = kvp.Key == _selectedCategory;
        }

        UpdateBuildingsDisplay();
    }

    private void OnBuildingSelected(BuildingResource building)
    {
        // Enter build mode with this building
        BuildingManager.Instance?.EnterBuildMode(building);
        Visible = false;
    }

    private void OnBuildingHovered(BuildingResource building)
    {
        UpdateInfo(building);
    }

    private void OnBuildModeChanged(bool enabled, BuildingResource building)
    {
        if (enabled)
        {
            Visible = false;
        }
    }

    public void Toggle()
    {
        Visible = !Visible;
        if (Visible)
        {
            UpdateBuildingsDisplay();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("build_menu"))
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
