using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// BuildingUI - Interface for interacting with building inventories.
/// Opens when clicking on buildings with inventories (chests, furnaces).
/// Shows building slots and player inventory side-by-side for easy transfers.
/// </summary>
public partial class BuildingUI : CanvasLayer
{
    [Signal]
    public delegate void ClosedEventHandler();

    private Node2D _currentBuilding;
    public bool IsOpen { get; private set; } = false;

    // UI containers
    private PanelContainer _panel;
    private Label _titleLabel;
    private Button _closeButton;
    private VBoxContainer _buildingSlotsContainer;
    private HBoxContainer _progressContainer;
    private ProgressBar _progressBar;
    private VBoxContainer _playerInventoryContainer;
    private GridContainer _playerGrid;

    // Slot tracking
    private Array<Panel> _buildingSlots = new();
    private Array<Panel> _playerSlots = new();

    // Tooltip
    private PanelContainer _tooltip;
    private Label _tooltipLabel;
    private int _hoverSlotIndex = -1;
    private bool _hoverIsPlayerSlot = false;
    private float _hoverTime = 0.0f;
    private const float TooltipDelay = 0.4f;

    // Slot types for furnace
    private enum FurnaceSlotType { Fuel = 0, Input = 1, Output = 2 }

    // Assembler recipe selection
    private Button _recipeSelectButton;
    private PanelContainer _recipePanel;
    private VBoxContainer _recipeList;
    private bool _recipePanelOpen = false;

    public override void _Ready()
    {
        Layer = 18;
        CreateUI();
        ConnectSignals();
        HideUI();
    }

    private void CreateUI()
    {
        // Main panel
        _panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(440, 500)
        };
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

        // Center the panel
        _panel.AnchorsPreset = (int)Control.LayoutPreset.Center;
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);

        // Add margin
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        _panel.AddChild(margin);

        var contentVbox = new VBoxContainer();
        contentVbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(contentVbox);

        // Title bar
        var titleBar = new HBoxContainer();
        contentVbox.AddChild(titleBar);

        _titleLabel = new Label
        {
            Text = "Building",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.AddThemeColorOverride("font_color", Constants.UiText);
        titleBar.AddChild(_titleLabel);

        _closeButton = new Button
        {
            Text = "X",
            CustomMinimumSize = new Vector2(28, 28)
        };
        _closeButton.Pressed += HideUI;
        titleBar.AddChild(_closeButton);

        // Separator
        contentVbox.AddChild(new HSeparator());

        // Building slots section
        _buildingSlotsContainer = new VBoxContainer();
        _buildingSlotsContainer.AddThemeConstantOverride("separation", 8);
        contentVbox.AddChild(_buildingSlotsContainer);

        // Progress bar (for furnaces)
        _progressContainer = new HBoxContainer { Visible = false };
        contentVbox.AddChild(_progressContainer);

        var progressLabel = new Label { Text = "Progress:" };
        progressLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        _progressContainer.AddChild(progressLabel);

        _progressBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(200, 20),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MinValue = 0.0f,
            MaxValue = 1.0f,
            Value = 0.0f,
            ShowPercentage = false
        };
        _progressContainer.AddChild(_progressBar);

        // Separator
        contentVbox.AddChild(new HSeparator());

        // Player inventory section
        _playerInventoryContainer = new VBoxContainer();
        _playerInventoryContainer.AddThemeConstantOverride("separation", 4);
        contentVbox.AddChild(_playerInventoryContainer);

        var playerLabel = new Label { Text = "Your Inventory" };
        playerLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        _playerInventoryContainer.AddChild(playerLabel);

        _playerGrid = new GridContainer { Columns = 10 };
        _playerInventoryContainer.AddChild(_playerGrid);

        // Create player inventory slots
        CreatePlayerSlots();

        // Create tooltip
        CreateTooltip();
    }

    private void CreatePlayerSlots()
    {
        _playerSlots.Clear();
        foreach (var child in _playerGrid.GetChildren())
        {
            child.QueueFree();
        }

        for (int i = 0; i < Constants.PlayerInventorySlots; i++)
        {
            var slot = CreateSlot(i, true);
            _playerGrid.AddChild(slot);
            _playerSlots.Add(slot);
        }
    }

    private void CreateTooltip()
    {
        _tooltip = new PanelContainer
        {
            Visible = false,
            ZIndex = 100
        };
        AddChild(_tooltip);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f),
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(6);
        _tooltip.AddThemeStyleboxOverride("panel", style);

        _tooltipLabel = new Label();
        _tooltipLabel.AddThemeFontSizeOverride("font_size", 12);
        _tooltipLabel.AddThemeColorOverride("font_color", Constants.UiText);
        _tooltip.AddChild(_tooltipLabel);
    }

    private Panel CreateSlot(int index, bool isPlayerSlot)
    {
        var slot = new Panel
        {
            CustomMinimumSize = new Vector2(40, 40),
            MouseFilter = Control.MouseFilterEnum.Stop
        };

        // Style
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f),
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(4);
        slot.AddThemeStyleboxOverride("panel", style);

        // Icon
        var icon = new TextureRect
        {
            Name = "Icon",
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(32, 32),
            Position = new Vector2(4, 4),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        slot.AddChild(icon);

        // Count label
        var count = new Label
        {
            Name = "Count",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Position = new Vector2(4, 22),
            Size = new Vector2(32, 14),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        count.AddThemeFontSizeOverride("font_size", 11);
        count.AddThemeColorOverride("font_color", Constants.UiText);
        slot.AddChild(count);

        // Connect input
        if (isPlayerSlot)
        {
            slot.GuiInput += (evt) => OnPlayerSlotInput(evt, index);
            slot.MouseEntered += () => OnSlotMouseEntered(index, true);
            slot.MouseExited += OnSlotMouseExited;
        }
        else
        {
            slot.GuiInput += (evt) => OnBuildingSlotInput(evt, index);
            slot.MouseEntered += () => OnSlotMouseEntered(index, false);
            slot.MouseExited += OnSlotMouseExited;
        }

        return slot;
    }

    private void ConnectSignals()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.InventoryChanged += UpdatePlayerInventory;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameTick += OnGameTick;
        }
    }

    public override void _Process(double delta)
    {
        if (!IsOpen)
            return;

        // Handle tooltip delay
        if (_hoverSlotIndex >= 0)
        {
            _hoverTime += (float)delta;
            if (_hoverTime >= TooltipDelay && !_tooltip.Visible)
            {
                ShowTooltip();
            }
        }
    }

    private void OnSlotMouseEntered(int index, bool isPlayer)
    {
        _hoverSlotIndex = index;
        _hoverIsPlayerSlot = isPlayer;
        _hoverTime = 0.0f;
    }

    private void OnSlotMouseExited()
    {
        _hoverSlotIndex = -1;
        _hoverTime = 0.0f;
        HideTooltipPanel();
    }

    private void ShowTooltip()
    {
        ItemStack stack = null;

        if (_hoverIsPlayerSlot)
        {
            stack = InventoryManager.Instance?.GetSlot(_hoverSlotIndex);
        }
        else
        {
            // Get from building
            if (_currentBuilding is SmallChest chest)
            {
                stack = chest.GetSlot(_hoverSlotIndex);
            }
            else if (_currentBuilding is StoneFurnace furnace)
            {
                stack = (FurnaceSlotType)_hoverSlotIndex switch
                {
                    FurnaceSlotType.Fuel => furnace.FuelSlot,
                    FurnaceSlotType.Input => furnace.InputSlot,
                    FurnaceSlotType.Output => furnace.OutputSlot,
                    _ => null
                };
            }
            else if (_currentBuilding is Assembler assembler)
            {
                stack = assembler.GetSlot(_hoverSlotIndex);
            }
        }

        if (stack == null || stack.IsEmpty())
            return;

        // Build tooltip text
        string text = stack.Item.Name;
        if (stack.Count > 1)
            text += $" x{stack.Count}";

        _tooltipLabel.Text = text;
        _tooltip.Visible = true;

        // Position near mouse
        var mousePos = GetViewport().GetMousePosition();
        _tooltip.Position = mousePos + new Vector2(16, 16);
    }

    private void HideTooltipPanel()
    {
        _tooltip.Visible = false;
    }

    private void OnGameTick(int tick)
    {
        if (IsOpen && _currentBuilding != null)
        {
            UpdateBuildingDisplay();
        }
    }

    public void OpenForBuilding(Node2D building)
    {
        _currentBuilding = building;
        IsOpen = true;
        Visible = true;

        SetupBuildingUI();
        UpdateBuildingDisplay();
        UpdatePlayerInventory();
    }

    private void SetupBuildingUI()
    {
        // Clear existing building slots
        _buildingSlots.Clear();
        foreach (var child in _buildingSlotsContainer.GetChildren())
        {
            child.QueueFree();
        }

        if (_currentBuilding == null)
            return;

        // Get building name
        if (_currentBuilding is BuildingEntity entity)
        {
            var def = entity.GetDefinition();
            if (def != null)
                _titleLabel.Text = def.Name;
        }

        // Setup based on building type
        if (_currentBuilding is SmallChest)
        {
            SetupChestUI();
            _progressContainer.Visible = false;
        }
        else if (_currentBuilding is StoneFurnace)
        {
            SetupFurnaceUI();
            _progressContainer.Visible = true;
        }
        else if (_currentBuilding is Lab)
        {
            SetupLabUI();
            _progressContainer.Visible = false;
        }
        else if (_currentBuilding is Assembler)
        {
            SetupAssemblerUI();
            _progressContainer.Visible = true;
        }
        else
        {
            SetupGenericUI();
            _progressContainer.Visible = false;
        }
    }

    private void SetupChestUI()
    {
        var label = new Label { Text = "Storage (16 slots)" };
        label.AddThemeColorOverride("font_color", Constants.UiTextDim);
        _buildingSlotsContainer.AddChild(label);

        var grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        _buildingSlotsContainer.AddChild(grid);

        for (int i = 0; i < 16; i++)
        {
            var slot = CreateSlot(i, false);
            grid.AddChild(slot);
            _buildingSlots.Add(slot);
        }
    }

    private void SetupFurnaceUI()
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 20);
        _buildingSlotsContainer.AddChild(hbox);

        // Fuel slot
        var fuelVbox = new VBoxContainer();
        fuelVbox.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(fuelVbox);

        var fuelLabel = new Label
        {
            Text = "Fuel",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        fuelLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        fuelVbox.AddChild(fuelLabel);

        var fuelSlot = CreateSlot((int)FurnaceSlotType.Fuel, false);
        fuelSlot.CustomMinimumSize = new Vector2(48, 48);
        fuelVbox.AddChild(fuelSlot);
        _buildingSlots.Add(fuelSlot);

        // Input slot
        var inputVbox = new VBoxContainer();
        inputVbox.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(inputVbox);

        var inputLabel = new Label
        {
            Text = "Input",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        inputLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        inputVbox.AddChild(inputLabel);

        var inputSlot = CreateSlot((int)FurnaceSlotType.Input, false);
        inputSlot.CustomMinimumSize = new Vector2(48, 48);
        inputVbox.AddChild(inputSlot);
        _buildingSlots.Add(inputSlot);

        // Arrow indicator
        var arrow = new Label
        {
            Text = "→",
            VerticalAlignment = VerticalAlignment.Center
        };
        arrow.AddThemeFontSizeOverride("font_size", 24);
        arrow.AddThemeColorOverride("font_color", Constants.UiTextDim);
        hbox.AddChild(arrow);

        // Output slot
        var outputVbox = new VBoxContainer();
        outputVbox.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(outputVbox);

        var outputLabel = new Label
        {
            Text = "Output",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        outputLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        outputVbox.AddChild(outputLabel);

        var outputSlot = CreateSlot((int)FurnaceSlotType.Output, false);
        outputSlot.CustomMinimumSize = new Vector2(48, 48);
        outputVbox.AddChild(outputSlot);
        _buildingSlots.Add(outputSlot);
    }

    private void SetupLabUI()
    {
        var label = new Label { Text = "Science Packs" };
        label.AddThemeColorOverride("font_color", Constants.UiTextDim);
        _buildingSlotsContainer.AddChild(label);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        _buildingSlotsContainer.AddChild(hbox);

        // Create a slot for each science pack type
        var packTypes = new[] { ("automation_science", "Red"), ("logistic_science", "Green") };
        for (int i = 0; i < packTypes.Length; i++)
        {
            var (packId, packName) = packTypes[i];

            var packVbox = new VBoxContainer();
            packVbox.AddThemeConstantOverride("separation", 4);
            hbox.AddChild(packVbox);

            var packLabel = new Label
            {
                Text = packName,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            packLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
            packVbox.AddChild(packLabel);

            var slot = CreateSlot(i, false);
            slot.CustomMinimumSize = new Vector2(48, 48);
            slot.SetMeta("pack_id", packId);
            packVbox.AddChild(slot);
            _buildingSlots.Add(slot);
        }

        // Add info about current research
        var infoLabel = new Label
        {
            Name = "ResearchInfo",
            Text = "Press T to open Research menu",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        infoLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        infoLabel.AddThemeFontSizeOverride("font_size", 11);
        _buildingSlotsContainer.AddChild(infoLabel);
    }

    private void SetupAssemblerUI()
    {
        var assembler = _currentBuilding as Assembler;
        if (assembler == null)
            return;

        // Recipe selection section
        var recipeSection = new HBoxContainer();
        recipeSection.AddThemeConstantOverride("separation", 8);
        _buildingSlotsContainer.AddChild(recipeSection);

        var recipeLabel = new Label { Text = "Recipe:" };
        recipeLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        recipeSection.AddChild(recipeLabel);

        _recipeSelectButton = new Button
        {
            Text = assembler.CurrentRecipe?.Name ?? "Select Recipe",
            CustomMinimumSize = new Vector2(150, 28),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _recipeSelectButton.Pressed += OnRecipeSelectPressed;
        recipeSection.AddChild(_recipeSelectButton);

        // Input/Output layout
        var ioLayout = new HBoxContainer();
        ioLayout.AddThemeConstantOverride("separation", 16);
        _buildingSlotsContainer.AddChild(ioLayout);

        // Input slots (4 slots in 2x2 grid)
        var inputVbox = new VBoxContainer();
        inputVbox.AddThemeConstantOverride("separation", 4);
        ioLayout.AddChild(inputVbox);

        var inputLabel = new Label
        {
            Text = "Inputs",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        inputLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        inputVbox.AddChild(inputLabel);

        var inputGrid = new GridContainer { Columns = 2 };
        inputGrid.AddThemeConstantOverride("h_separation", 4);
        inputGrid.AddThemeConstantOverride("v_separation", 4);
        inputVbox.AddChild(inputGrid);

        for (int i = 0; i < 4; i++)
        {
            var slot = CreateSlot(i, false);
            slot.CustomMinimumSize = new Vector2(44, 44);
            inputGrid.AddChild(slot);
            _buildingSlots.Add(slot);
        }

        // Arrow indicator
        var arrow = new Label
        {
            Text = "→",
            VerticalAlignment = VerticalAlignment.Center
        };
        arrow.AddThemeFontSizeOverride("font_size", 24);
        arrow.AddThemeColorOverride("font_color", Constants.UiTextDim);
        ioLayout.AddChild(arrow);

        // Output slot
        var outputVbox = new VBoxContainer();
        outputVbox.AddThemeConstantOverride("separation", 4);
        ioLayout.AddChild(outputVbox);

        var outputLabel = new Label
        {
            Text = "Output",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        outputLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
        outputVbox.AddChild(outputLabel);

        var outputSlot = CreateSlot(4, false); // Index 4 = output
        outputSlot.CustomMinimumSize = new Vector2(48, 48);
        outputVbox.AddChild(outputSlot);
        _buildingSlots.Add(outputSlot);

        // Create recipe selection panel (hidden by default)
        CreateRecipeSelectionPanel();
    }

    private void CreateRecipeSelectionPanel()
    {
        _recipePanel = new PanelContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(250, 200)
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.18f, 0.98f),
            BorderColor = Constants.UiBorder
        };
        panelStyle.SetBorderWidthAll(2);
        panelStyle.SetCornerRadiusAll(6);
        _recipePanel.AddThemeStyleboxOverride("panel", panelStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        _recipePanel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        var titleHbox = new HBoxContainer();
        vbox.AddChild(titleHbox);

        var title = new Label
        {
            Text = "Select Recipe",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        title.AddThemeColorOverride("font_color", Constants.UiText);
        titleHbox.AddChild(title);

        var closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(24, 24) };
        closeBtn.Pressed += () => { _recipePanel.Visible = false; _recipePanelOpen = false; };
        titleHbox.AddChild(closeBtn);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 150),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(scroll);

        _recipeList = new VBoxContainer();
        _recipeList.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_recipeList);

        _buildingSlotsContainer.AddChild(_recipePanel);
    }

    private void OnRecipeSelectPressed()
    {
        if (_recipePanelOpen)
        {
            _recipePanel.Visible = false;
            _recipePanelOpen = false;
            return;
        }

        // Populate recipe list
        foreach (var child in _recipeList.GetChildren())
        {
            child.QueueFree();
        }

        // Get assembler recipes
        var recipes = CraftingManager.Instance?.GetRecipesForBuilding(Enums.CraftingType.Assembler);
        if (recipes == null || recipes.Count == 0)
        {
            // Also include Player recipes that can be automated
            recipes = CraftingManager.Instance?.GetRecipesForBuilding(Enums.CraftingType.Player);
        }

        // Add "Clear" option
        var clearBtn = new Button
        {
            Text = "[ Clear Recipe ]",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        clearBtn.Pressed += () => SelectRecipe(null);
        _recipeList.AddChild(clearBtn);

        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                // Check if recipe is unlocked
                bool unlocked = string.IsNullOrEmpty(recipe.RequiredTechnology) ||
                    ResearchManager.Instance?.IsTechnologyUnlocked(recipe.RequiredTechnology) == true;

                var btn = new Button
                {
                    Text = unlocked ? recipe.Name : $"[Locked] {recipe.Name}",
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    Disabled = !unlocked
                };

                if (unlocked)
                {
                    var capturedRecipe = recipe;
                    btn.Pressed += () => SelectRecipe(capturedRecipe);
                }

                _recipeList.AddChild(btn);
            }
        }

        _recipePanel.Visible = true;
        _recipePanelOpen = true;
    }

    private void SelectRecipe(RecipeResource recipe)
    {
        if (_currentBuilding is Assembler assembler)
        {
            if (recipe == null)
            {
                assembler.ClearRecipe();
                _recipeSelectButton.Text = "Select Recipe";
            }
            else
            {
                assembler.SetRecipe(recipe);
                _recipeSelectButton.Text = recipe.Name;
            }
        }

        _recipePanel.Visible = false;
        _recipePanelOpen = false;
        UpdateBuildingDisplay();
    }

    private void SetupGenericUI()
    {
        if (_currentBuilding is not BuildingEntity entity)
            return;

        var label = new Label { Text = "Contents" };
        label.AddThemeColorOverride("font_color", Constants.UiTextDim);
        _buildingSlotsContainer.AddChild(label);

        var grid = new GridContainer { Columns = 4 };
        _buildingSlotsContainer.AddChild(grid);

        int slotCount = 16; // Default
        var def = entity.GetDefinition();
        if (def != null && def.StorageSlots > 0)
            slotCount = def.StorageSlots;

        for (int i = 0; i < slotCount; i++)
        {
            var slot = CreateSlot(i, false);
            grid.AddChild(slot);
            _buildingSlots.Add(slot);
        }
    }

    private void UpdateBuildingDisplay()
    {
        if (_currentBuilding == null)
            return;

        if (_currentBuilding is SmallChest chest)
            UpdateChestDisplay(chest);
        else if (_currentBuilding is StoneFurnace furnace)
            UpdateFurnaceDisplay(furnace);
        else if (_currentBuilding is Lab lab)
            UpdateLabDisplay(lab);
        else if (_currentBuilding is Assembler assembler)
            UpdateAssemblerDisplay(assembler);
        else if (_currentBuilding is BuildingEntity entity)
            UpdateGenericDisplay(entity);
    }

    private void UpdateChestDisplay(SmallChest chest)
    {
        for (int i = 0; i < _buildingSlots.Count; i++)
        {
            var slot = _buildingSlots[i];
            var stack = chest.GetSlot(i);
            UpdateSlotDisplay(slot, stack);
        }
    }

    private void UpdateFurnaceDisplay(StoneFurnace furnace)
    {
        // Update fuel slot (index 0)
        if (_buildingSlots.Count > (int)FurnaceSlotType.Fuel)
            UpdateSlotDisplay(_buildingSlots[(int)FurnaceSlotType.Fuel], furnace.FuelSlot);

        // Update input slot (index 1)
        if (_buildingSlots.Count > (int)FurnaceSlotType.Input)
            UpdateSlotDisplay(_buildingSlots[(int)FurnaceSlotType.Input], furnace.InputSlot);

        // Update output slot (index 2)
        if (_buildingSlots.Count > (int)FurnaceSlotType.Output)
            UpdateSlotDisplay(_buildingSlots[(int)FurnaceSlotType.Output], furnace.OutputSlot);

        // Update progress bar
        _progressBar.Value = furnace.GetSmeltingProgress();
    }

    private void UpdateLabDisplay(Lab lab)
    {
        var packTypes = new[] { "automation_science", "logistic_science" };
        for (int i = 0; i < _buildingSlots.Count && i < packTypes.Length; i++)
        {
            var slot = _buildingSlots[i];
            var packId = packTypes[i];

            if (lab.ScienceSlots.TryGetValue(packId, out var stack))
            {
                UpdateSlotDisplay(slot, stack);
            }
            else
            {
                UpdateSlotDisplay(slot, null);
            }
        }

        // Update research info label
        var infoLabel = _buildingSlotsContainer.GetNodeOrNull<Label>("ResearchInfo");
        if (infoLabel != null)
        {
            var currentResearch = ResearchManager.Instance?.CurrentResearch;
            if (currentResearch != null)
            {
                float progress = ResearchManager.Instance?.GetResearchProgress() ?? 0;
                infoLabel.Text = $"Researching: {currentResearch.Name} ({progress * 100:F0}%)";
            }
            else
            {
                infoLabel.Text = "No research active. Press T to start.";
            }
        }
    }

    private void UpdateAssemblerDisplay(Assembler assembler)
    {
        // Update input slots (indices 0-3)
        for (int i = 0; i < 4 && i < _buildingSlots.Count; i++)
        {
            UpdateSlotDisplay(_buildingSlots[i], assembler.InputSlots[i]);
        }

        // Update output slot (index 4)
        if (_buildingSlots.Count > 4)
        {
            UpdateSlotDisplay(_buildingSlots[4], assembler.OutputSlot);
        }

        // Update progress bar
        _progressBar.Value = assembler.GetCraftingProgress();

        // Update recipe button text
        if (_recipeSelectButton != null)
        {
            _recipeSelectButton.Text = assembler.CurrentRecipe?.Name ?? "Select Recipe";
        }
    }

    private void UpdateGenericDisplay(BuildingEntity entity)
    {
        for (int i = 0; i < _buildingSlots.Count; i++)
        {
            var slot = _buildingSlots[i];
            var stack = entity.GetSlot(i);
            UpdateSlotDisplay(slot, stack);
        }
    }

    private void UpdateSlotDisplay(Panel slot, ItemStack stack)
    {
        var icon = slot.GetNode<TextureRect>("Icon");
        var count = slot.GetNode<Label>("Count");

        if (stack != null && !stack.IsEmpty())
        {
            icon.Texture = GetItemTexture(stack.Item);
            count.Text = stack.Count > 1 ? stack.Count.ToString() : "";
            count.Visible = stack.Count > 1;
        }
        else
        {
            icon.Texture = null;
            count.Text = "";
            count.Visible = false;
        }
    }

    private void UpdatePlayerInventory()
    {
        for (int i = 0; i < _playerSlots.Count; i++)
        {
            var slot = _playerSlots[i];
            var stack = InventoryManager.Instance?.GetSlot(i);
            UpdateSlotDisplay(slot, stack);
        }
    }

    private Texture2D GetItemTexture(ItemResource item)
    {
        return item.Category switch
        {
            Enums.ItemCategory.RawMaterial => SpriteGenerator.Instance?.GenerateOre(item.SpriteColor),
            Enums.ItemCategory.Processed => SpriteGenerator.Instance?.GeneratePlate(item.SpriteColor),
            Enums.ItemCategory.Component when item.Id.Contains("gear") => SpriteGenerator.Instance?.GenerateGear(item.SpriteColor),
            Enums.ItemCategory.Component when item.Id.Contains("cable") => SpriteGenerator.Instance?.GenerateCable(item.SpriteColor),
            Enums.ItemCategory.Component when item.Id.Contains("circuit") => SpriteGenerator.Instance?.GenerateCircuit(item.SpriteColor),
            Enums.ItemCategory.Science => SpriteGenerator.Instance?.GenerateCircuit(item.SpriteColor, 2),
            _ => SpriteGenerator.Instance?.GeneratePlate(item.SpriteColor)
        };
    }

    private void OnPlayerSlotInput(InputEvent @event, int index)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                bool transferStack = mouseEvent.ShiftPressed;
                TransferFromPlayer(index, transferStack);
            }
        }
    }

    private void OnBuildingSlotInput(InputEvent @event, int index)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                bool transferStack = mouseEvent.ShiftPressed;
                TransferFromBuilding(index, transferStack);
            }
        }
    }

    private void TransferFromPlayer(int slotIndex, bool transferStack = false)
    {
        var stack = InventoryManager.Instance?.GetSlot(slotIndex);
        if (stack == null || stack.IsEmpty())
            return;

        if (_currentBuilding == null)
            return;

        int amountToTransfer = transferStack ? stack.Count : 1;

        // Try to transfer to building
        if (_currentBuilding is StoneFurnace furnace)
        {
            TransferToFurnace(furnace, stack, slotIndex, amountToTransfer);
        }
        else if (_currentBuilding is Lab lab)
        {
            TransferToLab(lab, stack, slotIndex, amountToTransfer);
        }
        else if (_currentBuilding is BuildingEntity entity)
        {
            if (entity.CanAcceptItem(stack.Item))
            {
                int transferred = 0;
                for (int i = 0; i < amountToTransfer; i++)
                {
                    if (entity.InsertItem(stack.Item, 1))
                    {
                        transferred++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (transferred > 0)
                {
                    InventoryManager.Instance?.RemoveItemAt(slotIndex, transferred);
                    UpdateBuildingDisplay();
                    UpdatePlayerInventory();
                }
            }
        }
    }

    private void TransferToFurnace(StoneFurnace furnace, ItemStack stack, int playerSlot, int amount)
    {
        // Check if it's fuel or ore
        if (furnace.CanAcceptItem(stack.Item))
        {
            int transferred = 0;
            for (int i = 0; i < amount; i++)
            {
                if (furnace.InsertItem(stack.Item, 1))
                {
                    transferred++;
                }
                else
                {
                    break;
                }
            }
            if (transferred > 0)
            {
                InventoryManager.Instance?.RemoveItemAt(playerSlot, transferred);
                UpdateBuildingDisplay();
                UpdatePlayerInventory();
            }
        }
    }

    private void TransferToLab(Lab lab, ItemStack stack, int playerSlot, int amount)
    {
        // Labs only accept science packs
        if (stack.Item.Category != Enums.ItemCategory.Science)
            return;

        // Check if this pack type is valid for the lab
        if (!lab.ScienceSlots.ContainsKey(stack.Item.Id))
            return;

        var labStack = lab.ScienceSlots[stack.Item.Id];
        int transferred = 0;
        for (int i = 0; i < amount; i++)
        {
            if (labStack.Count < 64) // Max stack size
            {
                if (labStack.Item == null)
                    labStack.Item = stack.Item;
                labStack.Add(1);
                transferred++;
            }
            else
            {
                break;
            }
        }
        if (transferred > 0)
        {
            InventoryManager.Instance?.RemoveItemAt(playerSlot, transferred);
            UpdateBuildingDisplay();
            UpdatePlayerInventory();
        }
    }

    private void TransferFromBuilding(int slotIndex, bool transferStack = false)
    {
        if (_currentBuilding == null)
            return;

        ItemResource item = null;
        int availableCount = 0;
        ItemStack sourceStack = null;

        if (_currentBuilding is SmallChest chest)
        {
            sourceStack = chest.GetSlot(slotIndex);
            if (sourceStack != null && !sourceStack.IsEmpty())
            {
                item = sourceStack.Item;
                availableCount = sourceStack.Count;
            }
        }
        else if (_currentBuilding is StoneFurnace furnace)
        {
            sourceStack = (FurnaceSlotType)slotIndex switch
            {
                FurnaceSlotType.Fuel => furnace.FuelSlot,
                FurnaceSlotType.Input => furnace.InputSlot,
                FurnaceSlotType.Output => furnace.OutputSlot,
                _ => null
            };

            if (sourceStack != null && !sourceStack.IsEmpty())
            {
                item = sourceStack.Item;
                availableCount = sourceStack.Count;
            }
        }
        else if (_currentBuilding is Lab lab)
        {
            var packTypes = new[] { "automation_science", "logistic_science" };
            if (slotIndex < packTypes.Length)
            {
                var packId = packTypes[slotIndex];
                if (lab.ScienceSlots.TryGetValue(packId, out var stack) && !stack.IsEmpty())
                {
                    sourceStack = stack;
                    item = stack.Item;
                    availableCount = stack.Count;
                }
            }
        }
        else if (_currentBuilding is Assembler assembler)
        {
            sourceStack = assembler.GetSlot(slotIndex);
            if (sourceStack != null && !sourceStack.IsEmpty())
            {
                item = sourceStack.Item;
                availableCount = sourceStack.Count;
            }
        }

        if (item == null || availableCount == 0)
            return;

        int amountToTransfer = transferStack ? availableCount : 1;
        int transferred = 0;

        // Add to player inventory
        for (int i = 0; i < amountToTransfer; i++)
        {
            int overflow = InventoryManager.Instance?.AddItem(item, 1) ?? 1;
            if (overflow == 0)
            {
                transferred++;
            }
            else
            {
                break; // Inventory full
            }
        }

        // Remove transferred amount from building
        if (transferred > 0 && sourceStack != null)
        {
            sourceStack.Remove(transferred);
            if (sourceStack.Count <= 0)
                sourceStack.Item = null;

            UpdateBuildingDisplay();
            UpdatePlayerInventory();
        }
    }

    public void HideUI()
    {
        Visible = false;
        IsOpen = false;
        _currentBuilding = null;
        _hoverSlotIndex = -1;
        HideTooltipPanel();
        EmitSignal(SignalName.Closed);
    }

    public void ToggleUI()
    {
        if (IsOpen)
            HideUI();
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsOpen)
            return;

        if (@event.IsActionPressed("cancel"))
        {
            HideUI();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("inventory"))
        {
            // Close building UI when pressing I (inventory key)
            HideUI();
            GetViewport().SetInputAsHandled();
        }
    }
}
