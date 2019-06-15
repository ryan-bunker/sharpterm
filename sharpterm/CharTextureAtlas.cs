using System;
using System.Numerics;
using SharpFont;
using Veldrid;

namespace SharpTerm
{
    public class CharTextureAtlas
    {
        private readonly Cell[] _cells = new Cell[256];

        public CharTextureAtlas(GraphicsDevice gd, Face face)
        {
            uint fontWidth = (uint)face.Size.Metrics.MaxAdvance.ToInt32();
            uint fontHeight = (uint)face.Size.Metrics.Height.ToInt32();

            Texture = gd.ResourceFactory.CreateTexture(new TextureDescription(
                16 * fontWidth, 16 * fontHeight, 1, 1, 1,
                PixelFormat.R8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));

            uint x = 0, y = 0;
            for (uint ci = 0; ci < 256; ci++)
            {
                var c = (char)ci;
                if (ci % 16 == 0) {
                    x = 0;
                    y = fontHeight * (ci / 16);
                }
                face.LoadChar((uint)Char.ConvertToUtf32(c.ToString(), 0), LoadFlags.Render, LoadTarget.Normal);
                var bmpIn = face.Glyph.Bitmap;
	
                uint xpos = x + (uint)face.Glyph.BitmapLeft;
                uint ypos = y - (uint)face.Glyph.BitmapTop + (uint)face.Size.Metrics.Ascender.ToInt32();
                
                _cells[ci] = new Cell(
                    x / (16f * fontWidth),
                    y / (16f * fontHeight),
                    1f / 16f,
                    1f / 16f);

                gd.UpdateTexture(Texture, bmpIn.Buffer, (uint)(bmpIn.Rows * bmpIn.Width),
                    xpos, ypos, 0, (uint)bmpIn.Width, (uint)bmpIn.Rows, 1, 0, 0);

                x += fontWidth;
            }
        }
        
        public Texture Texture { get; }

        public Cell this[char c] => _cells[c];

        public int Count => _cells.Length;

        public class Cell
        {
            public Vector2 TopLeft { get; }
            public Vector2 TopRight { get; }
            public Vector2 BottomLeft { get; }
            public Vector2 BottomRight { get; }

            public Cell(float x, float y, float width, float height)
            {
                TopLeft = new Vector2(x, y);
                TopRight = new Vector2(x + width, y);
                BottomLeft = new Vector2(x, y + height);
                BottomRight = new Vector2(x + width, y + height);
            }
        }
    }
}