using Brine2D.Input;
using SDL3;

namespace Brine2D.SDL3;

internal class SdlKeyboard : IKeyboard
{
    private static readonly Dictionary<SDL.Keycode, KeyCode> KeyCodeMap = new()
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
        { SDL.Keycode.PlusMinus, KeyCode.PlusMinus },
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
        { SDL.Keycode.F13, KeyCode.F13 }, { SDL.Keycode.F14, KeyCode.F14 }, { SDL.Keycode.F15, KeyCode.F15 },
        { SDL.Keycode.F16, KeyCode.F16 }, { SDL.Keycode.F17, KeyCode.F17 }, { SDL.Keycode.F18, KeyCode.F18 },
        { SDL.Keycode.F19, KeyCode.F19 }, { SDL.Keycode.F20, KeyCode.F20 }, { SDL.Keycode.F21, KeyCode.F21 },
        { SDL.Keycode.F22, KeyCode.F22 }, { SDL.Keycode.F23, KeyCode.F23 }, { SDL.Keycode.F24, KeyCode.F24 },
        { SDL.Keycode.PrintScreen, KeyCode.PrintScreen },
        { SDL.Keycode.ScrollLock, KeyCode.ScrollLock },
        { SDL.Keycode.Pause, KeyCode.Pause },
        { SDL.Keycode.Insert, KeyCode.Insert },
        { SDL.Keycode.Home, KeyCode.Home },
        { SDL.Keycode.Pageup, KeyCode.PageUp },
        { SDL.Keycode.End, KeyCode.End },
        { SDL.Keycode.Pagedown, KeyCode.PageDown },

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
        { SDL.Keycode.CurrenCyUnit, KeyCode.CurrencyUnit },
        { SDL.Keycode.CurrenCySubunit, KeyCode.CurrencySubunit },
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

    private static readonly Dictionary<SDL.Scancode, ScanKey> ScanKeyMap = new()
    {
        { SDL.Scancode.Unknown, ScanKey.Unknown },
        { SDL.Scancode.Escape, ScanKey.Escape },
        { SDL.Scancode.Space, ScanKey.Space },
        { SDL.Scancode.Left, ScanKey.Left },
        { SDL.Scancode.Right, ScanKey.Right },
        { SDL.Scancode.Up, ScanKey.Up },
        { SDL.Scancode.Down, ScanKey.Down },
        { SDL.Scancode.Return, ScanKey.Return },
        { SDL.Scancode.Backspace, ScanKey.Backspace },
        { SDL.Scancode.Tab, ScanKey.Tab },

        { SDL.Scancode.Apostrophe, ScanKey.Apostrophe },
        { SDL.Scancode.Comma, ScanKey.Comma },
        { SDL.Scancode.Minus, ScanKey.Minus },
        { SDL.Scancode.Period, ScanKey.Period },
        { SDL.Scancode.Slash, ScanKey.Slash },
        { SDL.Scancode.Alpha0, ScanKey.Alpha0 },
        { SDL.Scancode.Alpha1, ScanKey.Alpha1 },
        { SDL.Scancode.Alpha2, ScanKey.Alpha2 },
        { SDL.Scancode.Alpha3, ScanKey.Alpha3 },
        { SDL.Scancode.Alpha4, ScanKey.Alpha4 },
        { SDL.Scancode.Alpha5, ScanKey.Alpha5 },
        { SDL.Scancode.Alpha6, ScanKey.Alpha6 },
        { SDL.Scancode.Alpha7, ScanKey.Alpha7 },
        { SDL.Scancode.Alpha8, ScanKey.Alpha8 },
        { SDL.Scancode.Alpha9, ScanKey.Alpha9 },
        { SDL.Scancode.Leftbracket, ScanKey.LeftBracket },
        { SDL.Scancode.Rightbracket, ScanKey.RightBracket },
        { SDL.Scancode.Semicolon, ScanKey.Semicolon },
        { SDL.Scancode.Equals, ScanKey.Equals },
        { SDL.Scancode.Backslash, ScanKey.Backslash },
        { SDL.Scancode.Grave, ScanKey.Grave },
        { SDL.Scancode.NonUshash, ScanKey.NonUsHash },
        { SDL.Scancode.NonUsBackSlash, ScanKey.NonUsBackSlash },
        { SDL.Scancode.Scrolllock, ScanKey.ScrollLock },
        { SDL.Scancode.Printscreen, ScanKey.PrintScreen },
        { SDL.Scancode.KpEqualsAs400, ScanKey.KpEqualsAs400 },
        { SDL.Scancode.International1, ScanKey.International1 },
        { SDL.Scancode.International2, ScanKey.International2 },
        { SDL.Scancode.International3, ScanKey.International3 },
        { SDL.Scancode.International4, ScanKey.International4 },
        { SDL.Scancode.International5, ScanKey.International5 },
        { SDL.Scancode.International6, ScanKey.International6 },
        { SDL.Scancode.International7, ScanKey.International7 },
        { SDL.Scancode.International8, ScanKey.International8 },
        { SDL.Scancode.International9, ScanKey.International9 },
        { SDL.Scancode.Lang1, ScanKey.Lang1 },
        { SDL.Scancode.Lang2, ScanKey.Lang2 },
        { SDL.Scancode.Lang3, ScanKey.Lang3 },
        { SDL.Scancode.Lang4, ScanKey.Lang4 },
        { SDL.Scancode.Lang5, ScanKey.Lang5 },
        { SDL.Scancode.Lang6, ScanKey.Lang6 },
        { SDL.Scancode.Lang7, ScanKey.Lang7 },
        { SDL.Scancode.Lang8, ScanKey.Lang8 },
        { SDL.Scancode.Lang9, ScanKey.Lang9 },

        { SDL.Scancode.A, ScanKey.A }, { SDL.Scancode.B, ScanKey.B }, { SDL.Scancode.C, ScanKey.C },
        { SDL.Scancode.D, ScanKey.D }, { SDL.Scancode.E, ScanKey.E }, { SDL.Scancode.F, ScanKey.F },
        { SDL.Scancode.G, ScanKey.G }, { SDL.Scancode.H, ScanKey.H }, { SDL.Scancode.I, ScanKey.I },
        { SDL.Scancode.J, ScanKey.J }, { SDL.Scancode.K, ScanKey.K }, { SDL.Scancode.L, ScanKey.L },
        { SDL.Scancode.M, ScanKey.M }, { SDL.Scancode.N, ScanKey.N }, { SDL.Scancode.O, ScanKey.O },
        { SDL.Scancode.P, ScanKey.P }, { SDL.Scancode.Q, ScanKey.Q }, { SDL.Scancode.R, ScanKey.R },
        { SDL.Scancode.S, ScanKey.S }, { SDL.Scancode.T, ScanKey.T }, { SDL.Scancode.U, ScanKey.U },
        { SDL.Scancode.V, ScanKey.V }, { SDL.Scancode.W, ScanKey.W }, { SDL.Scancode.X, ScanKey.X },
        { SDL.Scancode.Y, ScanKey.Y }, { SDL.Scancode.Z, ScanKey.Z },

        { SDL.Scancode.Delete, ScanKey.Delete },
        { SDL.Scancode.Capslock, ScanKey.Capslock },
        { SDL.Scancode.F1, ScanKey.F1 }, { SDL.Scancode.F2, ScanKey.F2 }, { SDL.Scancode.F3, ScanKey.F3 },
        { SDL.Scancode.F4, ScanKey.F4 }, { SDL.Scancode.F5, ScanKey.F5 }, { SDL.Scancode.F6, ScanKey.F6 },
        { SDL.Scancode.F7, ScanKey.F7 }, { SDL.Scancode.F8, ScanKey.F8 }, { SDL.Scancode.F9, ScanKey.F9 },
        { SDL.Scancode.F10, ScanKey.F10 }, { SDL.Scancode.F11, ScanKey.F11 }, { SDL.Scancode.F12, ScanKey.F12 },
        { SDL.Scancode.F13, ScanKey.F13 }, { SDL.Scancode.F14, ScanKey.F14 }, { SDL.Scancode.F15, ScanKey.F15 },
        { SDL.Scancode.F16, ScanKey.F16 }, { SDL.Scancode.F17, ScanKey.F17 }, { SDL.Scancode.F18, ScanKey.F18 },
        { SDL.Scancode.F19, ScanKey.F19 }, { SDL.Scancode.F20, ScanKey.F20 }, { SDL.Scancode.F21, ScanKey.F21 },
        { SDL.Scancode.F22, ScanKey.F22 }, { SDL.Scancode.F23, ScanKey.F23 }, { SDL.Scancode.F24, ScanKey.F24 },
        { SDL.Scancode.Pause, ScanKey.Pause },
        { SDL.Scancode.Insert, ScanKey.Insert },
        { SDL.Scancode.Home, ScanKey.Home },
        { SDL.Scancode.Pageup, ScanKey.PageUp },
        { SDL.Scancode.End, ScanKey.End },
        { SDL.Scancode.Pagedown, ScanKey.PageDown },

        { SDL.Scancode.NumLockClear, ScanKey.NumLockClear },
        { SDL.Scancode.KpDivide, ScanKey.KpDivide },
        { SDL.Scancode.KpMultiply, ScanKey.KpMultiply },
        { SDL.Scancode.KpMinus, ScanKey.KpMinus },
        { SDL.Scancode.KpPlus, ScanKey.KpPlus },
        { SDL.Scancode.KpEnter, ScanKey.KpEnter },
        { SDL.Scancode.Kp1, ScanKey.Kp1 }, { SDL.Scancode.Kp2, ScanKey.Kp2 }, { SDL.Scancode.Kp3, ScanKey.Kp3 },
        { SDL.Scancode.Kp4, ScanKey.Kp4 }, { SDL.Scancode.Kp5, ScanKey.Kp5 }, { SDL.Scancode.Kp6, ScanKey.Kp6 },
        { SDL.Scancode.Kp7, ScanKey.Kp7 }, { SDL.Scancode.Kp8, ScanKey.Kp8 }, { SDL.Scancode.Kp9, ScanKey.Kp9 },
        { SDL.Scancode.Kp0, ScanKey.Kp0 },
        { SDL.Scancode.KpPeriod, ScanKey.KpPeriod },

        { SDL.Scancode.Application, ScanKey.Application },
        { SDL.Scancode.Power, ScanKey.Power },
        { SDL.Scancode.KpEquals, ScanKey.KpEquals },

        { SDL.Scancode.Execute, ScanKey.Execute },
        { SDL.Scancode.Help, ScanKey.Help },
        { SDL.Scancode.Menu, ScanKey.Menu },
        { SDL.Scancode.Select, ScanKey.Select },
        { SDL.Scancode.Stop, ScanKey.Stop },
        { SDL.Scancode.Again, ScanKey.Again },
        { SDL.Scancode.Undo, ScanKey.Undo },
        { SDL.Scancode.Cut, ScanKey.Cut },
        { SDL.Scancode.Copy, ScanKey.Copy },
        { SDL.Scancode.Paste, ScanKey.Paste },
        { SDL.Scancode.Find, ScanKey.Find },
        { SDL.Scancode.Mute, ScanKey.Mute },
        { SDL.Scancode.VolumeUp, ScanKey.VolumeUp },
        { SDL.Scancode.VolumeDown, ScanKey.VolumeDown },
        { SDL.Scancode.KpComma, ScanKey.KpComma },
        { SDL.Scancode.AltErase, ScanKey.AltErase },
        { SDL.Scancode.SysReq, ScanKey.SysReq },
        { SDL.Scancode.Cancel, ScanKey.Cancel },
        { SDL.Scancode.Clear, ScanKey.Clear },
        { SDL.Scancode.Prior, ScanKey.Prior },
        { SDL.Scancode.Return2, ScanKey.Return2 },
        { SDL.Scancode.Separator, ScanKey.Separator },
        { SDL.Scancode.Out, ScanKey.Out },
        { SDL.Scancode.Oper, ScanKey.Oper },
        { SDL.Scancode.ClearAgain, ScanKey.ClearAgain },
        { SDL.Scancode.CrSel, ScanKey.CrSel },
        { SDL.Scancode.ExSel, ScanKey.ExSel },
        { SDL.Scancode.Kp00, ScanKey.Kp00 },
        { SDL.Scancode.Kp000, ScanKey.Kp000 },
        { SDL.Scancode.ThousandsSeparator, ScanKey.ThousandsSeparator },
        { SDL.Scancode.DecimalSeparator, ScanKey.DecimalSeparator },
        { SDL.Scancode.KpLeftParen, ScanKey.KpLeftParen },
        { SDL.Scancode.KpRightParen, ScanKey.KpRightParen },
        { SDL.Scancode.KpLeftBrace, ScanKey.KpLeftBrace },
        { SDL.Scancode.KpRightBrace, ScanKey.KpRightBrace },
        { SDL.Scancode.KpTab, ScanKey.KpTab },
        { SDL.Scancode.KpBackspace, ScanKey.KpBackspace },
        { SDL.Scancode.KpA, ScanKey.KpA },
        { SDL.Scancode.KpB, ScanKey.KpB },
        { SDL.Scancode.KpC, ScanKey.KpC },
        { SDL.Scancode.KpD, ScanKey.KpD },
        { SDL.Scancode.KpE, ScanKey.KpE },
        { SDL.Scancode.KpF, ScanKey.KpF },
        { SDL.Scancode.KpXor, ScanKey.KpXor },
        { SDL.Scancode.KpPower, ScanKey.KpPower },
        { SDL.Scancode.KpPercent, ScanKey.KpPercent },
        { SDL.Scancode.KpLess, ScanKey.KpLess },
        { SDL.Scancode.KpGreater, ScanKey.KpGreater },
        { SDL.Scancode.KpAmpersand, ScanKey.KpAmpersand },
        { SDL.Scancode.KpDblAmpersand, ScanKey.KpDblAmpersand },
        { SDL.Scancode.KpVerticalBar, ScanKey.KpVerticalBar },
        { SDL.Scancode.KpDblVerticalBar, ScanKey.KpDblVerticalBar },
        { SDL.Scancode.KpColon, ScanKey.KpColon },
        { SDL.Scancode.KpHash, ScanKey.KpHash },
        { SDL.Scancode.KpSpace, ScanKey.KpSpace },
        { SDL.Scancode.KpAt, ScanKey.KpAt },
        { SDL.Scancode.KpExClam, ScanKey.KpExClam },
        { SDL.Scancode.KpMemStore, ScanKey.KpMemStore },
        { SDL.Scancode.KpMemRecall, ScanKey.KpMemRecall },
        { SDL.Scancode.KpMemClear, ScanKey.KpMemClear },
        { SDL.Scancode.KpMemAdd, ScanKey.KpMemAdd },
        { SDL.Scancode.KpMemSubtract, ScanKey.KpMemSubtract },
        { SDL.Scancode.KpMemMultiply, ScanKey.KpMemMultiply },
        { SDL.Scancode.KpMemDivide, ScanKey.KpMemDivide },
        { SDL.Scancode.KpPlusMinus, ScanKey.KpPlusMinus },
        { SDL.Scancode.KpClear, ScanKey.KpClear },
        { SDL.Scancode.KpClearEntry, ScanKey.KpClearEntry },
        { SDL.Scancode.KpBinary, ScanKey.KpBinary },
        { SDL.Scancode.KpOctal, ScanKey.KpOctal },
        { SDL.Scancode.KpDecimal, ScanKey.KpDecimal },
        { SDL.Scancode.KpHexadecimal, ScanKey.KpHexadecimal },
        { SDL.Scancode.LCtrl, ScanKey.LCtrl },
        { SDL.Scancode.LShift, ScanKey.LShift },
        { SDL.Scancode.LAlt, ScanKey.LAlt },
        { SDL.Scancode.LGUI, ScanKey.LGUI },
        { SDL.Scancode.RCtrl, ScanKey.RCtrl },
        { SDL.Scancode.RShift, ScanKey.RShift },
        { SDL.Scancode.RAlt, ScanKey.RAlt },
        { SDL.Scancode.RGUI, ScanKey.RGUI },
        { SDL.Scancode.Mode, ScanKey.Mode },
        { SDL.Scancode.Sleep, ScanKey.Sleep },
        { SDL.Scancode.Wake, ScanKey.Wake },
        { SDL.Scancode.ChannelIncrement, ScanKey.ChannelIncrement },
        { SDL.Scancode.ChannelDecrement, ScanKey.ChannelDecrement },
        { SDL.Scancode.MediaPlay, ScanKey.MediaPlay },
        { SDL.Scancode.MediaPause, ScanKey.MediaPause },
        { SDL.Scancode.MediaRecord, ScanKey.MediaRecord },
        { SDL.Scancode.MediaFastForward, ScanKey.MediaFastForward },
        { SDL.Scancode.MediaRewind, ScanKey.MediaRewind },
        { SDL.Scancode.MediaNextTrack, ScanKey.MediaNextTrack },
        { SDL.Scancode.MediaPreviousTrack, ScanKey.MediaPreviousTrack },
        { SDL.Scancode.MediaStop, ScanKey.MediaStop },
        { SDL.Scancode.MediaEject, ScanKey.MediaEject },
        { SDL.Scancode.MediaPlayPause, ScanKey.MediaPlayPause },
        { SDL.Scancode.MediaSelect, ScanKey.MediaSelect },
        { SDL.Scancode.SoftLeft, ScanKey.SoftLeft },
        { SDL.Scancode.SoftRight, ScanKey.SoftRight },
        { SDL.Scancode.Call, ScanKey.Call },
        { SDL.Scancode.EndCall, ScanKey.EndCall },

        { SDL.Scancode.CurrencyUnit, ScanKey.CurrencyUnit },
        { SDL.Scancode.CurrencySubunit, ScanKey.CurrencySubunit },
        { SDL.Scancode.ACNew, ScanKey.ACNew },
        { SDL.Scancode.ACOpen, ScanKey.ACOpen },
        { SDL.Scancode.ACClose, ScanKey.ACClose },
        { SDL.Scancode.ACExit, ScanKey.ACExit },
        { SDL.Scancode.ACSave, ScanKey.ACSave },
        { SDL.Scancode.ACPrint, ScanKey.ACPrint },
        { SDL.Scancode.ACProperties, ScanKey.ACProperties },
        { SDL.Scancode.ACSearch, ScanKey.ACSearch },
        { SDL.Scancode.ACHome, ScanKey.ACHome },
        { SDL.Scancode.ACBack, ScanKey.ACBack },
        { SDL.Scancode.ACForward, ScanKey.ACForward },
        { SDL.Scancode.ACStop, ScanKey.ACStop },
        { SDL.Scancode.ACRefresh, ScanKey.ACRefresh },
        { SDL.Scancode.ACBookmarks, ScanKey.ACBookmarks }
    };

    private readonly HashSet<KeyCode> _current = [];
    private readonly HashSet<KeyCode> _previous = [];
    private readonly HashSet<ScanKey> _scanCurrent = [];
    private readonly HashSet<ScanKey> _scanPrevious = [];
    
    public void EndFrame()
    {
        _previous.Clear();
        foreach (var k in _current)
        {
            _previous.Add(k);
        }

        _scanPrevious.Clear();
        foreach (var k in _scanCurrent)
        {
            _scanPrevious.Add(k);
        }
    }
    
    public bool IsDown(KeyCode key)
    {
        return _current.Contains(key);
    }

    public bool IsDown(ScanKey key)
    {
        return _scanCurrent.Contains(key);
    }

    public void OnKeyDown(SDL.Keycode keycode, SDL.Scancode scancode)
    {
        if (KeyCodeMap.TryGetValue(keycode, out var kc))
        {
            _current.Add(kc);
        }

        if (ScanKeyMap.TryGetValue(scancode, out var sk))
        {
            _scanCurrent.Add(sk);
        }
    }

    public void OnKeyUp(SDL.Keycode keycode, SDL.Scancode scancode)
    {
        if (KeyCodeMap.TryGetValue(keycode, out var kc))
        {
            _current.Remove(kc);
        }

        if (ScanKeyMap.TryGetValue(scancode, out var sk))
        {
            _scanCurrent.Remove(sk);
        }
    }

    public bool WasPressed(KeyCode key)
    {
        return _current.Contains(key) && !_previous.Contains(key);
    }

    public bool WasPressed(ScanKey key)
    {
        return _scanCurrent.Contains(key) && !_scanPrevious.Contains(key);
    }

    public bool WasReleased(KeyCode key)
    {
        return !_current.Contains(key) && _previous.Contains(key);
    }

    public bool WasReleased(ScanKey key)
    {
        return !_scanCurrent.Contains(key) && _scanPrevious.Contains(key);
    }
}