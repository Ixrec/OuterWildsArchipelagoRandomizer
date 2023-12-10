using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Tornadoes
{
    public static bool hasTornadoKnowledge = false;

    static TornadoFluidVolume counterClockwiseGiantsDeepTornadoFluidVolume = null;

    public static void Setup()
    {
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            Randomizer.Instance.ModHelper.Console.WriteLine($"Tornadoes.Setup fetching reference to counterclockwise GD tornado");

            var gd = Locator.GetAstroObject(AstroObject.Name.GiantsDeep);
            var st = gd.transform.Find("Sector_GD/Sector_GDInterior/Tornadoes_GDInterior/SouthernTornadoes");
            var tfvs = st.GetComponentsInChildren<TornadoFluidVolume>();
            foreach (var tfv in tfvs)
                if (tfv._verticalSpeed < 0)
                {
                    counterClockwiseGiantsDeepTornadoFluidVolume = tfv;
                    ApplyHasKnowledgeFlag();
                    break;
                }
        };
    }

    public static void SetHasTornadoKnowledge(bool hasTornadoKnowledge)
    {
        if (Tornadoes.hasTornadoKnowledge != hasTornadoKnowledge)
        {
            Tornadoes.hasTornadoKnowledge = hasTornadoKnowledge;
            ApplyHasKnowledgeFlag();
            if (hasTornadoKnowledge)
            {
                var nd = new NotificationData(NotificationTarget.All, "ADJUSTING SPACESHIP AERODYNAMICS FOR COUNTERCLOCKWISE TORNADOES", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }

    private static void ApplyHasKnowledgeFlag()
    {
        if (counterClockwiseGiantsDeepTornadoFluidVolume)
        {
            if (hasTornadoKnowledge)
                counterClockwiseGiantsDeepTornadoFluidVolume._inwardSpeed = 100; // the vanilla value
            else
                counterClockwiseGiantsDeepTornadoFluidVolume._inwardSpeed = -300;
        }
    }

    static ScreenPrompt tornadoAdjustmentsActivePrompt = new("Tornado Aerodynamic Adjustments: Active", 0);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(tornadoAdjustmentsActivePrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        tornadoAdjustmentsActivePrompt.SetVisibility(
            hasTornadoKnowledge &&
            OWInput.IsInputMode(InputMode.ShipCockpit) &&
            Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.GiantsDeep)
        );
    }
}
