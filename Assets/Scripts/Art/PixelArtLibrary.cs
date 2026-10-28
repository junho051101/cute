using System.Collections.Generic;
using UnityEngine;

namespace Hamster.Art
{
    // Procedural placeholder sprites in the game's mood: round, cute, pastel, dreamy, with a medieval village.
    // Swap for real art later; nothing here is final art.
    public static class PixelArtLibrary
    {
        public const float PixelsPerUnit = 16f;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static readonly Color32 Ink = Hex("5B4566");
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 White = Hex("FFFFFF");

        // Medieval building materials, kept soft and pastel.
        static readonly Color32 Stone = Hex("E4DCDC");
        static readonly Color32 StoneLight = Hex("F1EBEA");
        static readonly Color32 StoneShade = Hex("D3C8CA");
        static readonly Color32 StoneDark = Hex("BFB1B5");
        static readonly Color32 Plaster = Hex("FFF4E4");
        static readonly Color32 PlasterShade = Hex("F2E2CC");
        static readonly Color32 Timber = Hex("9C735C");
        static readonly Color32 TimberDark = Hex("7D5A49");
        static readonly Color32 DoorWood = Hex("B07A5C");
        static readonly Color32 GlassLit = Hex("FFE9A8");
        static readonly Color32 Iron = Hex("6E6274");
        static readonly Color32 Gold = Hex("FFE08A");

        // Plain golden hamster, no clothes or gear.
        static readonly Color32 HFur = Hex("F4A95E");
        static readonly Color32 HFurDark = Hex("E08A45");
        static readonly Color32 HFurLight = Hex("F8C48C");
        static readonly Color32 HBelly = Hex("FFF6EA");
        static readonly Color32 HPink = Hex("F7B2BE");
        static readonly Color32 HPaw = Hex("F5C2B8");

        public static Color32 Hex(string hex)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        static Color32 Mix(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);

        static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!cache.TryGetValue(key, out var s) || s == null)
            {
                s = make();
                cache[key] = s;
            }
            return s;
        }

        // ---------- Hamster ----------

        public enum Facing { Side, Front, Back }
        public const int WalkFrames = 4;
        const int HamsterW = 24, HamsterH = 21;
        static Sprite[] hamsterFrames;

        // Row per facing, column per walk frame; frame 0 doubles as the idle pose.
        // All frames share one texture so swapping frames never breaks batching.
        public static Sprite HamsterFrame(Facing facing, int frame)
        {
            if (hamsterFrames == null || hamsterFrames[0] == null) hamsterFrames = BuildHamsterFrames();
            return hamsterFrames[(int)facing * WalkFrames + frame];
        }

        static Sprite[] BuildHamsterFrames()
        {
            var atlas = new PixelCanvas(HamsterW * WalkFrames, HamsterH * 3);
            for (int f = 0; f < 3; f++)
            {
                for (int i = 0; i < WalkFrames; i++)
                {
                    var c = new PixelCanvas(HamsterW, HamsterH);
                    if (f == (int)Facing.Side) DrawHamsterSide(c, i);
                    else if (f == (int)Facing.Front) DrawHamsterFront(c, i);
                    else DrawHamsterBack(c, i);
                    c.Outline(Ink);
                    atlas.Blit(c, i * HamsterW, f * HamsterH);
                }
            }

            var tex = atlas.ToTexture();
            var pivot = new Vector2(0.5f, 1f / HamsterH);
            var sprites = new Sprite[3 * WalkFrames];
            for (int f = 0; f < 3; f++)
                for (int i = 0; i < WalkFrames; i++)
                    sprites[f * WalkFrames + i] = Sprite.Create(tex, new Rect(i * HamsterW, f * HamsterH, HamsterW, HamsterH),
                        pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            return sprites;
        }

        // Diagonal gait: one pair of paws steps together, the other pair half a cycle later.
        static readonly int[] StepX = { 1, 0, -1, 0 };
        static int StepLift(int phase) => phase == 3 ? 1 : 0;
        // The body rises while the paws pass under it.
        static int Bob(int frame) => frame % 2;

        // Facing right; flipX handles left.
        static void DrawHamsterSide(PixelCanvas c, int f)
        {
            int a = f, b = (f + 2) % 4;
            int y = 1 + Bob(f);
            var pawFar = Mix(HPaw, Ink, 0.2f);

            c.Rect(16 + StepX[b], 1 + StepLift(b), 3, 2, pawFar);
            c.Rect(6 + StepX[a], 1 + StepLift(a), 3, 2, pawFar);
            c.Ellipse(13.5f, y + 13f, 2.2f, 2.2f, HFurDark);
            c.Ellipse(13.5f, y + 13.4f, 1.1f, 1.1f, HPink);
            c.Ellipse(2.8f, y + 6f, 1.3f, 1.1f, HFurLight);
            c.Ellipse(10.5f, y + 6.5f, 8.5f, 6.2f, HFur);
            c.Ellipse(16f, y + 7.5f, 5.5f, 5f, HFur);
            c.Ellipse(10f, y + 11f, 5f, 1.3f, HFurLight);
            c.Ellipse(6.5f, y + 3f, 4f, 2.2f, HFurDark);
            c.Ellipse(14f, y + 3.5f, 5.5f, 2.8f, HBelly);
            c.Ellipse(17.5f, y + 5.5f, 2.6f, 2.1f, HBelly);
            c.Rect(17, y + 8, 2, 2, Ink);
            c.Set(18, y + 9, White);
            c.Set(18, y + 6, HPink);
            c.Set(21, y + 7, HPink);
            c.Rect(15 + StepX[a], 1 + StepLift(a), 3, 2, HPaw);
            c.Rect(5 + StepX[b], 1 + StepLift(b), 3, 2, HPaw);
        }

        static void DrawHamsterFront(PixelCanvas c, int f)
        {
            int y = 1 + Bob(f);
            foreach (var ex in new[] { 6.5f, 17.5f })
            {
                c.Ellipse(ex, y + 13.5f, 2.3f, 2.3f, HFurDark);
                c.Ellipse(ex, y + 13.8f, 1.2f, 1.2f, HPink);
            }
            c.Ellipse(12f, y + 7.5f, 8.5f, 7f, HFur);
            c.Ellipse(12f, y + 12f, 5f, 1.3f, HFurLight);
            c.Ellipse(12f, y + 4f, 6f, 4f, HBelly);
            c.Ellipse(12f, y + 7f, 3f, 2f, HBelly);
            c.Ellipse(6.5f, y + 6.5f, 2.4f, 2f, HBelly);
            c.Ellipse(17.5f, y + 6.5f, 2.4f, 2f, HBelly);
            c.Rect(8, y + 9, 2, 2, Ink);
            c.Rect(14, y + 9, 2, 2, Ink);
            c.Set(9, y + 10, White);
            c.Set(15, y + 10, White);
            c.Rect(11, y + 8, 2, 1, HPink);
            c.Set(6, y + 6, HPink);
            c.Set(17, y + 6, HPink);
            c.Rect(9, y + 4 + (f == 1 ? 1 : 0), 2, 1, HPaw);
            c.Rect(13, y + 4 + (f == 3 ? 1 : 0), 2, 1, HPaw);
            c.Rect(7, 1 + (f == 3 ? 1 : 0), 3, 2, HPaw);
            c.Rect(14, 1 + (f == 1 ? 1 : 0), 3, 2, HPaw);
        }

        static void DrawHamsterBack(PixelCanvas c, int f)
        {
            int y = 1 + Bob(f);
            c.Ellipse(6.5f, y + 13.5f, 2.3f, 2.3f, HFurDark);
            c.Ellipse(17.5f, y + 13.5f, 2.3f, 2.3f, HFurDark);
            c.Ellipse(12f, y + 7.5f, 8.5f, 7f, HFur);
            c.Ellipse(12f, y + 9.5f, 5.5f, 4f, Mix(HFur, HFurDark, 0.5f));
            c.Ellipse(12f, y + 12.5f, 4f, 1.2f, HFurLight);
            c.Ellipse(12f, y + 1.5f, 6.5f, 1.8f, HBelly);
            c.Rect(11, y + 3, 2, 2, Mix(HPink, HFur, 0.3f));
            c.Rect(7, 1 + (f == 3 ? 1 : 0), 3, 2, HPaw);
            c.Rect(14, 1 + (f == 1 ? 1 : 0), 3, 2, HPaw);
        }

        // ---------- Buildings ----------

        public static Sprite House(string roofHex, string style) =>
            style == "tower" ? Tower(roofHex) : Cottage(roofHex);

        // Half-timbered cottage on a stone footing with a shingled roof and chimney.
        static Sprite Cottage(string roofHex) => Cached("cottage_" + roofHex, () =>
        {
            var roof = Hex(roofHex);
            var roofDark = Mix(roof, Ink, 0.25f);
            var roofLight = Mix(roof, White, 0.4f);
            var c = new PixelCanvas(52, 48);

            c.Rect(8, 1, 36, 4, Stone);
            c.Rect(8, 3, 36, 1, StoneDark);
            for (int x = 8; x < 44; x++)
            {
                if ((x - 8) % 6 == 0) c.Rect(x, 1, 1, 2, StoneDark);
                if ((x - 5) % 6 == 0) c.Set(x, 4, StoneDark);
            }

            c.Rect(9, 5, 34, 16, Plaster);
            c.Rect(9, 6, 34, 2, PlasterShade);
            c.Rect(8, 5, 36, 1, TimberDark);
            c.Rect(9, 13, 34, 1, Timber);
            c.Rect(8, 20, 36, 1, Timber);
            foreach (var x in new[] { 9, 18, 33, 42 }) c.Rect(x, 5, 1, 15, Timber);
            foreach (var x in new[] { 10, 34 })
            {
                c.Line(x, 14, x + 7, 19, Timber);
                c.Line(x, 19, x + 7, 14, Timber);
            }

            foreach (var x in new[] { 10, 35 })
            {
                c.Rect(x, 7, 7, 6, TimberDark);
                c.Rect(x + 1, 8, 5, 4, GlassLit);
                c.Rect(x + 3, 8, 1, 4, TimberDark);
                c.Rect(x + 1, 10, 5, 1, TimberDark);
                c.Set(x + 1, 11, White);
            }

            c.Rect(21, 1, 10, 12, TimberDark);
            c.Ellipse(26f, 13f, 5f, 4f, TimberDark);
            c.Rect(22, 1, 8, 12, DoorWood);
            c.Ellipse(26f, 13f, 4f, 3f, DoorWood);
            var doorDark = Mix(DoorWood, Ink, 0.25f);
            c.Rect(24, 1, 1, 14, doorDark);
            c.Rect(27, 1, 1, 14, doorDark);
            c.Rect(22, 4, 3, 1, Iron);
            c.Rect(22, 10, 3, 1, Iron);
            c.Set(28, 7, Gold);

            Trapezoid(c, 26f, 21, 40, 22f, 12f, roof);
            c.Set(14, 40, Clear); c.Set(15, 40, Clear); c.Set(36, 40, Clear); c.Set(37, 40, Clear);
            c.Set(13, 39, Clear); c.Set(38, 39, Clear);
            c.Rect(4, 21, 44, 2, roofDark);
            Shingles(c, 25, 39, roof, roofDark);
            c.Ellipse(17f, 35f, 6f, 1.6f, roofLight);
            c.Set(13, 36, White);

            c.Ellipse(26f, 30f, 3.5f, 3.5f, TimberDark);
            c.Ellipse(26f, 30f, 2.5f, 2.5f, GlassLit);
            c.Set(25, 31, White);

            c.Rect(36, 33, 5, 11, Stone);
            c.Rect(36, 33, 1, 11, StoneLight);
            c.Rect(36, 37, 5, 1, StoneDark);
            c.Rect(38, 40, 1, 3, StoneDark);
            c.Rect(35, 43, 7, 2, StoneDark);

            c.Outline(Ink);
            return c.ToSprite(new Vector2(0.5f, 1f / 48f));
        });

        // Round stone tower with a conical roof and a pennant.
        static Sprite Tower(string roofHex) => Cached("tower_" + roofHex, () =>
        {
            var roof = Hex(roofHex);
            var roofDark = Mix(roof, Ink, 0.25f);
            var roofLight = Mix(roof, White, 0.4f);
            var c = new PixelCanvas(36, 64);

            c.Ellipse(18f, 3f, 11f, 2.5f, Stone);
            c.Rect(7, 3, 22, 34, Stone);
            c.Rect(7, 3, 3, 34, StoneLight);
            c.Rect(25, 3, 4, 34, StoneShade);
            for (int y = 6; y < 36; y += 5)
            {
                c.Rect(7, y, 22, 1, StoneDark);
                int shift = (y / 5) % 2 * 3;
                for (int x = 9 + shift; x < 29; x += 6) c.Rect(x, y + 1, 1, 4, StoneDark);
            }

            c.Rect(13, 1, 10, 9, TimberDark);
            c.Ellipse(18f, 10f, 5f, 4f, TimberDark);
            c.Rect(14, 1, 8, 9, DoorWood);
            c.Ellipse(18f, 10f, 4f, 3f, DoorWood);
            var doorDark = Mix(DoorWood, Ink, 0.25f);
            c.Rect(16, 1, 1, 11, doorDark);
            c.Rect(19, 1, 1, 11, doorDark);
            c.Rect(14, 3, 2, 1, Iron);
            c.Rect(14, 8, 2, 1, Iron);
            c.Set(20, 6, Gold);

            c.Rect(15, 19, 6, 8, TimberDark);
            c.Ellipse(18f, 27f, 3f, 2.5f, TimberDark);
            c.Rect(16, 20, 4, 7, GlassLit);
            c.Ellipse(18f, 27f, 2f, 1.5f, GlassLit);
            c.Rect(16, 23, 4, 1, TimberDark);
            c.Set(16, 26, White);
            c.Ellipse(18f, 32f, 2.2f, 2.2f, TimberDark);
            c.Ellipse(18f, 32f, 1.3f, 1.3f, GlassLit);

            c.Ellipse(18f, 37.5f, 13.5f, 2.6f, roofDark);
            Trapezoid(c, 18f, 38, 58, 13f, 1f, roof);
            Shingles(c, 41, 56, roof, roofDark);
            for (int y = 40; y <= 54; y++)
            {
                float hw = Mathf.Lerp(13f, 1f, (y - 38) / 20f);
                c.Set(Mathf.RoundToInt(18f - hw * 0.55f), y, roofLight);
            }

            c.Rect(18, 58, 1, 5, TimberDark);
            var pennant = Hex("FFF1A8");
            c.Rect(19, 60, 4, 2, pennant);
            c.Set(23, 61, pennant);

            c.Outline(Ink);
            return c.ToSprite(new Vector2(0.5f, 1f / 64f));
        });

        static void Trapezoid(PixelCanvas c, float cx, int y0, int y1, float halfBottom, float halfTop, Color32 col)
        {
            for (int y = y0; y <= y1; y++)
            {
                float hw = Mathf.Lerp(halfBottom, halfTop, (y - y0) / (float)(y1 - y0));
                int x0 = Mathf.RoundToInt(cx - hw), x1 = Mathf.RoundToInt(cx + hw);
                c.Rect(x0, y, x1 - x0, 1, col);
            }
        }

        // Rows of little U-shaped scallops, only painted over plain roof pixels.
        static void Shingles(PixelCanvas c, int y0, int y1, Color32 roof, Color32 line)
        {
            for (int y = y0; y <= y1; y += 4)
            {
                int shift = (y - y0) / 4 % 2 * 2;
                for (int x = 0; x < c.Width; x++)
                {
                    int k = (x + shift) % 4;
                    int py = k == 0 || k == 3 ? y + 1 : y;
                    if (Same(c.Get(x, py), roof)) c.Set(x, py, line);
                }
            }
        }

        public static Sprite Sign(string icon) => Cached("sign_" + icon, () =>
        {
            var c = new PixelCanvas(17, 17);
            c.Ellipse(8.5f, 8.5f, 7.5f, 7.5f, Hex("FFF9F0"));
            c.Ellipse(8.5f, 6.5f, 6.5f, 4.5f, Hex("F7EDF7"), 0f);
            c.Ellipse(8.5f, 8.5f, 7.5f, 7.5f, Hex("FFF9F0"), 7f);
            if (Icons.TryGetValue(icon, out var bmp))
                c.Bitmap(4, 4, bmp.rows, bmp.palette);
            c.Outline(Ink);
            return c.ToSprite(new Vector2(8f / 17f, 0f));
        });

        public static Sprite Fountain() => Cached("fountain", () =>
        {
            var water = Hex("A6DDF7");
            var waterLight = Hex("DDF4FF");
            var c = new PixelCanvas(46, 40);

            c.Ellipse(23f, 10f, 21f, 9f, StoneDark);
            c.Ellipse(23f, 11.5f, 21f, 8f, Stone);
            for (int x = 5; x <= 41; x += 6) c.Rect(x, 4, 1, 3, StoneDark);
            c.Ellipse(23f, 12f, 17f, 5.5f, water);
            c.Ellipse(16f, 13f, 4f, 1.2f, waterLight);
            c.Ellipse(30f, 11f, 3f, 1f, waterLight);
            c.Rect(20, 12, 6, 12, StoneShade);
            c.Rect(21, 12, 3, 12, StoneLight);
            c.Ellipse(23f, 24f, 9f, 3.5f, StoneDark);
            c.Ellipse(23f, 25f, 9f, 3f, Stone);
            c.Ellipse(23f, 25.5f, 6.5f, 1.8f, water);
            c.Ellipse(23f, 30f, 2.5f, 4f, water);
            c.Ellipse(22.5f, 31f, 1f, 2f, waterLight);
            c.Ellipse(23f, 35f, 1.5f, 1.5f, waterLight);
            c.Outline(Ink);
            return c.ToSprite(new Vector2(0.5f, 0.05f));
        });

        // ---------- Nature ----------

        public static Sprite Tree(int variant) => Cached("tree_" + variant, () =>
            TreeCanvas(variant).ToSprite(new Vector2(0.5f, 1f / 34f)));

        // Trunk base sits at pixel (14, 1). Shared by tree sprites and the forest painted around the map.
        static PixelCanvas TreeCanvas(int variant)
        {
            var leaf = variant % 3 == 0 ? Hex("9ED9B0") : variant % 3 == 1 ? Hex("B5E3A6") : Hex("A9D7C9");
            var leafDark = Mix(leaf, Ink, 0.18f);
            var leafLight = Mix(leaf, White, 0.4f);
            var c = new PixelCanvas(28, 34);

            c.Rect(12, 1, 5, 10, Hex("B48A6E"));
            c.Rect(12, 1, 2, 10, Hex("C9A285"));
            c.Ellipse(14f, 18f, 12f, 10f, leafDark);
            c.Ellipse(14f, 20f, 12f, 10f, leaf);
            c.Ellipse(9f, 24f, 6f, 5f, leafLight);
            c.Ellipse(19f, 26f, 4f, 3f, leaf);
            if (variant % 2 == 0)
            {
                foreach (var p in new[] { new Vector2Int(7, 18), new Vector2Int(19, 21), new Vector2Int(13, 26), new Vector2Int(21, 15) })
                {
                    c.Set(p.x, p.y, Hex("FFC6DA"));
                    c.Set(p.x + 1, p.y, Hex("FFC6DA"));
                }
            }
            c.Outline(Ink);
            return c;
        }

        public static Sprite Bush() => Cached("bush", () =>
        {
            var c = new PixelCanvas(16, 11);
            c.Ellipse(5f, 5f, 4.5f, 4f, Hex("A4D9A8"));
            c.Ellipse(11f, 5f, 4.5f, 4.5f, Hex("B5E3A6"));
            c.Ellipse(8f, 7f, 4f, 3.5f, Hex("C3EBB5"));
            c.Set(10, 8, Hex("FFC6DA"));
            c.Outline(Ink);
            return c.ToSprite(new Vector2(0.5f, 1f / 11f));
        });

        // Flowers are flat, so they are painted into the ground instead of being separate sprites.
        static void DrawFlower(PixelCanvas c, int x, int y, int variant)
        {
            var petal = variant % 3 == 0 ? Hex("FFC6DA") : variant % 3 == 1 ? Hex("FFF1A8") : Hex("D9C6FF");
            var stem = Hex("8CC79A");
            c.Set(x + 2, y + 4, petal); c.Set(x + 1, y + 3, petal); c.Set(x + 3, y + 3, petal); c.Set(x + 2, y + 2, petal);
            c.Set(x + 2, y + 3, White);
            c.Set(x + 2, y + 1, stem); c.Set(x + 2, y, stem);
        }

        // Soft round glow used for dreamy floating motes and droplets.
        public static Sprite Mote() => Cached("mote", () =>
        {
            var c = new PixelCanvas(5, 5);
            var edge = new Color32(255, 236, 250, 140);
            c.Set(2, 2, White);
            c.Set(1, 2, edge); c.Set(3, 2, edge); c.Set(2, 1, edge); c.Set(2, 3, edge);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        // ---------- UI ----------

        public static Sprite Circle(int size, Color32 fill, Color32 rim)
        {
            return Cached($"circle_{size}_{fill}_{rim}", () =>
            {
                var c = new PixelCanvas(size, size);
                float r = size / 2f;
                c.Ellipse(r, r, r, r, rim);
                c.Ellipse(r, r, r - Mathf.Max(1f, size / 16f), r - Mathf.Max(1f, size / 16f), fill);
                var s = c.ToSprite(new Vector2(0.5f, 0.5f), 100f);
                s.texture.filterMode = FilterMode.Bilinear;
                return s;
            });
        }

        public static Sprite Shadow(int width) => Cached("shadow_" + width, () =>
        {
            int h = Mathf.Max(3, width / 3);
            var c = new PixelCanvas(width, h);
            c.Ellipse(width / 2f, h / 2f, width / 2f, h / 2f, new Color32(120, 90, 150, 70));
            return c.ToSprite(new Vector2(0.5f, (h / 2) / (float)h));
        });

        // 9-sliced rounded panel for UI.
        public static Sprite RoundedRect(Color32 fill, Color32 rim) => Cached($"rounded_{fill}_{rim}", () =>
        {
            const int n = 48, r = 18, rimWidth = 3;
            var c = new PixelCanvas(n, n);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = RoundedDistance(x + 0.5f, y + 0.5f, n, r);
                    if (d > 0f) continue;
                    c.Set(x, y, d > -rimWidth ? rim : fill);
                }
            }
            var tex = c.ToTexture();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        });

        static float RoundedDistance(float x, float y, int n, int r)
        {
            float qx = Mathf.Max(Mathf.Abs(x - n / 2f) - (n / 2f - r), 0f);
            float qy = Mathf.Max(Mathf.Abs(y - n / 2f) - (n / 2f - r), 0f);
            return Mathf.Sqrt(qx * qx + qy * qy) - r;
        }

        // ---------- Ground ----------

        // Hand-authored layout the ground is painted from, in world units centered on the map.
        public class GroundLayout
        {
            public int mapWidth, mapHeight;
            // Extra ground around the map, filled with forest, for screens wider or taller than the map.
            public int margin = 4;
            public Rect plaza;
            public readonly List<(Vector2[] points, float width)> paths = new List<(Vector2[], float)>();
            public readonly List<Rect> flowerBeds = new List<Rect>();
            public readonly List<Rect> fields = new List<Rect>();
            public int seed;
        }

        // One texture for the whole village floor: flat grass with sparse tufts, winding dirt paths,
        // a square cobbled plaza, crop fields, flower beds and a forest ring outside the walkable map.
        public static Sprite Ground(GroundLayout g)
        {
            int wu = g.mapWidth + g.margin * 2, hu = g.mapHeight + g.margin * 2;
            int w = wu * (int)PixelsPerUnit, h = hu * (int)PixelsPerUnit;
            var c = new PixelCanvas(w, h);
            var grass = Hex("C7EBC0");
            var grassDark = Hex("B2DDAA");
            var grassLight = Hex("DDF5D3");

            Vector2 ToPx(Vector2 world) => new Vector2((world.x + wu / 2f) * PixelsPerUnit, (world.y + hu / 2f) * PixelsPerUnit);
            Rect ToPxRect(Rect r) => new Rect(ToPx(r.min), r.size * PixelsPerUnit);

            c.Rect(0, 0, w, h, grass);
            for (int cy = 0; cy < h; cy += 9)
            {
                for (int cx = 0; cx < w; cx += 9)
                {
                    int n = Hash(cx, cy, g.seed);
                    if (n % 4 != 0) continue;
                    Tuft(c, cx + (n >> 3) % 7, cy + (n >> 6) % 7, (n >> 9) % 3 == 0 ? grassLight : grassDark);
                }
            }

            foreach (var f in g.fields) PaintField(c, ToPxRect(f));

            var pathPx = new List<(Vector2[] points, float halfWidth)>();
            foreach (var (points, width) in g.paths)
            {
                var px = new Vector2[points.Length];
                for (int i = 0; i < points.Length; i++) px[i] = ToPx(points[i]);
                pathPx.Add((px, width * PixelsPerUnit / 2f));
            }
            var path = Hex("EEDCBE");
            var pathEdge = Hex("DCC3A0");
            var pebble = Hex("E2CCAA");
            foreach (var (pts, hw) in pathPx)
                for (int i = 0; i + 1 < pts.Length; i++) PaintSegment(c, pts[i], pts[i + 1], hw + 1.5f, pathEdge, pathEdge, g.seed);
            foreach (var (pts, hw) in pathPx)
                for (int i = 0; i + 1 < pts.Length; i++) PaintSegment(c, pts[i], pts[i + 1], hw, path, pebble, g.seed);

            PaintPlaza(c, ToPxRect(g.plaza), g.seed);

            foreach (var bed in g.flowerBeds)
            {
                var r = ToPxRect(bed);
                for (int y = (int)r.yMin; y < r.yMax - 4; y += 3)
                {
                    for (int x = (int)r.xMin; x < r.xMax - 4; x += 3)
                    {
                        int n = Hash(x, y, g.seed + 5);
                        if (n % 3 == 0) continue;
                        DrawFlower(c, x + (n >> 4) % 2, y + (n >> 6) % 2, (n >> 8) % 3);
                    }
                }
            }

            PaintForest(c, g, ToPx, pathPx);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static void Tuft(PixelCanvas c, int x, int y, Color32 col)
        {
            c.Set(x, y, col);
            c.Set(x - 1, y + 1, col);
            c.Set(x + 1, y + 1, col);
            c.Set(x + 1, y + 2, col);
        }

        // Tilled rows with little sprouts: the hamsters farm a bit at home.
        static void PaintField(PixelCanvas c, Rect r)
        {
            var soil = Hex("D9B48E");
            var furrow = Hex("C49A74");
            var sprout = Hex("8CC79A");
            int x0 = (int)r.xMin, x1 = (int)r.xMax, y0 = (int)r.yMin, y1 = (int)r.yMax;
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    bool edge = x == x0 || x == x1 - 1 || y == y0 || y == y1 - 1;
                    c.Set(x, y, edge || (y - y0) % 4 == 0 ? furrow : soil);
                }
            }
            for (int y = y0 + 2; y < y1 - 2; y += 4)
            {
                for (int x = x0 + 2; x < x1 - 2; x += 3)
                {
                    c.Set(x, y, sprout);
                    c.Set(x, y + 1, sprout);
                    c.Set(x + 1, y + 1, sprout);
                }
            }
        }

        // Rounded-square town square of uneven cobbles: each row is cut into stones of varying width.
        static void PaintPlaza(PixelCanvas c, Rect r, int seed)
        {
            var cobble = Hex("EEE5EA");
            var cobbleAlt = Hex("E3D8E0");
            var cobbleLight = Hex("F6F0F3");
            var mortar = Hex("D2C4CE");
            var rim = Hex("C2B3BD");
            const int corner = 10, rimWidth = 2, rowHeight = 4;
            int x0 = (int)r.xMin, x1 = (int)r.xMax, y0 = (int)r.yMin, y1 = (int)r.yMax;

            bool Inside(int x, int y, int inset)
            {
                float qx = Mathf.Max(Mathf.Max(x0 + inset + corner - (x + 0.5f), x + 0.5f - (x1 - inset - corner)), 0f);
                float qy = Mathf.Max(Mathf.Max(y0 + inset + corner - (y + 0.5f), y + 0.5f - (y1 - inset - corner)), 0f);
                return qx * qx + qy * qy <= corner * corner;
            }

            for (int y = y0; y < y1; y++)
            {
                int row = (y - y0) / rowHeight;
                // Stone boundaries for this row, walked left to right with uneven widths.
                int edge = x0 - Hash(row, 0, seed) % 5;
                int stone = 0;
                for (int x = x0; x < x1; x++)
                {
                    if (!Inside(x, y, 0)) continue;
                    if (!Inside(x, y, rimWidth))
                    {
                        c.Set(x, y, rim);
                        continue;
                    }
                    while (x >= edge)
                    {
                        stone = edge;
                        edge += 4 + Hash(edge, row, seed) % 4;
                    }
                    bool line = (y - y0) % rowHeight == 0 || x == stone;
                    int n = Hash(stone, row, seed + 2) % 6;
                    c.Set(x, y, line ? mortar : n == 0 ? cobbleAlt : n == 1 ? cobbleLight : cobble);
                }
            }
        }

        // Trees packed around the outside of the map so wider or taller screens see woods, not bare grass.
        // Canopies stay outside the walkable area, and the gaps where paths leave the map stay open.
        static void PaintForest(PixelCanvas c, GroundLayout g, System.Func<Vector2, Vector2> toPx,
            List<(Vector2[] points, float halfWidth)> paths)
        {
            float hw = g.mapWidth / 2f, hh = g.mapHeight / 2f;
            var spots = new List<(Vector2 world, int variant)>();
            const float step = 1.15f;
            for (float y = -hh - g.margin; y <= hh + g.margin; y += step)
            {
                for (float x = -hw - g.margin; x <= hw + g.margin; x += step)
                {
                    int n = Hash(Mathf.RoundToInt(x * 10f), Mathf.RoundToInt(y * 10f), g.seed + 9);
                    var p = new Vector2(x + (n % 7 - 3) * 0.08f, y + ((n >> 3) % 7 - 3) * 0.08f);
                    // Side canopies are ~0.9 wide and ~1.9 tall, so keep them clear of the map.
                    bool outside = p.y > hh || p.y < -hh - 1.9f || p.x < -hw - 0.9f || p.x > hw + 0.9f;
                    if (!outside) continue;
                    var px = toPx(p);
                    bool onPath = false;
                    foreach (var (pts, halfWidth) in paths)
                        for (int i = 0; i + 1 < pts.Length && !onPath; i++)
                            onPath = DistanceToSegment(px, pts[i], pts[i + 1]) < halfWidth + 14f;
                    if (onPath) continue;
                    spots.Add((p, (n >> 6) % 6));
                }
            }
            // Back to front so lower trees overlap the ones behind them.
            spots.Sort((a, b) => b.world.y.CompareTo(a.world.y));
            var trees = new Dictionary<int, PixelCanvas>();
            foreach (var (world, variant) in spots)
            {
                if (!trees.TryGetValue(variant, out var tree)) trees[variant] = tree = TreeCanvas(variant);
                var px = toPx(world);
                c.Blit(tree, Mathf.RoundToInt(px.x) - 14, Mathf.RoundToInt(px.y) - 1);
            }
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        // Only visits the segment's bounding box. Edges wobble a pixel so paths look hand-drawn.
        static void PaintSegment(PixelCanvas c, Vector2 a, Vector2 b, float halfWidth, Color32 color, Color32 speck, int seed)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - halfWidth - 1f));
            int x1 = Mathf.Min(c.Width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + halfWidth + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - halfWidth - 1f));
            int y1 = Mathf.Min(c.Height - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + halfWidth + 1f));
            var ab = b - a;
            float abLen2 = ab.sqrMagnitude;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = abLen2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLen2) : 0f;
                    float r = halfWidth + (Hash(x / 5, y / 5, seed + 3) % 3 - 1) * 0.8f;
                    if ((p - (a + ab * t)).sqrMagnitude > r * r) continue;
                    c.Set(x, y, Hash(x / 2, y / 2, seed + 1) % 23 == 0 ? speck : color);
                }
            }
        }

        static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 2147483647;
                h = (h ^ (h >> 13)) * 1274126177;
                return (h ^ (h >> 16)) & 0x7fffffff;
            }
        }

        // ---------- Sign icons ----------

        struct IconBitmap
        {
            public string[] rows;
            public System.Func<char, Color32?> palette;
        }

        static IconBitmap Icon(Color32 light, Color32 accent, params string[] rows) => new IconBitmap
        {
            rows = rows,
            palette = ch => ch == '#' ? Ink : ch == 'w' ? light : ch == 'o' ? accent : (Color32?)null
        };

        // 9x9 icons for building signs.
        static readonly Dictionary<string, IconBitmap> Icons = new Dictionary<string, IconBitmap>
        {
            ["sword"] = Icon(Hex("E4ECF5"), Hex("C98E5E"),
                "....#....",
                "...#w#...",
                "...#w#...",
                "...#w#...",
                "...#w#...",
                ".#######.",
                "...#o#...",
                "...#o#...",
                "....#...."),
            ["dagger"] = Icon(Hex("E4ECF5"), Hex("B07ACB"),
                ".........",
                "....#....",
                "...#w#...",
                "...#w#...",
                "...#w#...",
                "..#####..",
                "...#o#...",
                "....#....",
                "........."),
            ["bow"] = Icon(Hex("FFFFFF"), Hex("C98E5E"),
                "...oo....",
                "...#.oo..",
                "...#...o.",
                "...#...o.",
                "#wwwwwww#",
                "...#...o.",
                "...#...o.",
                "...#.oo..",
                "...oo...."),
            ["helmet"] = Icon(Hex("E4ECF5"), Hex("F7B2BE"),
                "....o....",
                "..#####..",
                ".#wwwww#.",
                "#wwwwwww#",
                "#w#####w#",
                "#w#...#w#",
                "#ww...ww#",
                ".##...##.",
                "........."),
            ["anvil"] = Icon(Hex("D9DEEA"), Hex("FFB86B"),
                "......o..",
                ".....o.o.",
                "#######..",
                ".#wwwww##",
                "..#www#..",
                "...#w#...",
                "..#www#..",
                ".#######.",
                "........."),
            ["moon"] = Icon(Hex("FFF1A8"), Hex("D9C6FF"),
                "...###..o",
                "..#ww#...",
                ".#ww#..o.",
                ".#w#.....",
                ".#w#.....",
                ".#ww#....",
                "..#ww###.",
                "...####..",
                "........."),
        };
    }
}
