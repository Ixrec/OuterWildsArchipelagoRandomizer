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
        public List<Location> Locations = new List<Location>();
        public CheckImportance Importance = CheckImportance.Junk;
        public bool HasBeenFound = false;
        public bool IsChildText = false;

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

        public static Material NormalTextMat
        {
            get
            {
                if (normalTextMat == null)
                    normalTextMat = Locator.GetAstroObject(AstroObject.Name.TimberHearth).transform
                        .Find("Sector_TH/Sector_Village/Sector_Observatory/Interactables_Observatory/NomaiEyeExhibit/NomaiEyePivot/Arc_TH_Museum_EyeSymbol/Arc 1")
                        .GetComponent<Renderer>().material;
                return normalTextMat;
            }
        }

        private static Material childTextMat;
        private static Material normalTextMat;

        private Renderer rend;

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
            rend = GetComponent<Renderer>();

            Locations.Add(loc);
            if (APRandomizer.APSession.Locations.AllLocationsChecked.Contains(LocationNames.locationToArchipelagoId[loc])) HasBeenFound = true;

            if (Importance != CheckImportance.Trap)
            {
                if (rend.material.name.Contains("TextChild")) IsChildText = true;
            }

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
                    rend.material = IsChildText ? NormalTextMat : ChildTextMat;
                    break;
            }
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
