using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace ArchipelagoRandomizer;

internal class NewHorizonsPatches
{
    public static bool IsWarping { get; protected set; } = false;

    protected static bool DiedInOtherSystem { get; set; }

    public static bool IsSpawningBroken
    {
        get
        {
            bool value = DiedInOtherSystem;
            DiedInOtherSystem = false; // Reset back to false when checked
            return value;
        }
    }

    protected static bool CheckIfLoaded() => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "NewHorizons");
    protected static MethodBase GetMethod(string prefix, string typeName, string methodName) => GetMethod($"{prefix}.{typeName}", methodName);
    protected static MethodBase GetMethod(string typeName, string methodName) => Type.GetType($"{typeName}, NewHorizons").GetMethod(methodName);
    protected static MethodBase GetMethod(string typeName, string methodName, BindingFlags flags) => Type.GetType($"{typeName}, NewHorizons").GetMethod(methodName, flags);
}

[HarmonyPatch]
internal class WarpOutPatch : NewHorizonsPatches
{
    private const string Namespace = "NewHorizons.Components.Ship";
    private const string Classname = "ShipWarpController";
    private const string Method = "WarpOut";

    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod(Namespace, Classname, Method);

    [HarmonyPostfix]
    private static void Patch() => IsWarping = true;
}

[HarmonyPatch]
internal class FinishWarpInPatch : NewHorizonsPatches
{
    private const string Namespace = "NewHorizons.Components.Ship";
    private const string Classname = "ShipWarpController";
    private const string Method = "FinishWarpIn";

    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod(Namespace, Classname, Method);

    [HarmonyPostfix]
    private static void Patch() => IsWarping = false;
}

[HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
internal class KillPlayerPatch : NewHorizonsPatches
{
    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyPrefix, HarmonyPriority(Priority.High)]
    private static void Patch() => DiedInOtherSystem |= !APRandomizer.IsVanillaSystemLoaded(); // Don't set a "true" to a "false" in case multiple deaths happen back-to-back
}
