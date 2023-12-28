using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer;

using HexagonalCoordinate = List<CoordinatePoint>;

enum CoordinatePoint
{
    UpperLeft,
    UpperRight,
    Left,
    Right,
    LowerLeft,
    LowerRight,
};

[HarmonyPatch]
public static class Coordinates
{
    private static bool _hasCoordinates = false;

    public static bool hasCoordinates
    {
        get => _hasCoordinates;
        set
        {
            if (_hasCoordinates != value)
            {
                _hasCoordinates = value;
            }
        }
    }

    private static List<HexagonalCoordinate> vanillaEOTUCoordinates = new List<HexagonalCoordinate>
    {
        new HexagonalCoordinate {
            CoordinatePoint.LowerLeft,
            CoordinatePoint.Left,
            CoordinatePoint.UpperRight
        },
        new HexagonalCoordinate {
            CoordinatePoint.LowerLeft,
            CoordinatePoint.UpperRight,
            CoordinatePoint.UpperLeft,
            CoordinatePoint.LowerRight
        },
        new HexagonalCoordinate {
            CoordinatePoint.LowerLeft,
            CoordinatePoint.Left,
            CoordinatePoint.UpperLeft,
            CoordinatePoint.LowerRight,
            CoordinatePoint.Right,
            CoordinatePoint.UpperRight
        },
    };

    // Taken from The Vision mod, so I have a second set of coordinates to test the drawing code with.
    /*private static List<HexagonalCoordinate> gloamingGalaxyCoordinates = new List<HexagonalCoordinate>
    {
        new HexagonalCoordinate {
            CoordinatePoint.Left,
            CoordinatePoint.UpperLeft,
            CoordinatePoint.UpperRight,
            CoordinatePoint.Right,
            CoordinatePoint.LowerLeft
        },
        new HexagonalCoordinate {
            CoordinatePoint.LowerLeft,
            CoordinatePoint.UpperLeft,
            CoordinatePoint.Right,
            CoordinatePoint.LowerRight,
            CoordinatePoint.Left
        },
        new HexagonalCoordinate {
            CoordinatePoint.UpperRight,
            CoordinatePoint.LowerLeft,
            CoordinatePoint.UpperLeft,
            CoordinatePoint.Left,
            CoordinatePoint.Right
        },
    };*/

    private static ScreenPrompt showCoordinatesPrompt = new(InputLibrary.lockOn, "", 0);
    private static ScreenPrompt requiresCoordinatesPrompt = new("Requires Coordinates", 0);
    private static NomaiCoordinateInterface nomaiInterfaceReference = null;
    private static Image coordinatesImage = null;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NomaiCoordinateInterface), nameof(NomaiCoordinateInterface.Awake))]
    public static void NomaiCoordinateInterface_Awake(NomaiCoordinateInterface __instance)
    {
        nomaiInterfaceReference = __instance;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Start))]
    public static void ToolModeUI_Start_Postfix(ToolModeUI __instance)
    {
        Locator.GetPromptManager().AddScreenPrompt(showCoordinatesPrompt, PromptPosition.LowerLeft, false);
        Locator.GetPromptManager().AddScreenPrompt(requiresCoordinatesPrompt, PromptPosition.LowerLeft, false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix(ToolModeUI __instance)
    {
        if (
            Locator.GetPlayerSectorDetector().InVesselDimension() &&
            OWInput.IsInputMode(InputMode.Character) &&
            Locator.GetPlayerSuit().IsWearingHelmet() // since we display the coordinates on the helmet HUD
        ) {
            if (hasCoordinates)
            {
                requiresCoordinatesPrompt.SetVisibility(false);

                showCoordinatesPrompt.SetText((coordinatesImage?.enabled ?? false) ? "Hide Eye Coordinates" : "Show Eye Coordinates");
                showCoordinatesPrompt.SetVisibility(true);

                if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.lockOn))
                {
                    if (coordinatesImage is null)
                        CreateCoordinatesImage();
                    else
                        coordinatesImage.enabled = !coordinatesImage.enabled;
                }
            }
            else
            {
                showCoordinatesPrompt.SetVisibility(false);

                // Don't annoy the player with 'Requires Coordinates' unless for some reason
                // they raise the coordinate input pillar before they get the coordinates.
                requiresCoordinatesPrompt.SetVisibility(nomaiInterfaceReference._pillarRaised);
            }
        }
        else
        {
            showCoordinatesPrompt.SetVisibility(false);
            requiresCoordinatesPrompt.SetVisibility(false);
            if (coordinatesImage is not null && !Locator.GetPlayerSectorDetector().InVesselDimension())
                coordinatesImage.enabled = false;
        }
    }

    public static void CreateCoordinatesImage()
    {
        // When your helmet's off there is no single canvas, but a small collection of canvases
        // for the various reasons you might have helmet-less HUD elements. One example is:
        //var drawBase = hud.transform.Find("HelmetOffUI/SignalscopeCanvas");
        // This is for future reference if/when we finally need to draw the Prisoner vault's
        // datamined codes on screen in the DLC, since you have no suit there.

        var drawBase = GameObject.Find("PlayerHUD").transform.Find("HelmetOnUI/UICanvas");
        GameObject go = new GameObject("APRandomizer_CoordinatesDisplay");
        go.transform.SetParent(drawBase.transform, false);

        var width = 1200;
        var height = 400;
        var texture = drawCoordinates(width, height, Color.white, Color.black, vanillaEOTUCoordinates);

        coordinatesImage = go.AddComponent<Image>();
        coordinatesImage.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -400);
        // must have same *aspect ratio* as the texture to prevent stretching/squishing, but the "magnitude" is independent
        rt.sizeDelta = new Vector2(400, 133);
    }

    // This coordinate drawing code is similar to and often borrows directly from:
    // https://github.com/PacificEngine/OW_CommonResources 's EyeCoordinates.cs and Shapes2D.cs
    // https://github.com/Outer-Wilds-New-Horizons/new-horizons/ 's VesselCoordinatePromptHandler.cs

    private static Texture2D drawCoordinates(int width, int height, Color foreground, Color background, List<HexagonalCoordinate> coordinates)
    {
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        foreach (var x in Enumerable.Range(0, width))
            foreach (var y in Enumerable.Range(0, height))
                tex.SetPixel(x, y, background);

        // We always draw the coordinates horizontally, so an X by Y texture of N coords
        // will be split into chunks of width X/N and height Y.
        var widthPerHexagon = width / coordinates.Count;

        int hexagonRadius = (int)Math.Round(widthPerHexagon * 0.375); // = 3/8ths, to leave 1/8th on each side as spacing

        for (var i = 0; i < coordinates.Count; i++)
        {
            var center = new Vector2Int((widthPerHexagon * i) + (widthPerHexagon / 2), height / 2);
            drawCoordinate(tex, center, hexagonRadius, foreground, coordinates[i]);
        }

        tex.Apply();
        return tex;
    }

    private static void drawCoordinate(Texture2D tex, Vector2Int center, int hexagonRadius, Color color, HexagonalCoordinate coordinate)
    {
        var pointOffsets = coordinate.Select(coordinatePoint => coordinatePointToIntOffsets(hexagonRadius, coordinatePoint));
        Vector2Int[] points = pointOffsets.Select(offset => center + offset).ToArray();

        var thickness = hexagonRadius / 10;

        points.Do(point => drawCircle(tex, point, thickness, color));

        for (var startIndex = 0; startIndex < (points.Length - 1); startIndex++)
        {
            var startPoint = points[startIndex];
            var endPoint = points[startIndex + 1];

            drawLine(tex, startPoint, endPoint, thickness, color);
        }
    }

    private static Vector2Int coordinatePointToIntOffsets(int hexagonRadius, CoordinatePoint coordinatePoint)
    {
        int angle;
        switch (coordinatePoint)
        {
            case CoordinatePoint.Right:      angle = 0;   break;
            case CoordinatePoint.UpperRight: angle = 60;  break;
            case CoordinatePoint.UpperLeft:  angle = 120; break;
            case CoordinatePoint.Left:       angle = 180; break;
            case CoordinatePoint.LowerLeft:  angle = 240; break;
            default:          /*LowerRight*/ angle = 300; break;
        }

        return new Vector2Int(
            (int)Math.Round(hexagonRadius * Math.Cos(Mathf.Deg2Rad * angle)),
            (int)Math.Round(hexagonRadius * Math.Sin(Mathf.Deg2Rad * angle))
        );
    }

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

    private static void drawLine(Texture2D texture, Vector2Int startPoint, Vector2Int endPoint, int thickness, Color color)
    {
        int x0 = Mathf.FloorToInt(Mathf.Min(startPoint.x, endPoint.x) - thickness * 2f);
        int y0 = Mathf.FloorToInt(Mathf.Min(startPoint.y, endPoint.y) - thickness * 2f);
        int x1 = Mathf.CeilToInt(Mathf.Max(startPoint.x, endPoint.x) + thickness * 2f);
        int y1 = Mathf.CeilToInt(Mathf.Max(startPoint.y, endPoint.y) + thickness * 2f);

        Vector2 dir = endPoint - startPoint;
        float length = dir.magnitude;
        dir.Normalize();

        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                Vector2 p = new Vector2(x, y);
                float dot = Vector2.Dot(p - startPoint, dir);
                dot = Mathf.Clamp(dot, 0f, length);
                Vector2 pointOnLine = startPoint + dir * dot;
                float distToLine = Mathf.Max(0f, Vector2.Distance(p, pointOnLine) - thickness);
                if (distToLine <= 1f)
                {
                    // Line is within 1 pixel, fill with color (with anti-aliased blending)
                    float blend = 1f - Mathf.Clamp01(distToLine);

                    if (color.a * blend < 1f)
                    {
                        Color existing = texture.GetPixel(x, y);
                        if (existing.a > 0f)
                        {
                            float colorA = color.a;
                            color.a = 1f;
                            texture.SetPixel(x, y, Color.Lerp(existing, color, Mathf.Clamp01(colorA * blend)));
                        }
                        else
                        {
                            color.a *= blend;
                            texture.SetPixel(x, y, color);
                        }
                    }
                    else
                    {
                        color.a *= blend;
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }
}
