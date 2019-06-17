using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpFont;
using Veldrid;

namespace SharpTerm
{
    public class FontAtlas
    {
        private readonly Cell[] _cells = new Cell[256];

        public FontAtlas(GraphicsDevice gd, string fontPath, uint fontSize)
        {
            FontFace font;
            using (var fontFile = File.OpenRead(fontPath))
                font = new FontFace(fontFile);

            var faceMetrics = font.GetFaceMetrics(fontSize);
            uint fontWidth = (uint) Math.Ceiling(font.GetGlyph('A', fontSize).HorizontalMetrics.LinearAdvance);
            uint fontHeight = (uint) Math.Ceiling(faceMetrics.LineHeight);
            CellWidth = fontWidth;
            CellHeight = fontHeight;

            Texture = gd.ResourceFactory.CreateTexture(new TextureDescription(
                16 * fontWidth, 16 * fontHeight, 1, 1, 1,
                PixelFormat.R8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));

            var bytesNeeded = (int) (fontWidth * fontHeight);
            // move bytesNeeded to the next 8 byte boundary, so we can very quickly fill it
            // with zeros by writing int64's into memory instead of doing it byte by byte
            bytesNeeded = (bytesNeeded + sizeof(long) - 1) & ~(sizeof(long) - 1);
            var surface = new Surface {Bits = Marshal.AllocHGlobal(bytesNeeded)};

            uint x = 0, y = 0;
            for (uint ci = 0; ci < 256; ci++)
            {
                var c = (char)ci;
                if (ci % 16 == 0) {
                    x = 0;
                    y = fontHeight * (ci / 16);
                }

                _cells[ci] = new Cell(
                    x / (16f * fontWidth),
                    y / (16f * fontHeight),
                    1f / 16f,
                    1f / 16f);

                var glyph = font.GetGlyph((char) ci, fontSize);
                if (glyph == null || glyph.RenderWidth == 0 || glyph.RenderHeight == 0) continue;
                
                surface.Width = glyph.RenderWidth;
                surface.Height = glyph.RenderHeight;
                surface.Pitch = glyph.RenderWidth;

                for (int i = 0; i < bytesNeeded; i += sizeof(long))
                    Marshal.WriteInt64(surface.Bits, i, 0);
                
                glyph.RenderTo(surface);

                uint xpos = x + (uint) Math.Floor(glyph.HorizontalMetrics.Bearing.X);
                uint ypos = y - (uint) Math.Floor(glyph.HorizontalMetrics.Bearing.Y) +
                            (uint) Math.Floor(faceMetrics.CellAscent);

                gd.UpdateTexture(Texture, surface.Bits, (uint) (glyph.RenderWidth * glyph.RenderHeight),
                    xpos, ypos, 0, (uint) glyph.RenderWidth, (uint) glyph.RenderHeight, 1, 0, 0);

                x += fontWidth;
            }
        }
        
        public Texture Texture { get; }

        public Cell this[char c] => _cells[c];

        public int Count => _cells.Length;
        
        public uint CellWidth { get; }
        
        public uint CellHeight { get; }

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