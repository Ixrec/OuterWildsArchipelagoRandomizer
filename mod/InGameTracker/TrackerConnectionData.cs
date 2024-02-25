using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer.InGameTracker
{
    /// <summary>
    /// Class for parsing all information in connections.jsonc
    /// Should support all info in it, but may need to be updated as logic becomes more complex
    /// </summary>
    public class TrackerConnectionData
    {
        public string from;
        public string to;
        public List<Requirement> requires;

        public struct Requirement
        {
            public string item;
            public List<Requirement> anyOf;
        }
    }
}
