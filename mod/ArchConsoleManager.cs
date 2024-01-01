using Archipelago.MultiClient.Net.MessageLog.Messages;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Class that handles adding text to the console
    /// </summary>
    public class ArchConsoleManager : MonoBehaviour
    {
        private GameObject console;
        private GameObject pauseConsole;
        private GameObject pauseConsoleVisuals;
        private GameObject gameplayConsole;
        private Text pauseConsoleText;
        private Text gameplayConsoleText;
        private List<string> consoleHistory;
        private Queue<string> gameplayConsoleEntries;
        private InputField consoleText;
        private bool isPaused;
        private List<float> gameplayConsoleTimers;

        private const float clearTimerMax = 20;

        private void Awake()
        {
            LoadManager.OnCompleteSceneLoad += CreateConsoles;
            consoleHistory = new List<string>();
        }

        private void Start()
        {
            GlobalMessenger.AddListener("EnterConversation", () => gameplayConsole.SetActive(false));
            GlobalMessenger.AddListener("ExitConversation", () => gameplayConsole.SetActive(true));
        }

        private void Update()
        {
            // Show the correct version of the console depending on if the game is paused or not
            if (isPaused != Randomizer.Instance.ModHelper.Menus.PauseMenu.IsOpen)
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
            gameplayConsoleTimers = new List<float>();
            console = GameObject.Instantiate(Randomizer.Assets.LoadAsset<GameObject>("ArchRandoCanvas"));
            pauseConsoleVisuals = console.transform.Find("PauseConsole").gameObject;
            pauseConsole = console.transform.Find("PauseConsole/Scroll View/Viewport/PauseConsoleText").gameObject;
            gameplayConsole = console.transform.Find("GameplayConsole/GameplayConsoleText").gameObject;
            pauseConsoleText = pauseConsole.GetComponent<Text>();
            gameplayConsoleText = gameplayConsole.GetComponent<Text>();

            pauseConsoleText.text = string.Empty;
            gameplayConsoleText.text = string.Empty;

            // Copy text over from previous loops
            foreach (string entry in consoleHistory)
            {
                AddText(entry, true, AudioType.None, true);
            }
            console.GetComponentInChildren<InputField>().onEndEdit.AddListener(OnConsoleEntry);
            consoleText = console.GetComponentInChildren<InputField>();
            pauseConsoleVisuals.SetActive(false);

            StartCoroutine(LoopGreeting());
        }

        // Shows the appropriate consoles when the game is paused or not
        private void ShowConsoles(bool showPauseConsole)
        {
            pauseConsoleVisuals.SetActive(showPauseConsole);
            gameplayConsole.SetActive(!showPauseConsole);
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
            pauseConsoleText.text += "\n" + text;

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
                if (soundToPlay != AudioType.None)
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
            gameplayConsoleText.text = string.Empty;
            foreach ( string entry in gameplayConsoleEntries)
            {
                gameplayConsoleText.text += entry;
            }
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles.
        /// Identical to Randomizer.InGameAPConsole.AddText(text), but implemented for convenience.
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        /// <param name="soundToPlay">If specified, plays the associated sound. See https://nh.outerwildsmods.com/reference/audio-enum/ for a list of sounds. Sounds do not play while paused.</param>
        /// <param name="skipHistory">Whether to not save this text between loops</param>
        public static void AddConsoleText(string text, bool skipGameplayConsole = false, AudioType soundToPlay = AudioType.None, bool skipHistory = false)
        {
            Randomizer.InGameAPConsole.AddText(text, skipGameplayConsole, soundToPlay, skipHistory);
        }

        /// <summary>
        /// Updates the gameplay console.
        /// Identical to Randomizer.InGameAPConsole.UpdateText(), but implemented for convenience.
        /// </summary>
        public static void UpdateConsoleText()
        {
            Randomizer.InGameAPConsole.UpdateText();
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

            Randomizer.OWMLModConsole.WriteLine($"AddAPMessage() sending this formatted string to the in-game console:\n{inGameConsoleMessage}");

            AddConsoleText(inGameConsoleMessage, false, soundToPlay, false);
        }

        /// <summary>
        /// Runs whenever the console text is submitted.
        /// </summary>
        /// <param name="text"></param>
        public void OnConsoleEntry(string text)
        {
            if (text == "") return;
            // This is not how actual commands should be handled, but this exists for testing
            if (text.StartsWith("!echo "))
            {
                AddText(text.Replace("!echo ", ""));
            }
            else if (text == "!loops")
            {
                AddText($"<color=#6BFF6B>Loops: {TimeLoop.GetLoopCount()}</color>");
            }
            else
            {
                AddText($"<color=#FF6868>Command {text.Split(' ')[0]} not recognized.</color>", true);
            }
            consoleText.text = "";
        }

        private string LoopNumber()
        {
            string loopSuffix = "th";
            int loopCount = TimeLoop.GetLoopCount();
            int shortCount = loopCount % 10;
            if (loopCount < 11 || loopCount > 13)
            {
                switch (shortCount)
                {
                    case 1:
                        loopSuffix = "st";
                        break;
                    case 2:
                        loopSuffix = "nd";
                        break;
                    case 3:
                        loopSuffix = "rd";
                        break;
                    default:
                        loopSuffix = "th";
                        break;
                }
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