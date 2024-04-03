using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerRequirement
    {
        public string item;
        public List<TrackerRequirement> anyOf;
    }
}
