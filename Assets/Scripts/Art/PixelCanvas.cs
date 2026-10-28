using UnityEngine;

namespace Hamster.Art
{
    // Small drawing surface for procedural placeholder pixel art.
    // Origin is bottom-left, matching Texture2D.
    public class PixelCanvas
    {
        public readonly int Width;
        public readonly int Height;
        readonly Color32[] pixels;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            pixels = new Color32[width * height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public void Set(int x, int y, Color32 c)
        {
            if (InBounds(x, y)) pixels[y * Width + x] = c;
        }

        public Color32 Get(int x, int y) => InBounds(x, y) ? pixels[y * Width + x] : new Color32(0, 0, 0, 0);

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Set(i, j, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c, float minY = float.MinValue)
        {
            int x0 = Mathf.FloorToInt(cx - rx), x1 = Mathf.CeilToInt(cx + rx);
            int y0 = Mathf.FloorToInt(cy - ry), y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
            {
                if (y + 0.5f < minY) continue;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
            }
        }

        // Draws a bitmap given as rows (top row first). Each char maps to a color; '.' is skipped.
        public void Bitmap(int x, int y, string[] rows, System.Func<char, Color32?> palette)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                int py = y + rows.Length - 1 - r;
                for (int i = 0; i < rows[r].Length; i++)
                {
                    var c = palette(rows[r][i]);
                    if (c.HasValue) Set(x + i, py, c.Value);
                }
            }
        }

        // Adds a 1px outline around every opaque shape.
        public void Outline(Color32 c)
        {
            var copy = (Color32[])pixels.Clone();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (copy[y * Width + x].a > 0) continue;
                    if (Opaque(copy, x - 1, y) || Opaque(copy, x + 1, y) || Opaque(copy, x, y - 1) || Opaque(copy, x, y + 1))
                        pixels[y * Width + x] = c;
                }
            }
        }

        bool Opaque(Color32[] src, int x, int y) => InBounds(x, y) && src[y * Width + x].a > 0;

        public void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) return;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        // Copies opaque pixels of another canvas, used to pack animation frames into one atlas.
        public void Blit(PixelCanvas src, int ox, int oy)
        {
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    var c = src.pixels[y * src.Width + x];
                    if (c.a > 0) Set(ox + x, oy + y, c);
                }
        }

        public Texture2D ToTexture()
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(pixels);
            // Upload once and drop the CPU copy; these textures are never read back.
            tex.Apply(false, true);
            return tex;
        }

        // FullRect skips runtime tight-mesh generation, which would also need a readable texture.
        public Sprite ToSprite(Vector2 pivot, float pixelsPerUnit = PixelArtLibrary.PixelsPerUnit)
        {
            return Sprite.Create(ToTexture(), new UnityEngine.Rect(0, 0, Width, Height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
    }
}
