using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Ernesto
{
    private static bool _hasErnesto = false;

    public static bool hasErnesto
    {
        get => _hasErnesto;
        set
        {
            if (_hasErnesto != value)
            {
                _hasErnesto = value;
                ApplyHasErnestoFlag(_hasErnesto);
                if (_hasErnesto)
                {
                    var nd = new NotificationData(NotificationTarget.All, "ERNESTO", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    public static void ApplyHasErnestoFlag(bool hasErnesto)
    {
        //var museumFish = GameObject.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Interactables_Observatory/AnglerFishExhibit/AnglerFishTankPivot/Beast_Anglerfish/Beast_Anglerfish");
        var museumFish = GameObject.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Interactables_Observatory/AnglerFishExhibit/AnglerFishTankPivot");
        var ernesto = GameObject.Instantiate(museumFish);
        var ship = Locator.GetShipBody()?.gameObject?.transform;
        ernesto.transform.SetParent(ship, false);
        //ernesto.transform.position = new Vector3(0, 0, 0);
        /*var rt = ernesto.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(1, 1);*/
    }
}
