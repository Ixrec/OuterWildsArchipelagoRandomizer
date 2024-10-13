using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class MagistariumAccessCodes
{
    private static bool _hasLibraryAccess = false;
    public static bool hasLibraryAccess
    {
        get => _hasLibraryAccess;
        set
        {
            if (_hasLibraryAccess != value)
            {
                _hasLibraryAccess = value;
                LibraryDoor.SetActive(!value);
            }
        }
    }

    private static bool _hasDormitoriesAccess = false;
    public static bool hasDormitoriesAccess
    {
        get => _hasDormitoriesAccess;
        set
        {
            if (_hasDormitoriesAccess != value)
            {
                _hasDormitoriesAccess = value;
                DormitoryDoor.SetActive(!value);
            }
        }
    }

    private static bool _hasEngineAccess = false;
    public static bool hasEngineAccess
    {
        get => _hasEngineAccess;
        set
        {
            if (_hasEngineAccess != value)
            {
                _hasEngineAccess = value;
                EngineDoor.SetActive(!value);
            }
        }
    }

    private static GameObject LibraryDoor = null;
    private static GameObject DormitoryDoor = null;
    private static GameObject EngineDoor = null;

    private static InteractReceiver LibraryDoorIR = null;
    private static InteractReceiver DormitoryDoorIR = null;
    private static InteractReceiver EngineDoorIR = null;

    public static void OnCompleteSceneLoad()
    {
        if (APRandomizer.NewHorizonsAPI == null) return;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "Jam3") return;

        var door = GameObject.Find("MAGISTARIUM_Body/Sector/Magistration/MagistariumSector/Sectors/GrandChamber/HorrorDoor");

        var grandChamber = door.transform.parent;

        LibraryDoor = UnityEngine.Object.Instantiate(door);
        LibraryDoor.name = "APRandomizer_HN2_LibraryDoor";
        LibraryDoor.transform.SetParent(grandChamber, false);
        var lp = LibraryDoor.transform.localPosition; LibraryDoor.transform.localPosition = new Vector3(lp.x, lp.y, 42);
        LibraryDoor.transform.eulerAngles = new Vector3(0, 33, 0);

        GameObject libraryDoorCollision = new GameObject("APRandomizer_HN2_LibraryDoor_ColliderAndPrompt");
        libraryDoorCollision.transform.SetParent(LibraryDoor.transform, false);
        var libraryBox = libraryDoorCollision.AddComponent<BoxCollider>();
        LibraryDoorIR = libraryDoorCollision.AddComponent<InteractReceiver>();

        DormitoryDoor = UnityEngine.Object.Instantiate(door);
        DormitoryDoor.name = "APRandomizer_HN2_DormitoryDoor";
        DormitoryDoor.transform.SetParent(grandChamber, false);
        lp = DormitoryDoor.transform.localPosition; DormitoryDoor.transform.localPosition = new Vector3(-30, lp.y, lp.z);
        DormitoryDoor.transform.eulerAngles = new Vector3(0, 215, 0);

        GameObject dormDoorPrompt = new GameObject("APRandomizer_HN2_DormitoryDoor_Prompt");
        dormDoorPrompt.transform.SetParent(DormitoryDoor.transform, false);
        var dormDoorPromptBox = dormDoorPrompt.AddComponent<BoxCollider>();
        DormitoryDoorIR = dormDoorPrompt.AddComponent<InteractReceiver>();

        EngineDoor = UnityEngine.Object.Instantiate(door);
        EngineDoor.name = "APRandomizer_HN2_EngineDoor";
        EngineDoor.transform.SetParent(grandChamber, false);
        lp = EngineDoor.transform.localPosition; EngineDoor.transform.localPosition = new Vector3(-30, lp.y, 42);
        EngineDoor.transform.eulerAngles = new Vector3(0, 144, 0);
        var engineBox = EngineDoor.AddComponent<BoxCollider>();
        EngineDoorIR = EngineDoor.AddComponent<InteractReceiver>();
    }

    // Unfortunately IR.ChangePrompt() explodes if called in OnCompleteSceneLoad, so we have to do hacky stuff to delay it
    private static bool promptsSet = false;

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerSectorDetector), nameof(PlayerSectorDetector.OnAddSector))]
    public static void PlayerSectorDetector_OnAddSector(PlayerSectorDetector __instance)
    {
        // we only need to do this once
        if (promptsSet) return;

        // and only if we're in the NH2 system
        if (APRandomizer.NewHorizonsAPI == null) return;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "Jam3") return;

        LibraryDoorIR.ChangePrompt("Requires Magistarium Library Access Code");
        LibraryDoorIR.SetKeyCommandVisible(false);

        DormitoryDoorIR.ChangePrompt("Requires Magistarium Dormitory Access Code");
        DormitoryDoorIR.SetKeyCommandVisible(false);

        EngineDoorIR.ChangePrompt("Requires Magistarium Engine Access Code");
        EngineDoorIR.SetKeyCommandVisible(false);
    }
}
