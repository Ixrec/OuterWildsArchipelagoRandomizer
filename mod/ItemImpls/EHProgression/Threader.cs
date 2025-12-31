using System;
using UnityEngine;

namespace ArchipelagoRandomizer;

internal class Threader
{
    private static bool _hasThreader = false;
    public static bool hasThreader
    {
        get => _hasThreader;
        set
        {
            if (_hasThreader != value)
            {
                _hasThreader = value;
                UpdateThreaders();
                if (value)
                {
                    var nd = new NotificationData(NotificationTarget.Player, "UNKNOWN DEVICES DETECTED THROUGHOUT THE SOLAR SYSTEM", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static private bool areThreadersAdded = false;

    public static void AddThreaders()
    {
        if (areThreadersAdded) return;
        if (APRandomizer.NewHorizonsAPI == null) return;
        if (!APRandomizer.Instance.ModHelper.Interaction.ModExists("Trifid.TrifidJam3")) return;
        void AddThreader(string planet, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, string starSystem = "SolarSystem")
        {
            // Use FormattableString to apply interpolation with invariant culture (ensures the decimal separator to be dot).
            FormattableString config = $$"""
            {
                "name": "{{planet}}",
                "$schema": "https://raw.githubusercontent.com/Outer-Wilds-New-Horizons/new-horizons/main/NewHorizons/Schemas/body_schema.json",
                "starSystem": "{{starSystem}}",
                "Props": {
                        "details": [
                            {
                            "assetBundle": "../Trifid.TrifidJam3/planets/trifid_jam3",
                            "path": "Assets/Jam3/Grapple.prefab",
                            "isRelativeToParent": true,
                            "keepLoaded": true,
                            "position": { "x": {{posX}}, "y": {{posY}}, "z": {{posZ}}},
                            "rotation": { "x": {{rotX}}, "y": {{rotY}}, "z": {{rotZ}}},
                        }
                    ]
                }
            }
            """;
            APRandomizer.NewHorizonsAPI.CreatePlanet(config.ToString(System.Globalization.CultureInfo.InvariantCulture), APRandomizer.Instance);
        }
        AddThreader("Timber Hearth", 11.8452f, -44.8447f, 185.5202f, 11.4649f, 338.4336f, 222.1763f);
        AddThreader("Ember Twin", 3.5126f, 156.8704f, 7.8129f, 355.9978f, 163.6575f, 35.1131f);
        AddThreader("Brittle Hollow", -33.8987f, 4.4243f, 279.8854f, 336.3344f, 346.9767f, 245.6233f);
        AddThreader("StatueIsland", 6.8373f, 32.6154f, -25.1648f, 330.509f, 119.5316f, 251.5381f);
        AddThreader("RINGWORLD", 45.0617f, -123.6726f, -290.0204f, 306.7843f, 83.1867f, 318.7848f);
        if (APRandomizer.Instance.ModHelper.Interaction.ModExists("GameWyrm.HearthsNeighbor"))
            AddThreader("LonelyHermit", 59.8121f, 14.2851f, 274.7672f, 287.8107f, 127.3934f, 85.9663f, "GameWyrm.HearthsNeighbor");
        if (APRandomizer.Instance.ModHelper.Interaction.ModExists("cleric.DeepBramble"))
            AddThreader("Bramble's Doorstep", -5f, 5f, 13f, 10f, 30f, 20f, "DeepBramble");
        areThreadersAdded = true;
    }

    public static void UpdateThreaders()
    {
        if (APRandomizer.NewHorizonsAPI == null) return;
        if (!APRandomizer.Instance.ModHelper.Interaction.ModExists("Trifid.TrifidJam3")) return;
        switch (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem())
        {
            case "Jam3":
                GameObject.Find("EchoHike_Body/Sector/PlanetInterior/EntranceRoot2/Interior/GrappleSpawn/Grapple")?.SetActive(hasThreader);
                break;
            case "SolarSystem":
                GameObject.Find("BrittleHollow_Body/Sector_BH/Grapple")?.SetActive(hasThreader);
                GameObject.Find("CaveTwin_Body/Sector_CaveTwin/Grapple")?.SetActive(hasThreader);
                GameObject.Find("RingWorld_Body/Sector_RingWorld/Grapple")?.SetActive(hasThreader);
                GameObject.Find("StatueIsland_Body/Sector_StatueIsland/Grapple")?.SetActive(hasThreader);
                GameObject.Find("TimberHearth_Body/Sector_TH/Grapple")?.SetActive(hasThreader);
                break;
            case "GameWyrm.HearthsNeighbor":
                GameObject.Find("LonelyHermit_Body/Sector/Grapple")?.SetActive(hasThreader);
                break;
            case "DeepBramble":
                GameObject.Find("BramblesDoorstep_Body/Sector/Grapple")?.SetActive(hasThreader);
                break;
        }
    }
}
