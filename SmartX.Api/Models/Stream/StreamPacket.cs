using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//StreamPacket

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Api.Models.Stream
{
    //holds one accepted reading while it waits on the queue to be processed
    public class StreamPacket
    {
        //identity of the device the reading came from
        public string MacAddress { get; set; } = string.Empty;

        //what the device was registered as
        public SensorCategory Category { get; set; }

        //the moisture percentage or the wattage, or null for an actuator
        public double? Value { get; set; }

        //whether the valve was open, or null for the other two categories
        public bool? State { get; set; }

        //when the reading was taken
        public DateTime RecordedAt { get; set; }

        //what the radio link said about itself
        public RadioStats Radio { get; set; } = new();

        //a fault code the device reported about itself, or null if it reported none
        public string? Fault { get; set; }

        //the neighbours the device said it could hear, or null if it did not say
        public List<NeighbourLink>? Neighbours { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
