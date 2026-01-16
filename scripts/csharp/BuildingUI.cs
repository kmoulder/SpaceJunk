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
                TransferFromPlayer(index);
            }
        }
    }

    private void OnBuildingSlotInput(InputEvent @event, int index)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                TransferFromBuilding(index);
            }
        }
    }

    private void TransferFromPlayer(int slotIndex)
    {
        var stack = InventoryManager.Instance?.GetSlot(slotIndex);
        if (stack == null || stack.IsEmpty())
            return;

        if (_currentBuilding == null)
            return;

        // Try to transfer to building
        if (_currentBuilding is StoneFurnace furnace)
        {
            TransferToFurnace(furnace, stack, slotIndex);
        }
        else if (_currentBuilding is BuildingEntity entity)
        {
            if (entity.CanAcceptItem(stack.Item))
            {
                if (entity.InsertItem(stack.Item, 1))
                {
                    InventoryManager.Instance?.RemoveItemAt(slotIndex, 1);
                    UpdateBuildingDisplay();
                    UpdatePlayerInventory();
                }
            }
        }
    }

    private void TransferToFurnace(StoneFurnace furnace, ItemStack stack, int playerSlot)
    {
        // Check if it's fuel or ore
        if (furnace.CanAcceptItem(stack.Item))
        {
            if (furnace.InsertItem(stack.Item, 1))
            {
                InventoryManager.Instance?.RemoveItemAt(playerSlot, 1);
                UpdateBuildingDisplay();
                UpdatePlayerInventory();
            }
        }
    }

    private void TransferFromBuilding(int slotIndex)
    {
        if (_currentBuilding == null)
            return;

        ItemResource item = null;

        if (_currentBuilding is SmallChest chest)
        {
            var stack = chest.GetSlot(slotIndex);
            if (stack != null && !stack.IsEmpty())
            {
                item = stack.Item;
                stack.Remove(1);
                if (stack.Count <= 0)
                    stack.Item = null;
            }
        }
        else if (_currentBuilding is StoneFurnace furnace)
        {
            ItemStack stack = (FurnaceSlotType)slotIndex switch
            {
                FurnaceSlotType.Fuel => furnace.FuelSlot,
                FurnaceSlotType.Input => furnace.InputSlot,
                FurnaceSlotType.Output => furnace.OutputSlot,
                _ => null
            };

            if (stack != null && !stack.IsEmpty())
            {
                item = stack.Item;
                stack.Remove(1);
                if (stack.Count <= 0)
                    stack.Item = null;
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
                    item = stack.Item;
                    stack.Remove(1);
                    if (stack.Count <= 0)
                        stack.Item = null;
                }
            }
        }

        // Add to player inventory
        if (item != null)
        {
            int overflow = InventoryManager.Instance?.AddItem(item, 1) ?? 1;
            if (overflow == 0)
            {
                UpdateBuildingDisplay();
                UpdatePlayerInventory();
            }
            else
            {
                // Failed to add to inventory, put it back
                if (_currentBuilding is SmallChest c)
                    c.InsertItem(item, 1);
                else if (_currentBuilding is StoneFurnace f)
                    f.InsertItem(item, 1);
            }
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
