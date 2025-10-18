namespace Brine2D
{
    /// <summary>
    /// <para>A polygon mesh used for drawing arbitrary textured shapes.</para>
    /// </summary>
    // TODO: Requires Review
    public class Mesh
    {
        /// <summary>
        /// <para>Attaches a vertex attribute from a different Mesh onto this Mesh, for use when drawing. This can be used to share vertex attribute data between several different Meshes.</para>
        /// </summary>
        /// <param name="name">The name of the vertex attribute to attach.</param>
        /// <param name="mesh">The Mesh to get the vertex attribute from.</param>
        public void AttachAttribute(string name, object mesh) => throw new NotImplementedException();
        /// <summary>
        /// <para>Attaches a vertex attribute from a different Mesh onto this Mesh, for use when drawing. This can be used to share vertex attribute data between several different Meshes.</para>
        /// </summary>
        /// <param name="name">The name of the vertex attribute to attach.</param>
        /// <param name="mesh">The Mesh to get the vertex attribute from.</param>
        /// <param name="step">Whether the attribute will be per-vertex or when the mesh is drawn.</param>
        /// <param name="attachname">The name of the attribute to use in shader code. Defaults to the name of the attribute in the given mesh. Can be used to use a different name for this attribute when rendering.</param>
        // TODO: public void AttachAttribute(string name, object mesh, VertexAttributeStep step = "pervertex", string attachname = "name") => throw new NotImplementedException();
        /// <summary>
        /// <para>Removes a previously attached vertex attribute from this Mesh.</para>
        /// </summary>
        /// <param name="name">The name of the attached vertex attribute to detach.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the attribute was successfully detached.</description></item>
        /// </list>
        /// </returns>
        public bool DetachAttribute(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Immediately sends all modified vertex data in the Mesh to the graphics card.</para>
        /// <para>Normally it isn't necessary to call this method as love.graphics.draw(mesh, ...) will do it automatically if needed, but explicitly using Mesh:flush gives more control over when the work happens.</para>
        /// <para>If this method is used, it generally shouldn't be called more than once (at most) between love.graphics.draw(mesh, ...) calls.</para>
        /// </summary>
        public void Flush() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the mode used when drawing the Mesh.</para>
        /// </summary>
        /// <param name="mode">The mode used when drawing the Mesh.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mode</term><description>The mode used when drawing the Mesh.</description></item>
        /// </list>
        /// </returns>
        public MeshDrawMode GetDrawMode(MeshDrawMode mode) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the range of vertices used when drawing the Mesh.</para>
        /// </summary>
        /// <param name="min">The index of the first vertex used when drawing, or the index of the first value in the vertex map used if one is set for this Mesh.</param>
        /// <param name="max">The index of the last vertex used when drawing, or the index of the last value in the vertex map used if one is set for this Mesh.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The index of the first vertex used when drawing, or the index of the first value in the vertex map used if one is set for this Mesh.</description></item>
        /// <item><term>max</term><description>The index of the last vertex used when drawing, or the index of the last value in the vertex map used if one is set for this Mesh.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (double min, double max) GetDrawRange(double min = null, double max = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the texture (Image or Canvas) used when drawing the Mesh.</para>
        /// </summary>
        /// <param name="texture">The Image or Canvas to texture the Mesh with when drawing, or nil if none is set.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>texture</term><description>The Image or Canvas to texture the Mesh with when drawing, or nil if none is set.</description></item>
        /// </list>
        /// </returns>
        public object GetTexture(object texture = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the properties of a vertex in the Mesh.</para>
        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="index">The one-based index of the vertex you want to retrieve the information for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>attributecomponent</term><description>The first component of the first vertex attribute in the specified vertex.</description></item>
        /// <item><term></term><description>Additional components of all vertex attributes in the specified vertex.</description></item>
        /// </list>
        /// </returns>
        public (double attributecomponent, object) GetVertex(double index) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the properties of a vertex in the Mesh.</para>
        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="index">The index of the vertex you want to retrieve the information for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The position of the vertex on the x-axis.</description></item>
        /// <item><term>y</term><description>The position of the vertex on the y-axis.</description></item>
        /// <item><term>u</term><description>The horizontal component of the texture coordinate.</description></item>
        /// <item><term>v</term><description>The vertical component of the texture coordinate.</description></item>
        /// <item><term>r</term><description>The red component of the vertex's color.</description></item>
        /// <item><term>g</term><description>The green component of the vertex's color.</description></item>
        /// <item><term>b</term><description>The blue component of the vertex's color.</description></item>
        /// <item><term>a</term><description>The alpha component of the vertex's color.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (double x, double y, double u, double v, double r, double g, double b, double a) GetVertex(double index) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the properties of a specific attribute within a vertex in the Mesh.</para>
        /// <para>Meshes without a custom vertex format specified in love.graphics.newMesh have position as their first attribute, texture coordinates as their second attribute, and color as their third attribute.</para>
        /// </summary>
        /// <param name="vertexindex">The index of the the vertex you want to retrieve the attribute for (one-based).</param>
        /// <param name="attributeindex">The index of the attribute within the vertex to be retrieved (one-based).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value1</term><description>The value of the first component of the attribute.</description></item>
        /// <item><term>value2</term><description>The value of the second component of the attribute.</description></item>
        /// <item><term></term><description>Any additional vertex attribute components.</description></item>
        /// </list>
        /// </returns>
        public (double value1, double value2, object) GetVertexAttribute(double vertexindex, double attributeindex) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the total number of vertices in the Mesh.</para>
        /// </summary>
        /// <param name="count">The total number of vertices in the mesh.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>The total number of vertices in the mesh.</description></item>
        /// </list>
        /// </returns>
        public double GetVertexCount(double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the vertex format that the Mesh was created with.</para>
        /// </summary>
        /// <param name="format">
        /// The vertex format of the Mesh, which is a table containing tables for each vertex attribute the Mesh was created with, in the form of .
        /// <list type="bullet">
        /// <item><term>attribute</term><description>table: A table containing the attribute's name, it's , and the number of components in the attribute, in the form of .</description></item>
        /// <item><term></term><description>table ...: Additional vertex attributes in the Mesh.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>format</term><description>
        /// The vertex format of the Mesh, which is a table containing tables for each vertex attribute the Mesh was created with, in the form of .
        /// <list type="bullet">
        /// <item><term>attribute</term><description>table: A table containing the attribute's name, it's , and the number of components in the attribute, in the form of .</description></item>
        /// <item><term></term><description>table ...: Additional vertex attributes in the Mesh.</description></item>
        /// </list>
        /// </description></item>
        /// </list>
        /// </returns>
        public object GetVertexFormat(object format) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the vertex map for the Mesh. The vertex map describes the order in which the vertices are used when the Mesh is drawn. The vertices, vertex map, and mesh draw mode work together to determine what exactly is displayed on the screen.</para>
        /// <para>If no vertex map has been set previously via Mesh:setVertexMap, then this function will return nil in LÖVE 0.10.0+, or an empty table in 0.9.2 and older.</para>
        /// </summary>
        /// <param name="map">A table containing the list of vertex indices used when drawing.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>map</term><description>A table containing the list of vertex indices used when drawing.</description></item>
        /// </list>
        /// </returns>
        public object GetVertexMap(object map) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets all the vertices in the Mesh.</para>
        /// </summary>
        /// <param name="vertices">
        /// The table filled with vertex information tables for each vertex as follows:
        /// <list type="bullet">
        /// <item><term></term><description>number [1]: The position of the vertex on the x-axis.</description></item>
        /// <item><term></term><description>number [2]: The position of the vertex on the y-axis.</description></item>
        /// <item><term></term><description>number [3]: The horizontal component of the texture coordinate. Texture coordinates are normally in the range of [0, 1], but can be greater or less (see .)</description></item>
        /// <item><term></term><description>number [4]: The vertical component of the texture coordinate. Texture coordinates are normally in the range of [0, 1], but can be greater or less (see .)</description></item>
        /// <item><term></term><description>number [5] (255): The red color component.</description></item>
        /// <item><term></term><description>number [6] (255): The green color component.</description></item>
        /// <item><term></term><description>number [7] (255): The blue color component.</description></item>
        /// <item><term></term><description>number [8] (255): The alpha color component.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>vertices</term><description>
        /// The table filled with vertex information tables for each vertex as follows:
        /// <list type="bullet">
        /// <item><term></term><description>number [1]: The position of the vertex on the x-axis.</description></item>
        /// <item><term></term><description>number [2]: The position of the vertex on the y-axis.</description></item>
        /// <item><term></term><description>number [3]: The horizontal component of the texture coordinate. Texture coordinates are normally in the range of [0, 1], but can be greater or less (see .)</description></item>
        /// <item><term></term><description>number [4]: The vertical component of the texture coordinate. Texture coordinates are normally in the range of [0, 1], but can be greater or less (see .)</description></item>
        /// <item><term></term><description>number [5] (255): The red color component.</description></item>
        /// <item><term></term><description>number [6] (255): The green color component.</description></item>
        /// <item><term></term><description>number [7] (255): The blue color component.</description></item>
        /// <item><term></term><description>number [8] (255): The alpha color component.</description></item>
        /// </list>
        /// </description></item>
        /// </list>
        /// </returns>
        public object GetVertices(object vertices) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether per-vertex colors are used instead of the constant color when drawing the Mesh (constant color being love.graphics.setColor.)</para>
        /// <para>Per-vertex colors are enabled by default for a Mesh if at least one vertex color was not the default (255, 255, 255, 255) when the Mesh was created.</para>
        /// </summary>
        /// <param name="vertexcolors">True if per-vertex coloring is used, otherwise is used when drawing the Mesh.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>vertexcolors</term><description>True if per-vertex coloring is used, otherwise is used when drawing the Mesh.</description></item>
        /// </list>
        /// </returns>
        public bool HasVertexColors(bool vertexcolors) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether a specific vertex attribute in the Mesh is enabled. Vertex data from disabled attributes is not used when drawing the Mesh.</para>
        /// </summary>
        /// <param name="name">The name of the vertex attribute to be checked.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>enabled</term><description>Whether the vertex attribute is used when drawing this Mesh.</description></item>
        /// </list>
        /// </returns>
        public bool IsAttributeEnabled(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Enables or disables a specific vertex attribute in the Mesh. Vertex data from disabled attributes is not used when drawing the Mesh.</para>
        /// </summary>
        /// <param name="name">The name of the vertex attribute to enable or disable.</param>
        /// <param name="enable">Whether the vertex attribute is used when drawing this Mesh.</param>
        public void SetAttributeEnabled(string name, bool enable) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the mode used when drawing the Mesh.</para>
        /// </summary>
        /// <param name="mode">The mode to use when drawing the Mesh.</param>
        public void SetDrawMode(MeshDrawMode mode) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the texture (Image or Canvas) used when drawing the Mesh.</para>
        /// </summary>
        /// <param name="texture">The Image or Canvas to texture the Mesh with when drawing.</param>
        public void SetTexture(object texture) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the texture (Image or Canvas) used when drawing the Mesh.</para>
        /// </summary>
        public void SetTexture() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the properties of a vertex in the Mesh.</para>
        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="index">The index of the the vertex you want to modify (one-based).</param>
        /// <param name="attributecomponent">The first component of the first vertex attribute in the specified vertex.</param>
        /// <param name="">Additional components of all vertex attributes in the specified vertex.</param>
        // TODO: public void SetVertex(double index, double attributecomponent, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the properties of a vertex in the Mesh.</para>
        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="index">The index of the the vertex you want to modify (one-based).</param>
        /// <param name="vertex">
        /// A table with vertex information, in the form of .
        /// <list type="bullet">
        /// <item><term>attributecomponent</term><description>number: The first component of the first vertex attribute in the specified vertex.</description></item>
        /// <item><term></term><description>number ...: Additional components of all vertex attributes in the specified vertex.</description></item>
        /// </list>
        /// </param>
        // TODO: public void SetVertex(double index, object vertex) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the properties of a vertex in the Mesh.</para>
        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="index">The index of the the vertex you want to modify (one-based).</param>
        /// <param name="x">The position of the vertex on the x-axis.</param>
        /// <param name="y">The position of the vertex on the y-axis.</param>
        /// <param name="u">The horizontal component of the texture coordinate.</param>
        /// <param name="v">The vertical component of the texture coordinate.</param>
        /// <param name="r">The red component of the vertex's color.</param>
        /// <param name="g">The green component of the vertex's color.</param>
        /// <param name="b">The blue component of the vertex's color.</param>
        /// <param name="a">The alpha component of the vertex's color.</param>
        public void SetVertex(double index, double x, double y, double u, double v, double r = 1, double g = 1, double b = 1, double a = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the properties of a vertex in the Mesh.</para>
        /// <para>In versions prior to 11.0, color and byte component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="index">The index of the the vertex you want to modify (one-based).</param>
        /// <param name="vertex">
        /// A table with vertex information.
        /// <list type="bullet">
        /// <item><term></term><description>number [1]: The position of the vertex on the x-axis.</description></item>
        /// <item><term></term><description>number [2]: The position of the vertex on the y-axis.</description></item>
        /// <item><term></term><description>number [3]: The u texture coordinate. Texture coordinates are normally in the range of [0, 1], but can be greater or less (see .)</description></item>
        /// <item><term></term><description>number [4]: The v texture coordinate. Texture coordinates are normally in the range of [0, 1], but can be greater or less (see .)</description></item>
        /// <item><term></term><description>number [5] (1): The red color component.</description></item>
        /// <item><term></term><description>number [6] (1): The green color component.</description></item>
        /// <item><term></term><description>number [7] (1): The blue color component.</description></item>
        /// <item><term></term><description>number [8] (1): The alpha color component.</description></item>
        /// </list>
        /// </param>
        public void SetVertex(double index, object vertex) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the properties of a specific attribute within a vertex in the Mesh.</para>
        /// <para>Meshes without a custom vertex format specified in love.graphics.newMesh have position as their first attribute, texture coordinates as their second attribute, and color as their third attribute.</para>
        /// </summary>
        /// <param name="vertexindex">The index of the the vertex to be modified (one-based).</param>
        /// <param name="attributeindex">The index of the attribute within the vertex to be modified (one-based).</param>
        /// <param name="value1">The new value for the first component of the attribute.</param>
        /// <param name="value2">The new value for the second component of the attribute.</param>
        /// <param name="">Any additional vertex attribute components.</param>
        // TODO: public void SetVertexAttribute(double vertexindex, double attributeindex, double value1, double value2, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the properties of a specific attribute within a vertex in the Mesh.</para>
        /// <para>Meshes without a custom vertex format specified in love.graphics.newMesh have position as their first attribute, texture coordinates as their second attribute, and color as their third attribute.</para>
        /// </summary>
        public void Draw() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets whether per-vertex colors are used instead of the constant color when drawing the Mesh (constant color being love.graphics.setColor.)</para>
        /// <para>Per-vertex colors are enabled by default for a Mesh if at least one vertex color was not the default (255, 255, 255, 255) when the Mesh was created.</para>
        /// </summary>
        /// <param name="on">True to use per-vertex coloring, otherwise is used when drawing.</param>
        public void SetVertexColors(bool on) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vertex map for the Mesh. The vertex map describes the order in which the vertices are used when the Mesh is drawn. The vertices, vertex map, and mesh draw mode work together to determine what exactly is displayed on the screen.</para>
        /// <para>The vertex map allows you to re-order or reuse vertices when drawing without changing the actual vertex parameters or duplicating vertices. It is especially useful when combined with different Mesh Draw Modes.</para>
        /// </summary>
        /// <param name="map">A table containing a list of vertex indices to use when drawing. Values must be in the range of [1, ].</param>
        public void SetVertexMap(object map) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vertex map for the Mesh. The vertex map describes the order in which the vertices are used when the Mesh is drawn. The vertices, vertex map, and mesh draw mode work together to determine what exactly is displayed on the screen.</para>
        /// <para>The vertex map allows you to re-order or reuse vertices when drawing without changing the actual vertex parameters or duplicating vertices. It is especially useful when combined with different Mesh Draw Modes.</para>
        /// </summary>
        /// <param name="vi1">The index of the first vertex to use when drawing. Must be in the range of [1, ].</param>
        /// <param name="vi2">The index of the second vertex to use when drawing.</param>
        /// <param name="vi3">The index of the third vertex to use when drawing.</param>
        public void SetVertexMap(double vi1, double vi2, double vi3) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vertex map for the Mesh. The vertex map describes the order in which the vertices are used when the Mesh is drawn. The vertices, vertex map, and mesh draw mode work together to determine what exactly is displayed on the screen.</para>
        /// <para>The vertex map allows you to re-order or reuse vertices when drawing without changing the actual vertex parameters or duplicating vertices. It is especially useful when combined with different Mesh Draw Modes.</para>
        /// </summary>
        /// <param name="data">Array of vertex indices to use when drawing. Values must be in the range of [0, -1]</param>
        /// <param name="datatype">Datatype of the vertex indices array above.</param>
        public void SetVertexMap(object data, IndexDataType datatype) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vertex map for the Mesh. The vertex map describes the order in which the vertices are used when the Mesh is drawn. The vertices, vertex map, and mesh draw mode work together to determine what exactly is displayed on the screen.</para>
        /// <para>The vertex map allows you to re-order or reuse vertices when drawing without changing the actual vertex parameters or duplicating vertices. It is especially useful when combined with different Mesh Draw Modes.</para>
        /// </summary>
        public void SetVertexMap() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the vertex map for the Mesh. The vertex map describes the order in which the vertices are used when the Mesh is drawn. The vertices, vertex map, and mesh draw mode work together to determine what exactly is displayed on the screen.</para>
        /// <para>The vertex map allows you to re-order or reuse vertices when drawing without changing the actual vertex parameters or duplicating vertices. It is especially useful when combined with different Mesh Draw Modes.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Replaces a range of vertices in the Mesh with new ones. The total number of vertices in a Mesh cannot be changed after it has been created. This is often more efficient than calling Mesh:setVertex in a loop.</para>
        /// </summary>
        /// <param name="vertices">
        /// The table filled with vertex information tables for each vertex, in the form of where each vertex is a table in the form of .
        /// <list type="bullet">
        /// <item><term>attributecomponent</term><description>number: The first component of the first vertex attribute in the vertex.</description></item>
        /// <item><term></term><description>number ...: Additional components of all vertex attributes in the vertex.</description></item>
        /// </list>
        /// </param>
        // TODO: public void SetVertices(object vertices) => throw new NotImplementedException();
        /// <summary>
        /// <para>Replaces a range of vertices in the Mesh with new ones. The total number of vertices in a Mesh cannot be changed after it has been created. This is often more efficient than calling Mesh:setVertex in a loop.</para>
        /// </summary>
        /// <param name="data">A Data object to copy from. The contents of the Data must match the layout of this Mesh's .</param>
        /// <param name="startvertex">The index of the first vertex to replace.</param>
        public void SetVertices(object data, double startvertex = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Replaces a range of vertices in the Mesh with new ones. The total number of vertices in a Mesh cannot be changed after it has been created. This is often more efficient than calling Mesh:setVertex in a loop.</para>
        /// </summary>
        /// <param name="vertices">
        /// The table filled with vertex information tables for each vertex as follows:
        /// <list type="bullet">
        /// <item><term></term><description>number [1]: The position of the vertex on the x-axis.</description></item>
        /// <item><term></term><description>number [2]: The position of the vertex on the y-axis.</description></item>
        /// <item><term></term><description>number [3]: The horizontal component of the texture coordinate. Texture coordinates are normally in the range of [0, 1], but can be greater or less (see ).</description></item>
        /// <item><term></term><description>number [4]: The vertical component of the texture coordinate. Texture coordinates are normally in the range of [0, 1], but can be greater or less (see ).</description></item>
        /// <item><term></term><description>number [5] (1): The red color component.</description></item>
        /// <item><term></term><description>number [6] (1): The green color component.</description></item>
        /// <item><term></term><description>number [7] (1): The blue color component.</description></item>
        /// <item><term></term><description>number [8] (1): The alpha color component.</description></item>
        /// </list>
        /// </param>
        public void SetVertices(object vertices) => throw new NotImplementedException();
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
