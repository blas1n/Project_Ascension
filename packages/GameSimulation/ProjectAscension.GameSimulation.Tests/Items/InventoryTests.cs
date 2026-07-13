using ProjectAscension.GameSimulation.Items;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Items
{
    /// <summary>
    /// Possession, headless — the first hour's map is an ITEM you hold (and can lose), not a UI panel,
    /// so ownership has to be a real, testable thing.
    /// </summary>
    public class InventoryTests
    {
        [Fact]
        public void AddingAnItem_MakesThePlayerOwnIt()
        {
            var inv = new Inventory();
            Assert.False(inv.Has("frontier_map"));

            inv.Add("frontier_map");

            Assert.True(inv.Has("frontier_map"));
            Assert.Equal(1, inv.Count("frontier_map"));
        }

        [Fact]
        public void AddingStacks()
        {
            var inv = new Inventory();
            inv.Add("hide", 2);
            inv.Add("hide", 3);
            Assert.Equal(5, inv.Count("hide"));
        }

        [Theory]
        [InlineData(null, 1)]
        [InlineData("", 1)]
        [InlineData("map", 0)]
        [InlineData("map", -3)]
        public void NothingRewards_AreIgnored(string key, int amount)
        {
            // A contract with no item reward must grant nothing rather than a phantom entry.
            var inv = new Inventory();
            inv.Add(key, amount);
            Assert.Empty(inv.Owned);
        }

        [Fact]
        public void Removing_TakesOnlyWhatIsOwned_AndDropsTheEntryWhenEmpty()
        {
            var inv = new Inventory();
            inv.Add("map", 1);

            Assert.Equal(0, inv.Remove("nothing"));      // cannot lose what you never had
            Assert.Equal(1, inv.Remove("map", 5));       // clamped to what's owned
            Assert.False(inv.Has("map"));
            Assert.Empty(inv.Owned);                      // no zero-count ghost entries
        }

        [Fact]
        public void PartialRemove_KeepsTheRemainder()
        {
            var inv = new Inventory();
            inv.Add("core", 3);
            Assert.Equal(2, inv.Remove("core", 2));
            Assert.Equal(1, inv.Count("core"));
        }
    }
}
