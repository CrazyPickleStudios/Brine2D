using SDL;
using System;
using System.Linq;
using static SDL.SDL3;

namespace Brine2D;

/// <summary>
/// Provides an interface for modifying and retrieving information about the program's window.
/// </summary>
// TODO: Needs review
public unsafe sealed class WindowModule : Module
{
    enum Setting
    {
        SETTING_FULLSCREEN,
        SETTING_FULLSCREEN_TYPE,
        SETTING_VSYNC,
        SETTING_MSAA,
        SETTING_STENCIL,
        SETTING_DEPTH,
        SETTING_RESIZABLE,
        SETTING_MIN_WIDTH,
        SETTING_MIN_HEIGHT,
        SETTING_BORDERLESS,
        SETTING_CENTERED,
        SETTING_DISPLAYINDEX,
        SETTING_USE_DPISCALE,
        SETTING_REFRESHRATE,
        SETTING_X,
        SETTING_Y,
        SETTING_MAX_ENUM
    };
    
    enum FileDialogType
    {
        FILEDIALOG_OPENFILE,
        FILEDIALOG_OPENFOLDER,
        FILEDIALOG_SAVEFILE,
        FILEDIALOG_MAX_ENUM
    };

    public struct WindowSize : IEquatable<WindowSize>
    {
        public int Width { get; }
        public int Height { get; }

        public WindowSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(WindowSize other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object? obj) => obj is WindowSize other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height);

        public static bool operator ==(WindowSize left, WindowSize right) => left.Equals(right);
        public static bool operator !=(WindowSize left, WindowSize right) => !left.Equals(right);
    }

    //std::string title;
    // 
     	int windowWidth  = 800;
     	int windowHeight = 600;
     	int pixelWidth   = 800;
     	int pixelHeight  = 600;
     	WindowSettings settings;
    // 	StrongRef<love::image::ImageData> icon;
    // 
    // #ifdef LOVE_WINDOWS
    // 	bool canUseDwmFlush = false;
    // #endif
    // 
     	bool open;
     
     	bool mouseGrabbed;
    // 
     	SDL_Window* window;
    // 
    // 	SDL_GLContext glcontext;
    // #ifdef LOVE_GRAPHICS_METAL
    // 	SDL_MetalView metalView;
    // #endif
    // 
    // 	graphics::Renderer windowRenderer = graphics::RENDERER_NONE;
    // 
     	bool displayedWindowError;
     	ContextAttribs contextAttribs;
     
    private GraphicsModule graphics;
    
    internal void SetGraphics(GraphicsModule graphics)
     {
     	this.graphics = graphics;
     }

    // 	Uint32 dialogEventId;

    struct ContextAttribs
    {
        int versionMajor;
        int versionMinor;
        bool gles;
        bool debug;
    };

    internal void GetWindow(out int width, out int height, out WindowSettings newSettings)
    {
        if (window != null)
            UpdateSettings(settings, true);

        width = windowWidth;
        height = windowHeight;
        newSettings = settings;
    }

    private void UpdateSettings(WindowSettings newsettings, bool updateGraphicsViewport)
     {
     	SDL_SyncWindow(window);
     
     	var wflags = SDL_GetWindowFlags(window);

        // TODO: Fix this whole block, seems weird.
        int h;
        int w;
     	SDL_GetWindowSize(window, &w, &h);
     
     	pixelWidth = windowWidth = w;
     	pixelHeight = windowHeight = h;
     
    	SDL_GetWindowSizeInPixels(window, &w, &h);

        pixelWidth = w;
        pixelHeight = h;

         	if (((wflags & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) == SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) && SDL_GetWindowFullscreenMode(window) == null)
         	{
         		settings.fullscreen = true;
         		settings.fstype = FullscreenType.Desktop;
         	}
         	else if ((wflags & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) == SDL_WindowFlags.SDL_WINDOW_FULLSCREEN)
         	{
         		settings.fullscreen = true;
         		settings.fstype = FullscreenType.Exclusive;
         	}
         	else
         	{
         		settings.fullscreen = false;
         		settings.fstype = newsettings.fstype;
         	}
         
         #if LOVE_ANDROID
         	settings.fullscreen = love::android::getImmersive();
         #endif
         
         	settings.minwidth = newsettings.minwidth;
         	settings.minheight = newsettings.minheight;
         
         	settings.resizable = (wflags & SDL_WindowFlags.SDL_WINDOW_RESIZABLE) != 0;
         	settings.borderless = (wflags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0;
         	settings.centered = newsettings.centered;
         
            // TODO: Solidify this.
         	var p = GetPosition();
            settings.x = p.x;
            settings.y = p.y;
            settings.displayindex = p.displayindex;
         
         	SetHighDPIAllowed((wflags & SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY) != 0);
         
         	settings.usedpiscale = newsettings.usedpiscale;
         
         	if (settings.fullscreen && settings.fstype == FullscreenType.Exclusive)
         		SDL_SetHint(SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS, "1");
         	else
         		SDL_SetHint(SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS, "0");
         
         	settings.vsync = GetVSync();
         
         	settings.stencil = newsettings.stencil;
         	settings.depth = newsettings.depth;
         
         	SDLDisplayIDs displayids = new();
         	var dmode = SDL_GetCurrentDisplayMode(displayids.ids[settings.displayindex]);
         
         	settings.refreshrate = dmode->refresh_rate;
         
            // TODO: Implement
         	//if (updateGraphicsViewport && graphics.Get())
         	//{
         	//	double scaledw, scaledh;
         	//	FromPixels((double) pixelWidth, (double) pixelHeight, scaledw, scaledh);
         	//	graphics.BackbufferChanged((int) scaledw, (int) scaledh, pixelWidth, pixelHeight);
         	//}
     }
    internal bool SetWindow(int width, int height, WindowSettings? settings)
     {
    // 	if (!graphics.get())
    // 		graphics.set(Module::getInstance<graphics::Graphics>(Module::M_GRAPHICS));
    // 
    // 	if (graphics.get() && graphics->isRenderTargetActive())
    // 		throw love::Exception("love.window.setMode cannot be called while a render target is active in love.graphics.");
    // 
    // 	auto renderer = graphics != nullptr ? graphics->getRenderer() : graphics::RENDERER_NONE;
    // 
     	if (IsOpen())
     		UpdateSettings(this.settings, false);
     
     	WindowSettings f = new();
     
     	if (settings != null)
     		f = settings;
     
     	f.minwidth = Math.Max(f.minwidth, 1);
     	f.minheight = Math.Max(f.minheight, 1);
     
     	SDLDisplayIDs displays = new();
     	int displaycount = displays.count;
     
     	f.displayindex = Math.Min(Math.Max(f.displayindex, 0), displaycount - 1);
     
     	if (width == 0 || height == 0)
     	{
     		var mode = SDL_GetDesktopDisplayMode(displays.ids[f.displayindex]);
     		width = mode->w;
     		height = mode->h;
     	}
     
     #if LOVE_ANDROID
     	bool fullscreen = f.fullscreen;
     
     	f.fullscreen = false;
     	f.fstype = FULLSCREEN_DESKTOP;
     #endif
     
     	int x = f.x;
     	int y = f.y;
     
     	if (f.useposition)
     	{
     		// The position needs to be in the global coordinate space.
     		SDL_Rect displaybounds;
     		SDL_GetDisplayBounds(displays.ids[f.displayindex], &displaybounds);
     		x += displaybounds.x;
     		y += displaybounds.y;
     	}
     	else
     	{
     		if (f.centered)
     			x = y = SDL_WINDOWPOS_CENTERED_DISPLAY(displays.ids[f.displayindex]);
     		else
     			x = y = SDL_WINDOWPOS_UNDEFINED_DISPLAY(displays.ids[f.displayindex]);
     	}

        SDL_WindowFlags sdlflags = 0;
     	SDL_DisplayMode fsmode = new();
     
     	if (f.fullscreen)
     	{
     		sdlflags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
     
     		if (f.fstype == FullscreenType.Exclusive)
     		{
     			SDL_DisplayID display = displays.ids[f.displayindex];
     			if (!SDL_GetClosestFullscreenDisplayMode(display, width, height, 0, IsHighDPIAllowed(), &fsmode))
     			{
     				// GetClosestDisplayMode will fail if we request a size larger
     				// than the largest available display mode, so we'll try to use
     				// the largest (first) mode in that case.
     				int modecount = 0;
     				SDL_DisplayMode **modes = SDL_GetFullscreenDisplayModes(display, &modecount);
     				if (modecount > 0)
     					fsmode = *modes[0];
     				SDL_free(modes);
     				if (fsmode.w == 0 || fsmode.h == 0)
     					return false;
     			}
     		}
     	}
     
     	bool needsetmode = false;
     
    // 	if (renderer != windowRenderer && isOpen())
    // 		close();
     
     	if (IsOpen())
     	{
     		if (fsmode.w > 0 && fsmode.h > 0)
     			SDL_SetWindowFullscreenMode(window, &fsmode);
     		else
     			SDL_SetWindowFullscreenMode(window, null);
     
            // TODO:
     		//if (SDL_SetWindowFullscreen(window, (sdlflags & SDL_WINDOW_FULLSCREEN) != 0) && renderer == graphics::RENDERER_OPENGL)
     		//	SDL_GL_MakeCurrent(window, glcontext);
     
     		// TODO: should we make this conditional, to avoid love.resize events when the size doesn't change?
     		SDL_SetWindowSize(window, width, height);
     
     		if (this.settings.resizable != f.resizable)
     			SDL_SetWindowResizable(window, f.resizable);
     
     		if (this.settings.borderless != f.borderless)
     			SDL_SetWindowBordered(window, !f.borderless);
     	}
     	else
     	{
            // TODO:
//            if (renderer == graphics::RENDERER_OPENGL)
//                sdlflags |= SDL_WINDOW_OPENGL;
//# if LOVE_GRAPHICS_METAL
//            if (renderer == graphics::RENDERER_METAL)
//                sdlflags |= SDL_WINDOW_METAL;
//#endif

//            if (renderer == graphics::RENDERER_VULKAN)
//                sdlflags |= SDL_WINDOW_VULKAN;

//            if (f.resizable)
//                sdlflags |= SDL_WINDOW_RESIZABLE;

//            if (f.borderless)
//                sdlflags |= SDL_WINDOW_BORDERLESS;

//            // Note: this flag is ignored on Windows.
//            if (isHighDPIAllowed())
//                sdlflags |= SDL_WINDOW_HIGH_PIXEL_DENSITY;

            SDL_WindowFlags createflags = sdlflags & (~SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);

            if (!CreateWindowAndContext(x, y, width, height, createflags))// TODO: , renderer))
                return false;

            if (f.fullscreen)
            {
                if (fsmode.w > 0 && fsmode.h > 0)
                    SDL_SetWindowFullscreenMode(window, &fsmode);
                else
                    SDL_SetWindowFullscreenMode(window, null);
                SDL_SetWindowFullscreen(window, true);
            }

            needsetmode = true;
     	}
     
        // TODO:
    // 	windowRenderer = renderer;
    // 
    // 	// Make sure the window keeps any previously set icon.
    // 	setIcon(icon.get());
    // 
    // 	SetMouseGrab(mouseGrabbed);
    // 
    
     	SDL_SetWindowMinimumSize(window, f.minwidth, f.minheight);
     
     	if (this.settings.displayindex != f.displayindex || f.useposition || f.centered)
     		SDL_SetWindowPosition(window, x, y);
     
     	SDL_RaiseWindow(window);
     
     	SetVSync(f.vsync);
     
     	UpdateSettings(f, false);
     
        // TODO:
    // 	if (graphics.get())
    // 	{
    // 		double scaledw, scaledh;
    // 		fromPixels((double) pixelWidth, (double) pixelHeight, scaledw, scaledh);
    // 
    // 		if (needsetmode)
    // 		{
    // 			void *context = nullptr;
    // 			if (renderer == graphics::RENDERER_OPENGL)
    // 				context = (void *) glcontext;
    // #ifdef LOVE_GRAPHICS_METAL
    // 			if (renderer == graphics::RENDERER_METAL && metalView)
    // 				context = (void *) SDL_Metal_GetLayer(metalView);
    // #endif
    // 
    // 			// TODO: try/catch
    // 			graphics->setMode(context, (int) scaledw, (int) scaledh, pixelWidth, pixelHeight, f.stencil, f.depth, f.msaa);
    // 		}
    // 		else
    // 		{
    // 			graphics->backbufferChanged((int) scaledw, (int) scaledh, pixelWidth, pixelHeight, f.stencil, f.depth, f.msaa);
    // 		}
    // 
    // 		this->settings.msaa = graphics->getBackbufferMSAA();
    // 	}
    // 
    // 	// Set fullscreen when user requested it before.
    // 	// See above for explanation.
    // #ifdef LOVE_ANDROID
    // 	setFullscreen(fullscreen);
    // 	love::android::setImmersive(fullscreen);
    // #endif
     
     	SDL_SyncWindow(window);
     
    	return true;
    }

    bool IsHighDPIAllowed()
    {
        return highDPIAllowed;
    }

    static bool highDPIAllowed = false;

    void SetHighDPIAllowed(bool enable)
    {
        // TODO: Do something?
        highDPIAllowed = enable;
    }

    public class WindowSettings
    {
        public bool fullscreen = false;
        public FullscreenType fstype = FullscreenType.Desktop;
        public int vsync = 1;
        public int msaa = 0;
        public bool stencil = true;
        public bool depth = false;
        public bool resizable = false;
        public int minwidth = 1;
        public int minheight = 1;
        public bool borderless = false;
        public bool centered = true;
        public int displayindex = 0;
        public bool usedpiscale = true;
        public double refreshrate = 0.0;
        public bool useposition = false;
        public int x = 0;
        public int y = 0;
    };

    class MessageBoxData
    {
        public MessageBoxType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Buttons { get; set; } = new();
        public int EnterButtonIndex { get; set; }
        public int EscapeButtonIndex { get; set; }
        public bool AttachToWindow { get; set; }
    }

    public class FileDialogFilter
    {
        public string Name { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
    }

    class FileDialogData
    {
        public FileDialogType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AcceptLabel { get; set; } = string.Empty;
        public string CancelLabel { get; set; } = string.Empty;
        public string DefaultName { get; set; } = string.Empty;
        public List<FileDialogFilter> Filters { get; set; } = new();
        public bool MultiSelect { get; set; }
        public bool AttachToWindow { get; set; }
    }

    internal WindowModule()
    {
        open = false;
        mouseGrabbed = false;
        window = null;
        displayedWindowError = false;

        // TODO: This doesn't match the C++
        settings = new();

        if (!SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS))
         		throw new Exception($"Could not initialize SDL video subsystem ({SDL_GetError()})");
         
        SetDisplaySleepEnabled(false);
         
        // #ifdef LOVE_WINDOWS
        // 	// Turned off by default, because it (ironically) causes stuttering issues
        // 	// on some setups. More investigation is needed before enabling it.
        // 	canUseDwmFlush = SDL_GetHintBoolean("LOVE_GRAPHICS_VSYNC_DWM", false);
        // #endif
        // 
        // 	dialogEventId = SDL_RegisterEvents(1);
    }

    protected internal override void Dispose()
    {
        Close(false);
        //graphics.set(nullptr);
        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS);

        base.Dispose();
    }
    
    /// <summary>
    /// <para>Closes the window. It can be reopened with love.window.setMode.</para>
    /// </summary>
    public void Close()
    {
        Close(true);
    }

    void Close(bool allowExceptions)
    {
        // 	if (graphics.get())
        // 	{
        // 		if (allowExceptions && graphics->isRenderTargetActive())
        // 			throw love::Exception("love.window.close cannot be called while a render target is active in love.graphics.");
        // 
        // 		graphics->unSetMode();
        // 	}
        // 
        // 	if (glcontext)
        // 	{
        // 		SDL_GL_DestroyContext(glcontext);
        // 		glcontext = nullptr;
        // 	}
        // 
        // #ifdef LOVE_GRAPHICS_METAL
        // 	if (metalView)
        // 	{
        // 		SDL_Metal_DestroyView(metalView);
        // 		metalView = nullptr;
        // 	}
        // #endif

        if (window != null)
        {
            SDL_DestroyWindow(window);
            window = null;

            SDL_FlushEvents(SDL_EventType.SDL_EVENT_WINDOW_FIRST, SDL_EventType.SDL_EVENT_WINDOW_LAST);
        }

        open = false;
    }

    /// <summary>
    /// <para>Converts a number from pixels to density-independent units.</para>
    /// <para>The pixel density inside the window might be greater (or smaller) than the "size" of the window. For example on a retina screen in Mac OS X with the highdpi window flag enabled, the window may take up the same physical size as an 800x600 window, but the area inside the window uses 1600x1200 pixels. love.window.fromPixels(1600) would return 800 in that case.</para>
    /// <para>This function converts coordinates from pixels to the size users are expecting them to display at onscreen. love.window.toPixels does the opposite. The highdpi window flag must be enabled to use the full pixel density of a Retina screen on Mac OS X and iOS. The flag currently does nothing on Windows and Linux, and on Android it is effectively always enabled.</para>
    /// <para>Most LÖVE functions return values and expect arguments in terms of pixels rather than density-independent units.</para>
    /// </summary>
    /// <param name="pixelvalue">A number in pixels to convert to density-independent units.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>value</term><description>The converted number, in density-independent units.</description></item>
    /// </list>
    /// </returns>
    public double FromPixels(double pixelvalue)
    {
        return pixelvalue / GetDPIScale();
    }

    /// <summary>
    /// <para>Converts a number from pixels to density-independent units.</para>
    /// <para>The pixel density inside the window might be greater (or smaller) than the "size" of the window. For example on a retina screen in Mac OS X with the highdpi window flag enabled, the window may take up the same physical size as an 800x600 window, but the area inside the window uses 1600x1200 pixels. love.window.fromPixels(1600) would return 800 in that case.</para>
    /// <para>This function converts coordinates from pixels to the size users are expecting them to display at onscreen. love.window.toPixels does the opposite. The highdpi window flag must be enabled to use the full pixel density of a Retina screen on Mac OS X and iOS. The flag currently does nothing on Windows and Linux, and on Android it is effectively always enabled.</para>
    /// <para>Most LÖVE functions return values and expect arguments in terms of pixels rather than density-independent units.</para>
    /// </summary>
    /// <param name="px">The x-axis value of a coordinate in pixels.</param>
    /// <param name="py">The y-axis value of a coordinate in pixels.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>x</term><description>The converted x-axis value of the coordinate, in density-independent units.</description></item>
    /// <item><term>y</term><description>The converted y-axis value of the coordinate, in density-independent units.</description></item>
    /// </list>
    /// </returns>
    public (double x, double y) FromPixels(double px, double py)
    {
        double scale = GetDPIScale();
        var x = px / scale;
        var y = py / scale;
        return (x, y);
    }

    /// <summary>
    /// <para>Gets the DPI scale factor associated with the window.</para>
    /// <para>The pixel density inside the window might be greater (or smaller) than the "size" of the window. For example on a retina screen in Mac OS X with the highdpi window flag enabled, the window may take up the same physical size as an 800x600 window, but the area inside the window uses 1600x1200 pixels. love.window.getDPIScale() would return 2.0 in that case.</para>
    /// <para>The love.window.fromPixels and love.window.toPixels functions can also be used to convert between units.</para>
    /// <para>The highdpi window flag must be enabled to use the full pixel density of a Retina screen on Mac OS X and iOS. The flag currently does nothing on Windows and Linux, and on Android it is effectively always enabled.</para>
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>scale</term><description>The pixel scale factor associated with the window.</description></item>
    /// </list>
    /// </returns>
    public double GetDPIScale()
    {
        return settings.usedpiscale ? GetNativeDPIScale() : 1.0;
    }

    private double GetNativeDPIScale()
    {
#if LOVE_ANDROID
        return love::android::getScreenScale();
#else
        return window != null ? SDL_GetWindowDisplayScale(window) : 1.0;
#endif
    }

    /// <summary>
/// <para>Gets the width and height of the desktop.</para>
/// </summary>
/// <param name="displayindex">The index of the display, if multiple monitors are available. First is 1.</param>
/// <returns>
/// <list type="bullet">
/// <item><term>width</term><description>The width of the desktop.</description></item>
/// <item><term>height</term><description>The height of the desktop.</description></item>
/// </list>
/// </returns>
public (double width, double height) GetDesktopDimensions(double displayindex = 1) =>
        throw new NotImplementedException();

    /// <summary>
    /// <para>Gets the width and height of the desktop.</para>
    /// </summary>
    public void GetMode() => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets the width and height of the window.</para>
    /// </summary>
    /// <param name="width">The width of the window.</param>
    /// <param name="height">The height of the window.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>width</term><description>The width of the window.</description></item>
    /// <item><term>height</term><description>The height of the window.</description></item>
    /// </list>
    /// </returns>
    public (double width, double height) GetDimensions(double width, double height) =>
        throw new NotImplementedException();

    /// <summary>
    /// <para>Gets the number of connected monitors.</para>
    /// </summary>
    /// <param name="count">The number of currently connected displays.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>count</term><description>The number of currently connected displays.</description></item>
    /// </list>
    /// </returns>
    public double GetDisplayCount(double count)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <para>Gets the name of a display.</para>
    /// </summary>
    /// <param name="displayindex">The index of the display to get the name of.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>name</term><description>The name of the specified display.</description></item>
    /// </list>
    /// </returns>
    public string GetDisplayName(double displayindex = 1) => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets current device display orientation.</para>
    /// </summary>
    /// <param name="displayindex">Display index to get its display orientation, or nil for default display index.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>orientation</term><description>Current device display orientation.</description></item>
    /// </list>
    /// </returns>
    public DisplayOrientation GetDisplayOrientation(double? displayindex = null) => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets whether the window is fullscreen.</para>
    /// </summary>
    /// <param name="fullscreen">True if the window is fullscreen, false otherwise.</param>
    /// <param name="fstype">The type of fullscreen mode used.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>fullscreen</term><description>True if the window is fullscreen, false otherwise.</description></item>
    /// <item><term>fstype</term><description>The type of fullscreen mode used.</description></item>
    /// </list>
    /// </returns>
    public (bool fullscreen, FullscreenType fstype) GetFullscreen(bool fullscreen, FullscreenType fstype) =>
        throw new NotImplementedException();

    /// <summary>
    /// <para>Gets a list of supported fullscreen modes.</para>
    /// </summary>
    /// <param name="displayindex">The index of the display, if multiple monitors are available.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>modes</term><description>A table of width/height pairs. (Note that this may not be in order.)</description></item>
    /// </list>
    /// </returns>
    public object GetFullscreenModes(double displayindex = 1) => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets the height of the window.</para>
    /// </summary>
    /// <returns>The height of the window.
    /// </returns>
    public double GetHeight()
    {
        return windowHeight;
    }

    /// <summary>
    /// <para>Gets the window icon.</para>
    /// </summary>
    /// <param name="imagedata">The window icon imagedata, or nil if no icon has been set with .</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>imagedata</term><description>The window icon imagedata, or nil if no icon has been set with .</description></item>
    /// </list>
    /// </returns>
    public object GetIcon(object imagedata) => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets the display mode and properties of the window.</para>
    /// </summary>
    /// <param name="width">Window width.</param>
    /// <param name="height">Window height.</param>
    /// <param name="flags">
    /// Table with the window properties:
    /// <list type="bullet">
    /// <item><term>fullscreen</term><description>boolean: Fullscreen (true), or windowed (false).</description></item>
    /// <item><term>fullscreentype</term><description>FullscreenType: The type of fullscreen mode used.</description></item>
    /// <item><term>vsync</term><description>number: 1 if the graphics framerate is synchronized with the monitor's refresh rate, 0 otherwise.</description></item>
    /// <item><term>msaa</term><description>number: The number of antialiasing samples used (0 if MSAA is disabled).</description></item>
    /// <item><term>resizable</term><description>boolean: True if the window is resizable in windowed mode, false otherwise.</description></item>
    /// <item><term>borderless</term><description>boolean: True if the window is borderless in windowed mode, false otherwise.</description></item>
    /// <item><term>centered</term><description>boolean: True if the window is centered in windowed mode, false otherwise.</description></item>
    /// <item><term>display</term><description>number: The index of the display the window is currently in, if multiple monitors are available. First is 1.</description></item>
    /// <item><term>minwidth</term><description>number: The minimum width of the window, if it's resizable.</description></item>
    /// <item><term>minheight</term><description>number: The minimum height of the window, if it's resizable.</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>width</term><description>Window width.</description></item>
    /// <item><term>height</term><description>Window height.</description></item>
    /// <item><term>flags</term><description>
    /// Table with the window properties:
    /// <list type="bullet">
    /// <item><term>fullscreen</term><description>boolean: Fullscreen (true), or windowed (false).</description></item>
    /// <item><term>fullscreentype</term><description>FullscreenType: The type of fullscreen mode used.</description></item>
    /// <item><term>vsync</term><description>number: 1 if the graphics framerate is synchronized with the monitor's refresh rate, 0 otherwise.</description></item>
    /// <item><term>msaa</term><description>number: The number of antialiasing samples used (0 if MSAA is disabled).</description></item>
    /// <item><term>resizable</term><description>boolean: True if the window is resizable in windowed mode, false otherwise.</description></item>
    /// <item><term>borderless</term><description>boolean: True if the window is borderless in windowed mode, false otherwise.</description></item>
    /// <item><term>centered</term><description>boolean: True if the window is centered in windowed mode, false otherwise.</description></item>
    /// <item><term>display</term><description>number: The index of the display the window is currently in, if multiple monitors are available. First is 1.</description></item>
    /// <item><term>minwidth</term><description>number: The minimum width of the window, if it's resizable.</description></item>
    /// <item><term>minheight</term><description>number: The minimum height of the window, if it's resizable.</description></item>
    /// </list>
    /// </description></item>
    /// </list>
    /// </returns>
    public (double width, double height, object flags) GetMode(double width, double height, object flags) =>
        throw new NotImplementedException();

    // TODO: Change to named tuples.
    internal void ClampPositionInWindow(ref double? wx, ref double? wy) 
     {
     	if (wx != null)
     		wx = Math.Min(Math.Max(0.0, wx.Value), (double) GetWidth() - 1);
     	if (wy != null)
     		wy = Math.Min(Math.Max(0.0, wy.Value), (double) GetHeight() - 1);
     }

    // TODO: Change to named tuples.
    internal void WindowToDPICoords(ref double? x, ref double? y)
    {

        double? px = x != null ? x : 0.0;
        double? py = y != null ? y : 0.0;

        WindowToPixelCoords(ref px, ref py);

        double dpix = 0.0;
        double dpiy = 0.0;

        (dpix, dpiy) = FromPixels(px!.Value, py!.Value);

        if (x != null)
            x = dpix;
        if (y != null)
            y = dpiy;
    }

    // TODO: Change to named tuples.
    private void WindowToPixelCoords(ref double? x,ref double? y) 
    {
        if (x != null)
            x = ( x)* ((double) pixelWidth / (double) windowWidth);
        if (y != null)
            y = ( y)* ((double) pixelHeight / (double) windowHeight);
    }
    
    /// <summary>
    /// <para>Gets the position of the window on the screen.</para>
    /// <para>The window position is in the coordinate space of the display it is currently in.</para>
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>x</term><description>The x-coordinate of the window's position.</description></item>
    /// <item><term>y</term><description>The y-coordinate of the window's position.</description></item>
    /// <item><term>displayindex</term><description>The index of the display that the window is in.</description></item>
    /// </list>
    /// </returns>
    public (int x, int y, int displayindex) GetPosition()
    {
        if (window == null)
         {
         	return (0,0,0);
         }

         SDL_DisplayID displayid = SDL_GetDisplayForWindow(window);
         SDLDisplayIDs displayids = new();
         int x, y;
         var displayindex = 0;
         for (int i = 0; i < displayids.count; i++)
         {
         	if (displayids.ids[i] == displayid)
         	{
         		displayindex = i;
         		break;
         	}
         }

         SDL_GetWindowPosition(window, &x, &y);

         // In SDL <= 2.0.3, fullscreen windows are always reported as 0,0. In every
         // other case we need to convert the position from global coordinates to the
         // monitor's coordinate space.
         if (x != 0 || y != 0)
         {
         	SDL_Rect displaybounds;
         	SDL_GetDisplayBounds(displayid, &displaybounds);

         	x -= displaybounds.x;
         	y -= displaybounds.y;
         }

         return (x, y, displayindex);
    }

    /// <summary>
    /// <para>Gets area inside the window which is known to be unobstructed by a system title bar, the iPhone X notch, etc. Useful for making sure UI elements can be seen by the user.</para>
    /// </summary>
    /// <param name="x">Starting position of safe area (x-axis).</param>
    /// <param name="y">Starting position of safe area (y-axis).</param>
    /// <param name="w">Width of safe area.</param>
    /// <param name="h">Height of safe area.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>x</term><description>Starting position of safe area (x-axis).</description></item>
    /// <item><term>y</term><description>Starting position of safe area (y-axis).</description></item>
    /// <item><term>w</term><description>Width of safe area.</description></item>
    /// <item><term>h</term><description>Height of safe area.</description></item>
    /// </list>
    /// </returns>
    public (double x, double y, double w, double h) GetSafeArea(double x, double y, double w, double h) =>
        throw new NotImplementedException();

    /// <summary>
    /// <para>Gets area inside the window which is known to be unobstructed by a system title bar, the iPhone X notch, etc. Useful for making sure UI elements can be seen by the user.</para>
    /// </summary>
    public void GetSafeArea() => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets the window title.</para>
    /// </summary>
    /// <param name="title">The current window title.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>title</term><description>The current window title.</description></item>
    /// </list>
    /// </returns>
    public string GetTitle(string title) => throw new NotImplementedException();

    /// <summary>
    ///     <para>Gets current vertical synchronization (vsync).</para>
    /// </summary>
    /// <returns>
    ///     Current vsync status. 1 if enabled, 0 if disabled, and -1 for adaptive vsync.
    /// </returns>
    public int GetVSync()
    {
        return 0;

        // TODO: How can this be done with SDL3 GPU API?
        //if (glcontext != nullptr)
        // 	{
        // 		int interval = 0;
        // 		SDL_GL_GetSwapInterval(&interval);
        // 		return interval;
        // 	}
        // 
        // #if defined(LOVE_GRAPHICS_METAL)
        // 	if (metalView != nullptr)
        // 	{
        // #ifdef LOVE_MACOS
        // 		void *metallayer = SDL_Metal_GetLayer(metalView);
        // 		return love::macos::getMetalLayerVSync(metallayer) ? 1 : 0;
        // #else
        // 		return 1;
        // #endif
        // 	}
        // #endif
        // 
        // #ifdef LOVE_GRAPHICS_VULKAN
        // 	if (windowRenderer == love::graphics::RENDERER_VULKAN)
        // 	{
        // 		auto vgfx = dynamic_cast<love::graphics::vulkan::Graphics*>(graphics.get());
        // 		return vgfx->getVsync();
        // 	}
        // #endif
        // 
        // 	return 0;
    }

    /// <summary>
    /// <para>Gets the width of the window.</para>
    /// </summary>
    /// <returns>The width of the window.
    /// </returns>
    public double GetWidth()
    {
        return windowWidth;
    }

    /// <summary>
    /// <para>Checks if the game window has keyboard focus.</para>
    /// </summary>
    /// <param name="focus">True if the window has the focus or false if not.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>focus</term><description>True if the window has the focus or false if not.</description></item>
    /// </list>
    /// </returns>
    public bool HasFocus(bool focus) => throw new NotImplementedException();

    /// <summary>
    /// <para>Checks if the game window has mouse focus.</para>
    /// </summary>
    /// <param name="focus">True if the window has mouse focus or false if not.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>focus</term><description>True if the window has mouse focus or false if not.</description></item>
    /// </list>
    /// </returns>
    public bool HasMouseFocus(bool focus) => throw new NotImplementedException();

    /// <summary>
    /// <para>Checks if the window has been created.</para>
    /// </summary>
    /// <param name="created">True if the window has been created, false otherwise.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>created</term><description>True if the window has been created, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool IsCreated(bool created) => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets whether the display is allowed to sleep while the program is running.</para>
    /// <para>Display sleep is disabled by default. Some types of input (e.g. joystick button presses) might not prevent the display from sleeping, if display sleep is allowed.</para>
    /// </summary>
    /// <param name="enabled">True if system display sleep is enabled / allowed, false otherwise.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>enabled</term><description>True if system display sleep is enabled / allowed, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool IsDisplaySleepEnabled(bool enabled) => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets whether the Window is currently maximized.</para>
    /// <para>The window can be maximized if it is not fullscreen and is resizable, and either the user has pressed the window's Maximize button or love.window.maximize has been called.</para>
    /// </summary>
    /// <param name="maximized">True if the window is currently maximized in windowed mode, false otherwise.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>maximized</term><description>True if the window is currently maximized in windowed mode, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool IsMaximized(bool maximized) => throw new NotImplementedException();

    /// <summary>
    /// <para>Makes the window as large as possible.</para>
    /// <para>This function has no effect if the window isn't resizable, since it essentially programmatically presses the window's "maximize" button.</para>
    /// </summary>
    public void Maximize() => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets whether the Window is currently minimized.</para>
    /// </summary>
    /// <param name="minimized">True if the window is currently minimized, false otherwise.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>minimized</term><description>True if the window is currently minimized, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool IsMinimized(bool minimized) => throw new NotImplementedException();

    /// <summary>
    /// <para>Minimizes the window to the system's task bar / dock.</para>
    /// </summary>
    public void Minimize() => throw new NotImplementedException();

    /// <summary>
    /// <para>Checks if the window is open.</para>
    /// </summary>
    /// <param name="open">True if the window is open, false otherwise.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>open</term><description>True if the window is open, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool IsOpen()
    {
        return open;
    }

    /// <summary>
    /// <para>Checks if the game window is visible.</para>
    /// <para>The window is considered visible if it's not minimized and the program isn't hidden.</para>
    /// </summary>
    /// <param name="visible">True if the window is visible or false if not.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>visible</term><description>True if the window is visible or false if not.</description></item>
    /// </list>
    /// </returns>
    public bool IsVisible(bool visible) => throw new NotImplementedException();

    /// <summary>
    /// <para>Causes the window to request the attention of the user if it is not in the foreground.</para>
    /// <para>In Windows the taskbar icon will flash, and in OS X the dock icon will bounce.</para>
    /// </summary>
    /// <param name="continuous">Whether to continuously request attention until the window becomes active, or to do it only once.</param>
    public void RequestAttention(bool continuous = false) => throw new NotImplementedException();

    /// <summary>
    /// <para>Restores the size and position of the window if it was minimized or maximized.</para>
    /// </summary>
    public void Restore() => throw new NotImplementedException();

    /// <summary>
    /// <para>Sets whether the display is allowed to sleep while the program is running.</para>
    /// <para>Display sleep is disabled by default. Some types of input (e.g. joystick button presses) might not prevent the display from sleeping, if display sleep is allowed.</para>
    /// </summary>
    /// <param name="enable">True to enable system display sleep, false to disable it.</param>
    public void SetDisplaySleepEnabled(bool enable)
    {
        if (enable)
            SDL_EnableScreenSaver();
        else
            SDL_DisableScreenSaver();
    }

    /// <summary>
    /// <para>Enters or exits fullscreen. The display to use when entering fullscreen is chosen based on which display the window is currently in, if multiple monitors are connected.</para>
    /// </summary>
    /// <param name="fullscreen">Whether to enter or exit fullscreen mode.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>True if an attempt to enter fullscreen was successful, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool SetFullscreen(bool fullscreen) => throw new NotImplementedException();

    /// <summary>
    /// <para>Enters or exits fullscreen. The display to use when entering fullscreen is chosen based on which display the window is currently in, if multiple monitors are connected.</para>
    /// </summary>
    /// <param name="fullscreen">Whether to enter or exit fullscreen mode.</param>
    /// <param name="fstype">The type of fullscreen mode to use.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>True if an attempt to enter fullscreen was successful, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool SetFullscreen(bool fullscreen, FullscreenType fstype) => throw new NotImplementedException();

    /// <summary>
    /// <para>Enters or exits fullscreen. The display to use when entering fullscreen is chosen based on which display the window is currently in, if multiple monitors are connected.</para>
    /// </summary>
    public void SetFullscreen() => throw new NotImplementedException();

    /// <summary>
    /// <para>Sets the window icon until the game is quit. Not all operating systems support very large icon images.</para>
    /// <para>Like t.window.icon in conf.lua, love.window.setIcon allows you to set the executable icon at runtime. Any fused executables will have the default LÖVE application icon in a file explorer or launcher (start menu, dock, etc). See Game Distribution for steps to change the icon.</para>
    /// </summary>
    /// <param name="imagedata">The window icon image.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>Whether the icon has been set successfully.</description></item>
    /// </list>
    /// </returns>
    public bool SetIcon(object imagedata) => throw new NotImplementedException();

    /// <summary>
    /// <para>Sets the display mode and properties of the window.</para>
    /// <para>If width or height is 0, setMode will use the width and height of the desktop.</para>
    /// <para>Changing the display mode may have side effects: for example, canvases will be cleared and values sent to shaders with Shader:send will be erased. Make sure to save the contents of canvases beforehand or re-draw to them afterward if you need to.</para>
    /// <para>Using this method will clear any existing properties if they're not set i.e. 'fullscreen', 'resizeable', 'usedpiscale' will revert to defaults. If you want to set the mode without resetting the window options already set, use love.window.updateMode instead.</para>
    /// </summary>
    /// <param name="width">Display width.</param>
    /// <param name="height">Display height.</param>
    /// <param name="flags">
    /// The flags table with the options:
    /// <list type="bullet">
    /// <item><term>fullscreen</term><description>boolean: Fullscreen (true), or windowed (false).</description></item>
    /// <item><term>fullscreentype</term><description>FullscreenType: The type of fullscreen to use. This defaults to "normal" in through and to "desktop" in and older.</description></item>
    /// <item><term>vsync</term><description>number: Enables or disables vertical synchronisation ('vsync'): to enable vsync, to disable it, and to use adaptive vsync (where supported). Prior to this was a boolean flag, rather than a number.</description></item>
    /// <item><term>msaa</term><description>number: The number of antialiasing samples.</description></item>
    /// <item><term>stencil</term><description>boolean: Whether a stencil buffer should be allocated. If true, the stencil buffer will have 8 bits.</description></item>
    /// <item><term>depth</term><description>number: The number of bits in the depth buffer.</description></item>
    /// <item><term>resizable</term><description>boolean: True if the window should be resizable in windowed mode, false otherwise.</description></item>
    /// <item><term>borderless</term><description>boolean: True if the window should be borderless in windowed mode, false otherwise.</description></item>
    /// <item><term>centered</term><description>boolean: True if the window should be centered in windowed mode, false otherwise.</description></item>
    /// <item><term>display</term><description>number: The index of the display to show the window in, if multiple monitors are available.</description></item>
    /// <item><term>minwidth</term><description>number: The minimum width of the window, if it's resizable. Cannot be less than 1.</description></item>
    /// <item><term>minheight</term><description>number: The minimum height of the window, if it's resizable. Cannot be less than 1.</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>True if successful, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool SetMode(int width, int height, WindowSettings flags)
    {
        // TODO: Move SetWindows method contents to here.
        
        return SetWindow(width, height, (WindowSettings)flags);
    }

    /// <summary>
    /// <para>Sets the display mode and properties of the window.</para>
    /// <para>If width or height is 0, setMode will use the width and height of the desktop.</para>
    /// <para>Changing the display mode may have side effects: for example, canvases will be cleared and values sent to shaders with Shader:send will be erased. Make sure to save the contents of canvases beforehand or re-draw to them afterward if you need to.</para>
    /// <para>Using this method will clear any existing properties if they're not set i.e. 'fullscreen', 'resizeable', 'usedpiscale' will revert to defaults. If you want to set the mode without resetting the window options already set, use love.window.updateMode instead.</para>
    /// </summary>
    public void SetMode() => throw new NotImplementedException();

    /// <summary>
    /// <para>Sets the position of the window on the screen.</para>
    /// <para>The window position is in the coordinate space of the specified display.</para>
    /// </summary>
    /// <param name="x">The x-coordinate of the window's position.</param>
    /// <param name="y">The y-coordinate of the window's position.</param>
    /// <param name="displayindex">The index of the display that the new window position is relative to.</param>
    public void SetPosition(int x, int y, int displayindex = 1)
    {
        // TODO: Verify this entire thing.
        if (window == null)
         	return;

         SDLDisplayIDs displayids = new();

         displayindex = Math.Min(Math.Max(displayindex, 0), displayids.count - 1);

         SDL_Rect displaybounds = new();
         SDL_GetDisplayBounds(displayids.ids[displayindex], &displaybounds);

         // The position needs to be in the global coordinate space.
         x += displaybounds.x;
         y += displaybounds.y;

         SDL_SetWindowPosition(window, x, y);
         SDL_SyncWindow(window);

         // TODO: settings.useposition = true;
    }

    private bool CreateWindowAndContext(int x, int y, int w, int h, SDL_WindowFlags windowflags)//, graphics::Renderer renderer)
     {
    // 	bool needsglcontext = (windowflags & SDL_WINDOW_OPENGL) != 0;
    // #ifdef LOVE_GRAPHICS_METAL
    // 	bool needsmetalview = (windowflags & SDL_WINDOW_METAL) != 0;
    // #endif
    // 
     	string windowerror;
    // 	std::string contexterror;
    // 	std::string glversion;
    // 
    // 	// Unfortunately some OpenGL context settings are part of the internal
    // 	// window state in the Windows and Linux SDL backends, so we have to
    // 	// recreate the window when we want to change those settings...
    // 	// Also, apparently some Intel drivers on Windows give back a Microsoft
    // 	// OpenGL 1.1 software renderer context when high MSAA values are requested!
    // 
    // 	const auto create = [&](const ContextAttribs *attribs) -> bool
    // 	{
    // 		if (glcontext)
    // 		{
    // 			SDL_GL_DestroyContext(glcontext);
    // 			glcontext = nullptr;
    // 		}
    // 
    // #ifdef LOVE_GRAPHICS_METAL
    // 		if (metalView)
    // 		{
    // 			SDL_Metal_DestroyView(metalView);
    // 			metalView = nullptr;
    // 		}
    // #endif
     
     		if (window != null)
     		{
     			SDL_DestroyWindow(window);
     			SDL_FlushEvents(SDL_EventType.SDL_EVENT_WINDOW_FIRST, SDL_EventType.SDL_EVENT_WINDOW_LAST);
     			window = null;
     		}
     
     		window = SDL_CreateWindow(_title, w, h, windowflags);
     
     		if (window == null)
     		{
     			windowerror = SDL_GetError();
     			return false;
     		}
     
     		SDL_SetWindowPosition(window, x, y);
     
    // 		if (attribs != nullptr && renderer == love::graphics::Renderer::RENDERER_OPENGL)
    // 		{
    // #ifdef LOVE_MACOS
    // 			love::macos::setWindowSRGBColorSpace(window);
    // #endif
    // 
    // 			glcontext = SDL_GL_CreateContext(window);
    // 
    // 			if (!glcontext)
    // 				contexterror = std::string(SDL_GetError());
    // 
    // 			// Make sure the context's version is at least what we requested.
    // 			if (glcontext && !checkGLVersion(*attribs, glversion))
    // 			{
    // 				SDL_GL_DestroyContext(glcontext);
    // 				glcontext = nullptr;
    // 			}
    // 
    // 			if (!glcontext)
    // 			{
    // 				SDL_DestroyWindow(window);
    // 				window = nullptr;
    // 				return false;
    // 			}
    // 		}
    // 
    // 		return true;
    // 	};
    // 
    // 	if (renderer == graphics::RENDERER_OPENGL)
    // 	{
    // 		std::vector<ContextAttribs> attribslist = getContextAttribsList();
    // 
    // 		// Try each context profile in order.
    // 		for (ContextAttribs attribs : attribslist)
    // 		{
    // 			bool curSRGB = love::graphics::isGammaCorrect();
    // 
    // 			setGLFramebufferAttributes(curSRGB);
    // 			setGLContextAttributes(attribs);
    // 
    // 			windowerror.clear();
    // 			contexterror.clear();
    // 
    // 			create(&attribs);
    // 
    // 			if (!window && curSRGB)
    // 			{
    // 				// The sRGB setting could have caused the failure.
    // 				setGLFramebufferAttributes(false);
    // 				if (create(&attribs))
    // 					curSRGB = false;
    // 			}
    // 
    // 			if (window && glcontext)
    // 			{
    // 				// Store the successful context attributes so we can re-use them in
    // 				// subsequent calls to createWindowAndContext.
    // 				contextAttribs = attribs;
    // 				love::graphics::setGammaCorrect(curSRGB);
    // 				break;
    // 			}
    // 		}
    // 	}
    // #ifdef LOVE_GRAPHICS_METAL
    // 	else if (renderer == graphics::RENDERER_METAL)
    // 	{
    // 		if (create(nullptr) && window != nullptr)
    // 			metalView = SDL_Metal_CreateView(window);
    // 
    // 		if (metalView == nullptr && window != nullptr)
    // 		{
    // 			contexterror = SDL_GetError();
    // 			SDL_DestroyWindow(window);
    // 			window = nullptr;
    // 		}
    // 	}
    // #endif
    // 	else
    // 	{
    // 		create(nullptr);
    // 	}
    // 
    	bool failed = window == null;
    // 	failed |= (needsglcontext && !glcontext);
    // #ifdef LOVE_GRAPHICS_METAL
    // 	failed |= (needsmetalview && !metalView);
    // #endif
    // 
     	if (failed)
     	{
     		string title = "Unable to create renderer";
     		string message = "This program requires a graphics card and video drivers which support OpenGL 3.3 or OpenGL ES 3.0.";
     
    // 		if (!glversion.empty())
    // 			message += "\n\nDetected OpenGL version:\n" + glversion;
    // 		else if (!contexterror.empty())
    // 			message += "\n\nRenderer context creation error: " + contexterror;
    // 		else if (!windowerror.empty())
    // 			message += "\n\nSDL window creation error: " + windowerror;
     
    // 		std::cerr << title << std::endl << message << std::endl;
     
     		// Display a message box with the error, but only once.
     		if (!displayedWindowError)
     		{
    // 			ShowMessageBox(title, message, MESSAGEBOX_ERROR, false);
     			displayedWindowError = true;
     		}
     
     		Close();
     		return false;
     	}
     
     	open = true;
     	return true;
     }

    class SDLDisplayIDs
    {
        public SDLDisplayIDs()
        {
            int count = 0;
            ids = SDL_GetDisplays(&count);
            this.count = count;
        }

        ~SDLDisplayIDs()
        {
            if (ids != null)
                SDL_free(ids);
        }

        public int count = 0;
        public SDL_DisplayID* ids = null;
    };

    private string _title;

    /// <summary>
    /// <para>Sets the window title.</para>
    /// </summary>
    /// <param name="title">The new window title.</param>
    public void SetTitle(string title)
    {
        _title = title;

        if (window != null)
            SDL_SetWindowTitle(window, title);
    }

    /// <summary>
    /// <para>Sets vertical synchronization mode.</para>
    /// </summary>
    /// <param name="vsync">VSync number: 1 to enable, 0 to disable, -1 for adaptive vsync, 2 or larger will wait that many frames before syncing.</param>
    public void SetVSync(double vsync)
    {
        // TODO: Implement.
    }

    /// <summary>
    /// <para>Displays a message box dialog above the love window. The message box contains a title, optional text, and buttons.</para>
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The text inside the message box.</param>
    /// <param name="type">The type of the message box.</param>
    /// <param name="attachtowindow">Whether the message box should be attached to the love window or free-floating.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>Whether the message box was successfully displayed.</description></item>
    /// </list>
    /// </returns>
    public bool ShowMessageBox
    (
        string title,
        string message,
        MessageBoxType type = MessageBoxType.Info,
        bool attachtowindow = true
    ) => throw new NotImplementedException();

    /// <summary>
    /// <para>Displays a message box dialog above the love window. The message box contains a title, optional text, and buttons.</para>
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The text inside the message box.</param>
    /// <param name="buttonlist">A table containing a list of button names to show. The table can also contain the fields and , which should be the index of the default button to use when the user presses 'enter' or 'escape', respectively.</param>
    /// <param name="type">The type of the message box.</param>
    /// <param name="attachtowindow">Whether the message box should be attached to the love window or free-floating.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>pressedbutton</term><description>The index of the button pressed by the user. May be 0 if the message box dialog was closed without pressing a button.</description></item>
    /// </list>
    /// </returns>
    public double ShowMessageBox
    (
        string title,
        string message,
        object buttonlist,
        MessageBoxType type = MessageBoxType.Info,
        bool attachtowindow = true
    ) => throw new NotImplementedException();

    /// <summary>
    /// <para>Displays a message box dialog above the love window. The message box contains a title, optional text, and buttons.</para>
    /// </summary>
    public void IsSupported() => throw new NotImplementedException();

    /// <summary>
    /// <para>Converts a number from density-independent units to pixels.</para>
    /// <para>The pixel density inside the window might be greater (or smaller) than the "size" of the window. For example on a retina screen in Mac OS X with the highdpi window flag enabled, the window may take up the same physical size as an 800x600 window, but the area inside the window uses 1600x1200 pixels. love.window.toPixels(800) would return 1600 in that case.</para>
    /// <para>This is used to convert coordinates from the size users are expecting them to display at onscreen to pixels. love.window.fromPixels does the opposite. The highdpi window flag must be enabled to use the full pixel density of a Retina screen on Mac OS X and iOS. The flag currently does nothing on Windows and Linux, and on Android it is effectively always enabled.</para>
    /// <para>Most LÖVE functions return values and expect arguments in terms of pixels rather than density-independent units.</para>
    /// </summary>
    /// <param name="value">A number in density-independent units to convert to pixels.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>pixelvalue</term><description>The converted number, in pixels.</description></item>
    /// </list>
    /// </returns>
    public double ToPixels(double value) => throw new NotImplementedException();

    /// <summary>
    /// <para>Converts a number from density-independent units to pixels.</para>
    /// <para>The pixel density inside the window might be greater (or smaller) than the "size" of the window. For example on a retina screen in Mac OS X with the highdpi window flag enabled, the window may take up the same physical size as an 800x600 window, but the area inside the window uses 1600x1200 pixels. love.window.toPixels(800) would return 1600 in that case.</para>
    /// <para>This is used to convert coordinates from the size users are expecting them to display at onscreen to pixels. love.window.fromPixels does the opposite. The highdpi window flag must be enabled to use the full pixel density of a Retina screen on Mac OS X and iOS. The flag currently does nothing on Windows and Linux, and on Android it is effectively always enabled.</para>
    /// <para>Most LÖVE functions return values and expect arguments in terms of pixels rather than density-independent units.</para>
    /// </summary>
    /// <param name="x">The x-axis value of a coordinate in density-independent units to convert to pixels.</param>
    /// <param name="y">The y-axis value of a coordinate in density-independent units to convert to pixels.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>px</term><description>The converted x-axis value of the coordinate, in pixels.</description></item>
    /// <item><term>py</term><description>The converted y-axis value of the coordinate, in pixels.</description></item>
    /// </list>
    /// </returns>
    public (double px, double py) ToPixels(double x, double y) => throw new NotImplementedException();

    /// <summary>
    /// <para>Sets the display mode and properties of the window, without modifying unspecified properties.</para>
    /// <para>If width or height is 0, updateMode will use the width and height of the desktop.</para>
    /// <para>Changing the display mode may have side effects: for example, canvases will be cleared. Make sure to save the contents of canvases beforehand or re-draw to them afterward if you need to.</para>
    /// </summary>
    /// <param name="width">Window width.</param>
    /// <param name="height">Window height.</param>
    /// <param name="settings">
    /// The settings table with the following optional fields. Any field not filled in will use the current value that would be returned by .
    /// <list type="bullet">
    /// <item><term>fullscreen</term><description>boolean: Fullscreen (true), or windowed (false).</description></item>
    /// <item><term>fullscreentype</term><description>FullscreenType: The type of fullscreen to use.</description></item>
    /// <item><term>vsync</term><description>boolean: True if LÖVE should wait for vsync, false otherwise.</description></item>
    /// <item><term>msaa</term><description>number: The number of antialiasing samples.</description></item>
    /// <item><term>resizable</term><description>boolean: True if the window should be resizable in windowed mode, false otherwise.</description></item>
    /// <item><term>borderless</term><description>boolean: True if the window should be borderless in windowed mode, false otherwise.</description></item>
    /// <item><term>centered</term><description>boolean: True if the window should be centered in windowed mode, false otherwise.</description></item>
    /// <item><term>display</term><description>number: The index of the display to show the window in, if multiple monitors are available.</description></item>
    /// <item><term>minwidth</term><description>number: The minimum width of the window, if it's resizable. Cannot be less than 1.</description></item>
    /// <item><term>minheight</term><description>number: The minimum height of the window, if it's resizable. Cannot be less than 1.</description></item>
    /// <item><term>highdpi</term><description>boolean: True if should be used on Retina displays in macOS and iOS. Does nothing on non-Retina displays.</description></item>
    /// <item><term>x</term><description>number: The x-coordinate of the window's position in the specified display.</description></item>
    /// <item><term>y</term><description>number: The y-coordinate of the window's position in the specified display.</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>True if successful, false otherwise.</description></item>
    /// </list>
    /// </returns>
    public bool UpdateMode(double width, double height, object settings) => throw new NotImplementedException();

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