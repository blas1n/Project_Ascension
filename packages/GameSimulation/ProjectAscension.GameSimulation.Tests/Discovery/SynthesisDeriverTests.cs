using ProjectAscension.GameSimulation.Discovery;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Discovery
{
    /// <summary>
    /// The fusion signal (ADR 0008), headless. These are the properties that stop the engine from
    /// reading POSSESSION as synthesis — the bug that let "shot a lot while holding a catalyst" become
    /// a flame bullet.
    /// </summary>
    public class SynthesisDeriverTests
    {
        [Fact]
        public void WreathingTheShot_IsAFusion()
        {
            // Catalyst, then pistol, near enough to the same moment: the shot carries the arcane.
            var d = new SynthesisDeriver(window: 0.5f);

            Assert.Null(d.Used("arcane", 0f));                       // nothing to fuse with yet
            Assert.Equal("Synthesis:arcane>firearm", d.Used("firearm", 0.2f));
        }

        [Fact]
        public void TheOrderIsPartOfTheSignal()
        {
            // The reverse act is a DIFFERENT act — detonating what the shot left behind — and so it must
            // be a different behaviour, and a different discovery. Same two hands, different play.
            var d = new SynthesisDeriver(window: 0.5f);
            d.Used("firearm", 0f);

            Assert.Equal("Synthesis:firearm>arcane", d.Used("arcane", 0.2f));
        }

        [Fact]
        public void FiringTheSameKindTwice_IsJustFiringTwice()
        {
            // The core correction: repetition is not fusion, however fast you do it.
            var d = new SynthesisDeriver(window: 0.5f);
            d.Used("firearm", 0f);

            Assert.Null(d.Used("firearm", 0.1f));
            Assert.Null(d.Used("firearm", 0.2f));
            Assert.Null(d.Used("firearm", 0.3f));
        }

        [Fact]
        public void TwoUnrelatedAttacks_FarApart_AreNotOneAct()
        {
            var d = new SynthesisDeriver(window: 0.5f);
            d.Used("arcane", 0f);

            Assert.Null(d.Used("firearm", 1.4f)); // a second later is a separate decision
        }

        [Fact]
        public void TheWindowBoundary_Holds()
        {
            var d = new SynthesisDeriver(window: 0.5f);
            d.Used("arcane", 0f);
            Assert.Equal("Synthesis:arcane>firearm", d.Used("firearm", 0.5f)); // exactly at the edge counts

            var e = new SynthesisDeriver(window: 0.5f);
            e.Used("arcane", 0f);
            Assert.Null(e.Used("firearm", 0.51f));                              // past it does not
        }

        [Fact]
        public void FusionsCanChain()
        {
            // Catalyst → shot → blade, each inside the window: two fusions, and the second one is its
            // own act (the burning shot, then the follow-up cut).
            var d = new SynthesisDeriver(window: 0.5f);

            d.Used("arcane", 0f);
            Assert.Equal("Synthesis:arcane>firearm", d.Used("firearm", 0.2f));
            Assert.Equal("Synthesis:firearm>melee", d.Used("melee", 0.4f));
        }

        [Fact]
        public void AnUnclassifiableUse_CannotFuse_AndDoesNotPoisonTheNextOne()
        {
            var d = new SynthesisDeriver(window: 0.5f);
            d.Used("arcane", 0f);

            Assert.Null(d.Used(null, 0.1f));                 // an unknown weapon fuses with nothing
            Assert.Null(d.Used("firearm", 0.2f));            // ...and isn't a primer either

            Assert.Equal("Synthesis:firearm>arcane", d.Used("arcane", 0.3f)); // but the world moves on
        }

        [Fact]
        public void Reset_BreaksTheChain()
        {
            // Dying, or swapping your hands out, means the next blow is not a continuation of the last.
            var d = new SynthesisDeriver(window: 0.5f);
            d.Used("arcane", 0f);
            d.Reset();

            Assert.Null(d.Used("firearm", 0.1f));
        }
    }
}
