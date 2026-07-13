using System.Collections.Generic;
using ProjectAscension.GameSimulation.Discovery;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Discovery
{
    /// <summary>
    /// The composition grammar (ADR 0009), headless.
    ///
    /// The point of these tests is not that each operator works — it is that the grammar SUBSUMES the
    /// four bespoke observers it replaced (air-attack, repeated-jump, charged-attack,
    /// weapon-fusion), and then keeps going: combinations nobody wrote a special case for still come
    /// out, because the engine owns the operators, not the combinations.
    /// </summary>
    public class CompositionDeriverTests
    {
        private static List<string> Run(CompositionDeriver d, params Act[] acts)
        {
            var outp = new List<string>();
            foreach (var a in acts) d.Observe(a, outp);
            return outp;
        }

        private static Act A(string verb, string instrument, float t, ActQuality q = ActQuality.None)
            => new(verb, instrument, t, q);

        // --- the four special cases, now just sentences in the grammar --------------------------

        [Fact]
        public void WeaponFusion_WasSynthesisDeriver()
        {
            // Wreathe the shot and fire it, near enough to be one act (ADR 0008, now a Fuse).
            var got = Run(new CompositionDeriver(),
                A("attack", "arcane", 0f),
                A("attack", "firearm", 0.1f));

            Assert.Contains("Fuse:arcane>firearm", got);
        }

        [Fact]
        public void AirAttack_WasItsOwnEvent()
        {
            var got = Run(new CompositionDeriver(),
                A("attack", "firearm", 0f, ActQuality.Airborne));

            Assert.Contains("While:firearm@airborne", got);
        }

        [Fact]
        public void ChargedAttack_WasItsOwnEvent()
        {
            var got = Run(new CompositionDeriver(),
                A("attack", "bow", 0f, ActQuality.Charged));

            Assert.Contains("While:bow@charged", got);
        }

        [Fact]
        public void RepeatedJump_WasAStreakCounter()
        {
            var d = new CompositionDeriver(chainWindow: 1.2f, chainLength: 3);
            var got = Run(d, A("jump", null, 0f), A("jump", null, 0.5f), A("jump", null, 1.0f));

            Assert.Contains("Chain:jump", got);
        }

        // --- and now the part that could not exist before ---------------------------------------

        [Fact]
        public void TheGrammarIsSHARPER_ThanTheSpecialCaseItReplaced()
        {
            // The grammar keeps the INSTRUMENT — and following a jump with a gunshot is not the same
            // mastery as following it with a sword. Two behaviours where a flat verb count would give one.
            var gun = Run(new CompositionDeriver(), A("jump", null, 0f), A("attack", "firearm", 0.3f));
            var blade = Run(new CompositionDeriver(), A("jump", null, 0f), A("attack", "melee", 0.3f));

            Assert.Contains("Seq:jump>firearm", gun);
            Assert.Contains("Seq:jump>melee", blade);
            Assert.DoesNotContain("Seq:jump>melee", gun);
        }

        [Fact]
        public void CombinationsNobodyWroteACaseFor_StillComeOut()
        {
            // Nothing in the engine knows what "jump into a catalyst cast" is. It falls out anyway —
            // which is the whole argument for a grammar over a pile of observers.
            var got = Run(new CompositionDeriver(),
                A("jump", null, 0f),
                A("attack", "arcane", 0.15f, ActQuality.Airborne));

            Assert.Contains("Fuse:jump>arcane", got);
            Assert.Contains("While:arcane@airborne", got);
        }

        [Fact]
        public void TightnessIsTheSignal_FuseAndSeqAreDifferentMasteries()
        {
            // Weaving two hands in a tenth of a second is not the same skill as stringing them over
            // half a second — and so they must be able to become different discoveries.
            var tight = Run(new CompositionDeriver(fuseWindow: 0.22f, seqWindow: 0.65f),
                A("attack", "arcane", 0f), A("attack", "firearm", 0.1f));
            var loose = Run(new CompositionDeriver(fuseWindow: 0.22f, seqWindow: 0.65f),
                A("attack", "arcane", 0f), A("attack", "firearm", 0.5f));

            Assert.Contains("Fuse:arcane>firearm", tight);
            Assert.DoesNotContain("Seq:arcane>firearm", tight);

            Assert.Contains("Seq:arcane>firearm", loose);
            Assert.DoesNotContain("Fuse:arcane>firearm", loose);
        }

        [Fact]
        public void OneActCanCompleteSeveralThingsAtOnce()
        {
            // A third quick jump, taken in the air: a chain AND a quality at once.
            var d = new CompositionDeriver(chainLength: 3);
            var got = Run(d,
                A("jump", null, 0f),
                A("jump", null, 0.4f),
                A("jump", null, 0.8f, ActQuality.Airborne));

            Assert.Contains("Chain:jump", got);
            Assert.Contains("While:jump@airborne", got);
        }

        [Fact]
        public void DoingTheSameThingTwice_IsAChain_NeverACombination()
        {
            // The correction that started all this: repetition is not fusion, however fast you do it.
            var got = Run(new CompositionDeriver(),
                A("attack", "firearm", 0f),
                A("attack", "firearm", 0.05f));

            Assert.DoesNotContain("Fuse:firearm>firearm", got);
            Assert.DoesNotContain("Seq:firearm>firearm", got);
        }

        [Fact]
        public void ActsTooFarApart_AreNotOneAct()
        {
            var got = Run(new CompositionDeriver(seqWindow: 0.65f),
                A("attack", "arcane", 0f),
                A("attack", "firearm", 2f));

            Assert.Empty(got);
        }

        [Fact]
        public void OrderIsPartOfTheSignal()
        {
            var forward = Run(new CompositionDeriver(), A("attack", "arcane", 0f), A("attack", "firearm", 0.1f));
            var reverse = Run(new CompositionDeriver(), A("attack", "firearm", 0f), A("attack", "arcane", 0.1f));

            Assert.Contains("Fuse:arcane>firearm", forward);
            Assert.Contains("Fuse:firearm>arcane", reverse);
            Assert.DoesNotContain("Fuse:arcane>firearm", reverse);
        }

        [Fact]
        public void Reset_BreaksTheStream()
        {
            var d = new CompositionDeriver();
            var outp = new List<string>();
            d.Observe(A("attack", "arcane", 0f), outp);
            d.Reset();
            d.Observe(A("attack", "firearm", 0.1f), outp);

            Assert.Empty(outp); // dying between two blows does not fuse them
        }

        [Fact]
        public void AnActWithNothingToNameItself_IsIgnored()
        {
            var got = Run(new CompositionDeriver(), A(null, null, 0f), A("jump", null, 0.1f));
            Assert.Empty(got);
        }
    }
}
