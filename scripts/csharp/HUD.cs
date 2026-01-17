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
    private HBoxContainer _toolbar;

    // Notification system
    private VBoxContainer _notificationContainer;
    private AudioStreamPlayer _notificationSound;

    // Crafting queue display
    private VBoxContainer _craftQueueContainer;
    private Label _craftQueueTitle;

    // Signals to tell Main to toggle UIs
    [Signal]
    public delegate void ToggleInventoryEventHandler();
    [Signal]
    public delegate void ToggleBuildMenuEventHandler();
    [Signal]
    public delegate void ToggleCraftingEventHandler();
    [Signal]
    public delegate void ToggleResearchEventHandler();

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
        SetupToolbar();
        SetupNotifications();
        SetupCraftQueueDisplay();
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

    private void SetupToolbar()
    {
        // Create toolbar panel at top-right of screen
        var toolbarPanel = new PanelContainer
        {
            Name = "ToolbarPanel"
        };
        AddChild(toolbarPanel);

        // Position in top-right corner
        toolbarPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        toolbarPanel.Position = new Vector2(-230, 10);

        // Style the panel
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f),
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        toolbarPanel.AddThemeStyleboxOverride("panel", style);

        // Add margin
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        toolbarPanel.AddChild(margin);

        // Toolbar container
        _toolbar = new HBoxContainer();
        _toolbar.AddThemeConstantOverride("separation", 8);
        margin.AddChild(_toolbar);

        // Create toolbar buttons
        CreateToolbarButton("Inventory", "I", () => EmitSignal(SignalName.ToggleInventory));
        CreateToolbarButton("Build", "B", () => EmitSignal(SignalName.ToggleBuildMenu));
        CreateToolbarButton("Crafting", "C", () => EmitSignal(SignalName.ToggleCrafting));
        CreateToolbarButton("Research", "T", () => EmitSignal(SignalName.ToggleResearch));
    }

    private void CreateToolbarButton(string label, string hotkey, System.Action action)
    {
        var btn = new Button
        {
            Text = $"{label} ({hotkey})",
            CustomMinimumSize = new Vector2(0, 28)
        };

        // Style
        var btnStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.25f) };
        btnStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", btnStyle);

        var btnHoverStyle = new StyleBoxFlat { BgColor = new Color(0.3f, 0.3f, 0.35f) };
        btnHoverStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", btnHoverStyle);

        var btnPressedStyle = new StyleBoxFlat { BgColor = Constants.UiHighlight };
        btnPressedStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("pressed", btnPressedStyle);

        btn.AddThemeFontSizeOverride("font_size", 12);

        btn.Pressed += () => action();
        _toolbar.AddChild(btn);
    }

    private void SetupNotifications()
    {
        // Create notification container in top-right (below toolbar)
        _notificationContainer = new VBoxContainer
        {
            Name = "NotificationContainer"
        };
        _notificationContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _notificationContainer.Position = new Vector2(-320, 60);
        _notificationContainer.AddThemeConstantOverride("separation", 8);
        AddChild(_notificationContainer);

        // Create notification sound (simple beep)
        _notificationSound = new AudioStreamPlayer
        {
            Name = "NotificationSound",
            VolumeDb = -6.0f
        };
        AddChild(_notificationSound);

        // Generate a simple notification sound
        GenerateNotificationSound();
    }

    private void GenerateNotificationSound()
    {
        // Create a simple beep sound programmatically
        var generator = new AudioStreamGenerator
        {
            MixRate = 44100,
            BufferLength = 0.15f
        };
        _notificationSound.Stream = generator;
    }

    private void PlayNotificationSound()
    {
        // Play a simple UI sound - we'll use a generated tone
        if (_notificationSound != null)
        {
            _notificationSound.Play();
            // Generate audio data for a pleasant chime
            if (_notificationSound.GetStreamPlayback() is AudioStreamGeneratorPlayback playback)
            {
                float sampleRate = 44100;
                int numSamples = (int)(sampleRate * 0.15f);
                float freq1 = 880; // A5
                float freq2 = 1320; // E6

                for (int i = 0; i < numSamples; i++)
                {
                    float t = i / sampleRate;
                    float envelope = Mathf.Max(0, 1.0f - t / 0.15f);
                    float sample = Mathf.Sin(2 * Mathf.Pi * freq1 * t) * 0.3f +
                                   Mathf.Sin(2 * Mathf.Pi * freq2 * t) * 0.2f;
                    sample *= envelope * envelope;
                    playback.PushFrame(new Vector2(sample, sample));
                }
            }
        }
    }

    public void ShowNotification(string message, Color? color = null)
    {
        var notifColor = color ?? new Color(0.3f, 0.8f, 0.4f);

        // Create notification panel
        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f),
            BorderColor = notifColor
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(6);
        panel.AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var label = new Label
        {
            Text = message,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", notifColor);
        margin.AddChild(label);

        _notificationContainer.AddChild(panel);

        // Play sound
        PlayNotificationSound();

        // Auto-remove after 4 seconds with fade
        var tween = CreateTween();
        tween.TweenInterval(3.0);
        tween.TweenProperty(panel, "modulate:a", 0.0f, 1.0f);
        tween.TweenCallback(Callable.From(() => panel.QueueFree()));
    }

    private void SetupCraftQueueDisplay()
    {
        // Create craft queue panel in bottom-left
        var panel = new PanelContainer
        {
            Name = "CraftQueuePanel",
            Visible = false
        };
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        panel.Position = new Vector2(20, -120);
        AddChild(panel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f),
            BorderColor = Constants.UiBorder
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        panel.AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        _craftQueueContainer = new VBoxContainer();
        _craftQueueContainer.AddThemeConstantOverride("separation", 6);
        margin.AddChild(_craftQueueContainer);

        _craftQueueTitle = new Label
        {
            Text = "Crafting Queue",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _craftQueueTitle.AddThemeFontSizeOverride("font_size", 12);
        _craftQueueTitle.AddThemeColorOverride("font_color", Constants.UiTextDim);
        _craftQueueContainer.AddChild(_craftQueueTitle);

        UpdateCraftQueueDisplay();
    }

    private void UpdateCraftQueueDisplay()
    {
        if (_craftQueueContainer == null)
            return;

        // Clear existing items (except title)
        var children = _craftQueueContainer.GetChildren();
        for (int i = children.Count - 1; i > 0; i--)
        {
            children[i].QueueFree();
        }

        var queue = CraftingManager.Instance?.CraftQueue;
        var panel = _craftQueueContainer.GetParent()?.GetParent() as PanelContainer;

        if (queue == null || queue.Count == 0)
        {
            if (panel != null)
                panel.Visible = false;
            return;
        }

        if (panel != null)
            panel.Visible = true;

        // Group by recipe
        var grouped = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var recipe in queue)
        {
            if (!grouped.ContainsKey(recipe.Name))
                grouped[recipe.Name] = 0;
            grouped[recipe.Name]++;
        }

        // Show current crafting with progress
        var currentRecipe = queue[0];
        var currentRow = new HBoxContainer();
        currentRow.AddThemeConstantOverride("separation", 8);
        _craftQueueContainer.AddChild(currentRow);

        var currentLabel = new Label
        {
            Text = $"► {currentRecipe.Name}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        currentLabel.AddThemeFontSizeOverride("font_size", 13);
        currentLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.5f));
        currentRow.AddChild(currentLabel);

        // Progress bar for current item
        var progress = new ProgressBar
        {
            CustomMinimumSize = new Vector2(60, 12),
            MinValue = 0,
            MaxValue = 100,
            Value = (CraftingManager.Instance?.CraftProgress ?? 0) * 100,
            ShowPercentage = false
        };
        currentRow.AddChild(progress);

        // Show remaining items
        int shown = 0;
        foreach (var kvp in grouped)
        {
            if (shown >= 3) // Limit to 3 more rows
            {
                var moreLabel = new Label { Text = "..." };
                moreLabel.AddThemeFontSizeOverride("font_size", 11);
                moreLabel.AddThemeColorOverride("font_color", Constants.UiTextDim);
                _craftQueueContainer.AddChild(moreLabel);
                break;
            }

            // Skip first item if it's already shown
            if (shown == 0 && kvp.Key == currentRecipe.Name && kvp.Value == 1)
            {
                continue;
            }

            int displayCount = kvp.Key == currentRecipe.Name ? kvp.Value - 1 : kvp.Value;
            if (displayCount <= 0)
                continue;

            var row = new Label
            {
                Text = $"  {kvp.Key} x{displayCount}"
            };
            row.AddThemeFontSizeOverride("font_size", 11);
            row.AddThemeColorOverride("font_color", Constants.UiTextDim);
            _craftQueueContainer.AddChild(row);
            shown++;
        }
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

        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.ResearchCompleted += OnResearchCompleted;
        }
    }

    private void OnResearchCompleted(TechnologyResource tech)
    {
        ShowNotification($"Research Complete: {tech.Name}", new Color(0.4f, 0.7f, 1.0f));
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
        UpdateCraftQueueDisplay();
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
        UpdateCraftQueueDisplay();
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
