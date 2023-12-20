using OWML.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Class that handles adding text to the console
    /// </summary>
    public class ArchConsoleManager : MonoBehaviour
    {
        private GameObject pauseConsole;
        private GameObject pauseConsoleContent;
        private GameObject pauseConsoleViewer;
        private GameObject gameplayConsole;
        private GameObject consoleEntry => Randomizer.Assets.LoadAsset<GameObject>("ConsoleText");
        private List<string> consoleMasterList;
        private List<GameObject> pauseConsoleEntries;
        private List<GameObject> gameplayConsoleEntries;
        private InputField consoleText;
        private bool isPaused;

        private void Awake()
        {
            LoadManager.OnCompleteSceneLoad += CreateConsoles;
            consoleMasterList = new List<string>();
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
                ShowConsoles(isPaused);
            }
        }

        // Creates the two console displays
        private void CreateConsoles(OWScene scene, OWScene loadScene)
        {
            if (loadScene != OWScene.SolarSystem && loadScene != OWScene.EyeOfTheUniverse) return;
            // Create objects and establish references
            pauseConsoleEntries = new List<GameObject>();
            gameplayConsoleEntries = new List<GameObject>();
            pauseConsole = GameObject.Instantiate(Randomizer.Assets.LoadAsset<GameObject>("ArchRandoCanvas"));
            pauseConsoleContent = pauseConsole.transform.Find("PauseConsole/Scroll View/Viewport/Content").gameObject;
            pauseConsoleViewer = pauseConsole.transform.GetChild(0).gameObject;
            gameplayConsole = pauseConsole.transform.Find("GameplayConsole").gameObject;
            foreach (Transform child in gameplayConsole.transform)
            {
                gameplayConsoleEntries.Add(child.gameObject);
                child.GetComponent<Text>().text = string.Empty;
            }
            pauseConsoleContent.AddComponent<FixConsoleLayout>();
            gameplayConsole.AddComponent<FixConsoleLayout>();

            // Copy text over from previous loops
            foreach (string entry in consoleMasterList)
            {
                AddText(entry, true, true);
            }
            pauseConsole.GetComponentInChildren<InputField>().onEndEdit.AddListener(OnConsoleEntry);
            consoleText = pauseConsole.GetComponentInChildren<InputField>();
            pauseConsoleViewer.SetActive(false);

            AddText($"<color=#6BFF6B>Welcome to your {LoopNumber()} loop!</color>", true);
        }

        // Shows the appropriate consoles when the game is paused or not
        private void ShowConsoles(bool showPauseConsole)
        {
            pauseConsoleViewer.SetActive(showPauseConsole);
            gameplayConsole.SetActive(!showPauseConsole);
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        public void AddText(string text, bool skipGameplayConsole = false, bool skipMasterList = false)
        {
            if (!skipMasterList) consoleMasterList.Add(text);
            GameObject textEntry = Instantiate(consoleEntry, pauseConsoleContent.transform);
            textEntry.GetComponent<Text>().text = text;
            pauseConsoleEntries.Add(textEntry);

            // We don't need to bother editing the Gameplay Console if this is on
            if (skipGameplayConsole) return;

            for (int i = 0; i < gameplayConsoleEntries.Count; i++)
            {
                if (i == gameplayConsoleEntries.Count - 1)
                {
                    gameplayConsoleEntries[i].GetComponent<Text>().text = text;
                }
                else
                {
                    gameplayConsoleEntries[i].GetComponent<Text>().text = gameplayConsoleEntries[i + 1].GetComponent<Text>().text;
                }
            }

            if (!isPaused)
            {
                gameplayConsole.SetActive(true);
            }
            // Attempts to fix weirdness in console layout when multiple messages are received
            LayoutRebuilder.ForceRebuildLayoutImmediate(gameplayConsole.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(pauseConsoleContent.GetComponent<RectTransform>());
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles.
        /// Identical to Randomizer.Instance.ArchConsoleManager.AddText(text), but implemented for convenience.
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        public static void AddConsoleText(string text, bool skipGameplayConsole = false)
        {
            Randomizer.Instance.ArchConsoleManager.AddText(text);
        }

        /// <summary>
        /// Runs whenever the console text is submitted. Currently has no implementation.
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
            else
            {
                Randomizer.Instance.ModHelper.Console.WriteLine($"Console text not implemented. Received {text}.", MessageType.Info);
            }
            consoleText.text = "";
        }

        private string LoopNumber()
        {
            string loopSuffix = "th";
            int loopCount = TimeLoop.GetLoopCount() + 1;
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
    }
}