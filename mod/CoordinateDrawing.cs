using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

using HexagonalCoordinate = List<CoordinateDrawing.CoordinatePoint>;

[HarmonyPatch]
public static class CoordinateDrawing
{
    // We commit to these specific numbers for the six possible coordinate points for a few reasons:
    // - randomly choosing coordinates naturally involves manipulating numbers
    // - the specific way I want to choose the coordinates does a lot of integer divisions and modulo operations,
    // so 0-indexing (or tedious conversions) is required anyway
    // - (de)serializing coordinates to/from slot_data is way more compact as [0,1,2] than as ["Right","UpperRight","UpperLeft"]
    // - drawing these points involves computing its angle from the hexagon's center, and the way the trigonometry works out,
    // choosing 0 for Right, 1 for UpperRight, etc allows us to simplify that to angle = 60 * (int)point
    public enum CoordinatePoint
    {
        Right = 0,
        UpperRight = 1,
        UpperLeft = 2,
        Left = 3,
        LowerLeft = 4,
        LowerRight = 5,
    };

    public static List<HexagonalCoordinate> vanillaEOTUCoordinates = new List<HexagonalCoordinate>
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
    public static List<HexagonalCoordinate> gloamingGalaxyCoordinates = new List<HexagonalCoordinate>
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
    };
    public static Sprite CreateCoordinatesSprite(List<HexagonalCoordinate> coordinates, Color bgColor)
    {
        var width = 600;
        var height = 200;

        var texture = drawCoordinates(width, height, Color.white, bgColor, coordinates);

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
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
        var maxWidthPerHexagon = width / coordinates.Count;

        int hexagonRadius = (int)Math.Round(maxWidthPerHexagon * 0.4);

        // how much we reduce the visual width of a coordinate if it doesn't use its
        // leftmost or rightmost point, as a fraction of the full hexagon width
        var kerningOffset = 0.20;

        int totalXOffset = 0;
        for (var i = 0; i < coordinates.Count; i++)
        {
            var coordinate = coordinates[i];

            var hexagonOffset = (maxWidthPerHexagon / 2);
            if (!coordinate.Contains(CoordinatePoint.Left)) hexagonOffset -= (int)(maxWidthPerHexagon * kerningOffset);

            var center = new Vector2Int(totalXOffset + hexagonOffset, height / 2);
            drawCoordinate(tex, center, hexagonRadius, foreground, background, coordinate);

            var hexagonWidth = maxWidthPerHexagon;
            if (!coordinate.Contains(CoordinatePoint.Left)) hexagonWidth -= (int)(maxWidthPerHexagon * kerningOffset);
            if (!coordinate.Contains(CoordinatePoint.Right)) hexagonWidth -= (int)(maxWidthPerHexagon * kerningOffset);
            totalXOffset += hexagonWidth;
        }

        tex.Apply();
        return tex;
    }

    private static void drawCoordinate(Texture2D tex, Vector2Int center, int hexagonRadius, Color color, Color backgroundColor, HexagonalCoordinate coordinate)
    {
        var pointOffsets = coordinate.Select(coordinatePoint => coordinatePointToIntOffsets(hexagonRadius, coordinatePoint));
        Vector2Int[] points = pointOffsets.Select(offset => center + offset).ToArray();

        var thickness = hexagonRadius / 15;

        points.Do(point => drawCircle(tex, point, thickness, color));

        for (var startIndex = 0; startIndex < (points.Length - 1); startIndex++)
        {
            var startPoint = points[startIndex];
            var endPoint = points[startIndex + 1];

            drawLine(tex, startPoint, endPoint, thickness, color, backgroundColor);
        }
    }

    private static Vector2Int coordinatePointToIntOffsets(int hexagonRadius, CoordinatePoint coordinatePoint)
    {
        int angle = (int)coordinatePoint * 60; // the integer values of CoordinatePoint were chosen to make this work

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

    private static void drawLine(Texture2D texture, Vector2Int startPoint, Vector2Int endPoint, int thickness, Color color, Color backgroundColor)
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
                    texture.SetPixel(x, y, color);
                else if (distToLine <= 5 && texture.GetPixel(x, y) == backgroundColor)
                    texture.SetPixel(x, y, Color.Lerp(color, backgroundColor, distToLine / 5));
            }
        }
    }
}
