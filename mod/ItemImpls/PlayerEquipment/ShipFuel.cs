using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class ShipFuel
{
    private static bool _hasFuelUpgrade = false;

    public static bool hasFuelUpgrade
    {
        get => _hasFuelUpgrade;
        set
        {
            if (_hasFuelUpgrade != value)
            {
                _hasFuelUpgrade = value;
                if (_hasFuelUpgrade)
                {
                    var nd = new NotificationData(NotificationTarget.All, "BIGGER SPACESHIP FUEL TANK IS INSTALLED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    public static float drainFactor { get; set; } = 1f;
    public static void ApplySlotData(long option)
    {
        switch (option)
        {
            case 1: drainFactor = 20f; break; // Normal
            case 2: drainFactor = 40f; break; // Hard
            default: drainFactor = 1f; break; // Disabled
        }
    }

    private static NotificationData _fuelLowNotification;
    private static NotificationData _fuelCriticalNotification;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.Awake))]
    public static void ShipResources_Awake_Prefix(ShipResources __instance)
    {
        _fuelLowNotification = new NotificationData(NotificationTarget.Ship, "FUEL LOW");
        _fuelCriticalNotification = new NotificationData(NotificationTarget.Ship, "FUEL CRITICAL");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.DrainFuel))]
    public static void ShipResources_DrainFuel_Prefix(ShipResources __instance, ref float amount)
    {
        if (!hasFuelUpgrade)
            amount *= drainFactor;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.DrainFuel))]
    public static void ShipResources_DrainFuel_Postfix(ShipResources __instance)
    {
        float fuel = __instance.GetFractionalFuel();
        float lowAmount = .33f;
        float criticalAmount = .1f;
        bool isLow = fuel <= lowAmount && fuel > criticalAmount;
        bool isCritical = fuel <= criticalAmount && fuel > 0f;

        if (isLow && !NotificationManager.SharedInstance.IsPinnedNotification(_fuelLowNotification))
            NotificationManager.SharedInstance.PostNotification(_fuelLowNotification, true);
        else if (!isLow && NotificationManager.SharedInstance.IsPinnedNotification(_fuelLowNotification))
            NotificationManager.SharedInstance.UnpinNotification(_fuelLowNotification);

        if (isCritical && !NotificationManager.SharedInstance.IsPinnedNotification(_fuelCriticalNotification))
            NotificationManager.SharedInstance.PostNotification(_fuelCriticalNotification, true);
        else if (!isCritical && NotificationManager.SharedInstance.IsPinnedNotification(_fuelCriticalNotification))
            NotificationManager.SharedInstance.UnpinNotification(_fuelCriticalNotification);
    }
}
