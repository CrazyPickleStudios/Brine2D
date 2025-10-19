using Brine2D.Event;
using Brine2D.Event.Messages;
using Brine2D.Event.Messages.Keyboard;
using Brine2D.Event.Messages.Mouse;
using Brine2D.Event.Messages.Touch;
using Brine2D.Event.Messages.Window;
using Brine2D.Graphics;
using Brine2D.Keyboard;
using Brine2D.Timer;
using Brine2D.Window;

namespace Brine2D;

public abstract class Game
{
    protected Game()
    {
        Timer = new TimerModule();
        Keyboard = new KeyboardModule();
        Window = new WindowModule();
        Event = new EventModule();
        Graphics = new GraphicsModule();
    }

    public EventModule Event { get; }

    public GraphicsModule Graphics { get; }

    public KeyboardModule Keyboard { get; }

    public TimerModule Timer { get; }

    public WindowModule Window { get; }

    /// <summary>
    ///     <para>Called when the candidate text for an IME (Input Method Editor) has changed.</para>
    ///     <para>The candidate text is not the final text that the user will eventually choose. Use love.textinput for that.</para>
    /// </summary>
    /// <param name="text">The UTF-8 encoded unicode candidate text.</param>
    /// <param name="start">The start cursor of the selected candidate text.</param>
    /// <param name="length">The length of the selected candidate text. May be 0.</param>
    public virtual void TextEdited(string text, int start, int length)
    {
    }

    // TODO: Move to GameHost?
    internal void Boot()
    {
        // TODO: Init Filesystem.
    }

    internal void Init()
    {
        var c = new
        {
            title = "Untitled",
            //version = love._version,
            window = new
            {
                width = 800,
                height = 600,
                x = (int?)null,
                y = (int?)null,
                minwidth = 1,
                minheight = 1,
                fullscreen = false,
                fullscreentype = FullscreenType.Desktop,
                displayindex = 1,
                vsync = 1,
                msaa = 0,
                borderless = false,
                resizable = false,
                centered = true,
                usedpiscale = true
            }
            //graphics = new{
            //	gammacorrect = false,
            //	lowpower = false,
            //	renderers = nil,
            //	excluderenderers = nil,
            //},
            // 		modules = {
            // 			data = true,
            // 			event = true,
            // 			keyboard = true,
            // 			mouse = true,
            // 			timer = true,
            // 			joystick = true,
            // 			touch = true,
            // 			image = true,
            // 			graphics = true,
            // 			audio = true,
            // 			math = true,
            // 			physics = true,
            // 			sensor = true,
            // 			sound = true,
            // 			system = true,
            // 			font = true,
            // 			thread = true,
            // 			window = true,
            // 			video = true,
            // 		},
            // 		audio = {
            // 			mixwithsystem = true, -- Only relevant for Android / iOS.
            // 			mic = false, -- Only relevant for Android.
            // 		},
            // 		console = false, -- Only relevant for windows.
            // 		identity = false,
            // 		appendidentity = false,
            // 		externalstorage = false, -- Only relevant for Android.
            // 		gammacorrect = nil, -- Moved to t.graphics.
            // 		highdpi = false,
            // 		renderers = nil, -- Moved to t.graphics.
            // 		excluderenderers = nil, -- Moved to t.graphics.
            // 		trackpadtouch = false,
        };
        // 
        // 	-- Console hack, part 1.
        // 	local openedconsole = false
        // 	if love.arg.options.console.set and love._openConsole then
        // 		love._openConsole()
        // 		openedconsole = true
        // 	end
        // 
        // 	-- If config file exists, load it and allow it to update config table.
        // 	local confok, conferr
        // 	if (not love.conf) and love.filesystem and love.filesystem.getInfo("conf.lua") then
        // 		confok, conferr = pcall(require, "conf")
        // 	end
        // 
        // 	-- Yes, conf.lua might not exist, but there are other ways of making
        // 	-- love.conf appear, so we should check for it anyway.
        // 	if love.conf then
        // 		confok, conferr = pcall(love.conf, c)
        // 		-- If love.conf errors, we'll trigger the error after loading modules so
        // 		-- the error message can be displayed in the window.
        // 	end
        // 
        // 	-- Console hack, part 2.
        // 	if c.console and love._openConsole and not openedconsole then
        // 		love._openConsole()
        // 	end
        // 
        // 	if love._setGammaCorrect then
        // 		local gammacorrect = false
        // 		if type(c.graphics) == "table" then
        // 			gammacorrect = c.graphics.gammacorrect
        // 		end
        // 		if c.gammacorrect ~= nil then
        // 			love.markDeprecated(2, "t.gammacorrect in love.conf", "field", "replaced", "t.graphics.gammacorrect")
        // 			gammacorrect = c.gammacorrect
        // 		end
        // 		love._setGammaCorrect(gammacorrect)
        // 	end
        // 
        // 	if love._setLowPowerPreferred and type(c.graphics) == "table" then
        // 		love._setLowPowerPreferred(c.graphics.lowpower)
        // 	end
        // 
        // 	if love._setRenderers then
        // 		local renderers = love._getDefaultRenderers()
        // 		if type(c.renderers) == "table" then
        // 			love.markDeprecated(2, "t.renderers in love.conf", "field", "replaced", "t.graphics.renderers")
        // 			renderers = c.renderers
        // 		end
        // 		if type(c.graphics) == "table" and type(c.graphics.renderers) == "table" then
        // 			renderers = c.graphics.renderers
        // 		end
        // 		if love.arg.options.renderers.set then
        // 			local renderersstr = love.arg.options.renderers.arg[1]
        // 			renderers = {}
        // 			for r in renderersstr:gmatch("[^,]+") do
        // 				table.insert(renderers, r)
        // 			end
        // 		end
        // 
        // 		local excluderenderers = nil
        // 		if type(c.excluderenderers) == "table" then
        // 			love.markDeprecated(2, "t.excluderenderers in love.conf", "field", "replaced", "t.graphics.excluderenderers")
        // 			excluderenderers = c.excluderenderers
        // 		end
        // 		if type(c.graphics) == "table" and type(c.graphics.excluderenderers) == "table" then
        // 			excluderenderers = c.graphics.excluderenderers
        // 		end
        // 		if love.arg.options.excluderenderers.set then
        // 			local excludestr = love.arg.options.excluderenderers.arg[1]
        // 			excluderenderers = {}
        // 			for r in excludestr:gmatch("[^,]+") do
        // 				table.insert(excluderenderers, r)
        // 			end
        // 		end
        // 
        // 		if type(excluderenderers) == "table" then
        // 			for i,v in ipairs(excluderenderers) do
        // 				for j=#renderers, 1, -1 do
        // 					if renderers[j] == v then
        // 						table.remove(renderers, j)
        // 						break
        // 					end
        // 				end
        // 			end
        // 		end
        // 
        // 		love._setRenderers(renderers)
        // 	end
        // 
        // 	if love._setHighDPIAllowed then
        // 		love._setHighDPIAllowed(c.highdpi)
        // 	end
        // 
        // 	if love._setTrackpadTouch then
        // 		love._setTrackpadTouch(c.trackpadtouch)
        // 	end
        // 
        // 	if love._setAudioMixWithSystem then
        // 		if c.audio and c.audio.mixwithsystem ~= nil then
        // 			love._setAudioMixWithSystem(c.audio.mixwithsystem)
        // 		end
        // 	end
        // 
        // 	if love._requestRecordingPermission then
        // 		love._requestRecordingPermission(c.audio and c.audio.mic)
        // 	end
        // 
        // 	-- Gets desired modules.
        // 	for k,v in ipairs{
        // 		"data",
        // 		"thread",
        // 		"timer",
        // 		"event",
        // 		"keyboard",
        // 		"joystick",
        // 		"mouse",
        // 		"touch",
        // 		"sound",
        // 		"system",
        // 		"sensor",
        // 		"audio",
        // 		"image",
        // 		"video",
        // 		"font",
        // 		"window",
        // 		"graphics",
        // 		"math",
        // 		"physics",
        // 	} do
        // 		if c.modules[v] then
        // 			require("love." .. v)
        // 		end
        // 	end
        // 
        // 	if love.event then
        // 		love.createhandlers()
        // 	end
        // 
        // 	-- Check the version
        // 	c.version = tostring(c.version)
        // 	if not love.isVersionCompatible(c.version) then
        // 		local major, minor, revision = c.version:match("^(%d+)%.(%d+)%.(%d+)$")
        // 		if (not major or not minor or not revision) or (major ~= love._version_major and minor ~= love._version_minor) then
        // 			local msg = ("This game indicates it was made for version '%s' of LOVE.\n"..
        // 				"It may not be compatible with the running version (%s)."):format(c.version, love._version)
        // 
        // 			print(msg)
        // 
        // 			if love.window then
        // 				love.window.showMessageBox("Compatibility Warning", msg, "warning")
        // 			end
        // 		end
        // 	end
        // 
        // 	if not confok and conferr then
        // 		error(conferr)
        // 	end
        // 
        // 	-- Setup window here.
        if (c.window != null) // TODO: && c.modules.window)
        {
            // 		if c.window.icon then
            // 			assert(love.image, "If an icon is set in love.conf, love.image must be loaded.")
            // 			love.window.setIcon(love.image.newImageData(c.window.icon))
            // 		end
            // 
            Window.SetTitle(c.title);

            // TODO: This was originally inside an assert to catch errors
            if (!Window.SetMode(c.window.width, c.window.height,
                    new WindowModule.WindowSettings
                    {
                        //fullscreen = c.window.fullscreen,
                        //fstype = c.window.fullscreentype,
                        //vsync = c.window.vsync,
                        //msaa = c.window.msaa,
                        //stencil = c.window.stencil,
                        //depth = c.window.depth,
                        //resizable = c.window.resizable,
                        //minwidth = c.window.minwidth,
                        ////minheight = c.window.minheight,
                        //borderless = c.window.borderless,
                        //centered = c.window.centered,
                        //displayindex = c.window.displayindex,
                        //usedpiscale = c.window.usedpiscale,
                        //x = c.window.x,
                        //y = c.window.y,
                    }))
                throw new Exception("Could not set window mode");

            // 
            // 	-- The first couple event pumps on some systems (e.g. macOS) can take a
            // 	-- while. We'd rather hit that slowdown here than in event processing
            // 	-- within the first frames.
            // 	if love.event then
            // 		for i = 1, 2 do love.event.pump() end
            // 	end
            // 
            // 	-- Our first timestep, because window creation can take some time
            // 	if love.timer then
            // 		love.timer.step()
            // 	end
            // 
            // 	if love.filesystem then
            // 		love.filesystem._setAndroidSaveExternal(c.externalstorage)
            // 		love.filesystem.setIdentity(c.identity or love.filesystem.getIdentity(), c.appendidentity)
            // 		if love.filesystem.getInfo(main_file) then
            // 			require(main_file:gsub("%.lua$", ""))
            // 		end
            // 	end
            // 
            // 	if no_game_code then
            // 		local opts = love.arg.options
            // 		local gamepath = opts.game.set and opts.game.arg[1] or ""
            // 		local gamestr = gamepath == "" and "" or " at "..gamepath
            // 		error("No code to run"..gamestr.."\nYour game might be packaged incorrectly.\nMake sure "..main_file.." is at the top level of the zip or folder.")
            // 	elseif invalid_game_path then
            // 		error("Cannot load game at path '" .. invalid_game_path .. "'.\nMake sure a folder exists at the specified path.")
            // 	end
            // end
        }
    }

    /// <summary>
    ///     This function is called by the default love.run exactly once at the beginning of the game.
    /// </summary>
    /// <remarks>
    ///     In LÖVE 11.0, the passed arguments excludes the game name and the fused command-line flag (if exist) when runs from
    ///     non-fused LÖVE executable. Previous version pass the argument as-is without any filtering.
    /// </remarks>
    /// <param name="arg">Command-line arguments given to the game.</param>
    /// <param name="unfilteredArg">Unfiltered command-line arguments given to the executable (see Notes).</param>
    protected internal virtual void Load(string[] arg, string[] unfilteredArg)
    {
    }

    /// <summary>
    ///     Callback function triggered by the default love.run when the game is closed.
    /// </summary>
    /// <returns>Abort quitting. If true, do not close the game.</returns>
    protected internal virtual bool Quit()
    {
        return false;
    }

    /// <summary>
    ///     The main callback function, containing the main loop. A sensible default is used when left out.
    /// </summary>
    protected internal Func<int?> Run()
    {
        Load(Environment.GetCommandLineArgs().Skip(1).ToArray(), Environment.GetCommandLineArgs());

        Timer.Step();

        return () =>
        {
            Event.Pump();

            foreach (var e in Event.Poll())
                switch (e)
                {
                    // General
                    case QuitMessage quit when !Quit():
                        return quit.ExitStatusCode;
                    case LowMemoryMessage:
                        LowMemory();
                        break;

                    // Window
                    case DirectoryDroppedMessage directoryDropped:
                        DirectoryDropped(directoryDropped.Path);
                        break;
                    case DisplayRotatedMessage displayRotated:
                        DisplayRotated(displayRotated.Index, displayRotated.Orientation);
                        break;
                    case FileDroppedMessage fileDropped:
                        FileDropped(fileDropped.File);
                        break;
                    case FocusMessage focus:
                        Focus(focus.Focus);
                        break;
                    case MouseFocusMessage mouseFocus:
                        MouseFocus(mouseFocus.Focus);
                        break;
                    case ResizeMessage resize:
                        Resize(resize.W, resize.H);
                        break;
                    case VisibleMessage visible:
                        Visible(visible.Visible);
                        break;


                    // Keyboard
                    case KeyPressedMessage keyPressed:
                        KeyPressed(keyPressed.Key, keyPressed.Scancode, keyPressed.IsRepeat);
                        break;
                    case KeyReleasedMessage keyReleased:
                        KeyReleased(keyReleased.Key, keyReleased.Scancode);
                        break;
                    case TextEditedMessage textEdited:
                        TextEdited(textEdited.Text, textEdited.Start, textEdited.Length);
                        break;
                    case TextInputMessage textInput:
                        TextInput(textInput.Text);
                        break;

                    // Mouse
                    case MouseMovedMessage mouseMoved:
                        MouseMoved(mouseMoved.X, mouseMoved.Y, mouseMoved.DX, mouseMoved.DY, mouseMoved.IsTouch);
                        break;
                    case MousePressedMessage mousePressed:
                        MousePressed(mousePressed.X, mousePressed.Y, mousePressed.Button, mousePressed.IsTouch,
                            mousePressed.Presses);
                        break;
                    case MouseReleasedMessage mouseReleased:
                        MouseReleased(mouseReleased.X, mouseReleased.Y, mouseReleased.Button, mouseReleased.IsTouch,
                            mouseReleased.Presses);
                        break;
                    case WheelMovedMessage wheelMoved:
                        WheelMoved(wheelMoved.X, wheelMoved.Y);
                        break;

                    // Joystick


                    // Touch
                    case TouchMovedMessage touchMoved:
                        TouchMoved(touchMoved.Id, touchMoved.X, touchMoved.Y, touchMoved.DX, touchMoved.DY,
                            touchMoved.Pressure);
                        break;
                    case TouchPressedMessage touchPressed:
                        TouchPressed(touchPressed.Id, touchPressed.X, touchPressed.Y, touchPressed.DX, touchPressed.DY,
                            touchPressed.Pressure);
                        break;
                    case TouchReleasedMessage touchReleased:
                        TouchReleased(touchReleased.Id, touchReleased.X, touchReleased.Y, touchReleased.DX,
                            touchReleased.DY, touchReleased.Pressure);
                        break;
                }

            var dt = Timer.Step();

            Update(dt);

            // 	if love.graphics and love.graphics.isActive() then
            // 		love.graphics.origin()
            // 		love.graphics.clear(love.graphics.getBackgroundColor())

            Draw();

            // 		love.graphics.present()
            // 	end

            Timer.Sleep(0.001);

            return null;
        };
    }

    /// <summary>
    ///     Callback function used by the default love.run to update the state of the game every frame.
    /// </summary>
    /// <param name="dt">Time since the last update in seconds.</param>
    protected internal void Update(double dt)
    {
    }

    protected virtual void Config()
    {
        // Override to set configuration.
    }

    /// <summary>
    ///     Callback function triggered when a directory is dragged and dropped onto the window.
    /// </summary>
    /// <remarks>
    ///     Paths passed into this callback are able to be used with love.filesystem.mount, which is the only way to get read
    ///     access via love.filesystem to the dropped directory. love.filesystem.mount does not generally accept other full
    ///     platform-dependent directory paths that haven't been dragged and dropped onto the window.
    /// </remarks>
    /// <param name="path">
    ///     The full platform-dependent path to the directory. It can be used as an argument to
    ///     love.filesystem.mount, in order to gain read access to the directory with love.filesystem.
    /// </param>
    protected virtual void DirectoryDropped(string path)
    {
    }

    /// <summary>
    ///     Called when the device display orientation changed, for example, user rotated their phone 180 degrees.
    /// </summary>
    /// <param name="index">The index of the display that changed orientation.</param>
    /// <param name="orientation">The new orientation.</param>
    protected virtual void DisplayRotated(double index, DisplayOrientation orientation)
    {
    }

    /// <summary>
    ///     Callback function used by the default love.run to draw on the screen every frame.
    /// </summary>
    protected void Draw()
    {
    }

    /// <summary>
    ///     Callback function triggered when a file is dragged and dropped onto the window.
    /// </summary>
    /// <param name="file">The unopened File object representing the file that was dropped.</param>
    protected virtual void FileDropped(DroppedFile file)
    {
    }

    /// <summary>
    ///     Callback function triggered when window receives or loses focus.
    /// </summary>
    /// <param name="focus">True if the window gains focus, false if it loses focus.</param>
    protected virtual void Focus(bool focus)
    {
    }

    /// <summary>
    ///     Callback function triggered when a key is pressed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Scancodes are keyboard layout-independent, so the scancode "w" will be generated if the key in the same place
    ///         as the "w" key on an American keyboard is pressed, no matter what the key is labelled or what the user's
    ///         operating system settings are.
    ///     </para>
    ///     <para>
    ///         Key repeat needs to be enabled with love.keyboard.setKeyRepeat for repeat keypress events to be received.
    ///         This does not affect love.textInput.
    ///     </para>
    /// </remarks>
    /// <param name="key">Character of the pressed key.</param>
    /// <param name="scancode">The scancode representing the pressed key.</param>
    /// <param name="isRepeat">
    ///     Whether this keypress event is a repeat. The delay between key repeats depends on the user's
    ///     system settings.
    /// </param>
    protected virtual void KeyPressed(Key key, Scancode scancode, bool isRepeat)
    {
    }

    /// <summary>
    ///     Callback function triggered when a keyboard key is released.
    /// </summary>
    /// <remarks>
    ///     Scancodes are keyboard layout-independent, so the scancode "w" will be used if the key in the same place as the "w"
    ///     key on an American keyboard is released, no matter what the key is labelled or what the user's operating system
    ///     settings are.
    /// </remarks>
    /// <param name="key">Character of the released key.</param>
    /// <param name="scancode">The scancode representing the released key.</param>
    protected virtual void KeyReleased(Key key, Scancode scancode)
    {
    }

    /// <summary>
    ///     <para>Callback function triggered when the system is running out of memory on mobile devices.</para>
    ///     <para>
    ///         Mobile operating systems may forcefully kill the game if it uses too much memory, so any non-critical
    ///         resource should be removed if possible (by setting all variables referencing the resources to nil), when this
    ///         event is triggered. Sounds and images in particular tend to use the most memory.
    ///     </para>
    /// </summary>
    protected virtual void LowMemory()
    {
    }

    /// <summary>
    ///     Callback function triggered when window receives or loses mouse focus.
    /// </summary>
    /// <param name="focus">Whether the window has mouse focus or not.</param>
    protected virtual void MouseFocus(bool focus)
    {
    }

    /// <summary>
    ///     Callback function triggered when the mouse is moved.
    /// </summary>
    /// <remarks>
    ///     If Relative Mode is enabled for the mouse, the dx and dy arguments of this callback will update but x and y are not
    ///     guaranteed to.
    /// </remarks>
    /// <param name="x">The mouse position on the x-axis.</param>
    /// <param name="y">The mouse position on the y-axis.</param>
    /// <param name="dx">The amount moved along the x-axis since the last time love.mousemoved was called.</param>
    /// <param name="dy">The amount moved along the y-axis since the last time love.mousemoved was called.</param>
    /// <param name="isTouch">True if the mouse button press originated from a touchscreen touch-press.</param>
    protected virtual void MouseMoved(double x, double y, double dx, double dy, bool isTouch)
    {
    }

    /// <summary>
    ///     Callback function triggered when a mouse button is pressed.
    /// </summary>
    /// <remarks>
    ///     Use love.wheelmoved to detect mouse wheel motion. It will not register as a button press in version 0.10.0 and
    ///     newer.
    /// </remarks>
    /// <param name="x">Mouse x position, in pixels.</param>
    /// <param name="y">Mouse y position, in pixels.</param>
    /// <param name="button">
    ///     The button index that was pressed. 1 is the primary mouse button, 2 is the secondary mouse button
    ///     and 3 is the middle button. Further buttons are mouse dependent.
    /// </param>
    /// <param name="isTouch">True if the mouse button press originated from a touchscreen touch-press.</param>
    /// <param name="presses">
    ///     The number of presses in a short time frame and small area, used to simulate double, triple
    ///     clicks
    /// </param>
    protected virtual void MousePressed(double x, double y, double button, bool isTouch, double presses)
    {
    }

    /// <summary>
    ///     Callback function triggered when a mouse button is released.
    /// </summary>
    /// <param name="x">Mouse x position, in pixels.</param>
    /// <param name="y">Mouse y position, in pixels.</param>
    /// <param name="button">
    ///     The button index that was released. 1 is the primary mouse button, 2 is the secondary mouse button
    ///     and 3 is the middle button. Further buttons are mouse dependent.
    /// </param>
    /// <param name="isTouch">True if the mouse button release originated from a touchscreen touch-release.</param>
    /// <param name="presses">
    ///     The number of presses in a short time frame and small area, used to simulate double, triple
    ///     clicks
    /// </param>
    protected virtual void MouseReleased(double x, double y, double button, bool isTouch, double presses)
    {
    }

    /// <summary>
    ///     Called when the window is resized, for example if the user resizes the window, or if love.window.setMode is called
    ///     with an unsupported width or height in fullscreen and the window chooses the closest appropriate size.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Calls to love.window.setMode will only trigger this event if the width or height of the window after the call
    ///         doesn't match the requested width and height. This can happen if a fullscreen mode is requested which doesn't
    ///         match any supported mode, or if the fullscreen type is 'desktop' and the requested width or height don't match
    ///         the desktop resolution.
    ///     </para>
    ///     <para>Since 11.0, this function returns width and height in DPI-scaled units rather than pixels.</para>
    /// </remarks>
    /// <param name="w">The new width.</param>
    /// <param name="h">The new height.</param>
    protected virtual void Resize(double w, double h)
    {
    }

    /// <summary>
    ///     Called when text has been entered by the user. For example if shift-2 is pressed on an American keyboard layout,
    ///     the text "@" will be generated.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Although Lua strings can store UTF-8 encoded unicode text just fine, many functions in Lua's string library
    ///         will not treat the text as you might expect. For example, #text (and string.len(text)) will give the number of
    ///         bytes in the string, rather than the number of unicode characters. The Lua wiki and a presentation by one of
    ///         Lua's creators give more in-depth explanations, with some tips.
    ///     </para>
    ///     <para>On Android and iOS, text input is disabled by default; call love.keyboard.setTextInput to enable it.</para>
    /// </remarks>
    /// <param name="text">The UTF-8 encoded unicode text.</param>
    protected virtual void TextInput(string text)
    {
    }

    /// <summary>
    ///     Callback function triggered when a touch press moves inside the touch screen.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The identifier is only guaranteed to be unique for the specific touch press until love.touchreleased is
    ///         called with that identifier, at which point it may be reused for new touch presses.
    ///     </para>
    ///     <para>
    ///         The unofficial Android and iOS ports of LÖVE 0.9.2 reported touch positions as normalized values in the range
    ///         of [0, 1], whereas this API reports positions in pixels.
    ///     </para>
    /// </remarks>
    /// <param name="id">The identifier for the touch press.</param>
    /// <param name="x">The x-axis position of the touch inside the window, in pixels.</param>
    /// <param name="y">The y-axis position of the touch inside the window, in pixels.</param>
    /// <param name="dx">The x-axis movement of the touch inside the window, in pixels.</param>
    /// <param name="dy">The y-axis movement of the touch inside the window, in pixels.</param>
    /// <param name="pressure">
    ///     The amount of pressure being applied. Most touch screens aren't pressure sensitive, in which
    ///     case the pressure will be 1.
    /// </param>
    protected virtual void TouchMoved(object id, double x, double y, double dx, double dy, double pressure)
    {
    }

    /// <summary>
    ///     Callback function triggered when the touch screen is touched.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The identifier is only guaranteed to be unique for the specific touch press until love.touchreleased is
    ///         called with that identifier, at which point it may be reused for new touch presses.
    ///     </para>
    ///     <para>
    ///         The unofficial Android and iOS ports of LÖVE 0.9.2 reported touch positions as normalized values in the range
    ///         of [0, 1], whereas this API reports positions in pixels.
    ///     </para>
    /// </remarks>
    /// <param name="id">The identifier for the touch press.</param>
    /// <param name="x">The x-axis position of the touch press inside the window, in pixels.</param>
    /// <param name="y">The y-axis position of the touch press inside the window, in pixels.</param>
    /// <param name="dx">The x-axis movement of the touch press inside the window, in pixels. This should always be zero.</param>
    /// <param name="dy">The y-axis movement of the touch press inside the window, in pixels. This should always be zero.</param>
    /// <param name="pressure">
    ///     The amount of pressure being applied. Most touch screens aren't pressure sensitive, in which
    ///     case the pressure will be 1.
    /// </param>
    protected virtual void TouchPressed(object id, double x, double y, double dx, double dy, double pressure)
    {
    }

    /// <summary>
    ///     Callback function triggered when the touch screen stops being touched.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The identifier is only guaranteed to be unique for the specific touch press until love.touchreleased is
    ///         called with that identifier, at which point it may be reused for new touch presses.
    ///     </para>
    ///     <para>
    ///         The unofficial Android and iOS ports of LÖVE 0.9.2 reported touch positions as normalized values in the range
    ///         of [0, 1], whereas this API reports positions in pixels.
    ///     </para>
    /// </remarks>
    /// <param name="id">The identifier for the touch press.</param>
    /// <param name="x">The x-axis position of the touch inside the window, in pixels.</param>
    /// <param name="y">The y-axis position of the touch inside the window, in pixels.</param>
    /// <param name="dx">The x-axis movement of the touch inside the window, in pixels.</param>
    /// <param name="dy">The y-axis movement of the touch inside the window, in pixels.</param>
    /// <param name="pressure">
    ///     The amount of pressure being applied. Most touch screens aren't pressure sensitive, in which
    ///     case the pressure will be 1.
    /// </param>
    protected virtual void TouchReleased(object id, double x, double y, double dx, double dy, double pressure)
    {
    }

    /// <summary>
    ///     Callback function triggered when window is minimized/hidden or unminimized by the user.
    /// </summary>
    /// <param name="visible">True if the window is visible, false if it isn't.</param>
    protected virtual void Visible(bool visible)
    {
    }

    /// <summary>
    ///     Callback function triggered when the mouse wheel is moved.
    /// </summary>
    /// <param name="x">Amount of horizontal mouse wheel movement. Positive values indicate movement to the right.</param>
    /// <param name="y">Amount of vertical mouse wheel movement. Positive values indicate upward movement.</param>
    protected virtual void WheelMoved(double x, double y)
    {
    }
}