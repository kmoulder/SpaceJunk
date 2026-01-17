using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// ResearchUI - UI panel for viewing and selecting technologies to research.
/// Shows available, locked, and completed technologies with their costs.
/// Toggle with 'T' key.
/// </summary>
public partial class ResearchUI : CanvasLayer
{
    /// <summary>
    /// Main panel container
    /// </summary>
    private PanelContainer _panel;

    /// <summary>
    /// Container for technology entries
    /// </summary>
    private VBoxContainer _techListContainer;

    /// <summary>
    /// Current research info panel
    /// </summary>
    private PanelContainer _currentResearchPanel;

    /// <summary>
    /// Progress bar for current research
    /// </summary>
    private ProgressBar _progressBar;

    /// <summary>
    /// Label for current research name
    /// </summary>
    private Label _currentResearchLabel;

    /// <summary>
    /// Cancel button for current research
    /// </summary>
    private Button _cancelButton;

    /// <summary>
    /// Technology buttons mapped by tech ID
    /// </summary>
    private readonly Dictionary<string, Button> _techButtons = new();

    /// <summary>
    /// Dragging state
    /// </summary>
    private bool _isDragging = false;
    private Vector2 _dragOffset = Vector2.Zero;

    public override void _Ready()
    {
        CreateUIStructure();
        ConnectSignals();
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

        // Position panel (center-right of screen)
        _panel.Position = new Vector2(650, 40);
        _panel.CustomMinimumSize = new Vector2(400, 560);

        // Main margin container
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        _panel.AddChild(margin);

        // Main VBox
        var vbox = new VBoxContainer { Name = "VBoxContainer" };
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        // Title bar with close button
        var titleBar = new HBoxContainer();
        vbox.AddChild(titleBar);

        var title = new Label
        {
            Text = "Research",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        title.AddThemeFontSizeOverride("font_size", 20);
        titleBar.AddChild(title);

        var closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(28, 28) };
        var closeBtnStyle = new StyleBoxFlat { BgColor = new Color(0.4f, 0.2f, 0.2f) };
        closeBtnStyle.SetCornerRadiusAll(4);
        closeBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
        var closeBtnHover = new StyleBoxFlat { BgColor = new Color(0.6f, 0.3f, 0.3f) };
        closeBtnHover.SetCornerRadiusAll(4);
        closeBtn.AddThemeStyleboxOverride("hover", closeBtnHover);
        closeBtn.Pressed += () => Visible = false;
        titleBar.AddChild(closeBtn);

        // Make title bar draggable
        titleBar.GuiInput += OnTitleBarInput;

        // Hint label
        var hintLabel = new Label
        {
            Text = "Press T to toggle",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        hintLabel.AddThemeFontSizeOverride("font_size", 10);
        hintLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
        vbox.AddChild(hintLabel);

        // Separator
        vbox.AddChild(new HSeparator());

        // Current research panel
        CreateCurrentResearchPanel(vbox);

        // Separator
        vbox.AddChild(new HSeparator());

        // Available technologies label
        var availableLabel = new Label
        {
            Text = "Technologies",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        availableLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(availableLabel);

        // Scroll container for tech list
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 320),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(scroll);

        _techListContainer = new VBoxContainer { Name = "TechList" };
        _techListContainer.AddThemeConstantOverride("separation", 10);
        _techListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_techListContainer);

        // Initial population
        RefreshTechList();
        UpdateCurrentResearchDisplay();
    }

    private void CreateCurrentResearchPanel(VBoxContainer parent)
    {
        _currentResearchPanel = new PanelContainer
        {
            Name = "CurrentResearchPanel",
            CustomMinimumSize = new Vector2(0, 90)
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.14f, 0.18f, 1.0f)
        };
        panelStyle.SetCornerRadiusAll(6);
        panelStyle.SetBorderWidthAll(1);
        panelStyle.BorderColor = new Color(0.3f, 0.35f, 0.4f);
        _currentResearchPanel.AddThemeStyleboxOverride("panel", panelStyle);
        parent.AddChild(_currentResearchPanel);

        var panelMargin = new MarginContainer();
        panelMargin.AddThemeConstantOverride("margin_left", 12);
        panelMargin.AddThemeConstantOverride("margin_right", 12);
        panelMargin.AddThemeConstantOverride("margin_top", 10);
        panelMargin.AddThemeConstantOverride("margin_bottom", 10);
        _currentResearchPanel.AddChild(panelMargin);

        var panelVbox = new VBoxContainer();
        panelVbox.AddThemeConstantOverride("separation", 8);
        panelMargin.AddChild(panelVbox);

        // Current research label
        _currentResearchLabel = new Label
        {
            Text = "No research in progress",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _currentResearchLabel.AddThemeFontSizeOverride("font_size", 14);
        panelVbox.AddChild(_currentResearchLabel);

        // Progress bar with custom styling
        _progressBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 22),
            Value = 0,
            ShowPercentage = true
        };

        var progressBgStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.12f) };
        progressBgStyle.SetCornerRadiusAll(4);
        _progressBar.AddThemeStyleboxOverride("background", progressBgStyle);

        var progressFillStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.5f, 0.7f) };
        progressFillStyle.SetCornerRadiusAll(4);
        _progressBar.AddThemeStyleboxOverride("fill", progressFillStyle);

        panelVbox.AddChild(_progressBar);

        // Cancel button
        _cancelButton = new Button
        {
            Text = "Cancel Research",
            Visible = false,
            CustomMinimumSize = new Vector2(0, 26)
        };
        _cancelButton.Pressed += OnCancelPressed;

        var cancelStyle = new StyleBoxFlat { BgColor = new Color(0.45f, 0.2f, 0.2f) };
        cancelStyle.SetCornerRadiusAll(4);
        _cancelButton.AddThemeStyleboxOverride("normal", cancelStyle);

        var cancelHoverStyle = new StyleBoxFlat { BgColor = new Color(0.55f, 0.25f, 0.25f) };
        cancelHoverStyle.SetCornerRadiusAll(4);
        _cancelButton.AddThemeStyleboxOverride("hover", cancelHoverStyle);

        panelVbox.AddChild(_cancelButton);
    }

    private void RefreshTechList()
    {
        if (_techListContainer == null)
            return;

        // Clear existing
        foreach (var child in _techListContainer.GetChildren())
        {
            child.QueueFree();
        }
        _techButtons.Clear();

        var allTechs = ResearchManager.Instance?.GetAllTechnologies();
        if (allTechs == null)
            return;

        // Sort: available first, then locked, then completed
        var sortedTechs = new Array<TechnologyResource>();
        var available = new Array<TechnologyResource>();
        var locked = new Array<TechnologyResource>();
        var completed = new Array<TechnologyResource>();

        foreach (var tech in allTechs)
        {
            if (ResearchManager.Instance.IsTechnologyUnlocked(tech.Id))
                completed.Add(tech);
            else if (ResearchManager.Instance.CanResearch(tech))
                available.Add(tech);
            else
                locked.Add(tech);
        }

        foreach (var tech in available) sortedTechs.Add(tech);
        foreach (var tech in locked) sortedTechs.Add(tech);
        foreach (var tech in completed) sortedTechs.Add(tech);

        // Create entries
        foreach (var tech in sortedTechs)
        {
            var entry = CreateTechEntry(tech);
            _techListContainer.AddChild(entry);
        }
    }

    private Control CreateTechEntry(TechnologyResource tech)
    {
        bool isUnlocked = ResearchManager.Instance?.IsTechnologyUnlocked(tech.Id) ?? false;
        bool canResearch = ResearchManager.Instance?.CanResearch(tech) ?? false;
        bool isCurrentResearch = ResearchManager.Instance?.CurrentResearch?.Id == tech.Id;

        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        // Color based on state
        Color bgColor;
        Color borderColor;
        if (isCurrentResearch)
        {
            bgColor = new Color(0.15f, 0.25f, 0.35f);
            borderColor = new Color(0.3f, 0.5f, 0.7f);
        }
        else if (isUnlocked)
        {
            bgColor = new Color(0.12f, 0.22f, 0.15f);
            borderColor = new Color(0.25f, 0.45f, 0.3f);
        }
        else if (canResearch)
        {
            bgColor = new Color(0.18f, 0.18f, 0.22f);
            borderColor = new Color(0.35f, 0.35f, 0.4f);
        }
        else
        {
            bgColor = new Color(0.12f, 0.12f, 0.14f);
            borderColor = new Color(0.2f, 0.2f, 0.22f);
        }

        var panelStyle = new StyleBoxFlat { BgColor = bgColor };
        panelStyle.SetCornerRadiusAll(6);
        panelStyle.SetBorderWidthAll(1);
        panelStyle.BorderColor = borderColor;
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        margin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddChild(margin);

        var mainVbox = new VBoxContainer();
        mainVbox.AddThemeConstantOverride("separation", 6);
        mainVbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        margin.AddChild(mainVbox);

        // Header row: status icon + name + research button
        var headerHbox = new HBoxContainer();
        headerHbox.AddThemeConstantOverride("separation", 8);
        headerHbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        mainVbox.AddChild(headerHbox);

        // Status icon
        var statusIcon = new Label();
        if (isUnlocked)
        {
            statusIcon.Text = "[OK]";
            statusIcon.Modulate = new Color(0.4f, 0.8f, 0.4f);
        }
        else if (isCurrentResearch)
        {
            statusIcon.Text = "[>>]";
            statusIcon.Modulate = new Color(0.5f, 0.7f, 0.9f);
        }
        else if (canResearch)
        {
            statusIcon.Text = "[  ]";
            statusIcon.Modulate = new Color(0.7f, 0.7f, 0.7f);
        }
        else
        {
            statusIcon.Text = "[X]";
            statusIcon.Modulate = new Color(0.5f, 0.4f, 0.4f);
        }
        statusIcon.AddThemeFontSizeOverride("font_size", 12);
        headerHbox.AddChild(statusIcon);

        // Tech name
        var nameLabel = new Label
        {
            Text = tech.Name,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 15);
        if (!canResearch && !isUnlocked)
            nameLabel.Modulate = new Color(0.55f, 0.55f, 0.55f);
        headerHbox.AddChild(nameLabel);

        // Research button (right side)
        if (canResearch && !isCurrentResearch)
        {
            var btn = new Button
            {
                Text = "Research",
                CustomMinimumSize = new Vector2(85, 28)
            };

            var btnStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.45f, 0.35f) };
            btnStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", btnStyle);

            var btnHoverStyle = new StyleBoxFlat { BgColor = new Color(0.25f, 0.55f, 0.4f) };
            btnHoverStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("hover", btnHoverStyle);

            btn.Pressed += () => OnResearchPressed(tech);
            headerHbox.AddChild(btn);
            _techButtons[tech.Id] = btn;
        }

        // Description
        var descLabel = new Label
        {
            Text = tech.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        descLabel.AddThemeFontSizeOverride("font_size", 11);
        descLabel.Modulate = new Color(0.75f, 0.75f, 0.75f);
        mainVbox.AddChild(descLabel);

        // Science cost row with colored indicators
        var costHbox = new HBoxContainer();
        costHbox.AddThemeConstantOverride("separation", 12);
        mainVbox.AddChild(costHbox);

        var costLabel = new Label { Text = "Cost:" };
        costLabel.AddThemeFontSizeOverride("font_size", 11);
        costLabel.Modulate = new Color(0.6f, 0.6f, 0.65f);
        costHbox.AddChild(costLabel);

        var cost = tech.GetScienceCost();
        foreach (var kvp in cost)
        {
            var packContainer = new HBoxContainer();
            packContainer.AddThemeConstantOverride("separation", 4);
            costHbox.AddChild(packContainer);

            // Colored circle for science pack
            Color packColor = kvp.Key switch
            {
                "automation_science" => new Color(0.9f, 0.3f, 0.3f), // Red
                "logistic_science" => new Color(0.3f, 0.9f, 0.4f),   // Green
                _ => new Color(0.7f, 0.7f, 0.7f)
            };

            var colorRect = new ColorRect
            {
                CustomMinimumSize = new Vector2(12, 12),
                Color = packColor
            };
            packContainer.AddChild(colorRect);

            string packName = kvp.Key switch
            {
                "automation_science" => "Red",
                "logistic_science" => "Green",
                _ => kvp.Key
            };

            var packLabel = new Label { Text = $"{kvp.Value} {packName}" };
            packLabel.AddThemeFontSizeOverride("font_size", 11);
            packLabel.Modulate = new Color(0.8f, 0.8f, 0.85f);
            packContainer.AddChild(packLabel);
        }

        // Prerequisites if locked
        if (!canResearch && !isUnlocked && tech.Prerequisites.Length > 0)
        {
            var prereqHbox = new HBoxContainer();
            prereqHbox.AddThemeConstantOverride("separation", 4);
            mainVbox.AddChild(prereqHbox);

            var prereqIcon = new Label { Text = "Requires:" };
            prereqIcon.AddThemeFontSizeOverride("font_size", 10);
            prereqIcon.Modulate = new Color(0.7f, 0.45f, 0.45f);
            prereqHbox.AddChild(prereqIcon);

            var prereqLabel = new Label { Text = string.Join(", ", tech.Prerequisites) };
            prereqLabel.AddThemeFontSizeOverride("font_size", 10);
            prereqLabel.Modulate = new Color(0.7f, 0.5f, 0.5f);
            prereqHbox.AddChild(prereqLabel);
        }

        return panel;
    }

    private void UpdateCurrentResearchDisplay()
    {
        var currentResearch = ResearchManager.Instance?.CurrentResearch;

        if (currentResearch == null)
        {
            _currentResearchLabel.Text = "No research in progress";
            _currentResearchLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
            _progressBar.Value = 0;
            _cancelButton.Visible = false;
        }
        else
        {
            _currentResearchLabel.Text = $"Researching: {currentResearch.Name}";
            _currentResearchLabel.Modulate = new Color(0.9f, 0.9f, 1.0f);
            _progressBar.Value = (ResearchManager.Instance?.GetResearchProgress() ?? 0) * 100;
            _cancelButton.Visible = true;
        }
    }

    private void ConnectSignals()
    {
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.ResearchStarted += OnResearchStarted;
            ResearchManager.Instance.ResearchProgress += OnResearchProgress;
            ResearchManager.Instance.ResearchCompleted += OnResearchCompleted;
            ResearchManager.Instance.TechnologyUnlocked += OnTechnologyUnlocked;
        }
    }

    private void OnResearchPressed(TechnologyResource tech)
    {
        ResearchManager.Instance?.StartResearch(tech);
    }

    private void OnCancelPressed()
    {
        ResearchManager.Instance?.CancelResearch();
        RefreshTechList();
        UpdateCurrentResearchDisplay();
    }

    private void OnResearchStarted(TechnologyResource tech)
    {
        RefreshTechList();
        UpdateCurrentResearchDisplay();
    }

    private void OnResearchProgress(TechnologyResource tech, float progress)
    {
        _progressBar.Value = progress * 100;
    }

    private void OnResearchCompleted(TechnologyResource tech)
    {
        RefreshTechList();
        UpdateCurrentResearchDisplay();
    }

    private void OnTechnologyUnlocked(TechnologyResource tech)
    {
        RefreshTechList();
    }

    public void Toggle()
    {
        Visible = !Visible;
        if (Visible)
        {
            RefreshTechList();
            UpdateCurrentResearchDisplay();
        }
    }

    private void OnTitleBarInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    _isDragging = true;
                    _dragOffset = _panel.Position - mouseButton.GlobalPosition;
                }
                else
                {
                    _isDragging = false;
                }
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion && _isDragging)
        {
            _panel.Position = mouseMotion.GlobalPosition + _dragOffset;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("research"))
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
