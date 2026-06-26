using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Engine.Transitions;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.UI;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace FeatureDemos.Scenes.UI;

/// <summary>
/// Full Brine2D UI framework showcase.
/// Every feature added to the UI system is demonstrated in its own tab.
///
/// Tabs:
///   0 – Basics          (buttons, labels, checkbox, slider, progress bar, dropdown, radio, text input)
///   1 – Rich Text / Overlays  (UIRichTextLabel, UIContextMenu, UIToast)
///   2 – Spin Box / Drag-Drop  (UISpinBox, IDragPayload, UIDropTarget)
///   3 – Virtual List    (UIVirtualList with large data set)
///   4 – Tweens          (UITween, UITweenSequence, UIEasing curves)
///   5 – Tree View       (UITreeView with hierarchical data)
///   6 – World Labels    (UIWorldLabel projected from world space via Camera2D)
///   7 – Menu Bar        (UIMenuBar — embedded demo inside the tab)
/// </summary>
public class UIDemoScene : DemoSceneBase
{
    private readonly UICanvas _canvas;
    private readonly InputLayerManager _inputLayerManager;

    // Shared status bar (bottom of canvas, outside the tab container)
    private UILabel? _status;

    // Tween demo state
    private UIProgressBar? _tweenBar;
    private UILabel? _tweenValueLabel;
    private UITween? _activeTween;
    private float _tweenBarTarget = 1f;

    // World-label demo state
    private readonly Camera2D _worldCamera = new(1280, 720);
    private UIWorldLabel? _worldLabel;
    private float _worldAngle;
    private const float WorldOrbitRadius = 80f;

    public UIDemoScene(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        UICanvas canvas,
        InputLayerManager inputLayerManager)
        : base(input, sceneManager, gameContext)
    {
        _canvas = canvas;
        _inputLayerManager = inputLayerManager;
    }

    protected override Task OnLoadAsync(CancellationToken cancellationToken, IProgress<float>? progress = null)
    {
        Logger.LogInformation("=== Brine2D UI Showcase ===");
        Renderer.ClearColor = new Color(22, 22, 32);

        _canvas.ScreenSize = new Vector2(1280, 720);

        BuildMenuBar();
        BuildTabContainer();
        BuildStatusBar();

        _inputLayerManager.RegisterLayer(_canvas);
        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Menu Bar
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildMenuBar()
    {
        var mb = new UIMenuBar
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(1280, 28),
            ZOrder = 200
        };

        var file = mb.AddMenu("File");
        file.Add("Reset Status", onClick: () => SetStatus("Status reset."));
        file.AddSeparator();
        file.Add("Return to Menu", onClick: ReturnToMenu);

        var view = mb.AddMenu("View");
        view.Add("Show Toast", onClick: () =>
            _canvas.ShowToast(new UIToast { Text = "Toast from menu bar!", Duration = 2f }));
        view.AddSeparator();
        view.Add("Disabled Item", enabled: false);

        var help = mb.AddMenu("Help");
        help.Add("About", onClick: () => SetStatus("Brine2D UI Showcase — all features demonstrated."));

        _canvas.Add(mb);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tab container (fills the area below the menu bar)
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildTabContainer()
    {
        var tabs = new UITabContainer(new Vector2(0, 28), new Vector2(1280, 660))
        {
            ZOrder = 0
        };

        tabs.AddTab("Basics");
        tabs.AddTab("Rich Text / Overlays");
        tabs.AddTab("Spin Box / Drag-Drop");
        tabs.AddTab("Virtual List");
        tabs.AddTab("Tweens");
        tabs.AddTab("Tree View");
        tabs.AddTab("World Labels");
        tabs.AddTab("Menu Bar");

        BuildBasicsTab(tabs);
        BuildRichTextOverlaysTab(tabs);
        BuildSpinBoxDragDropTab(tabs);
        BuildVirtualListTab(tabs);
        BuildTweensTab(tabs);
        BuildTreeViewTab(tabs);
        BuildWorldLabelsTab(tabs);
        BuildMenuBarTab(tabs);

        tabs.OnTabChanged += (_, title) => SetStatus($"Viewing: {title}");
        _canvas.Add(tabs);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tab 0 – Basics
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildBasicsTab(UITabContainer tabs)
    {
        int t = 0;
        int clickCount = 0;

        // Buttons
        L(tabs, t, "Buttons", 20, 12);
        var btn = B(tabs, t, "Click Me!", 20, 40, 130, 32);
        btn.OnClick += () => { clickCount++; SetStatus($"Button clicked {clickCount}×"); };

        var btnDisabled = B(tabs, t, "Disabled", 160, 40, 130, 32);
        btnDisabled.Enabled = false;
        btnDisabled.Tooltip = new UITooltip("This button is intentionally disabled.");

        // Text input
        L(tabs, t, "Text Input", 20, 90);
        var textIn = new UITextInput(new Vector2(20, 112), new Vector2(270, 30))
        {
            Placeholder = "Type something…",
            MaxLength = 40
        };
        textIn.OnTextChanged += txt => SetStatus($"Input: {txt}");
        tabs.AddComponentToTab(t, textIn);

        // Checkbox
        L(tabs, t, "Checkboxes", 320, 12);
        var cb1 = new UICheckbox("Enable Sound", new Vector2(320, 40)) { IsChecked = true };
        cb1.OnCheckedChanged += v => SetStatus($"Sound: {(v ? "ON" : "OFF")}");
        tabs.AddComponentToTab(t, cb1);
        var cb2 = new UICheckbox("VSync", new Vector2(320, 70));
        tabs.AddComponentToTab(t, cb2);

        // Slider
        L(tabs, t, "Slider (Volume)", 320, 105);
        var slider = new UISlider(new Vector2(320, 128), new Vector2(220, 18))
        {
            MinValue = 0f, MaxValue = 100f, Value = 70f, ShowValue = true, ValueFormat = "0",
            Tooltip = new UITooltip("Drag to adjust volume.")
        };
        slider.OnValueChanged += v => SetStatus($"Volume: {v:0}");
        tabs.AddComponentToTab(t, slider);

        // Progress bar
        L(tabs, t, "Progress Bar (Health)", 320, 160);
        var bar = new UIProgressBar(new Vector2(320, 182), new Vector2(220, 22))
        {
            FillColor = new Color(60, 200, 80), Value = 0.65f
        };
        tabs.AddComponentToTab(t, bar);
        var barDec = B(tabs, t, "−10%", 320, 212, 60, 26);
        barDec.OnClick += () => { bar.Value = Math.Max(0f, bar.Value - 0.1f); SetStatus($"Health: {bar.Value:P0}"); };
        var barInc = B(tabs, t, "+10%", 390, 212, 60, 26);
        barInc.OnClick += () => { bar.Value = Math.Min(1f, bar.Value + 0.1f); SetStatus($"Health: {bar.Value:P0}"); };

        // Dropdown
        L(tabs, t, "Dropdown", 600, 12);
        var dd = new UIDropdown(new Vector2(600, 40), new Vector2(180, 30));
        foreach (var q in new[] { "Low", "Medium", "High", "Ultra" }) dd.AddItem(q);
        dd.SelectedIndex = 2;
        dd.OnSelectionChanged += (_, txt) => SetStatus($"Quality: {txt}");
        tabs.AddComponentToTab(t, dd);

        // Radio buttons
        L(tabs, t, "Radio Buttons (Difficulty)", 600, 90);
        var rg = new UIRadioButtonGroup();
        rg.OnSelectionChanged += btn2 => SetStatus($"Difficulty: {btn2?.Label}");
        var rbE = new UIRadioButton("Easy", rg, new Vector2(600, 118));
        var rbN = new UIRadioButton("Normal", rg, new Vector2(690, 118));
        var rbH = new UIRadioButton("Hard", rg, new Vector2(790, 118));
        tabs.AddComponentToTab(t, rbE);
        tabs.AddComponentToTab(t, rbN);
        tabs.AddComponentToTab(t, rbH);
        rbN.Select();

        // Scroll view
        L(tabs, t, "Scroll View", 600, 155);
        var sv = new UIScrollView(new Vector2(600, 178), new Vector2(280, 200))
        {
            ContentHeight = 650, ShowVerticalScrollbar = true
        };
        for (int i = 0; i < 22; i++)
            sv.AddChild(new UILabel($"Scroll item {i + 1}", new Vector2(8, 6 + i * 28)));
        tabs.AddComponentToTab(t, sv);

        // Tab container nested
        L(tabs, t, "Tab Container", 920, 12);
        var nested = new UITabContainer(new Vector2(920, 40), new Vector2(330, 300));
        nested.AddTab("General"); nested.AddTab("Graphics"); nested.AddTab("Audio");
        nested.AddComponentToTab(0, new UILabel("Auto-save", new Vector2(10, 10)));
        nested.AddComponentToTab(0, new UICheckbox("Enabled", new Vector2(10, 35)) { IsChecked = true });
        nested.AddComponentToTab(1, new UILabel("Resolution: 1920×1080", new Vector2(10, 10)));
        nested.AddComponentToTab(2, new UILabel("Master Volume:", new Vector2(10, 10)));
        nested.AddComponentToTab(2, new UISlider(new Vector2(10, 35), new Vector2(200, 18)) { Value = 80f, ShowValue = true });
        tabs.AddComponentToTab(t, nested);

        // Dialog button
        var dlgBtn = B(tabs, t, "Show Dialog", 920, 360, 150, 34);
        dlgBtn.OnClick += () =>
        {
            var dlg = new UIDialog("Sample Dialog", "This is a modal dialog.\nClick OK to dismiss.", new Vector2(400, 220));
            dlg.CenterOnScreen(new Vector2(1280, 720));
            dlg.AddButton("OK", () => { dlg.Visible = false; SetStatus("Dialog dismissed."); });
            _canvas.Add(dlg);
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tab 1 – Rich Text / Overlays
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildRichTextOverlaysTab(UITabContainer tabs)
    {
        int t = 1;

        // Rich text
        L(tabs, t, "UIRichTextLabel — BBCode markup", 20, 12);
        var rich = new UIRichTextLabel(
            "[b]Bold[/b], [i]italic[/i], [color=#FF6060]red[/color], [color=#60FF60]green[/color], [color=#6090FF]blue[/color]\n\n" +
            "Use [b][color=#FFD700]markup[/color][/b] to style any inline text.\n\n" +
            "[i]UIRichTextLabel[/i] wraps the existing renderer markup pipeline.\n\n" +
            "Max width and word-wrap are fully supported across [b]long lines[/b] of text.",
            new Vector2(20, 38))
        {
            MaxWidth = 580f
        };
        tabs.AddComponentToTab(t, rich);

        // Context menu
        L(tabs, t, "UIContextMenu", 640, 12);
        L(tabs, t, "Right-click button below to open:", 640, 30);
        var ctxBtn = B(tabs, t, "Right-click me", 640, 56, 180, 34);
        ctxBtn.OnRightClick += () =>
        {
            var ctx = new UIContextMenu();
            ctx.AddItem("Copy");
            ctx.AddItem("Cut");
            ctx.AddItem("Paste");
            ctx.AddSeparator();
            ctx.AddItem("Delete");
            ctx.AddItem("Disabled Action", enabled: false);
            ctx.OnItemSelected += (_, label) => SetStatus($"Context: {label}");
            ctx.OnClosed += () => SetStatus("Context menu closed.");
            _canvas.ShowContextMenu(ctx, ctxBtn.Position + new Vector2(0, ctxBtn.Size.Y));
        };

        // Toasts
        L(tabs, t, "UIToast — ephemeral notifications", 20, 260);
        var toastShort = B(tabs, t, "Short Toast (1 s)", 20, 285, 180, 34);
        toastShort.OnClick += () => _canvas.ShowToast(new UIToast { Text = "Short notification!", Duration = 1f });

        var toastLong = B(tabs, t, "Long Toast (4 s)", 210, 285, 180, 34);
        toastLong.OnClick += () =>
        {
            var toast = new UIToast
            {
                Text = "Longer notification - fades in then out.",
                Duration = 4f, FadeInTime = 0.4f, FadeOutTime = 0.8f
            };
            toast.OnDismissed += () => SetStatus("Toast dismissed.");
            _canvas.ShowToast(toast);
        };

        var toastStack = B(tabs, t, "Stack 3 Toasts", 400, 285, 180, 34);
        toastStack.OnClick += () =>
        {
            _canvas.ShowToast(new UIToast { Text = "Toast 1 of 3", Duration = 3f });
            _canvas.ShowToast(new UIToast { Text = "Toast 2 of 3", Duration = 3f });
            _canvas.ShowToast(new UIToast { Text = "Toast 3 of 3", Duration = 3f });
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tab 2 – Spin Box / Drag-Drop
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildSpinBoxDragDropTab(UITabContainer tabs)
    {
        int t = 2;

        // Spin box
        L(tabs, t, "UISpinBox — numeric input with +/− arrows", 20, 12);

        L(tabs, t, "Integer (1–100, step 1):", 20, 38);
        var spinInt = new UISpinBox(new Vector2(20, 60), new Vector2(160, 34))
        {
            MinValue = 1f, MaxValue = 100f, Value = 50f, Step = 1f, ValueFormat = "0"
        };
        spinInt.OnValueChanged += v => SetStatus($"SpinBox int: {v:0}");
        tabs.AddComponentToTab(t, spinInt);

        L(tabs, t, "Float (0.0–1.0, step 0.05):", 220, 38);
        var spinFloat = new UISpinBox(new Vector2(220, 60), new Vector2(160, 34))
        {
            MinValue = 0f, MaxValue = 1f, Value = 0.5f, Step = 0.05f, ValueFormat = "0.00"
        };
        spinFloat.OnValueChanged += v => SetStatus($"SpinBox float: {v:0.00}");
        tabs.AddComponentToTab(t, spinFloat);

        // Drag-and-drop
        L(tabs, t, "Drag-and-Drop", 20, 120);
        L(tabs, t, "Drag the colored boxes onto the drop zones:", 20, 140);

        var payloads = new[]
        {
            ("Red Item",   new Color(220, 70,  70),  new Vector2(20,  170)),
            ("Blue Item",  new Color(70,  120, 220), new Vector2(150, 170)),
            ("Green Item", new Color(60,  180, 80),  new Vector2(280, 170)),
        };

        foreach (var (label, color, pos) in payloads)
        {
            var src = new UIButton(label, pos, new Vector2(110, 40))
            {
                NormalColor = color,
                HoverColor = new Color(color.R + 20, color.G + 20, color.B + 20, 255),
                PressedColor = new Color(Math.Max(0, color.R - 20), Math.Max(0, color.G - 20), Math.Max(0, color.B - 20), 255)
            };
            tabs.AddComponentToTab(t, src);
            _canvas.RegisterDraggable(src, new ColorPayload(label, color));
        }

        var dropZones = new[]
        {
            ("Zone A", new Vector2(450, 170)),
            ("Zone B", new Vector2(600, 170)),
            ("Zone C", new Vector2(750, 170)),
        };

        foreach (var (zoneName, pos) in dropZones)
        {
            var drop = new UIDropTarget
            {
                Position = pos,
                Size = new Vector2(120, 40),
                IdleColor = new Color(50, 50, 70, 200),
                HoverColor = new Color(80, 130, 200, 200),
                BorderColor = new Color(120, 120, 160)
            };
            var lbl = new UILabel(zoneName, pos + new Vector2(10, 12));
            tabs.AddComponentToTab(t, lbl);

            var capturedZone = zoneName;
            drop.OnDrop += payload =>
            {
                if (payload is ColorPayload cp)
                    SetStatus($"Dropped '{cp.Name}' onto {capturedZone}");
            };
            _canvas.RegisterDropTarget(drop);
            tabs.AddComponentToTab(t, drop);
        }

        _canvas.OnDragStarted += (src, _) => SetStatus($"Dragging: {src.GetType().Name}");
        _canvas.OnDragCancelled += (_, _) => SetStatus("Drag cancelled.");
    }

    private sealed record ColorPayload(string Name, Color Color) : IDragPayload;

    // ──────────────────────────────────────────────────────────────────────────
    // Tab 3 – Virtual List
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildVirtualListTab(UITabContainer tabs)
    {
        int t = 3;

        L(tabs, t, "UIVirtualList — renders only visible rows, no matter how large the data set", 20, 12);

        var items = Enumerable.Range(1, 500)
            .Select(i => $"Item {i,4}  —  value = {i * 7 % 100}")
            .ToList();

        var list = new UIVirtualList<string>
        {
            Position = new Vector2(20, 38),
            Size = new Vector2(580, 520),
            RowHeight = 24f
        };
        list.SetItems(items);
        list.OnSelectionChanged += idx => SetStatus(idx >= 0 ? $"Selected row {idx}: {items[idx]}" : "Deselected.");
        tabs.AddComponentToTab(t, list);

        L(tabs, t, $"{items.Count} items — scroll or use arrow keys while focused", 620, 38);

        var jumpTop = B(tabs, t, "Jump to Top", 620, 70, 140, 30);
        jumpTop.OnClick += () => { list.ScrollToTop(); SetStatus("Jumped to top."); };

        var jumpBot = B(tabs, t, "Jump to Bottom", 620, 110, 140, 30);
        jumpBot.OnClick += () => { list.ScrollToBottom(); SetStatus("Jumped to bottom."); };

        var selectMid = B(tabs, t, "Select Middle", 620, 150, 140, 30);
        selectMid.OnClick += () =>
        {
            int mid = items.Count / 2;
            ((UIVirtualListBase)list).Select(mid);
            list.ScrollIntoView(mid);
            SetStatus($"Selected middle: row {mid}");
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tab 4 – Tweens
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildTweensTab(UITabContainer tabs)
    {
        int t = 4;
        L(tabs, t, "UITween / UITweenSequence — animate any float property", 20, 12);

        // Animated progress bar
        L(tabs, t, "Animated progress bar:", 20, 38);
        _tweenBar = new UIProgressBar(new Vector2(20, 60), new Vector2(500, 28))
        {
            FillColor = new Color(80, 160, 240), Value = 0f
        };
        tabs.AddComponentToTab(t, _tweenBar);
        _tweenValueLabel = new UILabel("0.00", new Vector2(530, 62));
        tabs.AddComponentToTab(t, _tweenValueLabel);

        var easings = new (string Name, Func<float, float> Fn)[]
        {
            ("Linear",    UIEasing.Linear),
            ("Ease In",   UIEasing.QuadIn),
            ("Ease Out",  UIEasing.QuadOut),
            ("Ease InOut",UIEasing.QuadInOut),
            ("Back Out",  UIEasing.BackOut),
            ("Bounce Out",UIEasing.BounceOut),
            ("Elastic Out",UIEasing.ElasticOut),
        };

        L(tabs, t, "Click an easing to animate the bar:", 20, 105);
        for (int i = 0; i < easings.Length; i++)
        {
            var (name, fn) = easings[i];
            float bx = 20 + (i % 4) * 160f;
            float by = 128 + (i / 4) * 40f;
            var btn = B(tabs, t, name, bx, by, 148, 30);
            var capturedFn = fn;
            var capturedName = name;
            btn.OnClick += () =>
            {
                _canvas.StopAllTweens();
                float from = _tweenBarTarget >= 1f ? 1f : 0f;
                _tweenBarTarget = from >= 1f ? 0f : 1f;
                var target = _tweenBarTarget;
                _activeTween = new UITween(from, target, 1.2f, v =>
                {
                    if (_tweenBar != null) _tweenBar.Value = v;
                    if (_tweenValueLabel != null) _tweenValueLabel.Text = $"{v:0.00}";
                }, capturedFn);
                _activeTween.OnComplete += () => SetStatus($"{capturedName} tween complete.");
                _canvas.StartTween(_activeTween);
                SetStatus($"Running: {capturedName}");
            };
        }

        // Sequence demo
        L(tabs, t, "UITweenSequence — chained animations:", 20, 220);
        var seqBar = new UIProgressBar(new Vector2(20, 244), new Vector2(500, 22))
        {
            FillColor = new Color(220, 160, 60), Value = 0f
        };
        tabs.AddComponentToTab(t, seqBar);

        var runSeq = B(tabs, t, "Run Sequence", 20, 276, 150, 30);
        runSeq.OnClick += () =>
        {
            var seq = new UITweenSequence()
                .Then(0f, 0.33f, 0.5f, v => seqBar.Value = v, UIEasing.QuadOut)
                .Then(0.33f, 0.66f, 0.5f, v => seqBar.Value = v, UIEasing.Linear)
                .Then(0.66f, 1f, 0.5f, v => seqBar.Value = v, UIEasing.BounceOut)
                .Then(1f, 0f, 0.8f, v => seqBar.Value = v, UIEasing.ElasticOut);
            seq.OnComplete += () => SetStatus("Sequence complete.");
            _canvas.StartTween(seq);
            SetStatus("Sequence started…");
        };

        // Loop demo
        L(tabs, t, "Loop mode:", 220, 276);
        var loopBar = new UIProgressBar(new Vector2(20, 320), new Vector2(500, 22))
        {
            FillColor = new Color(180, 80, 220), Value = 0f
        };
        tabs.AddComponentToTab(t, loopBar);
        UITween? loopTween = null;
        var startLoop = B(tabs, t, "Start Loop", 20, 352, 130, 30);
        startLoop.OnClick += () =>
        {
            _canvas.StopAllTweens();
            loopTween = new UITween(0f, 1f, 1.5f, v => loopBar.Value = v, UIEasing.SineInOut)
                { LoopMode = UITweenLoopMode.PingPong };
            _canvas.StartTween(loopTween);
            SetStatus("Loop/PingPong started.");
        };
        var stopLoop = B(tabs, t, "Stop Loop", 160, 352, 130, 30);
        stopLoop.OnClick += () => { _canvas.StopAllTweens(); loopBar.Value = 0f; SetStatus("Loop stopped."); };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tab 5 – Tree View
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildTreeViewTab(UITabContainer tabs)
    {
        int t = 5;
        L(tabs, t, "UITreeView — hierarchical expandable tree", 20, 12);

        var tv = new UITreeView
        {
            Position = new Vector2(20, 38),
            Size = new Vector2(560, 500),
            RowHeight = 22f, IndentWidth = 18f
        };

        var sceneNode = new UITreeNode("Scene", isExpanded: true);
        sceneNode.Add(new UITreeNode("Camera").Add("Position").Add("Zoom").Add("Rotation"));
        sceneNode.Add(new UITreeNode("Entities", isExpanded: true)
            .Add(new UITreeNode("Player").Add("Transform").Add("Sprite").Add("Physics"))
            .Add(new UITreeNode("Enemy A").Add("Transform").Add("Sprite"))
            .Add(new UITreeNode("Enemy B").Add("Transform").Add("Sprite")));
        sceneNode.Add(new UITreeNode("UI").Add("Canvas").Add("HUD").Add("Menu"));

        var assetsNode = new UITreeNode("Assets");
        assetsNode.Add(new UITreeNode("Textures").Add("player.png").Add("tiles.png").Add("ui.png"));
        assetsNode.Add(new UITreeNode("Audio").Add("music.mp3").Add("sfx_jump.wav"));
        assetsNode.Add(new UITreeNode("Fonts").Add("default.ttf"));

        var settingsNode = new UITreeNode("Settings");
        settingsNode.Add("Window").Add("Graphics").Add("Audio").Add("Input");

        tv.AddRoot(sceneNode);
        tv.AddRoot(assetsNode);
        tv.AddRoot(settingsNode);

        tv.OnSelectionChanged += node =>
            SetStatus(node != null ? $"Selected: {node.Text}" : "Deselected.");
        tv.OnNodeToggled += node =>
            SetStatus($"{node.Text} {(node.IsExpanded ? "expanded" : "collapsed")}");

        tabs.AddComponentToTab(t, tv);

        // Side controls
        L(tabs, t, "Controls:", 620, 38);
        L(tabs, t, "↑↓  navigate rows", 620, 60);
        L(tabs, t, "→   expand node", 620, 80);
        L(tabs, t, "←   collapse node", 620, 100);
        L(tabs, t, "Click expander arrow to toggle", 620, 120);

        var expandAll = B(tabs, t, "Expand All Roots", 620, 155, 180, 30);
        expandAll.OnClick += () =>
        {
            foreach (var root in tv.Roots) root.IsExpanded = true;
            tv.Invalidate();
            SetStatus("All roots expanded.");
        };

        var collapseAll = B(tabs, t, "Collapse All Roots", 620, 195, 180, 30);
        collapseAll.OnClick += () =>
        {
            foreach (var root in tv.Roots) root.IsExpanded = false;
            tv.Invalidate();
            SetStatus("All roots collapsed.");
        };

        var addNode = B(tabs, t, "Add Dynamic Node", 620, 235, 180, 30);
        int dynCount = 0;
        addNode.OnClick += () =>
        {
            dynCount++;
            sceneNode.Add($"Dynamic Node {dynCount}");
            tv.Invalidate();
            SetStatus($"Added Dynamic Node {dynCount} to Scene.");
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tab 6 – World Labels
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildWorldLabelsTab(UITabContainer tabs)
    {
        int t = 6;
        L(tabs, t, "UIWorldLabel — projected from world space via Camera2D", 20, 12);
        L(tabs, t, "The label orbits a point in world space. Move the camera origin with the sliders.", 20, 32);

        // World camera controls
        L(tabs, t, "Camera X:", 20, 68);
        var camX = new UISlider(new Vector2(140, 68), new Vector2(200, 18))
            { MinValue = -300f, MaxValue = 300f, Value = 0f, ShowValue = true, ValueFormat = "0" };
        camX.OnValueChanged += v => { _worldCamera.Position = new Vector2(v, _worldCamera.Position.Y); };
        tabs.AddComponentToTab(t, camX);

        L(tabs, t, "Camera Y:", 20, 96);
        var camY = new UISlider(new Vector2(140, 96), new Vector2(200, 18))
            { MinValue = -300f, MaxValue = 300f, Value = 0f, ShowValue = true, ValueFormat = "0" };
        camY.OnValueChanged += v => { _worldCamera.Position = new Vector2(_worldCamera.Position.X, v); };
        tabs.AddComponentToTab(t, camY);

        L(tabs, t, "Camera Zoom:", 20, 124);
        var camZoom = new UISlider(new Vector2(140, 124), new Vector2(200, 18))
            { MinValue = 0.25f, MaxValue = 4f, Value = 1f, ShowValue = true, ValueFormat = "0.00" };
        camZoom.OnValueChanged += v => _worldCamera.Zoom = v;
        tabs.AddComponentToTab(t, camZoom);

        // Toggle culling
        var cullCheck = new UICheckbox("Cull when off-screen", new Vector2(20, 152)) { IsChecked = true };
        cullCheck.OnCheckedChanged += v => { if (_worldLabel != null) _worldLabel.CullWhenOffScreen = v; };
        tabs.AddComponentToTab(t, cullCheck);

        // Background padding toggle
        var bgCheck = new UICheckbox("Show background box", new Vector2(220, 152)) { IsChecked = true };
        tabs.AddComponentToTab(t, bgCheck);

        // Preview viewport indicator
        L(tabs, t, "Orbit preview (world origin = canvas center):", 20, 186);
        var orbitPanel = new UIPanel(new Vector2(20, 208), new Vector2(600, 300))
        {
            BackgroundColor = new Color(18, 28, 48),
            BorderColor = new Color(60, 80, 120)
        };
        tabs.AddComponentToTab(t, orbitPanel);

        // The world label itself
        _worldLabel = new UIWorldLabel
        {
            Text = "⬡ World Entity",
            WorldPosition = new Vector2(WorldOrbitRadius, 0),
            TextColor = new Color(255, 220, 80),
            BackgroundPadding = 4f,
            BackgroundColor = new Color(0, 0, 0, 160),
            BorderColor = new Color(255, 220, 80),
            CullWhenOffScreen = true
        };
        bgCheck.OnCheckedChanged += v => _worldLabel.BackgroundPadding = v ? 4f : 0f;
        _canvas.WorldCamera = _worldCamera;
        _worldLabel.Visible = false;
        _canvas.AddWorldComponent(_worldLabel);
        tabs.OnTabChanged += (index, _) => { if (_worldLabel != null) _worldLabel.Visible = (index == t); };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tab 7 – Menu Bar (embedded demo)
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildMenuBarTab(UITabContainer tabs)
    {
        int t = 7;
        L(tabs, t, "UIMenuBar — horizontal menu bar with dropdown submenus", 20, 12);
        L(tabs, t, "The menu bar at the very top of this scene is a live UIMenuBar instance.", 20, 32);

        // Embedded preview panel
        var previewPanel = new UIPanel(new Vector2(20, 55), new Vector2(800, 420))
        {
            BackgroundColor = new Color(28, 28, 38),
            BorderColor = new Color(80, 80, 110)
        };
        tabs.AddComponentToTab(t, previewPanel);

        L(tabs, t, "Live UIMenuBar  — click the menus below:", 30, 63);

        var demoMb = new UIMenuBar
        {
            Position = new Vector2(30, 143),
            Size = new Vector2(780, 28),
            BarHeight = 28f,
            ZOrder = 50
        };

        var fileMenu = demoMb.AddMenu("File");
        fileMenu.Add("New", onClick: () => SetStatus("[Embedded] File > New"));
        fileMenu.Add("Open…", onClick: () => SetStatus("[Embedded] File > Open"));
        fileMenu.Add("Save", onClick: () => SetStatus("[Embedded] File > Save"));
        fileMenu.AddSeparator();
        fileMenu.Add("Recent Files", enabled: false);
        fileMenu.AddSeparator();
        fileMenu.Add("Exit", onClick: () => SetStatus("[Embedded] File > Exit (demo only)"));

        var editMenu = demoMb.AddMenu("Edit");
        editMenu.Add("Undo", onClick: () => SetStatus("[Embedded] Edit > Undo"));
        editMenu.Add("Redo", onClick: () => SetStatus("[Embedded] Edit > Redo"));
        editMenu.AddSeparator();
        editMenu.Add("Cut", onClick: () => SetStatus("[Embedded] Edit > Cut"));
        editMenu.Add("Copy", onClick: () => SetStatus("[Embedded] Edit > Copy"));
        editMenu.Add("Paste", onClick: () => SetStatus("[Embedded] Edit > Paste"));

        var viewMenu = demoMb.AddMenu("View");
        viewMenu.Add("Zoom In", onClick: () => SetStatus("[Embedded] View > Zoom In"));
        viewMenu.Add("Zoom Out", onClick: () => SetStatus("[Embedded] View > Zoom Out"));
        viewMenu.Add("Reset Zoom", onClick: () => SetStatus("[Embedded] View > Reset Zoom"));
        viewMenu.AddSeparator();
        viewMenu.Add("Show Grid", onClick: () => SetStatus("[Embedded] View > Show Grid"));
        viewMenu.Add("Show Rulers", onClick: () => SetStatus("[Embedded] View > Show Rulers"));

        var helpMenu = demoMb.AddMenu("Help");
        helpMenu.Add("Documentation", onClick: () => SetStatus("[Embedded] Help > Documentation"));
        helpMenu.Add("Keyboard Shortcuts", onClick: () => SetStatus("[Embedded] Help > Shortcuts"));
        helpMenu.AddSeparator();
        helpMenu.Add("About Brine2D", onClick: () => SetStatus("[Embedded] Help > About"));

        // Register with canvas so hover / click / submenu overlay work.
        // Start hidden; only show when this tab is active.
        demoMb.Visible = false;
        _canvas.Add(demoMb);
        tabs.OnTabChanged += (index, _) => demoMb.Visible = (index == t);

        // Construction code example
        L(tabs, t, "Features:", 840, 55);
        L(tabs, t, "• Click title to open submenu", 840, 79);
        L(tabs, t, "• Hover-switch between menus", 840, 99);
        L(tabs, t, "• Disabled items (grey)", 840, 119);
        L(tabs, t, "• Separators", 840, 139);
        L(tabs, t, "• Escape / click outside closes", 840, 159);
        L(tabs, t, "• ← → switch menus (keyboard)", 840, 179);
        L(tabs, t, "• ↑ ↓ navigate items", 840, 199);
        L(tabs, t, "• Enter activates item", 840, 219);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Status bar (outside tabs, always visible)
    // ──────────────────────────────────────────────────────────────────────────

    private void BuildStatusBar()
    {
        var bar = new UIPanel(new Vector2(0, 688), new Vector2(1280, 32))
        {
            BackgroundColor = new Color(30, 30, 40),
            ZOrder = 50
        };
        _canvas.Add(bar);

        _status = new UILabel("Welcome to the Brine2D UI Showcase!", new Vector2(10, 694))
        {
            ZOrder = 51
        };
        _canvas.Add(_status);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Update / Render
    // ──────────────────────────────────────────────────────────────────────────

    protected override void OnUpdate(GameTime gameTime)
    {
        if (!_inputLayerManager.KeyboardConsumed && CheckReturnToMenu()) return;

        float dt = (float)gameTime.DeltaTime;

        // Advance world-label orbit
        _worldAngle += dt * 0.8f;
        if (_worldLabel != null)
        {
            _worldLabel.WorldPosition = new Vector2(
                MathF.Cos(_worldAngle) * WorldOrbitRadius,
                MathF.Sin(_worldAngle) * WorldOrbitRadius);
        }

        _canvas.Update(dt);
    }

    protected override void OnRender(GameTime gameTime)
    {
        // Draw the "world space" orbit backdrop on the world-labels tab panel
        DrawOrbitBackdrop();
        _canvas.Render(Renderer);
    }

    private void DrawOrbitBackdrop()
    {
        // Simple crosshair + orbit ring drawn directly to the renderer,
        // visible inside the orbit preview panel (tab 6).
        // Panel is at screen x=20 y=208+56(tab header)=264, 600×300.
        // We'll just draw at a fixed position that corresponds to the panel center.
        float cx = 320f, cy = 380f; // approximate center of the orbit panel in scene space
        Renderer.DrawCircleOutline(cx, cy, WorldOrbitRadius, new Color(40, 60, 100), 1f);
        Renderer.DrawLine(cx - 8, cy, cx + 8, cy, new Color(60, 80, 120));
        Renderer.DrawLine(cx, cy - 8, cx, cy + 8, new Color(60, 80, 120));
    }

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        _inputLayerManager.UnregisterLayer(_canvas);
        _canvas.RemoveWorldComponent(_worldLabel!);
        return base.OnUnloadAsync(cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private void SetStatus(string msg)
    {
        if (_status != null) _status.Text = msg;
        Logger.LogInformation("{Msg}", msg);
    }

    private static void L(UITabContainer tabs, int tab, string text, float x, float y) =>
        tabs.AddComponentToTab(tab, new UILabel(text, new Vector2(x, y)));

    private static UIButton B(UITabContainer tabs, int tab, string text, float x, float y, float w, float h)
    {
        var btn = new UIButton(text, new Vector2(x, y), new Vector2(w, h));
        tabs.AddComponentToTab(tab, btn);
        return btn;
    }
}
