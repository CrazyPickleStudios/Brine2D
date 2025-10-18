namespace Brine2D
{
    /// <summary>
    /// <para>Drawable text.</para>
    /// </summary>
    // TODO: Requires Review
    public class Text
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
        /// <para>Adds additional colored text to the Text object at the specified position.</para>
        /// </summary>
        /// <param name="textstring">The text to add to the object.</param>
        /// <param name="x">The position of the new text on the x-axis.</param>
        /// <param name="y">The position of the new text on the y-axis.</param>
        /// <param name="angle">The orientation of the new text in radians.</param>
        /// <param name="sx">Scale factor on the x-axis.</param>
        /// <param name="sy">Scale factor on the y-axis.</param>
        /// <param name="ox">Origin offset on the x-axis.</param>
        /// <param name="oy">Origin offset on the y-axis.</param>
        /// <param name="kx">Shearing / skew factor on the x-axis.</param>
        /// <param name="ky">Shearing / skew factor on the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>index</term><description>An index number that can be used with or .</description></item>
        /// </list>
        /// </returns>
        // TODO: public double Add(string textstring, double x = 0, double y = 0, double angle = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Adds additional colored text to the Text object at the specified position.</para>
        /// </summary>
        /// <param name="coloredtext">
        /// A table containing colors and strings to add to the object, in the form of .
        /// <list type="bullet">
        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>index</term><description>An index number that can be used with or .</description></item>
        /// </list>
        /// </returns>
        public double Add(object coloredtext) => throw new NotImplementedException();
        /// <summary>
        /// <para>Adds additional formatted / colored text to the Text object at the specified position.</para>
        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
        /// </summary>
        /// <param name="textstring">The text to add to the object.</param>
        /// <param name="wraplimit">The maximum width in pixels of the text before it gets automatically wrapped to a new line.</param>
        /// <param name="align">The alignment of the text.</param>
        /// <param name="x">The position of the new text (x-axis).</param>
        /// <param name="y">The position of the new text (y-axis).</param>
        /// <param name="angle">Orientation (radians).</param>
        /// <param name="sx">Scale factor (x-axis).</param>
        /// <param name="sy">Scale factor (y-axis).</param>
        /// <param name="ox">Origin offset (x-axis).</param>
        /// <param name="oy">Origin offset (y-axis).</param>
        /// <param name="kx">Shearing / skew factor (x-axis).</param>
        /// <param name="ky">Shearing / skew factor (y-axis).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>index</term><description>An index number that can be used with or .</description></item>
        /// </list>
        /// </returns>
        // TODO: public double Addf(string textstring, double wraplimit, AlignMode align, double x, double y, double angle = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Adds additional formatted / colored text to the Text object at the specified position.</para>
        /// <para>The word wrap limit is applied before any scaling, rotation, and other coordinate transformations. Therefore the amount of text per line stays constant given the same wrap limit, even if the scale arguments change.</para>
        /// </summary>
        /// <param name="coloredtext">
        /// A table containing colors and strings to add to the object, in the form of .
        /// <list type="bullet">
        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>index</term><description>An index number that can be used with or .</description></item>
        /// </list>
        /// </returns>
        public double Addf(object coloredtext) => throw new NotImplementedException();
        /// <summary>
        /// <para>Clears the contents of the Text object.</para>
        /// </summary>
        public void Clear() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width and height of the text in pixels.</para>
        /// </summary>
        /// <param name="width">The width of the text. If multiple sub-strings have been added with , the width of the last sub-string is returned.</param>
        /// <param name="height">The height of the text. If multiple sub-strings have been added with , the height of the last sub-string is returned.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the text. If multiple sub-strings have been added with , the width of the last sub-string is returned.</description></item>
        /// <item><term>height</term><description>The height of the text. If multiple sub-strings have been added with , the height of the last sub-string is returned.</description></item>
        /// </list>
        /// </returns>
        public (double width, double height) GetDimensions(double width, double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width and height of the text in pixels.</para>
        /// </summary>
        /// <param name="index">An index number returned by or .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the sub-string (before scaling and other transformations).</description></item>
        /// <item><term>height</term><description>The height of the sub-string (before scaling and other transformations).</description></item>
        /// </list>
        /// </returns>
        public (double width, double height) GetDimensions(double index) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the Font used with the Text object.</para>
        /// </summary>
        /// <param name="font">The font used with this Text object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>font</term><description>The font used with this Text object.</description></item>
        /// </list>
        /// </returns>
        public object GetFont(object font) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the height of the text in pixels.</para>
        /// </summary>
        /// <param name="height">The height of the text. If multiple sub-strings have been added with , the height of the last sub-string is returned.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>The height of the text. If multiple sub-strings have been added with , the height of the last sub-string is returned.</description></item>
        /// </list>
        /// </returns>
        // TODO: public double GetHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the height of the text in pixels.</para>
        /// </summary>
        /// <param name="index">An index number returned by or .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>The height of the sub-string (before scaling and other transformations).</description></item>
        /// </list>
        /// </returns>
        public double GetHeight(double index) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width of the text in pixels.</para>
        /// </summary>
        /// <param name="width">The width of the text. If multiple sub-strings have been added with , the width of the last sub-string is returned.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the text. If multiple sub-strings have been added with , the width of the last sub-string is returned.</description></item>
        /// </list>
        /// </returns>
        // TODO: public double GetWidth(double width) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width of the text in pixels.</para>
        /// </summary>
        /// <param name="index">An index number returned by or .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the sub-string (before scaling and other transformations).</description></item>
        /// </list>
        /// </returns>
        public double GetWidth(double index) => throw new NotImplementedException();
        /// <summary>
        /// <para>Replaces the Font used with the text.</para>
        /// </summary>
        /// <param name="font">The new font to use with this Text object.</param>
        public void SetFont(object font) => throw new NotImplementedException();
        /// <summary>
        /// <para>Replaces the contents of the Text object with a new formatted string.</para>
        /// </summary>
        /// <param name="textstring">The new string of text to use.</param>
        /// <param name="wraplimit">The maximum width in pixels of the text before it gets automatically wrapped to a new line.</param>
        /// <param name="align">The alignment of the text.</param>
        public void Setf(string textstring, double wraplimit, AlignMode align) => throw new NotImplementedException();
        /// <summary>
        /// <para>Replaces the contents of the Text object with a new formatted string.</para>
        /// </summary>
        /// <param name="coloredtext">
        /// A table containing colors and strings to use as the new text, in the form of .
        /// <list type="bullet">
        /// <item><term>color1</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
        /// <item><term>string1</term><description>string: A string of text which has a color specified by the previous color.</description></item>
        /// <item><term>color2</term><description>table: A table containing red, green, blue, and optional alpha components to use as a color for the next string in the table, in the form of .</description></item>
        /// <item><term>string2</term><description>string: A string of text which has a color specified by the previous color.</description></item>
        /// <item><term>and</term><description>tables: Additional colors and strings.</description></item>
        /// </list>
        /// </param>
        public void Setf(object coloredtext) => throw new NotImplementedException();
    }
}
