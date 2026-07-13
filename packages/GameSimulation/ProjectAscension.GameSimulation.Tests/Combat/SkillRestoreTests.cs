using System;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>
    /// Contract test for the server-response → usable-skill boundary — the seam the playtest
    /// regression lived on (ADR 0007 Phase 4c: graph-only skills carry no primitives, but the Unity
    /// loaders required them and silently dropped every restored skill). By moving the ACCEPTANCE +
    /// build into headless SkillRestore, this catches that class: a Ready graph-only response must
    /// yield a usable skill.
    /// </summary>
    public class SkillRestoreTests
    {
        private const string Graph = "{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Emit\",\"delivery\":\"Burst\",\"tier\":2}}";

        [Fact]
        public void GraphOnlyReady_WithNoPrimitives_YieldsAUsableSkill()
        {
            // The exact shape the graph-only server now returns: Ready, empty primitives, a graph.
            var skill = SkillRestore.FromResponse(
                status: "Ready", name: "Crimson Cascade", manifestation: "Weapon",
                primitives: Array.Empty<string>(),
                contextTags: new[] { "arcane" }, description: "A burst of flame.", effectGraph: Graph);

            Assert.NotNull(skill);                                  // <-- the regression: this was null
            Assert.Equal("Crimson Cascade", skill!.Name);
            Assert.Equal(ManifestationKind.Weapon, skill.Manifestation);
            Assert.IsType<Trigger>(skill.EffectiveGraph);           // runs on the graph path
        }

        [Fact]
        public void NullPrimitives_DoNotThrow_AndStillBuild()
        {
            // JsonUtility may hand null (not []) for an empty array — must still restore.
            var skill = SkillRestore.FromResponse("Ready", "Graphed", "Command",
                primitives: null, contextTags: null, description: null, effectGraph: Graph);
            Assert.NotNull(skill);
            Assert.Equal(ManifestationKind.Command, skill!.Manifestation);
        }

        [Fact]
        public void DiscoveryId_Parses_SoLicensingCanTargetTheDiscovery()
        {
            var id = Guid.NewGuid();
            var skill = SkillRestore.FromResponse(
                status: "Ready", name: "Bolt", manifestation: "Weapon",
                primitives: Array.Empty<string>(), contextTags: null, description: null,
                effectGraph: Graph, behaviors: null, discoveryId: id.ToString());

            Assert.Equal(id, skill!.DiscoveryId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public void DiscoveryId_AbsentOrUnparseable_IsEmpty(string? raw)
        {
            // A legacy/offline skill with no server discovery id can't be licensed — Guid.Empty
            // marks that (ADR 0014), rather than crashing on a bad string.
            var skill = SkillRestore.FromResponse("Ready", "Bolt", "Weapon",
                Array.Empty<string>(), null, null, Graph, discoveryId: raw);

            Assert.Equal(Guid.Empty, skill!.DiscoveryId);
        }

        [Fact]
        public void NotReady_IsRejected()
        {
            Assert.Null(SkillRestore.FromResponse("Pending", "x", "Weapon",
                Array.Empty<string>(), null, null, Graph));
            Assert.Null(SkillRestore.FromResponse(null, "x", "Weapon",
                Array.Empty<string>(), null, null, Graph));
        }

        [Fact]
        public void NoGraph_IsTranslatedFromPrimitives_SoItStillRuns()
        {
            // A legacy row with primitives but no graph (or an unparseable graph) still restores,
            // translated onto the graph path.
            var skill = SkillRestore.FromResponse("Ready", "Old Bolt", "Weapon",
                primitives: new[] { "Projectile x3 r1", "DamageOverTime x1 d2" },
                contextTags: null, description: null, effectGraph: null);
            Assert.NotNull(skill);
            Assert.IsType<Trigger>(skill!.EffectiveGraph);

            var garbled = SkillRestore.FromResponse("Ready", "Bad", "Command",
                new[] { "Beam x2" }, null, null, effectGraph: "{not json");
            Assert.NotNull(garbled); // unparseable graph → translated, not dropped
        }
    }
}
