//ST10445500 - PROG7312 - SmartX POE
//MeshReach

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds who can still reach the gateway, who cannot, and where the quiet devices could be routed round
    public class MeshReach
    {
        //every device with a live path to the gateway, the gateway included
        public List<string> Reachable { get; set; } = new();

        //every quiet device the mesh knows about
        public List<string> Down { get; set; } = new();

        //devices that are not quiet but have no live path to the gateway
        public List<string> Unreachable { get; set; } = new();

        //for each quiet device, the neighbours that can still reach the gateway
        public Dictionary<string, List<string>> Reroutes { get; set; } = new();
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
