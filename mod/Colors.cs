using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Colors
{
    public static void OnCompleteSceneLoad(OWScene scene, OWScene loadScene)
    {
        if (loadScene == OWScene.SolarSystem || loadScene == OWScene.EyeOfTheUniverse)
        {
            RandomizeFlashlightColor();
        }
    }

    private static void RandomizeFlashlightColor()
    {
        var flashlightSpotLight = GameObject.Find("Player_Body/PlayerCamera/FlashlightRoot/Flashlight_BasePivot/Flashlight_WobblePivot/Flashlight_SpotLight");
        var light = flashlightSpotLight.gameObject.GetComponent<Light>();
        var oldColor = light.color;
        var oldAlpha = oldColor.a;
        // Allow any hue and saturation, but keep "value" (darkness/lightness) well above 0 since a near-black flashlight isn't helpful
        var newColor = Random.ColorHSV(0f, 1f, 0f, 1f, 0.5f, 1f, oldAlpha, oldAlpha);
        APRandomizer.OWMLModConsole.WriteLine($"RandomizeFlashlightColor changing flashlight from {light.color} to {newColor}");
        light.color = newColor;
    }
}
