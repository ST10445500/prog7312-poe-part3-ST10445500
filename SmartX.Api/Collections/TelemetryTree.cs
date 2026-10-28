using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//TelemetryTree

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so the radio readings of a device can be found by time range.

namespace SmartX.Api.Collections
{
    //keeps one avl tree of radio readings for each device, keyed on the moment the device recorded them
    //an add is O(log n) and a range is O(log n + k), and nothing is ever dropped, so memory grows with the readings
    //every member takes the one lock because the trees themselves are not thread safe
    public class TelemetryTree
    {
        private readonly Dictionary<string, AvlTree<DateTime, TelemetryPoint>> _devices = new();
        private readonly object _lock = new();

        //..............................................................................//

        //retrieves how many readings a device has, zero if it has none
        public int Count(string macAddress)
        {
            lock (_lock)
            {
                return _devices.TryGetValue(Key(macAddress), out var tree) ? tree.Count : 0;
            }
        }

        //..............................................................................//

        //checks whether the device has any readings in the tree
        public bool Contains(string macAddress)
        {
            lock (_lock)
            {
                return _devices.ContainsKey(Key(macAddress));
            }
        }

        //..............................................................................//

        //adds a reading under the moment it was recorded, a second one at the same moment is kept beside the first
        public void Add(string macAddress, TelemetryPoint point)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                throw new ArgumentException("A reading needs a mac address.", nameof(macAddress));
            }

            var mac = Key(macAddress);

            lock (_lock)
            {
                if (!_devices.TryGetValue(mac, out var tree))
                {
                    tree = new AvlTree<DateTime, TelemetryPoint>();
                    _devices[mac] = tree;
                }

                tree.Insert(point.RecordedAt, point);
            }
        }

        //..............................................................................//

        //retrieves a copy of what a device reported between two moments, oldest first, both ends included
        public List<TelemetryPoint> GetRange(string macAddress, DateTime from, DateTime to)
        {
            lock (_lock)
            {
                if (!_devices.TryGetValue(Key(macAddress), out var tree))
                {
                    return new List<TelemetryPoint>();
                }

                // The tree throws if the range is back to front, so the caller finds out the same way as with the history log.
                return tree.GetRange(from, to);
            }
        }

        //..............................................................................//

        //puts a mac address in the one form the devices are keyed on
        private static string Key(string macAddress)
        {
            return macAddress.ToUpperInvariant();
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
