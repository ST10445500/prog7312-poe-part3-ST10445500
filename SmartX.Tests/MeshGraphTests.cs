using SmartX.Api.Collections;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//MeshGraphTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that a link is stored once whichever end reports it, a repeat only refreshes the signal, and the graph holds up under many threads
    public class MeshGraphTests
    {
        private const string MacA = "AA:BB:CC:DD:EE:01";
        private const string MacB = "AA:BB:CC:DD:EE:02";
        private const string MacC = "AA:BB:CC:DD:EE:03";

        private static List<NeighbourLink> Hears(string mac, int rssi)
        {
            return [new NeighbourLink { MacAddress = mac, Rssi = rssi }];
        }

        [Fact]
        public void GetNodes_NothingReported_IsEmpty()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            var nodes = graph.GetNodes();

            // Assert
            Assert.Empty(nodes);
            Assert.Empty(graph.GetLinks());
            Assert.Equal(0, graph.NodeCount);
        }

        [Fact]
        public void UpdateNode_NoNeighbours_AddsTheNodeWithNoLinks()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            graph.UpdateNode(MacA, null);

            // Assert
            Assert.Equal([MacA], graph.GetNodes());
            Assert.Empty(graph.GetLinks());
        }

        [Fact]
        public void UpdateNode_OneNeighbour_AddsBothNodesAndOneLink()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            graph.UpdateNode(MacB, Hears(MacA, -60));

            // Assert
            Assert.Equal([MacA, MacB], graph.GetNodes());
            var link = Assert.Single(graph.GetLinks());
            Assert.Equal((MacA, MacB, -60), link);
        }

        [Fact]
        public void UpdateNode_BothEndsReportTheLink_StoresItOnce()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            graph.UpdateNode(MacA, Hears(MacB, -60));
            graph.UpdateNode(MacB, Hears(MacA, -62));

            // Assert
            Assert.Single(graph.GetLinks());
            Assert.Equal(2, graph.NodeCount);
        }

        [Fact]
        public void UpdateNode_SameLinkTwice_RefreshesTheSignalWithoutDuplicating()
        {
            // Arrange
            var graph = new MeshGraph();
            graph.UpdateNode(MacA, Hears(MacB, -60));

            // Act
            graph.UpdateNode(MacA, Hears(MacB, -75));

            // Assert
            var link = Assert.Single(graph.GetLinks());
            Assert.Equal(-75, link.Rssi);
        }

        [Fact]
        public void UpdateNode_NeighbourNeverReported_BecomesANode()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            graph.UpdateNode(MacA, Hears(MacC, -70));

            // Assert
            Assert.True(graph.Contains(MacC));
            Assert.False(graph.Contains(MacB));
        }

        [Fact]
        public void UpdateNode_LinkToItself_IsIgnored()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            graph.UpdateNode(MacA, Hears(MacA, -40));

            // Assert
            Assert.Equal([MacA], graph.GetNodes());
            Assert.Empty(graph.GetLinks());
        }

        [Fact]
        public void UpdateNode_OnlyTheGateway_LinksTheDeviceToIt()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            graph.UpdateNode(MacA, Hears(MeshGateway.MacAddress, -50));

            // Assert
            Assert.True(graph.Contains(MeshGateway.MacAddress));
            var link = Assert.Single(graph.GetLinks());
            Assert.Equal(MeshGateway.MacAddress, link.MacA);
            Assert.Equal(MacA, link.MacB);
        }

        [Fact]
        public void Contains_LowerCaseMac_FindsTheNode()
        {
            // Arrange
            var graph = new MeshGraph();
            graph.UpdateNode(MacA, null);

            // Act
            var found = graph.Contains("aa:bb:cc:dd:ee:01");

            // Assert
            Assert.True(found);
        }

        [Fact]
        public void UpdateNode_BlankMac_Throws()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            var problem = Record.Exception(() => graph.UpdateNode(" ", null));

            // Assert
            Assert.IsType<ArgumentException>(problem);
        }

        [Fact]
        public void GetLinks_ThreeInAChain_ListsTwoLinksOldestMacFirst()
        {
            // Arrange
            var graph = new MeshGraph();
            graph.UpdateNode(MacB, [new NeighbourLink { MacAddress = MacA, Rssi = -55 }, new NeighbourLink { MacAddress = MacC, Rssi = -65 }]);

            // Act
            var links = graph.GetLinks();

            // Assert
            Assert.Equal([(MacA, MacB, -55), (MacB, MacC, -65)], links);
        }

        [Fact]
        public void UpdateNode_ManyThreadsOnALargeMesh_KeepsEveryNodeAndLinkOnce()
        {
            // Arrange
            var graph = new MeshGraph();
            const int devices = 5000;

            // Act
            Parallel.For(0, devices, index =>
            {
                var mac = $"AA:BB:CC:00:{index / 256:X2}:{index % 256:X2}";
                var previous = $"AA:BB:CC:00:{(index + devices - 1) % devices / 256:X2}:{(index + devices - 1) % devices % 256:X2}";

                graph.UpdateNode(mac, Hears(previous, -60));
            });

            // Assert
            // A ring, so every device has one link to the one before it and none are doubled.
            Assert.Equal(devices, graph.NodeCount);
            Assert.Equal(devices, graph.GetLinks().Count);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
