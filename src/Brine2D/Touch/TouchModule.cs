using SDL;
using static SDL.SDL3;

namespace Brine2D.Touch;

/// <summary>
///     Provides an interface to touch-screen presses.
/// </summary>
public sealed class TouchModule : Module
{
    private readonly List<TouchInfo> _touches = [];

    void SetTrackpadTouch(bool enable)
    {
        SDL_SetHint(SDL_HINT_TRACKPAD_IS_TOUCH_ONLY, enable ? "1" : "0");
    }

    /// <summary>
    ///     <para>Gets the current position of the specified touch-press, in pixels.</para>
    /// </summary>
    /// <param name="id">
    ///     The identifier of the touch-press. Use love.touch.gettouches, love.touchpressed, or love.touchmoved to
    ///     obtain touch id values.
    /// </param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>x</term>
    ///             <description>The position along the x-axis of the touch-press inside the window, in pixels.</description>
    ///         </item>
    ///         <item>
    ///             <term>y</term>
    ///             <description>The position along the y-axis of the touch-press inside the window, in pixels.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double x, double y) GetPosition(nint id)
    {
        var touch = GetTouch(id);

        return (touch.x, touch.y);
    }

    internal void OnEvent(SDL_EventType eventtype, TouchInfo info)
    {
        switch (eventtype)
        {
            case SDL_EventType.SDL_EVENT_FINGER_DOWN:
                _touches.RemoveAll(t => t.id == info.id);
                _touches.Add(info);
                break;
            case SDL_EventType.SDL_EVENT_FINGER_MOTION:
            {
                for (int i = 0; i < _touches.Count; i++)
                {
                    if (_touches[i].id == info.id)
                    {
                        _touches[i] = info;
                        break;
                    }
                }

                break;
            }

            case SDL_EventType.SDL_EVENT_FINGER_UP:
            case SDL_EventType.SDL_EVENT_FINGER_CANCELED:
                _touches.RemoveAll(t => t.id == info.id);
                break;
            default:
                break;
        }
    }

    /// <summary>
    ///     <para>Gets the current pressure of the specified touch-press.</para>
    /// </summary>
    /// <param name="id">
    ///     The identifier of the touch-press. Use love.touch.gettouches, love.touchpressed, or love.touchmoved to obtain touch
    ///     id values.
    /// </param>
    /// <returns>
    ///     The pressure of the touch-press. Most touch screens aren't pressure sensitive, in which case the pressure will be
    ///     1.
    /// </returns>
    public double GetPressure(nint id)
    {
        return GetTouch(id).pressure;
    }

    /// <summary>
    ///     <para>Gets a list of all active touch-presses.</para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The id values are the same as those used as arguments to love.touchpressed, love.touchmoved, and
    ///         love.touchreleased.
    ///     </para>
    ///     <para>
    ///         The id value of a specific touch-press is only guaranteed to be unique for the duration of that touch-press.
    ///         As soon as love.touchreleased is called using that id, it may be reused for a new touch-press via
    ///         love.touchpressed.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A list of active touch-press id values, which can be used with love.touch.getPosition.
    /// </returns>
    public IEnumerable<nint> GetTouches()
    {
        for (var i = 0; i < _touches.Count; i++)
        {
            // TODO: Verify that this cast actually works.
            yield return (nint)_touches[i].id;
        }
    }

    private TouchInfo GetTouch(nint id)
    {
        for (var i = 0; i < _touches.Count; i++)
        {
            var touch = _touches[i];
            if ((nint)touch.id == id)
                return touch;
        }

        throw new Exception($"Invalid active touch ID: {id}");
    }

    public override bool Release()
    {
        throw new NotImplementedException();
    }

    public override string Type()
    {
        throw new NotImplementedException();
    }

    public override bool TypeOf(string name)
    {
        throw new NotImplementedException();
    }
}