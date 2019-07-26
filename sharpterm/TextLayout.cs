#nullable enable
using System;
using System.Collections.Generic;
using Veldrid;

namespace SharpTerm
{
    public class TextLayout
    {
        private const uint TabWidth = 8;

        private static readonly Dictionary<(Set8ColorToken.ColorName, bool), RgbaFloat> ColorLookup8 =
            new Dictionary<(Set8ColorToken.ColorName, bool), RgbaFloat>
            {
                [(Set8ColorToken.ColorName.Black, false)]   = RgbaFloat.Black,
                [(Set8ColorToken.ColorName.Red, false)]     = new RgbaFloat(205 / 255f, 0, 0, 1),
                [(Set8ColorToken.ColorName.Green, false)]   = new RgbaFloat(0, 205 / 255f, 0, 1),
                [(Set8ColorToken.ColorName.Yellow, false)]  = new RgbaFloat(205 / 255f, 205 / 255f, 0, 1),
                [(Set8ColorToken.ColorName.Blue, false)]    = new RgbaFloat(0, 0, 238 / 255f, 1),
                [(Set8ColorToken.ColorName.Magenta, false)] = new RgbaFloat(205 / 255f, 0, 205 / 255f, 1),
                [(Set8ColorToken.ColorName.Cyan, false)]    = new RgbaFloat(0, 205 / 255f, 205 / 255f, 1),
                [(Set8ColorToken.ColorName.White, false)]   = new RgbaFloat(229 / 255f, 229 / 255f, 229 / 255f, 1),
                [(Set8ColorToken.ColorName.Black, true)]    = new RgbaFloat(0.5f, 0.5f, 0.5f, 1),
                [(Set8ColorToken.ColorName.Red, true)]      = new RgbaFloat(1, 0, 0, 1),
                [(Set8ColorToken.ColorName.Green, true)]    = new RgbaFloat(0, 1, 0, 1),
                [(Set8ColorToken.ColorName.Yellow, true)]   = new RgbaFloat(1, 1, 0, 1),
                [(Set8ColorToken.ColorName.Blue, true)]     = new RgbaFloat(92 / 255f, 92 / 255f, 1, 1),
                [(Set8ColorToken.ColorName.Magenta, true)]  = new RgbaFloat(1, 0, 1, 1),
                [(Set8ColorToken.ColorName.Cyan, true)]     = new RgbaFloat(0, 1, 1, 1),
                [(Set8ColorToken.ColorName.White, true)]    = new RgbaFloat(1, 1, 1, 1)
            };
        
        private readonly TextBuffer _buffer;
        
        public TextLayout(TextBuffer buffer)
        {
            _buffer = buffer;
        }
        
        public uint CursorLeft { get; set; }
        public uint CursorTop { get; set; }
        
        public bool IsBold { get; set; }
        
        public RgbaFloat ForeColor { get; set; }

        private void Write(char c)
        {
            switch (c)
            {
                case '\a':
                    // TODO: make a bell sound
                    break;
                case '\r':
                    // carriage return means to go back to the far left of the
                    // text array, so reset CursorLeft
                    CursorLeft = 0;
                    break;
                case '\n':
                    // newline means move down a line
                    ++CursorTop;
                    break;
                case '\t':
                    // move the cursor to the next tab stop
                    CursorLeft += TabWidth - (CursorLeft % TabWidth);
                    break;
                case '\b':
                    // move the cursor back..
                    MoveCursorBack();
                    // .. and clear the new cursor position
                    _buffer[CursorLeft, CursorTop] = null;
                    break;
                case '\u001b':
                    Write('^');
                    break;
                default:
                    // all other characters are copied to the buffer with proper
                    // line wrapping
                    _buffer[CursorLeft, CursorTop] = new CharCell(c, ForeColor);
                    MoveCursorForward();
                    break;
            }
        }

        public void Write(Token t)
        {
            switch (t)
            {
                case CharToken ct:
                    Write(ct.Char);
                    break;

                case SetCursorLocationToken setCursor:
                    CursorLeft = setCursor.Left - 1;
                    CursorTop = setCursor.Top - 1;
                    break;

                case EraseScreenToken eraseScreen:
                    switch (eraseScreen.Bounds)
                    {
                        case EraseScreenToken.EraseBounds.EntireScreen:
                            // TODO: this is only supposed to clear the screen but we're clearing the entire buffer
                            for (uint y = 0; y < _buffer.Height; ++y)
                                for (uint x = 0; x < _buffer.Width; ++x)
                                    _buffer[x, y] = null;
                            CursorLeft = 0;
                            CursorTop = 0;
                            break;

                        case EraseScreenToken.EraseBounds.EntireScreenAndScrollback:
                            for (uint y = 0; y < _buffer.Height; ++y)
                                for (uint x = 0; x < _buffer.Width; ++x)
                                    _buffer[x, y] = null;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                
                case EraseLineToken erase:
                    uint i, end;
                    switch (erase.Bounds)
                    {
                        case EraseLineToken.EraseBounds.BeginningToCursor:
                            i = 0;
                            end = CursorLeft;
                            break;
                        case EraseLineToken.EraseBounds.CursorToEnd:
                            i = CursorLeft;
                            end = _buffer.Width;
                            break;
                        case EraseLineToken.EraseBounds.BeginningToEnd:
                            i = 0;
                            end = _buffer.Width;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    for (; i < end; ++i)
                        _buffer[i, CursorTop] = null;
                    
                    break;
                
                case SgrResetToken _:
                    ForeColor = RgbaFloat.White;
                    break;
                
                case BoldToken bold:
                    IsBold = bold.IsSet;
                    break;
                
                case Set8ColorToken color:
                    if (color.IsForeground)
                        ForeColor = ColorLookup8[(color.Color, IsBold)];
                    break;
            }
        }

        private void MoveCursorBack()
        {
            if (CursorLeft > 0)
            {
                // the cursor wasn't against the left edge so we just decrement and we're done
                --CursorLeft;
                return;
            }
            
            // if we're also on the top line do nothing
            if (CursorTop == 0)
                return;
            
            // at this point we know we're against the left edge but not against the top, so
            // all we need to do is move the cursor to the end of the previous line
            
            // first move up a line
            --CursorTop;
            
            // now begin scanning the line looking for the first non-null char from the right
            for (int x = (int)_buffer.Width - 1; x >= 0; --x)
            {
                if (_buffer[(uint)x, CursorTop] == null) continue;
                // we found a non-null character at (x, CursorTop) so x + 1 is where the cursor should go
                CursorLeft = (uint)x + 1;
                break;
            }
        }

        private void MoveCursorForward()
        {
            ++CursorLeft;
            if (CursorLeft < _buffer.Width) return;
            CursorLeft = 0;
            ++CursorTop;
        }
    }
}