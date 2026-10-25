using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArchipelagoRandomizer;

internal class AstralVision
{
    private static bool _hasAstralVision = false;

    public static bool hasAstralVision
    {
        get => _hasAstralVision;
        set
        {
            if (_hasAstralVision != value)
            {
                _hasAstralVision = value;

                if (_hasAstralVision)
                {
                    // The animation takes care of everything.
                    // If the animation can't be shown (mod not installed, not in base system),
                    // just set the appropriate condition.
                    if (!TryShowAstralVisionAnimation())
                        PlayerData.SetPersistentCondition("CODEX_ENTERED_TESSERACT", true);
                }
            }
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
}
