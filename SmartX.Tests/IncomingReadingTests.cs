using System.Text.Json;
using SmartX.Api.Models.Telemetry;

//ST10445500 - PROG7312 - SmartX POE
//IncomingReadingTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that a reading with no neighbours still binds the way it did before, and one with neighbours keeps them
    public class IncomingReadingTests
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        [Fact]
        public void Deserialize_PayloadWithoutNeighbours_LeavesNeighboursNull()
        {
            // Arrange
            var json = """{"macAddress":"AA:BB:CC:DD:EE:01","value":42.5,"rssi":-60,"hopCount":2}""";

            // Act
            var reading = JsonSerializer.Deserialize<IncomingReading>(json, Options);

            // Assert
            Assert.NotNull(reading);
            Assert.Equal(42.5, reading.Value);
            Assert.Equal(-60, reading.Rssi);
            Assert.Null(reading.Neighbours);
        }

        [Fact]
        public void Deserialize_PayloadWithNeighbours_KeepsEachMacAndRssi()
        {
            // Arrange
            var json = """
                {"macAddress":"AA:BB:CC:DD:EE:02","state":true,
                 "neighbours":[{"macAddress":"AA:BB:CC:DD:EE:00","rssi":-55},{"macAddress":"AA:BB:CC:DD:EE:01","rssi":-72}]}
                """;

            // Act
            var reading = JsonSerializer.Deserialize<IncomingReading>(json, Options);

            // Assert
            Assert.NotNull(reading?.Neighbours);
            Assert.Equal(2, reading.Neighbours.Count);
            Assert.Equal("AA:BB:CC:DD:EE:00", reading.Neighbours[0].MacAddress);
            Assert.Equal(-72, reading.Neighbours[1].Rssi);
        }

        [Fact]
        public void Deserialize_EmptyNeighboursList_GivesAnEmptyListNotNull()
        {
            // Arrange
            var json = """{"macAddress":"AA:BB:CC:DD:EE:03","value":300,"neighbours":[]}""";

            // Act
            var reading = JsonSerializer.Deserialize<IncomingReading>(json, Options);

            // Assert
            Assert.NotNull(reading?.Neighbours);
            Assert.Empty(reading.Neighbours);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
