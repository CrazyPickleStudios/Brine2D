using System;

namespace Brine2D.Input;

// TODO: Needs review
public sealed class JoystickModule : Module
{
/// <summary>
        /// <para>Closes a joystick, i.e. stop using it for generating events and in query functions.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be closed</param>
    public void Close(double joystick) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the position of each axis.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>axisDir1</term><description>Direction of axis1</description></item>
        /// <item><term>axisDir2</term><description>Direction of axis2</description></item>
        /// <item><term>axisDirN</term><description>Direction of axisN</description></item>
        /// </list>
        /// </returns>
    public (double axisDir1, double axisDir2, double axisDirN) GetAxes(double joystick) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the direction of the axis.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <param name="axis">The axis to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>direction</term><description>Current value of the axis</description></item>
        /// </list>
        /// </returns>
    public double GetAxis(double joystick, double axis) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the change in ball position.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <param name="ball">The ball to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>dx</term><description>Change in x of the ball position.</description></item>
        /// <item><term>dy</term><description>Change in y of the ball position.</description></item>
        /// </list>
        /// </returns>
    public (double dx, double dy) GetBall(double joystick, double ball) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the full gamepad mapping string of the Joysticks which have the given GUID, or nil if the GUID isn't recognized as a gamepad.</para>
        /// <para>The mapping string contains binding information used to map the Joystick's buttons an axes to the standard gamepad layout, and can be used later with love.joystick.loadGamepadMappings.</para>
        /// </summary>
        /// <param name="guid">The value to get the mapping string for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mappingstring</term><description>A string containing the Joystick's gamepad mappings, or nil if the GUID is not recognized as a gamepad.</description></item>
        /// </list>
        /// </returns>
    public string GetGamepadMappingString(string guid) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the direction of a hat.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <param name="hat">The hat to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>direction</term><description>The direction the hat is pushed</description></item>
        /// </list>
        /// </returns>
    public object GetHat(double joystick, double hat) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the number of connected joysticks.</para>
        /// </summary>
        /// <param name="joystickcount">The number of connected joysticks.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joystickcount</term><description>The number of connected joysticks.</description></item>
        /// </list>
        /// </returns>
    public double GetJoystickCount(double joystickcount) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets a list of connected Joysticks.</para>
        /// </summary>
        /// <param name="joysticks">The list of currently connected .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joysticks</term><description>The list of currently connected .</description></item>
        /// </list>
        /// </returns>
    public object GetJoysticks(object joysticks) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets a list of connected Joysticks.</para>
        /// </summary>
    public void GetJoysticks() => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the name of a joystick.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>name</term><description>The name</description></item>
        /// </list>
        /// </returns>
    public string GetName(double joystick) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the number of axes on the joystick.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>axes</term><description>The number of axes available</description></item>
        /// </list>
        /// </returns>
    public double GetNumAxes(double joystick) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the number of balls on the joystick.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>balls</term><description>The number of balls available</description></item>
        /// </list>
        /// </returns>
    public double GetNumBalls(double joystick) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the number of buttons on the joystick.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>buttons</term><description>The number of buttons available</description></item>
        /// </list>
        /// </returns>
    public double GetNumButtons(double joystick) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the number of hats on the joystick.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>hats</term><description>How many hats the joystick has</description></item>
        /// </list>
        /// </returns>
    public double GetNumHats(double joystick) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns how many joysticks are available.</para>
        /// </summary>
        /// <param name="joysticks">The number of joysticks available</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joysticks</term><description>The number of joysticks available</description></item>
        /// </list>
        /// </returns>
    public double GetNumJoysticks(double joysticks) => throw new NotImplementedException();

    /// <summary>
    /// <para>Checks if a button on a joystick is pressed.</para>
    /// </summary>
    /// <param name="joystick">The joystick to be checked</param>
    /// <param name="button">The button to be checked</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>down</term><description>True if the button is down, false if it is not</description></item>
    /// </list>
    /// </returns>
    // TODO: public bool IsDown(double joystick, double button) => throw new NotImplementedException();

    /// <summary>
    /// <para>Checks if a button on a joystick is pressed.</para>
    /// </summary>
    /// <param name="joystick">The joystick to be checked</param>
    /// <param name="buttonN">A button to check</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>anyDown</term><description>True if any supplied button is down, false if not.</description></item>
    /// </list>
    /// </returns>
    public bool IsDown(double joystick, double buttonN) => throw new NotImplementedException();

/// <summary>
        /// <para>Checks if the joystick is open.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be checked</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>open</term><description>True if the joystick is open, false if it is closed.</description></item>
        /// </list>
        /// </returns>
    public bool IsOpen(double joystick) => throw new NotImplementedException();

    /// <summary>
    /// <para>Loads a gamepad mappings string or file created with love.joystick.saveGamepadMappings.</para>
    /// <para>It also recognizes any SDL gamecontroller mapping string, such as those created with Steam's Big Picture controller configure interface, or this nice database. If a new mapping is loaded for an already known controller GUID, the later version will overwrite the one currently loaded.</para>
    /// </summary>
    /// <param name="filename">The filename to load the mappings string from.</param>
    // TODO: public void LoadGamepadMappings(string filename) => throw new NotImplementedException();

    /// <summary>
    /// <para>Loads a gamepad mappings string or file created with love.joystick.saveGamepadMappings.</para>
    /// <para>It also recognizes any SDL gamecontroller mapping string, such as those created with Steam's Big Picture controller configure interface, or this nice database. If a new mapping is loaded for an already known controller GUID, the later version will overwrite the one currently loaded.</para>
    /// </summary>
    /// <param name="mappings">The mappings string to load.</param>
    public void LoadGamepadMappings(string mappings) => throw new NotImplementedException();

    /// <summary>
    /// <para>Saves the virtual gamepad mappings of all Joysticks that are recognized as gamepads and have either been recently used or their gamepad bindings have been modified.</para>
    /// <para>The mappings are stored as a string for use with love.joystick.loadGamepadMappings.</para>
    /// </summary>
    /// <param name="filename">The filename to save the mappings string to.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>mappings</term><description>The mappings string that was written to the file.</description></item>
    /// </list>
    /// </returns>
    // TODO: public string SaveGamepadMappings(string filename) => throw new NotImplementedException();

    /// <summary>
    /// <para>Saves the virtual gamepad mappings of all Joysticks that are recognized as gamepads and have either been recently used or their gamepad bindings have been modified.</para>
    /// <para>The mappings are stored as a string for use with love.joystick.loadGamepadMappings.</para>
    /// </summary>
    /// <param name="mappings">The mappings string.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>mappings</term><description>The mappings string.</description></item>
    /// </list>
    /// </returns>
    public string SaveGamepadMappings(string mappings) => throw new NotImplementedException();

/// <summary>
        /// <para>Opens up a joystick to be used, i.e. makes it ready to use. By default joysticks that are available at the start of your game will be opened.</para>
        /// </summary>
        /// <param name="joystick">The joystick to be opened</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>open</term><description>True if the joystick has been successfully opened or false on failure.</description></item>
        /// </list>
        /// </returns>
    public bool Open(double joystick) => throw new NotImplementedException();

    /// <summary>
    /// <para>Binds a virtual gamepad input to a button, axis or hat for all Joysticks of a certain type. For example, if this function is used with a GUID returned by a Dualshock 3 controller in OS X, the binding will affect Joystick:getGamepadAxis and Joystick:isGamepadDown for all Dualshock 3 controllers used with the game when run in OS X.</para>
    /// <para>LÖVE includes built-in gamepad bindings for many common controllers. This function lets you change the bindings or add new ones for types of Joysticks which aren't recognized as gamepads by default.</para>
    /// <para>The virtual gamepad buttons and axes are designed around the Xbox 360 controller layout.</para>
    /// </summary>
    /// <param name="guid">The OS-dependent for the type of Joystick the binding will affect.</param>
    /// <param name="button">The virtual gamepad button to bind.</param>
    /// <param name="inputtype">The type of input to bind the virtual gamepad button to.</param>
    /// <param name="inputindex">The 1-based index of the axis, button, or hat to bind the virtual gamepad button to.</param>
    /// <param name="hatdir">The direction of the hat, if the virtual gamepad button will be bound to a hat. nil otherwise.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>Whether the virtual gamepad button was successfully bound.</description></item>
    /// </list>
    /// </returns>
    // TODO: public bool SetGamepadMapping(string guid, GamepadButton button, JoystickInputType inputtype, double inputindex, JoystickHat hatdir = null) => throw new NotImplementedException();

    /// <summary>
    /// <para>Binds a virtual gamepad input to a button, axis or hat for all Joysticks of a certain type. For example, if this function is used with a GUID returned by a Dualshock 3 controller in OS X, the binding will affect Joystick:getGamepadAxis and Joystick:isGamepadDown for all Dualshock 3 controllers used with the game when run in OS X.</para>
    /// <para>LÖVE includes built-in gamepad bindings for many common controllers. This function lets you change the bindings or add new ones for types of Joysticks which aren't recognized as gamepads by default.</para>
    /// <para>The virtual gamepad buttons and axes are designed around the Xbox 360 controller layout.</para>
    /// </summary>
    /// <param name="guid">The OS-dependent for the type of Joystick the binding will affect.</param>
    /// <param name="axis">The virtual gamepad axis to bind.</param>
    /// <param name="inputtype">The type of input to bind the virtual gamepad axis to.</param>
    /// <param name="inputindex">The 1-based index of the axis, button, or hat to bind the virtual gamepad axis to.</param>
    /// <param name="hatdir">The direction of the hat, if the virtual gamepad axis will be bound to a hat. nil otherwise.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>Whether the virtual gamepad axis was successfully bound.</description></item>
    /// </list>
    /// </returns>
    // TODO: public bool SetGamepadMapping(string guid, GamepadAxis axis, JoystickInputType inputtype, double inputindex, JoystickHat hatdir = null) => throw new NotImplementedException();

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
