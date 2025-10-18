namespace Brine2D
{
    /// <summary>
    /// <para>Represents a physical joystick.</para>
    /// </summary>
    // TODO: Requires Review
    public class Joystick
    {
        /// <summary>
        /// <para>Gets the direction of each axis.</para>
        /// </summary>
        /// <param name="axisDir1">Direction of axis1.</param>
        /// <param name="axisDir2">Direction of axis2.</param>
        /// <param name="axisDirN">Direction of axisN.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>axisDir1</term><description>Direction of axis1.</description></item>
        /// <item><term>axisDir2</term><description>Direction of axis2.</description></item>
        /// <item><term>axisDirN</term><description>Direction of axisN.</description></item>
        /// </list>
        /// </returns>
        public (double axisDir1, double axisDir2, double axisDirN) GetAxes(double axisDir1, double axisDir2, double axisDirN) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the direction of an axis.</para>
        /// </summary>
        /// <param name="axis">The index of the axis to be checked.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>direction</term><description>Current value of the axis.</description></item>
        /// </list>
        /// </returns>
        public double GetAxis(double axis) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of axes on the joystick.</para>
        /// </summary>
        /// <param name="axes">The number of axes available.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>axes</term><description>The number of axes available.</description></item>
        /// </list>
        /// </returns>
        public double GetAxisCount(double axes) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of buttons on the joystick.</para>
        /// </summary>
        /// <param name="buttons">The number of buttons available.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>buttons</term><description>The number of buttons available.</description></item>
        /// </list>
        /// </returns>
        public double GetButtonCount(double buttons) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the USB vendor ID, product ID, and product version numbers of joystick which consistent across operating systems.</para>
        /// <para>Can be used to show different icons, etc. for different gamepads.</para>
        /// </summary>
        /// <param name="vendorID">The USB vendor ID of the joystick.</param>
        /// <param name="productID">The USB product ID of the joystick.</param>
        /// <param name="productVersion">The product version of the joystick.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>vendorID</term><description>The USB vendor ID of the joystick.</description></item>
        /// <item><term>productID</term><description>The USB product ID of the joystick.</description></item>
        /// <item><term>productVersion</term><description>The product version of the joystick.</description></item>
        /// </list>
        /// </returns>
        public (double vendorID, double productID, double productVersion) GetDeviceInfo(double vendorID, double productID, double productVersion) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a stable GUID unique to the type of the physical joystick which does not change over time. For example, all Sony Dualshock 3 controllers in OS X have the same GUID. The value is platform-dependent.</para>
        /// </summary>
        /// <param name="guid">The Joystick type's OS-dependent unique identifier.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>guid</term><description>The Joystick type's OS-dependent unique identifier.</description></item>
        /// </list>
        /// </returns>
        public string GetGUID(string guid) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the direction of a virtual gamepad axis. If the Joystick isn't recognized as a gamepad or isn't connected, this function will always return 0.</para>
        /// </summary>
        /// <param name="axis">The virtual axis to be checked.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>direction</term><description>Current value of the axis.</description></item>
        /// </list>
        /// </returns>
        public double GetGamepadAxis(GamepadAxis axis) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the direction of a virtual gamepad axis. If the Joystick isn't recognized as a gamepad or isn't connected, this function will always return 0.</para>
        /// </summary>
        // TODO: public void GetGamepadAxis() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the button, axis or hat that a virtual gamepad input is bound to.</para>
        /// </summary>
        /// <param name="axis">The virtual gamepad axis to get the binding for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>inputtype</term><description>The type of input the virtual gamepad axis is bound to.</description></item>
        /// <item><term>inputindex</term><description>The 1-based index of the Joystick's button, axis or hat that the virtual gamepad axis is bound to.</description></item>
        /// <item><term>hatdirection</term><description>The direction of the hat, if the virtual gamepad axis is bound to a hat. nil otherwise.</description></item>
        /// </list>
        /// </returns>
        public (JoystickInputType inputtype, double inputindex, JoystickHat hatdirection) GetGamepadMapping(GamepadAxis axis) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the button, axis or hat that a virtual gamepad input is bound to.</para>
        /// </summary>
        /// <param name="button">The virtual gamepad button to get the binding for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>inputtype</term><description>The type of input the virtual gamepad button is bound to.</description></item>
        /// <item><term>inputindex</term><description>The 1-based index of the Joystick's button, axis or hat that the virtual gamepad button is bound to.</description></item>
        /// <item><term>hatdirection</term><description>The direction of the hat, if the virtual gamepad button is bound to a hat. nil otherwise.</description></item>
        /// </list>
        /// </returns>
        public (JoystickInputType inputtype, double inputindex, JoystickHat hatdirection) GetGamepadMapping(GamepadButton button) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the full gamepad mapping string of this Joystick, or nil if it's not recognized as a gamepad.</para>
        /// <para>The mapping string contains binding information used to map the Joystick's buttons an axes to the standard gamepad layout, and can be used later with love.joystick.loadGamepadMappings.</para>
        /// </summary>
        /// <param name="mappingstring">A string containing the Joystick's gamepad mappings, or nil if the Joystick is not recognized as a gamepad.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mappingstring</term><description>A string containing the Joystick's gamepad mappings, or nil if the Joystick is not recognized as a gamepad.</description></item>
        /// </list>
        /// </returns>
        public string GetGamepadMappingString(string mappingstring = "nil") => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the direction of the Joystick's hat.</para>
        /// </summary>
        /// <param name="hat">The index of the hat to be checked.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>direction</term><description>The direction the hat is pushed.</description></item>
        /// </list>
        /// </returns>
        public JoystickHat GetHat(double hat) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of hats on the joystick.</para>
        /// </summary>
        /// <param name="hats">How many hats the joystick has.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>hats</term><description>How many hats the joystick has.</description></item>
        /// </list>
        /// </returns>
        public double GetHatCount(double hats) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the joystick's unique identifier. The identifier will remain the same for the life of the game, even when the Joystick is disconnected and reconnected, but it will change when the game is re-launched.</para>
        /// </summary>
        /// <param name="id">The Joystick's unique identifier. Remains the same as long as the game is running.</param>
        /// <param name="instanceid">Unique instance identifier. Changes every time the Joystick is reconnected. nil if the Joystick is not connected.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>id</term><description>The Joystick's unique identifier. Remains the same as long as the game is running.</description></item>
        /// <item><term>instanceid</term><description>Unique instance identifier. Changes every time the Joystick is reconnected. nil if the Joystick is not connected.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (double id, double instanceid) GetID(double id, double instanceid = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the name of the joystick.</para>
        /// </summary>
        /// <param name="name">The name of the joystick.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>name</term><description>The name of the joystick.</description></item>
        /// </list>
        /// </returns>
        public string GetName(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the current vibration motor strengths on a Joystick with rumble support.</para>
        /// </summary>
        /// <param name="left">Current strength of the left vibration motor on the Joystick.</param>
        /// <param name="right">Current strength of the right vibration motor on the Joystick.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>left</term><description>Current strength of the left vibration motor on the Joystick.</description></item>
        /// <item><term>right</term><description>Current strength of the right vibration motor on the Joystick.</description></item>
        /// </list>
        /// </returns>
        public (double left, double right) GetVibration(double left, double right) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Joystick is connected.</para>
        /// </summary>
        /// <param name="connected">True if the Joystick is currently connected, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>connected</term><description>True if the Joystick is currently connected, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsConnected(bool connected) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks if a button on the Joystick is pressed.</para>
        /// <para>LÖVE 0.9.0 had a bug which required the button indices passed to Joystick:isDown to be 0-based instead of 1-based, for example button 1 would be 0 for this function. It was fixed in 0.9.1.</para>
        /// </summary>
        /// <param name="buttonN">The index of a button to check.</param>
        /// <param name="">Additional buttons to check.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>anyDown</term><description>True if any supplied button is down, false if not.</description></item>
        /// </list>
        /// </returns>
        // TODO: public bool IsDown(double buttonN, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks if a button on the Joystick is pressed.</para>
        /// <para>LÖVE 0.9.0 had a bug which required the button indices passed to Joystick:isDown to be 0-based instead of 1-based, for example button 1 would be 0 for this function. It was fixed in 0.9.1.</para>
        /// </summary>
        /// <param name="buttons">
        /// Table of button indexes to check.
        /// <list type="bullet">
        /// <item><term>buttonN</term><description>number: The index of a button to check.</description></item>
        /// <item><term></term><description>number ...: Additional buttons to check.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>anyDown</term><description>True if any supplied button is down, false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsDown(object buttons) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Joystick is recognized as a gamepad. In LÖVE, a "gamepad" or "virtual gamepad" use names defined in GamepadAxis and GamepadButton instead of indexes so they are standardized across different operating systems and joystick models. Use gamepad-specific functions to query with these input names: Joystick:getGamepadAxis, Joystick:isGamepadDown, love.gamepadpressed, etc.</para>
        /// <para>LÖVE automatically recognizes most popular controllers with a similar layout to the Xbox 360 controller as gamepads, but you can add more with love.joystick.setGamepadMapping.</para>
        /// </summary>
        /// <param name="isgamepad">True if the Joystick is recognized as a gamepad, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>isgamepad</term><description>True if the Joystick is recognized as a gamepad, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsGamepad(bool isgamepad) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks if a virtual gamepad button on the Joystick is pressed. If the Joystick is not recognized as a Gamepad or isn't connected, then this function will always return false.</para>
        /// </summary>
        /// <param name="buttonN">The gamepad button to check.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>anyDown</term><description>True if any supplied button is down, false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsGamepadDown(GamepadButton buttonN) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks if a virtual gamepad button on the Joystick is pressed. If the Joystick is not recognized as a Gamepad or isn't connected, then this function will always return false.</para>
        /// </summary>
        /// <param name="buttons">
        /// Table of gamepad buttons to check.
        /// <list type="bullet">
        /// <item><term>buttonN</term><description>GamepadButton: The gamepad button to check.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>anyDown</term><description>True if any supplied button is down, false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsGamepadDown(object buttons) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Joystick supports vibration.</para>
        /// </summary>
        /// <param name="supported">True if rumble / force feedback vibration is supported on this Joystick, false if not.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>supported</term><description>True if rumble / force feedback vibration is supported on this Joystick, false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsVibrationSupported(bool supported) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vibration motor speeds on a Joystick with rumble support. Most common gamepads have this functionality, although not all drivers give proper support. Use Joystick:isVibrationSupported to check.</para>
        /// </summary>
        /// <param name="left">Strength of the left vibration motor on the Joystick. Must be in the range of [0, 1].</param>
        /// <param name="right">Strength of the right vibration motor on the Joystick. Must be in the range of [0, 1].</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the vibration was successfully applied, false if not.</description></item>
        /// </list>
        /// </returns>
        public bool SetVibration(double left, double right) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vibration motor speeds on a Joystick with rumble support. Most common gamepads have this functionality, although not all drivers give proper support. Use Joystick:isVibrationSupported to check.</para>
        /// </summary>
        /// <param name="success">True if the vibration was successfully disabled, false if not.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the vibration was successfully disabled, false if not.</description></item>
        /// </list>
        /// </returns>
        public bool SetVibration(bool success) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vibration motor speeds on a Joystick with rumble support. Most common gamepads have this functionality, although not all drivers give proper support. Use Joystick:isVibrationSupported to check.</para>
        /// </summary>
        /// <param name="left">Strength of the left vibration motor on the Joystick. Must be in the range of [0, 1].</param>
        /// <param name="right">Strength of the right vibration motor on the Joystick. Must be in the range of [0, 1].</param>
        /// <param name="duration">The duration of the vibration in seconds. A negative value means infinite duration.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the vibration was successfully applied, false if not.</description></item>
        /// </list>
        /// </returns>
        public bool SetVibration(double left, double right, double duration = -1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vibration motor speeds on a Joystick with rumble support. Most common gamepads have this functionality, although not all drivers give proper support. Use Joystick:isVibrationSupported to check.</para>
        /// </summary>
        public void GetGamepadAxis() => throw new NotImplementedException();
        /// <summary>
        /// <para>Destroys the object's Lua reference. The object will be completely deleted if it's not referenced by any other LÖVE object or thread.</para>
        /// <para>This method can be used to immediately clean up resources without waiting for Lua's garbage collector.</para>
        /// </summary>
        /// <param name="success">True if the object was released by this call, false if it had been previously released.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the object was released by this call, false if it had been previously released.</description></item>
        /// </list>
        /// </returns>
        public bool Release(bool success) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        /// <param name="type">The type as a string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>The type as a string.</description></item>
        /// </list>
        /// </returns>
        public string Type(string type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        /// <param name="name">The name of the type to check for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>b</term><description>True if the object is of the specified type, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool TypeOf(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
    }
}
