using System.Collections.Generic;
using UnityEngine;
using Archipelago.MultiClient.Net.Enums;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Determines the color of Nomai Text Arcs based on their contents
    /// </summary>
    public class ArcHintData : MonoBehaviour
    {
        // Since arcs have no idea which logs they're connected to,
        // we record them and their other location checks here in a list
        // so we can easily see them in Unity Explorer
        // Currently unused in any code but may use it later
        public List<Location> Locations = new List<Location>();
        public CheckImportance Importance = CheckImportance.Filler;
        // used to show importance in case of traps
        public CheckImportance DisplayImportance = CheckImportance.Trap;
        public bool HasBeenFound = false;

        private bool IsChildText = false;

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
            if (HasBeenFound) return HintColors.FoundColor;
            switch (DisplayImportance)
            {
                case CheckImportance.Filler:
                    DisplayImportance = CheckImportance.Filler;
                    return HintColors.FillerColor;
                case CheckImportance.Useful:
                    DisplayImportance = CheckImportance.Useful;
                    return HintColors.UsefulColor;
                case CheckImportance.Progression:
                    DisplayImportance = CheckImportance.Progression;
                    return HintColors.ProgressionColor;
                default: return Color.red;
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

            switch (LocationScouter.ScoutedLocations[loc].Flags)
            {
                case ItemFlags.None:
                    DisplayImportance = CheckImportance.Filler;
                    SetImportance(CheckImportance.Filler);
                    break;
                case ItemFlags.NeverExclude:
                    DisplayImportance = CheckImportance.Useful;
                    SetImportance(CheckImportance.Useful);
                    break;
                case ItemFlags.Advancement:
                    DisplayImportance = CheckImportance.Progression;
                    SetImportance(CheckImportance.Progression);
                    break;
                case ItemFlags.Trap:
                    int rnd = Random.Range(0, 3);
                    switch (rnd)
                    {
                        case 0:
                            DisplayImportance = CheckImportance.Filler;
                            break;
                        case 1:
                            DisplayImportance = CheckImportance.Useful;
                            break;
                        default:
                            DisplayImportance = CheckImportance.Progression;
                            break;
                    }
                    SetImportance(DisplayImportance);
                    rend.material = IsChildText ? NormalTextMat : ChildTextMat;
                    break;
            }
        }
    }
    
}
