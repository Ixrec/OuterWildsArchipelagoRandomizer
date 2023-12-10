using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace ArchipelagoRandomizer
{
    public class Randomizer : ModBehaviour
    {
        public static Randomizer Instance;

        private void Awake()
        {
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;
        }

        private void Start()
        {
            WarpPlatforms.Setup();
            Tornadoes.Setup();
            QuantumImaging.Setup();
            Jellyfish.Setup();

            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"Loaded Ixrec's Archipelago Randomizer", MessageType.Success);
        }
    }

}
