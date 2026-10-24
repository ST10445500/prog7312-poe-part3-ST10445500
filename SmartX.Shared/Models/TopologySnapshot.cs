//ST10445500 - PROG7312 - SmartX POE
//TopologySnapshot

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds the whole mesh the topology page draws, in one round trip
    public class TopologySnapshot
    {
        //every device the mesh has met, the gateway included
        public List<TopologyNode> Nodes { get; set; } = new();

        //every link between two of them
        public List<TopologyLink> Links { get; set; } = new();

        //when the gateway built this snapshot
        public DateTime TakenAt { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
