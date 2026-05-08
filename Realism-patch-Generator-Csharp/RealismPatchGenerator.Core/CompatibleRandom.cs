namespace RealismPatchGenerator.Core;

internal sealed class CompatibleRandom
{
    private const int StateSize = 624;
    private const int MiddleWord = 397;
    private const uint MatrixA = 0x9908b0df;
    private const uint UpperMask = 0x80000000;
    private const uint LowerMask = 0x7fffffff;

    private readonly uint[] state = new uint[StateSize];
    private int index = StateSize + 1;

    public CompatibleRandom(uint seed)
    {
        Initialize(seed);
    }

    public double NextDouble()
    {
        var first = GenerateUInt32() >> 5;
        var second = GenerateUInt32() >> 6;
        return ((first * 67108864.0) + second) * (1.0 / 9007199254740992.0);
    }

    public double Triangular(double low, double high, double mode)
    {
        if (low == high)
        {
            return low;
        }

        var u = NextDouble();
        var c = (mode - low) / (high - low);
        if (u > c)
        {
            u = 1.0 - u;
            c = 1.0 - c;
            (low, high) = (high, low);
        }

        return low + ((high - low) * Math.Sqrt(u * c));
    }

    private void Initialize(uint seed)
    {
        InitializeFromSeed(19650218u);

        var key = new[] { seed };
        var stateIndex = 1;
        var keyIndex = 0;
        var loopCount = Math.Max(StateSize, key.Length);
        for (var remaining = loopCount; remaining > 0; remaining -= 1)
        {
            state[stateIndex] = unchecked((state[stateIndex] ^ ((state[stateIndex - 1] ^ (state[stateIndex - 1] >> 30)) * 1664525u)) + key[keyIndex] + (uint)keyIndex);
            state[stateIndex] &= 0xffffffffu;

            stateIndex += 1;
            keyIndex += 1;

            if (stateIndex >= StateSize)
            {
                state[0] = state[StateSize - 1];
                stateIndex = 1;
            }

            if (keyIndex >= key.Length)
            {
                keyIndex = 0;
            }
        }

        for (var remaining = StateSize - 1; remaining > 0; remaining -= 1)
        {
            state[stateIndex] = unchecked((state[stateIndex] ^ ((state[stateIndex - 1] ^ (state[stateIndex - 1] >> 30)) * 1566083941u)) - (uint)stateIndex);
            state[stateIndex] &= 0xffffffffu;

            stateIndex += 1;
            if (stateIndex >= StateSize)
            {
                state[0] = state[StateSize - 1];
                stateIndex = 1;
            }
        }

        state[0] = 0x80000000u;
        index = StateSize;
    }

    private void InitializeFromSeed(uint seed)
    {
        state[0] = seed;
        for (var offset = 1; offset < StateSize; offset += 1)
        {
            state[offset] = unchecked(1812433253u * (state[offset - 1] ^ (state[offset - 1] >> 30)) + (uint)offset);
        }
    }

    private uint GenerateUInt32()
    {
        if (index >= StateSize)
        {
            Twist();
        }

        var value = state[index];
        index += 1;

        value ^= value >> 11;
        value ^= (value << 7) & 0x9d2c5680;
        value ^= (value << 15) & 0xefc60000;
        value ^= value >> 18;
        return value;
    }

    private void Twist()
    {
        for (var offset = 0; offset < StateSize; offset += 1)
        {
            var mixed = (state[offset] & UpperMask) | (state[(offset + 1) % StateSize] & LowerMask);
            state[offset] = state[(offset + MiddleWord) % StateSize] ^ (mixed >> 1);
            if ((mixed & 1) != 0)
            {
                state[offset] ^= MatrixA;
            }
        }

        index = 0;
    }
}