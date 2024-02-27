using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerRegionData(string name)
    {
        public List<TrackerConnectionData> fromConnections = new();
        public List<TrackerConnectionData> toConnections = new();

        public List<List<TrackerRequirement>> requirements = new();
        public string name = name;
    }
}
