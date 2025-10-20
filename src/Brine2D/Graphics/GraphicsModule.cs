using Brine2D.Window;
using System;

namespace Brine2D.Graphics;

//// TODO: Needs review
public sealed class GraphicsModule : Module
{
    internal GraphicsModule()
    {
        //: love::graphics::Graphics("love.graphics.opengl")
        // 	, windowHasStencil(false)
        // 	, mainVAO(0)
        // 	, internalBackbufferFBO(0)
        // 	, requestedBackbufferMSAA(0)
        // 	, bufferMapMemory(nullptr)
        // 	, bufferMapMemorySize(2 * 1024 * 1024)
        // 	, pixelFormatUsage()
        // {
        // 	gl = OpenGL();
        // 
        // 	try
        // 	{
        // 		bufferMapMemory = new char[bufferMapMemorySize];
        // 	}
        // 	catch (std::exception &)
        // 	{
        // 		// Handled in getBufferMapMemory.
        // 	}
        // 
        var window = GetInstance<WindowModule>();

        if (window != null)
        {
             		window.SetGraphics(this);
            
            if (window.IsOpen())
            {
                 			int w, h;
                 			WindowModule.WindowSettings settings;
                 			window.GetWindow(out w,out  h, out settings);
                 			window.SetWindow(w, h, settings);
            }
        }
    }

///// <summary>
//        /// <para>Draws a filled or unfilled arc at position (x, y). The arc is drawn from angle1 to angle2 in radians. The segments parameter determines how many segments are used to draw the arc. The more segments, the smoother the edge.</para>
//        /// </summary>
//        /// <param name="drawmode">How to draw the arc.</param>
//        /// <param name="x">The position of the center along x-axis.</param>
//        /// <param name="y">The position of the center along y-axis.</param>
//        /// <param name="radius">Radius of the arc.</param>
//        /// <param name="angle1">The angle at which the arc begins.</param>
//        /// <param name="angle2">The angle at which the arc terminates.</param>
//        /// <param name="segments">The number of segments used for drawing the arc.</param>
//    public void Arc(DrawMode drawmode, double x, double y, double radius, double angle1, double angle2, double segments = 10) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a filled or unfilled arc at position (x, y). The arc is drawn from angle1 to angle2 in radians. The segments parameter determines how many segments are used to draw the arc. The more segments, the smoother the edge.</para>
//        /// </summary>
//        /// <param name="drawmode">How to draw the arc.</param>
//        /// <param name="arctype">The type of arc to draw.</param>
//        /// <param name="x">The position of the center along x-axis.</param>
//        /// <param name="y">The position of the center along y-axis.</param>
//        /// <param name="radius">Radius of the arc.</param>
//        /// <param name="angle1">The angle at which the arc begins.</param>
//        /// <param name="angle2">The angle at which the arc terminates.</param>
//        /// <param name="segments">The number of segments used for drawing the arc.</param>
//    public void Arc(DrawMode drawmode, ArcType arctype, double x, double y, double radius, double angle1, double angle2, double segments = 10) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a circle.</para>
//        /// </summary>
//        /// <param name="mode">How to draw the circle. Has 2 modes: "fill" and "line".</param>
//        /// <param name="x">The position of the center along x-axis.</param>
//        /// <param name="y">The position of the center along y-axis.</param>
//        /// <param name="radius">The radius of the circle.</param>
//    public void Circle(DrawMode mode, double x, double y, double radius) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a circle.</para>
//        /// </summary>
//        /// <param name="mode">How to draw the circle.</param>
//        /// <param name="x">The position of the center along x-axis.</param>
//        /// <param name="y">The position of the center along y-axis.</param>
//        /// <param name="radius">The radius of the circle.</param>
//        /// <param name="segments">The number of segments used for drawing the circle. Note: The default variable for the segments parameter varies between different versions of LÖVE.</param>
//    public void Circle(DrawMode mode, double x, double y, double radius, double segments) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a circle.</para>
//        /// </summary>
//    public void SetColor() => throw new NotImplementedException();

///// <summary>
//        /// <para>Clears the screen or active Canvas to the specified color.</para>
//        /// <para>This function is called automatically before love.draw in the default love.run function. See the example in love.run for a typical use of this function.</para>
//        /// <para>Note that the scissor area bounds the cleared region.</para>
//        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// <para>In versions prior to 0.10.0, this function clears the screen to the currently set background color instead.</para>
//        /// </summary>
//        /// <param name="r">The red channel of the color to clear the screen to.</param>
//        /// <param name="g">The green channel of the color to clear the screen to.</param>
//        /// <param name="b">The blue channel of the color to clear the screen to.</param>
//        /// <param name="a">The alpha channel of the color to clear the screen to.</param>
//    public void Clear(double r, double g, double b, double a = 1) => throw new NotImplementedException();

///// <summary>
//        /// <para>Clears the screen or active Canvas to the specified color.</para>
//        /// <para>This function is called automatically before love.draw in the default love.run function. See the example in love.run for a typical use of this function.</para>
//        /// <para>Note that the scissor area bounds the cleared region.</para>
//        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// <para>In versions prior to 0.10.0, this function clears the screen to the currently set background color instead.</para>
//        /// </summary>
//        /// <param name="color">A table in the form of containing the color to clear the first active Canvas to.</param>
//        /// <param name="">Additional tables for each active Canvas.</param>
//    public void Clear(object color, object) => throw new NotImplementedException();

///// <summary>
//        /// <para>Clears the screen or active Canvas to the specified color.</para>
//        /// <para>This function is called automatically before love.draw in the default love.run function. See the example in love.run for a typical use of this function.</para>
//        /// <para>Note that the scissor area bounds the cleared region.</para>
//        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// <para>In versions prior to 0.10.0, this function clears the screen to the currently set background color instead.</para>
//        /// </summary>
//        /// <param name="clearcolor">Whether to clear the active color canvas to transparent black ( ). Typically this should be set to false with this variant of the function.</param>
//        /// <param name="clearstencil">Whether to clear the active stencil buffer, . It can also be an integer between 0 and 255 to clear the stencil buffer to a specific value.</param>
//        /// <param name="cleardepth">Whether to clear the active depth buffer, . It can also be a number between 0 and 1 to clear the depth buffer to a specific value.</param>
//    public void Clear(bool clearcolor, bool clearstencil, bool cleardepth) => throw new NotImplementedException();

///// <summary>
//        /// <para>Clears the screen or active Canvas to the specified color.</para>
//        /// <para>This function is called automatically before love.draw in the default love.run function. See the example in love.run for a typical use of this function.</para>
//        /// <para>Note that the scissor area bounds the cleared region.</para>
//        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// <para>In versions prior to 0.10.0, this function clears the screen to the currently set background color instead.</para>
//        /// </summary>
//    public void NewCanvas() => throw new NotImplementedException();

///// <summary>
//        /// <para>Discards (trashes) the contents of the screen or active Canvas. This is a performance optimization function with niche use cases.</para>
//        /// <para>If the active Canvas has just been changed and the "replace" BlendMode is about to be used to draw something which covers the entire screen, calling love.graphics.discard rather than calling love.graphics.clear or doing nothing may improve performance on mobile devices.</para>
//        /// <para>On some desktop systems this function may do nothing.</para>
//        /// </summary>
//        /// <param name="discardcolor">Whether to discard the texture(s) of the active Canvas(es) (the contents of the screen if no Canvas is active.)</param>
//        /// <param name="discardstencil">Whether to discard the contents of the of the screen / active Canvas.</param>
//    public void Discard(bool discardcolor = true, bool discardstencil = true) => throw new NotImplementedException();

///// <summary>
//        /// <para>Discards (trashes) the contents of the screen or active Canvas. This is a performance optimization function with niche use cases.</para>
//        /// <para>If the active Canvas has just been changed and the "replace" BlendMode is about to be used to draw something which covers the entire screen, calling love.graphics.discard rather than calling love.graphics.clear or doing nothing may improve performance on mobile devices.</para>
//        /// <para>On some desktop systems this function may do nothing.</para>
//        /// </summary>
//        /// <param name="discardcolors">An array containing boolean values indicating whether to discard the texture of each active Canvas, when multiple simultaneous Canvases are active.</param>
//        /// <param name="discardstencil">Whether to discard the contents of the of the screen / active Canvas.</param>
//    public void Discard(object discardcolors, bool discardstencil = true) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a Drawable object (an Image, Canvas, SpriteBatch, ParticleSystem, Mesh, Text object, or Video) on the screen with optional rotation, scaling and shearing.</para>
//        /// <para>Objects are drawn relative to their local coordinate system. The origin is by default located at the top left corner of Image and Canvas objects. All scaling, shearing, and rotation arguments transform the object relative to that point.</para>
//        /// <para>It's possible to rotate an object about, for example, its center by offsetting the origin to the center. Angles must be given in radians for rotation. One can also use negative scaling factors to flip the object about its origin.</para>
//        /// <para>Note that the offsets are applied before rotation, scaling, or shearing; scaling and shearing are applied before rotation.</para>
//        /// <para>The right and bottom edges of the object are shifted at an angle defined by the shearing factors.</para>
//        /// <para>When using the default shader anything drawn with this function will be tinted according to the currently selected color. Set it to pure white to preserve the object's original colors.</para>
//        /// </summary>
//        /// <param name="drawable">A drawable object.</param>
//        /// <param name="x">The position to draw the object (x-axis).</param>
//        /// <param name="y">The position to draw the object (y-axis).</param>
//        /// <param name="r">Orientation (radians).</param>
//        /// <param name="sx">Scale factor (x-axis).</param>
//        /// <param name="sy">Scale factor (y-axis).</param>
//        /// <param name="ox">Origin offset (x-axis).</param>
//        /// <param name="oy">Origin offset (y-axis).</param>
//    public void Draw(object drawable, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a Drawable object (an Image, Canvas, SpriteBatch, ParticleSystem, Mesh, Text object, or Video) on the screen with optional rotation, scaling and shearing.</para>
//        /// <para>Objects are drawn relative to their local coordinate system. The origin is by default located at the top left corner of Image and Canvas objects. All scaling, shearing, and rotation arguments transform the object relative to that point.</para>
//        /// <para>It's possible to rotate an object about, for example, its center by offsetting the origin to the center. Angles must be given in radians for rotation. One can also use negative scaling factors to flip the object about its origin.</para>
//        /// <para>Note that the offsets are applied before rotation, scaling, or shearing; scaling and shearing are applied before rotation.</para>
//        /// <para>The right and bottom edges of the object are shifted at an angle defined by the shearing factors.</para>
//        /// <para>When using the default shader anything drawn with this function will be tinted according to the currently selected color. Set it to pure white to preserve the object's original colors.</para>
//        /// </summary>
//        /// <param name="texture">A ( or ) to texture the with.</param>
//        /// <param name="quad">The Quad to draw on screen.</param>
//        /// <param name="x">The position to draw the object (x-axis).</param>
//        /// <param name="y">The position to draw the object (y-axis).</param>
//        /// <param name="r">Orientation (radians).</param>
//        /// <param name="sx">Scale factor (x-axis).</param>
//        /// <param name="sy">Scale factor (y-axis).</param>
//        /// <param name="ox">Origin offset (x-axis).</param>
//        /// <param name="oy">Origin offset (y-axis).</param>
//        /// <param name="kx">Shearing factor (x-axis).</param>
//        /// <param name="ky">Shearing factor (y-axis).</param>
//    public void Draw(object texture, object quad, double x, double y, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a Drawable object (an Image, Canvas, SpriteBatch, ParticleSystem, Mesh, Text object, or Video) on the screen with optional rotation, scaling and shearing.</para>
//        /// <para>Objects are drawn relative to their local coordinate system. The origin is by default located at the top left corner of Image and Canvas objects. All scaling, shearing, and rotation arguments transform the object relative to that point.</para>
//        /// <para>It's possible to rotate an object about, for example, its center by offsetting the origin to the center. Angles must be given in radians for rotation. One can also use negative scaling factors to flip the object about its origin.</para>
//        /// <para>Note that the offsets are applied before rotation, scaling, or shearing; scaling and shearing are applied before rotation.</para>
//        /// <para>The right and bottom edges of the object are shifted at an angle defined by the shearing factors.</para>
//        /// <para>When using the default shader anything drawn with this function will be tinted according to the currently selected color. Set it to pure white to preserve the object's original colors.</para>
//        /// </summary>
//        /// <param name="drawable">A drawable object.</param>
//        /// <param name="transform">Transformation object.</param>
//    public void Draw(object drawable, object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a Drawable object (an Image, Canvas, SpriteBatch, ParticleSystem, Mesh, Text object, or Video) on the screen with optional rotation, scaling and shearing.</para>
//        /// <para>Objects are drawn relative to their local coordinate system. The origin is by default located at the top left corner of Image and Canvas objects. All scaling, shearing, and rotation arguments transform the object relative to that point.</para>
//        /// <para>It's possible to rotate an object about, for example, its center by offsetting the origin to the center. Angles must be given in radians for rotation. One can also use negative scaling factors to flip the object about its origin.</para>
//        /// <para>Note that the offsets are applied before rotation, scaling, or shearing; scaling and shearing are applied before rotation.</para>
//        /// <para>The right and bottom edges of the object are shifted at an angle defined by the shearing factors.</para>
//        /// <para>When using the default shader anything drawn with this function will be tinted according to the currently selected color. Set it to pure white to preserve the object's original colors.</para>
//        /// </summary>
//        /// <param name="texture">A ( or ) to texture the with.</param>
//        /// <param name="quad">The Quad to draw on screen.</param>
//        /// <param name="transform">Transformation object.</param>
//    public void Draw(object texture, object quad, object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a Drawable object (an Image, Canvas, SpriteBatch, ParticleSystem, Mesh, Text object, or Video) on the screen with optional rotation, scaling and shearing.</para>
//        /// <para>Objects are drawn relative to their local coordinate system. The origin is by default located at the top left corner of Image and Canvas objects. All scaling, shearing, and rotation arguments transform the object relative to that point.</para>
//        /// <para>It's possible to rotate an object about, for example, its center by offsetting the origin to the center. Angles must be given in radians for rotation. One can also use negative scaling factors to flip the object about its origin.</para>
//        /// <para>Note that the offsets are applied before rotation, scaling, or shearing; scaling and shearing are applied before rotation.</para>
//        /// <para>The right and bottom edges of the object are shifted at an angle defined by the shearing factors.</para>
//        /// <para>When using the default shader anything drawn with this function will be tinted according to the currently selected color. Set it to pure white to preserve the object's original colors.</para>
//        /// </summary>
//    public void NewImage() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws many instances of a Mesh with a single draw call, using hardware geometry instancing.</para>
//        /// <para>Each instance can have unique properties (positions, colors, etc.) but will not by default unless a custom Shader along with either per-instance vertex attributes or the love_InstanceID GLSL 3 vertex shader variable is used, otherwise they will all render at the same position on top of each other.</para>
//        /// <para>Instancing is not supported by some older GPUs that are only capable of using OpenGL ES 2 or OpenGL 2. Use love.graphics.getSupported to check.</para>
//        /// </summary>
//        /// <param name="mesh">The mesh to render.</param>
//        /// <param name="instancecount">The number of instances to render.</param>
//        /// <param name="x">The position to draw the instances (x-axis).</param>
//        /// <param name="y">The position to draw the instances (y-axis).</param>
//        /// <param name="r">Orientation (radians).</param>
//        /// <param name="sx">Scale factor (x-axis).</param>
//        /// <param name="sy">Scale factor (y-axis).</param>
//        /// <param name="ox">Origin offset (x-axis).</param>
//        /// <param name="oy">Origin offset (y-axis).</param>
//        /// <param name="kx">Shearing factor (x-axis).</param>
//        /// <param name="ky">Shearing factor (y-axis).</param>
//    public void DrawInstanced(object mesh, double instancecount, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws many instances of a Mesh with a single draw call, using hardware geometry instancing.</para>
//        /// <para>Each instance can have unique properties (positions, colors, etc.) but will not by default unless a custom Shader along with either per-instance vertex attributes or the love_InstanceID GLSL 3 vertex shader variable is used, otherwise they will all render at the same position on top of each other.</para>
//        /// <para>Instancing is not supported by some older GPUs that are only capable of using OpenGL ES 2 or OpenGL 2. Use love.graphics.getSupported to check.</para>
//        /// </summary>
//        /// <param name="mesh">The mesh to render.</param>
//        /// <param name="instancecount">The number of instances to render.</param>
//        /// <param name="transform">A transform object.</param>
//    public void DrawInstanced(object mesh, double instancecount, object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws many instances of a Mesh with a single draw call, using hardware geometry instancing.</para>
//        /// <para>Each instance can have unique properties (positions, colors, etc.) but will not by default unless a custom Shader along with either per-instance vertex attributes or the love_InstanceID GLSL 3 vertex shader variable is used, otherwise they will all render at the same position on top of each other.</para>
//        /// <para>Instancing is not supported by some older GPUs that are only capable of using OpenGL ES 2 or OpenGL 2. Use love.graphics.getSupported to check.</para>
//        /// </summary>
//    public void NewMesh() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a layer of an Array Texture.</para>
//        /// </summary>
//        /// <param name="texture">The Array Texture to draw.</param>
//        /// <param name="layerindex">The index of the layer to use when drawing.</param>
//        /// <param name="x">The position to draw the texture (x-axis).</param>
//        /// <param name="y">The position to draw the texture (y-axis).</param>
//        /// <param name="r">Orientation (radians).</param>
//        /// <param name="sx">Scale factor (x-axis).</param>
//        /// <param name="sy">Scale factor (y-axis).</param>
//        /// <param name="ox">Origin offset (x-axis).</param>
//        /// <param name="oy">Origin offset (y-axis).</param>
//        /// <param name="kx">Shearing factor (x-axis).</param>
//        /// <param name="ky">Shearing factor (y-axis).</param>
//    public void DrawLayer(object texture, double layerindex, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a layer of an Array Texture.</para>
//        /// </summary>
//        /// <param name="texture">The Array Texture to draw.</param>
//        /// <param name="layerindex">The index of the layer to use when drawing.</param>
//        /// <param name="quad">The subsection of the texture's layer to use when drawing.</param>
//        /// <param name="x">The position to draw the texture (x-axis).</param>
//        /// <param name="y">The position to draw the texture (y-axis).</param>
//        /// <param name="r">Orientation (radians).</param>
//        /// <param name="sx">Scale factor (x-axis).</param>
//        /// <param name="sy">Scale factor (y-axis).</param>
//        /// <param name="ox">Origin offset (x-axis).</param>
//        /// <param name="oy">Origin offset (y-axis).</param>
//        /// <param name="kx">Shearing factor (x-axis).</param>
//        /// <param name="ky">Shearing factor (y-axis).</param>
//    public void DrawLayer(object texture, double layerindex, object quad, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a layer of an Array Texture.</para>
//        /// </summary>
//        /// <param name="texture">The Array Texture to draw.</param>
//        /// <param name="layerindex">The index of the layer to use when drawing.</param>
//        /// <param name="transform">A transform object.</param>
//    public void DrawLayer(object texture, double layerindex, object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a layer of an Array Texture.</para>
//        /// </summary>
//        /// <param name="texture">The Array Texture to draw.</param>
//        /// <param name="layerindex">The index of the layer to use when drawing.</param>
//        /// <param name="quad">The subsection of the texture's layer to use when drawing.</param>
//        /// <param name="transform">A transform object.</param>
//    public void DrawLayer(object texture, double layerindex, object quad, object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a layer of an Array Texture.</para>
//        /// </summary>
//    public void NewArrayImage() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new array Image.</para>
//        /// <para>An array image / array texture is a single object which contains multiple 'layers' or 'slices' of 2D sub-images. It can be thought of similarly to a texture atlas or sprite sheet, but it doesn't suffer from the same tile / quad bleeding artifacts that texture atlases do – although every sub-image must have the same dimensions.</para>
//        /// <para>A specific layer of an array image can be drawn with love.graphics.drawLayer / SpriteBatch:addLayer, or with the Quad variant of love.graphics.draw and Quad:setLayer, or via a custom Shader.</para>
//        /// <para>To use an array image in a Shader, it must be declared as a ArrayImage or sampler2DArray type (instead of Image or sampler2D). The Texel(ArrayImage image, vec3 texturecoord) shader function must be used to get pixel colors from a slice of the array image. The vec3 argument contains the texture coordinate in the first two components, and the 0-based slice index in the third component.</para>
//        /// </summary>
//        /// <param name="slices">A table containing filepaths to images (or , , , or objects), in an array. Each sub-image must have the same dimensions. A table of tables can also be given, where each sub-table contains all mipmap levels for the slice index of that sub-table.</param>
//        /// <param name="settings">
//        /// Optional table of settings to configure the array image, containing the following fields:
//        /// <list type="bullet">
//        /// <item><term>mipmaps</term><description>boolean: True to make the image use mipmaps, false to disable them. Mipmaps will be automatically generated if the image isn't a format.</description></item>
//        /// <item><term>linear</term><description>boolean: True to treat the image's pixels as linear instead of sRGB, when is enabled. Most images are authored as sRGB.</description></item>
//        /// <item><term>dpiscale</term><description>number: The DPI scale to use when drawing the array image and calling / .</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>image</term><description>An Array Image object.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewArrayImage(object slices, object settings = null) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws an ellipse.</para>
//        /// </summary>
//        /// <param name="mode">How to draw the ellipse.</param>
//        /// <param name="x">The position of the center along x-axis.</param>
//        /// <param name="y">The position of the center along y-axis.</param>
//        /// <param name="radiusx">The radius of the ellipse along the x-axis (half the ellipse's width).</param>
//        /// <param name="radiusy">The radius of the ellipse along the y-axis (half the ellipse's height).</param>
//        /// <param name="segments">The number of segments used for drawing the ellipse (automatically guessed when not set).</param>
//    public void Ellipse(DrawMode mode, double x, double y, double radiusx, double radiusy, double segments = null) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws an ellipse.</para>
//        /// </summary>
//    public void SetColor() => throw new NotImplementedException();

///// <summary>
//        /// <para>Immediately renders any pending automatically batched draws.</para>
//        /// <para>LÖVE will call this function internally as needed when most state is changed, so it is not necessary to manually call it.</para>
//        /// <para>The current batch will be automatically flushed by love.graphics state changes (except for the transform stack and the current color), as well as Shader:send and methods on Textures which change their state. Using a different Image in consecutive love.graphics.draw calls will also flush the current batch.</para>
//        /// <para>SpriteBatches, ParticleSystems, Meshes, and Text objects do their own batching and do not affect automatic batching of other draws, aside from flushing the current batch when they're drawn.</para>
//        /// </summary>
//    public void FlushBatch() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws lines between points.</para>
//        /// </summary>
//        /// <param name="x1">The position of first point on the x-axis.</param>
//        /// <param name="y1">The position of first point on the y-axis.</param>
//        /// <param name="x2">The position of second point on the x-axis.</param>
//        /// <param name="y2">The position of second point on the y-axis.</param>
//        /// <param name="">You can continue passing point positions to draw a polyline.</param>
//    public void Line(double x1, double y1, double x2, double y2, object) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws lines between points.</para>
//        /// </summary>
//        /// <param name="points">A table of point positions, as described above.</param>
//    public void Line(object points) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws lines between points.</para>
//        /// </summary>
//    public void Line() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a point.</para>
//        /// </summary>
//        /// <param name="x">The position on the x-axis.</param>
//        /// <param name="y">The position on the y-axis.</param>
//    public void Point(double x, double y) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a point.</para>
//        /// </summary>
//    public void GetDimensions() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws one or more points.</para>
//        /// </summary>
//        /// <param name="x">The position of the first point on the x-axis.</param>
//        /// <param name="y">The position of the first point on the y-axis.</param>
//        /// <param name="">The x and y coordinates of additional points.</param>
//    public void Points(double x, double y, object) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws one or more points.</para>
//        /// </summary>
//        /// <param name="points">
//        /// A table containing multiple point positions, in the form of .
//        /// <list type="bullet">
//        /// <item><term>x</term><description>number: The position of the first point on the x-axis.</description></item>
//        /// <item><term>y</term><description>number: The position of the first point on the y-axis.</description></item>
//        /// <item><term></term><description>number ...: The x and y coordinates of additional points.</description></item>
//        /// </list>
//        /// </param>
//    public void Points(object points) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws one or more points.</para>
//        /// </summary>
//        /// <param name="points">
//        /// A table containing multiple individually colored points, in the form of .
//        /// <list type="bullet">
//        /// <item><term>point</term><description>table: A table containing the position and color of the first point, in the form of . The color components are optional.</description></item>
//        /// <item><term></term><description>table ...: Additional tables containing the position and color of more points, in the form of . The color components are optional.</description></item>
//        /// </list>
//        /// </param>
//    public void Points(object points) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws one or more points.</para>
//        /// </summary>
//    public void GetDimensions() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a polygon.</para>
//        /// <para>Following the mode argument, this function can accept multiple numeric arguments or a single table of numeric arguments. In either case the arguments are interpreted as alternating x and y coordinates of the polygon's vertices.</para>
//        /// </summary>
//        /// <param name="mode">How to draw the polygon.</param>
//        /// <param name="">The vertices of the polygon.</param>
//    public void Polygon(DrawMode mode, object) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a polygon.</para>
//        /// <para>Following the mode argument, this function can accept multiple numeric arguments or a single table of numeric arguments. In either case the arguments are interpreted as alternating x and y coordinates of the polygon's vertices.</para>
//        /// </summary>
//        /// <param name="mode">How to draw the polygon.</param>
//        /// <param name="vertices">The vertices of the polygon as a table.</param>
//    public void Polygon(DrawMode mode, object vertices) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a polygon.</para>
//        /// <para>Following the mode argument, this function can accept multiple numeric arguments or a single table of numeric arguments. In either case the arguments are interpreted as alternating x and y coordinates of the polygon's vertices.</para>
//        /// </summary>
//    public void Polygon() => throw new NotImplementedException();

///// <summary>
//        /// <para>Displays the results of drawing operations on the screen.</para>
//        /// <para>This function is used when writing your own love.run function. It presents all the results of your drawing operations on the screen. See the example in love.run for a typical use of this function.</para>
//        /// </summary>
//    public void Present() => throw new NotImplementedException();

///// <summary>
//        /// <para>Displays the results of drawing operations on the screen.</para>
//        /// <para>This function is used when writing your own love.run function. It presents all the results of your drawing operations on the screen. See the example in love.run for a typical use of this function.</para>
//        /// </summary>
//    public void Sleep() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="text">The text to draw.</param>
//        /// <param name="x">The position to draw the object (x-axis).</param>
//        /// <param name="y">The position to draw the object (y-axis).</param>
//        /// <param name="r">Orientation (radians).</param>
//        /// <param name="sx">Scale factor (x-axis).</param>
//        /// <param name="sy">Scale factor (y-axis).</param>
//        /// <param name="ox">Origin offset (x-axis).</param>
//        /// <param name="oy">Origin offset (y-axis).</param>
//    public void Print(string text, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="text">The text to draw.</param>
//        /// <param name="font">The Font object to use.</param>
//        /// <param name="x">The position of the text on the x-axis.</param>
//        /// <param name="y">The position of the text on the y-axis.</param>
//        /// <param name="angle">The orientation of the text in radians.</param>
//        /// <param name="sx">Scale factor on the x-axis.</param>
//        /// <param name="sy">Scale factor on the y-axis.</param>
//        /// <param name="ox">Origin offset on the x-axis.</param>
//        /// <param name="oy">Origin offset on the y-axis.</param>
//        /// <param name="kx">Shearing / skew factor on the x-axis.</param>
//        /// <param name="ky">Shearing / skew factor on the y-axis.</param>
//    public void Print(string text, object font, double x = 0, double y = 0, double angle = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//    public void Print(object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//    public void Print(object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="text">The text to draw.</param>
//        /// <param name="transform">Transformation object.</param>
//    public void Print(string text, object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="text">The text to draw.</param>
//        /// <param name="font">The Font object to use.</param>
//        /// <param name="transform">Transformation object.</param>
//    public void Print(string text, object font, object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//    public void Print(object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//    public void Print(object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws text on screen. If no Font is set, one will be created and set (once) if needed.</para>
//        /// <para>As of LOVE 0.7.1, when using translation and scaling functions while drawing text, this function assumes the scale occurs first.  If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.</para>
//        /// <para>love.graphics.print and love.graphics.printf both support UTF-8 encoding. You'll also need a proper Font for special characters.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//    public void SetColor() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws formatted text, with word wrap and alignment.</para>
//        /// <para>See additional notes in love.graphics.print.</para>
//        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
//        /// <para>In version 0.9.2 and earlier, wrapping was implemented by breaking up words by spaces and putting them back together to make sure things fit nicely within the limit provided. However, due to the way this is done, extra spaces between words would end up missing when printed on the screen, and some lines could overflow past the provided wrap limit. In version 0.10.0 and newer this is no longer the case.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="text">A text string.</param>
//        /// <param name="x">The position on the x-axis.</param>
//        /// <param name="y">The position on the y-axis.</param>
//        /// <param name="limit">Wrap the line after this many horizontal pixels.</param>
//        /// <param name="align">The alignment.</param>
//    public void Printf(string text, double x, double y, double limit, AlignMode align = "left") => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws formatted text, with word wrap and alignment.</para>
//        /// <para>See additional notes in love.graphics.print.</para>
//        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
//        /// <para>In version 0.9.2 and earlier, wrapping was implemented by breaking up words by spaces and putting them back together to make sure things fit nicely within the limit provided. However, due to the way this is done, extra spaces between words would end up missing when printed on the screen, and some lines could overflow past the provided wrap limit. In version 0.10.0 and newer this is no longer the case.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="text">A text string.</param>
//        /// <param name="font">The Font object to use.</param>
//        /// <param name="x">The position on the x-axis.</param>
//        /// <param name="y">The position on the y-axis.</param>
//        /// <param name="limit">Wrap the line after this many horizontal pixels.</param>
//        /// <param name="align">The alignment.</param>
//        /// <param name="r">Orientation (radians).</param>
//        /// <param name="sx">Scale factor (x-axis).</param>
//        /// <param name="sy">Scale factor (y-axis).</param>
//        /// <param name="ox">Origin offset (x-axis).</param>
//        /// <param name="oy">Origin offset (y-axis).</param>
//        /// <param name="kx">Shearing factor (x-axis).</param>
//        /// <param name="ky">Shearing factor (y-axis).</param>
//    public void Printf(string text, object font, double x, double y, double limit, AlignMode align = "left", double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws formatted text, with word wrap and alignment.</para>
//        /// <para>See additional notes in love.graphics.print.</para>
//        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
//        /// <para>In version 0.9.2 and earlier, wrapping was implemented by breaking up words by spaces and putting them back together to make sure things fit nicely within the limit provided. However, due to the way this is done, extra spaces between words would end up missing when printed on the screen, and some lines could overflow past the provided wrap limit. In version 0.10.0 and newer this is no longer the case.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="text">A text string.</param>
//        /// <param name="transform">Transformation object.</param>
//        /// <param name="limit">Wrap the line after this many horizontal pixels.</param>
//        /// <param name="align">The alignment.</param>
//    public void Printf(string text, object transform, double limit, AlignMode align = "left") => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws formatted text, with word wrap and alignment.</para>
//        /// <para>See additional notes in love.graphics.print.</para>
//        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
//        /// <para>In version 0.9.2 and earlier, wrapping was implemented by breaking up words by spaces and putting them back together to make sure things fit nicely within the limit provided. However, due to the way this is done, extra spaces between words would end up missing when printed on the screen, and some lines could overflow past the provided wrap limit. In version 0.10.0 and newer this is no longer the case.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="text">A text string.</param>
//        /// <param name="font">The Font object to use.</param>
//        /// <param name="transform">Transformation object.</param>
//        /// <param name="limit">Wrap the line after this many horizontal pixels.</param>
//        /// <param name="align">The alignment.</param>
//    public void Printf(string text, object font, object transform, double limit, AlignMode align = "left") => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws formatted text, with word wrap and alignment.</para>
//        /// <para>See additional notes in love.graphics.print.</para>
//        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
//        /// <para>In version 0.9.2 and earlier, wrapping was implemented by breaking up words by spaces and putting them back together to make sure things fit nicely within the limit provided. However, due to the way this is done, extra spaces between words would end up missing when printed on the screen, and some lines could overflow past the provided wrap limit. In version 0.10.0 and newer this is no longer the case.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//    public void Printf(object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws formatted text, with word wrap and alignment.</para>
//        /// <para>See additional notes in love.graphics.print.</para>
//        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
//        /// <para>In version 0.9.2 and earlier, wrapping was implemented by breaking up words by spaces and putting them back together to make sure things fit nicely within the limit provided. However, due to the way this is done, extra spaces between words would end up missing when printed on the screen, and some lines could overflow past the provided wrap limit. In version 0.10.0 and newer this is no longer the case.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//    public void Printf(object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws formatted text, with word wrap and alignment.</para>
//        /// <para>See additional notes in love.graphics.print.</para>
//        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
//        /// <para>In version 0.9.2 and earlier, wrapping was implemented by breaking up words by spaces and putting them back together to make sure things fit nicely within the limit provided. However, due to the way this is done, extra spaces between words would end up missing when printed on the screen, and some lines could overflow past the provided wrap limit. In version 0.10.0 and newer this is no longer the case.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//    public void Printf(object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws formatted text, with word wrap and alignment.</para>
//        /// <para>See additional notes in love.graphics.print.</para>
//        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
//        /// <para>In version 0.9.2 and earlier, wrapping was implemented by breaking up words by spaces and putting them back together to make sure things fit nicely within the limit provided. However, due to the way this is done, extra spaces between words would end up missing when printed on the screen, and some lines could overflow past the provided wrap limit. In version 0.10.0 and newer this is no longer the case.</para>
//        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//    public void Printf(object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a rectangle.</para>
//        /// </summary>
//        /// <param name="mode">How to draw the rectangle.</param>
//        /// <param name="x">The position of top-left corner along the x-axis.</param>
//        /// <param name="y">The position of top-left corner along the y-axis.</param>
//        /// <param name="width">Width of the rectangle.</param>
//        /// <param name="height">Height of the rectangle.</param>
//    public void Rectangle(DrawMode mode, double x, double y, double width, double height) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws a rectangle.</para>
//        /// </summary>
//    public void Rectangle() => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws geometry as a stencil.</para>
//        /// <para>The geometry drawn by the supplied function sets invisible stencil values of pixels, instead of setting pixel colors. The stencil buffer (which contains those stencil values) can act like a mask / stencil - love.graphics.setStencilTest can be used afterward to determine how further rendering is affected by the stencil values in each pixel.</para>
//        /// <para>Stencil values are integers within the range of [0, 255].</para>
//        /// </summary>
//        /// <param name="stencilfunction">Function which draws geometry. The stencil values of pixels, rather than the color of each pixel, will be affected by the geometry.</param>
//        /// <param name="action">How to modify any stencil values of pixels that are touched by what's drawn in the stencil function.</param>
//        /// <param name="value">The new stencil value to use for pixels if the "replace" stencil action is used. Has no effect with other stencil actions. Must be between 0 and 255.</param>
//        /// <param name="keepvalues">True to preserve old stencil values of pixels, false to re-set every pixel's stencil value to 0 before executing the stencil function. will also re-set all stencil values.</param>
//    public void Stencil(object stencilfunction, StencilAction action = "replace", double value = 1, bool keepvalues = false) => throw new NotImplementedException();

///// <summary>
//        /// <para>Draws geometry as a stencil.</para>
//        /// <para>The geometry drawn by the supplied function sets invisible stencil values of pixels, instead of setting pixel colors. The stencil buffer (which contains those stencil values) can act like a mask / stencil - love.graphics.setStencilTest can be used afterward to determine how further rendering is affected by the stencil values in each pixel.</para>
//        /// <para>Stencil values are integers within the range of [0, 255].</para>
//        /// </summary>
//    public void Rectangle() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a screenshot once the current frame is done (after love.draw has finished).</para>
//        /// <para>Since this function enqueues a screenshot capture rather than executing it immediately, it can be called from an input callback or love.update and it will still capture all of what's drawn to the screen in that frame.</para>
//        /// </summary>
//        /// <param name="filename">The filename to save the screenshot to. The encoded image type is determined based on the extension of the filename, and must be one of the .</param>
//    public void CaptureScreenshot(string filename) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a screenshot once the current frame is done (after love.draw has finished).</para>
//        /// <para>Since this function enqueues a screenshot capture rather than executing it immediately, it can be called from an input callback or love.update and it will still capture all of what's drawn to the screen in that frame.</para>
//        /// </summary>
//        /// <param name="callback">Function which gets called once the screenshot has been captured. An is passed into the function as its only argument.</param>
//    public void CaptureScreenshot(object callback) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a screenshot once the current frame is done (after love.draw has finished).</para>
//        /// <para>Since this function enqueues a screenshot capture rather than executing it immediately, it can be called from an input callback or love.update and it will still capture all of what's drawn to the screen in that frame.</para>
//        /// </summary>
//        /// <param name="channel">The Channel to the generated to.</param>
//    public void CaptureScreenshot(object channel) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a screenshot once the current frame is done (after love.draw has finished).</para>
//        /// <para>Since this function enqueues a screenshot capture rather than executing it immediately, it can be called from an input callback or love.update and it will still capture all of what's drawn to the screen in that frame.</para>
//        /// </summary>
//    public void SetIdentity() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Canvas object for offscreen rendering.</para>
//        /// </summary>
//        /// <param name="canvas">A new Canvas with dimensions equal to the window's size in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>canvas</term><description>A new Canvas with dimensions equal to the window's size in pixels.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewCanvas(object canvas) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Canvas object for offscreen rendering.</para>
//        /// </summary>
//        /// <param name="width">The desired width of the Canvas.</param>
//        /// <param name="height">The desired height of the Canvas.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>canvas</term><description>A new Canvas with specified width and height.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewCanvas(double width, double height) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Canvas object for offscreen rendering.</para>
//        /// </summary>
//        /// <param name="width">The desired width of the Canvas.</param>
//        /// <param name="height">The desired height of the Canvas.</param>
//        /// <param name="settings">
//        /// A table containing the given fields:
//        /// <list type="bullet">
//        /// <item><term>type</term><description>TextureType: The type of Canvas to create.</description></item>
//        /// <item><term>format</term><description>PixelFormat: The format of the Canvas.</description></item>
//        /// <item><term>readable</term><description>boolean: Whether the Canvas is (drawable and accessible in a ). True by default for regular formats, false by default for depth/stencil formats.</description></item>
//        /// <item><term>msaa</term><description>number: The desired number of multisample antialiasing (MSAA) samples used when drawing to the Canvas.</description></item>
//        /// <item><term>dpiscale</term><description>number: The of the Canvas, used when drawing to the Canvas as well as when drawing the Canvas to the screen.</description></item>
//        /// <item><term>mipmaps</term><description>MipmapMode: Whether the Canvas has mipmaps, and whether to automatically regenerate them if so.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>canvas</term><description>A new Canvas with specified width and height.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewCanvas(double width, double height, object settings) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Canvas object for offscreen rendering.</para>
//        /// </summary>
//        /// <param name="width">The desired width of the Canvas.</param>
//        /// <param name="height">The desired height of the Canvas.</param>
//        /// <param name="layers">The number of array layers (if the Canvas is an Array Texture), or the volume depth (if the Canvas is a Volume Texture).</param>
//        /// <param name="settings">
//        /// A table containing the given fields:
//        /// <list type="bullet">
//        /// <item><term>type</term><description>TextureType: The type of Canvas to create.</description></item>
//        /// <item><term>format</term><description>PixelFormat: The format of the Canvas.</description></item>
//        /// <item><term>readable</term><description>boolean: Whether the Canvas is (drawable and accessible in a ). True by default for regular formats, false by default for depth/stencil formats.</description></item>
//        /// <item><term>msaa</term><description>number: The desired number of multisample antialiasing (MSAA) samples used when drawing to the Canvas.</description></item>
//        /// <item><term>dpiscale</term><description>number: The of the Canvas, used when drawing to the Canvas as well as when drawing the Canvas to the screen.</description></item>
//        /// <item><term>mipmaps</term><description>MipmapMode: Whether the Canvas has mipmaps, and whether to automatically regenerate them if so.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>canvas</term><description>A new Canvas with specified width and height.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewCanvas(double width, double height, double layers, object settings) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new cubemap Image.</para>
//        /// <para>Cubemap images have 6 faces (sides) which represent a cube. They can't be rendered directly, they can only be used in Shader code (and sent to the shader via Shader:send).</para>
//        /// <para>To use a cubemap image in a Shader, it must be declared as a CubeImage or samplerCube type (instead of Image or sampler2D). The Texel(CubeImage image, vec3 direction) shader function must be used to get pixel colors from the cubemap. The vec3 argument is a normalized direction from the center of the cube, rather than explicit texture coordinates.</para>
//        /// <para>Each face in a cubemap image must have square dimensions.</para>
//        /// <para>For variants of this function which accept a single image containing multiple cubemap faces, they must be laid out in one of the following forms in the image:</para>
//        /// <para>or:</para>
//        /// <para>or:</para>
//        /// <para>or:</para>
//        /// <para>Note that this form, despite looking like it should fold into a cube, does not do so: the orientation of the individual faces is the same as in the other three layouts.</para>
//        /// </summary>
//        /// <param name="filename">The filepath to a cubemap image file (or a , , or ).</param>
//        /// <param name="settings">
//        /// Optional table of settings to configure the cubemap image, containing the following fields:
//        /// <list type="bullet">
//        /// <item><term>mipmaps</term><description>boolean: True to make the image use mipmaps, false to disable them. Mipmaps will be automatically generated if the image isn't a format.</description></item>
//        /// <item><term>linear</term><description>boolean: True to treat the image's pixels as linear instead of sRGB, when is enabled. Most images are authored as sRGB.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>image</term><description>An cubemap Image object.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewCubeImage(string filename, object settings = null) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new cubemap Image.</para>
//        /// <para>Cubemap images have 6 faces (sides) which represent a cube. They can't be rendered directly, they can only be used in Shader code (and sent to the shader via Shader:send).</para>
//        /// <para>To use a cubemap image in a Shader, it must be declared as a CubeImage or samplerCube type (instead of Image or sampler2D). The Texel(CubeImage image, vec3 direction) shader function must be used to get pixel colors from the cubemap. The vec3 argument is a normalized direction from the center of the cube, rather than explicit texture coordinates.</para>
//        /// <para>Each face in a cubemap image must have square dimensions.</para>
//        /// <para>For variants of this function which accept a single image containing multiple cubemap faces, they must be laid out in one of the following forms in the image:</para>
//        /// <para>or:</para>
//        /// <para>or:</para>
//        /// <para>or:</para>
//        /// <para>Note that this form, despite looking like it should fold into a cube, does not do so: the orientation of the individual faces is the same as in the other three layouts.</para>
//        /// </summary>
//        /// <param name="faces">A table containing 6 filepaths to images (or , , , or objects), in an array, in the order +x -x +y -y +z -z. Each face image must have the same dimensions. A table of tables can also be given, where each sub-table contains all mipmap levels for the cube face index of that sub-table.</param>
//        /// <param name="settings">
//        /// Optional table of settings to configure the cubemap image, containing the following fields:
//        /// <list type="bullet">
//        /// <item><term>mipmaps</term><description>boolean: True to make the image use mipmaps, false to disable them. Mipmaps will be automatically generated if the image isn't a format.</description></item>
//        /// <item><term>linear</term><description>boolean: True to treat the image's pixels as linear instead of sRGB, when is enabled. Most images are authored as sRGB.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>image</term><description>An cubemap Image object.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewCubeImage(object faces, object settings = null) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Font from a TrueType font or BMFont file. OpenType fonts (.otf) work as well but some of its features may not be supported.</para>
//        /// <para>Created fonts are not cached, in that calling this function with the same arguments will always create a new Font object.</para>
//        /// <para>All variants which accept a filename can also accept a Data object instead.</para>
//        /// <para>The default font in LÖVE is 'Bitstream Vera Sans' size 12 (before LÖVE 12.0) and 'Noto Sans' size 13 (after LÖVE 12.0).</para>
//        /// </summary>
//        /// <param name="filename">The filepath or filedata to the font file. Types can be below:</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>A Font object which can be used to draw text on screen.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewFont(object filename) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Font from a TrueType font or BMFont file. OpenType fonts (.otf) work as well but some of its features may not be supported.</para>
//        /// <para>Created fonts are not cached, in that calling this function with the same arguments will always create a new Font object.</para>
//        /// <para>All variants which accept a filename can also accept a Data object instead.</para>
//        /// <para>The default font in LÖVE is 'Bitstream Vera Sans' size 12 (before LÖVE 12.0) and 'Noto Sans' size 13 (after LÖVE 12.0).</para>
//        /// </summary>
//        /// <param name="filename">The filepath to the TrueType font file.</param>
//        /// <param name="size">The size of the font in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>A Font object which can be used to draw text on screen.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewFont(string filename, double size) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Font from a TrueType font or BMFont file. OpenType fonts (.otf) work as well but some of its features may not be supported.</para>
//        /// <para>Created fonts are not cached, in that calling this function with the same arguments will always create a new Font object.</para>
//        /// <para>All variants which accept a filename can also accept a Data object instead.</para>
//        /// <para>The default font in LÖVE is 'Bitstream Vera Sans' size 12 (before LÖVE 12.0) and 'Noto Sans' size 13 (after LÖVE 12.0).</para>
//        /// </summary>
//        /// <param name="filename">The filepath to the BMFont file.</param>
//        /// <param name="imagefilename">The filepath to the BMFont's image file. If this argument is omitted, the path specified inside the BMFont file will be used.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>A Font object which can be used to draw text on screen.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewFont(string filename, string imagefilename) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Font from a TrueType font or BMFont file. OpenType fonts (.otf) work as well but some of its features may not be supported.</para>
//        /// <para>Created fonts are not cached, in that calling this function with the same arguments will always create a new Font object.</para>
//        /// <para>All variants which accept a filename can also accept a Data object instead.</para>
//        /// <para>The default font in LÖVE is 'Bitstream Vera Sans' size 12 (before LÖVE 12.0) and 'Noto Sans' size 13 (after LÖVE 12.0).</para>
//        /// </summary>
//        /// <param name="size">The size of the font in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>A Font object which can be used to draw text on screen.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewFont(double size = 12) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new framebuffer object for offscreen rendering.</para>
//        /// </summary>
//        /// <param name="framebuffer">A new framebuffer with width/height equal to the window width/height.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>framebuffer</term><description>A new framebuffer with width/height equal to the window width/height.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewFramebuffer(object framebuffer) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new framebuffer object for offscreen rendering.</para>
//        /// </summary>
//        /// <param name="width">The desired width of the framebuffer.</param>
//        /// <param name="height">The desired height of the framebuffer.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>framebuffer</term><description>A new framebuffer with specified width and height.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewFramebuffer(double width, double height) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Image from a filepath, FileData, an ImageData, or a CompressedImageData, and optionally generates or specifies mipmaps for the image.</para>
//        /// <para>Version 11.0 updated love.graphics.newImage to treat file names ending with "@2x", "@3x", etc. as a pixel density scale factor if none is explicitly supplied.</para>
//        /// </summary>
//        /// <param name="filename">The filepath to the image file (or a or or or object).</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>image</term><description>A new Image object which can be drawn on screen.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewImage(string filename) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Image from a filepath, FileData, an ImageData, or a CompressedImageData, and optionally generates or specifies mipmaps for the image.</para>
//        /// <para>Version 11.0 updated love.graphics.newImage to treat file names ending with "@2x", "@3x", etc. as a pixel density scale factor if none is explicitly supplied.</para>
//        /// </summary>
//    public void NewImage() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Font by loading a specifically formatted image.</para>
//        /// <para>In versions prior to 0.9.0, LÖVE expects ISO 8859-1 encoding for the glyphs string.</para>
//        /// </summary>
//        /// <param name="filename">The filepath to the image file.</param>
//        /// <param name="glyphs">A string of the characters in the image in order from left to right.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>A Font object which can be used to draw text on screen.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewImageFont(string filename, string glyphs) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Font by loading a specifically formatted image.</para>
//        /// <para>In versions prior to 0.9.0, LÖVE expects ISO 8859-1 encoding for the glyphs string.</para>
//        /// </summary>
//        /// <param name="imageData">The ImageData object to create the font from.</param>
//        /// <param name="glyphs">A string of the characters in the image in order from left to right.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>A Font object which can be used to draw text on screen.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewImageFont(object imageData, string glyphs) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Font by loading a specifically formatted image.</para>
//        /// <para>In versions prior to 0.9.0, LÖVE expects ISO 8859-1 encoding for the glyphs string.</para>
//        /// </summary>
//    public void NewImageFont() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new ParticleSystem.</para>
//        /// </summary>
//        /// <param name="image">The image to use.</param>
//        /// <param name="buffer">The max number of particles at the same time.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>system</term><description>A new ParticleSystem.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewParticleSystem(object image, double buffer = 1000) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new ParticleSystem.</para>
//        /// </summary>
//        /// <param name="texture">The texture ( or ) to use.</param>
//        /// <param name="buffer">The max number of particles at the same time.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>system</term><description>A new ParticleSystem.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewParticleSystem(object texture, double buffer = 1000) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new ParticleSystem.</para>
//        /// </summary>
//    public void NewImage() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new PixelEffect object for hardware-accelerated pixel level effects.</para>
//        /// <para>A PixelEffect contains at least one function, named effect, which is the effect itself, but it can contain additional functions.</para>
//        /// </summary>
//        /// <param name="code">The pixel effect code.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pixeleffect</term><description>A PixelEffect object for use in drawing operations.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewPixelEffect(string code) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Quad.</para>
//        /// <para>The purpose of a Quad is to use a fraction of a texture to draw objects, as opposed to drawing the entire texture. It is most useful for sprite sheets and atlases: in a sprite atlas, multiple sprites reside in the same texture, quad is used to draw a specific sprite from that texture; in animated sprites with all frames residing in the same texture, quad is used to draw specific frame from the animation.</para>
//        /// </summary>
//        /// <param name="x">The top-left position in the along the x-axis.</param>
//        /// <param name="y">The top-left position in the along the y-axis.</param>
//        /// <param name="width">The width of the Quad in the . (Must be greater than 0.)</param>
//        /// <param name="height">The height of the Quad in the . (Must be greater than 0.)</param>
//        /// <param name="sw">The reference width, the width of the . (Must be greater than 0.)</param>
//        /// <param name="sh">The reference height, the height of the . (Must be greater than 0.)</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>quad</term><description>The new Quad.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewQuad(double x, double y, double width, double height, double sw, double sh) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Quad.</para>
//        /// <para>The purpose of a Quad is to use a fraction of a texture to draw objects, as opposed to drawing the entire texture. It is most useful for sprite sheets and atlases: in a sprite atlas, multiple sprites reside in the same texture, quad is used to draw a specific sprite from that texture; in animated sprites with all frames residing in the same texture, quad is used to draw specific frame from the animation.</para>
//        /// </summary>
//        /// <param name="x">The top-left position in the along the x-axis.</param>
//        /// <param name="y">The top-left position in the along the y-axis.</param>
//        /// <param name="width">The width of the Quad in the . (Must be greater than 0.)</param>
//        /// <param name="height">The height of the Quad in the . (Must be greater than 0.)</param>
//        /// <param name="texture">The texture whose width and height will be used as the reference width and height.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>quad</term><description>The new Quad.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewQuad(double x, double y, double width, double height, object texture) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Quad.</para>
//        /// <para>The purpose of a Quad is to use a fraction of a texture to draw objects, as opposed to drawing the entire texture. It is most useful for sprite sheets and atlases: in a sprite atlas, multiple sprites reside in the same texture, quad is used to draw a specific sprite from that texture; in animated sprites with all frames residing in the same texture, quad is used to draw specific frame from the animation.</para>
//        /// </summary>
//    public void NewImage() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Shader object for hardware-accelerated vertex and pixel effects. A Shader contains either vertex shader code, pixel shader code, or both.</para>
//        /// <para>Shaders are small programs which are run on the graphics card when drawing. Vertex shaders are run once for each vertex (for example, an image has 4 vertices - one at each corner. A Mesh might have many more.) Pixel shaders are run once for each pixel on the screen which the drawn object touches. Pixel shader code is executed after all the object's vertices have been processed by the vertex shader.</para>
//        /// </summary>
//        /// <param name="code">The pixel shader or vertex shader code, or a filename pointing to a file with the code.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>shader</term><description>A Shader object for use in drawing operations.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewShader(string code) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Shader object for hardware-accelerated vertex and pixel effects. A Shader contains either vertex shader code, pixel shader code, or both.</para>
//        /// <para>Shaders are small programs which are run on the graphics card when drawing. Vertex shaders are run once for each vertex (for example, an image has 4 vertices - one at each corner. A Mesh might have many more.) Pixel shaders are run once for each pixel on the screen which the drawn object touches. Pixel shader code is executed after all the object's vertices have been processed by the vertex shader.</para>
//        /// </summary>
//        /// <param name="pixelcode">The pixel shader code, or a filename pointing to a file with the code.</param>
//        /// <param name="vertexcode">The vertex shader code, or a filename pointing to a file with the code.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>shader</term><description>A Shader object for use in drawing operations.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewShader(string pixelcode, string vertexcode) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new Shader object for hardware-accelerated vertex and pixel effects. A Shader contains either vertex shader code, pixel shader code, or both.</para>
//        /// <para>Shaders are small programs which are run on the graphics card when drawing. Vertex shaders are run once for each vertex (for example, an image has 4 vertices - one at each corner. A Mesh might have many more.) Pixel shaders are run once for each pixel on the screen which the drawn object touches. Pixel shader code is executed after all the object's vertices have been processed by the vertex shader.</para>
//        /// </summary>
//    public void NewShader() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new SpriteBatch object.</para>
//        /// </summary>
//        /// <param name="image">The Image to use for the sprites.</param>
//        /// <param name="maxsprites">The maximum number of sprites that the SpriteBatch can contain at any given time. Since version , additional sprites added past this number will automatically grow the spritebatch.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>spriteBatch</term><description>The new SpriteBatch.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewSpriteBatch(object image, double maxsprites = 1000) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new SpriteBatch object.</para>
//        /// </summary>
//        /// <param name="image">The Image to use for the sprites.</param>
//        /// <param name="maxsprites">The maximum number of sprites that the SpriteBatch can contain at any given time. Since version , additional sprites added past this number will automatically grow the spritebatch.</param>
//        /// <param name="usage">The expected usage of the SpriteBatch. The specified usage mode affects the SpriteBatch's memory usage and performance.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>spriteBatch</term><description>The new SpriteBatch.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewSpriteBatch(object image, double maxsprites = 1000, object usage = "dynamic") => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new SpriteBatch object.</para>
//        /// </summary>
//        /// <param name="texture">The or to use for the sprites.</param>
//        /// <param name="maxsprites">The maximum number of sprites that the SpriteBatch can contain at any given time. Since version , additional sprites added past this number will automatically grow the spritebatch.</param>
//        /// <param name="usage">The expected usage of the SpriteBatch. The specified usage mode affects the SpriteBatch's memory usage and performance.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>spriteBatch</term><description>The new SpriteBatch.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewSpriteBatch(object texture, double maxsprites = 1000, object usage = "dynamic") => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new SpriteBatch object.</para>
//        /// </summary>
//    public void NewImage() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new stencil.</para>
//        /// </summary>
//        /// <param name="stencilFunction">Function that draws the stencil.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>myStencil</term><description>Function that defines the new stencil.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewStencil(object stencilFunction) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new drawable Text object.</para>
//        /// </summary>
//        /// <param name="font">The font to use for the text.</param>
//        /// <param name="textstring">The initial string of text that the new Text object will contain. May be nil.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>text</term><description>The new drawable Text object.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewText(object font, string textstring = "nil") => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new drawable Text object.</para>
//        /// </summary>
//        /// <param name="font">The font to use for the text.</param>
//        /// <param name="coloredtext">
//        /// A table containing colors and strings to add to the object, in the form of .
//        /// <list type="bullet">
//        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
//        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
//        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>text</term><description>The new drawable Text object.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewText(object font, object coloredtext) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new drawable Video. Currently only Ogg Theora video files are supported.</para>
//        /// </summary>
//        /// <param name="filename">The file path to the Ogg Theora video file.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>video</term><description>A new Video.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewVideo(string filename) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new drawable Video. Currently only Ogg Theora video files are supported.</para>
//        /// </summary>
//        /// <param name="videostream">A video stream object.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>video</term><description>A new Video.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewVideo(object videostream) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new drawable Video. Currently only Ogg Theora video files are supported.</para>
//        /// </summary>
//        /// <param name="filename">The file path to the Ogg Theora video file (or ).</param>
//        /// <param name="settings">
//        /// A table containing the following fields:
//        /// <list type="bullet">
//        /// <item><term>audio</term><description>boolean: Whether to try to load the video's audio into an audio . If not explicitly set to true or false, it will try without causing an error if the video has no audio.</description></item>
//        /// <item><term>dpiscale</term><description>number: The DPI scale factor of the video.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>video</term><description>A new Video.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewVideo(string filename, object settings) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new drawable Video. Currently only Ogg Theora video files are supported.</para>
//        /// </summary>
//    public void NewVideo() => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates a new volume (3D) Image.</para>
//        /// <para>Volume images are 3D textures with width, height, and depth. They can't be rendered directly, they can only be used in Shader code (and sent to the shader via Shader:send). Or, more accurately, the default shaders can't render volume images, and you need to make one yourself to do it.</para>
//        /// <para>To use a volume image in a Shader, it must be declared as a VolumeImage or sampler3D type (instead of Image or sampler2D). The Texel(VolumeImage image, vec3 texcoords) shader function must be used to get pixel colors from the volume image. The vec3 argument is a normalized texture coordinate with the z component representing the depth to sample at (ranging from [0, 1]).</para>
//        /// <para>Volume images are typically used as lookup tables in shaders for color grading, for example, because sampling using a texture coordinate that is partway in between two pixels can interpolate across all 3 dimensions in the volume image, resulting in a smooth gradient even when a small-sized volume image is used as the lookup table.</para>
//        /// <para>Array images are a much better choice than volume images for storing multiple different sprites in a single array image for directly drawing them.</para>
//        /// </summary>
//        /// <param name="layers">A table containing filepaths to images (or , , , or objects), in an array. A table of tables can also be given, where each sub-table represents a single mipmap level and contains all layers for that mipmap.</param>
//        /// <param name="settings">
//        /// Optional table of settings to configure the volume image, containing the following fields:
//        /// <list type="bullet">
//        /// <item><term>mipmaps</term><description>boolean: True to make the image use mipmaps, false to disable them. Mipmaps will be automatically generated if the image isn't a format.</description></item>
//        /// <item><term>linear</term><description>boolean: True to treat the image's pixels as linear instead of sRGB, when is enabled. Most images are authored as sRGB.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>image</term><description>A volume Image object.</description></item>
//        /// </list>
//        /// </returns>
//    public object NewVolumeImage(object layers, object settings = null) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates and sets a new Font.</para>
//        /// </summary>
//        /// <param name="size">The size of the font.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>The new font.</description></item>
//        /// </list>
//        /// </returns>
//    public object SetNewFont(double size = 12) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates and sets a new Font.</para>
//        /// </summary>
//        /// <param name="filename">The path and name of the file with the font.</param>
//        /// <param name="size">The size of the font.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>The new font.</description></item>
//        /// </list>
//        /// </returns>
//    public object SetNewFont(string filename, double size = 12) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates and sets a new Font.</para>
//        /// </summary>
//        /// <param name="file">A with the font.</param>
//        /// <param name="size">The size of the font.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>The new font.</description></item>
//        /// </list>
//        /// </returns>
//    public object SetNewFont(object file, double size = 12) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates and sets a new Font.</para>
//        /// </summary>
//        /// <param name="data">A with the font.</param>
//        /// <param name="size">The size of the font.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>The new font.</description></item>
//        /// </list>
//        /// </returns>
//    public object SetNewFont(object data, double size = 12) => throw new NotImplementedException();

///// <summary>
//        /// <para>Creates and sets a new Font.</para>
//        /// </summary>
//        /// <param name="rasterizer">A rasterizer.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>The new font.</description></item>
//        /// </list>
//        /// </returns>
//    public object SetNewFont(object rasterizer) => throw new NotImplementedException();

///// <summary>
//        /// <para>Validates shader code. Check if specified shader code does not contain any errors.</para>
//        /// </summary>
//        /// <param name="gles">Validate code as GLSL ES shader.</param>
//        /// <param name="code">The pixel shader or vertex shader code, or a filename pointing to a file with the code.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>status</term><description>if specified shader code doesn't contain any errors. otherwise.</description></item>
//        /// <item><term>message</term><description>Reason why shader code validation failed (or if validation succeded).</description></item>
//        /// </list>
//        /// </returns>
//    public (bool status, string message) ValidateShader(bool gles, string code) => throw new NotImplementedException();

///// <summary>
//        /// <para>Validates shader code. Check if specified shader code does not contain any errors.</para>
//        /// </summary>
//        /// <param name="gles">Validate code as GLSL ES shader.</param>
//        /// <param name="pixelcode">The pixel shader code, or a filename pointing to a file with the code.</param>
//        /// <param name="vertexcode">The vertex shader code, or a filename pointing to a file with the code.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>status</term><description>if specified shader code doesn't contain any errors. otherwise.</description></item>
//        /// <item><term>message</term><description>Reason why shader code validation failed (or if validation succeded).</description></item>
//        /// </list>
//        /// </returns>
//    public (bool status, string message) ValidateShader(bool gles, string pixelcode, string vertexcode) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current background color.</para>
//        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="r">The red component (0-1).</param>
//        /// <param name="g">The green component (0-1).</param>
//        /// <param name="b">The blue component (0-1).</param>
//        /// <param name="a">The alpha component (0-1).</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>r</term><description>The red component (0-1).</description></item>
//        /// <item><term>g</term><description>The green component (0-1).</description></item>
//        /// <item><term>b</term><description>The blue component (0-1).</description></item>
//        /// <item><term>a</term><description>The alpha component (0-1).</description></item>
//        /// </list>
//        /// </returns>
//    public (double r, double g, double b, double a) GetBackgroundColor(double r, double g, double b, double a) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current target Canvas.</para>
//        /// </summary>
//        /// <param name="canvas">The Canvas set by . Returns if drawing to the screen.</param>
//        /// <param name="">Additional canvases if more than one was set.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>canvas</term><description>The Canvas set by . Returns if drawing to the screen.</description></item>
//        /// <item><term></term><description>Additional canvases if more than one was set.</description></item>
//        /// </list>
//        /// </returns>
//    public (object canvas, object) GetCanvas(object canvas, object) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current color.</para>
//        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="r">The red component (0-1).</param>
//        /// <param name="g">The green component (0-1).</param>
//        /// <param name="b">The blue component (0-1).</param>
//        /// <param name="a">The alpha component (0-1).</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>r</term><description>The red component (0-1).</description></item>
//        /// <item><term>g</term><description>The green component (0-1).</description></item>
//        /// <item><term>b</term><description>The blue component (0-1).</description></item>
//        /// <item><term>a</term><description>The alpha component (0-1).</description></item>
//        /// </list>
//        /// </returns>
//    public (double r, double g, double b, double a) GetColor(double r, double g, double b, double a) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the active color components used when drawing. Normally all 4 components are active unless love.graphics.setColorMask has been used.</para>
//        /// <para>The color mask determines whether individual components of the colors of drawn objects will affect the color of the screen. They affect love.graphics.clear and Canvas:clear as well.</para>
//        /// </summary>
//        /// <param name="r">Whether the red color component is active when rendering.</param>
//        /// <param name="g">Whether the green color component is active when rendering.</param>
//        /// <param name="b">Whether the blue color component is active when rendering.</param>
//        /// <param name="a">Whether the alpha color component is active when rendering.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>r</term><description>Whether the red color component is active when rendering.</description></item>
//        /// <item><term>g</term><description>Whether the green color component is active when rendering.</description></item>
//        /// <item><term>b</term><description>Whether the blue color component is active when rendering.</description></item>
//        /// <item><term>a</term><description>Whether the alpha color component is active when rendering.</description></item>
//        /// </list>
//        /// </returns>
//    public (bool r, bool g, bool b, bool a) GetColorMask(bool r, bool g, bool b, bool a) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the color mode (which controls how images are affected by the current color).</para>
//        /// </summary>
//        /// <param name="mode">The current color mode.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>mode</term><description>The current color mode.</description></item>
//        /// </list>
//        /// </returns>
//    public ColorMode GetColorMode(ColorMode mode) => throw new NotImplementedException();

///// <summary>
//        /// <para>Returns the default scaling filters used with Images, Canvases, and Fonts.</para>
//        /// </summary>
//        /// <param name="min">Filter mode used when scaling the image down.</param>
//        /// <param name="mag">Filter mode used when scaling the image up.</param>
//        /// <param name="anisotropy">Maximum amount of Anisotropic Filtering used.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>min</term><description>Filter mode used when scaling the image down.</description></item>
//        /// <item><term>mag</term><description>Filter mode used when scaling the image up.</description></item>
//        /// <item><term>anisotropy</term><description>Maximum amount of Anisotropic Filtering used.</description></item>
//        /// </list>
//        /// </returns>
//    public (FilterMode min, FilterMode mag, double anisotropy) GetDefaultFilter(FilterMode min, FilterMode mag, double anisotropy) => throw new NotImplementedException();

///// <summary>
//        /// <para>Returns the default scaling filters.</para>
//        /// </summary>
//        /// <param name="min">Filter mode used when scaling the image down.</param>
//        /// <param name="mag">Filter mode used when scaling the image up.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>min</term><description>Filter mode used when scaling the image down.</description></item>
//        /// <item><term>mag</term><description>Filter mode used when scaling the image up.</description></item>
//        /// </list>
//        /// </returns>
//    public (FilterMode min, FilterMode mag) GetDefaultImageFilter(FilterMode min, FilterMode mag) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current depth test mode and whether writing to the depth buffer is enabled.</para>
//        /// <para>This is low-level functionality designed for use with custom vertex shaders and Meshes with custom vertex attributes. No higher level APIs are provided to set the depth of 2D graphics such as shapes, lines, and Images.</para>
//        /// </summary>
//        /// <param name="comparemode">Depth comparison mode used for depth testing.</param>
//        /// <param name="write">Whether to write update / write values to the depth buffer when rendering.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>comparemode</term><description>Depth comparison mode used for depth testing.</description></item>
//        /// <item><term>write</term><description>Whether to write update / write values to the depth buffer when rendering.</description></item>
//        /// </list>
//        /// </returns>
//    public (CompareMode comparemode, bool write) GetDepthMode(CompareMode comparemode, bool write) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current Font object.</para>
//        /// </summary>
//        /// <param name="font">The current Font. Automatically creates and sets the default font, if none is set yet.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>font</term><description>The current Font. Automatically creates and sets the default font, if none is set yet.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetFont(object font) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets whether triangles with clockwise- or counterclockwise-ordered vertices are considered front-facing.</para>
//        /// <para>This is designed for use in combination with Mesh face culling. Other love.graphics shapes, lines, and sprites are not guaranteed to have a specific winding order to their internal vertices.</para>
//        /// </summary>
//        /// <param name="winding">The winding mode being used. The default winding is counterclockwise ("ccw").</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>winding</term><description>The winding mode being used. The default winding is counterclockwise ("ccw").</description></item>
//        /// </list>
//        /// </returns>
//    public VertexWinding GetFrontFaceWinding(VertexWinding winding) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the line join style.</para>
//        /// </summary>
//        /// <param name="join">The LineJoin style.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>join</term><description>The LineJoin style.</description></item>
//        /// </list>
//        /// </returns>
//    public LineJoin GetLineJoin(LineJoin join) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the line style.</para>
//        /// </summary>
//        /// <param name="style">The current line style.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>style</term><description>The current line style.</description></item>
//        /// </list>
//        /// </returns>
//    public LineStyle GetLineStyle(LineStyle style) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current line width.</para>
//        /// </summary>
//        /// <param name="width">The current line width.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>width</term><description>The current line width.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetLineWidth(double width) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current line width.</para>
//        /// </summary>
//    public void GetLineWidth() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets whether back-facing triangles in a Mesh are culled.</para>
//        /// <para>Mesh face culling is designed for use with low level custom hardware-accelerated 3D rendering via custom vertex attributes on Meshes, custom vertex shaders, and depth testing with a depth buffer.</para>
//        /// </summary>
//        /// <param name="mode">The Mesh face culling mode in use (whether to render everything, cull back-facing triangles, or cull front-facing triangles).</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>mode</term><description>The Mesh face culling mode in use (whether to render everything, cull back-facing triangles, or cull front-facing triangles).</description></item>
//        /// </list>
//        /// </returns>
//    public CullMode GetMeshCullMode(CullMode mode) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets whether triangles with clockwise- or counterclockwise-ordered vertices are considered front-facing.</para>
//        /// <para>This is designed for use in combination with Mesh face culling. Other love.graphics shapes, lines, and sprites are not guaranteed to have a specific winding order to their internal vertices.</para>
//        /// </summary>
//        /// <param name="winding">The winding mode to use. The default winding is counterclockwise ("ccw").</param>
//    public void SetFrontFaceWinding(VertexWinding winding) => throw new NotImplementedException();

///// <summary>
//        /// <para>Returns the current PixelEffect. Returns nil if none is set.</para>
//        /// </summary>
//        /// <param name="pe">The current PixelEffect.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pe</term><description>The current PixelEffect.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetPixelEffect(object pe) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the point size.</para>
//        /// </summary>
//        /// <param name="size">The current point size.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>size</term><description>The current point size.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetPointSize(double size) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the point size.</para>
//        /// </summary>
//    public void GetPointSize() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current scissor box.</para>
//        /// </summary>
//        /// <param name="x">The x-component of the top-left point of the box.</param>
//        /// <param name="y">The y-component of the top-left point of the box.</param>
//        /// <param name="width">The width of the box.</param>
//        /// <param name="height">The height of the box.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>x</term><description>The x-component of the top-left point of the box.</description></item>
//        /// <item><term>y</term><description>The y-component of the top-left point of the box.</description></item>
//        /// <item><term>width</term><description>The width of the box.</description></item>
//        /// <item><term>height</term><description>The height of the box.</description></item>
//        /// </list>
//        /// </returns>
//    public (double x, double y, double width, double height) GetScissor(double x, double y, double width, double height) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current Shader. Returns nil if none is set.</para>
//        /// </summary>
//        /// <param name="shader">The currently active Shader, or if none is set.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>shader</term><description>The currently active Shader, or if none is set.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetShader(object shader) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current depth of the transform / state stack (the number of pushes without corresponding pops).</para>
//        /// </summary>
//        /// <param name="depth">The current depth of the transform and state love.graphics stack.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>depth</term><description>The current depth of the transform and state love.graphics stack.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetStackDepth(double depth) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current depth of the transform / state stack (the number of pushes without corresponding pops).</para>
//        /// </summary>
//    public void GetStackDepth() => throw new NotImplementedException();

///// <summary>
//        /// <para>Copies and pushes the current coordinate transformation to the transformation stack.</para>
//        /// <para>This function is always used to prepare for a corresponding pop operation later. It stores the current coordinate transformation state into the transformation stack and keeps it active. Later changes to the transformation can be undone by using the pop operation, which returns the coordinate transform to the state it was in before calling push.</para>
//        /// </summary>
//    public void Push() => throw new NotImplementedException();

///// <summary>
//        /// <para>Copies and pushes the current coordinate transformation to the transformation stack.</para>
//        /// <para>This function is always used to prepare for a corresponding pop operation later. It stores the current coordinate transformation state into the transformation stack and keeps it active. Later changes to the transformation can be undone by using the pop operation, which returns the coordinate transform to the state it was in before calling push.</para>
//        /// </summary>
//    public void Push() => throw new NotImplementedException();

///// <summary>
//        /// <para>Pops the current coordinate transformation from the transformation stack.</para>
//        /// <para>This function is always used to reverse a previous push operation. It returns the current transformation state to what it was before the last preceding push.</para>
//        /// </summary>
//    public void Pop() => throw new NotImplementedException();

///// <summary>
//        /// <para>Pops the current coordinate transformation from the transformation stack.</para>
//        /// <para>This function is always used to reverse a previous push operation. It returns the current transformation state to what it was before the last preceding push.</para>
//        /// </summary>
//    public void Push() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the current stencil test configuration.</para>
//        /// <para>When stencil testing is enabled, the geometry of everything that is drawn afterward will be clipped / stencilled out based on a comparison between the arguments of this function and the stencil value of each pixel that the geometry touches. The stencil values of pixels are affected via love.graphics.stencil.</para>
//        /// <para>Each Canvas has its own per-pixel stencil values.</para>
//        /// </summary>
//        /// <param name="comparemode">The type of comparison that is made for each pixel. Will be "always" if stencil testing is disabled.</param>
//        /// <param name="comparevalue">The value used when comparing with the stencil value of each pixel.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>comparemode</term><description>The type of comparison that is made for each pixel. Will be "always" if stencil testing is disabled.</description></item>
//        /// <item><term>comparevalue</term><description>The value used when comparing with the stencil value of each pixel.</description></item>
//        /// </list>
//        /// </returns>
//    public (CompareMode comparemode, double comparevalue) GetStencilTest(CompareMode comparemode, double comparevalue) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the scissor to the rectangle created by the intersection of the specified rectangle with the existing scissor.  If no scissor is active yet, it behaves like love.graphics.setScissor.</para>
//        /// <para>The scissor limits the drawing area to a specified rectangle. This affects all graphics calls, including love.graphics.clear.</para>
//        /// <para>The dimensions of the scissor is unaffected by graphical transformations (translate, scale, ...).</para>
//        /// </summary>
//        /// <param name="x">The x-coordinate of the upper left corner of the rectangle to intersect with the existing scissor rectangle.</param>
//        /// <param name="y">The y-coordinate of the upper left corner of the rectangle to intersect with the existing scissor rectangle.</param>
//        /// <param name="width">The width of the rectangle to intersect with the existing scissor rectangle.</param>
//        /// <param name="height">The height of the rectangle to intersect with the existing scissor rectangle.</param>
//    public void IntersectScissor(double x, double y, double width, double height) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the scissor to the rectangle created by the intersection of the specified rectangle with the existing scissor.  If no scissor is active yet, it behaves like love.graphics.setScissor.</para>
//        /// <para>The scissor limits the drawing area to a specified rectangle. This affects all graphics calls, including love.graphics.clear.</para>
//        /// <para>The dimensions of the scissor is unaffected by graphical transformations (translate, scale, ...).</para>
//        /// </summary>
//    public void GetDimensions() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or disables scissor.</para>
//        /// <para>The scissor limits the drawing area to a specified rectangle. This affects all graphics calls, including love.graphics.clear.</para>
//        /// <para>The dimensions of the scissor are unaffected by graphical transformations (translate, scale, ...), and it operates on whole pixels rather than having sub-pixel precision.</para>
//        /// </summary>
//        /// <param name="x">The x-coordinate of the upper left corner of the clipping rectangle.</param>
//        /// <param name="y">The y-coordinate of the upper left corner of the clipping rectangle.</param>
//        /// <param name="width">The width of the clipping rectangle.</param>
//        /// <param name="height">The height of the clipping rectangle.</param>
//    public void SetScissor(double x, double y, double width, double height) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or disables scissor.</para>
//        /// <para>The scissor limits the drawing area to a specified rectangle. This affects all graphics calls, including love.graphics.clear.</para>
//        /// <para>The dimensions of the scissor are unaffected by graphical transformations (translate, scale, ...), and it operates on whole pixels rather than having sub-pixel precision.</para>
//        /// </summary>
//    public void SetScissor() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or disables scissor.</para>
//        /// <para>The scissor limits the drawing area to a specified rectangle. This affects all graphics calls, including love.graphics.clear.</para>
//        /// <para>The dimensions of the scissor are unaffected by graphical transformations (translate, scale, ...), and it operates on whole pixels rather than having sub-pixel precision.</para>
//        /// </summary>
//    public void GetDimensions() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets whether the graphics module is able to be used. If it is not active, love.graphics function and method calls will not work correctly and may cause the program to crash.</para>
//        /// <para>The graphics module is inactive if a window is not open, or if the app is in the background on iOS. Typically the app's execution will be automatically paused by the system, in the latter case.</para>
//        /// </summary>
//        /// <param name="active">Whether the graphics module is active and able to be used.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>active</term><description>Whether the graphics module is active and able to be used.</description></item>
//        /// </list>
//        /// </returns>
//    public bool IsActive(bool active) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets whether gamma-correct rendering is supported and enabled. It can be enabled by setting t.gammacorrect = true in love.conf.</para>
//        /// <para>Not all devices support gamma-correct rendering, in which case it will be automatically disabled and this function will return false. It is supported on desktop systems which have graphics cards that are capable of using OpenGL 3 / DirectX 10, and iOS devices that can use OpenGL ES 3.</para>
//        /// </summary>
//        /// <param name="gammacorrect">True if gamma-correct rendering is supported and was enabled in , false otherwise.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>gammacorrect</term><description>True if gamma-correct rendering is supported and was enabled in , false otherwise.</description></item>
//        /// </list>
//        /// </returns>
//    public bool IsGammaCorrect(bool gammacorrect) => throw new NotImplementedException();

///// <summary>
//        /// <para>Checks if certain graphics functions can be used.</para>
//        /// <para>Older and low-end systems do not always support all graphics extensions.</para>
//        /// </summary>
//        /// <param name="supportN">The graphics feature to check for.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>isSupported</term><description>True if everything is supported, false otherwise.</description></item>
//        /// </list>
//        /// </returns>
//    public bool IsSupported(GraphicsFeature supportN) => throw new NotImplementedException();

///// <summary>
//        /// <para>Checks if certain graphics functions can be used.</para>
//        /// <para>Older and low-end systems do not always support all graphics extensions.</para>
//        /// </summary>
//    public void IsSupported() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets whether wireframe mode is used when drawing.</para>
//        /// </summary>
//        /// <param name="wireframe">True if wireframe lines are used when drawing, false if it's not.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>wireframe</term><description>True if wireframe lines are used when drawing, false if it's not.</description></item>
//        /// </list>
//        /// </returns>
//    public bool IsWireframe(bool wireframe) => throw new NotImplementedException();

///// <summary>
//        /// <para>Resets the current graphics settings.</para>
//        /// <para>Calling reset makes the current drawing color white, the current background color black, disables any active Canvas or Shader, and removes any scissor settings. It sets the BlendMode to alpha, enables all color component masks, disables wireframe mode and resets the current graphics transformation to the origin. It also sets both the point and line drawing modes to smooth and their sizes to 1.0.</para>
//        /// </summary>
//    public void Reset() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the background color.</para>
//        /// </summary>
//        /// <param name="red">The red component (0-1).</param>
//        /// <param name="green">The green component (0-1).</param>
//        /// <param name="blue">The blue component (0-1).</param>
//    public void SetBackgroundColor(double red, double green, double blue) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the background color.</para>
//        /// </summary>
//        /// <param name="rgb">A numerical indexed table with the red, green and blue values as .</param>
//    public void SetBackgroundColor(object rgb) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the background color.</para>
//        /// </summary>
//        /// <param name="rgba">A numerical indexed table with the red, green, blue and alpha values as .</param>
//    public void SetBackgroundColor(object rgba) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the background color.</para>
//        /// </summary>
//    public void ColorFromBytes() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the blending mode.</para>
//        /// </summary>
//        /// <param name="mode">The blend mode to use.</param>
//    public void SetBlendMode(BlendMode mode) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the blending mode.</para>
//        /// </summary>
//    public void SetBackgroundColor() => throw new NotImplementedException();

///// <summary>
//        /// <para>Captures drawing operations to a Canvas.</para>
//        /// </summary>
//        /// <param name="canvas">The new target.</param>
//    public void SetCanvas(object canvas) => throw new NotImplementedException();

///// <summary>
//        /// <para>Captures drawing operations to a Canvas.</para>
//        /// </summary>
//        /// <param name="canvas1">The first render target.</param>
//        /// <param name="canvas2">The second render target.</param>
//        /// <param name="">More canvases.</param>
//    public void SetCanvas(object canvas1, object canvas2, object) => throw new NotImplementedException();

///// <summary>
//        /// <para>Captures drawing operations to a Canvas.</para>
//        /// </summary>
//        /// <param name="canvas">The new render target.</param>
//        /// <param name="slice">For cubemaps this is the cube face index to render to (between 1 and 6). For Array textures this is the . For volume textures this is the depth slice. 2D canvases should use a value of 1.</param>
//        /// <param name="mipmap">The mipmap level to render to, for Canvases with .</param>
//    public void SetCanvas(object canvas, double slice, double mipmap = 1) => throw new NotImplementedException();

///// <summary>
//        /// <para>Captures drawing operations to a Canvas.</para>
//        /// </summary>
//        /// <param name="setup">
//        /// A table specifying the active Canvas(es), their mipmap levels and active layers if applicable, and whether to use a stencil and/or depth buffer.
//        /// <list type="bullet">
//        /// <item><term></term><description>RenderTargetSetup [1]: The Canvas to render to.</description></item>
//        /// <item><term></term><description>RenderTargetSetup [2] (nil): An additional Canvas to render to, if multiple simultaneous render targets are wanted.</description></item>
//        /// <item><term></term><description>RenderTargetSetup ...: Additional Canvases to render to, if multiple simultaneous render targets are wanted.</description></item>
//        /// <item><term>stencil</term><description>boolean: Whether an internally managed stencil buffer should be used, if the field isn't set.</description></item>
//        /// <item><term>depth</term><description>boolean: Whether an internally managed depth buffer should be used, if the field isn't set.</description></item>
//        /// <item><term>depthstencil</term><description>RenderTargetSetup: An optional custom depth/stencil Canvas to use for the depth and/or stencil buffer.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term></term><description>The Canvas to use for this active render target.</description></item>
//        /// <item><term>mipmap</term><description>The mipmap level to render to, for Canvases with .</description></item>
//        /// <item><term>layer</term><description>Only used for -type Canvases. For Array textures this is the to render to. For volume textures this is the depth slice.</description></item>
//        /// <item><term>face</term><description>Only used for -type Canvases. The cube face index to render to (between 1 and 6)</description></item>
//        /// </list>
//        /// </returns>
//    public (object, double mipmap, double layer, double face) SetCanvas(object setup) => throw new NotImplementedException();

///// <summary>
//        /// <para>Captures drawing operations to a Canvas.</para>
//        /// </summary>
//    public void NewCanvas() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the color used for drawing. The color is used for all subsequent draw operations.</para>
//        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="red">The amount of red.</param>
//        /// <param name="green">The amount of green.</param>
//        /// <param name="blue">The amount of blue.</param>
//        /// <param name="alpha">The amount of alpha.</param>
//    public void SetColor(double red, double green, double blue, double alpha = 1) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the color used for drawing. The color is used for all subsequent draw operations.</para>
//        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
//        /// </summary>
//        /// <param name="rgba">A numerical indexed table with the red, green, blue and alpha values as . The alpha is optional and defaults to 1 if it is left out.</param>
//    public void SetColor(object rgba) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the color mask. Enables or disables specific color components when rendering and clearing the screen. For example, if red is set to false, no further changes will be made to the red component of any pixels.</para>
//        /// </summary>
//        /// <param name="red">Render red component.</param>
//        /// <param name="green">Render green component.</param>
//        /// <param name="blue">Render blue component.</param>
//        /// <param name="alpha">Render alpha component.</param>
//    public void SetColorMask(bool red, bool green, bool blue, bool alpha) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the color mask. Enables or disables specific color components when rendering and clearing the screen. For example, if red is set to false, no further changes will be made to the red component of any pixels.</para>
//        /// </summary>
//    public void SetColorMask() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the color mode (which controls how images are affected by the current color).</para>
//        /// </summary>
//        /// <param name="mode">The color mode to use.</param>
//    public void SetColorMode(ColorMode mode) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the default scaling filters used with Images, Canvases, and Fonts.</para>
//        /// </summary>
//        /// <param name="min">Filter mode used when scaling the image down.</param>
//        /// <param name="mag">Filter mode used when scaling the image up.</param>
//        /// <param name="anisotropy">Maximum amount of Anisotropic Filtering used.</param>
//    public void SetDefaultFilter(FilterMode min, FilterMode mag = min, double anisotropy = 1) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the default scaling filters.</para>
//        /// </summary>
//        /// <param name="min">Filter mode used when scaling the image down.</param>
//        /// <param name="mag">Filter mode used when scaling the image up.</param>
//    public void SetDefaultImageFilter(FilterMode min, FilterMode mag) => throw new NotImplementedException();

///// <summary>
//        /// <para>Configures depth testing and writing to the depth buffer.</para>
//        /// <para>This is low-level functionality designed for use with custom vertex shaders and Meshes with custom vertex attributes. No higher level APIs are provided to set the depth of 2D graphics such as shapes, lines, and Images.</para>
//        /// </summary>
//        /// <param name="comparemode">Depth comparison mode used for depth testing.</param>
//        /// <param name="write">Whether to write update / write values to the depth buffer when rendering.</param>
//    public void SetDepthMode(CompareMode comparemode, bool write) => throw new NotImplementedException();

///// <summary>
//        /// <para>Configures depth testing and writing to the depth buffer.</para>
//        /// <para>This is low-level functionality designed for use with custom vertex shaders and Meshes with custom vertex attributes. No higher level APIs are provided to set the depth of 2D graphics such as shapes, lines, and Images.</para>
//        /// </summary>
//    public void SetDepthMode() => throw new NotImplementedException();

///// <summary>
//        /// <para>Defines an inverted stencil for the drawing operations or releases the active one.</para>
//        /// <para>It's the same as love.graphics.setStencil with the mask inverted.</para>
//        /// </summary>
//        /// <param name="stencilFunction">Function that draws the stencil.</param>
//    public void SetInvertedStencil(object stencilFunction) => throw new NotImplementedException();

///// <summary>
//        /// <para>Defines an inverted stencil for the drawing operations or releases the active one.</para>
//        /// <para>It's the same as love.graphics.setStencil with the mask inverted.</para>
//        /// </summary>
//    public void SetInvertedStencil() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the line join style. See LineJoin for the possible options.</para>
//        /// </summary>
//        /// <param name="join">The LineJoin to use.</param>
//    public void SetLineJoin(LineJoin join) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the line join style. See LineJoin for the possible options.</para>
//        /// </summary>
//    public void SetLineWidth() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the line style.</para>
//        /// </summary>
//        /// <param name="style">The LineStyle to use. Line styles include and .</param>
//    public void SetLineStyle(LineStyle style) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the line style.</para>
//        /// </summary>
//    public void SetLineStyle() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the line width.</para>
//        /// </summary>
//        /// <param name="width">The width of the line.</param>
//    public void SetLineWidth(double width) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets whether back-facing triangles in a Mesh are culled.</para>
//        /// <para>This is designed for use with low level custom hardware-accelerated 3D rendering via custom vertex attributes on Meshes, custom vertex shaders, and depth testing with a depth buffer.</para>
//        /// <para>By default, both front- and back-facing triangles in Meshes are rendered.</para>
//        /// </summary>
//        /// <param name="mode">The Mesh face culling mode to use (whether to render everything, cull back-facing triangles, or cull front-facing triangles).</param>
//    public void SetMeshCullMode(CullMode mode) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a PixelEffect as the current pixel effect. All drawing operations until the next love.graphics.setPixelEffect will be drawn using the PixelEffect object specified.</para>
//        /// </summary>
//        /// <param name="pixeleffect">The new pixel effect.</param>
//    public void SetPixelEffect(object pixeleffect) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a PixelEffect as the current pixel effect. All drawing operations until the next love.graphics.setPixelEffect will be drawn using the PixelEffect object specified.</para>
//        /// </summary>
//    public void SetPixelEffect() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a PixelEffect as the current pixel effect. All drawing operations until the next love.graphics.setPixelEffect will be drawn using the PixelEffect object specified.</para>
//        /// </summary>
//    public void SetPixelEffect() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the point size.</para>
//        /// </summary>
//        /// <param name="size">The new point size.</param>
//    public void SetPointSize(double size) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets the point size.</para>
//        /// </summary>
//    public void GetPointSize() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a Framebuffer as render target. All drawing operations until the next love.graphics.setRenderTarget will be directed to the Framebuffer object specified.</para>
//        /// </summary>
//        /// <param name="framebuffer">The new render target.</param>
//    public void SetRenderTarget(object framebuffer) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a Framebuffer as render target. All drawing operations until the next love.graphics.setRenderTarget will be directed to the Framebuffer object specified.</para>
//        /// </summary>
//    public void SetRenderTarget() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a Framebuffer as render target. All drawing operations until the next love.graphics.setRenderTarget will be directed to the Framebuffer object specified.</para>
//        /// </summary>
//    public void SetRenderTarget() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a Shader as the current pixel effect or vertex shaders. All drawing operations until the next love.graphics.setShader will be drawn using the Shader object specified.</para>
//        /// </summary>
//        /// <param name="shader">The new shader.</param>
//    public void SetShader(object shader) => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a Shader as the current pixel effect or vertex shaders. All drawing operations until the next love.graphics.setShader will be drawn using the Shader object specified.</para>
//        /// </summary>
//    public void SetShader() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets or resets a Shader as the current pixel effect or vertex shaders. All drawing operations until the next love.graphics.setShader will be drawn using the Shader object specified.</para>
//        /// </summary>
//    public void SetShader() => throw new NotImplementedException();

///// <summary>
//        /// <para>Defines or releases a stencil for the drawing operations.</para>
//        /// <para>The passed function draws to the stencil instead of the screen, creating an image with transparent and opaque pixels. While active, it is used to test where pixels will be drawn or discarded. Image contents do not directly affect the stencil, but see below for a workaround.</para>
//        /// <para>Calling the function without arguments releases the active stencil.</para>
//        /// </summary>
//        /// <param name="stencilFunction">Function that draws the stencil.</param>
//    public void SetStencil(object stencilFunction) => throw new NotImplementedException();

///// <summary>
//        /// <para>Defines or releases a stencil for the drawing operations.</para>
//        /// <para>The passed function draws to the stencil instead of the screen, creating an image with transparent and opaque pixels. While active, it is used to test where pixels will be drawn or discarded. Image contents do not directly affect the stencil, but see below for a workaround.</para>
//        /// <para>Calling the function without arguments releases the active stencil.</para>
//        /// </summary>
//    public void SetStencil() => throw new NotImplementedException();

///// <summary>
//        /// <para>Defines or releases a stencil for the drawing operations.</para>
//        /// <para>The passed function draws to the stencil instead of the screen, creating an image with transparent and opaque pixels. While active, it is used to test where pixels will be drawn or discarded. Image contents do not directly affect the stencil, but see below for a workaround.</para>
//        /// <para>Calling the function without arguments releases the active stencil.</para>
//        /// </summary>
//    public void Rectangle() => throw new NotImplementedException();

///// <summary>
//        /// <para>Configures or disables stencil testing.</para>
//        /// <para>When stencil testing is enabled, the geometry of everything that is drawn afterward will be clipped / stencilled out based on a comparison between the arguments of this function and the stencil value of each pixel that the geometry touches. The stencil values of pixels are affected via love.graphics.stencil.</para>
//        /// </summary>
//        /// <param name="comparemode">The type of comparison to make for each pixel.</param>
//        /// <param name="comparevalue">The value to use when comparing with the stencil value of each pixel. Must be between 0 and 255.</param>
//    public void SetStencilTest(CompareMode comparemode, double comparevalue) => throw new NotImplementedException();

///// <summary>
//        /// <para>Configures or disables stencil testing.</para>
//        /// <para>When stencil testing is enabled, the geometry of everything that is drawn afterward will be clipped / stencilled out based on a comparison between the arguments of this function and the stencil value of each pixel that the geometry touches. The stencil values of pixels are affected via love.graphics.stencil.</para>
//        /// </summary>
//    public void SetStencilTest() => throw new NotImplementedException();

///// <summary>
//        /// <para>Configures or disables stencil testing.</para>
//        /// <para>When stencil testing is enabled, the geometry of everything that is drawn afterward will be clipped / stencilled out based on a comparison between the arguments of this function and the stencil value of each pixel that the geometry touches. The stencil values of pixels are affected via love.graphics.stencil.</para>
//        /// </summary>
//    public void Rectangle() => throw new NotImplementedException();

///// <summary>
//        /// <para>Sets whether wireframe lines will be used when drawing.</para>
//        /// </summary>
//        /// <param name="enable">True to enable wireframe mode when drawing, false to disable it.</param>
//    public void SetWireframe(bool enable) => throw new NotImplementedException();

///// <summary>
//        /// <para>Applies the given Transform object to the current coordinate transformation.</para>
//        /// <para>This effectively multiplies the existing coordinate transformation's matrix with the Transform object's internal matrix to produce the new coordinate transformation.</para>
//        /// </summary>
//        /// <param name="transform">The Transform object to apply to the current graphics coordinate transform.</param>
//    public void ApplyTransform(object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Applies the given Transform object to the current coordinate transformation.</para>
//        /// <para>This effectively multiplies the existing coordinate transformation's matrix with the Transform object's internal matrix to produce the new coordinate transformation.</para>
//        /// </summary>
//    public void NewTransform() => throw new NotImplementedException();

///// <summary>
//        /// <para>Converts the given 2D position from screen-space into global coordinates.</para>
//        /// <para>This effectively applies the reverse of the current graphics transformations to the given position. A similar Transform:inverseTransformPoint method exists for Transform objects.</para>
//        /// </summary>
//        /// <param name="screenX">The x component of the screen-space position.</param>
//        /// <param name="screenY">The y component of the screen-space position.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>globalX</term><description>The x component of the position in global coordinates.</description></item>
//        /// <item><term>globalY</term><description>The y component of the position in global coordinates.</description></item>
//        /// </list>
//        /// </returns>
//    public (double globalX, double globalY) InverseTransformPoint(double screenX, double screenY) => throw new NotImplementedException();

///// <summary>
//        /// <para>Resets the current coordinate transformation.</para>
//        /// <para>This function is always used to reverse any previous calls to love.graphics.rotate, love.graphics.scale, love.graphics.shear or love.graphics.translate. It restores the current transformation to its default state.</para>
//        /// <para>This function is called automatically right before love.draw.</para>
//        /// </summary>
//    public void Origin() => throw new NotImplementedException();

///// <summary>
//        /// <para>Resets the current coordinate transformation.</para>
//        /// <para>This function is always used to reverse any previous calls to love.graphics.rotate, love.graphics.scale, love.graphics.shear or love.graphics.translate. It restores the current transformation to its default state.</para>
//        /// <para>This function is called automatically right before love.draw.</para>
//        /// </summary>
//    public void NewImage() => throw new NotImplementedException();

///// <summary>
//        /// <para>Replaces the current coordinate transformation with the given Transform object.</para>
//        /// </summary>
//        /// <param name="transform">The Transform object to replace the current graphics coordinate transform with.</param>
//    public void ReplaceTransform(object transform) => throw new NotImplementedException();

///// <summary>
//        /// <para>Replaces the current coordinate transformation with the given Transform object.</para>
//        /// </summary>
//    public void NewTransform() => throw new NotImplementedException();

///// <summary>
//        /// <para>Rotates the coordinate system in two dimensions.</para>
//        /// <para>Calling this function affects all future drawing operations by rotating the coordinate system around the origin by the given amount of radians.</para>
//        /// <para>This change lasts until the next love.draw call, or a love.graphics.pop reverts to a previous love.graphics.push, or love.graphics.origin is called - whichever comes first.</para>
//        /// </summary>
//        /// <param name="angle">The amount to rotate the coordinate system in radians.</param>
//    public void Rotate(double angle) => throw new NotImplementedException();

///// <summary>
//        /// <para>Rotates the coordinate system in two dimensions.</para>
//        /// <para>Calling this function affects all future drawing operations by rotating the coordinate system around the origin by the given amount of radians.</para>
//        /// <para>This change lasts until the next love.draw call, or a love.graphics.pop reverts to a previous love.graphics.push, or love.graphics.origin is called - whichever comes first.</para>
//        /// </summary>
//    public void GetDimensions() => throw new NotImplementedException();

///// <summary>
//        /// <para>Scales the coordinate system in two dimensions.</para>
//        /// <para>By default the coordinate system in LÖVE corresponds to the display pixels in horizontal and vertical directions one-to-one, and the x-axis increases towards the right while the y-axis increases downwards. Scaling the coordinate system changes this relation.</para>
//        /// <para>After scaling by sx and sy, all coordinates are treated as if they were multiplied by sx and sy. Every result of a drawing operation is also correspondingly scaled, so scaling by (2, 2) for example would mean making everything twice as large in both x- and y-directions. Scaling by a negative value flips the coordinate system in the corresponding direction, which also means everything will be drawn flipped or upside down, or both. Scaling by zero is not a useful operation.</para>
//        /// <para>Scale and translate are not commutative operations, therefore, calling them in different orders will change the outcome.</para>
//        /// <para>This change lasts until the next love.draw call, or a love.graphics.pop reverts to a previous love.graphics.push, or love.graphics.origin is called - whichever comes first.</para>
//        /// </summary>
//        /// <param name="sx">The scaling in the direction of the x-axis.</param>
//        /// <param name="sy">The scaling in the direction of the y-axis. If omitted, it defaults to same as parameter sx.</param>
//    public void Scale(double sx, double sy = sx) => throw new NotImplementedException();

///// <summary>
//        /// <para>Scales the coordinate system in two dimensions.</para>
//        /// <para>By default the coordinate system in LÖVE corresponds to the display pixels in horizontal and vertical directions one-to-one, and the x-axis increases towards the right while the y-axis increases downwards. Scaling the coordinate system changes this relation.</para>
//        /// <para>After scaling by sx and sy, all coordinates are treated as if they were multiplied by sx and sy. Every result of a drawing operation is also correspondingly scaled, so scaling by (2, 2) for example would mean making everything twice as large in both x- and y-directions. Scaling by a negative value flips the coordinate system in the corresponding direction, which also means everything will be drawn flipped or upside down, or both. Scaling by zero is not a useful operation.</para>
//        /// <para>Scale and translate are not commutative operations, therefore, calling them in different orders will change the outcome.</para>
//        /// <para>This change lasts until the next love.draw call, or a love.graphics.pop reverts to a previous love.graphics.push, or love.graphics.origin is called - whichever comes first.</para>
//        /// </summary>
//    public void Push() => throw new NotImplementedException();

///// <summary>
//        /// <para>Shears the coordinate system.</para>
//        /// <para>This change lasts until the next love.draw call, or a love.graphics.pop reverts to a previous love.graphics.push, or love.graphics.origin is called - whichever comes first.</para>
//        /// </summary>
//        /// <param name="kx">The shear factor on the x-axis.</param>
//        /// <param name="ky">The shear factor on the y-axis.</param>
//    public void Shear(double kx, double ky) => throw new NotImplementedException();

///// <summary>
//        /// <para>Shears the coordinate system.</para>
//        /// <para>This change lasts until the next love.draw call, or a love.graphics.pop reverts to a previous love.graphics.push, or love.graphics.origin is called - whichever comes first.</para>
//        /// </summary>
//    public void Translate() => throw new NotImplementedException();

///// <summary>
//        /// <para>Converts the given 2D position from global coordinates into screen-space.</para>
//        /// <para>This effectively applies the current graphics transformations to the given position. A similar Transform:transformPoint method exists for Transform objects.</para>
//        /// </summary>
//        /// <param name="globalX">The x component of the position in global coordinates.</param>
//        /// <param name="globalY">The y component of the position in global coordinates.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>screenX</term><description>The x component of the position with graphics transformations applied.</description></item>
//        /// <item><term>screenY</term><description>The y component of the position with graphics transformations applied.</description></item>
//        /// </list>
//        /// </returns>
//    public (double screenX, double screenY) TransformPoint(double globalX, double globalY) => throw new NotImplementedException();

///// <summary>
//        /// <para>Translates the coordinate system in two dimensions.</para>
//        /// <para>When this function is called with two numbers, dx, and dy, all the following drawing operations take effect as if their x and y coordinates were x+dx and y+dy.</para>
//        /// <para>Scale and translate are not commutative operations, therefore, calling them in different orders will change the outcome.</para>
//        /// <para>This change lasts until the next love.draw call, or a love.graphics.pop reverts to a previous love.graphics.push, or love.graphics.origin is called - whichever comes first.</para>
//        /// <para>Translating using whole numbers will prevent tearing/blurring of images and fonts draw after translating.</para>
//        /// </summary>
//        /// <param name="dx">The translation relative to the x-axis.</param>
//        /// <param name="dy">The translation relative to the y-axis.</param>
//    public void Translate(double dx, double dy) => throw new NotImplementedException();

///// <summary>
//        /// <para>Translates the coordinate system in two dimensions.</para>
//        /// <para>When this function is called with two numbers, dx, and dy, all the following drawing operations take effect as if their x and y coordinates were x+dx and y+dy.</para>
//        /// <para>Scale and translate are not commutative operations, therefore, calling them in different orders will change the outcome.</para>
//        /// <para>This change lasts until the next love.draw call, or a love.graphics.pop reverts to a previous love.graphics.push, or love.graphics.origin is called - whichever comes first.</para>
//        /// <para>Translating using whole numbers will prevent tearing/blurring of images and fonts draw after translating.</para>
//        /// </summary>
//    public void Translate() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the DPI scale factor of the window.</para>
//        /// <para>The DPI scale factor represents relative pixel density. The pixel density inside the window might be greater (or smaller) than the "size" of the window. For example on a retina screen in Mac OS X with the highdpi window flag enabled, the window may take up the same physical size as an 800x600 window, but the area inside the window uses 1600x1200 pixels. love.graphics.getDPIScale() would return 2 in that case.</para>
//        /// <para>The love.window.fromPixels and love.window.toPixels functions can also be used to convert between units.</para>
//        /// <para>The highdpi window flag must be enabled to use the full pixel density of a Retina screen on Mac OS X and iOS. The flag currently does nothing on Windows and Linux, and on Android it is effectively always enabled.</para>
//        /// </summary>
//        /// <param name="scale">The pixel scale factor associated with the window.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>scale</term><description>The pixel scale factor associated with the window.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetDPIScale(double scale) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the width and height in pixels of the window.</para>
//        /// </summary>
//        /// <param name="width">The width of the window.</param>
//        /// <param name="height">The height of the window.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>width</term><description>The width of the window.</description></item>
//        /// <item><term>height</term><description>The height of the window.</description></item>
//        /// </list>
//        /// </returns>
//    public (double width, double height) GetDimensions(double width, double height) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the height in pixels of the window.</para>
//        /// </summary>
//        /// <param name="height">The height of the window.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>height</term><description>The height of the window.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetHeight(double height) => throw new NotImplementedException();

///// <summary>
//        /// <para>Returns the current display mode.</para>
//        /// </summary>
//        /// <param name="width">Display width.</param>
//        /// <param name="height">Display height.</param>
//        /// <param name="fullscreen">Fullscreen (true) or windowed (false).</param>
//        /// <param name="vsync">True if vertical sync is enabled or false if disabled.</param>
//        /// <param name="fsaa">The number of FSAA samples.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>width</term><description>Display width.</description></item>
//        /// <item><term>height</term><description>Display height.</description></item>
//        /// <item><term>fullscreen</term><description>Fullscreen (true) or windowed (false).</description></item>
//        /// <item><term>vsync</term><description>True if vertical sync is enabled or false if disabled.</description></item>
//        /// <item><term>fsaa</term><description>The number of FSAA samples.</description></item>
//        /// </list>
//        /// </returns>
//    public (double width, double height, bool fullscreen, bool vsync, double fsaa) GetMode(double width, double height, bool fullscreen, bool vsync, double fsaa) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the width and height in pixels of the window.</para>
//        /// <para>love.graphics.getDimensions gets the dimensions of the window in units scaled by the screen's DPI scale factor, rather than pixels. Use getDimensions for calculations related to drawing to the screen and using the graphics coordinate system (calculating the center of the screen, for example), and getPixelDimensions only when dealing specifically with underlying pixels (pixel-related calculations in a pixel Shader, for example).</para>
//        /// </summary>
//        /// <param name="pixelwidth">The width of the window in pixels.</param>
//        /// <param name="pixelheight">The width of the window in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pixelwidth</term><description>The width of the window in pixels.</description></item>
//        /// <item><term>pixelheight</term><description>The width of the window in pixels.</description></item>
//        /// </list>
//        /// </returns>
//    public (double pixelwidth, double pixelheight) GetPixelDimensions(double pixelwidth, double pixelheight) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the height in pixels of the window.</para>
//        /// <para>The graphics coordinate system and love.graphics.getHeight use units scaled by the screen's DPI scale factor, rather than raw pixels. Use getHeight for calculations related to drawing to the screen and using the coordinate system (calculating the center of the screen, for example), and getPixelHeight only when dealing specifically with underlying pixels (pixel-related calculations in a pixel Shader, for example).</para>
//        /// </summary>
//        /// <param name="pixelheight">The height of the window in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pixelheight</term><description>The height of the window in pixels.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetPixelHeight(double pixelheight) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the width in pixels of the window.</para>
//        /// <para>The graphics coordinate system and love.graphics.getWidth use units scaled by the screen's DPI scale factor, rather than raw pixels. Use getWidth for calculations related to drawing to the screen and using the coordinate system (calculating the center of the screen, for example), and getPixelWidth only when dealing specifically with underlying pixels (pixel-related calculations in a pixel Shader, for example).</para>
//        /// </summary>
//        /// <param name="pixelwidth">The width of the window in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pixelwidth</term><description>The width of the window in pixels.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetPixelWidth(double pixelwidth) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the width in pixels of the window.</para>
//        /// </summary>
//        /// <param name="width">The width of the window.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>width</term><description>The width of the window.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetWidth(double width) => throw new NotImplementedException();

///// <summary>
//        /// <para>Checks if the game window has keyboard focus.</para>
//        /// </summary>
//        /// <param name="focus">True if the window has the focus or false if not.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>focus</term><description>True if the window has the focus or false if not.</description></item>
//        /// </list>
//        /// </returns>
//    public bool HasFocus(bool focus) => throw new NotImplementedException();

///// <summary>
//        /// <para>Set window icon. This feature is not completely supported on Windows (apparently an SDL bug, not a LÖVE bug).</para>
//        /// </summary>
//        /// <param name="image">The window icon.</param>
//    public void SetIcon(object image) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the available Canvas formats, and whether each is supported.</para>
//        /// </summary>
//        /// <param name="formats">A table containing as keys, and a boolean indicating whether the format is supported as values. Not all systems support all formats.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>formats</term><description>A table containing as keys, and a boolean indicating whether the format is supported as values. Not all systems support all formats.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetCanvasFormats(object formats) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the available Canvas formats, and whether each is supported.</para>
//        /// </summary>
//        /// <param name="readable">If true, the returned formats will only be indicated as supported if will work with the flag set to true for that format, and vice versa if the parameter is false.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>formats</term><description>A table containing as keys, and a boolean indicating whether the format is supported as values (taking into account the readable parameter). Not all systems support all formats.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetCanvasFormats(bool readable) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the available Canvas formats, and whether each is supported.</para>
//        /// </summary>
//    public void GetCanvasFormats() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the available compressed image formats, and whether each is supported.</para>
//        /// </summary>
//        /// <param name="formats">A table containing as keys, and a boolean indicating whether the format is supported as values. Not all systems support all formats.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>formats</term><description>A table containing as keys, and a boolean indicating whether the format is supported as values. Not all systems support all formats.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetCompressedImageFormats(object formats) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the available compressed image formats, and whether each is supported.</para>
//        /// </summary>
//    public void GetCompressedImageFormats() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the raw and compressed pixel formats usable for Images, and whether each is supported.</para>
//        /// </summary>
//        /// <param name="formats">A table containing as keys, and a boolean indicating whether the format is supported as values. Not all systems support all formats.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>formats</term><description>A table containing as keys, and a boolean indicating whether the format is supported as values. Not all systems support all formats.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetImageFormats(object formats) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the raw and compressed pixel formats usable for Images, and whether each is supported.</para>
//        /// </summary>
//    public void GetImageFormats() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the max supported width or height of Images and Canvases.</para>
//        /// <para>Attempting to create an Image with a width *or* height greater than this number will create a checkerboard-patterned image instead. Doing the same for a canvas will result in an error.</para>
//        /// <para>The returned number depends on the system running the code. It is safe to assume it will never be less than 1024 and will almost always be 2048 or greater.</para>
//        /// </summary>
//        /// <param name="size">The max supported width or height of Images and Canvases.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>size</term><description>The max supported width or height of Images and Canvases.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetMaxImageSize(double size) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets information about the system's video card and drivers.</para>
//        /// </summary>
//        /// <param name="name">The name of the renderer, e.g. "OpenGL" or "OpenGL ES".</param>
//        /// <param name="version">The version of the renderer with some extra driver-dependent version info, e.g. "2.1 INTEL-8.10.44".</param>
//        /// <param name="vendor">The name of the graphics card vendor, e.g. "Intel Inc".</param>
//        /// <param name="device">The name of the graphics card, e.g. "Intel HD Graphics 3000 OpenGL Engine".</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>name</term><description>The name of the renderer, e.g. "OpenGL" or "OpenGL ES".</description></item>
//        /// <item><term>version</term><description>The version of the renderer with some extra driver-dependent version info, e.g. "2.1 INTEL-8.10.44".</description></item>
//        /// <item><term>vendor</term><description>The name of the graphics card vendor, e.g. "Intel Inc".</description></item>
//        /// <item><term>device</term><description>The name of the graphics card, e.g. "Intel HD Graphics 3000 OpenGL Engine".</description></item>
//        /// </list>
//        /// </returns>
//    public (string name, string version, string vendor, string device) GetRendererInfo(string name, string version, string vendor, string device) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets performance-related rendering statistics.</para>
//        /// </summary>
//        /// <param name="stats">
//        /// A table with the following fields:
//        /// <list type="bullet">
//        /// <item><term>drawcalls</term><description>number: The number of draw calls made so far during the current frame.</description></item>
//        /// <item><term>canvasswitches</term><description>number: The number of times the active has been switched so far during the current frame.</description></item>
//        /// <item><term>texturememory</term><description>number: The estimated total size in bytes of video memory used by all loaded , , and .</description></item>
//        /// <item><term>images</term><description>number: The number of objects currently loaded.</description></item>
//        /// <item><term>canvases</term><description>number: The number of objects currently loaded.</description></item>
//        /// <item><term>fonts</term><description>number: The number of objects currently loaded.</description></item>
//        /// </list>
//        /// </param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>stats</term><description>
//        /// A table with the following fields:
//        /// <list type="bullet">
//        /// <item><term>drawcalls</term><description>number: The number of draw calls made so far during the current frame.</description></item>
//        /// <item><term>canvasswitches</term><description>number: The number of times the active has been switched so far during the current frame.</description></item>
//        /// <item><term>texturememory</term><description>number: The estimated total size in bytes of video memory used by all loaded , , and .</description></item>
//        /// <item><term>images</term><description>number: The number of objects currently loaded.</description></item>
//        /// <item><term>canvases</term><description>number: The number of objects currently loaded.</description></item>
//        /// <item><term>fonts</term><description>number: The number of objects currently loaded.</description></item>
//        /// </list>
//        /// </description></item>
//        /// </list>
//        /// </returns>
//    public object GetStats(object stats) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets performance-related rendering statistics.</para>
//        /// </summary>
//    public void SetNewFont() => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the optional graphics features and whether they're supported on the system.</para>
//        /// <para>Some older or low-end systems don't always support all graphics features.</para>
//        /// </summary>
//        /// <param name="features">A table containing keys, and boolean values indicating whether each feature is supported.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>features</term><description>A table containing keys, and boolean values indicating whether each feature is supported.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetSupported(object features) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the system-dependent maximum value for a love.graphics feature.</para>
//        /// </summary>
//        /// <param name="limittype">The graphics feature to get the maximum value of.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>limit</term><description>The system-dependent max value for the feature.</description></item>
//        /// </list>
//        /// </returns>
//    public double GetSystemLimit(GraphicsLimit limittype) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the system-dependent maximum values for love.graphics features.</para>
//        /// </summary>
//        /// <param name="limits">A table containing keys, and number values.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>limits</term><description>A table containing keys, and number values.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetSystemLimits(object limits) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the available texture types, and whether each is supported.</para>
//        /// </summary>
//        /// <param name="texturetypes">A table containing as keys, and a boolean indicating whether the type is supported as values. Not all systems support all types.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>texturetypes</term><description>A table containing as keys, and a boolean indicating whether the type is supported as values. Not all systems support all types.</description></item>
//        /// </list>
//        /// </returns>
//    public object GetTextureTypes(object texturetypes) => throw new NotImplementedException();

///// <summary>
//        /// <para>Gets the available texture types, and whether each is supported.</para>
//        /// </summary>
//    public void GetTextureTypes() => throw new NotImplementedException();

}