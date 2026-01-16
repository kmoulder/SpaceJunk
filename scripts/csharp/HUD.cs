using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// HUD - Main game heads-up display.
/// Shows hotbar, resource counters, and minimap.
/// </summary>
public partial class HUD : CanvasLayer
{
    [Export]
    public HBoxContainer HotbarContainer { get; set; }

    [Export]
    public VBoxContainer ResourcePanel { get; set; }

    [Export]
    public ProgressBar CraftingProgressBar { get; set; }

    [Export]
    public PanelContainer Tooltip { get; set; }

    [Export]
    public Label TooltipLabel { get; set; }

    private Array<Panel> _hotbarSlots = new();

    public override void _Ready()
    {
        // Fetch node references
        HotbarContainer ??= GetNodeOrNull<HBoxContainer>("HotbarPanel/HotbarContainer");
        ResourcePanel ??= GetNodeOrNull<VBoxContainer>("ResourcePanel");
        CraftingProgressBar ??= GetNodeOrNull<ProgressBar>("CraftingProgress");
        Tooltip ??= GetNodeOrNull<PanelContainer>("Tooltip");
        TooltipLabel ??= GetNodeOrNull<Label>("Tooltip/Label");

        GD.Print($"[HUD] HotbarContainer: {HotbarContainer != null}, ResourcePanel: {ResourcePanel != null}");

        SetupHotbar();
        SetupResourceDisplay();
        ConnectSignals();

        // Hide tooltip initially
        if (Tooltip != null)
            Tooltip.Visible = false;
        if (CraftingProgressBar != null)
            CraftingProgressBar.Visible = false;
    }

    private void SetupHotbar()
    {
        if (HotbarContainer == null)
            return;

        // Clear existing
        foreach (var child in HotbarContainer.GetChildren())
        {
            child.QueueFree();
        }

        _hotbarSlots.Clear();

        // Create hotbar slots
        for (int i = 0; i < Constants.HotbarSlots; i++)
        {
            var slot = CreateHotbarSlot(i);
            HotbarContainer.AddChild(slot);
            _hotbarSlots.Add(slot);
        }

        UpdateHotbar();
    }

    private Panel CreateHotbarSlot(int index)
    {
        var slot = new Panel
        {
            CustomMinimumSize = new Vector2(48, 48)
        };

        // Style
        var style = new StyleBoxFlat
        {
            BgColor = Constants.UiBackground,
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(4);
        slot.AddThemeStyleboxOverride("panel", style);

        // Icon
        var icon = new TextureRect
        {
            Name = "Icon",
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(40, 40),
            Position = new Vector2(4, 4)
        };
        slot.AddChild(icon);

        // Count label
        var count = new Label
        {
            Name = "Count",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Position = new Vector2(4, 28),
            Size = new Vector2(40, 16)
        };
        count.AddThemeFontSizeOverride("font_size", 12);
        slot.AddChild(count);

        // Key hint
        var key = new Label
        {
            Name = "KeyHint",
            Text = ((index + 1) % 10).ToString(),
            Position = new Vector2(2, 2)
        };
        key.AddThemeFontSizeOverride("font_size", 10);
        key.AddThemeColorOverride("font_color", Constants.UiTextDim);
        slot.AddChild(key);

        return slot;
    }

    private void SetupResourceDisplay()
    {
        UpdateResourceDisplay();
    }

    private void ConnectSignals()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.InventoryChanged += OnInventoryChanged;
            InventoryManager.Instance.HotbarChanged += UpdateHotbar;
            InventoryManager.Instance.SlotSelected += OnSlotSelected;
        }

        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.CraftProgressChanged += OnCraftProgress;
            CraftingManager.Instance.CraftCompleted += OnCraftCompleted;
            CraftingManager.Instance.QueueChanged += OnCraftQueueChanged;
        }
    }

    private void OnInventoryChanged()
    {
        UpdateHotbar();
        UpdateResourceDisplay();
    }

    private void UpdateHotbar()
    {
        if (InventoryManager.Instance == null)
            return;

        for (int i = 0; i < _hotbarSlots.Count; i++)
        {
            var slot = _hotbarSlots[i];
            var stack = i < InventoryManager.Instance.Hotbar.Count
                ? InventoryManager.Instance.Hotbar[i]
                : null;

            var icon = slot.GetNode<TextureRect>("Icon");
            var count = slot.GetNode<Label>("Count");

            if (stack != null && !stack.IsEmpty())
            {
                icon.Texture = SpriteGenerator.Instance?.GeneratePlate(stack.Item.SpriteColor);
                count.Text = stack.Count > 1 ? stack.Count.ToString() : "";
                count.Visible = true;
            }
            else
            {
                icon.Texture = null;
                count.Visible = false;
            }

            // Highlight selected slot
            var style = (StyleBoxFlat)slot.GetThemeStylebox("panel").Duplicate();
            style.BorderColor = i == InventoryManager.Instance.SelectedHotbarSlot
                ? Constants.UiHighlight
                : Constants.UiBorder;
            slot.AddThemeStyleboxOverride("panel", style);
        }
    }

    private void UpdateResourceDisplay()
    {
        if (ResourcePanel == null || InventoryManager.Instance == null)
            return;

        // Clear existing
        foreach (var child in ResourcePanel.GetChildren())
        {
            child.QueueFree();
        }

        // Show counts for key resources
        var keyItems = new[] { "iron_ore", "copper_ore", "iron_plate", "copper_plate" };

        foreach (string itemId in keyItems)
        {
            var item = InventoryManager.Instance.GetItem(itemId);
            if (item != null)
            {
                int count = InventoryManager.Instance.GetItemCount(item);
                var row = CreateResourceRow(item, count);
                ResourcePanel.AddChild(row);
            }
        }
    }

    private HBoxContainer CreateResourceRow(ItemResource item, int count)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        // Icon
        var icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(20, 20),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = SpriteGenerator.Instance?.GeneratePlate(item.SpriteColor)
        };
        row.AddChild(icon);

        // Count
        var label = new Label { Text = count.ToString() };
        label.AddThemeFontSizeOverride("font_size", 14);
        row.AddChild(label);

        return row;
    }

    private void OnSlotSelected(int index)
    {
        UpdateHotbar();
    }

    private void OnCraftProgress(RecipeResource recipe, float progress)
    {
        if (CraftingProgressBar != null)
        {
            CraftingProgressBar.Visible = true;
            CraftingProgressBar.Value = progress * 100;
        }
    }

    private void OnCraftCompleted(RecipeResource recipe)
    {
        if (CraftingProgressBar != null && CraftingManager.Instance != null)
        {
            CraftingProgressBar.Visible = CraftingManager.Instance.CraftQueue.Count > 0;
        }
    }

    private void OnCraftQueueChanged()
    {
        if (CraftingProgressBar != null && CraftingManager.Instance != null)
        {
            CraftingProgressBar.Visible = CraftingManager.Instance.CraftQueue.Count > 0;
        }
    }

    public void ShowTooltip(string text, Vector2 position)
    {
        if (TooltipLabel != null)
            TooltipLabel.Text = text;
        if (Tooltip != null)
        {
            Tooltip.Position = position + new Vector2(16, 16);
            Tooltip.Visible = true;
        }
    }

    public void HideTooltip()
    {
        if (Tooltip != null)
            Tooltip.Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        // Number keys for hotbar selection
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
            {
                InventoryManager.Instance?.SelectHotbar((int)keyEvent.Keycode - (int)Key.Key1);
            }
            else if (keyEvent.Keycode == Key.Key0)
            {
                InventoryManager.Instance?.SelectHotbar(9);
            }
        }
    }
}
