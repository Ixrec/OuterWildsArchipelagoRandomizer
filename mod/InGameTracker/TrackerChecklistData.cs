namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerChecklistData(bool isAccessible, bool hasBeenChecked, string hintItem, string hintReceiver)
    {
        /// <summary>
        /// Whether the item has ever been accessible
        /// </summary>
        public bool isAccessible = isAccessible;
        /// <summary>
        /// Whether the location has been checked already
        /// </summary>
        public bool hasBeenChecked = hasBeenChecked;
        /// <summary>
        /// If a hint says something is here, this is the name of the item
        /// </summary>
        public string hintItem = hintItem;
        /// <summary>
        /// The player that owns the hinted item
        /// </summary>
        public string hintReceiver = hintReceiver;

        public void SetAccessible(bool access)
        {
            isAccessible = access;
        }
    }
}
