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
        public async Task FindReachable_WalkWhileLinksAreBeingAdded_NeverThrowsAndEndsWithEveryDevice()
        {
            // Arrange
            var graph = new MeshGraph();
            const int devices = 400;
            var writing = true;

            string MacOf(int index) => $"AA:BB:CC:00:{index / 256:X2}:{index % 256:X2}";

            graph.UpdateNode(MacOf(0), null);

            // Act
            var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            {
                while (Volatile.Read(ref writing))
                {
                    graph.FindReachable(MacOf(0), []);
                    graph.FindSpanningTree(MacOf(0), []);
                    graph.GetLinks();
                }
            })).ToArray();

            Parallel.For(1, devices, index =>
            {
                graph.UpdateNode(MacOf(index), Hears(MacOf(index - 1), -60));
            });

            Volatile.Write(ref writing, false);
            await Task.WhenAll(readers);

            // Assert
            // A line, so once the writers are done every device is reachable and sits in the tree.
            var reach = graph.FindReachable(MacOf(0), []);
            var tree = graph.FindSpanningTree(MacOf(0), []);
            Assert.Equal(devices, reach.Reachable.Count);
            Assert.Equal(devices - 1, tree.Edges.Count);
            Assert.Empty(tree.Unreached);
        }

        private const string Gateway = MeshGateway.MacAddress;

        // Builds a mesh from pairs of macs, each pair one link.
        private static MeshGraph Mesh(params (string, string)[] links)
        {
            var graph = new MeshGraph();

            foreach (var (a, b) in links)
            {
                graph.UpdateNode(a, Hears(b, -60));
            }

            return graph;
        }

        [Fact]
        public void FindReachable_Line_MiddleDown_CutsOffTheFarEnd()
        {
            // Arrange
            var graph = Mesh((Gateway, MacA), (MacA, MacB));

            // Act
            var reach = graph.FindReachable(Gateway, [MacA]);

            // Assert
            Assert.Equal([Gateway], reach.Reachable);
            Assert.Equal([MacA], reach.Down);
            Assert.Equal([MacB], reach.Unreachable);
            Assert.Equal([Gateway], reach.Reroutes[MacA]);
        }

        [Fact]
        public void FindReachable_Ring_OneDown_EveryoneElseGoesTheOtherWay()
        {
            // Arrange
            var graph = Mesh((Gateway, MacA), (MacA, MacB), (MacB, MacC), (MacC, Gateway));

            // Act
            var reach = graph.FindReachable(Gateway, [MacA]);

            // Assert
            Assert.Equal([Gateway, MacB, MacC], reach.Reachable);
            Assert.Empty(reach.Unreachable);
            Assert.Equal([Gateway, MacB], reach.Reroutes[MacA]);
        }

        [Fact]
        public void FindReachable_SplitMesh_ReportsTheOtherHalfUnreachable()
        {
            // Arrange
            var graph = Mesh((Gateway, MacA), (MacB, MacC));

            // Act
            var reach = graph.FindReachable(Gateway, []);

            // Assert
            Assert.Equal([Gateway, MacA], reach.Reachable);
            Assert.Equal([MacB, MacC], reach.Unreachable);
        }

    }
}

//.....................................o0oEND OF FILEo0o..........................................//
