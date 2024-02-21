namespace ArchipelagoRandomizer.InGameTracker
{
    /// <summary>
    /// Contains info for the tracker including the Ship Log image and description
    /// </summary>
    public struct TrackerInfo
    {
        /// <summary>
        /// Location name as displayed in Location.cs
        /// </summary>
        public string locationModID;
        /// <summary>
        /// Text shown to the player describing where the item is
        /// </summary>
        public string description;
        /// <summary>
        /// Explore Fact card picture showing the general location of the item
        /// </summary>
        public string thumbnail;
    }
}
