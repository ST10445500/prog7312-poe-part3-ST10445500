using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//MeshLayout

//.....................................o0oSTART OF FILEo0o........................................//

// The layout decides who can hear whom, so the mesh is the same shape on every run.

namespace SmartX.Simulator
{
    //works out which neighbours each simulated device can hear
    public static class MeshLayout
    {
        // Every this many devices, the next two sit in range of the gateway. The rest hop through them.
        private const int GatewayEvery = 7;
        private const int GatewayReach = 2;

        // A second route back, so some devices have more than one way to the gateway.
        private const int CrossLinkSpan = 3;

        //..............................................................................//

        //retrieves the neighbours of each device, keyed on its mac address
        public static Dictionary<string, List<string>> Build(List<string> macAddresses)
        {
            var layout = new Dictionary<string, List<string>>();

            for (var index = 0; index < macAddresses.Count; index++)
            {
                var neighbours = new List<string>();

                if (index % GatewayEvery < GatewayReach)
                {
                    neighbours.Add(MeshGateway.MacAddress);
                }

                // The device before it, so the fleet is one connected mesh and not islands.
                if (index > 0)
                {
                    neighbours.Add(macAddresses[index - 1]);
                }

                // The last device only hears the one before it, so that one is its only way out.
                var deadEnd = index == macAddresses.Count - 1;

                if (index >= CrossLinkSpan && !deadEnd)
                {
                    neighbours.Add(macAddresses[index - CrossLinkSpan]);
                }

                layout[macAddresses[index]] = neighbours;
            }

            return layout;
        }

        //..............................................................................//

        //retrieves the relay the last device depends on, or null when there are too few devices for one
        public static string? FindDeadEndRelay(List<string> macAddresses)
        {
            return macAddresses.Count >= 2 ? macAddresses[^2] : null;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
