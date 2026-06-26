using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;
using Brine2D.UI;
using FluentAssertions;
using NSubstitute;
using System.Numerics;

namespace Brine2D.Tests.UI;

public class UITests
{
    private static IRenderer MakeRenderer(Vector2? measuredTextSize = null)
    {
        var size = measuredTextSize ?? new Vector2(40f, 16f);
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(Arg.Any<string>()).Returns(size);
        renderer.MeasureText(Arg.Any<string>(), Arg.Any<float?>()).Returns(size);
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(size);
        return renderer;
    }

    private static IInputContext MakeInput(Vector2? mousePos = null, bool leftPressed = false, bool leftReleased = false, bool leftDown = false)
    {
        var input = Substitute.For<IInputContext>();
        input.MousePosition.Returns(mousePos ?? Vector2.Zero);
        input.IsMouseButtonPressed(MouseButton.Left).Returns(leftPressed);
        input.IsMouseButtonReleased(MouseButton.Left).Returns(leftReleased);
        input.IsMouseButtonDown(MouseButton.Left).Returns(leftDown);
        input.ScrollWheelDelta.Returns(0f);
        return input;
    }

    #region UIButton

    [Fact]
    public void UIButton_Render_TextCenteredUsingMeasureText()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var button = new UIButton("OK", new Vector2(100f, 100f), new Vector2(200f, 50f));

        button.Render(renderer);

        // textX = 100 + (200 - 20) / 2 = 190
        // textY = 100 + (50  - 16) / 2 = 117
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "OK")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[1]!).Should().BeApproximately(190f, 0.5f);
        ((float)calls[0].GetArguments()[2]!).Should().BeApproximately(117f, 0.5f);
    }

    [Fact]
    public void UIButton_Render_EmptyText_DrawTextNotCalled()
    {
        var renderer = MakeRenderer();
        var button = new UIButton(string.Empty, new Vector2(0f, 0f), new Vector2(100f, 40f));

        button.Render(renderer);

        renderer.DidNotReceiveWithAnyArgs().DrawText(default!, default, default, default(TextRenderOptions));
    }

    [Fact]
    public void UIButton_Render_WhenDisabled_TextUsesDisabledColor()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var button = new UIButton("X", new Vector2(0f, 0f), new Vector2(80f, 30f))
        {
            Enabled = false
        };

        button.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "X")
            .ToList();
        calls.Should().HaveCount(1);
    }

    [Fact]
    public void UIButton_Render_WhenNotVisible_NothingDrawn()
    {
        var renderer = MakeRenderer();
        var button = new UIButton("X", new Vector2(0f, 0f), new Vector2(80f, 30f))
        {
            Visible = false
        };

        button.Render(renderer);

        renderer.DidNotReceive().DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    #endregion

    #region UITabContainer

    [Fact]
    public void UITabContainer_Render_TabTitleCenteredUsingMeasureText()
    {
        var renderer = MakeRenderer(new Vector2(56f, 16f));
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f));
        container.AddTab("Settings");

        container.Render(renderer);

        // tabWidth = 200, textX = 0 + (200 - 56) / 2 = 72
        // TabHeight = 30,  textY = 0 + ( 30 - 16) / 2 =  7
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "Settings")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[1]!).Should().BeApproximately(72f, 0.5f);
        ((float)calls[0].GetArguments()[2]!).Should().BeApproximately(7f, 0.5f);
    }

    [Fact]
    public void UITabContainer_Render_MultipleTabs_EachTitleCentered()
    {
        var renderer = MakeRenderer(new Vector2(40f, 16f));
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f));
        container.AddTab("A");
        container.AddTab("B");

        container.Render(renderer);

        // tabWidth = 100, textX = tabX + (100 - 40) / 2 = tabX + 30
        var aCalls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "A")
            .ToList();
        var bCalls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "B")
            .ToList();
        aCalls.Should().HaveCount(1);
        bCalls.Should().HaveCount(1);
        ((float)aCalls[0].GetArguments()[1]!).Should().BeApproximately(30f, 0.5f);
        ((float)bCalls[0].GetArguments()[1]!).Should().BeApproximately(130f, 0.5f);
    }

    [Fact]
    public void UITabContainer_SelectTab_SetsCorrectIndex()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f));
        container.AddTab("One");
        container.AddTab("Two");

        // tabWidth = 100, click at x=150 → index 1
        container.SelectTab(new Vector2(150f, 15f));

        Assert.Equal(1, container.SelectedTabIndex);
    }

    [Fact]
    public void UITabContainer_RemoveTab_DecreasesTabCount()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(300f, 200f));
        container.AddTab("A");
        container.AddTab("B");
        container.AddTab("C");

        container.RemoveTab(1);

        Assert.Equal(2, container.TabCount);
    }

    [Fact]
    public void UITabContainer_RemoveTab_RemovesCorrectTitle()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(300f, 200f));
        container.AddTab("A");
        container.AddTab("B");
        container.AddTab("C");

        container.RemoveTab(1);

        Assert.Equal("A", container.GetTabTitle(0));
        Assert.Equal("C", container.GetTabTitle(1));
    }

    [Fact]
    public void UITabContainer_RemoveTab_SelectedTabRemoved_SelectionMovesToZero()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(300f, 200f));
        container.AddTab("A");
        container.AddTab("B");
        container.AddTab("C");
        container.SelectedTabIndex = 1;

        container.RemoveTab(1);

        Assert.Equal(0, container.SelectedTabIndex);
    }

    [Fact]
    public void UITabContainer_RemoveTab_SelectedTabRemoved_FiresOnTabChanged()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(300f, 200f));
        container.AddTab("A");
        container.AddTab("B");
        container.AddTab("C");
        container.SelectedTabIndex = 1;
        int? firedIndex = null;
        container.OnTabChanged += (i, _) => firedIndex = i;

        container.RemoveTab(1);

        firedIndex.Should().Be(0);
    }

    [Fact]
    public void UITabContainer_RemoveTab_TabBeforeSelected_SelectedIndexDecremented()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(300f, 200f));
        container.AddTab("A");
        container.AddTab("B");
        container.AddTab("C");
        container.SelectedTabIndex = 2;

        container.RemoveTab(0);

        Assert.Equal(1, container.SelectedTabIndex);
    }

    [Fact]
    public void UITabContainer_RemoveTab_TabBeforeSelected_DoesNotFireOnTabChanged()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(300f, 200f));
        container.AddTab("A");
        container.AddTab("B");
        container.AddTab("C");
        container.SelectedTabIndex = 2;
        bool fired = false;
        container.OnTabChanged += (_, _) => fired = true;

        container.RemoveTab(0);

        fired.Should().BeFalse();
    }

    [Fact]
    public void UITabContainer_RemoveTab_TabAfterSelected_SelectionUnchanged()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(300f, 200f));
        container.AddTab("A");
        container.AddTab("B");
        container.AddTab("C");
        container.SelectedTabIndex = 0;

        container.RemoveTab(2);

        Assert.Equal(0, container.SelectedTabIndex);
    }

    [Fact]
    public void UITabContainer_RemoveTab_LastRemainingTab_SelectedIndexIsNegativeOne()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f));
        container.AddTab("Only");

        container.RemoveTab(0);

        Assert.Equal(0, container.TabCount);
        Assert.Equal(-1, container.SelectedTabIndex);
    }

    [Fact]
    public void UITabContainer_RemoveTab_OutOfRangeIndex_DoesNotThrow()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f));
        container.AddTab("A");

        var ex = Record.Exception(() => container.RemoveTab(5));

        Assert.Null(ex);
        Assert.Equal(1, container.TabCount);
    }

    [Fact]
    public void UITabContainer_RemoveTab_NegativeIndex_DoesNotThrow()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f));
        container.AddTab("A");

        var ex = Record.Exception(() => container.RemoveTab(-1));

        Assert.Null(ex);
        Assert.Equal(1, container.TabCount);
    }

    #endregion

    #region UITextInput

    [Fact]
    public void UITextInput_Render_AlwaysAppliesScissorRect()
    {
        var renderer = MakeRenderer();
        var textInput = new UITextInput(new Vector2(10f, 10f), new Vector2(100f, 30f));
        textInput.Text = "Hello";

        textInput.Render(renderer);

        renderer.Received(1).PushScissorRect(Arg.Any<Rectangle?>());
        renderer.Received(1).PopScissorRect();
    }

    [Fact]
    public void UITextInput_Render_ScissorRect_AlsoAppliedWhenEmpty()
    {
        var renderer = MakeRenderer();
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(100f, 30f));

        textInput.Render(renderer);

        renderer.Received(1).PushScissorRect(Arg.Any<Rectangle?>());
        renderer.Received(1).PopScissorRect();
    }

    [Fact]
    public void UITextInput_Render_LongText_WhenFocused_TextIsScrolledLeftToShowCursor()
    {
        // text measures 200px wide, field usable width = 100 - 10*2 = 80px
        // cursor at end → scrollOffset = 200 - 80 = 120 → DrawText x = 10 - 120 = -110
        var renderer = MakeRenderer(new Vector2(200f, 16f));
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(100f, 30f));
        textInput.Text = "This text is way too long";
        textInput.SetFocused(true, MakeInput());

        textInput.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "This text is way too long")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[1]!).Should().BeLessThan(10f);
    }

    [Fact]
    public void UITextInput_Render_ShortText_WhenFocused_TextNotScrolled()
    {
        // text measures 30px, field usable width = 80px → no scroll needed
        var renderer = MakeRenderer(new Vector2(30f, 16f));
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(100f, 30f));
        textInput.Text = "Hi";
        textInput.SetFocused(true, MakeInput());

        textInput.Render(renderer);

        renderer.Received(1).DrawText(Arg.Is<string>(s => s == "Hi"), Arg.Is<float>(v => v == 10f), Arg.Any<float>(), Arg.Any<Color>());
    }

    [Fact]
    public void UITextInput_SetFocused_True_MovesCursorToEnd()
    {
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(100f, 30f));
        textInput.Text = "Hello";
        textInput.CursorPosition = 0;

        textInput.SetFocused(true, MakeInput());

        Assert.Equal(5, textInput.CursorPosition);
    }

    [Fact]
    public void UITextInput_SetFocused_False_ResetsFocus()
    {
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(100f, 30f));
        textInput.SetFocused(true, MakeInput());
        textInput.SetFocused(false, MakeInput());

        Assert.False(textInput.IsFocused);
    }

    [Fact]
    public void UITextInput_Render_Unfocused_LongText_ShowsFromStart()
    {
        var renderer = MakeRenderer(new Vector2(200f, 16f));
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(100f, 30f));
        textInput.Text = "This text is way too long";

        textInput.Render(renderer);

        renderer.Received(1).DrawText(
            "This text is way too long",
            10f,
            Arg.Any<float>(),
            Arg.Any<Color>());
    }

    #endregion

    #region UIScrollView

    [Fact]
    public void UIScrollView_Render_PushesAndPopsScissorRect()
    {
        var renderer = MakeRenderer();
        var scrollView = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f));

        scrollView.Render(renderer);

        renderer.Received(1).PushScissorRect(Arg.Any<Rectangle?>());
        renderer.Received(1).PopScissorRect();
    }

    [Fact]
    public void UIScrollView_Render_ChildInView_IsRendered()
    {
        var renderer = MakeRenderer();
        var scrollView = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f));
        scrollView.ContentHeight = 200f;
        scrollView.AddChild(new UILabel("Visible", new Vector2(10f, 10f)));

        scrollView.Render(renderer);

        var drawTextCalls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" &&
                        (string)c.GetArguments()[0] == "Visible");
        drawTextCalls.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void UIScrollView_Render_ChildScrolledOutOfView_IsNotRendered()
    {
        var renderer = MakeRenderer();
        var scrollView = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f));
        scrollView.ContentHeight = 1000f;
        scrollView.AddChild(new UILabel("Hidden", new Vector2(10f, 10f)));

        // Scroll 500px down: child at content Y=10 maps to screen Y = 0+10-500 = -490
        scrollView.ScrollOffset = new Vector2(0f, 500f);
        scrollView.Render(renderer);

        renderer.DidNotReceiveWithAnyArgs().DrawText(default!, default, default, default(Color));
    }

    [Fact]
    public void UIScrollView_Render_ScissorClipRect_MatchesViewBounds()
    {
        var renderer = MakeRenderer();
        var scrollView = new UIScrollView(new Vector2(50f, 60f), new Vector2(200f, 150f));

        scrollView.Render(renderer);

        renderer.Received(1).PushScissorRect(
            Arg.Is<Rectangle?>(r => r.HasValue
                && r.Value.X == 50f
                && r.Value.Y == 60f
                && r.Value.Width == 200f
                && r.Value.Height == 150f));
    }

    [Fact]
    public void UIScrollView_ScrollToChild_ChildBelowView_ScrollsDownVertically()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ContentHeight = 600f,
            ShowVerticalScrollbar = true
        };
        var child = new UILabel("X", new Vector2(0f, 500f)) { Size = new Vector2(50f, 20f) };
        sv.AddChild(child);

        sv.ScrollToChild(child);

        sv.ScrollOffset.Y.Should().BeGreaterThanOrEqualTo(320f); // 500+20-200 = 320
    }

    [Fact]
    public void UIScrollView_ScrollToChild_ChildAboveView_ScrollsUpVertically()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ContentHeight = 600f,
            ShowVerticalScrollbar = true
        };
        var child = new UILabel("X", new Vector2(0f, 10f)) { Size = new Vector2(50f, 20f) };
        sv.AddChild(child);
        sv.ScrollOffset = new Vector2(0f, 300f);

        sv.ScrollToChild(child);

        sv.ScrollOffset.Y.Should().Be(10f);
    }

    [Fact]
    public void UIScrollView_ScrollToChild_ChildRightOfView_ScrollsRightHorizontally()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ContentWidth = 600f,
            ShowHorizontalScrollbar = true
        };
        var child = new UILabel("X", new Vector2(500f, 0f)) { Size = new Vector2(50f, 20f) };
        sv.AddChild(child);

        sv.ScrollToChild(child);

        sv.ScrollOffset.X.Should().BeGreaterThanOrEqualTo(350f); // 500+50-200 = 350
    }

    [Fact]
    public void UIScrollView_ScrollToChild_ChildLeftOfView_ScrollsLeftHorizontally()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ContentWidth = 600f,
            ShowHorizontalScrollbar = true
        };
        var child = new UILabel("X", new Vector2(10f, 0f)) { Size = new Vector2(50f, 20f) };
        sv.AddChild(child);
        sv.ScrollOffset = new Vector2(300f, 0f);

        sv.ScrollToChild(child);

        sv.ScrollOffset.X.Should().Be(10f);
    }

    [Fact]
    public void UIScrollView_ScrollToChild_ChildAlreadyVisible_DoesNotChangeOffset()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ContentHeight = 600f,
            ContentWidth = 600f,
            ShowVerticalScrollbar = true,
            ShowHorizontalScrollbar = true
        };
        var child = new UILabel("X", new Vector2(50f, 50f)) { Size = new Vector2(50f, 20f) };
        sv.AddChild(child);
        sv.ScrollOffset = new Vector2(0f, 0f);

        sv.ScrollToChild(child);

        sv.ScrollOffset.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void UIScrollView_ScrollToChild_ChildNotInScrollView_NoChange()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ContentHeight = 600f
        };
        var outsider = new UILabel("X", new Vector2(0f, 500f)) { Size = new Vector2(50f, 20f) };

        sv.ScrollToChild(outsider);

        sv.ScrollOffset.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void UIScrollView_ScrollToChild_FiresOnScrollChanged()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ContentHeight = 600f,
            ShowVerticalScrollbar = true
        };
        var child = new UILabel("X", new Vector2(0f, 500f)) { Size = new Vector2(50f, 20f) };
        sv.AddChild(child);

        Vector2? received = null;
        sv.OnScrollChanged += v => received = v;

        sv.ScrollToChild(child);

        received.Should().NotBeNull();
        received!.Value.Y.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void UIScrollView_ScrollToChild_AlreadyVisible_DoesNotFireOnScrollChanged()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ContentHeight = 600f,
            ShowVerticalScrollbar = true
        };
        var child = new UILabel("X", new Vector2(0f, 10f)) { Size = new Vector2(50f, 20f) };
        sv.AddChild(child);
        sv.ScrollOffset = Vector2.Zero;

        int callCount = 0;
        sv.OnScrollChanged += _ => callCount++;

        sv.ScrollToChild(child);

        callCount.Should().Be(0);
    }

    [Fact]
    public void UIScrollView_FitContentToChildren_RemoveChild_ClampsScrollOffset()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            AutoFitContent = true,
            ShowVerticalScrollbar = true
        };
        var tall = new UILabel("X", new Vector2(0f, 0f)) { Size = new Vector2(50f, 600f) };
        sv.AddChild(tall);
        sv.ScrollOffset = new Vector2(0f, 400f);

        sv.RemoveChild(tall);

        sv.ScrollOffset.Y.Should().Be(0f);
    }

    [Fact]
    public void UIScrollView_FitContentToChildren_RemoveChild_FiresOnScrollChangedWhenClamped()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            AutoFitContent = true,
            ShowVerticalScrollbar = true
        };
        var tall = new UILabel("X", new Vector2(0f, 0f)) { Size = new Vector2(50f, 600f) };
        sv.AddChild(tall);
        sv.ScrollOffset = new Vector2(0f, 400f);

        Vector2? received = null;
        sv.OnScrollChanged += v => received = v;

        sv.RemoveChild(tall);

        received.Should().NotBeNull();
        received!.Value.Y.Should().Be(0f);
    }

    #endregion

    #region UIDialog

    [Fact]
    public void UIDialog_Constructor_CentersOnDefaultScreenSize()
    {
        var dialog = new UIDialog("Title", "Message", new Vector2(400f, 200f));

        Assert.Equal((1280f - 400f) / 2f, dialog.Position.X);
        Assert.Equal((720f - 200f) / 2f, dialog.Position.Y);
    }

    [Fact]
    public void UIDialog_CenterOnScreen_UpdatesPosition()
    {
        var dialog = new UIDialog("Title", "Message", new Vector2(400f, 200f));

        dialog.CenterOnScreen(new Vector2(800f, 600f));

        Assert.Equal(200f, dialog.Position.X);
        Assert.Equal(200f, dialog.Position.Y);
    }

    #endregion

    #region UICanvas

    [Fact]
    public void UICanvas_Add_Dialog_AutoCenteredOnCurrentScreenSize()
    {
        var canvas = new UICanvas(MakeInput());
        canvas.ScreenSize = new Vector2(800f, 600f);
        var dialog = new UIDialog("Title", "Msg", new Vector2(400f, 200f));

        canvas.Add(dialog);

        Assert.Equal(200f, dialog.Position.X);
        Assert.Equal(200f, dialog.Position.Y);
    }

    [Fact]
    public void UICanvas_ScreenSizeChange_RecentersExistingDialogs()
    {
        var canvas = new UICanvas(MakeInput());
        canvas.ScreenSize = new Vector2(1280f, 720f);
        var dialog = new UIDialog("Title", "Msg", new Vector2(400f, 200f));
        canvas.Add(dialog);

        canvas.ScreenSize = new Vector2(800f, 600f);

        Assert.Equal(200f, dialog.Position.X);
        Assert.Equal(200f, dialog.Position.Y);
    }

    [Fact]
    public void UICanvas_ProcessMouseInput_ClickOnTextInput_ReturnsTrueAndFocuses()
    {
        var inputCtx = MakeInput(mousePos: new Vector2(150f, 45f), leftPressed: true);
        var canvas = new UICanvas(inputCtx);
        var textInput = new UITextInput(new Vector2(100f, 30f), new Vector2(200f, 30f));
        canvas.Add(textInput);

        bool consumed = canvas.ProcessMouseInput(inputCtx, false);

        Assert.True(consumed);
        Assert.True(textInput.IsFocused);
    }

    [Fact]
    public void UICanvas_ProcessMouseInput_TextInputAlreadyFocused_StillConsumes()
    {
        var inputCtx = MakeInput(mousePos: new Vector2(150f, 45f), leftPressed: true);
        var canvas = new UICanvas(inputCtx);
        var textInput = new UITextInput(new Vector2(100f, 30f), new Vector2(200f, 30f));
        canvas.Add(textInput);
        canvas.ProcessMouseInput(inputCtx, false);

        var inputCtxIdle = MakeInput(mousePos: new Vector2(150f, 45f));
        bool consumed = canvas.ProcessMouseInput(inputCtxIdle, false);

        Assert.True(consumed);
    }

    [Fact]
    public void UICanvas_ProcessMouseInput_ConsumedByGame_DoesNotFocusTextInput()
    {
        var inputCtx = MakeInput(mousePos: new Vector2(150f, 45f), leftPressed: true);
        var canvas = new UICanvas(inputCtx);
        var textInput = new UITextInput(new Vector2(100f, 30f), new Vector2(200f, 30f));
        canvas.Add(textInput);

        canvas.ProcessMouseInput(inputCtx, true);

        Assert.False(textInput.IsFocused);
    }

    #endregion

    #region UIPanel

    [Fact]
    public void UIPanel_BlocksInput_DefaultIsFalse()
    {
        var panel = new UIPanel(new Vector2(0f, 0f), new Vector2(200f, 200f));

        Assert.False(panel.BlocksInput);
    }

    [Fact]
    public void UIPanel_BlocksInput_False_MouseOver_DoesNotConsumeInput()
    {
        var inputCtx = MakeInput(mousePos: new Vector2(50f, 50f));
        var canvas = new UICanvas(inputCtx);
        var panel = new UIPanel(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            BlocksInput = false
        };
        canvas.Add(panel);

        bool consumed = canvas.ProcessMouseInput(inputCtx, false);

        Assert.False(consumed);
    }

    [Fact]
    public void UIPanel_BlocksInput_True_MouseOver_ConsumesInput()
    {
        var inputCtx = MakeInput(mousePos: new Vector2(50f, 50f));
        var canvas = new UICanvas(inputCtx);
        var panel = new UIPanel(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            BlocksInput = true
        };
        canvas.Add(panel);

        bool consumed = canvas.ProcessMouseInput(inputCtx, false);

        Assert.True(consumed);
    }

    [Fact]
    public void UIPanel_BlocksInput_True_MouseOutside_DoesNotConsumeInput()
    {
        var inputCtx = MakeInput(mousePos: new Vector2(500f, 500f));
        var canvas = new UICanvas(inputCtx);
        var panel = new UIPanel(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            BlocksInput = true
        };
        canvas.Add(panel);

        bool consumed = canvas.ProcessMouseInput(inputCtx, false);

        Assert.False(consumed);
    }

    [Fact]
    public void UIPanel_BlocksInput_True_WhenDisabled_DoesNotConsumeInput()
    {
        var inputCtx = MakeInput(mousePos: new Vector2(50f, 50f));
        var canvas = new UICanvas(inputCtx);
        var panel = new UIPanel(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            BlocksInput = true,
            Enabled = false
        };
        canvas.Add(panel);

        bool consumed = canvas.ProcessMouseInput(inputCtx, false);

        Assert.False(consumed);
    }

    #endregion

    #region UICanvas Z-ordering

    [Fact]
    public void UICanvas_BringToFront_MovesComponentToEnd()
    {
        var canvas = new UICanvas(MakeInput());
        var a = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        var b = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        var c = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        canvas.Add(a);
        canvas.Add(b);
        canvas.Add(c);

        canvas.BringToFront(a);

        Assert.Same(a, canvas.Components[2]);
    }

    [Fact]
    public void UICanvas_BringToFront_AlreadyAtFront_NoChange()
    {
        var canvas = new UICanvas(MakeInput());
        var a = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        var b = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        canvas.Add(a);
        canvas.Add(b);

        canvas.BringToFront(b);

        Assert.Same(b, canvas.Components[1]);
        Assert.Equal(2, canvas.Components.Count);
    }

    [Fact]
    public void UICanvas_SendToBack_MovesComponentToStart()
    {
        var canvas = new UICanvas(MakeInput());
        var a = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        var b = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        var c = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        canvas.Add(a);
        canvas.Add(b);
        canvas.Add(c);

        canvas.SendToBack(c);

        Assert.Same(c, canvas.Components[0]);
    }

    [Fact]
    public void UICanvas_SendToBack_AlreadyAtBack_NoChange()
    {
        var canvas = new UICanvas(MakeInput());
        var a = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        var b = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));
        canvas.Add(a);
        canvas.Add(b);

        canvas.SendToBack(a);

        Assert.Same(a, canvas.Components[0]);
        Assert.Equal(2, canvas.Components.Count);
    }

    [Fact]
    public void UICanvas_BringToFront_ComponentNotOnCanvas_DoesNotThrow()
    {
        var canvas = new UICanvas(MakeInput());
        var orphan = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));

        var ex = Record.Exception(() => canvas.BringToFront(orphan));

        Assert.Null(ex);
    }

    [Fact]
    public void UICanvas_SendToBack_ComponentNotOnCanvas_DoesNotThrow()
    {
        var canvas = new UICanvas(MakeInput());
        var orphan = new UIPanel(Vector2.Zero, new Vector2(10f, 10f));

        var ex = Record.Exception(() => canvas.SendToBack(orphan));

        Assert.Null(ex);
    }

    #endregion

    #region UILabel size

    [Fact]
    public void UILabel_Render_UpdatesSizeFromMeasuredText()
    {
        var renderer = MakeRenderer(new Vector2(60f, 16f));
        var label = new UILabel("Hello", Vector2.Zero);

        label.Render(renderer);

        label.Size.Should().Be(new Vector2(60f, 16f));
    }

    [Fact]
    public void UILabel_Constructor_InitialSize_UsesCharacterEstimate()
    {
        var label = new UILabel("Hi", Vector2.Zero);

        label.Size.X.Should().BeGreaterThan(0f);
        label.Size.Y.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void UILabel_Contains_AfterRender_UsesRealSize()
    {
        var renderer = MakeRenderer(new Vector2(80f, 16f));
        var label = new UILabel("Test", new Vector2(10f, 10f));

        label.Render(renderer);

        label.Contains(new Vector2(50f, 18f)).Should().BeTrue();
        label.Contains(new Vector2(100f, 18f)).Should().BeFalse();
    }

    #endregion

    #region UIDropdown flip rendering

    [Fact]
    public void UIDropdown_Render_NearBottom_DrawsListAboveHeader()
    {
        var renderer = MakeRenderer();
        var dropdown = new UIDropdown(new Vector2(0, 680), new Vector2(150, 30))
        {
            ScreenHeight = 720f,
            MaxVisibleItems = 3
        };
        for (int i = 0; i < 5; i++)
            dropdown.AddItem($"Item {i}");
        dropdown.Toggle();

        dropdown.Render(renderer);

        renderer.Received().DrawRectangleFilled(
            Arg.Any<float>(),
            Arg.Is<float>(y => y < 680f),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<Color>());
    }

    [Fact]
    public void UIDropdown_Render_NotNearBottom_DrawsListBelowHeader()
    {
        var renderer = MakeRenderer();
        var dropdown = new UIDropdown(new Vector2(0, 10), new Vector2(150, 30))
        {
            ScreenHeight = 720f,
            MaxVisibleItems = 3
        };
        for (int i = 0; i < 5; i++)
            dropdown.AddItem($"Item {i}");
        dropdown.Toggle();

        dropdown.Render(renderer);

        renderer.Received().DrawRectangleFilled(
            Arg.Any<float>(),
            Arg.Is<float>(y => y >= 40f),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<Color>());
    }

    #endregion

    #region UITextInput click-to-cursor placement

    [Fact]
    public void UITextInput_Click_PlacesCursorAtClickedCharacter()
    {
        // Each char measures 10px wide. Text "Hello" = 50px total.
        // Click at screen X = 25 (25px into text) should land after char index 2 ("He|llo").
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(default(string)!).ReturnsForAnyArgs(call =>
        {
            string s = call.Arg<string>();
            return new Vector2(s.Length * 10f, 16f);
        });
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(call =>
        {
            string s = call.ArgAt<string>(0);
            return new Vector2(s.Length * 10f, 16f);
        });

        var input = MakeInput();
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(200f, 30f));
        textInput.Text = "Hello";

        // Focus with a click at X=25 (padding=10, so localX = 25-0-10 = 15; char 1=10px, char 2=20px → between 1 and 2)
        textInput.SetFocused(true, input, clickX: 25f);

        // Cursor is resolved on next Render
        textInput.Render(renderer);

        textInput.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void UITextInput_Click_EmptyText_CursorRemainsZero()
    {
        var renderer = MakeRenderer(new Vector2(0f, 16f));
        var input = MakeInput();
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(200f, 30f));

        textInput.SetFocused(true, input, clickX: 50f);
        textInput.Render(renderer);

        textInput.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void UITextInput_ClickAtStart_CursorIsZero()
    {
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(default(string)!).ReturnsForAnyArgs(call =>
            new Vector2(call.Arg<string>().Length * 10f, 16f));
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(call =>
            new Vector2(call.ArgAt<string>(0).Length * 10f, 16f));

        var input = MakeInput();
        var textInput = new UITextInput(new Vector2(0f, 0f), new Vector2(200f, 30f));
        textInput.Text = "Hello";

        // Click at padding (10f) → localX = 0 → before first char
        textInput.SetFocused(true, input, clickX: 10f);
        textInput.Render(renderer);

        textInput.CursorPosition.Should().Be(0);
    }

    #endregion

    #region UIDialog — message word-wrap

    [Fact]
    public void UIDialog_Render_Message_UsesTextRenderOptionsWithMaxWidth()
    {
        var renderer = MakeRenderer();
        var dialog = new UIDialog("Title", "This is a long message that should wrap", new Vector2(300f, 200f));

        dialog.Render(renderer);

        var call = renderer.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "DrawText" &&
                                 c.GetArguments() is [string t, _, _, TextRenderOptions] &&
                                 (string)c.GetArguments()[0] == "This is a long message that should wrap");
        call.Should().NotBeNull();
        var opts = (TextRenderOptions)call!.GetArguments()[3];
        opts.MaxWidth.Should().HaveValue();
        opts.MaxWidth!.Value.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void UIDialog_MessageMaxWidth_WhenZero_UsesContentWidth()
    {
        var renderer = MakeRenderer();
        var dialog = new UIDialog("Title", "Message", new Vector2(300f, 200f))
        {
            Padding = 20f,
            MessageMaxWidth = 0f
        };

        dialog.Render(renderer);

        // MaxWidth should default to Size.X - Padding*2 = 300 - 40 = 260
        var call = renderer.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "DrawText" &&
                                 c.GetArguments() is [string, _, _, TextRenderOptions] &&
                                 (string)c.GetArguments()[0] == "Message");
        call.Should().NotBeNull();
        var opts = (TextRenderOptions)call!.GetArguments()[3];
        opts.MaxWidth.Should().HaveValue();
        opts.MaxWidth!.Value.Should().BeApproximately(260f, 0.01f);
    }

    [Fact]
    public void UIDialog_MessageMaxWidth_WhenSet_UsesCustomWidth()
    {
        var renderer = MakeRenderer();
        var dialog = new UIDialog("Title", "Message", new Vector2(300f, 200f))
        {
            MessageMaxWidth = 150f
        };

        dialog.Render(renderer);

        var call = renderer.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "DrawText" &&
                                 c.GetArguments() is [string, _, _, TextRenderOptions] &&
                                 (string)c.GetArguments()[0] == "Message");
        call.Should().NotBeNull();
        var opts = (TextRenderOptions)call!.GetArguments()[3];
        opts.MaxWidth.Should().HaveValue();
        opts.MaxWidth!.Value.Should().BeApproximately(150f, 0.01f);
    }

    #endregion

    #region UICheckbox — renderer-accurate size

    [Fact]
    public void UICheckbox_Render_UpdatesSizeFromMeasuredLabel()
    {
        // Label "Option" should get a rendered width; box = 20, spacing = 10, label = 40 → total = 70
        var renderer = MakeRenderer(new Vector2(40f, 16f));
        var checkbox = new UICheckbox("Option", new Vector2(0f, 0f));

        checkbox.Render(renderer);

        checkbox.Size.X.Should().BeApproximately(70f, 0.01f);
    }

    #endregion

    #region UIRadioButton — renderer-accurate size

    [Fact]
    public void UIRadioButton_Render_UpdatesSizeFromMeasuredLabel()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("Option A", group, new Vector2(0f, 0f));

        rb.Render(renderer);

        // ButtonSize(20) + LabelSpacing(10) + labelWidth(50) = 80
        rb.Size.X.Should().BeApproximately(80f, 0.01f);
    }

    #endregion

    #region UIButton — DisabledColor

    [Fact]
    public void UIButton_Render_WhenDisabled_UsesDisabledColor()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var disabledColor = new Color(30, 30, 30, 255);
        var button = new UIButton("X", new Vector2(0f, 0f), new Vector2(80f, 30f))
        {
            Enabled = false,
            DisabledColor = disabledColor
        };

        button.Render(renderer);

        renderer.Received(1).DrawRectangleFilled(0f, 0f, 80f, 30f, disabledColor);
    }

    [Fact]
    public void UIButton_Render_WhenEnabled_DoesNotUseDisabledColor()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var disabledColor = new Color(30, 30, 30, 255);
        var normalColor = new Color(70, 70, 70, 255);
        var button = new UIButton("X", new Vector2(0f, 0f), new Vector2(80f, 30f))
        {
            Enabled = true,
            DisabledColor = disabledColor,
            NormalColor = normalColor
        };

        button.Render(renderer);

        renderer.Received(1).DrawRectangleFilled(0f, 0f, 80f, 30f, normalColor);
        renderer.DidNotReceive().DrawRectangleFilled(0f, 0f, 80f, 30f, disabledColor);
    }

    #endregion

    #region UIButton — TextAlignment

    [Fact]
    public void UIButton_Render_DefaultAlignment_CentersText()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var button = new UIButton("OK", new Vector2(100f, 100f), new Vector2(200f, 50f));

        button.Render(renderer);

        // textX = 100 + (200 - 20) / 2 = 190
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "OK")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[1]!).Should().BeApproximately(190f, 0.5f);
    }

    [Fact]
    public void UIButton_Render_LeftAlignment_PositionsAtPadding()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var button = new UIButton("OK", new Vector2(100f, 100f), new Vector2(200f, 50f))
        {
            TextAlignment = Brine2D.Rendering.Text.TextAlignment.Left,
            TextPadding = 10f
        };

        button.Render(renderer);

        // textX = 100 + 10 = 110
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "OK")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[1]!).Should().BeApproximately(110f, 0.5f);
    }

    [Fact]
    public void UIButton_Render_RightAlignment_PositionsAtRightEdgeMinusPadding()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var button = new UIButton("OK", new Vector2(100f, 100f), new Vector2(200f, 50f))
        {
            TextAlignment = Brine2D.Rendering.Text.TextAlignment.Right,
            TextPadding = 10f
        };

        button.Render(renderer);

        // textX = 100 + 200 - 20 - 10 = 270
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "OK")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[1]!).Should().BeApproximately(270f, 0.5f);
    }

    #endregion

    #region UIProgressBar — percentage text centering

    [Fact]
    public void UIProgressBar_Render_PercentageText_IsCenteredUsingMeasureText()
    {
        var renderer = MakeRenderer(new Vector2(24f, 16f));
        var bar = new UIProgressBar(new Vector2(0f, 0f), new Vector2(200f, 30f))
        {
            ShowPercentage = true,
            Value = 0.5f
        };

        bar.Render(renderer);

        // textX = 0 + (200 - 24) / 2 = 88, textY = 0 + (30 - 16) / 2 = 7
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "50%")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[1]!).Should().BeApproximately(88f, 0.5f);
        ((float)calls[0].GetArguments()[2]!).Should().BeApproximately(7f, 0.5f);
    }

    #endregion

    #region UISlider — vertical orientation

    [Fact]
    public void UISlider_Vertical_Render_DrawsTrackVertically()
    {
        var renderer = MakeRenderer();
        var slider = new UISlider(new Vector2(0f, 0f), new Vector2(20f, 100f))
        {
            Orientation = SliderOrientation.Vertical,
            ShowValue = false
        };

        slider.Render(renderer);

        // Track drawn at narrowed X, full Y height
        float trackWidth = 20f * 0.3f;
        float trackX = (20f - trackWidth) / 2f;
        renderer.Received(1).DrawRectangleFilled(Arg.Is<float>(v => v == trackX), Arg.Is<float>(v => v == 0f), Arg.Is<float>(v => v == trackWidth), Arg.Is<float>(v => v == 100f), Arg.Any<Color>());
    }

    [Fact]
    public void UISlider_Vertical_UpdateDrag_TopOfTrackIsMaxValue()
    {
        var slider = new UISlider(new Vector2(0f, 0f), new Vector2(20f, 100f))
        {
            Orientation = SliderOrientation.Vertical,
            MinValue = 0f,
            MaxValue = 1f
        };

        slider.StartDrag();
        slider.UpdateDrag(new Vector2(10f, 0f));

        slider.Value.Should().BeApproximately(1f, 0.01f);
    }

    [Fact]
    public void UISlider_Vertical_UpdateDrag_BottomOfTrackIsMinValue()
    {
        var slider = new UISlider(new Vector2(0f, 0f), new Vector2(20f, 100f))
        {
            Orientation = SliderOrientation.Vertical,
            MinValue = 0f,
            MaxValue = 1f
        };

        slider.StartDrag();
        slider.UpdateDrag(new Vector2(10f, 100f));

        slider.Value.Should().BeApproximately(0f, 0.01f);
    }

    [Fact]
    public void UISlider_Horizontal_UpdateDrag_RightIsMaxValue()
    {
        var slider = new UISlider(new Vector2(0f, 0f), new Vector2(100f, 20f))
        {
            Orientation = SliderOrientation.Horizontal,
            MinValue = 0f,
            MaxValue = 1f
        };

        slider.StartDrag();
        slider.UpdateDrag(new Vector2(100f, 10f));

        slider.Value.Should().BeApproximately(1f, 0.01f);
    }

    #endregion

    #region UILabel — MaxWidth wrapping

    [Fact]
    public void UILabel_Render_MaxWidthZero_PassesNullMaxWidthToOptions()
    {
        var renderer = MakeRenderer(new Vector2(60f, 16f));
        var label = new UILabel("Hello world", new Vector2(0f, 0f)) { MaxWidth = 0f };

        label.Render(renderer);

        var call = renderer.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "DrawText" &&
                                 c.GetArguments() is [string t, _, _, TextRenderOptions] &&
                                 (string)c.GetArguments()[0] == "Hello world");
        call.Should().NotBeNull();
        var opts = (TextRenderOptions)call!.GetArguments()[3];
        opts.MaxWidth.Should().BeNull();
    }

    [Fact]
    public void UILabel_Render_MaxWidthSet_PassesValueToOptions()
    {
        var renderer = MakeRenderer(new Vector2(60f, 32f));
        var label = new UILabel("Hello world", new Vector2(0f, 0f)) { MaxWidth = 80f };

        label.Render(renderer);

        var call = renderer.ReceivedCalls()
            .FirstOrDefault(c => c.GetMethodInfo().Name == "DrawText" &&
                                 c.GetArguments() is [string, _, _, TextRenderOptions] &&
                                 (string)c.GetArguments()[0] == "Hello world");
        call.Should().NotBeNull();
        var opts = (TextRenderOptions)call!.GetArguments()[3];
        opts.MaxWidth.Should().HaveValue();
        opts.MaxWidth!.Value.Should().BeApproximately(80f, 0.01f);
    }

    [Fact]
    public void UILabel_Render_MaxWidthSet_SizeIncludesWrappedHeight()
    {
        var renderer = MakeRenderer(new Vector2(60f, 32f));
        var label = new UILabel("Hello world", new Vector2(0f, 0f)) { MaxWidth = 80f };

        label.Render(renderer);

        label.Size.Y.Should().Be(32f);
    }

    #endregion

    #region UIDialog — AllowEscapeClose

    [Fact]
    public void UIDialog_AllowEscapeClose_DefaultIsTrue()
    {
        var dialog = new UIDialog("Title", "Msg", new Vector2(300f, 150f));
        dialog.AllowEscapeClose.Should().BeTrue();
    }

    [Fact]
    public void UIDialog_EscapeDismiss_WhenAllowed_FiresOnEscapeDismissed()
    {
        var dialog = new UIDialog("Title", "Msg", new Vector2(300f, 150f));
        bool fired = false;
        dialog.OnEscapeDismissed += () => fired = true;

        dialog.EscapeDismiss();

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIDialog_EscapeDismiss_WhenNotAllowed_DoesNotFireEvent()
    {
        var dialog = new UIDialog("Title", "Msg", new Vector2(300f, 150f))
        {
            AllowEscapeClose = false
        };
        bool fired = false;
        dialog.OnEscapeDismissed += () => fired = true;

        dialog.EscapeDismiss();

        fired.Should().BeFalse();
    }

    #endregion

    #region UIDialog — IsDraggable / IsOverTitleBar

    [Fact]
    public void UIDialog_IsDraggable_DefaultIsFalse()
    {
        var dialog = new UIDialog("Title", "Msg", new Vector2(300f, 150f));
        dialog.IsDraggable.Should().BeFalse();
    }

    [Fact]
    public void UIDialog_IsOverTitleBar_ReturnsTrueWhenInsideTitleBar()
    {
        var dialog = new UIDialog("Title", "Msg", new Vector2(300f, 150f));
        dialog.CenterOnScreen(new Vector2(800f, 600f));
        var insideTitleBar = dialog.Position + new Vector2(50f, 10f);

        dialog.IsOverTitleBar(insideTitleBar).Should().BeTrue();
    }

    [Fact]
    public void UIDialog_IsOverTitleBar_ReturnsFalseWhenBelowTitleBar()
    {
        var dialog = new UIDialog("Title", "Msg", new Vector2(300f, 150f));
        dialog.CenterOnScreen(new Vector2(800f, 600f));
        var belowTitleBar = dialog.Position + new Vector2(50f, dialog.TitleBarHeight + 10f);

        dialog.IsOverTitleBar(belowTitleBar).Should().BeFalse();
    }

    #endregion

    #region UITextInput — password masking

    [Fact]
    public void UITextInput_IsPassword_RendersmaskedText()
    {
        var renderer = MakeRenderer(new Vector2(10f, 16f));
        var input = new UITextInput(new Vector2(0f, 0f), new Vector2(300f, 30f))
        {
            Text = "secret",
            IsPassword = true,
            MaskChar = '*'
        };

        input.Render(renderer);

        renderer.Received().DrawText(
            Arg.Is<string>(s => s == "******"),
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
        renderer.DidNotReceive().DrawText(
            Arg.Is<string>(s => s == "secret"),
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    [Fact]
    public void UITextInput_IsPassword_False_RendersPlainText()
    {
        var renderer = MakeRenderer(new Vector2(10f, 16f));
        var input = new UITextInput(new Vector2(0f, 0f), new Vector2(300f, 30f))
        {
            Text = "visible",
            IsPassword = false
        };

        input.Render(renderer);

        renderer.Received().DrawText(
            Arg.Is<string>(s => s == "visible"),
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    [Fact]
    public void UITextInput_IsPassword_DefaultMaskCharIsBullet()
    {
        var input = new UITextInput(Vector2.Zero, new Vector2(200f, 30f));
        input.MaskChar.Should().Be('●');
    }

    #endregion

    #region UITextInput — undo / redo

    [Fact]
    public void UITextInput_Undo_RestoresPreviousText()
    {
        var input = Substitute.For<IInputContext>();
        var textInput = new UITextInput(Vector2.Zero, new Vector2(200f, 30f));
        textInput.SetFocused(true, input);

        SetupTyping(input, "H");
        textInput.HandleTextInput(input);
        SetupTyping(input, "i");
        textInput.HandleTextInput(input);

        textInput.Undo();

        textInput.Text.Should().Be("H");
    }

    [Fact]
    public void UITextInput_Undo_EmptyStack_DoesNotThrow()
    {
        var textInput = new UITextInput(Vector2.Zero, new Vector2(200f, 30f));
        var act = () => textInput.Undo();
        act.Should().NotThrow();
    }

    [Fact]
    public void UITextInput_Redo_AfterUndo_RestoresText()
    {
        var input = Substitute.For<IInputContext>();
        var textInput = new UITextInput(Vector2.Zero, new Vector2(200f, 30f));
        textInput.SetFocused(true, input);

        SetupTyping(input, "A");
        textInput.HandleTextInput(input);

        textInput.Undo();
        textInput.Text.Should().Be(string.Empty);

        textInput.Redo();
        textInput.Text.Should().Be("A");
    }

    [Fact]
    public void UITextInput_NewTypingAfterUndo_ClearsRedoStack()
    {
        var input = Substitute.For<IInputContext>();
        var textInput = new UITextInput(Vector2.Zero, new Vector2(200f, 30f));
        textInput.SetFocused(true, input);

        SetupTyping(input, "A");
        textInput.HandleTextInput(input);

        textInput.Undo();

        SetupTyping(input, "B");
        textInput.HandleTextInput(input);

        textInput.RedoStackDepthForTest.Should().Be(0);
    }

    [Fact]
    public void UITextInput_ClearUndoHistory_EmptiesBothStacks()
    {
        var input = Substitute.For<IInputContext>();
        var textInput = new UITextInput(Vector2.Zero, new Vector2(200f, 30f));
        textInput.SetFocused(true, input);

        SetupTyping(input, "X");
        textInput.HandleTextInput(input);
        textInput.Undo();

        textInput.ClearUndoHistory();

        textInput.UndoStackDepthForTest.Should().Be(0);
        textInput.RedoStackDepthForTest.Should().Be(0);
    }

    private static void SetupTyping(IInputContext input, string text)
    {
        input.GetTextInput().Returns(text);
        input.IsKeyDown(Arg.Any<Key>()).Returns(false);
        input.IsKeyPressed(Arg.Any<Key>()).Returns(false);
        input.IsBackspacePressed().Returns(false);
        input.IsDeletePressed().Returns(false);
        input.IsReturnPressed().Returns(false);
    }

    #endregion

    #region UITabContainer — scrollable tab bar

    [Fact]
    public void UITabContainer_NotScrollable_WhenTabsFitWithinMinTabWidth()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(400f, 200f))
        {
            MinTabWidth = 80f
        };
        container.AddTab("A");
        container.AddTab("B");

        container.IsScrollableForTest.Should().BeFalse();
    }

    [Fact]
    public void UITabContainer_Scrollable_WhenTabsWouldBeSmallerThanMinTabWidth()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            MinTabWidth = 80f
        };
        for (int i = 0; i < 5; i++)
            container.AddTab($"Tab{i}");

        container.IsScrollableForTest.Should().BeTrue();
    }

    [Fact]
    public void UITabContainer_ScrollableTabBar_LeftArrowClick_DecrementsOffset()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            MinTabWidth = 80f,
            TabArrowWidth = 20f
        };
        for (int i = 0; i < 5; i++)
            container.AddTab($"Tab{i}");

        // Scroll right first
        container.SelectTab(new Vector2(190f, 15f));
        var afterRight = container.TabScrollOffsetForTest;

        // Click left arrow
        container.SelectTab(new Vector2(5f, 15f));

        container.TabScrollOffsetForTest.Should().Be(afterRight - 1);
    }

    [Fact]
    public void UITabContainer_ScrollableTabBar_RightArrowClick_IncrementsOffset()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            MinTabWidth = 80f,
            TabArrowWidth = 20f
        };
        for (int i = 0; i < 5; i++)
            container.AddTab($"Tab{i}");

        container.SelectTab(new Vector2(190f, 15f));

        container.TabScrollOffsetForTest.Should().BeGreaterThan(0);
    }

    [Fact]
    public void UITabContainer_ScrollableTabBar_OffsetClampsToZero()
    {
        var container = new UITabContainer(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            MinTabWidth = 80f,
            TabArrowWidth = 20f
        };
        for (int i = 0; i < 5; i++)
            container.AddTab($"Tab{i}");

        // Clicking left arrow at offset 0 should not go negative
        container.SelectTab(new Vector2(5f, 15f));

        container.TabScrollOffsetForTest.Should().Be(0);
    }

    #endregion

    #region UIScrollView — scrollbar hover color

    [Fact]
    public void UIScrollView_Scrollbar_UsesScrollbarColorWhenNotHovered()
    {
        var renderer = MakeRenderer();
        var scrollView = new UIScrollView(new Vector2(0f, 0f), new Vector2(200f, 200f))
        {
            ShowVerticalScrollbar = true,
            ContentHeight = 500f,
            ScrollbarColor = new Color(100, 100, 100),
            ScrollbarHoverColor = new Color(200, 200, 200)
        };

        scrollView.Render(renderer);

        renderer.Received().DrawRectangleFilled(
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(),
            new Color(100, 100, 100));
    }

    #endregion
}

// ---------------------------------------------------------------------------
// Additions for review-fix items
// ---------------------------------------------------------------------------

public class UIReviewFixTests
{
    private static IRenderer MakeRenderer(Vector2? measuredTextSize = null)
    {
        var size = measuredTextSize ?? new Vector2(40f, 16f);
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(Arg.Any<string>()).Returns(size);
        renderer.MeasureText(Arg.Any<string>(), Arg.Any<float?>()).Returns(size);
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(size);
        return renderer;
    }

    #region UIProgressBar — BorderThickness

    [Fact]
    public void UIProgressBar_BorderThickness_DefaultIs2()
    {
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20));
        bar.BorderThickness.Should().Be(2f);
    }

    [Fact]
    public void UIProgressBar_BorderThickness_UsedForBorderDrawCalls()
    {
        var renderer = MakeRenderer();
        var bar = new UIProgressBar(new Vector2(10, 10), new Vector2(100, 20))
        {
            Value = 0.5f,
            ShowPercentage = false,
            BorderThickness = 4f
        };

        bar.Render(renderer);

        renderer.Received().DrawRectangleFilled(
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), 4f, Arg.Any<Color>());
    }

    #endregion

    #region UITextInput — CursorHeight

    [Fact]
    public void UITextInput_CursorHeight_DefaultIs16()
    {
        var ti = new UITextInput(Vector2.Zero, new Vector2(200, 30));
        ti.CursorHeight.Should().Be(16f);
    }

    [Fact]
    public void UITextInput_CursorHeight_UsedForCursorBarHeight()
    {
        var renderer = MakeRenderer();
        var input = Substitute.For<IInputContext>();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            Text = "Hi",
            CursorHeight = 24f
        };
        ti.SetFocused(true, input);
        ti.Render(renderer);

        renderer.Received().DrawRectangleFilled(
            Arg.Any<float>(), Arg.Any<float>(), 2f, 24f, Arg.Any<Color>());
    }

    #endregion

    #region UITabContainer — RenameTab / GetContentOrigin

    [Fact]
    public void UITabContainer_RenameTab_UpdatesTitle()
    {
        var container = new UITabContainer(new Vector2(0, 0), new Vector2(300, 200));
        container.AddTab("Old");

        container.RenameTab(0, "New");

        container.GetTabTitle(0).Should().Be("New");
    }

    [Fact]
    public void UITabContainer_RenameTab_OutOfRange_DoesNotThrow()
    {
        var container = new UITabContainer(new Vector2(0, 0), new Vector2(300, 200));
        container.AddTab("A");

        var act = () => container.RenameTab(5, "X");
        act.Should().NotThrow();
        container.GetTabTitle(0).Should().Be("A");
    }

    [Fact]
    public void UITabContainer_GetContentOrigin_ReturnsPositionBelowTabBar()
    {
        var container = new UITabContainer(new Vector2(100, 50), new Vector2(300, 200))
        {
            TabHeight = 30f
        };

        var origin = container.GetContentOrigin();

        origin.Should().Be(new System.Numerics.Vector2(100, 80));
    }

    #endregion

    #region UIPanel — children API

    [Fact]
    public void UIPanel_AddChild_ChildIsReturnedFromGetChildren()
    {
        var panel = new UIPanel(Vector2.Zero, new Vector2(200, 100));
        var label = new UILabel("Hi", new Vector2(10, 10));

        panel.AddChild(label);

        panel.GetChildren().Should().ContainSingle().Which.Should().BeSameAs(label);
    }

    [Fact]
    public void UIPanel_RemoveChild_ChildIsGone()
    {
        var panel = new UIPanel(Vector2.Zero, new Vector2(200, 100));
        var label = new UILabel("Hi", new Vector2(10, 10));
        panel.AddChild(label);

        panel.RemoveChild(label);

        panel.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void UIPanel_ClearChildren_RemovesAll()
    {
        var panel = new UIPanel(Vector2.Zero, new Vector2(200, 100));
        panel.AddChild(new UILabel("A", Vector2.Zero));
        panel.AddChild(new UILabel("B", Vector2.Zero));

        panel.ClearChildren();

        panel.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void UIPanel_Render_CallsChildRender()
    {
        var renderer = MakeRenderer();
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(200, 100));
        var label = new UILabel("Test", new Vector2(10, 10));
        panel.AddChild(label);

        panel.Render(renderer);

        renderer.ReceivedWithAnyArgs().DrawText(default(string)!, default, default, new TextRenderOptions());
    }

    [Fact]
    public void UIPanel_Render_HiddenChild_NotRendered()
    {
        var renderer = MakeRenderer();
        var panel = new UIPanel(Vector2.Zero, new Vector2(200, 100));
        var label = new UILabel("X", Vector2.Zero) { Visible = false };
        panel.AddChild(label);

        panel.Render(renderer);

        renderer.DidNotReceiveWithAnyArgs().DrawText(default!, default, default, default(Color));
    }

    #endregion

    #region UIDialog — children API

    [Fact]
    public void UIDialog_AddChild_ChildReturnedFromGetChildren()
    {
        var dialog = new UIDialog("T", "M", new System.Numerics.Vector2(400, 200));
        var label = new UILabel("Info", new System.Numerics.Vector2(10, 10));

        dialog.AddChild(label);

        dialog.GetChildren().Should().ContainSingle().Which.Should().BeSameAs(label);
    }

    [Fact]
    public void UIDialog_RemoveChild_ChildRemoved()
    {
        var dialog = new UIDialog("T", "M", new System.Numerics.Vector2(400, 200));
        var label = new UILabel("Info", System.Numerics.Vector2.Zero);
        dialog.AddChild(label);

        dialog.RemoveChild(label);

        dialog.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void UIDialog_ClearChildren_RemovesAll()
    {
        var dialog = new UIDialog("T", "M", new System.Numerics.Vector2(400, 200));
        dialog.AddChild(new UILabel("A", System.Numerics.Vector2.Zero));
        dialog.AddChild(new UILabel("B", System.Numerics.Vector2.Zero));

        dialog.ClearChildren();

        dialog.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void UIDialog_Move_ChildRendersAtDialogPlusLocalPosition()
    {
        var dialog = new UIDialog("T", "M", new System.Numerics.Vector2(400, 200));
        dialog.CenterOnScreen(new System.Numerics.Vector2(1280, 720));
        var label = new UILabel("X", new System.Numerics.Vector2(50, 60));
        dialog.AddChild(label);

        var initialDialogPos = dialog.Position;
        dialog.Position += new System.Numerics.Vector2(20, 10);

        // Child is stored in dialog-local coords; it should not have moved in local space.
        label.Position.Should().Be(new System.Numerics.Vector2(50, 60));
        // Dialog position reflects the move.
        dialog.Position.Should().Be(initialDialogPos + new System.Numerics.Vector2(20, 10));
    }

    [Fact]
    public void UIDialog_Render_CallsChildRender()
    {
        var renderer = MakeRenderer();
        var dialog = new UIDialog("T", "M", new System.Numerics.Vector2(400, 200));
        var label = new UILabel("Child", new System.Numerics.Vector2(10, 10));
        dialog.AddChild(label);

        dialog.Render(renderer);

        renderer.ReceivedWithAnyArgs().DrawText(default(string)!, default, default, new TextRenderOptions());
    }

    #endregion

    #region UILabel / UIImage — OnClick

    [Fact]
    public void UILabel_OnClick_CanBeSubscribed()
    {
        var label = new UILabel("Click me", new System.Numerics.Vector2(10, 10));
        bool fired = false;
        label.OnClick += () => fired = true;

        label.Click();

        fired.Should().BeTrue();
    }

    [Fact]
    public void UILabel_OnClick_NotFiredWhenDisabled()
    {
        var label = new UILabel("X", System.Numerics.Vector2.Zero) { Enabled = false };
        bool fired = false;
        label.OnClick += () => fired = true;

        label.Click();

        fired.Should().BeFalse();
    }

    [Fact]
    public void UIImage_OnClick_CanBeSubscribed()
    {
        var image = new UIImage(null, new System.Numerics.Vector2(0, 0), new System.Numerics.Vector2(50, 50));
        bool fired = false;
        image.OnClick += () => fired = true;

        image.Click();

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIImage_OnClick_NotFiredWhenDisabled()
    {
        var image = new UIImage(null, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(50, 50)) { Enabled = false };
        bool fired = false;
        image.OnClick += () => fired = true;

        image.Click();

        fired.Should().BeFalse();
    }

    #endregion

    #region Widget focus state — visual

    [Fact]
    public void UIButton_IsFocused_DefaultIsFalse()
    {
        var btn = new UIButton("X", System.Numerics.Vector2.Zero, new System.Numerics.Vector2(80, 30));
        btn.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void UIButton_SetFocused_SetsIsFocused()
    {
        var btn = new UIButton("X", System.Numerics.Vector2.Zero, new System.Numerics.Vector2(80, 30));
        btn.SetFocused(true);
        btn.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void UIButton_SetFocused_DisabledButton_IsFocusedRemainsfalse()
    {
        var btn = new UIButton("X", System.Numerics.Vector2.Zero, new System.Numerics.Vector2(80, 30)) { Enabled = false };
        btn.SetFocused(true);
        btn.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void UICheckbox_SetFocused_SetsIsFocused()
    {
        var cb = new UICheckbox("A", System.Numerics.Vector2.Zero);
        cb.SetFocused(true);
        cb.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void UISlider_SetFocused_SetsIsFocused()
    {
        var sl = new UISlider(System.Numerics.Vector2.Zero, new System.Numerics.Vector2(100, 20));
        sl.SetFocused(true);
        sl.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void UISlider_NudgeValue_IncreasesValue()
    {
        var sl = new UISlider(System.Numerics.Vector2.Zero, new System.Numerics.Vector2(100, 20))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Value = 50f,
            Step = 5f
        };

        sl.NudgeValue(1f);

        sl.Value.Should().BeApproximately(55f, 0.001f);
    }

    [Fact]
    public void UISlider_NudgeValue_DecreasesValue()
    {
        var sl = new UISlider(System.Numerics.Vector2.Zero, new System.Numerics.Vector2(100, 20))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Value = 50f,
            Step = 5f
        };

        sl.NudgeValue(-1f);

        sl.Value.Should().BeApproximately(45f, 0.001f);
    }

    [Fact]
    public void UISlider_NudgeValue_ClampsAtMax()
    {
        var sl = new UISlider(System.Numerics.Vector2.Zero, new System.Numerics.Vector2(100, 20))
        {
            MinValue = 0f, MaxValue = 10f, Value = 10f, Step = 1f
        };

        sl.NudgeValue(1f);

        sl.Value.Should().Be(10f);
    }

    [Fact]
    public void UISlider_NudgeValue_UsesOnePercentWhenStepIsZero()
    {
        var sl = new UISlider(System.Numerics.Vector2.Zero, new System.Numerics.Vector2(100, 20))
        {
            MinValue = 0f, MaxValue = 100f, Value = 50f, Step = 0f
        };

        sl.NudgeValue(1f);

        sl.Value.Should().BeApproximately(51f, 0.001f);
    }

    [Fact]
    public void UIRadioButton_SetFocused_SetsIsFocused()
    {
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("A", group, System.Numerics.Vector2.Zero);
        rb.SetFocused(true);
        rb.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void UIDropdown_SetFocused_SetsIsFocused()
    {
        var dd = new UIDropdown(System.Numerics.Vector2.Zero, new System.Numerics.Vector2(150, 30));
        dd.SetFocused(true);
        dd.IsFocused.Should().BeTrue();
    }

    #endregion

    private static void SetupTyping(IInputContext input, string text)
    {
        input.GetTextInput().Returns(text);
        input.IsBackspacePressed().Returns(false);
        input.IsDeletePressed().Returns(false);
        input.IsReturnPressed().Returns(false);
        input.IsKeyPressed(Arg.Any<Key>()).Returns(false);
        input.IsKeyDown(Arg.Any<Key>()).Returns(false);
    }

    #region UICheckbox vertical centering

    [Fact]
    public void UICheckbox_Render_LabelVerticallycenteredUsingMeasureText()
    {
        var renderer = MakeRenderer(new Vector2(60f, 14f));
        var checkbox = new UICheckbox("Label", new Vector2(10f, 20f));
        checkbox.BoxSize = 20f;

        checkbox.RecalculateSize(renderer);
        checkbox.Render(renderer);

        // labelY = 20 + (20 - 14) / 2 = 23
        float expectedY = 20f + (20f - 14f) / 2f;
        bool found = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IRenderer.DrawText) && c.GetArguments().Length == 4)
            .Any(c => c.GetArguments()[2] is float y && MathF.Abs(y - expectedY) < 0.001f);
        found.Should().BeTrue(because: $"expected a DrawText call with y ≈ {expectedY}");
    }

    [Fact]
    public void UICheckbox_Render_LabelY_DoesNotUse_MagicEight()
    {
        var renderer = MakeRenderer(new Vector2(60f, 20f));
        var checkbox = new UICheckbox("X", new Vector2(0f, 100f));
        checkbox.BoxSize = 20f;

        checkbox.RecalculateSize(renderer);
        checkbox.Render(renderer);

        // Old formula: 100 + 10 - 8 = 102. New formula: 100 + (20 - 20) / 2 = 100.
        float wrongY = 100f + (20f / 2f) - 8f;
        bool found = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IRenderer.DrawText) && c.GetArguments().Length == 4)
            .Any(c => c.GetArguments()[2] is float y && MathF.Abs(y - wrongY) < 0.001f);
        found.Should().BeFalse(because: $"DrawText should not have been called with the old magic-8 y={wrongY}");
    }

    #endregion

    #region UIDialog title vertical centering

    [Fact]
    public void UIDialog_Render_TitleVerticallycenteredUsingMeasureText()
    {
        var renderer = MakeRenderer(new Vector2(80f, 14f));
        var dialog = new UIDialog("Hello", "", new Vector2(400f, 200f));
        dialog.TitleBarHeight = 40f;
        dialog.Padding = 10f;
        dialog.ShowOverlay = false;

        dialog.Render(renderer);

        // titleY = posY + (40 - 14) / 2 = posY + 13
        float titleBarY = dialog.Position.Y + (40f - 14f) / 2f;
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "Hello")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[2]!).Should().BeApproximately(titleBarY, 0.5f);
    }

    [Fact]
    public void UIDialog_Render_TitleY_DoesNotUse_MagicEight()
    {
        // textH=14: old formula = posY + 40/2 - 8 = posY+12; new = posY + (40-14)/2 = posY+13.
        var renderer = MakeRenderer(new Vector2(80f, 14f));
        var dialog = new UIDialog("T", "", new Vector2(400f, 200f));
        dialog.TitleBarHeight = 40f;
        dialog.ShowOverlay = false;

        dialog.Render(renderer);

        float wrongY = dialog.Position.Y + (40f / 2f) - 8f;
        float correctY = dialog.Position.Y + (40f - 14f) / 2f;
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "T")
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[2]!).Should().NotBeApproximately(wrongY, 0.1f);
        ((float)calls[0].GetArguments()[2]!).Should().BeApproximately(correctY, 0.5f);
    }

    #endregion

    #region UITextInput UndoStackLimit

    [Fact]
    public void UITextInput_UndoStackLimit_DefaultIs100()
    {
        var ti = new UITextInput(Vector2.Zero, new Vector2(200f, 30f));
        ti.UndoStackLimit.Should().Be(100);
    }

    [Fact]
    public void UITextInput_UndoStackLimit_DoesNotExceedLimit()
    {
        var input = Substitute.For<IInputContext>();
        var ti = new UITextInput(Vector2.Zero, new Vector2(200f, 30f)) { UndoStackLimit = 3 };
        ti.SetFocused(true, input);

        for (int i = 0; i < 10; i++)
        {
            SetupTyping(input, "a");
            ti.HandleTextInput(input);
        }

        ti.UndoStackDepthForTest.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public void UITextInput_UndoStackLimit_Zero_AllowsUnboundedGrowth()
    {
        var input = Substitute.For<IInputContext>();
        var ti = new UITextInput(Vector2.Zero, new Vector2(200f, 30f)) { UndoStackLimit = 0 };
        ti.SetFocused(true, input);

        for (int i = 0; i < 20; i++)
        {
            SetupTyping(input, "a");
            ti.HandleTextInput(input);
        }

        ti.UndoStackDepthForTest.Should().Be(20);
    }

    [Fact]
    public void UITextInput_UndoStackLimit_OldestEntryDropped()
    {
        var input = Substitute.For<IInputContext>();
        var ti = new UITextInput(Vector2.Zero, new Vector2(200f, 30f)) { UndoStackLimit = 2 };
        ti.SetFocused(true, input);

        // Type three chars: each pushes one undo state; oldest should be dropped at limit.
        SetupTyping(input, "A");
        ti.HandleTextInput(input);
        SetupTyping(input, "B");
        ti.HandleTextInput(input);
        SetupTyping(input, "C");
        ti.HandleTextInput(input);

        // Stack depth capped at 2.
        ti.UndoStackDepthForTest.Should().Be(2);

        // Two undos should recover "AB" then "A" (not empty, proving oldest was dropped).
        ti.Undo();
        ti.Text.Should().Be("AB");
        ti.Undo();
        ti.Text.Should().Be("A");
    }

    #endregion

    #region UITextInput — Ctrl+Left/Right word jump

    [Fact]
    public void UITextInput_CtrlLeft_MovesBackwardOneWord()
    {
        var input = MakeCtrlArrowInput(Key.Left);
        var ti = new UITextInput(Vector2.Zero, new Vector2(300f, 30f))
        {
            Text = "hello world"
        };
        ti.SetFocused(true, MakeInput());
        ti.CursorPosition = 11; // end

        ti.HandleTextInput(input);

        // "hello world" — backward from 11 skips nothing, lands at 6 ("world" start)
        ti.CursorPosition.Should().Be(6);
    }

    [Fact]
    public void UITextInput_CtrlLeft_AtBeginning_StaysAtZero()
    {
        var input = MakeCtrlArrowInput(Key.Left);
        var ti = new UITextInput(Vector2.Zero, new Vector2(300f, 30f))
        {
            Text = "hello"
        };
        ti.SetFocused(true, MakeInput());
        ti.CursorPosition = 0;

        ti.HandleTextInput(input);

        ti.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void UITextInput_CtrlRight_MovesForwardOneWord()
    {
        var input = MakeCtrlArrowInput(Key.Right);
        var ti = new UITextInput(Vector2.Zero, new Vector2(300f, 30f))
        {
            Text = "hello world"
        };
        ti.SetFocused(true, MakeInput());
        ti.CursorPosition = 0;

        ti.HandleTextInput(input);

        // from 0, skips no whitespace, advances through "hello" → lands at 5
        ti.CursorPosition.Should().Be(5);
    }

    [Fact]
    public void UITextInput_CtrlRight_AtEnd_StaysAtEnd()
    {
        var input = MakeCtrlArrowInput(Key.Right);
        var ti = new UITextInput(Vector2.Zero, new Vector2(300f, 30f))
        {
            Text = "hello"
        };
        ti.SetFocused(true, MakeInput());
        ti.CursorPosition = 5;

        ti.HandleTextInput(input);

        ti.CursorPosition.Should().Be(5);
    }

    [Fact]
    public void UITextInput_ShiftCtrlRight_SelectsNextWord()
    {
        var input = MakeCtrlArrowInput(Key.Right, shift: true);
        var ti = new UITextInput(Vector2.Zero, new Vector2(300f, 30f))
        {
            Text = "hello world"
        };
        ti.SetFocused(true, MakeInput());
        ti.CursorPosition = 0;

        ti.HandleTextInput(input);

        ti.HasSelectionForTest().Should().BeTrue();
        var (s, e) = ti.GetSelectionRangeForTest();
        s.Should().Be(0);
        e.Should().Be(5);
    }

    private static IInputContext MakeCtrlArrowInput(Key arrowKey, bool shift = false)
    {
        var input = Substitute.For<IInputContext>();
        input.IsKeyDown(Key.LeftControl).Returns(true);
        input.IsKeyDown(Key.RightControl).Returns(false);
        input.IsKeyDown(Key.LeftShift).Returns(shift);
        input.IsKeyDown(Key.RightShift).Returns(false);
        input.IsKeyPressed(arrowKey).Returns(true);
        input.IsKeyPressed(Arg.Is<Key>(k => k != arrowKey)).Returns(false);
        input.IsBackspacePressed().Returns(false);
        input.IsDeletePressed().Returns(false);
        input.IsReturnPressed().Returns(false);
        input.GetTextInput().Returns(string.Empty);
        return input;
    }

    #endregion

    #region UITextInput — Text setter cursor clamping

    [Fact]
    public void UITextInput_SetText_ShortValue_ClampsCursorToEnd()
    {
        var ti = new UITextInput(Vector2.Zero, new Vector2(200f, 30f))
        {
            Text = "Hello World"
        };
        ti.CursorPosition = 11;

        ti.Text = "Hi";

        ti.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void UITextInput_SetText_EmptyValue_ResetsCursorToZero()
    {
        var ti = new UITextInput(Vector2.Zero, new Vector2(200f, 30f))
        {
            Text = "Some text"
        };
        ti.CursorPosition = 9;

        ti.Text = string.Empty;

        ti.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void UITextInput_SetText_SameLengthOrLonger_PreservesCursor()
    {
        var ti = new UITextInput(Vector2.Zero, new Vector2(200f, 30f))
        {
            Text = "Hello"
        };
        ti.CursorPosition = 3;

        ti.Text = "World!";

        ti.CursorPosition.Should().Be(3);
    }

    #endregion

    #region UIDropdown — keyboard Up/Down navigation

    [Fact]
    public void UIDropdown_NavigateItem_DownOpensListIfClosed()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(150, 30));
        dd.AddItem("A");
        dd.AddItem("B");

        dd.NavigateItem(1);

        dd.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void UIDropdown_NavigateItem_Down_MovesKeyboardCursor()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(150, 30));
        dd.AddItem("A");
        dd.AddItem("B");
        dd.AddItem("C");
        dd.Toggle(); // open

        dd.NavigateItem(1); // cursor moves down

        // Confirm selects whatever cursor is on
        dd.ConfirmKeyboardSelection();
        dd.SelectedText.Should().Be("B");
    }

    [Fact]
    public void UIDropdown_NavigateItem_Up_MovesKeyboardCursorBack()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(150, 30));
        dd.AddItem("A");
        dd.AddItem("B");
        dd.AddItem("C");
        dd.Toggle();
        dd.NavigateItem(1); // at B
        dd.NavigateItem(1); // at C

        dd.NavigateItem(-1); // back to B

        dd.ConfirmKeyboardSelection();
        dd.SelectedText.Should().Be("B");
    }

    [Fact]
    public void UIDropdown_NavigateItem_ClampsAtTop()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(150, 30));
        dd.AddItem("A");
        dd.AddItem("B");
        dd.Toggle();

        dd.NavigateItem(-1); // already at 0; should clamp, not throw

        dd.ConfirmKeyboardSelection();
        dd.SelectedText.Should().Be("A");
    }

    [Fact]
    public void UIDropdown_ConfirmKeyboardSelection_ClosesDropdown()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(150, 30));
        dd.AddItem("X");
        dd.Toggle();

        dd.ConfirmKeyboardSelection();

        dd.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void UIDropdown_SetFocused_False_ClosesDropdown()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(150, 30));
        dd.AddItem("X");
        dd.Toggle();
        dd.SetFocused(true);

        dd.SetFocused(false);

        dd.IsExpanded.Should().BeFalse();
    }

    #endregion

    #region UIScrollView — keyboard focus

    [Fact]
    public void UIScrollView_SetFocused_True_SetsIsFocused()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 200));

        sv.SetFocused(true);

        sv.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void UIScrollView_SetFocused_False_ClearsIsFocused()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 200));
        sv.SetFocused(true);

        sv.SetFocused(false);

        sv.IsFocused.Should().BeFalse();
    }

    [Fact]
    public void UIScrollView_Render_WhenFocused_DrawsFocusRingInFocusColor()
    {
        var renderer = MakeRenderer();
        var focusColor = new Color(120, 180, 255);
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            FocusColor = focusColor
        };
        sv.SetFocused(true);

        sv.Render(renderer);

        renderer.Received().DrawRectangleFilled(
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), 2f, focusColor);
    }

    [Fact]
    public void UIScrollView_Render_WhenNotFocused_DoesNotDrawFocusColor()
    {
        var renderer = MakeRenderer();
        var focusColor = new Color(120, 180, 255);
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            FocusColor = focusColor,
            BorderColor = new Color(80, 80, 80)
        };

        sv.Render(renderer);

        renderer.DidNotReceive().DrawRectangleFilled(
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), focusColor);
    }

    #endregion

    #region Dialog keyboard navigation

    [Fact]
    public void UICanvas_DialogActive_Tab_CyclesFocusThroughDialogButtons()
    {
        var inputCtx = Substitute.For<IInputContext>();
        inputCtx.MousePosition.Returns(Vector2.Zero);
        inputCtx.ScrollWheelDelta.Returns(0f);
        inputCtx.IsMouseButtonPressed(Arg.Any<MouseButton>()).Returns(false);
        inputCtx.IsMouseButtonReleased(Arg.Any<MouseButton>()).Returns(false);
        inputCtx.IsMouseButtonDown(Arg.Any<MouseButton>()).Returns(false);
        inputCtx.IsKeyPressed(Key.Tab).Returns(true);
        inputCtx.IsKeyDown(Key.LeftShift).Returns(false);
        inputCtx.IsKeyDown(Key.RightShift).Returns(false);
        inputCtx.IsKeyPressed(Arg.Is<Key>(k => k != Key.Tab)).Returns(false);

        var canvas = new UICanvas(inputCtx);
        var dialog = new UIDialog("T", "M", new Vector2(400, 200));
        bool clicked = false;
        dialog.AddButton("OK", () => clicked = true);
        canvas.Add(dialog);

        // Tab once — the OK button should get focus
        canvas.ProcessKeyboardInput(inputCtx, false);

        canvas.FocusedWidget.Should().BeOfType<UIButton>();
    }

    [Fact]
    public void UICanvas_DialogActive_EnterActivatesFocusedButton()
    {
        var inputCtx = Substitute.For<IInputContext>();
        inputCtx.MousePosition.Returns(Vector2.Zero);
        inputCtx.ScrollWheelDelta.Returns(0f);
        inputCtx.IsMouseButtonPressed(Arg.Any<MouseButton>()).Returns(false);
        inputCtx.IsMouseButtonReleased(Arg.Any<MouseButton>()).Returns(false);
        inputCtx.IsMouseButtonDown(Arg.Any<MouseButton>()).Returns(false);

        var canvas = new UICanvas(inputCtx);
        var dialog = new UIDialog("T", "M", new Vector2(400, 200));
        bool clicked = false;
        dialog.AddButton("OK", () => clicked = true);
        canvas.Add(dialog);

        // Tab to focus button
        inputCtx.IsKeyPressed(Key.Tab).Returns(true);
        inputCtx.IsKeyPressed(Arg.Is<Key>(k => k != Key.Tab)).Returns(false);
        canvas.ProcessKeyboardInput(inputCtx, false);

        // Enter to activate
        inputCtx.IsKeyPressed(Key.Tab).Returns(false);
        inputCtx.IsKeyPressed(Key.Enter).Returns(true);
        inputCtx.IsKeyPressed(Arg.Is<Key>(k => k != Key.Enter)).Returns(false);
        canvas.ProcessKeyboardInput(inputCtx, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void UICanvas_DialogActive_ProcessKeyboardInput_ReturnsTrue()
    {
        var inputCtx = Substitute.For<IInputContext>();
        inputCtx.MousePosition.Returns(Vector2.Zero);
        inputCtx.ScrollWheelDelta.Returns(0f);
        inputCtx.IsMouseButtonPressed(Arg.Any<MouseButton>()).Returns(false);
        inputCtx.IsMouseButtonReleased(Arg.Any<MouseButton>()).Returns(false);
        inputCtx.IsMouseButtonDown(Arg.Any<MouseButton>()).Returns(false);
        inputCtx.IsKeyPressed(Arg.Any<Key>()).Returns(false);

        var canvas = new UICanvas(inputCtx);
        canvas.Add(new UIDialog("T", "M", new Vector2(400, 200)));

        bool result = canvas.ProcessKeyboardInput(inputCtx, false);

        result.Should().BeTrue();
    }

    #endregion

    #region Dialog UIPanel children input

    [Fact]
    public void UICanvas_DialogPanelChild_ButtonIsClickable()
    {
        // Dialog centered on 1280×720 with size 400×300 → position (440, 210).
        // Panel at dialog-local (200, 100) → screen (640, 310).
        // Button at panel-local (5, 5) with size (90, 40) → screen rect (645, 315, 90, 40).
        // Mouse at (680, 335) is inside the button.
        var inputCtx = Substitute.For<IInputContext>();
        inputCtx.MousePosition.Returns(new Vector2(680f, 335f));
        inputCtx.ScrollWheelDelta.Returns(0f);
        inputCtx.IsMouseButtonPressed(MouseButton.Left).Returns(true);
        inputCtx.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        inputCtx.IsMouseButtonDown(MouseButton.Left).Returns(false);

        var canvas = new UICanvas(inputCtx);
        var dialog = new UIDialog("T", "M", new Vector2(400, 300));
        dialog.CenterOnScreen(new Vector2(1280, 720));

        var panel = new UIPanel(new Vector2(200f, 100f), new Vector2(100f, 50f));
        bool clicked = false;
        var btn = new UIButton("Go", new Vector2(5f, 5f), new Vector2(90f, 40f));
        btn.OnClick += () => clicked = true;
        panel.AddChild(btn);
        dialog.AddChild(panel);
        canvas.Add(dialog);

        // Press
        canvas.ProcessMouseInput(inputCtx, false);

        // Release over button
        inputCtx.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        inputCtx.IsMouseButtonReleased(MouseButton.Left).Returns(true);
        canvas.ProcessMouseInput(inputCtx, false);

        clicked.Should().BeTrue();
    }

    #endregion

    #region UIPanel ClipChildren

    [Fact]
    public void UIPanel_ClipChildrenFalse_NoPushScissor()
    {
        var renderer = MakeRenderer();
        var panel = new UIPanel(new Vector2(50f, 50f), new Vector2(100f, 100f))
        {
            ClipChildren = false
        };

        panel.Render(renderer);

        renderer.DidNotReceive().PushScissorRect(Arg.Any<Brine2D.Core.Rectangle>());
    }

    [Fact]
    public void UIPanel_ClipChildrenTrue_PushesAndPopsScissorRect()
    {
        var renderer = MakeRenderer();
        var panel = new UIPanel(new Vector2(50f, 50f), new Vector2(100f, 100f))
        {
            ClipChildren = true
        };

        panel.Render(renderer);

        renderer.Received(1).PushScissorRect(Arg.Is<Brine2D.Core.Rectangle>(r =>
            r.X == 50f && r.Y == 50f && r.Width == 100f && r.Height == 100f));
        renderer.Received(1).PopScissorRect();
    }

    [Fact]
    public void UIPanel_ClipChildrenTrue_ChildRenderedInsideScissor()
    {
        var renderer = MakeRenderer();
        var pushOrder = new System.Collections.Generic.List<string>();
        renderer.When(r => r.PushScissorRect(Arg.Any<Brine2D.Core.Rectangle>()))
            .Do(_ => pushOrder.Add("push"));
        renderer.When(r => r.DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Brine2D.Core.Color>()))
            .Do(ci =>
            {
                // Only record child rect draws (not the panel background or border itself).
                if (pushOrder.Count == 1)
                    pushOrder.Add("childDraw");
            });
        renderer.When(r => r.PopScissorRect()).Do(_ => pushOrder.Add("pop"));

        var panel = new UIPanel(new Vector2(0f, 0f), new Vector2(200f, 100f))
        {
            ClipChildren = true
        };
        var child = new UIPanel(new Vector2(10f, 10f), new Vector2(50f, 50f));
        panel.AddChild(child);

        panel.Render(renderer);

        pushOrder.Should().ContainInOrder("push", "childDraw", "pop");
    }

    #endregion

    #region UIPanel parent-relative child coordinates

    [Fact]
    public void UIPanel_Render_ChildTranslatedByPanelPosition()
    {
        var renderer = MakeRenderer();
        var panel = new UIPanel(new Vector2(100f, 200f), new Vector2(300f, 200f));
        var child = new UIButton("X", new Vector2(10f, 20f), new Vector2(50f, 30f));
        panel.AddChild(child);

        panel.Render(renderer);

        // Child at local (10, 20) inside panel at (100, 200) → screen (110, 220).
        renderer.Received().DrawRectangleFilled(Arg.Is<float>(v => v == 110f), Arg.Is<float>(v => v == 220f), Arg.Is<float>(v => v == 50f), Arg.Is<float>(v => v == 30f), Arg.Any<Color>());
    }

    [Fact]
    public void UIPanel_Render_ChildPositionNotMutated()
    {
        var renderer = MakeRenderer();
        var panel = new UIPanel(new Vector2(50f, 50f), new Vector2(200f, 200f));
        var child = new UIButton("X", new Vector2(10f, 10f), new Vector2(80f, 30f));
        panel.AddChild(child);

        panel.Render(renderer);

        child.Position.Should().Be(new Vector2(10f, 10f));
    }

    [Fact]
    public void UICanvas_PanelChild_HitTestUsesParentRelativeCoords()
    {
        var inputCtx = Substitute.For<IInputContext>();
        inputCtx.ScrollWheelDelta.Returns(0f);

        var canvas = new UICanvas(inputCtx);

        // Panel at screen (100, 100). Button at panel-local (10, 10), size 80×30.
        // → screen hit area: (110, 110) to (190, 140).
        var panel = new UIPanel(new Vector2(100f, 100f), new Vector2(200f, 150f));
        bool clicked = false;
        var btn = new UIButton("Go", new Vector2(10f, 10f), new Vector2(80f, 30f));
        btn.OnClick += () => clicked = true;
        panel.AddChild(btn);
        canvas.Add(panel);

        // Click at screen (130, 120) — inside the translated button bounds.
        inputCtx.MousePosition.Returns(new Vector2(130f, 120f));
        inputCtx.IsMouseButtonPressed(MouseButton.Left).Returns(true);
        inputCtx.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        inputCtx.IsMouseButtonDown(MouseButton.Left).Returns(true);
        canvas.ProcessMouseInput(inputCtx, false);

        inputCtx.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        inputCtx.IsMouseButtonReleased(MouseButton.Left).Returns(true);
        inputCtx.IsMouseButtonDown(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(inputCtx, false);

        clicked.Should().BeTrue();
    }

    [Fact]
    public void UICanvas_PanelChild_OldAbsoluteCoords_DoesNotHit()
    {
        var inputCtx = Substitute.For<IInputContext>();
        inputCtx.ScrollWheelDelta.Returns(0f);

        var canvas = new UICanvas(inputCtx);

        // Panel at screen (100, 100). Button at panel-local (10, 10), size 80×30.
        // Old absolute coords would have placed the button at screen (10, 10) — clicking there
        // should NOT fire after the fix.
        var panel = new UIPanel(new Vector2(100f, 100f), new Vector2(200f, 150f));
        bool clicked = false;
        var btn = new UIButton("Go", new Vector2(10f, 10f), new Vector2(80f, 30f));
        btn.OnClick += () => clicked = true;
        panel.AddChild(btn);
        canvas.Add(panel);

        inputCtx.MousePosition.Returns(new Vector2(30f, 20f));
        inputCtx.IsMouseButtonPressed(MouseButton.Left).Returns(true);
        inputCtx.IsMouseButtonReleased(MouseButton.Left).Returns(false);
        inputCtx.IsMouseButtonDown(MouseButton.Left).Returns(true);
        canvas.ProcessMouseInput(inputCtx, false);

        inputCtx.IsMouseButtonPressed(MouseButton.Left).Returns(false);
        inputCtx.IsMouseButtonReleased(MouseButton.Left).Returns(true);
        inputCtx.IsMouseButtonDown(MouseButton.Left).Returns(false);
        canvas.ProcessMouseInput(inputCtx, false);

        clicked.Should().BeFalse();
    }

    #endregion

    #region UITextInput double-click word select

    [Fact]
    public void UITextInput_SingleClick_DoesNotSelectWord()
    {
        var input = MakeInput();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            Text = "hello world"
        };
        ti.SetFocused(true, input);

        bool wasDouble = ti.StartMouseDrag(20f);

        wasDouble.Should().BeFalse();
        ti.HasPendingDoubleClickForTest.Should().BeFalse();
    }

    [Fact]
    public void UITextInput_DoubleClick_SetsPendingDoubleClick()
    {
        var input = MakeInput();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            Text = "hello world"
        };
        ti.SetFocused(true, input);

        // First click
        ti.StartMouseDrag(20f);
        // Simulate minimal time passing (less than interval)
        ti.Update(0.1f);
        // Second click at same position
        bool wasDouble = ti.StartMouseDrag(21f);

        wasDouble.Should().BeTrue();
        ti.HasPendingDoubleClickForTest.Should().BeTrue();
    }

    [Fact]
    public void UITextInput_DoubleClick_TooSlow_NotDetected()
    {
        var input = MakeInput();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            Text = "hello world"
        };
        ti.SetFocused(true, input);

        ti.StartMouseDrag(20f);
        ti.Update(0.5f); // exceeds 0.4s interval
        bool wasDouble = ti.StartMouseDrag(21f);

        wasDouble.Should().BeFalse();
    }

    [Fact]
    public void UITextInput_DoubleClick_TooFarApart_NotDetected()
    {
        var input = MakeInput();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30))
        {
            Text = "hello world"
        };
        ti.SetFocused(true, input);

        ti.StartMouseDrag(20f);
        ti.Update(0.1f);
        bool wasDouble = ti.StartMouseDrag(80f); // > 5px away

        wasDouble.Should().BeFalse();
    }

    [Fact]
    public void UITextInput_DoubleClick_Render_SelectsWord()
    {
        var renderer = MakeRenderer(new Vector2(8f, 16f));
        // Simulate each character being 8px wide so positions are predictable.
        renderer.MeasureText(Arg.Any<string>()).Returns(ci =>
        {
            string s = ci.Arg<string>();
            return new Vector2(s.Length * 8f, 16f);
        });

        var input = MakeInput();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(300, 30))
        {
            Text = "hello world"
        };
        ti.SetFocused(true, input);

        // First click
        ti.StartMouseDrag(20f);
        ti.Update(0.1f);
        // Second click (double-click)
        ti.StartMouseDrag(21f);

        // Render resolves the pending double-click
        ti.Render(renderer);

        ti.HasSelectionForTest().Should().BeTrue();
        var (start, end) = ti.GetSelectionRangeForTest();
        start.Should().Be(0); // "hello" starts at 0
        end.Should().Be(5);   // "hello" ends at 5
    }

    [Fact]
    public void UITextInput_DoubleClick_AfterFocusLostAndRegained_DoesNotFalsePositive()
    {
        var renderer = MakeRenderer(new Vector2(8f, 16f));
        var input = Substitute.For<IInputContext>();
        var ti = new UITextInput(new Vector2(0, 0), new Vector2(200, 30));
        ti.Text = "hello world";

        // First session: focus, click, then unfocus
        ti.SetFocused(true, input, 20f);
        ti.StartMouseDrag(20f);
        ti.Update(0.1f);
        ti.SetFocused(false, input);

        // Advance time well past double-click window
        ti.Update(1.0f);

        // Second session: refocus and click in the same position
        ti.SetFocused(true, input, 20f);
        bool wasDoubleClick = ti.StartMouseDrag(20f);

        // Should NOT be treated as a double-click
        wasDoubleClick.Should().BeFalse();
    }

    #endregion

    private static IInputContext MakeInput(Vector2? mousePos = null, bool leftPressed = false, bool leftReleased = false, bool leftDown = false)
    {
        var input = Substitute.For<IInputContext>();
        input.MousePosition.Returns(mousePos ?? Vector2.Zero);
        input.IsMouseButtonPressed(MouseButton.Left).Returns(leftPressed);
        input.IsMouseButtonReleased(MouseButton.Left).Returns(leftReleased);
        input.IsMouseButtonDown(MouseButton.Left).Returns(leftDown);
        input.ScrollWheelDelta.Returns(0f);
        return input;
    }

    #region Focus events – UIButton

    [Fact]
    public void UIButton_SetFocused_True_FiresOnFocusGained()
    {
        var button = new UIButton("A", Vector2.Zero, new Vector2(100, 40));
        bool fired = false;
        button.OnFocusGained += () => fired = true;

        button.SetFocused(true);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIButton_SetFocused_False_FiresOnFocusLost()
    {
        var button = new UIButton("A", Vector2.Zero, new Vector2(100, 40));
        button.SetFocused(true);
        bool fired = false;
        button.OnFocusLost += () => fired = true;

        button.SetFocused(false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIButton_SetFocused_SameValue_DoesNotFireAgain()
    {
        var button = new UIButton("A", Vector2.Zero, new Vector2(100, 40));
        button.SetFocused(true);
        int count = 0;
        button.OnFocusGained += () => count++;

        button.SetFocused(true);

        count.Should().Be(0);
    }

    [Fact]
    public void UIButton_SetFocused_WhenDisabled_DoesNotGainFocus()
    {
        var button = new UIButton("A", Vector2.Zero, new Vector2(100, 40)) { Enabled = false };
        bool fired = false;
        button.OnFocusGained += () => fired = true;

        button.SetFocused(true);

        fired.Should().BeFalse();
        button.IsFocused.Should().BeFalse();
    }

    #endregion

    #region Focus events – UICheckbox

    [Fact]
    public void UICheckbox_SetFocused_True_FiresOnFocusGained()
    {
        var cb = new UICheckbox("Check", Vector2.Zero);
        bool fired = false;
        cb.OnFocusGained += () => fired = true;

        cb.SetFocused(true);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UICheckbox_SetFocused_False_FiresOnFocusLost()
    {
        var cb = new UICheckbox("Check", Vector2.Zero);
        cb.SetFocused(true);
        bool fired = false;
        cb.OnFocusLost += () => fired = true;

        cb.SetFocused(false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UICheckbox_SetFocused_SameValue_DoesNotFireAgain()
    {
        var cb = new UICheckbox("Check", Vector2.Zero);
        cb.SetFocused(true);
        int count = 0;
        cb.OnFocusGained += () => count++;

        cb.SetFocused(true);

        count.Should().Be(0);
    }

    #endregion

    #region Focus events – UISlider

    [Fact]
    public void UISlider_SetFocused_True_FiresOnFocusGained()
    {
        var slider = new UISlider(Vector2.Zero, new Vector2(200, 20));
        bool fired = false;
        slider.OnFocusGained += () => fired = true;

        slider.SetFocused(true);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UISlider_SetFocused_False_FiresOnFocusLost()
    {
        var slider = new UISlider(Vector2.Zero, new Vector2(200, 20));
        slider.SetFocused(true);
        bool fired = false;
        slider.OnFocusLost += () => fired = true;

        slider.SetFocused(false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UISlider_SetFocused_SameValue_DoesNotFireAgain()
    {
        var slider = new UISlider(Vector2.Zero, new Vector2(200, 20));
        slider.SetFocused(true);
        int count = 0;
        slider.OnFocusGained += () => count++;

        slider.SetFocused(true);

        count.Should().Be(0);
    }

    #endregion

    #region Focus events – UIRadioButton

    [Fact]
    public void UIRadioButton_SetFocused_True_FiresOnFocusGained()
    {
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("Option", group, Vector2.Zero);
        bool fired = false;
        rb.OnFocusGained += () => fired = true;

        rb.SetFocused(true);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIRadioButton_SetFocused_False_FiresOnFocusLost()
    {
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("Option", group, Vector2.Zero);
        rb.SetFocused(true);
        bool fired = false;
        rb.OnFocusLost += () => fired = true;

        rb.SetFocused(false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIRadioButton_SetFocused_SameValue_DoesNotFireAgain()
    {
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("Option", group, Vector2.Zero);
        rb.SetFocused(true);
        int count = 0;
        rb.OnFocusGained += () => count++;

        rb.SetFocused(true);

        count.Should().Be(0);
    }

    #endregion

    #region Focus events – UIDropdown

    [Fact]
    public void UIDropdown_SetFocused_True_FiresOnFocusGained()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(120, 30));
        bool fired = false;
        dd.OnFocusGained += () => fired = true;

        dd.SetFocused(true);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIDropdown_SetFocused_False_FiresOnFocusLost()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(120, 30));
        dd.SetFocused(true);
        bool fired = false;
        dd.OnFocusLost += () => fired = true;

        dd.SetFocused(false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIDropdown_SetFocused_False_ClosesExpanded()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(120, 30));
        dd.AddItem("A");
        dd.Toggle();
        dd.SetFocused(true);

        dd.SetFocused(false);

        dd.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void UIDropdown_SetFocused_SameValue_DoesNotFireAgain()
    {
        var dd = new UIDropdown(Vector2.Zero, new Vector2(120, 30));
        dd.SetFocused(true);
        int count = 0;
        dd.OnFocusGained += () => count++;

        dd.SetFocused(true);

        count.Should().Be(0);
    }

    #endregion

    #region Focus events – UIScrollView

    [Fact]
    public void UIScrollView_SetFocused_True_FiresOnFocusGained()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 200));
        bool fired = false;
        sv.OnFocusGained += () => fired = true;

        sv.SetFocused(true);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIScrollView_SetFocused_False_FiresOnFocusLost()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 200));
        sv.SetFocused(true);
        bool fired = false;
        sv.OnFocusLost += () => fired = true;

        sv.SetFocused(false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIScrollView_SetFocused_SameValue_DoesNotFireAgain()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 200));
        sv.SetFocused(true);
        int count = 0;
        sv.OnFocusGained += () => count++;

        sv.SetFocused(true);

        count.Should().Be(0);
    }

    #endregion

    #region Focus events – UITabContainer

    [Fact]
    public void UITabContainer_SetFocused_True_FiresOnFocusGained()
    {
        var tab = new UITabContainer(Vector2.Zero, new Vector2(300, 200));
        tab.AddTab("A");
        bool fired = false;
        tab.OnFocusGained += () => fired = true;

        tab.SetFocused(true);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UITabContainer_SetFocused_False_FiresOnFocusLost()
    {
        var tab = new UITabContainer(Vector2.Zero, new Vector2(300, 200));
        tab.AddTab("A");
        tab.SetFocused(true);
        bool fired = false;
        tab.OnFocusLost += () => fired = true;

        tab.SetFocused(false);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UITabContainer_SetFocused_SameValue_DoesNotFireAgain()
    {
        var tab = new UITabContainer(Vector2.Zero, new Vector2(300, 200));
        tab.AddTab("A");
        tab.SetFocused(true);
        int count = 0;
        tab.OnFocusGained += () => count++;

        tab.SetFocused(true);

        count.Should().Be(0);
    }

    #endregion

    #region UIProgressBar.PercentageFormat

    [Fact]
    public void UIProgressBar_PercentageFormat_Default_RendersZeroDecimalPercent()
    {
        var renderer = MakeRenderer(new Vector2(30f, 16f));
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20))
        {
            Value = 0.75f,
            ShowPercentage = true
        };

        bar.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "75%")
            .ToList();
        calls.Should().HaveCount(1);
    }

    [Fact]
    public void UIProgressBar_PercentageFormat_OneDecimalPlace_RendersCorrectly()
    {
        var renderer = MakeRenderer(new Vector2(35f, 16f));
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20))
        {
            Value = 0.5f,
            ShowPercentage = true,
            PercentageFormat = "{0:0.0}%"
        };

        bar.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "50.0%")
            .ToList();
        calls.Should().HaveCount(1);
    }

    [Fact]
    public void UIProgressBar_PercentageFormat_CustomLabel_RendersCorrectly()
    {
        var renderer = MakeRenderer(new Vector2(40f, 16f));
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20))
        {
            Value = 1f,
            ShowPercentage = true,
            PercentageFormat = "{0:0} of 100"
        };

        bar.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "100 of 100")
            .ToList();
        calls.Should().HaveCount(1);
    }

    [Fact]
    public void UIProgressBar_PercentageFormat_DefaultValue_IsCorrectString()
    {
        var bar = new UIProgressBar(Vector2.Zero, new Vector2(200, 20));
        bar.PercentageFormat.Should().Be("{0:0}%");
    }

    #endregion

    #region UIScrollView – scrollbar non-overlap

    [Fact]
    public void UIScrollView_BothScrollbars_VerticalTrack_DoesNotExtendIntoCorner()
    {
        // When both scrollbars are visible the vertical track should stop ScrollbarWidth
        // pixels short of the bottom so it does not overlap the horizontal track.
        var renderer = MakeRenderer();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 400,
            ContentWidth = 400,
            ShowVerticalScrollbar = true,
            ShowHorizontalScrollbar = true
        };

        sv.Render(renderer);

        float scrollbarWidth = sv.ScrollbarWidth;
        float expectedTrackHeight = 200f - scrollbarWidth; // full height minus one scrollbar width

        // The vertical track rect height must be the shortened value.
        renderer.Received(1).DrawRectangleFilled(
            200f - scrollbarWidth,
            0f,
            scrollbarWidth,
            expectedTrackHeight,
            sv.ScrollbarTrackColor);
    }

    [Fact]
    public void UIScrollView_BothScrollbars_CornerFillerSquare_IsDrawn()
    {
        var renderer = MakeRenderer();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 400,
            ContentWidth = 400,
            ShowVerticalScrollbar = true,
            ShowHorizontalScrollbar = true
        };

        sv.Render(renderer);

        float scrollbarWidth = sv.ScrollbarWidth;

        // Corner filler square in the bottom-right
        renderer.Received(1).DrawRectangleFilled(
            200f - scrollbarWidth,
            200f - scrollbarWidth,
            scrollbarWidth,
            scrollbarWidth,
            sv.ScrollbarTrackColor);
    }

    [Fact]
    public void UIScrollView_OnlyVerticalScrollbar_VerticalTrack_UsesFullHeight()
    {
        var renderer = MakeRenderer();
        var sv = new UIScrollView(new Vector2(0, 0), new Vector2(200, 200))
        {
            ContentHeight = 400,
            ContentWidth = 200,   // no horizontal overflow
            ShowVerticalScrollbar = true,
            ShowHorizontalScrollbar = true
        };

        sv.Render(renderer);

        float scrollbarWidth = sv.ScrollbarWidth;

        // With no horizontal scrollbar the vertical track spans the full height
        renderer.Received(1).DrawRectangleFilled(
            200f - scrollbarWidth,
            0f,
            scrollbarWidth,
            200f,
            sv.ScrollbarTrackColor);
    }

    #endregion

    #region UIRadioButton – label-driven sizing

    [Fact]
    public void UIRadioButton_LabelChange_ImmediatelyUpdatesSize()
    {
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("A", group, Vector2.Zero);
        var smallSize = rb.Size;

        rb.Label = "Much Longer Label Text";

        rb.Size.X.Should().BeGreaterThan(smallSize.X);
    }

    [Fact]
    public void UIRadioButton_LabelChange_ToEmpty_ShrinksSize()
    {
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("Long Label", group, Vector2.Zero);
        var bigSize = rb.Size;

        rb.Label = string.Empty;

        rb.Size.X.Should().BeLessThan(bigSize.X);
    }

    [Fact]
    public void UIRadioButton_Render_UpdatesSizeFromMeasuredLabel_AfterLabelChange()
    {
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("A", group, Vector2.Zero);
        var renderer = MakeRenderer(new Vector2(80f, 16f));

        rb.Label = "Longer Label";
        rb.Render(renderer);

        rb.Size.X.Should().BeGreaterThanOrEqualTo(105f);
    }

    #endregion

    #region UIRadioButton — canvas removal unregisters from group

    [Fact]
    public void UICanvas_Remove_RadioButton_UnregistersFromGroup()
    {
        var input = MakeInput();
        var canvas = new UICanvas(input);
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("A", group, new Vector2(0, 0));
        canvas.Add(rb);

        canvas.Remove(rb);

        group.GetButtons().Should().NotContain(rb);
    }

    [Fact]
    public void UICanvas_Remove_RadioButton_SelectedButton_ClearsGroupSelection()
    {
        var input = MakeInput();
        var canvas = new UICanvas(input);
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("A", group, new Vector2(0, 0));
        rb.Select();
        canvas.Add(rb);

        canvas.Remove(rb);

        group.SelectedButton.Should().BeNull();
    }

    [Fact]
    public void UICanvas_Remove_PanelContainingRadioButton_UnregistersFromGroup()
    {
        var input = MakeInput();
        var canvas = new UICanvas(input);
        var group = new UIRadioButtonGroup();
        var rb = new UIRadioButton("A", group, new Vector2(0, 0));
        var panel = new UIPanel(new Vector2(0, 0), new Vector2(200, 200));
        panel.AddChild(rb);
        canvas.Add(panel);

        canvas.Remove(panel);

        group.GetButtons().Should().NotContain(rb);
    }

    #endregion

    #region UIScrollView – AutoFitContent

    [Fact]
    public void UIScrollView_AutoFitContent_False_DoesNotUpdateContentSize()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 100))
        {
            AutoFitContent = false,
            ContentHeight = 100
        };

        sv.AddChild(new UIButton("B", new Vector2(0, 0), new Vector2(50, 400)));

        sv.ContentHeight.Should().Be(100f);
    }

    [Fact]
    public void UIScrollView_AutoFitContent_True_ExpandsContentHeightOnAdd()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 100))
        {
            AutoFitContent = true
        };

        sv.AddChild(new UIButton("B", new Vector2(0, 0), new Vector2(50, 300)));

        sv.ContentHeight.Should().BeGreaterThanOrEqualTo(300f);
    }

    [Fact]
    public void UIScrollView_AutoFitContent_True_UpdatesContentHeightOnRemove()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 100))
        {
            AutoFitContent = true
        };

        var tall = new UIButton("T", new Vector2(0, 0), new Vector2(50, 400));
        sv.AddChild(tall);
        sv.ContentHeight.Should().BeGreaterThanOrEqualTo(400f);

        sv.RemoveChild(tall);
        sv.ContentHeight.Should().BeLessThanOrEqualTo(100f);
    }

    [Fact]
    public void UIScrollView_AutoFitContent_True_MultipleChildren_UsesMaxBounds()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 100))
        {
            AutoFitContent = true
        };

        sv.AddChild(new UIButton("A", new Vector2(0, 0), new Vector2(50, 150)));
        sv.AddChild(new UIButton("B", new Vector2(0, 160), new Vector2(50, 100)));

        sv.ContentHeight.Should().BeGreaterThanOrEqualTo(260f);
    }

    [Fact]
    public void UIScrollView_ClearChildren_RemovesAllChildren()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 100));
        sv.AddChild(new UIButton("A", Vector2.Zero, new Vector2(50, 30)));
        sv.AddChild(new UIButton("B", Vector2.Zero, new Vector2(50, 30)));

        sv.ClearChildren();

        sv.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void UIScrollView_ClearChildren_WithAutoFitContent_ResetsDimensions()
    {
        var sv = new UIScrollView(Vector2.Zero, new Vector2(200, 100))
        {
            AutoFitContent = true
        };
        sv.AddChild(new UIButton("A", new Vector2(0, 0), new Vector2(50, 600)));

        sv.ClearChildren();

        sv.ContentHeight.Should().BeLessThanOrEqualTo(sv.Size.Y);
        sv.ContentWidth.Should().BeLessThanOrEqualTo(sv.Size.X);
    }

    #endregion

    #region UIProgressBar – percentage uses fillAmount (no duplicate range)

    [Fact]
    public void UIProgressBar_ShowPercentage_DisplaysCorrectText()
    {
        var renderer = MakeRenderer();
        var pb = new UIProgressBar(Vector2.Zero, new Vector2(200, 20))
        {
            MinValue = 0f,
            MaxValue = 200f,
            ShowPercentage = true,
            PercentageFormat = "{0:0}%"
        };

        pb.Value = 100f;
        pb.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "50%")
            .ToList();
        calls.Should().HaveCount(1);
    }

    [Fact]
    public void UIProgressBar_ShowPercentage_AtMaxValue_Shows100()
    {
        var renderer = MakeRenderer();
        var pb = new UIProgressBar(Vector2.Zero, new Vector2(200, 20))
        {
            MinValue = 0f,
            MaxValue = 50f,
            ShowPercentage = true,
            PercentageFormat = "{0:0}%"
        };

        pb.Value = 50f;
        pb.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "100%")
            .ToList();
        calls.Should().HaveCount(1);
    }

    #endregion

    #region UIDialog – AddButton placement

    [Fact]
    public void UIDialog_AddButton_FirstButton_CenteredHorizontally()
    {
        var dialog = new UIDialog("T", "M", new Vector2(400, 200));

        var btn = dialog.AddButton("OK", () => { });

        // Single button is centered: startX = (400 - 100) / 2 = 150
        float expectedX = (dialog.Size.X - dialog.ButtonWidth) / 2f;
        btn.Position.X.Should().BeApproximately(expectedX, 0.01f);
    }

    [Fact]
    public void UIDialog_AddButton_SecondButton_RightOfFirst()
    {
        var dialog = new UIDialog("T", "M", new Vector2(400, 200));

        var btn1 = dialog.AddButton("OK", () => { });
        var btn2 = dialog.AddButton("Cancel", () => { });

        btn2.Position.X.Should().BeGreaterThan(btn1.Position.X);
    }

    [Fact]
    public void UIDialog_AddButton_TwoButtons_SpacedCorrectly()
    {
        var dialog = new UIDialog("T", "M", new Vector2(400, 200));

        var btn1 = dialog.AddButton("OK", () => { });
        var btn2 = dialog.AddButton("Cancel", () => { });

        float gap = btn2.Position.X - (btn1.Position.X + btn1.Size.X);
        gap.Should().BeApproximately(dialog.ButtonSpacing, 0.01f);
    }

    #endregion

    #region UIStackPanel

    [Fact]
    public void UIStackPanel_Vertical_StacksChildrenTopToBottom()
    {
        var panel = new UIStackPanel(Vector2.Zero)
        {
            Orientation = StackOrientation.Vertical,
            Spacing = 5f
        };
        var a = new UIButton("A", Vector2.Zero, new Vector2(100, 20));
        var b = new UIButton("B", Vector2.Zero, new Vector2(100, 30));
        panel.AddChild(a);
        panel.AddChild(b);

        panel.PerformLayout();

        a.Position.Y.Should().Be(0f);
        b.Position.Y.Should().Be(25f); // 0 + 20 (height of A) + 5 (spacing)
    }

    [Fact]
    public void UIStackPanel_Horizontal_StacksChildrenLeftToRight()
    {
        var panel = new UIStackPanel(Vector2.Zero)
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 8f
        };
        var a = new UIButton("A", Vector2.Zero, new Vector2(60, 20));
        var b = new UIButton("B", Vector2.Zero, new Vector2(40, 20));
        panel.AddChild(a);
        panel.AddChild(b);

        panel.PerformLayout();

        a.Position.X.Should().Be(0f);
        b.Position.X.Should().Be(68f); // 0 + 60 (width of A) + 8 (spacing)
    }

    [Fact]
    public void UIStackPanel_Vertical_SizeFitsContent()
    {
        var panel = new UIStackPanel(Vector2.Zero)
        {
            Orientation = StackOrientation.Vertical,
            Spacing = 4f
        };
        panel.AddChild(new UIButton("A", Vector2.Zero, new Vector2(80, 20)));
        panel.AddChild(new UIButton("B", Vector2.Zero, new Vector2(80, 20)));
        panel.AddChild(new UIButton("C", Vector2.Zero, new Vector2(80, 20)));

        var size = panel.PerformLayout();

        // 3 × 20 + 2 × 4 spacing = 68 height; 80 width
        size.Y.Should().Be(68f);
        size.X.Should().Be(80f);
    }

    [Fact]
    public void UIStackPanel_WithPadding_OffsetsChildren()
    {
        var panel = new UIStackPanel(Vector2.Zero)
        {
            Orientation = StackOrientation.Vertical,
            Spacing = 0f,
            Padding = 10f
        };
        var child = new UIButton("X", Vector2.Zero, new Vector2(50, 20));
        panel.AddChild(child);

        panel.PerformLayout();

        child.Position.X.Should().Be(10f);
        child.Position.Y.Should().Be(10f);
    }

    [Fact]
    public void UIStackPanel_Render_PositionsChildrenRelativeToPanel()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var panel = new UIStackPanel(new Vector2(100f, 50f))
        {
            Orientation = StackOrientation.Vertical,
            Spacing = 0f
        };
        var child = new UIButton("X", Vector2.Zero, new Vector2(80, 30));
        panel.AddChild(child);

        panel.Render(renderer);

        // Child's stored position should be restored to content-relative after render
        child.Position.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void UIStackPanel_ClearChildren_EmptiesChildren()
    {
        var panel = new UIStackPanel(Vector2.Zero);
        panel.AddChild(new UIButton("A", Vector2.Zero, new Vector2(50, 20)));
        panel.AddChild(new UIButton("B", Vector2.Zero, new Vector2(50, 20)));

        panel.ClearChildren();

        panel.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void UIStackPanel_Vertical_SkipsInvisibleChildren()
    {
        var panel = new UIStackPanel(Vector2.Zero)
        {
            Orientation = StackOrientation.Vertical,
            Spacing = 5f
        };
        var a = new UIButton("A", Vector2.Zero, new Vector2(80, 20));
        var hidden = new UIButton("H", Vector2.Zero, new Vector2(80, 20)) { Visible = false };
        var b = new UIButton("B", Vector2.Zero, new Vector2(80, 20));
        panel.AddChild(a);
        panel.AddChild(hidden);
        panel.AddChild(b);

        panel.PerformLayout();

        // b should be placed as if the hidden child doesn't exist
        b.Position.Y.Should().Be(25f); // 20 (A) + 5 (spacing)
    }

    [Fact]
    public void UIStackPanel_Update_DoesNotCallLayoutBeforeRender()
    {
        var panel = new UIStackPanel(Vector2.Zero)
        {
            Orientation = StackOrientation.Vertical,
            Spacing = 5f
        };
        var child = new UIButton("A", Vector2.Zero, new Vector2(80, 20));
        child.Position = new Vector2(99f, 99f);
        panel.AddChild(child);

        // Update alone should not reposition children — layout is Render's job.
        panel.Update(0f);

        child.Position.Should().Be(new Vector2(99f, 99f));
    }

    #endregion

    #region UIGrid

    [Fact]
    public void UIGrid_TwoColumns_FlowsChildrenLeftToRight()
    {
        var grid = new UIGrid(Vector2.Zero) { Columns = 2, HorizontalSpacing = 0f, VerticalSpacing = 0f };
        var children = new[]
        {
            new UIButton("A", Vector2.Zero, new Vector2(50, 30)),
            new UIButton("B", Vector2.Zero, new Vector2(50, 30)),
            new UIButton("C", Vector2.Zero, new Vector2(50, 30)),
            new UIButton("D", Vector2.Zero, new Vector2(50, 30))
        };
        foreach (var c in children) grid.AddChild(c);

        grid.PerformLayout();

        children[0].Position.Should().Be(new Vector2(0f, 0f));
        children[1].Position.Should().Be(new Vector2(50f, 0f));
        children[2].Position.Should().Be(new Vector2(0f, 30f));
        children[3].Position.Should().Be(new Vector2(50f, 30f));
    }

    [Fact]
    public void UIGrid_TwoColumns_WithSpacing_PositionsCorrectly()
    {
        var grid = new UIGrid(Vector2.Zero)
        {
            Columns = 2,
            HorizontalSpacing = 10f,
            VerticalSpacing = 8f
        };
        var a = new UIButton("A", Vector2.Zero, new Vector2(50, 30));
        var b = new UIButton("B", Vector2.Zero, new Vector2(50, 30));
        var c = new UIButton("C", Vector2.Zero, new Vector2(50, 30));
        grid.AddChild(a);
        grid.AddChild(b);
        grid.AddChild(c);

        grid.PerformLayout();

        a.Position.Should().Be(new Vector2(0f, 0f));
        b.Position.Should().Be(new Vector2(60f, 0f));  // 50 + 10 spacing
        c.Position.Should().Be(new Vector2(0f, 38f));  // 30 + 8 spacing
    }

    [Fact]
    public void UIGrid_SizeAutoFitsToContent()
    {
        var grid = new UIGrid(Vector2.Zero) { Columns = 3, HorizontalSpacing = 0f, VerticalSpacing = 0f };
        for (int i = 0; i < 6; i++)
            grid.AddChild(new UIButton(i.ToString(), Vector2.Zero, new Vector2(40, 25)));

        var size = grid.PerformLayout();

        // 3 cols × 40 = 120 wide; 2 rows × 25 = 50 tall
        size.X.Should().Be(120f);
        size.Y.Should().Be(50f);
    }

    [Fact]
    public void UIGrid_WithPadding_OffsetsAllChildren()
    {
        var grid = new UIGrid(Vector2.Zero)
        {
            Columns = 2,
            HorizontalSpacing = 0f,
            VerticalSpacing = 0f,
            Padding = 10f
        };
        var a = new UIButton("A", Vector2.Zero, new Vector2(40, 25));
        var b = new UIButton("B", Vector2.Zero, new Vector2(40, 25));
        grid.AddChild(a);
        grid.AddChild(b);

        grid.PerformLayout();

        a.Position.Should().Be(new Vector2(10f, 10f));
        b.Position.Should().Be(new Vector2(50f, 10f)); // 10 + 40 width
    }

    [Fact]
    public void UIGrid_Render_PositionsChildrenRelativeToGrid()
    {
        var renderer = MakeRenderer(new Vector2(20f, 16f));
        var grid = new UIGrid(new Vector2(200f, 100f))
        {
            Columns = 1,
            HorizontalSpacing = 0f,
            VerticalSpacing = 0f
        };
        var child = new UIButton("X", Vector2.Zero, new Vector2(80, 30));
        grid.AddChild(child);

        grid.Render(renderer);

        // Child's stored position should be restored to content-relative after render
        child.Position.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void UIGrid_ClearChildren_EmptiesChildren()
    {
        var grid = new UIGrid(Vector2.Zero) { Columns = 2 };
        grid.AddChild(new UIButton("A", Vector2.Zero, new Vector2(50, 30)));
        grid.AddChild(new UIButton("B", Vector2.Zero, new Vector2(50, 30)));

        grid.ClearChildren();

        grid.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void UIGrid_Update_DoesNotCallLayoutBeforeRender()
    {
        var grid = new UIGrid(Vector2.Zero) { Columns = 2 };
        var child = new UIButton("A", Vector2.Zero, new Vector2(50, 30));
        child.Position = new Vector2(99f, 99f);
        grid.AddChild(child);

        // Update alone should not reposition children — layout is Render's job.
        grid.Update(0f);

        child.Position.Should().Be(new Vector2(99f, 99f));
    }

    #endregion

    #region UIScrollView border overdraw fix

    [Fact]
    public void UIScrollView_Render_VerticalScrollbar_BorderRedrawnOnTop()
    {
        // The right border must be drawn AFTER the scrollbar track so it is never buried.
        // We verify that DrawRectangleFilled is called with the right border x/width/height
        // at some point after the scrollbar track call, by counting total calls.
        var renderer = MakeRenderer();
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(100f, 100f))
        {
            ContentHeight = 500f,
            ShowVerticalScrollbar = true,
            ScrollbarWidth = 10f
        };

        sv.Render(renderer);

        // Right-border x = 100 - 2 = 98, w = 2, h = 100
        renderer.Received().DrawRectangleFilled(Arg.Is<float>(v => v == 98f), Arg.Is<float>(v => v == 0f), Arg.Is<float>(v => v == 2f), Arg.Is<float>(v => v == 100f), Arg.Any<Color>());
    }

    [Fact]
    public void UIScrollView_Render_HorizontalScrollbar_BottomBorderRedrawnOnTop()
    {
        var renderer = MakeRenderer();
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(100f, 100f))
        {
            ContentWidth = 500f,
            ShowHorizontalScrollbar = true,
            ScrollbarWidth = 10f
        };

        sv.Render(renderer);

        // Bottom-border y = 100 - 2 = 98, w = 100, h = 2
        renderer.Received().DrawRectangleFilled(Arg.Is<float>(v => v == 0f), Arg.Is<float>(v => v == 98f), Arg.Is<float>(v => v == 100f), Arg.Is<float>(v => v == 2f), Arg.Any<Color>());
    }

    #endregion

    #region UIScrollView scrollbar drag track height fix

    [Fact]
    public void UIScrollView_UpdateScrollbarDrag_BothBars_VerticalTrackReducedByScrollbarWidth()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(100f, 100f))
        {
            ContentHeight = 400f,
            ContentWidth = 400f,
            ShowVerticalScrollbar = true,
            ShowHorizontalScrollbar = true,
            ScrollbarWidth = 10f,
            MinScrollbarThumbSize = 0f
        };

        sv.StartScrollbarDrag(new Vector2(95f, 0f), isVertical: true);

        // trackHeight = 100 - 10 = 90; thumbHeight = (100/400)*90 = 22.5; trackRange = 67.5
        // Dragging half the trackRange (33.75) → scrollRatio=0.5 → ScrollOffset.Y = 0.5*(400-100) = 150
        sv.UpdateScrollbarDrag(new Vector2(95f, 33.75f));

        sv.ScrollOffset.Y.Should().BeApproximately(150f, 5f);
    }

    [Fact]
    public void UIScrollView_UpdateScrollbarDrag_VerticalOnly_TrackUsesFullHeight()
    {
        var sv = new UIScrollView(new Vector2(0f, 0f), new Vector2(100f, 100f))
        {
            ContentHeight = 300f,
            ShowVerticalScrollbar = true,
            ShowHorizontalScrollbar = false,
            ScrollbarWidth = 10f,
            MinScrollbarThumbSize = 1f
        };

        sv.StartScrollbarDrag(new Vector2(95f, 0f), isVertical: true);

        // Thumb height = (100/300)*100 ≈ 33.3; track range = 100 - 33.3 = 66.7
        // Dragging by half the track range should reach ~50% scroll.
        float thumbH = (100f / 300f) * 100f;
        float trackRange = 100f - thumbH;
        sv.UpdateScrollbarDrag(new Vector2(95f, trackRange / 2f));

        sv.ScrollOffset.Y.Should().BeApproximately((300f - 100f) / 2f, 5f);
    }

    #endregion

    #region UIProgressBar MinValue/MaxValue clamping fix

    [Fact]
    public void UIProgressBar_SetMinValueAboveCurrentValue_ClampsValue()
    {
        var pb = new UIProgressBar(Vector2.Zero, new Vector2(100f, 20f));
        pb.MaxValue = 100f;
        pb.Value = 30f;

        pb.MinValue = 50f;

        pb.Value.Should().Be(50f);
    }

    [Fact]
    public void UIProgressBar_SetMaxValueBelowCurrentValue_ClampsValue()
    {
        var pb = new UIProgressBar(Vector2.Zero, new Vector2(100f, 20f));
        pb.MaxValue = 100f;
        pb.Value = 80f;

        pb.MaxValue = 50f;

        pb.Value.Should().Be(50f);
    }

    [Fact]
    public void UIProgressBar_SetMinValue_ValueAlreadyInRange_Unchanged()
    {
        var pb = new UIProgressBar(Vector2.Zero, new Vector2(100f, 20f));
        pb.MaxValue = 100f;
        pb.Value = 60f;

        pb.MinValue = 10f;

        pb.Value.Should().Be(60f);
    }

    #endregion

    #region UISlider MinValue/MaxValue clamping fix

    [Fact]
    public void UISlider_SetMinValueAboveCurrentValue_ClampsValue()
    {
        var sl = new UISlider(Vector2.Zero, new Vector2(200f, 20f));
        sl.MaxValue = 100f;
        sl.Value = 20f;

        sl.MinValue = 40f;

        sl.Value.Should().Be(40f);
    }

    [Fact]
    public void UISlider_SetMaxValueBelowCurrentValue_ClampsValue()
    {
        var sl = new UISlider(Vector2.Zero, new Vector2(200f, 20f));
        sl.MaxValue = 100f;
        sl.Value = 80f;

        sl.MaxValue = 50f;

        sl.Value.Should().Be(50f);
    }

    [Fact]
    public void UISlider_SetMinValue_ValueAlreadyInRange_Unchanged()
    {
        var sl = new UISlider(Vector2.Zero, new Vector2(200f, 20f));
        sl.MaxValue = 100f;
        sl.Value = 60f;

        sl.MinValue = 10f;

        sl.Value.Should().Be(60f);
    }

    [Fact]
    public void UISlider_ObjectInitializer_MinValueBeforeMaxValue_DoesNotThrow()
    {
        // Regression: setting MinValue = 30 before MaxValue = 144 in an object initializer
        // used to throw because Clamp(0, 30, 1) has min > max.
        var act = () => _ = new UISlider(Vector2.Zero, new Vector2(200f, 20f))
        {
            MinValue = 30f,
            MaxValue = 144f,
            Value = 60f
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void UISlider_ObjectInitializer_MinValueBeforeMaxValue_ValueClampedAfterBothSet()
    {
        var sl = new UISlider(Vector2.Zero, new Vector2(200f, 20f))
        {
            MinValue = 30f,
            MaxValue = 144f,
            Value = 60f
        };

        sl.Value.Should().Be(60f);
        sl.MinValue.Should().Be(30f);
        sl.MaxValue.Should().Be(144f);
    }

    #endregion

    #region UITooltip Font property

    [Fact]
    public void UITooltip_Render_WithFont_PassesFontToDrawText()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var font = Substitute.For<IFont>();
        var tooltip = new UITooltip("Hello") { Font = font };
        tooltip.Visible = true;

        tooltip.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "Hello" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).Font.Should().Be(font);
    }

    [Fact]
    public void UITooltip_Render_NullFont_DrawsWithNullFont()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var tooltip = new UITooltip("Hi") { Font = null };
        tooltip.Visible = true;

        tooltip.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "Hi" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).Font.Should().BeNull();
    }

    #endregion

    #region UIRichTextLabel

    [Fact]
    public void UIRichTextLabel_Render_DrawsTextWithParseMarkupTrue()
    {
        var renderer = MakeRenderer(new Vector2(80f, 16f));
        var label = new UIRichTextLabel("[b]Hello[/b]", new Vector2(10f, 20f));

        label.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "[b]Hello[/b]" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((float)calls[0].GetArguments()[1]!).Should().BeApproximately(10f, 0.5f);
        ((float)calls[0].GetArguments()[2]!).Should().BeApproximately(20f, 0.5f);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).ParseMarkup.Should().BeTrue();
    }

    [Fact]
    public void UIRichTextLabel_Render_UpdatesSize()
    {
        var renderer = MakeRenderer(new Vector2(60f, 18f));
        var label = new UIRichTextLabel("text", Vector2.Zero);

        label.Render(renderer);

        label.Size.Should().Be(new Vector2(60f, 18f));
    }

    [Fact]
    public void UIRichTextLabel_Render_NotVisible_DoesNotDraw()
    {
        var renderer = MakeRenderer();
        var label = new UIRichTextLabel("Hi", Vector2.Zero) { Visible = false };

        label.Render(renderer);

        renderer.DidNotReceiveWithAnyArgs().DrawText(default!, default, default, default(TextRenderOptions));
    }

    [Fact]
    public void UIRichTextLabel_Render_EmptyText_DoesNotDraw()
    {
        var renderer = MakeRenderer();
        var label = new UIRichTextLabel(string.Empty, Vector2.Zero);

        label.Render(renderer);

        renderer.DidNotReceiveWithAnyArgs().DrawText(default!, default, default, default(TextRenderOptions));
    }

    [Fact]
    public void UIRichTextLabel_Render_PassesColorToOptions()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var label = new UIRichTextLabel("Hello", Vector2.Zero) { Color = Color.Red };

        label.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).Color.Should().Be(Color.Red);
    }

    [Fact]
    public void UIRichTextLabel_Render_PassesFontToOptions()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var font = Substitute.For<IFont>();
        var label = new UIRichTextLabel("Hello", Vector2.Zero) { Font = font };

        label.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).Font.Should().Be(font);
    }

    [Fact]
    public void UIRichTextLabel_Render_PassesMaxWidthToOptions()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var label = new UIRichTextLabel("Hello", Vector2.Zero) { MaxWidth = 200f };

        label.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).MaxWidth.Should().Be(200f);
    }

    [Fact]
    public void UIRichTextLabel_Render_ZeroMaxWidth_PassesNullToOptions()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var label = new UIRichTextLabel("Hello", Vector2.Zero) { MaxWidth = 0f };

        label.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).MaxWidth.Should().BeNull();
    }

    [Fact]
    public void UIRichTextLabel_Render_PassesShadowOffsetToOptions()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var shadow = new Vector2(2f, 2f);
        var label = new UIRichTextLabel("Hi", Vector2.Zero) { ShadowOffset = shadow };

        label.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).ShadowOffset.Should().Be(shadow);
    }

    [Fact]
    public void UIRichTextLabel_Render_PassesLineSpacingToOptions()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var label = new UIRichTextLabel("Hi", Vector2.Zero) { LineSpacing = 1.5f };

        label.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).LineSpacing.Should().Be(1.5f);
    }

    [Fact]
    public void UIRichTextLabel_Render_PassesCustomMarkupParserToOptions()
    {
        var renderer = MakeRenderer(new Vector2(50f, 16f));
        var parser = Substitute.For<IMarkupParser>();
        var label = new UIRichTextLabel("Hi", Vector2.Zero) { MarkupParser = parser };

        label.Render(renderer);

        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[3] is TextRenderOptions)
            .ToList();
        calls.Should().HaveCount(1);
        ((TextRenderOptions)calls[0].GetArguments()[3]!).MarkupParser.Should().Be(parser);
    }

    [Fact]
    public void UIRichTextLabel_Contains_ReturnsTrueWhenInsideBounds()
    {
        var renderer = MakeRenderer(new Vector2(100f, 20f));
        var label = new UIRichTextLabel("Hello", new Vector2(10f, 10f));
        label.Render(renderer);

        label.Contains(new Vector2(50f, 18f)).Should().BeTrue();
    }

    [Fact]
    public void UIRichTextLabel_Contains_ReturnsFalseWhenOutsideBounds()
    {
        var renderer = MakeRenderer(new Vector2(100f, 20f));
        var label = new UIRichTextLabel("Hello", new Vector2(10f, 10f));
        label.Render(renderer);

        label.Contains(new Vector2(200f, 18f)).Should().BeFalse();
    }

    [Fact]
    public void UIRichTextLabel_OnClick_NotFired_WhenDisabled()
    {
        var label = new UIRichTextLabel("X", Vector2.Zero) { Enabled = false };
        bool fired = false;
        label.OnClick += () => fired = true;

        label.Click();

        fired.Should().BeFalse();
    }

    [Fact]
    public void UIRichTextLabel_OnClick_Fired_WhenEnabled()
    {
        var label = new UIRichTextLabel("X", Vector2.Zero);
        bool fired = false;
        label.OnClick += () => fired = true;

        label.Click();

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIRichTextLabel_DefaultZOrder_IsZero()
    {
        var label = new UIRichTextLabel("X", Vector2.Zero);
        label.ZOrder.Should().Be(0);
    }

    #endregion

    #region UIContextMenu

    [Fact]
    public void UIContextMenu_AddItem_IncrementsItemCount()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Cut").AddItem("Copy").AddItem("Paste");
        menu.ItemCount.Should().Be(3);
    }

    [Fact]
    public void UIContextMenu_AddSeparator_IncrementsItemCount()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Cut").AddSeparator().AddItem("Paste");
        menu.ItemCount.Should().Be(3);
    }

    [Fact]
    public void UIContextMenu_GetItemLabel_ReturnsCorrectLabel()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Cut").AddItem("Copy");
        menu.GetItemLabel(1).Should().Be("Copy");
    }

    [Fact]
    public void UIContextMenu_IsItemEnabled_DefaultTrue()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Cut");
        menu.IsItemEnabled(0).Should().BeTrue();
    }

    [Fact]
    public void UIContextMenu_IsItemEnabled_FalseWhenDisabled()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Undo", enabled: false);
        menu.IsItemEnabled(0).Should().BeFalse();
    }

    [Fact]
    public void UIContextMenu_IsItemSeparator_TrueForSeparator()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Cut").AddSeparator().AddItem("Paste");
        menu.IsItemSeparator(1).Should().BeTrue();
    }

    [Fact]
    public void UIContextMenu_IsItemSeparator_FalseForNormalItem()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Cut");
        menu.IsItemSeparator(0).Should().BeFalse();
    }

    [Fact]
    public void UIContextMenu_Height_SumsItemAndSeparatorHeights()
    {
        var menu = new UIContextMenu { ItemHeight = 24f, SeparatorHeight = 8f };
        menu.AddItem("A").AddSeparator().AddItem("B");
        menu.Height.Should().Be(24f + 8f + 24f);
    }

    [Fact]
    public void UIContextMenu_Contains_ReturnsTrueInsideBounds()
    {
        var menu = new UIContextMenu { Width = 100f, ItemHeight = 24f };
        menu.Position = new Vector2(50f, 50f);
        menu.AddItem("A");
        menu.Contains(new Vector2(75f, 60f)).Should().BeTrue();
    }

    [Fact]
    public void UIContextMenu_Contains_ReturnsFalseOutsideBounds()
    {
        var menu = new UIContextMenu { Width = 100f, ItemHeight = 24f };
        menu.Position = new Vector2(50f, 50f);
        menu.AddItem("A");
        menu.Contains(new Vector2(200f, 60f)).Should().BeFalse();
    }

    [Fact]
    public void UIContextMenu_HitTestItem_ReturnsCorrectIndex()
    {
        var menu = new UIContextMenu { Width = 120f, ItemHeight = 28f };
        menu.Position = new Vector2(0f, 0f);
        menu.AddItem("Cut").AddItem("Copy").AddItem("Paste");
        menu.HitTestItem(new Vector2(10f, 30f)).Should().Be(1);
    }

    [Fact]
    public void UIContextMenu_HitTestItem_ReturnsMinusOneOutside()
    {
        var menu = new UIContextMenu { Width = 120f, ItemHeight = 28f };
        menu.Position = new Vector2(0f, 0f);
        menu.AddItem("Cut");
        menu.HitTestItem(new Vector2(200f, 10f)).Should().Be(-1);
    }

    [Fact]
    public void UIContextMenu_FireItemSelected_FiresEventForEnabledItem()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Copy");
        int? firedIndex = null;
        string? firedLabel = null;
        menu.OnItemSelected += (i, l) => { firedIndex = i; firedLabel = l; };

        menu.FireItemSelected(0);

        firedIndex.Should().Be(0);
        firedLabel.Should().Be("Copy");
    }

    [Fact]
    public void UIContextMenu_FireItemSelected_DoesNotFireForDisabledItem()
    {
        var menu = new UIContextMenu();
        menu.AddItem("Grayed", enabled: false);
        bool fired = false;
        menu.OnItemSelected += (_, _) => fired = true;

        menu.FireItemSelected(0);

        fired.Should().BeFalse();
    }

    [Fact]
    public void UIContextMenu_FireItemSelected_DoesNotFireForSeparator()
    {
        var menu = new UIContextMenu();
        menu.AddSeparator();
        bool fired = false;
        menu.OnItemSelected += (_, _) => fired = true;

        menu.FireItemSelected(0);

        fired.Should().BeFalse();
    }

    [Fact]
    public void UIContextMenu_Render_DrawsBackgroundAndItemText()
    {
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(new Vector2(40f, 14f));
        var menu = new UIContextMenu { Width = 120f, ItemHeight = 28f };
        menu.Position = new Vector2(10f, 10f);
        menu.AddItem("Open");

        menu.Render(renderer, hoveredIndex: -1);

        renderer.Received(1).DrawRectangleFilled(Arg.Is<float>(v => v == 10f), Arg.Is<float>(v => v == 10f), Arg.Is<float>(v => v == 120f), Arg.Is<float>(v => v == 28f), Arg.Any<Color>());
        var calls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "Open")
            .ToList();
        calls.Should().HaveCount(1);
    }

    [Fact]
    public void UIContextMenu_Render_DrawsHoverHighlight()
    {
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(new Vector2(40f, 14f));
        var menu = new UIContextMenu { Width = 120f, ItemHeight = 28f, HoverColor = new Color(80, 120, 200) };
        menu.Position = new Vector2(0f, 0f);
        menu.AddItem("Save");

        menu.Render(renderer, hoveredIndex: 0);

        renderer.Received(1).DrawRectangleFilled(1f, 0f, 118f, 28f, new Color(80, 120, 200));
    }

    #endregion

    #region UIToast

    [Fact]
    public void UIToast_DefaultAlpha_IsZero()
    {
        var toast = new UIToast();
        toast.Alpha.Should().Be(0f);
    }

    [Fact]
    public void UIToast_Update_FadeIn_AlphaRisesToOne()
    {
        var toast = new UIToast { FadeInTime = 1f, Duration = 2f, FadeOutTime = 0.5f };

        toast.Update(0.5f);

        toast.Alpha.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void UIToast_Update_DuringDuration_AlphaIsOne()
    {
        var toast = new UIToast { FadeInTime = 0.2f, Duration = 2f, FadeOutTime = 0.4f };

        toast.Update(0.5f);

        toast.Alpha.Should().Be(1f);
    }

    [Fact]
    public void UIToast_Update_FadeOut_AlphaDropsToZero()
    {
        var toast = new UIToast { FadeInTime = 0f, Duration = 1f, FadeOutTime = 1f };

        toast.Update(1.5f);

        toast.Alpha.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void UIToast_Update_ZeroFadeIn_AlphaIsOneImmediately()
    {
        var toast = new UIToast { FadeInTime = 0f, Duration = 2f, FadeOutTime = 0.4f };

        toast.Update(0.01f);

        toast.Alpha.Should().Be(1f);
    }

    [Fact]
    public void UIToast_IsExpired_FalseBeforeTotalLifetime()
    {
        var toast = new UIToast { FadeInTime = 0.2f, Duration = 2f, FadeOutTime = 0.4f };

        toast.Update(1f);

        toast.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void UIToast_IsExpired_TrueAfterTotalLifetime()
    {
        var toast = new UIToast { FadeInTime = 0f, Duration = 0.5f, FadeOutTime = 0f };

        toast.Update(1f);

        toast.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void UIToast_RequestDismiss_AcceleratesIntoFadeOut()
    {
        var toast = new UIToast { FadeInTime = 0.2f, Duration = 5f, FadeOutTime = 0.5f };
        toast.Update(0.5f);

        toast.RequestDismiss();
        toast.Update(0.6f);

        toast.Alpha.Should().BeLessThan(1f);
    }

    [Fact]
    public void UIToast_FireDismissed_FiresOnDismissedEvent()
    {
        var toast = new UIToast();
        bool fired = false;
        toast.OnDismissed += () => fired = true;

        toast.FireDismissed();

        fired.Should().BeTrue();
    }

    [Fact]
    public void UIToast_TotalLifetime_SumsFadeInDurationFadeOut()
    {
        var toast = new UIToast { FadeInTime = 0.2f, Duration = 3f, FadeOutTime = 0.5f };
        toast.TotalLifetime.Should().BeApproximately(3.7f, 0.001f);
    }

    [Fact]
    public void UIToast_Render_DrawsBackgroundAndText()
    {
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(new Vector2(80f, 14f));
        var toast = new UIToast { Text = "Saved!", Width = 200f, Padding = 10f, FadeInTime = 0f, Duration = 2f };

        toast.Update(0.1f);
        toast.Render(renderer, new Vector2(20f, 30f));

        var rectCalls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawRectangleFilled")
            .ToList();
        rectCalls.Should().HaveCount(1);
        ((float)rectCalls[0].GetArguments()[0]!).Should().BeApproximately(20f, 0.5f);
        ((float)rectCalls[0].GetArguments()[1]!).Should().BeApproximately(30f, 0.5f);
        ((float)rectCalls[0].GetArguments()[2]!).Should().BeApproximately(200f, 0.5f);
        var textCalls = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "DrawText" && c.GetArguments()[0] is "Saved!")
            .ToList();
        textCalls.Should().HaveCount(1);
        ((float)textCalls[0].GetArguments()[1]!).Should().BeApproximately(30f, 0.5f);
        ((float)textCalls[0].GetArguments()[2]!).Should().BeApproximately(40f, 0.5f);
    }

    [Fact]
    public void UIToast_Render_AlphaZero_DoesNotDraw()
    {
        var renderer = Substitute.For<IRenderer>();
        var toast = new UIToast { Text = "Hi" };

        toast.Render(renderer, Vector2.Zero);

        renderer.DidNotReceive().DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    [Fact]
    public void UIToast_Render_SetsHeightFromMeasuredText()
    {
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(new Vector2(80f, 14f));
        var toast = new UIToast { Text = "Hi", Padding = 10f, FadeInTime = 0f, Duration = 2f };

        toast.Update(0.1f);
        toast.Render(renderer, Vector2.Zero);

        toast.Height.Should().BeApproximately(34f, 0.001f);
    }

    #endregion

    #region UISpinBox

    [Fact]
    public void UISpinBox_DefaultValue_IsMinValue()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            MinValue = 5f,
            MaxValue = 100f
        };

        spinBox.Value.Should().Be(5f);
    }

    [Fact]
    public void UISpinBox_Increment_IncreasesValueByStep()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Step = 2f,
            Value = 10f
        };

        spinBox.Increment();

        spinBox.Value.Should().BeApproximately(12f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_Decrement_DecreasesValueByStep()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Step = 3f,
            Value = 10f
        };

        spinBox.Decrement();

        spinBox.Value.Should().BeApproximately(7f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_Increment_ClampedAtMaxValue()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            MinValue = 0f,
            MaxValue = 10f,
            Step = 5f,
            Value = 8f
        };

        spinBox.Increment();

        spinBox.Value.Should().BeApproximately(10f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_Decrement_ClampedAtMinValue()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            MinValue = 0f,
            MaxValue = 10f,
            Step = 5f,
            Value = 2f
        };

        spinBox.Decrement();

        spinBox.Value.Should().BeApproximately(0f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_OnValueChanged_FiredOnIncrement()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            Value = 5f,
            MaxValue = 100f
        };

        float? captured = null;
        spinBox.OnValueChanged += v => captured = v;

        spinBox.Increment();

        captured.Should().NotBeNull();
        captured.Should().BeApproximately(6f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_BeginEdit_SetsIsEditing()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f));

        spinBox.BeginEdit();

        spinBox.IsEditing.Should().BeTrue();
    }

    [Fact]
    public void UISpinBox_CommitEdit_ParsesValidBuffer()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            MinValue = 0f,
            MaxValue = 200f
        };

        spinBox.BeginEdit();
        spinBox.HandleEditChar('4');
        spinBox.HandleEditChar('2');
        spinBox.CommitEdit();

        spinBox.Value.Should().BeApproximately(42f, 0.0001f);
        spinBox.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void UISpinBox_CommitEdit_InvalidBuffer_LeavesValueUnchanged()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            Value = 7f,
            MaxValue = 100f
        };

        spinBox.BeginEdit();
        spinBox.HandleEditChar('x');
        spinBox.CommitEdit();

        spinBox.Value.Should().BeApproximately(7f, 0.0001f);
        spinBox.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void UISpinBox_CancelEdit_DoesNotChangeValue()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            Value = 5f,
            MaxValue = 100f
        };

        spinBox.BeginEdit();
        spinBox.HandleEditChar('9');
        spinBox.CancelEdit();

        spinBox.Value.Should().BeApproximately(5f, 0.0001f);
        spinBox.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void UISpinBox_HandleEditBackspace_RemovesLastChar()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            MinValue = 0f,
            MaxValue = 200f
        };

        spinBox.BeginEdit();
        spinBox.HandleEditChar('1');
        spinBox.HandleEditChar('2');
        spinBox.HandleEditBackspace();
        spinBox.CommitEdit();

        spinBox.Value.Should().BeApproximately(1f, 0.0001f);
    }

    [Fact]
    public void UISpinBox_Render_DrawsBackground()
    {
        var renderer = Substitute.For<IRenderer>();
        renderer.MeasureText(default(string)!, default(TextRenderOptions)).ReturnsForAnyArgs(new Vector2(20f, 14f));
        var spinBox = new UISpinBox(new Vector2(10f, 20f), new Vector2(120f, 30f)) { Value = 5f };

        spinBox.Render(renderer);

        renderer.Received().DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    [Fact]
    public void UISpinBox_SetFocused_FiresEvents()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f));

        bool gained = false;
        bool lost = false;
        spinBox.OnFocusGained += () => gained = true;
        spinBox.OnFocusLost += () => lost = true;

        spinBox.SetFocused(true);
        gained.Should().BeTrue();

        spinBox.SetFocused(false);
        lost.Should().BeTrue();
    }

    [Fact]
    public void UISpinBox_Contains_ReturnsCorrectHitTest()
    {
        var spinBox = new UISpinBox(new Vector2(50f, 50f), new Vector2(120f, 30f));

        spinBox.Contains(new Vector2(110f, 65f)).Should().BeTrue();
        spinBox.Contains(new Vector2(10f, 10f)).Should().BeFalse();
    }

    [Fact]
    public void UISpinBox_Disabled_DoesNotIncrement()
    {
        var spinBox = new UISpinBox(Vector2.Zero, new Vector2(120f, 30f))
        {
            Value = 5f,
            MaxValue = 100f,
            Enabled = false
        };

        spinBox.Increment();

        spinBox.Value.Should().BeApproximately(5f, 0.0001f);
    }

    #endregion

    #region UIDropTarget

    [Fact]
    public void UIDropTarget_Render_DrawsIdleBackground()
    {
        var renderer = Substitute.For<IRenderer>();
        var target = new UIDropTarget
        {
            Position = new Vector2(10f, 10f),
            Size = new Vector2(100f, 80f),
            IdleColor = new Color(80, 80, 80, 60)
        };

        target.Render(renderer);

        renderer.Received(1).DrawRectangleFilled(10f, 10f, 100f, 80f, target.IdleColor);
    }

    [Fact]
    public void UIDropTarget_Render_WhenHovered_DrawsHoverBackground()
    {
        var renderer = Substitute.For<IRenderer>();
        var target = new UIDropTarget
        {
            Position = new Vector2(10f, 10f),
            Size = new Vector2(100f, 80f),
            HoverColor = new Color(100, 180, 255, 100)
        };
        target.SetHovered(true);

        target.Render(renderer);

        renderer.Received(1).DrawRectangleFilled(10f, 10f, 100f, 80f, target.HoverColor);
    }

    [Fact]
    public void UIDropTarget_Contains_ReturnsCorrectHitTest()
    {
        var target = new UIDropTarget
        {
            Position = new Vector2(50f, 50f),
            Size = new Vector2(100f, 60f)
        };

        target.Contains(new Vector2(100f, 80f)).Should().BeTrue();
        target.Contains(new Vector2(10f, 10f)).Should().BeFalse();
    }

    [Fact]
    public void UIDropTarget_FireDrop_InvokesOnDrop()
    {
        var target = new UIDropTarget
        {
            Position = Vector2.Zero,
            Size = new Vector2(100f, 60f)
        };

        IDragPayload? received = null;
        target.OnDrop += p => received = p;

        var payload = new TestDragPayload();
        target.FireDrop(payload);

        received.Should().BeSameAs(payload);
    }

    [Fact]
    public void UIDropTarget_AcceptsPayload_Null_AcceptsAll()
    {
        var target = new UIDropTarget { Position = Vector2.Zero, Size = new Vector2(100f, 60f) };
        target.AcceptsPayload.Should().BeNull();
    }

    [Fact]
    public void UIDropTarget_NotVisible_DoesNotRender()
    {
        var renderer = Substitute.For<IRenderer>();
        var target = new UIDropTarget
        {
            Position = Vector2.Zero,
            Size = new Vector2(100f, 60f),
            Visible = false
        };

        target.Render(renderer);

        renderer.DidNotReceive().DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    private sealed class TestDragPayload : IDragPayload { }

    #endregion

    #region UIVirtualList

    [Fact]
    public void UIVirtualList_SetItems_SetsItemCount()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 150f)
        };
        list.SetItems(["Alpha", "Beta", "Gamma"]);

        list.ItemCount.Should().Be(3);
    }

    [Fact]
    public void UIVirtualList_SetItems_ResetsSelectionAndScroll()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 150f),
            RowHeight = 30f
        };
        list.SetItems(["A", "B", "C", "D", "E", "A", "B", "C", "D", "E"]);
        list.Select(2);
        list.ScrollOffsetY = 60f;

        list.SetItems(["X", "Y"]);

        list.SelectedIndex.Should().Be(-1);
        list.ScrollOffsetY.Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void UIVirtualList_Select_UpdatesSelectedIndex()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 150f)
        };
        list.SetItems(["A", "B", "C"]);

        list.Select(1);

        list.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void UIVirtualList_Select_FiresOnSelectionChanged()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 150f)
        };
        list.SetItems(["A", "B", "C"]);

        int fired = -99;
        list.OnSelectionChanged += i => fired = i;
        list.Select(2);

        fired.Should().Be(2);
    }

    [Fact]
    public void UIVirtualList_ClearSelection_ResetsToMinusOne()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 150f)
        };
        list.SetItems(["A", "B", "C"]);
        list.Select(1);

        list.ClearSelection();

        list.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void UIVirtualList_SelectedItem_ReturnsCorrectItem()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 150f)
        };
        list.SetItems(["Alpha", "Beta", "Gamma"]);
        list.Select(1);

        list.SelectedItem.Should().Be("Beta");
    }

    [Fact]
    public void UIVirtualList_SelectedItem_NoSelection_ReturnsDefault()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 150f)
        };
        list.SetItems(["Alpha", "Beta"]);

        list.SelectedItem.Should().BeNull();
    }

    [Fact]
    public void UIVirtualList_ScrollOffsetY_ClampedToValidRange()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 100f),
            RowHeight = 20f
        };
        list.SetItems(["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"]);

        list.ScrollOffsetY = -50f;
        list.ScrollOffsetY.Should().BeApproximately(0f, 0.001f);

        list.ScrollOffsetY = 99999f;
        float expectedMax = list.ItemCount * 20f - 100f;
        list.ScrollOffsetY.Should().BeApproximately(expectedMax, 0.001f);
    }

    [Fact]
    public void UIVirtualList_ScrollIntoView_ScrollsWhenRowIsBelow()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 100f),
            RowHeight = 30f
        };
        list.SetItems(["A", "B", "C", "D", "E", "F"]);

        list.ScrollIntoView(5);

        // row 5 bottom = 180; view bottom = scrollOffset + 100; so offset >= 80
        list.ScrollOffsetY.Should().BeGreaterThanOrEqualTo(80f);
    }

    [Fact]
    public void UIVirtualList_NavigateDown_MovesSelectionDown()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 200f)
        };
        list.SetItems(["A", "B", "C"]);
        list.Select(0);

        list.NavigateDown();

        list.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void UIVirtualList_NavigateUp_MovesSelectionUp()
    {
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 200f)
        };
        list.SetItems(["A", "B", "C"]);
        list.Select(2);

        list.NavigateUp();

        list.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void UIVirtualList_Contains_CorrectHitTest()
    {
        var list = new UIVirtualList<string>
        {
            Position = new Vector2(50f, 50f),
            Size = new Vector2(200f, 150f)
        };

        list.Contains(new Vector2(100f, 100f)).Should().BeTrue();
        list.Contains(new Vector2(10f, 10f)).Should().BeFalse();
    }

    [Fact]
    public void UIVirtualList_Render_DrawsBackground()
    {
        var renderer = Substitute.For<IRenderer>();
        var list = new UIVirtualList<string>
        {
            Position = new Vector2(10f, 10f),
            Size = new Vector2(200f, 150f)
        };
        list.SetItems([]);

        list.Render(renderer);

        renderer.Received().DrawRectangleFilled(10f, 10f, 200f, 150f, list.BackgroundColor);
    }

    [Fact]
    public void UIVirtualList_Render_CallsRowRendererForVisibleRows()
    {
        var renderer = Substitute.For<IRenderer>();
        var rowsRendered = new List<string>();
        var list = new UIVirtualList<string>
        {
            Position = new Vector2(0f, 0f),
            Size = new Vector2(200f, 60f),
            RowHeight = 30f,
            RowRenderer = (r, item, x, y, w, h, sel, hov) => rowsRendered.Add(item)
        };
        list.SetItems(["A", "B", "C", "D"]);

        list.Render(renderer);

        rowsRendered.Count.Should().Be(2);
        rowsRendered.Should().Contain("A");
        rowsRendered.Should().Contain("B");
    }

    [Fact]
    public void UIVirtualList_NotVisible_DoesNotRender()
    {
        var renderer = Substitute.For<IRenderer>();
        var list = new UIVirtualList<string>
        {
            Position = Vector2.Zero,
            Size = new Vector2(200f, 100f),
            Visible = false
        };
        list.SetItems(["A"]);

        list.Render(renderer);

        renderer.DidNotReceive().DrawRectangleFilled(Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    #endregion

    #region UIEasing

    [Fact]
    public void UIEasing_Linear_ReturnsT()
    {
        UIEasing.Linear(0f).Should().BeApproximately(0f, 0.0001f);
        UIEasing.Linear(0.5f).Should().BeApproximately(0.5f, 0.0001f);
        UIEasing.Linear(1f).Should().BeApproximately(1f, 0.0001f);
    }

    [Fact]
    public void UIEasing_AllCurves_StartAtZeroAndEndAtOne()
    {
        Func<float, float>[] curves =
        [
            UIEasing.QuadIn, UIEasing.QuadOut, UIEasing.QuadInOut,
            UIEasing.CubicIn, UIEasing.CubicOut, UIEasing.CubicInOut,
            UIEasing.QuartIn, UIEasing.QuartOut, UIEasing.QuartInOut,
            UIEasing.SineIn, UIEasing.SineOut, UIEasing.SineInOut,
            UIEasing.ExpoIn, UIEasing.ExpoOut, UIEasing.ExpoInOut,
            UIEasing.BounceIn, UIEasing.BounceOut, UIEasing.BounceInOut,
            UIEasing.ElasticIn, UIEasing.ElasticOut, UIEasing.ElasticInOut,
        ];

        foreach (var curve in curves)
        {
            curve(0f).Should().BeApproximately(0f, 0.0001f, because: $"{curve.Method.Name}(0) should be 0");
            curve(1f).Should().BeApproximately(1f, 0.0001f, because: $"{curve.Method.Name}(1) should be 1");
        }
    }

    [Fact]
    public void UIEasing_QuadIn_IsFasterAtEnd()
    {
        UIEasing.QuadIn(0.9f).Should().BeLessThan(0.9f);
    }

    [Fact]
    public void UIEasing_QuadOut_IsFasterAtStart()
    {
        UIEasing.QuadOut(0.1f).Should().BeGreaterThan(0.1f);
    }

    [Fact]
    public void UIEasing_BackOut_OvershootsBeforeSettling()
    {
        bool overshot = false;
        for (int i = 1; i < 100; i++)
            if (UIEasing.BackOut(i / 100f) > 1.001f) { overshot = true; break; }
        overshot.Should().BeTrue();
    }

    #endregion

    #region UITween

    [Fact]
    public void UITween_UpdateToEnd_SetterCalledWithEndValue()
    {
        float received = 0f;
        var tween = new UITween(0f, 100f, 1f, v => received = v);
        tween.Update(1f);
        received.Should().BeApproximately(100f, 0.001f);
    }

    [Fact]
    public void UITween_PartialUpdate_SetterCalledWithInterpolatedValue()
    {
        float received = 0f;
        var tween = new UITween(0f, 100f, 1f, v => received = v, UIEasing.Linear);
        tween.Update(0.5f);
        received.Should().BeApproximately(50f, 0.001f);
    }

    [Fact]
    public void UITween_IsComplete_AfterFullDuration()
    {
        var tween = new UITween(0f, 10f, 0.5f, _ => { });
        tween.Update(0.5f);
        tween.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void UITween_OnComplete_FiredOnceAtEnd()
    {
        int fired = 0;
        var tween = new UITween(0f, 1f, 1f, _ => { });
        tween.OnComplete += () => fired++;
        tween.Update(1f);
        fired.Should().Be(1);
    }

    [Fact]
    public void UITween_OnUpdate_FiredEachFrame()
    {
        int calls = 0;
        var tween = new UITween(0f, 10f, 1f, _ => { });
        tween.OnUpdate += _ => calls++;
        tween.Update(0.25f);
        tween.Update(0.25f);
        calls.Should().Be(2);
    }

    [Fact]
    public void UITween_IsPaused_IgnoresUpdate()
    {
        float received = 0f;
        var tween = new UITween(0f, 100f, 1f, v => received = v) { IsPaused = true };
        tween.Update(1f);
        received.Should().BeApproximately(0f, 0.001f);
        tween.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void UITween_Delay_PostponesStart()
    {
        float received = 0f;
        var tween = new UITween(0f, 100f, 1f, v => received = v) { Delay = 0.5f };
        tween.Update(0.4f);
        received.Should().BeApproximately(0f, 0.001f);
        tween.Update(0.2f);
        received.Should().BeGreaterThan(0f).And.BeLessThan(100f);
    }

    [Fact]
    public void UITween_LoopMode_Loop_DoesNotComplete()
    {
        var tween = new UITween(0f, 100f, 1f, _ => { }) { LoopMode = UITweenLoopMode.Loop };
        tween.Update(2.5f);
        tween.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void UITween_LoopMode_PingPong_ReversesThenForward()
    {
        var values = new List<float>();
        var tween = new UITween(0f, 100f, 1f, v => values.Add(v)) { LoopMode = UITweenLoopMode.PingPong };
        tween.Update(1f);  // reaches end (100)
        tween.Update(0.5f); // reversed, half way back -> ~50
        values.Last().Should().BeApproximately(50f, 0.001f);
    }

    [Fact]
    public void UITween_Complete_JumpsToEndValue()
    {
        float received = 0f;
        var tween = new UITween(0f, 99f, 5f, v => received = v);
        tween.Complete();
        received.Should().BeApproximately(99f, 0.001f);
        tween.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void UITween_Reset_RestoresInitialState()
    {
        float received = 999f;
        var tween = new UITween(0f, 100f, 1f, v => received = v);
        tween.Update(1f);
        tween.Reset();
        tween.IsComplete.Should().BeFalse();
        received.Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void UITween_Constructor_ZeroDuration_Throws()
    {
        var act = () => new UITween(0f, 1f, 0f, _ => { });
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region UITweenSequence

    [Fact]
    public void UITweenSequence_Then_ChainsInOrder()
    {
        var order = new List<string>();
        var seq = new UITweenSequence()
            .Then(new UITween(0f, 1f, 0.5f, _ => order.Add("A")))
            .Then(new UITween(0f, 1f, 0.5f, _ => order.Add("B")));
        seq.Update(0.5f);
        seq.Update(0.5f);
        seq.IsComplete.Should().BeTrue();
        order.Should().Contain("A").And.Contain("B");
    }

    [Fact]
    public void UITweenSequence_OnComplete_FiredOnce()
    {
        int fired = 0;
        var seq = new UITweenSequence()
            .Then(0f, 1f, 0.2f, _ => { })
            .Then(0f, 1f, 0.3f, _ => { });
        seq.OnComplete += () => fired++;
        seq.Update(0.6f);
        fired.Should().Be(1);
    }

    [Fact]
    public void UITweenSequence_SecondTweenNotStartedUntilFirstComplete()
    {
        float bValue = -1f;
        var seq = new UITweenSequence()
            .Then(new UITween(0f, 1f, 1f, _ => { }))
            .Then(new UITween(0f, 50f, 1f, v => bValue = v));
        seq.Update(0.5f);
        bValue.Should().BeApproximately(-1f, 0.001f);
    }

    [Fact]
    public void UITweenSequence_CompleteAll_JumpsToEnd()
    {
        float last = 0f;
        var seq = new UITweenSequence()
            .Then(0f, 100f, 10f, v => last = v)
            .Then(0f, 200f, 10f, v => last = v);
        seq.CompleteAll();
        seq.IsComplete.Should().BeTrue();
        last.Should().BeApproximately(200f, 0.001f);
    }

    [Fact]
    public void UITweenSequence_Reset_RestartFromFirst()
    {
        var callOrder = new List<int>();
        var seq = new UITweenSequence()
            .Then(new UITween(0f, 1f, 0.1f, _ => callOrder.Add(1)))
            .Then(new UITween(0f, 1f, 0.1f, _ => callOrder.Add(2)));
        seq.Update(0.2f);
        seq.IsComplete.Should().BeTrue();
        seq.Reset();
        seq.IsComplete.Should().BeFalse();
        seq.Update(0.1f);
        callOrder.Should().Contain(1);
    }

    [Fact]
    public void UITweenSequence_IsPaused_IgnoresUpdate()
    {
        float v = 0f;
        var seq = new UITweenSequence().Then(0f, 100f, 1f, x => v = x);
        seq.IsPaused = true;
        seq.Update(1f);
        v.Should().BeApproximately(0f, 0.001f);
        seq.IsComplete.Should().BeFalse();
    }

    #endregion

    #region UIWorldLabel

    [Fact]
    public void UIWorldLabel_DefaultValues_AreCorrect()
    {
        var label = new UIWorldLabel();
        label.Visible.Should().BeTrue();
        label.Enabled.Should().BeTrue();
        label.CullWhenOffScreen.Should().BeTrue();
        label.BackgroundPadding.Should().Be(0f);
        label.Text.Should().BeEmpty();
    }

    [Fact]
    public void UIWorldLabel_Render_DrawsText()
    {
        var renderer = new HeadlessRenderer();
        var label = new UIWorldLabel
        {
            Text = "Hello",
            Position = new Vector2(100, 100),
            WorldPosition = new Vector2(50, 50)
        };
        var act = () => label.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UIWorldLabel_Render_Skips_WhenNotVisible()
    {
        var renderer = new HeadlessRenderer();
        var label = new UIWorldLabel { Text = "Hi", Visible = false, Position = new Vector2(10, 10) };
        var act = () => label.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UIWorldLabel_Render_WithBackground_DoesNotThrow()
    {
        var renderer = new HeadlessRenderer();
        var label = new UIWorldLabel
        {
            Text = "Tag",
            Position = new Vector2(50, 50),
            BackgroundPadding = 4f,
            BorderColor = Color.White
        };
        var act = () => label.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UIWorldLabel_Contains_ReflectsPosition()
    {
        var label = new UIWorldLabel
        {
            Position = new Vector2(100, 100),
            Size = new Vector2(80, 20)
        };
        label.Contains(new Vector2(110, 110)).Should().BeTrue();
        label.Contains(new Vector2(50, 50)).Should().BeFalse();
    }

    [Fact]
    public void UIWorldLabel_Update_DoesNotThrow()
    {
        var label = new UIWorldLabel { Text = "Test" };
        var act = () => label.Update(0.016f);
        act.Should().NotThrow();
    }

    #endregion

    #region UITreeNode

    [Fact]
    public void UITreeNode_DefaultValues_AreCorrect()
    {
        var node = new UITreeNode("Root");
        node.Text.Should().Be("Root");
        node.Tag.Should().BeNull();
        node.IsExpanded.Should().BeFalse();
        node.HasChildren.Should().BeFalse();
        node.Children.Should().BeEmpty();
    }

    [Fact]
    public void UITreeNode_FluentAdd_ReturnsParent()
    {
        var root = new UITreeNode("Root");
        var result = root.Add("Child A").Add("Child B");
        result.Should().BeSameAs(root);
        root.Children.Count.Should().Be(2);
    }

    [Fact]
    public void UITreeNode_Add_Node_ReturnsParent()
    {
        var root = new UITreeNode("Root");
        var child = new UITreeNode("Child");
        root.Add(child);
        root.Children.Should().Contain(child);
    }

    [Fact]
    public void UITreeNode_Remove_ReturnsTrueWhenFound()
    {
        var root = new UITreeNode("Root");
        var child = new UITreeNode("Child");
        root.Add(child);
        root.Remove(child).Should().BeTrue();
        root.HasChildren.Should().BeFalse();
    }

    [Fact]
    public void UITreeNode_Remove_ReturnsFalseWhenNotFound()
    {
        var root = new UITreeNode("Root");
        var other = new UITreeNode("Other");
        root.Remove(other).Should().BeFalse();
    }

    [Fact]
    public void UITreeNode_ClearChildren_RemovesAll()
    {
        var root = new UITreeNode("Root");
        root.Add("A").Add("B");
        root.ClearChildren();
        root.HasChildren.Should().BeFalse();
    }

    [Fact]
    public void UITreeNode_HasChildren_TrueWhenChildrenExist()
    {
        var root = new UITreeNode("Root");
        root.HasChildren.Should().BeFalse();
        root.Add("X");
        root.HasChildren.Should().BeTrue();
    }

    #endregion

    #region UITreeView

    [Fact]
    public void UITreeView_DefaultValues_AreCorrect()
    {
        var tv = new UITreeView();
        tv.Visible.Should().BeTrue();
        tv.Enabled.Should().BeTrue();
        tv.SelectedIndex.Should().Be(-1);
        tv.SelectedNode.Should().BeNull();
        tv.Roots.Should().BeEmpty();
    }

    [Fact]
    public void UITreeView_AddRoot_AppearsInRoots()
    {
        var tv = new UITreeView();
        var root = new UITreeNode("R");
        tv.AddRoot(root);
        tv.Roots.Should().Contain(root);
    }

    [Fact]
    public void UITreeView_SetRoots_ReplacesAll()
    {
        var tv = new UITreeView();
        tv.AddRoot(new UITreeNode("Old"));
        tv.SetRoots([new UITreeNode("New1"), new UITreeNode("New2")]);
        tv.Roots.Count.Should().Be(2);
        tv.Roots[0].Text.Should().Be("New1");
    }

    [Fact]
    public void UITreeView_ClearRoots_RemovesAll()
    {
        var tv = new UITreeView();
        tv.AddRoot(new UITreeNode("X"));
        tv.ClearRoots();
        tv.Roots.Should().BeEmpty();
    }

    [Fact]
    public void UITreeView_Render_DoesNotThrow()
    {
        var renderer = new HeadlessRenderer();
        var tv = new UITreeView { Position = new Vector2(0, 0), Size = new Vector2(200, 300) };
        tv.AddRoot(new UITreeNode("Root").Add("Child A").Add("Child B"));
        var act = () => tv.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UITreeView_Render_NotVisible_Skips()
    {
        var renderer = new HeadlessRenderer();
        var tv = new UITreeView { Visible = false, Position = Vector2.Zero, Size = new Vector2(200, 300) };
        tv.AddRoot(new UITreeNode("X"));
        var act = () => tv.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UITreeView_Render_ExpandedNode_ShowsChildren()
    {
        var renderer = new HeadlessRenderer();
        var root = new UITreeNode("Root", isExpanded: true).Add("Child");
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 300) };
        tv.AddRoot(root);
        var act = () => tv.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UITreeView_HandleClick_SelectsRow()
    {
        var tv = new UITreeView { Position = new Vector2(0, 0), Size = new Vector2(200, 200), RowHeight = 22f };
        tv.AddRoot(new UITreeNode("Root"));

        tv.HandleClick(new Vector2(100, 11));

        tv.SelectedIndex.Should().Be(0);
        tv.SelectedNode?.Text.Should().Be("Root");
    }

    [Fact]
    public void UITreeView_HandleClick_ExpanderTogglesNode()
    {
        var root = new UITreeNode("Root");
        root.Add("Child");
        var tv = new UITreeView { Position = new Vector2(0, 0), Size = new Vector2(200, 200), RowHeight = 22f, IndentWidth = 16f };
        tv.AddRoot(root);

        root.IsExpanded.Should().BeFalse();
        tv.HandleClick(new Vector2(8, 11)); // within expander zone (indent=4, expanderRight=20)
        root.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void UITreeView_NavigateDown_SelectsFirstRow()
    {
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 200), RowHeight = 22f };
        tv.AddRoot(new UITreeNode("A"));
        tv.AddRoot(new UITreeNode("B"));

        tv.NavigateDown();
        tv.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void UITreeView_NavigateUp_WrapsToLast()
    {
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 200), RowHeight = 22f };
        tv.AddRoot(new UITreeNode("A"));
        tv.AddRoot(new UITreeNode("B"));

        tv.NavigateUp();
        tv.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void UITreeView_ExpandSelected_ExpandsNode()
    {
        var root = new UITreeNode("R");
        root.Add("C");
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 200), RowHeight = 22f };
        tv.AddRoot(root);
        tv.NavigateDown();

        tv.ExpandSelected();
        root.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void UITreeView_CollapseSelected_CollapsesNode()
    {
        var root = new UITreeNode("R", isExpanded: true);
        root.Add("C");
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 200), RowHeight = 22f };
        tv.AddRoot(root);
        tv.NavigateDown();

        tv.CollapseSelected();
        root.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void UITreeView_OnSelectionChanged_FiredOnSelect()
    {
        UITreeNode? received = null;
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 200), RowHeight = 22f };
        tv.AddRoot(new UITreeNode("X"));
        tv.OnSelectionChanged += n => received = n;

        tv.NavigateDown();
        received?.Text.Should().Be("X");
    }

    [Fact]
    public void UITreeView_OnNodeToggled_FiredOnExpand()
    {
        UITreeNode? toggled = null;
        var root = new UITreeNode("Root");
        root.Add("Child");
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 200), RowHeight = 22f };
        tv.AddRoot(root);
        tv.OnNodeToggled += n => toggled = n;
        tv.NavigateDown();

        tv.ExpandSelected();
        toggled.Should().BeSameAs(root);
    }

    [Fact]
    public void UITreeView_Contains_ReturnsTrueInsideBounds()
    {
        var tv = new UITreeView { Position = new Vector2(50, 50), Size = new Vector2(100, 200) };
        tv.Contains(new Vector2(100, 150)).Should().BeTrue();
        tv.Contains(new Vector2(10, 10)).Should().BeFalse();
    }

    [Fact]
    public void UITreeView_Update_DoesNotThrow()
    {
        var tv = new UITreeView();
        var act = () => tv.Update(0.016f);
        act.Should().NotThrow();
    }

    [Fact]
    public void UITreeView_ScrollOffset_ClampsToZero()
    {
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 200), RowHeight = 22f };
        tv.ScrollOffsetY = -999f;
        tv.ScrollOffsetY.Should().Be(0f);
    }

    [Fact]
    public void UITreeView_Invalidate_AllowsRerender()
    {
        var renderer = new HeadlessRenderer();
        var tv = new UITreeView { Position = Vector2.Zero, Size = new Vector2(200, 300) };
        tv.AddRoot(new UITreeNode("R"));
        tv.Render(renderer);
        tv.Invalidate();
        var act = () => tv.Render(renderer);
        act.Should().NotThrow();
    }

    #endregion

    #region UIMenuItem

    [Fact]
    public void UIMenuItem_DefaultValues()
    {
        var item = new UIMenuItem("Save");
        item.Label.Should().Be("Save");
        item.Enabled.Should().BeTrue();
        item.IsSeparator.Should().BeFalse();
        item.OnClick.Should().BeNull();
    }

    [Fact]
    public void UIMenuItem_Separator_Factory()
    {
        var sep = UIMenuItem.Separator();
        sep.IsSeparator.Should().BeTrue();
        sep.Enabled.Should().BeFalse();
        sep.Label.Should().BeEmpty();
    }

    [Fact]
    public void UIMenuItem_OnClick_IsInvoked()
    {
        bool called = false;
        var item = new UIMenuItem("Go", onClick: () => called = true);
        item.OnClick!.Invoke();
        called.Should().BeTrue();
    }

    #endregion

    #region UIMenuBarMenu

    [Fact]
    public void UIMenuBarMenu_FluentAdd_ChainReturnsMenu()
    {
        var menu = new UIMenuBarMenu("File");
        var result = menu.Add("Open").Add("Save").AddSeparator().Add("Exit");
        result.Should().BeSameAs(menu);
        menu.Items.Count.Should().Be(4);
    }

    [Fact]
    public void UIMenuBarMenu_AddItem_AppearsInItems()
    {
        var menu = new UIMenuBarMenu("Edit");
        menu.Add("Undo");
        menu.Items[0].Label.Should().Be("Undo");
    }

    [Fact]
    public void UIMenuBarMenu_AddSeparator_IsSeparatorItem()
    {
        var menu = new UIMenuBarMenu("View");
        menu.AddSeparator();
        menu.Items[0].IsSeparator.Should().BeTrue();
    }

    [Fact]
    public void UIMenuBarMenu_FireItemSelected_InvokesOnClick()
    {
        bool called = false;
        var menu = new UIMenuBarMenu("File");
        menu.Add("New", onClick: () => called = true);
        menu.FireItemSelected(0);
        called.Should().BeTrue();
    }

    [Fact]
    public void UIMenuBarMenu_FireItemSelected_FiresEvent()
    {
        int receivedIdx = -1;
        string? receivedLabel = null;
        var menu = new UIMenuBarMenu("File");
        menu.Add("Open");
        menu.OnItemSelected += (i, l) => { receivedIdx = i; receivedLabel = l; };
        menu.FireItemSelected(0);
        receivedIdx.Should().Be(0);
        receivedLabel.Should().Be("Open");
    }

    [Fact]
    public void UIMenuBarMenu_FireItemSelected_DisabledItem_NotFired()
    {
        bool called = false;
        var menu = new UIMenuBarMenu("File");
        menu.Add("Ghost", enabled: false, onClick: () => called = true);
        menu.FireItemSelected(0);
        called.Should().BeFalse();
    }

    [Fact]
    public void UIMenuBarMenu_FireItemSelected_Separator_NotFired()
    {
        bool called = false;
        var menu = new UIMenuBarMenu("File");
        menu.AddSeparator();
        menu.OnItemSelected += (_, _) => called = true;
        menu.FireItemSelected(0);
        called.Should().BeFalse();
    }

    #endregion

    #region UIMenuBar

    [Fact]
    public void UIMenuBar_DefaultValues()
    {
        var mb = new UIMenuBar();
        mb.IsOpen.Should().BeFalse();
        mb.OpenMenuIndex.Should().Be(-1);
        mb.Menus.Should().BeEmpty();
    }

    [Fact]
    public void UIMenuBar_AddMenu_ByTitle_ReturnsMenu()
    {
        var mb = new UIMenuBar();
        var menu = mb.AddMenu("File");
        menu.Should().NotBeNull();
        menu.Title.Should().Be("File");
        mb.Menus.Count.Should().Be(1);
    }

    [Fact]
    public void UIMenuBar_AddMenu_FluentChain()
    {
        var mb = new UIMenuBar();
        var result = mb.AddMenu(new UIMenuBarMenu("A")).AddMenu(new UIMenuBarMenu("B"));
        result.Should().BeSameAs(mb);
        mb.Menus.Count.Should().Be(2);
    }

    [Fact]
    public void UIMenuBar_OpenMenu_SetsOpenIndex()
    {
        var mb = new UIMenuBar();
        mb.AddMenu("File");
        mb.OpenMenu(0);
        mb.IsOpen.Should().BeTrue();
        mb.OpenMenuIndex.Should().Be(0);
    }

    [Fact]
    public void UIMenuBar_CloseMenu_ResetsState()
    {
        var mb = new UIMenuBar();
        mb.AddMenu("File");
        mb.OpenMenu(0);
        mb.CloseMenu();
        mb.IsOpen.Should().BeFalse();
        mb.OpenMenuIndex.Should().Be(-1);
    }

    [Fact]
    public void UIMenuBar_Contains_TitleBarBounds()
    {
        var mb = new UIMenuBar { Position = new Vector2(0, 0), Size = new Vector2(800, 28), BarHeight = 28f };
        mb.Contains(new Vector2(100, 14)).Should().BeTrue();
        mb.Contains(new Vector2(100, 40)).Should().BeFalse();
    }

    [Fact]
    public void UIMenuBar_Render_DoesNotThrow()
    {
        var renderer = new HeadlessRenderer();
        var mb = new UIMenuBar { Position = Vector2.Zero, Size = new Vector2(800, 28) };
        mb.AddMenu("File").Add("New").Add("Open");
        mb.AddMenu("Edit").Add("Undo");
        var act = () => mb.Render(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UIMenuBar_RenderSubmenuOverlay_DoesNotThrow()
    {
        var renderer = new HeadlessRenderer();
        var mb = new UIMenuBar { Position = Vector2.Zero, Size = new Vector2(800, 28) };
        mb.AddMenu("File").Add("New").Add("Open").AddSeparator().Add("Exit");
        mb.OpenMenu(0);
        var act = () => mb.RenderSubmenuOverlay(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UIMenuBar_RenderSubmenuOverlay_WhenClosed_DoesNotThrow()
    {
        var renderer = new HeadlessRenderer();
        var mb = new UIMenuBar { Position = Vector2.Zero, Size = new Vector2(800, 28) };
        mb.AddMenu("File").Add("New");
        var act = () => mb.RenderSubmenuOverlay(renderer);
        act.Should().NotThrow();
    }

    [Fact]
    public void UIMenuBar_KeyboardMoveRight_CyclesThroughMenus()
    {
        var mb = new UIMenuBar();
        mb.AddMenu("File");
        mb.AddMenu("Edit");
        mb.AddMenu("View");
        mb.OpenMenu(0);
        mb.KeyboardMoveRight();
        mb.OpenMenuIndex.Should().Be(1);
        mb.KeyboardMoveRight();
        mb.OpenMenuIndex.Should().Be(2);
        mb.KeyboardMoveRight();
        mb.OpenMenuIndex.Should().Be(0); // wraps
    }

    [Fact]
    public void UIMenuBar_KeyboardMoveLeft_CyclesThroughMenus()
    {
        var mb = new UIMenuBar();
        mb.AddMenu("File");
        mb.AddMenu("Edit");
        mb.OpenMenu(0);
        mb.KeyboardMoveLeft();
        mb.OpenMenuIndex.Should().Be(1); // wraps to last
    }

    [Fact]
    public void UIMenuBar_KeyboardMoveDown_SelectsFirstEnabledItem()
    {
        var mb = new UIMenuBar();
        var menu = mb.AddMenu("File");
        menu.Add("New").Add("Open");
        mb.OpenMenu(0);
        mb.KeyboardMoveDown();
        mb.KeyboardActivate(); // should fire
    }

    [Fact]
    public void UIMenuBar_KeyboardActivate_FiresItem()
    {
        bool fired = false;
        var mb = new UIMenuBar();
        var menu = mb.AddMenu("File");
        menu.Add("Save", onClick: () => fired = true);
        mb.OpenMenu(0);
        mb.KeyboardMoveDown();
        mb.KeyboardActivate();
        fired.Should().BeTrue();
        mb.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void UIMenuBar_SubmenuContains_TrueInsideOpenSubmenu()
    {
        var mb = new UIMenuBar
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(800, 28),
            BarHeight = 28f,
            ItemHeight = 28f,
            SubmenuWidth = 180f
        };
        mb.AddMenu("File").Add("New");
        mb.OpenMenu(0);
        // Submenu renders below the bar starting at y=28
        mb.SubmenuContains(new Vector2(50, 35)).Should().BeTrue();
    }

    [Fact]
    public void UIMenuBar_HandleClick_TitleOpensMenu()
    {
        var mb = new UIMenuBar { Position = new Vector2(0, 0), Size = new Vector2(800, 28), BarHeight = 28f, TitlePadding = 12f };
        mb.AddMenu("File").Add("New");
        mb.HandleClick(new Vector2(10, 14)); // inside "File" title
        mb.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void UIMenuBar_Update_DoesNotThrow()
    {
        var mb = new UIMenuBar();
        var act = () => mb.Update(0.016f);
        act.Should().NotThrow();
    }

    #endregion
}
