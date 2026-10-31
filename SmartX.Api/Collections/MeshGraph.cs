using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//MeshGraph

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so the gateway can see which devices hear which, as a graph.

namespace SmartX.Api.Collections
{
    //keeps every device as a node and every link between two devices as an edge in an adjacency list
    //updating a node is O(degree) because it walks that node's own edges, and a snapshot is O(nodes + edges)
    //every member takes the one lock so the processor and requests can share it
    public class MeshGraph
    {
        //one link as the node on one end sees it, so every link is stored twice
        private class Edge
        {
            public string Neighbour { get; set; } = string.Empty;

            public int Rssi { get; set; }
        }

        private readonly Dictionary<string, List<Edge>> _nodes = new();
        private readonly object _lock = new();

        //..............................................................................//

        //retrieves how many devices are in the graph
        public int NodeCount
        {
            get
            {
                lock (_lock)
                {
                    return _nodes.Count;
                }
            }
        }

        //..............................................................................//

        //records a device and the neighbours it says it can hear, linking each pair both ways
        //a neighbour the graph has not met becomes a node, and a link reported again only has its signal refreshed
        public void UpdateNode(string macAddress, List<NeighbourLink>? neighbours)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                throw new ArgumentException("A node needs a mac address.", nameof(macAddress));
            }

            var mac = Key(macAddress);

            lock (_lock)
            {
                AddNode(mac);

                foreach (var neighbour in neighbours ?? [])
                {
                    if (string.IsNullOrWhiteSpace(neighbour.MacAddress))
                    {
                        continue;
                    }

                    var other = Key(neighbour.MacAddress);

                    // A device cannot hear itself, so a link back to the same mac is a bad report.
                    if (other == mac)
                    {
                        continue;
                    }

                    AddNode(other);
                    SetEdge(mac, other, neighbour.Rssi);
                    SetEdge(other, mac, neighbour.Rssi);
                }
            }
        }

        //..............................................................................//

        //checks whether the graph has met a device
        public bool Contains(string macAddress)
        {
            lock (_lock)
            {
                return _nodes.ContainsKey(Key(macAddress));
            }
        }

        //..............................................................................//

        //retrieves a sorted copy of every device in the graph
        public List<string> GetNodes()
        {
            lock (_lock)
            {
                return _nodes.Keys
                    .OrderBy(mac => mac, StringComparer.Ordinal)
                    .ToList();
            }
        }

        //..............................................................................//

        //retrieves a copy of every link once, with the lower mac first
        public List<(string MacA, string MacB, int Rssi)> GetLinks()
        {
            var links = new List<(string MacA, string MacB, int Rssi)>();

            lock (_lock)
            {
                foreach (var (mac, edges) in _nodes)
                {
                    foreach (var edge in edges)
                    {
                        // Each link is stored at both ends, so only the lower end hands it out.
                        if (string.CompareOrdinal(mac, edge.Neighbour) < 0)
                        {
                            links.Add((mac, edge.Neighbour, edge.Rssi));
                        }
                    }
                }
            }

            return links
                .OrderBy(link => link.MacA, StringComparer.Ordinal)
                .ThenBy(link => link.MacB, StringComparer.Ordinal)
                .ToList();
        }

        //..............................................................................//

        //walks the mesh breadth first from the gateway, never stepping onto a quiet device, in O(nodes + edges)
        //reports who is reachable, who is quiet, who is cut off, and which live neighbours each quiet device has left
        public MeshReach FindReachable(string gatewayMac, ICollection<string> quiet)
        {
            var gateway = Key(gatewayMac);
            var down = quiet.Select(Key).ToHashSet();
            var seen = new HashSet<string>();
            var queue = new Queue<string>();
            var result = new MeshReach();

            lock (_lock)
            {
                if (_nodes.ContainsKey(gateway))
                {
                    seen.Add(gateway);
                    queue.Enqueue(gateway);
                }

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    result.Reachable.Add(current);

                    foreach (var edge in _nodes[current])
                    {
                        // A quiet device is not walked through, so anything behind it has to find another way round.
                        if (down.Contains(edge.Neighbour) || !seen.Add(edge.Neighbour))
                        {
                            continue;
                        }

                        queue.Enqueue(edge.Neighbour);
                    }
                }

                foreach (var (mac, edges) in _nodes)
                {
                    if (down.Contains(mac))
                    {
                        result.Down.Add(mac);
                        result.Reroutes[mac] = edges
                            .Where(edge => seen.Contains(edge.Neighbour))
                            .Select(edge => edge.Neighbour)
                            .OrderBy(neighbour => neighbour, StringComparer.Ordinal)
                            .ToList();
                    }
                    else if (!seen.Contains(mac))
                    {
                        result.Unreachable.Add(mac);
                    }
                }
            }

            result.Reachable.Sort(StringComparer.Ordinal);
            result.Down.Sort(StringComparer.Ordinal);
            result.Unreachable.Sort(StringComparer.Ordinal);
            return result;
        }

        //..............................................................................//

        //grows a minimum spanning tree out from the gateway with Prim, never taking a link to a quiet device
        //a link costs its signal strength without the minus sign, so a weaker link costs more, and the cheapest link out of the tree is taken each time
        //O(edges log nodes) with the built-in priority queue, and a tie goes to the lower mac so equal weights give the same tree every time
        public MeshTree FindSpanningTree(string gatewayMac, ICollection<string> quiet)
        {
            var gateway = Key(gatewayMac);
            var down = quiet.Select(Key).ToHashSet();
            var inTree = new HashSet<string>();
            var result = new MeshTree();

            var queue = new PriorityQueue<(string From, string To, int Cost), (int Cost, string To)>(
                Comparer<(int Cost, string To)>.Create((x, y) =>
                {
                    var byCost = x.Cost.CompareTo(y.Cost);
                    return byCost != 0 ? byCost : string.CompareOrdinal(x.To, y.To);
                }));

            lock (_lock)
            {
                if (_nodes.ContainsKey(gateway))
                {
                    inTree.Add(gateway);
                    PushEdges(queue, gateway, inTree, down);
                }

                while (queue.Count > 0)
                {
                    var (from, to, cost) = queue.Dequeue();

                    // The same device can be queued from two sides, the cheaper one came out first.
                    if (!inTree.Add(to))
                    {
                        continue;
                    }

                    result.Edges.Add(new TopologyLink { MacA = from, MacB = to, Rssi = -cost });
                    result.Parents[to] = from;
                    result.TotalCost += cost;
                    PushEdges(queue, to, inTree, down);
                }

                foreach (var mac in _nodes.Keys)
                {
                    if (!inTree.Contains(mac) && !down.Contains(mac))
                    {
                        result.Unreached.Add(mac);
                    }
                }
            }

            result.Unreached.Sort(StringComparer.Ordinal);
            return result;
        }

        //..............................................................................//

        //queues every link out of a node that leads to a live device not yet in the tree, called with the lock held
        private void PushEdges(
            PriorityQueue<(string From, string To, int Cost), (int Cost, string To)> queue,
            string from,
            HashSet<string> inTree,
            HashSet<string> down)
        {
            foreach (var edge in _nodes[from])
            {
                if (inTree.Contains(edge.Neighbour) || down.Contains(edge.Neighbour))
                {
                    continue;
                }

                var cost = Math.Abs(edge.Rssi);
                queue.Enqueue((from, edge.Neighbour, cost), (cost, edge.Neighbour));
            }
        }

        //..............................................................................//

        //adds a node with no edges if the graph has not met the device, called with the lock held
        private void AddNode(string mac)
        {
            if (!_nodes.ContainsKey(mac))
            {
                _nodes[mac] = new List<Edge>();
            }
        }

        //..............................................................................//

        //adds the edge from one node to another, or refreshes its signal if it is already there, called with the lock held
        private void SetEdge(string from, string to, int rssi)
        {
            var edges = _nodes[from];

            // A node only has a handful of neighbours, so a walk is fine here.
            var existing = edges.Find(edge => edge.Neighbour == to);

            if (existing != null)
            {
                existing.Rssi = rssi;
                return;
            }

            edges.Add(new Edge { Neighbour = to, Rssi = rssi });
        }

        //..............................................................................//

        //puts a mac address in the one form the nodes are keyed on
        private static string Key(string macAddress)
        {
            return macAddress.ToUpperInvariant();
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
