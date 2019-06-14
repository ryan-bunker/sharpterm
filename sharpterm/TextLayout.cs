namespace SharpTerm
{
    public class TextLayout
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
            foreach (var c in s)
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
                        CursorLeft = (CursorLeft >> 2) << 2 + 4;
                        break;
                    default:
                        // all other characters are copied to the buffer with proper
                        // line wrapping
                        _buffer[CursorLeft, CursorTop] = c;
                        ++CursorLeft;
                        if (CursorLeft >= _buffer.Width)
                        {
                            CursorLeft = 0;
                            ++CursorTop;
                        }
                        break;
                }
            }
        }
    }
}