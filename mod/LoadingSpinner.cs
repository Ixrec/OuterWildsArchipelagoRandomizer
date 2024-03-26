using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class LoadingSpinner
{
    private static void drawCircle(Texture2D tex, Vector2Int center, int radius, Color color)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var distanceFromCenter = Math.Sqrt((x * x) + (y * y));
                if (distanceFromCenter < radius)
                {
                    tex.SetPixel(center.x + x, center.y + y, color);
                }
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SpinnerUI), nameof(SpinnerUI.Instantiate))]
    public static void SpinnerUI_Instantiate_Postfix()
    {
        var size = 512;
        var center = new Vector2Int(size / 2, size / 2);
        var spinnerRadius = 200;
        var pointRadius = 35;

        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "APRandomizer_LoadingSpinner";
        foreach (var x in Enumerable.Range(0, size))
            foreach (var y in Enumerable.Range(0, size))
                texture.SetPixel(x, y, Color.clear);

        // Used the eyedropper tool on https://github.com/ArchipelagoMW/Archipelago/blob/main/data/icon.png
        var apRed =    new Color(201 / 256f, 118 / 256f, 130 / 256f);
        var apGreen =  new Color(117 / 256f, 194 / 256f, 117 / 256f);
        var apPurple = new Color(202 / 256f, 148 / 256f, 194 / 256f);
        var apOrange = new Color(217 / 256f, 160 / 256f, 125 / 256f);
        var apBlue =   new Color(118 / 256f, 126 / 256f, 189 / 256f);
        var apYellow = new Color(238 / 256f, 227 / 256f, 145 / 256f);

        var angleToIntOffsets = (int degrees) => new Vector2Int(
            (int)Math.Round(spinnerRadius * Math.Cos(Mathf.Deg2Rad * degrees)),
            (int)Math.Round(spinnerRadius * Math.Sin(Mathf.Deg2Rad * degrees))
        );
        drawCircle(texture, center + angleToIntOffsets(90),   pointRadius, apRed);
        drawCircle(texture, center + angleToIntOffsets(30),   pointRadius, apGreen);
        drawCircle(texture, center + angleToIntOffsets(-30),  pointRadius, apPurple);
        drawCircle(texture, center + angleToIntOffsets(-90),  pointRadius, apOrange);
        drawCircle(texture, center + angleToIntOffsets(-150), pointRadius, apBlue);
        drawCircle(texture, center + angleToIntOffsets(150),  pointRadius, apYellow);
        texture.Apply();

        var spinnerImage = SpinnerUI.s_instance._spinnerTransform.GetComponent<UnityEngine.UI.Image>();
        spinnerImage.sprite = Sprite.Create(
            texture,
            new Rect(0.0f, 0.0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100.0f
        );
    }

    // Keeps the spinner visible indefinitely once shown. Useful for testing.
    /*[HarmonyPrefix, HarmonyPatch(typeof(SpinnerUI), nameof(SpinnerUI.Hide))]
    public static bool SpinnerUI_Hide_Prefix()
    {
        APRandomizer.OWMLModConsole.WriteLine($"skipping SpinnerUI.Hide() call");
        return false;
    }*/
}
