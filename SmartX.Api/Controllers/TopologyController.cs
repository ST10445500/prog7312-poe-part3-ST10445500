using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Collections;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//TopologyController

//.....................................o0oSTART OF FILEo0o........................................//

// The API controller receives HTTP requests and sends the work to services.

namespace SmartX.Api.Controllers
{
    //hands the topology page the mesh the processor has built up
    [ApiController]
    [Route("api/topology")]
    public class TopologyController : ControllerBase
    {
        private readonly MeshGraph _graph;
        private readonly DeviceCache _cache;
        private readonly ActiveFaults _faults;
        private readonly TelemetryTree _telemetry;

        public TopologyController(MeshGraph graph, DeviceCache cache, ActiveFaults faults, TelemetryTree telemetry)
        {
            _telemetry = telemetry;
            _faults = faults;
            _cache = cache;
            _graph = graph;
        }

        //..............................................................................//

        //retrieves every device and link the mesh knows about
        [HttpGet]
        public ActionResult<TopologySnapshot> GetTopology()
        {
            // Looked up once as a set, so each node does not walk the list of quiet devices.
            var quiet = _faults.GetDisconnected().ToHashSet();

            return new TopologySnapshot
            {
                Nodes = _graph.GetNodes().Select(mac => DescribeNode(mac, quiet)).ToList(),
                Links = _graph.GetLinks()
                    .Select(link => new TopologyLink { MacA = link.MacA, MacB = link.MacB, Rssi = link.Rssi })
                    .ToList(),
                TakenAt = DateTime.UtcNow
            };
        }

        //..............................................................................//

        //retrieves who can still reach the gateway with the quiet devices taken out, and who could carry for each quiet one
        [HttpGet("reach")]
        public ActionResult<MeshReach> GetReach()
        {
            return _graph.FindReachable(MeshGateway.MacAddress, _faults.GetDisconnected());
        }

        //..............................................................................//

        //retrieves the radio readings a device reported between two moments, oldest first, or everything kept if no range is given
        [HttpGet("{macAddress}/history")]
        public ActionResult<List<TelemetryPoint>> GetHistory(string macAddress, DateTime? from = null, DateTime? to = null)
        {
            if (!_telemetry.Contains(macAddress))
            {
                return NotFound($"The mesh has no readings from {macAddress}.");
            }

            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.MaxValue;

            if (start > end)
            {
                return BadRequest("The start of the range cannot be after the end.");
            }

            return _telemetry.GetRange(macAddress, start, end);
        }

        //..............................................................................//

        //builds one node from what the cache and the quiet list know about the device
        private TopologyNode DescribeNode(string mac, HashSet<string> quiet)
        {
            if (mac == MeshGateway.MacAddress)
            {
                return new TopologyNode { MacAddress = mac, Name = "Gateway", IsGateway = true, HopCount = 0 };
            }

            var device = _cache.Find(mac);

            return new TopologyNode
            {
                MacAddress = mac,
                // A neighbour that has only been heard about has no name or hop count yet.
                Name = device?.Name ?? mac,
                HopCount = device?.Radio.HopCount,
                Disconnected = quiet.Contains(mac)
            };
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
