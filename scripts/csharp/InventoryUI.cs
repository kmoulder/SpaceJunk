using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// InventoryUI - Player inventory panel.
/// Grid-based inventory display with drag-and-drop support.
/// </summary>
public partial class InventoryUI : CanvasLayer
{
    [Signal]
    public delegate void ClosedEventHandler();

    [Export]
    public PanelContainer Panel { get; set; }

    [Export]
    public GridContainer Grid { get; set; }

    [Export]
    public Label TitleLabel { get; set; }

    [Export]
    public Button CloseButton { get; set; }

    private Array<Panel> _inventorySlots = new();
    private int _draggingFrom = -1;
    public bool IsOpen { get; private set; } = false;

    // Tooltip
    private PanelContainer _tooltip;
    private Label _tooltipLabel;
    private int _hoverSlotIndex = -1;
    private float _hoverTime = 0.0f;
    private const float TooltipDelay = 0.4f;

    public override void _Ready()
    {
        // Fetch node references
        Panel ??= GetNodeOrNull<PanelContainer>("Panel");
        Grid ??= GetNodeOrNull<GridContainer>("Panel/VBox/ScrollContainer/Grid");
        TitleLabel ??= GetNodeOrNull<Label>("Panel/VBox/TitleBar/Title");
        CloseButton ??= GetNodeOrNull<Button>("Panel/VBox/TitleBar/CloseButton");

        GD.Print($"[InventoryUI] Panel: {Panel != null}, Grid: {Grid != null}");

        SetupPanel();
        SetupInventoryGrid();
        CreateTooltip();
        ConnectSignals();
        HideInventory();
    }

    private void SetupPanel()
    {
        if (Panel == null)
            return;

        // Style the main panel
        var style = new StyleBoxFlat
        {
            BgColor = Constants.UiBackground,
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        Panel.AddThemeStyleboxOverride("panel", style);

        if (CloseButton != null)
            CloseButton.Pressed += HideInventory;
    }

    private void SetupInventoryGrid()
    {
        if (Grid == null)
            return;

        // Clear existing
        foreach (var child in Grid.GetChildren())
        {
            child.QueueFree();
        }

        _inventorySlots.Clear();

        // Configure grid
        Grid.Columns = 10;

        // Create inventory slots
        for (int i = 0; i < Constants.PlayerInventorySlots; i++)
        {
            var slot = CreateInventorySlot(i);
            Grid.AddChild(slot);
            _inventorySlots.Add(slot);
        }

        UpdateInventory();
    }

    private Panel CreateInventorySlot(int index)
    {
        var slot = new Panel
        {
            CustomMinimumSize = new Vector2(48, 48),
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
            CustomMinimumSize = new Vector2(40, 40),
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
            Position = new Vector2(4, 28),
            Size = new Vector2(40, 16),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        count.AddThemeFontSizeOverride("font_size", 12);
        slot.AddChild(count);

        // Connect input
        slot.GuiInput += (evt) => OnSlotInput(evt, index);
        slot.MouseEntered += () => OnSlotHover(index);
        slot.MouseExited += OnSlotUnhover;

        return slot;
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

    private void ConnectSignals()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.InventoryChanged += UpdateInventory;
        }
    }

    private void UpdateInventory()
    {
        if (InventoryManager.Instance == null)
            return;

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            var slot = _inventorySlots[i];
            var stack = InventoryManager.Instance.GetSlot(i);

            var icon = slot.GetNode<TextureRect>("Icon");
            var count = slot.GetNode<Label>("Count");

            if (stack != null && !stack.IsEmpty())
            {
                icon.Texture = GetItemTexture(stack.Item);
                count.Text = stack.Count > 1 ? stack.Count.ToString() : "";
                count.Visible = true;
            }
            else
            {
                icon.Texture = null;
                count.Visible = false;
            }
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
            _ => SpriteGenerator.Instance?.GeneratePlate(item.SpriteColor)
        };
    }

    private void OnSlotInput(InputEvent @event, int index)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                HandleLeftClick(index, mouseEvent.ShiftPressed);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right)
            {
                HandleRightClick(index);
            }
        }
    }

    private void HandleLeftClick(int index, bool shift)
    {
        if (_draggingFrom == -1)
        {
            // Start dragging if slot has items
            var stack = InventoryManager.Instance?.GetSlot(index);
            if (stack != null && !stack.IsEmpty())
            {
                _draggingFrom = index;
                HighlightSlot(index, true);
            }
        }
        else
        {
            // Complete swap/merge
            if (index != _draggingFrom)
            {
                InventoryManager.Instance?.SwapSlots(_draggingFrom, index);
            }
            HighlightSlot(_draggingFrom, false);
            _draggingFrom = -1;
        }
    }

    private void HandleRightClick(int index)
    {
        // Right-click to split stack (take half)
        var stack = InventoryManager.Instance?.GetSlot(index);
        if (stack != null && stack.Count > 1)
        {
            int half = stack.Count / 2;
            // Find empty slot to put half in
            for (int i = 0; i < Constants.PlayerInventorySlots; i++)
            {
                var other = InventoryManager.Instance?.GetSlot(i);
                if (other != null && other.IsEmpty())
                {
                    var splitStack = stack.Split(half);
                    if (splitStack != null)
                    {
                        other.Item = splitStack.Item;
                        other.Count = splitStack.Count;
                        InventoryManager.Instance?.EmitSignal(InventoryManager.SignalName.InventoryChanged);
                    }
                    break;
                }
            }
        }
    }

    private void HighlightSlot(int index, bool highlight)
    {
        if (index < 0 || index >= _inventorySlots.Count)
            return;

        var slot = _inventorySlots[index];
        var style = (StyleBoxFlat)slot.GetThemeStylebox("panel").Duplicate();
        style.BorderColor = highlight ? Constants.UiHighlight : Constants.UiBorder;
        int borderWidth = highlight ? 2 : 1;
        style.BorderWidthBottom = borderWidth;
        style.BorderWidthTop = borderWidth;
        style.BorderWidthLeft = borderWidth;
        style.BorderWidthRight = borderWidth;
        slot.AddThemeStyleboxOverride("panel", style);
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

    private void OnSlotHover(int index)
    {
        _hoverSlotIndex = index;
        _hoverTime = 0.0f;
    }

    private void OnSlotUnhover()
    {
        _hoverSlotIndex = -1;
        _hoverTime = 0.0f;
        HideTooltipPanel();
    }

    private void ShowTooltip()
    {
        var stack = InventoryManager.Instance?.GetSlot(_hoverSlotIndex);
        if (stack == null || stack.IsEmpty())
            return;

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

    public void ShowInventory()
    {
        Visible = true;
        IsOpen = true;
        UpdateInventory();
    }

    public void HideInventory()
    {
        Visible = false;
        IsOpen = false;
        _draggingFrom = -1;
        _hoverSlotIndex = -1;
        HideTooltipPanel();
        EmitSignal(SignalName.Closed);
    }

    public void ToggleInventory()
    {
        if (IsOpen)
            HideInventory();
        else
            ShowInventory();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("inventory"))
        {
            ToggleInventory();
        }
        else if (@event.IsActionPressed("cancel") && IsOpen)
        {
            HideInventory();
        }
    }
}
