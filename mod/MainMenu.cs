using HarmonyLib;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;
using OWML.Common;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class MainMenu
{
    // OWML.MenuHelper has no API for hiding the vanilla buttons, so we do that here instead
    [HarmonyPostfix, HarmonyPatch(typeof(TitleScreenManager), nameof(TitleScreenManager.SetUpMainMenu))]
    public static void TitleScreenManager_SetUpMainMenu_Postfix()
    {
        var resumeButton = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-ResumeGame");
        resumeButton.SetActive(false);
        var newButton = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-NewGame");
        newButton.SetActive(false);
    }

    // The main menu does not autoselect the top visible button, but rather a specific named button, so the fact that we're forced
    // to hide some vanilla buttons means we also have to hijack this autoselection logic to unbreak the main menu.
    // In practice this is primarily a problem for controller input.
    [HarmonyPostfix, HarmonyPatch(typeof(OWMenuInputModule), nameof(OWMenuInputModule.SelectOnNextUpdate))]
    public static void OWMenuInputModule_SelectOnNextUpdate(OWMenuInputModule __instance, Selectable selectable)
    {
        if (selectable?.name == "Button-NewGame" || selectable?.name == "Button-ResumeGame")
        {
            GameObject gameObject = (APRandomizer.SaveData == null) ? NewRandomExpeditionGO : ResumeRandomExpeditionGO;
            var modButton = gameObject.GetComponent<Button>();
            if (modButton != null)
            {
                APRandomizer.OWMLModConsole.WriteLine($"OWMenuInputModule_SelectOnNextUpdate changing selectable from {selectable.name} to {gameObject.name}");
                __instance._nextSelectableQueue.Remove(selectable);
                __instance._nextSelectableQueue.Add(modButton);
            }
        }
    }

    private static GameObject NewRandomExpeditionGO = null;
    private static SubmitAction NewRandomExpeditionSA = null;
    public static GameObject ChangeConnInfoGO = null;
    private static SubmitAction ChangeConnInfoSA = null;
    public static GameObject ResumeRandomExpeditionGO = null;
    private static SubmitAction ResumeRandomExpeditionSA = null;

    public static void SetupTitleMenu(ITitleMenuManager titleManager)
    {
        var hostAndPortInput = APRandomizer.Instance.ModHelper.MenuHelper.PopupMenuManager.CreateInputFieldPopup(
            "Connection Info (1/3): Hostname & Port\n\ne.g. \"localhost:38281\", \"archipelago.gg:12345\"",
            "Enter hostname:port...",
            "Confirm", "Cancel");
        var slotInput = APRandomizer.Instance.ModHelper.MenuHelper.PopupMenuManager.CreateInputFieldPopup(
            "Connection Info (2/3): Slot/Player Name\n\ne.g. \"Hearthian1\", \"Hearthian2\"",
            "Enter slot/player name...",
            "Confirm", "Cancel");
        var passwordInput = APRandomizer.Instance.ModHelper.MenuHelper.PopupMenuManager.CreateInputFieldPopup(
            "Connection Info (3/3): Password\n\nLeave blank if your server isn't using a password",
            "Enter password...",
            "Confirm", "Cancel");

        NewRandomExpeditionSA = titleManager.CreateTitleButton("NEW RANDOM EXPEDITION", 0, true);
        ChangeConnInfoSA = titleManager.CreateTitleButton("CHANGE CONNECTION INFO", 0, true);
        ResumeRandomExpeditionSA = titleManager.CreateTitleButton("RESUME RANDOM EXPEDITION", 0, true);

        var pathToMainMenuButtons = "TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup";
        NewRandomExpeditionGO = GameObject.Find($"{pathToMainMenuButtons}/Button-NEW RANDOM EXPEDITION");
        ChangeConnInfoGO = GameObject.Find($"{pathToMainMenuButtons}/Button-CHANGE CONNECTION INFO");
        ResumeRandomExpeditionGO = GameObject.Find($"{pathToMainMenuButtons}/Button-RESUME RANDOM EXPEDITION");

        OWML.Utils.MenuExtensions.SetButtonVisible(ChangeConnInfoSA, APRandomizer.SaveData != null);
        OWML.Utils.MenuExtensions.SetButtonVisible(ResumeRandomExpeditionSA, APRandomizer.SaveData != null);

        SubmitAction lastButtonClicked = null;
        APConnectionData connData = (APRandomizer.SaveData == null) ? new() : APRandomizer.SaveData.apConnectionData;

        NewRandomExpeditionSA.OnSubmitAction += () =>
        {
            lastButtonClicked = NewRandomExpeditionSA;
            StartConnInfoInput(true);
        };
        ChangeConnInfoSA.OnSubmitAction += () =>
        {
            lastButtonClicked = ChangeConnInfoSA;
            StartConnInfoInput(false);
        };

        ResumeRandomExpeditionSA.OnSubmitAction += () =>
        {
            lastButtonClicked = ResumeRandomExpeditionSA;
            APRandomizer.AttemptToConnect(() => LoadTheGame(ResumeRandomExpeditionSA, titleManager));
        };

        void StartConnInfoInput(bool resetRoomId)
        {
            connData = (APRandomizer.SaveData == null) ? new() : APRandomizer.SaveData.apConnectionData;
            // It's important that we only warn the player about a "wrong" room id when they're connecting with a non-new save file,
            // and the easiest way to do that is for New Expedition to reset our cached room id to null.
            if (resetRoomId)
                connData.roomId = null;

            hostAndPortInput.EnableMenu(true);
            if (connData.hostname != null && connData.hostname.Length > 0)
                hostAndPortInput.GetInputField().text = connData.hostname + ':' + connData.port;
        }

        hostAndPortInput.OnPopupConfirm += () => {
            var inputText = hostAndPortInput.GetInputText();
            APRandomizer.OWMLModConsole.WriteLine($"hostAndPortInput.OnPopupConfirm: {inputText}");

            var split = inputText.Split(':');
            connData.hostname = split[0];
            // if the player left out a port number, use the default localhost port of 38281
            connData.port = (split.Length > 1) ? uint.Parse(split[1]) : 38281;

            slotInput.EnableMenu(true);
            if (connData.slotName != null && connData.slotName.Length > 0)
                slotInput.GetInputField().text = connData.slotName;
        };

        slotInput.OnPopupConfirm += () => {
            var inputText = slotInput.GetInputText();
            APRandomizer.OWMLModConsole.WriteLine($"slotInput.OnPopupConfirm: {inputText}");

            connData.slotName = inputText;

            passwordInput.EnableMenu(true);
            if (connData.password != null && connData.password.Length > 0)
                passwordInput.GetInputField().text = connData.password;
        };

        passwordInput.OnPopupConfirm += () => {
            var inputText = passwordInput.GetInputText();
            APRandomizer.OWMLModConsole.WriteLine($"passwordInput.OnPopupConfirm: {inputText}");

            connData.password = inputText;

            if (lastButtonClicked == ChangeConnInfoSA)
            {
                APRandomizer.SaveData.apConnectionData = connData;
                APRandomizer.WriteToSaveFile();
            }
            else if (lastButtonClicked == NewRandomExpeditionSA)
            {
                APRandomizerSaveData saveData = new();
                saveData.apConnectionData = connData;
                saveData.locationsChecked = Enum.GetValues(typeof(Location)).Cast<Location>().ToDictionary(ln => ln, _ => false);
                saveData.itemsAcquired = Enum.GetValues(typeof(Item)).Cast<Item>().ToDictionary(ln => ln, _ => 0u);
                APRandomizer.SaveData = saveData;

                APRandomizer.AttemptToConnect(() =>
                {
                    // we don't overwrite the mod save file and the player's inventory until we're sure we can really start playing this new game
                    APRandomizer.Instance.ModHelper.Storage.Save<APRandomizerSaveData>(saveData, APRandomizer.SaveFileName);
                    foreach (var kv in APRandomizer.SaveData.itemsAcquired)
                        LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);

                    // also wipe the vanilla save file, since we've bypassed the base game code that would normally do this
                    PlayerData.ResetGame();

                    LoadTheGame(NewRandomExpeditionSA, titleManager);
                });
            }
        };
    }

    // quick and dirty attempt to reproduce what the vanilla New/Resume Expedition buttons do
    private static void LoadTheGame(SubmitAction mainMenuButton, ITitleMenuManager titleManager)
    {
        var scene = PlayerData.GetWarpedToTheEye() ? OWScene.EyeOfTheUniverse : OWScene.SolarSystem;
        LoadManager.LoadSceneAsync(scene, true, LoadManager.FadeType.ToBlack, 1f, false);

        titleManager.SetButtonText(mainMenuButton, "Loading...");

        var lpu = GameObject.Find("TitleMenu").AddComponent<LoadProgressUpdater>();
        lpu.titleMenuManager = titleManager;
        lpu.mainMenuButton = mainMenuButton;

        // SetPersistentCondition() is not safe to call on the main menu because e.g. it can lead to Switch Profile mistakently copying save data onto other profiles,
        // so we wait to initialize the autosplitter conditions until we know we're about to leave the main menu and won't be switching profiles any more.
        foreach (var kv in APRandomizer.SaveData.itemsAcquired)
            if (ItemNames.itemToPersistentCondition.TryGetValue(kv.Key, out var condition))
                PlayerData.SetPersistentCondition(condition, kv.Value > 0); // for now, only unique items have conditions
    }

    class LoadProgressUpdater : MonoBehaviour
    {
        public ITitleMenuManager titleMenuManager;
        public SubmitAction mainMenuButton;

        private void Update()
        {
            if (
                mainMenuButton != null &&
                (LoadManager.GetLoadingScene() == OWScene.SolarSystem || LoadManager.GetLoadingScene() == OWScene.EyeOfTheUniverse)
            )
            {
                // I dunno what's going on with the GetAsyncLoadProgress() return value, but it's not a normal 0-1 or 0-100 number like you'd expect.
                // This translation into a human-readable progress percentage is copy-pasted from:
                // https://github.com/misternebula/MenuFramework/blob/3215c0d66782908e4de557bf71ee36adf693c640/MenuFramework/CustomSubmitActionLoadScene.cs#L16-L19
                var loadProgress = LoadManager.GetAsyncLoadProgress();
                loadProgress = loadProgress < 0.1f
                    ? Mathf.InverseLerp(0f, 0.1f, loadProgress) * 0.9f
                    : 0.9f + (Mathf.InverseLerp(0.1f, 1f, loadProgress) * 0.1f);

                var loadProgressString = loadProgress.ToString("P0"); // P = percentage format, 0 = no decimal digits

                titleMenuManager.SetButtonText(mainMenuButton, $"Loading... {loadProgressString}");
            }
        }
    }

    public static void SetupPauseMenu(IPauseMenuManager pauseManager)
    {
        if (LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse)
        {
            var quitAndReset = pauseManager.MakeSimpleButton("QUIT AND RESET\nTO SOLAR SYSTEM", 0, false);
            quitAndReset.OnSubmitAction += () =>
            {
                APRandomizer.OWMLModConsole.WriteLine($"reset clicked");
                PlayerData.SaveEyeCompletion();
                LoadManager.LoadScene(OWScene.TitleScreen, LoadManager.FadeType.None, 1f, true);
            };
        }
    }
}
