using System;
using Veldrid;

namespace SharpTerm
{
    public class TextLayout
    {
        private const uint TabWidth = 8;
        
        private readonly TextBuffer _buffer;
        
        public TextLayout(TextBuffer buffer)
        {
            _buffer = buffer;
        }
        
        public uint CursorLeft { get; set; }
        public uint CursorTop { get; set; }
        
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