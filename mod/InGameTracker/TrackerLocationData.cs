using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer.InGameTracker
{
    /// <summary>
    /// Class for parsing information in locations.jsonc.
    /// Should support all info currently in it, but this may have to be changed as logic grows more complex.
    /// </summary>
    public class TrackerLocationData
    {
        public string[] creation_settings;
        public int? address;
        public string name;
        public string region;
        public List<Requirement> requires;
        
        
        public struct Requirement
        {
            public string item;
            public string region;
            public string location;
            public AnyOf anyOf;
        }

        // AnyOf seems to be currently unused, thanks Ixrec, but leaving it here for now as we may need it later
        public struct AnyOf
        {
            public List<AnyOfConditions> conditions;
        }

        public struct AnyOfConditions
        {
            public string region;
            public string location;
        }
    }

    
}
