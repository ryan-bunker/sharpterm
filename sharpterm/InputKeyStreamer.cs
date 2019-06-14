using System;
using System.IO;
using Veldrid;

namespace SharpTerm
{
    public class InputKeyStreamer
    {
        private readonly MemoryStream _bufferStream = new MemoryStream(256);
        
        public ITextWriter? ScreenWriter { get; set; }
        public Stream? OutStream { get; set; }
        
        public void ProcessToStream(InputSnapshot input)
        {
            foreach (var c in input.KeyCharPresses)
            {
                _bufferStream.WriteByte((byte) c);
                ScreenWriter?.Write(c);
            }

            foreach (var key in input.KeyEvents)
            {
                switch (key.Key)
                {
                    case Key.Enter:
                        if (!key.Down)
                        {
                            ScreenWriter?.Write('\n');
                            _bufferStream.WriteByte((byte)'\n');
                            SubmitBuffer();
                        }
                        break;
                    case Key.BackSpace:
                        if (key.Down)
                        {
                            if (_bufferStream.Position > 0)
                            {
                                _bufferStream.Position--;
                                ScreenWriter?.Write('\b');
                            }
                        }
                        break;
                }
            }
        }

        private void SubmitBuffer()
        {
            OutStream?.Write(new ReadOnlySpan<byte>(_bufferStream.GetBuffer(), 0, (int) _bufferStream.Position));
            OutStream?.Flush();
            _bufferStream.Position = 0;
        }
    }
}