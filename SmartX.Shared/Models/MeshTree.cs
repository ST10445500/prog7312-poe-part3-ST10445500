//ST10445500 - PROG7312 - SmartX POE
//MeshTree

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds the cheapest set of links that joins the live devices to the gateway
    public class MeshTree
    {
        //the links in the tree, MacA is the end nearer the gateway and MacB the end further out
        public List<TopologyLink> Edges { get; set; } = new();

        //the cost of every link in the tree added up, a link costs its signal strength without the minus sign
        public int TotalCost { get; set; }

        //for each device in the tree, the device it hands its traffic to on the way to the gateway
        public Dictionary<string, string> Parents { get; set; } = new();

        //live devices the tree could not reach, because every path to the gateway runs through a quiet one or none exists
        public List<string> Unreached { get; set; } = new();
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
