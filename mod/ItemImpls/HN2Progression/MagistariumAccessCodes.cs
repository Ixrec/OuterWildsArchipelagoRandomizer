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
                LibraryDoor?.SetActive(!value);
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
                DormitoryDoor?.SetActive(!value);
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
                EngineDoor?.SetActive(!value);
            }
        }
    }

    private static GameObject LibraryDoor = null;
    private static GameObject DormitoryDoor = null;
    private static GameObject EngineDoor = null;

    private static InteractReceiver LibraryDoorIR = null;
    private static InteractReceiver DormitoryDoorIR = null;
    private static InteractReceiver EngineDoorIR = null;

    public static void OnJam3StarSystemLoadedEvent()
    {
        // make sure we aren't hanging on to any stale references
        LibraryDoor = null;
        DormitoryDoor = null;
        EngineDoor = null;

        LibraryDoorIR = null;
        DormitoryDoorIR = null;
        EngineDoorIR = null;

        // stop here unless we're in the HN2 system
        if (APRandomizer.NewHorizonsAPI == null) return;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "Jam3") return;

        var grandChamber = GameObject.Find("MAGISTARIUM_Body/Sector/Magistration/MagistariumSector/Sectors/GrandChamber");
        var door = grandChamber.transform.Find("HorrorDoor").gameObject;

        // library door
        LibraryDoor = UnityEngine.Object.Instantiate(door);
        LibraryDoor.name = "APRandomizer_HN2_LibraryDoor";
        LibraryDoor.transform.SetParent(grandChamber.transform, false);
        var lp = LibraryDoor.transform.localPosition; LibraryDoor.transform.localPosition = new Vector3(lp.x, lp.y, 42);
        LibraryDoor.transform.eulerAngles = new Vector3(0, 33, 0);

        GameObject libraryDoorCollision = new GameObject("APRandomizer_HN2_LibraryDoor_ColliderAndPrompt");
        libraryDoorCollision.transform.SetParent(LibraryDoor.transform, false);
        var libraryBox = libraryDoorCollision.AddComponent<BoxCollider>();
        LibraryDoorIR = libraryDoorCollision.AddComponent<InteractReceiver>();

        // dormitory door
        DormitoryDoor = UnityEngine.Object.Instantiate(door);
        DormitoryDoor.name = "APRandomizer_HN2_DormitoryDoor";
        DormitoryDoor.transform.SetParent(grandChamber.transform, false);
        lp = DormitoryDoor.transform.localPosition; DormitoryDoor.transform.localPosition = new Vector3(-30, lp.y, lp.z);
        DormitoryDoor.transform.eulerAngles = new Vector3(0, 215, 0);

        GameObject dormDoorCollision = new GameObject("APRandomizer_HN2_DormitoryDoor_ColliderAndPrompt");
        dormDoorCollision.transform.SetParent(DormitoryDoor.transform, false);
        var dormDoorPromptBox = dormDoorCollision.AddComponent<BoxCollider>();
        DormitoryDoorIR = dormDoorCollision.AddComponent<InteractReceiver>();

        // engine door
        EngineDoor = UnityEngine.Object.Instantiate(door);
        EngineDoor.name = "APRandomizer_HN2_EngineDoor";
        EngineDoor.transform.SetParent(grandChamber.transform, false);
        lp = EngineDoor.transform.localPosition; EngineDoor.transform.localPosition = new Vector3(-30, lp.y, 42);
        EngineDoor.transform.eulerAngles = new Vector3(0, 144, 0);

        GameObject engineDoorCollision = new GameObject("APRandomizer_HN2_EngineDoor_ColliderAndPrompt");
        engineDoorCollision.transform.SetParent(EngineDoor.transform, false);
        var engineDoorPromptBox = engineDoorCollision.AddComponent<BoxCollider>();
        EngineDoorIR = engineDoorCollision.AddComponent<InteractReceiver>();

        // finally, apply current game state
        LibraryDoor?.SetActive(!hasLibraryAccess);
        DormitoryDoor?.SetActive(!hasDormitoriesAccess);
        EngineDoor?.SetActive(!hasEngineAccess);
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
