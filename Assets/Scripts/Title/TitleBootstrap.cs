using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Utopia.UI;

namespace Utopia.Title
{
    // Builds the title / loading screen at runtime from Resources/Title, so the scene only needs this component.
    // The painting is split into layers (see title_layout.json) so the cat, hamster, scarf, flowers and
    // sparkles can each move a little. Flow: fake loading bar -> "tap to start" blinking -> tap.
    public class TitleBootstrap : MonoBehaviour
    {
        // World units per source-image pixel: 100 image pixels = 1 unit.
        const float PixelsPerUnit = 100f;
        // The painting's own pixel grid is about 6 image pixels per art pixel.
        const float ArtPixel = 6f;

        static readonly Color32 Plum = new Color32(43, 24, 56, 255);
        static readonly Color32 CatFur = new Color32(102, 93, 110, 255);
        static readonly Color32 EyeLine = new Color32(60, 5, 52, 255);
        static readonly Color32 Cream = new Color32(255, 250, 238, 255);

        TitleScreenConfig config;
        TitleLayout layout;
        Camera cam;
        Vector2Int fittedScreen;

        Font font;
        Font boldFont;
        GameObject loadingGroup;
        RectTransform barFill;
        Text loadingLabel;
        Text tapLabel;
        bool ready;
        bool started;

        void Awake()
        {
            // iOS and Android default to 30fps.
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            config = TitleScreenConfig.Load();
            layout = TitleLayout.Load();

            BuildCamera();
            BuildLight();
            BuildArt();
            BuildUI();
            StartCoroutine(Loading());
        }

        void Update()
        {
            if (Screen.width != fittedScreen.x || Screen.height != fittedScreen.y) FitCamera();

            if (!ready || started) return;
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame) StartCoroutine(StartGame());
        }

        // ---------- World ----------

        void BuildCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(197, 180, 250, 255);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            FitCamera();
        }

        // Cover-fit: the painting always fills the screen; taller phones crop the sides, tablets crop top and bottom.
        void FitCamera()
        {
            fittedScreen = new Vector2Int(Screen.width, Screen.height);
            if (cam == null || layout == null || Screen.height == 0) return;
            float w = layout.imageWidth / PixelsPerUnit, h = layout.imageHeight / PixelsPerUnit;
            float aspect = (float)Screen.width / Screen.height;
            cam.orthographicSize = aspect < w / h ? h / 2f : w / 2f / aspect;
        }

        void BuildLight()
        {
            var light = new GameObject("GlobalLight").AddComponent<Light2D>();
            light.transform.SetParent(transform, false);
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        void BuildArt()
        {
            var root = new GameObject("Art").transform;
            root.SetParent(transform, false);

            Transform cat = null, hamster = null;
            foreach (var l in layout.layers)
            {
                switch (l.kind)
                {
                    case "background":
                        Layer(l, root, 0);
                        break;
                    case "cat":
                        cat = Layer(l, root, 10).transform;
                        Idle(cat, 0.8f, new[] { Vector2.one, new Vector2(1.006f, 1.012f) });
                        break;
                    case "hamster":
                        hamster = Layer(l, root, 20).transform;
                        Idle(hamster, 0.45f, new[] { Vector2.one, new Vector2(1f, 1.03f) });
                        var hop = hamster.gameObject.AddComponent<PixelHop>();
                        hop.height = 2 * ArtPixel / PixelsPerUnit;
                        break;
                    case "scarf":
                        var tail = Layer(l, root, 21);
                        if (hamster != null) tail.transform.SetParent(hamster, true);
                        Idle(tail.transform, 0.18f, null, new[] { 0f, -7f, 3f, -4f });
                        break;
                    case "flower":
                        Idle(Layer(l, root, 30).transform, 0.35f, null, new[] { 0f, 6f, 0f, -6f });
                        break;
                }
            }

            if (cat != null) BuildBlink(cat);
            BuildSparkles(root);

            var motes = new GameObject("Motes").AddComponent<FloatingMotes>();
            motes.transform.SetParent(root, false);
            motes.sprite = PixelSprite(1, 1, (x, y) => Color.white);
            motes.pixel = ArtPixel / PixelsPerUnit;
            float hw = layout.imageWidth / PixelsPerUnit / 2f, hh = layout.imageHeight / PixelsPerUnit / 2f;
            motes.area = new Rect(-hw, -hh, hw * 2f, hh * 1.4f);
        }

        GameObject Layer(TitleLayout.Layer l, Transform parent, int order)
        {
            var tex = Resources.Load<Texture2D>("Title/" + l.name);
            var go = new GameObject(l.name);
            go.transform.SetParent(parent, false);
            if (tex == null)
            {
                Debug.LogError($"Missing title layer texture Resources/Title/{l.name}.png");
                return go;
            }
            tex.filterMode = FilterMode.Point;
            var pivot = new Vector2((l.pivotX - l.x) / (float)l.width, 1f - (l.pivotY - l.y) / (float)l.height);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = order;
            go.transform.localPosition = ToWorld(l.pivotX, l.pivotY);
            return go;
        }

        static void Idle(Transform t, float frameSeconds, Vector2[] scales, float[] angles = null)
        {
            var idle = t.gameObject.AddComponent<PixelIdle>();
            idle.frameSeconds = frameSeconds;
            if (scales != null) idle.scales = scales;
            if (angles != null) idle.angles = angles;
        }

        Vector3 ToWorld(float x, float y) =>
            new Vector3((x - layout.imageWidth / 2f) / PixelsPerUnit, (layout.imageHeight / 2f - y) / PixelsPerUnit, 0f);

        // Closed-eye overlays drawn over each star eye; they ride along with the cat's breathing.
        void BuildBlink(Transform cat)
        {
            const int size = 11;
            var closed = PixelSprite(size, size, (x, y) =>
            {
                // y counts from the top here; the arc is a soft "U" a little below the middle.
                bool line = (y == 5 && (x == 2 || x == 8)) || (y == 6 && (x == 2 || x == 3 || x == 7 || x == 8))
                            || (y == 7 && x >= 3 && x <= 7);
                if (line) return EyeLine;
                float dx = x - 5f, dy = y - 5f;
                return dx * dx + dy * dy <= 5.6f * 5.6f ? (Color)CatFur : Color.clear;
            });

            var blinker = cat.gameObject.AddComponent<Blinker>();
            blinker.overlays = new GameObject[layout.eyes.Length];
            for (int i = 0; i < layout.eyes.Length; i++)
            {
                var e = layout.eyes[i];
                var go = new GameObject("ClosedEye");
                go.transform.SetParent(cat, false);
                go.transform.position = ToWorld(e.x, e.y);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = closed;
                sr.sortingOrder = 11;
                blinker.overlays[i] = go;
            }
        }

        void BuildSparkles(Transform root)
        {
            var core = (Color)new Color32(255, 252, 222, 255);
            var arm = (Color)new Color32(252, 240, 170, 255);
            var frames = new[]
            {
                PixelSprite(7, 7, (x, y) => x == 3 && y == 3 ? core : Color.clear),
                PixelSprite(7, 7, (x, y) => (x == 3 && y >= 2 && y <= 4) || (y == 3 && x >= 2 && x <= 4) ? (x == 3 && y == 3 ? core : arm) : Color.clear),
                PixelSprite(7, 7, (x, y) =>
                {
                    bool cross = x == 3 || y == 3;
                    bool inner = Mathf.Abs(x - 3) + Mathf.Abs(y - 3) <= 1;
                    if (inner) return core;
                    return cross ? arm : Color.clear;
                }),
            };
            foreach (var s in layout.sparkles)
            {
                var go = new GameObject("Sparkle");
                go.transform.SetParent(root, false);
                go.transform.localPosition = ToWorld(s.x, s.y);
                // Bigger painted sparkles get a bigger star.
                float scale = Mathf.Clamp(Mathf.Round(s.size / (7f * ArtPixel)), 1f, 2f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 40;
                var tw = go.AddComponent<Twinkle>();
                tw.frames = frames;
            }
        }

        // A tiny procedural sprite where one texel is one art pixel. "pixel" gets x from the left, y from the top.
        static Sprite PixelSprite(int w, int h, System.Func<int, int, Color> pixel)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    colors[(h - 1 - y) * w + x] = pixel(x, y);
            tex.SetPixels(colors);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit / ArtPixel, 0, SpriteMeshType.FullRect);
        }

        // ---------- UI ----------

        void BuildUI()
        {
            font = LoadPixelFont("Fonts/Galmuri11");
            boldFont = LoadPixelFont("Fonts/Galmuri11-Bold") ?? font;

            var canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Portrait: lock UI to screen width so taller phones only gain vertical room.
            scaler.matchWidthOrHeight = 0f;

            var safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvasGo.transform, false);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var title = Label("Title", safe, boldFont, config.titleText, 176, Plum);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(1040f, 260f));
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(1f, 1f, 1f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -10f);

            // Loading bar and label sit in the middle of the screen, over the cat's belly.
            loadingGroup = new GameObject("Loading", typeof(RectTransform));
            var group = loadingGroup.GetComponent<RectTransform>();
            group.SetParent(safe, false);
            Place(group, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(660f, 200f));

            var track = Box("BarTrack", group, Cream);
            Place(track, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(660f, 48f));
            var inner = Box("BarInner", track, Plum);
            Stretch(inner, 8f);
            barFill = Box("BarFill", inner, Cream);
            Stretch(barFill, 6f);
            barFill.anchorMax = new Vector2(0f, 1f);

            loadingLabel = Label("LoadingLabel", group, font, config.loadingText, 66, Cream);
            Place(loadingLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(660f, 90f));
            Outline(loadingLabel);

            tapLabel = Label("TapLabel", safe, font, config.tapText, 66, Cream);
            Place(tapLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(1000f, 100f));
            Outline(tapLabel);
            tapLabel.gameObject.SetActive(false);
        }

        static Font LoadPixelFont(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f == null)
            {
                Debug.LogWarning($"Pixel font not found at Resources/{path}; using the built-in font.");
                return path.EndsWith("Bold") ? null : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            // Keep glyph edges hard: the dynamic font atlas is rebuilt as new characters appear.
            void Sharpen(Font changed)
            {
                if (changed == f && f.material != null && f.material.mainTexture != null) f.material.mainTexture.filterMode = FilterMode.Point;
            }
            Font.textureRebuilt += Sharpen;
            Sharpen(f);
            return f;
        }

        static Text Label(string name, RectTransform parent, Font font, string text, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static void Outline(Text t)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = Plum;
            o.effectDistance = new Vector2(6f, -6f);
        }

        static RectTransform Box(string name, RectTransform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, anchor.y);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        // ---------- Flow ----------

        IEnumerator Loading()
        {
            // TODO_ fake loading: fill the bar in blocky steps over loadingSeconds.
            const int steps = 12;
            float stepTime = Mathf.Max(0.01f, config.loadingSeconds) / steps;
            for (int i = 1; i <= steps; i++)
            {
                yield return new WaitForSeconds(stepTime);
                barFill.anchorMax = new Vector2(i / (float)steps, 1f);
                loadingLabel.text = config.loadingText + new string('.', i % 4);
            }
            yield return new WaitForSeconds(0.15f);

            loadingGroup.SetActive(false);
            tapLabel.gameObject.SetActive(true);
            ready = true;
            while (!started)
            {
                tapLabel.enabled = !tapLabel.enabled;
                yield return new WaitForSeconds(config.tapBlinkSeconds);
            }
        }

        IEnumerator StartGame()
        {
            started = true;
            for (int i = 0; i < 6; i++)
            {
                tapLabel.enabled = !tapLabel.enabled;
                yield return new WaitForSeconds(0.07f);
            }
            tapLabel.enabled = true;
            // TODO_ the next screen does not exist yet.
            Debug.Log("[Utopia] Start pressed. The next screen is not built yet.");
        }
    }
}
