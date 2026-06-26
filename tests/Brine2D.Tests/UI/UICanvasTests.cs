using System.Numerics;
using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;
using Brine2D.UI;
using FluentAssertions;
using NSubstitute;

namespace Brine2D.Tests.UI;

public class UICanvasTests
{
    private readonly IInputContext _input = Substitute.For<IInputContext>();

    private UICanvas CreateCanvas() => new(_input);

    private void SetMousePosition(Vector2 position) =>
        _input.MousePosition.Returns(position);

    private void SetMousePressed()
    {
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
    }

    private void SetMouseReleased()
    {
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(true);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(false);
    }

    private void SetMouseIdle()
    {
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(false);
    }

    private void SimulateButtonClick(UICanvas canvas, UIButton button)
    {
        var center = button.Position + new Vector2(button.Size.X / 2, button.Size.Y / 2);
        SetMousePosition(center);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);
    }

    private void SimulateDialogButtonClick(UICanvas canvas, UIDialog dialog, UIButton button)
    {
        var center = dialog.Position + button.Position + new Vector2(button.Size.X / 2, button.Size.Y / 2);
        SetMousePosition(center);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);
    }

    [Fact]
    public void ProcessMouseInput_NoDialog_ButtonClickFires()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("Test", new Vector2(0, 0), new Vector2(100, 100));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);

        SimulateButtonClick(canvas, button);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_WithVisibleDialog_BlocksButtonBehindIt()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("Test", new Vector2(0, 0), new Vector2(100, 100));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)));

        SimulateButtonClick(canvas, button);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_WithVisibleDialog_ReturnsTrue()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)));

        SetMousePosition(Vector2.Zero);
        SetMouseIdle();
        var result = canvas.ProcessMouseInput(_input, false);

        result.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_WithHiddenDialog_ButtonClickAllowed()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("Test", new Vector2(0, 0), new Vector2(100, 100));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)) { Visible = false });

        SimulateButtonClick(canvas, button);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_WithDisabledDialog_ButtonClickAllowed()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("Test", new Vector2(0, 0), new Vector2(100, 100));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)) { Enabled = false });

        SimulateButtonClick(canvas, button);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_DialogButtonClick_FiresDialogAction()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Confirm", "Proceed?", new Vector2(400, 200));
        dialog.CenterOnScreen(new Vector2(1280, 720));
        bool confirmed = false;
        var okButton = dialog.AddButton("OK", () => confirmed = true);
        canvas.Add(dialog);

        SimulateDialogButtonClick(canvas, dialog, okButton);

        confirmed.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_DialogHiddenAfterClose_ButtonClickResumes()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("Test", new Vector2(0, 0), new Vector2(100, 100));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);
        var dialog = new UIDialog("Title", "Message", new Vector2(400, 200));
        canvas.Add(dialog);

        SimulateButtonClick(canvas, button);
        clicked.Should().BeFalse();

        dialog.Visible = false;
        SimulateButtonClick(canvas, button);
        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessKeyboardInput_WithVisibleDialog_ConsumesInput()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)));

        canvas.ProcessKeyboardInput(_input, false).Should().BeTrue();
    }

    [Fact]
    public void ProcessKeyboardInput_WithNoDialog_DoesNotConsumeInput()
    {
        var canvas = CreateCanvas();

        canvas.ProcessKeyboardInput(_input, false).Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_WithHiddenDialog_DoesNotConsumeInput()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)) { Visible = false });

        canvas.ProcessKeyboardInput(_input, false).Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_AlreadyConsumed_ReturnsFalse()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)));

        SetMousePosition(Vector2.Zero);
        SetMouseIdle();

        canvas.ProcessMouseInput(_input, true).Should().BeFalse();
    }

    [Fact]
    public void Clear_AfterDialogAdded_SubsequentButtonClickAllowed()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)));

        SetMousePosition(Vector2.Zero);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);
        canvas.Clear();

        var button = new UIButton("Test", new Vector2(0, 0), new Vector2(100, 100));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);

        SimulateButtonClick(canvas, button);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void Clear_AfterDialogAdded_KeyboardNoLongerConsumed()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIDialog("Title", "Message", new Vector2(400, 200)));

        SetMousePosition(Vector2.Zero);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);
        canvas.Clear();

        canvas.ProcessKeyboardInput(_input, false).Should().BeFalse();
    }

    [Fact]
    public void Clear_WithNoComponents_DoesNotThrow()
    {
        var canvas = CreateCanvas();
        var act = () => canvas.Clear();
        act.Should().NotThrow();
    }

    [Fact]
    public void Clear_ProcessMouseInput_DoesNotThrow()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIButton("A", Vector2.Zero, new Vector2(100, 100)));
        canvas.Clear();

        SetMousePosition(new Vector2(50, 50));
        SetMousePressed();

        var act = () => canvas.ProcessMouseInput(_input, false);
        act.Should().NotThrow();
    }

    private static UITabContainer CreateTwoTabContainer()
    {
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 300));
        tab.AddTab("Tab0");
        tab.AddTab("Tab1");
        return tab;
    }

    [Fact]
    public void ProcessMouseInput_ButtonInActiveTab_FiresOnClick()
    {
        var canvas = CreateCanvas();
        var tab = CreateTwoTabContainer();
        var button = new UIButton("B", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        tab.AddComponentToTab(0, button);
        canvas.Add(tab);

        // Tab at (0,0) with TabHeight=30; content origin is (0,30).
        // Button at content-relative (10,50) → screen (10,80).
        var contentOrigin = tab.GetContentOrigin();
        var screenCenter = contentOrigin + button.Position + new Vector2(button.Size.X / 2, button.Size.Y / 2);
        SetMousePosition(screenCenter);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_ButtonInInactiveTab_DoesNotFire()
    {
        var canvas = CreateCanvas();
        var tab = CreateTwoTabContainer();
        var button = new UIButton("B", new Vector2(200, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        tab.AddComponentToTab(1, button);
        canvas.Add(tab);

        SimulateButtonClick(canvas, button);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_CheckboxInActiveTab_TogglesState()
    {
        var canvas = CreateCanvas();
        var tab = CreateTwoTabContainer();
        var checkbox = new UICheckbox("Option", new Vector2(10, 50));
        tab.AddComponentToTab(0, checkbox);
        canvas.Add(tab);

        // Checkbox at content-relative (10,50); content origin is tab.GetContentOrigin().
        var contentOrigin = tab.GetContentOrigin();
        var screenPos = contentOrigin + checkbox.Position + new Vector2(checkbox.Size.X / 2, checkbox.Size.Y / 2);
        SetMousePosition(screenPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        checkbox.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_SliderInActiveTab_StartsDrag()
    {
        var canvas = CreateCanvas();
        var tab = CreateTwoTabContainer();
        var slider = new UISlider(new Vector2(10, 50), new Vector2(150, 20));
        tab.AddComponentToTab(0, slider);
        canvas.Add(tab);

        // Slider at content-relative (10,50); content origin is tab.GetContentOrigin() = (0,30).
        // Screen position of slider top-left = (10,80); click somewhere inside it.
        var contentOrigin = tab.GetContentOrigin();
        SetMousePosition(contentOrigin + new Vector2(10, 55));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        slider.IsDragging.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_AfterTabSwitch_ButtonInNewTabFires()
    {
        var canvas = CreateCanvas();
        var tab = CreateTwoTabContainer();

        var tab0Button = new UIButton("T0", new Vector2(10, 50), new Vector2(80, 40));
        bool tab0Clicked = false;
        tab0Button.OnClick += () => tab0Clicked = true;
        tab.AddComponentToTab(0, tab0Button);

        var tab1Button = new UIButton("T1", new Vector2(200, 50), new Vector2(80, 40));
        bool tab1Clicked = false;
        tab1Button.OnClick += () => tab1Clicked = true;
        tab.AddComponentToTab(1, tab1Button);

        canvas.Add(tab);
        tab.SelectedTabIndex = 1;

        // Click at screen-space center of tab1Button (content-relative pos + contentOrigin).
        var contentOrigin = tab.GetContentOrigin();
        var screenCenter = contentOrigin + tab1Button.Position + new Vector2(tab1Button.Size.X / 2, tab1Button.Size.Y / 2);
        SetMousePosition(screenCenter);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        tab1Clicked.Should().BeTrue();
        tab0Clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_AfterTabSwitch_ButtonInOldTabDoesNotFire()
    {
        var canvas = CreateCanvas();
        var tab = CreateTwoTabContainer();

        var tab0Button = new UIButton("T0", new Vector2(10, 50), new Vector2(80, 40));
        bool tab0Clicked = false;
        tab0Button.OnClick += () => tab0Clicked = true;
        tab.AddComponentToTab(0, tab0Button);

        var tab1Button = new UIButton("T1", new Vector2(200, 50), new Vector2(80, 40));
        tab.AddComponentToTab(1, tab1Button);

        canvas.Add(tab);
        tab.SelectedTabIndex = 1;

        SimulateButtonClick(canvas, tab0Button);

        tab0Clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_DisabledTabContainer_ChildrenDoNotReceiveInput()
    {
        var canvas = CreateCanvas();
        var tab = CreateTwoTabContainer();
        tab.Enabled = false;
        var button = new UIButton("B", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        tab.AddComponentToTab(0, button);
        canvas.Add(tab);

        SimulateButtonClick(canvas, button);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_HiddenTabContainer_ChildrenDoNotReceiveInput()
    {
        var canvas = CreateCanvas();
        var tab = CreateTwoTabContainer();
        tab.Visible = false;
        var button = new UIButton("B", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        tab.AddComponentToTab(0, button);
        canvas.Add(tab);

        SimulateButtonClick(canvas, button);

        clicked.Should().BeFalse();
    }

    private static UIScrollView CreateScrollView(Vector2 position, Vector2 size, float contentHeight)
    {
        return new UIScrollView(position, size) { ContentHeight = contentHeight };
    }

    [Fact]
    public void ProcessMouseInput_ButtonInScrollView_FiresOnClick()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(0, 0), new Vector2(200, 200), 400);
        var button = new UIButton("B", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        canvas.Add(sv);

        SetMousePosition(new Vector2(50, 70));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_ButtonInScrollView_MouseOutsideScrollView_DoesNotFire()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(100, 100), new Vector2(200, 200), 400);
        var button = new UIButton("B", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        canvas.Add(sv);

        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_CheckboxInScrollView_TogglesState()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(0, 0), new Vector2(300, 300), 600);
        var checkbox = new UICheckbox("Opt", new Vector2(10, 20));
        sv.AddChild(checkbox);
        canvas.Add(sv);

        SetMousePosition(new Vector2(20, 30));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        checkbox.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_SliderInScrollView_StartsDrag()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(0, 0), new Vector2(300, 300), 600);
        var slider = new UISlider(new Vector2(10, 50), new Vector2(200, 20));
        sv.AddChild(slider);
        canvas.Add(sv);

        SetMousePosition(new Vector2(10, 60));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        slider.IsDragging.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_SliderInScrollView_DragAcrossFramesUpdatesValue()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(0, 0), new Vector2(300, 300), 600);
        var slider = new UISlider(new Vector2(0, 50), new Vector2(200, 20))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Value = 0f
        };
        sv.AddChild(slider);
        canvas.Add(sv);

        SetMousePosition(new Vector2(0, 60));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(100, 60));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        slider.Value.Should().BeApproximately(50f, 1f);
    }

    [Fact]
    public void ProcessMouseInput_ScrolledOutChild_DoesNotFire()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(0, 0), new Vector2(200, 200), 400);
        var button = new UIButton("B", new Vector2(10, 300), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        canvas.Add(sv);

        SetMousePosition(new Vector2(50, 100));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_ScrolledIntoView_ChildFires()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(0, 0), new Vector2(200, 200), 400);
        var button = new UIButton("B", new Vector2(10, 300), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        canvas.Add(sv);

        sv.ScrollOffset = new Vector2(0, 200);

        SetMousePosition(new Vector2(50, 130));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_DisabledScrollView_ChildDoesNotReceiveInput()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(0, 0), new Vector2(200, 200), 400);
        sv.Enabled = false;
        var button = new UIButton("B", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        canvas.Add(sv);

        SetMousePosition(new Vector2(50, 70));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_HiddenScrollView_ChildDoesNotReceiveInput()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollView(new Vector2(0, 0), new Vector2(200, 200), 400);
        sv.Visible = false;
        var button = new UIButton("B", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        canvas.Add(sv);

        SetMousePosition(new Vector2(50, 70));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    private static UIScrollView CreateScrollViewForScrollbarTests()
    {
        return new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 400,
            ShowVerticalScrollbar = true
        };
    }

    [Fact]
    public void ProcessMouseInput_ScrollbarThumbPress_SetsDragActive()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollViewForScrollbarTests();
        canvas.Add(sv);

        SetMousePosition(new Vector2(195, 50));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMouseIdle();
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        canvas.ProcessMouseInput(_input, false).Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_ScrollbarThumbDrag_ChangesScrollOffset()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollViewForScrollbarTests();
        canvas.Add(sv);

        SetMousePosition(new Vector2(195, 50));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        SetMousePosition(new Vector2(195, 100));
        canvas.ProcessMouseInput(_input, false);

        sv.ScrollOffset.Y.Should().BeApproximately(100f, 1f);
    }

    [Fact]
    public void ProcessMouseInput_ScrollbarThumbRelease_EndsDrag()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollViewForScrollbarTests();
        canvas.Add(sv);

        SetMousePosition(new Vector2(195, 50));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false).Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_ScrollWheelWithinScrollView_ChangesOffset()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollViewForScrollbarTests();
        canvas.Add(sv);

        SetMousePosition(new Vector2(50, 50));
        SetMouseIdle();
        _input.ScrollWheelDelta.Returns(-3f);
        canvas.ProcessMouseInput(_input, false);

        sv.ScrollOffset.Y.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void ProcessMouseInput_ScrollbarNotShown_ThumbDragDoesNotStart()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 400,
            ShowVerticalScrollbar = false
        };
        canvas.Add(sv);

        SetMousePosition(new Vector2(195, 50));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        sv.ScrollOffset.Y.Should().Be(0f);
    }

    [Fact]
    public void ProcessMouseInput_ContentSmallerThanView_ThumbDragDoesNotStart()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 100,
            ShowVerticalScrollbar = true
        };
        canvas.Add(sv);

        SetMousePosition(new Vector2(195, 50));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        sv.ScrollOffset.Y.Should().Be(0f);
    }

    [Fact]
    public void ProcessMouseInput_ClickScrollbarTrack_JumpsScrollOffset()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollViewForScrollbarTests();
        canvas.Add(sv);

        // The thumb starts at y=0 and is 100px tall (200/400 * 200). Click below it at y=150
        // which is clearly in the track area outside the thumb.
        SetMousePosition(new Vector2(195, 150));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        sv.ScrollOffset.Y.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void ProcessMouseInput_ClickScrollbarTrackThenDrag_ContinuesDrag()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollViewForScrollbarTests();
        canvas.Add(sv);

        // Click the lower half of the track to jump there.
        SetMousePosition(new Vector2(195, 150));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        float offsetAfterJump = sv.ScrollOffset.Y;
        offsetAfterJump.Should().BeGreaterThan(0f);

        // Hold button down and move down — offset should increase further.
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        SetMousePosition(new Vector2(195, 175));
        canvas.ProcessMouseInput(_input, false);

        sv.ScrollOffset.Y.Should().BeGreaterThanOrEqualTo(offsetAfterJump);
    }

    [Fact]
    public void ProcessMouseInput_ClickScrollbarTrackAboveThumb_JumpsScrollUp()
    {
        var canvas = CreateCanvas();
        var sv = CreateScrollViewForScrollbarTests();
        // Start scrolled to the bottom so thumb is at the bottom of the track.
        sv.ScrollOffset = new Vector2(0, 200);
        canvas.Add(sv);

        // Click near the top of the track — should jump back toward the top.
        SetMousePosition(new Vector2(195, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        sv.ScrollOffset.Y.Should().BeLessThan(200f);
    }

    private sealed class AnchoredButton : UIButton, IAnchoredUIComponent
    {
        public UIAnchor Anchor { get; set; }
        public Vector2 AnchorOffset { get; set; }

        public AnchoredButton(string text, Vector2 size)
            : base(text, Vector2.Zero, size) { }
    }

    [Fact]
    public void ProcessMouseInput_AnchoredButton_BottomRight_HitAtResolvedPosition()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);

        var button = new AnchoredButton("B", new Vector2(80, 30))
        {
            Anchor = UIAnchor.BottomRight,
            AnchorOffset = new Vector2(-100, -50)
        };
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);

        SetMousePosition(new Vector2(740, 565));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_AnchoredButton_MiddleCenter_HitAtResolvedPosition()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);

        var button = new AnchoredButton("B", new Vector2(80, 30))
        {
            Anchor = UIAnchor.MiddleCenter,
            AnchorOffset = new Vector2(-40, -15)
        };
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);

        SetMousePosition(new Vector2(400, 300));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_AnchoredButton_OldRawPosition_DoesNotHit()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);

        var button = new AnchoredButton("B", new Vector2(80, 30))
        {
            Anchor = UIAnchor.BottomRight,
            AnchorOffset = new Vector2(-100, -50)
        };
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);

        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void UIAnchorResolver_AllAnchors_ReturnCorrectOrigins()
    {
        const float w = 800f;
        const float h = 600f;

        UIAnchorResolver.Resolve(UIAnchor.TopLeft, w, h).Should().Be(new Vector2(0, 0));
        UIAnchorResolver.Resolve(UIAnchor.TopCenter, w, h).Should().Be(new Vector2(400, 0));
        UIAnchorResolver.Resolve(UIAnchor.TopRight, w, h).Should().Be(new Vector2(800, 0));
        UIAnchorResolver.Resolve(UIAnchor.MiddleLeft, w, h).Should().Be(new Vector2(0, 300));
        UIAnchorResolver.Resolve(UIAnchor.MiddleCenter, w, h).Should().Be(new Vector2(400, 300));
        UIAnchorResolver.Resolve(UIAnchor.MiddleRight, w, h).Should().Be(new Vector2(800, 300));
        UIAnchorResolver.Resolve(UIAnchor.BottomLeft, w, h).Should().Be(new Vector2(0, 600));
        UIAnchorResolver.Resolve(UIAnchor.BottomCenter, w, h).Should().Be(new Vector2(400, 600));
        UIAnchorResolver.Resolve(UIAnchor.BottomRight, w, h).Should().Be(new Vector2(800, 600));
    }

    [Fact]
    public void UICanvas_ScreenSize_DefaultIs1280x720()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize.Should().Be(new Vector2(1280, 720));
    }

    [Fact]
    public void UITextInput_FocusedAndEmpty_CursorPositionIsZero()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        input.SetFocused(true, _input);

        input.IsFocused.Should().BeTrue();
        input.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void UITextInput_FocusedWithText_CursorMovesToEnd()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            Text = "hello"
        };
        input.SetFocused(true, _input);

        input.CursorPosition.Should().Be(5);
    }

    [Fact]
    public void UITextInput_Unfocused_IsFocusedFalse()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        input.SetFocused(true, _input);
        input.SetFocused(false, _input);

        input.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void UITextInput_SetFocused_IdempotentOnSameValue()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        input.SetFocused(true, _input);
        var act = () => input.SetFocused(true, _input);
        act.Should().NotThrow();
        input.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void UITextInput_CursorPosition_ClampedToTextLength()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            Text = "hi"
        };

        input.CursorPosition = 999;
        input.CursorPosition.Should().Be(2);

        input.CursorPosition = -5;
        input.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void UIProgressBar_ValueProperty_FiresOnValueChanged()
    {
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20));
        float? receivedValue = null;
        bar.OnValueChanged += v => receivedValue = v;

        bar.Value = 0.5f;

        receivedValue.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void UIProgressBar_ValueProperty_ClampsBelowZero()
    {
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20));
        bar.Value = -1f;
        bar.Value.Should().Be(0f);
    }

    [Fact]
    public void UIProgressBar_ValueProperty_ClampsAboveOne()
    {
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20));
        bar.Value = 2f;
        bar.Value.Should().Be(1f);
    }

    [Fact]
    public void UIProgressBar_SetValue_AndPropertySetter_BothFireEvent()
    {
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20));
        int callCount = 0;
        bar.OnValueChanged += _ => callCount++;

        bar.Value = 0.3f;
        bar.SetValue(0.7f);

        callCount.Should().Be(2);
    }

    [Fact]
    public void UIProgressBar_ValueProperty_SameValue_DoesNotFireEvent()
    {
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20)) { Value = 0.5f };
        int callCount = 0;
        bar.OnValueChanged += _ => callCount++;

        bar.Value = 0.5f;

        callCount.Should().Be(0);
    }

    [Fact]
    public void UIRadioButtonGroup_SelectButton_IsCheckedTrueBeforeEventFires()
    {
        var group = new UIRadioButtonGroup();
        var rb1 = new UIRadioButton("A", group, new Vector2(0, 0));
        var rb2 = new UIRadioButton("B", group, new Vector2(0, 30));

        bool checkedDuringEvent = false;
        group.OnSelectionChanged += selected => checkedDuringEvent = selected?.IsChecked ?? false;

        rb1.Select();
        rb2.Select();

        checkedDuringEvent.Should().BeTrue();
    }

    [Fact]
    public void UIRadioButton_Select_DeselectedButtonIsCheckedFalse()
    {
        var group = new UIRadioButtonGroup();
        var rb1 = new UIRadioButton("A", group, new Vector2(0, 0));
        var rb2 = new UIRadioButton("B", group, new Vector2(0, 30));

        rb1.Select();
        rb2.Select();

        rb1.IsChecked.Should().BeFalse();
        rb2.IsChecked.Should().BeTrue();
    }



    [Fact]
    public void UIDialog_CenterOnScreen_ButtonStillClickable()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 200));
        bool confirmed = false;
        var okButton = dialog.AddButton("OK", () => confirmed = true);
        dialog.CenterOnScreen(new Vector2(1280, 720));
        canvas.Add(dialog);

        SimulateDialogButtonClick(canvas, dialog, okButton);

        confirmed.Should().BeTrue();
    }

    [Fact]
    public void UIDropdown_ScrollWheel_ScrollsVisibleItems()
    {
        var canvas = CreateCanvas();
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30))
        {
            MaxVisibleItems = 3
        };
        for (int i = 0; i < 8; i++)
            dropdown.AddItem($"Item {i}");

        canvas.Add(dropdown);

        SetMousePosition(new Vector2(10, 15));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        dropdown.IsExpanded.Should().BeTrue();

        SetMouseIdle();
        _input.ScrollWheelDelta.Returns(-1f);
        SetMousePosition(new Vector2(10, 50));
        canvas.ProcessMouseInput(_input, false);

        dropdown.Scroll(-1f);

        dropdown.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void UIDropdown_SelectItem_BeyondMaxVisible_IsReachableAfterScroll()
    {
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30))
        {
            MaxVisibleItems = 3
        };
        for (int i = 0; i < 6; i++)
            dropdown.AddItem($"Item {i}");

        dropdown.Toggle();
        dropdown.Scroll(-3f);
        dropdown.SelectItem(new Vector2(10, 30 + 30 * 0 + 15));

        dropdown.SelectedIndex.Should().Be(3);
    }

    [Fact]
    public void UICheckbox_ChangeLabel_SizeUpdates()
    {
        var checkbox = new UICheckbox("Hi", new Vector2(0, 0));
        var originalWidth = checkbox.Size.X;

        checkbox.Label = "Much Longer Label Text";

        checkbox.Size.X.Should().BeGreaterThan(originalWidth);
    }

    [Fact]
    public void UICheckbox_ChangeBoxSize_SizeUpdates()
    {
        var checkbox = new UICheckbox("Test", new Vector2(0, 0));

        checkbox.BoxSize = 40f;

        checkbox.Size.Y.Should().Be(40f);
    }

    #region UIScrollView nested in UITabContainer

    [Fact]
    public void ProcessMouseInput_ButtonInScrollViewInsideTab_FiresOnClick()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 400));
        tab.AddTab("Main");

        var sv = new UIScrollView(new Vector2(0, 40), new Vector2(400, 300)) { ContentHeight = 600f };
        var button = new UIButton("B", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        tab.AddComponentToTab(0, sv);
        canvas.Add(tab);

        SetMousePosition(new Vector2(50, 130));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_ButtonInScrollViewInsideInactiveTab_DoesNotFire()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 400));
        tab.AddTab("A");
        tab.AddTab("B");

        var sv = new UIScrollView(new Vector2(0, 40), new Vector2(400, 300)) { ContentHeight = 600f };
        var button = new UIButton("Btn", new Vector2(10, 50), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        tab.AddComponentToTab(1, sv);
        canvas.Add(tab);

        SetMousePosition(new Vector2(50, 130));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_ScrolledOutButtonInScrollViewInsideTab_DoesNotFire()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 400));
        tab.AddTab("Main");

        var sv = new UIScrollView(new Vector2(0, 40), new Vector2(400, 300)) { ContentHeight = 600f };
        var button = new UIButton("B", new Vector2(10, 400), new Vector2(80, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        sv.AddChild(button);
        tab.AddComponentToTab(0, sv);
        canvas.Add(tab);

        SetMousePosition(new Vector2(50, 200));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    #endregion

    #region IAnchoredUIComponent on built-in widgets

    [Fact]
    public void UIButton_ImplementsIAnchoredUIComponent()
    {
        var button = new UIButton("B", Vector2.Zero, new Vector2(80, 30));
        button.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UIPanel_ImplementsIAnchoredUIComponent()
    {
        var panel = new UIPanel(Vector2.Zero, new Vector2(100, 100));
        panel.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UILabel_ImplementsIAnchoredUIComponent()
    {
        var label = new UILabel("Hi", Vector2.Zero);
        label.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UIImage_ImplementsIAnchoredUIComponent()
    {
        var image = new UIImage(null, Vector2.Zero, new Vector2(64, 64));
        image.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UIButton_DefaultAnchor_IsTopLeft()
    {
        var button = new UIButton("B", Vector2.Zero, new Vector2(80, 30));
        button.Anchor.Should().Be(UIAnchor.TopLeft);
        button.AnchorOffset.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void ProcessMouseInput_AnchoredUIButton_BottomCenter_HitAtResolvedPosition()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);

        var button = new UIButton("B", Vector2.Zero, new Vector2(100, 30))
        {
            Anchor = UIAnchor.BottomCenter,
            AnchorOffset = new Vector2(-50, -40)
        };
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);

        SetMousePosition(new Vector2(400, 575));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    #endregion

    #region Tab focus cycling

    [Fact]
    public void ProcessKeyboardInput_Tab_FocusesFirstTextInput_WhenNoneActive()
    {
        var canvas = CreateCanvas();
        var input1 = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        var input2 = new UITextInput(new Vector2(0, 40), new Vector2(200, 30));
        canvas.Add(input1);
        canvas.Add(input2);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        canvas.ProcessKeyboardInput(_input, false);

        // Tab order is front-to-back: input2 was added last so it is topmost and focused first.
        input2.IsFocused.Should().BeTrue();
        input1.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_Tab_AdvancesFocusToNextInput()
    {
        var canvas = CreateCanvas();
        var input1 = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        var input2 = new UITextInput(new Vector2(0, 40), new Vector2(200, 30));
        canvas.Add(input1);
        canvas.Add(input2);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        // Tab 1: input2 (topmost) focused. Tab 2: advances to input1.
        canvas.ProcessKeyboardInput(_input, false);
        canvas.ProcessKeyboardInput(_input, false);

        input1.IsFocused.Should().BeTrue();
        input2.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_Tab_WrapsAroundToFirstInput()
    {
        var canvas = CreateCanvas();
        var input1 = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        var input2 = new UITextInput(new Vector2(0, 40), new Vector2(200, 30));
        canvas.Add(input1);
        canvas.Add(input2);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        // Tab1→input2, Tab2→input1, Tab3→wraps back to input2 (topmost).
        canvas.ProcessKeyboardInput(_input, false);
        canvas.ProcessKeyboardInput(_input, false);
        canvas.ProcessKeyboardInput(_input, false);

        input2.IsFocused.Should().BeTrue();
        input1.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_ShiftTab_CyclesBackward()
    {
        var canvas = CreateCanvas();
        var input1 = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        var input2 = new UITextInput(new Vector2(0, 40), new Vector2(200, 30));
        canvas.Add(input1);
        canvas.Add(input2);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        canvas.ProcessKeyboardInput(_input, false); // Tab → input2 (topmost, index 0)

        _input.IsKeyDown(Key.LeftShift).Returns(true);
        canvas.ProcessKeyboardInput(_input, false); // Shift+Tab from index 0 wraps to index 1 → input1

        input1.IsFocused.Should().BeTrue();
        input2.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_Tab_WithNoTextInputs_ReturnsTrueAndDoesNotThrow()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIButton("B", Vector2.Zero, new Vector2(80, 30)));

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);

        bool result = false;
        var act = () => result = canvas.ProcessKeyboardInput(_input, false);
        act.Should().NotThrow();
        result.Should().BeTrue();
    }

    [Fact]
    public void ProcessKeyboardInput_Tab_BlockedByActiveDialog()
    {
        var canvas = CreateCanvas();
        var input1 = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        canvas.Add(input1);
        canvas.Add(new UIDialog("Title", "Msg", new Vector2(300, 150)));

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        canvas.ProcessKeyboardInput(_input, false);

        input1.IsFocused.Should().BeFalse();
    }

    #endregion

    #region UIDropdown screen-edge flip

    [Fact]
    public void UIDropdown_ScreenHeight_DefaultIs720()
    {
        var dropdown = new UIDropdown(new Vector2(0, 690), new Vector2(150, 30));
        dropdown.ScreenHeight.Should().Be(720f);
    }

    [Fact]
    public void UICanvas_Add_Dropdown_SetsScreenHeightFromScreenSize()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1024, 768);
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));

        canvas.Add(dropdown);

        dropdown.ScreenHeight.Should().Be(768f);
    }

    [Fact]
    public void UICanvas_ScreenSizeChange_UpdatesDropdownScreenHeight()
    {
        var canvas = CreateCanvas();
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        canvas.Add(dropdown);

        canvas.ScreenSize = new Vector2(1920, 1080);

        dropdown.ScreenHeight.Should().Be(1080f);
    }

    [Fact]
    public void UIDropdown_Contains_FlippedList_HitTestWorksAboveHeader()
    {
        var dropdown = new UIDropdown(new Vector2(0, 680), new Vector2(150, 30))
        {
            ScreenHeight = 720f,
            MaxVisibleItems = 3
        };
        for (int i = 0; i < 5; i++)
            dropdown.AddItem($"Item {i}");
        dropdown.Toggle();

        var hitAbove = dropdown.Contains(new Vector2(50, 600));
        var hitBelow = dropdown.Contains(new Vector2(50, 730));

        hitAbove.Should().BeTrue();
        hitBelow.Should().BeFalse();
    }

    [Fact]
    public void UIDropdown_Contains_NormalList_HitTestWorksBelowHeader()
    {
        var dropdown = new UIDropdown(new Vector2(0, 10), new Vector2(150, 30))
        {
            ScreenHeight = 720f,
            MaxVisibleItems = 3
        };
        for (int i = 0; i < 5; i++)
            dropdown.AddItem($"Item {i}");
        dropdown.Toggle();

        var hitBelow = dropdown.Contains(new Vector2(50, 60));
        var hitAbove = dropdown.Contains(new Vector2(50, -10));

        hitBelow.Should().BeTrue();
        hitAbove.Should().BeFalse();
    }

    #endregion

    #region Bug 1 — flipped dropdown item selection

    [Fact]
    public void UIDropdown_FlippedList_SelectItem_SelectsCorrectItem()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1280, 720);

        // Place dropdown near bottom so list flips upward.
        var dropdown = new UIDropdown(new Vector2(0, 680), new Vector2(150, 30))
        {
            MaxVisibleItems = 3,
            ScreenHeight = 720f
        };
        for (int i = 0; i < 5; i++)
            dropdown.AddItem($"Item {i}");
        canvas.Add(dropdown);

        // Open the dropdown.
        SetMousePosition(new Vector2(10, 695));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        dropdown.IsExpanded.Should().BeTrue();

        // The list is above the header. First visible item sits just above the header top (680).
        // visibleCount=3, itemHeight=30, listTop = 680 - 90 = 590
        float listTop = dropdown.Position.Y - 3 * dropdown.Size.Y;
        float firstItemCenterY = listTop + dropdown.Size.Y / 2f;

        SetMousePosition(new Vector2(10, firstItemCenterY));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        dropdown.SelectedIndex.Should().Be(0);
        dropdown.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void UIDropdown_NormalList_SelectItem_SelectsCorrectItem()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1280, 720);

        var dropdown = new UIDropdown(new Vector2(0, 10), new Vector2(150, 30))
        {
            MaxVisibleItems = 3,
            ScreenHeight = 720f
        };
        for (int i = 0; i < 5; i++)
            dropdown.AddItem($"Item {i}");
        canvas.Add(dropdown);

        // Open.
        SetMousePosition(new Vector2(10, 25));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        dropdown.IsExpanded.Should().BeTrue();

        // Second item in the list (index 1), below the header.
        float secondItemCenterY = dropdown.Position.Y + dropdown.Size.Y + dropdown.Size.Y * 1 + dropdown.Size.Y / 2f;

        SetMousePosition(new Vector2(10, secondItemCenterY));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        dropdown.SelectedIndex.Should().Be(1);
        dropdown.IsExpanded.Should().BeFalse();
    }

    #endregion

    #region Bug 2 — UITabContainer content clipping (compile-only; visual test)

    [Fact]
    public void UITabContainer_ContentClip_DoesNotThrow()
    {
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(200, 150));
        tab.AddTab("T");
        var label = new UILabel("Overflow text", new Vector2(0, 200));
        tab.AddComponentToTab(0, label);

        // Rendering is headless here; just verify no exception is raised when the
        // scissor push/pop path is exercised (the renderer no-ops in tests).
        var renderer = Substitute.For<IRenderer>();
        var act = () => tab.Render(renderer);
        act.Should().NotThrow();
    }

    #endregion

    #region Bug 3 — Tab focus cycling finds nested UITextInputs

    [Fact]
    public void ProcessKeyboardInput_Tab_FindsTextInputInsideTabContainer()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 300));
        tab.AddTab("Main");
        var nestedInput = new UITextInput(new Vector2(10, 50), new Vector2(200, 30));
        tab.AddComponentToTab(0, nestedInput);
        canvas.Add(tab);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);

        // First Tab lands on the container itself (new tab-bar stop).
        canvas.ProcessKeyboardInput(_input, false);
        canvas.FocusedWidget.Should().Be(tab);

        // Second Tab moves into the container's active content.
        canvas.ProcessKeyboardInput(_input, false);
        nestedInput.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void ProcessKeyboardInput_Tab_FindsTextInputInsideScrollView()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(300, 200)) { ContentHeight = 500 };
        var nestedInput = new UITextInput(new Vector2(10, 50), new Vector2(200, 30));
        sv.AddChild(nestedInput);
        canvas.Add(sv);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        canvas.ProcessKeyboardInput(_input, false);

        nestedInput.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void ProcessKeyboardInput_Tab_CyclesBetweenTopLevelAndNestedInputs()
    {
        var canvas = CreateCanvas();

        var topInput = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        canvas.Add(topInput);

        var tab = new UITabContainer(new Vector2(0, 40), new Vector2(400, 300));
        tab.AddTab("Main");
        var nestedInput = new UITextInput(new Vector2(10, 50), new Vector2(200, 30));
        tab.AddComponentToTab(0, nestedInput);
        canvas.Add(tab);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);

        // Tab container was added last so it is topmost: Tab1 → container stop.
        canvas.ProcessKeyboardInput(_input, false);
        canvas.FocusedWidget.Should().Be(tab);
        nestedInput.IsFocused.Should().BeFalse();
        topInput.IsFocused.Should().BeFalse();

        // Tab2 → nestedInput (inside the container).
        canvas.ProcessKeyboardInput(_input, false);
        nestedInput.IsFocused.Should().BeTrue();
        topInput.IsFocused.Should().BeFalse();

        // Tab3 → topInput.
        canvas.ProcessKeyboardInput(_input, false);
        topInput.IsFocused.Should().BeTrue();
        nestedInput.IsFocused.Should().BeFalse();

        // Tab4 → wraps back to container stop.
        canvas.ProcessKeyboardInput(_input, false);
        canvas.FocusedWidget.Should().Be(tab);
        topInput.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_Tab_InactiveTabInputsAreSkipped()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 300));
        tab.AddTab("A");
        tab.AddTab("B");

        var activeInput = new UITextInput(new Vector2(10, 50), new Vector2(200, 30));
        tab.AddComponentToTab(0, activeInput);

        var inactiveInput = new UITextInput(new Vector2(10, 50), new Vector2(200, 30));
        tab.AddComponentToTab(1, inactiveInput);

        canvas.Add(tab);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);

        // First Tab stops on the container (tab-bar stop).
        canvas.ProcessKeyboardInput(_input, false);
        canvas.FocusedWidget.Should().Be(tab);

        // Second Tab moves into the active tab's content.
        canvas.ProcessKeyboardInput(_input, false);
        activeInput.IsFocused.Should().BeTrue();
        inactiveInput.IsFocused.Should().BeFalse();
    }

    #endregion

    #region Bug 4 — UITextInput selection anchor model

    [Fact]
    public void UITextInput_ShiftRight_ThenShiftLeft_CollapseSelectionCorrectly()
    {
        var textInput = new UITextInput(new Vector2(0, 0), new Vector2(300, 30)) { Text = "Hello" };
        textInput.SetFocused(true, _input);
        textInput.CursorPosition = 2;

        // Shift+Right twice: select chars 2 and 3 (anchor=2, active=4)
        _input.IsKeyDown(Key.LeftShift).Returns(true);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        _input.IsKeyDown(Key.LeftControl).Returns(false);
        _input.IsKeyDown(Key.RightControl).Returns(false);
        _input.IsKeyPressed(Key.Right).Returns(true);
        textInput.HandleTextInput(_input);
        textInput.HandleTextInput(_input);

        // Selection should be [2, 4]
        var (s1, e1) = textInput.GetSelectionRangeForTest();
        s1.Should().Be(2);
        e1.Should().Be(4);

        // Shift+Left twice: collapse back to cursor=2, no selection (anchor=2, active=2)
        _input.IsKeyPressed(Key.Right).Returns(false);
        _input.IsKeyPressed(Key.Left).Returns(true);
        textInput.HandleTextInput(_input);
        textInput.HandleTextInput(_input);

        textInput.HasSelectionForTest().Should().BeFalse();
        textInput.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void UITextInput_ShiftLeft_ThenShiftRight_CollapseSelectionCorrectly()
    {
        var textInput = new UITextInput(new Vector2(0, 0), new Vector2(300, 30)) { Text = "Hello" };
        textInput.SetFocused(true, _input);
        textInput.CursorPosition = 3;

        // Shift+Left twice: select chars 1-2 (anchor=3, active=1)
        _input.IsKeyDown(Key.LeftShift).Returns(true);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        _input.IsKeyDown(Key.LeftControl).Returns(false);
        _input.IsKeyDown(Key.RightControl).Returns(false);
        _input.IsKeyPressed(Key.Left).Returns(true);
        textInput.HandleTextInput(_input);
        textInput.HandleTextInput(_input);

        var (s1, e1) = textInput.GetSelectionRangeForTest();
        s1.Should().Be(1);
        e1.Should().Be(3);

        // Shift+Right twice: collapse back to cursor=3
        _input.IsKeyPressed(Key.Left).Returns(false);
        _input.IsKeyPressed(Key.Right).Returns(true);
        textInput.HandleTextInput(_input);
        textInput.HandleTextInput(_input);

        textInput.HasSelectionForTest().Should().BeFalse();
        textInput.CursorPosition.Should().Be(3);
    }

    [Fact]
    public void UITextInput_CtrlA_SelectsAll()
    {
        var textInput = new UITextInput(new Vector2(0, 0), new Vector2(300, 30)) { Text = "Hello" };
        textInput.SetFocused(true, _input);

        _input.IsKeyDown(Key.LeftControl).Returns(true);
        _input.IsKeyDown(Key.RightControl).Returns(false);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        _input.IsKeyPressed(Key.A).Returns(true);
        textInput.HandleTextInput(_input);

        var (s, e) = textInput.GetSelectionRangeForTest();
        s.Should().Be(0);
        e.Should().Be(5);
    }

    #endregion

    #region Minor — scrollbar minimum thumb size

    [Fact]
    public void UIScrollView_MinScrollbarThumbSize_DefaultIsTen()
    {
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200));
        sv.MinScrollbarThumbSize.Should().Be(10f);
    }

    [Fact]
    public void UIScrollView_VeryLargeContent_ThumbSizeIsAtLeastMinimum()
    {
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 100_000f,
            ShowVerticalScrollbar = true,
            MinScrollbarThumbSize = 10f
        };
        var canvas = CreateCanvas();
        canvas.Add(sv);

        // Attempt to drag the scrollbar — it should start a drag without throwing
        // (verifies thumb bounds are finite and the thumb is hittable at position 195,0).
        SetMousePosition(new Vector2(195, 0));
        SetMousePressed();
        var act = () => canvas.ProcessMouseInput(_input, false);
        act.Should().NotThrow();
    }

    [Fact]
    public void UIScrollView_MinScrollbarThumbSize_CanBeOverridden()
    {
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 10_000f,
            ShowVerticalScrollbar = true,
            MinScrollbarThumbSize = 20f
        };
        sv.MinScrollbarThumbSize.Should().Be(20f);
    }

    #endregion

    #region Bug fix — scroll wheel consumed by UIScrollView

    [Fact]
    public void ProcessMouseInput_ScrollWheelOverScrollView_ReturnsTrueConsumed()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 1000f,
            ShowVerticalScrollbar = true
        };
        canvas.Add(sv);

        SetMousePosition(new Vector2(50, 50));
        SetMouseIdle();
        _input.ScrollWheelDelta.Returns(3f);

        var result = canvas.ProcessMouseInput(_input, false);

        result.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_ScrollWheelNotOverScrollView_ReturnsFalse()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 1000f,
            ShowVerticalScrollbar = true
        };
        canvas.Add(sv);

        SetMousePosition(new Vector2(500, 500));
        SetMouseIdle();
        _input.ScrollWheelDelta.Returns(3f);

        var result = canvas.ProcessMouseInput(_input, false);

        result.Should().BeFalse();
    }

    #endregion

    #region Bug fix — dialog button requires press-then-release on same button

    [Fact]
    public void ProcessMouseInput_DialogButton_PressOnAReleasedOnB_OnlyBDoesNotFire()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Test", "Msg", new Vector2(400, 200));
        dialog.CenterOnScreen(new Vector2(1280, 720));
        bool aFired = false;
        bool bFired = false;
        var btnA = dialog.AddButton("A", () => aFired = true);
        var btnB = dialog.AddButton("B", () => bFired = true);
        canvas.Add(dialog);

        // Press on A
        var centerA = dialog.Position + btnA.Position + new Vector2(btnA.Size.X / 2, btnA.Size.Y / 2);
        SetMousePosition(centerA);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        // Release on B
        var centerB = dialog.Position + btnB.Position + new Vector2(btnB.Size.X / 2, btnB.Size.Y / 2);
        SetMousePosition(centerB);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        aFired.Should().BeFalse();
        bFired.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_DialogButton_PressAndReleaseOnSameButton_Fires()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Test", "Msg", new Vector2(400, 200));
        dialog.CenterOnScreen(new Vector2(1280, 720));
        bool fired = false;
        var btn = dialog.AddButton("OK", () => fired = true);
        canvas.Add(dialog);

        SimulateDialogButtonClick(canvas, dialog, btn);

        fired.Should().BeTrue();
    }

    #endregion

    #region Bug fix — dropdown brought to front on expand

    [Fact]
    public void HandleDropdownInput_OnExpand_DropdownIsAtFront()
    {
        var canvas = CreateCanvas();
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dropdown.AddItem("Option 1");
        dropdown.AddItem("Option 2");
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(200, 200));
        canvas.Add(dropdown);
        canvas.Add(panel);

        // Click header to expand
        SetMousePosition(new Vector2(75, 15));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        canvas.Components[canvas.Components.Count - 1].Should().BeSameAs(dropdown);
    }

    #endregion

    #region Bug fix — Clear closes active dropdown

    [Fact]
    public void Clear_WithExpandedDropdown_ClosesDropdown()
    {
        var canvas = CreateCanvas();
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dropdown.AddItem("X");
        canvas.Add(dropdown);

        // Expand it
        SetMousePosition(new Vector2(75, 15));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        canvas.Clear();
        canvas.Add(dropdown);

        dropdown.IsExpanded.Should().BeFalse();
    }

    #endregion

    #region IAnchoredUIComponent — interactive widgets

    [Fact]
    public void UISlider_ImplementsIAnchoredUIComponent()
    {
        var slider = new UISlider(new Vector2(0, 0), new Vector2(200, 20));
        slider.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UICheckbox_ImplementsIAnchoredUIComponent()
    {
        var checkbox = new UICheckbox("Label", new Vector2(0, 0));
        checkbox.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UIScrollView_ImplementsIAnchoredUIComponent()
    {
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200));
        sv.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UIDropdown_ImplementsIAnchoredUIComponent()
    {
        var dd = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dd.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UIRadioButton_ImplementsIAnchoredUIComponent()
    {
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("Option", group, new Vector2(0, 0));
        rb.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UITabContainer_ImplementsIAnchoredUIComponent()
    {
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 200));
        tab.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UIProgressBar_ImplementsIAnchoredUIComponent()
    {
        var pb = new UIProgressBar(new Vector2(0, 0), new Vector2(200, 20));
        pb.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UITextInput_ImplementsIAnchoredUIComponent()
    {
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        ti.Should().BeAssignableTo<IAnchoredUIComponent>();
    }

    [Fact]
    public void UICanvas_Render_AnchoredSlider_UsesResolvedPosition()
    {
        var input = Substitute.For<IInputContext>();
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(default(string)!).ReturnsForAnyArgs(new Vector2(40, 16));
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(new Vector2(40, 16));

        var canvas = new UICanvas(input);
        canvas.ScreenSize = new Vector2(800, 600);

        var slider = new UISlider(new Vector2(0, 0), new Vector2(200, 20))
        {
            Anchor = UIAnchor.BottomRight,
            AnchorOffset = new Vector2(-200, -20)
        };
        canvas.Add(slider);

        canvas.Render(renderer);

        // BottomRight anchor (800,600) + offset(-200,-20) = (600,580)
        renderer.Received().DrawRectangleFilled(600f, Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    #endregion

    #region UIScrollView.FitContentToChildren

    [Fact]
    public void UIScrollView_FitContentToChildren_SetsContentFromChildBounds()
    {
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200));
        sv.AddChild(new UILabel("A", new Vector2(10, 10)) { Size = new Vector2(100, 20) });
        sv.AddChild(new UILabel("B", new Vector2(50, 500)) { Size = new Vector2(80, 20) });

        sv.FitContentToChildren();

        sv.ContentWidth.Should().BeGreaterThanOrEqualTo(130f);
        sv.ContentHeight.Should().BeGreaterThanOrEqualTo(520f);
    }

    [Fact]
    public void UIScrollView_FitContentToChildren_NoChildren_ContentEqualsSize()
    {
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 300));

        sv.FitContentToChildren();

        sv.ContentWidth.Should().Be(200f);
        sv.ContentHeight.Should().Be(300f);
    }

    #endregion

    #region UICanvas.Remove — interaction state cleanup

    [Fact]
    public void Remove_FocusedTextInput_UnfocusesAndStopsTextInput()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        canvas.Add(textInput);

        // Focus through the canvas so it tracks the focused input internally
        var center = textInput.Position + new Vector2(textInput.Size.X / 2, textInput.Size.Y / 2);
        SetMousePosition(center);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        textInput.IsFocused.Should().BeTrue();

        canvas.Remove(textInput);

        textInput.IsFocused.Should().BeFalse();
        _input.Received().StopTextInput();
    }

    [Fact]
    public void Remove_FocusedTextInput_KeyboardNoLongerRouted()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        canvas.Add(textInput);
        textInput.SetFocused(true, _input);
        canvas.Remove(textInput);

        // ProcessKeyboardInput should not throw and should not return true (nothing on canvas)
        _input.IsKeyPressed(Key.Tab).Returns(false);
        bool consumed = canvas.ProcessKeyboardInput(_input, false);
        consumed.Should().BeFalse();
    }

    [Fact]
    public void Remove_HoveredButton_ClearsHoverState()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("B", new Vector2(0, 0), new Vector2(100, 40));
        canvas.Add(button);

        SetMousePosition(new Vector2(50, 20));
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        canvas.Remove(button);

        // After removal the button is gone from the canvas — ProcessMouseInput should not throw
        SetMousePosition(new Vector2(50, 20));
        SetMouseIdle();
        var act = () => canvas.ProcessMouseInput(_input, false);
        act.Should().NotThrow();
    }

    [Fact]
    public void Remove_ActiveDropdown_ClosesDropdown()
    {
        var canvas = CreateCanvas();
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dropdown.AddItem("X");
        canvas.Add(dropdown);

        SetMousePosition(new Vector2(10, 15));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);
        dropdown.IsExpanded.Should().BeTrue();

        canvas.Remove(dropdown);

        dropdown.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void Remove_ComponentNotOnCanvas_DoesNotThrow()
    {
        var canvas = CreateCanvas();
        var orphan = new UIButton("B", Vector2.Zero, new Vector2(80, 30));

        var act = () => canvas.Remove(orphan);
        act.Should().NotThrow();
    }

    #endregion

    #region UIDialog — Escape key

    [Fact]
    public void ProcessKeyboardInput_Escape_WithActiveDialog_AllowEscapeClose_FiresEvent()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Title", "Msg", new Vector2(300, 150));
        bool dismissed = false;
        dialog.OnEscapeDismissed += () => dismissed = true;
        canvas.Add(dialog);

        _input.IsKeyPressed(Key.Escape).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        dismissed.Should().BeTrue();
    }

    [Fact]
    public void ProcessKeyboardInput_Escape_WithActiveDialog_AllowEscapeCloseFalse_DoesNotFire()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Title", "Msg", new Vector2(300, 150))
        {
            AllowEscapeClose = false
        };
        bool dismissed = false;
        dialog.OnEscapeDismissed += () => dismissed = true;
        canvas.Add(dialog);

        _input.IsKeyPressed(Key.Escape).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        dismissed.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_Escape_WithActiveDialog_StillReturnsTrue()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIDialog("Title", "Msg", new Vector2(300, 150)));

        _input.IsKeyPressed(Key.Escape).Returns(true);
        bool consumed = canvas.ProcessKeyboardInput(_input, false);

        consumed.Should().BeTrue();
    }

    [Fact]
    public void ProcessKeyboardInput_Escape_NoDialog_DoesNotConsume()
    {
        var canvas = CreateCanvas();

        _input.IsKeyPressed(Key.Escape).Returns(true);
        bool consumed = canvas.ProcessKeyboardInput(_input, false);

        consumed.Should().BeFalse();
    }

    #endregion

    #region UISlider — vertical orientation via canvas

    [Fact]
    public void ProcessMouseInput_VerticalSlider_DragFromTop_SetsMaxValue()
    {
        var canvas = CreateCanvas();
        var slider = new UISlider(new Vector2(0, 0), new Vector2(20, 100))
        {
            Orientation = SliderOrientation.Vertical,
            MinValue = 0f,
            MaxValue = 1f
        };
        canvas.Add(slider);

        SetMousePosition(new Vector2(10, 0));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        slider.Value.Should().BeApproximately(1f, 0.01f);
    }

    [Fact]
    public void ProcessMouseInput_VerticalSlider_DragFromBottom_SetsMinValue()
    {
        var canvas = CreateCanvas();
        var slider = new UISlider(new Vector2(0, 0), new Vector2(20, 100))
        {
            Orientation = SliderOrientation.Vertical,
            MinValue = 0f,
            MaxValue = 1f,
            Value = 1f
        };
        canvas.Add(slider);

        SetMousePosition(new Vector2(10, 100));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        slider.Value.Should().BeApproximately(0f, 0.01f);
    }

    #endregion

    #region UILabel — MaxWidth via canvas render

    [Fact]
    public void UICanvas_Render_LabelWithMaxWidth_PassesMaxWidthToRenderer()
    {
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(Arg.Any<string>()).Returns(new Vector2(40, 16));
        renderer.MeasureText(Arg.Any<string>(), Arg.Any<float?>()).Returns(new Vector2(40, 16));
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(new Vector2(40, 32));

        var input = Substitute.For<IInputContext>();
        var canvas = new UICanvas(input);
        var label = new UILabel("Long text here", new Vector2(10, 10)) { MaxWidth = 80f };
        canvas.Add(label);

        canvas.Render(renderer);

        var call = renderer.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "DrawText" &&
                                 c.GetArguments() is [string, _, _, TextRenderOptions] &&
                                 (string)c.GetArguments()[0] == "Long text here");
        call.Should().NotBeNull();
        var opts = (TextRenderOptions)call!.GetArguments()[3];
        opts.MaxWidth.Should().HaveValue();
        opts.MaxWidth!.Value.Should().BeApproximately(80f, 0.01f);
    }

    #endregion

    #region UIDialog — draggable via UICanvas

    [Fact]
    public void ProcessMouseInput_DraggableDialog_TitleBarDrag_MovesDialog()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Title", "Msg", new Vector2(200f, 100f))
        {
            IsDraggable = true
        };
        dialog.CenterOnScreen(new Vector2(1280f, 720f));
        canvas.Add(dialog);

        var startPos = dialog.Position;
        var titleBarPoint = startPos + new Vector2(50f, 10f);

        // Press on title bar
        SetMousePosition(titleBarPoint);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        // Drag 30px right, 20px down
        var newPos = titleBarPoint + new Vector2(30f, 20f);
        SetMousePosition(newPos);
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        dialog.Position.X.Should().BeApproximately(startPos.X + 30f, 1f);
        dialog.Position.Y.Should().BeApproximately(startPos.Y + 20f, 1f);
    }

    [Fact]
    public void ProcessMouseInput_NonDraggableDialog_TitleBarDrag_DoesNotMove()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Title", "Msg", new Vector2(200f, 100f))
        {
            IsDraggable = false
        };
        dialog.CenterOnScreen(new Vector2(1280f, 720f));
        canvas.Add(dialog);

        var startPos = dialog.Position;
        var titleBarPoint = startPos + new Vector2(50f, 10f);

        SetMousePosition(titleBarPoint);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(titleBarPoint + new Vector2(50f, 50f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        dialog.Position.Should().Be(startPos);
    }

    [Fact]
    public void ProcessMouseInput_DraggableDialog_Release_EndsDrag()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Title", "Msg", new Vector2(200f, 100f))
        {
            IsDraggable = true
        };
        dialog.CenterOnScreen(new Vector2(1280f, 720f));
        canvas.Add(dialog);

        var titleBarPoint = dialog.Position + new Vector2(50f, 10f);

        SetMousePosition(titleBarPoint);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(titleBarPoint + new Vector2(10f, 10f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        var posAfterDrag = dialog.Position;

        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(titleBarPoint + new Vector2(100f, 100f));
        _input.IsMouseButtonDown(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        dialog.Position.Should().Be(posAfterDrag);
    }

    #endregion

    #region UITextInput — drag-to-select via UICanvas

    [Fact]
    public void ProcessMouseInput_TextInput_MouseDrag_CreatesSelection()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(300f, 30f));
        textInput.Text = "Hello World";
        canvas.Add(textInput);

        // Focus the field by pressing
        SetMousePosition(new Vector2(10f, 15f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        // Drag to the right
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        SetMousePosition(new Vector2(80f, 15f));
        canvas.ProcessMouseInput(_input, false);

        textInput.IsMouseDraggingForTest.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_TextInput_MouseRelease_EndsDrag()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(300f, 30f));
        textInput.Text = "Hello";
        canvas.Add(textInput);

        SetMousePosition(new Vector2(10f, 15f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        textInput.IsMouseDraggingForTest.Should().BeFalse();
    }

    #endregion

    #region UIScrollView — scrollbar hover via UICanvas

    [Fact]
    public void ProcessMouseInput_ScrollView_MouseOverScrollbar_UpdatesHoverState()
    {
        var canvas = CreateCanvas();
        var scrollView = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ShowVerticalScrollbar = true,
            ContentHeight = 600f
        };
        canvas.Add(scrollView);

        // Hover directly over the scrollbar column (right edge)
        float scrollbarX = 200f - scrollView.ScrollbarWidth + 1f;
        SetMousePosition(new Vector2(scrollbarX, 50f));
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        scrollView.IsHoveringVerticalScrollbarForTest.Should().BeTrue();
    }

    #endregion

    #region UICanvas.Clear() — StopTextInput

    [Fact]
    public void Clear_WithFocusedTextInput_CallsStopTextInput()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(200f, 30f));
        canvas.Add(textInput);

        SetMousePosition(new Vector2(50f, 15f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        textInput.IsFocused.Should().BeTrue();

        canvas.Clear();

        _input.Received().StopTextInput();
    }

    [Fact]
    public void Clear_WithFocusedTextInput_TextInputIsUnfocused()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(200f, 30f));
        canvas.Add(textInput);

        SetMousePosition(new Vector2(50f, 15f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        textInput.IsFocused.Should().BeTrue();

        canvas.Clear();

        textInput.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void Clear_WithNoFocusedTextInput_DoesNotCallStopTextInput()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UIButton("X", Vector2.Zero, new Vector2(100f, 40f)));

        canvas.Clear();

        _input.DidNotReceive().StopTextInput();
    }

    #endregion

    #region Hit-testing z-order — topmost widget wins

    [Fact]
    public void ProcessMouseInput_OverlappingButtons_TopmostButtonReceivesClick()
    {
        var canvas = CreateCanvas();
        var bottom = new UIButton("Bottom", new Vector2(0f, 0f), new Vector2(100f, 100f));
        var top = new UIButton("Top", new Vector2(0f, 0f), new Vector2(100f, 100f));
        bool bottomClicked = false;
        bool topClicked = false;
        bottom.OnClick += () => bottomClicked = true;
        top.OnClick += () => topClicked = true;
        canvas.Add(bottom);
        canvas.Add(top);

        SimulateButtonClick(canvas, top);

        topClicked.Should().BeTrue();
        bottomClicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_OverlappingButtons_AfterBringToFront_NewTopmostWins()
    {
        var canvas = CreateCanvas();
        var first = new UIButton("First", new Vector2(0f, 0f), new Vector2(100f, 100f));
        var second = new UIButton("Second", new Vector2(0f, 0f), new Vector2(100f, 100f));
        bool firstClicked = false;
        bool secondClicked = false;
        first.OnClick += () => firstClicked = true;
        second.OnClick += () => secondClicked = true;
        canvas.Add(first);
        canvas.Add(second);

        canvas.BringToFront(first);
        SimulateButtonClick(canvas, first);

        firstClicked.Should().BeTrue();
        secondClicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_OverlappingCheckboxes_TopmostCheckboxToggles()
    {
        var canvas = CreateCanvas();
        var bottom = new UICheckbox("B", new Vector2(0f, 0f));
        var top = new UICheckbox("T", new Vector2(0f, 0f));
        canvas.Add(bottom);
        canvas.Add(top);

        SetMousePosition(new Vector2(5f, 5f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        top.IsChecked.Should().BeTrue();
        bottom.IsChecked.Should().BeFalse();
    }

    #endregion

    #region Keyboard focus navigation

    private void SetTabPressed()
    {
        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
    }

    private void SetShiftTabPressed()
    {
        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(true);
        _input.IsKeyDown(Key.RightShift).Returns(false);
    }

    private void ClearKeyState()
    {
        _input.IsKeyPressed(Key.Tab).Returns(false);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);
        _input.IsKeyPressed(Key.Enter).Returns(false);
        _input.IsKeyPressed(Key.Space).Returns(false);
        _input.IsKeyPressed(Key.Right).Returns(false);
        _input.IsKeyPressed(Key.Left).Returns(false);
    }

    [Fact]
    public void Tab_FocusesFirstButton()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("A", new Vector2(0, 0), new Vector2(80, 30));
        canvas.Add(btn);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        btn.IsFocused.Should().BeTrue();
        canvas.FocusedWidget.Should().BeSameAs(btn);
    }

    [Fact]
    public void Tab_CyclesForwardThroughWidgets()
    {
        var canvas = CreateCanvas();
        var btn1 = new UIButton("A", new Vector2(0, 0), new Vector2(80, 30));
        var btn2 = new UIButton("B", new Vector2(100, 0), new Vector2(80, 30));
        canvas.Add(btn1);
        canvas.Add(btn2);

        // btn2 is topmost (added last): Tab1→btn2, Tab2→btn1.
        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        ClearKeyState();
        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        btn1.IsFocused.Should().BeTrue();
        btn2.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void ShiftTab_CyclesBackward()
    {
        var canvas = CreateCanvas();
        var btn1 = new UIButton("A", new Vector2(0, 0), new Vector2(80, 30));
        var btn2 = new UIButton("B", new Vector2(100, 0), new Vector2(80, 30));
        canvas.Add(btn1);
        canvas.Add(btn2);

        // Tab1→btn2 (topmost), Tab2→btn1, ShiftTab→btn2 (backward).
        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        ClearKeyState();
        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        ClearKeyState();
        SetShiftTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        btn2.IsFocused.Should().BeTrue();
        btn1.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void Tab_FocusesCheckbox()
    {
        var canvas = CreateCanvas();
        var cb = new UICheckbox("Opt", new Vector2(0, 0));
        canvas.Add(cb);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        cb.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void Tab_FocusesSlider()
    {
        var canvas = CreateCanvas();
        var sl = new UISlider(new Vector2(0, 0), new Vector2(100, 20));
        canvas.Add(sl);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        sl.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void Tab_FocusesRadioButton()
    {
        var canvas = CreateCanvas();
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("X", group, new Vector2(0, 0));
        canvas.Add(rb);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        rb.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void Tab_FocusesDropdown()
    {
        var canvas = CreateCanvas();
        var dd = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        canvas.Add(dd);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        dd.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void Enter_ActivatesFocusedButton()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("A", new Vector2(0, 0), new Vector2(80, 30));
        bool clicked = false;
        btn.OnClick += () => clicked = true;
        canvas.Add(btn);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        ClearKeyState();
        _input.IsKeyPressed(Key.Enter).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void Space_ActivatesFocusedCheckbox()
    {
        var canvas = CreateCanvas();
        var cb = new UICheckbox("Opt", new Vector2(0, 0));
        canvas.Add(cb);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        ClearKeyState();
        _input.IsKeyPressed(Key.Space).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        cb.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void RightArrow_NudgesFocusedSlider()
    {
        var canvas = CreateCanvas();
        var sl = new UISlider(new Vector2(0, 0), new Vector2(100, 20))
        {
            MinValue = 0f, MaxValue = 100f, Value = 50f, Step = 5f
        };
        canvas.Add(sl);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        ClearKeyState();
        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        sl.Value.Should().BeApproximately(55f, 0.001f);
    }

    [Fact]
    public void Clear_ClearsFocusedWidget()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("A", new Vector2(0, 0), new Vector2(80, 30));
        canvas.Add(btn);
        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        btn.IsFocused.Should().BeTrue();

        canvas.Clear();

        canvas.FocusedWidget.Should().BeNull();
        btn.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void Tab_IncludesWidgetsInScrollView()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200)) { ContentHeight = 400 };
        var btn = new UIButton("Inner", new Vector2(10, 10), new Vector2(80, 30));
        sv.AddChild(btn);
        canvas.Add(sv);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        btn.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void Tab_IncludesWidgetsInPanel()
    {
        var canvas = CreateCanvas();
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(200, 100));
        var btn = new UIButton("Inner", new Vector2(10, 10), new Vector2(80, 30));
        panel.AddChild(btn);
        canvas.Add(panel);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        btn.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void Tab_TraversalOrder_IsFrontToBack_TopmostFirst()
    {
        // Add two buttons; btn2 is added last so it is rendered on top (front).
        // Tab should focus btn2 first, then btn1.
        var canvas = CreateCanvas();
        var btn1 = new UIButton("Back", new Vector2(0, 0), new Vector2(80, 30));
        var btn2 = new UIButton("Front", new Vector2(0, 50), new Vector2(80, 30));
        canvas.Add(btn1);
        canvas.Add(btn2);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        canvas.FocusedWidget.Should().BeSameAs(btn2, "first Tab press should focus the topmost (front) widget");
    }

    [Fact]
    public void Tab_TraversalOrder_SecondTabMovesToNextFrontToBack()
    {
        var canvas = CreateCanvas();
        var btn1 = new UIButton("Back", new Vector2(0, 0), new Vector2(80, 30));
        var btn2 = new UIButton("Front", new Vector2(0, 50), new Vector2(80, 30));
        canvas.Add(btn1);
        canvas.Add(btn2);

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        ClearKeyState();
        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        canvas.FocusedWidget.Should().BeSameAs(btn1, "second Tab should move to the next widget in front-to-back order");
    }

    [Fact]
    public void ShiftTab_TraversalOrder_FirstShiftTabFocusesBackmostWidget()
    {
        var canvas = CreateCanvas();
        var btn1 = new UIButton("Back", new Vector2(0, 0), new Vector2(80, 30));
        var btn2 = new UIButton("Front", new Vector2(0, 50), new Vector2(80, 30));
        canvas.Add(btn1);
        canvas.Add(btn2);

        SetShiftTabPressed();
        canvas.ProcessKeyboardInput(_input, false);

        canvas.FocusedWidget.Should().BeSameAs(btn1, "Shift+Tab from no focus should wrap to the last widget (backmost)");
    }

    [Fact]
    public void Tab_TraversalOrder_ThreeWidgets_CyclesFullRound()
    {
        var canvas = CreateCanvas();
        var btn1 = new UIButton("1", new Vector2(0, 0), new Vector2(60, 20));
        var btn2 = new UIButton("2", new Vector2(0, 30), new Vector2(60, 20));
        var btn3 = new UIButton("3", new Vector2(0, 60), new Vector2(60, 20));
        canvas.Add(btn1);
        canvas.Add(btn2);
        canvas.Add(btn3);

        IUIComponent[] expectedOrder = [btn3, btn2, btn1];

        for (int i = 0; i < expectedOrder.Length; i++)
        {
            SetTabPressed();
            canvas.ProcessKeyboardInput(_input, false);
            ClearKeyState();
            canvas.FocusedWidget.Should().BeSameAs(expectedOrder[i], $"Tab #{i + 1} should focus {((UIButton)expectedOrder[i]).Text}");
        }

        SetTabPressed();
        canvas.ProcessKeyboardInput(_input, false);
        canvas.FocusedWidget.Should().BeSameAs(btn3, "Tab should wrap back to front after cycling all widgets");
    }

    #endregion

    #region UILabel / UIImage OnClick via canvas

    [Fact]
    public void ProcessMouseInput_LabelWithOnClick_FiresOnRelease()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Click", new Vector2(0, 0));
        label.Size = new Vector2(80, 20);
        bool fired = false;
        label.OnClick += () => fired = true;
        canvas.Add(label);

        SetMousePosition(new Vector2(10, 5));
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_LabelWithNoOnClick_DoesNotThrow()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("X", new Vector2(0, 0));
        label.Size = new Vector2(80, 20);
        canvas.Add(label);

        SetMousePosition(new Vector2(10, 5));
        SetMouseReleased();
        var act = () => canvas.ProcessMouseInput(_input, false);
        act.Should().NotThrow();
    }

    [Fact]
    public void ProcessMouseInput_ImageWithOnClick_FiresOnRelease()
    {
        var canvas = CreateCanvas();
        var image = new UIImage(null, new Vector2(0, 0), new Vector2(50, 50));
        bool fired = false;
        image.OnClick += () => fired = true;
        canvas.Add(image);

        SetMousePosition(new Vector2(10, 10));
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_LabelWithOnClick_OutsideBounds_DoesNotFire()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("X", new Vector2(200, 200));
        label.Size = new Vector2(80, 20);
        bool fired = false;
        label.OnClick += () => fired = true;
        canvas.Add(label);

        SetMousePosition(new Vector2(10, 10));
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        fired.Should().BeFalse();
    }

    #endregion

    #region UIPanel children input routing

    [Fact]
    public void ProcessMouseInput_ButtonInPanel_FiresOnClick()
    {
        var canvas = CreateCanvas();
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(300, 200));
        var btn = new UIButton("X", new Vector2(10, 10), new Vector2(80, 30));
        bool clicked = false;
        btn.OnClick += () => clicked = true;
        panel.AddChild(btn);
        canvas.Add(panel);

        SimulateButtonClick(canvas, btn);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_CheckboxInPanel_Toggles()
    {
        var canvas = CreateCanvas();
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(300, 200));
        var cb = new UICheckbox("A", new Vector2(10, 10));
        panel.AddChild(cb);
        canvas.Add(panel);

        SetMousePosition(new Vector2(15, 15));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        cb.IsChecked.Should().BeTrue();
    }

    #endregion

    #region UIDialog children input routing

    [Fact]
    public void ProcessMouseInput_ButtonInDialog_FiresOnClick()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        var btn = new UIButton("Inner", new Vector2(20, 220), new Vector2(80, 30));
        bool clicked = false;
        btn.OnClick += () => clicked = true;
        dialog.AddChild(btn);
        canvas.Add(dialog);

        // Screen position = dialog.Position + btn.Position + half-size
        var screenPos = dialog.Position + btn.Position + new Vector2(btn.Size.X / 2, btn.Size.Y / 2);
        SetMousePosition(screenPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_CheckboxInDialog_Toggles()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        var cb = new UICheckbox("Opt", new Vector2(20, 280));
        dialog.AddChild(cb);
        canvas.Add(dialog);

        // Mouse at dialog.Position + cb.Position + a few pixels inside
        var screenPos = dialog.Position + cb.Position + new Vector2(5, 5);
        SetMousePosition(screenPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        cb.IsChecked.Should().BeTrue();
    }



    #endregion

    #region Bug fixes

    [Fact]
    public void Remove_FocusedButton_ClearsFocusedWidget()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("A", new Vector2(0, 0), new Vector2(100, 40));
        canvas.Add(button);

        // Tab to focus the button.
        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);
        _input.IsKeyPressed(Key.Tab).Returns(false);

        canvas.FocusedWidget.Should().Be(button);

        canvas.Remove(button);

        canvas.FocusedWidget.Should().BeNull();
        button.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void Remove_FocusedCheckbox_ClearsFocusedWidget()
    {
        var canvas = CreateCanvas();
        var cb = new UICheckbox("opt", new Vector2(0, 0));
        canvas.Add(cb);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);
        _input.IsKeyPressed(Key.Tab).Returns(false);

        canvas.FocusedWidget.Should().Be(cb);

        canvas.Remove(cb);

        canvas.FocusedWidget.Should().BeNull();
        cb.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void ScreenSize_Updated_PushedIntoTabContainer()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(Vector2.Zero, new Vector2(400, 200));
        canvas.Add(tab);

        canvas.ScreenSize = new Vector2(1920, 1080);

        tab.ScreenSize.Should().Be(new Vector2(1920, 1080));
    }

    [Fact]
    public void Add_TabContainer_InheritsCurrentScreenSize()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1600, 900);
        var tab = new UITabContainer(Vector2.Zero, new Vector2(300, 150));

        canvas.Add(tab);

        tab.ScreenSize.Should().Be(new Vector2(1600, 900));
    }

    [Fact]
    public void ProcessMouseInput_DropdownInDialog_CanToggle()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative position
        var dd = new UIDropdown(new Vector2(20, 220), new Vector2(120, 30));
        dd.Items.Add("Option A");
        dd.Items.Add("Option B");
        dialog.AddChild(dd);
        canvas.Add(dialog);

        // Screen click on the dropdown header = dialog.Position + dd.Position + a few pixels inside
        var clickPos = dialog.Position + dd.Position + new Vector2(5, 5);
        SetMousePosition(clickPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        dd.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_DropdownInDialog_SelectItemClosesDropdown()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative position
        var ddRelPos = new Vector2(20, 220);
        var dd = new UIDropdown(ddRelPos, new Vector2(120, 30));
        dd.Items.Add("Option A");
        dd.Items.Add("Option B");
        dialog.AddChild(dd);
        canvas.Add(dialog);

        // Open (screen-space click on header)
        var openPos = dialog.Position + ddRelPos + new Vector2(5, 5);
        SetMousePosition(openPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        // Click first item: screen Y = dialog.Y + dd.Y (relative) + itemHeight + a few pixels
        var itemPos = new Vector2(openPos.X, dialog.Position.Y + ddRelPos.Y + 35);
        SetMousePosition(itemPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        dd.IsExpanded.Should().BeFalse();
        dd.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ProcessMouseInput_ScrollViewInDialog_ScrollWheelWorks()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative position
        var sv = new UIScrollView(new Vector2(20, 220), new Vector2(120, 80));
        sv.ContentHeight = 200f;
        sv.ShowVerticalScrollbar = true;
        dialog.AddChild(sv);
        canvas.Add(dialog);

        // Screen mouse inside the scroll view
        var mousePos = dialog.Position + sv.Position + new Vector2(5, 20);
        _input.ScrollWheelDelta.Returns(-3f);
        SetMousePosition(mousePos);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        sv.ScrollOffset.Y.Should().BeGreaterThan(0f);
    }

    #endregion

    #region Dialog child button press-tracking bug

    [Fact]
    public void ProcessMouseInput_DialogChildButton_DragOntoButton_DoesNotFire()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative position (60, 290)
        var btn = new UIButton("OK", new Vector2(60, 290), new Vector2(100, 40));
        bool clicked = false;
        btn.OnClick += () => clicked = true;
        dialog.AddChild(btn);
        canvas.Add(dialog);

        // Press somewhere outside the button (screen space), then release on the button.
        SetMousePosition(new Vector2(200, 200));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        // Screen pos of btn center = dialog.Position + btn.Position + half-size
        var btnCenter = dialog.Position + btn.Position + new Vector2(btn.Size.X / 2, btn.Size.Y / 2);
        SetMousePosition(btnCenter);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_DialogChildButton_NormalClick_Fires()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        var btn = new UIButton("OK", new Vector2(60, 290), new Vector2(100, 40));
        bool clicked = false;
        btn.OnClick += () => clicked = true;
        dialog.AddChild(btn);
        canvas.Add(dialog);

        var btnCenter = dialog.Position + btn.Position + new Vector2(btn.Size.X / 2, btn.Size.Y / 2);
        SetMousePosition(btnCenter);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    #endregion

    #region SetFocus

    [Fact]
    public void SetFocus_Button_SetsFocusedWidget()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("X", new Vector2(0, 0), new Vector2(100, 40));
        canvas.Add(btn);

        var result = canvas.SetFocus(btn);

        result.Should().BeTrue();
        canvas.FocusedWidget.Should().Be(btn);
        btn.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void SetFocus_DisabledComponent_ReturnsFalse()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("X", new Vector2(0, 0), new Vector2(100, 40)) { Enabled = false };
        canvas.Add(btn);

        var result = canvas.SetFocus(btn);

        result.Should().BeFalse();
        canvas.FocusedWidget.Should().BeNull();
    }

    [Fact]
    public void SetFocus_NotFocusableType_ReturnsFalse()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("hi", new Vector2(0, 0));
        canvas.Add(label);

        var result = canvas.SetFocus(label);

        result.Should().BeFalse();
        canvas.FocusedWidget.Should().BeNull();
    }

    [Fact]
    public void SetFocus_MovesFromPreviousWidget()
    {
        var canvas = CreateCanvas();
        var btn1 = new UIButton("A", new Vector2(0, 0), new Vector2(100, 40));
        var btn2 = new UIButton("B", new Vector2(0, 50), new Vector2(100, 40));
        canvas.Add(btn1);
        canvas.Add(btn2);

        canvas.SetFocus(btn1);
        canvas.SetFocus(btn2);

        btn1.IsFocused.Should().BeFalse();
        btn2.IsFocused.Should().BeTrue();
        canvas.FocusedWidget.Should().Be(btn2);
    }

    [Fact]
    public void SetFocus_TextInput_StartsFocus()
    {
        var canvas = CreateCanvas();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        canvas.Add(ti);

        var result = canvas.SetFocus(ti);

        result.Should().BeTrue();
        ti.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void SetFocus_InvisibleComponent_ReturnsFalse()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("X", new Vector2(0, 0), new Vector2(100, 40)) { Visible = false };
        canvas.Add(btn);

        var result = canvas.SetFocus(btn);

        result.Should().BeFalse();
        canvas.FocusedWidget.Should().BeNull();
    }

    #endregion

    #region UITabContainer keyboard tab-switching via click focus

    [Fact]
    public void ProcessKeyboardInput_RightArrow_ClickedTabBar_SelectsNextTab()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 150));
        tab.AddTab("A");
        tab.AddTab("B");
        tab.AddTab("C");
        canvas.Add(tab);

        // Click on the tab bar.
        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        tab.SelectedTabIndex.Should().Be(1);
    }

    [Fact]
    public void ProcessKeyboardInput_LeftArrow_ClickedTabBar_SelectsPreviousTab()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 150));
        tab.AddTab("A");
        tab.AddTab("B");
        tab.AddTab("C");
        tab.SelectedTabIndex = 2;
        canvas.Add(tab);

        SetMousePosition(new Vector2(250, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        _input.IsKeyPressed(Key.Left).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        tab.SelectedTabIndex.Should().Be(1);
    }

    [Fact]
    public void ProcessKeyboardInput_RightArrow_WrapsAroundToFirstTab()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 150));
        tab.AddTab("A");
        tab.AddTab("B");
        tab.SelectedTabIndex = 1;
        canvas.Add(tab);

        // Click tab 1 (X=200: tabWidth=150, index = 200/150 = 1).
        SetMousePosition(new Vector2(200, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        tab.SelectedTabIndex.Should().Be(0);
    }

    [Fact]
    public void ProcessKeyboardInput_RightArrow_NoTabBarClick_DoesNotSwitchTab()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 150));
        tab.AddTab("A");
        tab.AddTab("B");
        canvas.Add(tab);

        // No click — Right should not switch.
        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        tab.SelectedTabIndex.Should().Be(0);
    }

    [Fact]
    public void ProcessMouseInput_ClickContentArea_DoesNotGrantTabBarFocus()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 150));
        tab.AddTab("A");
        tab.AddTab("B");
        canvas.Add(tab);

        // Click in the content area (below tab bar, TabHeight defaults to 30).
        SetMousePosition(new Vector2(10, 50));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        tab.SelectedTabIndex.Should().Be(0);
    }

    [Fact]
    public void ProcessKeyboardInput_TabBarFocus_SetsIsFocusedOnContainer()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 150));
        tab.AddTab("A");
        tab.AddTab("B");
        canvas.Add(tab);

        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        tab.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_ClickElsewhere_ClearsTabBarFocus()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 150));
        tab.AddTab("A");
        tab.AddTab("B");
        canvas.Add(tab);

        // Grant focus.
        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        tab.IsFocused.Should().BeTrue();

        // Click away from the tab container.
        SetMousePosition(new Vector2(500, 500));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        tab.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_FocusedSlider_LeftRight_DoesNotSwitchTab()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(300, 150));
        tab.AddTab("A");
        tab.AddTab("B");
        canvas.Add(tab);

        var slider = new UISlider(new Vector2(400, 0), new Vector2(100, 20));
        canvas.Add(slider);

        // Click tab bar to give it focus.
        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        // Also focus the slider.
        canvas.SetFocus(slider);

        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        // Slider consumed Right — tab should stay on index 0.
        tab.SelectedTabIndex.Should().Be(0);
    }

    #endregion

    #region Cross-focus: UITextInput ↔ UITextArea

    [Fact]
    public void ProcessMouseInput_ClickTextInput_ClearsTextAreaFocus()
    {
        var canvas = CreateCanvas();
        var textArea = new UITextArea(new Vector2(0, 0), new Vector2(200, 80));
        var textInput = new UITextInput(new Vector2(0, 100), new Vector2(200, 30));
        canvas.Add(textArea);
        canvas.Add(textInput);

        // Focus the text area first.
        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        textArea.IsFocused.Should().BeTrue();

        // Now click the text input.
        SetMousePosition(new Vector2(10, 115));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        textArea.IsFocused.Should().BeFalse();
        textInput.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_ClickTextArea_ClearsTextInputFocus()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        var textArea = new UITextArea(new Vector2(0, 50), new Vector2(200, 80));
        canvas.Add(textInput);
        canvas.Add(textArea);

        // Focus the text input first.
        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        textInput.IsFocused.Should().BeTrue();

        // Now click the text area.
        SetMousePosition(new Vector2(10, 70));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        textInput.IsFocused.Should().BeFalse();
        textArea.IsFocused.Should().BeTrue();
    }

    #endregion

    #region UIDialog.SuppressOverlay – stacked dialogs

    [Fact]
    public void Render_SingleDialog_ShowOverlay_True_OverlayIsDrawn()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(300, 200)) { ShowOverlay = true };
        canvas.Add(dialog);
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(Arg.Any<string>()).Returns(new Vector2(40f, 16f));
        renderer.MeasureText(Arg.Any<string>(), Arg.Any<float?>()).Returns(new Vector2(40f, 16f));

        canvas.Render(renderer);

        renderer.Received().DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), dialog.OverlayColor);
    }

    [Fact]
    public void Render_TwoStackedDialogs_OnlyTopmostDrawsOverlay()
    {
        var canvas = CreateCanvas();
        var lower = new UIDialog("L", "lower", new Vector2(300, 200)) { ShowOverlay = true };
        var upper = new UIDialog("U", "upper", new Vector2(300, 200)) { ShowOverlay = true };
        canvas.Add(lower);
        canvas.Add(upper);

        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(Arg.Any<string>()).Returns(new Vector2(40f, 16f));
        renderer.MeasureText(Arg.Any<string>(), Arg.Any<float?>()).Returns(new Vector2(40f, 16f));

        canvas.Render(renderer);

        // Overlay is drawn exactly once (only the topmost dialog).
        renderer.Received(1).DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), lower.OverlayColor);
    }

    [Fact]
    public void Render_TwoDialogs_LowerHidden_VisibleDialogDrawsOverlay()
    {
        var canvas = CreateCanvas();
        var lower = new UIDialog("L", "lower", new Vector2(300, 200)) { ShowOverlay = true, Visible = false };
        var upper = new UIDialog("U", "upper", new Vector2(300, 200)) { ShowOverlay = true };
        canvas.Add(lower);
        canvas.Add(upper);

        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(Arg.Any<string>()).Returns(new Vector2(40f, 16f));
        renderer.MeasureText(Arg.Any<string>(), Arg.Any<float?>()).Returns(new Vector2(40f, 16f));

        canvas.Render(renderer);

        renderer.Received(1).DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), upper.OverlayColor);
    }

    #endregion

    #region Gamepad navigation

    private void SetGamepadConnected(int index = 0) =>
        _input.IsGamepadConnected(index).Returns(true);

    private void SetGamepadButtonPressed(GamepadButton button, int index = 0) =>
        _input.IsGamepadButtonPressed(button, index).Returns(true);

    private void ClearGamepadButtons()
    {
        foreach (var b in System.Enum.GetValues<GamepadButton>())
            _input.IsGamepadButtonPressed(b, 0).Returns(false);
    }

    [Fact]
    public void ProcessGamepadInput_NotConnected_ReturnsFalse()
    {
        var canvas = CreateCanvas();
        _input.IsGamepadConnected(0).Returns(false);

        canvas.ProcessGamepadInput(_input, false).Should().BeFalse();
    }

    [Fact]
    public void ProcessGamepadInput_Consumed_ReturnsFalse()
    {
        var canvas = CreateCanvas();
        SetGamepadConnected();

        canvas.ProcessGamepadInput(_input, true).Should().BeFalse();
    }

    [Fact]
    public void ProcessGamepadInput_DPadDown_FocusesFirstButton()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("A", new Vector2(0, 0), new Vector2(100, 40));
        canvas.Add(button);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.DPadDown);

        canvas.ProcessGamepadInput(_input, false);

        button.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void ProcessGamepadInput_DPadUp_FocusesFromEnd()
    {
        var canvas = CreateCanvas();
        var b1 = new UIButton("A", new Vector2(0, 0), new Vector2(100, 40));
        var b2 = new UIButton("B", new Vector2(0, 50), new Vector2(100, 40));
        canvas.Add(b1);
        canvas.Add(b2);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.DPadUp);

        canvas.ProcessGamepadInput(_input, false);

        // DPadUp with no current focus wraps to the last widget
        (b1.IsFocused || b2.IsFocused).Should().BeTrue();
    }

    [Fact]
    public void ProcessGamepadInput_DPadDown_ThenA_ClicksFocusedButton()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("A", new Vector2(0, 0), new Vector2(100, 40));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.DPadDown);
        canvas.ProcessGamepadInput(_input, false);

        ClearGamepadButtons();
        SetGamepadButtonPressed(GamepadButton.A);
        canvas.ProcessGamepadInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void ProcessGamepadInput_A_TogglesCheckbox()
    {
        var canvas = CreateCanvas();
        var checkbox = new UICheckbox("Check", new Vector2(0, 0));
        canvas.Add(checkbox);
        canvas.SetFocus(checkbox);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.A);
        canvas.ProcessGamepadInput(_input, false);

        checkbox.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void ProcessGamepadInput_DPadRight_NudgesSliderUp()
    {
        var canvas = CreateCanvas();
        var slider = new UISlider(new Vector2(0, 0), new Vector2(200, 20)) { Value = 0.5f };
        canvas.Add(slider);
        canvas.SetFocus(slider);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.DPadRight);
        canvas.ProcessGamepadInput(_input, false);

        slider.Value.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void ProcessGamepadInput_DPadLeft_NudgesSliderDown()
    {
        var canvas = CreateCanvas();
        var slider = new UISlider(new Vector2(0, 0), new Vector2(200, 20)) { Value = 0.5f };
        canvas.Add(slider);
        canvas.SetFocus(slider);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.DPadLeft);
        canvas.ProcessGamepadInput(_input, false);

        slider.Value.Should().BeLessThan(0.5f);
    }

    [Fact]
    public void ProcessGamepadInput_B_DismissesActiveDialog()
    {
        var canvas = CreateCanvas();
        bool dismissed = false;
        var dialog = new UIDialog("T", "M", new Vector2(300, 200));
        dialog.OnEscapeDismissed += () => dismissed = true;
        canvas.Add(dialog);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.B);
        canvas.ProcessGamepadInput(_input, false);

        dismissed.Should().BeTrue();
    }

    [Fact]
    public void ProcessGamepadInput_WithDialog_DPadDown_CyclesFocusInsideDialog()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        var ok = dialog.AddButton("OK", () => { });
        var cancel = dialog.AddButton("Cancel", () => { });
        canvas.Add(dialog);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.DPadDown);
        canvas.ProcessGamepadInput(_input, false);

        (ok.IsFocused || cancel.IsFocused).Should().BeTrue();
    }

    [Fact]
    public void ProcessGamepadInput_ReturnsTrue_WhenButtonPressed()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("A", new Vector2(0, 0), new Vector2(100, 40));
        canvas.Add(button);

        SetGamepadConnected();
        SetGamepadButtonPressed(GamepadButton.DPadDown);

        canvas.ProcessGamepadInput(_input, false).Should().BeTrue();
    }

    #endregion

    #region Bug fixes

    // Fix 1: Clicking a text input should clear _focusedWidget so the previous button
    // no longer displays its focus ring.
    [Fact]
    public void ClickingTextInput_ClearsPreviousButtonFocus()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("B", new Vector2(0, 0), new Vector2(100, 40));
        var input = new UITextInput(new Vector2(0, 50), new Vector2(200, 30));
        canvas.Add(button);
        canvas.Add(input);

        canvas.SetFocus(button);
        button.IsFocused.Should().BeTrue();

        SetMousePosition(new Vector2(10, 60));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        button.IsFocused.Should().BeFalse();
        canvas.FocusedWidget.Should().Be(input);
    }

    // Fix 1 (text area variant): Clicking a text area should clear _focusedWidget.
    [Fact]
    public void ClickingTextArea_ClearsPreviousButtonFocus()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("B", new Vector2(0, 0), new Vector2(100, 40));
        var area = new UITextArea(new Vector2(0, 50), new Vector2(200, 80));
        canvas.Add(button);
        canvas.Add(area);

        canvas.SetFocus(button);
        button.IsFocused.Should().BeTrue();

        SetMousePosition(new Vector2(10, 60));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        button.IsFocused.Should().BeFalse();
        canvas.FocusedWidget.Should().Be(area);
    }

    // Fix 2: Hiding a focused button should cause ValidateFocusedWidgets to clear focus
    // before the next ProcessMouseInput/ProcessKeyboardInput so keyboard events are not
    // silently swallowed.
    [Fact]
    public void HidingFocusedButton_ClearsFocusOnNextProcess()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("B", new Vector2(0, 0), new Vector2(100, 40));
        canvas.Add(button);
        canvas.SetFocus(button);

        button.Visible = false;

        SetMousePosition(Vector2.Zero);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        button.IsFocused.Should().BeFalse();
        canvas.FocusedWidget.Should().BeNull();
    }

    // Fix 2: Disabling a focused widget should clear focus on the next frame.
    [Fact]
    public void DisablingFocusedButton_ClearsFocusOnNextProcess()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("B", new Vector2(0, 0), new Vector2(100, 40));
        canvas.Add(button);
        canvas.SetFocus(button);

        button.Enabled = false;

        SetMousePosition(Vector2.Zero);
        SetMouseIdle();
        canvas.ProcessKeyboardInput(_input, false);

        button.IsFocused.Should().BeFalse();
        canvas.FocusedWidget.Should().BeNull();
    }

    // Fix 2: Hiding a focused text input should stop keyboard routing on the next frame.
    [Fact]
    public void HidingFocusedTextInput_ClearsFocusOnNextKeyboardProcess()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        canvas.Add(textInput);
        canvas.SetFocus(textInput);
        textInput.IsFocused.Should().BeTrue();

        textInput.Visible = false;

        SetMousePosition(Vector2.Zero);
        SetMouseIdle();
        canvas.ProcessKeyboardInput(_input, false);

        textInput.IsFocused.Should().BeFalse();
    }

    // Fix 3: A UIPanel inside a UIScrollView inside a UIDialog should have its children
    // receive input — a button inside that panel must fire OnClick.
    [Fact]
    public void ButtonInPanelInScrollViewInDialog_ReceivesClick()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(600, 400));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative: sv at (20, 150), panel at (0,0), btn at (5,5)
        var sv = new UIScrollView(new Vector2(20, 150), new Vector2(300, 200))
        {
            ContentHeight = 200,
            ShowVerticalScrollbar = false
        };
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(280, 80));
        var btn = new UIButton("X", new Vector2(5, 5), new Vector2(60, 30));
        bool clicked = false;
        btn.OnClick += () => clicked = true;
        panel.AddChild(btn);
        sv.AddChild(panel);
        dialog.AddChild(sv);
        canvas.Add(dialog);

        var screenPos = dialog.Position + sv.Position + panel.Position + btn.Position + new Vector2(btn.Size.X / 2, btn.Size.Y / 2);
        SetMousePosition(screenPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    // Fix 4: A UILabel with OnClick inside a UIDialog should fire when clicked.
    [Fact]
    public void LabelWithOnClick_InDialog_FiresOnClick()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative position
        var label = new UILabel("Click me", new Vector2(20, 220));
        label.Size = new Vector2(100, 20);
        bool clicked = false;
        label.OnClick += () => clicked = true;
        dialog.AddChild(label);
        canvas.Add(dialog);

        var screenPos = dialog.Position + label.Position + new Vector2(5, 5);
        SetMousePosition(screenPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    // Fix 4: A UIImage with OnClick inside a UIDialog should fire when clicked.
    [Fact]
    public void ImageWithOnClick_InDialog_FiresOnClick()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative position
        var image = new UIImage(null, new Vector2(20, 220), new Vector2(80, 40));
        bool clicked = false;
        image.OnClick += () => clicked = true;
        dialog.AddChild(image);
        canvas.Add(dialog);

        var screenPos = dialog.Position + image.Position + new Vector2(5, 5);
        SetMousePosition(screenPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    // Fix 5: A tooltip on a button embedded in a UIDialog should become active
    // when the mouse hovers over that button.
    [Fact]
    public void Tooltip_OnDialogButton_BecomesActive_OnHover()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        var btn = dialog.AddButton("OK", () => { });
        btn.Tooltip = new UITooltip("Hint text");
        canvas.Add(dialog);

        // btn.Position is dialog-relative; hover in screen space = dialog.Position + btn.Position + center
        SetMousePosition(dialog.Position + btn.Position + btn.Size / 2);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        btn.Tooltip.Should().NotBeNull();
        // After one frame of hovering, the tooltip owner should be set (hover started).
        // We verify by checking that a second hover call doesn't throw and that
        // the tooltip started accumulating its delay.
        var act = () =>
        {
            canvas.ProcessMouseInput(_input, false);
            canvas.Update(1.0f);
        };
        act.Should().NotThrow();
    }

    // Fix 6: Scrollbar drag in a UIScrollView inside a UIDialog must not update
    // a second, unrelated UIScrollView in the same dialog.
    [Fact]
    public void ScrollbarDrag_InDialog_DoesNotAffectOtherScrollViews()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(600, 500));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative: two scroll views side by side
        var sv1 = new UIScrollView(new Vector2(10, 150), new Vector2(100, 150))
        {
            ContentHeight = 400,
            ShowVerticalScrollbar = true
        };
        var sv2 = new UIScrollView(new Vector2(130, 150), new Vector2(100, 150))
        {
            ContentHeight = 400,
            ShowVerticalScrollbar = true
        };
        dialog.AddChild(sv1);
        dialog.AddChild(sv2);
        canvas.Add(dialog);

        // Screen position of sv1's scrollbar right edge
        var scrollbarX = dialog.Position.X + sv1.Position.X + sv1.Size.X - sv1.ScrollbarWidth / 2;
        var scrollbarY = dialog.Position.Y + sv1.Position.Y + sv1.Size.Y / 2;

        SetMousePosition(new Vector2(scrollbarX, scrollbarY));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        var sv2OffsetBefore = sv2.ScrollOffset;

        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        SetMousePosition(new Vector2(scrollbarX, scrollbarY + 20));
        canvas.ProcessMouseInput(_input, false);

        sv2.ScrollOffset.Should().Be(sv2OffsetBefore);
    }

    #endregion

    #region Bug fixes and new feature tests

    // Bug 1: _activeDialogDropdown must be closed when the last dialog becomes hidden.
    [Fact]
    public void ActiveDialogDropdown_IsClosedWhenDialogBecomesInactive()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(600, 400));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative position
        var dropdown = new UIDropdown(new Vector2(20, 220), new Vector2(150, 30));
        dropdown.Items.AddRange(["One", "Two", "Three"]);
        dialog.AddChild(dropdown);
        canvas.Add(dialog);

        // Open the dropdown — screen click = dialog.Position + dropdown.Position + a few pixels
        var clickPos = dialog.Position + dropdown.Position + new Vector2(10, 5);
        SetMousePosition(clickPos);
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        dropdown.IsExpanded.Should().BeTrue();

        // Hide the dialog — next mouse frame should close the dropdown.
        dialog.Visible = false;
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        dropdown.IsExpanded.Should().BeFalse();
    }

    // Bug 2: Escape should blur a focused UITextInput when no modal dialog is active.
    [Fact]
    public void ProcessKeyboardInput_Escape_BlursFocusedTextInput()
    {
        var canvas = CreateCanvas();
        var textInput = new UITextInput(new Vector2(100, 100), new Vector2(200, 30));
        canvas.Add(textInput);

        // Focus via mouse click.
        SetMousePosition(new Vector2(110, 110));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        textInput.IsFocused.Should().BeTrue();

        // Press Escape — should defocus.
        _input.IsKeyPressed(Key.Escape).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        textInput.IsFocused.Should().BeFalse();
    }

    // Bug 2: Escape should blur a focused UITextArea when no modal dialog is active.
    [Fact]
    public void ProcessKeyboardInput_Escape_BlursFocusedTextArea()
    {
        var canvas = CreateCanvas();
        var textArea = new UITextArea(new Vector2(100, 100), new Vector2(200, 80));
        canvas.Add(textArea);

        SetMousePosition(new Vector2(110, 110));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        textArea.IsFocused.Should().BeTrue();

        _input.IsKeyPressed(Key.Escape).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        textArea.IsFocused.Should().BeFalse();
    }

    // UITabContainer: Tab cycling should eventually land on the tab container itself.
    [Fact]
    public void ProcessKeyboardInput_Tab_CyclesFocusOntoTabContainer()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 300));
        tab.AddTab("A");
        tab.AddTab("B");
        canvas.Add(tab);

        bool focusGained = false;
        tab.OnFocusGained += () => focusGained = true;

        // Cycle Tab until we land on the tab container (max one iteration for a single widget).
        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        focusGained.Should().BeTrue();
        canvas.FocusedWidget.Should().Be(tab);
    }

    // UITabContainer: SetFocus should succeed for a UITabContainer.
    [Fact]
    public void SetFocus_OnTabContainer_ReturnsTrue()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 300));
        tab.AddTab("A");
        canvas.Add(tab);

        canvas.SetFocus(tab).Should().BeTrue();
        canvas.FocusedWidget.Should().Be(tab);
    }

    // UITabContainer: Left/Right arrows should switch tabs once the container is focused via Tab.
    [Fact]
    public void ProcessKeyboardInput_ArrowKeys_SwitchTabsOnFocusedTabContainer()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 300));
        tab.AddTab("A");
        tab.AddTab("B");
        canvas.Add(tab);

        canvas.SetFocus(tab);

        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        tab.SelectedTabIndex.Should().Be(1);
    }

    // UIScrollView: Page Down should scroll by one page height.
    [Fact]
    public void ProcessKeyboardInput_PageDown_ScrollsOneFocusedScrollViewPage()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 100))
        {
            ContentHeight = 400,
            ShowVerticalScrollbar = true
        };
        canvas.Add(sv);
        canvas.SetFocus(sv);

        _input.IsKeyPressed(Key.PageDown).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        sv.ScrollOffset.Y.Should().BeApproximately(100f, 0.01f);
    }

    // UIScrollView: Page Up should scroll back by one page height.
    [Fact]
    public void ProcessKeyboardInput_PageUp_ScrollsBackOneFocusedScrollViewPage()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 100))
        {
            ContentHeight = 400,
            ShowVerticalScrollbar = true
        };
        canvas.Add(sv);
        canvas.SetFocus(sv);

        sv.ScrollOffset = new Vector2(0, 200f);

        _input.IsKeyPressed(Key.PageUp).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        sv.ScrollOffset.Y.Should().BeApproximately(100f, 0.01f);
    }

    // UIScrollView: Home should scroll to top.
    [Fact]
    public void ProcessKeyboardInput_Home_ScrollsToTopOfFocusedScrollView()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 100))
        {
            ContentHeight = 400,
            ShowVerticalScrollbar = true
        };
        canvas.Add(sv);
        canvas.SetFocus(sv);

        sv.ScrollOffset = new Vector2(0, 200f);

        _input.IsKeyPressed(Key.Home).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        sv.ScrollOffset.Y.Should().Be(0f);
    }

    // UIScrollView: End should scroll to bottom.
    [Fact]
    public void ProcessKeyboardInput_End_ScrollsToBottomOfFocusedScrollView()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 100))
        {
            ContentHeight = 400,
            ShowVerticalScrollbar = true
        };
        canvas.Add(sv);
        canvas.SetFocus(sv);

        _input.IsKeyPressed(Key.End).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        sv.ScrollOffset.Y.Should().BeApproximately(300f, 0.01f);
    }

    #endregion

    #region UIScrollView tab-cycling (Bug #3)

    [Fact]
    public void Tab_CyclesFocus_ToScrollView()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 100))
        {
            ContentHeight = 200,
            ShowVerticalScrollbar = true
        };
        canvas.Add(sv);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        canvas.FocusedWidget.Should().Be(sv);
    }

    [Fact]
    public void Tab_ScrollViewWithFocusableChild_FocusesChildNotScrollView()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200));
        var btn = new UIButton("B", new Vector2(0, 0), new Vector2(80, 30));
        sv.AddChild(btn);
        canvas.Add(sv);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        canvas.FocusedWidget.Should().Be(btn);
        sv.IsFocused.Should().BeFalse();
    }

    #endregion

    #region UITextInput ReadOnly and CharacterFilter

    [Fact]
    public void UITextInput_ReadOnly_BlocksTyping()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            ReadOnly = true,
            Text = "initial"
        };
        _input.GetTextInput().Returns("x");
        input.SetFocused(true, _input);
        input.HandleTextInput(_input);

        input.Text.Should().Be("initial");
    }

    [Fact]
    public void UITextInput_ReadOnly_AllowsCopy()
    {
        string copied = string.Empty;
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            ReadOnly = true,
            Text = "hello"
        };
        input.SetFocused(true, _input);
        _input.IsKeyDown(Key.LeftControl).Returns(true);
        _input.IsKeyPressed(Key.A).Returns(true);
        input.HandleTextInput(_input);

        _input.IsKeyPressed(Key.A).Returns(false);
        _input.IsKeyPressed(Key.C).Returns(true);
        _input.When(x => x.SetClipboardText(Arg.Any<string>())).Do(ci => copied = ci.Arg<string>());
        input.HandleTextInput(_input);

        copied.Should().Be("hello");
    }

    [Fact]
    public void UITextInput_ReadOnly_BlocksBackspace()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            ReadOnly = true,
            Text = "abc"
        };
        input.SetFocused(true, _input);
        _input.IsBackspacePressed().Returns(true);
        input.HandleTextInput(_input);

        input.Text.Should().Be("abc");
    }

    [Fact]
    public void UITextInput_CharacterFilter_RejectsNonMatchingChars()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            CharacterFilter = char.IsDigit
        };
        input.SetFocused(true, _input);
        _input.GetTextInput().Returns("a5b3");
        input.HandleTextInput(_input);

        input.Text.Should().Be("53");
    }

    [Fact]
    public void UITextInput_CharacterFilter_AppliedToPaste()
    {
        var input = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            CharacterFilter = char.IsDigit
        };
        input.SetFocused(true, _input);
        _input.IsKeyDown(Key.LeftControl).Returns(true);
        _input.IsKeyPressed(Key.V).Returns(true);
        _input.GetClipboardText().Returns("a1b2c3");
        input.HandleTextInput(_input);

        input.Text.Should().Be("123");
    }

    #endregion

    #region UITextArea ReadOnly, CharacterFilter, and TabInsertsTab

    [Fact]
    public void UITextArea_ReadOnly_BlocksTyping()
    {
        var ta = new UITextArea(new Vector2(0, 0), new Vector2(200, 100))
        {
            ReadOnly = true,
            Text = "initial"
        };
        ta.SetFocused(true, _input);
        _input.GetTextInput().Returns("x");
        ta.HandleTextInput(_input);

        ta.Text.Should().Be("initial");
    }

    [Fact]
    public void UITextArea_ReadOnly_BlocksBackspace()
    {
        var ta = new UITextArea(new Vector2(0, 0), new Vector2(200, 100))
        {
            ReadOnly = true,
            Text = "abc"
        };
        ta.SetFocused(true, _input);
        _input.IsBackspacePressed().Returns(true);
        ta.HandleTextInput(_input);

        ta.Text.Should().Be("abc");
    }

    [Fact]
    public void UITextArea_CharacterFilter_RejectsNonMatchingChars()
    {
        var ta = new UITextArea(new Vector2(0, 0), new Vector2(200, 100))
        {
            CharacterFilter = char.IsLetter
        };
        ta.SetFocused(true, _input);
        _input.GetTextInput().Returns("h3ll0");
        ta.HandleTextInput(_input);

        ta.Text.Should().Be("hll");
    }

    [Fact]
    public void UITextArea_TabInsertsTab_InsertsTabCharacter()
    {
        var canvas = CreateCanvas();
        var ta = new UITextArea(new Vector2(0, 0), new Vector2(200, 100))
        {
            TabInsertsTab = true
        };
        canvas.Add(ta);

        SetMousePosition(new Vector2(50, 50));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        ta.Text.Should().Be("\t");
    }

    [Fact]
    public void UITextArea_TabInsertsTab_False_CyclesFocus()
    {
        var canvas = CreateCanvas();
        var ta = new UITextArea(new Vector2(0, 0), new Vector2(200, 100))
        {
            TabInsertsTab = false
        };
        var btn = new UIButton("B", new Vector2(300, 0), new Vector2(80, 30));
        canvas.Add(ta);
        canvas.Add(btn);

        SetMousePosition(new Vector2(50, 50));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        ta.Text.Should().Be(string.Empty);
        canvas.FocusedWidget.Should().NotBe(ta);
    }

    [Fact]
    public void UITextArea_TabInsertsTab_ReadOnly_DoesNotInsertTab()
    {
        var ta = new UITextArea(new Vector2(0, 0), new Vector2(200, 100))
        {
            TabInsertsTab = true,
            ReadOnly = true
        };
        ta.InsertTab();

        ta.Text.Should().Be(string.Empty);
    }

    #endregion

    #region UIDropdown disabled items

    [Fact]
    public void UIDropdown_DisabledItem_CannotBeSelectedByClick()
    {
        var canvas = CreateCanvas();
        var dd = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dd.Items.AddRange(new[] { "A", "B", "C" });
        dd.SetItemEnabled(1, false);
        canvas.Add(dd);

        // Open the dropdown
        SetMousePosition(new Vector2(75, 15));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        dd.IsExpanded.Should().BeTrue();

        // Click the disabled second item (y=30 is header, y=60 is item index 1)
        SetMousePosition(new Vector2(75, 60));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        dd.SelectedIndex.Should().NotBe(1);
    }

    [Fact]
    public void UIDropdown_IsItemEnabled_DefaultsToTrue()
    {
        var dd = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dd.Items.Add("A");

        dd.IsItemEnabled(0).Should().BeTrue();
    }

    [Fact]
    public void UIDropdown_SetItemEnabled_False_ThenTrue_Roundtrips()
    {
        var dd = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dd.Items.Add("A");
        dd.SetItemEnabled(0, false);
        dd.IsItemEnabled(0).Should().BeFalse();
        dd.SetItemEnabled(0, true);
        dd.IsItemEnabled(0).Should().BeTrue();
    }

    [Fact]
    public void UIDropdown_RemoveItem_ShiftsDisabledIndices()
    {
        var dd = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dd.Items.AddRange(new[] { "A", "B", "C" });
        dd.SetItemEnabled(2, false);
        dd.RemoveItem("A");

        dd.IsItemEnabled(0).Should().BeTrue();
        dd.IsItemEnabled(1).Should().BeFalse();
    }

    [Fact]
    public void UIDropdown_ClearItems_ClearsDisabledIndices()
    {
        var dd = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dd.Items.Add("A");
        dd.SetItemEnabled(0, false);
        dd.ClearItems();
        dd.Items.Add("B");

        dd.IsItemEnabled(0).Should().BeTrue();
    }

    [Fact]
    public void UIDropdown_KeyboardNavigate_SkipsDisabledItem()
    {
        var dd = new UIDropdown(new Vector2(0, 0), new Vector2(150, 30));
        dd.Items.AddRange(new[] { "A", "B", "C" });
        dd.SetItemEnabled(1, false);

        dd.Toggle();
        dd.NavigateItem(1);
        dd.NavigateItem(1);

        dd.ConfirmKeyboardSelection();

        dd.SelectedIndex.Should().Be(2);
    }

    #endregion

    #region BringToFront nested containers (Bug #2)

    [Fact]
    public void BringToFront_NestedInPanel_BringsOwnerToFront()
    {
        var canvas = CreateCanvas();
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(200, 200));
        var btn = new UIButton("B", new Vector2(0, 0), new Vector2(80, 30));
        panel.AddChild(btn);
        var other = new UIButton("O", new Vector2(300, 0), new Vector2(80, 30));
        canvas.Add(panel);
        canvas.Add(other);

        canvas.BringToFront(btn);

        canvas.Components[^1].Should().Be(panel);
    }

    [Fact]
    public void BringToFront_DirectChild_StillWorks()
    {
        var canvas = CreateCanvas();
        var btn1 = new UIButton("A", new Vector2(0, 0), new Vector2(80, 30));
        var btn2 = new UIButton("B", new Vector2(0, 0), new Vector2(80, 30));
        canvas.Add(btn1);
        canvas.Add(btn2);

        canvas.BringToFront(btn1);

        canvas.Components[^1].Should().Be(btn1);
    }

    [Fact]
    public void BringToFront_ComponentNotOnCanvas_NoOp()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("A", new Vector2(0, 0), new Vector2(80, 30));
        var other = new UIButton("B", new Vector2(0, 0), new Vector2(80, 30));
        canvas.Add(btn);

        var act = () => canvas.BringToFront(other);
        act.Should().NotThrow();
        canvas.Components.Should().HaveCount(1);
    }

    #endregion

    #region Bug 1 – FocusedWidget populated after mouse-click on text fields

    [Fact]
    public void FocusedWidget_IsSet_WhenTextInputClickedWithMouse()
    {
        var canvas = CreateCanvas();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        canvas.Add(ti);

        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        canvas.FocusedWidget.Should().Be(ti);
    }

    [Fact]
    public void FocusedWidget_IsSet_WhenTextAreaClickedWithMouse()
    {
        var canvas = CreateCanvas();
        var ta = new UITextArea(new Vector2(0, 0), new Vector2(200, 100));
        canvas.Add(ta);

        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        canvas.FocusedWidget.Should().Be(ta);
    }

    [Fact]
    public void FocusedWidget_IsConsistent_AfterMouseAndTabFocus()
    {
        var canvas = CreateCanvas();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        canvas.Add(ti);

        SetMousePosition(new Vector2(10, 10));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        var mouseWidget = canvas.FocusedWidget;

        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);
        _input.IsKeyPressed(Key.Tab).Returns(false);

        var tabWidget = canvas.FocusedWidget;

        mouseWidget.Should().Be(ti);
        tabWidget.Should().Be(ti);
    }

    #endregion

    #region Bug 2 – UILabel / UIImage OnClick in dialog scroll view

    [Fact]
    public void UILabel_OnClick_FiresInsideDialogScrollView()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative sv position
        var svRelPos = new Vector2(10, 80);
        var sv = new UIScrollView(svRelPos, new Vector2(360, 180))
        {
            ContentHeight = 200
        };

        // Children in a UIScrollView use content-relative positions (0,0 = top-left of sv).
        var label = new UILabel("click me", new Vector2(10, 10));
        label.Size = new Vector2(80, 20);
        bool clicked = false;
        label.OnClick += () => clicked = true;
        sv.AddChild(label);
        dialog.AddChild(sv);
        canvas.Add(dialog);

        // Screen position = dialog.Position + sv dialog-relative pos + label pos + inset
        var labelScreenPos = dialog.Position + svRelPos + label.Position + new Vector2(5, 5);

        SetMousePosition(labelScreenPos);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void UIImage_OnClick_FiresInsideDialogScrollView()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        // dialog-relative sv position
        var svRelPos = new Vector2(10, 80);
        var sv = new UIScrollView(svRelPos, new Vector2(360, 180))
        {
            ContentHeight = 200
        };

        var image = new UIImage(null, new Vector2(10, 10), new Vector2(80, 40));
        bool clicked = false;
        image.OnClick += () => clicked = true;
        sv.AddChild(image);
        dialog.AddChild(sv);
        canvas.Add(dialog);

        // Screen position = dialog.Position + sv dialog-relative pos + image pos + inset
        var imgScreenPos = dialog.Position + svRelPos + image.Position + new Vector2(5, 5);

        SetMousePosition(imgScreenPos);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeTrue();
    }

    #endregion

    #region Bug 3 – Remove clears stale _focusedWidget for container children

    [Fact]
    public void Remove_Panel_ClearsFocusOnContainedButton()
    {
        var canvas = CreateCanvas();
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(200, 200));
        var btn = new UIButton("B", new Vector2(10, 10), new Vector2(80, 30));
        panel.AddChild(btn);
        canvas.Add(panel);

        canvas.SetFocus(btn);
        canvas.FocusedWidget.Should().Be(btn);

        canvas.Remove(panel);

        canvas.FocusedWidget.Should().BeNull();
        btn.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void Remove_ScrollView_ClearsFocusOnContainedTextInput()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(300, 200))
        {
            ContentHeight = 400
        };
        var ti = new UITextInput(new Vector2(10, 10), new Vector2(200, 30));
        sv.AddChild(ti);
        canvas.Add(sv);

        canvas.SetFocus(ti);
        canvas.FocusedWidget.Should().Be(ti);

        canvas.Remove(sv);

        canvas.FocusedWidget.Should().BeNull();
    }

    [Fact]
    public void Remove_TabContainer_ClearsFocusOnContainedCheckbox()
    {
        var canvas = CreateCanvas();
        var tab = new UITabContainer(new Vector2(0, 0), new Vector2(400, 200));
        tab.AddTab("Tab1");
        var cb = new UICheckbox("check", new Vector2(10, 10));
        tab.AddComponentToTab(0, cb);
        canvas.Add(tab);

        canvas.SetFocus(cb);
        canvas.FocusedWidget.Should().Be(cb);

        canvas.Remove(tab);

        canvas.FocusedWidget.Should().BeNull();
        cb.IsFocused.Should().BeFalse();
    }

    #endregion

    #region ScreenSize propagation

    [Fact]
    public void ScreenSize_Setter_PropagatesDropdownScreenHeightIntoNestedPanel()
    {
        var canvas = CreateCanvas();
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(400, 300));
        var dropdown = new UIDropdown(new Vector2(10, 10), new Vector2(200, 30));
        panel.AddChild(dropdown);
        canvas.Add(panel);

        canvas.ScreenSize = new Vector2(1920, 1080);

        dropdown.ScreenHeight.Should().Be(1080f);
    }

    [Fact]
    public void ScreenSize_Setter_PropagatesTabContainerScreenSizeIntoNestedPanel()
    {
        var canvas = CreateCanvas();
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(400, 300));
        var tab = new UITabContainer(new Vector2(10, 10), new Vector2(380, 270));
        panel.AddChild(tab);
        canvas.Add(panel);

        canvas.ScreenSize = new Vector2(1920, 1080);

        tab.ScreenSize.Should().Be(new Vector2(1920, 1080));
    }

    [Fact]
    public void ScreenSize_Setter_PropagatesDropdownInsideScrollView()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(400, 300)) { ContentHeight = 600 };
        var dropdown = new UIDropdown(new Vector2(10, 10), new Vector2(200, 30));
        sv.AddChild(dropdown);
        canvas.Add(sv);

        canvas.ScreenSize = new Vector2(1920, 1080);

        dropdown.ScreenHeight.Should().Be(1080f);
    }

    [Fact]
    public void Add_PropagatesDropdownScreenHeightIntoNestedScrollView()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1920, 1080);

        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(400, 300)) { ContentHeight = 600 };
        var dropdown = new UIDropdown(new Vector2(10, 10), new Vector2(200, 30));
        sv.AddChild(dropdown);
        canvas.Add(sv);

        dropdown.ScreenHeight.Should().Be(1080f);
    }

    [Fact]
    public void Add_PropagatesDropdownScreenHeightIntoNestedStackPanel()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1920, 1080);

        var sp = new UIStackPanel(new Vector2(0, 0));
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(200, 30));
        sp.AddChild(dropdown);
        canvas.Add(sp);

        dropdown.ScreenHeight.Should().Be(1080f);
    }

    [Fact]
    public void ScreenSize_Setter_PropagatesDropdownInsideGrid()
    {
        var canvas = CreateCanvas();
        var grid = new UIGrid(new Vector2(0, 0));
        var dropdown = new UIDropdown(new Vector2(0, 0), new Vector2(200, 30));
        grid.AddChild(dropdown);
        canvas.Add(grid);

        canvas.ScreenSize = new Vector2(2560, 1440);

        dropdown.ScreenHeight.Should().Be(1440f);
    }

    #endregion

    #region Tooltip blocked by modal dialog

    [Fact]
    public void HandleTooltips_WithActiveDialog_DoesNotShowTooltipOnBackgroundButton()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1280, 720);

        // Background button with a tooltip, positioned where the mouse will hover.
        var bgButton = new UIButton("BG", new Vector2(100, 100), new Vector2(200, 50));
        bgButton.Tooltip = new UITooltip("background tip");
        canvas.Add(bgButton);

        // Modal dialog covering the background button area.
        var dialog = new UIDialog("Modal", "Content", new Vector2(400, 300));
        canvas.Add(dialog);

        // Hover over the background button's position.
        SetMousePosition(new Vector2(200, 125));
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);
        canvas.Update(2.0f); // exceed tooltip show delay

        // Tooltip on the background button must not become visible through the dialog.
        bgButton.Tooltip.Visible.Should().BeFalse();
    }

    [Fact]
    public void HandleTooltips_WithActiveDialog_ShowsTooltipOnDialogChild()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1280, 720);

        var dialog = new UIDialog("Modal", "Content", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        var btn = dialog.AddButton("OK", () => { });
        btn.Tooltip = new UITooltip("dialog tip") { ShowDelay = 0f };
        canvas.Add(dialog);

        // Hover over the dialog button in screen space.
        var btnScreenCenter = dialog.Position + btn.Position + btn.Size / 2;
        SetMousePosition(btnScreenCenter);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);
        canvas.Update(0.1f);

        // The tooltip belonging to the dialog button should be allowed to show.
        btn.Tooltip.Visible.Should().BeTrue();
    }

    #endregion

    #region ScrollFocusedWidgetIntoView — UIDialog support

    [Fact]
    public void ProcessKeyboardInput_Tab_InsideDialogScrollView_ScrollsWidgetIntoView()
    {
        var canvas = CreateCanvas();

        var dialog = new UIDialog("Test", string.Empty, new Vector2(300, 200));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        var scrollView = new UIScrollView(new Vector2(0, 0), new Vector2(300, 100))
        {
            ContentHeight = 200f,
            ShowVerticalScrollbar = false
        };

        var btn1 = new UIButton("First", new Vector2(0, 0), new Vector2(100, 30));
        var btn2 = new UIButton("Second", new Vector2(0, 150), new Vector2(100, 30));
        scrollView.AddChild(btn1);
        scrollView.AddChild(btn2);

        dialog.AddChild(scrollView);
        canvas.Add(dialog);

        // Focus btn1 first so Tab cycles to btn2 (which is scrolled out of view).
        canvas.SetFocus(btn1);
        scrollView.ScrollOffset = Vector2.Zero;

        _input.IsKeyPressed(Key.Tab).Returns(true);
        _input.IsKeyDown(Key.LeftShift).Returns(false);
        _input.IsKeyDown(Key.RightShift).Returns(false);

        canvas.ProcessKeyboardInput(_input, false);

        // btn2 is at Y=150 in a 100-tall scroll view — it should now be visible.
        scrollView.ScrollOffset.Y.Should().BeGreaterThan(0f);
    }

    #endregion

    #region FindByName

    [Fact]
    public void FindByName_TopLevelComponent_ReturnsIt()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("hello", Vector2.Zero) { Name = "myLabel" };
        canvas.Add(label);

        canvas.FindByName("myLabel").Should().BeSameAs(label);
    }

    [Fact]
    public void FindByName_UnknownName_ReturnsNull()
    {
        var canvas = CreateCanvas();
        canvas.Add(new UILabel("x", Vector2.Zero));

        canvas.FindByName("nope").Should().BeNull();
    }

    [Fact]
    public void FindByName_NestedInPanel_ReturnsIt()
    {
        var canvas = CreateCanvas();
        var panel = new UIPanel(Vector2.Zero, new Vector2(200, 200));
        var btn = new UIButton("Click", Vector2.Zero, new Vector2(100, 30)) { Name = "okBtn" };
        panel.AddChild(btn);
        canvas.Add(panel);

        canvas.FindByName("okBtn").Should().BeSameAs(btn);
    }

    [Fact]
    public void FindByName_NestedInScrollView_ReturnsIt()
    {
        var canvas = CreateCanvas();
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 100));
        var check = new UICheckbox("opt", Vector2.Zero) { Name = "chk" };
        sv.AddChild(check);
        canvas.Add(sv);

        canvas.FindByName("chk").Should().BeSameAs(check);
    }

    [Fact]
    public void FindByName_NestedInStackPanel_ReturnsIt()
    {
        var canvas = CreateCanvas();
        var sp = new UIStackPanel(Vector2.Zero);
        var btn = new UIButton("Go", Vector2.Zero, new Vector2(100, 30)) { Name = "go" };
        sp.AddChild(btn);
        canvas.Add(sp);

        canvas.FindByName("go").Should().BeSameAs(btn);
    }

    [Fact]
    public void FindByName_NestedInDialog_ReturnsChild()
    {
        var canvas = CreateCanvas();
        var dialog = new UIDialog("Title", "Msg", new Vector2(300, 200));
        var label = new UILabel("info", Vector2.Zero) { Name = "info" };
        dialog.AddChild(label);
        canvas.Add(dialog);

        canvas.FindByName("info").Should().BeSameAs(label);
    }

    [Fact]
    public void FindByName_TypedOverload_CastsCorrectly()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("X", Vector2.Zero, new Vector2(50, 30)) { Name = "btn" };
        canvas.Add(btn);

        canvas.FindByName<UIButton>("btn").Should().BeSameAs(btn);
    }

    [Fact]
    public void FindByName_TypedOverload_WrongType_ReturnsNull()
    {
        var canvas = CreateCanvas();
        var btn = new UIButton("X", Vector2.Zero, new Vector2(50, 30)) { Name = "btn" };
        canvas.Add(btn);

        canvas.FindByName<UILabel>("btn").Should().BeNull();
    }

    #endregion

    #region Dropdown inside ScrollView overlay

    [Fact]
    public void Dropdown_InsideScrollView_SuppressListRender_SetOnOpen()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);

        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(300, 200));
        var dd = new UIDropdown(new Vector2(10, 10), new Vector2(120, 24));
        dd.AddItem("Alpha");
        dd.AddItem("Beta");
        sv.AddChild(dd);
        canvas.Add(sv);

        SetMousePosition(new Vector2(60, 22));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        dd.IsExpanded.Should().BeTrue();
        dd.SuppressListRender.Should().BeTrue("expanded dropdown inside scroll view must suppress its own list render");
    }

    [Fact]
    public void Dropdown_InsideScrollView_SuppressListRender_ClearedOnClose()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);

        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(300, 200));
        var dd = new UIDropdown(new Vector2(10, 10), new Vector2(120, 24));
        dd.AddItem("Alpha");
        sv.AddChild(dd);
        canvas.Add(sv);

        // Open.
        SetMousePosition(new Vector2(60, 22));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        dd.IsExpanded.Should().BeTrue();

        // Click elsewhere to close.
        SetMousePosition(new Vector2(700, 500));
        canvas.ProcessMouseInput(_input, false);

        dd.IsExpanded.Should().BeFalse();
        dd.SuppressListRender.Should().BeFalse("SuppressListRender must be cleared when the dropdown closes");
    }

    [Fact]
    public void Dropdown_NotInsideScrollView_SuppressListRender_NotSet()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);

        var dd = new UIDropdown(new Vector2(10, 10), new Vector2(120, 24));
        dd.AddItem("Alpha");
        canvas.Add(dd);

        SetMousePosition(new Vector2(60, 22));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        dd.IsExpanded.Should().BeTrue();
        dd.SuppressListRender.Should().BeFalse("top-level dropdown should not use overlay path");
    }

    #endregion

    #region ZOrder

    [Fact]
    public void ProcessMouseInput_OverlappingButtons_HigherZOrderWins_RegardlessOfAddOrder()
    {
        var canvas = CreateCanvas();
        var back = new UIButton("Back", new Vector2(0f, 0f), new Vector2(100f, 100f));
        var front = new UIButton("Front", new Vector2(0f, 0f), new Vector2(100f, 100f));
        bool backClicked = false;
        bool frontClicked = false;
        back.OnClick += () => backClicked = true;
        front.OnClick += () => frontClicked = true;
        front.ZOrder = 10;

        // Add front first — without ZOrder, back (added last) would win.
        canvas.Add(front);
        canvas.Add(back);

        SimulateButtonClick(canvas, front);

        frontClicked.Should().BeTrue();
        backClicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessMouseInput_OverlappingButtons_LowerZOrderLoses()
    {
        var canvas = CreateCanvas();
        var bottom = new UIButton("Bottom", new Vector2(0f, 0f), new Vector2(100f, 100f));
        var top = new UIButton("Top", new Vector2(0f, 0f), new Vector2(100f, 100f));
        bool bottomClicked = false;
        bool topClicked = false;
        bottom.OnClick += () => bottomClicked = true;
        top.OnClick += () => topClicked = true;
        bottom.ZOrder = 0;
        top.ZOrder = 5;
        canvas.Add(bottom);
        canvas.Add(top);

        SimulateButtonClick(canvas, top);

        topClicked.Should().BeTrue();
        bottomClicked.Should().BeFalse();
    }

    [Fact]
    public void ZOrder_DefaultIsZero()
    {
        var btn = new UIButton("B", Vector2.Zero, new Vector2(100f, 40f));
        btn.ZOrder.Should().Be(0);
    }

    [Fact]
    public void ZOrder_EqualZOrder_FallsBackToAddOrder_LastAddedWins()
    {
        var canvas = CreateCanvas();
        var first = new UIButton("First", new Vector2(0f, 0f), new Vector2(100f, 100f));
        var second = new UIButton("Second", new Vector2(0f, 0f), new Vector2(100f, 100f));
        bool firstClicked = false;
        bool secondClicked = false;
        first.OnClick += () => firstClicked = true;
        second.OnClick += () => secondClicked = true;
        // Both have default ZOrder = 0; last-added should still win.
        canvas.Add(first);
        canvas.Add(second);

        SimulateButtonClick(canvas, second);

        secondClicked.Should().BeTrue();
        firstClicked.Should().BeFalse();
    }

    #endregion

    #region UIContextMenu

    private UIContextMenu MakeMenu() =>
        new UIContextMenu { Width = 120f, ItemHeight = 28f }
            .AddItem("Cut")
            .AddItem("Copy")
            .AddItem("Paste");

    [Fact]
    public void ShowContextMenu_SetsActiveContextMenu()
    {
        var canvas = CreateCanvas();
        var menu = MakeMenu();

        canvas.ShowContextMenu(menu, new Vector2(100f, 100f));

        canvas.ActiveContextMenu.Should().BeSameAs(menu);
    }

    [Fact]
    public void ShowContextMenu_ClampsPositionToScreenEdge()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800f, 600f);
        var menu = new UIContextMenu { Width = 120f, ItemHeight = 28f };
        menu.AddItem("A");

        canvas.ShowContextMenu(menu, new Vector2(750f, 590f));

        menu.Position.X.Should().BeLessThanOrEqualTo(800f - 120f);
        menu.Position.Y.Should().BeLessThanOrEqualTo(600f - 28f);
    }

    [Fact]
    public void ShowContextMenu_ReplacesExistingMenu()
    {
        var canvas = CreateCanvas();
        var first = MakeMenu();
        var second = MakeMenu();
        bool firstClosed = false;
        first.OnClosed += () => firstClosed = true;

        canvas.ShowContextMenu(first, Vector2.Zero);
        canvas.ShowContextMenu(second, new Vector2(10f, 10f));

        firstClosed.Should().BeTrue();
        canvas.ActiveContextMenu.Should().BeSameAs(second);
    }

    [Fact]
    public void CloseContextMenu_ClearsActiveMenu()
    {
        var canvas = CreateCanvas();
        canvas.ShowContextMenu(MakeMenu(), Vector2.Zero);

        canvas.CloseContextMenu();

        canvas.ActiveContextMenu.Should().BeNull();
    }

    [Fact]
    public void CloseContextMenu_FiresOnClosedEvent()
    {
        var canvas = CreateCanvas();
        var menu = MakeMenu();
        bool closed = false;
        menu.OnClosed += () => closed = true;
        canvas.ShowContextMenu(menu, Vector2.Zero);

        canvas.CloseContextMenu();

        closed.Should().BeTrue();
    }

    [Fact]
    public void ProcessMouseInput_ContextMenuOpen_ClickOutside_ClosesMenu()
    {
        var canvas = CreateCanvas();
        var menu = MakeMenu();
        canvas.ShowContextMenu(menu, new Vector2(200f, 200f));

        SetMousePosition(new Vector2(10f, 10f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        canvas.ActiveContextMenu.Should().BeNull();
    }

    [Fact]
    public void ProcessMouseInput_ContextMenuOpen_ClickEnabledItem_FiresOnItemSelectedAndCloses()
    {
        var canvas = CreateCanvas();
        var menu = new UIContextMenu { Width = 120f, ItemHeight = 28f };
        menu.AddItem("Copy");
        int? selectedIndex = null;
        string? selectedLabel = null;
        menu.OnItemSelected += (i, l) => { selectedIndex = i; selectedLabel = l; };
        canvas.ShowContextMenu(menu, new Vector2(0f, 0f));

        SetMousePosition(new Vector2(10f, 10f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        selectedIndex.Should().Be(0);
        selectedLabel.Should().Be("Copy");
        canvas.ActiveContextMenu.Should().BeNull();
    }

    [Fact]
    public void ProcessMouseInput_ContextMenuOpen_ClickDisabledItem_DoesNotFireAndKeepsMenuOpen()
    {
        var canvas = CreateCanvas();
        var menu = new UIContextMenu { Width = 120f, ItemHeight = 28f };
        menu.AddItem("Grayed", enabled: false);
        bool fired = false;
        menu.OnItemSelected += (_, _) => fired = true;
        canvas.ShowContextMenu(menu, new Vector2(0f, 0f));

        SetMousePosition(new Vector2(10f, 10f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        fired.Should().BeFalse();
        canvas.ActiveContextMenu.Should().NotBeNull();
    }

    [Fact]
    public void ProcessMouseInput_ContextMenuOpen_ConsumesInputSoButtonBehindDoesNotFire()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("B", new Vector2(0f, 0f), new Vector2(200f, 200f));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);

        var menu = MakeMenu();
        canvas.ShowContextMenu(menu, new Vector2(10f, 10f));

        SetMousePosition(new Vector2(15f, 15f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseReleased();
        canvas.ProcessMouseInput(_input, false);

        clicked.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyboardInput_ContextMenuOpen_EscapeClosesMenu()
    {
        var canvas = CreateCanvas();
        canvas.ShowContextMenu(MakeMenu(), Vector2.Zero);

        _input.IsKeyPressed(Key.Escape).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        canvas.ActiveContextMenu.Should().BeNull();
    }

    [Fact]
    public void ProcessKeyboardInput_ContextMenuOpen_ConsumesAllKeyboardInput()
    {
        var canvas = CreateCanvas();
        var button = new UIButton("B", new Vector2(0f, 0f), new Vector2(100f, 40f));
        bool clicked = false;
        button.OnClick += () => clicked = true;
        canvas.Add(button);
        canvas.SetFocus(button);
        canvas.ShowContextMenu(MakeMenu(), Vector2.Zero);

        _input.IsKeyPressed(Key.Enter).Returns(true);
        var consumed = canvas.ProcessKeyboardInput(_input, false);

        consumed.Should().BeTrue();
        clicked.Should().BeFalse();
    }

    [Fact]
    public void Clear_ClosesActiveContextMenu()
    {
        var canvas = CreateCanvas();
        var menu = MakeMenu();
        bool closed = false;
        menu.OnClosed += () => closed = true;
        canvas.ShowContextMenu(menu, Vector2.Zero);

        canvas.Clear();

        canvas.ActiveContextMenu.Should().BeNull();
        closed.Should().BeTrue();
    }

    #endregion

    #region UIToast

    private static UIToast MakeToast(float duration = 2f, float fadeIn = 0f, float fadeOut = 0f) =>
        new UIToast { Text = "Hello", Width = 200f, Duration = duration, FadeInTime = fadeIn, FadeOutTime = fadeOut };

    [Fact]
    public void ShowToast_AddsToActiveToasts()
    {
        var canvas = CreateCanvas();
        var toast = MakeToast();

        canvas.ShowToast(toast);

        canvas.ActiveToasts.Should().ContainSingle().Which.Should().BeSameAs(toast);
    }

    [Fact]
    public void ShowToast_MultipleToasts_AllPresent()
    {
        var canvas = CreateCanvas();
        canvas.ShowToast(MakeToast());
        canvas.ShowToast(MakeToast());

        canvas.ActiveToasts.Should().HaveCount(2);
    }

    [Fact]
    public void ShowToast_ExceedsMaxVisibleToasts_EvictsOldest()
    {
        var canvas = CreateCanvas();
        canvas.MaxVisibleToasts = 2;
        var first = MakeToast();
        bool firstDismissed = false;
        first.OnDismissed += () => firstDismissed = true;

        canvas.ShowToast(first);
        canvas.ShowToast(MakeToast());
        canvas.ShowToast(MakeToast());

        firstDismissed.Should().BeTrue();
        canvas.ActiveToasts.Should().HaveCount(2);
        canvas.ActiveToasts.Should().NotContain(first);
    }

    [Fact]
    public void DismissToast_RequestsDismiss_ToastEntersFadeOut()
    {
        var canvas = CreateCanvas();
        var toast = new UIToast { Text = "Hi", Duration = 10f, FadeInTime = 0f, FadeOutTime = 1f };
        canvas.ShowToast(toast);

        canvas.DismissToast(toast);
        canvas.Update(0.01f);

        toast.Alpha.Should().BeLessThan(1f);
    }

    [Fact]
    public void DismissToast_NotInList_NoOp()
    {
        var canvas = CreateCanvas();
        var toast = MakeToast();

        var act = () => canvas.DismissToast(toast);

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_ExpiredToast_RemovesFromActiveToasts()
    {
        var canvas = CreateCanvas();
        var toast = MakeToast(duration: 0.1f);
        canvas.ShowToast(toast);

        canvas.Update(1f);

        canvas.ActiveToasts.Should().BeEmpty();
    }

    [Fact]
    public void Update_ExpiredToast_FiresOnDismissed()
    {
        var canvas = CreateCanvas();
        var toast = MakeToast(duration: 0.1f);
        bool dismissed = false;
        toast.OnDismissed += () => dismissed = true;
        canvas.ShowToast(toast);

        canvas.Update(1f);

        dismissed.Should().BeTrue();
    }

    [Fact]
    public void Update_ActiveToast_StaysInList()
    {
        var canvas = CreateCanvas();
        var toast = MakeToast(duration: 10f);
        canvas.ShowToast(toast);

        canvas.Update(0.5f);

        canvas.ActiveToasts.Should().ContainSingle();
    }

    [Fact]
    public void Clear_DismissesAllActiveToasts()
    {
        var canvas = CreateCanvas();
        var t1 = MakeToast();
        var t2 = MakeToast();
        bool d1 = false, d2 = false;
        t1.OnDismissed += () => d1 = true;
        t2.OnDismissed += () => d2 = true;
        canvas.ShowToast(t1);
        canvas.ShowToast(t2);

        canvas.Clear();

        canvas.ActiveToasts.Should().BeEmpty();
        d1.Should().BeTrue();
        d2.Should().BeTrue();
    }

    [Fact]
    public void ToastAnchor_DefaultIsBottomRight()
    {
        var canvas = CreateCanvas();
        canvas.ToastAnchor.Should().Be(ToastAnchor.BottomRight);
    }

    [Fact]
    public void MaxVisibleToasts_DefaultIsFive()
    {
        var canvas = CreateCanvas();
        canvas.MaxVisibleToasts.Should().Be(5);
    }

    #endregion

    #region UISpinBox

    [Fact]
    public void UISpinBox_ClickIncrement_IncrementsValue()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Step = 1f,
            Value = 10f
        };
        canvas.Add(spinBox);

        // Increment button is on the right side; width = Size.Y = 30, so X range is 160..190
        SetMousePosition(new Vector2(175f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        spinBox.Value.Should().BeApproximately(11f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_ClickDecrement_DecrementsValue()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Step = 1f,
            Value = 10f
        };
        canvas.Add(spinBox);

        // Decrement button is on the left side; X range is 100..130
        SetMousePosition(new Vector2(115f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        spinBox.Value.Should().BeApproximately(9f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_ClickField_BeginEdit()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f));
        canvas.Add(spinBox);

        // Field is between the two buttons: X range is 130..160
        SetMousePosition(new Vector2(145f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        spinBox.IsEditing.Should().BeTrue();
    }

    [Fact]
    public void UISpinBox_ClickGainsFocus()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f));
        canvas.Add(spinBox);

        SetMousePosition(new Vector2(145f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        canvas.FocusedWidget.Should().Be(spinBox);
    }

    [Fact]
    public void UISpinBox_ClickOutside_LosesFocus()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f));
        canvas.Add(spinBox);

        SetMousePosition(new Vector2(145f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(0f, 0f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        canvas.FocusedWidget.Should().BeNull();
    }

    [Fact]
    public void UISpinBox_ArrowUp_NudgesValue()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f))
        {
            Value = 5f,
            MaxValue = 100f,
            Step = 2f
        };
        canvas.Add(spinBox);

        // Click to focus
        SetMousePosition(new Vector2(145f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        _input.IsKeyPressed(Key.Up).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        spinBox.Value.Should().BeApproximately(7f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_ArrowDown_NudgesValue()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f))
        {
            Value = 5f,
            MaxValue = 100f,
            Step = 1f
        };
        canvas.Add(spinBox);

        SetMousePosition(new Vector2(145f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        _input.IsKeyPressed(Key.Down).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        spinBox.Value.Should().BeApproximately(4f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_TabFocusCycles()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f)) { TabIndex = 0 };
        var button = new UIButton("OK", new Vector2(100f, 200f), new Vector2(90f, 30f)) { TabIndex = 1 };
        canvas.Add(spinBox);
        canvas.Add(button);

        _input.IsKeyPressed(Key.Tab).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        canvas.FocusedWidget.Should().Be(spinBox);
    }

    [Fact]
    public void UISpinBox_Clear_ResetsHoveredSpinBox()
    {
        var canvas = CreateCanvas();
        var spinBox = new UISpinBox(new Vector2(100f, 100f), new Vector2(90f, 30f));
        canvas.Add(spinBox);

        SetMousePosition(new Vector2(145f, 115f));
        canvas.ProcessMouseInput(_input, false);

        canvas.Clear();

        // Should not throw; canvas is in a clean state
        canvas.FocusedWidget.Should().BeNull();
    }

    #endregion

    #region DragAndDrop

    private sealed class TestPayload : IDragPayload { }

    [Fact]
    public void DragDrop_BelowThreshold_IsDraggingIsFalse()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        canvas.RegisterDraggable(label, new TestPayload());

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();
        // Move only 3px — below threshold of 6px
        SetMousePosition(new Vector2(143f, 115f));
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        canvas.ProcessMouseInput(_input, false);

        canvas.IsDragging.Should().BeFalse();
    }

    [Fact]
    public void DragDrop_AboveThreshold_IsDraggingIsTrue()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        canvas.RegisterDraggable(label, new TestPayload());

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        // Move 10px — above threshold
        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        canvas.IsDragging.Should().BeTrue();
        canvas.DragSource.Should().Be(label);
    }

    [Fact]
    public void DragDrop_OnDragStarted_EventFired()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        var payload = new TestPayload();
        canvas.RegisterDraggable(label, payload);

        IUIComponent? startedSource = null;
        canvas.OnDragStarted += (src, _) => startedSource = src;

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        startedSource.Should().Be(label);
    }

    [Fact]
    public void DragDrop_DropOnTarget_FiresOnDrop()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        var payload = new TestPayload();
        canvas.RegisterDraggable(label, payload);

        var target = new UIDropTarget
        {
            Position = new Vector2(300f, 100f),
            Size = new Vector2(120f, 60f)
        };
        canvas.Add(target);
        canvas.RegisterDropTarget(target);

        IDragPayload? dropped = null;
        target.OnDrop += p => dropped = p;

        // Press on label
        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        // Drag past threshold
        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        // Move over drop target and release
        SetMousePosition(new Vector2(360f, 130f));
        _input.IsMouseButtonDown(MouseButton.Left).Returns(false);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(true);
        canvas.ProcessMouseInput(_input, false);

        dropped.Should().BeSameAs(payload);
        canvas.IsDragging.Should().BeFalse();
    }

    [Fact]
    public void DragDrop_DropOutsideTarget_FiresOnDragCancelled()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        var payload = new TestPayload();
        canvas.RegisterDraggable(label, payload);

        IDragPayload? cancelled = null;
        canvas.OnDragCancelled += (_, p) => cancelled = p;

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        // Release away from any target
        SetMousePosition(new Vector2(500f, 500f));
        _input.IsMouseButtonDown(MouseButton.Left).Returns(false);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(true);
        canvas.ProcessMouseInput(_input, false);

        cancelled.Should().BeSameAs(payload);
        canvas.IsDragging.Should().BeFalse();
    }

    [Fact]
    public void DragDrop_AcceptsPayload_False_DoesNotDrop()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        var payload = new TestPayload();
        canvas.RegisterDraggable(label, payload);

        var target = new UIDropTarget
        {
            Position = new Vector2(300f, 100f),
            Size = new Vector2(120f, 60f),
            AcceptsPayload = _ => false
        };
        canvas.Add(target);
        canvas.RegisterDropTarget(target);

        bool dropped = false;
        target.OnDrop += _ => dropped = true;

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(360f, 130f));
        _input.IsMouseButtonDown(MouseButton.Left).Returns(false);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(true);
        canvas.ProcessMouseInput(_input, false);

        dropped.Should().BeFalse();
    }

    [Fact]
    public void DragDrop_Clear_CancelsDrag()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        canvas.RegisterDraggable(label, new TestPayload());

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        canvas.IsDragging.Should().BeTrue();

        canvas.Clear();

        canvas.IsDragging.Should().BeFalse();
    }

    [Fact]
    public void DragDrop_Remove_CancelsDrag()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        canvas.RegisterDraggable(label, new TestPayload());

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        canvas.IsDragging.Should().BeTrue();

        canvas.Remove(label);

        canvas.IsDragging.Should().BeFalse();
    }

    [Fact]
    public void DragDrop_UnregisterDraggable_PreventsNewDrag()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        canvas.RegisterDraggable(label, new TestPayload());
        canvas.UnregisterDraggable(label);

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        canvas.IsDragging.Should().BeFalse();
    }

    [Fact]
    public void DragDrop_DropTarget_HoveredHighlight_SetDuringDrag()
    {
        var canvas = CreateCanvas();
        var label = new UILabel("Item", new Vector2(100f, 100f));
        label.Size = new Vector2(80f, 30f);
        canvas.Add(label);
        canvas.RegisterDraggable(label, new TestPayload());

        var target = new UIDropTarget
        {
            Position = new Vector2(300f, 100f),
            Size = new Vector2(120f, 60f)
        };
        canvas.Add(target);
        canvas.RegisterDropTarget(target);

        SetMousePosition(new Vector2(140f, 115f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        SetMousePosition(new Vector2(150f, 115f));
        _input.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
        _input.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(_input, false);

        // Move over target
        SetMousePosition(new Vector2(360f, 130f));
        canvas.ProcessMouseInput(_input, false);

        target.IsHovered.Should().BeTrue();
    }

    #endregion

    #region UIVirtualList integration

    private static UIVirtualList<string> CreateStringList(int rowCount = 20, float rowHeight = 24f) =>
        new UIVirtualList<string>
        {
            Position = new Vector2(10f, 10f),
            Size = new Vector2(200f, 120f),
            RowHeight = rowHeight
        };

    [Fact]
    public void VirtualList_Click_SelectsRow()
    {
        var canvas = CreateCanvas();
        var list = CreateStringList();
        list.SetItems(["Alpha", "Beta", "Gamma", "Delta", "Epsilon"]);
        canvas.Add(list);

        // Click centre of row 1 (y = 10 + 24 + 12 = 46)
        SetMousePosition(new Vector2(100f, 46f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        list.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void VirtualList_Click_GivesFocus()
    {
        var canvas = CreateCanvas();
        var list = CreateStringList();
        list.SetItems(["A", "B", "C"]);
        canvas.Add(list);

        SetMousePosition(new Vector2(100f, 22f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        list.IsFocused.Should().BeTrue();
        canvas.FocusedWidget.Should().BeSameAs(list);
    }

    [Fact]
    public void VirtualList_ClickOutside_ClearsFocus()
    {
        var canvas = CreateCanvas();
        var list = CreateStringList();
        list.SetItems(["A", "B"]);
        canvas.Add(list);

        // Focus first
        SetMousePosition(new Vector2(100f, 22f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        list.IsFocused.Should().BeTrue();

        // Click outside
        SetMousePosition(new Vector2(500f, 500f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        list.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void VirtualList_ScrollWheel_ScrollsList()
    {
        var canvas = CreateCanvas();
        var list = CreateStringList(rowHeight: 24f);
        list.SetItems(Enumerable.Range(0, 30).Select(i => $"Item {i}").ToList());
        canvas.Add(list);

        // Hover the list
        SetMousePosition(new Vector2(100f, 60f));
        SetMouseIdle();
        canvas.ProcessMouseInput(_input, false);

        // Simulate scroll down
        _input.ScrollWheelDelta.Returns(-1f);
        canvas.ProcessMouseInput(_input, false);

        list.ScrollOffsetY.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void VirtualList_KeyboardDown_NavigatesSelection()
    {
        var canvas = CreateCanvas();
        var list = CreateStringList();
        list.SetItems(["A", "B", "C"]);
        canvas.Add(list);

        // Give focus via click
        SetMousePosition(new Vector2(100f, 22f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        list.Select(0);

        // Arrow down
        _input.IsKeyPressed(Key.Down).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        list.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void VirtualList_Clear_RemovesListState()
    {
        var canvas = CreateCanvas();
        var list = CreateStringList();
        list.SetItems(["A", "B"]);
        canvas.Add(list);

        SetMousePosition(new Vector2(100f, 22f));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        list.IsFocused.Should().BeTrue();

        canvas.Clear();

        list.IsFocused.Should().BeFalse(); // Clear() properly unfocuses all widgets
        canvas.FocusedWidget.Should().BeNull();
    }

    #endregion

    #region UITween — canvas integration

    [Fact]
    public void StartTween_TweenAdvancedByUpdate()
    {
        var canvas = CreateCanvas();
        float received = 0f;
        var tween = new UITween(0f, 100f, 1f, v => received = v);
        canvas.StartTween(tween);

        canvas.Update(0.5f);

        received.Should().BeApproximately(50f, 0.001f);
    }

    [Fact]
    public void StartTween_CompletedTween_RemovedAutomatically()
    {
        var canvas = CreateCanvas();
        var tween = new UITween(0f, 1f, 0.2f, _ => { });
        canvas.StartTween(tween);

        canvas.Update(0.2f);

        canvas.ActiveTweens.Should().BeEmpty();
    }

    [Fact]
    public void StopTween_PreventsAdvancement()
    {
        var canvas = CreateCanvas();
        float received = 0f;
        var tween = new UITween(0f, 100f, 1f, v => received = v);
        canvas.StartTween(tween);
        canvas.StopTween(tween);

        canvas.Update(1f);

        received.Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void StopAllTweens_ClearsAllActive()
    {
        var canvas = CreateCanvas();
        canvas.StartTween(new UITween(0f, 1f, 1f, _ => { }));
        canvas.StartTween(new UITween(0f, 1f, 1f, _ => { }));

        canvas.StopAllTweens();

        canvas.ActiveTweens.Should().BeEmpty();
    }

    [Fact]
    public void Clear_StopsAllTweens()
    {
        var canvas = CreateCanvas();
        canvas.StartTween(new UITween(0f, 1f, 2f, _ => { }));

        canvas.Clear();

        canvas.ActiveTweens.Should().BeEmpty();
    }

    [Fact]
    public void StartTween_Sequence_AdvancedByUpdate()
    {
        var canvas = CreateCanvas();
        float b = 0f;
        var seq = new UITweenSequence()
            .Then(0f, 1f, 0.2f, _ => { })
            .Then(0f, 50f, 0.5f, v => b = v);
        canvas.StartTween(seq);

        canvas.Update(0.2f); // first tween completes
        canvas.Update(0.25f); // half of second tween

        b.Should().BeApproximately(25f, 0.5f);
    }

    [Fact]
    public void StartTween_Sequence_CompletedSequence_RemovedAutomatically()
    {
        var canvas = CreateCanvas();
        var seq = new UITweenSequence().Then(0f, 1f, 0.1f, _ => { });
        canvas.StartTween(seq);

        canvas.Update(0.1f);

        canvas.ActiveTweenSequences.Should().BeEmpty();
    }

    [Fact]
    public void StartTween_LoopingTween_NeverAutoRemoved()
    {
        var canvas = CreateCanvas();
        var tween = new UITween(0f, 1f, 0.1f, _ => { }) { LoopMode = UITweenLoopMode.Loop };
        canvas.StartTween(tween);

        canvas.Update(1f); // runs 10 loops

        canvas.ActiveTweens.Should().Contain(tween);
    }

    #endregion

    #region World-Space UI

    [Fact]
    public void AddWorldComponent_IsRenderedWithoutCamera()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var label = new UIWorldLabel { Text = "Foo", WorldPosition = new Vector2(100, 100) };
        canvas.AddWorldComponent(label);
        var act = () => canvas.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void AddWorldComponent_WithCamera_ProjectsWorldToScreen()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();

        var camera = Substitute.For<ICamera>();
        camera.WorldToScreen(Arg.Any<Vector2>()).Returns(new Vector2(400, 300));

        canvas.WorldCamera = camera;

        var label = new UIWorldLabel { Text = "Tag", WorldPosition = new Vector2(50, 50) };
        canvas.AddWorldComponent(label);
        canvas.Render(renderer);

        label.Position.Should().Be(new Vector2(400, 300));
    }

    [Fact]
    public void AddWorldComponent_WithScreenOffset_AppliedAfterProjection()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();

        var camera = Substitute.For<ICamera>();
        camera.WorldToScreen(Arg.Any<Vector2>()).Returns(new Vector2(200, 150));

        canvas.WorldCamera = camera;

        var label = new UIWorldLabel
        {
            Text = "Tag",
            WorldPosition = new Vector2(0, 0),
            ScreenOffset = new Vector2(-10, -20)
        };
        canvas.AddWorldComponent(label);
        canvas.Render(renderer);

        label.Position.Should().Be(new Vector2(190, 130));
    }

    [Fact]
    public void WorldComponent_CullWhenOffScreen_HidesWhenOutsideViewport()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);
        var renderer = new HeadlessRenderer();

        var camera = Substitute.For<ICamera>();
        camera.WorldToScreen(Arg.Any<Vector2>()).Returns(new Vector2(-100, -100));

        canvas.WorldCamera = camera;

        var label = new UIWorldLabel
        {
            Text = "Off",
            WorldPosition = new Vector2(-999, -999),
            CullWhenOffScreen = true
        };
        canvas.AddWorldComponent(label);
        var act = () => canvas.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void WorldComponent_CullWhenOffScreen_False_StillRendersOutsideViewport()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(800, 600);
        var renderer = new HeadlessRenderer();

        var camera = Substitute.For<ICamera>();
        camera.WorldToScreen(Arg.Any<Vector2>()).Returns(new Vector2(-100, -100));

        canvas.WorldCamera = camera;

        bool rendered = false;
        var label = new UIWorldLabel
        {
            Text = "AlwaysOn",
            WorldPosition = new Vector2(-999, -999),
            CullWhenOffScreen = false
        };
        canvas.AddWorldComponent(label);
        canvas.Render(renderer);
        label.Position.Should().Be(new Vector2(-100, -100));
    }

    [Fact]
    public void RemoveWorldComponent_StopsRendering()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();

        var camera = Substitute.For<ICamera>();
        camera.WorldToScreen(Arg.Any<Vector2>()).Returns(new Vector2(200, 200));
        canvas.WorldCamera = camera;

        var label = new UIWorldLabel { Text = "Remove Me", WorldPosition = new Vector2(10, 10) };
        canvas.AddWorldComponent(label);
        canvas.RemoveWorldComponent(label);

        label.Position = new Vector2(0, 0);
        canvas.Render(renderer);

        label.Position.Should().Be(new Vector2(0, 0));
    }

    [Fact]
    public void Clear_RemovesWorldComponents()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();

        var camera = Substitute.For<ICamera>();
        camera.WorldToScreen(Arg.Any<Vector2>()).Returns(new Vector2(200, 200));
        canvas.WorldCamera = camera;

        var label = new UIWorldLabel { Text = "X", WorldPosition = new Vector2(1, 1) };
        canvas.AddWorldComponent(label);

        canvas.Render(renderer);
        label.Position.Should().Be(new Vector2(200, 200));

        canvas.Clear();

        label.Position = new Vector2(0, 0);
        canvas.Render(renderer);

        label.Position.Should().Be(new Vector2(0, 0));
    }

    [Fact]
    public void WorldComponent_Update_IsCalled()
    {
        var canvas = CreateCanvas();
        var label = new UIWorldLabel { Text = "T", WorldPosition = new Vector2(0, 0) };
        canvas.AddWorldComponent(label);
        var act = () => canvas.Update(0.016f);
        act.Should().NotThrow();
    }

    #endregion

    #region UITreeView Canvas Integration

    private static UITreeView CreateTreeView(Vector2 position, Vector2 size, float rowHeight = 22f)
    {
        return new UITreeView { Position = position, Size = size, RowHeight = rowHeight };
    }

    [Fact]
    public void TreeView_Add_IsRenderedByCanvas()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var tv = CreateTreeView(new Vector2(0, 0), new Vector2(200, 300));
        tv.AddRoot(new UITreeNode("Root").Add("Child A"));
        canvas.Add(tv);
        var act = () => canvas.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void TreeView_MouseClick_SelectsRow()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var tv = CreateTreeView(new Vector2(0, 0), new Vector2(200, 300));
        tv.AddRoot(new UITreeNode("Root"));
        canvas.Add(tv);
        canvas.Render(renderer);

        SetMousePosition(new Vector2(100, 11));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        tv.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void TreeView_MouseClick_GivesFocus()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var tv = CreateTreeView(new Vector2(0, 0), new Vector2(200, 300));
        tv.AddRoot(new UITreeNode("Root"));
        canvas.Add(tv);
        canvas.Render(renderer);

        SetMousePosition(new Vector2(100, 11));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        tv.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void TreeView_KeyboardNavigate_Down_SelectsFirst()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var tv = CreateTreeView(new Vector2(0, 0), new Vector2(200, 300));
        tv.AddRoot(new UITreeNode("A"));
        tv.AddRoot(new UITreeNode("B"));
        canvas.Add(tv);
        canvas.Render(renderer);

        // Give focus via click
        SetMousePosition(new Vector2(100, 11));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        // Navigate up to deselect by clicking outside, then navigate down
        _input.IsKeyPressed(Key.Down).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        tv.SelectedIndex.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void TreeView_KeyboardNavigate_ExpandRight()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var root = new UITreeNode("Root");
        root.Add("Child");
        var tv = CreateTreeView(new Vector2(0, 0), new Vector2(200, 300));
        tv.AddRoot(root);
        canvas.Add(tv);
        canvas.Render(renderer);

        // Click to focus + select root row
        SetMousePosition(new Vector2(100, 11));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        SetMouseIdle();

        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        root.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void TreeView_Clear_RemovesTreeView()
    {
        var canvas = CreateCanvas();
        var tv = CreateTreeView(Vector2.Zero, new Vector2(200, 300));
        tv.AddRoot(new UITreeNode("X"));
        canvas.Add(tv);

        SetMousePosition(new Vector2(100, 11));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        tv.IsFocused.Should().BeTrue();

        canvas.Clear();
        tv.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void TreeView_Remove_CleansUpFocus()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var tv = CreateTreeView(Vector2.Zero, new Vector2(200, 300));
        tv.AddRoot(new UITreeNode("X"));
        canvas.Add(tv);
        canvas.Render(renderer);

        SetMousePosition(new Vector2(100, 11));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);
        tv.IsFocused.Should().BeTrue();

        canvas.Remove(tv);
        tv.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void TreeView_MouseScroll_ScrollsContent()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var tv = CreateTreeView(new Vector2(0, 0), new Vector2(200, 100), 22f);
        for (int i = 0; i < 20; i++)
            tv.AddRoot(new UITreeNode($"Item {i}"));
        canvas.Add(tv);
        canvas.Render(renderer);

        SetMousePosition(new Vector2(100, 50));
        _input.ScrollWheelDelta.Returns(-3f);
        canvas.ProcessMouseInput(_input, false);

        tv.ScrollOffsetY.Should().BeGreaterThan(0f);
    }

    #endregion

    #region UIMenuBar Canvas Integration

    private static UIMenuBar CreateMenuBar() =>
        new() { Position = new Vector2(0, 0), Size = new Vector2(800, 28), BarHeight = 28f, TitlePadding = 12f };

    [Fact]
    public void MenuBar_Add_RegistersActiveMenuBar()
    {
        var canvas = CreateCanvas();
        var mb = CreateMenuBar();
        canvas.Add(mb);
        canvas.ActiveMenuBar.Should().BeSameAs(mb);
    }

    [Fact]
    public void MenuBar_Render_RendersWithoutThrow()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New").Add("Open");
        mb.AddMenu("Edit").Add("Undo");
        canvas.Add(mb);
        var act = () => canvas.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void MenuBar_TitleClick_OpensSubmenu()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New");
        canvas.Add(mb);
        canvas.Render(renderer);

        SetMousePosition(new Vector2(10, 14));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        mb.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void MenuBar_OpenSubmenu_RendersOverlay()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New").Add("Open");
        canvas.Add(mb);
        mb.OpenMenu(0);
        var act = () => canvas.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void MenuBar_EscapeKey_ClosesSubmenu()
    {
        var canvas = CreateCanvas();
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New");
        canvas.Add(mb);
        mb.OpenMenu(0);

        _input.IsKeyPressed(Key.Escape).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        mb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_KeyboardRightLeft_SwitchesMenus()
    {
        var canvas = CreateCanvas();
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New");
        mb.AddMenu("Edit").Add("Undo");
        canvas.Add(mb);
        mb.OpenMenu(0);

        _input.IsKeyPressed(Key.Right).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);
        mb.OpenMenuIndex.Should().Be(1);

        _input.IsKeyPressed(Key.Right).Returns(false);
        _input.IsKeyPressed(Key.Left).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);
        mb.OpenMenuIndex.Should().Be(0);
    }

    [Fact]
    public void MenuBar_KeyboardDownEnter_ActivatesItem()
    {
        var canvas = CreateCanvas();
        bool fired = false;
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("Save", onClick: () => fired = true);
        canvas.Add(mb);
        mb.OpenMenu(0);

        _input.IsKeyPressed(Key.Down).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        _input.IsKeyPressed(Key.Down).Returns(false);
        _input.IsKeyPressed(Key.Enter).Returns(true);
        canvas.ProcessKeyboardInput(_input, false);

        fired.Should().BeTrue();
        mb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_ClickOutsideSubmenu_ClosesMenu()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New");
        canvas.Add(mb);
        canvas.Render(renderer);
        mb.OpenMenu(0);

        SetMousePosition(new Vector2(700, 400));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        mb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_ClickItem_FiresAndClosesMenu()
    {
        var canvas = CreateCanvas();
        var renderer = new HeadlessRenderer();
        bool fired = false;
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New", onClick: () => fired = true);
        canvas.Add(mb);
        canvas.Render(renderer);
        mb.OpenMenu(0);

        // Submenu "New" is at ~y=28..56 under "File" title which starts at x≈0
        SetMousePosition(new Vector2(50, 42));
        SetMousePressed();
        canvas.ProcessMouseInput(_input, false);

        fired.Should().BeTrue();
        mb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_Clear_ClosesAndRemovesMenuBar()
    {
        var canvas = CreateCanvas();
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New");
        canvas.Add(mb);
        mb.OpenMenu(0);

        canvas.Clear();

        canvas.ActiveMenuBar.Should().BeNull();
        mb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_Remove_ClosesAndClearsReference()
    {
        var canvas = CreateCanvas();
        var mb = CreateMenuBar();
        mb.AddMenu("File").Add("New");
        canvas.Add(mb);
        mb.OpenMenu(0);

        canvas.Remove(mb);

        canvas.ActiveMenuBar.Should().BeNull();
        mb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void MenuBar_ScreenSizePropagated_OnAdd()
    {
        var canvas = CreateCanvas();
        canvas.ScreenSize = new Vector2(1920, 1080);
        var mb = CreateMenuBar();
        canvas.Add(mb);
        mb.ScreenHeight.Should().Be(1080f);
    }

    #endregion
}
