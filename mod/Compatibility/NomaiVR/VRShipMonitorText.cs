using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// NomaiVR ship-cockpit monitor text.
/// 
/// Replaces "interact with screen to activate" with "not available" if the relevant items are not unlocked.
/// </summary>
internal static class VRShipMonitorText
{

    // The GameObject names NomaiVR gives the three monitor texts (Ship{Signalscope,Probe,LandingCam}Interact.Awake).
    private static readonly Dictionary<string, Item> textObjectNames = new()
    {
        ["VrShipSignalscopeText"] = Item.Signalscope,
        ["VrShipProbeMonitorText"] = Item.Scout,
        ["VrLandingCamText"] = Item.LandingCamera,
    };

    private class ManagedText
    {
        public Text text;
        public string originalText;
        public Color originalColor;
    }

    private static readonly Dictionary<Item, ManagedText> managedTexts = new();

    private static readonly Color UnavailableColor = new(1f, 0.4f, 0.3f, 0.1f);

    // How many frames to keep retrying the scene-load sweep while waiting for NomaiVR to create the texts.
    private const int MaxSweepAttempts = 10;

    private static bool IsToolOwned(Item tool) => tool switch
    {
        Item.Signalscope => SignalscopeManager.hasSignalscope,
        Item.Scout => Scout.hasScout,
        Item.LandingCamera => LandingCamera.hasLandingCamera,
        _ => true, // not relevant
    };

    /// <summary>
    /// Called from APRandomizer's OnCompleteSceneLoad dispatcher. NomaiVR (re)creates the ship monitor texts on every
    /// SolarSystem load, so we re-find them and apply their text/color here.
    /// </summary>
    public static void OnCompleteSceneLoad(OWScene scene, OWScene loadScene)
    {
        if (!VRCompatibility.IsNomaiVRLoaded) return;
        if (loadScene != OWScene.SolarSystem) return;

        // The texts are recreated with the ship, so drop the stale refs before re-registering.
        managedTexts.Clear();

        // NomaiVR creates the texts in its module Start(), which runs after OnCompleteSceneLoad, so we continuously
        // retry until they exist.
        MainThreadDispatcher.Enqueue(() => TryRegisterAndApply(0));
    }

    private static void TryRegisterAndApply(int attempt)
    {
        if (!VRCompatibility.IsNomaiVRLoaded) return;

        // Register and apply each text as we find it. The three interact receivers are created together in
        // ShipBody.Start's postfix.
        foreach (var text in Object.FindObjectsOfType<Text>())
        {
            if (!textObjectNames.TryGetValue(text.gameObject.name, out var tool)) continue;
            if (managedTexts.ContainsKey(tool)) continue; // keep the pristine first capture

            managedTexts[tool] = new ManagedText { text = text, originalText = text.text, originalColor = text.color };
            ApplyText(tool);
        }

        if (managedTexts.Count >= textObjectNames.Count) return;

        if (attempt < MaxSweepAttempts)
            MainThreadDispatcher.Enqueue(() => TryRegisterAndApply(attempt + 1));
        else
            APRandomizer.OWMLModConsole.WriteLine(
                $"Could not find all NomaiVR ship monitor texts (found {managedTexts.Count}/{textObjectNames.Count}); " +
                "the VR \"not available\" tool prompts may be incomplete.",
                OWML.Common.MessageType.Warning);
    }

    /// <summary>Updates the monitor text for the given item.</summary>
    public static void ApplyManagedTextForItem(Item item)
    {
        if (item is Item.Signalscope or Item.Scout or Item.LandingCamera)
        {
            ApplyText(item);
        }
    }

    private static void ApplyText(Item tool)
    {
        if (!managedTexts.TryGetValue(tool, out var managed) || managed.text == null) return;

        if (IsToolOwned(tool))
        {
            managed.text.text = managed.originalText;
            managed.text.color = managed.originalColor;
        }
        else
        {
            // Keep NomaiVR's "<color=grey>TOOL NAME</color>" header (first line), swap the action line for
            // "not available". The inline grey overrides Text.color, so only "not available" takes the red tint.
            var header = managed.originalText.Split('\n')[0];
            managed.text.text = $"{header}\n\nnot available";
            managed.text.color = UnavailableColor;
        }
    }
}
