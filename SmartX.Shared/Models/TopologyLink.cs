//ST10445500 - PROG7312 - SmartX POE
//TopologyLink

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds one link between two devices, listed once whichever end reported it
    public class TopologyLink
    {
        //the device at one end of the link, the lower mac address of the two
        public string MacA { get; set; } = string.Empty;

        //the device at the other end
        public string MacB { get; set; } = string.Empty;

        //signal strength of the link in dBm, closer to zero is stronger
        public int Rssi { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
