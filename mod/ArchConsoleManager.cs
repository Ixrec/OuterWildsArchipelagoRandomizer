using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Class that handles adding text to the console
    /// </summary>
    public class ArchConsoleManager : MonoBehaviour
    {
        public static bool ConsoleMuted = false;
        public static bool FilterPlayer = false;

        private GameObject console;
        private GameObject pauseConsole;
        private GameObject pauseConsoleVisuals;
        private GameObject gameplayConsole;
        private GameObject overflowWarning;
        private Text pauseConsoleText;
        private Text gameplayConsoleText;
        private List<string> consoleHistory;
        private Queue<string> gameplayConsoleEntries;
        private InputField consoleText;
        private Button muteButton;
        private Button filterButton;
        private bool isPaused;
        private List<float> gameplayConsoleTimers;
        private Material progressMat;
        private Text progressText;
        private ArchipelagoSession session;

        // Console can only handle ~65000 vertices
        // As there's 4 vertices per character, we can only support a quarter of this
        private const int maxCharacters = 16000;
        private const float clearTimerMax = 20;
        private const string consoleInfoColor = "#6E6E6E";

        private void Awake()
        {
            LoadManager.OnCompleteSceneLoad += CreateConsoles;
            consoleHistory = [];
        }

        private void Start()
        {
            GlobalMessenger.AddListener("EnterConversation", () => gameplayConsole.SetActive(false));
            GlobalMessenger.AddListener("ExitConversation", () => gameplayConsole.SetActive(true));

            APRandomizer.OnSessionOpened += (s) =>
            {
                s.Locations.CheckedLocationsUpdated += UpdateProgress;
                session = s;
            };
        }

        private void Update()
        {
            // Show the correct version of the console depending on if the game is paused or not
            if (isPaused != APRandomizer.Instance.ModHelper.Menus.PauseMenu.IsOpen)
            {
                isPaused = !isPaused;
                if (isPaused)
                {
                    // On most aspect ratios, "MEDITATE UNTIL NEXT LOOP" is the only pause menu button that clips into this console,
                    // and it's much wider than all the other buttons, and the console would have to be painfully narrow to avoid this,
                    // so shortening this button to only one word is the least bad way of reducing clipping.
                    var pauseMenuMedidateButtonText = GameObject.Find("PauseMenu/PauseMenuCanvas/PauseMenuBlock/PauseMenuItems/PauseMenuItemsLayout/Button-EndCurrentLoop/HorizontalLayoutGroup/Text");
                    if (pauseMenuMedidateButtonText) pauseMenuMedidateButtonText.GetComponent<Text>().text = "MEDITATE";
                }
                ShowConsoles(isPaused);
            }
            // Clear console entries after enough time has passed
            if (gameplayConsoleTimers != null && gameplayConsoleTimers.Count > 0)
            {
                for (int i = 0; i < gameplayConsoleTimers.Count; i++)
                {
                    gameplayConsoleTimers[i] -= Time.deltaTime;
                    if (gameplayConsoleTimers[i] <= 0 && gameplayConsoleEntries.Count > 0)
                    {
                        gameplayConsoleEntries.Dequeue();
                        UpdateText();
                    }
                }
                if (gameplayConsoleTimers[0] <= 0)
                {
                    gameplayConsoleTimers.RemoveAt(0);
                }
            }
        }

        // Creates the two console displays
        private void CreateConsoles(OWScene scene, OWScene loadScene)
        {
            if (loadScene != OWScene.SolarSystem && loadScene != OWScene.EyeOfTheUniverse) return;
            // Create objects and establish references
            gameplayConsoleEntries = new Queue<string>();
            gameplayConsoleTimers = [];
            console = GameObject.Instantiate(APRandomizer.Assets.LoadAsset<GameObject>("ArchRandoCanvas"));
            pauseConsoleVisuals = console.transform.Find("PauseConsole").gameObject;
            pauseConsole = console.transform.Find("PauseConsole/Scroll View/Viewport/PauseConsoleText").gameObject;
            gameplayConsole = console.transform.Find("GameplayConsole/GameplayConsoleText").gameObject;
            overflowWarning = pauseConsoleVisuals.transform.Find("Warning Message").gameObject;
            muteButton = pauseConsoleVisuals.transform.Find("Buttons/Buttons Container/MuteButton").GetComponent<Button>();
            muteButton.transform.Find("MuteImageOff").gameObject.SetActive(!ConsoleMuted);
            muteButton.transform.Find("MuteImageOn").gameObject.SetActive(ConsoleMuted);
            filterButton = pauseConsoleVisuals.transform.Find("Buttons/Buttons Container/FilterButton").GetComponent<Button>();
            filterButton.transform.Find("FilterAll").gameObject.SetActive(!FilterPlayer);
            filterButton.transform.Find("FilterPlayer").gameObject.SetActive(FilterPlayer);
            overflowWarning.SetActive(false);
            pauseConsoleText = pauseConsole.GetComponent<Text>();
            gameplayConsoleText = gameplayConsole.GetComponent<Text>();
            progressText = pauseConsoleVisuals.transform.Find("Buttons/Buttons Container/ProgressWheel/Progress").GetComponent<Text>();
            progressMat = pauseConsoleVisuals.transform.Find("Buttons/Buttons Container/ProgressWheel/WheelBG/WheelProgress").GetComponent<Image>().material;

            pauseConsoleText.text = string.Empty;
            gameplayConsoleText.text = string.Empty;

            // Copy text over from previous loops
            foreach (string entry in consoleHistory)
            {
                AddText(entry, true, AudioType.None, true);
            }
            console.GetComponentInChildren<InputField>().onEndEdit.AddListener(OnConsoleEntry);
            muteButton.onClick.AddListener(OnClickMuteButton);
            filterButton.onClick.AddListener(OnClickFilterButton);
            consoleText = console.GetComponentInChildren<InputField>();
            pauseConsoleVisuals.SetActive(false);

            UpdateProgress();

            if (loadScene == OWScene.SolarSystem)
                StartCoroutine(LoopGreeting());
        }

        // Shows the appropriate consoles when the game is paused or not
        private void ShowConsoles(bool showPauseConsole)
        {
            pauseConsoleVisuals.SetActive(showPauseConsole);
            gameplayConsole.SetActive(!showPauseConsole);
        }

        private void UpdateProgress(ReadOnlyCollection<long> checkedLocations = null)
        {
            float progress = session.Locations.AllLocationsChecked.Count;
            float maxLocations = session.Locations.AllLocations.Count;
            float progressPercent = progress / maxLocations;
            APRandomizer.OWMLWriteLine($"Percent Complete: {progressPercent}%", OWML.Common.MessageType.Debug);
            progressText.text = $"{progress}/{maxLocations}";
            progressMat.SetFloat("_PercentAccessible", progressPercent);
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        /// <param name="soundToPlay">If specified, plays the associated sound. See https://nh.outerwildsmods.com/reference/audio-enum/ for a list of sounds. Sounds do not play while paused.</param>
        /// <param name="skipHistory">Whether to not save this text between loops</param>
        public void AddText(string text, bool skipGameplayConsole = false, AudioType soundToPlay = AudioType.None, bool skipHistory = false)
        {
            if (!skipHistory) consoleHistory.Add(text);
            string consoleText = pauseConsoleText.text;
            if (consoleText == "")
            {
                consoleText = text;
            }
            else
            {                
                consoleText += "\n" + text;
            }
            // Overflow fix
            while (consoleText.Length > maxCharacters)
            {
                string str = consoleText.Split('\n')[0] + "\n";
                consoleText = consoleText.Replace(str, "");
                overflowWarning.SetActive(true);
            }
            pauseConsoleText.text = consoleText;

            // We don't need to bother editing the Gameplay Console if this is on
            if (!skipGameplayConsole)
            {
                if (gameplayConsoleEntries.Count >= 6)
                {
                    gameplayConsoleEntries.Dequeue();
                }
                gameplayConsoleEntries.Enqueue("\n" + text);
                UpdateText();
                gameplayConsoleTimers.Add(clearTimerMax);
            }

            if (!isPaused)
            {
                gameplayConsole.SetActive(true);
                if (soundToPlay != AudioType.None && !ConsoleMuted && !skipGameplayConsole)
                {
                    Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(soundToPlay);
                }
            }
        }

        /// <summary>
        /// Updates the gameplay console
        /// </summary>
        public void UpdateText()
        {
            if (gameplayConsoleText == null)
            {
                APRandomizer.OWMLWriteLine($"UpdateText() returning early because somehow gameplayConsoleText is null", OWML.Common.MessageType.Warning);
                return;
            }

            gameplayConsoleText.text = string.Empty;
            foreach ( string entry in gameplayConsoleEntries)
            {
                gameplayConsoleText.text += entry;
            }
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles.
        /// Identical to APRandomizer.InGameAPConsole.AddText(text), but implemented for convenience.
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        /// <param name="soundToPlay">If specified, plays the associated sound. See https://nh.outerwildsmods.com/reference/audio-enum/ for a list of sounds. Sounds do not play while paused.</param>
        /// <param name="skipHistory">Whether to not save this text between loops</param>
        public static void AddConsoleText(string text, bool skipGameplayConsole = false, AudioType soundToPlay = AudioType.None, bool skipHistory = false)
        {
            APRandomizer.InGameAPConsole.AddText(text, skipGameplayConsole, soundToPlay, skipHistory);
        }

        /// <summary>
        /// Updates the gameplay console.
        /// Identical to APRandomizer.InGameAPConsole.UpdateText(), but implemented for convenience.
        /// </summary>
        public static void UpdateConsoleText()
        {
            APRandomizer.InGameAPConsole.UpdateText();
        }

        public static void AddAPMessage(LogMessage message, AudioType soundToPlay = AudioType.ShipLogMarkLocation)
        {
            var colorizedParts = message.Parts.Select(messagePart =>
            {
                if (messagePart.IsBackgroundColor) return messagePart.Text;

                var c = messagePart.Color;
                var hexColor = $"{c.R:X2}{c.G:X2}{c.B:X2}";
                return $"<color=#{hexColor}>{messagePart.Text}</color>";
            });
            var inGameConsoleMessage = string.Join("", colorizedParts);

            APRandomizer.OWMLWriteLine($"AddAPMessage() sending this formatted string to the in-game console:\n{inGameConsoleMessage}", OWML.Common.MessageType.Debug);

            // Determine if we should filter out the message
            bool irrelevantToPlayer = true;
            var slot = APRandomizer.APSession.ConnectionInfo.Slot;
            if (FilterPlayer)
            {
                switch (message)
                {
                    case ItemSendLogMessage itemSendLogMessage:
                        var receiver = itemSendLogMessage.Receiver;
                        var sender = itemSendLogMessage.Sender;
                        var networkItem = itemSendLogMessage.Item;
                        if (slot == receiver.Slot || slot == sender.Slot) irrelevantToPlayer = false;
                        break;
                }
            }
            else irrelevantToPlayer = false;

            AddConsoleText(inGameConsoleMessage, irrelevantToPlayer, soundToPlay, false);
        }

        /// <summary>
        /// Runs whenever the console text is submitted.
        /// </summary>
        /// <param name="text"></param>
        public void OnConsoleEntry(string text)
        {
            if (text.StartsWith("!debug "))
            {
                var tokens = text.Substring("!debug ".Length).Split(',');
                Item item = ItemNames.itemNamesReversed[tokens[0]];
                uint count = uint.Parse(tokens[1]);
                APRandomizer.OWMLWriteLine($"Received debug command '{text}'. Calling ApplyItemToPlayer({item}, {count}).");
                LocationTriggers.ApplyItemToPlayer(item, count);
                consoleText.text = "";
                return;
            }

            if (text == "") return;

            // we want to time out relatively quickly if the server happens to be down
            var sayPacketTask = Task.Run(() => session.Socket.SendPacket(new SayPacket() { Text = text }));
            if (!sayPacketTask.Wait(TimeSpan.FromSeconds(1)))
                APRandomizer.OWMLWriteLine($"OnConsoleEntry({text}) task timed out", OWML.Common.MessageType.Warning);

            consoleText.text = "";
        }

        #region ConsoleSettingButtons

        private void OnClickMuteButton()
        {
            ConsoleMuted = !ConsoleMuted;
            muteButton.transform.Find("MuteImageOff").gameObject.SetActive(!ConsoleMuted);
            muteButton.transform.Find("MuteImageOn").gameObject.SetActive(ConsoleMuted);
            AddText((
                ConsoleMuted ?
                $"<color={consoleInfoColor}>Notification sounds muted.</color>" :
                $"<color={consoleInfoColor}>Notification sounds will now play.</color>"), true, AudioType.None, true);
        }

        private void OnClickFilterButton()
        {
            FilterPlayer = !FilterPlayer;
            filterButton.transform.Find("FilterAll").gameObject.SetActive(!FilterPlayer);
            filterButton.transform.Find("FilterPlayer").gameObject.SetActive(FilterPlayer);
            AddText((
                FilterPlayer ?
                $"<color={consoleInfoColor}>You will now only receive notifications for items you receive or send during gameplay. However, all messages will still be logged to the pause console.</color>" :
                $"<color={consoleInfoColor}>You will now receive all notifications during gameplay.</color>"), true, AudioType.None, true);
        }

        #endregion

        private string LoopNumber()
        {
            string loopSuffix = "th";
            int loopCount = TimeLoop.GetLoopCount();
            int shortCount = loopCount % 10;
            if (loopCount < 11 || loopCount > 13)
            {
                loopSuffix = shortCount switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th",
                };
            }
            return loopCount.ToString() + loopSuffix;
        }

        // We need to wait for the end of the frame when loading into the system for the game to be able to read the current loop number
        IEnumerator LoopGreeting()
        {
            yield return new WaitForEndOfFrame();
            AddText($"<color=#6BFF6B>Welcome to your {LoopNumber()} loop!</color>", true);
        }

    }
}