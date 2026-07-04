using System;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The continuous bonuses a passive discovery grants — applied always, not invoked:
    /// <see cref="DamageReduction"/> (a fraction of incoming damage prevented) and
    /// <see cref="Lifesteal"/> (a fraction of damage dealt returned as health). Passives
    /// add up across the player's discoveries (<c>+</c>), clamped to sane caps.
    /// </summary>
    // A record class (not record struct) so it compiles under Unity's C# 9.
    // ExtraJumps is a movement CAPABILITY (extra air jumps, e.g. double jump) — a mobility
    // passive grants it, and it is used via the jump input, not cast.
    public sealed record PassiveEffect(float DamageReduction, float Lifesteal, int ExtraJumps = 0)
    {
        public const float MaxDamageReduction = 0.75f;
        public const float MaxLifesteal = 1.0f;
        public const int MaxExtraJumps = 2;

        public static readonly PassiveEffect None = new(0f, 0f, 0);

        public static PassiveEffect operator +(PassiveEffect a, PassiveEffect b)
            => new(
                Math.Min(MaxDamageReduction, a.DamageReduction + b.DamageReduction),
                Math.Min(MaxLifesteal, a.Lifesteal + b.Lifesteal),
                Math.Min(MaxExtraJumps, a.ExtraJumps + b.ExtraJumps));
    }
}
