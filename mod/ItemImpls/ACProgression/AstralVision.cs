using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArchipelagoRandomizer;

internal class AstralVision
{
    private static bool enabled = true;
    private static bool _hasAstralVision = false;

    public static bool hasAstralVision
    {
        get => _hasAstralVision;
        set
        {
            if (_hasAstralVision != value && enabled)
            {
                _hasAstralVision = value;

                PlayerData.SetPersistentCondition("CODEX_ENTERED_TESSERACT", value);
                ChangeTesseractState(value);
                if (_hasAstralVision)
                    TryShowAstralVisionAnimation();
            }
        }
    }

    private static bool conditionSetInSession = false;
    public static void OnSolarSystemLoadedEvent()
    {
        if (!enabled)
            return;
        // Set persistent condition first time we are loading up the game, as a failsafe if the player gets the item while not online or if they disable the vision via tesseract and quit.
        // If the player doesn't have launch codes they could softlock themselves out of the vision.
        if (hasAstralVision && !conditionSetInSession)
        {
            PlayerData.SetPersistentCondition("CODEX_ENTERED_TESSERACT", hasAstralVision);
            conditionSetInSession = true;
        }
        // Hide the tesseract.
        if (!hasAstralVision)
            ChangeTesseractState(false);
    }

    private static void ChangeTesseractState(bool enabled)
    {
        GameObject.Find("LingeringChime_Body/Sector/Station/Tesseract")?.GetComponent<BoxShape>()?.enabled = enabled;
        var points = GameObject.Find("LingeringChime_Body/Sector/Station/Tesseract/Points");
        if (points != null)
        {
            points.GetComponent<ParticleSystemRenderer>().enabled = enabled;
            foreach (var point in points.GetComponentsInChildren<LineRenderer>())
                point.enabled = enabled;
        }
    }

    private static bool TryShowAstralVisionAnimation()
    {
        if (SceneManager.GetActiveScene().name != "SolarSystem")
        {
            APRandomizer.OWMLModConsole.WriteLine($"Failed to show Astral Vision animation, not in base solar system.");
            return false;
        }
        var tesseractType = Type.GetType("AstralCodex.TesseractInteraction, AstralCodex");
        if (tesseractType == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Failed to show Astral Vision animation, TesseractInteraction type not found.", OWML.Common.MessageType.Warning);
            return false;
        }
        var enteredTesseractMethod = tesseractType.GetMethod("EnteredTesseract", BindingFlags.NonPublic | BindingFlags.Instance);
        if (enteredTesseractMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Failed to show Astral Vision animation, EnteredTesseract method not found.", OWML.Common.MessageType.Warning);
            return false;
        }
        var tesseractInteraction = GameObject.FindObjectOfType(tesseractType);
        if (tesseractInteraction == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Failed to show Astral Vision animation, couldn't find TesseractInteraction object.", OWML.Common.MessageType.Warning);
            return false;
        }
        enteredTesseractMethod.Invoke(tesseractInteraction, [true, true]);
        return true;
    }

    public static void ApplySlotData(Version apworld_version)
    {
        enabled = apworld_version >= new Version(1, 4, 1);
    }
}
