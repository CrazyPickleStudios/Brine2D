using System.Globalization;
using System.Reflection.Metadata.Ecma335;

namespace Brine2D.Math;

/// <summary>
///     <para>A random number generation object which has its own random state.</para>
/// </summary>
public class RandomGenerator : Object
{
    private double _lastRandomNormal;
    private Seed _rangeState;
    private Seed _seed;

    internal RandomGenerator()
    {
        SetSeed(new Seed { Low = 0xCBBF7A44, High = 0x0139408D });
    }

    /// <summary>
    /// <para>Gets the seed of the random number generator object.</para>
    /// <para>The seed is split into two numbers due to Lua's use of doubles for all number values - doubles can't accurately represent integer values above 2^53, but the seed value is an integer number in the range of [0, 2^64 - 1].</para>
    /// </summary>
    /// <list type="bullet">
    /// <item><term>low</term><description>Integer number representing the lower 32 bits of the random number generator's 64 bit seed value.</description></item>
    /// <item><term>high</term><description>Integer number representing the higher 32 bits of the random number generator's 64 bit seed value.</description></item>
    /// </list>
    public (double low, double high) GetSeed()
    {
        return (_seed.Low, _seed.High);
    }

    /// <summary>
    ///     <para>
    ///         Gets the current state of the random number generator. This returns an opaque string which is only useful for
    ///         later use with RandomGenerator:setState in the same major version of LÖVE.
    ///     </para>
    ///     <para>
    ///         This is different from RandomGenerator:getSeed in that getState gets the RandomGenerator's current state,
    ///         whereas getSeed gets the previously set seed number.
    ///     </para>
    /// </summary>
    /// <remarks>The value of the state string does not depend on the current operating system.</remarks>
    /// <returns>
    ///     The current state of the RandomGenerator object, represented as a string.
    /// </returns>
    public string GetState()
    {
        return $"0x{_rangeState.B64:x16}";
    }

    /// <summary>
    ///     Get uniformly distributed pseudo-random real number within [0, 1].
    /// </summary>
    /// <returns>
    ///     The pseudo-random number.
    /// </returns>
    public double Random()
    {
        var r = Rand();
        var bits = (0x3FFUL << 52) | (r >> 12);
        var d = BitConverter.Int64BitsToDouble(unchecked((long)bits));
        return d - 1.0;
    }

    /// <summary>
    ///     Get uniformly distributed pseudo-random integer number within [1, max].
    /// </summary>
    /// <param name="max">The maximum possible value it should return.</param>
    /// <returns>
    ///     The pseudo-random integer number.
    /// </returns>
    public int Random(int max)
    {
        return (int)SysMath.Floor(Random() * max) + 1;
    }

    /// <summary>
    ///     Get uniformly distributed pseudo-random integer number within [min, max].
    /// </summary>
    /// <param name="min">The minimum possible value it should return.</param>
    /// <param name="max">The maximum possible value it should return.</param>
    /// <returns>
    ///     The pseudo-random integer number.
    /// </returns>
    public int Random(int min, int max)
    {
        var range = max - min + 1;
        return (int)SysMath.Floor(Random() * range) + min;
    }

    /// <summary>
    ///     Generates a normally-distributed pseudo-random number.
    /// </summary>
    /// <param name="stddev">Standard deviation of the distribution.</param>
    /// <param name="mean">The mean of the distribution.</param>
    /// <returns>
    ///     Normally distributed random number with variance (stddev)² and the specified mean.
    /// </returns>
    public double RandomNormal(double stddev = 1, double mean = 0)
    {
        if (_lastRandomNormal != double.PositiveInfinity)
        {
            var cached = _lastRandomNormal;
            _lastRandomNormal = double.PositiveInfinity;
            return cached * stddev + mean;
        }

        var u1 = 1.0 - Random(); // (0,1]
        var u2 = 1.0 - Random();
        var r = SysMath.Sqrt(-2.0 * SysMath.Log(u1));
        var phi = 2.0 * SysMath.PI * u2;

        _lastRandomNormal = r * SysMath.Cos(phi);
        return r * SysMath.Sin(phi) * stddev + mean;
    }

    /// <summary>
    ///     <para>Sets the seed of the random number generator using the specified integer number.</para>
    /// </summary>
    /// <param name="seed">
    ///     The integer number with which you want to seed the randomization. Must be within the range of [1,
    ///     2^53].
    /// </param>
    public void SetSeed(long seed)
    {
        SetSeed(new Seed(unchecked((ulong)seed)));
    }

    /// <summary>
    ///     Sets the seed of the random number generator using the specified integer number.
    /// </summary>
    /// <param name="low">The lower 32 bits of the seed value. Must be within the range of [0, 2^32 - 1].</param>
    /// <param name="high">The higher 32 bits of the seed value. Must be within the range of [0, 2^32 - 1].</param>
    public void SetSeed(int low, int high)
    {
        SetSeed(new Seed(unchecked((uint)low), unchecked((uint)high)));
    }

    /// <summary>
    ///     <para>
    ///         Sets the current state of the random number generator. The value used as an argument for this function is an
    ///         opaque string and should only originate from a previous call to RandomGenerator:getState in the same major
    ///         version of LÖVE.
    ///     </para>
    ///     <para>
    ///         This is different from RandomGenerator:setSeed in that setState directly sets the RandomGenerator's current
    ///         implementation-dependent state, whereas setSeed gives it a new seed value.
    ///     </para>
    /// </summary>
    /// <remarks>The effect of the state string does not depend on the current operating system.</remarks>
    /// <param name="state">
    ///     The new state of the RandomGenerator object, represented as a string. This should originate from a
    ///     previous call to RandomGenerator.GetState.
    /// </param>
    public void SetState(string state)
    {
        if (string.IsNullOrEmpty(state) || !state.StartsWith("0x", StringComparison.Ordinal) || state.Length < 3)
        {
            throw new Exception($"Invalid random state: {state}");
        }

        var hexPart = state.Substring(2);

        if (!ulong.TryParse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new Exception($"Invalid random state: {state}");
        }

        _rangeState = new Seed(parsed);

        _lastRandomNormal = double.PositiveInfinity;
    }

    private static ulong WangHash64(ulong key)
    {
        key = ~key + (key << 21); // key = (key << 21) - key - 1;
        key = key ^ (key >> 24);
        key = key + (key << 3) + (key << 8); // key * 265
        key = key ^ (key >> 14);
        key = key + (key << 2) + (key << 4); // key * 21
        key = key ^ (key >> 28);
        key = key + (key << 31);
        return key;
    }

    private ulong Rand()
    {
        _rangeState.B64 ^= _rangeState.B64 >> 12;
        _rangeState.B64 ^= _rangeState.B64 << 25;
        _rangeState.B64 ^= _rangeState.B64 >> 27;
        return _rangeState.B64 * 2685821657736338717UL;
    }

    private void SetSeed(Seed seed)
    {
        _seed = seed;

        do
        {
            _seed.B64 = WangHash64(_seed.B64);
        } while (_seed.B64 == 0);

        _rangeState = _seed;

        _lastRandomNormal = double.PositiveInfinity;
    }

    private struct Seed
    {
        public ulong B64;

        public uint Low
        {
            get => (uint)(B64 & 0xFFFFFFFFu);
            set => B64 = (B64 & 0xFFFFFFFF00000000UL) | value;
        }

        public uint High
        {
            get => (uint)(B64 >> 32);
            set => B64 = ((ulong)value << 32) | (B64 & 0xFFFFFFFFUL);
        }

        public Seed(ulong value)
        {
            B64 = value;
        }

        public Seed(uint low, uint high)
        {
            B64 = ((ulong)high << 32) | low;
        }
    }
}