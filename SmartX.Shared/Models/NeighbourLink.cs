//ST10445500 - PROG7312 - SmartX POE
//NeighbourLink

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds one neighbour a device can hear, the way a mesh routing table lists it
    public class NeighbourLink
    {
        //identity of the neighbour, which may be another device or the gateway
        public string MacAddress { get; set; } = string.Empty;

        //signal strength the device hears that neighbour at, in dBm, closer to zero is stronger
        public int Rssi { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
