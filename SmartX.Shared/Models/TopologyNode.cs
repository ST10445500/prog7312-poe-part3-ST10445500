//ST10445500 - PROG7312 - SmartX POE
//TopologyNode

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds one device in the mesh and how far it is from the gateway
    public class TopologyNode
    {
        //identity of the device
        public string MacAddress { get; set; } = string.Empty;

        //the name the device was given, or its mac address if the stream has not heard from it
        public string Name { get; set; } = string.Empty;

        //whether this is the gateway every route ends at
        public bool IsGateway { get; set; }

        //how many hops its last packet took to reach the gateway, or null if the stream has not said
        public int? HopCount { get; set; }

        //whether the device has gone quiet
        public bool Disconnected { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
