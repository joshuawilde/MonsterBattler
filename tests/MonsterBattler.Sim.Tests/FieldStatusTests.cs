using MonsterBattler.Game;
using MonsterBattler.Sim;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    public class FieldStatusTests
    {
        [Fact]
        public void Field_ShowsWeatherAndTrickRoom()
        {
            var b = TestBattlers.SetupBattle(TestBattlers.Make("pikachu"), TestBattlers.Make("blissey"));
            Assert.Equal("", FieldStatusText.Field(b)); // nothing initially

            b.SetWeather(Weather.Sandstorm, 5);
            Assert.Contains("Sandstorm", FieldStatusText.Field(b));
        }

        [Fact]
        public void Side_ListsHazardsAndScreens()
        {
            var b = TestBattlers.SetupBattle(TestBattlers.Make("pikachu"), TestBattlers.Make("blissey"));
            Assert.Equal("", FieldStatusText.Side(b.Sides[0]));

            b.AddSideCondition(b.Sides[0], "stealthrock", maxLayers: 1);
            b.AddSideCondition(b.Sides[0], "spikes", maxLayers: 3);
            b.AddSideCondition(b.Sides[0], "spikes", maxLayers: 3); // second layer

            var text = FieldStatusText.Side(b.Sides[0]);
            Assert.Contains("Rocks", text);
            Assert.Contains("Spikes×2", text);
        }
    }
}
