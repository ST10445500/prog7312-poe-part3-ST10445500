using SmartX.Api.Collections;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//MeshGraphSpanningTreeTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that prim takes the cheapest links from the gateway, leaves quiet devices out, reports who it cannot reach and gives the same tree on a tie
    public class MeshGraphSpanningTreeTests
    {
        private const string Gateway = "AA:BB:CC:DD:EE:00";
        private const string MacA = "AA:BB:CC:DD:EE:01";
        private const string MacB = "AA:BB:CC:DD:EE:02";
        private const string MacC = "AA:BB:CC:DD:EE:03";

        private static void Link(MeshGraph graph, string one, string two, int rssi)
        {
            graph.UpdateNode(one, [new NeighbourLink { MacAddress = two, Rssi = rssi }]);
        }

        [Fact]
        public void FindSpanningTree_EmptyGraph_HasNoEdgesAndNoCost()
        {
            // Arrange
            var graph = new MeshGraph();

            // Act
            var tree = graph.FindSpanningTree(Gateway, []);

            // Assert
            Assert.Empty(tree.Edges);
            Assert.Empty(tree.Unreached);
            Assert.Equal(0, tree.TotalCost);
        }

        [Fact]
        public void FindSpanningTree_GatewayAlone_HasNoEdgesAndNoCost()
        {
            // Arrange
            var graph = new MeshGraph();
            graph.UpdateNode(Gateway, null);

            // Act
            var tree = graph.FindSpanningTree(Gateway, []);

            // Assert
            Assert.Empty(tree.Edges);
            Assert.Empty(tree.Parents);
            Assert.Equal(0, tree.TotalCost);
        }

        [Fact]
        public void FindSpanningTree_SmallMesh_TakesTheCheapestLinks()
        {
            // Arrange
            // Worked by hand: gateway to A costs 40, then A to B 50 beats gateway to B 70, then B to C 60 beats A to C 90.
            var graph = new MeshGraph();
            Link(graph, Gateway, MacA, -40);
            Link(graph, Gateway, MacB, -70);
            Link(graph, MacA, MacB, -50);
            Link(graph, MacB, MacC, -60);
            Link(graph, MacA, MacC, -90);

            // Act
            var tree = graph.FindSpanningTree(Gateway, []);

            // Assert
            Assert.Equal(150, tree.TotalCost);
            Assert.Equal(3, tree.Edges.Count);
            Assert.Equal(Gateway, tree.Parents[MacA]);
            Assert.Equal(MacA, tree.Parents[MacB]);
            Assert.Equal(MacB, tree.Parents[MacC]);
            Assert.Empty(tree.Unreached);
        }

        [Fact]
        public void FindSpanningTree_DisconnectedDevice_IsListedAsUnreached()
        {
            // Arrange
            var graph = new MeshGraph();
            Link(graph, Gateway, MacA, -40);
            Link(graph, MacB, MacC, -50);

            // Act
            var tree = graph.FindSpanningTree(Gateway, []);

            // Assert
            Assert.Equal([MacB, MacC], tree.Unreached);
            Assert.Equal(40, tree.TotalCost);
            Assert.Single(tree.Edges);
        }

        [Fact]
        public void FindSpanningTree_QuietDevice_IsLeftOutAndTheTreeGoesRound()
        {
            // Arrange
            var graph = new MeshGraph();
            Link(graph, Gateway, MacA, -40);
            Link(graph, MacA, MacB, -40);
            Link(graph, Gateway, MacC, -80);
            Link(graph, MacC, MacB, -80);

            // Act
            var tree = graph.FindSpanningTree(Gateway, [MacA]);

            // Assert
            Assert.False(tree.Parents.ContainsKey(MacA));
            Assert.Equal(MacC, tree.Parents[MacB]);
            Assert.Equal(160, tree.TotalCost);
            Assert.Empty(tree.Unreached);
        }

        [Fact]
        public void FindSpanningTree_QuietDeviceIsTheOnlyWay_CutsOffTheOnesBehindIt()
        {
            // Arrange
            var graph = new MeshGraph();
            Link(graph, Gateway, MacA, -40);
            Link(graph, MacA, MacB, -40);

            // Act
            var tree = graph.FindSpanningTree(Gateway, [MacA]);

            // Assert
            Assert.Empty(tree.Edges);
            Assert.Equal([MacB], tree.Unreached);
        }

        [Fact]
        public void FindSpanningTree_EqualWeights_GivesTheSameTreeEveryTime()
        {
            // Arrange
            var graph = new MeshGraph();
            Link(graph, Gateway, MacA, -50);
            Link(graph, Gateway, MacB, -50);
            Link(graph, MacA, MacC, -50);
            Link(graph, MacB, MacC, -50);

            // Act
            var first = graph.FindSpanningTree(Gateway, []);
            var second = graph.FindSpanningTree(Gateway, []);

            // Assert
            // The tie goes to the lower mac, so C hangs off A and not B.
            Assert.Equal(MacA, first.Parents[MacC]);
            Assert.Equal(first.Parents, second.Parents);
            Assert.Equal(150, first.TotalCost);
        }

        [Fact]
        public void FindSpanningTree_LowercaseGateway_StillFindsIt()
        {
            // Arrange
            var graph = new MeshGraph();
            Link(graph, Gateway, MacA, -40);

            // Act
            var tree = graph.FindSpanningTree(Gateway.ToLowerInvariant(), []);

            // Assert
            Assert.Equal(40, tree.TotalCost);
        }

        [Fact]
        public void FindSpanningTree_FiveThousandInALine_ReachesTheEnd()
        {
            // Arrange
            var graph = new MeshGraph();
            var previous = Gateway;

            for (var i = 1; i <= 5000; i++)
            {
                var mac = $"AA:BB:CC:{i / 65536:X2}:{i / 256 % 256:X2}:{i % 256:X2}";
                Link(graph, previous, mac, -50);
                previous = mac;
            }

            // Act
            var tree = graph.FindSpanningTree(Gateway, []);

            // Assert
            Assert.Equal(5000, tree.Edges.Count);
            Assert.Equal(250000, tree.TotalCost);
            Assert.Empty(tree.Unreached);
        }

        [Fact]
        public void FindSpanningTree_WhileLinksAreUpdated_StaysConsistent()
        {
            // Arrange
            var graph = new MeshGraph();
            Link(graph, Gateway, MacA, -40);
            var ring = Enumerable.Range(0, 200).Select(i => $"AA:BB:CC:00:00:{i:X2}").ToList();

            // Act
            Parallel.For(0, 200, i =>
            {
                Link(graph, i == 0 ? MacA : ring[i - 1], ring[i], -60);
                graph.FindSpanningTree(Gateway, []);
            });

            var tree = graph.FindSpanningTree(Gateway, []);

            // Assert
            Assert.Equal(201, tree.Edges.Count);
            Assert.Equal(40 + 200 * 60, tree.TotalCost);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
