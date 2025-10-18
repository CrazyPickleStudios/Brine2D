namespace Brine2D
{
    /// <summary>
    /// <para>A ParticleSystem can be used to create particle effects like fire or smoke.</para>
/// <para>The particle system has to be created using love.graphics.newParticleSystem. Just like any other Drawable it can be drawn to the screen using love.graphics.draw. You also have to update it in the update callback to see any changes in the particles emitted.</para>
/// <para>The particle system won't create any particles unless you call setParticleLifetime and setEmissionRate.</para>
    /// </summary>
    // TODO: Requires Review
    public class ParticleSystem
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
        /// <para>Creates an identical copy of the ParticleSystem in the stopped state.</para>
        /// </summary>
        /// <param name="particlesystem">The new identical copy of this ParticleSystem.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>particlesystem</term><description>The new identical copy of this ParticleSystem.</description></item>
        /// </list>
        /// </returns>
        public object Clone(object particlesystem) => throw new NotImplementedException();
        /// <summary>
        /// <para>Emits a burst of particles from the particle emitter.</para>
        /// </summary>
        /// <param name="numparticles">The amount of particles to emit. The number of emitted particles will be truncated if the particle system's max is reached.</param>
        public void Emit(double numparticles) => throw new NotImplementedException();
        /// <summary>
        /// <para>Emits a burst of particles from the particle emitter.</para>
        /// </summary>
        public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the maximum number of particles the ParticleSystem can have at once.</para>
        /// </summary>
        /// <param name="size">The maximum number of particles.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>size</term><description>The maximum number of particles.</description></item>
        /// </list>
        /// </returns>
        public double GetBufferSize(double size) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the series of colors applied to the particle sprite.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="rgba1">First color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1).</param>
        /// <param name="rgba2">Second color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1).</param>
        /// <param name="rgba8">Eighth color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rgba1</term><description>First color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1).</description></item>
        /// <item><term>rgba2</term><description>Second color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1).</description></item>
        /// <item><term>rgba8</term><description>Eighth color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1).</description></item>
        /// </list>
        /// </returns>
        public (object rgba1, object rgba2, object rgba8) GetColors(object rgba1, object rgba2, object rgba8) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of particles that are currently in the system.</para>
        /// </summary>
        /// <param name="count">The current number of live particles.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>The current number of live particles.</description></item>
        /// </list>
        /// </returns>
        public double GetCount(double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the direction of the particle emitter (in radians).</para>
        /// </summary>
        /// <param name="direction">The direction of the emitter (radians).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>direction</term><description>The direction of the emitter (radians).</description></item>
        /// </list>
        /// </returns>
        public double GetDirection(double direction) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the area-based spawn parameters for the particles.</para>
        /// </summary>
        /// <param name="distribution">The type of distribution for new particles.</param>
        /// <param name="dx">The maximum spawn distance from the emitter along the x-axis for uniform distribution, or the standard deviation along the x-axis for normal distribution.</param>
        /// <param name="dy">The maximum spawn distance from the emitter along the y-axis for uniform distribution, or the standard deviation along the y-axis for normal distribution.</param>
        /// <param name="angle">The angle in radians of the emission area.</param>
        /// <param name="directionRelativeToCenter">True if newly spawned particles will be oriented relative to the center of the emission area, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>distribution</term><description>The type of distribution for new particles.</description></item>
        /// <item><term>dx</term><description>The maximum spawn distance from the emitter along the x-axis for uniform distribution, or the standard deviation along the x-axis for normal distribution.</description></item>
        /// <item><term>dy</term><description>The maximum spawn distance from the emitter along the y-axis for uniform distribution, or the standard deviation along the y-axis for normal distribution.</description></item>
        /// <item><term>angle</term><description>The angle in radians of the emission area.</description></item>
        /// <item><term>directionRelativeToCenter</term><description>True if newly spawned particles will be oriented relative to the center of the emission area, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public (object distribution, double dx, double dy, double angle, bool directionRelativeToCenter) GetEmissionArea(object distribution, double dx, double dy, double angle, bool directionRelativeToCenter) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the amount of particles emitted per second.</para>
        /// </summary>
        /// <param name="rate">The amount of particles per second.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rate</term><description>The amount of particles per second.</description></item>
        /// </list>
        /// </returns>
        public double GetEmissionRate(double rate) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets how long the particle system will emit particles (if -1 then it emits particles forever).</para>
        /// </summary>
        /// <param name="life">The lifetime of the emitter (in seconds).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>life</term><description>The lifetime of the emitter (in seconds).</description></item>
        /// </list>
        /// </returns>
        public double GetEmitterLifetime(double life) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the mode used when the ParticleSystem adds new particles.</para>
        /// </summary>
        /// <param name="mode">The mode used when the ParticleSystem adds new particles.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mode</term><description>The mode used when the ParticleSystem adds new particles.</description></item>
        /// </list>
        /// </returns>
        public object GetInsertMode(object mode) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the linear acceleration (acceleration along the x and y axes) for particles.</para>
        /// <para>Every particle created will accelerate along the x and y axes between xmin,ymin and xmax,ymax.</para>
        /// </summary>
        /// <param name="xmin">The minimum acceleration along the x axis.</param>
        /// <param name="ymin">The minimum acceleration along the y axis.</param>
        /// <param name="xmax">The maximum acceleration along the x axis.</param>
        /// <param name="ymax">The maximum acceleration along the y axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>xmin</term><description>The minimum acceleration along the x axis.</description></item>
        /// <item><term>ymin</term><description>The minimum acceleration along the y axis.</description></item>
        /// <item><term>xmax</term><description>The maximum acceleration along the x axis.</description></item>
        /// <item><term>ymax</term><description>The maximum acceleration along the y axis.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (double xmin, double ymin, double xmax, double ymax) GetLinearAcceleration(double xmin, double ymin, double xmax = xmin, double ymax = ymin) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the amount of linear damping (constant deceleration) for particles.</para>
        /// </summary>
        /// <param name="min">The minimum amount of linear damping applied to particles.</param>
        /// <param name="max">The maximum amount of linear damping applied to particles.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The minimum amount of linear damping applied to particles.</description></item>
        /// <item><term>max</term><description>The maximum amount of linear damping applied to particles.</description></item>
        /// </list>
        /// </returns>
        public (double min, double max) GetLinearDamping(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the particle image's draw offset.</para>
        /// </summary>
        /// <param name="ox">The x coordinate of the particle image's draw offset.</param>
        /// <param name="oy">The y coordinate of the particle image's draw offset.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>ox</term><description>The x coordinate of the particle image's draw offset.</description></item>
        /// <item><term>oy</term><description>The y coordinate of the particle image's draw offset.</description></item>
        /// </list>
        /// </returns>
        public (double ox, double oy) GetOffset(double ox, double oy) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the lifetime of the particles.</para>
        /// </summary>
        /// <param name="min">The minimum life of the particles (in seconds).</param>
        /// <param name="max">The maximum life of the particles (in seconds).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The minimum life of the particles (in seconds).</description></item>
        /// <item><term>max</term><description>The maximum life of the particles (in seconds).</description></item>
        /// </list>
        /// </returns>
        public (double min, double max) GetParticleLifetime(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the position of the emitter.</para>
        /// </summary>
        /// <param name="x">Position along x-axis.</param>
        /// <param name="y">Position along y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>Position along x-axis.</description></item>
        /// <item><term>y</term><description>Position along y-axis.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y) GetPosition(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the series of Quads used for the particle sprites.</para>
        /// </summary>
        /// <param name="quads">A table containing the Quads used.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>quads</term><description>A table containing the Quads used.</description></item>
        /// </list>
        /// </returns>
        public object GetQuads(object quads) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the radial acceleration (away from the emitter).</para>
        /// </summary>
        /// <param name="min">The minimum acceleration.</param>
        /// <param name="max">The maximum acceleration.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The minimum acceleration.</description></item>
        /// <item><term>max</term><description>The maximum acceleration.</description></item>
        /// </list>
        /// </returns>
        public (double min, double max) GetRadialAcceleration(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the rotation of the image upon particle creation (in radians).</para>
        /// </summary>
        /// <param name="min">The minimum initial angle (radians).</param>
        /// <param name="max">The maximum initial angle (radians).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The minimum initial angle (radians).</description></item>
        /// <item><term>max</term><description>The maximum initial angle (radians).</description></item>
        /// </list>
        /// </returns>
        public (double min, double max) GetRotation(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the amount of size variation (0 meaning no variation and 1 meaning full variation between start and end).</para>
        /// </summary>
        /// <param name="variation">The amount of variation (0 meaning no variation and 1 meaning full variation between start and end).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>variation</term><description>The amount of variation (0 meaning no variation and 1 meaning full variation between start and end).</description></item>
        /// </list>
        /// </returns>
        public double GetSizeVariation(double variation) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the series of sizes by which the sprite is scaled. 1.0 is normal size. The particle system will interpolate between each size evenly over the particle's lifetime.</para>
        /// </summary>
        /// <param name="size1">The first size.</param>
        /// <param name="size2">The second size.</param>
        /// <param name="size8">The eighth size.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>size1</term><description>The first size.</description></item>
        /// <item><term>size2</term><description>The second size.</description></item>
        /// <item><term>size8</term><description>The eighth size.</description></item>
        /// </list>
        /// </returns>
        public (double size1, double size2, double size8) GetSizes(double size1, double size2, double size8) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the speed of the particles.</para>
        /// </summary>
        /// <param name="min">The minimum linear speed of the particles.</param>
        /// <param name="max">The maximum linear speed of the particles.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The minimum linear speed of the particles.</description></item>
        /// <item><term>max</term><description>The maximum linear speed of the particles.</description></item>
        /// </list>
        /// </returns>
        public (double min, double max) GetSpeed(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the spin of the sprite.</para>
        /// </summary>
        /// <param name="min">The minimum spin (radians per second).</param>
        /// <param name="max">The maximum spin (radians per second).</param>
        /// <param name="variation">The degree of variation (0 meaning no variation and 1 meaning full variation between start and end).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The minimum spin (radians per second).</description></item>
        /// <item><term>max</term><description>The maximum spin (radians per second).</description></item>
        /// <item><term>variation</term><description>The degree of variation (0 meaning no variation and 1 meaning full variation between start and end).</description></item>
        /// </list>
        /// </returns>
        // TODO: public (double min, double max, double variation) GetSpin(double min, double max = min, double variation = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the amount of spin variation (0 meaning no variation and 1 meaning full variation between start and end).</para>
        /// </summary>
        /// <param name="variation">The amount of variation (0 meaning no variation and 1 meaning full variation between start and end).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>variation</term><description>The amount of variation (0 meaning no variation and 1 meaning full variation between start and end).</description></item>
        /// </list>
        /// </returns>
        public double GetSpinVariation(double variation) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the amount of directional spread of the particle emitter (in radians).</para>
        /// </summary>
        /// <param name="spread">The spread of the emitter (radians).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>spread</term><description>The spread of the emitter (radians).</description></item>
        /// </list>
        /// </returns>
        public double GetSpread(double spread) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the tangential acceleration (acceleration perpendicular to the particle's direction).</para>
        /// </summary>
        /// <param name="min">The minimum acceleration.</param>
        /// <param name="max">The maximum acceleration.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The minimum acceleration.</description></item>
        /// <item><term>max</term><description>The maximum acceleration.</description></item>
        /// </list>
        /// </returns>
        public (double min, double max) GetTangentialAcceleration(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the texture (Image or Canvas) used for the particles.</para>
        /// </summary>
        /// <param name="texture">The or used for the particles.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>texture</term><description>The or used for the particles.</description></item>
        /// </list>
        /// </returns>
        public object GetTexture(object texture) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether particle angles and rotations are relative to their velocities. If enabled, particles are aligned to the angle of their velocities and rotate relative to that angle.</para>
        /// </summary>
        /// <param name="enable">True if relative particle rotation is enabled, false if it's disabled.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>enable</term><description>True if relative particle rotation is enabled, false if it's disabled.</description></item>
        /// </list>
        /// </returns>
        public bool HasRelativeRotation(bool enable) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether the particle system is actively emitting particles.</para>
        /// </summary>
        /// <param name="active">True if system is active, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>active</term><description>True if system is active, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsActive(bool active) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether the particle system is paused.</para>
        /// </summary>
        /// <param name="paused">True if system is paused, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>paused</term><description>True if system is paused, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsPaused(bool paused) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether the particle system is stopped.</para>
        /// </summary>
        /// <param name="stopped">True if system is stopped, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>stopped</term><description>True if system is stopped, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsStopped(bool stopped) => throw new NotImplementedException();
        /// <summary>
        /// <para>Moves the position of the emitter. This results in smoother particle spawning behaviour than if ParticleSystem:setPosition is used every frame.</para>
        /// </summary>
        /// <param name="x">Position along x-axis.</param>
        /// <param name="y">Position along y-axis.</param>
        public void MoveTo(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Pauses the particle emitter.</para>
        /// </summary>
        public void Pause() => throw new NotImplementedException();
        /// <summary>
        /// <para>Resets the particle emitter, removing any existing particles and resetting the lifetime counter.</para>
        /// </summary>
        public void Reset() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the size of the buffer (the max allowed amount of particles in the system).</para>
        /// </summary>
        /// <param name="size">The buffer size.</param>
        public void SetBufferSize(double size) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a series of colors to apply to the particle sprite. The particle system will interpolate between each color evenly over the particle's lifetime.</para>
        /// <para>Arguments can be passed in groups of four, representing the components of the desired RGBA value, or as tables of RGBA component values, with a default alpha value of 1 if only three values are given. At least one color must be specified. A maximum of eight may be used.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="r1">First color, red component (0-1).</param>
        /// <param name="g1">First color, green component (0-1).</param>
        /// <param name="b1">First color, blue component (0-1).</param>
        /// <param name="a1">First color, alpha component (0-1).</param>
        /// <param name="r2">Second color, red component (0-1).</param>
        /// <param name="g2">Second color, green component (0-1).</param>
        /// <param name="b2">Second color, blue component (0-1).</param>
        /// <param name="a2">Second color, alpha component (0-1).</param>
        /// <param name="r8">Eighth color, red component (0-1).</param>
        /// <param name="g8">Eighth color, green component (0-1).</param>
        /// <param name="b8">Eighth color, blue component (0-1).</param>
        /// <param name="a8">Eighth color, alpha component (0-1).</param>
        public void SetColors(double r1, double g1, double b1, double a1, double r2, double g2, double b2, double a2, double r8, double g8, double b8, double a8) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a series of colors to apply to the particle sprite. The particle system will interpolate between each color evenly over the particle's lifetime.</para>
        /// <para>Arguments can be passed in groups of four, representing the components of the desired RGBA value, or as tables of RGBA component values, with a default alpha value of 1 if only three values are given. At least one color must be specified. A maximum of eight may be used.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="rgba1">First color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1). The alpha is optional and defaults to 1 if it is left out.</param>
        /// <param name="rgba2">Second color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1). The alpha is optional and defaults to 1 if it is left out.</param>
        /// <param name="rgba8">Eighth color, a numerical indexed table with the red, green, blue and alpha values as numbers (0-1). The alpha is optional and defaults to 1 if it is left out.</param>
        public void SetColors(object rgba1, object rgba2, object rgba8) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the direction the particles will be emitted in.</para>
        /// </summary>
        /// <param name="direction">The direction of the particles (in radians).</param>
        public void SetDirection(double direction) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets area-based spawn parameters for the particles. Newly created particles will spawn in an area around the emitter based on the parameters to this function.</para>
        /// </summary>
        /// <param name="distribution">The type of distribution for new particles.</param>
        /// <param name="dx">The maximum spawn distance from the emitter along the x-axis for uniform distribution, or the standard deviation along the x-axis for normal distribution.</param>
        /// <param name="dy">The maximum spawn distance from the emitter along the y-axis for uniform distribution, or the standard deviation along the y-axis for normal distribution.</param>
        /// <param name="angle">The angle in radians of the emission area.</param>
        /// <param name="directionRelativeToCenter">True if newly spawned particles will be oriented relative to the center of the emission area, false otherwise.</param>
        public void SetEmissionArea(object distribution, double dx, double dy, double angle = 0, bool directionRelativeToCenter = false) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the amount of particles emitted per second.</para>
        /// </summary>
        /// <param name="rate">The amount of particles per second.</param>
        public void SetEmissionRate(double rate) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets how long the particle system should emit particles (if -1 then it emits particles forever).</para>
        /// </summary>
        /// <param name="life">The lifetime of the emitter (in seconds).</param>
        public void SetEmitterLifetime(double life) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the mode to use when the ParticleSystem adds new particles.</para>
        /// </summary>
        /// <param name="mode">The mode to use when the ParticleSystem adds new particles.</param>
        public void SetInsertMode(object mode) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the linear acceleration (acceleration along the x and y axes) for particles.</para>
        /// <para>Every particle created will accelerate along the x and y axes between xmin,ymin and xmax,ymax.</para>
        /// </summary>
        /// <param name="xmin">The minimum acceleration along the x axis.</param>
        /// <param name="ymin">The minimum acceleration along the y axis.</param>
        /// <param name="xmax">The maximum acceleration along the x axis.</param>
        /// <param name="ymax">The maximum acceleration along the y axis.</param>
        // TODO: public void SetLinearAcceleration(double xmin, double ymin, double xmax = xmin, double ymax = ymin) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the amount of linear damping (constant deceleration) for particles.</para>
        /// </summary>
        /// <param name="min">The minimum amount of linear damping applied to particles.</param>
        /// <param name="max">The maximum amount of linear damping applied to particles.</param>
        // TODO: public void SetLinearDamping(double min, double max = min) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the offset position which the particle sprite is rotated around.</para>
        /// <para>If this function is not used, the particles rotate around their center.</para>
        /// </summary>
        /// <param name="x">The x coordinate of the rotation offset.</param>
        /// <param name="y">The y coordinate of the rotation offset.</param>
        public void SetOffset(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the lifetime of the particles.</para>
        /// </summary>
        /// <param name="min">The minimum life of the particles (in seconds).</param>
        /// <param name="max">The maximum life of the particles (in seconds).</param>
        // TODO: public void SetParticleLifetime(double min, double max = min) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the position of the emitter.</para>
        /// </summary>
        /// <param name="x">Position along x-axis.</param>
        /// <param name="y">Position along y-axis.</param>
        public void SetPosition(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a series of Quads to use for the particle sprites. Particles will choose a Quad from the list based on the particle's current lifetime, allowing for the use of animated sprite sheets with ParticleSystems.</para>
        /// </summary>
        /// <param name="quad1">The first Quad to use.</param>
        /// <param name="quad2">The second Quad to use.</param>
        public void SetQuads(object quad1, object quad2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a series of Quads to use for the particle sprites. Particles will choose a Quad from the list based on the particle's current lifetime, allowing for the use of animated sprite sheets with ParticleSystems.</para>
        /// </summary>
        /// <param name="quads">A table containing the Quads to use.</param>
        public void SetQuads(object quads) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the radial acceleration (away from the emitter).</para>
        /// </summary>
        /// <param name="min">The minimum acceleration.</param>
        /// <param name="max">The maximum acceleration.</param>
        // TODO: public void SetRadialAcceleration(double min, double max = min) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets whether particle angles and rotations are relative to their velocities. If enabled, particles are aligned to the angle of their velocities and rotate relative to that angle.</para>
        /// </summary>
        /// <param name="enable">True to enable relative particle rotation, false to disable it.</param>
        public void SetRelativeRotation(bool enable) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the rotation of the image upon particle creation (in radians).</para>
        /// </summary>
        /// <param name="min">The minimum initial angle (radians).</param>
        /// <param name="max">The maximum initial angle (radians).</param>
        // TODO: public void SetRotation(double min, double max = min) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the amount of size variation (0 meaning no variation and 1 meaning full variation between start and end).</para>
        /// </summary>
        /// <param name="variation">The amount of variation (0 meaning no variation and 1 meaning full variation between start and end).</param>
        public void SetSizeVariation(double variation) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a series of sizes by which to scale a particle sprite. 1.0 is normal size. The particle system will interpolate between each size evenly over the particle's lifetime.</para>
        /// <para>At least one size must be specified. A maximum of eight may be used.</para>
        /// </summary>
        /// <param name="size1">The first size.</param>
        /// <param name="size2">The second size.</param>
        /// <param name="size8">The eighth size.</param>
        public void SetSizes(double size1, double size2, double size8) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the speed of the particles.</para>
        /// </summary>
        /// <param name="min">The minimum linear speed of the particles.</param>
        /// <param name="max">The maximum linear speed of the particles.</param>
        // TODO: public void SetSpeed(double min, double max = min) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the amount of spin variation (0 meaning no variation and 1 meaning full variation between start and end).</para>
        /// </summary>
        /// <param name="variation">The amount of variation (0 meaning no variation and 1 meaning full variation between start and end).</param>
        public void SetSpinVariation(double variation) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the amount of spread for the system.</para>
        /// </summary>
        /// <param name="spread">The amount of spread (radians).</param>
        public void SetSpread(double spread) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the tangential acceleration (acceleration perpendicular to the particle's direction).</para>
        /// </summary>
        /// <param name="min">The minimum acceleration.</param>
        /// <param name="max">The maximum acceleration.</param>
        // TODO: public void SetTangentialAcceleration(double min, double max = min) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the texture (Image or Canvas) to be used for the particles.</para>
        /// </summary>
        /// <param name="texture">An or to use for the particles.</param>
        public void SetTexture(object texture) => throw new NotImplementedException();
        /// <summary>
        /// <para>Starts the particle emitter.</para>
        /// </summary>
        public void Start() => throw new NotImplementedException();
        /// <summary>
        /// <para>Stops the particle emitter, resetting the lifetime counter.</para>
        /// </summary>
        public void Stop() => throw new NotImplementedException();
        /// <summary>
        /// <para>Updates the particle system; moving, creating and killing particles.</para>
        /// </summary>
        /// <param name="dt">The time (seconds) since last frame.</param>
        public void Update(double dt) => throw new NotImplementedException();
    }
}
