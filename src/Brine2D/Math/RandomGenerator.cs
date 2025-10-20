namespace Brine2D.Math
{
    /// <summary>
    /// <para>A random number generation object which has its own random state.</para>
    /// </summary>
    // TODO: Requires Review
    public class RandomGenerator
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
        /// <para>Gets the seed of the random number generator object.</para>
        /// <para>The seed is split into two numbers due to Lua's use of doubles for all number values - doubles can't accurately represent integer  values above 2^53, but the seed value is an integer number in the range of [0, 2^64 - 1].</para>
        /// </summary>
        /// <param name="low">Integer number representing the lower 32 bits of the RandomGenerator's 64 bit seed value.</param>
        /// <param name="high">Integer number representing the higher 32 bits of the RandomGenerator's 64 bit seed value.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>low</term><description>Integer number representing the lower 32 bits of the RandomGenerator's 64 bit seed value.</description></item>
        /// <item><term>high</term><description>Integer number representing the higher 32 bits of the RandomGenerator's 64 bit seed value.</description></item>
        /// </list>
        /// </returns>
        public (double low, double high) GetSeed(double low, double high) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the current state of the random number generator. This returns an opaque string which is only useful for later use with RandomGenerator:setState in the same major version of LÖVE.</para>
        /// <para>This is different from RandomGenerator:getSeed in that getState gets the RandomGenerator's current state, whereas getSeed gets the previously set seed number.</para>
        /// </summary>
        /// <param name="state">The current state of the RandomGenerator object, represented as a string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>state</term><description>The current state of the RandomGenerator object, represented as a string.</description></item>
        /// </list>
        /// </returns>
        public string GetState(string state) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the current state of the random number generator. This returns an opaque string which is only useful for later use with RandomGenerator:setState in the same major version of LÖVE.</para>
        /// <para>This is different from RandomGenerator:getSeed in that getState gets the RandomGenerator's current state, whereas getSeed gets the previously set seed number.</para>
        /// </summary>
        // TODO: public void NewRandomGenerator() => throw new NotImplementedException();
        /// <summary>
        /// <para>Generates a pseudo-random number in a platform independent manner.</para>
        /// </summary>
        /// <param name="number">The pseudo-random number.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>number</term><description>The pseudo-random number.</description></item>
        /// </list>
        /// </returns>
        // TODO: public double Random(double number) => throw new NotImplementedException();
        /// <summary>
        /// <para>Generates a pseudo-random number in a platform independent manner.</para>
        /// </summary>
        /// <param name="max">The maximum possible value it should return.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>number</term><description>The pseudo-random integer number.</description></item>
        /// </list>
        /// </returns>
        public double Random(double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Generates a pseudo-random number in a platform independent manner.</para>
        /// </summary>
        /// <param name="min">The minimum possible value it should return.</param>
        /// <param name="max">The maximum possible value it should return.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>number</term><description>The pseudo-random integer number.</description></item>
        /// </list>
        /// </returns>
        public double Random(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Generates a normally-distributed pseudo-random number.</para>
        /// <para>While a typical uniform distribution looks like this:</para>
        /// <para>A typical normal distribution looks like this (note the values aggregating at the center, and the shape of a bell curve):</para>
        /// </summary>
        /// <param name="stddev">Standard deviation of the distribution.</param>
        /// <param name="mean">The mean of the distribution.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>number</term><description>Normally distributed random number with variance (stddev)² and the specified mean.</description></item>
        /// </list>
        /// </returns>
        public double RandomNormal(double stddev = 1, double mean = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the seed of the random number generator using the specified integer number.</para>
        /// </summary>
        /// <param name="seed">The integer number with which you want to seed the randomization. Must be within the range of [1, 2^53].</param>
        public void SetSeed(double seed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the seed of the random number generator using the specified integer number.</para>
        /// </summary>
        /// <param name="low">The lower 32 bits of the seed value. Must be within the range of [0, 2^32 - 1].</param>
        /// <param name="high">The higher 32 bits of the seed value. Must be within the range of [0, 2^32 - 1].</param>
        public void SetSeed(double low, double high) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the seed of the random number generator using the specified integer number.</para>
        /// </summary>
        // TODO: public void NewRandomGenerator() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the current state of the random number generator. The value used as an argument for this function is an opaque string and should only originate from a previous call to RandomGenerator:getState in the same major version of LÖVE.</para>
        /// <para>This is different from RandomGenerator:setSeed in that setState directly sets the RandomGenerator's current implementation-dependent state, whereas setSeed gives it a new seed value.</para>
        /// </summary>
        /// <param name="state">The new state of the RandomGenerator object, represented as a string. This should originate from a previous call to .</param>
        public void SetState(string state) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the current state of the random number generator. The value used as an argument for this function is an opaque string and should only originate from a previous call to RandomGenerator:getState in the same major version of LÖVE.</para>
        /// <para>This is different from RandomGenerator:setSeed in that setState directly sets the RandomGenerator's current implementation-dependent state, whereas setSeed gives it a new seed value.</para>
        /// </summary>
        public void NewRandomGenerator() => throw new NotImplementedException();
    }
}
