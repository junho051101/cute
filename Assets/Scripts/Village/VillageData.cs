using System;
using UnityEngine;

namespace Hamster.Village
{
    // Mirrors Resources/data/village.json. Layout and text live in data, not code;
    // every road, tree and flower bed is placed by hand there.
    [Serializable]
    public class VillageData
    {
        // 12x26 matches a portrait iPhone (19.5:9), so the fixed camera shows the whole village edge to edge.
        public int mapWidth = 12;
        public int mapHeight = 26;
        public int seed = 7;
        public Point playerStart;
        public float moveSpeed = 3.5f;
        public float interactRange = 1.2f;
        public Point fountain;
        public Area plaza;
        public PathData[] paths;
        public Area[] fields;
        public Area[] flowerBeds;
        public Prop[] trees;
        public Prop[] bushes;
        public BuildingData[] buildings;

        public static VillageData Load(string resourcePath = "data/village")
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"Village data not found at Resources/{resourcePath}.json");
                return new VillageData { buildings = new BuildingData[0] };
            }
            return JsonUtility.FromJson<VillageData>(asset.text);
        }
    }

    [Serializable]
    public class BuildingData
    {
        public string id;
        public string name;
        public string kind;
        public string classId;
        public Point position;
        // "cottage" (default) or "tower".
        public string style;
        public string roofColor;
        public string icon;
        public string description;
    }

    [Serializable]
    public class PathData
    {
        public float width = 1f;
        public Point[] points;
    }

    // Axis-aligned area given by its center and half size.
    [Serializable]
    public struct Area
    {
        public float x;
        public float y;
        public float halfWidth;
        public float halfHeight;

        public Rect ToRect() => new Rect(x - halfWidth, y - halfHeight, halfWidth * 2f, halfHeight * 2f);
    }

    [Serializable]
    public struct Prop
    {
        public float x;
        public float y;
        public int variant;

        public Vector2 ToVector2() => new Vector2(x, y);
    }

    [Serializable]
    public struct Point
    {
        public float x;
        public float y;

        public Vector2 ToVector2() => new Vector2(x, y);
    }
}
