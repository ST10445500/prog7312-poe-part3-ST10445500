//ST10445500 - PROG7312 - SmartX POE
//TelemetryPoint

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds the radio side of one reading, as the telemetry tree kept it
    public class TelemetryPoint
    {
        //when the reading was taken, as the device said
        public DateTime RecordedAt { get; set; }

        //signal strength in dBm, or null if the device did not say
        public int? Rssi { get; set; }

        //how many hops the packet took to reach the gateway, or null if the device did not say
        public int? HopCount { get; set; }

        //share of time the radio was transmitting, or null if the device did not say
        public double? DutyCycle { get; set; }

        //share of packets lost on the way, or null if the device did not say
        public double? PacketLoss { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
