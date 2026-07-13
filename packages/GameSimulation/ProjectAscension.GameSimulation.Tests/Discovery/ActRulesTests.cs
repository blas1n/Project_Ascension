using ProjectAscension.GameSimulation.Discovery;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Discovery
{
    /// <summary>The Act.Moving predicate, headless (ADR: Unity is a shell) — a real displacement
    /// reads as moving; a rounding twitch below the DB-driven threshold does not.</summary>
    public class ActRulesTests
    {
        [Fact]
        public void DisplacementBelowThreshold_IsNotMoving()
        {
            Assert.False(ActRules.IsMoving(0.001f, 0.001f, thresholdMeters: 0.02f));
        }

        [Fact]
        public void DisplacementAboveThreshold_IsMoving()
        {
            Assert.True(ActRules.IsMoving(0.05f, 0f, thresholdMeters: 0.02f));
        }

        [Fact]
        public void DisplacementExactlyAtThreshold_IsNotMoving()
        {
            // Strictly greater-than, matching the "just crossed" semantics used elsewhere (TickTimer).
            Assert.False(ActRules.IsMoving(0.02f, 0f, thresholdMeters: 0.02f));
        }

        [Fact]
        public void DiagonalDisplacement_UsesCombinedMagnitude()
        {
            // 0.015^2 + 0.015^2 ≈ 0.00045 > 0.02^2 (0.0004) — diagonal motion crosses even though
            // neither axis alone would.
            Assert.True(ActRules.IsMoving(0.015f, 0.015f, thresholdMeters: 0.02f));
        }

        [Fact]
        public void NoDisplacement_IsNotMoving()
        {
            Assert.False(ActRules.IsMoving(0f, 0f, thresholdMeters: 0.02f));
        }
    }
}
