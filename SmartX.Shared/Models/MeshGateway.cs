//ST10445500 - PROG7312 - SmartX POE
//MeshGateway

//.....................................o0oSTART OF FILEo0o........................................//

// The api and the simulator both have to agree on which address is the gateway, so it is said once here.

namespace SmartX.Shared.Models
{
    //names the one gateway every route through the mesh ends at
    public static class MeshGateway
    {
        //the gateway sits at hop 0, and no sensor is registered under this address
        public const string MacAddress = "AA:BB:CC:DD:EE:00";
    }
}

//.....................................o0oEND OF FILEo0o........................................//
