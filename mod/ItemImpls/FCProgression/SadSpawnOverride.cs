using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using static ArchipelagoRandomizer.Victory;

namespace ArchipelagoRandomizer
{
    public class ForgottenCastawaysPatch
    {
        protected static bool CheckIfLoaded() => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "DeepBramble");
        protected static MethodBase GetMethod(string prefix, string typeName, string methodName) => GetMethod($"{prefix}.{typeName}", methodName);
        protected static MethodBase GetMethod(string prefix, string typeName, string methodName, BindingFlags flags) => GetMethod($"{prefix}.{typeName}", methodName, flags);
        protected static MethodBase GetMethod(string typeName, string methodName) => Type.GetType($"{typeName}, DeepBramble").GetMethod(methodName);
        protected static MethodBase GetMethod(string typeName, string methodName, BindingFlags flags) => Type.GetType($"{typeName}, DeepBramble").GetMethod(methodName, flags);
    }

    [HarmonyPatch]
    internal class SadDitylumPatch : ForgottenCastawaysPatch
    {
        private const string Namespace = "DeepBramble.Ditylum";
        private const string Classname = "SadDitylumManager";
        private const string Method = "Sit";

        [HarmonyPrepare]
        private static bool Prepare()
        {
            return CheckIfLoaded();
        }

        [HarmonyTargetMethod]
        private static MethodBase Target() => GetMethod(Namespace, Classname, Method, BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPostfix]
        private static void Patch()
        {
            Spawn.ResetSpawnSystem();
        }
    }
}
