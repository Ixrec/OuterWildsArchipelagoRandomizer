using System.Collections.Generic;
using UnityEngine;
using Archipelago.MultiClient.Net.Enums;

namespace ArchipelagoRandomizer
{
    public class CheckHintData : MonoBehaviour
    {
        // Since arcs have no idea which logs they're connected to,
        // we record them and their other location checks here in a list
        // so we can easily see them in Unity Explorer
        // Currently unused in any code but may use it later
        public List<Location> locations = new List<Location>();
        public CheckImportance Importance = CheckImportance.Junk;
        public bool HasBeenFound = false;

        public static readonly Color JunkColor = new(0.5f, 1.5f, 1.5f, 1);
        public static readonly Color UsefulColor = new(0.5f, 1.8f, 0.5f, 1);
        public static readonly Color ProgressionColor = new(1.5f, 0.5f, 1.5f, 1);
        public static readonly Color FoundColor = new(1.2f, 1.2f, 1.2f, 1);

        public static Material ChildTextMat
        {
            get
            {
                if (childTextMat == null)
                    childTextMat = Locator.GetAstroObject(AstroObject.Name.CaveTwin).transform
                        .Find("Sector_CaveTwin/Sector_SouthHemisphere/Sector_SouthUnderground/Sector_City/Interactables_City/Arc_CT_City_KidDirectionToFossil_1/Arc 1")
                        .GetComponent<Renderer>().material;
                return childTextMat;
            }
        }

        private static Material childTextMat;

        public Color NomaiWallColor()
        {
            if (HasBeenFound) return FoundColor;
            switch (Importance)
            {
                case CheckImportance.Junk:
                    return JunkColor;
                case CheckImportance.Useful:
                    return UsefulColor;
                case CheckImportance.Progression:
                    return ProgressionColor;
                default:
                    {
                        int rnd = Random.Range(0, 3);
                        switch (rnd)
                        {
                            case 0:
                                return JunkColor;
                            case 1:
                                return UsefulColor;
                            default:
                                return ProgressionColor;
                        }    
                    }
            }
        }

        public void SetImportance(CheckImportance importance)
        {
            if ((int)Importance < (int)importance)
            {
                Importance = importance;
            }
        }

        public void DetermineImportance(Location loc)
        {
            locations.Add(loc);

            switch (Scouter.ScoutedLocations[loc].Flags)
            {
                case ItemFlags.None:
                    SetImportance(CheckImportance.Junk);
                    break;
                case ItemFlags.NeverExclude:
                    SetImportance(CheckImportance.Useful);
                    break;
                case ItemFlags.Advancement:
                    SetImportance(CheckImportance.Progression);
                    break;
                case ItemFlags.Trap:
                    SetImportance(CheckImportance.Trap);
                    GetComponent<Renderer>().material = ChildTextMat; // TODO make this depend on whether it's already a child arc
                    break;
            }
        }

        private void Awake()
        {
            APRandomizer.OWMLModConsole.WriteLine($"I've been added to {transform.parent.name}/{gameObject.name}!");
        }
    }
    public enum CheckImportance
    {
        Trap = 0,
        Junk = 1,
        Useful = 2,
        Progression = 3
    }
}
