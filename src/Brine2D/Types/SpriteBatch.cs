namespace Brine2D
{
    /// <summary>
    /// <para>Using a single image, draw any number of identical copies of the image using a single call to love.graphics.draw(). This can be used, for example, to draw repeating copies of a single background image with high performance.</para>
/// <para>A SpriteBatch can be even more useful when the underlying image is a texture atlas (a single image file containing many independent images); by adding Quads to the batch, different sub-images from within the atlas can be drawn.</para>
    /// </summary>
    // TODO: Requires Review
    public class SpriteBatch
    {
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
        /// <summary>
        /// <para>Adds a sprite to a batch created with an Array Texture.</para>
        /// </summary>
        /// <param name="layerindex">The index of the layer to use for this sprite.</param>
        /// <param name="x">The position to draw the sprite (x-axis).</param>
        /// <param name="y">The position to draw the sprite (y-axis).</param>
        /// <param name="r">Orientation (radians).</param>
        /// <param name="sx">Scale factor (x-axis).</param>
        /// <param name="sy">Scale factor (y-axis).</param>
        /// <param name="ox">Origin offset (x-axis).</param>
        /// <param name="oy">Origin offset (y-axis).</param>
        /// <param name="kx">Shearing factor (x-axis).</param>
        /// <param name="ky">Shearing factor (y-axis).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>spriteindex</term><description>The index of the added sprite, for use with or .</description></item>
        /// </list>
        /// </returns>
        // TODO: public double AddLayer(double layerindex, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Adds a sprite to a batch created with an Array Texture.</para>
        /// </summary>
        /// <param name="layerindex">The index of the layer to use for this sprite.</param>
        /// <param name="quad">The subsection of the texture's layer to use when drawing the sprite.</param>
        /// <param name="x">The position to draw the sprite (x-axis).</param>
        /// <param name="y">The position to draw the sprite (y-axis).</param>
        /// <param name="r">Orientation (radians).</param>
        /// <param name="sx">Scale factor (x-axis).</param>
        /// <param name="sy">Scale factor (y-axis).</param>
        /// <param name="ox">Origin offset (x-axis).</param>
        /// <param name="oy">Origin offset (y-axis).</param>
        /// <param name="kx">Shearing factor (x-axis).</param>
        /// <param name="ky">Shearing factor (y-axis).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>spriteindex</term><description>The index of the added sprite, for use with or .</description></item>
        /// </list>
        /// </returns>
        // TODO: public double AddLayer(double layerindex, object quad, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Adds a sprite to a batch created with an Array Texture.</para>
        /// </summary>
        /// <param name="layerindex">The index of the layer to use for this sprite.</param>
        /// <param name="transform">A transform object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>spriteindex</term><description>The index of the added sprite, for use with or .</description></item>
        /// </list>
        /// </returns>
        public double AddLayer(double layerindex, object transform) => throw new NotImplementedException();
        /// <summary>
        /// <para>Adds a sprite to a batch created with an Array Texture.</para>
        /// </summary>
        /// <param name="layerindex">The index of the layer to use for this sprite.</param>
        /// <param name="quad">The subsection of the texture's layer to use when drawing the sprite.</param>
        /// <param name="transform">A transform object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>spriteindex</term><description>The index of the added sprite, for use with or .</description></item>
        /// </list>
        /// </returns>
        public double AddLayer(double layerindex, object quad, object transform) => throw new NotImplementedException();
        /// <summary>
        /// <para>Adds a sprite to a batch created with an Array Texture.</para>
        /// </summary>
        public void NewArrayImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Attaches a per-vertex attribute from a Mesh onto this SpriteBatch, for use when drawing. This can be combined with a Shader to augment a SpriteBatch with per-vertex or additional per-sprite information instead of just having per-sprite colors.</para>
        /// <para>Each sprite in a SpriteBatch has 4 vertices in the following order: top-left, bottom-left, top-right, bottom-right. The index returned by SpriteBatch:add (and used by SpriteBatch:set) can used to determine the first vertex of a specific sprite with the formula 1 + 4 * ( id - 1 ).</para>
        /// </summary>
        /// <param name="name">The name of the vertex attribute to attach.</param>
        /// <param name="mesh">The Mesh to get the vertex attribute from.</param>
        public void AttachAttribute(string name, object mesh) => throw new NotImplementedException();
        /// <summary>
        /// <para>Removes all sprites from the buffer.</para>
        /// </summary>
        public void Clear() => throw new NotImplementedException();
        /// <summary>
        /// <para>Immediately sends all new and modified sprite data in the batch to the graphics card.</para>
        /// <para>Normally it isn't necessary to call this method as love.graphics.draw(spritebatch, ...) will do it automatically if needed, but explicitly using SpriteBatch:flush gives more control over when the work happens.</para>
        /// <para>If this method is used, it generally shouldn't be called more than once (at most) between love.graphics.draw(spritebatch, ...) calls.</para>
        /// </summary>
        public void Flush() => throw new NotImplementedException();
        /// <summary>
        /// <para>Immediately sends all new and modified sprite data in the batch to the graphics card.</para>
        /// <para>Normally it isn't necessary to call this method as love.graphics.draw(spritebatch, ...) will do it automatically if needed, but explicitly using SpriteBatch:flush gives more control over when the work happens.</para>
        /// <para>If this method is used, it generally shouldn't be called more than once (at most) between love.graphics.draw(spritebatch, ...) calls.</para>
        /// </summary>
        public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the maximum number of sprites the SpriteBatch can hold.</para>
        /// </summary>
        /// <param name="size">The maximum number of sprites the batch can hold.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>size</term><description>The maximum number of sprites the batch can hold.</description></item>
        /// </list>
        /// </returns>
        public double GetBufferSize(double size) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the color that will be used for the next add and set operations.</para>
        /// <para>If no color has been set with SpriteBatch:setColor or the current SpriteBatch color has been cleared, this method will return nil.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="r">The red component (0-1).</param>
        /// <param name="g">The green component (0-1).</param>
        /// <param name="b">The blue component (0-1).</param>
        /// <param name="a">The alpha component (0-1).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>r</term><description>The red component (0-1).</description></item>
        /// <item><term>g</term><description>The green component (0-1).</description></item>
        /// <item><term>b</term><description>The blue component (0-1).</description></item>
        /// <item><term>a</term><description>The alpha component (0-1).</description></item>
        /// </list>
        /// </returns>
        public (double r, double g, double b, double a) GetColor(double r, double g, double b, double a) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of sprites currently in the SpriteBatch.</para>
        /// </summary>
        /// <param name="count">The number of sprites currently in the batch.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>The number of sprites currently in the batch.</description></item>
        /// </list>
        /// </returns>
        public double GetCount(double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the texture (Image or Canvas) used by the SpriteBatch.</para>
        /// </summary>
        /// <param name="texture">The Image or Canvas used by the SpriteBatch.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>texture</term><description>The Image or Canvas used by the SpriteBatch.</description></item>
        /// </list>
        /// </returns>
        public object GetTexture(object texture) => throw new NotImplementedException();
        /// <summary>
        /// <para>Changes a sprite in the batch. This requires the sprite index returned by SpriteBatch:add or SpriteBatch:addLayer.</para>
        /// </summary>
        /// <param name="spriteindex">The index of the sprite that will be changed.</param>
        /// <param name="x">The position to draw the object (x-axis).</param>
        /// <param name="y">The position to draw the object (y-axis).</param>
        /// <param name="r">Orientation (radians).</param>
        /// <param name="sx">Scale factor (x-axis).</param>
        /// <param name="sy">Scale factor (y-axis).</param>
        /// <param name="ox">Origin offset (x-axis).</param>
        /// <param name="oy">Origin offset (y-axis).</param>
        /// <param name="kx">Shear factor (x-axis).</param>
        /// <param name="ky">Shear factor (y-axis).</param>
        // TODO: public void Set(double spriteindex, double x, double y, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Changes a sprite in the batch. This requires the sprite index returned by SpriteBatch:add or SpriteBatch:addLayer.</para>
        /// </summary>
        /// <param name="spriteindex">The index of the sprite that will be changed.</param>
        /// <param name="quad">The Quad used on the image of the batch.</param>
        /// <param name="x">The position to draw the object (x-axis).</param>
        /// <param name="y">The position to draw the object (y-axis).</param>
        /// <param name="r">Orientation (radians).</param>
        /// <param name="sx">Scale factor (x-axis).</param>
        /// <param name="sy">Scale factor (y-axis).</param>
        /// <param name="ox">Origin offset (x-axis).</param>
        /// <param name="oy">Origin offset (y-axis).</param>
        /// <param name="kx">Shear factor (x-axis).</param>
        /// <param name="ky">Shear factor (y-axis).</param>
        // TODO: public void Set(double spriteindex, object quad, double x, double y, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the maximum number of sprites the SpriteBatch can hold. Existing sprites in the batch (up to the new maximum) will not be cleared when this function is called.</para>
        /// </summary>
        /// <param name="size">The new maximum number of sprites the batch can hold.</param>
        public void SetBufferSize(double size) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the color that will be used for the next add and set operations. Calling the function without arguments will disable all per-sprite colors for the SpriteBatch.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// <para>In version 0.9.2 and older, the global color set with love.graphics.setColor will not work on the SpriteBatch if any of the sprites has its own color.</para>
        /// </summary>
        /// <param name="r">The amount of red.</param>
        /// <param name="g">The amount of green.</param>
        /// <param name="b">The amount of blue.</param>
        /// <param name="a">The amount of alpha.</param>
        public void SetColor(double r, double g, double b, double a = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Restricts the drawn sprites in the SpriteBatch to a subset of the total.</para>
        /// </summary>
        /// <param name="start">The index of the first sprite to draw. Index 1 corresponds to the first sprite added with .</param>
        /// <param name="count">The number of sprites to draw.</param>
        public void SetDrawRange(double start, double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Restricts the drawn sprites in the SpriteBatch to a subset of the total.</para>
        /// </summary>
        public void SetDrawRange() => throw new NotImplementedException();
        /// <summary>
        /// <para>Changes a sprite previously added with add or addLayer, in a batch created with an Array Texture.</para>
        /// </summary>
        /// <param name="spriteindex">The index of the existing sprite to replace.</param>
        /// <param name="layerindex">The index of the layer in the Array Texture to use for this sprite.</param>
        /// <param name="x">The position to draw the sprite (x-axis).</param>
        /// <param name="y">The position to draw the sprite (y-axis).</param>
        /// <param name="r">Orientation (radians).</param>
        /// <param name="sx">Scale factor (x-axis).</param>
        /// <param name="sy">Scale factor (y-axis).</param>
        /// <param name="ox">Origin offset (x-axis).</param>
        /// <param name="oy">Origin offset (y-axis).</param>
        /// <param name="kx">Shearing factor (x-axis).</param>
        /// <param name="ky">Shearing factor (y-axis).</param>
        // TODO: public void SetLayer(double spriteindex, double layerindex, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Changes a sprite previously added with add or addLayer, in a batch created with an Array Texture.</para>
        /// </summary>
        /// <param name="spriteindex">The index of the existing sprite to replace.</param>
        /// <param name="layerindex">The index of the layer to use for this sprite.</param>
        /// <param name="quad">The subsection of the texture's layer to use when drawing the sprite.</param>
        /// <param name="x">The position to draw the sprite (x-axis).</param>
        /// <param name="y">The position to draw the sprite (y-axis).</param>
        /// <param name="r">Orientation (radians).</param>
        /// <param name="sx">Scale factor (x-axis).</param>
        /// <param name="sy">Scale factor (y-axis).</param>
        /// <param name="ox">Origin offset (x-axis).</param>
        /// <param name="oy">Origin offset (y-axis).</param>
        /// <param name="kx">Shearing factor (x-axis).</param>
        /// <param name="ky">Shearing factor (y-axis).</param>
        // TODO: public void SetLayer(double spriteindex, double layerindex, object quad, double x = 0, double y = 0, double r = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Changes a sprite previously added with add or addLayer, in a batch created with an Array Texture.</para>
        /// </summary>
        /// <param name="spriteindex">The index of the existing sprite to replace.</param>
        /// <param name="layerindex">The index of the layer to use for the sprite.</param>
        /// <param name="transform">A transform object.</param>
        public void SetLayer(double spriteindex, double layerindex, object transform) => throw new NotImplementedException();
        /// <summary>
        /// <para>Changes a sprite previously added with add or addLayer, in a batch created with an Array Texture.</para>
        /// </summary>
        /// <param name="spriteindex">The index of the existing sprite to replace.</param>
        /// <param name="layerindex">The index of the layer to use for the sprite.</param>
        /// <param name="quad">The subsection of the texture's layer to use when drawing the sprite.</param>
        /// <param name="transform">A transform object.</param>
        public void SetLayer(double spriteindex, double layerindex, object quad, object transform) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the texture (Image or Canvas) used for the sprites in the batch, when drawing.</para>
        /// </summary>
        /// <param name="texture">The new Image or Canvas to use for the sprites in the batch.</param>
        public void SetTexture(object texture) => throw new NotImplementedException();
    }
}
