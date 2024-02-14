using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class ShipDamage
{
    private static uint _shipDamageTraps = 0;

    public static uint shipDamageTraps
    {
        get => _shipDamageTraps;
        set
        {
            if (value > _shipDamageTraps)
            {
                _shipDamageTraps = value;
                DamageShip();
            }
        }
    }

    static ShipDamageController shipDamageController = null;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.Awake))]
    public static void ShipDamageController_Awake(ShipDamageController __instance) => shipDamageController = __instance;

    private static Random rng = new Random();

    // Randomly choose 2 hulls and 2 components to damage.
    // The one special wrinkle is that we want to avoid damaging both thruster banks in a single trap if possible.
    private static void DamageShip()
    {
        if (shipDamageController != null)
        {
            // Ignore ship parts that are already damaged, so multiple traps back-to-back work correctly
            var hulls = new List<ShipHull>(shipDamageController._shipHulls.Where(h => !h.isDamaged));
            var components = new List<ShipComponent>(shipDamageController._shipComponents.Where(c => !c.isDamaged));

            // For future reference, these are the hull names:
            /*Module_Cockpit
              Module_Cabin (x2)
              Module_Supplies
              Module_Engine
              Module_LandingGear*/
            var damagedHullNames = new HashSet<string>();
            if (hulls.Count > 0)
            {
                var i = rng.Next(hulls.Count);
                var hull1 = hulls[i];

                // ideally we'd set the resulting _integrity to a random number, but because we can't fire OnDamaged
                // from the outside even with Harmony patches, it's much safer to call this method instead
                hull1.ApplyDebugImpact();
                damagedHullNames.Add(hull1.name);

                hulls.RemoveAt(i);
                if (hulls.Count > 0)
                {
                    i = rng.Next(hulls.Count);
                    var hull2 = hulls[i];
                    hull2.ApplyDebugImpact();
                    damagedHullNames.Add(hull2.name);
                }
            }

            // For future reference, these are the component names:
            /*AutopilotComponent
              OxygenTankComponent
              FuelTankComponent
              MainElectricalComponent
              RightThrusterBankComponent
              ReactorComponent
              GravityComponent
              LeftThrusterBankComponent
              HeadlightsComponent
              LandingCameraComponent*/
            var damagedComponentNames = new HashSet<string>();
            if (components.Count > 0)
            {
                var i = rng.Next(components.Count);
                var component1 = components[i];
                component1.SetDamaged(true);
                damagedComponentNames.Add(component1.name);
                components.RemoveAt(i);

                if (components.Count > 0)
                {
                    // If we just disabled one thruster bank, and the other thruster bank is not yet disabled, then
                    // don't allow this trap to disable both at the same time unless there's nothing else left to disable.
                    if (component1.name.Contains("ThrusterBankComponent"))
                    {
                        var otherThrusterIndex = components.FindIndex(c => c.name.Contains("ThrusterBankComponent"));
                        if (otherThrusterIndex >= 0 && components.Count > 1)
                        {
                            APRandomizer.OWMLModConsole.WriteLine($"this trap already disabled one thruster bank, so excluding the other thruster bank");
                            components.RemoveAt(otherThrusterIndex);
                        }
                    }

                    i = rng.Next(components.Count);
                    var component2 = components[i];
                    component2.SetDamaged(true);
                    damagedComponentNames.Add(component2.name);
                }
            }

            APRandomizer.OWMLModConsole.WriteLine($"Ship Damage Trap chose to damage hulls: [{string.Join(", ", damagedHullNames)}] and components: [{string.Join(", ", damagedComponentNames)}]");

            if (!PlayerState.IsInsideShip())
            {
                APRandomizer.OWMLModConsole.WriteLine($"generating notification about ship damage because player is outside the ship");
                var text = "SPACESHIP DAMAGED";

                var sensitiveDamagedComponents = damagedComponentNames.Intersect(new HashSet<string> { "ReactorComponent", "FuelTankComponent" });
                if (sensitiveDamagedComponents.Any())
                {
                    text += ", INCLUDING TIME-SENSITIVE COMPONENT(S): ";
                    if (sensitiveDamagedComponents.Count() == 2) text += "REACTOR, FUEL TANK";
                    else if (sensitiveDamagedComponents.Contains("ReactorComponent")) text += "REACTOR";
                    else if (sensitiveDamagedComponents.Contains("FuelTankComponent")) text += "FUEL TANK";
                }

                var nd = new NotificationData(NotificationTarget.Player, text, 10f, false);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }
}
