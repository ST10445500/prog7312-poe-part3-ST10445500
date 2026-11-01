using SmartX.Api.Collections;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//TelemetryTreeTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that each device keeps its own readings, a range includes both ends, and the tree holds up under many threads
    public class TelemetryTreeTests
    {
        private const string MacA = "AA:BB:CC:DD:EE:01";
        private const string MacB = "AA:BB:CC:DD:EE:02";

        private static readonly DateTime Start = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

        private static TelemetryPoint At(int second, int rssi = -60)
        {
            return new TelemetryPoint { RecordedAt = Start.AddSeconds(second), Rssi = rssi };
        }

        [Fact]
        public void GetRange_TwoDevices_KeepsTheirReadingsApart()
        {
            // Arrange
            var tree = new TelemetryTree();
            tree.Add(MacA, At(1, -50));
            tree.Add(MacA, At(2, -51));
            tree.Add(MacB, At(1, -90));

            // Act
            var forA = tree.GetRange(MacA, Start, Start.AddMinutes(1));
            var forB = tree.GetRange(MacB, Start, Start.AddMinutes(1));

            // Assert
            Assert.Equal([-50, -51], forA.Select(point => point.Rssi));
            Assert.Equal([-90], forB.Select(point => point.Rssi));
        }

        [Fact]
        public void Add_ManyThreadsAtOnce_LosesNoReading()
        {
            // Arrange
            var tree = new TelemetryTree();
            const int readings = 20000;

            // Act
            Parallel.For(0, readings, index =>
            {
                tree.Add(index % 2 == 0 ? MacA : MacB, At(index));
            });

            // Assert
            Assert.Equal(readings / 2, tree.Count(MacA));
            Assert.Equal(readings / 2, tree.Count(MacB));
            Assert.Equal(readings / 2, tree.GetRange(MacA, DateTime.MinValue, DateTime.MaxValue).Count);
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
