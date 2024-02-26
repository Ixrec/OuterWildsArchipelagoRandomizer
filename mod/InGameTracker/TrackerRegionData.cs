using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerRegionData
    {
        public List<TrackerConnectionData> fromConnections = new();
        public List<TrackerConnectionData> toConnections = new();

        public List<List<TrackerConnectionData.Requirement>> reqs = new();
    }
}
