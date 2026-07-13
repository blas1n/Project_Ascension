using ProjectAscension.GameSimulation.Items;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Items
{
    /// <summary>
    /// The map has to DO something, or "지도는 자산이다" is just a line in a doc. Holding the chart is
    /// what makes the deep frontier findable — so the survey is what buys the deep contract.
    /// </summary>
    public class MapsTests
    {
        [Fact]
        public void WithoutTheChart_TheDeepFrontierCannotBeFound()
        {
            var empty = new Inventory();
            Assert.False(Maps.CanEnterDeepFrontier(empty));
        }

        [Fact]
        public void HoldingTheChart_OpensTheWay()
        {
            var inv = new Inventory();
            inv.Add(Maps.FrontierMapKey);
            Assert.True(Maps.CanEnterDeepFrontier(inv));
        }

        [Fact]
        public void LosingTheChart_ClosesTheRoadAgain()
        {
            // A possession you can lose is a possession that matters — the road shuts behind it.
            var inv = new Inventory();
            inv.Add(Maps.FrontierMapKey);
            inv.Remove(Maps.FrontierMapKey);

            Assert.False(Maps.CanEnterDeepFrontier(inv));
        }

        [Fact]
        public void OtherLoot_IsNotAMap()
        {
            var inv = new Inventory();
            inv.Add("hide", 5);
            inv.Add("core");
            Assert.False(Maps.CanEnterDeepFrontier(inv));
        }

        [Fact]
        public void NoInventory_IsNotAWayIn()
        {
            Assert.False(Maps.CanEnterDeepFrontier(null));
        }
    }
}
