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
    public static Sprite CreateCoordinatesSprite(int width, int height, List<HexagonalCoordinate> coordinates, Color bgColor)
    {
        var texture = drawCoordinates(width, height, Color.white, bgColor, coordinates);

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    // This coordinate drawing code is similar to and often borrows directly from:
    // https://github.com/PacificEngine/OW_CommonResources 's EyeCoordinates.cs and Shapes2D.cs
    // https://github.com/Outer-Wilds-New-Horizons/new-horizons/ 's VesselCoordinatePromptHandler.cs

    private static Texture2D drawCoordinates(int width, int textureHeight, Color foreground, Color background, List<HexagonalCoordinate> coordinates)
    {
        var tex = new Texture2D(width, textureHeight, TextureFormat.ARGB32, false);
        foreach (var x in Enumerable.Range(0, width))
            foreach (var y in Enumerable.Range(0, textureHeight))
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

            var center = new Vector2Int(totalXOffset + hexagonOffset, textureHeight / 2);
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

    // The following methods are for drawing 3D coordinate models, based on:
    // https://github.com/PacificEngine/OW_CommonResources 's EyeCoordinates.cs, Shapes3D.cs and Geometry/*.cs
    // Because this code is only used on the PTM hologram, yet is much longer (~500 LoC) and more complex than any other single piece of this mod,
    // it's just not worth the effort to rewrite all this to be optimal for my use case. So most of it is simply copy-pasted.

    public static Shapes3D getCoordinatesModel(List<HexagonalCoordinate> coordinates)
    {
        // Most of my code counts the hexagon points from 0 = Right counterclockwise.
        // This 3D drawing code counts them from 0 = UpperLeft clockwise (to match the base game's code?).
        // Ideally I'd rewrite the code to use the same numbers, but for now it's not worth the time/effort/risk, so instead we convert.
        List<int[]> adjustedCoordinates = coordinates.Select(hexCoord =>
        {
            return hexCoord.Select(coordPoint =>
            {
                switch (coordPoint)
                {
                    case CoordinatePoint.Right:      return 2;
                    case CoordinatePoint.UpperRight: return 1;
                    case CoordinatePoint.UpperLeft:  return 0;
                    case CoordinatePoint.Left:       return 5;
                    case CoordinatePoint.LowerLeft:  return 4;
                    default:          /*LowerRight*/ return 3;
                }
            }).ToArray();
        }).ToList();

        var firstCoord = getCoordinate3D(adjustedCoordinates[0]);
        var secondCoord = getCoordinate3D(adjustedCoordinates[1]);
        var thirdCoord = getCoordinate3D(adjustedCoordinates[2]);

        var model = new Shapes3D();
        drawCoordinate3D(ref model, ref firstCoord, 1.25f, 0.25f);
        drawCoordinate3D(ref model, ref secondCoord, -1.25f, 0.25f);
        drawCoordinate3D(ref model, ref thirdCoord, -3.75f, 0.25f);
        return model;
    }

    public static Shapes3D getQuestionMarksModel()
    {
        var model = new Shapes3D();

        Vector3[] questionMarkConnectedVertices = [
            new Vector3(-0.5f, 0f, -1.5f),
            new Vector3(-1f, 0f, -2f),
            new Vector3(-1.5f, 0f, -1.5f),
            new Vector3(-1f, 0f, -1f),
            new Vector3(-1f, 0f, -0.5f),
        ];
        drawCoordinate3D(ref model, ref questionMarkConnectedVertices, 1.25f, 0.25f);
        drawCoordinate3D(ref model, ref questionMarkConnectedVertices, -1.25f, 0.25f);
        drawCoordinate3D(ref model, ref questionMarkConnectedVertices, -3.75f, 0.25f);

        Vector3[] questionMarkDotVertices = [
            new Vector3(-1f, 0f, 0f)
        ];
        drawCoordinate3D(ref model, ref questionMarkDotVertices, 1.25f, 0.25f);
        drawCoordinate3D(ref model, ref questionMarkDotVertices, -1.25f, 0.25f);
        drawCoordinate3D(ref model, ref questionMarkDotVertices, -3.75f, 0.25f);

        return model;
    }

    private static void drawCoordinate3D(ref Shapes3D model, ref Vector3[] w, float xOffset, float width)
    {
        var multiplier = 0.2f;
        if (w.Length > 0)
        {
            var x = (w[0].x + xOffset) * multiplier;
            var y = (w[0].y) * multiplier;
            var z = (w[0].z) * multiplier;
            model.drawSphere(new Vector3(x, y, z), width * (multiplier / 2f), 8);
        }

        for (int i = 1; i < w.Length; i++)
        {
            var x1 = (w[i].x + xOffset) * multiplier;
            var y1 = (w[i].y) * multiplier;
            var z1 = (w[i].z) * multiplier;
            var x2 = (w[i - 1].x + xOffset) * multiplier;
            var y2 = (w[i - 1].y) * multiplier;
            var z2 = (w[i - 1].z) * multiplier;
            model.drawSphere(new Vector3(x1, y1, z1), width * (multiplier / 2f), 8);
            model.drawCylinder(new Vector3(x1, y1, z1), new Vector3(x2, y2, z2), width * (multiplier / 2f), width * (multiplier / 2f), 25);
        }
    }

    private static Vector3[] getCoordinate3D(int[] coordinate)
    {
        var vectors = new Vector3[coordinate.Length];
        for (int i = 0; i < coordinate.Length; i++)
        {
            var vector = getCoordinate2D(coordinate[i]);
            vectors[i] = new Vector3(-1 * vector.x, 0f, -1 * vector.y);
        }
        return vectors;
    }

    private static Vector2[] getCoordinate2D(int[] coordinate)
    {
        var vectors = new Vector2[coordinate.Length];
        for (int i = 0; i < coordinate.Length; i++)
        {
            vectors[i] = getCoordinate2D(coordinate[i]);
        }
        return vectors;
    }

    // In my 2D drawing code I use offsets from each hexagon's center
    // This getCoordinate2D used by the 3D modeling code appears to be calculating offsets from a Cartesian origin to the lower left of the hexagon
    private static Vector2 getCoordinate2D(int coordinate)
    {
        if (coordinate == 0)
        {
            return new Vector2(0.5f, 1.732f);
        }
        if (coordinate == 1)
        {
            return new Vector2(1.5f, 1.732f);
        }
        if (coordinate == 2)
        {
            return new Vector2(2f, 0.866f);
        }
        if (coordinate == 3)
        {
            return new Vector2(1.5f, 0f);
        }
        if (coordinate == 4)
        {
            return new Vector2(0.5f, 0f);
        }
        if (coordinate == 5)
        {
            return new Vector2(0f, 0.866f);
        }
        return new Vector2(1f, 0.866f);
    }

    public class Shapes3D
    {
        public Dictionary<Vector3, int> verticeIndex;
        public Dictionary<int, Vector3> indexVectice;
        public List<Vector3> _vertices;
        public List<int> _triangles;

        public Vector3[] vertices { get { return _vertices.ToArray(); } }
        public int[] triangles { get { return _triangles.ToArray(); } }

        public Shapes3D()
        {
            verticeIndex = new Dictionary<Vector3, int>();
            indexVectice = new Dictionary<int, Vector3>();
            _vertices = new List<Vector3>();
            _triangles = new List<int>();
        }
        public static float angle(Vector2 p1, Vector2 p2)
        {
            // / p1.magnitude;
            // / p2.magnitude;
            var value = Math.Atan2(p1.x * p2.y - p1.y * p2.x, p1.x * p2.x + p1.y * p2.y);
            if (double.IsNaN(value))
            {
                return 0f;
            }

            value = value % (2d * Math.PI);
            if (value > Math.PI)
            {
                return Mathf.Rad2Deg * (float)(Math.PI - value);
            }
            return Mathf.Rad2Deg * (float)value;
        }

        private static float angleX(Vector3 p1, Vector3 p2)
        {
            return angle(new Vector2(p1.y, p1.z), new Vector2(p2.y, p2.z));
        }

        private static float angleY(Vector3 p1, Vector3 p2)
        {
            return angle(new Vector2(p1.x, p1.z), new Vector2(p2.x, p2.z));
        }

        private static float angleZ(Vector3 p1, Vector3 p2)
        {
            return angle(new Vector2(p1.x, p1.y), new Vector2(p2.x, p2.y));
        }

        public static Vector3 angleXYZ(Vector3 p1, Vector3 p2)
        {
            return new Vector3(angleX(p1, p2), angleY(p1, p2), angleZ(p1, p2));
        }
        private Vector3 getRotation(Vector3 start, Vector3 end)
        {
            bool flipX = false;
            bool flipY = false;
            bool flipZ = false;

            var difference = end - start;
            if (difference.z > 0f)
            {
                return getRotation(end, start);
            }

            if (difference.x <= 0f && difference.y <= 0f)
            {
                flipX = true;
                flipY = true;
                difference = new Vector3(-1f * difference.x, -1f * difference.y, difference.z);
            }

            var angles = angleXYZ(difference, Vector3.forward);
            if (difference.x != 0f && difference.z == 0f)
            {
                angles = angles + angleXYZ(difference, Vector3.right);
            }

            if (flipX)
            {
                difference = new Vector3(-1f * difference.x, difference.y, difference.z);
                angles = new Vector3(-1f * angles.x, angles.y, angles.z);
            }
            if (flipY)
            {
                difference = new Vector3(difference.x, -1f * difference.y, difference.z);
                angles = new Vector3(angles.x, -1f * angles.y, angles.z);
            }
            if (flipZ)
            {
                difference = new Vector3(difference.x, difference.y, -1f * difference.z);
                angles = new Vector3(angles.x, angles.y, -1f * angles.z);
            }

            return angles;

        }
        private static Vector3 rotatePointX(ref Vector3 point, float xRotation)
        {
            var sin = (float)Math.Sin(xRotation);
            var cos = (float)Math.Cos(xRotation);
            return new Vector3(point.x, point.y * cos - point.z * sin, point.y * sin + point.z * cos);
        }

        private static Vector3 rotatePointY(ref Vector3 point, float yRotation)
        {
            var sin = (float)Math.Sin(yRotation);
            var cos = (float)Math.Cos(yRotation);
            return new Vector3(point.x * cos + point.z * sin, point.y, -1 * point.x * sin + point.z * cos);
        }

        private static Vector3 rotatePointZ(ref Vector3 point, float zRotation)
        {
            var sin = (float)Math.Sin(zRotation);
            var cos = (float)Math.Cos(zRotation);
            return new Vector3(point.x * cos - point.y * sin, point.x * sin + point.y * cos, point.z);
        }

        public static Vector3 rotatePoint(Vector3 point, Vector3 rotation)
        {
            return rotatePoint(ref point, ref rotation);
        }

        public static Vector3 rotatePoint(ref Vector3 point, ref Vector3 rotation)
        {
            var rotate = point;
            rotate = rotatePointX(ref rotate, Mathf.Deg2Rad * rotation.x);
            rotate = rotatePointY(ref rotate, Mathf.Deg2Rad * rotation.y);
            rotate = rotatePointZ(ref rotate, Mathf.Deg2Rad * rotation.z);
            return rotate;
        }
        private Vector3 calculateRotation(Vector3 point, float rotation, Vector3 angle)
        {
            return rotatePoint(rotatePoint(point, Vector3.forward * 0), angle);
        }

        public static Vector2 getPointOnCircle(ref Vector2 center, float radius, float arcAngle)
        {
            arcAngle = Mathf.Deg2Rad * arcAngle;
            return new Vector2(radius * (float)Math.Cos(arcAngle) + center.x, radius * (float)Math.Sin(arcAngle) + center.y);
        }
        public void drawCylinder(Vector3 start, Vector3 end, float startRadius, float endRadius, int sides)
        {
            var difference = end - start;
            if (difference.z > 0f)
            {
                drawCylinder(end, start, endRadius, startRadius, sides);
                return;
            }
            bool normal = !(difference.y == 0f && difference.z == 0f) && !(difference.x < 0f && difference.y > 0f && difference.z == 0f);

            Vector3 angles = getRotation(start, end);

            var zero = Vector2.zero;

            int top = addVertex(start);
            int bottom = addVertex(end);
            int[] topVertices = new int[sides];
            int[] bottomVertices = new int[sides];

            var vertex = getPointOnCircle(ref zero, startRadius, 0);
            topVertices[0] = addVertex(start + calculateRotation(new Vector3(vertex.x, vertex.y, 0f), 0, angles));

            vertex = getPointOnCircle(ref zero, endRadius, 0);
            bottomVertices[0] = addVertex(end + calculateRotation(new Vector3(vertex.x, vertex.y, 0f), 0, angles));

            var increment = 360f / (float)sides;
            for (int i = 1; i < sides; i++)
            {
                vertex = getPointOnCircle(ref zero, startRadius, increment * (float)i);
                topVertices[i] = addVertex(start + calculateRotation(new Vector3(vertex.x, vertex.y, 0f), 0, angles));

                vertex = getPointOnCircle(ref zero, endRadius, increment * (float)i);
                bottomVertices[i] = addVertex(end + calculateRotation(new Vector3(vertex.x, vertex.y, 0f), 0, angles));

                addTriangle(topVertices[i], topVertices[i - 1], top, !normal);
                addTriangle(bottomVertices[i], bottomVertices[i - 1], bottom, normal);
                addRectangle(topVertices[i], bottomVertices[i], bottomVertices[i - 1], topVertices[i - 1], !normal);
            }

            addTriangle(topVertices[0], topVertices[sides - 1], top, !normal);
            addTriangle(bottomVertices[0], bottomVertices[sides - 1], bottom, normal);
            addRectangle(topVertices[0], bottomVertices[0], bottomVertices[sides - 1], topVertices[sides - 1], !normal);
        }

        public void drawSphere(Vector3 center, float radius, int levels)
        {
            // TODO: Make this code draw a sphere better
            var root3 = (float)Math.Sqrt(3f);
            var latIncrement = 360f / (levels * 2);
            var chordLength = ((float)Math.Sin((Mathf.Deg2Rad * latIncrement) / 2f)) * radius * 2f;
            var length = chordLength * root3 / 2f;
            var height = radius - (float)Math.Sqrt(radius * radius - 0.25f * chordLength * chordLength);
            var topLatitude = (float)(180f) - (height / radius);
            var bottomLattiude = (float)180f - topLatitude;

            var totalLevels = (int)Math.Ceiling((topLatitude / latIncrement));
            var topTeirs = new Vector3[totalLevels / 2][];
            var bottomTeirs = new Vector3[totalLevels / 2][];
            var isTopRotated = totalLevels % 2 == 1;
            var isBottomRotated = false;
            topTeirs[0] = makeASphereTierViaAngle(ref center, topLatitude, radius, 120f, isTopRotated);
            bottomTeirs[0] = makeASphereTierViaAngle(ref center, bottomLattiude, radius, 120f, isBottomRotated);

            addTriangle(ref topTeirs[0][0], ref topTeirs[0][1], ref topTeirs[0][2], false);
            addTriangle(ref bottomTeirs[0][0], ref bottomTeirs[0][1], ref bottomTeirs[0][2], true);

            var level = 1;
            for (level = 1; level < topTeirs.Length; level++)
            {
                isTopRotated = !isTopRotated;
                isBottomRotated = !isBottomRotated;

                topTeirs[level] = makeASphereTierViaLength(ref center, topLatitude - (float)level * latIncrement, radius, length, isTopRotated);
                bottomTeirs[level] = makeASphereTierViaLength(ref center, bottomLattiude + (float)level * latIncrement, radius, length, isBottomRotated);

                connectTwoSphereTeirs(ref topTeirs[level], ref topTeirs[level - 1], false);
                connectTwoSphereTeirs(ref bottomTeirs[level], ref bottomTeirs[level - 1], true);
            }

            var currentTier = topTeirs[topTeirs.Length - 1];
            var lastTier = bottomTeirs[bottomTeirs.Length - 1];
            if (totalLevels % 2 == 1)
            {
                currentTier = makeASphereTierViaLength(ref center, 90f, radius, length, false);

                connectTwoSphereTeirs(ref currentTier, ref topTeirs[topTeirs.Length - 1], false);
            }

            connectTwoSphereTeirs(ref currentTier, ref lastTier, true);
        }
        private static float normalizeLatitude(float latitude)
        {
            return latitude > Math.PI ? ((float)Math.PI - (latitude - (float)Math.PI)) : latitude;
        }
        public static float getRadiusOnSphere(float latitude, float radius)
        {
            var percentage = (float)Math.Sin(normalizeLatitude(Mathf.Deg2Rad * latitude) - ((float)Math.PI / 2f));
            return (float)Math.Sqrt(radius * radius - radius * radius * percentage * percentage);
        }

        public static Vector3 getPointOnSphere(ref Vector3 center, float longitude, float latitude, float radius)
        {
            longitude = (Mathf.Deg2Rad * longitude) - (float)Math.PI;
            latitude = normalizeLatitude(Mathf.Deg2Rad * latitude) - ((float)Math.PI / 2f);
            return new Vector3((-1f * radius * (float)Math.Cos(latitude) * (float)Math.Sin(longitude)) + center.x, (-1f * radius * (float)Math.Cos(latitude) * (float)Math.Cos(longitude)) + center.y, (radius * (float)Math.Sin(latitude)) + center.z);
        }
        private Vector3[] makeASphereTierViaLength(ref Vector3 center, float latitude, float radius, float length, bool isRotated)
        {
            var rad = getRadiusOnSphere(latitude, radius);
            var arcAngle = Mathf.Rad2Deg * ((float)Math.Asin(length / (2f * rad)) * 2f);

            return makeASphereTierViaAngle(ref center, latitude, radius, arcAngle, isRotated);
        }

        private Vector3[] makeASphereTierViaAngle(ref Vector3 center, float latitude, float radius, float arcAngle, bool isRotated)
        {
            var count = (int)Math.Ceiling((2f * (float)Math.PI) / (Mathf.Deg2Rad * arcAngle));
            arcAngle = 360f / count;

            var rotation = isRotated ? (arcAngle / 2f) : 0f;
            var currentTier = new Vector3[count];
            for (int j = 0; j < count; j++)
            {
                currentTier[j] = getPointOnSphere(ref center, ((float)j * arcAngle + rotation) - 180f, latitude, radius);
            }

            return currentTier;
        }

        private void connectTwoSphereTeirs(ref Vector3[] currentTier, ref Vector3[] lastTier, bool normal)
        {
            int lastIndex = 0;
            float distance = float.MaxValue;
            for (int i = 0; i < lastTier.Length; i++)
            {
                var d2 = (currentTier[0] - lastTier[0]).sqrMagnitude;
                if (d2 < distance)
                {
                    lastIndex = i;
                    distance = d2;
                }
            }
            int firstIndex = lastIndex;


            for (int j = 1; j < currentTier.Length; j++)
            {
                int nextIndex = (lastIndex + 1) % lastTier.Length;
                if ((currentTier[j] - lastTier[lastIndex]).sqrMagnitude > (currentTier[j] - lastTier[nextIndex]).sqrMagnitude)
                {
                    addTriangle(ref currentTier[j], ref lastTier[nextIndex], ref lastTier[lastIndex], normal);
                    addTriangle(ref currentTier[j], ref currentTier[j - 1], ref lastTier[lastIndex], !normal);
                    lastIndex = nextIndex;
                }
                else
                {
                    addTriangle(ref currentTier[j], ref currentTier[j - 1], ref lastTier[lastIndex], !normal);
                }
            }

            if (firstIndex != lastIndex)
            {
                addTriangle(ref currentTier[0], ref lastTier[firstIndex], ref lastTier[lastIndex], normal);
                addTriangle(ref currentTier[0], ref currentTier[currentTier.Length - 1], ref lastTier[lastIndex], !normal);
            }
            else
            {
                addTriangle(ref currentTier[0], ref currentTier[currentTier.Length - 1], ref lastTier[lastIndex], !normal);
            }
        }
        private int addVertex(Vector3 vertex)
        {
            return addVertex(ref vertex);
        }

        private int addVertex(ref Vector3 vertex)
        {
            int value = 0;
            if (verticeIndex.TryGetValue(vertex, out value))
            {
                return value;
            }
            else
            {
                _vertices.Add(vertex);
                indexVectice.Add(_vertices.Count - 1, vertex);
                verticeIndex.Add(vertex, _vertices.Count - 1);
                return _vertices.Count - 1;
            }
        }

        private void addRectangle(int v1, int v2, int v3, int v4, bool clockwise)
        {
            addTriangle(v1, v2, v3, clockwise);
            addTriangle(v1, v3, v4, clockwise);
        }

        private void addTriangle(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3, bool clockwise)
        {
            addTriangle(addVertex(ref p1), addVertex(ref p2), addVertex(ref p3), clockwise);
        }

        private void addTriangle(int v1, int v2, int v3, bool clockwise)
        {
            if (clockwise)
            {
                _triangles.Add(v1);
                _triangles.Add(v2);
                _triangles.Add(v3);
            }
            else
            {
                _triangles.Add(v3);
                _triangles.Add(v2);
                _triangles.Add(v1);
            }
        }
    }
}
