using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Tornadoes
{
    private static bool _hasTornadoKnowledge = false;

    public static bool hasTornadoKnowledge
    {
        get => _hasTornadoKnowledge;
        set
        {
            if (_hasTornadoKnowledge != value)
            {
                _hasTornadoKnowledge = value;
                ApplyHasKnowledgeFlag();
                if (value)
                {
                    var nd = new NotificationData(NotificationTarget.All, "ADJUSTING SPACESHIP AERODYNAMICS FOR COUNTERCLOCKWISE TORNADOES", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static TornadoFluidVolume counterClockwiseGiantsDeepTornadoFluidVolume = null;

    public static void Setup()
    {
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

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

    private static void ApplyHasKnowledgeFlag()
    {
        if (counterClockwiseGiantsDeepTornadoFluidVolume)
        {
            if (_hasTornadoKnowledge)
                counterClockwiseGiantsDeepTornadoFluidVolume._inwardSpeed = 100; // the vanilla value
            else
                counterClockwiseGiantsDeepTornadoFluidVolume._inwardSpeed = -300;
        }
    }

    static ScreenPrompt tornadoAdjustmentsActivePrompt = new("Tornado Aerodynamic Adjustments: Active", 0);

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(tornadoAdjustmentsActivePrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        tornadoAdjustmentsActivePrompt.SetVisibility(
            _hasTornadoKnowledge &&
            OWInput.IsInputMode(InputMode.ShipCockpit) &&
            Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.GiantsDeep)
        );
    }
}
