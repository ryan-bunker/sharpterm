namespace SharpTerm
{
    public class TextLayout : ITextWriter
    {
        private readonly TextBuffer _buffer;
        
        public TextLayout(TextBuffer buffer)
        {
            _buffer = buffer;
        }
        
        public uint CursorLeft { get; set; }
        public uint CursorTop { get; set; }

        public void Write(string s)
        {
            foreach (var c in s) Write(c);
        }

        public void Write(char c)
        {
            switch (c)
            {
                case '\r':
                    // carriage return means to go back to the far left of the
                    // text array, so reset CursorLeft
                    CursorLeft = 0;
                    break;
                case '\n':
                    // newline currently also contains CR behavior since this
                    // is not yet platform independent
                    CursorLeft = 0;
                    ++CursorTop;
                    break;
                case '\t':
                    // move the cursor to the next tab stop
                    CursorLeft = ((CursorLeft >> 2) << 2) + 4;
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
                    _buffer[CursorLeft, CursorTop] = c;
                    MoveCursorForward();
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