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
        private GameObject gameplayConsole;
        private GameObject consoleEntry => Randomizer.Assets.LoadAsset<GameObject>("ConsoleText");
        private List<string> consoleEntries;
        private List<GameObject> pauseConsoleEntries;
        private List<GameObject> gameplayConsoleEntries;
        private Text consoleText;
        private bool isPaused;

        public void Awake()
        {
            LoadManager.OnCompleteSceneLoad += CreateConsoles;
            consoleEntries = new List<string>();
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

        private void CreateConsoles(OWScene scene, OWScene loadScene)
        {
            if (loadScene != OWScene.SolarSystem) return;
            // Create objects and establish references
            pauseConsoleEntries = new List<GameObject>();
            gameplayConsoleEntries = new List<GameObject>();
            pauseConsole = GameObject.Instantiate(Randomizer.Assets.LoadAsset<GameObject>("ArchRandoCanvas"));
            pauseConsoleContent = pauseConsole.transform.Find("PauseConsole/Scroll View/Viewport/Content").gameObject;
            gameplayConsole = pauseConsole.transform.Find("GameplayConsole").gameObject;
            // Copy text over from previous loops
            foreach (string entry in consoleEntries)
            {
                AddText(entry, true);
            }
            pauseConsole.GetComponentInChildren<InputField>().onEndEdit.AddListener(OnConsoleEntry);
            consoleText = pauseConsole.GetComponentInChildren<InputField>().transform.Find("Text").GetComponent<Text>(); // What is this line KEKW
            pauseConsole.transform.GetChild(0).gameObject.SetActive(false);
        }

        private void ShowConsoles(bool showPauseConsole)
        {
            pauseConsole.transform.GetChild(0).gameObject.SetActive(showPauseConsole);
            gameplayConsole.SetActive(!showPauseConsole);
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        public void AddText(string text, bool skipGameplayConsole = false)
        {
            consoleEntries.Add(text);
            GameObject textEntry = Instantiate(consoleEntry, pauseConsoleContent.transform);
            textEntry.GetComponent<Text>().text = text;
            pauseConsoleEntries.Add(textEntry);

            // We don't need to bother editing the Gameplay Console if this is on
            if (skipGameplayConsole) return;

            if (gameplayConsoleEntries.Count < 6)
            {
                textEntry = Instantiate(consoleEntry, gameplayConsole.transform);
                gameplayConsoleEntries.Add(textEntry);
            }
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
        }

        /// <summary>
        /// Adds a new text entry to the in-game consoles.
        /// Identical to Randomizer.Instance.ArchConsoleManager.AddText(text), but implemented for convenience.
        /// </summary>
        /// <param name="text">The text to add to the consoles</param>
        /// <param name="skipGameplayConsole">Whether to only show text on the pause console</param>
        public static void AddConsoleText(string text, bool skipGameplayConsole)
        {
            Randomizer.Instance.ArchConsoleManager.AddText(text);
        }

        /// <summary>
        /// Runs whenever the console text is submitted. Currently has no implementation.
        /// </summary>
        /// <param name="text"></param>
        public void OnConsoleEntry(string text)
        {
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
    }
}