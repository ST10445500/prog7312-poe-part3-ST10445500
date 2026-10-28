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
        public void GetRange_UnseenDevice_IsEmptyAndNotContained()
        {
            // Arrange
            var tree = new TelemetryTree();

            // Act
            var points = tree.GetRange(MacA, DateTime.MinValue, DateTime.MaxValue);

            // Assert
            Assert.Empty(points);
            Assert.False(tree.Contains(MacA));
            Assert.Equal(0, tree.Count(MacA));
        }

        [Fact]
        public void Add_OneReading_IsFoundByARangeAroundIt()
        {
            // Arrange
            var tree = new TelemetryTree();

            // Act
            tree.Add(MacA, At(5));

            // Assert
            Assert.True(tree.Contains(MacA));
            Assert.Single(tree.GetRange(MacA, Start, Start.AddSeconds(10)));
            Assert.Empty(tree.GetRange(MacA, Start.AddSeconds(6), Start.AddSeconds(10)));
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
        public void GetRange_BothEnds_AreIncludedAndOutOfOrderArrivalsComeBackSorted()
        {
            // Arrange
            var tree = new TelemetryTree();

            foreach (var second in new[] { 7, 3, 9, 1, 5, 8, 2, 6, 4, 0 })
            {
                tree.Add(MacA, At(second));
            }

            // Act
            var points = tree.GetRange(MacA, Start.AddSeconds(3), Start.AddSeconds(6));

            // Assert
            Assert.Equal([3, 4, 5, 6], points.Select(point => (int)(point.RecordedAt - Start).TotalSeconds));
        }

        [Fact]
        public void Add_LowerCaseMac_JoinsTheUpperCaseDevice()
        {
            // Arrange
            var tree = new TelemetryTree();
            tree.Add("aa:bb:cc:dd:ee:01", At(1));

            // Act
            tree.Add(MacA, At(2));

            // Assert
            Assert.Equal(2, tree.Count(MacA));
            Assert.True(tree.Contains("aa:bb:cc:dd:ee:01"));
        }

        [Fact]
        public void Add_SameMomentTwice_KeepsBothReadings()
        {
            // Arrange
            var tree = new TelemetryTree();

            // Act
            tree.Add(MacA, At(4, -55));
            tree.Add(MacA, At(4, -56));

            // Assert
            Assert.Equal([-55, -56], tree.GetRange(MacA, Start, Start.AddMinutes(1)).Select(point => point.Rssi));
        }

        [Fact]
        public void Add_BlankMac_Throws()
        {
            // Arrange
            var tree = new TelemetryTree();

            // Act
            var act = () => tree.Add(" ", At(1));

            // Assert
            Assert.Throws<ArgumentException>(act);
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
