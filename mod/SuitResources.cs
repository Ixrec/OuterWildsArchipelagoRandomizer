using HarmonyLib;
using OWML.Common;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class SuitResources
{
    // properties for `filler` refill items

    private static uint _fuelRefills = 0;

    public static uint fuelRefills
    {
        get => _fuelRefills;
        set
        {
            if (value > _fuelRefills)
                RefillFuel();
            _fuelRefills = value;
        }
    }
    private static uint _oxygenRefills = 0;

    public static uint oxygenRefills
    {
        get => _oxygenRefills;
        set
        {
            if (value > _oxygenRefills)
                RefillOxygen();
            _oxygenRefills = value;
        }
    }

    // properties for `useful` upgrade items

    private static uint _fuelCapacityUpgrades = 0;
    public static uint fuelCapacityUpgrades
    {
        get => _fuelCapacityUpgrades;
        set
        {
            _fuelCapacityUpgrades = value;
            ApplyMaxFuel();
        }
    }
    private static uint _oxygenCapacityUpgrades = 0;
    public static uint oxygenCapacityUpgrades
    {
        get => _oxygenCapacityUpgrades;
        set
        {
            _oxygenCapacityUpgrades = value;
            ApplyMaxOxygen();
        }
    }
    private static uint _boostDurationUpgrades = 0;
    public static uint boostDurationUpgrades
    {
        get => _boostDurationUpgrades;
        set
        {
            _boostDurationUpgrades = value;
            ApplyMaxBoost();
        }
    }

    // private state, constants and reference-fetching setup patches to enable the real item implementations

    private static PlayerResources playerResources = null;
    private static JetpackThrusterModel jetpackThrusterModel = null;

    // values copied from PlayerResources and JetpackThrusterModel
    private static float vanillaFuelCapacity = 100f;
    private static float vanillaOxygenCapacity = 450f; // measured in in-universe seconds
    private static float vanillaBoostSeconds = 1f;

    // these are percentages of vanilla values
    private static int initialFuelCapacity = 50;
    private static int fuelCapacityPerUpgrade = 50;
    private static int initialOxygenCapacity = 50;
    private static int oxygenCapacityPerUpgrade = 50;
    private static int initialBoostDuration = 50;
    private static int boostDurationPerUpgrade = 50;

    public static void ModSettingsChanged(IModConfig config)
    {
        var initialFuelSettingString = config.GetSettingsValue<string>("Fuel Capacity: Initial % of Vanilla");
        int.TryParse(initialFuelSettingString, out var initialFuelSettingNumber);
        var fuelUpgradeSettingString = config.GetSettingsValue<string>("Fuel Capacity: % Increase Per Upgrade");
        int.TryParse(fuelUpgradeSettingString, out var fuelUpgradeSettingNumber);
        if (initialFuelCapacity != initialFuelSettingNumber || fuelCapacityPerUpgrade != fuelUpgradeSettingNumber)
        {
            initialFuelCapacity = initialFuelSettingNumber;
            fuelCapacityPerUpgrade = fuelUpgradeSettingNumber;
            ApplyMaxFuel();
        }

        var initialOxygenSettingString = config.GetSettingsValue<string>("Oxygen Capacity: Initial % of Vanilla");
        int.TryParse(initialOxygenSettingString, out var initialOxygenSettingNumber);
        var oxygenUpgradeSettingString = config.GetSettingsValue<string>("Oxygen Capacity: % Increase Per Upgrade");
        int.TryParse(oxygenUpgradeSettingString, out var oxygenUpgradeSettingNumber);
        if (initialOxygenCapacity != initialOxygenSettingNumber || oxygenCapacityPerUpgrade != oxygenUpgradeSettingNumber)
        {
            initialOxygenCapacity = initialOxygenSettingNumber;
            oxygenCapacityPerUpgrade = oxygenUpgradeSettingNumber;
            ApplyMaxOxygen();
        }

        var initialBoostSettingString = config.GetSettingsValue<string>("Boost Duration: Initial % of Vanilla");
        int.TryParse(initialBoostSettingString, out var initialBoostSettingNumber);
        var boostUpgradeSettingString = config.GetSettingsValue<string>("Boost Duration: % Increase Per Upgrade");
        int.TryParse(boostUpgradeSettingString, out var boostUpgradeSettingNumber);
        if (initialBoostDuration != initialBoostSettingNumber || boostDurationPerUpgrade != boostUpgradeSettingNumber)
        {
            initialBoostDuration = initialBoostSettingNumber;
            boostDurationPerUpgrade = boostUpgradeSettingNumber;
            ApplyMaxBoost();
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.Awake))]
    public static void PlayerResources_Awake(PlayerResources __instance)
    {
        playerResources = __instance;

        ApplyMaxOxygen();
        ApplyMaxFuel();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.Awake))]
    public static void JetpackThrusterModel_Awake(JetpackThrusterModel __instance)
    {
        jetpackThrusterModel = __instance;

        ApplyMaxBoost();
    }

    // implementing the `filler` refill items

    private static void RefillFuel()
    {
        if (playerResources != null)
        {
            playerResources._currentFuel = PlayerResources._maxFuel;

            // Based on the parts of PlayerRecoveryPoint.OnPressInteract() and PlayerResources.StartRefillResources() that handle vanilla fuel-only refills
            // In vanilla this is a pinned notification, which doesn't fit suddenly receiving an AP item, so also based on the oxygen refill code.
            Locator.GetPlayerAudioController().PlayRefuel();
            var nd = new NotificationData(NotificationTarget.Player, UITextLibrary.GetString(UITextType.NotificationRefuel), 3f, false);
            NotificationManager.SharedInstance.PostNotification(nd, false);
        }
    }

    private static void RefillOxygen()
    {
        if (playerResources != null)
        {
            playerResources._currentOxygen = PlayerResources._maxOxygen;

            // Based on the part of PlayerResources.UpdateOxygen() that handles vanilla refills
            Locator.GetPlayerAudioController().PlayRefillOxygen();
            var nd = new NotificationData(NotificationTarget.Player, UITextLibrary.GetString(UITextType.NotificationO2), 3f, false);
            NotificationManager.SharedInstance.PostNotification(nd, false);
        }
    }

    // implementing the `useful` upgrade items

    private static Text fuelPercent = null;
    private static Text oxygenPercent = null;
    private static Text boostPercent = null;

    private static Text[] fuelNumbers = [];
    private static Text[] oxygenNumbers = [];

    [HarmonyPostfix, HarmonyPatch(typeof(HUDCanvas), nameof(HUDCanvas.Start))]
    public static void HUDCanvas_Start(HUDCanvas __instance)
    {
        var gaugeStuff = GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas/GaugeGroup/");
        var gaugeAnchor = gaugeStuff.transform.Find("Gauge");

        boostPercent = __instance._boostValueDisplay;

        fuelPercent = UnityEngine.Object.Instantiate(boostPercent);
        fuelPercent.name = "APRandomizer_FuelPercentLabel";
        fuelPercent.transform.SetParent(gaugeAnchor.transform, false);
        fuelPercent.transform.localPosition = new Vector3(-15, 15, 0);
        // TODO: follow Unity docs and use Quaternion * instead???
        fuelPercent.transform.localEulerAngles = new Vector3(0, 0, 90);

        oxygenPercent = UnityEngine.Object.Instantiate(boostPercent);
        oxygenPercent.name = "APRandomizer_OxygenPercentLabel";
        oxygenPercent.transform.SetParent(gaugeAnchor.transform, false);
        // The O2 and FUEL labels have slightly different shapes, so actual symmetry doesn't look right
        oxygenPercent.transform.localPosition = new Vector3(16, -35, 0);
        oxygenPercent.transform.localEulerAngles = new Vector3(0, 0, 90);

        var fn = gaugeStuff?.transform?.Find("FuelNumbers");
        fuelNumbers = fn.GetComponentsInChildren<Text>();
        var on = gaugeStuff?.transform?.Find("OxygenNumbers");
        oxygenNumbers = on.GetComponentsInChildren<Text>();

        ApplyMaxOxygen();
        ApplyMaxFuel();
        ApplyMaxBoost();
    }

    private static void ApplyMaxOxygen()
    {
        // in this file "multiplier" means: 1 = 100% of the vanilla value, 0.5 = 50%, 2 = 200%, etc
        double multiplier = (initialOxygenCapacity/100f) + ((oxygenCapacityPerUpgrade/100f) * _oxygenCapacityUpgrades);
        PlayerResources._maxOxygen = (float)(vanillaOxygenCapacity * multiplier);

        if (oxygenPercent != null)
            oxygenPercent.text = multiplier.ToString("P1"); // percentage with 1dp, e.g. turns 1 into "100.0%"

        // the vanilla values are: 00 10 20 30 40 50 in that order
        if (oxygenNumbers.Length > 0 && oxygenNumbers.All(on => on != null))
        {
            oxygenNumbers[0].text = ((int)Math.Round(multiplier * 0)).ToString("D2"); // 2 digit integer, e.g. turns 1 into "01"
            oxygenNumbers[1].text = ((int)Math.Round(multiplier * 10)).ToString("D2");
            oxygenNumbers[2].text = ((int)Math.Round(multiplier * 20)).ToString("D2");
            oxygenNumbers[3].text = ((int)Math.Round(multiplier * 30)).ToString("D2");
            oxygenNumbers[4].text = ((int)Math.Round(multiplier * 40)).ToString("D2");
            // replace 100 with 99, since for some reason this text cannot render a 3rd digit
            oxygenNumbers[5].text = ((int)Math.Min(Math.Round(multiplier * 50), 99)).ToString("D2");
        }

        // The "3 Minutes Remaining" notification becomes more annoying than helpful when your
        // maximum oxygen capacity is only 3.5 minutes. The 60 second warning is plenty.
        if (multiplier < 1)
            // Because the "low" threshold is only ever checked after the "critical" threshold (in UpdateOxygen()),
            // the simplest way to disable the low-but-not-critical warning is to set the thresholds equal.
            PlayerResources._lowOxygen = PlayerResources._criticalOxygen;
        else
            PlayerResources._lowOxygen = 180f; // vanilla value
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HUDCanvas), nameof(HUDCanvas.UpdateOxygen))]
    public static void HUDCanvas_UpdateOxygen_Postfix(HUDCanvas __instance)
    {
        double oxygenCapacityMultiplier = (initialOxygenCapacity/100f) + ((oxygenCapacityPerUpgrade/100f) * _oxygenCapacityUpgrades);

        oxygenPercent.text = (__instance._oxygenFraction * oxygenCapacityMultiplier).ToString("P1");
    }

    private static void ApplyMaxFuel()
    {
        double multiplier = (initialFuelCapacity/100f) + ((fuelCapacityPerUpgrade/100f) * _fuelCapacityUpgrades);
        PlayerResources._maxFuel = (float)(vanillaFuelCapacity * multiplier);

        if (fuelPercent != null)
            fuelPercent.text = multiplier.ToString("P1"); // percentage with 1dp, e.g. turns 1 into "100.0%"

        // the vanilla values are: 5 4 3 2 1 0 in that order
        if (fuelNumbers.Length > 0 && fuelNumbers.All(fn => fn != null))
        {
            fuelNumbers[0].text = (multiplier * 5).ToString();
            fuelNumbers[1].text = (multiplier * 4).ToString();
            fuelNumbers[2].text = (multiplier * 3).ToString();
            fuelNumbers[3].text = (multiplier * 2).ToString();
            fuelNumbers[4].text = (multiplier * 1).ToString();
            fuelNumbers[5].text = (multiplier * 0).ToString();
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HUDCanvas), nameof(HUDCanvas.UpdateFuel))]
    public static void HUDCanvas_UpdateFuel_Postfix(HUDCanvas __instance)
    {
        double fuelCapacityMultiplier = (initialFuelCapacity/100f) + ((fuelCapacityPerUpgrade/100f) * _fuelCapacityUpgrades);

        fuelPercent.text = (__instance._fuelFraction * fuelCapacityMultiplier).ToString("P1");
    }

    private static void ApplyMaxBoost()
    {
        double multiplier = (initialBoostDuration/100f) + ((boostDurationPerUpgrade/100f) * _boostDurationUpgrades);
        if (jetpackThrusterModel != null)
            jetpackThrusterModel._boostSeconds = (float)(vanillaBoostSeconds * multiplier);

        if (boostPercent != null)
            boostPercent.text = multiplier.ToString("P1"); // percentage with 1dp, e.g. turns 1 into "100.0%"
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HUDCanvas), nameof(HUDCanvas.UpdateBoost))]
    public static void HUDCanvas_UpdateBoost_Postfix(HUDCanvas __instance)
    {
        double boostDurationMultiplier = (initialBoostDuration/100f) + ((boostDurationPerUpgrade/100f) * _boostDurationUpgrades);

        __instance._boostValueDisplay.text = (__instance._chargeFraction * boostDurationMultiplier).ToString("P1");
    }

    // may be useful for testing even lower, logic-relevant oxygen/fuel/boost limits
    /*[HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.left2))
        {
            _oxygenCapacityUpgrades = (_oxygenCapacityUpgrades + 1) % 3;
            APRandomizer.OWMLWriteLine($"_oxygenCapacityUpgrades={_oxygenCapacityUpgrades}");
            ApplyMaxOxygen();
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.right2))
        {
            _fuelCapacityUpgrades = (_fuelCapacityUpgrades + 1) % 3;
            APRandomizer.OWMLWriteLine($"_fuelCapacityUpgrades={_fuelCapacityUpgrades}");
            ApplyMaxFuel();
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
        {
            _boostDurationUpgrades = (_boostDurationUpgrades + 1) % 3;
            APRandomizer.OWMLWriteLine($"_boostDurationUpgrades={_boostDurationUpgrades}");

            ApplyMaxBoost();
        }
    }*/
}
