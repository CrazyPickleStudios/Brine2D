using Brine2D.Abstractions;
using Brine2D.Input;
using SDL3;

namespace Brine2D.SDL3;

public sealed class SdlInput : IInput
{
    private static readonly Dictionary<SDL.Keycode, KeyCode> Map = new()
    {
        { SDL.Keycode.Unknown, KeyCode.Unknown },
        { SDL.Keycode.Escape, KeyCode.Escape },
        { SDL.Keycode.Space, KeyCode.Space },
        { SDL.Keycode.Left, KeyCode.Left },
        { SDL.Keycode.Right, KeyCode.Right },
        { SDL.Keycode.Up, KeyCode.Up },
        { SDL.Keycode.Down, KeyCode.Down },
        { SDL.Keycode.Return, KeyCode.Return },
        { SDL.Keycode.Backspace, KeyCode.Backspace },
        { SDL.Keycode.Tab, KeyCode.Tab },

        { SDL.Keycode.Exclaim, KeyCode.Exclaim },
        { SDL.Keycode.DblApostrophe, KeyCode.DblApostrophe },
        { SDL.Keycode.Hash, KeyCode.Hash },
        { SDL.Keycode.Dollar, KeyCode.Dollar },
        { SDL.Keycode.Percent, KeyCode.Percent },
        { SDL.Keycode.Ampersand, KeyCode.Ampersand },
        { SDL.Keycode.Apostrophe, KeyCode.Apostrophe },
        { SDL.Keycode.LeftParen, KeyCode.LeftParen },
        { SDL.Keycode.RightParen, KeyCode.RightParen },
        { SDL.Keycode.Asterisk, KeyCode.Asterisk },
        { SDL.Keycode.Plus, KeyCode.Plus },
        { SDL.Keycode.Comma, KeyCode.Comma },
        { SDL.Keycode.Minus, KeyCode.Minus },
        { SDL.Keycode.Period, KeyCode.Period },
        { SDL.Keycode.Slash, KeyCode.Slash },
        { SDL.Keycode.Alpha0, KeyCode.Alpha0 },
        { SDL.Keycode.Alpha1, KeyCode.Alpha1 },
        { SDL.Keycode.Alpha2, KeyCode.Alpha2 },
        { SDL.Keycode.Alpha3, KeyCode.Alpha3 },
        { SDL.Keycode.Alpha4, KeyCode.Alpha4 },
        { SDL.Keycode.Alpha5, KeyCode.Alpha5 },
        { SDL.Keycode.Alpha6, KeyCode.Alpha6 },
        { SDL.Keycode.Alpha7, KeyCode.Alpha7 },
        { SDL.Keycode.Alpha8, KeyCode.Alpha8 },
        { SDL.Keycode.Alpha9, KeyCode.Alpha9 },
        { SDL.Keycode.Colon, KeyCode.Colon },
        { SDL.Keycode.Semicolon, KeyCode.Semicolon },
        { SDL.Keycode.Less, KeyCode.Less },
        { SDL.Keycode.Equals, KeyCode.Equals },
        { SDL.Keycode.Greater, KeyCode.Greater },
        { SDL.Keycode.Question, KeyCode.Question },
        { SDL.Keycode.At, KeyCode.At },
        { SDL.Keycode.LeftBracket, KeyCode.LeftBracket },
        { SDL.Keycode.Backslash, KeyCode.Backslash },
        { SDL.Keycode.RightBracket, KeyCode.RightBracket },
        { SDL.Keycode.Caret, KeyCode.Caret },
        { SDL.Keycode.Underscore, KeyCode.Underscore },
        { SDL.Keycode.Grave, KeyCode.Grave },

        { SDL.Keycode.A, KeyCode.A }, { SDL.Keycode.B, KeyCode.B }, { SDL.Keycode.C, KeyCode.C },
        { SDL.Keycode.D, KeyCode.D }, { SDL.Keycode.E, KeyCode.E }, { SDL.Keycode.F, KeyCode.F },
        { SDL.Keycode.G, KeyCode.G }, { SDL.Keycode.H, KeyCode.H }, { SDL.Keycode.I, KeyCode.I },
        { SDL.Keycode.J, KeyCode.J }, { SDL.Keycode.K, KeyCode.K }, { SDL.Keycode.L, KeyCode.L },
        { SDL.Keycode.M, KeyCode.M }, { SDL.Keycode.N, KeyCode.N }, { SDL.Keycode.O, KeyCode.O },
        { SDL.Keycode.P, KeyCode.P }, { SDL.Keycode.Q, KeyCode.Q }, { SDL.Keycode.R, KeyCode.R },
        { SDL.Keycode.S, KeyCode.S }, { SDL.Keycode.T, KeyCode.T }, { SDL.Keycode.U, KeyCode.U },
        { SDL.Keycode.V, KeyCode.V }, { SDL.Keycode.W, KeyCode.W }, { SDL.Keycode.X, KeyCode.X },
        { SDL.Keycode.Y, KeyCode.Y }, { SDL.Keycode.Z, KeyCode.Z },

        { SDL.Keycode.LeftBrace, KeyCode.LeftBrace },
        { SDL.Keycode.Pipe, KeyCode.Pipe },
        { SDL.Keycode.RightBrace, KeyCode.RightBrace },
        { SDL.Keycode.Tilde, KeyCode.Tilde },

        { SDL.Keycode.Delete, KeyCode.Delete },
        { SDL.Keycode.Capslock, KeyCode.Capslock },
        { SDL.Keycode.F1, KeyCode.F1 }, { SDL.Keycode.F2, KeyCode.F2 }, { SDL.Keycode.F3, KeyCode.F3 },
        { SDL.Keycode.F4, KeyCode.F4 }, { SDL.Keycode.F5, KeyCode.F5 }, { SDL.Keycode.F6, KeyCode.F6 },
        { SDL.Keycode.F7, KeyCode.F7 }, { SDL.Keycode.F8, KeyCode.F8 }, { SDL.Keycode.F9, KeyCode.F9 },
        { SDL.Keycode.F10, KeyCode.F10 }, { SDL.Keycode.F11, KeyCode.F11 }, { SDL.Keycode.F12, KeyCode.F12 },
        { SDL.Keycode.PrintScreen, KeyCode.PrintScreen },
        { SDL.Keycode.ScrollLock, KeyCode.ScrollLock },
        { SDL.Keycode.Pause, KeyCode.Pause },
        { SDL.Keycode.Insert, KeyCode.Insert },
        { SDL.Keycode.Home, KeyCode.Home },
        { SDL.Keycode.Pageup, KeyCode.Pageup },
        { SDL.Keycode.End, KeyCode.End },
        { SDL.Keycode.Pagedown, KeyCode.Pagedown },

        { SDL.Keycode.NumLockClear, KeyCode.NumLockClear },
        { SDL.Keycode.KpDivide, KeyCode.KpDivide },
        { SDL.Keycode.KpMultiply, KeyCode.KpMultiply },
        { SDL.Keycode.KpMinus, KeyCode.KpMinus },
        { SDL.Keycode.KpPlus, KeyCode.KpPlus },
        { SDL.Keycode.KpEnter, KeyCode.KpEnter },
        { SDL.Keycode.Kp1, KeyCode.Kp1 }, { SDL.Keycode.Kp2, KeyCode.Kp2 }, { SDL.Keycode.Kp3, KeyCode.Kp3 },
        { SDL.Keycode.Kp4, KeyCode.Kp4 }, { SDL.Keycode.Kp5, KeyCode.Kp5 }, { SDL.Keycode.Kp6, KeyCode.Kp6 },
        { SDL.Keycode.Kp7, KeyCode.Kp7 }, { SDL.Keycode.Kp8, KeyCode.Kp8 }, { SDL.Keycode.Kp9, KeyCode.Kp9 },
        { SDL.Keycode.Kp0, KeyCode.Kp0 },
        { SDL.Keycode.KpPeriod, KeyCode.KpPeriod },

        { SDL.Keycode.Application, KeyCode.Application },
        { SDL.Keycode.Power, KeyCode.Power },
        { SDL.Keycode.KpEquals, KeyCode.KpEquals },

        { SDL.Keycode.Execute, KeyCode.Execute },
        { SDL.Keycode.Help, KeyCode.Help },
        { SDL.Keycode.Menu, KeyCode.Menu },
        { SDL.Keycode.Select, KeyCode.Select },
        { SDL.Keycode.Stop, KeyCode.Stop },
        { SDL.Keycode.Again, KeyCode.Again },
        { SDL.Keycode.Undo, KeyCode.Undo },
        { SDL.Keycode.Cut, KeyCode.Cut },
        { SDL.Keycode.Copy, KeyCode.Copy },
        { SDL.Keycode.Paste, KeyCode.Paste },
        { SDL.Keycode.Find, KeyCode.Find },
        { SDL.Keycode.Mute, KeyCode.Mute },
        { SDL.Keycode.VolumeUp, KeyCode.VolumeUp },
        { SDL.Keycode.VolumeDown, KeyCode.VolumeDown },
        { SDL.Keycode.KpComma, KeyCode.KpComma },
        { SDL.Keycode.KpEqualAas400, KeyCode.KpEqualAas400 },
        { SDL.Keycode.AltErase, KeyCode.AltErase },
        { SDL.Keycode.SysReq, KeyCode.SysReq },
        { SDL.Keycode.Cancel, KeyCode.Cancel },
        { SDL.Keycode.Clear, KeyCode.Clear },
        { SDL.Keycode.Prior, KeyCode.Prior },
        { SDL.Keycode.Return2, KeyCode.Return2 },
        { SDL.Keycode.Separator, KeyCode.Separator },
        { SDL.Keycode.Out, KeyCode.Out },
        { SDL.Keycode.Oper, KeyCode.Oper },
        { SDL.Keycode.ClearAgain, KeyCode.ClearAgain },
        { SDL.Keycode.CrSel, KeyCode.CrSel },
        { SDL.Keycode.ExSel, KeyCode.ExSel },
        { SDL.Keycode.Kp00, KeyCode.Kp00 },
        { SDL.Keycode.Kp000, KeyCode.Kp000 },
        { SDL.Keycode.ThousandsSeparator, KeyCode.ThousandsSeparator },
        { SDL.Keycode.DecimalSeparator, KeyCode.DecimalSeparator },
        { SDL.Keycode.CurrenCyUnit, KeyCode.CurrenCyUnit },
        { SDL.Keycode.CurrenCySubunit, KeyCode.CurrenCySubunit },
        { SDL.Keycode.KpLeftParen, KeyCode.KpLeftParen },
        { SDL.Keycode.KpRightParen, KeyCode.KpRightParen },
        { SDL.Keycode.KpLeftBrace, KeyCode.KpLeftBrace },
        { SDL.Keycode.KpRightBrace, KeyCode.KpRightBrace },
        { SDL.Keycode.KpTab, KeyCode.KpTab },
        { SDL.Keycode.KpBackspace, KeyCode.KpBackspace },
        { SDL.Keycode.KpA, KeyCode.KpA },
        { SDL.Keycode.KpB, KeyCode.KpB },
        { SDL.Keycode.KpC, KeyCode.KpC },
        { SDL.Keycode.KpD, KeyCode.KpD },
        { SDL.Keycode.KpE, KeyCode.KpE },
        { SDL.Keycode.KpF, KeyCode.KpF },
        { SDL.Keycode.KpXor, KeyCode.KpXor },
        { SDL.Keycode.KpPower, KeyCode.KpPower },
        { SDL.Keycode.KpPercent, KeyCode.KpPercent },
        { SDL.Keycode.KpLess, KeyCode.KpLess },
        { SDL.Keycode.KpGreater, KeyCode.KpGreater },
        { SDL.Keycode.KpAmpersand, KeyCode.KpAmpersand },
        { SDL.Keycode.KpDblAmpersand, KeyCode.KpDblAmpersand },
        { SDL.Keycode.KpVerticalBar, KeyCode.KpVerticalBar },
        { SDL.Keycode.KpDblVerticalBar, KeyCode.KpDblVerticalBar },
        { SDL.Keycode.KpColon, KeyCode.KpColon },
        { SDL.Keycode.KpHash, KeyCode.KpHash },
        { SDL.Keycode.KpSpace, KeyCode.KpSpace },
        { SDL.Keycode.KpAt, KeyCode.KpAt },
        { SDL.Keycode.KpExClam, KeyCode.KpExClam },
        { SDL.Keycode.KpMemStore, KeyCode.KpMemStore },
        { SDL.Keycode.KpMemRecall, KeyCode.KpMemRecall },
        { SDL.Keycode.KpMemClear, KeyCode.KpMemClear },
        { SDL.Keycode.KpMemAdd, KeyCode.KpMemAdd },
        { SDL.Keycode.KpMemSubtract, KeyCode.KpMemSubtract },
        { SDL.Keycode.KpMemMultiply, KeyCode.KpMemMultiply },
        { SDL.Keycode.KpMemDivide, KeyCode.KpMemDivide },
        { SDL.Keycode.KpPlusMinus, KeyCode.KpPlusMinus },
        { SDL.Keycode.KpClear, KeyCode.KpClear },
        { SDL.Keycode.KpClearEntry, KeyCode.KpClearEntry },
        { SDL.Keycode.KpBinary, KeyCode.KpBinary },
        { SDL.Keycode.KpOctal, KeyCode.KpOctal },
        { SDL.Keycode.KpDecimal, KeyCode.KpDecimal },
        { SDL.Keycode.KpHexadecimal, KeyCode.KpHexadecimal },
        { SDL.Keycode.LCtrl, KeyCode.LCtrl },
        { SDL.Keycode.LShift, KeyCode.LShift },
        { SDL.Keycode.LAlt, KeyCode.LAlt },
        { SDL.Keycode.LGUI, KeyCode.LGUI },
        { SDL.Keycode.RCtrl, KeyCode.RCtrl },
        { SDL.Keycode.RShift, KeyCode.RShift },
        { SDL.Keycode.RAlt, KeyCode.RAlt },
        { SDL.Keycode.RGUI, KeyCode.RGUI },
        { SDL.Keycode.Mode, KeyCode.Mode },
        { SDL.Keycode.Sleep, KeyCode.Sleep },
        { SDL.Keycode.Wake, KeyCode.Wake },
        { SDL.Keycode.ChannelIncrement, KeyCode.ChannelIncrement },
        { SDL.Keycode.ChannelDecrement, KeyCode.ChannelDecrement },
        { SDL.Keycode.MediaPlay, KeyCode.MediaPlay },
        { SDL.Keycode.MediaPause, KeyCode.MediaPause },
        { SDL.Keycode.MediaRecord, KeyCode.MediaRecord },
        { SDL.Keycode.MediaFastForward, KeyCode.MediaFastForward },
        { SDL.Keycode.MediaRewind, KeyCode.MediaRewind },
        { SDL.Keycode.MediaNextTrack, KeyCode.MediaNextTrack },
        { SDL.Keycode.MediaPreviousTrack, KeyCode.MediaPreviousTrack },
        { SDL.Keycode.MediaStop, KeyCode.MediaStop },
        { SDL.Keycode.MediaEject, KeyCode.MediaEject },
        { SDL.Keycode.MediaPlayPause, KeyCode.MediaPlayPause },
        { SDL.Keycode.MediaSelect, KeyCode.MediaSelect },
        { SDL.Keycode.AcNew, KeyCode.AcNew },
        { SDL.Keycode.AcOpen, KeyCode.AcOpen },
        { SDL.Keycode.AcClose, KeyCode.AcClose },
        { SDL.Keycode.AcExit, KeyCode.AcExit },
        { SDL.Keycode.AcSave, KeyCode.AcSave },
        { SDL.Keycode.AcPrint, KeyCode.AcPrint },
        { SDL.Keycode.AcProperties, KeyCode.AcProperties },
        { SDL.Keycode.AcSearch, KeyCode.AcSearch },
        { SDL.Keycode.AcHome, KeyCode.AcHome },
        { SDL.Keycode.AcBack, KeyCode.AcBack },
        { SDL.Keycode.AcForward, KeyCode.AcForward },
        { SDL.Keycode.AcStop, KeyCode.AcStop },
        { SDL.Keycode.AcRefresh, KeyCode.AcRefresh },
        { SDL.Keycode.AcBookmarks, KeyCode.AcBookmarks },
        { SDL.Keycode.SoftLeft, KeyCode.SoftLeft },
        { SDL.Keycode.SoftRight, KeyCode.SoftRight },
        { SDL.Keycode.Call, KeyCode.Call },
        { SDL.Keycode.EndCall, KeyCode.EndCall },
        { SDL.Keycode.LeftTab, KeyCode.LeftTab },
        { SDL.Keycode.Level5Shift, KeyCode.Level5Shift },
        { SDL.Keycode.MultiKeyCompose, KeyCode.MultiKeyCompose },
        { SDL.Keycode.LMeta, KeyCode.LMeta },
        { SDL.Keycode.RMeta, KeyCode.RMeta },
        { SDL.Keycode.LHyper, KeyCode.LHyper },
        { SDL.Keycode.RHyper, KeyCode.RHyper }
    };

    private readonly HashSet<KeyCode> _current = [];
    private readonly HashSet<KeyCode> _previous = [];

    public void CommitFrame()
    {
        _previous.Clear();
        foreach (var k in _current)
        {
            _previous.Add(k);
        }
    }

    public bool IsKeyDown(KeyCode key)
    {
        return _current.Contains(key);
    }

    public void OnKeyDown(SDL.Keycode sym)
    {
        if (Map.TryGetValue(sym, out var kc))
        {
            _current.Add(kc);
        }
    }

    public void OnKeyUp(SDL.Keycode sym)
    {
        if (Map.TryGetValue(sym, out var kc))
        {
            _current.Remove(kc);
        }
    }

    public bool WasKeyPressed(KeyCode key)
    {
        return _current.Contains(key) && !_previous.Contains(key);
    }

    public bool WasKeyReleased(KeyCode key)
    {
        return !_current.Contains(key) && _previous.Contains(key);
    }
}