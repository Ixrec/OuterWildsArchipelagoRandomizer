using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer;
/// <summary>
/// Depracated. Use ArchConsoleManager instead.
/// </summary>
internal class InGameConsole : MonoBehaviour
{
    // Deprecated, use ArchConsoleManager instead.
    // This asset was provided by GameWyrm based on what they use for Outer Relics' suitless fallback notifications.
    // I currently don't know how to edit asset bundles (despite attempting to look up how) so
    // significant portions of this file, like Awake(), are probably very redundant.
    public static AssetBundle fallbackNotificationsBundle;
    public static InGameConsole Instance = null;

    public static void Setup()
    {
        fallbackNotificationsBundle = Randomizer.Instance.ModHelper.Assets.LoadBundle("Assets/fallbacknotifications");
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            GameObject canvasObject = Instantiate(fallbackNotificationsBundle.LoadAsset<GameObject>("FallbackCanvas"));
            var fallbackNotificationsObject = canvasObject.transform.GetChild(0);

            Queue<string> oldBuffer = Instance?.bufferedMessages;
            List<(DateTimeOffset, string)> oldContent = Instance?.consoleContent;

            Instance = fallbackNotificationsObject.gameObject.AddComponent<InGameConsole>();
            if ((oldBuffer?.Any() ?? false) || (oldContent?.Any() ?? false))
            {
                Randomizer.Instance.ModHelper.Console.WriteLine($"InGameConsole.OnCompleteSceneLoad: previous scene's console component still has " +
                    $"{oldContent.Count} visible messages and {oldBuffer.Count} buffered messages. Moving these to the new console component.");
                Instance.bufferedMessages = oldBuffer;
                Instance.consoleContent = oldContent;
            }
        };
    }

    private Text unityTextObject;

    private void Awake()
    {
        var rt = GetComponent<RectTransform>();
        var ap = rt.anchoredPosition;
        ap.x = -250;
        ap.y = -200;
        rt.anchoredPosition = ap;
        var sd = rt.sizeDelta;
        sd.x = 400;
        sd.y = 0;
        rt.sizeDelta = sd;

        unityTextObject = GetComponent<Text>();
        unityTextObject.font = Resources.Load<Font>("fonts/english - latin/SpaceMono-Regular");
        unityTextObject.fontSize = 12;
        unityTextObject.alignment = TextAnchor.LowerLeft;
        unityTextObject.text = "";
    }

    // Together, these values imply that at most 20/2 = 10 messages can be displayed at once
    const double MessageDisplaySeconds = 20;
    const double MessageBufferSeconds = 2;

    private List<(DateTimeOffset, string)> consoleContent = new();
    private DateTimeOffset lastConsoleMessageAdded = DateTimeOffset.MinValue;
    private Queue<string> bufferedMessages = new();

    private double SecondsSinceLastMessageAdd() =>
        (DateTimeOffset.UtcNow - lastConsoleMessageAdded).TotalSeconds;
    private void AddMessageToConsole(string message)
    {
        lastConsoleMessageAdded = DateTimeOffset.UtcNow;
        consoleContent.Add((lastConsoleMessageAdded, message));
    }
    private void UpdateConsoleText() =>
        unityTextObject.text = string.Join("\n", consoleContent.Select(content => content.Item2));

    private void Update()
    {
        bool changed = false;

        var contentToContinueDisplaying = consoleContent.Where(content => (DateTimeOffset.UtcNow - content.Item1).TotalSeconds < MessageDisplaySeconds);
        if (contentToContinueDisplaying.Count() < consoleContent.Count)
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"InGameConsole.Update: {consoleContent.Count - contentToContinueDisplaying.Count()} " +
                $"console message(s) expired, leaving {contentToContinueDisplaying.Count()} visible message(s)");
            consoleContent = contentToContinueDisplaying.ToList();
            changed = true;
        }
        if (bufferedMessages.Count > 0 && SecondsSinceLastMessageAdd() > MessageBufferSeconds)
        {
            var text = bufferedMessages.Dequeue();
            Randomizer.Instance.ModHelper.Console.WriteLine($"InGameConsole.Update: adding buffered message to console: '{text}'");
            AddMessageToConsole(text);
            changed = true;
        }

        if (changed) {
            UpdateConsoleText();
        }
    }

    public void AddNotification(string notification)
    {
        NotificationData notif = new NotificationData(NotificationTarget.None, notification.ToUpper());
        NotificationManager.SharedInstance.PostNotification(notif);

        if (SecondsSinceLastMessageAdd() < MessageBufferSeconds)
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"InGameConsole.AddNotification: last console message was too recent, buffering new message: '{notification}'");
            bufferedMessages.Enqueue(notification);
            return;
        }

        Randomizer.Instance.ModHelper.Console.WriteLine($"InGameConsole.AddNotification: immediately adding message to console: '{notification}'");
        AddMessageToConsole(notification);
        UpdateConsoleText();
    }
}
