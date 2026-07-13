namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// A tiny seeded PRNG (xorshift32) for gameplay FACTS that must be reproducible bit-for-bit by
    /// whoever else resolves the same event — a server (ADR: Unity is a shell), a replay, another
    /// client. NOT for anything cryptographic, and not a substitute for <c>UnityEngine.Random</c> in
    /// pure cosmetic jitter (VFX that can't change an outcome may keep using engine randomness).
    ///
    /// A readonly struct: sampling returns the drawn value ALONGSIDE the next generator, so callers
    /// thread the state through explicitly rather than mutating a shared instance — a hidden mutable
    /// RNG is itself a source of client/server divergence (two callers drawing in a different order
    /// get different results even with "the same seed").
    /// </summary>
    public readonly struct DeterministicRng
    {
        private readonly uint _state;

        public DeterministicRng(uint seed) => _state = seed == 0 ? 0x9E3779B9u : seed; // 0 is xorshift32's fixed point

        /// <summary>Advance the generator, returning the next raw 32-bit value and the generator's next state.</summary>
        public (uint Value, DeterministicRng Next) NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return (x, new DeterministicRng(x));
        }

        /// <summary>A uniform float in [0, 1).</summary>
        public (float Value, DeterministicRng Next) NextFloat01()
        {
            var (raw, next) = NextUInt();
            return (raw / 4294967296f, next); // raw / 2^32
        }

        /// <summary>Mix two facts into one well-distributed seed (e.g. an actor/weapon-instance id and a
        /// monotonically increasing shot/spawn index), so each combination of the two draws an
        /// independent stream instead of colliding on the raw sum. Deterministic and order-sensitive —
        /// both sides must combine the same two facts in the same order to agree. (Murmur3-style
        /// integer finalizer; not cryptographic, just cheap and well-mixed.)</summary>
        public static uint Combine(uint a, uint b)
        {
            uint h = a * 0x9E3779B1u ^ b;
            h ^= h >> 16; h *= 0x7FEB352Du;
            h ^= h >> 15; h *= 0x846CA68Bu;
            h ^= h >> 16;
            return h;
        }
    }
}
