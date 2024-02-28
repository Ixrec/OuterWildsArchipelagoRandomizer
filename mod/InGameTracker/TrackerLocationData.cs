﻿using System;
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
        public List<TrackerRequirement> requires;
    }

    
}
