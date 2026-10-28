using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// Splits the single title painting into layers that can move on their own.
public static class TitleSplitter
{
    static int W, H;
    static int[] px;

    static int R(int c) { return (c >> 16) & 255; }
    static int G(int c) { return (c >> 8) & 255; }
    static int B(int c) { return c & 255; }

    static bool IsOutline(int c) { return G(c) < 40 && R(c) < 130 && B(c) < 120; }

    public static string Run(string src, string outDir)
    {
        var sb = new StringBuilder();
        using (var bmp = new Bitmap(src)) Load(bmp);

        var outline = new bool[W * H];
        for (int i = 0; i < px.Length; i++) outline[i] = IsOutline(px[i]);

        // Everything reachable from the border without crossing an outline is "outside" (open background).
        var outside = FloodFromBorder(i => !outline[i]);

        // Cat: the region around a body seed, grown into its outline, with interior holes (eyes etc.) filled.
        var catCore = Component(i => !outline[i] && !outside[i], Idx(380, 850));
        var catGrown = GrowInto(catCore, outline, 20);
        var cat = FillHoles(catGrown);

        // Hamster: enclosed pixels inside its box that are not the cat, grown into its outline.
        var hamCore = new bool[W * H];
        for (int y = 1020; y < 1310; y++)
            for (int x = 280; x < 600; x++)
            {
                int i = Idx(x, y);
                if (!outline[i] && !outside[i] && !catCore[i]) hamCore[i] = true;
            }
        // Keep only the parts connected to the hamster body and scarf.
        hamCore = KeepConnected(hamCore, new[] { Idx(380, 1230), Idx(400, 1160) });
        var ham = FillHoles(GrowInto(hamCore, outline, 20));
        for (int i = 0; i < ham.Length; i++) if (ham[i]) cat[i] = false;

        // Scarf tail: hamster pixels right of the body, cut off as their own piece.
        var hamBox = BoundsOf(ham);
        int tailX = FindTailSplit(ham, hamBox);
        var tail = new bool[W * H];
        for (int i = 0; i < ham.Length; i++)
            if (ham[i] && i % W >= tailX) { tail[i] = true; ham[i] = false; }

        // Flowers and seeds: small non-green blobs on the meadow.
        var flowers = SmallBlobs(i =>
        {
            int x = i % W, y = i / W;
            if (y < 1040 || cat[i] || ham[i] || tail[i]) return false;
            int c = px[i];
            bool green = G(c) > R(c) + 15 && G(c) >= B(c) - 10;
            return !green;
        }, 3, 40, 6000, 110);
        // Real flowers have petals (white, pink or orange) and sit clear of the cat and hamster.
        var nearFigure = Dilate(Or(cat, Or(ham, tail)), 6);
        flowers = flowers.FindAll(f =>
        {
            int petals = 0;
            foreach (int i in f.pixels)
            {
                if (nearFigure[i]) return false;
                int c = px[i];
                bool white = R(c) > 225 && G(c) > 220 && B(c) > 200;
                bool warm = R(c) > 200 && R(c) > G(c) + 40;
                if (white || warm) petals++;
            }
            return petals >= 25;
        });

        // Sky sparkles: small pale-yellow crosses away from the cat (joined wide so hollow crosses stay one piece).
        var sparkles = SmallBlobs(i =>
        {
            int y = i / W;
            if (y > 760 || cat[i]) return false;
            int c = px[i];
            return R(c) > 225 && G(c) > 225 && B(c) < 235;
        }, 7, 6, 4000, 80);

        // Eyes: pale-yellow clusters inside the cat's face.
        var eyes = SmallBlobs(i =>
        {
            int y = i / W;
            if (y < 400 || y > 620 || !cat[i]) return false;
            int c = px[i];
            return R(c) > 225 && G(c) > 225 && B(c) < 235;
        }, 2, 20, 8000, 120);

        // Background: fill where the moving pieces used to be so nothing doubles up when they move.
        var hole = new bool[W * H];
        for (int i = 0; i < hole.Length; i++) hole[i] = ham[i] || tail[i];
        foreach (var f in flowers) foreach (int i in f.pixels) hole[i] = true;
        foreach (var s in sparkles) foreach (int i in s.pixels) hole[i] = true;
        hole = Dilate(hole, 2);
        var bg = (int[])px.Clone();
        Inpaint(bg, hole);

        Directory.CreateDirectory(outDir);
        var json = new StringBuilder();
        json.Append("{\n  \"imageWidth\": " + W + ",\n  \"imageHeight\": " + H + ",\n  \"layers\": [\n");
        var layers = new List<string>();

        SaveFull(bg, Path.Combine(outDir, "bg.png"));
        layers.Add(LayerJson("bg", "background", 0, 0, W, H, W / 2, H / 2));

        var catBox = BoundsOf(cat);
        SaveMasked(px, cat, catBox, Path.Combine(outDir, "cat.png"));
        layers.Add(LayerJson("cat", "cat", catBox, (catBox.Left + catBox.Right) / 2, catBox.Bottom));

        var hb = BoundsOf(ham);
        SaveMasked(px, ham, hb, Path.Combine(outDir, "hamster.png"));
        layers.Add(LayerJson("hamster", "hamster", hb, (hb.Left + hb.Right) / 2, hb.Bottom));

        var tb = BoundsOf(tail);
        if (tb.Width > 0)
        {
            SaveMasked(px, tail, tb, Path.Combine(outDir, "scarf_tail.png"));
            // Swings from where it leaves the hamster's neck.
            int attachY = TailAttachY(tail, tb);
            layers.Add(LayerJson("scarf_tail", "scarf", tb, tb.Left, attachY));
        }

        int n = 0;
        foreach (var f in flowers)
        {
            var mask = new bool[W * H];
            foreach (int i in f.pixels) mask[i] = true;
            mask = Dilate(mask, 1);
            var fb = BoundsOf(mask);
            string name = "flower_" + n++;
            SaveMasked(px, mask, fb, Path.Combine(outDir, name + ".png"));
            layers.Add(LayerJson(name, "flower", fb, (fb.Left + fb.Right) / 2, fb.Bottom));
        }
        json.Append(string.Join(",\n", layers));
        json.Append("\n  ],\n  \"sparkles\": [\n");
        var sp = new List<string>();
        foreach (var s in sparkles)
            sp.Add(string.Format("    {{ \"x\": {0}, \"y\": {1}, \"size\": {2} }}", s.cx, s.cy, Math.Max(s.box.Width, s.box.Height)));
        json.Append(string.Join(",\n", sp));
        json.Append("\n  ],\n  \"eyes\": [\n");
        var ey = new List<string>();
        foreach (var e in eyes)
            ey.Add(string.Format("    {{ \"x\": {0}, \"y\": {1}, \"size\": {2} }}", e.cx, e.cy, Math.Max(e.box.Width, e.box.Height)));
        json.Append(string.Join(",\n", ey));
        json.Append("\n  ]\n}\n");
        File.WriteAllText(Path.Combine(outDir, "title_layout.json"), json.ToString(), new UTF8Encoding(false));

        sb.AppendLine("cat " + catBox + " hamster " + hb + " tail " + tb + " tailX " + tailX);
        sb.AppendLine("flowers " + flowers.Count + " sparkles " + sparkles.Count + " eyes " + eyes.Count);
        foreach (var f in flowers) sb.AppendLine("  flower " + f.box);
        foreach (var e in eyes) sb.AppendLine("  eye " + e.cx + "," + e.cy + " " + e.box);
        foreach (var s in sparkles) sb.AppendLine("  sparkle " + s.cx + "," + s.cy + " " + s.box);
        return sb.ToString();
    }

    // ---------- helpers ----------

    static int Idx(int x, int y) { return y * W + x; }

    static bool[] Or(bool[] a, bool[] b)
    {
        var r = new bool[a.Length];
        for (int i = 0; i < r.Length; i++) r[i] = a[i] || b[i];
        return r;
    }

    static void Load(Bitmap bmp)
    {
        W = bmp.Width; H = bmp.Height;
        var data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        px = new int[W * H];
        for (int y = 0; y < H; y++) Marshal.Copy(data.Scan0 + y * data.Stride, px, y * W, W);
        bmp.UnlockBits(data);
    }

    static bool[] FloodFromBorder(Func<int, bool> pass)
    {
        var seen = new bool[W * H];
        var q = new Queue<int>();
        for (int x = 0; x < W; x++) { Seed(q, seen, pass, Idx(x, 0)); Seed(q, seen, pass, Idx(x, H - 1)); }
        for (int y = 0; y < H; y++) { Seed(q, seen, pass, Idx(0, y)); Seed(q, seen, pass, Idx(W - 1, y)); }
        Spread(q, seen, pass);
        return seen;
    }

    static void Seed(Queue<int> q, bool[] seen, Func<int, bool> pass, int i)
    {
        if (!seen[i] && pass(i)) { seen[i] = true; q.Enqueue(i); }
    }

    static void Spread(Queue<int> q, bool[] seen, Func<int, bool> pass)
    {
        while (q.Count > 0)
        {
            int i = q.Dequeue(), x = i % W, y = i / W;
            if (x > 0) Seed(q, seen, pass, i - 1);
            if (x < W - 1) Seed(q, seen, pass, i + 1);
            if (y > 0) Seed(q, seen, pass, i - W);
            if (y < H - 1) Seed(q, seen, pass, i + W);
        }
    }

    static bool[] Component(Func<int, bool> pass, int seed)
    {
        var seen = new bool[W * H];
        var q = new Queue<int>();
        Seed(q, seen, pass, seed);
        Spread(q, seen, pass);
        return seen;
    }

    static bool[] KeepConnected(bool[] mask, int[] seeds)
    {
        var seen = new bool[W * H];
        var q = new Queue<int>();
        Func<int, bool> pass = i => mask[i];
        foreach (int s in seeds) Seed(q, seen, pass, s);
        Spread(q, seen, pass);
        return seen;
    }

    // Grows a region step by step, but only into pixels allowed by "into" (its outline).
    static bool[] GrowInto(bool[] region, bool[] into, int steps)
    {
        var cur = (bool[])region.Clone();
        for (int s = 0; s < steps; s++)
        {
            var next = (bool[])cur.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = Idx(x, y);
                    if (cur[i] || !into[i]) continue;
                    if ((x > 0 && cur[i - 1]) || (x < W - 1 && cur[i + 1]) || (y > 0 && cur[i - W]) || (y < H - 1 && cur[i + W]))
                        next[i] = true;
                }
            cur = next;
        }
        return cur;
    }

    static bool[] FillHoles(bool[] region)
    {
        var outsideOfRegion = FloodFromBorder(i => !region[i]);
        var filled = new bool[W * H];
        for (int i = 0; i < filled.Length; i++) filled[i] = !outsideOfRegion[i];
        return filled;
    }

    static bool[] Dilate(bool[] m, int r)
    {
        var cur = m;
        for (int s = 0; s < r; s++)
        {
            var next = (bool[])cur.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = Idx(x, y);
                    if (cur[i]) continue;
                    if ((x > 0 && cur[i - 1]) || (x < W - 1 && cur[i + 1]) || (y > 0 && cur[i - W]) || (y < H - 1 && cur[i + W]))
                        next[i] = true;
                }
            cur = next;
        }
        return cur;
    }

    static Rectangle BoundsOf(bool[] m)
    {
        int x0 = W, y0 = H, x1 = -1, y1 = -1;
        for (int i = 0; i < m.Length; i++)
        {
            if (!m[i]) continue;
            int x = i % W, y = i / W;
            if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
        }
        if (x1 < 0) return Rectangle.Empty;
        return new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    // The tail starts where the hamster's column height drops to a thin strip right of its body.
    static int FindTailSplit(bool[] ham, Rectangle box)
    {
        int body = 0;
        int[] heights = new int[box.Width];
        for (int x = box.Left; x < box.Right; x++)
        {
            int h = 0;
            for (int y = box.Top; y < box.Bottom; y++) if (ham[Idx(x, y)]) h++;
            heights[x - box.Left] = h;
            if (h > body) body = h;
        }
        for (int x = box.Left + box.Width / 2; x < box.Right; x++)
            if (heights[x - box.Left] < body * 0.35) return x;
        return box.Right;
    }

    static int TailAttachY(bool[] tail, Rectangle tb)
    {
        int sum = 0, count = 0;
        for (int y = tb.Top; y < tb.Bottom; y++)
            for (int x = tb.Left; x < tb.Left + 4; x++)
                if (tail[Idx(x, y)]) { sum += y; count++; }
        return count > 0 ? sum / count : tb.Top + tb.Height / 2;
    }

    class Blob { public List<int> pixels = new List<int>(); public Rectangle box; public int cx, cy; }

    // Groups matching pixels (after a small dilation so a blob's pieces join) into blobs of a sane size.
    static List<Blob> SmallBlobs(Func<int, bool> match, int join, int minArea, int maxArea, int maxSide)
    {
        var m = new bool[W * H];
        for (int i = 0; i < m.Length; i++) m[i] = match(i);
        var joined = Dilate(m, join);
        var seen = new bool[W * H];
        var result = new List<Blob>();
        for (int s = 0; s < joined.Length; s++)
        {
            if (!joined[s] || seen[s]) continue;
            var comp = new List<int>();
            var q = new Queue<int>();
            seen[s] = true; q.Enqueue(s);
            while (q.Count > 0)
            {
                int i = q.Dequeue(), x = i % W, y = i / W;
                comp.Add(i);
                if (x > 0 && joined[i - 1] && !seen[i - 1]) { seen[i - 1] = true; q.Enqueue(i - 1); }
                if (x < W - 1 && joined[i + 1] && !seen[i + 1]) { seen[i + 1] = true; q.Enqueue(i + 1); }
                if (y > 0 && joined[i - W] && !seen[i - W]) { seen[i - W] = true; q.Enqueue(i - W); }
                if (y < H - 1 && joined[i + W] && !seen[i + W]) { seen[i + W] = true; q.Enqueue(i + W); }
            }
            var b = new Blob();
            foreach (int i in comp) if (m[i]) b.pixels.Add(i);
            if (b.pixels.Count < minArea || b.pixels.Count > maxArea) continue;
            var mask = new bool[W * H];
            foreach (int i in comp) mask[i] = true;
            b.box = BoundsOf(mask);
            if (b.box.Width > maxSide || b.box.Height > maxSide) continue;
            b.cx = b.box.Left + b.box.Width / 2; b.cy = b.box.Top + b.box.Height / 2;
            // Take the whole joined area (it holds the blob's outline too).
            b.pixels = comp;
            result.Add(b);
        }
        return result;
    }

    // Fills holes from their edges inward, each new pixel copying the most common known neighbour colour.
    static void Inpaint(int[] img, bool[] hole)
    {
        var unknown = (bool[])hole.Clone();
        bool any = true;
        var counts = new Dictionary<int, int>();
        while (any)
        {
            any = false;
            var updates = new List<KeyValuePair<int, int>>();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = Idx(x, y);
                    if (!unknown[i]) continue;
                    counts.Clear();
                    int best = -1, bestN = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                            int j = Idx(nx, ny);
                            if (unknown[j]) continue;
                            int c = img[j];
                            int k; counts.TryGetValue(c, out k); k++; counts[c] = k;
                            if (k > bestN) { bestN = k; best = c; }
                        }
                    if (bestN > 0) updates.Add(new KeyValuePair<int, int>(i, best));
                }
            foreach (var u in updates) { img[u.Key] = u.Value; unknown[u.Key] = false; any = true; }
        }
    }

    static string LayerJson(string name, string kind, Rectangle r, int pivotX, int pivotY)
    {
        return LayerJson(name, kind, r.Left, r.Top, r.Width, r.Height, pivotX, pivotY);
    }

    static string LayerJson(string name, string kind, int x, int y, int w, int h, int pivotX, int pivotY)
    {
        return string.Format("    {{ \"name\": \"{0}\", \"kind\": \"{1}\", \"x\": {2}, \"y\": {3}, \"width\": {4}, \"height\": {5}, \"pivotX\": {6}, \"pivotY\": {7} }}",
            name, kind, x, y, w, h, pivotX, pivotY);
    }

    static void SaveFull(int[] img, string path)
    {
        using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        {
            var data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var row = new int[W];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++) row[x] = img[Idx(x, y)] | unchecked((int)0xFF000000);
                Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, W);
            }
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static void SaveMasked(int[] img, bool[] mask, Rectangle r, string path)
    {
        using (var bmp = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb))
        {
            var data = bmp.LockBits(new Rectangle(0, 0, r.Width, r.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var row = new int[r.Width];
            for (int y = 0; y < r.Height; y++)
            {
                for (int x = 0; x < r.Width; x++)
                {
                    int i = Idx(r.Left + x, r.Top + y);
                    row[x] = mask[i] ? (img[i] | unchecked((int)0xFF000000)) : 0;
                }
                Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, r.Width);
            }
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
