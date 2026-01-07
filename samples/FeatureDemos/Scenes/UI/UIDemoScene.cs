using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.UI;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace FeatureDemos.Scenes.UI;

/// <summary>
/// Demo scene showcasing all UI components:
/// - Buttons, labels, text inputs
/// - Sliders, checkboxes, radio buttons
/// - Progress bars, dropdowns
/// - Panels, dialogs, tabs
/// - Scroll views, tooltips
/// </summary>
public class UIDemoScene : DemoSceneBase
{
    private readonly IRenderer _renderer;
    private readonly UICanvas _uiCanvas;
    private readonly InputLayerManager _inputLayerManager;
    
    private UILabel? _statusLabel;
    private UIProgressBar? _healthBar;
    private UISlider? _volumeSlider;
    private UITextInput? _nameInput;
    private UIDropdown? _qualityDropdown;
    private UIRadioButtonGroup? _difficultyGroup;
    private int _buttonClickCount = 0;

    public UIDemoScene(
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        UICanvas uiCanvas,
        InputLayerManager inputLayerManager,
        ILogger<UIDemoScene> logger)
        : base(input, sceneManager, gameContext, logger, world: null)
    {
        _renderer = renderer;
        _uiCanvas = uiCanvas;
        _inputLayerManager = inputLayerManager;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("=== UI Components Demo ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  Mouse - Interact with UI");
        Logger.LogInformation("  TAB - Navigate between inputs");
        Logger.LogInformation("  ESC - Return to menu");
        
        _renderer.ClearColor = new Color(30, 30, 45);
        
        BuildUI();
        
        // Register UI canvas as input layer
        _inputLayerManager.RegisterLayer(_uiCanvas);
    }

    private void BuildUI()
    {
        // Title
        var title = new UILabel("UI Components Demo", new Vector2(20, 20))
        {
            Tooltip = new UITooltip("Brine2D UI Framework Showcase")
        };
        _uiCanvas.Add(title);
        
        // ===== LEFT COLUMN: Basic Components =====
        
        // Panel for basic components
        var basicPanel = new UIPanel(new Vector2(20, 60), new Vector2(280, 540)) // ← Shortened height
        {
            BackgroundColor = new Color(40, 40, 60, 200),
            BorderColor = new Color(100, 100, 150)
        };
        _uiCanvas.Add(basicPanel);
        
        var basicLabel = new UILabel("Basic Components", new Vector2(30, 70));
        _uiCanvas.Add(basicLabel);
        
        // Buttons
        var buttonY = 100f;
        var button1 = new UIButton("Click Me!", new Vector2(30, buttonY), new Vector2(120, 35))
        {
            Tooltip = new UITooltip("Primary button")
        };
        button1.OnClick += () =>
        {
            _buttonClickCount++;
            if (_statusLabel != null)
                _statusLabel.Text = $"Button clicked {_buttonClickCount} times!";
            Logger.LogInformation("Button clicked! Count: {Count}", _buttonClickCount);
        };
        _uiCanvas.Add(button1);
        
        var button2 = new UIButton("Disabled", new Vector2(160, buttonY), new Vector2(120, 35))
        {
            Enabled = false,
            Tooltip = new UITooltip("This button is disabled")
        };
        _uiCanvas.Add(button2);
        
        // Text Input
        buttonY += 50;
        var inputLabel = new UILabel("Text Input:", new Vector2(30, buttonY));
        _uiCanvas.Add(inputLabel);
        
        _nameInput = new UITextInput(new Vector2(30, buttonY + 25), new Vector2(250, 30))
        {
            Placeholder = "Enter your name...",
            MaxLength = 30,
            Tooltip = new UITooltip("Type your name here")
        };
        _nameInput.OnTextChanged += (text) =>
        {
            if (_statusLabel != null)
                _statusLabel.Text = $"Hello, {text}!";
        };
        _nameInput.OnSubmit += (text) =>
        {
            Logger.LogInformation("Name submitted: {Name}", text);
        };
        _uiCanvas.Add(_nameInput);
        
        // Checkbox
        buttonY += 75;
        var checkbox1 = new UICheckbox("Enable Sound", new Vector2(30, buttonY))
        {
            IsChecked = true,
            Tooltip = new UITooltip("Toggle sound effects")
        };
        checkbox1.OnCheckedChanged += (isChecked) =>
        {
            if (_statusLabel != null)
                _statusLabel.Text = $"Sound: {(isChecked ? "ON" : "OFF")}";
        };
        _uiCanvas.Add(checkbox1);
        
        var checkbox2 = new UICheckbox("VSync", new Vector2(30, buttonY + 30))
        {
            IsChecked = false
        };
        _uiCanvas.Add(checkbox2);
        
        // Slider
        buttonY += 80;
        var sliderLabel = new UILabel("Volume:", new Vector2(30, buttonY));
        _uiCanvas.Add(sliderLabel);
        
        _volumeSlider = new UISlider(new Vector2(30, buttonY + 25), new Vector2(220, 20))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Value = 75f,
            ShowValue = true,
            ValueFormat = "0",
            Tooltip = new UITooltip("Adjust volume (0-100)")
        };
        _volumeSlider.OnValueChanged += (value) =>
        {
            Logger.LogDebug("Volume: {Volume}", value);
        };
        _uiCanvas.Add(_volumeSlider);
        
        // Progress Bar
        buttonY += 70;
        var progressLabel = new UILabel("Health:", new Vector2(30, buttonY));
        _uiCanvas.Add(progressLabel);
        
        _healthBar = new UIProgressBar(new Vector2(30, buttonY + 25), new Vector2(220, 25))
        {
            Label = "HP",
            FillColor = new Color(0, 200, 0),
            Value = 0.65f,
            Tooltip = new UITooltip("Player health: 65%")
        };
        _uiCanvas.Add(_healthBar);
        
        // Buttons to control progress bar
        var decreaseButton = new UIButton("-", new Vector2(30, buttonY + 60), new Vector2(40, 30));
        decreaseButton.OnClick += () =>
        {
            if (_healthBar != null)
                _healthBar.Value = Math.Max(0f, _healthBar.Value - 0.1f);
        };
        _uiCanvas.Add(decreaseButton);
        
        var increaseButton = new UIButton("+", new Vector2(80, buttonY + 60), new Vector2(40, 30));
        increaseButton.OnClick += () =>
        {
            if (_healthBar != null)
                _healthBar.Value = Math.Min(1f, _healthBar.Value + 0.1f);
        };
        _uiCanvas.Add(increaseButton);
        
        // Dropdown (moved down slightly)
        buttonY += 110;
        var dropdownLabel = new UILabel("Graphics:", new Vector2(30, buttonY));
        _uiCanvas.Add(dropdownLabel);
        
        _qualityDropdown = new UIDropdown(new Vector2(30, buttonY + 25), new Vector2(150, 30))
        {
            Tooltip = new UITooltip("Select graphics quality")
        };
        _qualityDropdown.AddItem("Low");
        _qualityDropdown.AddItem("Medium");
        _qualityDropdown.AddItem("High");
        _qualityDropdown.AddItem("Ultra");
        _qualityDropdown.SelectedIndex = 2;
        _qualityDropdown.OnSelectionChanged += (index, text) =>
        {
            Logger.LogInformation("Graphics quality: {Quality}", text);
            if (_statusLabel != null)
                _statusLabel.Text = $"Quality: {text}";
        };
        _uiCanvas.Add(_qualityDropdown);
        
        // ===== LEFT BOTTOM: Radio Buttons (moved to separate panel) =====
        
        var radioPanel = new UIPanel(new Vector2(20, 610), new Vector2(280, 100))
        {
            BackgroundColor = new Color(40, 40, 60, 200),
            BorderColor = new Color(100, 100, 150)
        };
        _uiCanvas.Add(radioPanel);
        
        var difficultyLabel = new UILabel("Difficulty:", new Vector2(30, 620));
        _uiCanvas.Add(difficultyLabel);
        
        _difficultyGroup = new UIRadioButtonGroup();
        _difficultyGroup.OnSelectionChanged += (button) =>
        {
            Logger.LogInformation("Difficulty: {Difficulty}", button?.Label);
        };
        
        // Horizontal radio buttons to save space
        var easyRadio = new UIRadioButton("Easy", _difficultyGroup, new Vector2(30, 650))
        {
            Tooltip = new UITooltip("Relaxed gameplay")
        };
        var normalRadio = new UIRadioButton("Normal", _difficultyGroup, new Vector2(110, 650))
        {
            Tooltip = new UITooltip("Balanced challenge")
        };
        var hardRadio = new UIRadioButton("Hard", _difficultyGroup, new Vector2(210, 650))
        {
            Tooltip = new UITooltip("For veterans!")
        };
        
        _uiCanvas.Add(easyRadio);
        _uiCanvas.Add(normalRadio);
        _uiCanvas.Add(hardRadio);
        
        normalRadio.Select();
        
        // ===== CENTER COLUMN: Tabs and Panels =====
        
        var tabContainer = new UITabContainer(new Vector2(320, 60), new Vector2(380, 400))
        {
            Tooltip = new UITooltip("Organized settings")
        };
        
        // Add tabs
        tabContainer.AddTab("General");
        tabContainer.AddTab("Graphics");
        tabContainer.AddTab("Audio");
        tabContainer.AddTab("Controls");
        
        // General tab content
        var generalLabel1 = new UILabel("Game Settings", new Vector2(340, 110));
        tabContainer.AddComponentToTab(0, generalLabel1);
        
        var autoSave = new UICheckbox("Auto-save enabled", new Vector2(340, 140));
        autoSave.IsChecked = true;
        tabContainer.AddComponentToTab(0, autoSave);
        
        var showTutorials = new UICheckbox("Show tutorials", new Vector2(340, 170));
        tabContainer.AddComponentToTab(0, showTutorials);
        
        // Graphics tab content
        var graphicsLabel1 = new UILabel("Display Settings", new Vector2(340, 110));
        tabContainer.AddComponentToTab(1, graphicsLabel1);
        
        var fullscreenCheck = new UICheckbox("Fullscreen", new Vector2(340, 140));
        tabContainer.AddComponentToTab(1, fullscreenCheck);
        
        var vsyncCheck = new UICheckbox("VSync", new Vector2(340, 170));
        vsyncCheck.IsChecked = true;
        tabContainer.AddComponentToTab(1, vsyncCheck);
        
        var fpsLabel = new UILabel("FPS Limit:", new Vector2(340, 210));
        tabContainer.AddComponentToTab(1, fpsLabel);
        
        var fpsSlider = new UISlider(new Vector2(340, 235), new Vector2(200, 20))
        {
            MinValue = 30f,
            MaxValue = 144f,
            Value = 60f,
            ShowValue = true,
            ValueFormat = "0"
        };
        tabContainer.AddComponentToTab(1, fpsSlider);
        
        // Audio tab content
        var audioLabel1 = new UILabel("Volume Controls", new Vector2(340, 110));
        tabContainer.AddComponentToTab(2, audioLabel1);
        
        var masterVolLabel = new UILabel("Master:", new Vector2(340, 140));
        tabContainer.AddComponentToTab(2, masterVolLabel);
        
        var masterVolSlider = new UISlider(new Vector2(340, 165), new Vector2(180, 20))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Value = 80f,
            ShowValue = true
        };
        tabContainer.AddComponentToTab(2, masterVolSlider);
        
        var musicVolLabel = new UILabel("Music:", new Vector2(340, 200));
        tabContainer.AddComponentToTab(2, musicVolLabel);
        
        var musicVolSlider = new UISlider(new Vector2(340, 225), new Vector2(180, 20))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Value = 70f,
            ShowValue = true
        };
        tabContainer.AddComponentToTab(2, musicVolSlider);
        
        var sfxVolLabel = new UILabel("SFX:", new Vector2(340, 260));
        tabContainer.AddComponentToTab(2, sfxVolLabel);
        
        var sfxVolSlider = new UISlider(new Vector2(340, 285), new Vector2(180, 20))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Value = 90f,
            ShowValue = true
        };
        tabContainer.AddComponentToTab(2, sfxVolSlider);
        
        // Controls tab content
        var controlsLabel1 = new UILabel("Key Bindings", new Vector2(340, 110));
        tabContainer.AddComponentToTab(3, controlsLabel1);
        
        var keybindLabel = new UILabel("Press buttons to rebind keys", new Vector2(340, 140));
        tabContainer.AddComponentToTab(3, keybindLabel);
        
        var jumpButton = new UIButton("Jump: SPACE", new Vector2(340, 170), new Vector2(180, 30));
        tabContainer.AddComponentToTab(3, jumpButton);
        
        var attackButton = new UIButton("Attack: F", new Vector2(340, 210), new Vector2(180, 30));
        tabContainer.AddComponentToTab(3, attackButton);
        
        tabContainer.OnTabChanged += (index, title) =>
        {
            Logger.LogInformation("Switched to tab: {Tab}", title);
        };
        
        _uiCanvas.Add(tabContainer);
        
        // ===== CENTER BOTTOM: Dialog Buttons (moved from bottom) =====
        
        var dialogPanel = new UIPanel(new Vector2(320, 480), new Vector2(380, 110))
        {
            BackgroundColor = new Color(40, 40, 60, 200),
            BorderColor = new Color(100, 100, 150)
        };
        _uiCanvas.Add(dialogPanel);
        
        var dialogLabel = new UILabel("Dialogs & Actions", new Vector2(330, 490));
        _uiCanvas.Add(dialogLabel);
        
        var dialogButton = new UIButton("Show Dialog", new Vector2(330, 520), new Vector2(150, 35));
        dialogButton.OnClick += ShowExampleDialog;
        _uiCanvas.Add(dialogButton);
        
        var confirmButton = new UIButton("Show Confirmation", new Vector2(490, 520), new Vector2(160, 35));
        confirmButton.OnClick += ShowConfirmationDialog;
        _uiCanvas.Add(confirmButton);
        
        // ===== CENTER BOTTOM 2: Control Buttons =====
        
        var controlPanel = new UIPanel(new Vector2(320, 610), new Vector2(380, 100))
        {
            BackgroundColor = new Color(40, 40, 60, 200),
            BorderColor = new Color(100, 100, 150)
        };
        _uiCanvas.Add(controlPanel);
        
        var resetButton = new UIButton("Reset All", new Vector2(330, 640), new Vector2(150, 35));
        resetButton.OnClick += () =>
        {
            ResetAllControls();
            if (_statusLabel != null)
                _statusLabel.Text = "All controls reset!";
        };
        _uiCanvas.Add(resetButton);
        
        var menuButton = new UIButton("Back to Menu (ESC)", new Vector2(490, 640), new Vector2(200, 35));
        menuButton.OnClick += ReturnToMenu;
        _uiCanvas.Add(menuButton);
        
        // ===== RIGHT COLUMN: Scroll View =====
        
        var scrollView = new UIScrollView(new Vector2(720, 60), new Vector2(280, 530))
        {
            ContentHeight = 800,
            ShowVerticalScrollbar = true,
            Tooltip = new UITooltip("Scrollable list")
        };
        
        var scrollTitle = new UILabel("Scrollable List", new Vector2(10, 10));
        scrollView.AddChild(scrollTitle);
        
        for (int i = 0; i < 25; i++)
        {
            var item = new UILabel($"Item {i + 1}", new Vector2(10, 40 + i * 30));
            scrollView.AddChild(item);
        }
        
        _uiCanvas.Add(scrollView);
        
        // ===== RIGHT BOTTOM: Status and Instructions =====
        
        var statusPanel = new UIPanel(new Vector2(720, 610), new Vector2(540, 100))
        {
            BackgroundColor = new Color(40, 40, 60, 200),
            BorderColor = new Color(100, 100, 150)
        };
        _uiCanvas.Add(statusPanel);
        
        _statusLabel = new UILabel("Welcome to UI Demo!", new Vector2(730, 620));
        _uiCanvas.Add(_statusLabel);
        
        var instructions = new UILabel("Hover over components to see tooltips!", new Vector2(730, 660));
        _uiCanvas.Add(instructions);
    }

    private void ShowExampleDialog()
    {
        var dialog = new UIDialog(
            "Information",
            "This is a simple dialog box.\n\nYou can display messages to the user.",
            new Vector2(400, 250)
        );
        
        dialog.CenterOnScreen(new Vector2(1280, 720));
        
        dialog.AddButton("OK", () =>
        {
            dialog.Visible = false;
            Logger.LogInformation("Dialog closed");
        });
        
        _uiCanvas.Add(dialog);
    }

    private void ShowConfirmationDialog()
    {
        var dialog = new UIDialog(
            "Confirm Action",
            "Are you sure you want to proceed?",
            new Vector2(400, 250)
        );
        
        dialog.CenterOnScreen(new Vector2(1280, 720));
        
        dialog.AddButton("Yes", () =>
        {
            dialog.Visible = false;
            if (_statusLabel != null)
                _statusLabel.Text = "Action confirmed!";
            Logger.LogInformation("User confirmed action");
        });
        
        dialog.AddButton("No", () =>
        {
            dialog.Visible = false;
            if (_statusLabel != null)
                _statusLabel.Text = "Action cancelled.";
            Logger.LogInformation("User cancelled action");
        });
        
        _uiCanvas.Add(dialog);
    }

    private void ResetAllControls()
    {
        if (_nameInput != null)
            _nameInput.Text = "";
        
        if (_volumeSlider != null)
            _volumeSlider.Value = 75f;
        
        if (_healthBar != null)
            _healthBar.Value = 0.65f;
        
        if (_qualityDropdown != null)
            _qualityDropdown.SelectedIndex = 2;
        
        _buttonClickCount = 0;
        
        Logger.LogInformation("All controls reset");
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Check for return to menu
        if (CheckReturnToMenu()) return;
        
        // Update UI
        _uiCanvas.Update((float)gameTime.DeltaTime);
    }

    protected override void OnRender(GameTime gameTime)
    {
        // UI canvas renders automatically
        _uiCanvas.Render(_renderer);
    }

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        // Unregister input layer
        _inputLayerManager.UnregisterLayer(_uiCanvas);
        
        return base.OnUnloadAsync(cancellationToken);
    }
}