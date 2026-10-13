using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.HSProgression
{
    internal class ProjectRequirementLocations
    {
        static GameObject[] orbs = new GameObject[6];
        public static void OnJam5LoadEvent()
        {
            // Get the door
            var door = GameObject.Find("Walker_Jam5_Planet4_Body/Sector/WaterPlanet/Core/Sector_CullCore/Door");
            // The door already has a trigger nicely set up, so we just borrow it
            door.GetComponent<OWTriggerVolume>().OnEntry += CheckProjectRequirements;
            var indicatorRoot = door.transform.Find("Door/Door (1)/On");
            // The orbs are not arranged nicely, so we find them by name
            for (int i = 0; i < 6; i++)
            {
                orbs[i] = indicatorRoot.Find($"RQL On ({i + 1})").gameObject;
            }
        }

        public static void CheckProjectRequirements(GameObject enteredObj)
        {
            // Only trigger the locations if the player is entering
            if (enteredObj.name != "PlayerDetector") return;

            for (int i = 0; i < 6; i++)
            {
                // If each requirement is met, the corresponding orb will be active
                if (orbs[i].activeSelf)
                {
                    Location location;
                    Location.TryParse($"HS_COMPLETE_REQUIREMENT_{i + 1}", out location);
                    LocationTriggers.CheckLocation(location);
                }
            }
        }
    }
}
