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

        // Position panel (right side of screen)
        _panel.Position = new Vector2(700, 50);
        _panel.CustomMinimumSize = new Vector2(350, 500);

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
            Text = "Research (T)",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(title);

        // Separator
        vbox.AddChild(new HSeparator());

        // Current research panel
        CreateCurrentResearchPanel(vbox);

        // Separator
        vbox.AddChild(new HSeparator());

        // Available technologies label
        var availableLabel = new Label
        {
            Text = "Available Technologies",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        availableLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(availableLabel);

        // Scroll container for tech list
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 300),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        vbox.AddChild(scroll);

        _techListContainer = new VBoxContainer { Name = "TechList" };
        _techListContainer.AddThemeConstantOverride("separation", 8);
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
            CustomMinimumSize = new Vector2(0, 80)
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f, 0.9f)
        };
        panelStyle.SetCornerRadiusAll(4);
        _currentResearchPanel.AddThemeStyleboxOverride("panel", panelStyle);
        parent.AddChild(_currentResearchPanel);

        var panelMargin = new MarginContainer();
        panelMargin.AddThemeConstantOverride("margin_left", 8);
        panelMargin.AddThemeConstantOverride("margin_right", 8);
        panelMargin.AddThemeConstantOverride("margin_top", 8);
        panelMargin.AddThemeConstantOverride("margin_bottom", 8);
        _currentResearchPanel.AddChild(panelMargin);

        var panelVbox = new VBoxContainer();
        panelVbox.AddThemeConstantOverride("separation", 5);
        panelMargin.AddChild(panelVbox);

        // Current research label
        _currentResearchLabel = new Label
        {
            Text = "No research in progress",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _currentResearchLabel.AddThemeFontSizeOverride("font_size", 14);
        panelVbox.AddChild(_currentResearchLabel);

        // Progress bar
        _progressBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 20),
            Value = 0,
            ShowPercentage = true
        };
        panelVbox.AddChild(_progressBar);

        // Cancel button
        _cancelButton = new Button
        {
            Text = "Cancel Research",
            Visible = false
        };
        _cancelButton.Pressed += OnCancelPressed;

        var cancelStyle = new StyleBoxFlat { BgColor = new Color(0.5f, 0.2f, 0.2f) };
        cancelStyle.SetCornerRadiusAll(4);
        _cancelButton.AddThemeStyleboxOverride("normal", cancelStyle);

        var cancelHoverStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.3f, 0.3f) };
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

        // Color based on state
        Color bgColor;
        if (isCurrentResearch)
            bgColor = new Color(0.2f, 0.3f, 0.4f);
        else if (isUnlocked)
            bgColor = new Color(0.15f, 0.25f, 0.15f);
        else if (canResearch)
            bgColor = new Color(0.2f, 0.2f, 0.25f);
        else
            bgColor = new Color(0.15f, 0.15f, 0.15f);

        var panelStyle = new StyleBoxFlat { BgColor = bgColor };
        panelStyle.SetCornerRadiusAll(4);
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(hbox);

        // Left side: tech info
        var infoVbox = new VBoxContainer();
        infoVbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(infoVbox);

        // Tech name with status indicator
        string statusIcon = isUnlocked ? "[DONE] " : (isCurrentResearch ? "[...] " : "");
        var nameLabel = new Label
        {
            Text = statusIcon + tech.Name
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        if (!canResearch && !isUnlocked)
            nameLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
        infoVbox.AddChild(nameLabel);

        // Description
        var descLabel = new Label
        {
            Text = tech.Description,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        descLabel.AddThemeFontSizeOverride("font_size", 11);
        descLabel.Modulate = new Color(0.8f, 0.8f, 0.8f);
        infoVbox.AddChild(descLabel);

        // Science cost
        var costLabel = new Label();
        var costParts = new Array<string>();
        var cost = tech.GetScienceCost();
        foreach (var kvp in cost)
        {
            string packName = kvp.Key switch
            {
                "automation_science" => "Red",
                "logistic_science" => "Green",
                _ => kvp.Key
            };
            costParts.Add($"{kvp.Value} {packName}");
        }
        costLabel.Text = "Cost: " + string.Join(", ", costParts);
        costLabel.AddThemeFontSizeOverride("font_size", 11);
        costLabel.Modulate = new Color(0.7f, 0.7f, 0.9f);
        infoVbox.AddChild(costLabel);

        // Prerequisites if locked
        if (!canResearch && !isUnlocked && tech.Prerequisites.Length > 0)
        {
            var prereqLabel = new Label
            {
                Text = "Requires: " + string.Join(", ", tech.Prerequisites)
            };
            prereqLabel.AddThemeFontSizeOverride("font_size", 10);
            prereqLabel.Modulate = new Color(0.8f, 0.5f, 0.5f);
            infoVbox.AddChild(prereqLabel);
        }

        // Right side: research button
        if (canResearch && !isCurrentResearch)
        {
            var btn = new Button
            {
                Text = "Research",
                CustomMinimumSize = new Vector2(80, 30)
            };

            var btnStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.4f, 0.3f) };
            btnStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", btnStyle);

            var btnHoverStyle = new StyleBoxFlat { BgColor = new Color(0.3f, 0.5f, 0.4f) };
            btnHoverStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("hover", btnHoverStyle);

            btn.Pressed += () => OnResearchPressed(tech);
            hbox.AddChild(btn);
            _techButtons[tech.Id] = btn;
        }

        return panel;
    }

    private void UpdateCurrentResearchDisplay()
    {
        var currentResearch = ResearchManager.Instance?.CurrentResearch;

        if (currentResearch == null)
        {
            _currentResearchLabel.Text = "No research in progress";
            _progressBar.Value = 0;
            _cancelButton.Visible = false;
        }
        else
        {
            _currentResearchLabel.Text = $"Researching: {currentResearch.Name}";
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
