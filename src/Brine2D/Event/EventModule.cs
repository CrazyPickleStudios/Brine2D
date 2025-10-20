using Brine2D.Event.Messages;
using Brine2D.Event.Messages.Keyboard;
using Brine2D.Event.Messages.Mouse;
using Brine2D.Event.Messages.Touch;
using Brine2D.Event.Messages.Window;
using Brine2D.Keyboard;
using Brine2D.Window;
using Brine2D.Joystick;
using SDL;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using static SDL.SDL3;
using static System.Net.Mime.MediaTypeNames;

namespace Brine2D.Event;

/// <summary>
///     <para>Manages events, like keypresses.</para>
///     <para>
///         It is possible to define new events by appending the table love.handlers. Such functions can be invoked as
///         usual, via love.event.push using the table index as an argument.
///     </para>
/// </summary>
public sealed class EventModule : Module
{
    internal EventModule()
    {
        if (!SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_EVENTS))
            throw new Exception($"Could not initialize SDL events subsystem ({SDL_GetError()})");

        // TODO: SDL_AddEventWatch(watchAppEvents, this);
    }

    protected internal override void Dispose()
    {
        // TODO: SDL_RemoveEventWatch(watchAppEvents, this);
        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_EVENTS);

        base.Dispose();
    }

    //static bool SDLCALL watchAppEvents(void *udata, SDL_Event *event)
    // {
    // 	auto eventModule = (Event *)udata;
    // 
    // 	switch (event->type)
    // 	{
    // 	case SDL_EVENT_DID_ENTER_BACKGROUND:
    // 	case SDL_EVENT_WILL_ENTER_FOREGROUND:
    // 		// On iOS, calling any OpenGL ES function after the function which triggers
    // 		// SDL_APP_DIDENTERBACKGROUND is called will kill the app, so we handle it
    // 		// with an event watch callback, which will be called inside that function.
    // 		{
    // 			auto gfx = Module::getInstance<graphics::Graphics>(Module::M_GRAPHICS);
    // 			if (gfx && SDL_IsMainThread())
    // 				gfx->setActive(event->type == SDL_EVENT_WILL_ENTER_FOREGROUND);
    // 		}
    // 		break;
    // 	case SDL_EVENT_WINDOW_EXPOSED:
    // 		// Only redraw during live-resize events (data1 is 1 in that situation).
    // 		if (event->window.data1 == 1 && eventModule != nullptr && SDL_IsMainThread() && eventModule->allowModalDraws())
    // 			eventModule->modalDraw();
    // 		break;
    // 	default:
    // 		break;
    // 	}
    // 
    // 	return true;
    // }

    private Lock _mutex = new();

    private Queue<Message> _queue = new();

    /// <summary>
    ///     Clears the event queue.
    /// </summary>
    public unsafe void Clear()
    {
        ExceptionIfInRenderPass();

        SDL_Event e;

        while (SDL_PollEvent(&e))
        {
            // Do nothing with 'e' ...
        }

        using (_mutex.EnterScope())
        {
            _queue.Clear();
        }
    }

    /// <summary>
    ///     Returns an iterator for messages in the event queue.
    /// </summary>
    /// <returns>
    ///     Iterator function usable in a for loop.
    /// </returns>
    /// TODO: This needs better documentation.
    public IEnumerable<Message> Poll()
    {
        while (true)
        {
            Message args;

            using (_mutex.EnterScope())
            {
                if (!_queue.TryDequeue(out args!))
                {
                    yield break;
                }
            }

            yield return args;
        }
    }
    
    bool insideEventPump = false;

    private Message? Convert( SDL_Event e)
     {
     	Message? msg = null;
    // 
    // 	std::vector<Variant> vargs;
    // 	vargs.reserve(4);
    // 
    // 	love::filesystem::Filesystem *filesystem = nullptr;
    // 	love::sensor::Sensor *sensorInstance = nullptr;
    var win = GetInstance<WindowModule>();
     
     	Key key = Key.KEY_UNKNOWN;
     	Scancode scancode = Scancode.SCANCODE_UNKNOWN;
     
     	string? txt;
    // 	const char *txt2;
    // 
    // 	love::touch::sdl::Touch *touchmodule = nullptr;
    // 	love::touch::Touch::TouchInfo touchinfo = {};
    
    // TODO: ???????
    // 	if (win != null)
    //	{
    // 		// Dubious cast, but it's not like having an SDL event backend
    // 		// with a non-SDL window backend will be a thing.
    // 		auto sdlwin = dynamic_cast<love::window::sdl::Window *>(win);
    // 		if (sdlwin != nullptr)
    // 			sdlwin->handleSDLEvent(e);
    // 	}
     
     	switch (e.Type)
     	{
            case SDL_EventType.SDL_EVENT_KEY_DOWN:
                if (e.key.repeat)
                {
                    var kb = GetInstance<KeyboardModule>();

                    if (kb != null && !kb.HasKeyRepeat())
                        break;
                }

                msg = KeyPressedMessage.FromSDL(e);

                break;
            case SDL_EventType.SDL_EVENT_KEY_UP:
                msg = KeyReleasedMessage.FromSDL(e);
                break;
            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                msg = TextInputMessage.FromSDL(e);
                break;
            case SDL_EventType.SDL_EVENT_TEXT_EDITING:
                msg = TextEditedMessage.FromSDL(e);
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                msg = MouseMovedMessage.FromSDL(e);
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                msg = MousePressedMessage.FromSDL(e);
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                msg = MouseReleasedMessage.FromSDL(e);
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                msg = WheelMovedMessage.FromSDL(e);
                break;
             	case SDL_EventType.SDL_EVENT_FINGER_DOWN:
             	case SDL_EventType.SDL_EVENT_FINGER_UP:
             	case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                msg = TouchMessage.FromSDL(e);
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
            case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
            case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION:
            case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
            case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
            case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
            case SDL_EventType.SDL_EVENT_GAMEPAD_SENSOR_UPDATE:
                msg = ConvertJoystickEvent(e);
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
            case SDL_EventType.SDL_EVENT_WINDOW_SHOWN:
            case SDL_EventType.SDL_EVENT_WINDOW_HIDDEN:
            case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
            case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
            case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
            case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
            case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
            case SDL_EventType.SDL_EVENT_WINDOW_OCCLUDED:
                msg = ConvertWindowEvent(e, win);
                break;
            case SDL_EventType.SDL_EVENT_DISPLAY_ORIENTATION:
                msg = DisplayRotatedMessage.FromSDL(e);
                break;
            // 	case SDL_EventType.SDL_EVENT_DROP_BEGIN:
            // 		msg = new Message("dropbegan", vargs);
            // 		break;
            // 	case SDL_EventType.SDL_EVENT_DROP_COMPLETE:
            // 		{
            // 			double x = e.drop.x;
            // 			double y = e.drop.y;
            // 			windowToDPICoords(win, &x, &y);
            // 			vargs.emplace_back(x);
            // 			vargs.emplace_back(y);
            // 			msg = new Message("dropcompleted", vargs);
            // 		}
            // 		break;
            // 	case SDL_EVENT_DROP_POSITION:
            // 		{
            // 			double x = e.drop.x;
            // 			double y = e.drop.y;
            // 			windowToDPICoords(win, &x, &y);
            // 			vargs.emplace_back(x);
            // 			vargs.emplace_back(y);
            // 			msg = new Message("dropmoved", vargs);
            // 		}
            // 		break;
            //case SDL_EventType.SDL_EVENT_DROP_FILE:
            //    filesystem = Module::getInstance<filesystem::Filesystem>(Module::M_FILESYSTEM);
            //    if (filesystem != nullptr)
            //    {
            //        const char* filepath = e.drop.data;
            //        // Allow mounting any dropped path, so zips or dirs can be mounted.
            //        filesystem->allowMountingForPath(filepath);

            //        double x = e.drop.x;
            //        double y = e.drop.y;
            //        windowToDPICoords(win, &x, &y);

            //        if (filesystem->isRealDirectory(filepath))
            //        {
            //            vargs.emplace_back(filepath, strlen(filepath));
            //            vargs.emplace_back(x);
            //            vargs.emplace_back(y);
            //            msg = new Message("directorydropped", vargs);
            //        }
            //        else
            //        {
            //            auto* file = filesystem->openNativeFile(filepath, love::filesystem::File::MODE_CLOSED);
            //            vargs.emplace_back(&love::filesystem::File::type, file);
            //            vargs.emplace_back(x);
            //            vargs.emplace_back(y);
            //            msg = new Message("filedropped", vargs);
            //            file->release();
            //        }
            //    }
            //    break;
            case SDL_EventType.SDL_EVENT_QUIT:
             	case SDL_EventType.SDL_EVENT_TERMINATING:
                    // TODO: Is '0' correct?
             		msg = new QuitMessage(0);
             		break;
            case SDL_EventType.SDL_EVENT_LOW_MEMORY:
                msg = new LowMemoryMessage();
                break;
            case SDL_EventType.SDL_EVENT_LOCALE_CHANGED:
                msg = new LocaleChangedMessage();
                break;
            // 	case SDL_EventType.SDL_EVENT_SENSOR_UPDATE:
            // 		sensorInstance = Module::getInstance<sensor::Sensor>(M_SENSOR);
            // 		if (sensorInstance)
            // 		{
            // 			std::vector<void*> sensors = sensorInstance->getHandles();
            // 
            // 			for (void *s: sensors)
            // 			{
            // 				SDL_Sensor *sensor = (SDL_Sensor *) s;
            // 				SDL_SensorID id = SDL_GetSensorID(sensor);
            // 
            // 				if (e.sensor.which == id)
            // 				{
            // 					// Found sensor
            // 					const char *sensorType;
            // 					auto sdltype = SDL_GetSensorType(sensor);
            // 					if (!sensor::Sensor::getConstant(sensor::sdl::Sensor::convert(sdltype), sensorType))
            // 						sensorType = "unknown";
            // 
            // 					vargs.emplace_back(sensorType, strlen(sensorType));
            // 					// Both accelerometer and gyroscope only pass up to 3 values.
            // 					// https://github.com/libsdl-org/SDL/blob/SDL2/include/SDL_sensor.h#L81-L127
            // 					vargs.emplace_back(e.sensor.data[0]);
            // 					vargs.emplace_back(e.sensor.data[1]);
            // 					vargs.emplace_back(e.sensor.data[2]);
            // 					msg = new Message("sensorupdated", vargs);
            // 
            // 					break;
            // 				}
            // 			}
            // 		}
            // 		break;
            default:
     		break;
     	}
     
     	return msg;
     }

    private Message? ConvertJoystickEvent(SDL_Event e)
    {

        var joymodule = GetInstance<JoystickModule>();

        if (joymodule == null)
            return null;

        Message? msg = null;

//        std::vector<Variant> vargs;
//        vargs.reserve(4);

//        love::Type* joysticktype = &love::joystick::Joystick::type;
//        love::joystick::Joystick* stick = nullptr;
//        love::joystick::Joystick::Hat hat;
//        love::joystick::Joystick::GamepadButton padbutton;
//        love::joystick::Joystick::GamepadAxis padaxis;
//        const char* txt;

//        switch (e.Type)
//        {
//            case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
//            case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
//                stick = joymodule->getJoystickFromID(e.jbutton.which);
//                if (!stick)
//                    break;

//                vargs.emplace_back(joysticktype, stick);
//                vargs.emplace_back((double)(e.jbutton.button + 1));
//                msg = new Message((e.type == SDL_EVENT_JOYSTICK_BUTTON_DOWN) ? "joystickpressed" : "joystickreleased",
//                    vargs);
//                break;
//            case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
//            {
//                stick = joymodule->getJoystickFromID(e.jaxis.which);
//                if (!stick)
//                    break;

//                vargs.emplace_back(joysticktype, stick);
//                vargs.emplace_back((double)(e.jaxis.axis + 1));
//                float value = joystick::Joystick::clampval(e.jaxis.value / 32768.0f);
//                vargs.emplace_back((double)value);
//                msg = new Message("joystickaxis", vargs);
//            }
//                break;
//            case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION:
//                if (!joystick::sdl::Joystick::getConstant(e.jhat.value, hat) ||
//                    !joystick::Joystick::getConstant(hat, txt))
//                    break;

//                stick = joymodule->getJoystickFromID(e.jhat.which);
//                if (!stick)
//                    break;

//                vargs.emplace_back(joysticktype, stick);
//                vargs.emplace_back((double)(e.jhat.hat + 1));
//                vargs.emplace_back(txt, strlen(txt));
//                msg = new Message("joystickhat", vargs);
//                break;
//            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
//            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
//            {
//                const auto  &b = e.gbutton;
//                if (!joystick::sdl::Joystick::getConstant((SDL_GamepadButton)b.button, padbutton))
//                    break;

//                if (!joystick::Joystick::getConstant(padbutton, txt))
//                    break;

//                stick = joymodule->getJoystickFromID(b.which);
//                if (!stick)
//                    break;

//                vargs.emplace_back(joysticktype, stick);
//                vargs.emplace_back(txt, strlen(txt));
//                msg = new Message(
//                    e.Type == SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN ? "gamepadpressed" : "gamepadreleased",
//                    vargs);
//            }
//                break;

//            case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
//                if (joystick::sdl::Joystick::getConstant((SDL_GamepadAxis)e.gaxis.axis, padaxis))
//                {
//                    if (!joystick::Joystick::getConstant(padaxis, txt))
//                        break;

//                    const auto  &a = e.gaxis;
//                    stick = joymodule->getJoystickFromID(a.which);
//                    if (!stick)
//                        break;

//                    vargs.emplace_back(joysticktype, stick);
//                    vargs.emplace_back(txt, strlen(txt));
//                    float value = joystick::Joystick::clampval(a.value / 32768.0f);
//                    vargs.emplace_back((double)value);
//                    msg = new Message("gamepadaxis", vargs);
//                }

//                break;
//            case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
//                // jdevice.which is the joystick device index.
//                stick = joymodule->addJoystick(e.jdevice.which);
//                if (stick)
//                {
//                    vargs.emplace_back(joysticktype, stick);
//                    msg = new Message("joystickadded", vargs);
//                }

//                break;
//            case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
//                // jdevice.which is the joystick instance ID now.
//                stick = joymodule->getJoystickFromID(e.jdevice.which);
//                if (stick)
//                {
//                    joymodule->removeJoystick(stick);
//                    vargs.emplace_back(joysticktype, stick);
//                    msg = new Message("joystickremoved", vargs);
//                }

//                break;
//#if LOVE_ENABLE_SENSOR
//     	case SDL_EVENT_GAMEPAD_SENSOR_UPDATE:
//     		{
//     			const auto &sens = e.gsensor;
//     			stick = joymodule->getJoystickFromID(sens.which);
//     			if (stick)
//     			{
//     				using Sensor = love::sensor::Sensor;
     
//     				const char *sensorName;
//     				Sensor::SensorType sensorType = love::sensor::sdl::Sensor::convert((SDL_SensorType) sens.sensor);
//     				if (!Sensor::getConstant(sensorType, sensorName))
//     					sensorName = "unknown";
     
//     				vargs.emplace_back(joysticktype, stick);
//     				vargs.emplace_back(sensorName, strlen(sensorName));
//     				vargs.emplace_back(sens.data[0]);
//     				vargs.emplace_back(sens.data[1]);
//     				vargs.emplace_back(sens.data[2]);
//     				msg = new Message("joysticksensorupdated", vargs);
//     			}
//     		}
//     		break;
//#endif // defined(LOVE_ENABLE_SENSOR)
//            default:
//                break;
//        }

        return msg;
    }

    private Message ConvertWindowEvent(SDL_Event e, WindowModule win)
     {

         Message? msg = null;

//    std::vector<Variant> vargs;
//    vargs.reserve(4);
     
//     	graphics::Graphics* gfx = nullptr;

    var @event = e.Type;
     
     	switch (@event)

         {

         case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
        case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                    msg = FocusMessage.FromSDL(e);
                     		break;
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                            msg = MouseFocusMessage.FromSDL(e);
                     		break;
                     	//case SDL_EventType.SDL_EVENT_WINDOW_SHOWN:
                     	//case SDL_EventType.SDL_EVENT_WINDOW_HIDDEN:
                     	//case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
                     	//case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
                     #if LOVE_ANDROID
                     		if (auto audio = Module::getInstance<audio::Audio>(Module::M_AUDIO))
                     		{
                     			if (event == SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED)
                     				audio->pauseContext();
                     			else if (event == SDL_EventType.SDL_EVENT_WINDOW_RESTORED)
                     				audio->resumeContext();
                     		}
                #endif
                //// WINDOW_RESTORED can also happen when going from maximized -> unmaximized,
                //// but there isn't a nice way to avoid sending our event in that situation.
                //vargs.emplace_back(event == SDL_EventType.SDL_EVENT_WINDOW_SHOWN || event == SDL_EventType.SDL_EVENT_WINDOW_RESTORED);
                //msg = new Message("visible", vargs);
                //     		break;
                //     	case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
                //     		msg = new Message("exposed");
                //     		break;
                //     	case SDL_EventType.SDL_EVENT_WINDOW_OCCLUDED:
                //     		msg = new Message("occluded");
                //     		break;
                //     	case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
                //     		{
                //     			double width = e.window.data1;
                //double height = e.window.data2;

                // TODO: Implement
                //gfx = Module::getInstance<graphics::Graphics>(Module::M_GRAPHICS);
                //     			if (win)
                //                     win->onSizeChanged(e.window.data1, e.window.data2);
                //     			// The size values in the Window aren't necessarily the same as the
                //     			// graphics size, which is what we want to output.
                //     			if (gfx)
                //     			{
                //     				width = gfx->getWidth();
                //height = gfx->getHeight();
                //     			}
                //                 else if (win)
                //{
                //    width = win->getWidth();
                //    height = win->getHeight();
                //    windowToDPICoords(win, &width, &height);
                //}

                //vargs.emplace_back(width);
                //vargs.emplace_back(height);
                //msg = new Message("resize", vargs);
                //     		}
                //     		break;
        }

        return msg;
     }

    private string? deferredExceptionMessage;

    /// <summary>
    ///     <para>Pump events into the event queue.</para>
    ///     <para>This is a low-level function, and is usually not called by the user, but by love.run.</para>
    ///     <para>
    ///         Note that this does need to be called for any OS to think your program is still running, and if you want to
    ///         handle OS-generated events at all (think callbacks).
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     love.event.pump can only be called from the main thread, but afterwards, the rest of love.event can be used
    ///     from any other thread.
    /// </remarks>
    public unsafe void Pump()
    {
        ExceptionIfInRenderPass();
        const int LOVE_INT32_MAX = 0x7FFFFFFF;
        var waitTimeout = 0.0f;
        bool shouldPoll = false;

         if (insideEventPump)
         {
         	// Don't pump if we're inside the event pump already, but do allow
         	// polling what's in the SDL queue.
         	shouldPoll = true;
         }
         else
         {
         	int waitTimeoutMS = 0;
         	if (float.IsInfinity(waitTimeout) || waitTimeout < 0.0f)
         		waitTimeoutMS = -1; // Wait forever.
         	else if (waitTimeout > 0.0f)
         		waitTimeoutMS = (int)SysMath.Min(LOVE_INT32_MAX, 1000L * waitTimeout);

         	// Wait for the first event, if requested. WaitEvent also calls PumpEvents.
         	SDL_Event e;
         	insideEventPump = true;
         	bool success = false;
         	try
         	{
         		success = SDL_WaitEventTimeout(&e, waitTimeoutMS);
         	}
         	catch (Exception)
         	{
         		insideEventPump = false;
         		throw;
         	}
         	insideEventPump = false;

         	if (success)
            {
                var msg = Convert(e);

                 if (msg != null)
         			Push(msg);

         		// Fetch any extra events that came in during WaitEvent.
         		shouldPoll = true;
         	}

         	// For exceptions generated inside a modal draw callback, propagate them
         	// outside of OS event processing instead of inside.
         	if (!string.IsNullOrEmpty(deferredExceptionMessage))
         	{
         		var exceptionstr = deferredExceptionMessage;
         		deferredExceptionMessage = null;
                // TODO: deferredReturnValues[0] = Variant();
                // TODO: deferredReturnValues[1] = Variant();
                throw new Exception(exceptionstr);
         	}

            // TODO: Implement
        //	if (deferredReturnValues[0].getType() != Variant::NIL)
        // 	{
        // 		// Third arg being true will tell love.run to skip the love.quit callback,
        // 		// since the original modal draw function already processed that.
        // 		std::vector<Variant> args = {deferredReturnValues[0], deferredReturnValues[1], Variant(true)};

        // 		StrongRef<Message> msg(new Message("quit", args), Acquire::NORETAIN);

        // 		// Push to the front of queue so it's dealt with before any other event.
        // 		push(msg, true);

        // 		deferredReturnValues[0] = Variant();
        // 		deferredReturnValues[1] = Variant();
        // 	}
         }

         if (shouldPoll)
         {
         	SDL_Event e;
         	while (SDL_PeepEvents(&e, 1, SDL_EventAction.SDL_GETEVENT, (int)SDL_EventType.SDL_EVENT_FIRST, (int)SDL_EventType.SDL_EVENT_LAST) > 0)
            {
                var msg = Convert(e);
         		if (msg != null)
         			Push(msg);
         	}
         }
    }

    private void ExceptionIfInRenderPass([CallerMemberName] string? name = null)
    {
        // Some core OS graphics functionality (e.g. swap buffers on some platforms)
        // happens inside SDL_PumpEvents - which is called by SDL_PollEvent and
        // friends. It's probably a bad idea to call those functions while a RT
        // is active.
        // TODO: auto gfx = Module::getInstance<graphics::Graphics>(Module::M_GRAPHICS);
        // 	if (gfx != nullptr && gfx->isRenderTargetActive())
        // 		throw love::Exception("%s cannot be called while a render target is active in love.graphics.", name);
    }

    /// <summary>
    ///     <para>Adds an event to the event queue.</para>
    ///     <para>See Variant for the list of supported types for the arguments.</para>
    ///     <para>
    ///         From 0.10.0 onwards, you may pass an arbitrary amount of arguments with this function, though the default
    ///         callbacks don't ever use more than six.
    ///     </para>
    /// </summary>
    /// <param name="message">The event message.</param>
    public void Push(Message message)
    {
        Push(message, false);
    }

    private void Push(Message args, bool front)
    {
        using (_mutex.EnterScope())
        {
            if (front)
            {
                var newQueue = new Queue<Message>();
                newQueue.Enqueue(args);
                
                while (_queue.Count > 0)
                    newQueue.Enqueue(_queue.Dequeue());
                
                _queue = newQueue;
            }
            else
            {
                _queue.Enqueue(args);
            }
        }
    }

    /// <summary>
    ///     <para>Adds the quit event to the queue.</para>
    ///     <para>
    ///         The quit event is a signal for the event handler to close LÖVE. It's possible to abort the exit process with
    ///         the love.quit callback.
    ///     </para>
    ///     <para>Equivalent to love.event.push("quit", exitstatus)</para>
    /// </summary>
    /// <param name="exitStatus">The program exit status to use when closing the application.</param>
    /// <remarks>
    ///     On iOS, programmatically exiting the app (this includes call to os.exit) is not recommended and may result
    ///     your app being rejected from App Store. Thus, calling this variant will actually perform restart (see below).
    /// </remarks>
    public void Quit(int exitStatus = 0)
    {
        Push(new QuitMessage(exitStatus));
    }

    /// <summary>
    ///     Restarts the game without relaunching the executable. This cleanly shuts down the main Lua state instance and
    ///     creates a brand new one.
    /// </summary>
    /// <param name="restart">Tells the default love.run to exit and restart the game without relaunching the executable</param>
    /// TODO: Not really sure what this signature should look like.
    public void Quit(string restart = "restart")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Like love.event.poll(), but blocks until there is an event in the queue.
    /// </summary>
    /// <returns>The event message.</returns>
    public unsafe Message? Wait()
    {
        ExceptionIfInRenderPass();

        SDL_Event e;

        if (!SDL_WaitEvent(&e))
            return null;

        return Convert(e);
    }
}