using System;
using UnityEngine;

namespace Utopia.Title
{
    // Mirrors Resources/Title/title_screen.json. Texts and the (temporary) loading time live in data.
    [Serializable]
    public class TitleScreenConfig
    {
        public string titleText = "유토피아";
        public string loadingText = "LOADING";
        public string tapText = "터치하면 시작";
        // TODO_ placeholder: fake loading until there is something real to load.
        public float loadingSeconds = 1f;
        public float tapBlinkSeconds = 0.55f;

        public static TitleScreenConfig Load()
        {
            var asset = Resources.Load<TextAsset>("Title/title_screen");
            return asset != null ? JsonUtility.FromJson<TitleScreenConfig>(asset.text) : new TitleScreenConfig();
        }
    }

    // Mirrors Resources/Title/title_layout.json, written by the tool that split the title painting into layers.
    // Positions are in source-image pixels with y pointing down; pivots are absolute image coordinates.
    [Serializable]
    public class TitleLayout
    {
        public int imageWidth;
        public int imageHeight;
        public Layer[] layers;
        public Spot[] sparkles;
        public Spot[] eyes;

        [Serializable]
        public class Layer
        {
            public string name;
            public string kind;
            public int x, y, width, height;
            public int pivotX, pivotY;
        }

        [Serializable]
        public class Spot
        {
            public int x, y, size;
        }

        public static TitleLayout Load()
        {
            var asset = Resources.Load<TextAsset>("Title/title_layout");
            if (asset == null)
            {
                Debug.LogError("Title layout not found at Resources/Title/title_layout.json");
                return new TitleLayout { layers = new Layer[0], sparkles = new Spot[0], eyes = new Spot[0] };
            }
            return JsonUtility.FromJson<TitleLayout>(asset.text);
        }
    }
}
